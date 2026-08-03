using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Monsters;

namespace SaiyanTransformations
{
    /// <summary>Rival Saiyans who come looking for you rather than waiting in a mine.
    /// Reuses the boss spawner wholesale; only the trigger is new.</summary>
    internal sealed class RivalManager
    {
        public const string RivalKey = "khaleelkhan.SaiyanTransformations/rival";

        private readonly ModEntry Owner;

        private bool armedToday;
        private int triggerTime = -1;
        private bool active;
        private bool sawAlive;
        private GameLocation arena;

        public RivalManager(ModEntry owner)
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

        /// <summary>Roll once each morning and pick an hour for the ambush.</summary>
        public void OnDayStarted()
        {
            this.Reset();

            if (!Owner.Config.EnableRivalInvasions)
                return;
            if (Owner.DeepestMineLevel() < Owner.Config.RivalMinimumMineLevel)
                return;

            int wishes = Owner.DragonBalls.State.WishesGranted;
            float chance = Math.Max(0f, Owner.Config.RivalDailyChance) * (1 + wishes);
            if (Game1.random.NextDouble() > chance)
                return;

            int earliest = Math.Max(600, Owner.Config.RivalEarliestTime);
            int latest = Math.Max(earliest + 100, Owner.Config.RivalLatestTime);
            int slots = Math.Max(1, (latest - earliest) / 100);
            this.triggerTime = earliest + (Game1.random.Next(slots) * 100);
            this.armedToday = true;

            Owner.Monitor.Log($"Rival invasion armed for {this.triggerTime}.", LogLevel.Trace);
        }

        public void OnTimeChanged(int newTime)
        {
            if (!this.armedToday || this.active || newTime < this.triggerTime)
                return;

            GameLocation location = Game1.currentLocation;
            if (location == null || !Context.IsPlayerFree)
                return;

            // outdoors or down a mine shaft, but never mid-ritual or in a boss arena
            bool eligible = location.IsOutdoors || location is StardewValley.Locations.MineShaft;
            if (!eligible || Owner.DragonBalls.RitualActive || Owner.BossFightInProgress)
                return;

            this.armedToday = false;
            this.Spawn(location);
        }

        private void Spawn(GameLocation location)
        {
            int wishes = Owner.DragonBalls.State.WishesGranted;
            int depth = Owner.DeepestMineLevel();
            int spawned = Owner.SpawnRival(location, Game1.player.Tile, wishes, depth);

            if (spawned <= 0)
            {
                Owner.Monitor.Log("Rival invasion found nowhere to land; skipping.", LogLevel.Trace);
                return;
            }

            this.active = true;
            this.sawAlive = false;
            this.arena = location;

            Owner.PlayCue("boss_roar", "shadowpeep");
            if (Owner.Config.ScreenFlash)
                Game1.flashAlpha = 0.7f;
            Game1.drawObjectDialogue("Something lands hard enough to shake the ground. "
                                     + "They have come looking for you.");
        }

        public void Update()
        {
            if (!this.active)
                return;

            if (this.arena == null || Game1.currentLocation != this.arena)
                return;   // fight pauses while you are away; they do not despawn

            int alive = 0;
            foreach (NPC npc in this.arena.characters)
            {
                if (npc is Monster monster && monster.modData.ContainsKey(RivalKey))
                    alive++;
            }

            if (alive > 0)
            {
                this.sawAlive = true;
                return;
            }

            if (!this.sawAlive)
                return;

            this.active = false;
            this.sawAlive = false;

            int reward = (int)Math.Max(0f, Owner.Config.RivalGoldReward);
            Game1.player.Money += reward;
            Owner.Ki.Fill();
            if (Game1.random.NextDouble() < 0.5)
                Owner.DragonBalls.GrantSenzu(1);
            Owner.PlayCue("boss_defeat", "explosion");
            ModEntry.Notify($"Rival defeated! {reward}g, and your ki surges back.");
        }

        /// <summary>Rivals get an aura wherever they turn up, not just in the mine.</summary>
        public void DrawWorld(SpriteBatch b, FxRenderer fx)
        {
            GameLocation location = Game1.currentLocation;
            if (location == null)
                return;

            foreach (NPC npc in location.characters)
            {
                if (npc is Monster monster && monster.modData.ContainsKey(RivalKey))
                {
                    fx.DrawAuraAt(b, fx.MonsterAnchor(monster), new Color(255, 150, 90),
                                  4.4f, 0.6f, monster.GetHashCode());
                }
            }
        }
    }
}
