# Mod Manager

这个仓库把所有可安装 mod 统一放在 `mods\` 目录下。Windows 11 玩家建议优先使用根目录的 `manage-mods-win11.bat` 添加或删除 mod。

## 使用脚本管理 mod

1. 双击运行 `manage-mods-win11.bat`。
2. 在弹出的文件夹选择窗口中选择《Slay the Spire 2》的安装目录。
   - 如果不知道位置，可以在 Steam 中找到 `Slay the Spire 2`，右键选择 `管理`，再选择 `浏览本地文件`。
3. 在命令行菜单中选择：
   - `1. Add mod`：从本仓库的 `mods\` 目录选择一个 mod 并安装到游戏目录。
   - `2. Remove mod`：从游戏目录的 `mods\` 文件夹选择一个已安装 mod 并删除。
   - `3. Change game folder`：重新选择并保存《Slay the Spire 2》安装目录。
   - `4. Exit`：退出脚本。

脚本会检查所选目录中是否存在 `data_sts2_windows_x86_64\sts2.dll`，以避免把 mod 安装到错误位置。

## Script Step Details

`manage-mods-win11.bat` 每一步会做这些事：

1. 切换到脚本所在目录。
   - 作用：保证脚本能从当前仓库的 `mods\` 目录读取可安装 mod。
   - 不会修改系统环境变量或依赖。

2. 打开 Windows 文件夹选择窗口。
   - 作用：让玩家选择《Slay the Spire 2》安装目录。
   - 提示玩家可以通过 `Steam -> Slay the Spire 2 -> Manage -> Browse local files` 找到目录。

3. 校验游戏目录。
   - 作用：检查所选目录下是否存在 `data_sts2_windows_x86_64\sts2.dll`。
   - 如果不存在，脚本会停止，不会安装或删除 mod。

4. 准备游戏 mod 目录。
   - 作用：把目标 mod 目录固定为 `<Slay the Spire 2>\mods`。
   - 如果 `<Slay the Spire 2>\mods` 不存在，脚本会创建它。

5. 显示主菜单。
   - `Add mod` 会列出本仓库 `mods\*` 下带有 `dist\<ModName>.dll` 和 `dist\<ModName>.json` 的 mod。
   - `Remove mod` 会列出玩家游戏目录 `<Slay the Spire 2>\mods\*` 下已有的 mod 文件夹。
   - `Change game folder` 会重新打开文件夹选择窗口，校验通过后覆盖 `mod-manager-config.yaml`。
   - `Exit` 只退出脚本。

6. 添加 mod。
   - 作用：把 `mods\<ModName>\dist\<ModName>.dll` 和 `mods\<ModName>\dist\<ModName>.json` 复制到 `<Slay the Spire 2>\mods\<ModName>\`。
   - 如果目标目录不存在，脚本会创建 `<Slay the Spire 2>\mods\<ModName>\`。
   - 如果目标目录里已有同名 `.dll` 或 `.json`，脚本会覆盖它们。
   - 脚本还会尝试删除目标 mod 目录里的旧 `.pck` 和旧同名配置文件，避免旧安装残留影响加载。

7. 添加 `VakuuRoomInjection` 时的额外处理。
   - 作用：如果 `%APPDATA%\SlayTheSpire2\mod_configs\VakuuRoomInjectionConfig.json` 不存在，脚本会创建配置目录并初始化配置文件。
   - 如果旧配置 `%APPDATA%\SlayTheSpire2\mod_configs\MapNodeChangerConfig.json` 存在，脚本会复制它作为新配置。
   - 如果旧配置不存在，脚本会从 `mods\VakuuRoomInjection\VakuuRoomInjectionConfig.json.example` 复制默认配置。
   - 脚本会删除旧安装目录 `<Slay the Spire 2>\mods\MapNodeChanger`，这是从旧 mod 名迁移到 `VakuuRoomInjection` 的清理步骤。

8. 删除 mod。
   - 作用：删除玩家在菜单中选择的 `<Slay the Spire 2>\mods\<SelectedMod>\` 文件夹。
   - 这是递归删除，会删除该 mod 文件夹里的所有文件。
   - 脚本只会删除菜单列出的游戏 `mods` 子目录，不会主动扫描或删除游戏目录外的文件。

## Potential Side Effects

脚本不会修改 PATH、注册表、Steam、.NET、Godot 或系统环境变量。

In short: the script does not change PATH, registry, Steam, .NET, Godot, or system environment variables.

## Config File

`manage-mods-win11.bat` stores the selected game folder in `mod-manager-config.yaml` next to the script:

```yaml
game_dir: C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2
```

On startup, the script checks this file first. If `game_dir` points to a valid Slay the Spire 2 folder, the script uses it and skips the folder picker. If the file is missing or the saved folder is invalid, the script opens the folder picker and writes a fresh `mod-manager-config.yaml` after a valid folder is selected.

Use the `Change game folder` menu option when the game is moved or when you selected the wrong folder. This option opens the folder picker again and overwrites `mod-manager-config.yaml` after validation succeeds.

脚本可能产生这些文件系统改动：

- 创建或覆盖 `mod-manager-config.yaml`，用于保存游戏目录。
- 创建 `<Slay the Spire 2>\mods`。
- 创建 `<Slay the Spire 2>\mods\<ModName>`。
- 覆盖 `<Slay the Spire 2>\mods\<ModName>\<ModName>.dll`。
- 覆盖 `<Slay the Spire 2>\mods\<ModName>\<ModName>.json`。
- 删除 `<Slay the Spire 2>\mods\<ModName>\<ModName>.pck`，如果它存在。
- 删除 `<Slay the Spire 2>\mods\<ModName>\<ModName>Config.json`，如果它存在。
- 删除玩家在 `Remove mod` 菜单里选择的整个 mod 文件夹。
- 安装 `VakuuRoomInjection` 时，可能创建 `%APPDATA%\SlayTheSpire2\mod_configs`。
- 安装 `VakuuRoomInjection` 时，可能创建 `%APPDATA%\SlayTheSpire2\mod_configs\VakuuRoomInjectionConfig.json`。
- 安装 `VakuuRoomInjection` 时，可能删除旧目录 `<Slay the Spire 2>\mods\MapNodeChanger`。

最需要注意的是删除功能：选择 `Remove mod` 后，脚本会直接删除所选 mod 文件夹。运行删除前请确认选中的 mod 名称正确。

## 当前包含的 mod

- `mods\VakuuRoomInjection`
- `mods\CardRewardEnchantments`

`VakuuRoomInjection` 会在进入房间时按配置概率注入指定 Ancient 事件。默认目标是 Vakuu，详细行为和开发说明见 `mods\VakuuRoomInjection\README.md`。

`CardRewardEnchantments` 会在获得卡牌奖励时按配置概率为可附魔卡牌添加随机附魔。默认概率为 100%，附魔关键词黑名单可在游戏内菜单中用复选框配置，详细行为和开发说明见 `mods\CardRewardEnchantments\README.md`。

## 手动安装说明

通常不需要手动安装。若确实需要，可以把某个 mod 的 `dist` 目录里的 `.dll` 和 `.json` 文件复制到：

```text
<Slay the Spire 2>\mods\<ModName>\
```

例如：

```text
mods\VakuuRoomInjection\dist\VakuuRoomInjection.dll
mods\VakuuRoomInjection\dist\VakuuRoomInjection.json
mods\CardRewardEnchantments\dist\CardRewardEnchantments.dll
mods\CardRewardEnchantments\dist\CardRewardEnchantments.json
```

复制到：

```text
<Slay the Spire 2>\mods\VakuuRoomInjection\
```

## 开发

构建 `VakuuRoomInjection`：

```powershell
powershell -ExecutionPolicy Bypass -File .\mods\VakuuRoomInjection\build.ps1
```

构建 `CardRewardEnchantments`：

```powershell
powershell -ExecutionPolicy Bypass -File .\mods\CardRewardEnchantments\build.ps1
```

运行验证：

```powershell
powershell -ExecutionPolicy Bypass -File .\tests\ManageModsScript.Tests.ps1
dotnet test .\tests\MapNodeChanger.Tests\MapNodeChanger.Tests.csproj
```
