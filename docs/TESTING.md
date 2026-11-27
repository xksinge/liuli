# 测试记录

## v2.0 Phase 7 Dock 收纳入口、任务栏全隐藏与 DPI 回正（2026-11-27，Issue #86/#87 / PR #88）

- 环境：Windows 11 25H2，DISPLAY1 `0,0 2560×1440` 主屏、DISPLAY2 `-2560,0 2560×1440` 副屏，均 100% DPI；`displayMode=primary`、`dockAutoHide` 临时关闭用于截图。
- 构建：`bin\Liuli.exe` 336 KB；C#5 无新增 error，仅历史 warning。
- Dock：窗口 `971,1296..1588,1396`（617×100）；右侧 4 个 36px 动作按钮，顺序收纳 / 应用 / 控制 / 设置；截图 `dock-collector-button.png`。
- 主屏收纳：点击第 1 个按钮，日志 `[collector] Dock 打开收纳 screen=\\.\DISPLAY1`；收纳窗口 `958,846..1602,1300`（644×454），水平中心 1280，玻璃体底边距 Dock 顶 8px。
- 副屏收纳：临时 `displayMode=all`，副屏 Dock `-1589,1296..-972,1396`；点副屏收纳后日志 `screen=\\.\DISPLAY2`，窗口 `-1602,846..-958,1300`，水平中心 -1280；截图 `collector-on-secondary-dock.png`。
- 任务栏：设置 Dock 页开关点开，日志 `[taskbar] 完全隐藏=True n=2 any=False`；2 秒后 `Shell_TrayWnd` / `Shell_SecondaryTrayWnd` 均 `vis=False`，Dock 回到底部；再点关闭 `完全隐藏=False n=2 any=True`，两屏任务栏恢复；截图 `settings-taskbar-full-hide.png`、`taskbar-full-hide-dock.png`。
- 残留恢复：停止进程后手改 `taskbarFullHide=true` 再启动，日志 `W [taskbar] 上次完全隐藏未正常恢复，启动已恢复任务栏 n=2`，`shell.json` 自动复位 false。
- 收尾：恢复 `_scratch\shell.backup.json`，`displayMode=primary`、`dockAutoHide=true`、8 个 Dock 项、0 profiles，重启。
- 未验证：真机 125% / 150% / 100% 往返后的 Dock / 灵动岛尺寸恢复；主副屏互换 / 双屏变单屏后的 profile 归属；Explorer 在 2 秒守护间隔内重新唤出任务栏。

## v2.0 Phase 7 每屏 Dock 布局收口（2026-11-27，Issue #84 / PR #85）

- 环境：Windows 11 25H2，DISPLAY1 `0,0 2560×1440` 主屏、DISPLAY2 `-2560,0 2560×1440` 副屏，均 100% DPI。
- 构建：`bin\Liuli.exe` 333.5 KB；C#5 无新增 error。
- 保留布局：共享 `dockItems=9`、DISPLAY2 profile=8；副屏 Dock 576 宽、主屏 625。设置关闭开关后日志 `每屏独立布局=False profiles=1 kept=1`，`shell.json` profile 仍在，两块 Dock 都变 625；再开启 `True profiles=1`，副屏回 576。
- 去重/上限：手改 6 条 profile（DISPLAY2 8 项 + DISPLAY2 7 项 + DISPLAY3/4/5/6 各 8 项）；启动后副屏取第一条 8 项（576 宽），触发保存后只剩 `DISPLAY2/3/4/5` 四条，重复的 7 项条目被丢弃。
- 仅主屏：`displayMode=primary` 时 Dock 页“每屏独立布局”只显示说明，没有可点开关；profiles 仍为 1。
- 图标：共享 Dock 固定 7-Zip 后第 9 项图标铺满 44px 槽位，截图 `dock-shortcut-icon.png`；不再出现 10px 小点。
- 截图：`docs/screens/v8/dock-shortcut-icon.png`、`settings-dock-primary-info.png`、`settings-dock-per-screen.png`（新文案）。
- 测试后恢复 `shell.json`：`displayMode=primary`、8 个 Dock 项、0 profiles，并重启。
- 未验证：`IconContent` 在 99 项目录首次打开 Hub 的累计耗时、显示器改名/主副互换后的 profile 归属、面板/收纳每屏组合。

## v2.0 Phase 7 每屏独立 Dock 图标布局（2026-11-27，Issue #82 / PR #83）

- 环境：Windows 11 25H2，DISPLAY1 `0,0 2560×1440` 主屏、DISPLAY2 `-2560,0 2560×1440` 副屏，均 100% DPI；两块任务栏自动隐藏。
- 构建：`bin\Liuli.exe` 332 KB；C#5 无新增 error，只有历史 warning。
- 准备：备份 `_scratch\shell-before-profile.json`；`displayMode=all`、`dockPerScreen=true` 启动，日志 `screens=2 extraDocks=1`。
- 建立 profile：在副屏 Dock 打开 Hub，搜索 `7-zip`，右键首项固定。日志 `[dock] 建立副屏独立布局 \\.\DISPLAY2 n=8`、`[apps] 右键固定 ...7-Zip File Manager.lnk n=1`。
- 隔离：`shell.json` 共享 `dockItems=8`，`dockProfiles[0] screen=\\.\DISPLAY2 items=9`，tail `7-Zip File Manager`；主屏项数未变。
- 几何：副屏 Dock `-1593,1442..-968,1542`（625×100，收起态），主屏 Dock `992,1442..1568,1542`（576×100）；副屏唤出后第 9 槽 hover 标签为 `7-Zip File Manager`。
- 持久化：重启后 profile 仍 1 个、DISPLAY2 仍 9 项，副屏 Dock 仍 625 宽，主屏仍 576。
- 开关：设置 Dock 页 toggle 关闭，`dockPerScreen=false`、profiles=0，日志 `[dock] 每屏独立布局=False profiles=0`，两块 Dock 都回 8 项；再开启为 `True profiles=0`。
- 截图：`docs/screens/v8/dock-secondary-profile.png`、`settings-dock-per-screen.png`。
- 测试后恢复 `shell-before-profile.json`（`displayMode=primary`、8 项、0 profiles）并重启。
- 未验证：双屏不同 DPI、真实拔插/改分辨率、显示器改名/主副互换后的 profile 归属；7-Zip 图标在 Dock 内偏小，另记。

## v2.0 Phase 7 副屏任务栏与跨屏玻璃（2026-11-27，Issue #80 / PR #81）

- 环境：Windows 11 25H2，DISPLAY1 `0,0 2560×1440` 主屏、DISPLAY2 `-2560,0 2560×1440` 副屏，均 100% DPI；两块任务栏均开自动隐藏。
- 构建：`bin\Liuli.exe` 329.5 KB；C#5 无新增 error。
- 任务栏枚举：启动日志 `taskbars=TrayWnd=0,1392,2560,1440; SecondaryTrayWnd=-2560,1392,0,1440`；`--selftest` 输出 `screens=2 virtual={X=-2560,Y=0,Width=5120,Height=1440}` 与两条实例。
- 副屏：鼠标只压副屏底部，副屏任务栏 `-2560,1392..1440`；副屏 Dock `-1568,1296..-992,1396`，玻璃底边 1384，距任务栏顶 8px；主屏 Dock `992,1444..1568,1544` 保持收起。
- 主屏：鼠标只压主屏底部，主屏任务栏 `0,1392..2560,1440`；主屏 Dock `992,1296..1568,1396`；副屏 Dock `-1568,1444..-992,1544` 收起。主屏避让无回归。
- 全部隐藏/恢复：光标移离屏幕边缘后 `--taskbar-cycle` 输出 `hide n=2`，两屏 `visibleAfterHide=False`；`show n=2`，两屏 `visibleAfterShow=True`。
- 跨屏玻璃：时钟面板放在 `x=-200..164` 横跨虚拟屏接缝，左右屏分别抓取后拼接 `panel-seam.png`，面板玻璃在接缝处连续、无重复纹理（两屏同 DPI）。
- 截图：`docs/screens/v8/dock-secondary-taskbar.png`、`panel-seam.png`。
- 未验证：双屏不同 DPI、真实拔插显示器/改分辨率、任务栏左/右边缘、Explorer 重启后副任务栏重建。

## v2.0 Phase 7 应用面板与控制中心（2026-11-27，Issue #78 / PR #79）

- 环境：Windows 11 25H2，双屏 DISPLAY1 `0,0 2560×1440` 主屏、DISPLAY2 `-2560,0 2560×1440` 副屏，均 100% DPI。
- 构建：`bin\Liuli.exe` 327.5 KB；C#5 无新增 error，只有历史 warning。
- 应用目录：日志 `开始菜单入口 99 项`；首页 1/7 页，4×4 共 16 项，页脚“共 99 项”；搜索 `edge` 后 1/1 页、仅 Microsoft Edge。
- 右键固定：Edge 条目右键，日志 `[apps] 右键固定 ...Microsoft Edge.lnk n=1`，`shell.json` `dockItems` 8→9。
- 拖拽固定：搜索 `7-zip` 后从首项拖到屏幕底部唤出 Dock，日志 `[drop] OLE 进入 Dock，1 项`、`[drop] OLE 放下 Dock，1 项`，`dockItems` 8→9；松手后 Hub 自动收起。
- 窗口位置：Hub `956,800 648×488`，底边 1288；Dock `992,1296 576×100`，两者间距 8px，无重叠。
- 控制中心：音量 100%、非静音、网络“已连接”；点击音量卡片日志 `静音=True`，再点 `静音=False`；滚轮下滚 100→96、上滚回 100。
- 降级：蓝牙 radio 与亮度 WMI 在本机不可用，对应卡片不出现；亮度失败日志只出现一次。
- 收起：点击面板外部后 Hub 窗口从可见窗口列表消失；Esc 可关闭；Dock 第一枚快捷按钮打开应用面板、第二枚打开控制中心。
- 测试配置：右键/拖拽测试前备份 `_scratch\shell-before-hub.json`（8 项），测试后恢复并重启，运行配置回到 8 项。
- 截图：`docs/screens/v8/hub-apps.png`、`hub-search.png`、`hub-control.png`、`dock-hub-actions.png`。
- 未验证：蓝牙/亮度在支持设备上的实际操作、125%/150% DPI、双屏不同 DPI、副屏唤出 Hub 的定位偏好、Dock 已满 24 项与跨屏拖拽。

## v2.0 Phase 7 多屏第一步（2026-11-27，Issue #76）

- 环境：Windows 11 25H2，`Screen.AllScreens` 2 屏，DISPLAY1 `0,0 2560×1440` 主屏，DISPLAY2 `-2560,0 2560×1440` 副屏，均 100% DPI。
- 构建：`bin\Liuli.exe` 306.5 KB；C#5 无新增 error。
- 设置：Dock 页显示“多屏模式：仅主屏 / 所有屏”，默认“仅主屏”；切“所有屏”后 `shell.json` 的 `displayMode=all`，日志 `screens=2 extraDocks=1`。
- 副屏 Dock：出现在 DISPLAY2 底部居中，与主屏 Dock 同一套图标；鼠标在副屏底部时副屏 Dock `y=1344` 露出，主屏 Dock `y=1442` 保持收起。
- 主屏 Dock：鼠标在主屏底部时主屏 Dock `y=1296` 露出，副屏 Dock `y=1444` 保持收起；截图 `dock-two-screens.png` 左屏有 Dock、右屏无 Dock。
- 显示器变化：向 Liuli 窗口发送 `WM_DISPLAYCHANGE (0x007E)`，日志 `reflow screens=2 extraDocks=1`，副屏 Dock 重建一套，主 Dock、灵动岛、设置窗未崩。
- 关闭副屏：切回“仅主屏”后 `displayMode=primary`，副屏 Dock 关闭，主屏 Dock 保留；截图 `settings-dock-multiscreen-primary.png`、`settings-dock-multiscreen-all.png`。
- 未验证：真实拔插显示器 / 改分辨率、副屏任务栏自动隐藏与多实例、双屏不同 DPI、跨屏玻璃切片、每屏独立布局。

## v2.0 M6 Dock 动态避让任务栏（2026-11-27，Issue #72）

| 项 | 结果 | 证据 |
|---|---|---|
| 构建 | 通过 | `bin\Liuli.exe` 300 KB；C#5 无新增 error |
| 任务栏露出检测 | 通过（真机） | 任务栏 rect top=1392、高 48；缩到屏幕边缘 2px 时不判为露出 |
| Dock 上移 | 通过（真机） | Dock 窗口 1296..1396，玻璃主体底边 1384，距任务栏顶 8px；截图 `docs/screens/v8/dock-above-taskbar.png` |
| 关闭 Dock 自动隐藏 | 通过（测试） | 临时 `dockAutoHide=false` 后 Dock 与任务栏同时可见，不遮挡；已恢复 true |
| 任务栏缩回后回底 | **未单独实测** | 走同一 `BaseTopY`，未拍到回底帧 |
| 连续扫底 / 任务栏换边 | **未验证** | 未测 |
| 125% DPI | **未验证** | 当前 100% |

## v2.0 M6 日志诊断与摘要（2026-11-27，Issue #70）

| 项 | 结果 | 证据 |
|---|---|---|
| 构建 | 通过 | `bin\Liuli.exe` 299.5 KB；C#5 无新增 error |
| 系统页日志区 | 通过（真机） | 路径、当前 118.8 KB、轮转 0 B、512 KB 说明；截图 `docs/screens/v8/settings-logs.png`（轮转后：52 B / 568.0 KB） |
| 复制诊断摘要 | 通过（真机） | 剪贴板含版本 1.5.0、DPI 96、日志/配置路径、大小与最近 16 行 |
| 打开日志目录 | 通过（真机） | 资源管理器打开 `E:\meihuaDesktop\bin\data\logs` |
| 512 KB 轮转 | 通过（真机） | 注入后总 581,641 B；触发写入后当前 52 B、`.1` 581,641 B |
| 日志恢复 | 通过 | 测试后复制回原日志并删除 `.1`；当前无测试脏行 |
| 连续多次轮转 / 多进程写 | **未验证** | 只做一次轮转 |
| 无权限目录 / 剪贴板占用 | **未验证** | 未模拟 |

## v2.0 M6 全局热键查看与重置（2026-11-27，Issue #68）

| 项 | 结果 | 证据 |
|---|---|---|
| 构建 | 通过 | `bin\Liuli.exe` 298 KB；C#5 无新增 error |
| 系统页快捷键区 | 通过（真机） | 显示 Ctrl+Alt+L 显示/隐藏、Ctrl+Alt+G 打开设置；截图 `docs/screens/v8/settings-hotkeys.png` |
| Ctrl+Alt+G | 通过（真机） | 热键打开设置，状态“已注册” |
| Ctrl+Alt+L | **环境受限** | 当前机器被其它进程占用，状态“失败”；日志 `toggle=fail settings=ok` |
| 重新注册 | 通过（真机） | 点击后先注销再注册，日志新增一次 `toggle=fail settings=ok`，页面刷新且不崩 |
| 恢复注册 | **未验证** | 未关闭占用方后重试 |
| 非管理员 / 第三方冲突 | **未验证** | 未测 |

## v2.0 M6 开机自启（2026-11-27，Issue #66）

| 项 | 结果 | 证据 |
|---|---|---|
| 构建 | 通过 | `bin\Liuli.exe` 297 KB；C#5 无新增 error |
| 设置第 10 页 | 通过（真机） | 十页导航主题/配色/面板/Dock/灵动岛/壁纸/桌面/收纳/系统/关于；系统页开关、启动项路径、后续说明完整；截图 `docs/screens/v8/settings-system.png` |
| 开启 | 通过（真机） | `autoStart=True`；Run=`"E:\meihuaDesktop\bin\Liuli.exe"`；日志 `已写入` |
| 关闭 | 通过（真机） | `autoStart=False`；Run 值不存在；日志 `已删除当前用户启动项` |
| 移动 exe | 通过（真机） | exe+shell.json 复制到 `_scratch\move-test` 启动，Run 改为 `"E:\meihuaDesktop\_scratch\move-test\Liuli.exe"` |
| 恢复主 exe | 通过（真机） | 主 exe 启动后 Run 回到 `"E:\meihuaDesktop\bin\Liuli.exe"` |
| 自检 | 通过 | `--selftest` 输出 `autoStart=False run=` |
| Windows 重启实际自启 | **未验证** | 未重启系统 |
| 非管理员受限账户 | **未验证** | 当前用户可写 HKCU |
| 多用户切换 / 杀软拦截 | **未验证** | 未测 |

### 自启真机记录（2026-11-27）

- 开启后 `HKCU\Software\Microsoft\Windows\CurrentVersion\Run\Liuli` 值为 `"E:\meihuaDesktop\bin\Liuli.exe"`；关闭后 `Get-ItemProperty` 取不到该值。
- 移动测试使用 `_scratch\move-test\Liuli.exe` + `data\shell.json`；启动日志写入临时路径，Run 同步改为临时路径。
- 恢复主 exe 启动后，Run 自动回到主路径；测试完关闭开关，配置与注册表均回到 false/无值。
- 测试目录 `_scratch\move-test` 已回收。

## v2.0 M5 桌面组件（2026-11-27，Issue #64）

| 项 | 结果 | 证据 |
|---|---|---|
| 构建 | 通过 | `bin\Liuli.exe` 295.5 KB；C#5 无新增 error |
| 12 小时制 + 秒 | 通过（真机） | 临时时钟面板显示 `01:10:34`，顶部“上午”；截图 `docs/screens/v8/clock-12h-seconds.png` |
| 24 小时制 + 秒 | 通过（真机） | 右键“时间制”切换后显示 `01:11:41`；`shell.json` `clock24=true`；截图 `clock-24h-seconds.png` |
| 24 小时制无秒 | 通过（真机） | 右键“显示秒”关闭后显示 `01:11` 格式，`clockSeconds=false`；截图 `clock-24h.png` |
| 秒刷新 | 通过（真机） | 同一面板间隔 2 秒两帧 PNG SHA-256 不同，秒位在走 |
| 系统 2 秒刷新 | 通过（真机） | 切到系统面板后两帧哈希不同；CPU/内存/磁盘显示 10%/41%/79%；截图 `panel-stats.png` |
| 右键菜单 | 通过（真机） | 时钟面板菜单含“时间制：24/12 小时”“显示秒：开/关”；实际点击可落盘 |
| 天气离线缓存 | 通过（离屏反射） | 反射 `PaintWeather`，`Weather.Offline=true` 时输出“道孚 · 晴 · 离线缓存”；截图 `docs/screens/v8/weather-offline-cache.png`；真实断网仍未回归 |
| 12 小时跨中午 | **部分验证** | `ClockText` 对 12:05 与 00:05 均返回 `12:05:09`，时段标签由 `PaintClock` 的上午/下午分支负责，未注入指定时间做实拍 |
| 125% DPI / 极窄面板 | **未验证** | 仅 100% DPI、340×200 面板 |

### 组件真机记录（2026-11-27）

- 临时面板 `panel-clocktest`，位置 `1000,220`、340×200；验证后配置已回滚到测试前 `shell.json`。
- 12 小时 + 秒：`01:10:34`，标题行下“上午”；24 小时 + 秒：`01:11:41`，标题“本地时间”。
- 右键菜单点击顺序实际误点过一次“内容：系统”，随后切回时钟；最终只保留 `clock24=true`、`clockSeconds=false` 的验证状态，再回滚。
- 系统面板显示 `CPU 10%`、`内存 41%`、`磁盘 79%`；2 秒采样来自 `V2Shell.statsTimer`。

## v2.0 M5 桌面收纳（2026-11-27，Issue #16）

| 项 | 结果 | 证据 |
|---|---|---|
| 构建 | 通过 | `pwsh -NoProfile -File tools\build.ps1` → `bin\Liuli.exe` 293 KB；C#5 无新增 error |
| 首次收纳 | 通过（真机） | 启动日志 `[collector] 桌面入口同步 69 项，已写入 shell.json`；`shell.json` 有 69 条 path/name/group |
| 面板布局 | 通过（真机） | 620×430，位置 `1900,96`；9 个分组标签、每页 18 项、共 4 页；滚轮/按钮翻页区域可见 |
| 分组筛选 | 通过（真机） | 点击“文档”组显示 11 项，底部“第 1/1 页”；“全部”显示 69 项/4 页 |
| 新建自动收集 | 通过（真机） | 创建 `Liuli收纳测试.txt`，3 秒内日志 `同步 70 项`，文档组出现，顶部计数 70 |
| 删除自动移除 | 通过（真机） | 测试文件送入回收站，3 秒内日志 `同步 69 项`，入口消失 |
| 双击打开 | 通过（真机） | 双击测试文件，Notepad 打开 `Liuli收纳测试.txt - Notepad` |
| 拖动换组 | 通过（真机） | 拖测试文件到“应用”，日志 `换组 Liuli收纳测试 -> 应用（未移动文件）`；`group=应用` |
| 重启保持 | 通过（真机） | 重启后 `collectorOn=true`、`collectorGroup=文档`、`collectorX/Y=1900/96`、`collectorW/H=620/430` |
| 收纳页 | 通过（真机） | 第 8 页“收纳”，九页导航完整；两个开关、三个按钮、当前收纳信息、去桌面页按钮不裁切 |
| 开关关闭/重开 | 通过（真机） | 收纳页开关关闭 → `collectorOn=false`、面板销毁；再开 → `true`、面板重建并保持位置 |
| 原生图标 | 通过（真机） | 启动配置 `desktopIconsHidden=true`，桌面 `SysListView32` 不可见；收纳面板可见 |
| 125% DPI / 多屏 | **未验证** | 仅在 100% 主屏 2560×1440 |
| OLE 拖入/拖出 | **未验证** | 只验证代码路径与 Copy 效果参数，未用真实 Explorer 拖拽 |
| >500 项 | **未验证** | 当前桌面 69 项 |

### 收纳真机记录（2026-11-27）

- 面板窗口 `1900,96,2520,526`（620×430 + 12px 外扩阴影）；分组标签 y≈159，文档组点击点 `2145,159`；面板“文档”页 11 项。
- 测试文件路径 `C:\Users\10195\Desktop\Liuli收纳测试.txt`；创建后 3 秒日志 70 项；送入回收站后 3 秒日志 69 项。
- 双击测试文件后 Notepad 标题 `Liuli收纳测试.txt - Notepad`；测试结束后关闭。
- 拖动换组从文档 tile 到“应用”组，配置项 `group=应用`、文件仍在桌面；测试文件已送入回收站。
- 收纳页截图 `docs/screens/v8/settings-collector.png`；面板截图 `docs/screens/v8/collector-panel.png`。
- 未验证：真实 Explorer 拖入/拖出、Explorer 重启后原生图标再隐藏、125% DPI/多屏。

## v2.0 M5 桌面图标（2026-11-26，Issue #61）

| 项 | 结果 | 证据 |
|---|---|---|
| 设置页 | 通过（真机） | 第 7 页“桌面”，八页导航全部落在 720 内；全部开关、五个系统开关、说明与提示完整显示；控制面板提示标注 Win11 25H2 限制 |
| 启动默认隐藏 | 通过（真机） | `shell.json` 六个字段默认 true；启动日志 `[icons] 桌面 hidden=True visible=False`；桌面 `SysListView32` 不可见 |
| 显示全部 | 通过（真机） | 第一开关点击后日志 `hidden=False visible=True`，桌面用户图标恢复；`desktop-icons.runtime` 删除 |
| 此电脑开关 | 通过（真机） | 显示后桌面项数 69→70，隐藏 70→69；注册表 `NewStartPanel` 0/1 正确 |
| 回收站开关 | 通过（真机） | 同上，69↔70 |
| 用户文件夹开关 | 通过（真机） | 同上，69↔70 |
| 网络开关 | 通过（真机） | 同上，69↔70 |
| 控制面板开关 | **受限** | Win11 25H2 build 26200：显示/隐藏均 69，注册表与配置 0/1 正常；原生“桌面图标设置”预览也无该图标 |
| 正常退出恢复 | 通过（真机） | 切回隐藏（marker 写入）后从主菜单“退出 Liuli”；进程退出，桌面图标恢复可见，marker 删除，`desktopIconsHidden` 保持 true |
| Explorer 重启保持 | 通过（真机） | 此电脑设为显示后重启 Explorer，桌面项数保持 70；控制面板仍无图标 |
| 异常退出校准 | 通过（真机） | 隐藏态强杀进程，marker 保持 `1`；重启日志“上次异常退出，已校准桌面图标 visible=True”后再次隐藏，marker 重建 |
| 125% DPI / 多屏 | **未验证** | 100% 单屏 2560×1440 |
| Win10 1903 | **未验证** | 只测 Win11 26200 |

### M5 桌面图标真机记录（2026-11-26）

- 设置窗口 `830,360,1730,1080`；桌面页索引 6，导航客户端 `(80,380)`；开关 x≈846，y≈144（全部）/270（此电脑）/328（回收站）/386（用户文件夹）/444（网络）/502（控制面板）。
- 初始全部隐藏时 `SysListView32` 项数 69；四个系统图标分别显示后 70、隐藏后 69。
- 强杀验证：隐藏态 `Stop-Process -Force`，`desktop-icons.runtime=1`；重启日志 `上次异常退出，已校准桌面图标 visible=True`，随后 `hidden=True visible=False`，marker 重新写入。
- 测试前备份 `_scratch/shell-before-desktop.json` 与 `_scratch/desktop-icons-before.reg`；验收后恢复默认六个字段 true，桌面图标运行期隐藏。
- 截图：`docs/screens/v7/settings-desktop.png`；过程截图在 `_scratch/`。

## v2.0 M5 壁纸页（2026-11-26，Issue #59）

| 项 | 结果 | 说明 |
| --- | --- | --- |
| 构建 | 通过 | `pwsh -NoProfile -File tools\build.ps1` → `bin\Liuli.exe` 268.5 KB；C#5 warning 无新增 |
| 设置页 | 通过（真机） | 第 6 页“壁纸”，七页导航全部落在 720 内；预览、文件名/路径/样式、压暗滑杆、两个按钮、说明完整显示 |
| 当前壁纸预览 | 通过（真机） | 默认 `img0.jpg` 解码为 760×428 cover 缩略图，卡片 380×214；切换测试图后预览同步刷新 |
| 原生选择图片 | 通过（真机） | 打开 `#32770 选择壁纸`，文件名框粘贴 `_scratch\wallpaper-test.png` 后 Enter；Windows 壁纸写入测试图，`wallpaperOrig=C:\WINDOWS\web\wallpaper\Windows\img0.jpg`，预览更新 |
| 压暗滑杆 | 通过（真机） | 0→41→79，`wallpaperDim` 落盘；松手才重烘，日志 `磨砂重烘 ... dim=79 mean=37,41,44 light=0`，设置页自动切深色 |
| 恢复系统壁纸 | 通过（真机） | 按钮写回 `img0.jpg`（style 10 / tile 0），`wallpaperOrig` 清空，预览与按钮说明刷新为“当前没有可恢复记录” |
| 0% 不压暗 | 通过（真机） | 恢复后 `wallpaperDim=0`，mean 回到 `175,195,208 light=1` |
| 重启保持 | 通过（真机） | 配置含 `wallpaperDim=79` 时重启，日志 `dim=79 mean=37,41,44 light=0`，设置页深色；验收后恢复 `dim=0` |
| 页面视觉 | 通过（真机） | `docs/screens/v7/settings-wallpaper.png` |
| 主题快照含压暗 | 通过（反射） | `WallpaperDim=42` → `SaveTheme` → 重新 `Load` → `FindTheme` 读回 42；`ApplyTo` 后 `Config.WallpaperDim=42`；未做真机保存/加载 UI 回归 |
| WebP | **未验证** | Filter 含 `webp`，但 GDI+ 解码依赖系统 WIC；未用真实 WebP 文件回归 |
| 125% DPI / 多屏 | **未验证** | 仅在 100% 主屏验证 |
| 换壁纸 5 秒联动 | **未验证** | 未测 `Backdrop.Tick` 5 秒轮询对新系统壁纸的自动重烘 |

### M5 壁纸真机记录（2026-11-26）

- 设置窗口 `830,360,1730,1080`；壁纸页索引 5，导航客户端 `(80,336)`；压暗滑杆 y≈469，选择按钮 y≈535，恢复按钮 y≈589。
- 测试壁纸 `_scratch/wallpaper-test.png`（1920×1080，浅蓝渐变 + “Liuli 壁纸测试”）；系统样式始终 10（填充）。
- 压暗 79% 后 `shell.json`：`wallpaperDim=79`；日志 mean 37,41,44 → `light=0`，背景叠层、文字、导航同步转深色，说明压暗已进入平均色/主题重算。
- 原生选择对话框：`SetWindowText` 可写入但没有触发 `EN_CHANGE`，`BM_CLICK` / Enter 均不提交；改用真实鼠标点文件名框 + 剪贴板 `Ctrl+V` + 真实 Enter 才生效。
- 原生对话框的 UIA 树只暴露帮助/视图/组织/新建文件夹/名称/搜索等控件，文件名编辑框与“打开”按钮不在树内；验证不要依赖 UIA 自动化。
- 验收前备份 `_scratch/shell-before-wallpaper.json`（`wallpaperDim=0`、`wallpaperOrig` 空、0 主题）；最终恢复原壁纸并清测试配置。

## v2.0 M5 配色（2026-11-26，Issue #56；完整形态 #57）

| 项 | 结果 | 说明 |
| --- | --- | --- |
| 构建 | 通过 | `bin\Liuli.exe` 263.5 KB；C#5 warning 无新增 |
| 设置页 | 通过（真机） | 新增第 2 页“配色”，六页导航全部落在 720 内；来源三段、6 色卡、自定义按钮、当前配色说明均可见 |
| 跟随壁纸 | 通过（配置/代码） | `accent=auto` 时 `ResolveAccent` 返回 `Backdrop.Accent`；换壁纸仍走 M1 `Backdrop.Tick → ApplyTheme` |
| 6 预设 | 通过（真机） | 逐一点击 6 个色卡，`accentPreset` 依次 0→5；选中环、区块标题、导航高亮随 accent 变化 |
| 预设显色 | 通过（真机） | 紫雾 `#B49AFF` 选中态截图；石墨 `#B0BED0` 选中态截图；6 色卡各自颜色正确 |
| 自定义取色 | 通过（真机） | 打开系统颜色对话框，选粉色并确定，`accentCustom=#FF80FF`、`accentMode=custom`；设置标题、导航、Dock 运行点/图标高亮、灵动岛太阳同步变粉 |
| 取消不改 | 通过（真机） | 再次打开颜色对话框直接取消，`accentMode/custom` 保持 `custom/#FF80FF` |
| 主题快照 | 通过（真机） | 保存“主题 1”时 `accentMode=custom / accentPreset=5 / accentCustom=#FF80FF`；切到 auto 后加载该主题，三项恢复 |
| 重启保持 | 通过（真机） | 重启外壳后 `activeTheme=主题 1`，配色页仍显示自定义 `#FF80FF` |
| 坏值收敛 | 通过（自检） | 手改 `weird / 9 / nothex` 后 `--selftest` 输出 `auto / 5 / #7EB2FF`；`NormalizeThemes` 对快照同样收敛 |
| Dock/灵动岛入口 | 通过（真机/消息） | Dock 齿轮打开设置落在“Dock”页（索引 3）；灵动岛齿轮改索引 4 未真机点击 |
| 页面视觉 | 通过（真机） | `docs/screens/v6/settings-palette.png`、`settings-palette-preset.png`、`settings-palette-custom.png`、`m5-dock-accent.png`、`m5-island-accent.png` |
| 真实热键 | **未验证** | 仍用 `WM_HOTKEY` 消息打开设置；物理键盘注入未稳定复现 |
| 换壁纸 5 秒 | **未验证** | 未切换系统壁纸做 auto 变色的 5 秒计时 |
| 性能/对比度 | **未验证** | 未量重算耗时；6 预设仅切 accent，玻璃染色/描边/文字层级仍为全局 token |
| 125% DPI | **未验证** | 仅在 100% 主屏验证 |

### M5 配色真机记录（2026-11-26）

- 设置窗口 `830,360,1730,1080`；第 1 页主题、第 2 页配色；Dock 齿轮落第 4 页（Dock），灵动岛齿轮代码指向第 5 页。
- 6 色卡客户端坐标：第 1 行 `(270/530/760, 270)`，第 2 行 `(270/530/760, 330)`；点击后立即写 `shell.json`。
- 颜色对话框 `1016,340,1544,760`；选基本色粉色 + 确定后配置 `#FF80FF`；取消不改。
- 测试配置备份 `_scratch/shell-before-color.json`；测试主题 `主题 1` 与自定义色在验收后清理，最终恢复 `auto`。
- Dock 齿轮真机点击后设置落在 Dock 页；灵动岛齿轮入口未真机点击（窗口被前台窗口遮挡）。

## v2.0 M4 主题与设置（2026-11-26，Issue #54）

| 项 | 结果 | 说明 |
| --- | --- | --- |
| 构建 | 通过 | `pwsh -NoProfile -File tools\build.ps1` → `bin\Liuli.exe` 259 KB；旧 v1 代码既有 warning 不变 |
| 主题保存 | 通过（真机） | 主题页“保存当前主题”→原生 TextBox 默认名“主题 1”→保存；`themes[]` 出现该条，`activeTheme=主题 1` |
| 主题加载 | 通过（真机） | 加载含 1 个天气面板、玻璃 100% 的主题；`panels=1`、`content=weather`、`glass=100`、`activeTheme` 指向该主题 |
| 主题覆盖 | 通过（真机） | 玻璃改为 62% 后点行内“覆盖”，主题数保持 6、`themes[0].glass=62`、`activeTheme` 切到该条 |
| 主题删除 | 通过（真机） | 第一次点“删除”变红色“确认删除”，第二次才删除；删当前主题后 `activeTheme` 清空 |
| 6 主题上限 | 通过（真机） | 连续保存到第 7 个，`themes` 保持 6，最早“主题 1”被淘汰，名称顺序为“主题 2…主题 7” |
| 手动修改清当前 | 通过（真机） | 加载后拖玻璃滑杆，`activeTheme` 变空；180ms 后主题库“当前”徽标与底部“当前主题”同步清空 |
| 面板页 | 通过（真机） | 连续“新建面板”到 6 个，按钮变“已达 6 个面板上限”；6 行完整落在 720 高度内，无遮挡底栏 |
| 面板删除 | 通过（真机） | 行内“删除”两次确认，`panels` 6→5；内容 chip 与置顶/置底入口可见 |
| Dock 页 | 通过（真机） | 显示开关关掉后 `dockOn=false`、Dock 窗口销毁；再打开重建 `992,1344,1568,1444`；自动隐藏/尺寸三档可见 |
| 灵动岛页 | 通过（真机） | 显示开关关掉后 `islandOn=false`、窗口销毁；再打开重建 `1146,6,1413,70`；宽度滑杆 380→433→再调回；四滑杆+三开关完整显示 |
| 设置入口 | 通过（真机/消息） | Dock 齿轮打开设置并直接落在“Dock”页；托盘/热键走 `OpenSettings(0)`；已打开时切页不重复建窗 |
| 页面视觉 | 通过（真机） | 五页均截图；`docs/screens/v5/settings-theme.png`、`settings-panels.png`、`settings-dock.png`、`settings-island.png`、`settings-about.png` |
| 中文名称输入 | **部分验证** | 默认名保存、剪贴板粘贴均写对；输入法候选窗首次干扰，已加 `ActiveControl=null; Application.DoEvents()` 失焦提交，未复测直接中文键入 |
| 跨壁纸加载 | **未验证** | 测试壁纸始终为 `C:\WINDOWS\web\wallpaper\Windows\img0.jpg`；换壁纸后加载主题的系统壁纸写入未回归 |
| 坏主题/坏 JSON | **部分验证** | `NormalizeThemes` 收敛空名、重名、6 上限与数值范围；未手改 `themes[]` 做坏值真机回归 |
| 热键真实按键 | **未验证** | `Ctrl+Alt+G` 的 `WM_HOTKEY` 处理已验证可开设置；测试环境键盘注入未稳定复现，未当作通过 |
| 125% DPI / 多屏 | **未验证** | 设置窗与滑杆仅在 100% 主屏验证 |

### M4 主题真机记录（2026-11-26）

- 设置窗口：900×720，位置 `830,360`，日志 `[settings] shown ... dpi=1`；Dock 齿轮落在 Dock 页，主题页为默认入口。
- 主题快照：`主题 1`（0 面板）、`主题 2Evening`（1 天气面板）；加载 `主题 1` 后面板消失、玻璃 100%；再加载 `主题 2Evening` 后天气面板回来。
- 状态清理：测试产生的 5 个面板、6 个主题和临时配色已回滚到 `_scratch/shell-before-acceptance.json`；最终用户配置无测试主题/面板。

## v2.0 M4 面板（2026-11-26，Issue #52）

| 项 | 结果 | 说明 |
| --- | --- | --- |
| 构建 | 通过 | `pwsh -NoProfile -File tools\build.ps1` → `bin\Liuli.exe` 225.0 KB |
| 默认状态 | 通过 | `--selftest`：`panels=0`；新装/旧配置不自动创建面板 |
| 新建入口 | 通过（真机） | Dock 左侧“+”创建第 1 个面板；灵动岛展开后“+”创建第 2 个；面板右键“新建面板”再创建 1 个 |
| 多面板 | 通过（真机） | 同时存在 2 个面板（时钟 440×280、天气 340×200）；最多 6 个按代码限制 |
| 拖动 | 通过（真机） | 第 1 个面板从 `2180,160` 拖到 `1880,360`，松手写 `shell.json` |
| 缩放 | 通过（真机） | 右下角热区从 `340×200` 缩放到 `440×280`，配置同步 |
| 四类内容 | 通过（真机） | 同一面板 `empty → clock → weather → stats`，`x/y/w/h` 保持 `1880/360/440/280`；天气显示 `道孚 1° 多云`，系统显示 CPU/内存/磁盘三条 |
| 右键菜单 | 通过（真机） | 7 行：内容 4 项、置顶/置底、新建面板、删除此面板；截图 `m4-panel-menu.png` |
| 持久化 | 通过（真机） | 重启后两个面板位置、尺寸、内容恢复；`--selftest` 输出 `panels=2` 与逐面板字段 |
| 点击穿透 | 通过（接口） | `WM_NCHITTEST`：阴影 `1870,350=-1`、body `1900,380=1`、外部 `-1`；未做真实点击桌面图标回归 |
| 置顶/置底 | 通过（真机） | 置底后 `WS_EX_TOPMOST` 位清除（`0x08090080`），记事本覆盖天气面板；置顶时钟面板 `0x08090088` 仍在记事本上方 |
| 删除 | 通过（真机） | 右键“删除此面板”逐个关闭，`panels` 从 2 降到 0 |
| 截图 | 通过 | `docs/screens/v4/m4-panel-clock.png`、`m4-panel-weather.png`、`m4-panel-stats.png`、`m4-panel-empty.png`、`m4-panel-menu.png`、`m4-panels-two.png` |
| 6 面板上限 | **未验证** | 代码限制 `Panels.Count >= 6`，未在真机连续创建到上限 |
| 坏配置/越界 | **部分验证** | 代码 `NormalizePanels` 收敛尺寸与越界；未手改 `panels[]` 做坏值真机回归 |
| 125% DPI | **未验证** | 需缩放后测拖动/缩放/字体与阴影边距 |
| 多屏 | **未验证** | 面板固定回主屏工作区，跨屏拖动未测 |

### M4 真机记录（2026-11-26）

- 启动：`bin\Liuli.exe`，PID 27776；主屏 2560×1440，任务栏高 48，缩放 100%。
- 面板窗口 = body + 12 逻辑像素阴影；配置坐标是 body 左上角，窗口 Location 为 `(X-12,Y-12)`。
- 第 1 个面板：`panel-7eb768c2`，`clock`，`1880,360,440×280`，`topMost=true`；窗口 rect `1868,348,2332,652`。
- 第 2 个面板：`panel-61930040`，`weather`，`1906,38,340×200`，测试置底后恢复 `topMost=true`；窗口 rect `1894,26,2258,250`。
- ExStyle：置顶 `0x08090088`，置底 `0x08090080`，差异仅 `WS_EX_TOPMOST (0x8)`。
- 命中：body 内 `1`（`HTCLIENT`），阴影/窗口外 `-1`（`HTTRANSPARENT`）。
- 删除测试后当前配置 `panels=0`，外壳保持默认无面板状态。

## v2.0 M3（2026-11-26，Issue #50）

| 项 | 结果 | 说明 |
| --- | --- | --- |
| 构建 | 通过 | `pwsh -NoProfile -File tools\build.ps1` → `bin\Liuli.exe` 214.5 KB |
| 默认配置 | 通过 | `--selftest`：`island.width=380 height=112 top=6 animMs=180 quick=True weather=True stats=True` |
| 越界夹取 | 通过 | 手改 `999 / 1 / -100 / 999` 后自检收敛为 `520 / 100 / 0 / 400` |
| 默认尺寸 | 通过（真机） | 收起 `263×64`；展开 `404×136`；截图 `m3-default-collapsed.png` / `m3-default-expanded.png` |
| 宽度/高度/偏移 | 通过（真机菜单） | 宽 380→420：收起 `289×64`；高 112→136：展开 `444×160`、收起 `289×73`；顶部 6→16：窗口 y 从 6 到 16 |
| 展开动画 | 通过（真机） | 180→260 时扫描到中间 `395×133`；`islandAnimMs=0` 时指针移入后立即为 `404×136`，无中间尺寸 |
| 内容开关 | 通过（真机菜单） | `islandQuick=false` 无快捷按钮；`islandWeather=false` 收起态改显 CPU；`islandStats=false` 展开态无三读数；截图 `m3-quick-off.png` / `m3-weather-off.png` / `m3-stats-off.png` |
| 设置子菜单 | 通过（真机） | 主菜单第 4 行“灵动岛设置”打开 8 行子菜单；宽度/高度/偏移/动画/天气/读数/快捷/恢复默认均可点；截图 `m3-main-menu.png` / `m3-island-menu.png` |
| 恢复默认 | 通过（真机） | 写回 `380/112/6/180`、`quick/weather/stats=true`，灵动岛立即回到 `263×64` / `404×136` |
| 重启保持 | 通过（真机） | `420/136/16/260` 重启后仍为收起 `289×73`、展开 `444×160` |
| 菜单点击时序 | 通过（本轮） | 菜单项改为 MouseUp 触发；子菜单 140ms 延迟打开；主菜单→子菜单连续打开通过，未做多轮边界压测 |
| 125% DPI | **未验证** | 需改缩放后测尺寸、偏移、动画与菜单命中 |
| 动画主观手感 | **未验证** | 260ms 只确认中间尺寸，未做主观观感评审 |

### M3 真机验收记录（2026-11-26）

- 启动：`bin\Liuli.exe`，PID 17336 / 25272 / 1176 / 408，`[v2] 外壳启动完成 ms=422`，天气 `道孚 1° 少云`。
- 屏幕：主屏 2560×1440，任务栏高 48，缩放 100%；Dock standard `576×100` @ `992,1344,1568,1444`。
- 默认：灵动岛收起 `263×64` @ `1148,6,1411,70`；展开 `404×136` @ `1078,6,1482,142`。
- 配置实测：宽 420 → 收起 `289×64`；高 136 → 展开 `444×160`、收起 `289×73`；顶部 16 → 窗口 y=16；动画 260 → 中间 `395×133`。
- 内容：快捷动作关闭后展开态只剩时间/日期/天气/读数；天气关闭后收起态显示时间 + CPU；读数关闭后展开态三进度条消失。
- 菜单：`1268,42,1566,398` 主菜单 10 行高 356；子菜单 `1268,42,1566,330` 8 行高 288；行中心公式 `top + 14 + i*34 + 17`。
- 复位：点“恢复灵动岛默认”后 `shell.json` 回到 `380/112/6/180`，三开关为 true；测试后 Dock 仍为 `standard`、`dockAutoHide=false`。
- 截图：`docs/screens/v3/m3-default-collapsed.png`、`m3-default-expanded.png`、`m3-main-menu.png`、`m3-island-menu.png`、`m3-quick-off.png`、`m3-weather-off.png`、`m3-stats-off.png`。
- 已知：菜单点击在极快连续操作下仍可能丢一次；未系统记录所有命中边界；动画 260ms 的观感未主观验收。
- 集成：PR #51 以 merge commit `4091f8f` 合入 `main`；Issue #50 关闭。

## v2.0 M2（2026-11-26）

| 项 | 结果 | 说明 |
| --- | --- | --- |
| 构建 | 通过 | `pwsh -File tools\build.ps1` → `bin\Liuli.exe` 209 KB |
| 配置默认值 | 通过 | `--selftest`：`dockAutoHide=False`、`dockPreset=standard`，未写字段时保持常显/标准 |
| 配置读写 | 通过 | 临时写入 `dockAutoHide=true` / `dockPreset=large` 后自检读取正确；测试后恢复原 `shell.json` |
| 隐藏几何 | 通过（离屏） | `--preview`：`dockHidden top=1442`，`screenBottom=1440`，窗口完全移出屏幕 |
| 三档尺寸 | 通过（离屏） | small 494×86、standard 576×100、large 658×114；`m2-dock-presets.png` 对照已生成 |
| 运行指示 | 通过（离屏） | `dockRunning=explorer,SystemSettings`；`m2-dock-running.png` 可见 explorer/settings 下方圆点；自检映射 `calc.exe → CalculatorApp / Calculator / WindowsCalculator` |
| 拖拽排序 | 通过（离屏） | 开头/中间/末尾三个插入位对照 `m2-dock-reorder.png`；插入线位置与三档布局共用 `itemRects` |
| 悬停放大 | 通过（离屏 + 真机） | 真机悬停第 4 项出现“资源管理器”标签、图标放大；截图 `m2-real-hover.png` |
| 真实拖拽 | 通过（真机） | 按住资源管理器拖到末尾，`dockItems` 顺序变为 7 项后追加资源管理器；截图 `m2-real-drag.png` |
| 自动隐藏真机 | 通过（真机） | 隐藏 `top=1444`，底部热区唤出 `top=1344`，离开 650ms 后回到 `1444` |
| 任务栏共存 | 通过（修复后真机） | 任务栏常显时 Dock `top=1296`，body 在任务栏上方留约 8px；截图 `m2-real-taskbar.png`；修复前为 1248（多让 48px） |
| 灵动岛悬停 | 通过（真机） | 悬停展开日期/天气/CPU/内存/磁盘/三快捷动作，离开 1.5s 后收起 |
| 点击反馈 | 部分通过（真机） | Dock 齿轮点击能打开菜单；按压 0.94 的瞬时视觉未截到，按钮动作未逐个验 |
| 尺寸切换/重启保持 | **未验证** | 三档尺寸离屏通过；菜单循环切换与重启保持未测 |
| 运行状态实时变化/CPU | **部分通过** | 真机可见 explorer/设置下方圆点；启动/退出应用实时增减与 2s 枚举 CPU 未测 |
| 外部文件拖入 | **未验证** | 需真实拖文件进 Dock，确认与内部排序不冲突、不移动源文件 |
| 125% DPI | **未验证** | 需改缩放后实测圆点、悬停、插入线与窗口位置 |
| 外部文件拖入 Dock | **未验证** | 需从资源管理器拖真实文件，验证 COPY 固定与内部排序优先级 |
| 2s 运行枚举 CPU | **未单独测量** | 只确认功能可用，未测长时占用 |

### M2 真机验收记录（2026-11-26）

- 启动：`bin\Liuli.exe`，PID 9604 / 29708，`[v2] 外壳启动完成 ms=407~891`，天气 `道孚 2° 晴`。
- 窗口：Dock `576×100`，标准档；灵动岛 `264×64`（收起）；主屏 2560×1440。
- 悬停：Dock 第 4 项出现标签并放大；灵动岛展开后显示 CPU/内存/磁盘 + 三快捷动作，离开 1.5s 收起。
- 拖拽：资源管理器从第 4 位拖到末尾，`shell.json` 顺序实际变更，Dock 立即重排。
- 自动隐藏：隐藏 `top=1444` → 底部热区 `top=1344` → 离开 650ms 回到 `1444`。
- 任务栏共存：任务栏常显时 Dock `top=1296`；修复前 1248，确认 `WorkingArea` 与 `VisibleHeight` 双重扣除（Issue #47）。
- 状态恢复：验收后已恢复原 `dockItems` 顺序、`dockAutoHide=false`、`taskbarAutoHide=true`，并退出 Liuli。
- 三档菜单：真机点齿轮 → 菜单第 3 行连续切换，`standard→large→small→standard`，实测 `576×100 / 658×114 / 494×86`；`large` 重启后仍为 `658×114`，再恢复 `standard` 重启回 `576×100`。
- 运行指示实时增减：启动记事本 3s 后“记事本”图标下方圆点出现；关闭 3s 后圆点消失。截图 `m2-real-running-on.png` / `m2-real-running-off.png`。
- 按压态：在 Edge 图标上按下并截图，出现按压底色且图标缩小；随后把指针移出 Dock 再松开，取消点击、未启动 Edge。截图 `m2-real-pressed.png`；未逐像素测量 0.94。
- 留档：`m2-real-hover.png`、`m2-real-drag.png`、`m2-real-taskbar.png`、`m2-real-running-on/off.png`、`m2-real-pressed.png`。
- 集成：PR #49 以 merge commit `c222f91` 合入 `main`；合入前在 `feat/v2-m2-integration` 重新构建（209 KB）、`--selftest` 通过、真机启动正常。

## v2.0 M1（2026-11-26）

| 项 | 结果 | 说明 |
| --- | --- | --- |
| 构建 | 通过 | `pwsh -File tools\build.ps1` → `bin\Liuli.exe` 199 KB |
| 配置自检 | 通过 | `Liuli.exe --selftest`：`shell.json` 生成，默认 8 个 Dock 项 |
| 坏配置回退 | 通过 | 写入 `not a json object` 后自检：备份 `shell.json.bad-20261126-171933`，重建默认 8 项 |
| 任务栏状态 | 通过（只读） | `ABM_GETSTATE=1`；隐藏时 `Shell_TrayWnd rect.top=1438`，`Visible=false` |
| UI 启动 | **未验证** | 用户要求先不运行；不能算视觉/交互验收 |
| 离屏预览 | 通过 | `Liuli.exe --preview` 生成 `m1-preview.png`、`m1-preview-pressed.png`、`m1-feedback-compare.png`；不显示窗口 |
| 点击反馈 | 静态预览通过，动态**未验证** | 按下 0.94 缩放 + 按压底色；释放 190ms 回弹 + 280ms 波纹；灵动岛按钮同步；真机手感待测 |
| Dock 图标高清 | 提取通过，125% **未验证** | `--selftest` 探针 `explorer 256x256`；改用 `SHIL_JUMBO` + `IImageList`，失败回退 32px |
| 125% DPI | **未验证** | 代码按句柄 DPI 缩放，需改缩放后实测 |
| Dock 点击/拖入/右键移除 | **未验证** | 需真机操作 |
| 任务栏开关/恢复 | **未验证** | 需真机操作并对比 `ABM_GETSTATE` |

## 环境

| 项 | 值 |
| --- | --- |
| 系统 | Windows 11 专业版 10.0.26200 |
| CPU / GPU | 本机（AMD RX 6700 4GB） |
| 屏幕 | 双屏 2560×1440，副屏 X=-2560，缩放 100% |
| 壁纸 | `C:\WINDOWS\web\wallpaper\Windows\img0.jpg`，样式 10（填充） |
| 编译器 | `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe` |
| 构建 | `pwsh -File tools\build.ps1` → `bin\Liuli.exe`（约 147KB） |

## 性能实测（2026-11-26，v1.0.2）

| 指标 | 结果 | 测量方式 |
| --- | --- | --- |
| 常驻内存（Working Set） | 55.7MB | 启动 90s 后 `(Get-Process Liuli).WorkingSet64` |
| 私有内存 | 33.8MB | `PrivateMemorySize64`（设置面板打开时） |
| 闲置 CPU | 0.11%（单核占比） | 进程 `TotalProcessorTime` 30 秒增量 / 30 / 逻辑核数 |
| 磨砂烘焙 | 219~266ms | 日志 `[wall] 磨砂重烘 ... ms=` |
| exe 体积 | 148.5KB | 构建输出 |

结论：默认四块看板下内存与闲置 CPU 均达到既定目标（<80MB / <1%）。
补充观察：图标整理后 11 块看板时 Working Set 约 79~81MB，接近 80MB 上限；属于极端布局，未做进一步优化，已记录为后续优化项。

## 性能实测（2026-11-26，v1.1.0）

| 指标 | 结果 | 测量方式 |
| --- | --- | --- |
| 常驻内存（Working Set） | 56.8MB | 重启后闲置 60s `(Get-Process Liuli).WorkingSet64` |
| 私有内存 | 31.1MB | `PrivateMemorySize64` |
| 闲置 CPU | 0.06%（单核占比） | `TotalProcessorTime` 20 秒增量 / 20 / 逻辑核数 |
| exe 体积 | 150KB（153600B） | 构建输出 |
| 磨砂烘焙 | 203~234ms | 日志 `[wall] 磨砂重烘 ... ms=` |

结论：默认四块看板下仍在目标内（<80MB / <1%）。图标整理生成 11 块看板并渲染 70 个图标后，Working Set 升至约 79MB，属极端布局，记录为后续优化项。
v1.2.0 补充实测：WS 53.6MB、私有 30.9MB、exe 151KB（开机自启开关加入后无回退）。

## 已跑通

- 编译（csc，C# 5，UTF-8 BOM）
- 启动、四块默认看板、托盘、控制条、Ctrl+Alt+G 打开设置
- 天气拉取（Open-Meteo，`道孚 -3° 晴`），TLS1.2 修复后成功
- 设置面板：明暗分段切换（auto → dark 生效并保存）
- 实时玻璃：DWM 亚克力开启后看板变为实时模糊的深色玻璃
- 导出备份：生成 `data\backups\Liuli-backup-20261125-2350.zip`（3159B）
- 导入备份：选择同一个 zip，先自动备份当前状态再覆盖，日志 `[store] 导入备份 ...`，看板数恢复为导出时的 11 块
- 图标整理：首次生成 7 块文件看板（总计 11 块），再次点击原地更新，总数不变
- 便签编辑：双击正文 → 编辑窗（560×420）→ 粘贴/输入 → Ctrl+Enter 与「保存」按钮均落盘，`boards.json` 文本长度 70→76/83
- 待办勾选：点首项勾选框 → `done=true` 落盘；点文字行进入编辑
- 拖拽标题栏：看板位置变化并落盘；单击不再引起磁吸位移
- 设置页：磁吸、总在最前、显示控制条开关点验通过（配置与行为一致）
- 隐藏桌面图标：点击后图标真实隐藏（`nowVisible=False`），再点恢复（`nowVisible=True`），v1.0.2 修复后通过
- 设置回归（#2，v1.0.2/v1.1.0）：玻璃浓度 100→40、磨砂 62→20（触发重烘 218ms）、圆角 22→16 均落盘并有视觉变化；壁纸压暗 30→56、全屏暂停、视频静音写入正确
- 壁纸来源：切「图片」→ 选择 `img0.jpg` → `wallMode=image`、壁纸生效、日志 `[wall] 图片壁纸已设置`；点「恢复系统壁纸」→ `wallMode=system`、`wallPath` 清空、按系统壁纸重烘
- 图标整理撤销（#3，v1.1.0）：整理前写快照，整理 4→11 块；点「撤销上次整理」恢复为 4 块，快照自动删除，日志 `[icons] 撤销上次整理，看板 4 块`
- 自定义分组（#3，v1.1.0）：`data\groups.txt` 写入 `sketch=图片`，重启后首次整理日志 `[icons] groups.txt 自定义规则已加载`；非法分组名会告警并忽略
- 开机自启（#12，v1.2.0）：设置 → 看板开关打开后 `HKCU\...\Run\Liuli` 写入 `"…\Liuli.exe"`，配置 `autoStart=true`；关闭开关后注册表值删除、配置归 false
- 开机自启校准（#12）：手工把配置改成 true 并删掉注册表值，重启后自动补写，日志 `[app] 开机自启 on …`
- 手势回归（v1.2.1，配合 #14）：
  - 待办「添加一条」→ 编辑窗 → 粘贴 → Ctrl+Enter，`items` 3→4 且落盘（`[editor] save len=11`）
  - 代码「收藏一段代码」→ 编辑窗 → Ctrl+Enter，`items` 0→1（`len=39`）
  - 代码卡片右上复制按钮：剪贴板从占位文本变为卡片内容
  - 文件看板双击「dist」文件夹：资源管理器窗口数 0→1，文件真实打开
  - 缩放：右边缘（w 300→348）、左边缘（x/w 同步变化）、右下角（w+h 同步增大）均落盘并立即重绘
- 控制条遮挡修复（#14，v1.2.1）：编辑窗/设置面板打开时胶囊隐藏，关闭后恢复；截图 `shots\g20-editor-nobar.png`、`g22-settings-nobar.png`、`g23-settingsclosed.png`
- 真实移动整理与撤销（#17，v1.3.0）：开启开关 → 确认框 → 桌面顶层 1 项（仅 `Liuli`）、7 块分类看板、清单 70 条；点撤销 → 70 项原位恢复、桌面 70 项、`Liuli` 与清单自动删除、看板回 4 块。首轮因公共桌面不可写中断，修复后复测通过
- 收纳后单击打开（v1.3.1）：应用板 `ASUS DriverHub.lnk` → 前台出现 `ASUS DriverHub - Google Chrome`；文档板 `26事业下岗位汇总（截止11月19号16点）.xlsx` → WPS 窗口标题含该文件名。路径均在 `Desktop\Liuli\<分组>` 下，说明移动后仍可直接打开
- 文件看板图标网格（v1.4.0）：临时 10 项文件看板截图 `shots\v140-gridtest.png`，确认 4 列、40px 图标、下方单行截断文件名、滚动条正常；测试板已删除
- 收纳根默认 `文档\Liuli`（v1.4.0）：反射调用 `Icons.MoveIntoGroup`，桌面 `liuli-store-test.txt` 真实移动到 `C:\Users\10195\Documents\Liuli\测试\liuli-store-test.txt`，成功；测试文件与目录已清理，桌面未残留
- 设置「数据 → 收纳位置」（v1.4.0）：显示默认路径，点击打开 `FolderBrowserDialog` 并定位「文档」，取消后配置不变；截图 `shots\v140-set-data.png`
- 收纳目录双向同步（v1.5.0）：
  - 反射单测：空目录同步出 3 项（2 文件 + 1 子文件夹）；新增第 4 项；`a.txt` 改名 `a2.txt` 后旧条目移除、新条目补入；删 `b.txt` 后条目移除
  - 真机轮询：临时看板绑定 `shots\synctest`，启动后自动收进 2 项；外部新建 `z.txt` 约 4 秒后变 3 项，日志 `[folder] 同步 同步测试 ← …`
  - 真机真实看板：向 `Documents\Liuli\文档` 写入 `liuli-sync-test.txt`，约 4 秒后文档板 11→12；删除后 12→11；测试文件已清理
  - 用户实际整理的 11 块看板已自动补上 `folder` 字段（应用 36 / 文件夹 7 / 文档 11 / 图片 9 / 压缩包 2 / 代码 4 / 其他 2）

- 文字四档（TASK-062 / Issue #74）：标题栏切 0.9 / 1.0 / 1.15 / 1.3 均落盘；设置窗实测 810×648 / 900×720 / 1170×936，无文字裁切
- 文字四档即时重排：收纳 0.9 580×409、1.0 644×454、1.3 838×591；Dock 1.3 窗口 754×131，玻璃主体底边 1382，任务栏 top 1392，不遮挡
- 文字四档持久化：1.3 档重启仍为 1.3，切回标准写入 `textSize=1`；截图 `settings-text-standard.png`、`settings-text-large.png`、`collector-text-large.png`、`dock-text-large.png`
- 未验证：125%/150% DPI、1.15 档截图、多屏、面板实窗（当前 `panels=[]`）、0.9 档极长文件名换行

## 已知不通过

- 暂无（v1.0.2 已修 #8）

## 未验证 / 待验证

- 编辑器其余路径（待办回车新增、代码卡片编辑、文件拖入）
- 视频壁纸（WMP + WorkerW）未在本机实测
- Win10 1903、125%/150%/200% 缩放、负坐标多屏之外的布局
- 开机自启与设置面板「数据」页的清理图标缓存/恢复默认布局未逐项点验（自动启动目前只有配置字段，未暴露成开关）
- 八方向缩放未逐项手势回归
- 拖文件入板（普通入口路径）、文件看板里双击 exe/快捷方式（只验证了文件夹打开与单击打开）；「拖入即移动原件」未做真实 OLE 拖放：合成鼠标拖拽未能触发 Explorer 的拖放循环，跨进程伪造 `WM_DROPFILES` 又因 HDROP 不跨进程而文件列表为空，需人工拖一次确认
- 拖放修复（#21，v1.3.2）：`DragAcceptFiles` 改为 `shell32.dll` 后不再报入口点缺失；11 块看板 `RegisterDragDrop` 均返回 `hr=0`。真实拖拽效果待用户确认
- 文件看板图标网格（v1.4.0）的单击打开、拖出、拖入真实手势待用户复测；整桌 71 项在 `文档\Liuli` 下的完整整理/撤销回归未跑
- 一次性异常：整理后新建代码看板并点击其「收藏」时，待办看板出现过一次内容空白，重启恢复，后续同路径操作未复现——记录在案，暂未修

以上均已在 GitHub Issues 建单跟踪，不在此处标记为“通过”。


