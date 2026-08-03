using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.Objects;
using StardewValley.Monsters;
using SObject = StardewValley.Object;

namespace SaiyanTransformations
{
    /// <summary>Persisted wish state.</summary>
    public sealed class WishSaveData
    {
        /// <summary>Extra attack multiplier granted permanently by wishes.</summary>
        public float BonusAttackMultiplier { get; set; }

        /// <summary>Transformations no longer drain energy.</summary>
        public bool FreeTransformations { get; set; }

        public int WishesGranted { get; set; }

        /// <summary>Permanent ki capacity surrendered to the dragon.</summary>
        public float KiCapacityToll { get; set; }
    }

    /// <summary>Dragon Ball items, the summoning ritual, and the wishes.</summary>
    internal sealed class DragonBallManager
    {
        public const string ItemPrefix = "khaleelkhan.SaiyanTransformations_DragonBall";
        public const string TextureAsset = "Mods/khaleelkhan.SaiyanTransformations/dragonballs";
        public const string SenzuId = "khaleelkhan.SaiyanTransformations_SenzuBean";
        public const string SenzuTextureAsset = "Mods/khaleelkhan.SaiyanTransformations/senzu";

        private static readonly string[] Numerals =
            { "One", "Two", "Three", "Four", "Five", "Six", "Seven" };

        public const string TrialKey = "khaleelkhan.SaiyanTransformations/trial";

        private enum Phase { Idle, Darkening, Trial, Asking, Fading }

        private readonly ModEntry Owner;

        private WishSaveData Wishes = new WishSaveData();
        private Phase phase = Phase.Idle;
        private int phaseTicks;
        private GameLocation ritualLocation;
        private List<Vector2> ritualTiles = new List<Vector2>();
        private Vector2 ritualCentre;
        private bool sawTrialAlive;

        public DragonBallManager(ModEntry owner)
        {
            this.Owner = owner;
        }

        public WishSaveData State => this.Wishes;
        public bool RitualActive => this.phase != Phase.Idle;
        public bool TrialActive => this.phase == Phase.Trial;

        public static string ItemId(int number) => ItemPrefix + number;

        // ------------------------------------------------------------ save data

        public void LoadSaveData()
        {
            this.Wishes = Owner.Helper.Data.ReadSaveData<WishSaveData>("wishes") ?? new WishSaveData();
            this.Reset();
        }

        public void WriteSaveData()
        {
            Owner.Helper.Data.WriteSaveData("wishes", this.Wishes);
        }

        public void Reset()
        {
            this.phase = Phase.Idle;
            this.phaseTicks = 0;
            this.ritualLocation = null;
            this.ritualTiles.Clear();
        }

        // ------------------------------------------------------------ item data

        /// <summary>Register the seven balls as craftable-category objects, which is what
        /// makes them placeable on the ground rather than merely carryable.</summary>
        public void EditObjectData(IAssetData asset)
        {
            IDictionary<string, ObjectData> data = asset.AsDictionary<string, ObjectData>().Data;

            for (int i = 1; i <= 7; i++)
            {
                string id = ItemId(i);
                data[id] = new ObjectData
                {
                    Name = $"Dragon Ball ({i}-Star)",
                    DisplayName = $"{Numerals[i - 1]}-Star Dragon Ball",
                    Description = "An orange sphere that hums faintly. Gather all seven and "
                                  + "set them down together under open sky.",
                    Type = "Crafting",
                    Category = SObject.CraftingCategory,
                    Price = 0,
                    Texture = TextureAsset,
                    SpriteIndex = i - 1,
                    Edibility = -300,
                    CanBeGivenAsGift = false,
                    ExcludeFromRandomSale = true,
                    ExcludeFromShippingCollection = true
                };
            }

            data[SenzuId] = new ObjectData
            {
                Name = "Senzu Bean",
                DisplayName = "Senzu Bean",
                Description = "One bean restores a fighter completely. Health and ki, all of it.",
                Type = "Basic",
                Category = SObject.CookingCategory,
                Price = 0,
                Texture = SenzuTextureAsset,
                SpriteIndex = 0,
                Edibility = 40,
                CanBeGivenAsGift = false,
                ExcludeFromRandomSale = true
            };
        }

        public void GrantSenzu(int count)
        {
            Game1.player.addItemByMenuIfNecessary(new SObject(SenzuId, Math.Max(1, count)));
            Owner.PlayCue("dragonball", "yoba");
            ModEntry.Notify(count > 1 ? $"{count} Senzu Beans!" : "A Senzu Bean!");
        }

        public void GrantBall(int number)
        {
            if (number < 1 || number > 7)
                return;

            SObject ball = new SObject(ItemId(number), 1);
            Game1.player.addItemByMenuIfNecessary(ball);
            Owner.PlayCue("dragonball", "yoba");
            ModEntry.Notify($"{Numerals[number - 1]}-Star Dragon Ball recovered!");
        }

        // ------------------------------------------------------------ the ritual

        /// <summary>Called when objects are placed or removed; looks for all seven balls
        /// set down close together outdoors.</summary>
        public void CheckForSummon(GameLocation location)
        {
            if (this.phase != Phase.Idle || location == null || !location.IsOutdoors)
                return;

            Dictionary<int, Vector2> found = new Dictionary<int, Vector2>();
            foreach (KeyValuePair<Vector2, SObject> pair in location.objects.Pairs)
            {
                string id = pair.Value?.ItemId;
                if (string.IsNullOrEmpty(id) || !id.StartsWith(ItemPrefix))
                    continue;

                if (int.TryParse(id.Substring(ItemPrefix.Length), out int number))
                    found[number] = pair.Key;
            }

            if (found.Count < 7)
                return;

            // all seven must be clustered, not scattered across the map
            List<Vector2> tiles = new List<Vector2>(found.Values);
            Vector2 centre = Vector2.Zero;
            foreach (Vector2 tile in tiles)
                centre += tile;
            centre /= tiles.Count;

            foreach (Vector2 tile in tiles)
            {
                if (Vector2.Distance(tile, centre) > Owner.Config.DragonBallClusterRadius)
                {
                    ModEntry.Notify("The spheres hum, but they are too far apart.");
                    return;
                }
            }

            this.BeginRitual(location, tiles);
        }

        private void BeginRitual(GameLocation location, List<Vector2> tiles)
        {
            this.phase = Phase.Darkening;
            this.phaseTicks = 0;
            this.ritualLocation = location;
            this.ritualTiles = tiles;

            Vector2 centre = Vector2.Zero;
            foreach (Vector2 tile in tiles)
                centre += tile;
            this.ritualCentre = centre / Math.Max(1, tiles.Count);
            this.sawTrialAlive = false;

            Owner.PlayCue("shenron", "thunder");
            Game1.player.canMove = false;
            Owner.Monitor.Log("Dragon Ball ritual started.", LogLevel.Info);
        }

        public void Update()
        {
            if (this.phase == Phase.Idle)
                return;

            this.phaseTicks++;

            switch (this.phase)
            {
                case Phase.Darkening:
                    Game1.player.canMove = false;
                    if (this.phaseTicks % 40 == 0)
                        Game1.flashAlpha = 0.6f;
                    if (this.phaseTicks == 60 || this.phaseTicks == 130)
                        ModEntry.SafeSound("thunder");
                    if (this.phaseTicks >= 190)
                    {
                        this.phaseTicks = 0;
                        if (this.NeedsTrial())
                            this.BeginTrial();
                        else
                        {
                            this.phase = Phase.Asking;
                            this.AskForWish();
                        }
                    }
                    break;

                case Phase.Trial:
                    this.UpdateTrial();
                    break;

                case Phase.Asking:
                    Game1.player.canMove = false;
                    // waits for the dialogue callback
                    break;

                case Phase.Fading:
                    if (this.phaseTicks >= 70)
                    {
                        Game1.player.canMove = true;
                        this.Reset();
                    }
                    break;
            }
        }

        private bool NeedsTrial()
        {
            return this.Wishes.WishesGranted >= Math.Max(0, Owner.Config.FreeWishes);
        }

        /// <summary>Past the free wishes the dragon wants proof, not payment. A guardian
        /// appears at the circle and the wish waits until it is dead.</summary>
        private void BeginTrial()
        {
            int cycle = this.Wishes.WishesGranted;
            int spawned = Owner.SpawnWishTrial(this.ritualLocation, this.ritualCentre, cycle);

            if (spawned <= 0)
            {
                // nowhere to put it; do not soft-lock the player out of their wish
                Owner.Monitor.Log("Could not place the wish trial; granting the wish anyway.",
                                  LogLevel.Warn);
                this.phase = Phase.Asking;
                this.AskForWish();
                return;
            }

            this.phase = Phase.Trial;
            this.sawTrialAlive = false;
            Game1.player.canMove = true;
            Owner.PlayCue("boss_roar", "shadowpeep");

            // the dragon takes your strength first: you begin the trial spent
            if (Owner.Config.ExhaustBeforeTrial)
                Owner.Ki.Empty();

            Game1.drawObjectDialogue("A voice fills the dark: YOU HAVE ASKED ENOUGH. "
                                     + "PROVE YOU STILL DESERVE TO ASK."
                                     + "^Your ki is torn out of you. Survive until it returns.");
        }

        private void UpdateTrial()
        {
            if (this.ritualLocation == null)
            {
                this.phase = Phase.Asking;
                this.AskForWish();
                return;
            }

            int alive = 0;
            foreach (NPC npc in this.ritualLocation.characters)
            {
                if (npc is Monster monster && monster.modData.ContainsKey(TrialKey))
                    alive++;
            }

            if (alive > 0)
            {
                this.sawTrialAlive = true;
                return;
            }

            if (!this.sawTrialAlive)
                return;

            Owner.PlayCue("boss_defeat", "explosion");
            this.phase = Phase.Asking;
            this.phaseTicks = 0;
            Game1.player.canMove = false;
            this.AskForWish();
        }

        private void AskForWish()
        {
            List<Response> options = new List<Response>
            {
                new Response("power", "\"Grant me limitless power.\""),
                new Response("body", "\"Make my body unbreakable.\""),
                new Response("riches", "\"Give me riches beyond counting.\""),
                new Response("mastery", "\"Teach me the art of battle.\""),
                new Response("awaken", "\"Awaken the power sleeping in me.\""),
                new Response("endless", "\"Free me from exhaustion.\"")
            };

            Game1.currentLocation.createQuestionDialogue(
                "The sky has gone black. An enormous voice says: SPEAK YOUR WISH.",
                options.ToArray(),
                this.OnWishChosen);
        }

        private void OnWishChosen(Farmer who, string answer)
        {
            string result = this.ApplyWish(answer);

            this.Wishes.WishesGranted++;
            this.ConsumeBalls();
            Owner.OnWishGranted();

            // the price: permanent capacity, and you are left hollow
            string toll = string.Empty;
            if (this.Wishes.WishesGranted > Math.Max(0, Owner.Config.FreeWishes))
            {
                this.Wishes.KiCapacityToll += Math.Max(0f, Owner.Config.KiTollPerWish);
                toll = $"\n\nSomething is taken in exchange: {Owner.Config.KiTollPerWish:0} "
                       + "of your ki capacity, permanently.";
            }


            this.phase = Phase.Fading;
            this.phaseTicks = 0;
            Game1.flashAlpha = 1f;
            ModEntry.SafeSound("thunder");
            Game1.drawObjectDialogue(result
                + "\n\nThe spheres turn to stone and scatter to the winds. "
                + "They will have to be found again." + toll);
        }

        private string ApplyWish(string answer)
        {
            Farmer player = Game1.player;

            // The seven balls now cost a ~290-floor descent to gather, and every wish
            // consumes all of them, so a wish is a rare, hard-won thing. Each one is scaled
            // to feel like the reward for a full run of the mine.
            switch (answer)
            {
                case "power":
                    this.Wishes.BonusAttackMultiplier += 1.0f;
                    Owner.Progress.GrantPowerBonus(80f, 0f);
                    return "Your ki roars. Every transformation now hits "
                           + $"{this.Wishes.BonusAttackMultiplier:0.#}x harder on top of its own "
                           + "multiplier, and your ki reserves swell.";

                case "body":
                    player.maxHealth += 300;
                    player.health = player.maxHealth;
                    player.maxStamina.Value += 400;
                    player.Stamina = player.MaxStamina;
                    Owner.Progress.GrantPowerBonus(120f, 0.05f);
                    return "Your body is remade. +300 max health, +400 max energy, a far deeper "
                           + "ki well and harder strikes - all permanent.";

                case "riches":
                    player.Money += 2000000;
                    return "Gold rains from nowhere. 2,000,000g.";

                case "mastery":
                {
                    // every skill to mastery, not just combat and mining
                    for (int skill = 0; skill <= 4; skill++)
                    {
                        int needed = Math.Max(0, 15000 - player.experiencePoints[skill]);
                        if (needed > 0)
                            player.gainExperience(skill, needed);
                    }
                    player.addItemByMenuIfNecessary(new SObject("74", 25));
                    Owner.Progress.GrantPowerBonus(0f, 0.1f);
                    return "Centuries of battle pour into you. Every skill mastered, your strikes "
                           + "sharpened for good, and twenty-five Prismatic Shards besides.";
                }

                case "awaken":
                {
                    int unlocked = Owner.UnlockedCount();
                    if (unlocked >= Transformation.All.Length)
                    {
                        player.Money += 1500000;
                        Owner.Progress.GrantPowerBonus(150f, 0.1f);
                        return "There is nothing left to awaken - so raw power is poured in "
                               + "instead, and 1,500,000g with it.";
                    }
                    Owner.GrantFormUnlock(unlocked);
                    Owner.Progress.GrantPowerBonus(60f, 0.03f);
                    return $"{Transformation.All[unlocked].DisplayName} awakens within you, "
                           + "and your ki grows to hold it.";
                }

                case "endless":
                    if (this.Wishes.FreeTransformations)
                    {
                        player.Money += 1500000;
                        Owner.Progress.GrantPowerBonus(120f, 0.05f);
                        return "You are already tireless - so the dragon deepens your ki "
                               + "instead, and leaves 1,500,000g.";
                    }
                    this.Wishes.FreeTransformations = true;
                    Owner.Progress.GrantPowerBonus(60f, 0f);
                    return "Exhaustion loses its grip. Transformations no longer drain ki, "
                           + "and your reserves deepen besides.";

                default:
                    player.Money += 500000;
                    return "The voice takes your silence for greed. 500,000g.";
            }
        }

        private void ConsumeBalls()
        {
            if (this.ritualLocation == null)
                return;

            foreach (Vector2 tile in this.ritualTiles)
            {
                if (this.ritualLocation.objects.TryGetValue(tile, out SObject obj)
                    && obj?.ItemId != null
                    && obj.ItemId.StartsWith(ItemPrefix))
                {
                    this.ritualLocation.objects.Remove(tile);
                }
            }
            this.ritualTiles.Clear();
        }

        // ------------------------------------------------------------ drawing

        /// <summary>Sky goes black and green lightning cracks. No dragon is drawn -
        /// the absence is the point.</summary>
        public void DrawWorld(SpriteBatch b)
        {
            if (this.phase == Phase.Idle)
                return;

            float darkness;
            if (this.phase == Phase.Darkening)
                darkness = MathHelper.Clamp(this.phaseTicks / 120f, 0f, 1f) * 0.88f;
            else if (this.phase == Phase.Fading)
                darkness = MathHelper.Clamp(1f - (this.phaseTicks / 70f), 0f, 1f) * 0.88f;
            else if (this.phase == Phase.Trial)
                darkness = 0.55f;   // still ominous, but you can actually see the fight
            else
                darkness = 0.88f;

            Rectangle full = new Rectangle(0, 0, Game1.viewport.Width, Game1.viewport.Height);
            b.Draw(Game1.staminaRect, full, new Color(6, 10, 6) * darkness);

            // green flickers rolling across the dark
            int beat = Owner.AnimTicks % 90;
            if (this.phase != Phase.Fading && beat < 6)
            {
                float flicker = (6 - beat) / 6f * 0.28f;
                b.Draw(Game1.staminaRect, full, new Color(90, 220, 120) * flicker);
            }
        }
    }
}
