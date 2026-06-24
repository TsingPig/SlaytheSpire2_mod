# NinjaMod — ready-to-use package / 开箱即用的成品包

This folder is the **built, ready-to-install** Ninja mod (DLL + JSON + PCK with all art).
It is committed to the repo on purpose so you do **not** need a dev environment to play.

本文件夹是**已编译、可直接安装**的忍者 mod（包含 DLL + JSON + 所有贴图的 PCK），
特意提交到仓库，这样无需开发环境即可游玩。

## Install / 安装

1. Install **BaseLib** first (Steam Workshop, or the `Alchyr.Sts2.BaseLib` release).
   先安装 **BaseLib**（Steam 创意工坊，或 `Alchyr.Sts2.BaseLib` 发行版）。
2. Copy the whole `NinjaMod` folder from here into your game's mods folder:
   把这里的整个 `NinjaMod` 文件夹复制到游戏 mods 目录：
   ```
   <Steam>\steamapps\common\Slay the Spire 2\mods\NinjaMod\
   ```
   The folder must contain: `NinjaMod.dll`, `NinjaMod.json`, `NinjaMod.pck`.
   文件夹内需包含：`NinjaMod.dll`、`NinjaMod.json`、`NinjaMod.pck`。
3. Launch the game, enable **BaseLib** and **NinjaMod**, start a run and pick **Ninja / 忍者**.
   启动游戏，启用 **BaseLib** 与 **NinjaMod**，开始游戏并选择 **忍者**。

> If textures are missing, you copied the DLL/JSON but **not** `NinjaMod.pck` — the PCK holds all art.
> 如果贴图缺失，说明你只复制了 DLL/JSON 而**漏了** `NinjaMod.pck` —— 所有贴图都在 PCK 里。
