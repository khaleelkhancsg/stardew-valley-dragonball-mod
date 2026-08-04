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
        KiBlast = 1,        // aimed orbs flung at the player
        Beam = 2,           // telegraphed line that scorches a corridor
        Teleport = 4,       // blinks in beside the player
        Regenerate = 8,     // heals itself over time
        SelfDestruct = 16,  // detonates where it dies
        Rush = 32,          // a sudden burst of speed - a dash at the player
        Paralyze = 64,      // freezes the player in place for a moment
        Shockwave = 128,    // a telegraphed ring of force around the boss

        // ---- signature moves for the marquee villains ----
        DeathBall = 256,    // Frieza: a huge telegraphed sphere dropped on your position
        Absorb = 512,       // Cell: siphons your ki from range and heals itself
        CandyBeam = 1024,   // Buu: a beam that weakens and slows instead of just hurting
        TimeStop = 2048     // Guldo: freezes you outright and blinks in for a free hit
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

        /// <summary>How many times this boss powers up as its health falls (0 = no phases).
        /// Thresholds are spaced evenly, e.g. 2 phases fire at ~66% and ~33% health.</summary>
        public int PhaseCount;

        /// <summary>An ability the boss gains when it reaches its final phase - so a melee-only
        /// boss can start throwing ki once it is cornered.</summary>
        public BossAbility PhaseAbility = BossAbility.None;

        /// <summary>Only applies to Green Slimes, which are the one vanilla monster
        /// with a per-instance tint.</summary>
        public Color? SlimeTint;

        public static readonly BossDefinition[] All =
        {
            // Villains ordered by strength: deeper = stronger. Form/technique rewards are
            // reassigned onto fitting villains after the old gate bosses were removed. New
            // bosses have no custom sprite yet, so they fall back to the shared per-kind art.

            // ---- 10-90: Saiyan saga, the Ginyu Force -----------------------
            new BossDefinition
            {
                Id = "Saibamen", DisplayName = "Saibaman Squad",
                Subtitle = "Sprouted from the cavern floor",
                MineLevel = 10, Reward = BossReward.Form, FormIndex = 0,
                Squad = new[] { MonsterKind.ShadowBrute, MonsterKind.ShadowBrute,
                                MonsterKind.ShadowBrute, MonsterKind.ShadowBrute },
                HealthMultiplier = 1.0f, DamageMultiplier = 1.0f, Resilience = 1,
                Scale = 0.90f, AuraColor = new Color(120, 230, 90),
                SlimeTint = new Color(96, 200, 72), Abilities = BossAbility.SelfDestruct,
                SpriteSheet = "saibamen"
            },
            new BossDefinition
            {
                Id = "Guldo", DisplayName = "Guldo",
                Subtitle = "He would stop time if you let him",
                MineLevel = 20, Reward = BossReward.Technique, TechniqueId = "DestructoDisk",
                Squad = new[] { MonsterKind.SquidKid },
                HealthMultiplier = 0.95f, DamageMultiplier = 0.95f, Resilience = 2,
                Scale = 0.70f, AuraColor = new Color(140, 210, 130),
                Abilities = BossAbility.Teleport | BossAbility.TimeStop, SpriteSheet = "guldo"
            },
            new BossDefinition
            {
                Id = "Nappa", DisplayName = "Nappa",
                Subtitle = "He is only here to warm up",
                MineLevel = 30, Reward = BossReward.PowerCache, CacheKi = 20f, CacheAttack = 0.03f,
                Squad = new[] { MonsterKind.ShadowBrute },
                HealthMultiplier = 1.05f, DamageMultiplier = 1.05f, Resilience = 2,
                Scale = 0.95f, AuraColor = new Color(230, 210, 130), SpriteSheet = "nappa",
                Abilities = BossAbility.Shockwave
            },
            new BossDefinition
            {
                Id = "Jeice", DisplayName = "Jeice",
                Subtitle = "The Red Magma, and half of a duo",
                MineLevel = 40, Reward = BossReward.Supplies, SupplySenzu = 2, SupplyGold = 8000,
                Squad = new[] { MonsterKind.ShadowBrute },
                HealthMultiplier = 1.05f, DamageMultiplier = 1.05f, Resilience = 3,
                Scale = 0.70f, AuraColor = new Color(255, 110, 90),
                Abilities = BossAbility.KiBlast, SpriteSheet = "jeice"
            },
            new BossDefinition
            {
                Id = "EliteWarrior", DisplayName = "Elite Saiyan Warrior",
                Subtitle = "He does not need to power up for you",
                MineLevel = 50, Reward = BossReward.Form, FormIndex = 1,
                BonusTechniqueId = "InstantTransmission",
                Squad = new[] { MonsterKind.ShadowBrute },
                HealthMultiplier = 1.15f, DamageMultiplier = 1.1f, Resilience = 3,
                Scale = 0.95f, AuraColor = new Color(180, 120, 255),
                PhaseCount = 1, PhaseAbility = BossAbility.KiBlast, SpriteSheet = "eliteboss"
            },
            new BossDefinition
            {
                Id = "Burter", DisplayName = "Burter",
                Subtitle = "The fastest in the universe, he says",
                MineLevel = 60, Reward = BossReward.PowerCache, CacheKi = 26f, CacheAttack = 0.04f,
                Squad = new[] { MonsterKind.ShadowBrute },
                HealthMultiplier = 1.1f, DamageMultiplier = 1.05f, Resilience = 3,
                Scale = 0.90f, AuraColor = new Color(120, 180, 255), SpeedBonus = 2,
                Abilities = BossAbility.Rush, SpriteSheet = "burter"
            },
            new BossDefinition
            {
                Id = "BallGuardian1", DisplayName = "Guardian of the One-Star Ball",
                Subtitle = "It will not give the sphere up quietly",
                MineLevel = 70, Reward = BossReward.DragonBall, DragonBallNumber = 1,
                Squad = new[] { MonsterKind.ShadowBrute, MonsterKind.ShadowBrute,
                                MonsterKind.ShadowBrute },
                HealthMultiplier = 1.1f, DamageMultiplier = 1.05f, Resilience = 3,
                Scale = 0.88f, AuraColor = new Color(255, 170, 60), SpriteSheet = "guardian"
            },
            new BossDefinition
            {
                Id = "Recoome", DisplayName = "Recoome",
                Subtitle = "He would like you to watch the whole routine",
                MineLevel = 80, Reward = BossReward.PowerCache, CacheKi = 28f, CacheAttack = 0.04f,
                Squad = new[] { MonsterKind.ShadowBrute },
                HealthMultiplier = 1.1f, DamageMultiplier = 1.1f, Resilience = 3,
                Scale = 1.00f, AuraColor = new Color(255, 140, 90), SpriteSheet = "recoome",
                Abilities = BossAbility.Rush
            },
            new BossDefinition
            {
                Id = "CaptainGinyu", DisplayName = "Captain Ginyu",
                Subtitle = "He leads the pose, and the Force",
                MineLevel = 90, Reward = BossReward.Form, FormIndex = 2,
                Squad = new[] { MonsterKind.ShadowBrute },
                HealthMultiplier = 1.2f, DamageMultiplier = 1.15f, Resilience = 4,
                Scale = 0.95f, AuraColor = new Color(180, 120, 255),
                PhaseCount = 1, PhaseAbility = BossAbility.KiBlast, SpriteSheet = "captainginyu"
            },

            // ---- 100-190: Frieza saga, Cooler, the Cells -------------------
            new BossDefinition
            {
                Id = "FriezaFirst", DisplayName = "Frieza (First Form)",
                Subtitle = "Even restrained, he outclasses you",
                MineLevel = 100, Reward = BossReward.Technique, TechniqueId = "SolarFlare",
                Squad = new[] { MonsterKind.SquidKid },
                HealthMultiplier = 1.15f, DamageMultiplier = 1.15f, Resilience = 4,
                Scale = 0.95f, AuraColor = new Color(200, 150, 235),
                Abilities = BossAbility.KiBlast, SpriteSheet = "friezafirst"
            },
            new BossDefinition
            {
                Id = "CoolerFirst", DisplayName = "Cooler (First Form)",
                Subtitle = "Frieza's colder, worse brother",
                MineLevel = 110, Reward = BossReward.PowerCache, CacheKi = 32f, CacheAttack = 0.045f,
                Squad = new[] { MonsterKind.ShadowBrute },
                HealthMultiplier = 1.15f, DamageMultiplier = 1.1f, Resilience = 5,
                Scale = 0.95f, AuraColor = new Color(150, 230, 220), SpeedBonus = 1,
                SpriteSheet = "coolerfirst"
            },
            new BossDefinition
            {
                Id = "FriezaFinal", DisplayName = "Frieza (Final Form)",
                Subtitle = "No armour, no restraint, no mercy",
                MineLevel = 140, Reward = BossReward.Form, FormIndex = 3,
                Squad = new[] { MonsterKind.ShadowBrute },
                HealthMultiplier = 1.28f, DamageMultiplier = 1.2f, Resilience = 6,
                Scale = 1.00f, AuraColor = new Color(235, 205, 245),
                Abilities = BossAbility.Beam | BossAbility.KiBlast | BossAbility.DeathBall,
                PhaseCount = 2, PhaseAbility = BossAbility.KiBlast, SpriteSheet = "friezafinal"
            },
            new BossDefinition
            {
                Id = "BallGuardian2", DisplayName = "Guardian of the Two-Star Ball",
                Subtitle = "Coiled around its prize",
                MineLevel = 130, Reward = BossReward.DragonBall, DragonBallNumber = 2,
                BonusTechniqueId = "SpiritBomb",
                Squad = new[] { MonsterKind.ShadowBrute },
                HealthMultiplier = 1.15f, DamageMultiplier = 1.1f, Resilience = 5,
                Scale = 0.88f, AuraColor = new Color(255, 170, 60), SpriteSheet = "guardian"
            },
            new BossDefinition
            {
                Id = "FriezaGolden", DisplayName = "Golden Frieza",
                Subtitle = "He has touched god ki. It shows.",
                MineLevel = 280, Reward = BossReward.PowerCache, CacheKi = 78f, CacheAttack = 0.12f,
                Squad = new[] { MonsterKind.ShadowBrute },
                HealthMultiplier = 1.78f, DamageMultiplier = 1.44f, Resilience = 13,
                Scale = 1.05f, AuraColor = new Color(255, 215, 90),
                Abilities = BossAbility.Beam | BossAbility.KiBlast | BossAbility.DeathBall, SpeedBonus = 3,
                PhaseCount = 2, PhaseAbility = BossAbility.KiBlast, SpriteSheet = "friezagolden"
            },
            new BossDefinition
            {
                Id = "CoolerFinal", DisplayName = "Cooler (Final Form)",
                Subtitle = "The form his brother never reached",
                MineLevel = 150, Reward = BossReward.Supplies, SupplySenzu = 3, SupplyGold = 16000,
                Squad = new[] { MonsterKind.ShadowBrute },
                HealthMultiplier = 1.25f, DamageMultiplier = 1.2f, Resilience = 6,
                Scale = 1.00f, AuraColor = new Color(110, 210, 200),
                Abilities = BossAbility.Beam, SpeedBonus = 1, PhaseCount = 1,
                SpriteSheet = "coolerfinal"
            },
            new BossDefinition
            {
                Id = "BallGuardian3", DisplayName = "Guardian of the Three-Star Ball",
                Subtitle = "Two of them, and neither tires",
                MineLevel = 160, Reward = BossReward.DragonBall, DragonBallNumber = 3,
                Squad = new[] { MonsterKind.ShadowBrute, MonsterKind.ShadowBrute },
                HealthMultiplier = 1.2f, DamageMultiplier = 1.15f, Resilience = 6,
                Scale = 0.88f, AuraColor = new Color(255, 170, 60), SpriteSheet = "guardian"
            },
            new BossDefinition
            {
                Id = "CellImperfect", DisplayName = "Imperfect Cell",
                Subtitle = "Still feeding. Do not let him.",
                MineLevel = 170, Reward = BossReward.PowerCache, CacheKi = 38f, CacheAttack = 0.055f,
                Squad = new[] { MonsterKind.Mummy },
                HealthMultiplier = 1.2f, DamageMultiplier = 1.15f, Resilience = 6,
                Scale = 0.90f, AuraColor = new Color(120, 200, 120),
                Abilities = BossAbility.Regenerate | BossAbility.Absorb, SpriteSheet = "cellimperfect"
            },
            new BossDefinition
            {
                Id = "CellSemiPerfect", DisplayName = "Semi-Perfect Cell",
                Subtitle = "One android short of complete",
                MineLevel = 175, Reward = BossReward.Supplies, SupplySenzu = 3, SupplyGold = 18000,
                Squad = new[] { MonsterKind.Mummy },
                HealthMultiplier = 1.25f, DamageMultiplier = 1.2f, Resilience = 7,
                Scale = 0.90f, AuraColor = new Color(140, 220, 140),
                Abilities = BossAbility.Regenerate | BossAbility.Absorb, SpriteSheet = "cellsemiperfect"
            },
            new BossDefinition
            {
                Id = "CellJuniors", DisplayName = "Cell Juniors",
                Subtitle = "Small, blue, and merciless",
                MineLevel = 180, Reward = BossReward.Supplies, SupplySenzu = 3, SupplyGold = 20000,
                Squad = new[] { MonsterKind.ShadowBrute, MonsterKind.ShadowBrute,
                                MonsterKind.ShadowBrute, MonsterKind.ShadowBrute },
                HealthMultiplier = 1.25f, DamageMultiplier = 1.2f, Resilience = 6,
                Scale = 0.92f, AuraColor = new Color(150, 210, 255), SpriteSheet = "celljr"
            },
            new BossDefinition
            {
                Id = "CellPerfect", DisplayName = "Perfect Cell",
                Subtitle = "Complete. And he wants you to know it.",
                MineLevel = 190, Reward = BossReward.Form, FormIndex = 4,
                Squad = new[] { MonsterKind.Mummy },
                HealthMultiplier = 1.35f, DamageMultiplier = 1.25f, Resilience = 7,
                Scale = 0.98f, AuraColor = new Color(140, 240, 140),
                Abilities = BossAbility.Beam | BossAbility.Regenerate | BossAbility.Absorb,
                PhaseCount = 2, SpriteSheet = "cellperfect"
            },

            // ---- 200-300: Buu saga and the deep endgame --------------------
            new BossDefinition
            {
                Id = "Bojack", DisplayName = "Bojack",
                Subtitle = "Sealed away once. Not well enough.",
                MineLevel = 200, Reward = BossReward.Technique, TechniqueId = "Kaioken",
                Squad = new[] { MonsterKind.ShadowBrute, MonsterKind.ShadowBrute },
                HealthMultiplier = 1.3f, DamageMultiplier = 1.2f, Resilience = 7,
                Scale = 0.92f, AuraColor = new Color(120, 230, 170), SpriteSheet = "bojack"
            },
            new BossDefinition
            {
                Id = "Dabura", DisplayName = "Dabura, Demon King",
                Subtitle = "Spit turns flesh to stone",
                MineLevel = 210, Reward = BossReward.Supplies, SupplySenzu = 3, SupplyGold = 24000,
                Squad = new[] { MonsterKind.ShadowBrute, MonsterKind.ShadowBrute,
                                MonsterKind.ShadowBrute },
                HealthMultiplier = 1.3f, DamageMultiplier = 1.2f, Resilience = 8,
                Scale = 0.92f, AuraColor = new Color(200, 60, 60), SpriteSheet = "dabura",
                Abilities = BossAbility.Paralyze
            },
            new BossDefinition
            {
                Id = "BallGuardian4", DisplayName = "Guardian of the Four-Star Ball",
                Subtitle = "Deep in the Skull Cavern now",
                MineLevel = 220, Reward = BossReward.DragonBall, DragonBallNumber = 4,
                Squad = new[] { MonsterKind.ShadowBrute, MonsterKind.ShadowBrute },
                HealthMultiplier = 1.3f, DamageMultiplier = 1.2f, Resilience = 8,
                Scale = 0.88f, AuraColor = new Color(255, 170, 60), SpriteSheet = "guardian"
            },
            new BossDefinition
            {
                Id = "BuuFat", DisplayName = "Fat Buu",
                Subtitle = "Childlike, until it is not",
                MineLevel = 225, Reward = BossReward.PowerCache, CacheKi = 48f, CacheAttack = 0.07f,
                Squad = new[] { MonsterKind.Mummy },
                HealthMultiplier = 1.35f, DamageMultiplier = 1.25f, Resilience = 8,
                Scale = 1.05f, AuraColor = new Color(255, 180, 220),
                Abilities = BossAbility.Regenerate | BossAbility.CandyBeam, SpriteSheet = "buufat"
            },
            new BossDefinition
            {
                Id = "SuperBuu", DisplayName = "Super Buu",
                Subtitle = "It copies what it eats",
                MineLevel = 230, Reward = BossReward.Supplies, SupplySenzu = 4, SupplyGold = 30000,
                Squad = new[] { MonsterKind.Mummy, MonsterKind.Mummy },
                HealthMultiplier = 1.4f, DamageMultiplier = 1.25f, Resilience = 8,
                Scale = 0.78f, AuraColor = new Color(255, 130, 210), SpriteSheet = "superbuu",
                Abilities = BossAbility.Regenerate | BossAbility.CandyBeam
            },
            new BossDefinition
            {
                Id = "BuuSuperGohan", DisplayName = "Super Buu (Gohan absorbed)",
                Subtitle = "It ate a demigod and grew calm",
                MineLevel = 240, Reward = BossReward.Form, FormIndex = 5,
                Squad = new[] { MonsterKind.Mummy },
                HealthMultiplier = 1.55f, DamageMultiplier = 1.3f, Resilience = 9,
                Scale = 0.98f, AuraColor = new Color(255, 120, 200),
                Abilities = BossAbility.Beam | BossAbility.Regenerate | BossAbility.Teleport
                            | BossAbility.CandyBeam,
                PhaseCount = 2, PhaseAbility = BossAbility.Teleport, SpriteSheet = "buusupergohan"
            },
            new BossDefinition
            {
                Id = "BallGuardian5", DisplayName = "Guardian of the Five-Star Ball",
                Subtitle = "Three abreast, filling the tunnel",
                MineLevel = 250, Reward = BossReward.DragonBall, DragonBallNumber = 5,
                Squad = new[] { MonsterKind.ShadowBrute, MonsterKind.ShadowBrute,
                                MonsterKind.ShadowBrute },
                HealthMultiplier = 1.35f, DamageMultiplier = 1.25f, Resilience = 9,
                Scale = 0.88f, AuraColor = new Color(255, 170, 60), SpriteSheet = "guardian"
            },
            new BossDefinition
            {
                Id = "MetalCoolerLegion", DisplayName = "Metal Cooler Legion",
                Subtitle = "Break one and the next steps forward",
                MineLevel = 260, Reward = BossReward.Supplies, SupplySenzu = 4, SupplyGold = 45000,
                Squad = new[] { MonsterKind.Mummy, MonsterKind.Mummy, MonsterKind.Mummy,
                                MonsterKind.Mummy },
                HealthMultiplier = 1.5f, DamageMultiplier = 1.35f, Resilience = 11,
                Scale = 0.92f, AuraColor = new Color(170, 220, 255), SpriteSheet = "metalcooler"
            },
            new BossDefinition
            {
                Id = "KidBuu", DisplayName = "Kid Buu",
                Subtitle = "The original, and the worst",
                MineLevel = 265, Reward = BossReward.PowerCache, CacheKi = 62f, CacheAttack = 0.10f,
                Squad = new[] { MonsterKind.Mummy, MonsterKind.Mummy,
                                MonsterKind.Mummy, MonsterKind.Mummy },
                HealthMultiplier = 1.65f, DamageMultiplier = 1.4f, Resilience = 12,
                Scale = 0.92f, AuraColor = new Color(255, 150, 210), SpriteSheet = "kidbuu",
                Abilities = BossAbility.Regenerate | BossAbility.Shockwave | BossAbility.CandyBeam,
                SpeedBonus = 2, PhaseCount = 1
            },
            new BossDefinition
            {
                Id = "BallGuardian6", DisplayName = "Guardian of the Six-Star Ball",
                Subtitle = "The dust never settles around them",
                MineLevel = 270, Reward = BossReward.DragonBall, DragonBallNumber = 6,
                Squad = new[] { MonsterKind.ShadowBrute, MonsterKind.ShadowBrute,
                                MonsterKind.ShadowBrute },
                HealthMultiplier = 1.4f, DamageMultiplier = 1.3f, Resilience = 10,
                Scale = 0.88f, AuraColor = new Color(255, 170, 60), SpriteSheet = "guardian"
            },
            new BossDefinition
            {
                Id = "Broly", DisplayName = "Broly, the Legendary",
                Subtitle = "He does not stop, and he does not tire",
                MineLevel = 205, Reward = BossReward.PowerCache, CacheKi = 44f, CacheAttack = 0.07f,
                Squad = new[] { MonsterKind.ShadowBrute },
                HealthMultiplier = 1.4f, DamageMultiplier = 1.25f, Resilience = 8,
                Scale = 1.12f, AuraColor = new Color(150, 240, 120), SpriteSheet = "broly",
                Abilities = BossAbility.Shockwave, SpeedBonus = 1,
                PhaseCount = 2, PhaseAbility = BossAbility.KiBlast
            },
            new BossDefinition
            {
                Id = "FriezaBlack", DisplayName = "Black Frieza",
                Subtitle = "He surpassed everyone. Quietly.",
                MineLevel = 285, Reward = BossReward.PowerCache, CacheKi = 80f, CacheAttack = 0.13f,
                Squad = new[] { MonsterKind.ShadowBrute },
                HealthMultiplier = 1.8f, DamageMultiplier = 1.45f, Resilience = 13,
                Scale = 1.15f, AuraColor = new Color(130, 60, 160),
                Abilities = BossAbility.Beam | BossAbility.KiBlast | BossAbility.Teleport
                            | BossAbility.DeathBall,
                SpeedBonus = 3, PhaseCount = 2, PhaseAbility = BossAbility.Beam,
                SpriteSheet = "friezablack"
            },
            new BossDefinition
            {
                Id = "BallGuardian7", DisplayName = "Guardian of the Seven-Star Ball",
                Subtitle = "The last sphere is the worst defended by far",
                MineLevel = 290, Reward = BossReward.DragonBall, DragonBallNumber = 7,
                Squad = new[] { MonsterKind.ShadowBrute, MonsterKind.ShadowBrute,
                                MonsterKind.ShadowBrute, MonsterKind.ShadowBrute },
                HealthMultiplier = 1.5f, DamageMultiplier = 1.35f, Resilience = 11,
                Scale = 0.88f, AuraColor = new Color(255, 170, 60), SpriteSheet = "guardian"
            },
            new BossDefinition
            {
                Id = "Destroyer", DisplayName = "God of Destruction",
                Subtitle = "You should not have come this deep",
                MineLevel = 300, Reward = BossReward.PowerCache, CacheKi = 90f, CacheAttack = 0.15f,
                Squad = new[] { MonsterKind.ShadowBrute, MonsterKind.ShadowBrute,
                                MonsterKind.ShadowBrute, MonsterKind.ShadowBrute },
                HealthMultiplier = 1.85f, DamageMultiplier = 1.5f, Resilience = 14,
                Scale = 1.18f, AuraColor = new Color(190, 120, 255), SpriteSheet = "destroyer",
                Abilities = BossAbility.Teleport | BossAbility.KiBlast | BossAbility.Rush,
                SpeedBonus = 3, PhaseCount = 2, PhaseAbility = BossAbility.Beam
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
        public const string PhaseKey = "khaleelkhan.SaiyanTransformations/phase";

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

        /// <summary>One queued dialogue box (narrator or boss), opened on the next free tick
        /// so a warp-in or a kill does not try to open a dialogue mid-transition. Steps are
        /// shown one at a time - the next only opens once the player dismisses the current -
        /// so a beat's narrator line and the boss's reply read in order.</summary>
        private sealed class DialogueStep
        {
            public string Id;
            public string Name;
            public string Text;
            public bool Portrait;   // true = boss portrait box, false = plain narrator box
        }

        private readonly Queue<DialogueStep> pendingDialogue = new Queue<DialogueStep>();

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

        /// <summary>Turn every boss ki blast currently in flight back on its casters - the
        /// reward for a well-timed parry. Also lets out a short counter-burst near the player.</summary>
        public void ReflectBossOrbs(Vector2 playerCentre)
        {
            this.abilities?.ReflectOrbs(playerCentre);
        }

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
            this.pendingDialogue.Clear();
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

        /// <summary>Frame size for each hand-drawn boss sheet. These are deliberately larger
        /// than a vanilla monster so the art keeps its detail; a sheet not listed here falls
        /// back to its monster kind's native frame size.</summary>
        private static readonly Dictionary<string, (int W, int H)> SheetDims =
            new Dictionary<string, (int, int)>
            {
                ["saibamen"] = (24, 34), ["celljr"] = (22, 32), ["friezafirst"] = (24, 36),
                ["nappa"] = (30, 46), ["eliteboss"] = (28, 44), ["burter"] = (28, 46),
                ["recoome"] = (32, 48), ["captainginyu"] = (28, 44), ["friezafinal"] = (26, 42),
                ["friezagolden"] = (28, 46), ["friezablack"] = (28, 46), ["coolerfirst"] = (28, 44),
                ["coolerfinal"] = (30, 48), ["metalcooler"] = (28, 46), ["cellimperfect"] = (30, 46),
                ["cellsemiperfect"] = (30, 48), ["cellperfect"] = (30, 50), ["bojack"] = (28, 46),
                ["dabura"] = (28, 46), ["broly"] = (32, 50), ["destroyer"] = (30, 50),
                ["buufat"] = (32, 46), ["buusupergohan"] = (30, 50), ["superbuu"] = (40, 64),
                ["guardian"] = (30, 48), ["invader"] = (42, 66),
                ["jeice"] = (38, 60), ["guldo"] = (34, 50), ["kidbuu"] = (28, 46)
            };

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
                if (SheetDims.TryGetValue(overrideSheet, out (int W, int H) d))
                {
                    w = d.W;
                    h = d.H;
                }
                else
                {
                    FrameDimsFor(kind, out w, out h);   // e.g. the invader placeholder
                }
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
            // Tuned upward from the old curve to keep pace with the mastery-carryover power
            // gains (mastering forms grants stacking, global stat bonuses), with the biggest
            // bumps late, where the player has had the most time to master forms.
            if (mineLevel < 10) return 1f;      // base, before the first fight
            if (mineLevel < 50) return 1.6f;    // Super Saiyan
            if (mineLevel < 90) return 2.4f;    // Super Saiyan 2
            if (mineLevel < 140) return 3.6f;   // Super Saiyan 3
            if (mineLevel < 190) return 5.2f;   // Super Saiyan God
            if (mineLevel < 240) return 7.2f;   // Super Saiyan Blue
            if (mineLevel < 280) return 10.5f;  // Ultra Instinct
            return 15f;                         // Mastered Ultra Instinct, floors 280+
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
            // flat uplift so boss hits still land through the extra defense that mastery
            // carryover hands the player; the difficulty preset scales on top of this
            const float masteryOffset = 1.15f;
            return Math.Max(1, (int)(Owner.Config.BaseBossDamage
                                     * (1f + (def.MineLevel / divisor))
                                     * depthBite * def.DamageMultiplier
                                     * cycleDmg * this.RematchMultiplier(def)
                                     * masteryOffset
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
            tile = Vector2.Zero;

            // Flood-fill the walkable region the player is actually standing in and only
            // spawn somewhere inside it. Picking random map tiles could drop a boss into a
            // sealed-off cavern pocket (or off the walkable area entirely) where the player
            // can never reach it - and, with the floor gate, could never leave either.
            Vector2 start = new Vector2((int)playerTile.X, (int)playerTile.Y);
            var visited = new HashSet<Vector2> { start };
            var queue = new Queue<Vector2>();
            queue.Enqueue(start);

            var preferred = new List<Vector2>();   // 4-18 tiles away: normal spawn band
            var fallback = new List<Vector2>();    // 3+ tiles: used only if the band is empty

            Vector2[] steps = { new Vector2(1, 0), new Vector2(-1, 0),
                                new Vector2(0, 1), new Vector2(0, -1) };

            int guard = 0;
            while (queue.Count > 0 && guard++ < 6000)
            {
                Vector2 cur = queue.Dequeue();
                float dist = Vector2.Distance(cur, start);

                if (dist >= 3f && this.IsSpawnable(location, cur))
                {
                    if (dist >= 4f && dist <= 18f)
                        preferred.Add(cur);
                    else
                        fallback.Add(cur);
                }

                if (dist > 22f)          // no need to walk the whole map
                    continue;

                foreach (Vector2 step in steps)
                {
                    Vector2 n = cur + step;
                    if (!visited.Add(n))
                        continue;
                    if (!location.isTileOnMap(n))
                        continue;
                    if (!location.isTilePassable(
                            new xTile.Dimensions.Location((int)n.X, (int)n.Y), Game1.viewport))
                        continue;
                    if (location.isWaterTile((int)n.X, (int)n.Y))
                        continue;
                    queue.Enqueue(n);
                }
            }

            List<Vector2> pool = preferred.Count > 0 ? preferred : fallback;
            if (pool.Count == 0)
                return false;

            tile = pool[Game1.random.Next(pool.Count)];
            return true;
        }

        private void Announce(BossDefinition def, bool returning)
        {
            this.introTicks = 1;
            Owner.PlayCue("boss_roar", "shadowpeep");
            if (Owner.Config.ScreenFlash)
                Game1.flashAlpha = 0.5f;

            // a fresh spawn gets its scene and its line; a boss you merely walked back in on
            // just gets a quiet reminder that it is still down there
            if (returning)
            {
                ModEntry.Notify($"{def.DisplayName} is still here.");
                return;
            }

            Beat beat = this.EncounterBeat(def);
            if (beat != null)
                this.Play(beat, def);
            else
                ModEntry.Notify($"{def.DisplayName} blocks the way down!");
        }

        /// <summary>The meeting beat for a boss, chosen by how many times it has been beaten:
        /// first meeting, second, third, then a repeatable beat from the fourth on.</summary>
        private Beat EncounterBeat(BossDefinition def)
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

        /// <summary>Queue a beat: the narrator's scene-setting line as a plain dialogue box,
        /// then the boss's own words as a portrait box. Both stay up until dismissed so they
        /// can actually be read, and they open one after the other.</summary>
        private void Play(Beat beat, BossDefinition def)
        {
            if (beat == null)
                return;
            if (!string.IsNullOrEmpty(beat.Narration))
                this.pendingDialogue.Enqueue(new DialogueStep { Text = beat.Narration, Portrait = false });
            if (!string.IsNullOrEmpty(beat.Speech))
                this.pendingDialogue.Enqueue(new DialogueStep
                {
                    Id = def.Id, Name = def.DisplayName, Text = beat.Speech, Portrait = true
                });
        }

        /// <summary>Open the next queued dialogue box once the world can display one (not
        /// mid-warp, not with a menu already open). One per free tick, so each box waits for
        /// the previous to be dismissed.</summary>
        private void ShowPendingDialogue()
        {
            if (this.pendingDialogue.Count == 0)
                return;
            if (!Context.IsPlayerFree || Game1.activeClickableMenu != null || Game1.eventUp)
                return;

            DialogueStep step = this.pendingDialogue.Dequeue();
            if (step.Portrait)
                Owner.ShowBossSpeech(step.Id, step.Name, step.Text);
            else
                Owner.ShowNarration(step.Text);
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
                    if (Owner.Config.EnableBossPhases)
                        this.CheckPhase(monster, def);
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

        /// <summary>Major bosses power up as their health falls. Each phase hits harder and
        /// moves faster, and the last phase can hand the boss a brand-new move.</summary>
        private void CheckPhase(Monster monster, BossDefinition def)
        {
            if (def.PhaseCount <= 0 || monster.MaxHealth <= 0)
                return;

            int done = 0;
            if (monster.modData.TryGetValue(PhaseKey, out string s))
                int.TryParse(s, out done);
            if (done >= def.PhaseCount)
                return;

            // thresholds spaced evenly: 2 phases -> 66% and 33%
            float fraction = (float)monster.Health / monster.MaxHealth;
            float threshold = 1f - ((done + 1f) / (def.PhaseCount + 1f));
            if (fraction > threshold)
                return;

            done++;
            monster.modData[PhaseKey] = done.ToString();
            monster.DamageToFarmer = (int)(monster.DamageToFarmer * 1.3f);
            monster.speed += 1;
            monster.resilience.Value += 2;

            // the final phase can grant a new ability the boss did not start with
            if (done >= def.PhaseCount && def.PhaseAbility != BossAbility.None)
                monster.modData[BossAbilityRunner.PhaseAbilityKey] = ((int)def.PhaseAbility).ToString();

            Owner.PlayCue("boss_roar", "shadowpeep");
            if (Owner.Config.ScreenFlash)
                Game1.flashAlpha = 0.6f;
            ModEntry.Notify(done >= def.PhaseCount
                ? $"{def.DisplayName} stops holding back!"
                : $"{def.DisplayName}'s power surges!");
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
            // all Shadow Brutes so every body can wear the single hand-drawn invader sheet
            var squad = new System.Collections.Generic.List<MonsterKind> { MonsterKind.ShadowBrute };
            if (defeats >= 1) squad.Add(MonsterKind.ShadowBrute);
            if (defeats >= 3) squad.Add(MonsterKind.ShadowBrute);
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
                monster.Scale = 0.82f;   // 66px hand-drawn sheet; keep on-screen size sane
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

            // the boss's parting scene and line, shown once the drop menus (if any) have cleared
            BossLines lines = BossDialogue.For(def.Id);
            if (lines != null)
                this.Play(lines.Defeat, def);

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
