using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buffs;

namespace SaiyanTransformations
{
    /// <summary>Persisted ki pool.</summary>
    public sealed class KiSaveData
    {
        public float Current { get; set; } = -1f;   // -1 = fill on first load
        public bool Exhausted { get; set; }
    }

    /// <summary>The ki pool: capacity, regeneration, the charging stance, and the
    /// exhaustion penalty. Everything the mod used to charge to stamina now charges here.</summary>
    internal sealed class KiManager
    {
        private const string ExhaustedBuffId = "khaleelkhan.SaiyanTransformations.Exhausted";

        private readonly ModEntry Owner;

        private KiSaveData Data = new KiSaveData();
        private Vector2 lastPlayerPosition;
        private bool charging;
        private int chargeBlockedTicks;
        private ICue chargeCue;
        private int buffRefresh;

        public KiManager(ModEntry owner)
        {
            this.Owner = owner;
        }

        public bool IsCharging => this.charging;
        public bool IsExhausted => this.Data.Exhausted;
        public float Current => Math.Max(0f, this.Data.Current);

        /// <summary>Capacity grows with the deepest mine floor reached, and is multiplied
        /// by the active transformation's energy multiplier.</summary>
        public float Max
        {
            get
            {
                float baseMax = Owner.Config.BaseMaxKi
                                + (Owner.DeepestMineLevel() * Owner.Config.KiPerMineLevel)
                                + Owner.Progress.State.ZenkaiKiBonus
                                - Owner.DragonBalls.State.KiCapacityToll;

                baseMax = Math.Max(Owner.Config.MinimumBaseKi, baseMax);

                Transformation form = Owner.CurrentForm;
                if (form != null)
                    baseMax *= form.EnergyMultiplier;

                return Math.Max(1f, baseMax);
            }
        }

        public float Fraction => MathHelper.Clamp(this.Current / this.Max, 0f, 1f);

        /// <summary>At or below this fraction, exhaustion sets in.</summary>
        private float EnterFraction =>
            MathHelper.Clamp(Owner.Config.ExhaustionEnterFraction, 0f, 0.9f);

        /// <summary>Fraction that lifts exhaustion, kept at least 0.1 above the enter point
        /// so the state cannot chatter at the threshold.</summary>
        private float ExitFraction =>
            Math.Max(this.EnterFraction + 0.1f, Owner.Config.ExhaustionRecoveryFraction);

        // ------------------------------------------------------------- save data

        public void LoadSaveData()
        {
            this.Data = Owner.Helper.Data.ReadSaveData<KiSaveData>("ki") ?? new KiSaveData();
            if (this.Data.Current < 0f)
                this.Data.Current = this.Max;
            this.Data.Current = MathHelper.Clamp(this.Data.Current, 0f, this.Max);
            this.charging = false;
            this.lastPlayerPosition = Game1.player?.Position ?? Vector2.Zero;
        }

        public void WriteSaveData()
        {
            Owner.Helper.Data.WriteSaveData("ki", this.Data);
        }

        public void Reset()
        {
            this.StopCharging();
            this.Data = new KiSaveData();
        }

        // ------------------------------------------------------------- spending

        public bool CanAfford(float amount) => this.Current >= amount;

        /// <summary>Spend ki. Returns false and spends nothing if there is not enough.</summary>
        public bool Spend(float amount)
        {
            if (amount <= 0f)
                return true;
            if (this.Current < amount)
                return false;

            this.Data.Current -= amount;
            if (this.Data.Current <= 0f)
            {
                this.Data.Current = 0f;
                this.EnterExhaustion();
            }
            return true;
        }

        /// <summary>Drain that is allowed to bottom out, used for the per-second cost of
        /// holding a transformation.</summary>
        public void Drain(float amount)
        {
            if (amount <= 0f)
                return;
            this.Data.Current = Math.Max(0f, this.Data.Current - amount);
            if (this.Data.Current <= 0f)
                this.EnterExhaustion();
        }

        public void Restore(float amount)
        {
            this.Data.Current = MathHelper.Clamp(this.Data.Current + amount, 0f, this.Max);
        }

        /// <summary>Bottom out and enter exhaustion, used as the price of a wish.</summary>
        public void Empty()
        {
            this.Data.Current = 0f;
            this.EnterExhaustion();
        }

        public void Fill()
        {
            this.Data.Current = this.Max;
            this.LeaveExhaustion();
        }

        // ------------------------------------------------------------- exhaustion

        private void EnterExhaustion()
        {
            if (this.Data.Exhausted)
                return;

            this.Data.Exhausted = true;
            this.StopCharging();
            Owner.PowerDown(false);
            this.ApplyExhaustionBuff();   // show the status immediately, not a second later
            this.buffRefresh = 0;
            ModEntry.SafeSound("clank");
            ModEntry.Notify("Your ki is spent. You can barely move.");
        }

        private void LeaveExhaustion()
        {
            if (!this.Data.Exhausted)
                return;

            this.Data.Exhausted = false;
            Owner.RemoveBuffById(ExhaustedBuffId);
            ModEntry.Notify("Your ki steadies. You can fight again.");
        }

        private void ApplyExhaustionBuff()
        {
            float penalty = MathHelper.Clamp(Owner.Config.ExhaustedStatPenalty, 0f, 0.95f);

            BuffEffects effects = new BuffEffects();
            effects.AttackMultiplier.Value = -penalty;
            effects.WeaponSpeedMultiplier.Value = -penalty;
            effects.CriticalChanceMultiplier.Value = -penalty;
            effects.Speed.Value = -(int)Math.Round(5 * penalty);
            effects.Defense.Value = -(int)Math.Round(10 * penalty);

            Game1.player.applyBuff(new Buff(
                id: ExhaustedBuffId,
                displayName: "Ki Exhaustion",
                description: $"Everything is {(int)(penalty * 100)}% weaker until your ki "
                             + $"reaches {(int)(this.ExitFraction * 100)}%.",
                iconTexture: Owner.IconTexture,
                iconSheetIndex: Transformation.All.Length,   // the exhaustion cell
                duration: 3000,
                effects: effects));
        }

        // ------------------------------------------------------------- update

        public void Update()
        {
            if (!Context.IsWorldReady || Game1.player == null)
                return;

            float dt = 1f / 60f;

            // capacity shrinks when a transformation drops, so keep the pool in range
            if (this.Data.Current > this.Max)
                this.Data.Current = this.Max;

            if (this.Data.Exhausted)
            {
                if (++this.buffRefresh >= 60)
                {
                    this.buffRefresh = 0;
                    this.ApplyExhaustionBuff();
                }

                // passive only while exhausted, so a full drain actually costs you
                this.Restore(Owner.Config.PassiveKiPerSecond
                             * Owner.Config.ExhaustedRegenScale * this.SituationalRegenScale * dt);

                if (this.Fraction >= this.ExitFraction)
                    this.LeaveExhaustion();

                this.lastPlayerPosition = Game1.player.Position;
                return;
            }

            // exhaustion sets in at the enter fraction, not only at empty
            if (this.Fraction <= this.EnterFraction)
            {
                this.EnterExhaustion();
                this.lastPlayerPosition = Game1.player.Position;
                return;
            }

            this.UpdateCharging(dt);

            // Passive regeneration only applies in base form. While transformed the aura is
            // burning ki, so nothing should be refilling it unless you deliberately charge
            // (the shift stance) - the transformation drain in ModEntry then wins and the
            // pool falls, as it should.
            if (!this.charging && Owner.CurrentForm == null)
                this.Restore(Owner.Config.PassiveKiPerSecond * this.SituationalRegenScale * dt);

            this.lastPlayerPosition = Game1.player.Position;
        }

        /// <summary>Recovery runs faster during a wish trial, so being stripped of ki at the
        /// start is survivable rather than fatal.</summary>
        private float SituationalRegenScale =>
            Owner.DragonBalls != null && Owner.DragonBalls.TrialActive
                ? Math.Max(1f, Owner.Config.TrialRegenScale)
                : 1f;

        private void UpdateCharging(float dt)
        {
            bool wants = this.WantsToCharge();

            if (wants && !this.charging)
                this.StartCharging();
            else if (!wants && this.charging)
                this.StopCharging();

            if (!this.charging)
                return;

            this.Restore(Owner.Config.ActiveKiPerSecond * dt);
            if (this.Current >= this.Max)
                this.StopCharging();
        }

        private bool WantsToCharge()
        {
            if (this.chargeBlockedTicks > 0)
            {
                this.chargeBlockedTicks--;
                return false;
            }

            if (!Context.IsPlayerFree || Owner.TechniquesActive)
                return false;
            if (!Owner.Config.ChargeKey.IsDown())
                return false;

            // any movement at all cuts the stance
            if (Game1.player.isMoving() || Game1.player.Position != this.lastPlayerPosition)
                return false;
            if (Game1.player.UsingTool || Game1.player.usingSlingshot)
                return false;

            return this.Current < this.Max;
        }

        /// <summary>Taking a hit breaks the stance immediately and locks it out briefly.</summary>
        public void InterruptCharging()
        {
            if (this.charging)
                ModEntry.Notify("Your concentration breaks.");
            this.StopCharging();
            this.chargeBlockedTicks = 30;
        }

        private void StartCharging()
        {
            this.charging = true;
            this.chargeCue = Owner.PlayLoop("aura_loop");
        }

        private void StopCharging()
        {
            this.charging = false;
            ModEntry.StopLoop(ref this.chargeCue);
        }

        // ------------------------------------------------------------- drawing

        /// <summary>Charging aura, drawn in the world layer.</summary>
        public void DrawWorld(SpriteBatch b, FxRenderer fx)
        {
            if (!this.charging)
                return;

            float pulse = 0.55f + 0.25f * (float)Math.Sin(Owner.AnimTicks * 0.14);
            fx.DrawAuraAt(b, fx.PlayerAnchor(), new Color(150, 225, 255), 3.4f, pulse);
        }

        // The hand-drawn bar sprite is 12x56 native, drawn at 4x. Its light interior
        // track (the region the ki level fills) runs source columns 3-8 and rows 13-53,
        // measured from the drawing itself.
        private const int BarW = 12;
        private const int BarH = 56;
        private const int BarScale = 4;
        private const int TrackX = 3;      // first interior column
        private const int TrackW = 6;      // interior width in source pixels
        private const int TrackY = 13;     // first interior row
        private const int TrackH = 41;     // interior height in source pixels

        /// <summary>Vertical ki bar, drawn from the hand-authored sprite so its frame and
        /// badge stay exactly as designed. The mod only paints the ki level into the
        /// interior track on top.</summary>
        public void DrawHud(SpriteBatch b)
        {
            if (!Context.IsWorldReady || Owner.KiBarTexture == null)
                return;

            int x = Game1.uiViewport.Width - 168 + Owner.Config.KiBarOffsetX;
            int y = Game1.uiViewport.Height - 244 + Owner.Config.KiBarOffsetY;

            // the frame first...
            b.Draw(Owner.KiBarTexture, new Rectangle(x, y, BarW * BarScale, BarH * BarScale),
                   null, Color.White);

            int fillX = x + (TrackX * BarScale);
            int fillW = TrackW * BarScale;
            int fillTop = y + (TrackY * BarScale);
            int fillH = TrackH * BarScale;

            float fraction = this.Fraction;
            int filled = (int)(fillH * fraction);

            // cyan to match the badge; red once low or spent, so the danger reads at a glance
            bool low = this.Data.Exhausted
                       || fraction < MathHelper.Clamp(Owner.Config.KiLowFraction, 0f, 1f);
            Color fill;
            if (low)
                fill = new Color(214, 78, 72);
            else if (this.charging)
                fill = Color.Lerp(new Color(0, 162, 232), Color.White,
                                  0.30f + 0.30f * (float)Math.Sin(Owner.AnimTicks * 0.25));
            else
                fill = new Color(0, 162, 232);

            if (filled > 0)
            {
                b.Draw(Game1.staminaRect,
                       new Rectangle(fillX, fillTop + fillH - filled, fillW, filled), fill);
                b.Draw(Game1.staminaRect,
                       new Rectangle(fillX, fillTop + fillH - filled, fillW, Math.Min(4, filled)),
                       Color.Lerp(fill, Color.White, 0.45f));
            }

            // exact value on hover, the way vanilla reveals its own bar numbers
            Point mouse = Game1.getMousePosition();
            Rectangle bounds = this.HudBounds();
            if (bounds.Contains(mouse))
            {
                string label = $"{(int)this.Current}/{(int)this.Max}";
                Vector2 size = Game1.smallFont.MeasureString(label);
                Utility.drawTextWithShadow(b, label, Game1.smallFont,
                    new Vector2(x - size.X - 8, y + (bounds.Height / 2f) - (size.Y / 2f)),
                    Color.White);
            }
        }

        /// <summary>Screen rectangle the bar occupies, so other HUD bits can sit clear of it.</summary>
        public Rectangle HudBounds()
        {
            int x = Game1.uiViewport.Width - 168 + Owner.Config.KiBarOffsetX;
            int y = Game1.uiViewport.Height - 244 + Owner.Config.KiBarOffsetY;
            return new Rectangle(x, y, BarW * BarScale, BarH * BarScale);
        }
    }
}
