# 设计说明 · Liuli V2（GlassBoard 功能对齐）

- 状态：Confirmed；M1 已按实现校正
- 日期：2026-11-26
- 关联：Issue #33（已关闭）、Issue #35（M1 实现）
- 原则：新外壳、旧功能封存；复用玻璃与底层能力；每步可回滚。

## 1. 目标与约束

V2 不再以“看板”为主结构，而是由 Dock、灵动岛、玻璃面板、桌面组件、壁纸与配色组成的桌面层。

约束：

- 单 exe、零外部 DLL、C# 5 语法、.NET Framework 4.x。
- 不替换 Explorer、不要求管理员权限、不做整屏截屏玻璃。
- 旧 `boards.json` 与收纳目录只读保留，不迁移、不删除。
- 1080p < 80MB，4K/多屏 < 120MB，闲置 CPU < 1%。

## 2. 与 v1 的关系

- `Program` 切换到新的 `V2Shell`；旧 `App`、`BoardWindow`、`BoardPaint` 保留编译，但不创建窗口、不暴露入口。
- 复用：`Backdrop`、`Glass`、`Theme`、`Native`、`Util`、`Log`、`Paths`、`Weather`、`Stats`、`Icons`（仅图标缓存/绘制）。
- 不复用：旧 `Workspace`/`Board` 作为主数据模型；旧控制条交互。
- 配置独立：`bin\data\shell.json`，首次启动生成默认；损坏时回退默认并备份 `.broken-*`。

## 3. 模块划分（首版）

```
V2Shell.cs        新控制器：启动、托盘、热键、定时器、菜单、退出
V2Config.cs       ShellConfig / DockItem：shell.json 读写、默认值、原子替换
V2Window.cs       分层窗口基类：ULW、无焦点、DPI、画布、玻璃、按钮
DockForm.cs       底部 Dock：默认项、固定、启动、移除、任务栏避让
DockGlyphs.cs     系统入口矢量图标 + 设置/计算器自定义图标
IslandForm.cs     顶部灵动岛：收起/展开、时间/天气/系统、快捷动作
Taskbar.cs        任务栏自动隐藏/恢复（ABM_GETSTATE/ABM_SETSTATE）
PanelForm.cs       玻璃面板容器（M4）
PanelContent.cs    面板四类内容绘制（M4）
Widgets.cs        时钟/天气/系统组件（M5）
Palette.cs        壁纸取色、预设、对比度（M5）
Monitors.cs       单屏/多屏、工作区、DPI、热插拔（Phase 7）
SettingsV2.cs     设置中心（M6）
V2Menu.cs         玻璃右键菜单（M1 复用 MenuForm）
```

M1 实际命名：`V2Shell`、`ShellConfig`、`V2Window`、`DockForm`、`IslandForm`；后续文档中的 `V2App`/`DockWindow`/`IslandWindow` 均指这些实现类。

## 4. 数据模型（shell.json）

```json
{
  "version": 2,
  "dockOn": true,
  "dockAutoHide": false,
  "dockPreset": "standard",
  "islandOn": true,
  "islandWeather": true,
  "islandStats": true,
  "taskbarAutoHide": true,
  "taskbarOriginalAutoHide": true,
  "taskbarFullHide": false,
  "themeMode": "auto",
  "themePreset": "mist",
  "accentMode": "auto",
  "accentPreset": 0,
  "accentCustom": "#7EB2FF",
  "glassPct": 100,
  "blurPct": 62,
  "radius": 20,
  "desktopIconsHidden": true,
  "hideShellThisPc": true,
  "hideShellRecycle": true,
  "hideShellUserFiles": true,
  "hideShellNetwork": true,
  "hideShellControlPanel": true,
  "dockItems": [
    { "kind": "system", "id": "thispc", "label": "此电脑" },
    { "kind": "system", "id": "recycle", "label": "回收站" },
    { "kind": "app", "path": "C:\\Windows\\explorer.exe", "label": "资源管理器" }
  ]
}
```

- M1 采用**扁平字段**，便于 C# 5 下用现有 `Util.Json` 读写；M5 扩展主题/面板时再决定是否嵌套，迁移由 `version` 判断。
- `dockItems[].kind`：`system` / `app`；M1 只实现这两类。
- `system.id`：`thispc`、`recycle`、`userfiles`、`network`、`controlpanel`；默认 Dock 只用前三个。
- 缺字段用默认值；未知字段忽略；保存写 `.tmp` 后替换；坏 JSON 备份为 `.bad-yyyyMMdd-HHmmss`。

## 5. 窗口与 DPI

- 所有窗口 `WS_EX_LAYERED | TOOLWINDOW | NOACTIVATE | TOPMOST`，`ShowWithoutActivation`。
- 画布用 32bppPArgb；`UpdateLayeredWindow` 每帧推送。
- 尺寸以 96 DPI 逻辑单位定义，窗口按 `GetDpiForWindow` 计算 `scale`；M1 每个窗口按自身句柄 DPI 计算，主屏单屏。
- M1 处理 `WM_DISPLAYCHANGE` 重算位置；每窗 `WM_DPICHANGED` 与文字四档在 M6 补齐。
- 透明区域点击穿透由分层窗口 alpha 决定；命中区在窗口内显式判断。

## 6. Dock

- 默认组合：系统入口（此电脑、回收站、用户文件夹）+ 自带应用（资源管理器、设置、Edge、记事本、计算器）；缺失跳过。
- 布局：系统入口段 → 分隔线 → 固定应用段 → 右侧三个快捷动作（面板/桌面图标/设置），M1 不预留弹性空白。
- 交互：左键启动；拖入固定（COPY effect，不移动源文件）；右键取消固定；`P1` 拖拽排序。
- 自动隐藏（M2）：`dockAutoHide` 默认关闭；全局 55ms 轮询鼠标，底部 6 逻辑像素热区唤出，离开约 650ms 收起；窗口向下滑出，170ms `1-(1-t)^3` ease-out，动画目标可变、可中途反向；隐藏位置以屏幕底边为准，任务栏露出时也能完全推出屏幕。基础位置只读 `WorkingArea`，任务栏常显时已扣除其高度，不再二次减 `VisibleHeight`（Issue #47）。
- 尺寸预设（M2）：`dockPreset` 为 `small` / `standard` / `large`，整体缩放系数 0.86 / 1.0 / 1.14；缩放图标、间距、圆按钮、内边距、窗口与文字；切换后立即重排并持久化。
- 运行指示（M2）：每 2s 一次性枚举 `Process.GetProcesses()`，缓存目标进程名，不在绘制帧枚举；仅 `app` 项显示 accent 小圆点（约 4 逻辑像素），系统入口、URL 与 `.lnk` 暂不显示；检测到变化才重绘。
- 拖拽排序（M2，P1）：左键移动超过 8px 进入排序；按指针 X 计算 0..N 插入槽，绘制 accent 竖线；松手后 `V2Shell.MoveDockItem` 调整 `dockItems` 并保存；移动不超过 4px 仍按点击启动。
- 悬停放大（M2）：指针进入 Dock 项后 140ms cubic ease-out 放大到 1.08；按下 0.94 优先；拖拽时禁用；移出或拖拽开始立即恢复 1.0。
- 任务栏避让：M1 每 320ms 检测任务栏是否露出；露出则整体上移任务栏高度，收起回底；M2 的滑入/滑出只叠加在基础位置上，不改变任务栏避让判定。
- 图标：`Icons` 取 256px Shell 图标；开关控制是否绘制快捷方式箭头。

## 7. 灵动岛

- 状态机：`Collapsed` / `Expanded`。
- 收起宽 230、高 38；展开宽 352、高 106（逻辑像素，按 DPI 换算）；固定在主屏顶部居中。
- 收起显示时间 + 天气摘要；展开显示日期、天气详情、CPU/内存/磁盘、三个快捷动作。
- 悬停 140ms 检测、进入即展开；离开 900ms 收起；1s 定时刷新时间，2s 刷新读数。
- 不抢焦点；点击空白或齿轮打开设置菜单；M1 快捷动作为面板/桌面图标/设置占位。

### 7.1 M3 可配置（REQ-025，P0）

- `islandWidth`：展开态 body 宽度，360–520，默认 380。
- `islandHeight`：展开态 body 高度，100–180，默认 112。
- `islandTop`：顶部偏移，0–80，默认 6。
- `islandAnimMs`：展开/收起时长，0–400，默认 180；`0` 为立即切换。
- 收起尺寸派生：宽 `round(width * 0.63)` 夹 200–320，高 `round(height * 0.36)` 夹 34–54；窗口再加左右/上下各 12 逻辑像素阴影边距。
- 内容开关：沿用 `islandWeather`、`islandStats`，新增 `islandQuick`；关闭后不绘制对应区域。
- 动画：16ms 定时器按 `1-(1-t)^3` 插值窗口宽高并重新居中；过渡期间按目标状态绘制，尺寸变化由窗口裁剪呈现。配置为 0 时跳过定时器。
- 设置入口：灵动岛主菜单新增“灵动岛设置”，子菜单按档位循环宽度/高度/偏移/动画，并切换内容开关；每次改动 `ApplyConfig()` + `Save()`，提供“恢复默认”。
- 菜单项统一在 `MouseUp` 触发（按下只记录行号），子菜单由 140ms 定时器在父菜单关闭后打开，避免父菜单的按下/抬起事件命中刚出现的子菜单。
- 越界值在 `ShellConfig.Load` 与 `ApplyConfig` 双重夹取，坏值不会把窗口推到屏幕外。

### 7.2 P1（M7，不在 M3 验收）

- SMTC 媒体标题/艺术家/进度与播放控制（REQ-023）。
- Windows 通知摘要与忽略/清除（REQ-024）。
- M3 验收只覆盖 REQ-020–022、025。

### 7.3 M4 玻璃面板（REQ-030/031，P0）

- `PanelForm : V2Window`，窗口四周 12 逻辑像素阴影边距；玻璃主体用 `PaintGlass` + `Theme.Radius`，不另造材质参数。
- 配置 `panels[]`：`id`、`content`（`empty` / `clock` / `weather` / `stats`）、`x/y/w/h`（body 屏幕坐标与尺寸）、`topMost`；最多 6 个，W 220–720、H 120–520，越界收回当前工作区。
- 交互：按住玻璃主体拖动；右下角 22px 热区缩放；右键菜单切换内容、置顶/置底、新建、删除。拖动/缩放/切换后立即写 `shell.json`。
- 点击穿透：`WM_NCHITTEST` 在 body 外返回 `HTTRANSPARENT`、body 内返回 `HTCLIENT`，透明阴影不挡桌面图标。
- 内容与容器解耦：`PanelContent.Paint` 只读 `content` 与全局 `Weather/Stats`；切换内容不重建窗口，位置尺寸不变。
- 置顶/置底：`V2Window` 仅在 `TopMost=true` 时设置 `WS_EX_TOPMOST`；面板可降为普通 z 序，被普通窗口覆盖。
- 入口：Dock 左侧“+”快捷动作、灵动岛“+”快捷动作、托盘/主菜单“新建面板”；没有面板时默认空，不自动创建。
- 面板的拖拽/缩放/右键切换仍保留在面板本体；参数与批量管理集中到 v2 设置面板的“面板”页。

### 7.4 主题快照与 v2 设置面板（REQ-032，Issue #54）

- 主题快照存 `ShellConfig.Themes[]`，最多 6 个；每项含名称、保存时间、Dock 三项、灵动岛八项、玻璃五项（明暗/预设/浓度/磨砂/圆角）、配色三项（来源/预设序号/自定义色）、壁纸路径/样式/平铺与压暗、`Panels` 深拷贝。
- 名称统一 `CleanName`：去首尾空白、限 24 字、空名回“主题”；重名加 ` (2)` 后缀，最多 6 个，超过时保存第 7 个会淘汰最早一个。
- 保存/覆盖：`SaveTheme` 同名覆盖，`ActiveTheme` 指向当前快照；手动改玻璃、Dock、灵动岛、面板后立刻清空 `ActiveTheme`，避免把已偏离的状态误标成“当前”。
- 加载：`LoadTheme` 先应用快照，壁纸文件存在时 `Wallpaper.Set` + `Backdrop.Rebuild`，再重建面板、Dock、灵动岛；任一步失败只影响对应窗口，不改快照内容。
- 删除：二次确认（同一条目点两次），删除当前主题时同步清空 `activeTheme`；坏 JSON 由 `NormalizeThemes` 收敛，空/重复名自动修复。
- 设置面板 `V2SettingsForm`：900×720 自绘玻璃窗、九页导航、窗口可拖动；滑杆松手保存，磨砂/压暗松手才重烘壁纸，开关/内容切换即时写 `shell.json`。
- 页面职责：
  - 主题：玻璃浓度 20–140、磨砂 10–100、圆角 10–30、自动/深色/浅色、保存当前主题（内嵌原生 TextBox 命名）、恢复外观默认、主题库加载/覆盖/删除。
  - 配色：强调色来源三选一（跟随壁纸 / 预设 / 自定义）、6 个预设色卡、自定义取色（`ColorDialog`，取消不改）、当前配色说明。
  - 面板：最多 6 个、新建、逐面板内容 chip（空容器/时钟/天气/系统）、置顶/置底、删除（二次确认）。
  - Dock：显示开关、自动隐藏、小/标准/大三档；图标拖拽/移除仍在 Dock 本体，设置页明确提示。
  - 灵动岛：显示开关、宽/高/顶部偏移/动画四个滑杆、天气/系统读数/快捷动作三个开关。
  - 壁纸：当前壁纸预览、文件名/路径/系统样式、0–80% 压暗、选择本地图片、恢复系统壁纸。
  - 桌面：全部桌面图标运行期隐藏；五个系统图标（此电脑/回收站/用户文件夹/网络/控制面板）独立开关。
  - 关于：版本、运行状态、快捷键、打开数据目录、关闭设置。
- 入口与上下文：Dock 齿轮 `OpenSettings(3)`、灵动岛齿轮 `OpenSettings(4)`、托盘双击/热键/菜单 `OpenSettings(0)`；已打开时只切换页，不重复创建窗口。
- 状态刷新：设置页内的主题库“当前”徽标由 `ActiveTheme` 驱动；滑杆/开关改完后 180ms 重建页面，避免徽标停留旧值，同时避开命名输入与拖动过程。
- 键盘：`Esc` 关闭，`←/→` 切页；命名框内 Enter 保存、Esc 取消。

### 7.5 壁纸页（REQ-050–052，Issue #59）

- 配置：`ShellConfig.WallpaperDim` 0–80、`WallpaperOrig` / `WallpaperOrigStyle` / `WallpaperOrigTile`；Load clamp，Save 落盘，`RestoreVisualDefaults` 只重置压暗、不清原壁纸记录。
- 预览：`EnsurePreview` 读文件字节 → GDI+ `Image.FromStream` 解码 → 760×428 cover 缩略图缓存在内存；路径不变不重复解码，`OnFormClosed` 释放。预览不截屏，不把窗口/图标烤进玻璃。
- 换图：`V2Shell.SetWallpaperImage` 校验文件存在；首次换图时捕获当前系统壁纸路径与样式；`Wallpaper.Set(path, 10, 0)` 写系统壁纸，保存配置后重烘 + Toast。
- 恢复：`V2Shell.RestoreSystemWallpaper` 校验原路径存在，按原 style/tile 写回，清空 `WallpaperOrig`，保存并重烘；设置页收到成功后立刻重建壁纸页，避免预览停留旧图。
- 压暗：`Backdrop.Rebuild` 在 Frost/Blur 后、`ImageFx.Average` 前调用 `ApplyDim`，用黑色 alpha 覆盖 `pct*255/100`；`RebuildFromFrame` 同样在取平均色前压暗。
- 重烘与 tone：`ApplyVisualSettings(true)` 先 `Config.ApplyTheme()` 再 `Backdrop.Rebuild()` 再 `Config.ApplyTheme()`；`LoadTheme` 也采取同样二次应用，确保压暗后的 mean/light/accent 进入同一轮绘制。`V2Shell.Boot` 在 `Backdrop.Start()` 前设置 `Theme.WallpaperDim`。
- 限制：WebP 走系统 WIC/GDI+，解码能力取决于系统组件；无效/不存在图片不写壁纸，保持当前壁纸。

### 7.6 桌面图标页（REQ-080–082，Issue #61）

- 全部图标隐藏：`Native.SetDesktopIconsVisible` 只对桌面 `SysListView32` 调 `ShowWindow`，不写注册表；属于运行期状态。
- 启动记录：`V2Shell.Boot` 在应用配置前记录 `desktopIconOriginalVisible`；隐藏成功后写 `data\desktop-icons.runtime`，退出时按原状态恢复并删除标记。
- 异常恢复：下次启动先读标记恢复原始可见状态，再重新应用配置；标记不写进 `shell.json`，避免把临时隐藏当永久偏好。
- 系统图标：`DesktopState` 维护五个 CLSID，写 `HideDesktopIcons\NewStartPanel` 与 `ClassicStartMenu`（1 隐藏 / 0 显示）；`SHChangeNotify(0x08000000)` 通知资源管理器刷新，不重启 Explorer。
- 退出语义：`Quit` 只在“配置仍要隐藏且确实是 Liuli 隐藏”时恢复原状态；用户切到显示后退出不再反向隐藏。系统图标开关属于显式偏好，退出后保持。
- 兼容：Windows 11 25H2 桌面已不绘制“控制面板”系统图标，开关仍写注册表/配置但无可见图标；Win10 1903 待回归。

### 7.7 桌面收纳面板（TASK-054，Issue #16）

- 产品路线：一期只做“单面板 + 分组标签”，不是多个酷呆式盒子；数据模型与二期多盒子共用。参考 GlassBoard 的 `Entry` 索引模型，只借鉴机制，不复制代码。
- 数据模型：`ShellConfig` 新增 `collectorOn` / `collectorAutoScan` / `collectorX/Y/W/H` / `collectorGroup` / `collectorItems[]`；`CollectorItem` 保存 `path/name/group/hidden/manual/folder`，`path` 永远指向原文件或文件夹，没有“收纳目录/移动后路径”。
- 同步：`CollectorState.Sync` 扫描用户桌面 + 公共桌面，按路径合并；新文件用 `Icons.GroupOf` 按扩展名/文件夹/`groups.txt` 分组；文件消失只移除入口，不删除磁盘文件。手动拖入的桌面外入口标记 `manual`，原件在就保留。
- 刷新：面板内 `Timer(2000)` 在 UI 线程轮询，满足“新/删/改 5 秒内可见”；不引入 `FileSystemWatcher` 跨线程回 UI 的复杂度。默认开启，可在收纳页关闭。
- 面板：`CollectorForm` 是独立分层窗口，620×430，可拖标题、可拖右下角缩放；顶部分组标签（全部/应用/文件夹/文档/图片/影音/压缩包/代码/其他），网格每页 18 项，滚轮翻页；双击入口打开原路径，右键可移出（只隐藏入口）或在资源管理器中显示。
- 分组：面板内拖动超过 8px 进入换组模式，松手只改 `CollectorItem.Group`，不产生文件移动；当前分组、面板位置/尺寸写回 `shell.json`。
- 安全拖出：入口拖出面板边界时提供 `DataFormats.FileDrop`，允许效果只给 `DragDropEffects.Copy`；资源管理器默认复制，绝不默认移动原件。
- 原生图标：收纳开启时配合桌面页“隐藏全部桌面图标”（运行期 `ShowWindow`）；面板 2 秒 tick 检测 Explorer 重启后原生图标是否重现并再次隐藏，不写系统“显示桌面图标”注册表偏好。
- 限制：一期不替换 Explorer 桌面视图，不做多个盒子；`REQ-044` 的“绑定文件夹组件”仍保留为后续独立组件；125% DPI、多屏、超大桌面（>500 项）未回归。


## 8. 配色

- 当前实现（M5 第一刀）：`ShellConfig.ApplyTheme` 调 `ResolveAccent`，按 `accentMode` 选择 `Backdrop.Accent`（auto）、`Palette.Get` 六套预设（preset）或 `ParseHexColor` 自定义色（custom），最后统一写入 `Theme.Accent`；Dock、灵动岛、面板、设置窗全部只读 `Theme.Accent`。
- `Palette` 预设：雾蓝 / 青瓷 / 紫雾 / 暖砂 / 绯红 / 石墨；浅色主题下自动压暗。`ColorDialog` 自定义结果序列化为 `#RRGGBB`，坏值/越界在 `Load`、`NormalizeThemes` 中收敛到 `#7EB2FF` 或 auto。
- 主题快照会保存 `accentMode/accentPreset/accentCustom`，加载时恢复并重绘；手动改配色会清空 `activeTheme`。
- 取色只在换壁纸、切预设、改自定义值时执行，不进入逐帧绘制；换壁纸后 `Backdrop.Tick` 检测变化并 `ApplyTheme` 一次。
- 仍待补齐（REQ-054/055 完整形态）：每套预设独立玻璃染色/描边/文字层级、描边强度与文字对比单独滑杆；当前这些仍由全局 `Theme` token 统一给出。

## 9. 任务栏

- 自动隐藏：`SHAppBarMessage(ABM_SETSTATE, ABS_AUTOHIDE)`；记录原状态（`ABM_GETSTATE`），可恢复。
- 完全隐藏（P1，实验）：`ShowWindow(FindWindow("Shell_TrayWnd"), SW_HIDE)`，退出/崩溃恢复；同时处理 `Shell_SecondaryTrayWnd`。
- 不做永久移除、不替换 Explorer。
- 崩溃恢复：启动时检查上次 `taskbarRestore` 标记，若任务栏仍隐藏则恢复。

## 10. 屏幕

- 首版默认仅主屏；`screens.mode = primary`。
- P1：`allDocks`（每屏 Dock + 主屏灵动岛）；每屏布局后置。
- 多屏坐标用虚拟屏物理坐标；玻璃切片按 `Backdrop.Virtual` 偏移。
- 热插拔：2 秒内重排，窗口收回主屏可见区。

## 11. 迁移与回滚

- 首次启动：若 `shell.json` 不存在，生成默认；不动 `boards.json`。
- 旧功能入口全部隐藏；旧数据保留。
- 回滚：`Program` 切回旧 `App.Boot()` 即回到 v1.5 行为；`shell.json` 不参与旧逻辑。
- 发布前保留 `v1.5.0` 二进制与 Release。

## 12. 测试计划（M1）

- 冷启动 ≤ 3s；旧看板不出现；托盘/热键可用。
- Dock 默认 8 项，缺失跳过；点击资源管理器可打开。
- 灵动岛收起/展开，1 分钟时间刷新。
- 任务栏自动隐藏可设可恢复；退出后恢复。
- 100%/125% 下 Dock、灵动岛不裁切。
- 构建日志、截图、内存/CPU 读数留档。

## 13. 风险

- 任务栏自动隐藏与 Dock 底部热区冲突：必须先做避让，否则互相遮挡。
- 分层窗口 + 多 DPI：首版只保证主屏；多屏单独里程碑。
- 旧 App 代码仍在编译：必须确保不会被入口引用，避免旧窗口复活。
- 灵动岛展开状态与鼠标离开判定：用定时器轮询，避免 resize 抖动。
