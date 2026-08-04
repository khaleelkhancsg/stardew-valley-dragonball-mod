using Microsoft.Xna.Framework;

namespace SaiyanTransformations
{
    /// <summary>One transformation stage and everything that makes it feel different.</summary>
    public sealed class Transformation
    {
        /// <summary>Stable key, used for the buff id and console commands.</summary>
        public string Id;

        public string DisplayName;
        public string Description;

        /// <summary>Column in assets/saiyanhair.png and assets/icons.png.</summary>
        public int SpriteIndex;

        /// <summary>File in assets/sounds (without extension) played when entering this form.</summary>
        public string SoundName;

        /// <summary>Entry added to Data/HairData. Kept far from the ranges used by
        /// popular hair packs so it will not collide with other mods.</summary>
        public int HairId;

        public Color AuraColor;

        /// <summary>Default mine level that unlocks this form; overridable in config.</summary>
        public int DefaultUnlockLevel;

        public float AttackMultiplier;
        public int FlatAttack;
        public int SpeedBonus;
        public float EnergyMultiplier;
        public int Defense;
        public float CritChanceBonus;
        public float CritPowerBonus;
        public float WeaponSpeedBonus;

        /// <summary>Energy burned per second while the form is held.</summary>
        public float StaminaDrainPerSecond;

        /// <summary>Multiplier applied to Kamehameha damage in this form.</summary>
        public float KamehamehaMultiplier;

        /// <summary>Chance to evade an incoming hit outright. Ultra Instinct has this;
        /// it is paid for with a much steeper energy drain.</summary>
        public float DodgeChance;

        /// <summary>Extra dodge chance earned by going unhit, on top of DodgeChance.
        /// Only Mastered Ultra Instinct uses this.</summary>
        public float DodgeGrowth;

        /// <summary>Wishes that must already be granted before this form can unlock at all.
        /// Used to gate Mastered Ultra Instinct behind winning wish trials.</summary>
        public int RequiredWishes;

        /// <summary>Whether the aura crackles with lightning.</summary>
        public bool Lightning;

        public float AuraScale;

        /// <summary>Per-form passive: health restored per second while this form is held.
        /// Scales up with the form, so deeper forms sustain you harder.</summary>
        public float HealthRegenPerSecond;

        /// <summary>Short passive tagline shown on the form's buff, so each form reads as
        /// having its own identity beyond raw numbers.</summary>
        public string Passive;

        public static readonly Transformation[] All =
        {
            new Transformation
            {
                Id = "SuperSaiyan", SoundName = "transform_ssj", DisplayName = "Super Saiyan",
                Description = "Golden fury. Attack x1.5, faster, tougher.",
                SpriteIndex = 0, HairId = 77213001, DefaultUnlockLevel = 10,
                AuraColor = new Color(255, 214, 78),
                AttackMultiplier = 1.5f, FlatAttack = 5, SpeedBonus = 1, EnergyMultiplier = 1.25f,
                Defense = 2, CritChanceBonus = 0.10f, CritPowerBonus = 0.15f, WeaponSpeedBonus = 0.15f,
                StaminaDrainPerSecond = 0.20f, KamehamehaMultiplier = 1.0f,
                HealthRegenPerSecond = 0.2f, Passive = "Steady regeneration, balanced power.",
                Lightning = false, AuraScale = 1.00f
            },
            new Transformation
            {
                Id = "SuperSaiyan2", SoundName = "transform_ssj2", DisplayName = "Super Saiyan 2",
                Description = "Sparks of rage. Attack x2, energy x1.5.",
                SpriteIndex = 1, HairId = 77213002, DefaultUnlockLevel = 50,
                AuraColor = new Color(255, 233, 108),
                AttackMultiplier = 2.0f, FlatAttack = 10, SpeedBonus = 2, EnergyMultiplier = 1.5f,
                Defense = 4, CritChanceBonus = 0.15f, CritPowerBonus = 0.25f, WeaponSpeedBonus = 0.25f,
                StaminaDrainPerSecond = 0.35f, KamehamehaMultiplier = 1.4f,
                HealthRegenPerSecond = 0.3f, Passive = "Sharper crits and quicker swings.",
                Lightning = true, AuraScale = 1.05f
            },
            new Transformation
            {
                Id = "SuperSaiyan3", SoundName = "transform_ssj3", DisplayName = "Super Saiyan 3",
                Description = "Overwhelming, and it burns energy fast.",
                SpriteIndex = 2, HairId = 77213003, DefaultUnlockLevel = 90,
                AuraColor = new Color(255, 204, 56),
                AttackMultiplier = 3.0f, FlatAttack = 18, SpeedBonus = 3, EnergyMultiplier = 1.75f,
                Defense = 6, CritChanceBonus = 0.20f, CritPowerBonus = 0.35f, WeaponSpeedBonus = 0.35f,
                StaminaDrainPerSecond = 0.90f, KamehamehaMultiplier = 1.9f,
                HealthRegenPerSecond = 0.5f, Passive = "Overwhelming power at a steep ki cost.",
                Lightning = true, AuraScale = 1.15f
            },
            new Transformation
            {
                Id = "SuperSaiyanGod", SoundName = "transform_god", DisplayName = "Super Saiyan God",
                Description = "Divine ki. Attack x4, energy x2.",
                SpriteIndex = 3, HairId = 77213004, DefaultUnlockLevel = 140,
                AuraColor = new Color(255, 84, 122),
                AttackMultiplier = 4.0f, FlatAttack = 26, SpeedBonus = 4, EnergyMultiplier = 2.0f,
                Defense = 9, CritChanceBonus = 0.25f, CritPowerBonus = 0.45f, WeaponSpeedBonus = 0.40f,
                StaminaDrainPerSecond = 0.50f, KamehamehaMultiplier = 2.5f,
                HealthRegenPerSecond = 0.8f, Passive = "Divine durability and regeneration.",
                Lightning = false, AuraScale = 1.10f
            },
            new Transformation
            {
                Id = "SuperSaiyanBlue", SoundName = "transform_blue", DisplayName = "Super Saiyan Blue",
                Description = "God ki under perfect control. Attack x5.5.",
                SpriteIndex = 4, HairId = 77213005, DefaultUnlockLevel = 190,
                AuraColor = new Color(112, 202, 255),
                AttackMultiplier = 5.5f, FlatAttack = 36, SpeedBonus = 5, EnergyMultiplier = 2.5f,
                Defense = 12, CritChanceBonus = 0.30f, CritPowerBonus = 0.60f, WeaponSpeedBonus = 0.45f,
                StaminaDrainPerSecond = 0.75f, KamehamehaMultiplier = 3.2f,
                HealthRegenPerSecond = 1.0f, Passive = "God ki under control; strong all round.",
                Lightning = true, AuraScale = 1.15f
            },
            new Transformation
            {
                Id = "UltraInstinct", SoundName = "transform_ui", DisplayName = "Ultra Instinct",
                Description = "The body moves on its own. Attack x8, energy x3, evades half of all hits.",
                SpriteIndex = 5, HairId = 77213006, DefaultUnlockLevel = 240,
                AuraColor = new Color(214, 236, 255),
                AttackMultiplier = 8.0f, FlatAttack = 50, SpeedBonus = 6, EnergyMultiplier = 3.0f,
                Defense = 16, CritChanceBonus = 0.40f, CritPowerBonus = 0.85f, WeaponSpeedBonus = 0.55f,
                StaminaDrainPerSecond = 1.75f, KamehamehaMultiplier = 4.5f,
                HealthRegenPerSecond = 1.3f, Passive = "The body dodges half of all hits on its own.",
                DodgeChance = 0.5f, Lightning = true, AuraScale = 1.25f
            },
            new Transformation
            {
                Id = "MasteredUltraInstinct", SoundName = "transform_ui",
                DisplayName = "Mastered Ultra Instinct",
                Description = "Perfect stillness. Attack x12, and evasion that climbs while untouched.",
                SpriteIndex = 6, HairId = 77213007, DefaultUnlockLevel = 280,
                RequiredWishes = 5,
                AuraColor = new Color(238, 246, 255),
                AttackMultiplier = 12f, FlatAttack = 70, SpeedBonus = 7, EnergyMultiplier = 3.5f,
                Defense = 22, CritChanceBonus = 0.5f, CritPowerBonus = 1.1f, WeaponSpeedBonus = 0.65f,
                StaminaDrainPerSecond = 2.4f, KamehamehaMultiplier = 6f,
                HealthRegenPerSecond = 1.8f, Passive = "Evasion climbs the longer you go untouched.",
                DodgeChance = 0.55f, DodgeGrowth = 0.3f,
                Lightning = true, AuraScale = 1.35f
            }
        };
    }
}
