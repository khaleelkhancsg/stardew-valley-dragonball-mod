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

        /// <summary>Speak through the portrait dialogue system so his own face appears, or fall
        /// back to a HUD line if the world cannot open a dialogue right now.</summary>
        private void Say(string[] lines)
        {
            if (lines == null || lines.Length == 0)
                return;
            if (Context.IsPlayerFree && !Game1.eventUp)
                Owner.ShowSpeechLines("Invader", "Multiversal Invader", lines);
            else
                ModEntry.Notify(lines[0]);
        }

        private static string[] Pick(string[][] pool) => pool[Game1.random.Next(pool.Length)];

        /// <summary>He is met dozens of times a run, so his lines are drawn from deep pools:
        /// first meetings, then an escalating rivalry that remembers being fought.</summary>
        private string[] IntroLines(bool fromMine, int defeats)
        {
            if (defeats <= 0)
                return Pick(fromMine ? FirstMine : FirstOverworld);
            if (defeats >= 5 && Game1.random.NextDouble() < 0.45)
                return Pick(DeepReturn);
            return Pick(fromMine ? ReturnMine : ReturnOverworld);
        }

        private string[] DefeatLines() => Pick(DefeatPool);

        // ---- first meeting, deep in the mine ----------------------------------
        private static readonly string[][] FirstMine =
        {
            new[] { "The rock down here is thin. I felt you bending fate through it from a realm away.",
                    "I have crossed a thousand dead worlds to find a fight worth having.",
                    "So. Show me why yours is still standing." },
            new[] { "Do you feel the seam in the air? I came through it, following the scent of a reality that refuses to end.",
                    "Every world I have walked has gone silent. Yours is still loud. That is why I am here." },
            new[] { "I am not from your saga, farmer. I am from after it. After all of them.",
                    "When your story ends, and it will, I will be the thing that walks out of the dark to watch how." },
            new[] { "You have been carving through echoes down here. Dead men wearing old shapes.",
                    "I am not an echo. I am the one who noticed you doing it." },
            new[] { "A hundred versions of this cavern, and in only one of them are you strong enough to be worth killing.",
                    "Lucky me. Lucky you. Let us find out which cavern this is." },
            new[] { "I have a name, but it belonged to a world that no longer exists to remember it.",
                    "Call me what the others called me before the end: the last thing they saw." },
        };

        // ---- first meeting, out in the overworld ------------------------------
        private static readonly string[][] FirstOverworld =
        {
            new[] { "Your sky tore open for a reason, farmer. I stepped through the seam.",
                    "I do not lurk in caves and wait to be found. I come to where you live.",
                    "Put down the hoe. You will want both hands for this." },
            new[] { "I have hunted champions on battlefields, in throne rooms, at the ends of worlds.",
                    "And now, apparently, on a farm. The multiverse has a sense of humour." },
            new[] { "I felt your power all the way up here, in the open air. You have grown careless with it.",
                    "Power that loud is an invitation. I have accepted." },
            new[] { "There is nowhere you are safe from me. Not the deep dark, not your fields, not your sleep.",
                    "I wanted you to understand that clearly, before we begin." },
            new[] { "You tend your little plot as though the walls between worlds were not paper.",
                    "Allow me to introduce the outside. It has teeth." },
        };

        // ---- returning, in the mine -------------------------------------------
        private static readonly string[][] ReturnMine =
        {
            new[] { "You put me down before. The multiverse is wide. I simply walked back in.",
                    "This body is stronger than the last. I made certain of it." },
            new[] { "The dark remembers you. So do I. I have thought of little else between worlds.",
                    "Shall we continue where the dying left off?" },
            new[] { "I found a reality where I had already beaten you. I studied it for a long, long time.",
                    "Do not worry. I did not enjoy it half as much as I will enjoy the real thing." },
            new[] { "Down here again. Good. The open sky flatters you; the dark tells the truth.",
                    "And the truth is that I am closer every single time." },
            new[] { "Between our fights I temper myself against the corpses of your other selves.",
                    "You are all so similar at the end. I am learning your shape." },
            new[] { "Every death teaches me one more thing you cannot do. My list of your limits grows.",
                    "One day it will be complete. Perhaps today is that day." },
            new[] { "I have walked through the wreckage of nine worlds since I last bled here.",
                    "I brought all of it back with me. For you." },
        };

        // ---- returning, in the overworld --------------------------------------
        private static readonly string[][] ReturnOverworld =
        {
            new[] { "Again. And this time I remember exactly how you move.",
                    "One reality beat me. I have burned through several since. Let us see." },
            new[] { "I could have waited in the mine. I chose your doorstep instead.",
                    "I wanted you to know there is no season, no field, no peace I cannot reach." },
            new[] { "You went back to your little chores, as if I were finished. As if anything is ever finished.",
                    "Nothing ends. That is the first lesson of the multiverse. Let me teach you the rest." },
            new[] { "I have crossed more worlds since we last spoke than you have days in your life.",
                    "And in every one of them, I was thinking of this exact moment." },
            new[] { "The wish you chase leaks its light into every reality at once. I follow it.",
                    "It always leads back to you. You are the knot in the middle of everything. I mean to cut it." },
            new[] { "Look at you, alive, planting seeds, as though our arrangement had lapsed.",
                    "It has not lapsed. It will never lapse. Draw your ki." },
        };

        // ---- returning after many defeats: the rivalry has become something else
        private static readonly string[][] DeepReturn =
        {
            new[] { "We have done this more times than either of us can name. I stopped counting the deaths, yours and mine both.",
                    "You are the only constant I have left. In a strange way, I have come to need you." },
            new[] { "Across a thousand endings, you are the one thing that keeps happening. My rival. My ritual.",
                    "Do not die for good, farmer. I would not know what to do with a quiet multiverse." },
            new[] { "I have forgotten the name of the world I was born in. I have not forgotten one of our fights.",
                    "You are all the history I have left." },
            new[] { "I no longer come for the wish, or the power, or the ending.",
                    "I come for you. That should frighten you more than any of it does." },
            new[] { "Somewhere I have a version of this where we simply talk. I never stay in it long.",
                    "This is the only conversation I know how to finish. Again, then." },
        };

        // ---- on defeat --------------------------------------------------------
        private static readonly string[][] DefeatPool =
        {
            new[] { "...Hah. A worthy reality after all.",
                    "I will find a stronger one, temper myself against it, and return for you." },
            new[] { "Not this shape, then. There are always more shapes.",
                    "Rest, farmer. I will be back before you have forgotten my face." },
            new[] { "You win the moment. You will not win the war. There is no war long enough.",
                    "I have forever. You have a lifetime. Do the arithmetic." },
            new[] { "Good. GOOD. If you had fallen easily I would have had to find someone else.",
                    "And I do not want anyone else. Until the next seam." },
            new[] { "The dark takes me back. It always takes me back.",
                    "And the seam always reopens. Count on it. I do." },
            new[] { "You are improving faster than I am. That is new. That is... interesting.",
                    "I will have to try something drastic. Look forward to it." },
            new[] { "One more world falls quiet behind me, and still you stand.",
                    "You are becoming the last loud thing in all creation. I will be back to hear it." },
        };

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
