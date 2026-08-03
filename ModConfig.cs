using StardewModdingAPI.Utilities;
using StardewValley;

namespace SaiyanTransformations
{
    public sealed class ModConfig
    {
        // ---- controls ----------------------------------------------------

        /// <summary>Ascend one stage. Press at your highest unlocked form to power down.</summary>
        public KeybindList TransformKey { get; set; } = KeybindList.Parse("F");

        /// <summary>Drop straight back to base form.</summary>
        public KeybindList PowerDownKey { get; set; } = KeybindList.Parse("LeftShift + F");

        /// <summary>Fire the equipped technique. Only works while transformed.
        /// Defaults to R rather than G because G is commonly taken by other mods
        /// (Deluxe Grabber, Status Announcements, Customize Dresser).</summary>
        public KeybindList KamehamehaKey { get; set; } = KeybindList.Parse("R");

        /// <summary>Cycle to the next unlocked technique. Checked before the fire key,
        /// since SMAPI keybinds are not exclusive and Shift+R also satisfies plain R.</summary>
        public KeybindList SwitchTechniqueKey { get; set; } = KeybindList.Parse("LeftShift + R");

        // ---- unlocks -----------------------------------------------------

        /// <summary>Mine level that unlocks each form. Skull Cavern levels count as 121+.</summary>
        public int SuperSaiyanMineLevel { get; set; } = 10;
        public int SuperSaiyan2MineLevel { get; set; } = 50;
        public int SuperSaiyan3MineLevel { get; set; } = 90;
        public int SuperSaiyanGodMineLevel { get; set; } = 140;
        public int SuperSaiyanBlueMineLevel { get; set; } = 190;
        public int UltraInstinctMineLevel { get; set; } = 240;
        public int MasteredUltraInstinctMineLevel { get; set; } = 280;

        /// <summary>Wishes that must already be granted before Mastered Ultra Instinct can
        /// unlock. With FreeWishes at 3, the default means winning two wish trials.</summary>
        public int MasteredUltraInstinctWishes { get; set; } = 5;

        /// <summary>Seconds without taking a hit for Mastered Ultra Instinct's evasion to
        /// climb from its base to its maximum.</summary>
        public float MasteredDodgeRampSeconds { get; set; } = 25f;

        /// <summary>Skip the mine requirement entirely.</summary>
        public bool UnlockEverything { get; set; } = false;

        // ---- ki ----------------------------------------------------------

        /// <summary>Hold this, standing perfectly still, to charge ki quickly.
        /// Any movement, any tool use, or any damage taken breaks the stance.</summary>
        public KeybindList ChargeKey { get; set; } = KeybindList.Parse("LeftShift");

        /// <summary>Ki capacity before mine bonuses.</summary>
        public float BaseMaxKi { get; set; } = 100f;

        /// <summary>Extra capacity per mine floor reached. At floor 120 the default
        /// gives +240, so capacity roughly quadruples over a full descent.</summary>
        public float KiPerMineLevel { get; set; } = 2f;

        /// <summary>Trickle regeneration, always active.</summary>
        public float PassiveKiPerSecond { get; set; } = 1.5f;

        /// <summary>Regeneration while holding the charge stance.</summary>
        public float ActiveKiPerSecond { get; set; } = 14f;

        /// <summary>Passive regeneration still runs at full rate while exhausted; only the
        /// charge stance is locked out. Lower this if you want the recovery to bite harder.</summary>
        public float ExhaustedRegenScale { get; set; } = 1f;

        /// <summary>Ki fraction at or below which exhaustion sets in - your form drops and
        /// the stat penalty applies. Not just at empty.</summary>
        public float ExhaustionEnterFraction { get; set; } = 0.2f;

        /// <summary>Ki fraction that ends exhaustion. Forced to sit at least 0.1 above the
        /// enter point so the state cannot flicker on and off at the boundary.</summary>
        public float ExhaustionRecoveryFraction { get; set; } = 0.3f;
        /// <summary>Ki fraction below which the bar turns red as a low-energy warning.</summary>
        public float KiLowFraction { get; set; } = 0.4f;

        /// <summary>How much weaker everything is while exhausted. 0.8 = 80% weaker.</summary>
        public float ExhaustedStatPenalty { get; set; } = 0.8f;

        /// <summary>Nudge the ki bar if it does not sit where you want beside the
        /// vanilla energy meter.</summary>
        public int KiBarOffsetX { get; set; } = 0;
        public int KiBarOffsetY { get; set; } = 0;

        // ---- mastery and Zenkai ------------------------------------------

        /// <summary>Minutes spent holding a form to fully master it.</summary>
        public float MasteryFullMinutes { get; set; } = 30f;

        /// <summary>Ki drain reduction at full mastery. 0.4 = 40% cheaper to hold.</summary>
        public float MasteryMaxDrainReduction { get; set; } = 0.4f;

        /// <summary>Permanent bumps for surviving a near-death fight.</summary>
        public bool EnableZenkai { get; set; } = true;

        /// <summary>Health fraction you must drop below to arm a Zenkai.</summary>
        public float ZenkaiHealthFraction { get; set; } = 0.1f;

        public float ZenkaiKiPerBoost { get; set; } = 8f;
        public float ZenkaiAttackPerBoost { get; set; } = 0.02f;

        // ---- transformed combat feel --------------------------------------

        /// <summary>Fading copies of your sprite while running transformed.</summary>
        public bool ShowAfterimage { get; set; } = true;
        public int AfterimageCount { get; set; } = 4;
        public int AfterimageIntervalTicks { get; set; } = 4;
        public int AfterimageLifetimeTicks { get; set; } = 22;

        /// <summary>Weapon swings while transformed throw a shockwave past the blade.</summary>
        public bool EnableKiMelee { get; set; } = true;
        public float KiMeleeCost { get; set; } = 2f;
        public int KiMeleeRangeTiles { get; set; } = 2;
        public float KiMeleeDamageFraction { get; set; } = 0.35f;

        // ---- bosses ------------------------------------------------------

        /// <summary>Spawn a guardian on each milestone floor.</summary>
        public bool EnableBosses { get; set; } = true;

        /// <summary>Seal the way down while a boss is alive on the floor, so no boss can be
        /// skipped: try to descend past a living boss and you are sent back to finish it.
        /// Turn off to let bosses be walked past like ordinary monsters.</summary>
        public bool GateBossFloors { get; set; } = true;

        /// <summary>In-game days before a beaten boss returns to its floor. The cooldown is
        /// what stops the fights being farmed for drops. Set to 0 to disable respawning.</summary>
        public int BossRespawnCooldownDays { get; set; } = 40;

        /// <summary>How much stronger a boss gets each time it has been beaten. At 0.35 a
        /// boss on its third rematch has +105% health and damage, so rematches keep biting.</summary>
        public float BossRematchScale { get; set; } = 0.35f;

        /// <summary>Base gold per mine floor dropped when a boss dies, before difficulty and
        /// rematch scaling. A floor-100 boss drops about this * 100 on a first kill.</summary>
        public float BossDropGoldPerFloor { get; set; } = 300f;

        /// <summary>Give bosses their own sprite sheets instead of the vanilla monster
        /// art. Only the boss instances are affected; ordinary monsters are untouched.</summary>
        public bool CustomBossSprites { get; set; } = true;

        /// <summary>Let marquee bosses use special moves (ki blasts, beams, blinks, healing,
        /// self-destruct) on top of their vanilla melee. Turn off for plain stat-block bosses.</summary>
        public bool EnableBossAbilities { get; set; } = true;

        /// <summary>Scales the damage of every boss special move. Their base damage already
        /// tracks depth and rematch tier; this is a blunt global dial on top.</summary>
        public float BossAbilityDamageScale { get; set; } = 1.0f;

        /// <summary>Percent of max health a regenerating boss heals per second.</summary>
        public float BossRegenPercentPerSecond { get; set; } = 2.0f;

        /// <summary>Let bosses move faster than their base monster type — a per-boss bonus plus
        /// a small climb per rematch tier, so signature bosses feel quick and deep repeat
        /// fights are harder to run from. Turn off to keep vanilla per-type speeds.</summary>
        public bool EnableBossSpeedScaling { get; set; } = true;

        /// <summary>Most extra move speed a boss can gain from rematches, on top of its own
        /// per-boss bonus. Vanilla monster speeds are small integers, so this stays low.</summary>
        public int BossMaxRematchSpeedBonus { get; set; } = 3;

        /// <summary>Require the guardian to be beaten before its form unlocks.
        /// With this off, reaching the mine level is enough (the original behaviour).</summary>
        public bool RequireBossKills { get; set; } = true;

        /// <summary>Total health of a whole level-20 encounter, which is the fight that
        /// set the difficulty benchmark. Everything else is derived from this rather than
        /// multiplying vanilla monster health, whose bases differ by more than 10x and
        /// made the curve wildly inconsistent.</summary>
        public float BaseEncounterHealth { get; set; } = 480f;

        /// <summary>Damage per hit for a level-20 guardian, scaled by depth from there.</summary>
        public float BaseBossDamage { get; set; } = 9f;

        /// <summary>How fast damage climbs with depth: damage is multiplied by
        /// (1 + mineLevel / this). Smaller means a steeper curve and a harsher late game.
        /// At 40 a floor-120 guardian hits for roughly three times a floor-20 one, before
        /// its own weight and your defence are applied.</summary>
        public float BossDamageDepthDivisor { get; set; } = 40f;

        public float BossHealthScale { get; set; } = 1.0f;
        public float BossDamageScale { get; set; } = 1.0f;

        // ---- escalating dragon ball runs ---------------------------------

        /// <summary>Compounding health multiplier per completed wish, applied to every
        /// guardian. 1.55 means the third run has roughly 3.7x the health of the first.</summary>
        public float BossCycleHealthScale { get; set; } = 1.55f;

        /// <summary>Compounding damage multiplier per completed wish.</summary>
        public float BossCycleDamageScale { get; set; } = 1.30f;

        /// <summary>Extra monsters added to every guardian squad per completed wish.</summary>
        public int BossCycleExtraMinions { get; set; } = 1;

        /// <summary>Cap on those extra monsters, so a squad cannot become a mob.</summary>
        public int BossCycleMaxExtraMinions { get; set; } = 4;

        /// <summary>From this run onward, guardians refuse to die the first time: at low
        /// health they surge back to 45%. Set to 0 to disable.</summary>
        public int SecondWindFromCycle { get; set; } = 3;

        // ---- the price of wishing ----------------------------------------

        /// <summary>Wishes past this many demand a trial before the dragon will listen.</summary>
        public int FreeWishes { get; set; } = 3;

        /// <summary>Permanent ki capacity surrendered per wish past the free ones.
        /// Deliberately not a gold cost, since one of the wishes grants gold. Kept small now
        /// that a full ball gather is a ~290-floor run, so a hard-won wish is not half-eaten
        /// by its own toll.</summary>
        public float KiTollPerWish { get; set; } = 6f;

        /// <summary>Capacity can never be tolled below this.</summary>
        public float MinimumBaseKi { get; set; } = 40f;

        /// <summary>The trial begins with your ki emptied, so you start it exhausted and
        /// have to survive long enough to recover before you can fight back.</summary>
        public bool ExhaustBeforeTrial { get; set; } = true;

        /// <summary>Passive regeneration multiplier while a wish trial is running. Recovery
        /// is faster than normal so the opening is a tense scramble rather than a death
        /// sentence: at default settings you are back in the fight in under twenty seconds.</summary>
        public float TrialRegenScale { get; set; } = 2.5f;

        // ---- balance -----------------------------------------------------

        /// <summary>Burn energy while transformed. Turn off for a pure power fantasy.</summary>
        public bool DrainStaminaWhileTransformed { get; set; } = true;

        /// <summary>Scales every form's drain rate.</summary>
        public float StaminaDrainScale { get; set; } = 1.0f;

        /// <summary>Chance for Ultra Instinct to evade an incoming hit entirely.</summary>
        public float UltraInstinctDodgeChance { get; set; } = 0.5f;

        /// <summary>Energy spent each time a dodge actually fires.</summary>
        public int UltraInstinctDodgeEnergyCost { get; set; } = 3;

        public int KamehamehaEnergyCost { get; set; } = 50;

        // ---- destructo disk ----------------------------------------------

        public int DestructoDiskEnergyCost { get; set; } = 30;
        public int DestructoDiskCooldownMs { get; set; } = 1800;
        public int DestructoDiskRangeTiles { get; set; } = 11;
        public float DestructoDiskDamageMultiplier { get; set; } = 1.6f;

        /// <summary>Let the disc clear weeds, twigs and stones it passes over.</summary>
        public bool DestructoDiskCutsDebris { get; set; } = true;

        // ---- solar flare -------------------------------------------------

        public int SolarFlareEnergyCost { get; set; } = 25;
        public int SolarFlareCooldownMs { get; set; } = 9000;
        public int SolarFlareStunMs { get; set; } = 5000;
        public float SolarFlareRadiusTiles { get; set; } = 14f;

        // ---- spirit bomb -------------------------------------------------

        public int SpiritBombEnergyCost { get; set; } = 90;
        public int SpiritBombCooldownMs { get; set; } = 20000;
        public float SpiritBombRadiusTiles { get; set; } = 7f;
        public float SpiritBombDamageMultiplier { get; set; } = 4.5f;

        // ---- instant transmission ----------------------------------------

        public int InstantTransmissionEnergyCost { get; set; } = 40;
        public int InstantTransmissionCooldownMs { get; set; } = 12000;

        // ---- rival invasions ---------------------------------------------

        /// <summary>Rival Saiyans track you down while you are outdoors.</summary>
        public bool EnableRivalInvasions { get; set; } = true;

        /// <summary>Base chance per day of an invasion once you have a form. Every wish
        /// granted adds this much again, so late runs are hunted far more often.</summary>
        public float RivalDailyChance { get; set; } = 0.12f;

        /// <summary>Deepest mine floor you must have reached before rivals start hunting.</summary>
        public int RivalMinimumMineLevel { get; set; } = 40;

        /// <summary>How far above your own progression a rival is allowed to be tuned, in
        /// mine floors. Rivals are pegged to the highest form you have actually unlocked,
        /// not to how deep you have ever been, so clearing Skull Cavern once cannot leave
        /// you ambushed by floor-200 enemies while you are still fighting in Super Saiyan.</summary>
        public int RivalLevelOffset { get; set; } = 10;

        /// <summary>Earliest and latest in-game time an invasion can trigger.</summary>
        public int RivalEarliestTime { get; set; } = 800;
        public int RivalLatestTime { get; set; } = 2200;

        public float RivalHealthScale { get; set; } = 1.0f;
        public float RivalGoldReward { get; set; } = 12000f;

        // ---- multiversal invader -----------------------------------------

        /// <summary>A recurring end-game boss that tears its way in from another reality.
        /// It appears in the deep Skull Cavern and, once you have been that far, out in the
        /// overworld too. It never stays dead — each defeat only makes it come back stronger.</summary>
        public bool EnableMultiversalInvader { get; set; } = true;

        /// <summary>The Invader only shows up on this mine floor and deeper (and only hunts
        /// you in the overworld once you have actually been at least this deep). The God of
        /// Destruction holds floor 300, so the Invader begins at 310.</summary>
        public int InvaderMineFloor { get; set; } = 310;

        /// <summary>Chance the Invader is waiting when you step onto an eligible deep floor.</summary>
        public float InvaderMineChance { get; set; } = 0.1f;

        /// <summary>Base daily chance the Invader hunts you down in the overworld. Every wish
        /// granted adds this much again.</summary>
        public float InvaderOverworldDailyChance { get; set; } = 0.02f;

        /// <summary>Earliest and latest in-game time an overworld Invader attack can trigger.</summary>
        public int InvaderEarliestTime { get; set; } = 700;
        public int InvaderLatestTime { get; set; } = 2400;

        public int InvaderGoldReward { get; set; } = 100000;

        // ---- kaioken -----------------------------------------------------

        public int KaiokenEnergyCost { get; set; } = 45;
        public int KaiokenCooldownMs { get; set; } = 25000;

        /// <summary>Seconds the multiplier lasts once triggered.</summary>
        public float KaiokenSeconds { get; set; } = 20f;

        /// <summary>Added on top of your current form's attack multiplier.</summary>
        public float KaiokenAttackBonus { get; set; } = 2.0f;

        public int KaiokenSpeedBonus { get; set; } = 2;

        /// <summary>Extra ki burned per second while it is running.</summary>
        public float KaiokenKiPerSecond { get; set; } = 6f;

        /// <summary>Health torn out per second while it is running. This is the cost that
        /// makes it a gamble rather than a free upgrade.</summary>
        public float KaiokenHealthPerSecond { get; set; } = 2.5f;

        // ---- dragon balls ------------------------------------------------

        /// <summary>How far each sphere may sit from the centre of the group, in tiles,
        /// for the summoning to trigger.</summary>
        public float DragonBallClusterRadius { get; set; } = 4f;
        public int KamehamehaCooldownMs { get; set; } = 2500;

        /// <summary>Tiles the beam reaches.</summary>
        public int KamehamehaRangeTiles { get; set; } = 10;

        /// <summary>Multiplies Kamehameha damage. Raise or lower to taste.</summary>
        public float KamehamehaDamageScale { get; set; } = 1.0f;

        // ---- visuals -----------------------------------------------------

        public bool ShowAura { get; set; } = true;
        public bool ShowKiBar { get; set; } = true;

        /// <summary>Aggregate power level number above the ki bar.</summary>
        public bool ShowPowerLevel { get; set; } = true;
        public bool ShowLightning { get; set; } = true;

        /// <summary>Once a form is fully mastered it runs calm: its aura, crackle and hum
        /// fall silent unless you are actively charging ki. Turn off to keep every form's
        /// aura blazing regardless of mastery.</summary>
        public bool CalmMasteredAura { get; set; } = true;

        /// <summary>Additive blending makes the aura glow. Disable if it looks blown out
        /// or conflicts with another mod that also draws over the world.</summary>
        public bool AdditiveBlending { get; set; } = true;

        public float AuraOpacity { get; set; } = 0.85f;

        /// <summary>Nudge the aura in screen pixels if it does not sit on your farmer.</summary>
        public int AuraOffsetX { get; set; } = 0;
        public int AuraOffsetY { get; set; } = 0;

        /// <summary>Screen flash when transforming.</summary>
        public bool ScreenFlash { get; set; } = true;

        // ---- technique pose ----------------------------------------------

        /// <summary>Farmer sprite frames held while channelling a technique, one per
        /// facing direction. These default to vanilla's sword-swipe frames, so the pose
        /// is real game art rather than anything drawn for this mod.
        ///
        /// Each swipe is an 8-frame animation starting at the constant, so the usable
        /// ranges are 232-239 down, 240-247 right, 248-255 up, 256-263 left. Earlier
        /// frames are the wind-up, later ones the follow-through - pick whichever reads
        /// best as a thrust. Set a value to -1 to leave that direction unposed.
        ///
        /// Preview any frame live with the console command: saiyan pose &lt;n&gt;</summary>
        public int PoseFrameDown { get; set; } = FarmerSprite.swordswipeDown;
        public int PoseFrameRight { get; set; } = FarmerSprite.swordswipeRight;
        public int PoseFrameUp { get; set; } = FarmerSprite.swordswipeUp;
        public int PoseFrameLeft { get; set; } = FarmerSprite.swordswipeLeft;

        public int PoseFrameForDirection(int facing)
        {
            switch (facing)
            {
                case 0: return this.PoseFrameUp;
                case 1: return this.PoseFrameRight;
                case 2: return this.PoseFrameDown;
                default: return this.PoseFrameLeft;
            }
        }

        // ---- audio -------------------------------------------------------

        /// <summary>Use the mod's own sound effects. Turn off to fall back to vanilla cues.</summary>
        public bool EnableCustomSounds { get; set; } = true;

        /// <summary>Continuous aura roar while transformed.</summary>
        public bool AuraLoopSound { get; set; } = true;
    }
}
