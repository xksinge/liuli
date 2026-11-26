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
PanelWindow.cs    玻璃面板（M4）
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
- 自动隐藏（M2）：`dockAutoHide` 默认关闭；全局 55ms 轮询鼠标，底部 6 逻辑像素热区唤出，离开约 650ms 收起；窗口向下滑出，170ms `1-(1-t)^3` ease-out，动画目标可变、可中途反向；隐藏位置以屏幕底边为准，任务栏露出时也能完全推出屏幕。
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
- 悬停 140ms 检测、进入即展开；离开 900ms 收起；1s 定时刷新时间，2s 刷新读数；不做逐帧动画。
- 不抢焦点；点击空白或齿轮打开设置菜单；M1 快捷动作为面板/桌面图标/设置占位。

## 8. 配色

- `Palette` 从 `Backdrop.Source` 或壁纸缩略图提取主色；HSL 限制饱和度/亮度，生成：
  `Accent`、`GlassTint`、`TextMain`、`TextSub`、`TextFaint`、`EdgeHi/Lo`、`Hover`。
- 模式：`auto`（跟随壁纸）/ `preset`（6 套）/ `custom`。
- 对比度：按玻璃合成底色估算，主文字 ≥ 4.5:1；不达标自动切明暗。
- 只写 `Theme` token；窗口不直接取色。换壁纸/切主题时重算一次，≤ 500ms。

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
