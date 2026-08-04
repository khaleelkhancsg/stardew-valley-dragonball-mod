using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Buffs;
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

        private const int RushCooldown = 180;      // 3s between dashes
        private const int RushTicks = 30;          // half-second burst
        private const int RushSpeedBonus = 5;      // added to the monster's speed while rushing

        private const int ParalyzeCooldown = 480;  // 8s
        private const int ParalyzeMs = 700;        // frozen this long
        private const float ParalyzeRange = 7f * 64f;

        private const int ShockCooldown = 260;
        private const int ShockTelegraph = 30;
        private const int ShockTotal = 50;
        private const float ShockRadius = 190f;

        // how many ticks after a boom/beam fires it keeps trying to connect
        private const int AoeHitWindow = 4;

        // ---- signature moves ----
        private const int DeathBallCooldown = 460;   // ~7.7s
        private const int DeathBallTelegraph = 55;   // long wind-up: you can run out of it
        private const int DeathBallTotal = 82;
        private const float DeathBallRadius = 260f;

        private const int AbsorbCooldown = 300;      // ~5s
        private const float AbsorbRange = 6f * 64f;
        private const float AbsorbKi = 24f;

        private const int CandyBeamCooldown = 360;   // ~6s
        private const int CandyDebuffMs = 6000;

        private const int TimeStopCooldown = 560;    // ~9.3s
        private const int TimeStopMs = 1400;
        private const float TimeStopRange = 9f * 64f;

        /// <summary>modData key a boss carries once a phase grants it an extra ability.</summary>
        public const string PhaseAbilityKey = "khaleelkhan.SaiyanTransformations/phaseability";

        private enum Kind { Orb, Beam, Boom }

        private sealed class Hazard
        {
            public Kind Kind;
            public Vector2 Pos;       // orb position / beam origin / boom centre
            public Vector2 Vel;       // orb only
            public float Angle;       // beam direction
            public float Length;      // beam length
            public float HalfWidth;   // beam / orb radius / boom radius
            public int Timer;
            public int Life;
            public int Telegraph;     // boom: ticks before it detonates
            public int Total;         // boom: ticks before it clears
            public int Damage;
            public Color Colour;
            public bool Hit;
            public bool Done;
            public bool Reflected;   // orb parried back: now damages monsters, not the player
            public bool Candy;       // beam applies Buu's weaken/slow debuff instead of raw damage
        }

        private sealed class MState
        {
            public int KiCd;
            public int BeamCd;
            public int TeleportCd;
            public int RushCd;
            public int RushTicks;
            public bool Rushing;
            public int ParalyzeCd;
            public int ShockCd;
            public int DeathBallCd;
            public int AbsorbCd;
            public int CandyCd;
            public int TimeStopCd;
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

        /// <summary>Damage from a telegraphed, dodgeable area attack (a shockwave, a beam, a
        /// self-destruct). Unlike a stray projectile these ignore the brief invincibility the
        /// player gets from being grazed in melee - otherwise a boss standing next to you
        /// would have its big wind-up move eaten by its own poke, and it would look like the
        /// attack did nothing. Standing in the blast when it goes off means you take it.</summary>
        private void HitPlayerForce(int damage)
        {
            Farmer p = Game1.player;
            if (p == null || damage <= 0)
                return;
            p.temporaryInvincibilityTimer = 0;
            p.takeDamage(damage, true, null);   // takeDamage grants its own fresh i-frames
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
            // effective abilities = the boss's own plus anything a phase has granted it
            BossAbility abilities = def.Abilities;
            if (monster.modData.TryGetValue(PhaseAbilityKey, out string pa)
                && int.TryParse(pa, out int extra))
            {
                abilities |= (BossAbility)extra;
            }

            bool selfDestruct = def.Abilities.HasFlag(BossAbility.SelfDestruct);
            if (abilities == BossAbility.None && !selfDestruct)
                return;

            if (!this.states.TryGetValue(monster, out MState st))
            {
                st = new MState
                {
                    // stagger initial cooldowns so a squad does not fire in unison
                    KiCd = Game1.random.Next(KiBlastCooldown),
                    BeamCd = Game1.random.Next(BeamCooldown),
                    TeleportCd = Game1.random.Next(TeleportCooldown),
                    RushCd = Game1.random.Next(RushCooldown),
                    ParalyzeCd = Game1.random.Next(ParalyzeCooldown),
                    ShockCd = Game1.random.Next(ShockCooldown),
                    DeathBallCd = Game1.random.Next(DeathBallCooldown),
                    AbsorbCd = Game1.random.Next(AbsorbCooldown),
                    CandyCd = Game1.random.Next(CandyBeamCooldown),
                    TimeStopCd = Game1.random.Next(TimeStopCooldown),
                    SelfDestruct = def.Abilities.HasFlag(BossAbility.SelfDestruct),
                    SelfDestructDamage = def.Abilities.HasFlag(BossAbility.SelfDestruct)
                        ? this.Scaled(def, 1.6f) : 0
                };
                this.states[monster] = st;
            }

            st.Seen = true;
            st.LastCentre = Centre(monster);

            Vector2 target = Centre(Game1.player);

            if (abilities.HasFlag(BossAbility.Regenerate))
                this.Regenerate(monster, st);

            if (abilities.HasFlag(BossAbility.KiBlast) && --st.KiCd <= 0)
            {
                st.KiCd = KiBlastCooldown;
                if (!this.AnyActive(Kind.Orb))           // one volley in flight at a time
                    this.FireKiBlast(def, st.LastCentre, target);
            }

            if (abilities.HasFlag(BossAbility.Beam) && --st.BeamCd <= 0)
            {
                st.BeamCd = BeamCooldown;
                if (!this.AnyActive(Kind.Beam))          // one beam at a time
                    this.FireBeam(def, st.LastCentre, target);
            }

            if (abilities.HasFlag(BossAbility.Teleport) && --st.TeleportCd <= 0)
            {
                st.TeleportCd = TeleportCooldown;
                this.Blink(monster);
            }

            if (abilities.HasFlag(BossAbility.Rush))
                this.UpdateRush(monster, st);

            if (abilities.HasFlag(BossAbility.Paralyze) && --st.ParalyzeCd <= 0)
            {
                st.ParalyzeCd = ParalyzeCooldown;
                this.Paralyze(monster);
            }

            if (abilities.HasFlag(BossAbility.Shockwave) && --st.ShockCd <= 0)
            {
                st.ShockCd = ShockCooldown;
                this.SpawnWave(def, st.LastCentre);
            }

            // ---- signature moves ----
            if (abilities.HasFlag(BossAbility.DeathBall) && --st.DeathBallCd <= 0)
            {
                st.DeathBallCd = DeathBallCooldown;
                this.FireDeathBall(def, target);
            }

            if (abilities.HasFlag(BossAbility.Absorb) && --st.AbsorbCd <= 0)
            {
                st.AbsorbCd = AbsorbCooldown;
                this.DoAbsorb(monster, def);
            }

            if (abilities.HasFlag(BossAbility.CandyBeam) && --st.CandyCd <= 0)
            {
                st.CandyCd = CandyBeamCooldown;
                if (!this.AnyActive(Kind.Beam))
                    this.FireCandyBeam(def, st.LastCentre, target);
            }

            if (abilities.HasFlag(BossAbility.TimeStop) && --st.TimeStopCd <= 0)
            {
                st.TimeStopCd = TimeStopCooldown;
                this.DoTimeStop(monster, def);
            }
        }

        // ------------------------------------------------------------- signature moves

        /// <summary>Frieza's Death Ball: a huge sphere dropped on where you stand, with a long
        /// wind-up (a big red danger ring) so it can be out-run, and a brutal hit if it lands.</summary>
        private void FireDeathBall(BossDefinition def, Vector2 target)
        {
            this.hazards.Add(new Hazard
            {
                Kind = Kind.Boom, Pos = target, HalfWidth = DeathBallRadius,
                Telegraph = DeathBallTelegraph, Total = DeathBallTotal, Timer = 0,
                Damage = this.Scaled(def, 2.2f),
                Colour = new Color(206, 120, 255)
            });
            Owner.PlayCue("kame_charge", "flameSpell");
        }

        /// <summary>Cell's absorption: from range it siphons the player's ki and heals itself,
        /// punishing you for hanging back and topping the fight back up.</summary>
        private void DoAbsorb(Monster monster, BossDefinition def)
        {
            Farmer p = Game1.player;
            if (p == null)
                return;
            if (Vector2.Distance(Centre(monster), Centre(p)) > AbsorbRange)
                return;

            Owner.Ki.Drain(AbsorbKi);
            int heal = Math.Max(1, (int)(monster.MaxHealth * 0.05f));
            monster.Health = Math.Min(monster.MaxHealth, monster.Health + heal);
            Owner.PlayCue("kame_charge", "flameSpell");
            if (Owner.Config.ScreenFlash)
                Game1.flashAlpha = 0.15f;
            ModEntry.Notify("It drains your ki!");
        }

        /// <summary>Buu's candy beam: a telegraphed line that, on hit, weakens and slows you
        /// for a while rather than dealing heavy damage - a threat you must side-step.</summary>
        private void FireCandyBeam(BossDefinition def, Vector2 origin, Vector2 target)
        {
            Vector2 dir = target - origin;
            float angle = (float)Math.Atan2(dir.Y, dir.X);
            this.hazards.Add(new Hazard
            {
                Kind = Kind.Beam, Pos = origin, Angle = angle, Length = 12 * 64f, HalfWidth = 40f,
                Timer = 0, Damage = this.Scaled(def, 0.6f), Candy = true,
                Colour = new Color(255, 150, 220)
            });
            Owner.PlayCue("kame_charge", "flameSpell");
        }

        private void ApplyCandyDebuff()
        {
            Farmer p = Game1.player;
            if (p == null)
                return;
            BuffEffects effects = new BuffEffects();
            effects.Speed.Value = -2;
            effects.AttackMultiplier.Value = -0.3f;
            effects.WeaponSpeedMultiplier.Value = -0.3f;
            p.applyBuff(new Buff(
                id: "khaleelkhan.SaiyanTransformations.Candy",
                displayName: "Turned to Candy",
                description: "Weakened and slowed by Buu's beam.",
                duration: CandyDebuffMs,
                effects: effects));
        }

        /// <summary>Guldo's time-stop: freezes you where you stand, blinks in beside you and
        /// lands a free hit while you cannot act.</summary>
        private void DoTimeStop(Monster monster, BossDefinition def)
        {
            Farmer p = Game1.player;
            if (p == null || p.freezePause > 0f)
                return;
            if (Vector2.Distance(Centre(monster), Centre(p)) > TimeStopRange)
                return;

            p.freezePause = TimeStopMs;
            this.Blink(monster);
            Owner.PlayCue("dodge", "wand");
            if (Owner.Config.ScreenFlash)
                Game1.flashAlpha = 0.25f;
            ModEntry.Notify("Time stops!");
            this.HitPlayerForce(this.Scaled(def, 1.1f));
        }

        /// <summary>Turn every boss ki blast in flight into a player-owned projectile that
        /// homes back the way it came, and let out a counter-burst around the player. The
        /// payoff for a well-timed parry.</summary>
        public void ReflectOrbs(Vector2 playerCentre)
        {
            foreach (Hazard h in this.hazards)
            {
                if (h.Kind != Kind.Orb || h.Done || h.Reflected)
                    continue;
                h.Reflected = true;
                h.Hit = false;
                h.Life = Math.Max(h.Life, 90);

                Vector2 dir = h.Pos - playerCentre;
                if (dir.LengthSquared() < 1f)
                    dir = -h.Vel;
                if (dir.LengthSquared() < 1f)
                    dir = new Vector2(0f, -1f);
                dir.Normalize();
                float speed = h.Vel.Length();
                if (speed < 6f)
                    speed = 9f;
                h.Vel = dir * speed;
                h.Colour = Color.Lerp(h.Colour, Color.White, 0.5f);
            }

            // a counter-burst so a parry always bites, even with nothing to reflect
            GameLocation loc = Game1.currentLocation;
            if (loc != null)
            {
                int cb = 30 + (Game1.player.CombatLevel * 10);
                Rectangle burst = new Rectangle((int)playerCentre.X - 112, (int)playerCentre.Y - 112,
                                                224, 224);
                loc.damageMonster(burst, cb, cb * 2, false, Game1.player);
            }
        }

        /// <summary>A dash: a short, sharp burst of move speed so the boss lunges at the
        /// player through its own chase AI, then drops back to normal.</summary>
        private void UpdateRush(Monster monster, MState st)
        {
            if (st.Rushing)
            {
                if (--st.RushTicks <= 0)
                {
                    monster.speed -= RushSpeedBonus;
                    st.Rushing = false;
                }
                return;
            }

            if (--st.RushCd <= 0)
            {
                st.RushCd = RushCooldown;
                monster.speed += RushSpeedBonus;
                st.RushTicks = RushTicks;
                st.Rushing = true;
                Owner.PlayCue("dodge", "wand");
            }
        }

        /// <summary>Freeze the player where they stand for a moment - only if the boss is close
        /// enough for it to read as a reaching, deliberate grab.</summary>
        private void Paralyze(Monster monster)
        {
            Farmer p = Game1.player;
            if (p == null || p.freezePause > 0f)
                return;
            if (Vector2.Distance(Centre(monster), Centre(p)) > ParalyzeRange)
                return;

            p.freezePause = ParalyzeMs;
            Owner.PlayCue("dodge", "wand");
            if (Owner.Config.ScreenFlash)
                Game1.flashAlpha = 0.3f;
            ModEntry.Notify("Held fast!");
        }

        private void SpawnWave(BossDefinition def, Vector2 centre)
        {
            this.hazards.Add(new Hazard
            {
                Kind = Kind.Boom, Pos = centre, HalfWidth = ShockRadius,
                Telegraph = ShockTelegraph, Total = ShockTotal, Timer = 0,
                Damage = this.Scaled(def, 1.2f),
                Colour = Color.Lerp(def.AuraColor, Color.White, 0.4f)
            });
            Owner.PlayCue("kame_charge", "flameSpell");
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
                Telegraph = BoomTelegraph, Total = BoomTotal,
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
                        if (h.Reflected)
                        {
                            // parried back: this orb now hunts monsters instead of the player
                            Rectangle area = new Rectangle(
                                (int)(h.Pos.X - h.HalfWidth), (int)(h.Pos.Y - h.HalfWidth),
                                (int)(h.HalfWidth * 2f), (int)(h.HalfWidth * 2f));
                            if (!h.Hit && Game1.currentLocation != null
                                && Game1.currentLocation.damageMonster(
                                       area, h.Damage, h.Damage + 1, false, Game1.player))
                            {
                                h.Hit = true;
                                h.Done = true;
                            }
                        }
                        else if (!h.Hit && Vector2.Distance(h.Pos, player) <= h.HalfWidth)
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
                        if (h.Timer == BeamTelegraph)
                            Owner.PlayCue("kame_fire", "explosion");
                        // damage lands across a short window so a single frame of i-frames
                        // (or a step through the beam) does not swallow the whole attack
                        if (h.Timer >= BeamTelegraph && h.Timer <= BeamTelegraph + AoeHitWindow
                            && !h.Hit && this.InCorridor(h, player))
                        {
                            this.HitPlayerForce(h.Damage);
                            if (h.Candy)
                                this.ApplyCandyDebuff();
                            h.Hit = true;
                        }
                        if (h.Timer >= BeamTelegraph + BeamFire)
                            h.Done = true;
                        break;

                    case Kind.Boom:
                        h.Timer++;
                        if (h.Timer == h.Telegraph && Owner.Config.ScreenFlash)
                            Game1.flashAlpha = 0.35f;
                        if (h.Timer >= h.Telegraph && h.Timer <= h.Telegraph + AoeHitWindow
                            && !h.Hit && Vector2.Distance(h.Pos, player) <= h.HalfWidth)
                        {
                            this.HitPlayerForce(h.Damage);
                            h.Hit = true;
                        }
                        if (h.Timer >= h.Total)
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
                        float fullScale = h.HalfWidth / 16f;
                        if (h.Timer < h.Telegraph)
                        {
                            // wind-up: a pulsing red danger ring at the true blast radius so
                            // the player can read exactly where to not be standing
                            float pulse = 0.30f + 0.30f * (float)Math.Sin(h.Timer * 0.4);
                            b.Draw(tex, new Vector2(local.X - (16 * fullScale), local.Y - (16 * fullScale)),
                                   new Rectangle(0, 64, 32, 32), new Color(255, 70, 60) * pulse, 0f,
                                   Vector2.Zero, fullScale, SpriteEffects.None, 0f);
                        }
                        else
                        {
                            // detonation: bright ring snaps out to full radius and fades
                            float t = (h.Timer - h.Telegraph)
                                      / (float)Math.Max(1, h.Total - h.Telegraph);
                            int frame = Math.Min(3, (int)(t * 4f));
                            float scale = (0.55f + (0.45f * t)) * fullScale;
                            b.Draw(tex, new Vector2(local.X - (16 * scale), local.Y - (16 * scale)),
                                   new Rectangle(frame * 32, 64, 32, 32), h.Colour * (1f - (t * 0.6f)), 0f,
                                   Vector2.Zero, scale, SpriteEffects.None, 0f);
                        }
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
