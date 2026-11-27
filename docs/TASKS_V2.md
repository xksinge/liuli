# V2 任务清单

- 关联：Issue #33、`docs/DESIGN_V2.md`
- 规则：每个任务一个提交或一组小提交；完成勾选前必须有验证记录。
- 当前聚焦：M4 玻璃面板（Issue #52）已通过 PR #53 合入 `main`（`49de477`）；REQ-032 主题快照与 v2 集中设置面板已通过 PR #55 合入 `main`（`0dc1a6a`），Issue #54 已关闭。M3 灵动岛配置已通过 PR #51 合入 `main`（`4091f8f`）。

## Phase 0 · 需求与设计（已完成）

- [x] **TASK-001** [REQ-001–105] 需求基线、封存清单、设计说明、开发流程 [验证：文档评审 + Draft PR #34]
- [x] **TASK-002** [REQ-001] 旧 v1.6 原型隔离到 `experiment/v1.6-dock-island`，不进入 v2 [验证：分支隔离、工作区干净]
- [x] **TASK-003** [REQ-001] 需求分支 `docs/v2-requirements` 推送并开 Draft PR [验证：PR #34]

## Phase 1 · M1 新外壳

**目标**：启动只见 Dock、灵动岛；旧看板入口不可达；任务栏自动隐藏可设可恢复；主屏 100%/125% 不裁切。

### M1 实现状态（2026-11-26）

- 已提交：`3be11bb`、`62ce47c`、`7282f77`、`87c92b7`、`0bbc294`、`92eab42`；点击反馈 `a7b7b3e`；高清图标 `f3f5a1b`；Draft PR #36。
- 已验证：`pwsh tools/build.ps1` 编译成功（199 KB）；`bin\Liuli.exe --selftest` 读取 `bin\data\shell.json`，默认 8 项，任务栏 `ABS_AUTOHIDE=1`，图标探针 `explorer 256x256`。
- 未验证：真机 UI 启动、截图、125% DPI、Dock 点击/拖入/右键移除、任务栏开关恢复、点击反馈动态手感、`.lnk` 实际显示。
- 已生成离屏预览 `docs/screens/v2/m1-preview.png` 与按压态对照 `m1-preview-pressed.png` / `m1-feedback-compare.png`；不是桌面真机截图，仅用于材质/布局/按压态初评。
- 点击反馈：Dock 项与右侧按钮、灵动岛快捷按钮统一为按下 0.94 缩放 + 按压底色，释放回弹 + accent 波纹；静态预览可见，真机手感仍待验证。
- 高清图标：Dock 应用走 `SHGetImageList(SHIL_JUMBO)` + `IImageList.GetIcon`，探针确认 256×256；系统入口仍用矢量图标；`.lnk` 箭头由 Shell 图标源决定，未做全局箭头开关。
- M1/M2 已于 2026-11-26 真机验收并经 PR #49 合入 `main`；下列任务仍按各自验收标准勾选，未验证项保留未勾选。

- [ ] **TASK-010** [REQ-002] `ShellStore` + `shell.json` 默认配置、原子保存、坏文件回退
  - 实现：`ShellConfig`；默认 8 项；原子写；损坏文件回退 `.bad-*`。
  - 验证：首次生成、默认 8 项、非对象 JSON 回退（生成 `.bad-*` 并重建）已通过；删除文件后重建未单独测。
- [ ] **TASK-011** [REQ-090/091] `V2Window` 分层窗口基类：ULW、无焦点、画布、DPI、命中区
  - 实现：`V2Window`；Per-Monitor 句柄 DPI；画布/玻璃/按钮/标签。
  - 验证：编译通过；显示/隐藏、透明区穿透、125% 未真机验证。
- [ ] **TASK-012** [REQ-001/004] `V2App` 启动、单实例、托盘、热键、退出清理；`Program` 切到 V2
  - 验证：旧看板不出现；托盘可退出；`Ctrl+Alt+L` 隐藏/恢复；无残留进程。
- [ ] **TASK-013** [REQ-005/007] `Taskbar` 自动隐藏设置/恢复；Dock 底部避让
  - 验证：`ABM_GETSTATE` 前后对比；任务栏唤出时 Dock 上移；退出恢复原状态。
- [ ] **TASK-014** [REQ-010–013/017/018] `DockWindow` 默认组合、系统入口、启动、右键移除、拖入固定
  - 验证：8 项默认；点击资源管理器/此电脑/回收站；拖入 `.lnk` 后持久化；重启仍在。
- [ ] **TASK-015** [REQ-019] Dock 图标高清（256px）与去脚标开关
  - 实现：`Icons.Draw` 源尺寸参数 + `Icons.Get` 按尺寸缓存；`SHIL_JUMBO` 256px 提取，失败回退 32px；Dock 应用调用 256px。
  - 验证：`--selftest` 探针 `explorer 256x256`；`.lnk` 实际显示、125% 清晰度未真机验证。
- [ ] **TASK-016** [REQ-020–022/025] `IslandWindow` 收起/展开、时间/天气/系统、快捷动作
  - 验证：悬停 150ms 展开；离开 1s 收起；时间跨分钟刷新；不抢焦点。
- [ ] **TASK-017** [REQ-003/004] 设置/编辑期间隐藏 Dock 与灵动岛，关闭恢复
  - 验证：打开设置两者消失；关闭后按配置恢复。
- [ ] **TASK-018** [REQ-100–104] M1 视觉评审：同角度截图、六项清单
  - 验证：`docs/screens/v2/m1-*.png`；P0 视觉缺陷为 0。
- [ ] **TASK-019** [REQ-001/097] M1 性能与稳定性测量
  - 验证：启动 ≤3s；空闲内存/CPU 记录；连续开关 20 次不崩。

## Phase 2 · M2 Dock 完整

### M2 实现状态（2026-11-26）

- 集成：`feat/v2-m2-integration` → PR #49 已 MERGED（`c222f91`）；逐级 PR #36/#38/#40/#42/#44/#46/#48 已关闭。
- Issue #37 / `feat/v2-dock-autohide` / PR #38：自动隐藏与热区唤出。
- Issue #39 / `feat/v2-dock-presets` / PR #40：三档尺寸预设。
- Issue #41 / `feat/v2-dock-running` / PR #42：运行指示点。
- Issue #43 / `feat/v2-dock-reorder` / PR #44：拖拽排序。
- Issue #45 / `feat/v2-dock-hover` / PR #46：悬停放大。
- Issue #47 / `fix/v2-dock-taskbar-offset` / PR #48：任务栏常显位置修复。
- 已实现：`shell.json` 增加 `dockAutoHide`（默认 false）；托盘/快捷菜单增加切换；Dock 全局 55ms 鼠标轮询、底部 6px 热区、离开 650ms 收起；170ms cubic ease-out 滑入/滑出；隐藏位置按屏幕底边计算，任务栏露出时也能完全推出；切换开关、显示全部、重排时刷新状态。
- 已验证：构建通过（205 KB）；`--selftest` 读取 `dockAutoHide=False`；临时写入 `true` 后读取为 `True`，已恢复原文件；离屏 `--preview` 打印隐藏态 `dockHidden top=1442`，`screenBottom=1440`，窗口完全移出屏幕。
- 未验证：真机热区唤出、650ms 收起、动画中途反向、任务栏唤出共存、125% DPI。需要用真实鼠标事件验收。
- 三档尺寸（Issue #39）：`dockPreset=small/standard/large` 对应 0.86/1.0/1.14 整体缩放；托盘/快捷菜单循环切换；离屏尺寸 small 494×86、standard 576×100、large 658×114，对照图 `docs/screens/v2/m2-dock-presets.png`。
- 运行指示（Issue #41）：每 2s 一次进程枚举，缓存目标名；仅 app 项绘制 accent 小圆点；预览检测到 `explorer`、`SystemSettings`，对照图 `docs/screens/v2/m2-dock-running.png`。
- 拖拽排序（Issue #43）：左键移动超过 8px 进入排序，拖拽时显示 accent 插入竖线，松手写回 `shell.json`；`m2-dock-reorder.png` 展示开头/中间/末尾三个插入位。
- 悬停放大（Issue #45）：指针进入 Dock 项后 140ms ease-out 放大到 1.08，按下仍为 0.94；`m2-dock-hover.png` 对照普通态/第 4 项悬停态。
- 真机验收（2026-11-26）：Dock 悬停、拖拽到末尾、自动隐藏 `top=1444→1344→1444`、任务栏常显 `top=1296`、灵动岛展开/收起通过；三档菜单循环 `576/658/494` 并重启保持；记事本圆点 3s 内出现/消失；按压态截图。留档 `m2-real-*.png`。
- 任务栏常显 bug（Issue #47）：修复 `WorkingArea` 与 `Taskbar.VisibleHeight` 双重扣除，Dock top 1248→1296。

- [ ] **TASK-020** [REQ-014] Dock 自动隐藏与唤出动画（含任务栏共存）
  - 实现：`DockForm` 的 `autoTimer` / `slideTimer` / `SlideTo` / `HiddenOffset`；配置与菜单切换。
  - 验证：真机底部热区唤出、650ms 收起、任务栏常显位置通过；动画中途反向未验证。
- [ ] **TASK-021** [REQ-015] 图标尺寸/间距/整体缩放三档预设
  - 实现：`dockPreset` 配置 + `DockForm.PresetScale()` 乘到 `UiScale`；托盘/快捷菜单循环切换；`--preview` 三档对照。
  - 验证：配置写入 `large` 后自检读取正确；真机菜单循环 `standard→large→small→standard`、尺寸 `576×100 / 658×114 / 494×86`、`large` 重启保持均通过；1366×768 未验证。
- [ ] **TASK-022** [REQ-016] 运行指示点（P1）
  - 实现：`DockItem.ProcessNames()` + `DockForm.RefreshRunning()` 2s 缓存枚举；仅 app 项绘制 accent 圆点；`.lnk` 暂不解析。
  - 验证：真机 explorer/设置圆点可见；记事本启动 3s 圆点出现、关闭 3s 消失；2s 枚举 CPU 长时占用未测。
- [ ] **TASK-023** [REQ-013] 拖拽排序（P1）
  - 实现：`DockForm` 的 `dragging/dragFrom/dragTo` 与 `InsertSlot()`；`V2Shell.MoveDockItem()` 写回配置；插入位 accent 竖线。
  - 验证：真机拖资源管理器到末尾、配置顺序变化并立即重排通过；外部文件拖入未回归验证。
- [ ] **TASK-024** [REQ-019] GlassBoard Dock 实测核对：动画、分组、滚轮、右侧小按钮
  - 已补：悬停 1.08 放大（Issue #45）；真机核对悬停/拖拽/自动隐藏/任务栏共存；分组、滚轮、右侧小按钮仍未核对。
- [ ] **TASK-025** [REQ-010–019] Dock 验收截图与回归
  - 已留档：`m2-real-hover.png`、`m2-real-drag.png`、`m2-real-taskbar.png`、`m2-real-running-on/off.png`、`m2-real-pressed.png`；125% DPI / 外部文件拖入仍待验。

## Phase 3 · M3 灵动岛

- Issue #50（P0）：尺寸/偏移/动画/内容开关可配置；分支 `feat/v3-island-config`，已通过 PR #51 合入 `main`。
- [x] **TASK-032** [REQ-025] 宽度/高度/偏移/动画/内容可配置
  - 实现：`islandWidth` / `islandHeight` / `islandTop` / `islandAnimMs` / `islandQuick`；设置子菜单与恢复默认；收起尺寸派生。
  - 验证：档位循环、即时应用、重启保持、越界收敛、动画 0/180/260ms、真机截图均已记录；菜单项改为 MouseUp 触发并延迟打开子菜单。
- [x] **TASK-030** [REQ-023] SMTC 媒体显示与控制（Issue #95 / PR #96）
  - 实现：`Media.cs` 后台 MTA 线程 0.9s 轮询 `GlobalSystemMediaTransportControlsSessionManager`；读取当前会话标题/艺术家/播放状态/时间轴；元数据或播放状态变化才回 UI 线程，进度由既有 1s 展开重绘刷新；控制调用 `TrySkipPreviousAsync / TryTogglePlayPauseAsync / TrySkipNextAsync`。
  - 灵动岛：有媒体时展开高度 +40，新增媒体行（标题、歌手/应用名、进度条、上一首/播放暂停/下一首）；收起态显示 `♪ 标题`；无媒体时整行与额外高度都不出现。`islandMedia` 默认开，设置页可关。
  - 构建：`build.ps1` 引用 `System.Runtime.WindowsRuntime.dll` 与系统 `WinMetadata/*.winmd`；SMTC 不可用时后台线程记 warning，窗口不崩。
  - 验证：本地 `MediaPlayer` 播放 8s WAV 循环，展开窗 404×176（无媒体 404×136）；点暂停图标由 Pause 变 Play，播放状态同步；点上一首/下一首不崩；截图 `island-media.png`、`settings-island-media.png`。
  - 未验证：Spotify/浏览器真实会话标题+歌手、会话 1s 内切换、125%/150% DPI、Win10 1903 无 SMTC 情形。
- [ ] **TASK-031** [REQ-024] 通知摘要（P1，M7）

## Phase 4 · M4 面板与主题

- Issue #52（P0）：玻璃面板 + 四类内容；分支 `feat/v4-panel`，已通过 PR #53 合入 `main`。
- [x] **TASK-040** [REQ-030/031] `PanelForm` 玻璃面板 + 四类内容
  - 实现：`PanelForm` / `PanelContent`；多面板、拖动/缩放、置顶/置底、右键切换内容；`panels[]` 持久化；Dock/灵动岛/菜单入口。
  - 验证：构建 225 KB；2 面板创建、拖动/缩放、四类内容切换、位置尺寸不变、重启保持、透明区 `HTTRANSPARENT`、置底被记事本覆盖均真机通过；截图 `docs/screens/v4/`。
- [x] **TASK-041** [REQ-032] 主题保存/加载/删除（Issue #54）
  - 实现：`ThemeSnapshot` 快照（Dock / 灵动岛 / 玻璃 / 明暗 / 壁纸 / 面板深拷贝）；`Themes[]` + `activeTheme`；最多 6 个、同名覆盖、超限淘汰最早、二次确认删除；加载恢复壁纸与整套外壳。
  - 设置面板：新增 `V2SettingsForm` 900×720 五页（主题/面板/Dock/灵动岛/关于），M5 扩展配色/壁纸/桌面/收纳、M6 加入系统后为十页；主题库加载/覆盖/删除、滑杆松手保存、磨砂松手重烘、180ms 状态刷新；Dock/灵动岛齿轮、托盘、热键统一入口。
  - 验证：构建 `bin\Liuli.exe` 259 KB；主题保存/加载/删除、覆盖、7 个主题触发 6 上限淘汰、面板 6 上限、Dock/灵动岛显隐、上下文入口、坏配置收敛均已记录；截图 `docs/screens/v5/`。
- [ ] **TASK-042** [REQ-033] 导入推荐配置（P1）
- [ ] **TASK-043** [REQ-034] 统一 Theme token 审查

## Phase 5 · M5 组件、壁纸、配色、桌面图标

- [x] **TASK-050** [REQ-040–042] 时钟/天气/系统组件（Issue #64 / PR #65，已合入 `5a1b1a2`）
  - 实现：时钟面板右键新增“时间制：24/12 小时”“显示秒：开/关”，配置 `clock24` / `clockSeconds`；`PanelContent.ClockText` 统一 24/12 小时与秒格式；12 小时制顶部显示上午/下午。
  - 天气：`Weather.Offline` 时在“城市 · 天气”后追加“离线缓存”，无缓存仍显示“天气离线”。
  - 刷新：`PanelForm.ApplyTickInterval` 秒显示 1 秒、系统 2 秒、其它 5 秒；`V2Shell` 系统读数保持 2 秒采样上限。
  - 验证：构建 `bin\Liuli.exe` 295.5 KB；测试面板 12 小时+秒显示 `01:10:34`、24 小时+秒 `01:11:41`、24 小时无秒，右键菜单切换落盘 `clock24=true/false`、`clockSeconds=true/false`；秒显示两帧哈希不同，系统面板两帧哈希不同；截图 `docs/screens/v8/clock-12h-seconds.png`、`clock-24h-seconds.png`、`clock-24h.png`、`panel-stats.png`。
  - 未验证：真实断网时的天气缓存标记、12 小时制跨中午、125% DPI、小面板极窄宽度。

- [x] **TASK-051** [REQ-050–052] 壁纸读取/切换/压暗（Issue #59 / PR #60）
  - 实现：设置新增“壁纸”页；当前壁纸预览（文件解码，不截屏）、文件名/路径/系统样式、选择本地图片、0–80% 压暗、恢复首次换图前的系统壁纸；`ShellConfig` 增加 `wallpaperDim` / `wallpaperOrig` / `wallpaperOrigStyle` / `wallpaperOrigTile`，`ThemeSnapshot` 同步保存压暗。
  - 玻璃底：`Backdrop.ApplyDim` 在磨砂后、取平均色前压暗；`ApplyVisualSettings` / `LoadTheme` 二次 `ApplyTheme`，让新壁纸的明暗与自动强调色进入重烘。
  - 验证：构建 `bin\Liuli.exe` 268.5 KB；7 页导航、预览、路径/样式、滑杆、选择/恢复按钮真机显示；原生对话框实际选中测试图并写 `img0` 原壁纸记录；压暗 0→41→79 落盘并日志 `mean=37,41,44 light=0`；恢复按钮写回原图、清记录、刷新预览；主题快照 `WallpaperDim=42` 保存/加载/`ApplyTo` 反射通过；截图 `docs/screens/v7/`。
  - 未验证：WebP 真文件、125% DPI、多屏、换壁纸 5 秒联动、主题快照含压暗的真机 UI 回归。
- [x] **TASK-052** [REQ-053–057] 配色系统（Issue #56，第一刀；完整形态 Issue #57）
  - 实现：设置新增“配色”页；`accentMode=auto/preset/custom`、`accentPreset 0–5`、`accentCustom #RRGGBB`；`ShellConfig.ResolveAccent` 统一写入 `Theme.Accent`；6 套预设（雾蓝/青瓷/紫雾/暖砂/绯红/石墨）+ `ColorDialog` 自定义；主题快照保存/恢复配色三项。
  - 验证：6 预设逐一点击 `accentPreset` 0→5；自定义取色 `#FF80FF` 全局生效、取消不改；坏值收敛 `auto/5/#7EB2FF`；主题保存后加载恢复 `custom/#FF80FF`；重启保持；截图 `docs/screens/v6/`。
  - 剩余：REQ-054 每套预设独立玻璃染色/描边/文字层级、REQ-055 描边强度与文字对比滑杆、REQ-056 全组件 token 审查、REQ-057 重算耗时实测。
- [x] **TASK-053** [REQ-080–082] 桌面图标默认隐藏、系统图标桌面开关、退出恢复（Issue #61 / PR #62）
  - 实现：设置新增“桌面”页；启动默认隐藏全部桌面图标（只对 `SysListView32` 做 `ShowWindow`，运行期状态）；五个系统图标独立开关写 `HideDesktopIcons\NewStartPanel` + `ClassicStartMenu`，`SHChangeNotify` 刷新；`desktop-icons.runtime` 标记异常退出校准；正常 `Quit` 恢复启动前可见状态。
  - 验证：启动 `hidden=True visible=False`；切显示 `visible=True`；此电脑/回收站/用户文件夹/网络逐个 69↔70；隐藏状态菜单退出后图标恢复、标记删除、配置保留；Explorer 重启后已显示系统图标保持（项数 70）。截图 `docs/screens/v7/settings-desktop.png`。
  - 限制：Windows 11 25H2 桌面不再绘制“控制面板”图标，开关写偏好但无可见项（项数 69→69）；Win10 1903、125% DPI、多屏未真机回归。
  - 异常退出：隐藏态强杀后 marker=`1`；重启日志“上次异常退出，已校准桌面图标 visible=True”，随后重新隐藏并重建 marker。
- [x] **TASK-054** [REQ-085/086] 桌面收纳单面板（Issue #16 / PR #63，已合入 `dc2ef27`）
  - 实现：新增 `CollectorState` / `CollectorForm`；设置由八页增至九页，新“收纳”页；`ShellConfig` 增加 `collectorOn` / `collectorAutoScan` / `collectorX/Y/W/H` / `collectorGroup` / `collectorItems[]`；入口只存原路径，不移动/复制/删除桌面文件；扫描用户桌面+公共桌面，按 `groups.txt`/扩展名/文件夹自动分组；2 秒轮询；双击打开、面板内拖动换组、拖入加入口、右键移出；拖出到资源管理器只允许 Copy。
  - 验证：构建 `bin\Liuli.exe` 293 KB；首次启动 `桌面入口同步 69 项，已写入 shell.json`；面板 620×430 位于 `1900,96`，分组标签/18 项每页/4 页/滚动翻页显示正常；点“文档”组显示 11 项；新建 `Liuli收纳测试.txt` 3 秒内变 70 项并出现在文档组；删除到回收站后 3 秒内回到 69 项；双击打开 Notepad；拖动到“应用”组落盘 `group=应用` 且原文件未移动；重启后 `collectorGroup=文档`、位置/尺寸保持；收纳页开关可关闭/重开面板。截图 `docs/screens/v8/collector-panel.png`、`docs/screens/v8/settings-collector.png`。
  - 限制：`REQ-044` 的“绑定文件夹组件”未实现，保留为后续独立能力；125% DPI、多屏、>500 项、Explorer 重启后原生图标再隐藏未回归；二期多盒子未做。


## Phase 6 · M6 设置与系统

- [ ] **TASK-060** [REQ-060] `SettingsV2` 分模块页面
  - M4 已完成主题/面板/Dock/灵动岛/关于五页与统一入口，M5 加入配色、壁纸、桌面、收纳四页，M6 加入系统页后为十页；快捷键、日志诊断、文字四档已补，剩“系统页拆独立页/自定义键位”未定。
- [x] **TASK-061** [REQ-061] 开机自启（Issue #66 / PR #67，已合入 `812661f`）
  - 实现：设置第 10 页“系统”（索引 8）；`ShellConfig.AutoStart` 读写 `autoStart`；`AutoStart` 只写 `HKCU\Software\Microsoft\Windows\CurrentVersion\Run\Liuli`，值带引号指向当前 exe；`V2Shell.Boot` 每次按配置写入/删除，移动 exe 后下次启动自动修正。
  - 验证：构建 `bin\Liuli.exe` 297 KB；系统页开关开启后 Run=`"E:\meihuaDesktop\bin\Liuli.exe"`，关闭后值不存在；把 exe+shell.json 复制到 `_scratch\move-test` 启动，Run 更新为临时路径；恢复主 exe 后 Run 回到主路径；`--selftest` 输出 `autoStart=False run=`；截图 `docs/screens/v8/settings-system.png`。
  - 未验证：Windows 重启后的实际自启行为、非管理员受限账户、多用户切换、杀软拦截。

- [x] **TASK-062** [REQ-062/092] HiDPI + 文字四档（Issue #74 / PR #75，已合入 `47204c5`）
  - 实现：`ShellConfig.TextSize` 读写 `textSize`（0 小 / 1 标准 / 2 大 / 3 特大），对应 0.9 / 1.0 / 1.15 / 1.3；`Theme.TextScale` 统一倍率，`V2Window.DpiUiScale`、设置 `S`、面板/收纳 body、Dock/灵动岛一起缩放，避免只放大字体导致固定行高裁切。
  - 入口：设置标题栏常驻“文字 小/标准/大/特大”四枚胶囊，任意页可切换；`V2Shell.SetTextSize` 写入配置并即时重排 Dock、灵动岛、面板、收纳和设置，Toast + 日志 `textsize`。
  - 验证：构建 `bin\Liuli.exe` 302 KB；0.9 设置 810×648、1.0 900×720、1.3 1170×936；收纳 0.9 580×409、1.0 644×454、1.3 838×591；Dock 1.3 窗口 754×131、玻璃主体底边 1382，任务栏顶 1392，仍留 10px；重启保持 `textSize=3`，切回标准后 `textSize=1`；截图 `settings-text-standard.png`、`settings-text-large.png`、`collector-text-large.png`、`dock-text-large.png`。
  - 未验证：125%/150% DPI 真机、1.15 档截图、多屏、面板实窗截图（当前 `panels=[]`）、0.9 档最小字号 8px 后的极长文件名换行。
- [x] **TASK-063** [REQ-063] 热键查看与重置（Issue #68 / PR #69，已合入 `ea378c0`）
  - 实现：系统页新增“快捷键”区，显示 Ctrl+Alt+L 显示/隐藏、Ctrl+Alt+G 打开设置的注册状态；新增“重新注册快捷键”，先 `UnregisterHotKey` 再注册默认组合，结果写日志并刷新页面。
  - 验证：构建 `bin\Liuli.exe` 298 KB；Ctrl+Alt+G 可正常打开设置，系统页显示“已注册”；Ctrl+Alt+L 在当前环境被其它进程占用，显示“失败”且启动日志为 `toggle=fail settings=ok`；点击重新注册后日志再次记录同样状态，页面不崩、无裁切；截图 `docs/screens/v8/settings-hotkeys.png`。
  - 未验证：关闭占用 Ctrl+Alt+L 的程序后能否恢复注册；非管理员账户；与系统/第三方热键的冲突枚举。
- [x] **TASK-064** [REQ-064] 日志诊断与摘要（Issue #70 / PR #71，已合入 `9851d21`）
  - 实现：系统页新增“日志诊断”区，显示日志路径、当前大小、`liuli.log.1` 大小与 512 KB 轮转说明；新增“打开日志目录”“复制诊断摘要”。`Log.Diagnostic` 输出版本、DPI、日志/配置路径、大小和最近 16 行。
  - 验证：构建 `bin\Liuli.exe` 299.5 KB；系统页文字不裁切；复制摘要成功写入剪贴板；打开日志目录定位 `bin\data\logs`；注入 581,641 B 后触发写入，`liuli.log` 变 52 B、`liuli.log.1` 为 581,641 B；测试后恢复原日志并删除 `.1`；截图 `docs/screens/v8/settings-logs.png`。
  - 未验证：连续多次轮转、多进程同时写日志、日志目录无权限、剪贴板被其它程序占用。

- [x] **TASK-065** [REQ-007/010] Dock 动态避让任务栏（Issue #72 / PR #73，已合入 `8b00fb2`）
  - 实现：`Taskbar.Revealed()` 区分自动隐藏的 2px 边缘和真正露出；`DockForm.BaseTopY` 在自动隐藏且露出时按 `VisibleHeight` 上移，缩回后回底；`TickAutoHide` 每 55ms 顺带定位；切换任务栏自动隐藏/恢复时立即刷新 Dock。
  - 验证：构建 `bin\Liuli.exe` 300 KB；任务栏露出 top=1392、高 48 时，Dock 窗口 1296..1396、玻璃主体底边 1384，距任务栏顶 8px；关闭 Dock 自动隐藏后截图 `docs/screens/v8/dock-above-taskbar.png` 确认不遮挡。
  - 未验证：任务栏缩回后的回底连续实测、连续扫底、任务栏换边、125% DPI。

## Phase 7 · 多屏与 P1

- [x] **TASK-070a** [REQ-093–095] 多屏模式第一步：每屏 Dock 与显示器变化重排（Issue #76 / PR #77，已合入 `e4628c6`）
  - 实现：`ShellConfig.DisplayMode` 读写 `displayMode`，收敛为 `primary/all`；`DockForm` 改为按 `Screen` 构造，新增 `SetScreen` / `RelayoutForScreen`，所有底部基准、热区、隐藏偏移按所在屏计算；`V2Shell` 新增 `ExtraDocks`、`LiveDocks/OpenExtraDocks/CloseExtraDocks/RelayoutDocks/HideAllDocks`；`all` 时为每块非主屏各建一套 Dock，共享图标配置，灵动岛仍在主屏；设置 Dock 页新增“多屏模式：仅主屏 / 所有屏”。
  - 重排：`V2Host` 收到 `WM_DISPLAYCHANGE (0x007E)` 后走 `OnDisplayChanged()`，重建副屏 Dock、重排主 Dock、灵动岛、面板、收纳和设置；副屏 Dock 只在本屏底部热区唤出，避免一块屏触发全部 Dock。
  - 越界收回：`PanelForm.ApplyBounds` / `CollectorForm.ApplyBounds` 改为 `Screen.FromPoint` 取窗口所在屏工作区，先夹左上再夹右下。
  - 验证：构建 `bin\Liuli.exe` 306.5 KB；本机 `AllScreens` 实为 2 屏（DISPLAY1 `0,0 2560×1440`、DISPLAY2 `-2560,0 2560×1440`）。`all` 后日志 `screens=2 extraDocks=1`，副屏 Dock 落在 DISPLAY2 底部居中，主屏 Dock 同时隐藏；鼠标只在主屏底部时主屏 Dock `y=1296`、副屏 `y=1444`，只在副屏底部时副屏 `y=1344`、主屏 `y=1442`。发送 `WM_DISPLAYCHANGE` 后日志 `reflow screens=2 extraDocks=1`，进程不崩、副屏 Dock 重建一套；切回“仅主屏”后副屏 Dock 关闭，截图 `settings-dock-multiscreen-primary.png`、`settings-dock-multiscreen-all.png`、`dock-two-screens.png`。
  - 未验证：真实拔插显示器 / 改分辨率（当前用 `WM_DISPLAYCHANGE` 消息模拟）；副屏任务栏自动隐藏与多实例（`Shell_SecondaryTrayWnd`）；双屏不同 DPI；跨屏玻璃切片；每屏独立布局。
- [x] **TASK-070b** [REQ-095/096] 副屏任务栏感知与跨屏玻璃连续（Issue #80 / PR #81，已合入 `7cc91ca`）
  - 实现：`Taskbar.Handles()` 枚举 `Shell_TrayWnd` + 全部 `Shell_SecondaryTrayWnd`，`Visible/VisibleHeight/Revealed` 增加 `Screen` 重载；`HideAll/ShowAll` 遍历全部实例。`DockForm` 保存 `targetScreen`，按本屏任务栏露出状态上移。多屏 `Wallpaper.Render` 强制 `DrawSpan` 连续虚拟屏，`Backdrop` 日志 `span=1 screens=2`。
  - 诊断：`--selftest` 输出屏幕/虚拟屏/任务栏实例；新增 `--taskbar-cycle` 隐藏并恢复全部任务栏。
  - 验证：构建 `bin\Liuli.exe` 329.5 KB；副屏任务栏 `-2560,1392..1440` 时副屏 Dock `-1568,1296..-992,1396`、主屏 Dock 1444 收起；反向主屏任务栏露出时主屏 Dock 1296、副屏 Dock 1444；`--taskbar-cycle` hide/show 均 `n=2`，两屏可见状态 False→True；跨屏面板拼接截图 `panel-seam.png` 无缝。
  - 未验证：双屏不同 DPI、真实拔插/改分辨率、任务栏左/右边缘、Explorer 重启后副任务栏重建。
- [x] **TASK-070c** [REQ-096] 每屏独立 Dock 图标布局（Issue #82 / PR #83，已合入 `0cc8cb7`）
  - 配置：`ShellConfig` 新增 `dockPerScreen=false` 与 `dockProfiles[]`；profile 为 `{ screen: DeviceName, items: dockItem[] }`，最多 4 个、每个最多 16 项；抽出 `ReadDockItems/WriteDockItems` 复用读写。
  - 选择：关闭开关时全部屏共享 `dockItems`；主屏始终共享；副屏第一次排序/移除/固定时深拷贝共享列表为该屏独立 profile，日志 `[dock] 建立副屏独立布局 \\.\DISPLAY2 n=8`。
  - 操作落屏：`DockItemsFor(Screen)` 决定该屏列表；`PinPaths/RemoveDockItem/MoveDockItem` 增加 `Screen` 重载；Dock 拖入、右键移除、拖拽排序和 Hub 右键固定都传操作所在屏。
  - 设置：Dock 页多屏模式之后新增“每屏独立布局”开关；关闭时保留 profiles、只停用，`ResetDock` 才清理；`PinPaths` 成功后立即重排 Dock，修掉 Hub 固定后宽度不刷新。
  - 验证：构建 `bin\Liuli.exe` 332 KB；副屏 Hub 固定 7-Zip 后共享 `dockItems=8`、`DISPLAY2` profile `items=9`，副屏 Dock `576→625`（`-1593,1442..-968,1542`），主屏 `992,1442..1568,1542` 不变；重启后 profile 与宽度保持；关闭开关 profiles 保留，两块 Dock 立即回共享项数；截图 `dock-secondary-profile.png`、`settings-dock-per-screen.png`。
  - 未验证：双屏不同 DPI、真实拔插/改分辨率、显示器改名/主副互换后 profile 归属。
- [x] **TASK-070d** [REQ-096] 每屏 Dock 布局收口修复（Issue #84 / PR #85，已合入 `80c2b3a`）
  - 保留：`SetDockPerScreen(false)` 不再清空 `dockProfiles`，只停用并重排；重新开启后副屏恢复原布局。设置文案改为“关闭后各屏布局仍保留”。
  - 去重/上限：加载与保存都按 `screen` 忽略大小写去重，最多 4 条；达到上限时 `EnsureDockItems` 返回 null 并记 warning，`PinPaths/Remove/Move` 安全退出，避免误改共享列表。
  - 开关可见性：仅主屏模式下 Dock 页不再显示可点的每屏开关，改为“切到所有屏后可开启；关闭只停用，不删除布局”。
  - 图标：`.lnk` 先解析 `TargetPath`，再取目标图标；对 256 画布按不透明内容裁边铺满槽位，修复 7-Zip 图标缩成小点。
  - 验证：构建 `bin\Liuli.exe` 333.5 KB；关闭开关 `profiles=1 kept=1`，副屏 576→625，再开启回 576；6 条手改 profile 启动后只保留 `DISPLAY2/3/4/5` 四条且 DISPLAY2 取第一条 8 项；仅主屏页无开关；7-Zip 图标铺满 44px 槽位；截图 `dock-shortcut-icon.png`、`settings-dock-primary-info.png`。
  - 未验证：`IconContent` 对超大图标集的启动耗时、显示器改名后的 profile 归属；面板/收纳每屏组合未纳入。
- [x] **TASK-070e** [REQ-096/097] Dock 收纳入口、任务栏完全隐藏与 DPI 尺寸回正（Issue #86/#87 / PR #88）
  - 收纳：Dock 右侧动作按钮扩为 4 个（桌面收纳 / 应用面板 / 控制中心 / 设置），新增 `Glyph.Tray`；`OpenCollectorFromDock(Screen)` 在该 Dock 所在屏打开收纳，`CollectorForm.AnchorToDock` 居中贴 Dock 上方；启动/开关恢复时按保存坐标所在屏重新锚定。
  - 任务栏：设置 Dock 页新增“完全隐藏任务栏”开关，`SetTaskbarFullHide` 对主/副 `Shell_TrayWnd` 全部 `HideAll`，2 秒守护重试；关闭或退出 `Quit` 恢复。开关运行期有效，下次启动若发现残留 `taskbarFullHide=true` 强制恢复并复位，避免异常退出后任务栏消失。
  - DPI：`V2Window` 处理 `WM_DPICHANGED`，120ms 合并后全局重排 Dock、灵动岛、收纳、Hub、设置和面板；收纳尺寸/保存改用 `UiScale`（DPI × 文字倍率），修复 125% → 150% → 100% 后尺寸漂移。
  - 验证：构建 `bin\Liuli.exe` 336 KB；主屏 Dock 4 按钮 617 宽，点收纳后窗口 `958,846..1602,1300`、底边距 Dock 8px；`all` 模式下点副屏收纳日志 `screen=\\.\DISPLAY2`，窗口移到 `-1602,846..-958,1300`；任务栏开关 hide `n=2 any=False`、恢复 `n=2 any=True`；残留 `taskbarFullHide=true` 启动日志 `W 上次完全隐藏未正常恢复` 且配置复位；截图 `dock-collector-button.png`、`collector-on-secondary-dock.png`、`settings-taskbar-full-hide.png`、`taskbar-full-hide-dock.png`。
  - 未验证：真机 125% / 150% / 100% 往返后的尺寸恢复（当前代码已处理 `WM_DPICHANGED`，需用户复测）；主副屏互换/双屏变单屏后的 profile 归属。
- [x] **TASK-070f** [REQ-097] Dock 收纳按钮切换、点外部收起与位置保持（Issue #89 / PR #90）
  - 切换：`ToggleCollectorFromDock` 在收纳可见时直接 `HideCollector`，隐藏时才打开并锚定；点 Dock 按钮不会被外部点击判定提前收起。收起同时写 `collectorOn=false`，重启不自动弹回。
  - 外部点击：Dock 55ms 定时器检测左键按下上升沿，`CloseCollectorIfOutside` 排除收纳窗口与 Dock 收纳按钮矩形；点桌面/其它窗口收起，点收纳内部不收起。
  - 位置：启动、DPI 重排、设置开启不再强制重锚；从 Dock 打开时把锚定位置 `SaveBounds` 写回 `CollectorX/Y`，之后沿用，尊重用户拖动后的位置。
  - 验证：构建 `bin\Liuli.exe` 336.5 KB；收纳可见时点 Dock 按钮日志 `收纳已收起`、窗口 `vis=False`、`collectorOn=false`；再点打开 `958,846..1602,1300`，`CollectorX/Y=970/858`，`collectorOn=true`；点桌面 `1000,500` 日志 `收纳已收起`；点收纳内部不收起；`displayMode=all` 下点副屏 Dock 按钮日志 `screen=\\.\DISPLAY2`、窗口 `-1602,846..-958,1300`，再点收起；把 `CollectorX/Y` 改为 `1625/735` 后启动窗口 `1613,723..2257,1177`，不再强制回 Dock 上方；截图 `collector-toggle-anchored.png`。
  - 未验证：125%/150% DPI 下外部点击与位置保持。
- [x] **TASK-070g** [REQ-098] Dock 常驻任务栏上方、位置设置与任务栏防闪（Issue #91 / PR #92）
  - 位置：`dockAlign` 保存 `left` / `center` / `right`；设置 Dock 页“Dock 位置”三档，切换即时重排。
  - 常驻：用户配置 `dockAutoHide=false`，Dock 不再触底弹出/离开收起；自动隐藏开关仍保留。
  - 防闪：`taskbarUpStable` 对任务栏露出/缩回做 300ms 稳定判断；任务栏露出时，鼠标在 Dock 横向范围内保持 Dock 显示，范围外不强行拉起。
  - 验证：居中 `971,1344..1588,1444`，靠左 `8,1344..625,1444`，靠右 `1935,1344..2552,1444`；设置页实时写回 `dockAlign`；截图 `settings-dock-position.png`。
- [x] **TASK-070h** [REQ-098] Dock 距底部、自由拖动/锁定、收纳固定跟随与面板锁定（Issue #93 / PR #94）
  - 距底部：`dockBottomGap` 默认 8，设置滑杆 0–160px；`BaseTopY` 按 `WorkArea` 底边/任务栏减该值。
  - 自由位置：横向默认居中；`dockLocked` 默认开，解锁后顶部 48×8 把手或空白处拖动，`dockCustomX/Y` 落库；回到居中按钮清坐标。
  - 收纳：`AnchorToDock` 读 Dock 实际 `Bounds.Top`，不再用 152 硬编码；启动、DPI、设置开启、Dock 拖动实时跟随；标题区禁拖。
  - 面板：`panelsLocked` 默认关；设置·面板页与右键菜单可锁定，锁定后拖动与缩放都不响应。
  - 验证：间距 8 时 Dock `971,1344..1588,1444`、收纳 `957,894..1601,1348`；间距 40 时 Dock `971,1312..1588,1412`、收纳 `957,862..1601,1316`；拖 Dock 到 `592,1180` 后收纳 `578,730..1222,1182`；截图 `dock-collector-above.png`、`settings-dock-gap-lock.png`。
  - 未验证：125%/150% DPI 下的拖动与收纳跟随；副屏自由拖动。
- [x] **TASK-070** [REQ-093–098] 多屏模式完整：每屏 Dock 定位、副屏任务栏避让、跨屏玻璃、每屏独立 Dock 图标布局、收纳入口与任务栏完全隐藏、收纳按钮切换与外部点击收起、Dock 常驻与位置设置、Dock 自由位置/锁定与收纳跟随（070a/070b/070c/070d/070e/070f/070g/070h 均已完成）
- [x] **TASK-071** [REQ-070/071] 应用面板、控制中心（Issue #78 / PR #79，已合入 `1c19d51`）
  - 实现：新增 `Apps.cs` 扫描用户/公共开始菜单 `.lnk`（递归 ≤3 层、最多 500 项，过滤卸载/帮助/readme/网站/Startup，名称去重排序，缓存 5 分钟）；新增 `ControlCenter.cs` 读 Core Audio 音量/静音、`NetworkInterface` 网络、`bthprops.cpl` 蓝牙、WMI 亮度，接口失败只记一次日志并隐藏对应卡片。
  - Hub：`HubForm` 为 620×460 玻璃面板，两个页签“应用 / 控制中心”；应用页 4×4 网格、16 项/页、搜索、分页、左键启动、右键固定、拖到 Dock 固定；控制页音量点击静音/滚轮 4% 步进、网络/蓝牙打开设置、亮度循环 25/50/75/100；仅控制页可见时 2 秒采样。
  - 入口：Dock 与灵动岛快捷动作改为应用面板 / 控制中心 / 设置；托盘菜单与自绘菜单新增两项；原“新建面板”保留在托盘与 Hub 内按钮；文字四档、显示器变化、`HideAll`、退出均同步 Hub。
  - 验证：构建 `bin\Liuli.exe` 327.5 KB，C#5 无新增 error；开始菜单 99 项、搜索 `edge` 1 项；右键固定 Edge `dockItems` 8→9；拖拽 7-Zip 到 Dock 日志 `[drop] OLE 放下 Dock`、8→9 且 Hub 自动收起；Hub `956,800 648×488`，底边 1288 距 Dock 顶 1296 为 8px；音量 100%、静音切换与滚轮 100→96→100 通过；蓝牙/亮度不可用自动隐藏；截图 `hub-apps.png`、`hub-search.png`、`hub-control.png`、`dock-hub-actions.png`。
  - 未验证：支持设备上的蓝牙卡片与亮度调节、125%/150% DPI、双屏不同 DPI、副屏唤出 Hub 的定位偏好、Dock 已满 24 项/跨屏拖拽。

## Phase 8 · 发布

- [ ] **TASK-080** [DoD] 全量验收、性能、截图、文档
- [ ] **TASK-081** [DoD] CHANGELOG、tag、Release、资产校验

## 验证记录格式

```text
任务：TASK-014
环境：Windows 11 26200 / 2560×1440@100%
步骤：...
结果：通过/失败
证据：docs/screens/v2/m1-dock.png、日志时间戳
未验证：...
```


