# Ninja — Slay the Spire 2 custom character mod

`NinjaMod` adds a selectable **Ninja (忍者)** character to Slay the Spire 2. The character is
built around **Bleed**, delayed **fire ninjutsu** damage, **stealth/clone** tempo and defensive
**ninjutsu**. This is a robust, minimal, playable v0.1.

- Internal mod id: `NinjaMod` (BaseLib applies this as the stable id namespace/prefix for all content)
- Project / output name: `NinjaMod`
- Character: **Ninja / 忍者** — 72 max HP, 3 energy/turn, default starting gold
- Starting relic: **Hidden Blade / 藏刃**
- Starting deck: 4× Ninja Strike, 4× Ninja Defend, 1× Shuriken, 1× Assassination

> All game-specific absolute paths live in `local.props` (gitignored). No proprietary game DLLs
> or assets are committed to this repo — the project references the game DLL by local path only.

---

## Template & dependencies

- **Template:** `Alchyr.Sts2.Templates` → *Slay the Spire 2 Character* (`alchyrsts2charmod`, v2.5.0),
  generated with `--PublicizeSts true`.
- **Content dependency:** `Alchyr.Sts2.BaseLib` (3.3.2) — the standard StS2 content library.
- **Build-time packages:** `Krafs.Publicizer`, `Alchyr.Sts2.ModAnalyzers`, `BSchneppe.StS2.PckPacker`
  (generates the mod `.pck` from simple PNG/JSON assets without needing a full Godot/MegaDot export).
- **Engine:** Godot.NET.Sdk 4.5.1, targeting `net9.0`.

## Prerequisites

- Windows + Steam install of Slay the Spire 2.
- **.NET 9 SDK** (`dotnet --list-sdks` should list a `9.x` SDK).
- The game at the path configured in `local.props`.

## Local configuration (`local.props`, gitignored)

```xml
<Project>
  <PropertyGroup>
    <GameDir>D:\SteamLibrary\steamapps\common\Slay the Spire 2</GameDir>
    <ModsDir>D:\SteamLibrary\steamapps\common\Slay the Spire 2\mods</ModsDir>
    <Sts2Path>$(GameDir)</Sts2Path>
  </PropertyGroup>
</Project>
```

`Directory.Build.props` (also gitignored) imports `local.props`. If `local.props` is missing, the
template's `Sts2PathDiscovery.props` tries to auto-detect Steam.

---

## Build / install / play

All commands are Windows PowerShell, run from the repo root.

### 1. One-time setup (validates env, installs BaseLib)

```powershell
powershell -ExecutionPolicy Bypass -File scripts\setup.ps1
```

This validates the game path, the mods folder and the .NET SDK, and installs **BaseLib** into
`<ModsDir>\BaseLib` from the **official** `Alchyr.Sts2.BaseLib` NuGet package (no unofficial
downloads). If you already manage BaseLib via the official Steam Workshop release, that is used
instead.

### 2. Build

```powershell
powershell -ExecutionPolicy Bypass -File scripts\build.ps1            # Debug (default)
powershell -ExecutionPolicy Bypass -File scripts\build.ps1 -Configuration Release
```

### 3. Install into the game

```powershell
powershell -ExecutionPolicy Bypass -File scripts\install.ps1
```

Copies `NinjaMod.dll`, `NinjaMod.json`, `NinjaMod.pck` and `NinjaMod.pdb` into
`<ModsDir>\NinjaMod`. It never deletes other mods.

### 4. Build + install in one step

```powershell
powershell -ExecutionPolicy Bypass -File scripts\build-and-install.ps1
```

> The build also auto-copies outputs to the mods folder via an MSBuild post-build target, so a
> plain `dotnet build NinjaMod.csproj` installs the DLL/JSON/PCK too.

### Installed layout

```
<ModsDir>\BaseLib\   BaseLib.dll, BaseLib.json, BaseLib.pck
<ModsDir>\NinjaMod\  NinjaMod.dll, NinjaMod.json, NinjaMod.pck, NinjaMod.pdb
```

---

## Manual in-game test checklist

1. Launch Slay the Spire 2 from Steam.
2. Enable mods if the game asks; ensure **BaseLib** is enabled (and loads before NinjaMod).
3. Start a new run.
4. Select **Ninja**.
5. Confirm HP is **72**.
6. Confirm energy is **3**.
7. Confirm the starting deck has 4 Ninja Strike, 4 Ninja Defend, 1 Shuriken, 1 Assassination.
8. Confirm the starting relic **Hidden Blade** is present.
9. Start combat and confirm a **Kunai** is added to your hand at the start of each of your turns.
10. Confirm Kunai/Shuriken apply **Bleed** only when their damage actually reaches HP (not when fully blocked).
11. Confirm **Assassination** ignores Block and still triggers existing Bleed.
12. Confirm **Burning** triggers at the enemy's next turn start, and that **Demon Flame Burst** ignites it.
13. Confirm **Resist** (from Earth Escape) reduces each incoming attack hit before Block.
14. Confirm **Shadow Clone** makes your other cards resolve twice and does not recurse on itself.

---

## Mechanics summary (v0.1)

| Content | Type | Cost | Effect (upgraded) |
|---|---|---|---|
| Ninja Strike / 忍者打击 | Attack | 1 | Deal 6 (9) |
| Ninja Defend / 忍者防御 | Skill | 1 | Gain 5 (8) Block |
| Shuriken / 手里剑 | Attack | 2 | Deal 9 (12); if unblocked apply 2 (3) Bleed; Exhaust |
| Assassination / 暗杀 | Attack | 1 | Deal 7 (10) ignoring Block |
| Kunai / 飞刀 (generated, temporary) | Attack | 1 | Retain, Exhaust. Deal 5 (7); if unblocked apply 1 Bleed |
| Flame Barrage / 火忍：火焰弹幕 | Skill | 1 | Deal 2×3 (3×3), then apply 3 (4) Burning |
| Demon Flame Burst / 火忍：火魔爆 | Skill | 2 | Deal 12 (16), then ignite all Burning |
| Shadow Clone / 影分身 | Skill | 3 (→2) | Copy your non-Shadow-Clone cards this turn and next turn |
| Earth Escape / 土忍：土遁 | Power | 1 | Gain 1 (2) Resist |
| Earth Wall / 土忍：土墙 | Skill | 1 | Gain 7 (10) Block + Debuff Immunity until end of next turn |
| Hidden Blade / 藏刃 | Relic (Starter) | – | Start of each turn: add a Kunai to hand (the Ninja core passive) |

**Powers:** Bleed/流血, Burning/燃烧, Resist/抵挡, Debuff Immunity/免疫负面, Shadow Clone/影分身.

### How the custom mechanics are implemented (verified against the installed BaseLib/game API)

- **Bleed** uses the `AfterDamageReceived` hook and `DamageResult.UnblockedDamage` to detect HP
  loss. It only triggers on **move (attack)** damage. The bonus is dealt as
  `Unblockable | Unpowered` (not a move), so it **cannot trigger itself** (no recursion). A boolean
  guard is also present as a safety net.
- **Burning** uses `AfterSideTurnStart` (like Poison) to deal `Unblockable | Unpowered` damage at the
  affected enemy's next turn start, then removes itself. *Ignite* deals `Burning × 2` and removes it.
- **Resist** uses `ModifyDamageAdditive` (the same pre-block hook Strength uses) to reduce each
  incoming attack hit before Block. It does not reduce unblockable HP loss.
- **Debuff Immunity** uses `TryModifyPowerAmountReceived` (like Artifact) to cancel debuffs on the
  owner, ticking down at end of turn until end of next turn.
- **Shadow Clone** uses `ModifyCardPlayCount` (like Duplication) to resolve each non-Shadow-Clone
  card an extra time while active; the play-count hook is queried once per play, so copies cannot
  recurse.
- The **Ninja passive** (add a Kunai each turn) lives on the **Hidden Blade** relic, because relics
  are reliably part of the combat hook loop.

---

## Known limitations / approximations

- **Shadow Clone has no visible clone body and does not take damage.** The current StS2/BaseLib
  surface used here has no ally/summon entity API, so Shadow Clone is a player power that copies card
  effects only. A visible companion and "the clone also takes damage" are **TODO** and intentionally
  not faked with unsafe global hacks.
- **Burning does not trigger Bleed** (by design for v0.1). Because Bleed only fires on *move/attack*
  damage and Burning is unblockable non-attack damage, Burning never triggers Bleed. This avoids
  unclear balance/recursion.
- **"Not fully blocked"** is read from `DamageResult.UnblockedDamage > 0` (the engine's exact
  post-block HP-loss value), so it is precise.
- **Resist** reduces only attack hits (move damage) before Block, never unblockable HP loss
  (Bleed/Burning), matching the requested intent.
- **Localization** is defined **in code** (the BaseLib-recommended approach) and is **language-aware**:
  it returns English or Simplified Chinese (`zhs`) based on the active game language. The JSON files
  under `NinjaMod/localization/eng/` are template placeholders; the prefixed/slugified runtime entry
  ids make hand-written JSON fragile, so in-code loc is the source of truth.
- **The "Architect" character banter** loc (optional flavor the analyzer flags as `STS001`) is not
  authored; BaseLib provides runtime fallbacks, so this is non-fatal. The diagnostic is suppressed
  via `NoWarn`.
- **Visuals** use the Ironclad as a placeholder body/animations (`PlaceholderCharacterModel`).
- **No multiplayer is implemented or faked.** The mod is a normal single-player character mod that
  loads via the standard StS2 mod loader and coexists with other mods.

## Placeholder art

The images under `NinjaMod/images/**` are the template's **placeholder** PNGs (card/power/relic/
character icons). They are clearly placeholders and ship inside the generated `.pck`. Replace them
with real art (keep the same file names, or update the path helpers) to customise. `Kunai`'s art
falls back to the placeholder `card.png` until a `kunai.png` is added.

## Stable IDs / namespace

All content ids are the model class names, namespaced under the `NinjaMod` mod prefix that BaseLib
applies (e.g. the character entry is `Ninja`). Code namespaces: `NinjaMod.NinjaModCode.Character`,
`.Cards`, `.Powers`, `.Relics`.

## Next recommended tasks

1. Author real art for the character, cards, powers and the Hidden Blade relic (replace placeholders).
2. Add proper per-language localization JSON if you prefer file-based loc over in-code loc.
3. Add more reward cards (more fire/earth/water ninjutsu) and a few relics/potions for the pool.
4. Add card hover-tip keywords (Bleed/Burning/Resist) on the cards for clearer tooltips.
5. Explore a visible Shadow Clone companion if/when an ally/summon API becomes available.
6. Author the optional "Architect" character banter localization and remove the `STS001` suppression.

## Repository notes

- `local.props` and `Directory.Build.props` are **gitignored** (machine-specific paths).
- `bin/`, `obj/`, `.godot/`, `*.pck`, `*.pdb`, logs and any game DLLs are gitignored.
- No copyrighted game assets or DLLs are committed.

