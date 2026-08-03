using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Monsters;

namespace SaiyanTransformations
{
    /// <summary>Blinding flash that stuns everything on screen. No damage.</summary>
    internal sealed class SolarFlareTechnique : Technique
    {
        public SolarFlareTechnique(ModEntry owner, FxRenderer fx) : base(owner, fx) { }

        public override string Id => "SolarFlare";
        public override string DisplayName => "Solar Flare";
        public override int IconIndex => 2;
        public override int EnergyCost => Owner.Config.SolarFlareEnergyCost;
        public override int ChargeTicks => 12;
        public override int FireTicks => 26;
        public override int CooldownMs => Owner.Config.SolarFlareCooldownMs;
        public override int MinFormIndex => 0;

        protected override void OnChargeStart(Transformation form)
        {
            Owner.PlayCue("kame_charge", "flameSpell");
        }

        protected override void OnFireStart(Transformation form)
        {
            Owner.PlayCue("kame_fire", "explosion");
            Game1.flashAlpha = 1.2f;

            GameLocation location = Game1.currentLocation;
            if (location == null)
                return;

            int stunned = 0;
            int duration = Math.Max(200, Owner.Config.SolarFlareStunMs);
            foreach (NPC npc in location.characters)
            {
                if (!(npc is Monster monster))
                    continue;
                if (Vector2.Distance(monster.Position, Game1.player.Position)
                    > Owner.Config.SolarFlareRadiusTiles * 64f)
                {
                    continue;
                }

                monster.stunTime.Value = duration;
                monster.Halt();
                stunned++;
            }

            ModEntry.Notify(stunned > 0 ? $"Blinded {stunned} enemies!" : "Nothing was watching.");
        }

        protected override void OnFireTick(Transformation form, int tick) { }

        protected override void OnFireEnd(Transformation form) { }

        public override void Draw(SpriteBatch b, Transformation form)
        {
            Vector2 anchor = Fx.PlayerAnchor();
            Vector2 centre = new Vector2(anchor.X, anchor.Y - 64f);

            float t = this.phase == Phase.Charging
                ? (float)this.ticks / this.ChargeTicks * 0.4f
                : 0.4f + ((float)this.ticks / this.FireTicks * 0.6f);

            float scale = 2f + (t * 26f);
            float alpha = this.phase == Phase.Charging ? t : 1f - ((float)this.ticks / this.FireTicks);

            b.Draw(Owner.KameTexture,
                   new Vector2(centre.X - (32 * scale / 2f), centre.Y - (32 * scale / 2f)),
                   new Rectangle(Math.Min(3, (int)(t * 4f)) * 32, 64, 32, 32),
                   new Color(255, 252, 214) * alpha, 0f, Vector2.Zero, scale,
                   SpriteEffects.None, 0f);
        }
    }

    /// <summary>Long, vulnerable charge into a huge blast centred on you.</summary>
    internal sealed class SpiritBombTechnique : Technique
    {
        public SpiritBombTechnique(ModEntry owner, FxRenderer fx) : base(owner, fx) { }

        public override string Id => "SpiritBomb";
        public override string DisplayName => "Spirit Bomb";
        public override int IconIndex => 3;
        public override int EnergyCost => Owner.Config.SpiritBombEnergyCost;
        public override int ChargeTicks => 170;   // ~2.8s, you are wide open
        public override int FireTicks => 55;
        public override int CooldownMs => Owner.Config.SpiritBombCooldownMs;
        public override int MinFormIndex => 0;

        protected override void OnChargeStart(Transformation form)
        {
            Owner.PlayCue("kame_charge", "flameSpell");
            ModEntry.Notify("Gathering energy...");
        }

        protected override void OnFireStart(Transformation form)
        {
            Owner.PlayCue("kame_fire", "explosion");
            if (Owner.Config.ScreenFlash)
                Game1.flashAlpha = 1.3f;

            GameLocation location = Game1.currentLocation;
            if (location == null)
                return;

            int radius = (int)(Owner.Config.SpiritBombRadiusTiles * 64f);
            Rectangle area = new Rectangle(
                (int)Game1.player.Position.X + 32 - radius,
                (int)Game1.player.Position.Y + 32 - radius,
                radius * 2, radius * 2);

            int damage = this.ScaledDamage(form, Owner.Config.SpiritBombDamageMultiplier);
            location.damageMonster(area, damage, damage + Math.Max(1, damage / 3),
                                   false, Game1.player);
        }

        protected override void OnFireTick(Transformation form, int tick)
        {
            // second and third pulse so it feels like a sustained detonation
            if (tick != 14 && tick != 28)
                return;

            GameLocation location = Game1.currentLocation;
            if (location == null)
                return;

            int radius = (int)(Owner.Config.SpiritBombRadiusTiles * 64f);
            Rectangle area = new Rectangle(
                (int)Game1.player.Position.X + 32 - radius,
                (int)Game1.player.Position.Y + 32 - radius,
                radius * 2, radius * 2);

            int damage = this.ScaledDamage(form, Owner.Config.SpiritBombDamageMultiplier * 0.5f);
            location.damageMonster(area, damage, damage + Math.Max(1, damage / 3),
                                   false, Game1.player);
        }

        protected override void OnFireEnd(Transformation form)
        {
            Owner.PlayCue("kame_impact", "explosion");
        }

        public override void Draw(SpriteBatch b, Transformation form)
        {
            Vector2 anchor = Fx.PlayerAnchor();

            if (this.phase == Phase.Charging)
            {
                float t = (float)this.ticks / this.ChargeTicks;
                float scale = 1.5f + (t * 9f);
                Vector2 centre = new Vector2(anchor.X, anchor.Y - 150f - (t * 60f));
                int frame = (this.ticks / 5) % 4;

                b.Draw(Owner.KameTexture,
                       new Vector2(centre.X - (32 * scale / 2f), centre.Y - (32 * scale / 2f)),
                       new Rectangle(frame * 32, 32, 32, 32),
                       new Color(150, 220, 255) * (0.5f + t * 0.5f), 0f, Vector2.Zero, scale,
                       SpriteEffects.None, 0f);
                b.Draw(Owner.KameTexture,
                       new Vector2(centre.X - (32 * scale * 0.6f / 2f),
                                   centre.Y - (32 * scale * 0.6f / 2f)),
                       new Rectangle(frame * 32, 32, 32, 32),
                       Color.White * (0.3f + t * 0.7f), 0f, Vector2.Zero, scale * 0.6f,
                       SpriteEffects.None, 0f);
                return;
            }

            float ft = (float)this.ticks / this.FireTicks;
            float burst = 6f + (ft * 34f);
            Vector2 mid = new Vector2(anchor.X, anchor.Y - 64f);
            b.Draw(Owner.KameTexture,
                   new Vector2(mid.X - (32 * burst / 2f), mid.Y - (32 * burst / 2f)),
                   new Rectangle(Math.Min(3, (int)(ft * 4f)) * 32, 64, 32, 32),
                   new Color(170, 230, 255) * (1f - ft), 0f, Vector2.Zero, burst,
                   SpriteEffects.None, 0f);
        }
    }

    /// <summary>Two-finger warp home.</summary>
    internal sealed class InstantTransmissionTechnique : Technique
    {
        public InstantTransmissionTechnique(ModEntry owner, FxRenderer fx) : base(owner, fx) { }

        public override string Id => "InstantTransmission";
        public override string DisplayName => "Instant Transmission";
        public override int IconIndex => 4;
        public override int EnergyCost => Owner.Config.InstantTransmissionEnergyCost;
        public override int ChargeTicks => 45;
        public override int FireTicks => 16;
        public override int CooldownMs => Owner.Config.InstantTransmissionCooldownMs;
        public override int MinFormIndex => 0;

        protected override void OnChargeStart(Transformation form)
        {
            Owner.PlayCue("kame_charge", "flameSpell");
        }

        protected override void OnFireStart(Transformation form)
        {
            Owner.PlayCue("dodge", "wand");
            Game1.flashAlpha = 1f;

            // same destination as the Return Scepter: the farmhouse porch
            Game1.warpFarmer("Farm", 64, 15, false);
        }

        protected override void OnFireTick(Transformation form, int tick) { }

        protected override void OnFireEnd(Transformation form)
        {
            Owner.PlayCue("dodge", "wand");
        }

        public override void Draw(SpriteBatch b, Transformation form)
        {
            Vector2 anchor = Fx.PlayerAnchor();
            Vector2 centre = new Vector2(anchor.X, anchor.Y - 64f);

            float t = this.phase == Phase.Charging
                ? (float)this.ticks / this.ChargeTicks
                : 1f;
            float scale = 8f * (1f - (t * 0.75f));
            float alpha = this.phase == Phase.Charging ? t * 0.9f : 0.9f;

            b.Draw(Owner.KameTexture,
                   new Vector2(centre.X - (32 * scale / 2f), centre.Y - (32 * scale / 2f)),
                   new Rectangle(((this.ticks / 4) % 4) * 32, 64, 32, 32),
                   new Color(200, 235, 255) * alpha, 0f, Vector2.Zero, scale,
                   SpriteEffects.None, 0f);
        }
    }

    /// <summary>A multiplier laid on top of whatever form you are already in. It does not
    /// replace the form; it burns ki and your own health for as long as it holds.</summary>
    internal sealed class KaiokenTechnique : Technique
    {
        public KaiokenTechnique(ModEntry owner, FxRenderer fx) : base(owner, fx) { }

        public override string Id => "Kaioken";
        public override string DisplayName => "Kaioken";
        public override int IconIndex => 5;
        public override int EnergyCost => Owner.Config.KaiokenEnergyCost;
        public override int ChargeTicks => 16;
        public override int FireTicks => 20;
        public override int CooldownMs => Owner.Config.KaiokenCooldownMs;
        public override int MinFormIndex => 0;

        protected override void OnChargeStart(Transformation form)
        {
            Owner.PlayCue("kame_charge", "flameSpell");
        }

        protected override void OnFireStart(Transformation form)
        {
            Owner.PlayCue("transform_ssj2", "yoba");
            if (Owner.Config.ScreenFlash)
                Game1.flashAlpha = 1f;
            Owner.BeginKaioken();
        }

        protected override void OnFireTick(Transformation form, int tick) { }

        protected override void OnFireEnd(Transformation form) { }

        public override void Draw(SpriteBatch b, Transformation form)
        {
            Vector2 anchor = Fx.PlayerAnchor();
            float t = this.phase == Phase.Charging
                ? (float)this.ticks / this.ChargeTicks
                : 1f - ((float)this.ticks / this.FireTicks);
            float scale = 4f + (t * 14f);

            b.Draw(Owner.KameTexture,
                   new Vector2(anchor.X - (32 * scale / 2f), anchor.Y - 64f - (32 * scale / 2f)),
                   new Rectangle(Math.Min(3, (int)((1f - t) * 4f)) * 32, 64, 32, 32),
                   new Color(255, 70, 60) * MathHelper.Clamp(t, 0f, 1f),
                   0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
    }
}
