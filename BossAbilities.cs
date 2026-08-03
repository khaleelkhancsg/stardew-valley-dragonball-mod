using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Monsters;

namespace SaiyanTransformations
{
    /// <summary>Runs the special moves marquee bosses use on top of their vanilla melee AI:
    /// aimed ki blasts, telegraphed beams, blink-strikes, self-healing, and a death blast.
    /// It owns a small pool of world hazards (orbs, beams, explosions) that it advances,
    /// draws, and checks against the player each tick. No Harmony patching is involved - the
    /// boss update loop simply hands each live boss monster to this runner.</summary>
    internal sealed class BossAbilityRunner
    {
        private const int KiBlastCooldown = 150;   // 2.5s
        private const int BeamCooldown = 300;      // 5s
        private const int TeleportCooldown = 300;  // 5s
        private const int BeamTelegraph = 45;
        private const int BeamFire = 15;
        private const int BoomTelegraph = 8;
        private const int BoomTotal = 22;

        private enum Kind { Orb, Beam, Boom }

        private sealed class Hazard
        {
            public Kind Kind;
            public Vector2 Pos;       // orb position / beam origin / boom centre
            public Vector2 Vel;       // orb only
            public float Angle;       // beam direction
            public float Length;      // beam length
            public float HalfWidth;   // beam / orb radius
            public int Timer;
            public int Life;
            public int Damage;
            public Color Colour;
            public bool Hit;
            public bool Done;
        }

        private sealed class MState
        {
            public int KiCd;
            public int BeamCd;
            public int TeleportCd;
            public float RegenAcc;
            public bool SelfDestruct;
            public int SelfDestructDamage;
            public Vector2 LastCentre;
            public bool Seen;
        }

        private readonly ModEntry Owner;
        private readonly BossManager Boss;
        private readonly FxRenderer Fx;

        private readonly List<Hazard> hazards = new List<Hazard>();
        private readonly Dictionary<Monster, MState> states = new Dictionary<Monster, MState>();

        public BossAbilityRunner(ModEntry owner, BossManager boss, FxRenderer fx)
        {
            this.Owner = owner;
            this.Boss = boss;
            this.Fx = fx;
        }

        public void Reset()
        {
            this.hazards.Clear();
            this.states.Clear();
        }

        // ------------------------------------------------------------- helpers

        private static Vector2 Centre(Character c)
        {
            Rectangle box = c.GetBoundingBox();
            return new Vector2(box.Center.X, box.Center.Y);
        }

        private void HitPlayer(int damage)
        {
            Farmer p = Game1.player;
            if (p == null || damage <= 0 || p.temporaryInvincibilityTimer > 0)
                return;
            p.takeDamage(damage, true, null);
        }

        private int Scaled(BossDefinition def, float multiple)
        {
            float scale = Math.Max(0f, Owner.Config.BossAbilityDamageScale);
            return Math.Max(1, (int)(this.Boss.EncounterDamage(def) * multiple * scale));
        }

        private bool AnyActive(Kind kind)
        {
            foreach (Hazard h in this.hazards)
                if (h.Kind == kind && !h.Done)
                    return true;
            return false;
        }

        // ------------------------------------------------------------- per monster

        /// <summary>Called for each live boss monster every tick.</summary>
        public void TickMonster(Monster monster, BossDefinition def)
        {
            if (def.Abilities == BossAbility.None)
                return;

            if (!this.states.TryGetValue(monster, out MState st))
            {
                st = new MState
                {
                    // stagger initial cooldowns so a squad does not fire in unison
                    KiCd = Game1.random.Next(KiBlastCooldown),
                    BeamCd = Game1.random.Next(BeamCooldown),
                    TeleportCd = Game1.random.Next(TeleportCooldown),
                    SelfDestruct = def.Abilities.HasFlag(BossAbility.SelfDestruct),
                    SelfDestructDamage = def.Abilities.HasFlag(BossAbility.SelfDestruct)
                        ? this.Scaled(def, 1.6f) : 0
                };
                this.states[monster] = st;
            }

            st.Seen = true;
            st.LastCentre = Centre(monster);

            Vector2 target = Centre(Game1.player);

            if (def.Abilities.HasFlag(BossAbility.Regenerate))
                this.Regenerate(monster, st);

            if (def.Abilities.HasFlag(BossAbility.KiBlast) && --st.KiCd <= 0)
            {
                st.KiCd = KiBlastCooldown;
                if (!this.AnyActive(Kind.Orb))           // one volley in flight at a time
                    this.FireKiBlast(def, st.LastCentre, target);
            }

            if (def.Abilities.HasFlag(BossAbility.Beam) && --st.BeamCd <= 0)
            {
                st.BeamCd = BeamCooldown;
                if (!this.AnyActive(Kind.Beam))          // one beam at a time
                    this.FireBeam(def, st.LastCentre, target);
            }

            if (def.Abilities.HasFlag(BossAbility.Teleport) && --st.TeleportCd <= 0)
            {
                st.TeleportCd = TeleportCooldown;
                this.Blink(monster);
            }
        }

        private void Regenerate(Monster monster, MState st)
        {
            if (monster.Health >= monster.MaxHealth)
                return;
            float perTick = monster.MaxHealth
                            * (Math.Max(0f, Owner.Config.BossRegenPercentPerSecond) / 100f) / 60f;
            st.RegenAcc += perTick;
            if (st.RegenAcc >= 1f)
            {
                int heal = (int)st.RegenAcc;
                st.RegenAcc -= heal;
                monster.Health = Math.Min(monster.MaxHealth, monster.Health + heal);
            }
        }

        private void FireKiBlast(BossDefinition def, Vector2 origin, Vector2 target)
        {
            int damage = this.Scaled(def, 0.7f);
            Vector2 dir = target - origin;
            if (dir.LengthSquared() < 1f)
                dir = new Vector2(0f, 1f);
            float baseAngle = (float)Math.Atan2(dir.Y, dir.X);

            foreach (float spread in new[] { -0.16f, 0f, 0.16f })
            {
                float a = baseAngle + spread;
                Vector2 v = new Vector2((float)Math.Cos(a), (float)Math.Sin(a)) * 9f;
                this.hazards.Add(new Hazard
                {
                    Kind = Kind.Orb, Pos = origin, Vel = v, HalfWidth = 34f,
                    Life = 100, Damage = damage, Colour = Color.Lerp(def.AuraColor, Color.White, 0.4f)
                });
            }
            Owner.PlayCue("kame_fire", "flameSpell");
        }

        private void FireBeam(BossDefinition def, Vector2 origin, Vector2 target)
        {
            Vector2 dir = target - origin;
            float angle = (float)Math.Atan2(dir.Y, dir.X);
            this.hazards.Add(new Hazard
            {
                Kind = Kind.Beam, Pos = origin, Angle = angle, Length = 12 * 64f, HalfWidth = 40f,
                Timer = 0, Damage = this.Scaled(def, 1.4f),
                Colour = Color.Lerp(def.AuraColor, Color.White, 0.5f)
            });
            Owner.PlayCue("kame_charge", "flameSpell");
        }

        private void Blink(Monster monster)
        {
            GameLocation loc = Game1.currentLocation;
            if (loc == null)
                return;
            Vector2 playerTile = Game1.player.Tile;

            for (int i = 0; i < 12; i++)
            {
                int dx = Game1.random.Next(-2, 3);
                int dy = Game1.random.Next(-2, 3);
                if (dx == 0 && dy == 0)
                    continue;
                int tx = (int)playerTile.X + dx;
                int ty = (int)playerTile.Y + dy;
                if (loc.isTilePassable(new xTile.Dimensions.Location(tx, ty), Game1.viewport))
                {
                    monster.Position = new Vector2(tx * 64f, ty * 64f);
                    Owner.PlayCue("dodge", "wand");
                    if (this.Fx != null)
                        Game1.flashAlpha = 0.15f;
                    return;
                }
            }
        }

        /// <summary>After the per-monster pass, any tracked monster we did not see this tick
        /// has died: fire its self-destruct, then drop it from the table.</summary>
        public void SweepDead()
        {
            List<Monster> gone = null;
            foreach (KeyValuePair<Monster, MState> pair in this.states)
            {
                MState st = pair.Value;
                if (st.Seen)
                {
                    st.Seen = false;   // reset for next tick
                    continue;
                }

                if (st.SelfDestruct)
                    this.SpawnBoom(st.LastCentre, st.SelfDestructDamage);

                (gone ??= new List<Monster>()).Add(pair.Key);
            }

            if (gone != null)
                foreach (Monster m in gone)
                    this.states.Remove(m);
        }

        private void SpawnBoom(Vector2 centre, int damage)
        {
            this.hazards.Add(new Hazard
            {
                Kind = Kind.Boom, Pos = centre, HalfWidth = 96f, Timer = 0,
                Damage = damage, Colour = new Color(255, 180, 90)
            });
            Owner.PlayCue("kame_impact", "explosion");
        }

        // ------------------------------------------------------------- hazards

        public void Update()
        {
            if (this.hazards.Count == 0)
                return;

            Vector2 player = Centre(Game1.player);

            for (int i = this.hazards.Count - 1; i >= 0; i--)
            {
                Hazard h = this.hazards[i];
                switch (h.Kind)
                {
                    case Kind.Orb:
                        h.Pos += h.Vel;
                        h.Life--;
                        if (!h.Hit && Vector2.Distance(h.Pos, player) <= h.HalfWidth)
                        {
                            this.HitPlayer(h.Damage);
                            h.Hit = true;
                            h.Done = true;
                        }
                        if (h.Life <= 0)
                            h.Done = true;
                        break;

                    case Kind.Beam:
                        h.Timer++;
                        if (h.Timer == BeamTelegraph && !h.Hit)
                        {
                            if (this.InCorridor(h, player))
                                this.HitPlayer(h.Damage);
                            h.Hit = true;
                            Owner.PlayCue("kame_fire", "explosion");
                        }
                        if (h.Timer >= BeamTelegraph + BeamFire)
                            h.Done = true;
                        break;

                    case Kind.Boom:
                        h.Timer++;
                        if (h.Timer == BoomTelegraph && !h.Hit)
                        {
                            if (Vector2.Distance(h.Pos, player) <= h.HalfWidth)
                                this.HitPlayer(h.Damage);
                            h.Hit = true;
                        }
                        if (h.Timer >= BoomTotal)
                            h.Done = true;
                        break;
                }

                if (h.Done)
                    this.hazards.RemoveAt(i);
                else
                    this.hazards[i] = h;
            }
        }

        private bool InCorridor(Hazard beam, Vector2 p)
        {
            Vector2 dir = new Vector2((float)Math.Cos(beam.Angle), (float)Math.Sin(beam.Angle));
            Vector2 rel = p - beam.Pos;
            float along = Vector2.Dot(rel, dir);
            if (along < 0f || along > beam.Length)
                return false;
            Vector2 perp = new Vector2(-dir.Y, dir.X);
            return Math.Abs(Vector2.Dot(rel, perp)) <= beam.HalfWidth;
        }

        // ------------------------------------------------------------- drawing

        public void Draw(SpriteBatch b)
        {
            if (this.hazards.Count == 0)
                return;

            Texture2D tex = Owner.KameTexture;
            foreach (Hazard h in this.hazards)
            {
                switch (h.Kind)
                {
                    case Kind.Orb:
                    {
                        Vector2 local = Game1.GlobalToLocal(Game1.viewport, h.Pos);
                        int frame = (Owner.AnimTicks / 3) % 4;
                        const float scale = 2.0f;
                        b.Draw(tex, new Vector2(local.X - (16 * scale), local.Y - (16 * scale)),
                               new Rectangle(frame * 32, 32, 32, 32), h.Colour * 0.95f, 0f,
                               Vector2.Zero, scale, SpriteEffects.None, 0f);
                        break;
                    }

                    case Kind.Beam:
                    {
                        bool firing = h.Timer >= BeamTelegraph;
                        float thickness = firing ? h.HalfWidth * 2f : 6f;
                        float alpha = firing
                            ? 1f - ((h.Timer - BeamTelegraph) / (float)BeamFire)
                            : 0.35f + 0.25f * (float)Math.Sin(h.Timer * 0.5);
                        Color colour = firing ? h.Colour : new Color(255, 60, 60);
                        DrawLine(b, h.Pos, h.Angle, h.Length, thickness, colour * alpha);
                        if (firing)
                            DrawLine(b, h.Pos, h.Angle, h.Length, thickness * 0.4f,
                                     Color.White * (alpha * 0.9f));
                        break;
                    }

                    case Kind.Boom:
                    {
                        Vector2 local = Game1.GlobalToLocal(Game1.viewport, h.Pos);
                        float t = h.Timer / (float)BoomTotal;
                        int frame = Math.Min(3, (int)(t * 4f));
                        float scale = 3f + (t * 6f);
                        b.Draw(tex, new Vector2(local.X - (16 * scale), local.Y - (16 * scale)),
                               new Rectangle(frame * 32, 64, 32, 32), h.Colour * (1f - t), 0f,
                               Vector2.Zero, scale, SpriteEffects.None, 0f);
                        break;
                    }
                }
            }
        }

        private static void DrawLine(SpriteBatch b, Vector2 worldOrigin, float angle,
                                     float length, float thickness, Color colour)
        {
            Vector2 local = Game1.GlobalToLocal(Game1.viewport, worldOrigin);
            b.Draw(Game1.staminaRect, local, new Rectangle(0, 0, 1, 1), colour, angle,
                   new Vector2(0f, 0.5f), new Vector2(length, thickness), SpriteEffects.None, 0f);
        }
    }
}
