using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Monsters;
using StardewValley.Tools;

namespace SaiyanTransformations
{
    /// <summary>Long-term progression that is not tied to a boss or a wish.</summary>
    public sealed class ProgressSaveData
    {
        /// <summary>Seconds spent holding each form, keyed by Transformation.Id.</summary>
        public Dictionary<string, float> MasterySeconds { get; set; } = new Dictionary<string, float>();

        public float ZenkaiKiBonus { get; set; }
        public float ZenkaiAttackBonus { get; set; }
        public int ZenkaiCount { get; set; }

        /// <summary>Day the last Zenkai fired, so it cannot be farmed in a loop.</summary>
        public int LastZenkaiDay { get; set; } = -1;
    }

    /// <summary>Mastery, Zenkai, the afterimage trail and ki-charged melee.</summary>
    internal sealed class ProgressManager
    {
        private readonly ModEntry Owner;

        private ProgressSaveData Data = new ProgressSaveData();

        // afterimage ring buffer
        private readonly List<Ghost> ghosts = new List<Ghost>();
        private int ghostTimer;

        // zenkai
        private bool zenkaiPending;

        // melee
        private bool wasSwinging;
        private int meleeFlashTicks;
        private int meleeFacing;

        // per-form passive health regen carries a fractional remainder between ticks
        private float healthRegenAcc;

        private readonly HashSet<string> announcedMastery = new HashSet<string>();

        // training: monster health watched between ticks, so damage the player deals can be
        // measured without patching the game's combat path
        private readonly Dictionary<Monster, int> watchedHealth = new Dictionary<Monster, int>();
        private readonly List<Monster> watchedGone = new List<Monster>();
        private GameLocation watchedLocation;
        private int lastPlayerHealth = -1;

        private struct Ghost
        {
            public Vector2 World;
            public Rectangle Source;
            public bool Flipped;
            public int Age;
        }

        public ProgressManager(ModEntry owner)
        {
            this.Owner = owner;
        }

        public ProgressSaveData State => this.Data;

        // ------------------------------------------------------------- save data

        public void LoadSaveData()
        {
            this.Data = Owner.Helper.Data.ReadSaveData<ProgressSaveData>("progress")
                        ?? new ProgressSaveData();
            this.Reset();
        }

        public void WriteSaveData()
        {
            Owner.Helper.Data.WriteSaveData("progress", this.Data);
        }

        public void Reset()
        {
            this.ghosts.Clear();
            this.zenkaiPending = false;
            this.wasSwinging = false;
            this.meleeFlashTicks = 0;
            this.healthRegenAcc = 0f;
            this.announcedMastery.Clear();
            this.watchedHealth.Clear();
            this.watchedLocation = null;
            this.lastPlayerHealth = -1;
        }

        // ------------------------------------------------------------- mastery

        public float MasterySeconds(Transformation form)
        {
            return form != null && this.Data.MasterySeconds.TryGetValue(form.Id, out float s) ? s : 0f;
        }

        /// <summary>0 to 1. Reaching 1 takes MasteryFullMinutes of holding that form.</summary>
        public float MasteryFraction(Transformation form)
        {
            float full = Math.Max(1f, Owner.Config.MasteryFullMinutes) * 60f;
            return MathHelper.Clamp(this.MasterySeconds(form) / full, 0f, 1f);
        }

        /// <summary>True once a form is fully mastered (100%). A mastered form can run
        /// "calm": no visible aura, hum or crackle unless the player is charging ki.</summary>
        public bool IsMastered(Transformation form)
        {
            return form != null && this.MasteryFraction(form) >= 1f;
        }

        /// <summary>Multiplier applied to a form's ki drain. Mastery makes a form cheaper
        /// to hold, which is the whole point: it rewards living in a form rather than
        /// popping it for one fight.</summary>
        public float DrainMultiplier(Transformation form)
        {
            float reduction = MathHelper.Clamp(Owner.Config.MasteryMaxDrainReduction, 0f, 0.9f);
            return 1f - (reduction * this.MasteryFraction(form));
        }

        /// <summary>Total mastery across all forms, each counted by how far it is mastered
        /// (0-1). Fully mastering every form gives Transformation.All.Length. This is the
        /// currency behind the carryover bonuses: they stack, and partial mastery pays out
        /// proportionally, so mastering many forms is always worth it.</summary>
        private float TotalMasteryWeight()
        {
            float sum = 0f;
            foreach (Transformation f in Transformation.All)
                sum += this.MasteryFraction(f);
            return sum;
        }

        /// <summary>Attack-multiplier bonus that mastery grants in EVERY form (and stacks with
        /// the form you are actually in). 0.15 per fully-mastered form by default.</summary>
        public float MasteryGlobalAttackBonus()
        {
            return Owner.Config.EnableMasteryBonuses
                ? this.TotalMasteryWeight() * Math.Max(0f, Owner.Config.MasteryAttackBonusPerForm)
                : 0f;
        }

        /// <summary>Flat defense that mastery grants in every form.</summary>
        public int MasteryGlobalDefenseBonus()
        {
            return Owner.Config.EnableMasteryBonuses
                ? (int)(this.TotalMasteryWeight() * Math.Max(0, Owner.Config.MasteryDefenseBonusPerForm))
                : 0;
        }

        /// <summary>Max-ki bonus that mastery grants at all times.</summary>
        public float MasteryGlobalKiBonus()
        {
            return Owner.Config.EnableMasteryBonuses
                ? this.TotalMasteryWeight() * Math.Max(0f, Owner.Config.MasteryKiBonusPerForm)
                : 0f;
        }

        /// <summary>Fractional boost to the active-charge ki restore rate from mastery, summed
        /// across every mastered form (0.2 = +20%). Stacks the same way the other carryover
        /// bonuses do.</summary>
        public float MasteryChargeRateBonus()
        {
            return Owner.Config.EnableMasteryBonuses
                ? this.TotalMasteryWeight() * Math.Max(0f, Owner.Config.MasteryChargeBonusPerForm)
                : 0f;
        }

        /// <summary>A permanent power gain handed out by a boss "power cache". It rides the
        /// same Zenkai bonus fields, so it feeds straight into max ki and the attack buff and
        /// persists with the save.</summary>
        public void GrantPowerBonus(float ki, float attack)
        {
            this.Data.ZenkaiKiBonus += Math.Max(0f, ki);
            this.Data.ZenkaiAttackBonus += Math.Max(0f, attack);
        }

        private void AccumulateMastery(Transformation form, float seconds)
        {
            if (form == null)
                return;

            this.Data.MasterySeconds.TryGetValue(form.Id, out float current);
            float updated = current + seconds;
            this.Data.MasterySeconds[form.Id] = updated;

            float full = Math.Max(1f, Owner.Config.MasteryFullMinutes) * 60f;
            foreach (float mark in new[] { 0.25f, 0.5f, 0.75f, 1f })
            {
                float threshold = full * mark;
                if (current < threshold && updated >= threshold)
                {
                    string key = form.Id + mark;
                    if (this.announcedMastery.Add(key))
                    {
                        ModEntry.Notify($"{form.DisplayName} mastery {(int)(mark * 100)}% "
                                        + $"- it costs {(int)((1f - this.DrainMultiplier(form)) * 100)}% less to hold.");
                        Owner.PlayCue("unlock", "yoba");
                    }
                }
            }
        }

        // ------------------------------------------------------------- training

        /// <summary>Award training to the form currently held. Strain teaches the form you are
        /// wearing while you feel it, so nothing accrues out of transformation - the same rule
        /// the old time-based mastery followed.</summary>
        public void Train(float seconds)
        {
            if (seconds > 0f && Owner.Config.MasteryFromTraining)
                this.AccumulateMastery(Owner.CurrentForm, seconds);
        }

        /// <summary>Ki spent on an action: a technique, a dash, a block, a ki-charged swing.
        /// Measured against the current pool, so a deep-mine ki bar and an early one are worth
        /// the same. The passive cost of holding a form does not come through here, or standing
        /// still would train you twice over.</summary>
        public void TrainFromKiSpent(float amount)
        {
            float max = Owner.Ki?.Max ?? 0f;
            if (amount > 0f && max > 0f)
                this.Train(amount / max * Math.Max(0f, Owner.Config.MasteryPerKiBarSpent));
        }

        public void TrainFromBossDefeat()
        {
            this.Train(Math.Max(0f, Owner.Config.MasteryPerBossDefeat));
        }

        /// <summary>Reaching a mine floor deeper than the one just left.</summary>
        public void TrainFromDescent(int floors)
        {
            if (floors > 0)
                this.Train(floors * Math.Max(0f, Owner.Config.MasteryPerFloorDescended));
        }

        /// <summary>Damage the player dealt since the last tick, read off monster health because
        /// the game raises no event for it. Each hit counts as the fraction of that monster's
        /// maximum health it removed, so training does not inflate as your damage numbers grow:
        /// clearing a slime at floor 10 is worth what clearing one at floor 200 is.</summary>
        private void TrainFromDamageDealt()
        {
            GameLocation location = Game1.currentLocation;
            if (location != this.watchedLocation)
            {
                // monsters left behind on another floor were walked away from, not killed
                this.watchedHealth.Clear();
                this.watchedLocation = location;
            }
            if (location == null)
                return;

            float perClear = Math.Max(0f, Owner.Config.MasteryPerMonsterCleared);
            float bossFactor = Math.Max(1f, Owner.Config.MasteryBossFactor);
            float earned = 0f;

            this.watchedGone.Clear();
            this.watchedGone.AddRange(this.watchedHealth.Keys);

            foreach (NPC npc in location.characters)
            {
                if (!(npc is Monster monster) || monster.MaxHealth <= 0)
                    continue;

                this.watchedGone.Remove(monster);
                if (this.watchedHealth.TryGetValue(monster, out int before) && monster.Health < before)
                    earned += Share(monster, before - monster.Health);
                this.watchedHealth[monster] = monster.Health;
            }

            // the game removes a monster on the blow that kills it, often before its health is
            // ever seen at zero, so the health it still had is credited when it disappears
            foreach (Monster monster in this.watchedGone)
            {
                if (this.watchedHealth.TryGetValue(monster, out int last) && last > 0)
                    earned += Share(monster, last);
                this.watchedHealth.Remove(monster);
            }

            this.Train(earned);

            float Share(Monster m, int damage)
            {
                float fraction = Math.Min(1f, damage / (float)m.MaxHealth);
                return fraction * perClear
                       * (m.modData.ContainsKey(BossManager.BossKey) ? bossFactor : 1f);
            }
        }

        /// <summary>Damage taken since the last tick, as a share of the health bar. Surviving a
        /// fight that hurt teaches more than winning one that did not.</summary>
        private void TrainFromDamageTaken()
        {
            Farmer player = Game1.player;
            if (player == null || player.maxHealth <= 0)
                return;

            int now = player.health;
            if (this.lastPlayerHealth > now && this.lastPlayerHealth >= 0)
            {
                float share = (this.lastPlayerHealth - now) / (float)player.maxHealth;
                this.Train(share * Math.Max(0f, Owner.Config.MasteryPerHealthBarLost));
            }
            this.lastPlayerHealth = now;
        }

        /// <summary>Per-form passive: bleed health back while the form is held. Fractional
        /// amounts are carried between ticks so even small rates heal over time.</summary>
        private void UpdateFormRegen(Transformation form)
        {
            if (form == null || form.HealthRegenPerSecond <= 0f || !Context.IsPlayerFree)
                return;

            Farmer p = Game1.player;
            if (p == null || p.health >= p.maxHealth)
                return;

            this.healthRegenAcc += form.HealthRegenPerSecond / 60f;
            if (this.healthRegenAcc >= 1f)
            {
                int heal = (int)this.healthRegenAcc;
                this.healthRegenAcc -= heal;
                p.health = Math.Min(p.maxHealth, p.health + heal);
            }
        }

        // ------------------------------------------------------------- zenkai

        private void UpdateZenkai()
        {
            if (!Owner.Config.EnableZenkai)
                return;

            Farmer player = Game1.player;
            float fraction = player.maxHealth > 0 ? (float)player.health / player.maxHealth : 1f;

            if (fraction <= MathHelper.Clamp(Owner.Config.ZenkaiHealthFraction, 0.01f, 0.9f))
            {
                this.zenkaiPending = true;
                return;
            }

            if (!this.zenkaiPending || fraction < 0.5f)
                return;

            // survived it: bank the boost, once per day
            int today = (int)Game1.stats.DaysPlayed;
            this.zenkaiPending = false;
            if (this.Data.LastZenkaiDay == today)
                return;

            this.Data.LastZenkaiDay = today;
            this.Data.ZenkaiCount++;
            this.Data.ZenkaiKiBonus += Math.Max(0f, Owner.Config.ZenkaiKiPerBoost);
            this.Data.ZenkaiAttackBonus += Math.Max(0f, Owner.Config.ZenkaiAttackPerBoost);

            Owner.PlayCue("unlock", "yoba");
            if (Owner.Config.ScreenFlash)
                Game1.flashAlpha = 0.8f;
            ModEntry.Notify($"Zenkai! You come back stronger. (+{Owner.Config.ZenkaiKiPerBoost:0} max ki, "
                            + $"+{Owner.Config.ZenkaiAttackPerBoost:0.##} attack)");
        }

        // ------------------------------------------------------------- ki melee

        private void UpdateMelee(Transformation form)
        {
            if (!Owner.Config.EnableKiMelee || form == null)
                return;

            bool swinging = Game1.player.UsingTool && Game1.player.CurrentTool is MeleeWeapon;
            if (swinging && !this.wasSwinging)
                this.Shockwave(form);
            this.wasSwinging = swinging;
        }

        /// <summary>A swing while transformed throws the strike a little past the blade.</summary>
        private void Shockwave(Transformation form)
        {
            GameLocation location = Game1.currentLocation;
            if (location == null || Owner.Ki.IsExhausted)
                return;

            float cost = Math.Max(0f, Owner.Config.KiMeleeCost);
            if (!Owner.Ki.CanAfford(cost))
                return;
            Owner.Ki.Spend(cost);

            this.meleeFacing = Game1.player.FacingDirection;
            this.meleeFlashTicks = 1;

            int reach = Math.Max(1, Owner.Config.KiMeleeRangeTiles) * 64;
            const int half = 48;
            Vector2 origin = Game1.player.Position + new Vector2(32f, 32f);
            int x = (int)origin.X;
            int y = (int)origin.Y;

            Rectangle area;
            switch (this.meleeFacing)
            {
                case 0: area = new Rectangle(x - half, y - reach, half * 2, reach); break;
                case 1: area = new Rectangle(x, y - half, reach, half * 2); break;
                case 2: area = new Rectangle(x - half, y, half * 2, reach); break;
                default: area = new Rectangle(x - reach, y - half, reach, half * 2); break;
            }

            int baseDamage = 20 + (Game1.player.CombatLevel * 5);
            int damage = Math.Max(1, (int)(baseDamage * form.KamehamehaMultiplier
                                           * Owner.Config.KiMeleeDamageFraction));
            location.damageMonster(area, damage, damage + Math.Max(1, damage / 4),
                                   false, Game1.player);
        }

        // ------------------------------------------------------------- afterimage

        private void UpdateGhosts(Transformation form)
        {
            for (int i = this.ghosts.Count - 1; i >= 0; i--)
            {
                Ghost g = this.ghosts[i];
                g.Age++;
                this.ghosts[i] = g;
                if (g.Age > Owner.Config.AfterimageLifetimeTicks)
                    this.ghosts.RemoveAt(i);
            }

            if (form == null || !Owner.Config.ShowAfterimage || !Context.IsPlayerFree)
                return;
            if (!Game1.player.isMoving() || !Game1.player.running)
                return;

            if (++this.ghostTimer < Math.Max(1, Owner.Config.AfterimageIntervalTicks))
                return;
            this.ghostTimer = 0;

            this.ghosts.Add(new Ghost
            {
                World = Game1.player.Position,
                Source = Game1.player.Sprite.SourceRect,
                Flipped = Game1.player.FacingDirection == 3,
                Age = 0
            });

            while (this.ghosts.Count > Math.Max(1, Owner.Config.AfterimageCount))
                this.ghosts.RemoveAt(0);
        }

        public void DrawGhosts(SpriteBatch b, Transformation form)
        {
            if (form == null || this.ghosts.Count == 0)
                return;

            Texture2D tex = Game1.player.Sprite.Texture;
            if (tex == null)
                return;

            float life = Math.Max(1, Owner.Config.AfterimageLifetimeTicks);
            foreach (Ghost g in this.ghosts)
            {
                float alpha = (1f - (g.Age / life)) * 0.45f;
                if (alpha <= 0.02f)
                    continue;

                Vector2 local = Game1.GlobalToLocal(Game1.viewport, g.World);
                Vector2 pos = new Vector2(local.X + Owner.Config.AuraOffsetX,
                                          local.Y - 64f + Owner.Config.AuraOffsetY);

                b.Draw(tex, pos, g.Source, form.AuraColor * alpha, 0f, Vector2.Zero, 4f,
                       g.Flipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);
            }
        }

        public void DrawMeleeArc(SpriteBatch b, Transformation form)
        {
            if (this.meleeFlashTicks <= 0 || form == null)
                return;

            float t = this.meleeFlashTicks / 12f;
            float scale = 3f + (t * 6f);
            Vector2 anchor = Owner.FxPlayerAnchor();
            Vector2 centre = new Vector2(anchor.X, anchor.Y - 56f);
            switch (this.meleeFacing)
            {
                case 0: centre.Y -= 48f; break;
                case 1: centre.X += 48f; break;
                case 2: centre.Y += 48f; break;
                default: centre.X -= 48f; break;
            }

            b.Draw(Owner.KameTexture,
                   new Vector2(centre.X - (32 * scale / 2f), centre.Y - (32 * scale / 2f)),
                   new Rectangle(Math.Min(3, (int)(t * 4f)) * 32, 64, 32, 32),
                   Color.Lerp(form.AuraColor, Color.White, 0.4f) * (1f - t),
                   0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        public bool HasVisuals => this.ghosts.Count > 0 || this.meleeFlashTicks > 0;

        // ------------------------------------------------------------- tick

        public void Update()
        {
            if (this.meleeFlashTicks > 0)
            {
                this.meleeFlashTicks++;
                if (this.meleeFlashTicks > 12)
                    this.meleeFlashTicks = 0;
            }

            if (!Context.IsWorldReady || Game1.player == null)
                return;

            Transformation form = Owner.CurrentForm;
            bool training = Owner.Config.MasteryFromTraining;

            // holding a form still counts for something, but only a trickle: the bulk of mastery
            // is earned by the strain below, so the form cannot be mastered by standing in it
            if (form != null && Context.IsPlayerFree)
            {
                float rate = training
                    ? MathHelper.Clamp(Owner.Config.MasteryIdleFraction, 0f, 1f)
                    : 1f;
                this.AccumulateMastery(form, rate / 60f);
            }

            if (training && form != null && Context.IsPlayerFree)
            {
                this.TrainFromDamageDealt();
                this.TrainFromDamageTaken();
            }
            else
            {
                // keep the baselines current, so transforming mid-fight cannot pay out a backlog
                this.watchedHealth.Clear();
                this.watchedLocation = null;
                this.lastPlayerHealth = Game1.player?.health ?? -1;
            }

            this.UpdateFormRegen(form);
            this.UpdateZenkai();
            this.UpdateMelee(form);
            this.UpdateGhosts(form);
        }
    }
}
