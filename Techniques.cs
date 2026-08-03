using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Monsters;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using SObject = StardewValley.Object;

namespace SaiyanTransformations
{
    /// <summary>Charged beam that sweeps everything in front of you.</summary>
    internal sealed class KamehamehaTechnique : Technique
    {
        private const int DamageInterval = 9;
        private ICue beamCue;

        public KamehamehaTechnique(ModEntry owner, FxRenderer fx) : base(owner, fx) { }

        public override string Id => "Kamehameha";
        public override string DisplayName => "Kamehameha";
        public override int IconIndex => 0;
        public override int EnergyCost => Owner.Config.KamehamehaEnergyCost;
        public override int ChargeTicks => 42;
        public override int FireTicks => 45;
        public override int CooldownMs => Owner.Config.KamehamehaCooldownMs;
        public override int MinFormIndex => 0;

        private int BeamLengthPixels()
        {
            return Math.Max(1, Owner.Config.KamehamehaRangeTiles) * 64;
        }

        protected override void OnChargeStart(Transformation form)
        {
            Owner.PlayCue("kame_charge", "flameSpell");
        }

        protected override void OnFireStart(Transformation form)
        {
            Owner.PlayCue("kame_fire", "explosion");
            this.beamCue = Owner.PlayLoop("kame_beam_loop");
            if (Owner.Config.ScreenFlash)
                Game1.flashAlpha = 0.7f;
        }

        protected override void OnFireTick(Transformation form, int tick)
        {
            if (tick % DamageInterval != 1)
                return;

            GameLocation location = Game1.currentLocation;
            if (location == null)
                return;

            int damage = this.ScaledDamage(form, 1f);
            location.damageMonster(this.BeamArea(), damage, damage + Math.Max(1, damage / 4),
                                   false, Game1.player);
        }

        protected override void OnFireEnd(Transformation form)
        {
            ModEntry.StopLoop(ref this.beamCue);
            Owner.PlayCue("kame_impact", "explosion");
        }

        protected override void OnCancelled()
        {
            ModEntry.StopLoop(ref this.beamCue);
        }

        private Rectangle BeamArea()
        {
            Vector2 origin = Game1.player.Position + new Vector2(32f, 32f);
            int len = this.BeamLengthPixels();
            const int half = 48;
            int x = (int)origin.X;
            int y = (int)origin.Y;

            switch (this.facing)
            {
                case 0: return new Rectangle(x - half, y - len, half * 2, len);
                case 1: return new Rectangle(x, y - half, len, half * 2);
                case 2: return new Rectangle(x - half, y, half * 2, len);
                default: return new Rectangle(x - len, y - half, len, half * 2);
            }
        }

        public override void Draw(SpriteBatch b, Transformation form)
        {
            Vector2 hands = this.HandPosition();

            if (this.phase == Phase.Charging)
            {
                float t = (float)this.ticks / this.ChargeTicks;
                float scale = 1.0f + (t * 2.2f);
                int frame = (this.ticks / 4) % 4;
                Vector2 pos = new Vector2(hands.X - (32 * scale / 2f), hands.Y - (32 * scale / 2f));

                b.Draw(Owner.KameTexture, pos, new Rectangle(frame * 32, 32, 32, 32),
                       form.AuraColor * 0.9f, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                b.Draw(Owner.KameTexture, pos, new Rectangle(frame * 32, 32, 32, 32),
                       Color.White * (0.4f + t * 0.5f), 0f, Vector2.Zero, scale * 0.55f,
                       SpriteEffects.None, 0f);
                return;
            }

            float extend = Math.Min(1f, this.ticks / 10f);
            float fade = this.ticks > this.FireTicks - 8 ? (this.FireTicks - this.ticks) / 8f : 1f;
            float length = this.BeamLengthPixels() * extend;
            int thickness = (int)(36 + (6 * Math.Sin(this.ticks * 0.6)));
            int beamFrame = (this.ticks / 3) % 4;
            Rectangle src = new Rectangle(beamFrame * 32, 0, 32, 32);
            float rotation = this.FacingRotation();
            Vector2 srcOrigin = new Vector2(0f, 16f);

            b.Draw(Owner.KameTexture,
                   new Rectangle((int)hands.X, (int)hands.Y, (int)length, thickness), src,
                   form.AuraColor * (0.9f * fade), rotation, srcOrigin, SpriteEffects.None, 0f);
            b.Draw(Owner.KameTexture,
                   new Rectangle((int)hands.X, (int)hands.Y, (int)length, thickness / 2), src,
                   Color.White * (0.95f * fade), rotation, srcOrigin, SpriteEffects.None, 0f);

            float muzzle = 3.0f;
            b.Draw(Owner.KameTexture,
                   new Vector2(hands.X - (32 * muzzle / 2f), hands.Y - (32 * muzzle / 2f)),
                   new Rectangle(((this.ticks / 3) % 4) * 32, 32, 32, 32),
                   Color.White * (0.85f * fade), 0f, Vector2.Zero, muzzle, SpriteEffects.None, 0f);

            Vector2 end = hands + (this.FacingVector() * length);
            int burst = (this.ticks / 4) % 4;
            const float burstScale = 3.3f;
            b.Draw(Owner.KameTexture,
                   new Vector2(end.X - (32 * burstScale / 2f), end.Y - (32 * burstScale / 2f)),
                   new Rectangle(burst * 32, 64, 32, 32),
                   Color.Lerp(form.AuraColor, Color.White, 0.5f) * (0.9f * fade),
                   0f, Vector2.Zero, burstScale, SpriteEffects.None, 0f);
        }
    }

    /// <summary>Thrown spinning disc. Travels in a straight line, cuts anything it
    /// passes through, and clears breakable debris on the way.</summary>
    internal sealed class DestructoDiskTechnique : Technique
    {
        private const float SpeedPerTick = 15f;

        private Vector2 worldPosition;
        private float travelled;
        private float spin;
        private readonly HashSet<Monster> alreadyHit = new HashSet<Monster>();

        public DestructoDiskTechnique(ModEntry owner, FxRenderer fx) : base(owner, fx) { }

        public override string Id => "DestructoDisk";
        public override string DisplayName => "Destructo Disk";
        public override int IconIndex => 1;
        public override int EnergyCost => Owner.Config.DestructoDiskEnergyCost;
        public override int ChargeTicks => 20;
        public override int FireTicks => 48;
        public override int CooldownMs => Owner.Config.DestructoDiskCooldownMs;
        public override int MinFormIndex => 1;   // Super Saiyan 2

        private float MaxTravel()
        {
            return Math.Max(1, Owner.Config.DestructoDiskRangeTiles) * 64f;
        }

        protected override void OnChargeStart(Transformation form)
        {
            Owner.PlayCue("kame_charge", "flameSpell");
            this.spin = 0f;
        }

        protected override void OnFireStart(Transformation form)
        {
            this.worldPosition = this.HandWorldPosition();
            this.travelled = 0f;
            this.alreadyHit.Clear();
            Owner.PlayCue("dodge", "swordswipe");
        }

        protected override void OnFireTick(Transformation form, int tick)
        {
            GameLocation location = Game1.currentLocation;
            if (location == null)
                return;

            Vector2 step = this.FacingVector() * SpeedPerTick;
            this.worldPosition += step;
            this.travelled += SpeedPerTick;
            this.spin += 0.55f;

            if (this.travelled >= this.MaxTravel())
            {
                this.ticks = this.FireTicks;   // ends this tick
                return;
            }

            Rectangle hitbox = new Rectangle((int)this.worldPosition.X - 40,
                                             (int)this.worldPosition.Y - 40, 80, 80);

            // damage each monster once per throw, so a fast disc does not shred
            int damage = this.ScaledDamage(form, Owner.Config.DestructoDiskDamageMultiplier);
            foreach (NPC npc in location.characters)
            {
                if (!(npc is Monster monster) || this.alreadyHit.Contains(monster))
                    continue;
                if (!monster.GetBoundingBox().Intersects(hitbox))
                    continue;

                this.alreadyHit.Add(monster);
                location.damageMonster(monster.GetBoundingBox(), damage,
                                       damage + Math.Max(1, damage / 4), false, Game1.player);
            }

            if (Owner.Config.DestructoDiskCutsDebris)
                this.CutDebris(location);
        }

        /// <summary>Cut through the small debris the disc passes over: weeds, single twigs
        /// (loose wood), and mine stones/ore nodes (coal, copper, iron, gold, gems) — which
        /// drop their proper contents. Large resource clumps (stumps, logs, boulders) are
        /// deliberately left intact.</summary>
        private void CutDebris(GameLocation location)
        {
            int tx = (int)(this.worldPosition.X / 64f);
            int ty = (int)(this.worldPosition.Y / 64f);
            Vector2 tile = new Vector2(tx, ty);

            if (!location.objects.TryGetValue(tile, out SObject obj) || obj == null)
                return;

            // IsBreakableStone() covers plain stone and every ore/gem/coal node
            if (obj.IsBreakableStone())
            {
                // spawn the node's ore/gem/coal (and let the mine drop ladders/decrement
                // its stone count) exactly as a pickaxe strike would, then clear the tile
                try { location.OnStoneDestroyed(obj.ItemId, tx, ty, Game1.player); }
                catch (Exception) { }
                location.destroyObject(tile, Game1.player);
            }
            else if (obj.Name == "Weeds" || obj.Name == "Twig")
            {
                // single loose wood / weeds — just clear them out of the path
                location.destroyObject(tile, Game1.player);
            }
            // anything else (big stumps, logs, boulders as resource clumps) is left alone
        }

        protected override void OnFireEnd(Transformation form)
        {
            Owner.PlayCue("kame_impact", "explosion");
            this.alreadyHit.Clear();
        }

        public override void Draw(SpriteBatch b, Transformation form)
        {
            if (this.phase == Phase.Charging)
            {
                // charge it in the hand just like the Kamehameha orb; the disc frames
                // animate their own spin, so no extra rotation is applied here.
                Vector2 hands = this.HandPosition();
                float t = (float)this.ticks / this.ChargeTicks;
                float scale = 1.0f + (t * 2.2f);
                int frame = (this.ticks / 3) % 4;
                Vector2 pos = new Vector2(hands.X - (32 * scale / 2f), hands.Y - (32 * scale / 2f));

                b.Draw(Owner.DiskTexture, pos, new Rectangle(frame * 32, 0, 32, 32),
                       Color.Lerp(form.AuraColor, Color.White, 0.6f) * (0.4f + t * 0.6f),
                       0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                b.Draw(Owner.DiskTexture, pos, new Rectangle(frame * 32, 0, 32, 32),
                       Color.White * (0.3f + t * 0.5f), 0f, Vector2.Zero, scale * 0.55f,
                       SpriteEffects.None, 0f);
                return;
            }

            Vector2 local = Game1.GlobalToLocal(Game1.viewport, this.worldPosition);
            const float diskScale = 3.0f;
            int frame2 = (this.ticks / 2) % 4;

            // glow behind the blade — frames animate the spin, so rotation stays at 0
            b.Draw(Owner.DiskTexture,
                   local, new Rectangle(frame2 * 32, 0, 32, 32),
                   form.AuraColor * 0.55f, 0f,
                   new Vector2(16f, 16f), diskScale * 1.35f, SpriteEffects.None, 0f);

            b.Draw(Owner.DiskTexture,
                   local, new Rectangle(frame2 * 32, 0, 32, 32),
                   Color.White * 0.95f, 0f,
                   new Vector2(16f, 16f), diskScale, SpriteEffects.None, 0f);
        }
    }
}
