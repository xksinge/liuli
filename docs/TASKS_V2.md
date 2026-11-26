# V2 任务清单

- 关联：Issue #33、`docs/DESIGN_V2.md`
- 规则：每个任务一个提交或一组小提交；完成勾选前必须有验证记录。
- 当前聚焦：M3 灵动岛配置（Issue #50，分支 `feat/v3-island-config`）；REQ-025 已实现并真机验收，待提交/PR。M2 已通过集成 PR #49 合入 `main`（`c222f91`）。

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

- Issue #50（P0）：尺寸/偏移/动画/内容开关可配置；分支 `feat/v3-island-config`。
- [x] **TASK-032** [REQ-025] 宽度/高度/偏移/动画/内容可配置
  - 实现：`islandWidth` / `islandHeight` / `islandTop` / `islandAnimMs` / `islandQuick`；设置子菜单与恢复默认；收起尺寸派生。
  - 验证：档位循环、即时应用、重启保持、越界收敛、动画 0/180/260ms、真机截图均已记录；菜单项改为 MouseUp 触发并延迟打开子菜单。
- [ ] **TASK-030** [REQ-023] SMTC 媒体显示与控制（P1，M7）
- [ ] **TASK-031** [REQ-024] 通知摘要（P1，M7）

## Phase 4 · M4 面板与主题

- [ ] **TASK-040** [REQ-030/031] `PanelWindow` 玻璃面板 + 四类内容
- [ ] **TASK-041** [REQ-032] 主题保存/加载/删除
- [ ] **TASK-042** [REQ-033] 导入推荐配置（P1）
- [ ] **TASK-043** [REQ-034] 统一 Theme token 审查

## Phase 5 · M5 组件、壁纸、配色、桌面图标

- [ ] **TASK-050** [REQ-040–042] 时钟/天气/系统组件
- [ ] **TASK-051** [REQ-050–052] 壁纸读取/切换/压暗
- [ ] **TASK-052** [REQ-053–057] 配色系统：自动取色、6 预设、自定义、对比度
- [ ] **TASK-053** [REQ-080–082] 桌面图标默认隐藏、系统图标桌面开关、退出恢复
- [ ] **TASK-054** [REQ-044] 图标收纳组件（P1）

## Phase 6 · M6 设置与系统

- [ ] **TASK-060** [REQ-060] `SettingsV2` 分模块页面
- [ ] **TASK-061** [REQ-061] 开机自启
- [ ] **TASK-062** [REQ-062/092] HiDPI + 文字四档
- [ ] **TASK-063** [REQ-063] 热键配置/重置
- [ ] **TASK-064** [REQ-064] 日志与诊断

## Phase 7 · 多屏与 P1

- [ ] **TASK-070** [REQ-093–096] 多屏模式、每屏 Dock、任务栏多实例、跨屏玻璃
- [ ] **TASK-071** [REQ-070/071] 应用面板、控制中心（P1）

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


