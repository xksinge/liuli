# 设计说明 · Liuli V2（GlassBoard 功能对齐）

- 状态：Draft，随 M1 实现校正
- 日期：2026-11-26
- 关联：Issue #33、`docs/REQUIREMENTS.md`
- 原则：新外壳、旧功能封存；复用玻璃与底层能力；每步可回滚。

## 1. 目标与约束

V2 不再以“看板”为主结构，而是由 Dock、灵动岛、玻璃面板、桌面组件、壁纸与配色组成的桌面层。

约束：

- 单 exe、零外部 DLL、C# 5 语法、.NET Framework 4.x。
- 不替换 Explorer、不要求管理员权限、不做整屏截屏玻璃。
- 旧 `boards.json` 与收纳目录只读保留，不迁移、不删除。
- 1080p < 80MB，4K/多屏 < 120MB，闲置 CPU < 1%。

## 2. 与 v1 的关系

- `Program` 切换到新的 `V2App`；旧 `App`、`BoardWindow`、`BoardPaint` 保留编译，但不创建窗口、不暴露入口。
- 复用：`Backdrop`、`Glass`、`Theme`、`Native`、`Util`、`Log`、`Paths`、`Weather`、`Stats`、`Icons`（仅图标缓存/绘制）。
- 不复用：旧 `Workspace`/`Board` 作为主数据模型；旧控制条交互。
- 配置独立：`bin\data\shell.json`，首次启动生成默认；损坏时回退默认并备份 `.broken-*`。

## 3. 模块划分（首版）

```
V2App.cs          新控制器：启动、托盘、热键、定时器、窗口集合
ShellStore.cs     shell.json 读写、默认值、原子替换、迁移
V2Window.cs       分层窗口基类：ULW、无焦点、DPI、渲染入口、命中区
DockWindow.cs     底部 Dock：默认项、固定、启动、移除、自动隐藏、避让
IslandWindow.cs   顶部灵动岛：收起/展开、时间/天气/系统、快捷动作
PanelWindow.cs    玻璃面板：自由摆放、缩放、内容宿主
Widgets.cs        时钟/天气/系统组件绘制
Taskbar.cs        任务栏自动隐藏/恢复、完全隐藏（实验）
Palette.cs        壁纸取色、预设、对比度、Theme token 注入
Monitors.cs       单屏/多屏、工作区、DPI、热插拔
SettingsV2.cs     设置中心（分模块）
V2Menu.cs         玻璃右键菜单（可复用 MenuForm）
```

## 4. 数据模型（shell.json）

```json
{
  "app": "Liuli",
  "version": 2,
  "theme": {
    "mode": "auto",
    "preset": "mist",
    "accent": "",
    "glass": 100,
    "blur": 62,
    "radius": 22,
    "dim": 36,
    "textScale": 1.0
  },
  "dock": {
    "enabled": true,
    "autoHide": false,
    "iconSize": 44,
    "items": [
      { "type": "system", "id": "thispc" },
      { "type": "system", "id": "recycle" },
      { "type": "app", "path": "C:\\Windows\\explorer.exe", "label": "文件资源管理器" }
    ]
  },
  "island": {
    "enabled": true,
    "showWeather": true,
    "showStats": true,
    "width": 360,
    "offsetY": 8
  },
  "panels": [],
  "screens": { "mode": "primary" },
  "desktop": { "hideIcons": true, "restoreOnExit": true }
}
```

- `items[].type`：`system` / `app` / `folder` / `file`。
- `system.id`：`thispc`、`recycle`、`userfiles`、`network`、`controlpanel`。
- 缺字段用默认值；未知字段忽略；保存写 `.tmp` 后替换。

## 5. 窗口与 DPI

- 所有窗口 `WS_EX_LAYERED | TOOLWINDOW | NOACTIVATE | TOPMOST`，`ShowWithoutActivation`。
- 画布用 32bppPArgb；`UpdateLayeredWindow` 每帧推送。
- 尺寸以 96 DPI 逻辑单位定义，窗口按 `GetDpiForWindow` 计算 `scale`；首版仅主屏，后续每窗独立。
- 收到 `WM_DPICHANGED` / `WM_DISPLAYCHANGE`：重算尺寸、位置、画布，限制在工作区。
- 透明区域点击穿透由分层窗口 alpha 决定；命中区在 `HitTest` 中显式判断。

## 6. Dock

- 默认组合：系统入口（此电脑、回收站、用户文件夹）+ 自带应用（资源管理器、设置、Edge、记事本、计算器）；缺失跳过。
- 布局：左侧系统段 → 分隔线 → 固定应用段 → 弹性空白 → 右侧快捷动作（新建面板/设置/隐藏）。
- 交互：左键启动；拖入固定（COPY effect，不移动源文件）；右键取消固定；`P1` 拖拽排序。
- 自动隐藏：底部 8px 热区，`Timer` 检测鼠标；进入唤出，离开 600ms 收起；动画 ≤ 200ms。
- 任务栏避让：读取 `ABM_GETSTATE`；任务栏唤出时 Dock 上移任务栏高度或淡出；任务栏收起时回底。
- 图标：`Icons` 取 256px Shell 图标；开关控制是否绘制快捷方式箭头。

## 7. 灵动岛

- 状态机：`Collapsed` / `Expanded`。
- 收起宽 230、高 40；展开宽 360、高 104（逻辑像素，按 DPI 换算）；固定在主屏顶部居中。
- 收起显示时间 + 天气摘要；展开显示日期、天气详情、CPU/内存/磁盘、三个快捷动作。
- 悬停 150ms 展开；离开 1s 收起；1s 定时刷新时间与读数，不逐帧动画。
- 不抢焦点；点击空白打开设置；快捷按钮用 `Glass.Button`。

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
