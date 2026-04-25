# MapNodeChanger

主人，这是一个《Slay the Spire 2》地图节点修改 Mod 的起步工程。

它会在每一幕地图生成并进入时，遍历当前地图节点，并按 `MapNodeChangerConfig.json` 里的规则修改节点类型。第一版只修改 `PointType`，不改地图连线。

## 默认行为

默认配置会尝试：

- 把最多 2 个 `Unknown` 节点改成 `Shop`，概率 15%
- 把最多 2 个 `Unknown` 节点改成 `RestSite`，概率 10%
- 预留一个关闭状态的 `Unknown` -> `Elite` 规则

受保护节点类型不会被规则修改：`Boss` 和非标准地图节点不会被当作可修改目标。

## 房间类型

常用可配置值：

- `Unknown`
- `Monster`
- `Elite`
- `RestSite`
- `Treasure`
- `Shop`

## 构建前准备

需要安装：

- .NET 9.0 SDK
- Godot 4.5.1 Mono
- Slay the Spire 2

本机已经检测到游戏目录：

```text
C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2
```

但当前环境没有 .NET SDK，所以我暂时无法替主人完成本地编译。

## 构建

如果 `godot` 已经在 PATH 中：

```powershell
.\build.ps1
```

如果没有在 PATH 中，请指定 Godot 可执行文件：

```powershell
.\build.ps1 -Godot "C:\Path\To\Godot_v4.5.1-stable_mono_win64.exe"
```

构建成功后，产物会在：

```text
dist\
```

把 `dist` 里的文件放到游戏 `mods\MapNodeChanger\` 目录即可。

## 配置

首次启动后，Mod 会在 DLL 同目录创建：

```text
MapNodeChangerConfig.json
```

也可以先把 `MapNodeChangerConfig.json.example` 复制成 `MapNodeChangerConfig.json`，然后按需修改。

关键字段：

- `enabled`: 总开关
- `seed`: 设为 `0` 表示随机；设为固定数字可以让每幕修改结果稳定
- `skip_current_node`: 避免修改玩家当前所在节点
- `rules[].from`: 原节点类型
- `rules[].to`: 目标节点类型
- `rules[].chance`: 触发概率，范围 `0.0` 到 `1.0`
- `rules[].max_changes`: 单条规则最多修改几个节点；小于等于 `0` 表示不限制

## 下一步建议

第一版跑通后，建议继续加：

- 按楼层范围限制修改
- 按路线是否可达 Boss 限制修改
- 接入 ModConfig 做游戏内配置
- Patch 地图 UI，让已经打开的地图能立即刷新节点图标
