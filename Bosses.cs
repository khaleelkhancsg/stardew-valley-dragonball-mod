using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Monsters;
using SObject = StardewValley.Object;

namespace SaiyanTransformations
{
    /// <summary>Vanilla monster types the bosses are built from. Reusing the real
    /// classes means we inherit working AI, pathing and save serialisation - all the
    /// boss character comes from stats, aura and framing instead.</summary>
    internal enum MonsterKind
    {
        GreenSlime,
        ShadowBrute,
        SquidKid,
        Serpent,
        Mummy,
        Skeleton
    }

    internal enum BossReward { Form, Technique, DragonBall, Supplies, PowerCache }

    /// <summary>Special moves a boss can use on top of its inherited vanilla melee AI.</summary>
    [Flags]
    internal enum BossAbility
    {
        None = 0,
        KiBlast = 1,       // aimed orbs flung at the player
        Beam = 2,          // telegraphed line that scorches a corridor
        Teleport = 4,      // blinks in beside the player
        Regenerate = 8,    // heals itself over time
        SelfDestruct = 16  // detonates where it dies
    }

    internal sealed class BossDefinition
    {
        public string Id;
        public string DisplayName;
        public string Subtitle;
        public int MineLevel;

        public BossReward Reward;

        /// <summary>Index into Transformation.All, for Form bosses. -1 otherwise.</summary>
        public int FormIndex = -1;

        /// <summary>Technique.Id granted, for Technique bosses.</summary>
        public string TechniqueId;

        /// <summary>An extra technique granted alongside the main reward, so a single
        /// fight can hand over two things without crowding two bosses onto one floor.</summary>
        public string BonusTechniqueId;

        /// <summary>1-7, for DragonBall bosses.</summary>
        public int DragonBallNumber;

        /// <summary>Supplies bosses: what the cache holds. Defaults preserve the original
        /// 3 beans / 25,000g so existing supply fights are unchanged.</summary>
        public int SupplySenzu = 3;
        public int SupplyGold = 25000;

        /// <summary>PowerCache bosses: a permanent, one-time power gain (max ki and an
        /// attack-multiplier bump) banked the moment the fight is won.</summary>
        public float CacheKi;
        public float CacheAttack;

        /// <summary>Dragon Ball guardians and supply caches come back once a wish
        /// consumes the spheres, so they can be farmed again each run.</summary>
        public bool Repeatable => this.Reward == BossReward.DragonBall
                                 || this.Reward == BossReward.Supplies;

        /// <summary>One entry per monster to spawn. First entry is the leader.</summary>
        public MonsterKind[] Squad;

        public float HealthMultiplier;
        public float DamageMultiplier;
        public int Resilience;
        public float Scale;
        public Color AuraColor;

        /// <summary>Optional per-boss sprite sheet name under assets/monsters/. When set,
        /// every monster in this boss uses it instead of the shared per-kind sheet, so each
        /// boss can look unique. The sheet must match the leader monster kind's frame size.</summary>
        public string SpriteSheet;

        /// <summary>Special moves this boss uses in addition to its vanilla melee AI.</summary>
        public BossAbility Abilities = BossAbility.None;

        /// <summary>Extra move speed over the base monster type. Vanilla monster speeds are
        /// small integers, so keep this in the 1-3 range.</summary>
        public int SpeedBonus;

        /// <summary>Only applies to Green Slimes, which are the one vanilla monster
        /// with a per-instance tint.</summary>
        public Color? SlimeTint;

        public static readonly BossDefinition[] All =
        {
            // ---- the mine -------------------------------------------------
            new BossDefinition
            {
                Id = "Saibamen", DisplayName = "Saibaman Squad",
                Subtitle = "Sprouted from the cavern floor",
                MineLevel = 10, Reward = BossReward.Form, FormIndex = 0,
                Squad = new[] { MonsterKind.GreenSlime, MonsterKind.GreenSlime,
                                MonsterKind.GreenSlime, MonsterKind.GreenSlime },
                HealthMultiplier = 1.0f, DamageMultiplier = 1.0f, Resilience = 1,
                Scale = 1.15f, AuraColor = new Color(120, 230, 90),
                SlimeTint = new Color(96, 200, 72), Abilities = BossAbility.SelfDestruct
            },
            new BossDefinition
            {
                Id = "BladeAdepts", DisplayName = "Blade Adepts",
                Subtitle = "They fight with edges, not fists",
                MineLevel = 30, Reward = BossReward.Technique, TechniqueId = "DestructoDisk",
                Squad = new[] { MonsterKind.Skeleton, MonsterKind.Skeleton },
                HealthMultiplier = 1.0f, DamageMultiplier = 1.0f, Resilience = 2,
                Scale = 1.25f, AuraColor = new Color(210, 240, 255)
            },
            new BossDefinition
            {
                Id = "EliteWarrior", DisplayName = "Elite Saiyan Warrior",
                Subtitle = "He does not need to power up for you",
                MineLevel = 50, Reward = BossReward.Form, FormIndex = 1,
                BonusTechniqueId = "InstantTransmission",
                Squad = new[] { MonsterKind.ShadowBrute },
                HealthMultiplier = 1.15f, DamageMultiplier = 1.1f, Resilience = 3,
                Scale = 1.5f, AuraColor = new Color(180, 120, 255)
            },
            new BossDefinition
            {
                Id = "BallGuardian1", DisplayName = "Guardian of the One-Star Ball",
                Subtitle = "It will not give the sphere up quietly",
                MineLevel = 70, Reward = BossReward.DragonBall, DragonBallNumber = 1,
                Squad = new[] { MonsterKind.Skeleton, MonsterKind.Skeleton, MonsterKind.Skeleton },
                HealthMultiplier = 1.1f, DamageMultiplier = 1.05f, Resilience = 3,
                Scale = 1.3f, AuraColor = new Color(255, 170, 60)
            },
            new BossDefinition
            {
                Id = "GinyuSquad", DisplayName = "The Ginyu Squad",
                Subtitle = "They insist on posing first",
                MineLevel = 90, Reward = BossReward.Form, FormIndex = 2,
                Squad = new[] { MonsterKind.ShadowBrute, MonsterKind.SquidKid, MonsterKind.SquidKid },
                HealthMultiplier = 1.15f, DamageMultiplier = 1.1f, Resilience = 3,
                Scale = 1.3f, AuraColor = new Color(255, 150, 60)
            },
            new BossDefinition
            {
                Id = "KiAdepts", DisplayName = "Ki Adepts",
                Subtitle = "Light gathers unpleasantly around them",
                MineLevel = 110, Reward = BossReward.Technique, TechniqueId = "SolarFlare",
                Squad = new[] { MonsterKind.SquidKid, MonsterKind.SquidKid, MonsterKind.SquidKid },
                HealthMultiplier = 1.05f, DamageMultiplier = 1.15f, Resilience = 4,
                Scale = 1.25f, AuraColor = new Color(255, 245, 170)
            },
            new BossDefinition
            {
                Id = "Emperor", DisplayName = "Emperor of the Universe",
                Subtitle = "This is not even his final form",
                MineLevel = 140, Reward = BossReward.Form, FormIndex = 3,
                Squad = new[] { MonsterKind.Serpent },
                HealthMultiplier = 1.25f, DamageMultiplier = 1.15f, Resilience = 5,
                Scale = 1.4f, AuraColor = new Color(255, 90, 190), Abilities = BossAbility.Beam
            },
            new BossDefinition
            {
                Id = "BallGuardian2", DisplayName = "Guardian of the Two-Star Ball",
                Subtitle = "Coiled around its prize",
                MineLevel = 130, Reward = BossReward.DragonBall, DragonBallNumber = 2,
                BonusTechniqueId = "SpiritBomb",
                Squad = new[] { MonsterKind.Serpent },
                HealthMultiplier = 1.15f, DamageMultiplier = 1.1f, Resilience = 5,
                Scale = 1.4f, AuraColor = new Color(255, 170, 60)
            },
            new BossDefinition
            {
                Id = "PerfectAndroid", DisplayName = "The Perfect Android",
                Subtitle = "It regenerates. Finish it properly.",
                MineLevel = 190, Reward = BossReward.Form, FormIndex = 4,
                Squad = new[] { MonsterKind.Mummy },
                HealthMultiplier = 1.3f, DamageMultiplier = 1.2f, Resilience = 6,
                Scale = 1.6f, AuraColor = new Color(140, 240, 140),
                Abilities = BossAbility.Beam | BossAbility.Regenerate
            },
            new BossDefinition
            {
                Id = "BallGuardian3", DisplayName = "Guardian of the Three-Star Ball",
                Subtitle = "Two of them, and neither tires",
                MineLevel = 160, Reward = BossReward.DragonBall, DragonBallNumber = 3,
                Squad = new[] { MonsterKind.ShadowBrute, MonsterKind.ShadowBrute },
                HealthMultiplier = 1.2f, DamageMultiplier = 1.15f, Resilience = 6,
                Scale = 1.45f, AuraColor = new Color(255, 170, 60)
            },
            new BossDefinition
            {
                Id = "CrimsonMaster", DisplayName = "The Crimson Master",
                Subtitle = "He burns himself to fight you, and does not seem to mind",
                MineLevel = 210, Reward = BossReward.Technique, TechniqueId = "Kaioken",
                Squad = new[] { MonsterKind.ShadowBrute, MonsterKind.ShadowBrute },
                HealthMultiplier = 1.2f, DamageMultiplier = 1.15f, Resilience = 6,
                Scale = 1.55f, AuraColor = new Color(255, 72, 62)
            },
            new BossDefinition
            {
                Id = "Majin", DisplayName = "Majin",
                Subtitle = "Nothing about this should be possible",
                MineLevel = 240, Reward = BossReward.Form, FormIndex = 5,
                Squad = new[] { MonsterKind.ShadowBrute },
                HealthMultiplier = 1.5f, DamageMultiplier = 1.25f, Resilience = 8,
                Scale = 2.0f, AuraColor = new Color(255, 130, 210)
            },

            // ---- skull cavern (mine level 120 + floor) ----------------------
            new BossDefinition
            {
                Id = "BallGuardian4", DisplayName = "Guardian of the Four-Star Ball",
                Subtitle = "Skull Cavern, ten floors down",
                MineLevel = 220, Reward = BossReward.DragonBall, DragonBallNumber = 4,
                Squad = new[] { MonsterKind.Serpent, MonsterKind.Serpent },
                HealthMultiplier = 1.3f, DamageMultiplier = 1.2f, Resilience = 7,
                Scale = 1.45f, AuraColor = new Color(255, 170, 60)
            },
            new BossDefinition
            {
                Id = "Ascetics", DisplayName = "The Ascetics",
                Subtitle = "They guard a cache, and they do not need it themselves",
                MineLevel = 180, Reward = BossReward.Supplies,
                Squad = new[] { MonsterKind.Mummy, MonsterKind.Mummy },
                HealthMultiplier = 1.3f, DamageMultiplier = 1.2f, Resilience = 8,
                Scale = 1.55f, AuraColor = new Color(160, 220, 255)
            },
            new BossDefinition
            {
                Id = "BallGuardian5", DisplayName = "Guardian of the Five-Star Ball",
                Subtitle = "Three abreast, filling the tunnel",
                MineLevel = 250, Reward = BossReward.DragonBall, DragonBallNumber = 5,
                Squad = new[] { MonsterKind.ShadowBrute, MonsterKind.ShadowBrute,
                                MonsterKind.ShadowBrute },
                HealthMultiplier = 1.35f, DamageMultiplier = 1.25f, Resilience = 9,
                Scale = 1.6f, AuraColor = new Color(255, 170, 60)
            },
            new BossDefinition
            {
                Id = "Vanishers", DisplayName = "The Vanishers",
                Subtitle = "They are never quite where you struck",
                MineLevel = 265, Reward = BossReward.Supplies,
                Squad = new[] { MonsterKind.Serpent, MonsterKind.Serpent, MonsterKind.Serpent },
                HealthMultiplier = 1.35f, DamageMultiplier = 1.25f, Resilience = 9,
                Scale = 1.5f, AuraColor = new Color(220, 245, 255),
                Abilities = BossAbility.Teleport, SpeedBonus = 3
            },
            new BossDefinition
            {
                Id = "BallGuardian6", DisplayName = "Guardian of the Six-Star Ball",
                Subtitle = "The dust never settles around them",
                MineLevel = 270, Reward = BossReward.DragonBall, DragonBallNumber = 6,
                Squad = new[] { MonsterKind.Mummy, MonsterKind.Mummy, MonsterKind.Mummy },
                HealthMultiplier = 1.4f, DamageMultiplier = 1.3f, Resilience = 10,
                Scale = 1.6f, AuraColor = new Color(255, 170, 60)
            },
            new BossDefinition
            {
                Id = "BallGuardian7", DisplayName = "Guardian of the Seven-Star Ball",
                Subtitle = "The last sphere is the worst defended by far",
                MineLevel = 290, Reward = BossReward.DragonBall, DragonBallNumber = 7,
                Squad = new[] { MonsterKind.ShadowBrute, MonsterKind.ShadowBrute,
                                MonsterKind.Serpent, MonsterKind.Serpent },
                HealthMultiplier = 1.5f, DamageMultiplier = 1.35f, Resilience = 11,
                Scale = 1.7f, AuraColor = new Color(255, 170, 60)
            },

            // ---- extra mine encounters (fill the gaps between the gate fights) -----
            // Optional side-bosses; they never gate a form or technique, so they can be
            // skipped, and they auto-scale to their floor like every other boss.
            new BossDefinition
            {
                Id = "CavernAmbush", DisplayName = "Cavern Ambushers",
                Subtitle = "They were waiting in the dark",
                MineLevel = 20, Reward = BossReward.Supplies, SupplySenzu = 1, SupplyGold = 4000,
                Squad = new[] { MonsterKind.GreenSlime, MonsterKind.GreenSlime,
                                MonsterKind.GreenSlime },
                HealthMultiplier = 0.9f, DamageMultiplier = 0.95f, Resilience = 1,
                Scale = 1.1f, AuraColor = new Color(150, 200, 120), SpriteSheet = "ambush"
            },
            new BossDefinition
            {
                Id = "Nappa", DisplayName = "Nappa",
                Subtitle = "He is only here to warm up",
                MineLevel = 40, Reward = BossReward.PowerCache, CacheKi = 20f, CacheAttack = 0.03f,
                Squad = new[] { MonsterKind.ShadowBrute },
                HealthMultiplier = 1.05f, DamageMultiplier = 1.05f, Resilience = 2,
                Scale = 1.55f, AuraColor = new Color(230, 210, 130), SpriteSheet = "nappa"
            },
            new BossDefinition
            {
                Id = "CellJuniors", DisplayName = "Cell Juniors",
                Subtitle = "Small, blue, and merciless",
                MineLevel = 150, Reward = BossReward.Supplies, SupplySenzu = 3, SupplyGold = 14000,
                Squad = new[] { MonsterKind.GreenSlime, MonsterKind.GreenSlime,
                                MonsterKind.GreenSlime, MonsterKind.GreenSlime },
                HealthMultiplier = 1.15f, DamageMultiplier = 1.15f, Resilience = 4,
                Scale = 1.05f, AuraColor = new Color(150, 210, 255), SpriteSheet = "celljr"
            },
            new BossDefinition
            {
                Id = "Recoome", DisplayName = "Recoome",
                Subtitle = "He would like you to watch the whole routine",
                MineLevel = 60, Reward = BossReward.PowerCache, CacheKi = 26f, CacheAttack = 0.04f,
                Squad = new[] { MonsterKind.ShadowBrute },
                HealthMultiplier = 1.1f, DamageMultiplier = 1.05f, Resilience = 3,
                Scale = 1.55f, AuraColor = new Color(255, 140, 90), SpriteSheet = "recoome"
            },
            new BossDefinition
            {
                Id = "FriezaElites", DisplayName = "Frieza's Elites",
                Subtitle = "The vanguard of a very bad day",
                MineLevel = 80, Reward = BossReward.Supplies, SupplySenzu = 2, SupplyGold = 12000,
                Squad = new[] { MonsterKind.SquidKid, MonsterKind.SquidKid, MonsterKind.SquidKid },
                HealthMultiplier = 1.05f, DamageMultiplier = 1.1f, Resilience = 3,
                Scale = 1.25f, AuraColor = new Color(210, 130, 255), SpriteSheet = "frieza"
            },
            new BossDefinition
            {
                Id = "Cooler", DisplayName = "Cooler",
                Subtitle = "Frieza's colder, worse brother",
                MineLevel = 120, Reward = BossReward.PowerCache, CacheKi = 34f, CacheAttack = 0.05f,
                Squad = new[] { MonsterKind.ShadowBrute },
                HealthMultiplier = 1.05f, DamageMultiplier = 1.05f, Resilience = 3,
                Scale = 1.45f, AuraColor = new Color(150, 230, 220), SpriteSheet = "cooler",
                SpeedBonus = 1
            },
            new BossDefinition
            {
                Id = "Dabura", DisplayName = "Dabura, Demon King",
                Subtitle = "Spit turns flesh to stone",
                MineLevel = 170, Reward = BossReward.Supplies, SupplySenzu = 3, SupplyGold = 18000,
                Squad = new[] { MonsterKind.ShadowBrute, MonsterKind.ShadowBrute,
                                MonsterKind.ShadowBrute },
                HealthMultiplier = 1.2f, DamageMultiplier = 1.15f, Resilience = 6,
                Scale = 1.45f, AuraColor = new Color(200, 60, 60), SpriteSheet = "dabura"
            },
            new BossDefinition
            {
                Id = "Bojack", DisplayName = "Bojack",
                Subtitle = "Sealed away once. Not well enough.",
                MineLevel = 200, Reward = BossReward.PowerCache, CacheKi = 40f, CacheAttack = 0.06f,
                Squad = new[] { MonsterKind.ShadowBrute, MonsterKind.ShadowBrute },
                HealthMultiplier = 1.2f, DamageMultiplier = 1.15f, Resilience = 6,
                Scale = 1.55f, AuraColor = new Color(120, 230, 170), SpriteSheet = "bojack"
            },
            new BossDefinition
            {
                Id = "SuperBuu", DisplayName = "Super Buu",
                Subtitle = "It copies what it eats",
                MineLevel = 230, Reward = BossReward.Supplies, SupplySenzu = 3, SupplyGold = 22000,
                Squad = new[] { MonsterKind.Mummy, MonsterKind.Mummy },
                HealthMultiplier = 1.3f, DamageMultiplier = 1.2f, Resilience = 7,
                Scale = 1.6f, AuraColor = new Color(255, 130, 210), SpriteSheet = "superbuu",
                Abilities = BossAbility.Regenerate
            },

            // ---- deep Skull Cavern challenges (endgame, well past the last guardian) ---
            new BossDefinition
            {
                Id = "MetalCoolerLegion", DisplayName = "Metal Cooler Legion",
                Subtitle = "Break one and the next steps forward",
                MineLevel = 260, Reward = BossReward.Supplies, SupplySenzu = 4, SupplyGold = 40000,
                Squad = new[] { MonsterKind.Mummy, MonsterKind.Mummy, MonsterKind.Mummy,
                                MonsterKind.Mummy },
                HealthMultiplier = 1.5f, DamageMultiplier = 1.35f, Resilience = 11,
                Scale = 1.6f, AuraColor = new Color(170, 220, 255), SpriteSheet = "metalcooler"
            },
            new BossDefinition
            {
                Id = "Broly", DisplayName = "Broly, the Legendary",
                Subtitle = "He does not stop, and he does not tire",
                MineLevel = 280, Reward = BossReward.PowerCache, CacheKi = 60f, CacheAttack = 0.10f,
                Squad = new[] { MonsterKind.ShadowBrute },
                HealthMultiplier = 1.7f, DamageMultiplier = 1.4f, Resilience = 12,
                Scale = 2.2f, AuraColor = new Color(150, 240, 120), SpriteSheet = "broly",
                SpeedBonus = 1
            },
            new BossDefinition
            {
                Id = "KidBuu", DisplayName = "Kid Buu",
                Subtitle = "The original, and the worst",
                MineLevel = 295, Reward = BossReward.Supplies, SupplySenzu = 5, SupplyGold = 60000,
                Squad = new[] { MonsterKind.Mummy, MonsterKind.Mummy,
                                MonsterKind.Mummy, MonsterKind.Mummy },
                HealthMultiplier = 1.7f, DamageMultiplier = 1.4f, Resilience = 12,
                Scale = 1.7f, AuraColor = new Color(255, 150, 210), SpriteSheet = "kidbuu",
                Abilities = BossAbility.Regenerate, SpeedBonus = 2
            },
            new BossDefinition
            {
                Id = "Frieza", DisplayName = "Frieza",
                Subtitle = "He will show you each of his forms in turn",
                MineLevel = 100, Reward = BossReward.PowerCache, CacheKi = 30f, CacheAttack = 0.045f,
                Squad = new[] { MonsterKind.ShadowBrute },
                HealthMultiplier = 1.2f, DamageMultiplier = 1.15f, Resilience = 5,
                Scale = 1.6f, AuraColor = new Color(210, 150, 235), SpriteSheet = "friezalord",
                Abilities = BossAbility.KiBlast, SpeedBonus = 1
            },
            new BossDefinition
            {
                Id = "Destroyer", DisplayName = "God of Destruction",
                Subtitle = "You should not have come this deep",
                MineLevel = 300, Reward = BossReward.PowerCache, CacheKi = 90f, CacheAttack = 0.15f,
                Squad = new[] { MonsterKind.ShadowBrute, MonsterKind.ShadowBrute,
                                MonsterKind.ShadowBrute, MonsterKind.ShadowBrute },
                HealthMultiplier = 1.85f, DamageMultiplier = 1.5f, Resilience = 14,
                Scale = 2.4f, AuraColor = new Color(190, 120, 255), SpriteSheet = "destroyer",
                Abilities = BossAbility.Teleport | BossAbility.KiBlast, SpeedBonus = 3
            }
        };

        public static BossDefinition ForMineLevel(int level)
        {
            foreach (BossDefinition def in All)
            {
                if (def.MineLevel == level)
                    return def;
            }
            return null;
        }

        public static BossDefinition ForForm(int formIndex)
        {
            foreach (BossDefinition def in All)
            {
                if (def.Reward == BossReward.Form && def.FormIndex == formIndex)
                    return def;
            }
            return null;
        }

        public static BossDefinition ById(string id)
        {
            foreach (BossDefinition def in All)
            {
                if (def.Id == id)
                    return def;
            }
            return null;
        }
    }

    /// <summary>Persisted per save.</summary>
    public sealed class BossSaveData
    {
        public List<string> DefeatedBosses { get; set; } = new List<string>();

        /// <summary>Technique ids earned from bosses.</summary>
        public List<string> UnlockedTechniques { get; set; } = new List<string>();

        /// <summary>Forms handed out by wishes, bypassing their guardian.</summary>
        public int GrantedFormUnlocks { get; set; }

        /// <summary>How many times the Multiversal Invader has been beaten. Each defeat
        /// makes the next appearance stronger, and it persists with the save.</summary>
        public int InvaderDefeats { get; set; }

        /// <summary>Day (Game1.stats.DaysPlayed) each boss was last beaten, so it can respawn
        /// on its floor once a cooldown has passed rather than staying dead forever.</summary>
        public Dictionary<string, int> DefeatDay { get; set; } = new Dictionary<string, int>();

        /// <summary>How many times each boss has been beaten. Every rematch scales the boss
        /// up and its drops with it.</summary>
        public Dictionary<string, int> DefeatCount { get; set; } = new Dictionary<string, int>();
    }

    internal readonly struct MonsterSheet
    {
        public readonly string Asset;
        public readonly int Width;
        public readonly int Height;

        public MonsterSheet(string asset, int width, int height)
        {
            this.Asset = asset;
            this.Width = width;
            this.Height = height;
        }
    }

    internal sealed class BossManager
    {
        public const string BossKey = "khaleelkhan.SaiyanTransformations/boss";
        public const string SecondWindKey = "khaleelkhan.SaiyanTransformations/secondwind";
        public const string InvaderKey = "khaleelkhan.SaiyanTransformations/invader";

        /// <summary>Custom sheet per monster type, sized to match vanilla exactly so the
        /// game's frame-index maths lands on the right art. Serpents keep vanilla art.</summary>
        private static readonly Dictionary<MonsterKind, MonsterSheet> Sheets =
            new Dictionary<MonsterKind, MonsterSheet>
            {
                [MonsterKind.GreenSlime] = new MonsterSheet("saibaman", 16, 24),
                [MonsterKind.ShadowBrute] = new MonsterSheet("elite", 16, 32),
                [MonsterKind.Skeleton] = new MonsterSheet("blade", 16, 32),
                [MonsterKind.Mummy] = new MonsterSheet("android", 16, 32),
                [MonsterKind.SquidKid] = new MonsterSheet("adept", 16, 24)
            };

        private readonly ModEntry Owner;
        private readonly FxRenderer Fx;

        private BossSaveData Data = new BossSaveData();

        /// <summary>The encounter on the current floor, if it is alive.</summary>
        private BossDefinition active;
        private int activeHealth;
        private int activeMaxHealth;
        private int activeAlive;

        /// <summary>Guards against declaring victory just because we walked in after
        /// the fight, or because the floor has not finished loading.</summary>
        private bool sawAliveThisVisit;

        private int introTicks;

        /// <summary>Lines queued to show as a dialogue box on the next free tick, so a warp-in
        /// or a kill does not try to open a dialogue mid-transition.</summary>
        private string[] pendingDialogue;

        /// <summary>Drives the special moves marquee bosses use on top of their melee.</summary>
        private readonly BossAbilityRunner abilities;

        public BossManager(ModEntry owner, FxRenderer fx)
        {
            this.Owner = owner;
            this.Fx = fx;
            this.abilities = new BossAbilityRunner(owner, this, fx);
        }

        public BossDefinition Active => this.active;

        /// <summary>True while a boss is alive on the floor you are standing on — the way
        /// down is sealed until it falls. Drives the "no boss can be skipped" gate.</summary>
        public bool CurrentFloorSealed => this.active != null;

        // ------------------------------------------------------------- save data

        public void LoadSaveData()
        {
            this.Data = Owner.Helper.Data.ReadSaveData<BossSaveData>("bosses") ?? new BossSaveData();
            this.Reset();
        }

        public void WriteSaveData()
        {
            Owner.Helper.Data.WriteSaveData("bosses", this.Data);
        }

        public void Reset()
        {
            this.active = null;
            this.sawAliveThisVisit = false;
            this.introTicks = 0;
            this.pendingDialogue = null;
            this.activeHealth = this.activeMaxHealth = this.activeAlive = 0;
            this.abilities?.Reset();
        }

        public bool IsDefeated(BossDefinition def)
        {
            return def == null || this.Data.DefeatedBosses.Contains(def.Id);
        }

        private int Today => (int)Game1.stats.DaysPlayed;

        /// <summary>How many times this boss has already been beaten (0 on a first meeting).</summary>
        public int DefeatCountOf(BossDefinition def)
        {
            return def != null && def.Id != null && this.Data.DefeatCount.TryGetValue(def.Id, out int c)
                ? c : 0;
        }

        /// <summary>A boss is on cooldown - and so does not respawn - until the configured
        /// number of in-game days have passed since it was last beaten. A boss never beaten
        /// is not on cooldown, so it appears the first time you arrive.</summary>
        public bool IsOnCooldown(BossDefinition def)
        {
            if (def == null)
                return true;
            int days = Math.Max(0, Owner.Config.BossRespawnCooldownDays);
            if (days == 0)
                return false;   // respawning disabled -> always eligible
            if (!this.Data.DefeatDay.TryGetValue(def.Id, out int day))
                return false;   // never beaten
            return (this.Today - day) < days;
        }

        /// <summary>Health/damage multiplier from how many times a boss has been beaten, so
        /// every rematch is harder than the last. Synthetic yardsticks (no Id) are unaffected.</summary>
        private float RematchMultiplier(BossDefinition def)
        {
            if (def == null || string.IsNullOrEmpty(def.Id))
                return 1f;
            return 1f + (Math.Max(0f, Owner.Config.BossRematchScale) * this.DefeatCountOf(def));
        }

        public bool IsFormUnlockedByBoss(int formIndex)
        {
            BossDefinition def = BossDefinition.ForForm(formIndex);
            return def == null || this.IsDefeated(def);
        }

        public void ForceClear(BossDefinition def)
        {
            if (def != null && !this.Data.DefeatedBosses.Contains(def.Id))
                this.Data.DefeatedBosses.Add(def.Id);
        }

        public IEnumerable<string> Defeated => this.Data.DefeatedBosses;

        // ------------------------------------------------------------- spawning

        public void OnWarped(GameLocation location)
        {
            this.Reset();

            if (!Owner.Config.EnableBosses || !(location is MineShaft shaft))
                return;

            BossDefinition def = BossDefinition.ForMineLevel(shaft.mineLevel);
            if (def == null || this.IsOnCooldown(def))
                return;

            // if the encounter is already down there from a previous visit, just adopt it
            if (this.CountBossMonsters(shaft, def) > 0)
            {
                this.Announce(def, returning: true);
                return;
            }

            this.Spawn(shaft, def);
        }

        private int CountBossMonsters(GameLocation location, BossDefinition def)
        {
            int count = 0;
            foreach (NPC npc in location.characters)
            {
                if (npc is Monster monster
                    && monster.modData.TryGetValue(BossKey, out string id)
                    && id == def.Id)
                {
                    count++;
                }
            }
            return count;
        }

        private void Spawn(MineShaft shaft, BossDefinition def)
        {
            Vector2 playerTile = Game1.player.Tile;
            int spawned = 0;

            int extra = Math.Min(
                Math.Max(0, Owner.Config.BossCycleExtraMinions) * this.Cycle,
                Math.Max(0, Owner.Config.BossCycleMaxExtraMinions));
            int total = def.Squad.Length + extra;

            // the encounter has a health budget; more bodies means each is thinner
            int share = Math.Max(1, this.EncounterHealth(def) / Math.Max(1, total));

            for (int i = 0; i < total; i++)
            {
                if (!this.TryFindSpot(shaft, playerTile, out Vector2 tile))
                    continue;

                MonsterKind kind = def.Squad[i % def.Squad.Length];
                Monster monster = this.Create(kind, tile * 64f);
                if (monster == null)
                    continue;

                this.Configure(monster, def, kind, share);
                shaft.addCharacter(monster);
                spawned++;
            }

            if (spawned == 0)
            {
                Owner.Monitor.Log($"Could not find anywhere to place {def.DisplayName} on mine level "
                                  + $"{shaft.mineLevel}; it will try again next visit.", LogLevel.Warn);
                return;
            }

            this.Announce(def, returning: false);
        }

        private Monster Create(MonsterKind kind, Vector2 pixelPosition)
        {
            switch (kind)
            {
                case MonsterKind.GreenSlime: return new GreenSlime(pixelPosition);
                case MonsterKind.ShadowBrute: return new ShadowBrute(pixelPosition);
                case MonsterKind.SquidKid: return new SquidKid(pixelPosition);
                case MonsterKind.Serpent: return new Serpent(pixelPosition);
                case MonsterKind.Mummy: return new Mummy(pixelPosition);
                case MonsterKind.Skeleton: return new Skeleton(pixelPosition);
                default: return null;
            }
        }

        /// <summary>How many wishes have already been granted. Every guardian scales
        /// off this, so each run of the spheres is harder than the last.</summary>
        public int Cycle => Owner.DragonBalls?.State?.WishesGranted ?? 0;

        /// <summary>Swap in our own sheet for this instance only. Ordinary monsters of the
        /// same type elsewhere in the world keep vanilla art.</summary>
        /// <summary>Give the frame size a monster kind's sheet must use, so a per-boss sheet
        /// lines up with the animation maths the vanilla class expects.</summary>
        private static void FrameDimsFor(MonsterKind kind, out int width, out int height)
        {
            switch (kind)
            {
                case MonsterKind.GreenSlime: width = 16; height = 24; break;
                case MonsterKind.SquidKid: width = 16; height = 24; break;
                default: width = 16; height = 32; break;
            }
        }

        /// <summary>Swap in a sprite sheet for this instance only. A boss may name its own
        /// sheet (<paramref name="overrideSheet"/>) so it looks unique; otherwise it falls
        /// back to the shared per-kind sheet. Ordinary monsters elsewhere keep vanilla art.</summary>
        private void ApplySprite(Monster monster, MonsterKind kind, string overrideSheet = null)
        {
            if (!Owner.Config.CustomBossSprites)
                return;

            string asset;
            int w, h;
            if (!string.IsNullOrEmpty(overrideSheet))
            {
                asset = overrideSheet;
                FrameDimsFor(kind, out w, out h);
            }
            else if (Sheets.TryGetValue(kind, out MonsterSheet sheet))
            {
                asset = sheet.Asset;
                w = sheet.Width;
                h = sheet.Height;
            }
            else
            {
                return;
            }

            try
            {
                monster.Sprite = new AnimatedSprite(ModEntry.MonsterAssetName(asset), 0, w, h);
            }
            catch (Exception ex)
            {
                Owner.Monitor.Log($"Could not apply the {asset} sprite sheet: {ex.Message}",
                                  LogLevel.Warn);
            }
        }

        /// <summary>The strongest form you can realistically bring to a given floor. The
        /// health curve is expressed relative to this, so a boss stays about as hard as the
        /// level-20 fight rather than drifting easy or impossible.</summary>
        private static float ExpectedPlayerMultiplier(int mineLevel)
        {
            // Anchored to where each form now unlocks. Boss health is scaled by the form the
            // player is expected to be holding, so a floor is brutal without that form and
            // fair with it - the forms are meant to be all but required to keep descending.
            if (mineLevel < 10) return 1f;      // base, before the first fight
            if (mineLevel < 50) return 1.5f;    // Super Saiyan
            if (mineLevel < 90) return 2.2f;    // Super Saiyan 2
            if (mineLevel < 140) return 3.2f;   // Super Saiyan 3
            if (mineLevel < 190) return 4.4f;   // Super Saiyan God
            if (mineLevel < 240) return 6f;     // Super Saiyan Blue
            if (mineLevel < 280) return 8.5f;   // Ultra Instinct
            return 11.5f;                       // Mastered Ultra Instinct, floors 280+
        }

        /// <summary>Total health for the whole encounter, split across the squad.</summary>
        public int EncounterHealth(BossDefinition def)
        {
            // Anchored at floor 20 (the benchmark fight) so it stays unchanged there, then
            // ramps harder with depth than the old 1 + level/120 curve did - the deeper
            // fights were the ones that felt too soft.
            float depth = 1.167f + (Math.Max(0, def.MineLevel - 20) / 60f);
            float cycleHp = (float)Math.Pow(Math.Max(1f, Owner.Config.BossCycleHealthScale), this.Cycle);
            return Math.Max(1, (int)(Owner.Config.BaseEncounterHealth
                                     * ExpectedPlayerMultiplier(def.MineLevel)
                                     * depth * def.HealthMultiplier
                                     * cycleHp * this.RematchMultiplier(def)
                                     * Owner.Config.BossHealthScale));
        }

        public int EncounterDamage(BossDefinition def)
        {
            float cycleDmg = (float)Math.Pow(Math.Max(1f, Owner.Config.BossCycleDamageScale), this.Cycle);
            float divisor = Math.Max(5f, Owner.Config.BossDamageDepthDivisor);
            // extra depth term, also anchored at floor 20, so late hits sting more
            float depthBite = 1f + (Math.Max(0, def.MineLevel - 20) / 220f);
            return Math.Max(1, (int)(Owner.Config.BaseBossDamage
                                     * (1f + (def.MineLevel / divisor))
                                     * depthBite * def.DamageMultiplier
                                     * cycleDmg * this.RematchMultiplier(def)
                                     * Owner.Config.BossDamageScale));
        }

        private void Configure(Monster monster, BossDefinition def, MonsterKind kind, int share)
        {
            int cycle = this.Cycle;

            monster.MaxHealth = Math.Max(1, share);
            monster.Health = monster.MaxHealth;
            monster.DamageToFarmer = this.EncounterDamage(def);
            monster.resilience.Value += def.Resilience + (3 * cycle) + (2 * this.DefeatCountOf(def));

            // faster than their base type: a per-boss bonus plus a slow climb per rematch
            if (Owner.Config.EnableBossSpeedScaling)
            {
                int rematchSpeed = Math.Min(Math.Max(0, Owner.Config.BossMaxRematchSpeedBonus),
                                            this.DefeatCountOf(def) / 2);
                monster.speed += def.SpeedBonus + rematchSpeed;
            }

            // hunt the player across the whole floor instead of idling
            monster.moveTowardPlayerThreshold.Value = 999;

            monster.Scale = def.Scale;
            monster.modData[BossKey] = def.Id;

            this.ApplySprite(monster, kind, def.SpriteSheet);

            // the tint multiplies the sheet, so it only helps when the art is vanilla
            bool custom = Owner.Config.CustomBossSprites
                          && (Sheets.ContainsKey(kind) || !string.IsNullOrEmpty(def.SpriteSheet));
            if (monster is GreenSlime slime)
                slime.color.Value = custom ? Color.White
                                           : (def.SlimeTint ?? slime.color.Value);
        }

        /// <summary>A tile is only safe to drop a monster on if it is inside the map, on
        /// a passable tile, clear of objects, and not water. Checking only
        /// CanItemBePlacedHere let monsters land in walls and off the edge of mine floors,
        /// where they could not path and could not be reached.</summary>
        private bool IsSpawnable(GameLocation location, Vector2 tile)
        {
            int x = (int)tile.X;
            int y = (int)tile.Y;

            if (x < 1 || y < 1)
                return false;
            if (x >= location.Map.Layers[0].LayerWidth - 1
                || y >= location.Map.Layers[0].LayerHeight - 1)
            {
                return false;
            }
            if (!location.isTileOnMap(tile))
                return false;
            if (!location.isTilePassable(new xTile.Dimensions.Location(x, y), Game1.viewport))
                return false;
            if (location.isWaterTile(x, y))
                return false;
            if (!location.CanItemBePlacedHere(tile))
                return false;

            // the four neighbours must be sane too, so nothing spawns wedged in a nook
            int open = 0;
            foreach (Vector2 offset in new[] { new Vector2(1, 0), new Vector2(-1, 0),
                                               new Vector2(0, 1), new Vector2(0, -1) })
            {
                Vector2 n = tile + offset;
                if (location.isTileOnMap(n)
                    && location.isTilePassable(
                        new xTile.Dimensions.Location((int)n.X, (int)n.Y), Game1.viewport))
                {
                    open++;
                }
            }
            return open >= 2;
        }

        private bool TryFindSpot(GameLocation location, Vector2 playerTile, out Vector2 tile)
        {
            int width = location.Map.Layers[0].LayerWidth;
            int height = location.Map.Layers[0].LayerHeight;

            for (int attempt = 0; attempt < 500; attempt++)
            {
                Vector2 candidate = new Vector2(Game1.random.Next(width), Game1.random.Next(height));
                float distance = Vector2.Distance(candidate, playerTile);
                if (distance < 4f || distance > 18f)
                    continue;
                if (!this.IsSpawnable(location, candidate))
                    continue;

                tile = candidate;
                return true;
            }

            tile = Vector2.Zero;
            return false;
        }

        private void Announce(BossDefinition def, bool returning)
        {
            this.introTicks = 1;
            Owner.PlayCue("boss_roar", "shadowpeep");
            if (Owner.Config.ScreenFlash)
                Game1.flashAlpha = 0.5f;
            ModEntry.Notify(returning
                ? $"{def.DisplayName} is still here."
                : $"{def.DisplayName} blocks the way down!");

            // a fresh spawn speaks; a boss you merely walked back in on does not repeat itself
            if (!returning)
                this.pendingDialogue = this.EncounterLines(def);
        }

        /// <summary>The meeting line for a boss, chosen by how many times it has been beaten:
        /// first meeting, second, third, then a repeatable line from the fourth on.</summary>
        private string[] EncounterLines(BossDefinition def)
        {
            BossLines lines = BossDialogue.For(def?.Id);
            if (lines == null)
                return null;

            switch (this.DefeatCountOf(def))
            {
                case 0: return lines.Meet;
                case 1: return lines.Rematch2 ?? lines.Meet;
                case 2: return lines.Rematch3 ?? lines.Rematch2 ?? lines.Meet;
                default: return lines.RematchLoop ?? lines.Rematch3 ?? lines.Meet;
            }
        }

        /// <summary>Show queued lines as a dialogue box once the world can display one.</summary>
        private void ShowPendingDialogue()
        {
            if (this.pendingDialogue == null)
                return;
            if (!Context.IsPlayerFree || Game1.activeClickableMenu != null || Game1.eventUp)
                return;

            string[] lines = this.pendingDialogue;
            this.pendingDialogue = null;
            if (lines.Length == 0)
                return;
            try
            {
                Game1.multipleDialogues(lines);
            }
            catch (Exception)
            {
                ModEntry.Notify(lines[0]);
            }
        }

        // ---------------------------------------------------------------- update

        public void Update()
        {
            if (this.introTicks > 0)
            {
                this.introTicks++;
                if (this.introTicks > 150)
                    this.introTicks = 0;
            }

            if (!Owner.Config.EnableBosses || !Context.IsWorldReady)
                return;

            this.ShowPendingDialogue();

            if (Owner.Config.EnableBossAbilities)
                this.abilities.Update();

            if (!(Game1.currentLocation is MineShaft shaft))
            {
                this.active = null;
                return;
            }

            BossDefinition def = BossDefinition.ForMineLevel(shaft.mineLevel);
            if (def == null || this.IsOnCooldown(def))
            {
                this.active = null;
                return;
            }

            int alive = 0;
            int health = 0;
            int maxHealth = 0;
            foreach (NPC npc in shaft.characters)
            {
                if (npc is Monster monster
                    && monster.modData.TryGetValue(BossKey, out string id)
                    && id == def.Id)
                {
                    this.CheckSecondWind(monster);
                    if (Owner.Config.EnableBossAbilities)
                        this.abilities.TickMonster(monster, def);
                    alive++;
                    health += Math.Max(0, monster.Health);
                    maxHealth += Math.Max(1, monster.MaxHealth);
                }
            }

            if (Owner.Config.EnableBossAbilities)
                this.abilities.SweepDead();

            if (alive > 0)
            {
                this.active = def;
                this.activeAlive = alive;
                this.activeHealth = health;
                this.activeMaxHealth = maxHealth;
                this.sawAliveThisVisit = true;
                return;
            }

            this.active = null;

            // only a kill if we actually saw them standing on this visit
            if (this.sawAliveThisVisit)
                this.Defeat(def);
        }

        /// <summary>From the configured run onward, a guardian surges back the first time
        /// it is nearly finished. Triggered just above zero rather than on death, so it
        /// fires reliably before the game removes the monster.</summary>
        private void CheckSecondWind(Monster monster)
        {
            int from = Owner.Config.SecondWindFromCycle;
            if (from <= 0 || this.Cycle < from)
                return;
            if (monster.modData.ContainsKey(SecondWindKey))
                return;
            if (monster.Health > monster.MaxHealth * 0.08f)
                return;

            monster.modData[SecondWindKey] = "1";
            monster.Health = (int)(monster.MaxHealth * 0.45f);
            Owner.PlayCue("boss_roar", "shadowpeep");
            if (Owner.Config.ScreenFlash)
                Game1.flashAlpha = 0.5f;
            ModEntry.Notify("It refuses to fall.");
        }

        /// <summary>Spawns the guardian that gates a late wish. Tagged separately from the
        /// mine bosses so it never touches the reward or unlock logic.</summary>
        public int SpawnWishTrial(GameLocation location, Vector2 centre, int cycle)
        {
            if (location == null)
                return 0;

            MonsterKind[] squad = cycle >= 6
                ? new[] { MonsterKind.ShadowBrute, MonsterKind.ShadowBrute,
                          MonsterKind.Serpent, MonsterKind.Serpent, MonsterKind.Mummy }
                : cycle >= 4
                    ? new[] { MonsterKind.ShadowBrute, MonsterKind.Serpent, MonsterKind.Serpent }
                    : new[] { MonsterKind.ShadowBrute, MonsterKind.Serpent };

            BossDefinition yardstick = new BossDefinition
            {
                MineLevel = 130, HealthMultiplier = 1.45f, DamageMultiplier = 1.3f
            };
            int budget = this.EncounterHealth(yardstick);
            int perTrial = Math.Max(1, budget / Math.Max(1, squad.Length));
            int trialDamage = this.EncounterDamage(yardstick);

            int spawned = 0;
            foreach (MonsterKind kind in squad)
            {
                if (!this.TryFindSpot(location, centre, out Vector2 tile))
                    continue;

                Monster monster = this.Create(kind, tile * 64f);
                if (monster == null)
                    continue;

                monster.MaxHealth = perTrial;
                monster.Health = monster.MaxHealth;
                monster.DamageToFarmer = trialDamage;
                monster.resilience.Value += 6 + (3 * cycle);
                monster.moveTowardPlayerThreshold.Value = 999;
                monster.Scale = 1.6f;
                monster.modData[DragonBallManager.TrialKey] = "1";
                this.ApplySprite(monster, kind);

                location.addCharacter(monster);
                spawned++;
            }
            return spawned;
        }

        /// <summary>Spawns a rival squad wherever the player happens to be standing.</summary>
        public int SpawnRival(GameLocation location, Vector2 centre, int wishes, int depth)
        {
            if (location == null)
                return 0;

            MonsterKind[] squad = wishes >= 3
                ? new[] { MonsterKind.ShadowBrute, MonsterKind.ShadowBrute, MonsterKind.Serpent }
                : wishes >= 1
                    ? new[] { MonsterKind.ShadowBrute, MonsterKind.Skeleton }
                    : new[] { MonsterKind.ShadowBrute };

            // Rivals are pegged to the strongest form you have actually unlocked, capped by
            // how deep you have been, so one early Skull Cavern trip cannot set every ambush
            // to floor-200 difficulty. Within that, they hit hard: each rival is a full
            // boss-weight encounter, not a fraction of one, so an invasion is a real fight.
            BossDefinition yardstick = new BossDefinition
            {
                MineLevel = Owner.RivalTuningLevel(depth),
                HealthMultiplier = 2.6f * Math.Max(0.1f, Owner.Config.RivalHealthScale),
                DamageMultiplier = 1.9f
            };
            // each rival gets the FULL budget rather than a split share, so more rivals
            // means a harder invasion, not thinner enemies
            int perRival = this.EncounterHealth(yardstick);
            int rivalDamage = this.EncounterDamage(yardstick);

            int spawned = 0;
            foreach (MonsterKind kind in squad)
            {
                if (!this.TryFindSpot(location, centre, out Vector2 tile))
                    continue;

                Monster monster = this.Create(kind, tile * 64f);
                if (monster == null)
                    continue;

                monster.MaxHealth = perRival;
                monster.Health = monster.MaxHealth;
                monster.DamageToFarmer = rivalDamage;
                monster.resilience.Value += 6 + (2 * wishes);
                monster.moveTowardPlayerThreshold.Value = 999;
                monster.speed += 2;   // keep pace with a speed-buffed, transformed player
                monster.Scale = 1.5f;
                monster.modData[RivalManager.RivalKey] = "1";
                this.ApplySprite(monster, kind);

                location.addCharacter(monster);
                spawned++;
            }
            return spawned;
        }

        /// <summary>Times the Multiversal Invader has been beaten (drives its escalation).</summary>
        public int InvaderDefeats => this.Data.InvaderDefeats;

        public void RecordInvaderDefeat()
        {
            this.Data.InvaderDefeats++;
        }

        /// <summary>Spawns the Multiversal Invader wherever the player is standing — a deep
        /// Skull Cavern floor or the open overworld. It is deliberately built off a yardstick
        /// well past floor 300 and climbs every time it is put down, so it stays a genuine
        /// endgame threat no matter how strong the player becomes.</summary>
        public int SpawnInvader(GameLocation location, Vector2 centre, int defeats)
        {
            if (location == null)
                return 0;

            // one relentless invader, with escalating backup as it keeps coming back
            var squad = new System.Collections.Generic.List<MonsterKind> { MonsterKind.ShadowBrute };
            if (defeats >= 1) squad.Add(MonsterKind.Serpent);
            if (defeats >= 3) squad.Add(MonsterKind.Mummy);
            if (defeats >= 5) squad.Add(MonsterKind.ShadowBrute);

            BossDefinition yardstick = new BossDefinition
            {
                MineLevel = 300 + (defeats * 20),
                HealthMultiplier = 0.9f + (0.2f * defeats),
                DamageMultiplier = 1.4f + (0.1f * defeats)
            };
            int perBody = this.EncounterHealth(yardstick);
            int invaderDamage = this.EncounterDamage(yardstick);

            int spawned = 0;
            foreach (MonsterKind kind in squad)
            {
                if (!this.TryFindSpot(location, centre, out Vector2 tile))
                    continue;

                Monster monster = this.Create(kind, tile * 64f);
                if (monster == null)
                    continue;

                monster.MaxHealth = perBody;
                monster.Health = monster.MaxHealth;
                monster.DamageToFarmer = invaderDamage;
                monster.resilience.Value += 12 + (2 * defeats);
                monster.moveTowardPlayerThreshold.Value = 999;
                monster.speed += 2;
                monster.Scale = 2.2f;
                monster.modData[InvaderKey] = "1";
                this.ApplySprite(monster, kind, "invader");

                location.addCharacter(monster);
                spawned++;
            }
            return spawned;
        }

        private void Defeat(BossDefinition def)
        {
            this.sawAliveThisVisit = false;

            // how many times it had been beaten *before* this kill (0 the first time)
            int priorDefeats = this.DefeatCountOf(def);

            if (!this.Data.DefeatedBosses.Contains(def.Id))
                this.Data.DefeatedBosses.Add(def.Id);
            this.Data.DefeatDay[def.Id] = this.Today;       // starts the respawn cooldown
            this.Data.DefeatCount[def.Id] = priorDefeats + 1;

            Owner.PlayCue("boss_defeat", "explosion");
            if (Owner.Config.ScreenFlash)
                Game1.flashAlpha = 0.9f;
            ModEntry.Notify(priorDefeats > 0
                ? $"{def.DisplayName} defeated again! (rematch {priorDefeats + 1})"
                : $"{def.DisplayName} defeated!");
            Owner.Monitor.Log($"Boss defeated: {def.Id} (mine level {def.MineLevel}, "
                              + $"kill #{priorDefeats + 1}).", LogLevel.Info);

            this.GrantReward(def, priorDefeats);

            // the boss's parting line, shown once the drop menus (if any) have cleared
            BossLines lines = BossDialogue.For(def.Id);
            if (lines != null)
                this.pendingDialogue = lines.Defeat;

            // the unlock announcement itself is handled by ModEntry's unlock check
        }

        private void GrantTechnique(string techniqueId)
        {
            if (string.IsNullOrEmpty(techniqueId)
                || this.Data.UnlockedTechniques.Contains(techniqueId))
            {
                return;
            }

            this.Data.UnlockedTechniques.Add(techniqueId);
            Owner.PlayCue("unlock", "yoba");
            ModEntry.Notify($"Technique learned: {Owner.TechniqueName(techniqueId)}!");
        }

        private void GrantReward(BossDefinition def, int priorDefeats)
        {
            bool firstKill = priorDefeats == 0;

            // ---- milestone reward -------------------------------------------------
            // Permanent unlocks (techniques, the power cache) are handed out only the first
            // time, so a respawned boss cannot be farmed for them. Dragon Balls are the
            // exception: they are consumed by every wish, so a guardian yields its ball on
            // every kill. Forms are granted through ModEntry's unlock check, which is already
            // idempotent.
            if (firstKill)
                this.GrantTechnique(def.BonusTechniqueId);

            switch (def.Reward)
            {
                case BossReward.Technique:
                    if (firstKill)
                        this.GrantTechnique(def.TechniqueId);
                    break;

                case BossReward.PowerCache:
                    if (firstKill)
                    {
                        Owner.Progress.GrantPowerBonus(def.CacheKi, def.CacheAttack);
                        ModEntry.Notify($"You grow stronger from the fight. "
                                        + $"(+{def.CacheKi:0} max ki, +{def.CacheAttack * 100f:0}% attack)");
                    }
                    break;

                case BossReward.DragonBall:
                    Owner.DragonBalls.GrantBall(def.DragonBallNumber);
                    break;

                case BossReward.Form:
                case BossReward.Supplies:
                    // Form: handled by the unlock check. Supplies: the drop below is the reward.
                    break;
            }

            // ---- combat drop: every kill, scaled by the boss's difficulty ---------
            this.GrantCombatDrop(def, priorDefeats);
        }

        /// <summary>Spoils dropped on every boss kill, scaled to how deep and how tough the
        /// boss is and how many times it has already been beaten. This is the reward loop for
        /// respawned bosses, so it has to climb as the rematches get harder.</summary>
        private void GrantCombatDrop(BossDefinition def, int priorDefeats)
        {
            Farmer player = Game1.player;
            float rematch = 1f + (0.3f * priorDefeats);
            int floor = Math.Max(1, def.MineLevel);

            int gold = Math.Max(0, (int)(floor * Math.Max(0f, Owner.Config.BossDropGoldPerFloor)
                                         * rematch * Math.Max(0.5f, def.HealthMultiplier)));
            if (gold > 0)
                player.Money += gold;

            int senzu = Math.Min(6, 1 + (floor / 80)) + priorDefeats / 2;
            senzu = Math.Min(9, senzu);
            if (senzu > 0)
                player.addItemByMenuIfNecessary(new SObject(DragonBallManager.SenzuId, senzu));

            PickMaterialDrop(floor, priorDefeats, out string itemId, out int qty);
            string itemLabel = string.Empty;
            if (itemId != null && qty > 0)
            {
                SObject drop = new SObject(itemId, qty);
                player.addItemByMenuIfNecessary(drop);
                itemLabel = $", {qty}x {drop.DisplayName}";
            }

            ModEntry.Notify($"Spoils: {gold:N0}g, {senzu} Senzu{itemLabel}.");
        }

        /// <summary>The signature material a boss drops, by depth tier. Deeper bosses give
        /// rarer, more valuable materials, and every rematch adds to the stack.</summary>
        private static void PickMaterialDrop(int floor, int priorDefeats, out string id, out int qty)
        {
            int bonus = priorDefeats;   // +1 to the stack per rematch
            if (floor <= 40)       { id = "749"; qty = 2 + bonus; }   // Omni Geode
            else if (floor <= 90)  { id = "336"; qty = 2 + bonus; }   // Gold Bar
            else if (floor <= 150) { id = "337"; qty = 2 + bonus; }   // Iridium Bar
            else if (floor <= 210) { id = "72";  qty = 2 + bonus; }   // Diamond
            else if (floor <= 270) { id = "337"; qty = 4 + bonus; }   // Iridium Bar (more)
            else                   { id = "74";  qty = 1 + bonus; }   // Prismatic Shard
        }

        public bool IsTechniqueUnlocked(string techniqueId)
        {
            return this.Data.UnlockedTechniques.Contains(techniqueId);
        }

        public int GrantedFormUnlocks => this.Data.GrantedFormUnlocks;

        public void GrantFormUnlock(int formIndex)
        {
            int wanted = formIndex + 1;
            if (this.Data.GrantedFormUnlocks < wanted)
                this.Data.GrantedFormUnlocks = wanted;
        }

        /// <summary>Wishing consumes the spheres, so their guardians return at once - their
        /// respawn cooldown is cleared rather than waiting out the usual 40 days, so the ball
        /// hunt can begin again immediately.</summary>
        public void ResetRepeatableBosses()
        {
            int removed = 0;
            foreach (BossDefinition def in BossDefinition.All)
            {
                if (!def.Repeatable)
                    continue;
                bool cleared = this.Data.DefeatedBosses.Remove(def.Id);
                cleared |= this.Data.DefeatDay.Remove(def.Id);
                if (cleared)
                    removed++;
            }
            if (removed > 0)
                Owner.Monitor.Log($"{removed} Dragon Ball guardians have returned.", LogLevel.Info);
        }

        // ---------------------------------------------------------------- drawing

        public void DrawWorld(SpriteBatch b)
        {
            if (!(Game1.currentLocation is MineShaft shaft))
                return;

            if (Owner.Config.EnableBossAbilities)
                this.abilities.Draw(b);

            if (!Owner.Config.ShowAura)
                return;

            foreach (NPC npc in shaft.characters)
            {
                if (!(npc is Monster monster)
                    || !monster.modData.TryGetValue(BossKey, out string id))
                {
                    continue;
                }


                BossDefinition def = BossDefinition.ById(id);
                if (def == null)
                    continue;

                Vector2 anchor = Fx.MonsterAnchor(monster);
                Fx.DrawAuraAt(b, anchor, def.AuraColor, 3f * def.Scale, 0.55f, monster.GetHashCode());
            }
        }

        public void DrawHud(SpriteBatch b)
        {
            if (this.active == null || this.activeMaxHealth <= 0)
                return;

            const int barWidth = 520;
            const int barHeight = 26;
            int x = (Game1.uiViewport.Width - barWidth) / 2;
            int y = 84;

            float fraction = MathHelper.Clamp((float)this.activeHealth / this.activeMaxHealth, 0f, 1f);

            b.Draw(Game1.staminaRect, new Rectangle(x - 4, y - 4, barWidth + 8, barHeight + 8),
                   Color.Black * 0.7f);
            b.Draw(Game1.staminaRect, new Rectangle(x, y, barWidth, barHeight),
                   new Color(48, 12, 16) * 0.95f);
            b.Draw(Game1.staminaRect, new Rectangle(x, y, (int)(barWidth * fraction), barHeight),
                   this.active.AuraColor);

            string title = this.activeAlive > 1
                ? $"{this.active.DisplayName}  x{this.activeAlive}"
                : this.active.DisplayName;
            // show the rematch tier once the boss has been beaten before
            int rematch = this.DefeatCountOf(this.active);
            if (rematch > 0)
                title += $"  ×{rematch + 1}";
            Vector2 size = Game1.smallFont.MeasureString(title);
            Utility.drawTextWithShadow(b, title, Game1.smallFont,
                new Vector2(x + (barWidth / 2f) - (size.X / 2f), y - size.Y - 2), Color.White);

            if (this.introTicks > 0 && this.active.Subtitle != null)
            {
                Vector2 subSize = Game1.smallFont.MeasureString(this.active.Subtitle);
                Utility.drawTextWithShadow(b, this.active.Subtitle, Game1.smallFont,
                    new Vector2(x + (barWidth / 2f) - (subSize.X / 2f), y + barHeight + 6),
                    Color.Wheat);
            }
        }
    }
}
