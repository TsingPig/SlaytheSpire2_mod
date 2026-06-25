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

## Card Gallery / 卡牌图鉴

> Click any card to view full-size. Images in `NinjaMod/images/card_portraits/` (250×350 px).

### Basic（基础牌）

<table>
<tr align="center">
<td><img src="NinjaMod/images/card_portraits/ninja_strike.png" width="130"><br><b>忍者打击</b><br>1⚡ 攻击<br>6(9) 伤害</td>
<td><img src="NinjaMod/images/card_portraits/ninja_defend.png" width="130"><br><b>忍者防御</b><br>1⚡ 技能<br>5(8) 格挡</td>
<td><img src="NinjaMod/images/card_portraits/shuriken.png" width="130"><br><b>手里剑</b><br>1⚡ 攻击<br>10(13) 伤+2(3)流血</td>
<td><img src="NinjaMod/images/card_portraits/assassination.png" width="130"><br><b>暗杀</b><br>1⚡ 攻击 · 静默<br>7(10) 无视格挡</td>
</tr>
</table>

### Token（衍生牌）

<table>
<tr align="center">
<td><img src="NinjaMod/images/card_portraits/kunai.png" width="130"><br><b>飞刀</b><br>1⚡ 攻击 · 保留消耗<br>5(7) 伤+1流血</td>
<td><img src="NinjaMod/images/card_portraits/shuriken.png" width="130"><br><b>注入手里剑</b><br>1⚡ 攻击 · 保留消耗<br>10 伤+2流血·燃烧追加6</td>
<td><img src="NinjaMod/images/card_portraits/afterimage_attack.png" width="130"><br><b>残影</b><br>0⚡ 攻击 · 消耗<br>动态伤害（半伤）</td>
</tr>
</table>

### Common（普通）

<table>
<tr align="center">
<td><img src="NinjaMod/images/card_portraits/katana_art.png" width="130"><br><b>武士刀法</b><br>1⚡ 攻击<br>5×2(3) AOE</td>
<td><img src="NinjaMod/images/card_portraits/prowl.png" width="130"><br><b>潜行</b><br>0⚡ 技能<br>4 格挡+下回合暗杀</td>
<td><img src="NinjaMod/images/card_portraits/quenching.png" width="130"><br><b>火忍：淬火术</b><br>1⚡ 技能<br>6(8) 淬火</td>
<td><img src="NinjaMod/images/card_portraits/rock_shatter.png" width="130"><br><b>土忍：碎石</b><br>1⚡ 技能 · 消耗<br>8(13) 格挡+打出防御</td>
<td><img src="NinjaMod/images/card_portraits/kunai_throw.png" width="130"><br><b>苦无</b><br>1⚡ 攻击<br>6(9) 伤·流血回能</td>
</tr>
<tr align="center">
<td><img src="NinjaMod/images/card_portraits/iai_strike.png" width="130"><br><b>居合</b><br>2⚡ 攻击<br>10(15)+5(8)追加</td>
<td><img src="NinjaMod/images/card_portraits/ki_breath.png" width="130"><br><b>气合</b><br>1⚡ 技能<br>4(6) 回复</td>
<td><img src="NinjaMod/images/card_portraits/swallow_return.png" width="130"><br><b>燕返</b><br>1⚡ 攻击<br>4(7) 伤·全挡回能</td>
<td><img src="NinjaMod/images/card_portraits/stone_summon.png" width="130"><br><b>土忍：唤石</b><br>1⚡ 技能<br>抵挡×4(5) 格挡</td>
<td><img src="NinjaMod/images/card_portraits/earth_escape.png" width="130"><br><b>土忍：土遁</b><br>0⚡ 能力<br>1(2) 抵挡</td>
</tr>
<tr align="center">
<td><img src="NinjaMod/images/card_portraits/earth_wall.png" width="130"><br><b>土忍：土墙</b><br>1⚡ 技能<br>7(10) 格挡+免疫负面</td>
<td><img src="NinjaMod/images/card_portraits/detonation.png" width="130"><br><b>火忍：起爆符</b><br>0⚡ 技能<br>点燃单体燃烧</td>
<td><img src="NinjaMod/images/card_portraits/forge_flame_thrust.png" width="130"><br><b>火忍：锻火刺</b><br>1⚡ 攻击<br>6(9) 伤+4(5)燃烧</td>
<td><img src="NinjaMod/images/card_portraits/stone_gather_thrust.png" width="130"><br><b>土忍：聚石刺</b><br>1⚡ 攻击<br>6(9) 伤+3(4)抵挡</td>
<td><img src="NinjaMod/images/card_portraits/musashi_thrust.png" width="130"><br><b>武藏：刺</b><br>0⚡ 攻击 · 消耗<br>9 伤+2流血</td>
</tr>
</table>

### Uncommon（罕见）

<table>
<tr align="center">
<td><img src="NinjaMod/images/card_portraits/kusari_gama.png" width="130"><br><b>锁镰</b><br>1⚡ 攻击<br>9(12)+4(6)追加</td>
<td><img src="NinjaMod/images/card_portraits/rising_fist.png" width="130"><br><b>起承拳</b><br>1⚡ 攻击<br>6(9) 伤+打出飞刀</td>
<td><img src="NinjaMod/images/card_portraits/earth_rend.png" width="130"><br><b>土忍：裂地</b><br>1⚡ 技能<br>敌debuff和=格挡</td>
<td><img src="NinjaMod/images/card_portraits/bone_art.png" width="130"><br><b>骨法</b><br>1⚡ 技能<br>11 格挡+2活力</td>
<td><img src="NinjaMod/images/card_portraits/yata_mirror.png" width="130"><br><b>八咫镜</b><br>1⚡ 能力<br>每回合2(3)格挡</td>
</tr>
<tr align="center">
<td><img src="NinjaMod/images/card_portraits/kuji_protection.png" width="130"><br><b>九字护身法</b><br>2(1)⚡ 能力·消耗<br>3抵挡+每回合2×格挡</td>
<td><img src="NinjaMod/images/card_portraits/flame_shield.png" width="130"><br><b>火忍：火盾</b><br>1(0)⚡ 能力<br>受击反烧</td>
<td><img src="NinjaMod/images/card_portraits/blaze_inferno.png" width="130"><br><b>火忍：豪炎</b><br>0⚡ 技能<br>7(9) 燃烧AOE</td>
<td><img src="NinjaMod/images/card_portraits/crimson_claw.png" width="130"><br><b>凤仙花爪红</b><br>1⚡ 技能·消耗<br>生成2张注入手里剑</td>
<td><img src="NinjaMod/images/card_portraits/petrification.png" width="130"><br><b>土忍：石化术</b><br>2(1)⚡ 技能<br>13格挡+清负面</td>
</tr>
<tr align="center">
<td><img src="NinjaMod/images/card_portraits/ashes.png" width="130"><br><b>火忍：灰烬</b><br>1(0)⚡ 技能<br>引爆AOE+抽1</td>
<td><img src="NinjaMod/images/card_portraits/demon_flame_burst.png" width="130"><br><b>火忍：火魔爆</b><br>2⚡ 技能<br>12(16) 伤+引爆</td>
<td><img src="NinjaMod/images/card_portraits/flame_barrage.png" width="130"><br><b>火忍：火焰弹幕</b><br>1⚡ 技能<br>2(3)×3+3(4)燃烧</td>
<td><img src="NinjaMod/images/card_portraits/rashomon.png" width="130"><br><b>多重罗生门</b><br>2⚡ 技能<br>抽3(4)·攻牌×9格挡</td>
<td><img src="NinjaMod/images/card_portraits/soul_chase.png" width="130"><br><b>追魂</b><br>3⚡ 技能·消耗<br>打出消耗飞刀+抽2(3)</td>
</tr>
<tr align="center">
<td><img src="NinjaMod/images/card_portraits/crane_shield.png" width="130"><br><b>守鹤之盾</b><br>2(1)⚡ 技能·消耗<br>已损失HP=格挡</td>
<td><img src="NinjaMod/images/card_portraits/earth_talisman.png" width="130"><br><b>土忍：土护符</b><br>1(0)⚡ 技能·消耗<br>消耗堆牌数=格挡</td>
<td><img src="NinjaMod/images/card_portraits/light_snow.png" width="130"><br><b>细雪</b><br>0⚡ 攻击<br>1×6段+2(3)回复</td>
<td><img src="NinjaMod/images/card_portraits/lihuo.png" width="130"><br><b>火忍：离火符</b><br>0⚡ 技能<br>5(8) 燃烧单体</td>
<td><img src="NinjaMod/images/card_portraits/musashi_advancing_fountain.png" width="130"><br><b>武藏：前进喷泉</b><br>1⚡ 技能·消耗<br>4回复+下回合1(2)能</td>
</tr>
<tr align="center">
<td><img src="NinjaMod/images/card_portraits/musashi_enmei_style.png" width="130"><br><b>武藏：圆明流</b><br>1(0)⚡ 能力<br>打出武藏牌回血</td>
<td><img src="NinjaMod/images/card_portraits/musashi_godspeed.png" width="130"><br><b>武藏：神速</b><br>0⚡ 技能·消耗<br>3(5)伤+8(11)挡+抽1</td>
<td><img src="NinjaMod/images/card_portraits/musashi_swift_triangle.png" width="130"><br><b>武藏：迅光三角剑</b><br>1⚡ 技能<br>11(15)伤+3(4)敏捷</td>
<td><img src="NinjaMod/images/card_portraits/soul_reap.png" width="130"><br><b>索命</b><br>2⚡ 技能<br>移除8(12)流血·回血</td>
</tr>
</table>

### Rare（稀有）

<table>
<tr align="center">
<td><img src="NinjaMod/images/card_portraits/shadow_pierce.png" width="130"><br><b>影心刺</b><br>0⚡ 攻击 · 静默<br>9(14) 伤+5(6)流血</td>
<td><img src="NinjaMod/images/card_portraits/blade_edge.png" width="130"><br><b>锄刃</b><br>2(1)⚡ 能力·消耗<br>飞刀/手里剑永久-1费</td>
<td><img src="NinjaMod/images/card_portraits/stealth_art.png" width="130"><br><b>隐身法</b><br>2(1)⚡ 能力<br>活力1+隐身3</td>
<td><img src="NinjaMod/images/card_portraits/seppuku.png" width="130"><br><b>切腹</b><br>X⚡ 技能<br>失2X血·得X能牌力</td>
<td><img src="NinjaMod/images/card_portraits/eight_techniques.png" width="130"><br><b>忍者八法</b><br>1⚡ 技能<br>全属性+1·飞刀+1</td>
</tr>
<tr align="center">
<td><img src="NinjaMod/images/card_portraits/susanoo.png" width="130"><br><b>须佐能乎</b><br>3⚡ 攻击<br>7(9)×6段+每段1流血</td>
<td><img src="NinjaMod/images/card_portraits/absolute_defense.png" width="130"><br><b>回天：绝对防御</b><br>2⚡ 技能<br>20(30)挡+4(6)回复</td>
<td><img src="NinjaMod/images/card_portraits/shadow_clone.png" width="130"><br><b>影分身</b><br>3(2)⚡ 技能<br>2回合复制+减伤40%</td>
<td><img src="NinjaMod/images/card_portraits/burning_heart.png" width="130"><br><b>火忍：燃心</b><br>X⚡ 技能<br>选消耗K张·K×3燃烧AOE</td>
<td><img src="NinjaMod/images/card_portraits/afterimage_art.png" width="130"><br><b>残影术</b><br>2(1)⚡ 能力<br>攻击→生成半伤复制</td>
</tr>
<tr align="center">
<td><img src="NinjaMod/images/card_portraits/musashi_inheritance.png" width="130"><br><b>武藏：承袭</b><br>3(2)⚡ 技能·消耗<br>13挡+神速/空明/刺</td>
<td><img src="NinjaMod/images/card_portraits/musashi_seven_star.png" width="130"><br><b>武藏：七星光芒斩</b><br>2⚡ 技能<br>7×7+升级斩杀</td>
<td><img src="NinjaMod/images/card_portraits/musashi_two_heavens.png" width="130"><br><b>武藏：二天一流</b><br>3(2)⚡ 技能<br>16×2+流血燃烧→眩晕</td>
<td><img src="NinjaMod/images/card_portraits/musashi_void_slash.png" width="130"><br><b>武藏：空明斩</b><br>0⚡ 攻击·消耗<br>15(21)伤+1(2)抵挡</td>
</tr>
</table>

> **贴图命名规则**：`images/card_portraits/<snake_case>.png`（如 `KatanaArt` → `katana_art.png`），大图在 `big/` 子目录。注入手里剑复用 `shuriken.png`。

---

## 自定义机制速查

| 关键词 | 类型 | 说明 |
|---|---|---|
| 流血 (Bleed) | Debuff | 受未被格挡的攻击伤害时，额外失去等量生命 |
| 燃烧 (Burning) | Debuff | 下回合开始时受到等量无法格挡伤害后移除；可引爆（×2） |
| 抵挡 (Resist) | Buff | 每次受攻击伤害前减免等量数值（格挡计算之前） |
| 静默 (Silence) | 卡牌属性 | 打出后不破除隐身（黄字显示，可悬停查看） |
| 隐身 (Stealth) | Buff | 敌人无法攻击你；攻击后失去（静默牌除外）；每回合-1 |
| 燃烧追加 (Burning Infusion) | 卡牌属性 | 攻击命中后额外施加对应层数燃烧 |
| 淬火 (Quenching) | Buff | 本回合攻击命中时附加等量燃烧；回合结束移除 |
| 火盾 (Flame Shield) | Buff | 受击时对攻击者施加等量燃烧 |
| 影分身 (Shadow Clone) | Buff | 非影分身卡×2结算 + 受击-40% + 荆棘反击 |
| 圆明 (Enmei) | Buff | 打出武藏牌时回复等量生命（可叠加） |
| 残影 (Afterimage) | Buff | 打出攻击牌→生成N张0费半伤复制到弃牌堆 |
| 免疫负面 (Debuff Immunity) | Buff | 免疫Debuff，持续回合递减 |

---

## How the custom mechanics are implemented (verified against the installed BaseLib/game API)

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

## Architecture notes

- **Localization** is in-code (BaseLib-recommended), language-aware (zh/eng).
- **Custom powers**: 15 total — `BleedPower`, `BurningPower`, `ResistPower`, `StealthPower`, `ShadowClonePower`, `DebuffImmunityPower`, `FlameShieldPower`, `QuenchingPower`, `YataMirrorPower`, `KujiProtectionPower`, `ProwlPower`, `AfterimagePower`, `EnmeiPower`, `BladeEdgePower`.
- **`HasSilence`** (静默) is a virtual bool on `NinjaModCard` — cards with this attribute do not break Stealth when played. Override as `=> IsUpgraded` for upgrade-granted silence. Hover tip auto-generated.
- **X-cost cards**: `HasEnergyCostX => true` with base cost 0. Use `ResolveEnergyXValue()` in `OnPlay`.

## 贴图命名

`images/card_portraits/<snake_case>.png` + `big/<snake_case>.png`。如 `KatanaArt` → `katana_art.png`。注入手里剑复用 `shuriken.png`。类名→蛇形规则由 `StringExtensions.cs` 处理。

---

## Repository notes

- `local.props` / `Directory.Build.props` are gitignored (machine-specific paths).
- `bin/`, `obj/`, `.godot/`, `*.pck`, `*.pdb`, logs, temp/ are gitignored EXCEPT `dist/NinjaMod/` (committed as release payload).
- `dist/NinjaMod/` contains the latest DLL+JSON+PCK for friend install: copy this folder into `<game>\mods\`.
- No copyrighted game assets or DLLs committed.

