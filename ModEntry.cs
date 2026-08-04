using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Buffs;
using StardewValley.GameData;
using StardewValley.Locations;
using StardewValley.Monsters;

namespace SaiyanTransformations
{
    public sealed class ModEntry : Mod
    {
        /// <summary>Custom hair sheet, referenced from our Data/HairData entries.</summary>
        public const string HairAssetName = "Characters/Farmer/saiyanhair";

        /// <summary>Prefix for our boss sprite sheets, loaded on demand.</summary>
        public const string MonsterAssetPrefix = "Mods/khaleelkhan.SaiyanTransformations/monsters/";

        public static string MonsterAssetName(string sheet) => MonsterAssetPrefix + sheet;

        private const string BuffIdPrefix = "khaleelkhan.SaiyanTransformations.";
        private const string CuePrefix = "khaleelkhan.SaiyanTransformations_";
        private const int BurstLength = 22;
        private const int DodgeFlashLength = 30;

        /// <summary>Every wav in assets/sounds. Names ending in "_loop" are registered
        /// as looping cues and driven with ICue.Play/Stop.</summary>
        private static readonly string[] CueNames =
        {
            "transform_ssj", "transform_ssj2", "transform_ssj3", "transform_god",
            "transform_blue", "transform_ui", "aura_loop", "kame_charge", "kame_fire",
            "kame_beam_loop", "kame_impact", "powerdown", "unlock",
            "boss_roar", "boss_defeat", "dodge"
        };

        internal ModConfig Config;
        internal Texture2D AuraTexture;
        internal Texture2D LightningTexture;
        internal Texture2D KameTexture;
        internal Texture2D IconTexture;
        internal Texture2D DiskTexture;
        internal Texture2D TechniqueIconTexture;
        internal Texture2D KiBarTexture;
        internal int AnimTicks;

        private FxRenderer Fx;
        private TechniqueManager Techniques;

        private int formIndex = -1;
        private int savedHair = -1;
        private Color savedHairColor = Color.White;
        private int burstTicks;
        private int announcedUnlocks = -1;
        private ICue auraCue;
        private BossManager Bosses;
        internal DragonBallManager DragonBalls;
        internal KiManager Ki;
        internal ProgressManager Progress;
        private RivalManager Rivals;
        private InvaderManager Invader;

        // boss-floor gate: remember the floor we are on and whether its boss still lives,
        // so a descent past a living boss can be bounced back
        private int lastMineLevel = -1;
        private bool lastFloorSealed;
        private int pendingGateBounce = -1;
        private int lastHealth = -1;
        private int dodgeTicks;
        private int untouchedTicks;
        private bool senzuPending;
        private int kaiokenTicks;
        private float kaiokenHealthCarry;
        private int dashCooldownTicks;
        private int dashFlashTicks;
        private bool blocking;
        private int blockFlashTicks;
        private int parryWindowTicks;

        internal Transformation CurrentForm =>
            this.formIndex >= 0 && this.formIndex < Transformation.All.Length
                ? Transformation.All[this.formIndex]
                : null;

        public override void Entry(IModHelper helper)
        {
            this.Config = helper.ReadConfig<ModConfig>();
            this.Fx = new FxRenderer(this);
            this.Techniques = new TechniqueManager(this, this.Fx);
            this.Bosses = new BossManager(this, this.Fx);
            this.DragonBalls = new DragonBallManager(this);
            this.Ki = new KiManager(this);
            this.Progress = new ProgressManager(this);
            this.Rivals = new RivalManager(this);
            this.Invader = new InvaderManager(this);

            // let the config drive Ultra Instinct's evasion rate
            Transformation.All[5].DodgeChance = this.Config.UltraInstinctDodgeChance;
            Transformation.All[6].RequiredWishes = this.Config.MasteredUltraInstinctWishes;

            helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
            helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
            helper.Events.GameLoop.Saving += this.OnSaving;
            helper.Events.GameLoop.ReturnedToTitle += this.OnReturnedToTitle;
            helper.Events.Content.AssetRequested += this.OnAssetRequested;
            helper.Events.Input.ButtonsChanged += this.OnButtonsChanged;
            helper.Events.Player.Warped += this.OnWarped;
            helper.Events.Display.RenderedWorld += this.OnRenderedWorld;
            helper.Events.Display.RenderedHud += this.OnRenderedHud;
            helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
            helper.Events.World.ObjectListChanged += this.OnObjectListChanged;
            helper.Events.GameLoop.DayStarted += this.OnDayStarted;
            helper.Events.GameLoop.TimeChanged += this.OnTimeChanged;

            helper.ConsoleCommands.Add(
                "saiyan",
                "Saiyan Transformations.\n\n"
                + "Usage: saiyan status | unlock_all | form <1-6> | off | bosses | clearboss | pose <n>",
                this.OnCommand);
        }

        // ------------------------------------------------------------ setup

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            this.AuraTexture = this.Helper.ModContent.Load<Texture2D>("assets/aura.png");
            this.LightningTexture = this.Helper.ModContent.Load<Texture2D>("assets/lightning.png");
            this.KameTexture = this.Helper.ModContent.Load<Texture2D>("assets/kamehameha.png");
            this.IconTexture = this.Helper.ModContent.Load<Texture2D>("assets/icons.png");
            this.DiskTexture = this.Helper.ModContent.Load<Texture2D>("assets/disk.png");
            this.TechniqueIconTexture =
                this.Helper.ModContent.Load<Texture2D>("assets/technique_icons.png");
            this.KiBarTexture = this.Helper.ModContent.Load<Texture2D>("assets/kibar.png");

            this.SetupConfigMenu();
        }

        /// <summary>Register an in-game options page with Generic Mod Config Menu, if present.
        /// GMCM is optional; without it the mod is configured through config.json as before.</summary>
        private void SetupConfigMenu()
        {
            var gmcm = this.Helper.ModRegistry
                .GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (gmcm == null)
                return;

            IManifest m = this.ModManifest;
            gmcm.Register(m, () => this.Config = new ModConfig(),
                          () => this.Helper.WriteConfig(this.Config));

            gmcm.AddSectionTitle(m, () => "Controls");
            gmcm.AddKeybindList(m, () => this.Config.TransformKey, v => this.Config.TransformKey = v, () => "Transform / ascend");
            gmcm.AddKeybindList(m, () => this.Config.PowerDownKey, v => this.Config.PowerDownKey = v, () => "Power down");
            gmcm.AddKeybindList(m, () => this.Config.KamehamehaKey, v => this.Config.KamehamehaKey = v, () => "Fire technique");
            gmcm.AddKeybindList(m, () => this.Config.SwitchTechniqueKey, v => this.Config.SwitchTechniqueKey = v, () => "Switch technique");
            gmcm.AddKeybindList(m, () => this.Config.DashKey, v => this.Config.DashKey = v, () => "Dash");
            gmcm.AddKeybindList(m, () => this.Config.BlockKey, v => this.Config.BlockKey = v, () => "Block (hold)");

            gmcm.AddSectionTitle(m, () => "Dash & Block");
            gmcm.AddBoolOption(m, () => this.Config.EnableDash, v => this.Config.EnableDash = v, () => "Enable dash");
            gmcm.AddNumberOption(m, () => this.Config.DashKiCost, v => this.Config.DashKiCost = v, () => "Dash ki cost", null, 0f, 100f, 1f);
            gmcm.AddNumberOption(m, () => this.Config.DashTiles, v => this.Config.DashTiles = v, () => "Dash distance (tiles)", null, 1, 8, 1);
            gmcm.AddNumberOption(m, () => this.Config.DashCooldownMs, v => this.Config.DashCooldownMs = v, () => "Dash cooldown (ms)", null, 0, 5000, 100);
            gmcm.AddBoolOption(m, () => this.Config.EnableBlock, v => this.Config.EnableBlock = v, () => "Enable block");
            gmcm.AddNumberOption(m, () => this.Config.BlockDamageReduction, v => this.Config.BlockDamageReduction = v, () => "Block damage reduction", null, 0f, 0.95f, 0.05f);
            gmcm.AddNumberOption(m, () => this.Config.BlockKiPerSecond, v => this.Config.BlockKiPerSecond = v, () => "Block ki / second", null, 0f, 50f, 1f);
            gmcm.AddBoolOption(m, () => this.Config.EnableParry, v => this.Config.EnableParry = v, () => "Enable parry (perfect block)");
            gmcm.AddNumberOption(m, () => this.Config.ParryWindowMs, v => this.Config.ParryWindowMs = v, () => "Parry window (ms)", null, 60, 600, 10);
            gmcm.AddBoolOption(m, () => this.Config.ParryReflect, v => this.Config.ParryReflect = v, () => "Parry deflects ki blasts");

            gmcm.AddSectionTitle(m, () => "Mastery & passives");
            gmcm.AddBoolOption(m, () => this.Config.EnableMasteryBonuses, v => this.Config.EnableMasteryBonuses = v, () => "Mastery carries over to all forms");
            gmcm.AddNumberOption(m, () => this.Config.MasteryAttackBonusPerForm, v => this.Config.MasteryAttackBonusPerForm = v, () => "Attack bonus per mastered form", null, 0f, 0.5f, 0.05f);
            gmcm.AddNumberOption(m, () => this.Config.MasteryDefenseBonusPerForm, v => this.Config.MasteryDefenseBonusPerForm = v, () => "Defense bonus per mastered form", null, 0, 20, 1);
            gmcm.AddNumberOption(m, () => this.Config.MasteryKiBonusPerForm, v => this.Config.MasteryKiBonusPerForm = v, () => "Max ki per mastered form", null, 0f, 60f, 5f);

            gmcm.AddSectionTitle(m, () => "Bosses");
            gmcm.AddBoolOption(m, () => this.Config.EnableBosses, v => this.Config.EnableBosses = v, () => "Enable bosses");
            gmcm.AddBoolOption(m, () => this.Config.GateBossFloors, v => this.Config.GateBossFloors = v, () => "Seal floors until boss falls");
            gmcm.AddBoolOption(m, () => this.Config.EnableBossAbilities, v => this.Config.EnableBossAbilities = v, () => "Boss special moves");
            gmcm.AddBoolOption(m, () => this.Config.EnableBossPhases, v => this.Config.EnableBossPhases = v, () => "Boss phases");
            gmcm.AddBoolOption(m, () => this.Config.EnableBossSpeedScaling, v => this.Config.EnableBossSpeedScaling = v, () => "Boss speed scaling");
            gmcm.AddNumberOption(m, () => this.Config.BossRespawnCooldownDays, v => this.Config.BossRespawnCooldownDays = v, () => "Boss respawn cooldown (days)", null, 0, 200, 5);
            gmcm.AddNumberOption(m, () => this.Config.BossAbilityDamageScale, v => this.Config.BossAbilityDamageScale = v, () => "Boss ability damage x", null, 0f, 5f, 0.1f);

            gmcm.AddSectionTitle(m, () => "Difficulty");
            gmcm.AddTextOption(m, () => this.Config.DifficultyPreset,
                v => { if (v != this.Config.DifficultyPreset) { this.Config.DifficultyPreset = v; this.ApplyDifficultyPreset(v); } },
                () => "Preset (sets the dials below)", null,
                new[] { "Story", "Normal", "Hard", "Brutal", "Custom" });
            gmcm.AddParagraph(m, () => "Pick a preset to set boss health/damage in one click, then reopen this page to see the values. Choose Custom to tune them yourself.");
            gmcm.AddNumberOption(m, () => this.Config.BossHealthScale, v => this.Config.BossHealthScale = v, () => "Boss health x", null, 0.25f, 5f, 0.25f);
            gmcm.AddNumberOption(m, () => this.Config.BossDamageScale, v => this.Config.BossDamageScale = v, () => "Boss damage x", null, 0.25f, 5f, 0.25f);
            gmcm.AddBoolOption(m, () => this.Config.DrainStaminaWhileTransformed, v => this.Config.DrainStaminaWhileTransformed = v, () => "Ki drains while transformed");
            gmcm.AddNumberOption(m, () => this.Config.StaminaDrainScale, v => this.Config.StaminaDrainScale = v, () => "Ki drain x", null, 0f, 5f, 0.25f);

            gmcm.AddSectionTitle(m, () => "Invasions");
            gmcm.AddBoolOption(m, () => this.Config.EnableRivalInvasions, v => this.Config.EnableRivalInvasions = v, () => "Rival invasions");
            gmcm.AddBoolOption(m, () => this.Config.EnableMultiversalInvader, v => this.Config.EnableMultiversalInvader = v, () => "Multiversal Invader");

            gmcm.AddSectionTitle(m, () => "Visuals");
            gmcm.AddBoolOption(m, () => this.Config.ShowAura, v => this.Config.ShowAura = v, () => "Show aura");
            gmcm.AddBoolOption(m, () => this.Config.ShowLightning, v => this.Config.ShowLightning = v, () => "Show lightning");
            gmcm.AddBoolOption(m, () => this.Config.CalmMasteredAura, v => this.Config.CalmMasteredAura = v, () => "Calm mastered aura");
            gmcm.AddBoolOption(m, () => this.Config.ShowKiBar, v => this.Config.ShowKiBar = v, () => "Show ki bar");
            gmcm.AddBoolOption(m, () => this.Config.ShowPowerLevel, v => this.Config.ShowPowerLevel = v, () => "Show power level");
            gmcm.AddBoolOption(m, () => this.Config.ScreenFlash, v => this.Config.ScreenFlash = v, () => "Screen flash");
        }

        /// <summary>Apply a one-click difficulty preset by overwriting the boss scaling dials.
        /// Bosses are already tuned upward in code for the mastery-carryover power gains, so
        /// "Normal" leaves the scales at 1.0; the others scale from that baseline. "Custom"
        /// changes nothing, leaving whatever the player has set by hand.</summary>
        private void ApplyDifficultyPreset(string preset)
        {
            switch ((preset ?? "").Trim().ToLowerInvariant())
            {
                case "story":
                    this.Config.BossHealthScale = 0.6f;
                    this.Config.BossDamageScale = 0.5f;
                    this.Config.BossAbilityDamageScale = 0.6f;
                    this.Config.StaminaDrainScale = 0.5f;
                    break;
                case "normal":
                    this.Config.BossHealthScale = 1.0f;
                    this.Config.BossDamageScale = 1.0f;
                    this.Config.BossAbilityDamageScale = 1.0f;
                    this.Config.StaminaDrainScale = 1.0f;
                    break;
                case "hard":
                    this.Config.BossHealthScale = 1.6f;
                    this.Config.BossDamageScale = 1.4f;
                    this.Config.BossAbilityDamageScale = 1.4f;
                    this.Config.StaminaDrainScale = 1.15f;
                    break;
                case "brutal":
                    this.Config.BossHealthScale = 2.4f;
                    this.Config.BossDamageScale = 1.9f;
                    this.Config.BossAbilityDamageScale = 1.8f;
                    this.Config.StaminaDrainScale = 1.3f;
                    break;
                // "custom": leave every dial exactly as the player set it
            }

            this.Helper.WriteConfig(this.Config);
        }

        private void OnAssetRequested(object sender, AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo(HairAssetName))
                e.LoadFromModFile<Texture2D>("assets/saiyanhair.png", AssetLoadPriority.Medium);
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/HairData"))
                e.Edit(this.EditHairData, AssetEditPriority.Late);
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/AudioChanges"))
                e.Edit(this.EditAudioChanges, AssetEditPriority.Late);
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/Objects"))
                e.Edit(this.DragonBalls.EditObjectData, AssetEditPriority.Late);
            else if (e.NameWithoutLocale.IsEquivalentTo(DragonBallManager.TextureAsset))
                e.LoadFromModFile<Texture2D>("assets/dragonballs.png", AssetLoadPriority.Medium);
            else if (e.NameWithoutLocale.IsEquivalentTo(DragonBallManager.SenzuTextureAsset))
                e.LoadFromModFile<Texture2D>("assets/senzu.png", AssetLoadPriority.Medium);
            else if (e.NameWithoutLocale.StartsWith(MonsterAssetPrefix))
            {
                string sheet = e.NameWithoutLocale.Name.Substring(MonsterAssetPrefix.Length);
                e.LoadFromModFile<Texture2D>($"assets/monsters/{sheet}.png",
                                             AssetLoadPriority.Medium);
            }
        }

        /// <summary>Register the transformation hairstyles without replacing any vanilla ones.
        /// Format is "texture/tileX/tileY/uniqueLeftSprite/coveredIndex/isBald", where the
        /// texture name is relative to Characters/Farmer and the tiles are 16px units.</summary>
        private void EditHairData(IAssetData asset)
        {
            string Entry(Transformation form) => $"saiyanhair/{form.SpriteIndex}/0/false/-1/false";

            // the key type changed between game versions, so handle both
            if (asset.Data is IDictionary<int, string> byInt)
            {
                foreach (Transformation form in Transformation.All)
                    byInt[form.HairId] = Entry(form);
            }
            else if (asset.Data is IDictionary<string, string> byString)
            {
                foreach (Transformation form in Transformation.All)
                    byString[form.HairId.ToString()] = Entry(form);
            }
            else
            {
                this.Monitor.Log(
                    $"Data/HairData was {asset.Data?.GetType().FullName ?? "null"}, which this mod "
                    + "does not know how to edit. Transformations will still work, but hair will not change.",
                    LogLevel.Warn);
            }
        }

        /// <summary>Register our wavs as game audio cues, so Game1.playSound can reach them.</summary>
        private void EditAudioChanges(IAssetData asset)
        {
            IDictionary<string, AudioCueData> data = asset.AsDictionary<string, AudioCueData>().Data;
            string dir = Path.Combine(this.Helper.DirectoryPath, "assets", "sounds");

            foreach (string name in CueNames)
            {
                string path = Path.Combine(dir, name + ".wav");
                if (!File.Exists(path))
                {
                    this.Monitor.Log($"Missing sound file {path}; that cue will be silent.", LogLevel.Warn);
                    continue;
                }

                string id = CueId(name);
                data[id] = new AudioCueData
                {
                    Id = id,
                    Category = "Sound",
                    FilePaths = new List<string> { path },
                    Looped = name.EndsWith("_loop"),
                    StreamedVorbis = false,
                    UseReverb = false
                };
            }
        }

        internal static string CueId(string name) => CuePrefix + name;

        /// <summary>Play one of our cues, falling back to a vanilla one if custom audio is off.</summary>
        internal void PlayCue(string name, string fallback = null)
        {
            if (this.Config.EnableCustomSounds)
                ModEntry.SafeSound(CueId(name));
            else if (fallback != null)
                ModEntry.SafeSound(fallback);
        }

        /// <summary>Start a looping cue. Returns null if audio is off or the cue is missing.</summary>
        internal ICue PlayLoop(string name)
        {
            if (!this.Config.EnableCustomSounds)
                return null;
            try
            {
                ICue cue = Game1.soundBank.GetCue(CueId(name));
                cue.Play();
                return cue;
            }
            catch (Exception ex)
            {
                this.Monitor.Log($"Could not start looping cue '{name}': {ex.Message}", LogLevel.Debug);
                return null;
            }
        }

        internal static void StopLoop(ref ICue cue)
        {
            if (cue == null)
                return;
            try
            {
                cue.Stop(AudioStopOptions.Immediate);
            }
            catch (Exception)
            {
                // already gone
            }
            cue = null;
        }

        private void StartAuraLoop()
        {
            ModEntry.StopLoop(ref this.auraCue);
            if (this.Config.AuraLoopSound)
                this.auraCue = this.PlayLoop("aura_loop");
        }

        /// <summary>A fully mastered form runs "calm" — no visible aura, no electric
        /// crackle and no hum — unless the player is actively charging ki, at which point
        /// the aura roars back to life. Only the form you have mastered goes calm; an
        /// unmastered form still blazes normally.</summary>
        internal bool FormIsCalm(Transformation form)
        {
            return form != null
                   && this.Config.CalmMasteredAura
                   && this.Progress.IsMastered(form)
                   && !this.Ki.IsCharging;
        }

        /// <summary>Start or silence the aura hum to match the calm state each tick.</summary>
        private void UpdateAuraSound()
        {
            Transformation form = this.CurrentForm;
            if (form == null)
                return;

            bool wantHum = this.Config.AuraLoopSound && !this.FormIsCalm(form);
            if (wantHum && this.auraCue == null)
                this.StartAuraLoop();
            else if (!wantHum && this.auraCue != null)
                ModEntry.StopLoop(ref this.auraCue);
        }

        // ------------------------------------------------------------ unlocks

        internal int UnlockLevelFor(int index)
        {
            switch (index)
            {
                case 0: return this.Config.SuperSaiyanMineLevel;
                case 1: return this.Config.SuperSaiyan2MineLevel;
                case 2: return this.Config.SuperSaiyan3MineLevel;
                case 3: return this.Config.SuperSaiyanGodMineLevel;
                case 4: return this.Config.SuperSaiyanBlueMineLevel;
                case 5: return this.Config.UltraInstinctMineLevel;
                case 6: return this.Config.MasteredUltraInstinctMineLevel;
                default: return Transformation.All[index].DefaultUnlockLevel;
            }
        }

        internal int DeepestMineLevel()
        {
            if (!Context.IsWorldReady || Game1.player == null)
                return 0;

            int deepest = Game1.player.deepestMineLevel;
            if (Game1.currentLocation is MineShaft shaft)
            {
                int here = shaft.mineLevel;
                if (here > deepest)
                    deepest = here;
            }
            return deepest;
        }

        /// <summary>How many forms are available, in order.</summary>
        internal int UnlockedCount()
        {
            if (this.Config.UnlockEverything)
                return Transformation.All.Length;

            int deepest = this.DeepestMineLevel();
            int count = 0;
            for (int i = 0; i < Transformation.All.Length; i++)
            {
                if (deepest < this.UnlockLevelFor(i))
                    break;
                if (this.Config.EnableBosses && this.Config.RequireBossKills
                    && !this.Bosses.IsFormUnlockedByBoss(i))
                {
                    break;
                }
                if (Transformation.All[i].RequiredWishes > 0
                    && this.DragonBalls.State.WishesGranted < Transformation.All[i].RequiredWishes)
                {
                    break;
                }
                count = i + 1;
            }
            return Math.Max(count, this.Bosses.GrantedFormUnlocks);
        }

        internal Vector2 FxPlayerAnchor() => this.Fx.PlayerAnchor();

        internal bool KaiokenActive => this.kaiokenTicks > 0;

        internal void BeginKaioken()
        {
            this.kaiokenTicks = (int)(Math.Max(1f, this.Config.KaiokenSeconds) * 60f);
            this.kaiokenHealthCarry = 0f;
            ModEntry.Notify("KAIOKEN!");
        }

        /// <summary>Burns ki and health for as long as it holds, and ends itself if either
        /// runs out. Health is drained through a carry so the per-tick cost stays fractional.</summary>
        private void UpdateKaioken()
        {
            if (this.kaiokenTicks <= 0)
                return;

            this.kaiokenTicks--;

            if (this.CurrentForm == null || this.Ki.IsExhausted)
            {
                this.EndKaioken("Kaioken collapses.");
                return;
            }

            this.Ki.Drain(Math.Max(0f, this.Config.KaiokenKiPerSecond) / 60f);

            this.kaiokenHealthCarry += Math.Max(0f, this.Config.KaiokenHealthPerSecond) / 60f;
            if (this.kaiokenHealthCarry >= 1f)
            {
                int whole = (int)this.kaiokenHealthCarry;
                this.kaiokenHealthCarry -= whole;
                Game1.player.health = Math.Max(1, Game1.player.health - whole);
            }

            if (Game1.player.health <= 1)
                this.EndKaioken("Your body gives out.");
            else if (this.kaiokenTicks <= 0)
                this.EndKaioken("Kaioken fades.");
        }

        private void EndKaioken(string message)
        {
            this.kaiokenTicks = 0;
            this.kaiokenHealthCarry = 0f;
            this.PlayCue("powerdown", "stoneCrack");
            ModEntry.Notify(message);
        }

        /// <summary>The mine floor a rival should be tuned to: the unlock floor of your
        /// strongest form plus a small margin, never more than how deep you have actually
        /// been. This keeps ambushes at or just above your own level instead of scaling off
        /// a single deep Skull Cavern run.</summary>
        internal int RivalTuningLevel(int deepest)
        {
            int unlocked = this.UnlockedCount();
            int formLevel = unlocked > 0
                ? this.UnlockLevelFor(Math.Min(unlocked - 1, Transformation.All.Length - 1))
                : 20;

            int ceiling = formLevel + Math.Max(0, this.Config.RivalLevelOffset);
            int level = Math.Min(Math.Max(20, deepest), ceiling);
            return Math.Max(20, level);
        }

        internal bool BossFightInProgress => this.Bosses != null && this.Bosses.Active != null;

        internal bool RivalActive => this.Rivals != null && this.Rivals.Active;

        internal bool TechniquesActive => this.Techniques != null && this.Techniques.AnyActive;

        /// <summary>Replace a buff with one that lapses immediately.</summary>
        internal void RemoveBuffById(string id)
        {
            Game1.player.applyBuff(new Buff(id: id, duration: 1, effects: new BuffEffects()));
        }

        internal bool IsTechniqueUnlocked(string techniqueId)
        {
            return this.Bosses.IsTechniqueUnlocked(techniqueId);
        }

        internal string TechniqueName(string techniqueId)
        {
            return this.Techniques.NameOf(techniqueId);
        }

        internal void GrantFormUnlock(int formIndex)
        {
            this.Bosses.GrantFormUnlock(formIndex);
        }

        /// <summary>Called after a wish resolves: the spheres are gone, so their
        /// guardians come back.</summary>
        internal int SpawnWishTrial(GameLocation location, Vector2 centre, int cycle)
        {
            return this.Bosses.SpawnWishTrial(location, centre, cycle);
        }

        /// <summary>Trial guardians can appear anywhere, so their aura is drawn from here
        /// rather than from the mine-only boss pass.</summary>
        private void DrawTrialAuras(SpriteBatch b)
        {
            GameLocation location = Game1.currentLocation;
            if (location == null)
                return;

            foreach (NPC npc in location.characters)
            {
                if (npc is Monster monster
                    && monster.modData.ContainsKey(DragonBallManager.TrialKey))
                {
                    this.Fx.DrawAuraAt(b, this.Fx.MonsterAnchor(monster),
                                       new Color(120, 240, 160), 4.2f, 0.6f,
                                       monster.GetHashCode());
                }
            }
        }

        internal void OnWishGranted()
        {
            this.Bosses.ResetRepeatableBosses();
        }

        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            this.Rivals.OnDayStarted();
            this.Invader.OnDayStarted();
        }

        private void OnTimeChanged(object sender, TimeChangedEventArgs e)
        {
            this.Rivals.OnTimeChanged(e.NewTime);
            this.Invader.OnTimeChanged(e.NewTime);
        }

        internal int SpawnRival(GameLocation location, Vector2 centre, int wishes, int depth)
        {
            return this.Bosses.SpawnRival(location, centre, wishes, depth);
        }

        internal int SpawnInvader(GameLocation location, Vector2 centre, int defeats)
        {
            return this.Bosses.SpawnInvader(location, centre, defeats);
        }

        internal int InvaderDefeats => this.Bosses != null ? this.Bosses.InvaderDefeats : 0;

        internal void RecordInvaderDefeat() => this.Bosses?.RecordInvaderDefeat();

        private void OnObjectListChanged(object sender, ObjectListChangedEventArgs e)
        {
            // only rescan when a sphere was actually placed, not on every machine tick
            foreach (KeyValuePair<Vector2, StardewValley.Object> pair in e.Added)
            {
                string id = pair.Value?.ItemId;
                if (!string.IsNullOrEmpty(id) && id.StartsWith(DragonBallManager.ItemPrefix))
                {
                    this.DragonBalls.CheckForSummon(e.Location);
                    return;
                }
            }
        }

        // ------------------------------------------------------------ forms

        private static string BuffId(Transformation form) => BuffIdPrefix + form.Id;

        private void ApplyBuff(Transformation form)
        {
            BuffEffects effects = new BuffEffects();
            effects.Attack.Value = form.FlatAttack;
            effects.AttackMultiplier.Value =
                form.AttackMultiplier - 1f
                + this.DragonBalls.State.BonusAttackMultiplier
                + this.Progress.State.ZenkaiAttackBonus
                + this.Progress.MasteryGlobalAttackBonus()
                + (this.KaiokenActive ? this.Config.KaiokenAttackBonus : 0f);
            effects.Defense.Value = form.Defense + this.Progress.MasteryGlobalDefenseBonus();
            effects.Speed.Value = form.SpeedBonus
                                  + (this.KaiokenActive ? this.Config.KaiokenSpeedBonus : 0);
            effects.MagneticRadius.Value = 128;
            effects.CriticalChanceMultiplier.Value = form.CritChanceBonus;
            effects.CriticalPowerMultiplier.Value = form.CritPowerBonus;
            effects.WeaponSpeedMultiplier.Value = form.WeaponSpeedBonus;

            // show the form's mastery level (and the ki-drain discount it earns) in the
            // buff tooltip; refreshed every tick-batch, so it climbs live while held
            int masteryPct = (int)(this.Progress.MasteryFraction(form) * 100f);
            int drainCut = (int)((1f - this.Progress.DrainMultiplier(form)) * 100f);
            string masteryLine = masteryPct >= 100
                ? "Mastery: MAX (ki drain -" + drainCut + "%)"
                : $"Mastery: {masteryPct}% (ki drain -{drainCut}%)";

            // the stacking bonus every mastered form grants in this (and every) form
            int carryAtk = (int)Math.Round(this.Progress.MasteryGlobalAttackBonus() * 100f);
            string carryLine = carryAtk > 0
                ? $"Mastery bonus (all forms): +{carryAtk}% attack, +{this.Progress.MasteryGlobalDefenseBonus()} def"
                : null;

            string description = form.Description
                                 + (string.IsNullOrEmpty(form.Passive) ? "" : "\nPassive: " + form.Passive)
                                 + "\n" + masteryLine
                                 + (carryLine != null ? "\n" + carryLine : "");

            Game1.player.applyBuff(new Buff(
                id: BuffId(form),
                displayName: form.DisplayName,
                description: description,
                iconTexture: this.IconTexture,
                iconSheetIndex: form.SpriteIndex,
                duration: 3000,
                effects: effects));
        }

        /// <summary>Replace the buff with one that lapses immediately.</summary>
        private void RemoveBuff(Transformation form)
        {
            Game1.player.applyBuff(new Buff(id: BuffId(form), duration: 1, effects: new BuffEffects()));
        }

        internal void SetForm(int index)
        {
            if (index < 0 || index >= Transformation.All.Length || Game1.player == null)
                return;

            if (this.Ki.IsExhausted)
            {
                ModEntry.Notify("You have no ki left to transform.");
                return;
            }

            Transformation form = Transformation.All[index];
            Farmer player = Game1.player;

            if (this.formIndex < 0)
            {
                this.savedHair = player.hair.Value;
                this.savedHairColor = player.hairstyleColor.Value;
            }
            else
            {
                this.RemoveBuff(Transformation.All[this.formIndex]);
            }

            this.formIndex = index;
            player.hairstyleColor.Value = Color.White;   // the sprites carry their own colours
            player.changeHairStyle(form.HairId);
            this.ApplyBuff(form);

            this.burstTicks = 1;
            this.PlayCue(form.SoundName, "yoba");
            if (!this.FormIsCalm(form))
                this.StartAuraLoop();
            if (this.Config.ScreenFlash)
                Game1.flashAlpha = 1f;
            ModEntry.Notify(form.DisplayName + "!");
        }

        internal void PowerDown(bool announce)
        {
            if (this.formIndex < 0)
                return;

            this.RemoveBuff(Transformation.All[this.formIndex]);
            this.formIndex = -1;
            this.Techniques.Cancel();
            if (this.kaiokenTicks > 0)
                this.EndKaioken("Kaioken fades.");
            ModEntry.StopLoop(ref this.auraCue);
            this.PlayCue("powerdown", "stoneCrack");

            Farmer player = Game1.player;
            if (player != null && this.savedHair >= 0)
            {
                player.hairstyleColor.Value = this.savedHairColor;
                player.changeHairStyle(this.savedHair);
                this.savedHair = -1;
            }

            if (player != null && player.Stamina > player.MaxStamina)
                player.Stamina = player.MaxStamina;

            if (announce)
                ModEntry.Notify("Powered down.");
        }

        private void Ascend()
        {
            int unlocked = this.UnlockedCount();
            if (unlocked <= 0)
            {
                ModEntry.Notify($"Nothing stirs yet. Reach mine level {this.UnlockLevelFor(0)}.");
                return;
            }

            int next = this.formIndex + 1;
            if (next >= unlocked)
                this.PowerDown(true);
            else
                this.SetForm(next);
        }

        // ------------------------------------------------------------ events

        private void OnButtonsChanged(object sender, ButtonsChangedEventArgs e)
        {
            if (!Context.IsPlayerFree)
                return;

            if (this.Config.DashKey.JustPressed())
            {
                this.TryDash();
                this.Helper.Input.SuppressActiveKeybinds(this.Config.DashKey);
            }
            else if (this.Config.SwitchTechniqueKey.JustPressed())
            {
                this.Techniques.Cycle();
                this.Helper.Input.SuppressActiveKeybinds(this.Config.SwitchTechniqueKey);
            }
            else if (this.Config.KamehamehaKey.JustPressed())
            {
                this.Techniques.TryFire(this.CurrentForm, this.formIndex);
                this.Helper.Input.SuppressActiveKeybinds(this.Config.KamehamehaKey);
            }
            else if (this.Config.PowerDownKey.JustPressed())
            {
                this.PowerDown(true);
                this.Helper.Input.SuppressActiveKeybinds(this.Config.PowerDownKey);
            }
            else if (this.Config.TransformKey.JustPressed())
            {
                this.Ascend();
                this.Helper.Input.SuppressActiveKeybinds(this.Config.TransformKey);
            }
        }

        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            this.AnimTicks++;

            if (this.burstTicks > 0)
            {
                this.burstTicks++;
                if (this.burstTicks >= BurstLength)
                    this.burstTicks = 0;
            }

            if (this.dodgeTicks > 0)
            {
                this.dodgeTicks++;
                if (this.dodgeTicks >= DodgeFlashLength)
                    this.dodgeTicks = 0;
            }

            if (this.dashCooldownTicks > 0)
                this.dashCooldownTicks--;
            if (this.dashFlashTicks > 0 && ++this.dashFlashTicks > 10)
                this.dashFlashTicks = 0;
            if (this.blockFlashTicks > 0 && ++this.blockFlashTicks > 8)
                this.blockFlashTicks = 0;

            if (!Context.IsWorldReady)
                return;

            // carry out a pending boss-gate bounce, then skip the rest of this tick
            if (this.pendingGateBounce >= 0 && Context.IsPlayerFree)
            {
                int level = this.pendingGateBounce;
                this.pendingGateBounce = -1;
                ModEntry.Notify("There is no escape until the boss falls.");
                this.PlayCue("powerdown", "stoneCrack");
                Game1.enterMine(level);
                return;
            }

            this.Techniques.Update();
            this.Bosses.Update();
            this.DragonBalls.Update();
            this.Ki.Update();
            this.Progress.Update();
            this.Rivals.Update();
            this.Invader.Update();
            this.CheckSenzu();
            this.UpdateKaioken();
            this.CheckNewUnlocks();
            this.UpdateBlock();
            this.CheckDodge();
            this.UpdateAuraSound();

            // remember the floor we are on and whether its boss still seals the way down
            if (Game1.currentLocation is MineShaft here)
            {
                this.lastMineLevel = here.mineLevel;
                this.lastFloorSealed = this.Bosses.CurrentFloorSealed;
            }
            else
            {
                this.lastFloorSealed = false;
            }

            Transformation form = this.CurrentForm;
            if (form == null)
                return;

            if (e.IsMultipleOf(30))
                this.ApplyBuff(form);

            if (this.Config.DrainStaminaWhileTransformed && Context.IsPlayerFree
                && !this.DragonBalls.State.FreeTransformations)
            {
                this.Ki.Drain(form.StaminaDrainPerSecond * this.Config.StaminaDrainScale
                              * this.Progress.DrainMultiplier(form) / 60f);
            }
        }

        /// <summary>Ultra Instinct evasion. Implemented by refunding the damage rather than
        /// by Harmony-patching Farmer.takeDamage: a dozen installed mods already patch around
        /// that method, and a cancelling prefix there would skip theirs.</summary>
        private void CheckDodge()
        {
            Farmer player = Game1.player;
            if (player == null)
                return;

            int health = player.health;
            if (this.lastHealth < 0)
            {
                this.lastHealth = health;
                return;
            }

            int lost = this.lastHealth - health;
            Transformation form = this.CurrentForm;

            if (lost > 0)
            {
                this.Ki.InterruptCharging();
                this.untouchedTicks = 0;
            }
            else
            {
                this.untouchedTicks++;
            }

            // a perfectly-timed guard negates the whole hit, costs no ki, and grants a beat
            // of invincibility - plus it deflects the boss's ki blasts and counter-bursts
            if (lost > 0 && this.Config.EnableParry && this.parryWindowTicks > 0)
            {
                player.health = Math.Min(player.maxHealth, health + lost);
                health = player.health;
                lost = 0;
                this.parryWindowTicks = 0;
                this.blockFlashTicks = 1;
                player.temporarilyInvincible = true;
                player.temporaryInvincibilityTimer = 0;
                player.currentTemporaryInvincibilityDuration = 600;
                this.PlayCue("dodge", "crystal");
                if (this.Config.ScreenFlash)
                    Game1.flashAlpha = 0.25f;
                ModEntry.Notify("Parry!");
                if (this.Config.ParryReflect)
                {
                    Rectangle box = player.GetBoundingBox();
                    this.Bosses.ReflectBossOrbs(new Vector2(box.Center.X, box.Center.Y));
                }
                this.lastHealth = health;
                return;
            }

            // a held guard soaks most of the hit, at the cost of ki
            if (this.blocking && lost > 0)
            {
                float reduction = MathHelper.Clamp(this.Config.BlockDamageReduction, 0f, 0.95f);
                int refund = (int)(lost * reduction);
                if (refund > 0)
                {
                    player.health = Math.Min(player.maxHealth, health + refund);
                    health = player.health;
                    lost -= refund;
                    this.Ki.Spend(this.Config.BlockKiPerHit);
                    this.blockFlashTicks = 1;
                    this.PlayCue("dodge", "crystal");
                }
            }

            if (lost > 0 && form != null && form.DodgeChance > 0f
                && this.Ki.CanAfford(this.Config.UltraInstinctDodgeEnergyCost)
                && Game1.random.NextDouble() < this.EffectiveDodgeChance(form))
            {
                player.health = Math.Min(player.maxHealth, health + lost);
                this.Ki.Spend(this.Config.UltraInstinctDodgeEnergyCost);
                player.temporarilyInvincible = true;
                player.temporaryInvincibilityTimer = 0;
                player.currentTemporaryInvincibilityDuration = 700;

                this.dodgeTicks = 1;
                this.PlayCue("dodge", "swordswipe");
                health = player.health;
            }

            this.lastHealth = health;
        }

        /// <summary>A ki-cost dash: a short blink in the direction you are moving (diagonals
        /// included) with brief invincibility, so the new boss telegraphs have real counterplay.</summary>
        private void TryDash()
        {
            if (!this.Config.EnableDash || this.dashCooldownTicks > 0)
                return;

            Farmer p = Game1.player;
            if (p == null || !Context.IsPlayerFree)
                return;
            if (this.Ki.IsExhausted || !this.Ki.CanAfford(this.Config.DashKiCost))
            {
                ModEntry.Notify("Not enough ki to dash.");
                return;
            }

            Vector2 dir = this.DashDirection(p);
            if (dir == Vector2.Zero)
                return;

            GameLocation loc = p.currentLocation;
            int startX = (int)p.Tile.X;
            int startY = (int)p.Tile.Y;
            int stepX = Math.Sign(dir.X);
            int stepY = Math.Sign(dir.Y);

            Vector2 dest = p.Position;
            int reached = 0;
            for (int i = 1; i <= Math.Max(1, this.Config.DashTiles); i++)
            {
                int tx = startX + (stepX * i);
                int ty = startY + (stepY * i);

                // for a diagonal, refuse to cut through a solid corner
                if (stepX != 0 && stepY != 0
                    && (!IsPassable(loc, tx - stepX, ty) || !IsPassable(loc, tx, ty - stepY)))
                    break;
                if (!IsPassable(loc, tx, ty))
                    break;

                dest = new Vector2(tx * 64f, ty * 64f);
                reached = i;
            }

            if (reached == 0)
                return;

            p.Position = dest;
            this.Ki.Spend(this.Config.DashKiCost);
            this.Ki.InterruptCharging();
            p.temporarilyInvincible = true;
            p.temporaryInvincibilityTimer = 0;
            p.currentTemporaryInvincibilityDuration = Math.Max(100, this.Config.DashInvincibilityMs);
            this.dashCooldownTicks = Math.Max(1, this.Config.DashCooldownMs * 60 / 1000);
            this.dashFlashTicks = 1;
            this.PlayCue("dodge", "wand");
        }

        /// <summary>The direction to dash: the keys actually held (so W+A blinks up-left),
        /// falling back to the way the farmer faces when standing still.</summary>
        private Vector2 DashDirection(Farmer p)
        {
            Vector2 d = Vector2.Zero;
            foreach (int dir in p.movementDirections)
            {
                switch (dir)
                {
                    case 0: d.Y -= 1f; break;   // up
                    case 1: d.X += 1f; break;   // right
                    case 2: d.Y += 1f; break;   // down
                    case 3: d.X -= 1f; break;   // left
                }
            }
            if (d != Vector2.Zero)
                return d;

            switch (p.FacingDirection)
            {
                case 0: return new Vector2(0f, -1f);
                case 1: return new Vector2(1f, 0f);
                case 2: return new Vector2(0f, 1f);
                default: return new Vector2(-1f, 0f);
            }
        }

        private static bool IsPassable(GameLocation loc, int x, int y)
        {
            return loc != null
                   && loc.isTilePassable(new xTile.Dimensions.Location(x, y), Game1.viewport);
        }

        /// <summary>Refresh the held-guard state each tick and bleed ki while it is up.</summary>
        private void UpdateBlock()
        {
            if (this.parryWindowTicks > 0)
                this.parryWindowTicks--;

            bool held = this.Config.EnableBlock && Context.IsPlayerFree && !this.Ki.IsExhausted
                        && this.KeyDown(this.Config.BlockKey) && this.Ki.Current > 0.5f;

            // raising the guard opens a brief parry window: a hit landing inside it is a
            // perfect block rather than a soaked one
            if (held && !this.blocking && this.Config.EnableParry)
                this.parryWindowTicks = Math.Max(1, this.Config.ParryWindowMs * 60 / 1000);

            if (held)
            {
                this.Ki.Drain(Math.Max(0f, this.Config.BlockKiPerSecond) / 60f);
                if (this.Ki.Current <= 0f)
                    held = false;
            }
            this.blocking = held;
        }

        private bool KeyDown(KeybindList keys)
        {
            SButtonState state = keys.GetState();
            return state == SButtonState.Held || state == SButtonState.Pressed;
        }

        /// <summary>Mastered Ultra Instinct sharpens the longer you go untouched; every
        /// other form just uses its flat chance.</summary>
        internal float EffectiveDodgeChance(Transformation form)
        {
            if (form == null || form.DodgeChance <= 0f)
                return 0f;
            if (form.DodgeGrowth <= 0f)
                return form.DodgeChance;

            float ramp = Math.Max(1f, this.Config.MasteredDodgeRampSeconds) * 60f;
            float t = Math.Min(1f, this.untouchedTicks / ramp);
            return Math.Min(0.95f, form.DodgeChance + (form.DodgeGrowth * t));
        }

        /// <summary>A senzu is eaten through the normal food path, so we watch for the
        /// eat animation holding one and top everything up when it lands.</summary>
        private void CheckSenzu()
        {
            Farmer player = Game1.player;
            Item eating = player.itemToEat;

            // arm the restore while the eating animation is actually running; match either
            // id form to be safe. Requiring isEating here means we latch onto a real bite,
            // not a stray frame where itemToEat is briefly the senzu.
            bool isSenzu = eating != null
                && (eating.ItemId == DragonBallManager.SenzuId
                    || eating.QualifiedItemId == "(O)" + DragonBallManager.SenzuId);
            if (isSenzu && player.isEating)
                this.senzuPending = true;

            // fire the full restore on the falling edge of the eat animation. Keying off
            // isEating alone (rather than also demanding itemToEat==null on the same tick)
            // makes this fire reliably - the old dual condition could miss its window and
            // leave the player with only the bean's vanilla edibility and no ki refill.
            if (this.senzuPending && !player.isEating)
            {
                this.senzuPending = false;
                this.RestoreEverything();
            }
        }

        /// <summary>Top up health, stamina and ki to full and clear exhaustion.</summary>
        internal void RestoreEverything()
        {
            Game1.player.health = Game1.player.maxHealth;
            Game1.player.Stamina = Game1.player.MaxStamina;
            this.Ki.Fill();
            this.PlayCue("unlock", "yoba");
            ModEntry.Notify("Fully restored - health, energy and ki.");
        }

        /// <summary>A single legible number for everything the mod has given you.</summary>
        internal int PowerLevel()
        {
            if (!Context.IsWorldReady || Game1.player == null)
                return 0;

            Transformation form = this.CurrentForm;
            float multiplier = form?.AttackMultiplier ?? 1f;
            multiplier += this.DragonBalls.State.BonusAttackMultiplier
                          + this.Progress.State.ZenkaiAttackBonus
                          + this.Progress.MasteryGlobalAttackBonus();
            if (this.KaiokenActive)
                multiplier += this.Config.KaiokenAttackBonus;

            float mastery = form != null ? 1f + (0.25f * this.Progress.MasteryFraction(form)) : 1f;
            float basePower = 40f
                              + (Game1.player.CombatLevel * 18f)
                              + (this.DeepestMineLevel() * 3f)
                              + (this.Ki.Max * 0.9f);

            return (int)Math.Max(1f, basePower * multiplier * mastery);
        }

        private void CheckNewUnlocks()
        {
            int unlocked = this.UnlockedCount();
            if (this.announcedUnlocks < 0)
            {
                this.announcedUnlocks = unlocked;
                return;
            }

            if (unlocked <= this.announcedUnlocks)
                return;

            // wait for a moment we can actually open a dialogue box; while one is already up
            // this stays queued, so several unlocks at once each get their own box in turn
            if (!Context.IsPlayerFree || Game1.activeClickableMenu != null || Game1.eventUp)
                return;

            string name = Transformation.All[this.announcedUnlocks].DisplayName;
            this.ShowNarration($"A new power awakens: {name}!  Press {this.Config.TransformKey} to "
                               + "ascend toward it - at your highest form, press it again to power down.");
            this.PlayCue("unlock", "yoba");
            if (this.Config.ScreenFlash)
                Game1.flashAlpha = 1f;
            this.announcedUnlocks++;   // advance one; any further unlocks show on later ticks
        }

        private void OnRenderedWorld(object sender, RenderedWorldEventArgs e)
        {
            if (!Context.IsWorldReady || Game1.player == null || Game1.currentLocation == null)
                return;
            if (Game1.eventUp || Game1.farmEvent != null)
                return;

            Transformation form = this.CurrentForm;
            bool bossVisible = this.Config.EnableBosses && Game1.currentLocation is MineShaft;
            if (form == null && !this.Techniques.AnyActive && this.burstTicks <= 0
                && this.dodgeTicks <= 0 && !bossVisible && !this.DragonBalls.RitualActive
                && !this.Ki.IsCharging && !this.Progress.HasVisuals && !this.Rivals.Active
                && !this.Invader.Active && !this.blocking && this.dashFlashTicks <= 0
                && this.blockFlashTicks <= 0)
            {
                return;
            }

            SpriteBatch b = e.SpriteBatch;
            this.Fx.BeginGlow(b);
            try
            {
                if (bossVisible)
                    this.Bosses.DrawWorld(b);
                if (this.DragonBalls.RitualActive)
                {
                    this.DragonBalls.DrawWorld(b);
                    this.DrawTrialAuras(b);
                }
                this.Ki.DrawWorld(b, this.Fx);
                this.Progress.DrawGhosts(b, form);
                if (this.Rivals.Active)
                    this.Rivals.DrawWorld(b, this.Fx);
                if (this.Invader.Active)
                    this.Invader.DrawWorld(b, this.Fx);
                this.Progress.DrawMeleeArc(b, form);
                if (this.KaiokenActive && form != null)
                {
                    this.Fx.DrawAuraAt(b, this.Fx.PlayerAnchor(), new Color(255, 64, 54),
                                       4.6f * form.AuraScale, 0.6f, 3);
                }
                if (this.dodgeTicks > 0)
                    this.Fx.DrawDodge(b, this.dodgeTicks, DodgeFlashLength);
                this.DrawGuardAndDash(b);
                if (form != null)
                {
                    bool calm = this.FormIsCalm(form);
                    if (this.Config.ShowAura && !calm)
                        this.Fx.DrawAura(b, form);
                    if (form.Lightning && this.Config.ShowLightning && !calm)
                        this.Fx.DrawLightning(b, form);
                    if (this.burstTicks > 0)
                        this.Fx.DrawTransformBurst(b, form, this.burstTicks, BurstLength);
                }
                if (form != null)
                    this.Techniques.Draw(b, form);
            }
            finally
            {
                this.Fx.EndGlow(b);
            }
        }

        /// <summary>A blue guard bubble while blocking, and a quick flare on a dash.</summary>
        private void DrawGuardAndDash(SpriteBatch b)
        {
            Vector2 anchor = this.Fx.PlayerAnchor();
            Vector2 centre = new Vector2(anchor.X, anchor.Y - 32f);

            if (this.blocking || this.blockFlashTicks > 0)
            {
                float pulse = 0.30f + (0.10f * (float)Math.Sin(this.AnimTicks * 0.2));
                float alpha = this.blockFlashTicks > 0 ? 0.7f : pulse;
                int frame = (this.AnimTicks / 4) % 4;
                const float scale = 4.2f;
                b.Draw(this.KameTexture,
                       new Vector2(centre.X - (16 * scale), centre.Y - (16 * scale)),
                       new Rectangle(frame * 32, 64, 32, 32), new Color(120, 200, 255) * alpha,
                       0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            }

            if (this.dashFlashTicks > 0)
            {
                float t = this.dashFlashTicks / 10f;
                float scale = 3f + (t * 4f);
                int frame = Math.Min(3, (int)(t * 4f));
                b.Draw(this.KameTexture,
                       new Vector2(centre.X - (16 * scale), centre.Y - (16 * scale)),
                       new Rectangle(frame * 32, 64, 32, 32), new Color(200, 235, 255) * (1f - t),
                       0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            }
        }

        private void OnRenderedHud(object sender, RenderedHudEventArgs e)
        {
            if (!Context.IsWorldReady || Game1.eventUp)
                return;
            this.Bosses.DrawHud(e.SpriteBatch);
            this.Techniques.DrawHud(e.SpriteBatch);
            if (this.Config.ShowKiBar)
                this.Ki.DrawHud(e.SpriteBatch);
            if (this.Config.ShowPowerLevel)
                this.DrawPowerLevel(e.SpriteBatch);
        }

        private void DrawPowerLevel(SpriteBatch b)
        {
            // sits below the bar, not above it: a number directly over the meter reads
            // as the meter's value, which is exactly how it got mistaken for ki
            Rectangle bar = this.Ki.HudBounds();
            string label = $"PWR {this.PowerLevel():n0}";
            Vector2 size = Game1.smallFont.MeasureString(label) * 0.85f;

            Color colour = this.Ki.IsExhausted
                ? new Color(240, 150, 150)
                : this.CurrentForm?.AuraColor ?? Color.White;

            Vector2 pos = new Vector2(bar.X + (bar.Width / 2f) - (size.X / 2f), bar.Bottom + 4);
            b.DrawString(Game1.smallFont, label, pos + new Vector2(2f, 2f),
                         new Color(30, 20, 14) * 0.8f, 0f, Vector2.Zero, 0.85f,
                         SpriteEffects.None, 0f);
            b.DrawString(Game1.smallFont, label, pos, colour, 0f, Vector2.Zero, 0.85f,
                         SpriteEffects.None, 0f);
        }

        private void OnWarped(object sender, WarpedEventArgs e)
        {
            if (!e.IsLocalPlayer)
                return;
            this.Techniques.Cancel();

            // once a boss is engaged on your floor you are locked in until it falls: no
            // ladder up or down, no elevator, no warp totem out. (Skipping OVER a boss
            // floor you never actually set foot on - e.g. an elevator jump past it - is
            // still allowed; the seal only applies once you are standing on the floor.)
            // Dying is exempt so a defeat can still warp you out.
            if (this.Config.GateBossFloors && this.lastFloorSealed && this.lastMineLevel >= 0
                && Game1.player != null && Game1.player.health > 0)
            {
                bool stayingHere = e.NewLocation is MineShaft ms && ms.mineLevel == this.lastMineLevel;
                if (!stayingHere)
                {
                    this.pendingGateBounce = this.lastMineLevel;
                    return;
                }
            }

            this.Bosses.OnWarped(e.NewLocation);
            this.Invader.OnWarped(e.NewLocation);
        }

        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            this.Bosses.LoadSaveData();
            this.DragonBalls.LoadSaveData();
            this.Ki.LoadSaveData();
            this.Progress.LoadSaveData();
            this.lastHealth = Game1.player != null ? Game1.player.health : -1;
            this.announcedUnlocks = -1;
        }

        private void OnSaving(object sender, SavingEventArgs e)
        {
            // never write transformation hair into the save file
            this.PowerDown(false);
            this.Bosses.WriteSaveData();
            this.DragonBalls.WriteSaveData();
            this.Ki.WriteSaveData();
            this.Progress.WriteSaveData();
        }

        private void OnReturnedToTitle(object sender, ReturnedToTitleEventArgs e)
        {
            this.formIndex = -1;
            this.savedHair = -1;
            this.burstTicks = 0;
            this.announcedUnlocks = -1;
            this.Techniques.Cancel();
            this.Bosses.Reset();
            this.DragonBalls.Reset();
            this.Ki.Reset();
            this.Progress.Reset();
            this.Rivals.Reset();
            this.Invader.Reset();
            this.lastMineLevel = -1;
            this.lastFloorSealed = false;
            this.pendingGateBounce = -1;
            this.blocking = false;
            this.parryWindowTicks = 0;
            this.dashCooldownTicks = 0;
            this.dashFlashTicks = 0;
            this.blockFlashTicks = 0;
            this.lastHealth = -1;
            this.dodgeTicks = 0;
            this.kaiokenTicks = 0;
            this.kaiokenHealthCarry = 0f;
            ModEntry.StopLoop(ref this.auraCue);
        }

        // ------------------------------------------------------------ console

        private void OnCommand(string name, string[] args)
        {
            string sub = args.Length > 0 ? args[0].ToLowerInvariant() : "status";

            switch (sub)
            {
                case "status":
                {
                    int unlocked = this.UnlockedCount();
                    this.Monitor.Log($"Deepest mine level: {this.DeepestMineLevel()}", LogLevel.Info);
                    this.Monitor.Log($"Current form: {this.CurrentForm?.DisplayName ?? "base"}", LogLevel.Info);
                    this.Monitor.Log($"Zenkai boosts: {this.Progress.State.ZenkaiCount} "
                                     + $"(+{this.Progress.State.ZenkaiKiBonus:0} ki, "
                                     + $"+{this.Progress.State.ZenkaiAttackBonus:0.##} attack)",
                                     LogLevel.Info);
                    for (int i = 0; i < Transformation.All.Length; i++)
                    {
                        string state = i < unlocked ? "UNLOCKED" : $"locked (mine level {this.UnlockLevelFor(i)})";
                        int mastery = (int)(this.Progress.MasteryFraction(Transformation.All[i]) * 100);
                        this.Monitor.Log($"  {i + 1}. {Transformation.All[i].DisplayName}: {state}"
                                         + (i < unlocked ? $"  mastery {mastery}%" : ""),
                                         LogLevel.Info);
                    }
                    break;
                }

                case "unlock_all":
                    this.Config.UnlockEverything = true;
                    this.Helper.WriteConfig(this.Config);
                    this.Monitor.Log("All transformations unlocked (saved to config.json).", LogLevel.Info);
                    break;

                case "form":
                {
                    if (!Context.IsWorldReady)
                    {
                        this.Monitor.Log("Load a save first.", LogLevel.Error);
                        break;
                    }
                    if (args.Length < 2 || !int.TryParse(args[1], out int n)
                        || n < 1 || n > Transformation.All.Length)
                    {
                        this.Monitor.Log($"Usage: saiyan form <1-{Transformation.All.Length}>", LogLevel.Error);
                        break;
                    }
                    this.SetForm(n - 1);
                    break;
                }

                case "off":
                    this.PowerDown(true);
                    break;

                case "senzu":
                    if (!Context.IsWorldReady)
                        this.Monitor.Log("Load a save first.", LogLevel.Error);
                    else
                    {
                        this.RestoreEverything();
                        this.Monitor.Log("Restored health, stamina and ki to full.", LogLevel.Info);
                    }
                    break;

                case "pose":
                {
                    if (!Context.IsWorldReady)
                    {
                        this.Monitor.Log("Load a save first.", LogLevel.Error);
                        break;
                    }
                    if (args.Length < 2 || !int.TryParse(args[1], out int frame))
                    {
                        this.Monitor.Log("Usage: saiyan pose <frame index>", LogLevel.Error);
                        break;
                    }
                    Game1.player.FarmerSprite.setCurrentFrame(frame);
                    this.Monitor.Log($"Holding farmer frame {frame}. Move to clear it.",
                                     LogLevel.Info);
                    break;
                }

                case "bosses":
                {
                    foreach (BossDefinition def in BossDefinition.All)
                    {
                        string state = this.Bosses.IsDefeated(def) ? "DEFEATED" : "alive";
                        string reward;
                        switch (def.Reward)
                        {
                            case BossReward.Form:
                                reward = Transformation.All[def.FormIndex].DisplayName;
                                break;
                            case BossReward.Technique:
                                reward = this.TechniqueName(def.TechniqueId);
                                break;
                            case BossReward.Supplies:
                                reward = "senzu cache";
                                break;
                            default:
                                reward = $"{def.DragonBallNumber}-Star Dragon Ball";
                                break;
                        }
                        if (!string.IsNullOrEmpty(def.BonusTechniqueId))
                            reward += " + " + this.TechniqueName(def.BonusTechniqueId);
                        string where = def.MineLevel > 120
                            ? $"skull {def.MineLevel - 120,3}"
                            : $"mine  {def.MineLevel,3}";
                        this.Monitor.Log($"  {where}  {def.DisplayName,-32} {state,-8} -> {reward}",
                                         LogLevel.Info);
                    }
                    break;
                }

                case "clearboss":
                {
                    if (!Context.IsWorldReady)
                    {
                        this.Monitor.Log("Load a save first.", LogLevel.Error);
                        break;
                    }
                    MineShaft here = Game1.currentLocation as MineShaft;
                    BossDefinition def2 = here != null
                        ? BossDefinition.ForMineLevel(here.mineLevel)
                        : null;
                    if (def2 == null)
                    {
                        this.Monitor.Log("No boss floor here. Stand on a milestone floor and retry.",
                                         LogLevel.Error);
                        break;
                    }
                    this.Bosses.ForceClear(def2);
                    this.Monitor.Log($"Marked {def2.DisplayName} as defeated.", LogLevel.Info);
                    break;
                }

                default:
                    this.Monitor.Log(
                        "Usage: saiyan status | unlock_all | form <1-6> | off | bosses | clearboss | pose <n>",
                        LogLevel.Error);
                    break;
            }
        }

        // ------------------------------------------------------------ helpers

        internal static void Notify(string text)
        {
            if (!string.IsNullOrEmpty(text))
                Game1.addHUDMessage(new HUDMessage(text, 2));
        }

        private readonly Dictionary<string, Texture2D> portraitCache =
            new Dictionary<string, Texture2D>();

        /// <summary>Load a boss's portrait from assets/portraits/&lt;id&gt;.png, cached, falling
        /// back to a shared default. Replacing the PNG swaps the portrait with no code change.</summary>
        internal Texture2D GetBossPortrait(string id)
        {
            id ??= "_default";
            if (this.portraitCache.TryGetValue(id, out Texture2D cached))
                return cached;

            Texture2D tex = null;
            try { tex = this.Helper.ModContent.Load<Texture2D>($"assets/portraits/{id}.png"); }
            catch (Exception) { }
            if (tex == null && id != "_default")
                tex = this.GetBossPortrait("_default");

            this.portraitCache[id] = tex;
            return tex;
        }

        /// <summary>Show narrator text as a plain dialogue box at the bottom of the screen -
        /// no portrait, no name - so lore lines stay up until dismissed instead of scrolling
        /// past as a HUD toast that is gone before it can be read.</summary>
        internal void ShowNarration(string text)
        {
            if (string.IsNullOrEmpty(text))
                return;
            try { Game1.drawObjectDialogue(text); }
            catch (Exception) { ModEntry.Notify(text); }
        }

        /// <summary>Show a boss line the way an NPC speaks: a dialogue box with a portrait and
        /// the boss's name. Narration is handled separately as a plain narrator box.</summary>
        internal void ShowBossSpeech(string id, string displayName, string speech)
        {
            if (string.IsNullOrEmpty(speech))
                return;
            try
            {
                Texture2D portrait = this.GetBossPortrait(id);
                NPC speaker = new NPC(null, new Vector2(-2000f, -2000f), "", 0,
                                      displayName ?? "???", false, portrait);
                speaker.displayName = displayName ?? "???";
                Game1.DrawDialogue(new Dialogue(speaker, null, speech));
            }
            catch (Exception)
            {
                Game1.drawObjectDialogue(speech);   // portrait-less fallback
            }
        }

        /// <summary>Show several lines as consecutive portrait dialogue pages from one speaker
        /// (page breaks via the "#$b#" dialogue token), so a boss can say a few lines in a row.</summary>
        internal void ShowSpeechLines(string id, string displayName, string[] lines)
        {
            if (lines == null || lines.Length == 0)
                return;
            try
            {
                Texture2D portrait = this.GetBossPortrait(id);
                NPC speaker = new NPC(null, new Vector2(-2000f, -2000f), "", 0,
                                      displayName ?? "???", false, portrait);
                speaker.displayName = displayName ?? "???";
                Game1.DrawDialogue(new Dialogue(speaker, null, string.Join("#$b#", lines)));
            }
            catch (Exception)
            {
                Game1.multipleDialogues(lines);     // portrait-less fallback
            }
        }

        /// <summary>Play a sound cue without letting a missing cue take the mod down.</summary>
        internal static void SafeSound(string cue)
        {
            try
            {
                Game1.playSound(cue);
            }
            catch (Exception)
            {
                // a missing audio cue is not worth an error
            }
        }
    }
}
