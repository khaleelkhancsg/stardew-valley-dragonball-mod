using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace SaiyanTransformations
{
    /// <summary>Shared charge-fire-cooldown machinery. A technique only has to say what
    /// it costs, how long each phase runs, what it hits and how it draws.</summary>
    internal abstract class Technique
    {
        protected enum Phase { Idle, Charging, Firing, Cooldown }

        protected readonly ModEntry Owner;
        protected readonly FxRenderer Fx;

        protected Phase phase = Phase.Idle;
        protected int ticks;
        protected int facing;
        private int cooldown;
        private bool frozePlayer;

        protected Technique(ModEntry owner, FxRenderer fx)
        {
            this.Owner = owner;
            this.Fx = fx;
        }

        public abstract string Id { get; }
        public abstract string DisplayName { get; }

        /// <summary>Column in assets/technique_icons.png.</summary>
        public abstract int IconIndex { get; }

        public abstract int EnergyCost { get; }
        public abstract int ChargeTicks { get; }
        public abstract int FireTicks { get; }
        public abstract int CooldownMs { get; }

        /// <summary>Lowest form index that can use this. 0 = Super Saiyan.</summary>
        public abstract int MinFormIndex { get; }

        public bool IsActive => this.phase == Phase.Charging || this.phase == Phase.Firing;
        public int CooldownTicks => this.cooldown;

        public bool TryFire(Transformation form, int formIndex)
        {
            if (this.IsActive)
                return false;

            if (form == null)
            {
                ModEntry.Notify("You need to transform first.");
                return false;
            }

            if (!Owner.IsTechniqueUnlocked(this.Id) && this.Id != "Kamehameha")
            {
                ModEntry.Notify($"You have not learned {this.DisplayName} yet.");
                return false;
            }

            if (this.cooldown > 0)
            {
                ModEntry.Notify($"{this.DisplayName} recharging ({(this.cooldown / 60f):0.0}s)");
                return false;
            }

            if (Owner.Ki.IsExhausted)
            {
                ModEntry.Notify("You have no ki left to shape.");
                return false;
            }

            int cost = Math.Max(0, this.EnergyCost);
            if (!Owner.Ki.CanAfford(cost))
            {
                ModEntry.Notify($"Not enough ki ({cost} required).");
                return false;
            }

            Owner.Ki.Spend(cost);
            this.facing = Game1.player.FacingDirection;
            this.phase = Phase.Charging;
            this.ticks = 0;
            this.Freeze();
            this.OnChargeStart(form);
            return true;
        }

        public void Update()
        {
            if (this.cooldown > 0)
                this.cooldown--;

            if (this.phase == Phase.Idle || this.phase == Phase.Cooldown)
            {
                if (this.phase == Phase.Cooldown && this.cooldown <= 0)
                    this.phase = Phase.Idle;
                return;
            }

            Transformation form = Owner.CurrentForm;
            if (form == null || !Context.IsWorldReady)
            {
                this.Cancel();
                return;
            }

            this.ticks++;
            this.Freeze();

            switch (this.phase)
            {
                case Phase.Charging:
                    if (this.ticks >= this.ChargeTicks)
                    {
                        this.phase = Phase.Firing;
                        this.ticks = 0;
                        this.OnFireStart(form);
                    }
                    break;

                case Phase.Firing:
                    this.OnFireTick(form, this.ticks);
                    if (this.ticks >= this.FireTicks)
                    {
                        this.phase = Phase.Cooldown;
                        this.cooldown = Math.Max(0, this.CooldownMs) * 60 / 1000;
                        this.OnFireEnd(form);
                        this.Unfreeze();
                    }
                    break;
            }
        }

        public void Cancel()
        {
            if (this.IsActive)
            {
                this.phase = Phase.Cooldown;
                this.OnCancelled();
            }
            this.ticks = 0;
            this.Unfreeze();
        }

        /// <summary>Hold the farmer still, face the locked direction, and hold a pose.
        /// The pose reuses vanilla sword-swipe frames so it matches the game's art.</summary>
        private void Freeze()
        {
            Farmer player = Game1.player;
            player.canMove = false;
            player.faceDirection(this.facing);
            this.frozePlayer = true;

            int frame = Owner.Config.PoseFrameForDirection(this.facing);
            if (frame >= 0)
                player.FarmerSprite.setCurrentFrame(frame);
        }

        private void Unfreeze()
        {
            if (!this.frozePlayer)
                return;
            Game1.player.canMove = true;
            Game1.player.completelyStopAnimatingOrDoingAction();
            this.frozePlayer = false;
        }

        /// <summary>Where the technique leaves the farmer's hands, in screen pixels.</summary>
        protected Vector2 HandPosition()
        {
            Vector2 anchor = Fx.PlayerAnchor();
            Vector2 hands = new Vector2(anchor.X, anchor.Y - 72f);
            switch (this.facing)
            {
                case 0: hands.Y -= 24f; break;
                case 1: hands.X += 24f; break;
                case 2: hands.Y += 24f; break;
                default: hands.X -= 24f; break;
            }
            return hands;
        }

        /// <summary>Same offsets as <see cref="HandPosition"/> but in world pixels,
        /// for collision rather than drawing.</summary>
        protected Vector2 HandWorldPosition()
        {
            Vector2 origin = Game1.player.Position + new Vector2(32f, 32f);
            switch (this.facing)
            {
                case 0: return origin + new Vector2(0f, -24f);
                case 1: return origin + new Vector2(24f, 0f);
                case 2: return origin + new Vector2(0f, 24f);
                default: return origin + new Vector2(-24f, 0f);
            }
        }

        protected Vector2 FacingVector()
        {
            switch (this.facing)
            {
                case 0: return new Vector2(0f, -1f);
                case 1: return new Vector2(1f, 0f);
                case 2: return new Vector2(0f, 1f);
                default: return new Vector2(-1f, 0f);
            }
        }

        protected float FacingRotation()
        {
            switch (this.facing)
            {
                case 0: return -MathHelper.PiOver2;
                case 1: return 0f;
                case 2: return MathHelper.PiOver2;
                default: return MathHelper.Pi;
            }
        }

        protected int ScaledDamage(Transformation form, float multiplier)
        {
            int baseDamage = 20 + (Game1.player.CombatLevel * 5);
            int damage = (int)(baseDamage * form.KamehamehaMultiplier * multiplier
                               * Owner.Config.KamehamehaDamageScale);
            return Math.Max(1, damage);
        }

        public abstract void Draw(SpriteBatch b, Transformation form);

        protected abstract void OnChargeStart(Transformation form);
        protected abstract void OnFireStart(Transformation form);
        protected abstract void OnFireTick(Transformation form, int tick);
        protected abstract void OnFireEnd(Transformation form);

        protected virtual void OnCancelled()
        {
        }
    }

    /// <summary>Owns the technique list and which one is equipped.</summary>
    internal sealed class TechniqueManager
    {
        private readonly ModEntry Owner;
        private readonly List<Technique> techniques = new List<Technique>();
        private int index;
        private int switchToastTicks;

        public TechniqueManager(ModEntry owner, FxRenderer fx)
        {
            this.Owner = owner;
            this.techniques.Add(new KamehamehaTechnique(owner, fx));
            this.techniques.Add(new DestructoDiskTechnique(owner, fx));
            this.techniques.Add(new SolarFlareTechnique(owner, fx));
            this.techniques.Add(new SpiritBombTechnique(owner, fx));
            this.techniques.Add(new InstantTransmissionTechnique(owner, fx));
            this.techniques.Add(new KaiokenTechnique(owner, fx));
        }

        public Technique Current =>
            this.index >= 0 && this.index < this.techniques.Count
                ? this.techniques[this.index]
                : null;

        public bool AnyActive
        {
            get
            {
                foreach (Technique t in this.techniques)
                {
                    if (t.IsActive)
                        return true;
                }
                return false;
            }
        }

        /// <summary>Kamehameha is the signature move and is always available; the rest
        /// are earned from specific bosses.</summary>
        private bool IsRevealed(Technique technique)
        {
            return technique.Id == "Kamehameha" || Owner.IsTechniqueUnlocked(technique.Id);
        }

        public string NameOf(string techniqueId)
        {
            foreach (Technique t in this.techniques)
            {
                if (t.Id == techniqueId)
                    return t.DisplayName;
            }
            return techniqueId;
        }

        public IEnumerable<Technique> All => this.techniques;

        public bool IsUnlocked(Technique technique) => this.IsRevealed(technique);

        public void Cycle()
        {
            int count = this.techniques.Count;
            for (int step = 1; step <= count; step++)
            {
                int candidate = (this.index + step) % count;
                if (this.IsRevealed(this.techniques[candidate]))
                {
                    if (candidate == this.index)
                        break;
                    this.index = candidate;
                    this.switchToastTicks = 1;
                    ModEntry.SafeSound("smallSelect");
                    ModEntry.Notify($"Technique: {this.Current.DisplayName}");
                    return;
                }
            }
            ModEntry.Notify("No other techniques unlocked yet.");
        }

        public void TryFire(Transformation form, int formIndex)
        {
            this.Current?.TryFire(form, formIndex);
        }

        public void Update()
        {
            if (this.switchToastTicks > 0)
            {
                this.switchToastTicks++;
                if (this.switchToastTicks > 180)
                    this.switchToastTicks = 0;
            }
            foreach (Technique t in this.techniques)
                t.Update();
        }

        public void Cancel()
        {
            foreach (Technique t in this.techniques)
                t.Cancel();
        }

        public void Draw(SpriteBatch b, Transformation form)
        {
            foreach (Technique t in this.techniques)
            {
                if (t.IsActive)
                    t.Draw(b, form);
            }
        }

        /// <summary>Small equipped-technique chip above the toolbar.</summary>
        public void DrawHud(SpriteBatch b)
        {
            Technique current = this.Current;
            if (current == null || Owner.CurrentForm == null)
                return;

            const int box = 44;
            int x = 16;
            int y = Game1.uiViewport.Height - box - 140;

            b.Draw(Game1.staminaRect, new Rectangle(x - 3, y - 3, box + 6, box + 6),
                   Color.Black * 0.55f);

            Color tint = current.CooldownTicks > 0 ? Color.Gray : Color.White;
            b.Draw(Owner.TechniqueIconTexture,
                   new Rectangle(x + 6, y + 6, box - 12, box - 12),
                   new Rectangle(current.IconIndex * 16, 0, 16, 16), tint);

            if (current.CooldownTicks > 0)
            {
                string secs = (current.CooldownTicks / 60f).ToString("0.0");
                Utility.drawTextWithShadow(b, secs, Game1.smallFont,
                    new Vector2(x + box + 6, y + 10), Color.White);
            }
            else if (this.switchToastTicks > 0)
            {
                Utility.drawTextWithShadow(b, current.DisplayName, Game1.smallFont,
                    new Vector2(x + box + 6, y + 10), Color.White);
            }
        }
    }
}
