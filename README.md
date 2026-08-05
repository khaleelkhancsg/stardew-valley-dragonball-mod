# Saiyan Transformations — a Dragon Ball total-combat mod for Stardew Valley

> Descend the mines, unlock seven escalating transformations, master ki techniques, fight 34 hand-authored bosses across 300 floors, gather the seven Dragon Balls, and summon a wish. A full progression RPG bolted onto a farming game.

**A Stardew Valley 1.6 mod built on SMAPI, ~8,300 lines of C# across a clean modular architecture, with hand-drawn pixel art and a bespoke ~2,400-line procedural asset pipeline.**

Designed, art-directed, game-balanced and shipped by **Khaleel Khan**, using agentic AI as an implementation partner.

---

## What it is

Saiyan Transformations turns the Stardew Valley mine into a Dragon Ball power-fantasy. It is not a cosmetic reskin — it is a self-contained combat progression system with its own resource economy, boss ladder, difficulty curve, technique set, save data, and end-game loop, all layered non-destructively on top of the base game.

- **7 transformations**, unlocked by depth and by beating the boss that guards each one, each a meaningful power tier with its own aura, hand-drawn hair, stat multipliers and drawbacks.
- **A ki resource system** that replaces stamina for combat — it grows as you descend, recharges passively and actively, and punishes you with an exhaustion state if you burn it all.
- **Switchable ki techniques** — Kamehameha, Destructo Disk, Solar Flare, Spirit Bomb, Instant Transmission, Kaioken — each with charge/fire timing, cooldowns, and reused vanilla animation frames.
- **34 bosses across 300 floors**, reskinned from vanilla monsters, each with a unique hand-drawn sprite sheet, signature special moves, phases, an escalating strength curve, difficulty-scaled loot, respawn-on-cooldown, and full meet/defeat/rematch dialogue.
- **The Dragon Ball wish system** — gather seven spheres from boss guardians, place them on your farm, and summon a wish that permanently reshapes your character.
- **A recurring end-game invader** that hunts you in the deep mine *and* the overworld, escalating every time it is beaten.
- **Rival invasions, senzu beans, a power-level readout, a Zenkai comeback mechanic, a mastery system, and a hand-drawn ki gauge.**

Everything is tunable — one-click **difficulty presets** (Story / Normal / Hard / Brutal), unlock depths, drain rates, drop rates, keybinds and dozens of toggles — through `config.json`, or through an in-game options page if [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098) is installed (an optional dependency).

---

## Feature breakdown

### Transformations (7 forms)
Super Saiyan → Super Saiyan 2 → Super Saiyan 3 → Super Saiyan God → Super Saiyan Blue → Ultra Instinct → Mastered Ultra Instinct.

- Unlocks are paced ~40–50 floors apart so each form is held long enough to **master** it before the next arrives.
- Each form applies attack / speed / defence / crit multipliers via `BuffEffects`, swaps in hand-drawn hair, and lights a coloured aura with additive-blended glow and optional electric crackle.
- **Mastery**: time spent in a form accrues mastery; a fully mastered form runs *calm* — its aura, hum and crackle fall silent unless you actively charge ki, and it costs less ki to hold.
- **Mastery carryover**: mastering a form grants a permanent bonus (attack, defence, max ki) that applies in **every** form and **stacks** across all mastered forms — an incentive to master the whole roster, not just the strongest.
- **Per-form passives & defence**: each form has its own passive (a scaling out-of-nowhere health regen) and a damage-reduction that grows with mastery, both surfaced in the buff tooltip.
- **Ultra Instinct** grants a dodge chance; **Mastered UI** is the capstone tier.

### Ki system
- Replaces stamina as the combat resource. Capacity scales with mine depth.
- Passive regen in base form; **active charge** by holding Shift while stationary (interrupted instantly by movement or damage).
- **Exhaustion**: drop below 20% and all stats are cut 80% until you recover; active charging is locked until you climb back out.
- A **hand-drawn ki gauge** (self-made) renders the bar and turns red under threshold or when exhausted.

### Techniques
Fire (R) and cycle (Shift+R) between unlocked techniques, each using vanilla sword-thrust frames for the pose:
- **Kamehameha** — charged sweeping beam.
- **Destructo Disk** — charge-and-throw spinning blade that cuts weeds, single wood and mine ore nodes (and drops their contents).
- **Solar Flare**, **Spirit Bomb**, **Instant Transmission**, **Kaioken** — unlocked by defeating their boss.

### Dash & block
- **Dash** — a ki-cost blink with brief invincibility, thrown in your **actual movement direction (diagonals included)** and falling back to your facing when standing still; refuses to cut solid corners.
- **Block** — hold to soak most incoming damage at the cost of ki per second and per hit, with a guard bubble while it's up.
- **Parry** — a *perfectly-timed* guard (raise it just as a hit lands) negates the hit entirely, costs no ki, grants a beat of invincibility, and **deflects the boss's ki blasts back at it** with a counter-burst. Together these give the boss telegraphs real counterplay — dash the beams, parry the blasts.

### Bosses & the 300-floor ladder
- 34 bosses spaced on a clean 10-floor grid from floor 10 to 300, ordered so **deeper = stronger** and themed across the DBZ sagas (Saibamen → Frieza → Cell → Buu → Broly → God of Destruction). Every boss is a single named fight except the Saibaman squad.
- Each boss is a reskinned vanilla monster with its **own hand-drawn sprite sheet** at larger-than-vanilla frames, custom health/damage budget, aura, and a health bar.
- **Special moves** — bosses fire **aimed ki-blast spreads, telegraphed beams, blink-strikes, self-healing, shockwaves, paralysis and death blasts** on top of their vanilla melee, all through a bespoke ability runner (no Harmony patching).
- **Signature moves** — the marquee villains get identity moves: **Frieza's Death Ball** (a huge dropped sphere), **Cell's ki-absorption** (siphons your ki and heals), **Buu's candy beam** (weakens and slows), and **Guldo's time-stop**.
- **Phases** — every non-guardian boss powers up as its health falls (harder hits, more speed) and hits a "stops holding back" surge, gaining a brand-new move at the final phase.
- **Regenerators** — the Cell / Buu / Metal Cooler bosses reform once at half health, with their own comeback line, before they can be put down for good.
- **No boss can be skipped** — a living boss seals the way down until it falls.
- **Respawn on cooldown** (~40 in-game days) so fights can be repeated without being farmed.
- **Rematches escalate**: every defeat permanently buffs that boss's health, damage, resilience — and, for signature bosses, move speed — shown as a `×N` tier on its health bar.
- **Difficulty-scaled drops**: gold, senzu and depth-appropriate materials (geodes → gold bars → iridium → diamonds → prismatic shards), scaling with floor and rematch count.
- **Full dialogue** — every boss has distinct lines on first meeting, on defeat, on the 2nd and 3rd rematch, and a repeatable signature line thereafter, all wrapped in original lore about why the fallen are drawn to the depths.

### Dragon Balls & wishes
- The seven spheres drop from their guardian bosses across the descent.
- Place all seven together outdoors to trigger a Shenron summoning ritual (screen darkens, lightning, a wish menu — no dragon is drawn; the absence is the point).
- **Six wishes**, each scaled to a full ~290-floor gather: permanent attack, a remade body, riches, total skill mastery, awakening the next form, or freedom from exhaustion — several also bank permanent ki and attack.
- Escalating wish trials guard later wishes; the spheres scatter after each wish and must be re-gathered.

### The Multiversal Invader
- A recurring end-game boss with its own dialogue that tears in from another reality.
- Appears past floor 310 (10% per deep floor) **and** hunts you in the overworld (2%/day, +2% per wish) once you have actually been that deep.
- Never stays dead — every defeat makes its next arrival stronger and changes its taunts.

### Quality-of-life systems
Rival invasions, senzu beans (full restore), a power-level readout, Zenkai boosts (come back stronger after near-death), afterimage trails, ki-charged melee, custom SFX synthesised to match the source material, and an exhaustion status indicator.

---

## The art is hand-made

All of the **hero art is hand-drawn pixel work by me**:

- the **hair for all seven transformations**,
- the **ki gauge**,
- the **Kamehameha** and **Destructo Disk** effect sheets,
- **~28 boss sprite sheets and matching dialogue portraits** — every named villain, hand-drawn and fed through the pipeline below.

A **bespoke procedural asset pipeline** — ~2,400 lines of Python (Pillow / NumPy / SciPy) — turns that raw art into game-ready sheets: it keys the background, detects the sprite grid, scales each boss uniformly, bottom-aligns the feet, and lays the frames out in Stardew's exact 4-direction animation convention (front / right / up / left, with per-boss facing handled). The same pipeline generates the auras, lightning, item icons and synthesised SFX to exact vanilla frame dimensions.

---

## Tech stack & architecture

| Layer | Choice |
|---|---|
| Language | **C# (.NET 6)** |
| Platform | **SMAPI** (Stardew Modding API) |
| Build/deploy | **Pathoschild ModBuildConfig** (auto-deploys to the game's Mods folder) |
| Game APIs | `Data/HairData`, `Data/AudioChanges`, `Data/Objects`, `BuffEffects`, `AnimatedSprite`, `MineShaft`, content `AssetRequested` pipeline |
| Art tooling | **Python** — Pillow, NumPy, SciPy |
| Config | Data-driven `ModConfig`, GMCM-compatible |

**Codebase:** ~8,300 lines of C# across 16 focused files, each owning one system:

```
ModEntry.cs        orchestration, events, transformations, buffs, unlocks, dash/block/parry, boss-gate
Bosses.cs          boss roster, spawning, scaling, phases, respawn/cooldown, drops, health bar
BossAbilities.cs   the ability runner: ki blasts, beams, blinks, signature moves, hazards
BossDialogue.cs    per-boss meet / defeat / rematch / revive dialogue data
Ki.cs              ki pool, charging, exhaustion, HUD gauge
Technique.cs       base technique framework + switching
Techniques.cs      Kamehameha, Destructo Disk
TechniquesExtra.cs Solar Flare, Spirit Bomb, Instant Transmission, Kaioken
DragonBalls.cs     items, summoning ritual, wish menu and effects
Invader.cs         the recurring Multiversal Invader
Rivals.cs          overworld rival invasions
Progress.cs        mastery + carryover, Zenkai, afterimage, ki-melee, form regen
Transformation.cs  form definitions (stats, passives, hair, aura)
FxRenderer.cs      aura / lightning / shockwave rendering
ModConfig.cs       every tunable, documented
GmcmApi.cs         optional Generic Mod Config Menu interface
tools/*.py         procedural sprite & sound generators
```

The design favours **declarative data + shared systems**: bosses are `BossDefinition` records driven through one scaling curve; the whole 300-floor difficulty ladder is a single function; adding a boss, technique or form is a data change, not a rewrite.

---

## Built with agentic AI — and what that demonstrates

This project was built by directing an **agentic AI coding assistant** (Claude Code) as an implementation partner, while I owned every decision that mattered:

- **Product & game design** — the progression, the 300-floor boss ladder, the ki economy, the wish system, the respawn/rematch loop, the pacing of unlocks for mastery.
- **Balancing & tuning** — the difficulty curve, drop tables, cooldowns, and the "difficult but doable" feel, iterated through repeated playtest feedback.
- **Art direction & original pixel art** — hand-drawing the transformation hair, ki gauge and technique effects, and reviewing every generated asset frame-by-frame.
- **Technical direction** — steering architecture, catching regressions, insisting on the modular breakdown, and reviewing changes before they shipped.
- **Writing** — the lore and the full set of boss dialogue.

The AI accelerated implementation; the vision, judgement, art and design are mine. Shipping a coherent 8,000-line codebase this way is itself the point: it shows the ability to **specify, direct, review and integrate an autonomous agent** into a real, working, non-trivial software product — a genuinely modern engineering skill.

### What this project shows I can do
- Design and ship a **large, cohesive software system** with real architecture, save data and an extensible plugin surface.
- Work fluently across a **C# / .NET game-modding stack** and a **Python art/audio toolchain**.
- Do **systems and game design**: economies, difficulty curves, progression loops, boss design.
- Produce **original pixel art** and integrate it into an existing engine's conventions.
- **Direct and collaborate with agentic AI** to deliver production-quality output faster, with human review and taste in the loop.
- Iterate against feedback, own quality, and carry a project from concept to a polished, configurable, shippable release.

---

## Install (players)

1. Install [SMAPI](https://smapi.io/).
2. Download this repo and drop the built `SaiyanTransformations` folder into `Stardew Valley/Mods/`.
3. Launch through SMAPI. Descend the mine to floor 10 and press **F** to transform.

Default keys (all rebindable in `config.json` or the GMCM menu): **F** transform · **Shift+F** power down · **R** fire technique · **Shift+R** switch technique · **Q** dash · **C** block (hold).

## Build (developers)

```bash
dotnet build -c Release
```

Requires the .NET 6 SDK. `ModBuildConfig` compiles the DLL and auto-deploys the mod folder into your Stardew Valley `Mods/` directory. Regenerate art/sound with:

```bash
python tools/generate_monsters.py
python tools/generate_assets.py
python tools/generate_sounds.py
```

---

## Credits

- **Design, art, balancing, direction & lore:** Khaleel Khan
- **Implementation:** authored with Claude Code (agentic AI), under direction and review
- **Framework:** [SMAPI](https://smapi.io/) and [ModBuildConfig](https://github.com/Pathoschild/SMAPI)
- Dragon Ball is a trademark of its respective owners. This is a non-commercial fan project.
