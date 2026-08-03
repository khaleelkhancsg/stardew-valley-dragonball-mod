using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Monsters;

namespace SaiyanTransformations
{
    /// <summary>The Multiversal Invader: a recurring end-game boss who is never gone for
    /// good. He turns up on the deepest Skull Cavern floors and, once you have been that
    /// far, hunts you in the overworld as well. Every defeat is banked so his next arrival
    /// is stronger and his taunts change. He speaks — in the mine and above ground — which
    /// is what sets him apart from the wordless rival invasions.</summary>
    internal sealed class InvaderManager
    {
        private readonly ModEntry Owner;

        // overworld ambush, rolled each morning like the rivals
        private bool armedToday;
        private int triggerTime = -1;

        // shared fight state
        private bool active;
        private bool sawAlive;
        private GameLocation arena;

        public InvaderManager(ModEntry owner)
        {
            this.Owner = owner;
        }

        public bool Active => this.active;

        public void Reset()
        {
            this.armedToday = false;
            this.triggerTime = -1;
            this.active = false;
            this.sawAlive = false;
            this.arena = null;
        }

        private bool Enabled => Owner.Config.EnableMultiversalInvader;

        /// <summary>He can only be met once the player has actually reached his depth.</summary>
        private bool Unlocked => Owner.DeepestMineLevel() >= Owner.Config.InvaderMineFloor;

        private bool Busy =>
            this.active || Owner.DragonBalls.RitualActive || Owner.BossFightInProgress
            || Owner.RivalActive;

        // -------------------------------------------------------------- overworld

        /// <summary>Roll once each morning for an overworld attack and pick an hour.</summary>
        public void OnDayStarted()
        {
            this.armedToday = false;
            this.triggerTime = -1;

            if (!this.Enabled || !this.Unlocked)
                return;

            int wishes = Owner.DragonBalls.State.WishesGranted;
            float chance = Math.Max(0f, Owner.Config.InvaderOverworldDailyChance) * (1 + wishes);
            if (Game1.random.NextDouble() > chance)
                return;

            int earliest = Math.Max(600, Owner.Config.InvaderEarliestTime);
            int latest = Math.Max(earliest + 100, Owner.Config.InvaderLatestTime);
            int slots = Math.Max(1, (latest - earliest) / 100);
            this.triggerTime = earliest + (Game1.random.Next(slots) * 100);
            this.armedToday = true;

            Owner.Monitor.Log($"Multiversal Invader (overworld) armed for {this.triggerTime}.",
                              LogLevel.Trace);
        }

        public void OnTimeChanged(int newTime)
        {
            if (!this.armedToday || newTime < this.triggerTime)
                return;

            GameLocation location = Game1.currentLocation;
            if (location == null || !Context.IsPlayerFree || !location.IsOutdoors || this.Busy)
                return;

            this.armedToday = false;
            this.Spawn(location, fromMine: false);
        }

        // -------------------------------------------------------------- mine

        /// <summary>Called on every warp. On a deep enough Skull Cavern floor he may already
        /// be waiting.</summary>
        public void OnWarped(GameLocation location)
        {
            if (!this.Enabled)
                return;
            if (!(location is MineShaft shaft) || this.Busy)
                return;
            if (shaft.mineLevel < Owner.Config.InvaderMineFloor)
                return;

            if (Game1.random.NextDouble() > Math.Max(0f, Owner.Config.InvaderMineChance))
                return;

            this.Spawn(location, fromMine: true);
        }

        // -------------------------------------------------------------- spawn

        private void Spawn(GameLocation location, bool fromMine)
        {
            int defeats = Owner.InvaderDefeats;
            int spawned = Owner.SpawnInvader(location, Game1.player.Tile, defeats);
            if (spawned <= 0)
            {
                Owner.Monitor.Log("Multiversal Invader found nowhere to land; skipping.",
                                  LogLevel.Trace);
                return;
            }

            this.active = true;
            this.sawAlive = false;
            this.arena = location;

            Owner.PlayCue("boss_roar", "shadowpeep");
            if (Owner.Config.ScreenFlash)
                Game1.flashAlpha = 0.9f;

            this.Say(this.IntroLines(fromMine, defeats));
        }

        // -------------------------------------------------------------- update

        public void Update()
        {
            if (!this.active)
                return;

            if (this.arena == null || Game1.currentLocation != this.arena)
                return;   // the fight waits while you are elsewhere

            int alive = 0;
            foreach (NPC npc in this.arena.characters)
            {
                if (npc is Monster monster && monster.modData.ContainsKey(BossManager.InvaderKey))
                    alive++;
            }

            if (alive > 0)
            {
                this.sawAlive = true;
                return;
            }

            if (!this.sawAlive)
                return;

            this.Defeat();
        }

        private void Defeat()
        {
            this.active = false;
            this.sawAlive = false;
            this.arena = null;

            Owner.RecordInvaderDefeat();

            int reward = Math.Max(0, Owner.Config.InvaderGoldReward);
            Game1.player.Money += reward;
            Owner.Ki.Fill();
            Owner.DragonBalls.GrantSenzu(2);
            // a permanent trophy for beating the hardest thing in the mod
            Owner.Progress.GrantPowerBonus(35f, 0.05f);

            Owner.PlayCue("boss_defeat", "explosion");
            if (Owner.Config.ScreenFlash)
                Game1.flashAlpha = 0.9f;

            ModEntry.Notify($"Multiversal Invader defeated! {reward:N0}g, 2 Senzu Beans, "
                            + "and your power grows.");
            this.Say(this.DefeatLines());
        }

        // -------------------------------------------------------------- dialogue

        /// <summary>Show a short run of dialogue boxes if the world is in a state that can
        /// display them, otherwise fall back to a HUD line so nothing is ever lost.</summary>
        private void Say(string[] lines)
        {
            if (lines == null || lines.Length == 0)
                return;

            try
            {
                if (Context.IsPlayerFree && !Game1.eventUp)
                {
                    Game1.multipleDialogues(lines);
                    return;
                }
            }
            catch (Exception)
            {
                // fall through to the safe path
            }

            ModEntry.Notify(lines[0]);
        }

        private string[] IntroLines(bool fromMine, int defeats)
        {
            if (defeats <= 0)
            {
                return fromMine
                    ? new[]
                    {
                        "The rock down here is thin. I felt you bending fate through it from a realm away.",
                        "I have crossed a thousand dead worlds to find a fight worth having.",
                        "So. Show me why yours is still standing."
                    }
                    : new[]
                    {
                        "Your sky tore open for a reason, farmer. I stepped through the seam.",
                        "I do not lurk in caves and wait to be found. I come to where you live.",
                        "Put down the hoe. You will want both hands for this."
                    };
            }

            // he remembers being beaten, and it only makes him keener
            return fromMine
                ? new[]
                {
                    $"You put me down before. The multiverse is wide — I simply walked back in.",
                    "This body is stronger than the last. I made certain of it."
                }
                : new[]
                {
                    "Again. And this time I remember exactly how you move.",
                    "One reality beat me. I have burned through several since. Let us see."
                };
        }

        private string[] DefeatLines()
        {
            return new[]
            {
                "...Hah. A worthy reality after all.",
                "I will find a stronger one, temper myself against it, and return for you."
            };
        }

        // -------------------------------------------------------------- drawing

        public void DrawWorld(SpriteBatch b, FxRenderer fx)
        {
            GameLocation location = Game1.currentLocation;
            if (location == null)
                return;

            foreach (NPC npc in location.characters)
            {
                if (npc is Monster monster && monster.modData.ContainsKey(BossManager.InvaderKey))
                {
                    fx.DrawAuraAt(b, fx.MonsterAnchor(monster), new Color(190, 120, 255),
                                  5f, 0.65f, monster.GetHashCode());
                }
            }
        }
    }
}
