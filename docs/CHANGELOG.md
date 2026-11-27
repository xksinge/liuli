# 变更日志

遵循 Keep a Changelog；版本遵循 SemVer。

## [Unreleased · v2.0 Phase 7 灵动岛 SMTC 媒体显示与控制]

### 新增

- 新增 `Media.cs`：后台线程通过 `Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager` 读取当前媒体会话，0.9s 轮询标题/歌手/播放状态/进度；无会话时发布空快照，不报错
- 灵动岛展开态新增媒体行：标题、歌手（无歌手时显示应用名）、进度条、上一首/播放暂停/下一首；媒体存在时展开高度加 40px，无媒体恢复原高度
- 收起态在有媒体时右侧显示 `♪ 标题`，替代天气/CPU 文案
- 设置·灵动岛页新增“媒体信息”开关（`islandMedia`，默认开）；`ThemeSnapshot` 一并保存
- 新增 `Glyph.Prev / Play / Pause / Next` 四个线性图标

### 构建

- `tools/build.ps1` 新增 `System.Runtime.dll`、`System.Runtime.WindowsRuntime.dll` 与 `C:\Windows\System32\WinMetadata\*.winmd` 引用；Win10 1903+ 自带 SMTC，缺失时后台线程记 warning、媒体区维持隐藏

### 验证

- `bin\Liuli.exe` 348.5 KB；C#5 无新增 error，仅历史 warning
- 用 `Windows.Media.Playback.MediaPlayer` 播放本地 8s WAV 循环，SMTC 探针返回 `media-player.exe`；Liuli 展开窗 `1078,6..1482,182`（404×176，较无媒体 404×136 高 40px）
- 媒体行显示 `media-player`、进度条与三枚控制；点中间按钮后图标从暂停变播放，播放状态同步；点上一首/下一首不崩
- 无媒体时展开窗 `1078,6..1482,142`，媒体行隐藏；截图 `island-media.png`、`settings-island-media.png`
- 未验证：真实 Spotify/浏览器/网易云会话的标题+歌手、SMTC 会话切换 1s 内、125%/150% DPI、无 SMTC 的 Win10 1903

## [Unreleased · v2.0 Phase 7 Dock 自由位置、收纳跟随与面板锁定]

### 新增

- 设置·Dock 页新增“距底部”滑杆（0–160px），实时改写 `dockBottomGap`；Dock 底边按该距离贴任务栏/屏幕底边
- 设置·Dock 页新增“锁定 Dock 位置”（`dockLocked`，默认开）；解锁后 Dock 顶部出现拖动把手，按住把手或空白处可移动到任意位置，写入 `dockCustomX/Y`
- 新增“Dock 回到居中”按钮：清除自定义坐标，按“距底部”回到屏幕水平居中
- 设置·面板页与面板右键菜单新增“锁定所有面板位置”（`panelsLocked`）；锁定后所有桌面面板不再响应拖动
- 收纳固定贴 Dock 正上方：`CollectorForm.AnchorToDock` 改为读取 Dock 实际 `Bounds.Top`，不再使用硬编码 152；启动、DPI 重排、设置开启、Dock 拖动时都立即重锚
- 收纳标题区不再可拖动，避免被拖离 Dock

### 变更

- Dock 横向位置收敛为“居中 + 自由拖动”，不再提供靠左/靠右三档；旧 `dockAlign` 仍兼容读取，但新写入统一 `center`
- Dock 拖动时收纳实时跟随；松手后落库一次，避免拖动过程频繁写盘
- 点 Dock 任意区域（不只收纳按钮）不再触发收纳外部收起

### 验证

- `bin\Liuli.exe` 341.5 KB；C#5 无新增 error
- 主屏 100% DPI：Dock `971,1344..1588,1444`；间距 8px 时收纳 `957,894..1601,1348`，玻璃底边距 Dock 顶 8px
- “距底部”=40px：Dock `971,1312..1588,1412`，收纳 `957,862..1601,1316`，跟随下移 32px
- 解锁后从 Dock 顶部把手拖到 `592,1180`：配置写入 `dockCustomX/Y=592/1180`，收纳同步到 `578,730..1222,1182`，玻璃底边距 Dock 顶 8px
- 锁定后拖动把手不再响应；`panelsLocked=true` 时面板 `OnMouseDown` 直接返回
- 截图 `dock-collector-above.png`、`settings-dock-gap-lock.png`

## [Unreleased · v2.0 Phase 7 Dock 常驻与位置设置]

### 新增

- Dock 默认关闭自动隐藏，常驻任务栏上方，不再触底弹出/离开收起；自动隐藏开关仍保留
- 设置·Dock 页曾提供“Dock 位置：靠左 / 居中 / 靠右”，保存到 `dockAlign`；已由上方“自由位置”阶段收敛为居中/拖动

### 修复

- 任务栏露出/缩回加 300ms 稳定判断；任务栏在 Dock 横向范围外露出时不强行拉起 Dock，避免两者在屏幕底边互相触发闪动

### 验证

- `bin\Liuli.exe` 338 KB；C#5 无新增 error
- 居中 `971,1344..1588,1444`；靠左 `8,1344..625,1444`；靠右 `1935,1344..2552,1444`
- 设置页点击“靠左”后 `dockAlign=left`，Dock 即时移到 `8,1344..625,1444`
- 截图 `settings-dock-position.png`

## [Unreleased · v2.0 Phase 7 Dock 收纳按钮切换]

### 新增

- Dock 收纳按钮改为切换：未显示时从该屏 Dock 打开并锚定到 Dock 上方；已显示时再点一次直接收起
- 收纳显示时，鼠标左键点桌面/其它窗口/任务栏自动收起；点收纳窗口内部或 Dock 收纳按钮本身不触发外部收起
- 收起同时把 `collectorOn=false` 写回配置，重启不自动弹回收纳；再点 Dock 按钮会重新置 true 并打开

### 修复

- 启动、DPI 重排、设置开启不再强制把收纳拉回 Dock 上方；从 Dock 打开时把锚定位置写回 `CollectorX/Y`，之后沿用，尊重用户拖动后的自定义位置

### 验证

- `bin\Liuli.exe` 336.5 KB；C#5 无新增 error
- 收纳可见时点 Dock 按钮：日志 `收纳已收起`，窗口 `vis=False`；再点一次打开 `958,846..1602,1300`，`CollectorX/Y=970/858`
- 打开后点桌面 `1000,500`：日志 `收纳已收起`；点收纳内部 `1000,900` 保持可见
- 手改 `CollectorX/Y=1625/735` 后启动，窗口 `1613,723..2257,1177`，不再强制回 Dock 上方
- 截图 `collector-toggle-anchored.png`
- 测试后 `shell.json` 恢复自动隐藏开启

### 未验证

- 真机副屏 Dock 按钮切换、125% / 150% DPI 下的外部点击与位置保持

## [Unreleased · v2.0 Phase 7 Dock 收纳入口、任务栏全隐藏与 DPI 回正]

### 新增

- Dock 右侧动作按钮扩为 4 个：桌面收纳 / 应用面板 / 控制中心 / 设置；收纳按钮在该 Dock 所在屏打开收纳，并居中贴在 Dock 上方 8px
- 设置 Dock 页新增“完全隐藏任务栏”开关，运行期隐藏主/副任务栏，2 秒守护重试；关闭或退出 Liuli 自动恢复

### 修复

- `WM_DPICHANGED` 后 120ms 合并重排 Dock、灵动岛、收纳、Hub、设置与面板，修复 125% → 150% → 100% 往返后尺寸漂移
- 收纳面板尺寸与保存改用 `UiScale`（DPI × 文字倍率），不再只乘文字倍率
- 启动发现残留 `taskbarFullHide=true` 时强制恢复任务栏并复位，避免异常退出后任务栏一直隐藏

### 验证

- `bin\Liuli.exe` 336 KB；C#5 无新增 error
- 主屏 Dock 617×100，四按钮依次为收纳/应用/控制/设置；点收纳窗口 `958,846..1602,1300`，底边距 Dock 8px
- `displayMode=all` 时副屏 Dock `-1589,1296..-972,1396`；点副屏收纳后窗口 `-1602,846..-958,1300`，日志 `screen=\\.\DISPLAY2`
- 任务栏开关 hide `n=2 any=False`，恢复 `n=2 any=True`；残留标记启动日志 `上次完全隐藏未正常恢复`
- 截图 `dock-collector-button.png`、`collector-on-secondary-dock.png`、`settings-taskbar-full-hide.png`、`taskbar-full-hide-dock.png`
- 测试后 `shell.json` 恢复 `primary`、自动隐藏开启

### 未验证

- 真机 125% / 150% / 100% 往返后的尺寸恢复（已加 `WM_DPICHANGED` 重排，需用户复测）
- 主副屏互换 / 双屏变单屏后的 profile 归属
- 任务栏在 2 秒守护间隔内被 Explorer 重新唤出的极端场景

## [Unreleased · v2.0 Phase 7 每屏 Dock 布局收口]

### 修复

- 关闭“每屏独立布局”不再清空 `dockProfiles`，只停用并保留；重新开启后副屏恢复原布局
- `dockProfiles` 加载/保存按 `screen` 去重，最多 4 条；达到上限时拒绝新增 profile，`Pin/Remove/Move` 不再误改共享列表
- “仅主屏”模式下 Dock 页不再显示无效果的每屏开关，改为文字说明
- `.lnk` 图标先解析 `TargetPath` 再取目标图标，并按不透明内容裁边铺满；7-Zip 在 Dock 内恢复 44px 槽位满幅

### 验证

- `bin\Liuli.exe` 333.5 KB；C#5 无新增 error
- 关闭开关 `profiles=1 kept=1`，副屏 576→625，再开启回 576
- 手改 6 条 profile 启动后保留 `DISPLAY2/3/4/5` 四条，DISPLAY2 取第一条 8 项
- 仅主屏页无可点开关；7-Zip 图标满幅，截图 `dock-shortcut-icon.png`、`settings-dock-primary-info.png`
- 测试后 `shell.json` 恢复 `primary`、8 项、0 profiles

### 未验证

- `IconContent` 对超大图标集的首次加载耗时；显示器改名/主副互换后的 profile 归属；面板/收纳每屏组合

## [Unreleased · v2.0 Phase 7 每屏独立 Dock 图标布局]

### 新增

- 设置 Dock 页新增“每屏独立布局”开关，配置字段 `dockPerScreen`
- `dockProfiles[]` 按 `Screen.DeviceName` 保存副屏 Dock 图标列表；副屏第一次排序/移除/固定时从共享 `dockItems` 深拷贝
- Dock 拖入、右键移除、拖拽排序与 Hub 右键固定都落到操作所在屏

### 修复

- Hub 右键固定后 Dock 不重排：`PinPaths` 成功后立即 `RelayoutDocks()`，宽度与槽位即时更新

### 验证

- `bin\Liuli.exe` 332 KB；C#5 无新增 error
- 副屏 Hub 固定 7-Zip：共享 `dockItems=8`、`DISPLAY2` profile `items=9`；副屏 Dock `576→625`（`-1593,1442..-968,1542`），主屏保持 `992,1442..1568,1542`
- 重启后 profile 仍 9 项，副屏 Dock 仍 625 宽；关闭开关 profiles=0，两块 Dock 立即回 8 项
- 截图 `dock-secondary-profile.png`、`settings-dock-per-screen.png`

### 未验证

- 双屏不同 DPI、真实拔插/改分辨率、显示器改名或主副互换后 profile 归属
- 新固定 7-Zip 的 Dock 图标偏小，属 `.lnk` 源图标尺寸问题，未在本项处理

## [Unreleased · v2.0 Phase 7 副屏任务栏与跨屏玻璃]

### 新增

- 任务栏多实例枚举：主 `Shell_TrayWnd` + 所有 `Shell_SecondaryTrayWnd`，`--selftest` 输出实例 rect
- `--taskbar-cycle` 诊断：隐藏/恢复全部任务栏并打印每屏可见状态

### 修复

- 副屏 Dock 不再只避让主屏任务栏：按 `targetScreen` 找到本屏任务栏，露出时上移一个任务栏高度，玻璃底边保持 8px 间距
- `Taskbar.HideAll/ShowAll` 覆盖全部任务栏实例，避免只隐藏主屏
- 多屏玻璃“填充/平铺”在每块屏重新起算导致接缝重复纹理：改为虚拟屏 `DrawSpan` 连续渲染

### 验证

- `bin\Liuli.exe` 329.5 KB；C#5 无新增 error
- 启动日志 `taskbars=TrayWnd=0,1392,2560,1440; SecondaryTrayWnd=-2560,1392,0,1440`，`wall span=1 screens=2`
- 副屏任务栏露出时副屏 Dock `-1568,1296..-992,1396`、主屏 Dock 收起；反向主屏 Dock `992,1296..1568,1396`、副屏收起
- `--taskbar-cycle`：hide/show 均 `n=2`，两屏 visible False→True
- 跨屏时钟面板拼接截图 `panel-seam.png`，接缝无错位/重复纹理（同 DPI）
- 截图 `dock-secondary-taskbar.png`

### 未验证

- 双屏不同 DPI 的真实接缝、真实拔插/改分辨率、任务栏左/右边缘、Explorer 重启重建副任务栏

## [Unreleased · v2.0 Phase 7 应用面板与控制中心]

### 新增

- 应用面板：扫描用户/公共开始菜单 99 个 `.lnk`，4×4 网格、16 项/页、搜索、分页；左键启动，右键固定到 Dock，按住拖到 Dock 固定
- 控制中心：音量/静音（点击切换、滚轮 4% 步进）、网络状态、蓝牙 radio、WMI 亮度循环；接口不可用时对应卡片自动隐藏
- Dock 与灵动岛快捷动作改为应用面板 / 控制中心 / 设置；托盘与自绘菜单新增两个入口

### 修复

- Hub 与 Dock 重叠：定位偏移 112→152，Hub 底边 1288、Dock 顶 1296，保留 8px 间距
- 亮度接口不可用时每 2 秒刷日志：首次失败后缓存并隐藏，后续不再查询
- 控制中心卡片原按剩余高度均分会拉得过高：改为最高 126 的紧凑卡片并垂直居中

### 验证

- `bin\Liuli.exe` 327.5 KB；C#5 无新增 error
- 应用面板 99 项、1/7 页；搜索 `edge` 1 项；右键固定 Edge `dockItems` 8→9
- 拖拽 7-Zip 到 Dock：`[drop] OLE 进入 Dock`、`[drop] OLE 放下 Dock`，`dockItems` 8→9，松手后 Hub 自动收起
- 控制中心音量 100%；静音 `True/False` 往返；滚轮 100→96→100；蓝牙/亮度不可用自动隐藏
- 点击面板外失焦收起、Esc 关闭；Dock 前两枚快捷按钮分别打开应用面板/控制中心
- 截图 `hub-apps.png`、`hub-search.png`、`hub-control.png`、`dock-hub-actions.png`

### 未验证

- 支持设备上的蓝牙卡片与亮度调节；125%/150% DPI、双屏不同 DPI；副屏唤出 Hub 的定位偏好；Dock 已满 24 项、跨屏拖拽

## [Unreleased · v2.0 Phase 7 多屏第一步]

### 新增

- `displayMode` 配置（默认 `primary`），设置 Dock 页新增“多屏模式：仅主屏 / 所有屏”
- `all` 时为每块非主屏创建独立 Dock，共享 Dock 图标配置；灵动岛仍在主屏

### 修复

- 每屏 Dock 只在自身屏幕底部热区唤出，避免鼠标在任一屏底部时所有 Dock 同时弹出
- 显示器变化/分辨率变化后重建副屏 Dock，并重排 Dock、灵动岛、面板、收纳和设置
- 面板与收纳按窗口所在屏工作区收回，缩屏或跨屏后不再留在屏外

### 验证

- `bin\Liuli.exe` 306.5 KB；C#5 无新增 error
- 本机双屏：DISPLAY1 `0,0 2560×1440`、DISPLAY2 `-2560,0 2560×1440`；`all` 日志 `screens=2 extraDocks=1`
- 鼠标主屏底部：主 Dock `y=1296`、副屏 `y=1444`；鼠标副屏底部：副屏 `y=1344`、主 Dock `y=1442`
- 模拟 `WM_DISPLAYCHANGE` 后 `reflow screens=2 extraDocks=1`，进程不崩、副屏 Dock 重建一套
- 切回“仅主屏”后副屏 Dock 关闭；截图 `settings-dock-multiscreen-primary.png`、`settings-dock-multiscreen-all.png`、`dock-two-screens.png`

### 未验证

- 真实拔插显示器、改分辨率；副屏任务栏自动隐藏/多实例；双屏不同 DPI；跨屏玻璃；每屏独立布局

## [Unreleased · v2.0 M6 文字大小四档]

### 新增

- 设置标题栏常驻“文字 小/标准/大/特大”四档（0.9 / 1.0 / 1.15 / 1.3），任意页可切换，写入 `shell.json` 的 `textSize`
- `Theme.TextScale` 统一倍率；Dock、灵动岛、面板、收纳、设置同步等比缩放，切换后即时重排，不重启

### 变更

- 面板与收纳 body 按文字档缩放，拖拽/缩放时把物理尺寸换算回逻辑配置，避免重启后二次放大
- 设置窗口尺寸随档位变化：0.9 810×648、1.0 900×720、1.3 1170×936

### 验证

- `bin\Liuli.exe` 302 KB；C#5 无新增 error
- 0.9 / 1.0 / 1.3 设置窗实测；收纳 580×409 / 644×454 / 838×591；Dock 1.3 754×131，玻璃主体底边 1382，任务栏顶 1392
- 重启保持 `textSize=3`；切回标准后 `textSize=1`
- 截图 `docs/screens/v8/settings-text-standard.png`、`settings-text-large.png`、`collector-text-large.png`、`dock-text-large.png`

### 未验证

- 125%/150% DPI 真机、1.15 档截图、多屏、面板实窗（当前 `panels=[]`）、0.9 档极长文件名换行

## [Unreleased · v2.0 M6 Dock 任务栏避让]

### 修复

- 任务栏自动隐藏时，Dock 不再固定贴屏幕底部挡住唤出的任务栏；`Taskbar.Revealed()` 区分“只剩 2px 边缘”和“真正露出”，露出时 Dock 上移任务栏高度，缩回后回底
- `TickAutoHide` 每 55ms 顺带 `TickPosition`，比原 320ms 定位更快让位
- 切换/恢复任务栏自动隐藏后立即刷新 Dock 位置

### 验证

- `bin\Liuli.exe` 300 KB；任务栏露出 top=1392、高 48，Dock 窗口 1296..1396、玻璃主体底边 1384，距任务栏顶 8px
- 关闭 Dock 自动隐藏后截图 `docs/screens/v8/dock-above-taskbar.png` 确认 Dock 与任务栏同时可见且不重叠

### 未验证

- 任务栏缩回后的回底连续实测、连续扫底、任务栏换边、125% DPI

## [Unreleased · v2.0 M6 日志诊断]

### 新增

- 系统页新增“日志诊断”区：日志路径、当前大小、`liuli.log.1` 轮转大小、总大小与 512 KB 规则
- “打开日志目录”和“复制诊断摘要”按钮；摘要含版本、DPI、日志/配置路径、大小与最近 16 行

### 变更

- `Log` 增加 `CurrentSize` / `RotatedSize` / `SizeText` / `Diagnostic`，只读元信息，不打开写句柄
- 剪贴板失败时 Toast 提示改开日志目录，不阻塞设置页

### 验证

- `bin\Liuli.exe` 299.5 KB；C#5 无新增 error
- 系统页日志区完整显示；复制摘要剪贴板内容含版本/DPI/路径/大小/最近 16 行
- 点击“打开日志目录”打开 `bin\data\logs`
- 注入 581,641 B 后触发一次写入：`liuli.log` 52 B、`liuli.log.1` 581,641 B；测试后恢复原日志并删除 `.1`

### 未验证 / 剩余

- 连续多次轮转、多进程同时写日志、日志目录无权限、剪贴板被其它程序占用
- 115% / 125% DPI 下文字是否裁切；TASK-062 仍未做

## [Unreleased · v2.0 M6 全局热键]

### 新增

- 系统页新增“快捷键”区，显示 `Ctrl+Alt+L` 显示/隐藏与 `Ctrl+Alt+G` 打开设置的实际注册状态
- “重新注册快捷键”按钮：先注销 id 11/12，再注册默认组合；失败不静默，Toast 提示占用

### 变更

- `V2Host` 捕获 `RegisterHotKey` 返回值，写入 `HotkeyToggleOk` / `HotkeySettingsOk` 和日志 `hotkey toggle=... settings=...`
- `V2Shell.HotkeyStatus()` / `ResetHotkeys()` 统一提供状态文本与重注册入口；默认组合固定，不新增自定义键位编辑

### 验证

- `bin\Liuli.exe` 298 KB；C#5 无新增 error
- `Ctrl+Alt+G` 注册成功并可打开设置；系统页显示“已注册”
- 当前机器 `Ctrl+Alt+L` 被其它进程占用，系统页如实显示“失败”；启动日志 `toggle=fail settings=ok`
- 点击重新注册后日志再次写入，页面不崩、文字不裁切；截图 `docs/screens/v8/settings-hotkeys.png`

### 未验证 / 剩余

- 关闭占用 `Ctrl+Alt+L` 的程序后能否恢复；非管理员账户；第三方热键冲突枚举
- 日志诊断（REQ-064）仍待补；TASK-060 仍未完成

## [Unreleased · v2.0 M6 开机自启]

### 新增

- 设置新增第 10 页“系统”（索引 8），首项为“开机自启”开关
- `ShellConfig.AutoStart` 读写 `autoStart`；`--selftest` 输出配置值与当前 Run 值
- `AutoStart` 写 `HKCU\Software\Microsoft\Windows\CurrentVersion\Run\Liuli`，值为带引号当前 exe；关闭删除，不写 HKLM
- `V2Shell.Boot` 每次按配置同步 Run 项，移动 exe 后下次启动自动修正路径

### 变更

- 设置导航由九页增至十页：主题 / 配色 / 面板 / Dock / 灵动岛 / 壁纸 / 桌面 / 收纳 / 系统 / 关于；`navRects` 扩为 10
- 页面索引：系统 8，关于 9；Dock/灵动岛上下文入口仍 3/4，桌面 6，收纳 7

### 验证

- `bin\Liuli.exe` 297 KB；C#5 无新增 error
- 开启后 Run=`"E:\meihuaDesktop\bin\Liuli.exe"`，`autoStart=True`；关闭后 Run 不存在，`autoStart=False`
- 把 exe+shell.json 复制到 `_scratch\move-test` 启动，Run 自动改为临时路径；恢复主 exe 启动后回到主路径
- 系统页十页导航完整，开关、启动项路径、后续说明不裁切；截图 `docs/screens/v8/settings-system.png`
- `--selftest` 输出 `autoStart=False run=`

### 未验证 / 剩余

- Windows 重启后的实际自启行为、非管理员受限账户、多用户切换、杀软拦截
- 快捷键配置（REQ-063）、日志诊断（REQ-064）仍待补；TASK-060 仍未完成

## [Unreleased · v2.0 M5 桌面组件]

### 新增

- 时钟面板支持 24/12 小时制与秒显示开关：右键菜单“时间制：24/12 小时”“显示秒：开/关”，`clock24` / `clockSeconds` 落盘
- 12 小时制显示上午/下午，24 小时制保持 `HH:mm`；秒显示为 `HH:mm:ss`
- 天气离线且仍有缓存时，面板在“城市 · 天气”后追加“离线缓存”；无缓存仍显示“天气离线”

### 变更

- `PanelForm.ApplyTickInterval`：秒显示 1 秒、系统读数 2 秒、其它内容 5 秒；系统采样仍为 `V2Shell` 2 秒一次
- 系统面板每 2 秒重绘，CPU/内存/磁盘变化更及时；时钟秒显示字号上限 32，避免窄面板裁切
- `--selftest` 输出 `clock24` / `clockSeconds`

### 验证

- 构建通过：`bin\Liuli.exe` 295.5 KB；C#5 无新增 error
- 12 小时+秒面板显示 `01:10:34`，24 小时+秒 `01:11:41`，24 小时无秒正常
- 右键菜单实际切换并写 `clock24=true/false`、`clockSeconds=true/false`；秒显示两帧哈希不同，系统面板两帧哈希不同
- 截图：`docs/screens/v8/clock-12h-seconds.png`、`clock-24h-seconds.png`、`clock-24h.png`、`panel-stats.png`

### 未验证 / 剩余

- 真实断网时天气缓存标记未强制回归；离屏反射已验证 `Weather.Offline=true` 时显示“离线缓存”。12 小时制跨中午、125% DPI、极窄面板未回归
- 主题快照不包含时钟格式/秒开关（外壳级偏好）

## [Unreleased · v2.0 M5 桌面收纳]

### 新增

- 新增“桌面收纳”单面板：分组标签（全部/应用/文件夹/文档/图片/影音/压缩包/代码/其他），620×430 可拖动/缩放，网格每页 18 项，滚轮翻页
- `CollectorState` 入口索引：只保存原路径，不移动/复制/删除桌面文件；扫描用户桌面 + 公共桌面，按扩展名/文件夹/`data\groups.txt` 自动分组
- 新桌面文件 2 秒轮询、5 秒内出现；删除/重命名为移除/新增入口；拖入文件加入口；双击打开；面板内拖动换组；右键移出只隐藏入口；拖出到资源管理器只允许 Copy
- 设置由八页增至九页，新“收纳”页：启用开关、自动收集、重新扫描、恢复移出、打开面板、去桌面页设置原生图标
- `shell.json` 新增 `collectorOn` / `collectorAutoScan` / `collectorX/Y/W/H` / `collectorGroup` / `collectorItems[]`

### 变更

- `V2Shell` 纳入收纳面板生命周期：启动/显示全部/隐藏全部/退出/显示器变化；托盘与右键菜单新增收纳开关和重新扫描
- 收纳开启时沿用桌面页“隐藏全部桌面图标”运行期隐藏；面板 tick 检测 Explorer 重启后原生图标重现并再次隐藏
- `V2SettingsForm` 导航索引：主题 0 / 配色 1 / 面板 2 / Dock 3 / 灵动岛 4 / 壁纸 5 / 桌面 6 / 收纳 7 / 关于 8

### 验证

- 构建通过：`bin\Liuli.exe` 293 KB；C#5 无新增 error
- 首次启动日志 `桌面入口同步 69 项，已写入 shell.json`；面板位置 `1900,96`、620×430，分组标签/4 页/翻页完整显示
- 点“文档”组显示 11 项；新建 `Liuli收纳测试.txt` 3 秒内总数 70、文档组出现；删除到回收站后 3 秒内回到 69
- 双击测试文件打开 Notepad；拖动测试文件到“应用”组，日志 `换组 ...（未移动文件）`，原文件仍存在
- 重启后 `collectorOn=true`、`collectorGroup=文档`、位置/尺寸保持；收纳页开关关闭/重开面板成功
- 截图：`docs/screens/v8/collector-panel.png`、`docs/screens/v8/settings-collector.png`

### 未验证 / 剩余

- `REQ-044` 绑定文件夹组件未实现，保留为后续独立能力
- 125% DPI、多屏、>500 项、Explorer 重启后原生图标再隐藏未真机回归
- 拖入/拖出的 OLE 全流程只做了代码路径，未用真实 Explorer 拖拽回归
- 二期多盒子（酷呆式独立盒子）未做；一期只有单面板 + 分组标签

## [Unreleased · v2.0 M5 桌面图标]

### 新增

- 设置新增“桌面”页：启动默认隐藏全部桌面图标（运行期，不写注册表）；此电脑/回收站/用户文件夹/网络/控制面板五个系统图标独立开关
- 五个系统图标写 `HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons`（NewStartPanel + ClassicStartMenu），通过 `SHChangeNotify` 刷新，不创建/删除快捷方式
- 桌面图标显隐使用 `SysListView32` 的 `ShowWindow`，启动记录原可见状态，正常退出恢复；运行期标记 `data\desktop-icons.runtime` 在异常退出后下次启动校准
- `ShellConfig` 已有 `desktopIconsHidden` / `hideShell*` 字段正式生效；`DesktopState` 统一封装五个 CLSID 与注册表写入

### 变更

- 设置由七页增至八页：主题 / 配色 / 面板 / Dock / 灵动岛 / 壁纸 / 桌面 / 关于；Dock/灵动岛上下文页索引保持 3/4
- `V2Shell.Boot` 在启动时先校准上次异常残留，再记录原始可见状态并应用桌面配置；`Quit` 恢复运行期隐藏

### 验证

- 构建通过：`bin\Liuli.exe` 274 KB；C#5 warning 无新增
- 首次启动日志 `[icons] 桌面 hidden=True visible=False`，桌面图标隐藏；切换“隐藏全部桌面图标”后 `visible=True`，`desktopIconsHidden=false`
- 五个系统图标逐个点击：此电脑/回收站/用户文件夹/网络显示后桌面项数 69→70、隐藏后 70→69；注册表对应值 0/1 正确
- 资源管理器重启后，已设为显示的系统图标仍在（桌面项数保持）
- 隐藏状态下从 Liuli 菜单“退出”：桌面图标恢复显示，`desktop-icons.runtime` 删除，`shell.json` 保留用户偏好
- 截图：`docs/screens/v7/settings-desktop.png`

### 未验证 / 剩余

- Windows 11 25H2 桌面不再绘制“控制面板”图标；开关能写注册表与配置，但桌面项数不变。Win10 1903 未回归。
- 125% DPI、多屏未回归
- “隐藏全部桌面图标”只在当前桌面视图生效；Explorer 重启后由下次启动重新应用

## [Unreleased · v2.0 M5 壁纸页]

### 新增

- 设置新增“壁纸”页：当前壁纸缩略预览、当前文件名/路径/系统样式、本地图片选择（jpg/jpeg/png/bmp/webp）、0–80% 压暗
- 首次换图前记录原系统壁纸路径、样式与平铺；“恢复系统壁纸”按原样式写回并清空记录
- `shell.json` 新增 `wallpaperDim` / `wallpaperOrig` / `wallpaperOrigStyle` / `wallpaperOrigTile`；`ThemeSnapshot` 同步保存壁纸压暗
- 玻璃底新增黑色 alpha 压暗层，在磨砂/模糊之后、平均色取色之前执行；压暗同时影响自动明暗与自动强调色

### 变更

- 设置由六页增至七页：主题 / 配色 / 面板 / Dock / 灵动岛 / 壁纸 / 关于；关于页索引变为 6
- `ApplyVisualSettings(true)` 改为“应用主题 → 重烘 → 再应用主题”，让新壁纸的明暗/强调色进入同一轮重绘
- `LoadTheme` 顺序改为“应用快照 → 写壁纸 → 应用主题 → 重烘 → 再应用主题”，避免压暗没进重烘或重烘后仍用旧 tone
- 松手才重烘压暗滑杆；拖动中只更新 `Config.WallpaperDim` / `Theme.WallpaperDim`

### 验证

- 构建通过：`bin\Liuli.exe` 268.5 KB；C#5 warning 无新增
- 壁纸页 7 页导航、预览、路径、样式、滑杆、选择/恢复按钮在 900×720 内完整显示
- 原生文件对话框实际选中 `_scratch\wallpaper-test.png`：Windows 壁纸切换、`wallpaperOrig` 记录 `img0.jpg`、预览更新
- 压暗 0→41→79：`wallpaperDim` 落盘；日志 `dim=79 mean=37,41,44 light=0`，设置页自动切深色主题
- “恢复系统壁纸”按钮：写回 `img0.jpg`、`wallpaperOrig` 清空、预览刷新
- 主题快照含 `wallpaperDim`：`WallpaperDim=42` → 保存 → 重新加载 → `ApplyTo` 后仍为 42（反射验证）
- 截图：`docs/screens/v7/settings-wallpaper.png`

### 未验证 / 剩余

- WebP 解码依赖系统 WIC，未用真实 WebP 文件回归
- 125% DPI、多屏、换壁纸后 5 秒自动重烘计时未测
- 原生选择对话框的自动化仅在本机用真实鼠标 + 剪贴板粘贴路径验证，未覆盖不同 DPI/输入法
- 主题快照含压暗的真机保存/加载 UI 未回归

## [Unreleased · v2.0 M5 配色]

### 新增

- 设置新增“配色”页：强调色来源三选一（跟随壁纸 / 预设 / 自定义），6 个预设色卡，自定义取色（系统 `ColorDialog`，取消不改）
- `shell.json` 新增 `accentMode` / `accentPreset` / `accentCustom`；`ShellConfig.ResolveAccent` 按模式统一写入 `Theme.Accent`
- 6 套预设：雾蓝 / 青瓷 / 紫雾 / 暖砂 / 绯红 / 石墨；浅色主题自动压暗
- `ThemeSnapshot` 增加配色三项，保存/加载主题时一并恢复；手动改配色清空 `activeTheme`
- `--selftest` 输出 `accent=mode preset=N custom=#...`

### 变更

- 设置由五页增至六页：主题 / 配色 / 面板 / Dock / 灵动岛 / 关于；Dock、灵动岛齿轮上下文页索引后移一位
- `RestoreVisualDefaults` 同时把强调色来源恢复为 auto
- 坏值收敛：未知 `accentMode`→auto、`accentPreset` 越界→0–5、非法 `accentCustom`→`#7EB2FF`

### 验证

- 构建通过：`bin\Liuli.exe` 263.5 KB
- 6 预设逐一点击 `accentPreset` 0→5；设置页色卡选中态、组件 accent 同步
- 自定义取色 `#FF80FF` 全局生效（设置标题、导航、Dock 运行点/图标高亮、灵动岛）；取消不改
- 主题保存含 `accentMode=custom / accentPreset=5 / accentCustom=#FF80FF`；切到 auto 后加载主题恢复 custom
- 坏配置收敛为 `auto/5/#7EB2FF`；重启保持
- 截图：`docs/screens/v6/settings-palette.png`、`settings-palette-preset.png`、`settings-palette-custom.png`、`m5-dock-accent.png`、`m5-island-accent.png`

### 未验证 / 剩余

- 真实键盘 `Ctrl+Alt+G` 注入未稳定复现，本轮用 `WM_HOTKEY` 消息验证入口
- 换壁纸联动 5 秒未重测（沿用 M1 的 `Backdrop.Tick → ApplyTheme` 路径）
- 6 套预设目前只切强调色；每套独立玻璃染色/描边/文字层级、描边强度/文字对比滑杆、全组件 token 审查与重算耗时仍待补

## [Unreleased · v2.0 M4 主题与设置]

### 新增

- 主题快照 `ThemeSnapshot`：名称、保存时间、Dock 三项、灵动岛八项、玻璃五项、壁纸路径/样式/平铺、面板布局深拷贝
- `themes[]` / `activeTheme`：最多 6 个；同名覆盖、超限淘汰最早一条、删除二次确认；加载恢复整套外观与面板
- `V2SettingsForm`：900×720 自绘玻璃五页——主题 / 面板 / Dock / 灵动岛 / 关于
- 主题页：玻璃浓度、磨砂、圆角、自动/深色/浅色、保存（原生 TextBox 命名）、恢复外观默认、主题库加载/覆盖/删除
- 面板页：新建、逐面板四类内容 chip、置顶/置底、删除；6 个上限
- Dock 页：显示开关、自动隐藏、小/标准/大三档；灵动岛页：显示开关、宽/高/顶部偏移/动画、天气/系统读数/快捷动作
- 关于页：版本、运行态面板/主题数量、快捷键、打开数据目录、关闭设置
- `OpenSettings(page)` 上下文入口：Dock 齿轮落 Dock 页、灵动岛齿轮落灵动岛页；托盘/热键/菜单落主题页

### 变更

- 主题/玻璃/磨砂/圆角/明暗、Dock/灵动岛/面板手动修改后立即清空 `activeTheme`，不再把已偏离状态标成“当前主题”
- 磨砂滑杆松手才重烘壁纸；其他滑杆即时重绘，设置页 180ms 后重建以同步“当前”徽标
- `Ctrl+Alt+G`、托盘双击、Dock/灵动岛齿轮统一进入 v2 设置；旧右键菜单仍保留“打开设置”作为兜底
- `--selftest` 增加 `themes=N active=...` 与逐主题字段输出

### 验证

- 构建通过：`bin\Liuli.exe` 259 KB
- 保存/加载/覆盖/删除/6 上限/手动修改清当前均真机通过；加载主题后 1 面板内容与玻璃 100% 恢复
- 面板 6 上限、行内删除二次确认、Dock/灵动岛显隐重建通过
- 截图：`docs/screens/v5/settings-theme.png`、`settings-panels.png`、`settings-dock.png`、`settings-island.png`、`settings-about.png`
- **未验证**：直接中文输入法命名、跨壁纸加载、坏 `themes[]` 真机回归、125% DPI/多屏、真实键盘热键注入

## [Unreleased · v2.0 M4 面板]

### 新增

- 玻璃面板 `PanelForm`：最多 6 个，默认不创建；按住主体拖动，右下角 22px 热区缩放，位置/尺寸/内容即时写 `shell.json`
- 面板四类内容：空容器 / 时钟 / 天气 / 系统读数；`PanelContent` 与容器解耦，切换内容不重建窗口、不改变位置尺寸
- 面板右键菜单：切换四类内容、置顶/置底、新建面板、删除此面板
- `panels[]` 配置：`id`、`content`、`x/y/w/h`（body 屏幕坐标）、`topMost`；W 220–720、H 120–520，越界/坏值收敛到主屏工作区
- 入口：Dock 左侧“+”快捷动作、灵动岛“+”快捷动作、托盘/主菜单“新建面板”
- 透明区点击穿透：`WM_NCHITTEST` 在玻璃 body 外返回 `HTTRANSPARENT`，body 内返回 `HTCLIENT`
- `--selftest` 输出 `panels=N` 与每个面板的内容/边界/置顶状态

### 变更

- `V2Window` 仅在 `TopMost=true` 时加 `WS_EX_TOPMOST`；面板可降为普通 z 序，被普通窗口覆盖
- 主菜单“显示/隐藏 Dock 与灵动岛”改为“显示/隐藏桌面层”，面板一并显隐
- Dock/灵动岛“+”快捷动作从占位提示改为新建面板

### 验证

- 构建通过：`bin\Liuli.exe` 225 KB；`--selftest` 读到 2 个面板及边界
- 新建：Dock “+” 与灵动岛 “+” 均成功创建面板，默认出现在主屏右侧
- 拖动：`2180,160` → `1880,360`，松手写盘；缩放：`340×200` → `440×280`，写盘
- 内容：同一面板 `clock → weather → stats → empty`，`x/y/w/h` 全程保持 `1880/360/440/280`
- 多面板：两个面板同时显示，第二面板内容切到天气；重启后两个面板位置/内容恢复
- 透明区：`WM_NCHITTEST` 阴影 `1870,350=-1`、body 内 `1900,380=1`、屏幕外 `=-1`
- 置底：第二面板 `topMost=false` 后 `WS_EX_TOPMOST` 位清除（`0x08090080`），记事本覆盖该面板；第一面板 `topMost=true`（`0x08090088`）仍压在记事本上
- 删除：右键“删除此面板”后面板关闭、`panels` 计数减少
- 截图：`docs/screens/v4/m4-panel-clock.png`、`m4-panel-weather.png`、`m4-panel-stats.png`、`m4-panel-empty.png`、`m4-panel-menu.png`、`m4-panels-two.png`
- **未验证**：125% DPI、多屏跨屏拖动、6 面板长时 CPU、面板与 M5 壁纸/配色联动

## [Unreleased · v2.0 M3]

### 新增

- 灵动岛尺寸/偏移可配置：`islandWidth` 360–520（默认 380）、`islandHeight` 100–180（默认 112）、`islandTop` 0–80（默认 6）
- 展开/收起动画可配置：`islandAnimMs` 0–400（默认 180），`0` 为立即切换，非零按 16ms cubic ease-out 插值
- 内容开关：`islandWeather` / `islandStats` 沿用，新增 `islandQuick`；关闭后不绘制对应区域，快捷动作也不再命中
- 收起尺寸由展开尺寸派生：宽 63% 夹 200–320、高 36% 夹 34–54，窗口统一加 12 逻辑像素阴影边距
- 托盘/灵动岛菜单新增“灵动岛设置”子菜单：宽度、高度、顶部偏移、展开动画按档位循环，天气/读数/快捷动作开关，以及“恢复灵动岛默认”
- `--selftest` 输出 `island.width/height/top/animMs/quick/weather/stats`

### 变更

- 菜单项改为 `MouseUp` 触发：按下只记录行号，抬起且仍在同一行才执行，避免按下即关菜单后误触刚弹出的子菜单
- 子菜单改为 140ms 定时器延迟打开，父菜单关闭后再显示
- 灵动岛配置改动时 `ApplyConfig()` 立即停止动画并落位，不做过渡

### 修复

- 设置子菜单在父菜单 `MouseDown` 中立即打开，父菜单 `MouseUp` 会命中新子菜单行；改为延迟打开并统一鼠标抬起触发
- 灵动岛设置个别行首次点击不命中：菜单行高/内边距统一按 34/14 逻辑像素计算，点击与绘制共用同一 `items` 顺序，并改为鼠标抬起触发；本轮设置入口连续打开通过，多轮边界点击未压测

### 验证

- 构建通过：`pwsh -NoProfile -File tools\build.ps1` → `bin\Liuli.exe` 214.5 KB
- 默认值：`--selftest` 输出 `width=380 height=112 top=6 animMs=180 quick=True weather=True stats=True`
- 越界收敛：手改 `width=999 height=1 top=-100 animMs=999` 后自检输出 `width=520 height=100 top=0 animMs=400`
- 真机默认：收起 `263×64`，展开 `404×136`；截图 `docs/screens/v3/m3-default-collapsed.png` / `m3-default-expanded.png`
- 真机改宽 420：收起 `289×64`；改高 136：展开 `444×160`、收起 `289×73`；改顶部偏移 16：窗口 y 从 6 到 16；动画 260 捕捉到中间 `395×133`
- 动画 0：指针移入后立即扫描即为 `404×136`，无中间尺寸；180/260 可见过渡
- 内容开关：`islandQuick=false` 展开态无快捷按钮（`m3-quick-off.png`）；`islandWeather=false` 收起态改显 CPU（`m3-weather-off.png`）；`islandStats=false` 展开态无 CPU/内存/磁盘读数（`m3-stats-off.png`）
- 菜单：主菜单“灵动岛设置”与 8 行子菜单真机打开，截图 `m3-main-menu.png` / `m3-island-menu.png`；“恢复默认”写回 380/112/6/180 与三个 `true`
- 重启保持：420/136/16/260 重启后仍为 `289×73` 收起、`444×160` 展开
- **未验证**：125% DPI、动画 260ms 的主观手感、菜单多轮压力点击；M2 遗留项不变
- 集成：M3 经 PR #51 以 merge commit `4091f8f` 合入 `main`，Issue #50 关闭

## [Unreleased · v2.0 M2]

### 新增

- Dock 自动隐藏：`shell.json` 新增 `dockAutoHide`（默认关闭）；托盘与快捷菜单可切换
- 底部 6 逻辑像素热区唤出，离开约 650ms 收起；170ms cubic ease-out 滑入/滑出
- 隐藏位置按屏幕底边计算，任务栏露出时 Dock 也能完全移出屏幕
- Dock 三档尺寸预设：`dockPreset=small/standard/large`，整体缩放 0.86/1.0/1.14；托盘/快捷菜单循环切换并持久化
- Dock 运行指示点：每 2s 一次进程枚举并缓存，仅 app 项绘制 accent 小圆点；`.lnk` 暂不解析
- Dock 拖拽排序：移动超过 8px 进入排序，显示 accent 插入竖线，松手写回 `shell.json`
- Dock 悬停放大：指针进入后 140ms ease-out 到 1.08，按下仍 0.94，拖拽时禁用
- `m2-dock-presets.png`：三档 Dock 同背景离屏对照（494×86 / 576×100 / 658×114）
- `m2-dock-running.png`：运行指示放大对照（explorer / SystemSettings）
- `m2-dock-reorder.png`：拖拽插入位置对照（开头 / 中间 / 末尾）
- `m2-dock-hover.png`：普通态 / 悬停第 4 项对照
- `--selftest` 输出 `dockAutoHide` 与 `dockPreset`；`--preview` 输出隐藏态窗口位置与三档尺寸

### 修复

- 任务栏常显时 Dock 上移量翻倍：`BaseTopY()` 不再重复扣 `Taskbar.VisibleHeight()`；真机 top 从 1248 修正为 1296（Issue #47）
- 资源管理器/计算器等 exe 的运行指示映射补齐；`calc.exe` 映射到 `CalculatorApp / Calculator / WindowsCalculator`

### 验证

- 构建通过，`bin\Liuli.exe` 209 KB
- 配置读写：临时写入 `dockAutoHide=true` 后自检读取为 `True`；临时写入 `dockPreset=large` 后自检读取为 `large`，测试后恢复原文件
- 隐藏几何：`dockHidden top=1442`，`screenBottom=1440`
- 三档尺寸：small 494×86、standard 576×100、large 658×114；对照图 `docs/screens/v2/m2-dock-presets.png`
- 运行指示：`--preview` 检测到 `dockRunning=explorer,SystemSettings`，放大对照图 `docs/screens/v2/m2-dock-running.png`
- 拖拽排序：`--preview` 生成开头/中间/末尾插入位对照 `docs/screens/v2/m2-dock-reorder.png`
- 悬停放大：`--preview` 生成普通态/第 4 项悬停态对照 `docs/screens/v2/m2-dock-hover.png`
- 三档尺寸：真机菜单循环 `standard→large→small→standard`，实测 `576×100 / 658×114 / 494×86`；`large` 重启保持，恢复 `standard` 后重启保持
- 运行指示：启动记事本 3s 后圆点出现，关闭 3s 后消失；截图 `m2-real-running-on/off.png`
- 按压态：Edge 按下出现按压底色并缩小，移出后松开取消、未启动；截图 `m2-real-pressed.png`
- **未验证**：外部文件拖入、125% DPI、2s 枚举 CPU 长时占用；按压 0.94 未逐像素测量
- 集成：M1+M2 经 PR #49 以 merge commit `c222f91` 合入 `main`；逐级 Draft PR 与 Issue #35/#37/#39/#41/#43/#45/#47 已关闭

## [Unreleased · v2.0 M1]

### 新增

- 新外壳 `V2Shell`：启动不再创建旧看板窗口；托盘菜单与 `Ctrl+Alt+L` / `Ctrl+Alt+G` 热键
- 底部玻璃 Dock：默认 3 个系统入口 + 5 个 Windows 内置应用；左键启动、拖入固定（COPY）、右键移除、配置持久化
- 顶部灵动岛：收起时间/天气，悬停展开日期、天气、CPU/内存/磁盘与三个快捷动作；不抢焦点
- `bin\data\shell.json` 独立配置：默认生成、原子写入、坏文件备份回退
- `Taskbar`：读取/设置 `ABS_AUTOHIDE`，托盘内切换与恢复；Dock 检测任务栏露出时上移避让
- `Liuli.exe --selftest` 无界面自检入口
- `Liuli.exe --preview` 离屏预览：不显示窗口，把 Dock 与展开态灵动岛合成到 PNG
- Dock / 灵动岛点击反馈：按下 0.94 缩放与按压底色，释放 190ms 回弹、280ms accent 波纹；右侧圆按钮同步
- `m1-preview-pressed.png` / `m1-feedback-compare.png`：静止态与按压态对照，供视觉评审
- Dock 应用图标改用 Shell `SHIL_JUMBO` 256px 图像列表；失败回退 32px，缓存按尺寸区分

### 变更

- `Program.Main` 从 `App.Boot()` 切换到 `V2Shell.Boot()`；旧 `App`/`BoardWindow` 仍编译但不创建
- `DropTarget` 增加可指定 COPY/MOVE 的重载，Dock 固定不再移动源文件
- `Theme` 增加按窗口缩放取字的 `Font(px, style, scale)` 重载

### 验证

- `pwsh tools/build.ps1` 构建成功（193 KB）
- `Liuli.exe --selftest`：`shell.json` 生成成功，默认 8 个 Dock 项；任务栏 `ABS_AUTOHIDE=1`、隐藏状态 `rect.top=1438`
- 坏 JSON 回退：写入非对象 JSON 后自检生成 `.bad-*` 备份并重建默认 8 项
- **未验证**：真机 UI 启动/截图、125% DPI、Dock 点击/拖入/右键移除、任务栏开关恢复、热键与退出清理

## [1.5.0] - 2026-11-26

### 新增

- 文件看板绑定独立收纳目录（`board.folder`，默认 `文档\Liuli\<看板标题>`）；改标题不再悄悄换目录
- 收纳目录双向同步：外部新增/删除/重命名后约 4 秒内看板自动对齐；目录外的入口保持不动
- 设置「数据 → 打开收纳目录」一键打开收纳根

### 变更

- 拖入文件始终移动到看板绑定的目录，而不是按当前标题临时拼路径
- 整理生成/更新的文件看板写入真实分组目录

### 验证

- 构建通过；文件看板自动同步需实机验证（外部新建/删除/重命名各一次）
- 未验证：大量文件（>100）目录的同步耗时；4 秒轮询对闲置 CPU 的影响

## [1.4.2] - 2026-11-26

### 变更

- 文件看板图标网格按内容高度自适应：默认 330×300 尽量露出 3 行图标，不再一打开就截掉半行；看板拉高后仍保持 88px 行高

### 验证

- 构建通过；临时 10 项文件看板实机截图确认 3 行可见、第 4 行滚动

## [1.4.1] - 2026-11-26

### 修复

- 撤销整理后的空目录清理只作用于默认 `文档\Liuli`；自定义收纳根不再被自动删除，避免误删用户自己的空文件夹

### 验证

- 构建通过；自定义收纳根路径下撤销时 `CleanupRoot` 直接返回，默认路径逻辑不变

## [1.4.0] - 2026-11-26

### 新增

- 文件看板升级为图标网格：按看板宽度 4/3/2 列，40px 图标在上、文件名在下，接近酷呆/Fences 盒子；保留单击打开、按住拖出的手势
- 设置「数据 → 收纳位置」可选择收纳根目录（`settings.storeRoot`），默认 `文档\Liuli`，桌面不再出现 `Liuli` 文件夹
- 收纳根内按看板标题自动建子目录，拖入/整理仍为真实移动，撤销按清单移回原位

### 变更

- 「整理时移动原件」与整理确认框文案改为显示实际收纳路径（`文档\Liuli\<分组>`）
- 拖入成功 toast 改用实际收纳路径，不再硬编码 `桌面\Liuli`

### 验证

- 构建通过；图标网格用 10 项临时文件看板实机截图确认（4 列、图标 + 截断文件名、可滚动）
- `Icons.MoveIntoGroup` 实机移动测试：桌面测试文件 → `C:\Users\10195\Documents\Liuli\测试\`，成功；测试文件与目录已清理
- 设置「数据」页收纳位置显示默认路径，`FolderBrowserDialog` 可正常打开到「文档」并取消
- 未验证：整桌 71 项真实整理/撤销在 1.4.0 下的完整回归；图标网格的拖出/拖入手势需用户复测

## [1.3.3] - 2026-11-26

### 修复

- 单击条目立即打开导致无法拖出：文件条目改为按下只记录、移动超过系统拖拽阈值才 `DoDragDrop` 拖出，松开且未拖动才打开；连续点击 400ms 内只打开一次
- 拖出成功（`Move`）后自动移除失效条目，避免看板留下已不存在的路径
- OLE 拖入改为直接按 `CF_HDROP` 读取 `IDataObject`，不再依赖不可靠的 WinForms 接口转换，修复拖入文件列表为空的问题

### 验证

- 构建通过；单击打开、拖出、拖入的真实手势仍需本机复测（脚本合成拖拽不能可靠模拟资源管理器拖放）

## [1.3.2] - 2026-11-26

### 修复

- 拖文件进看板完全无反应：`DragAcceptFiles` 声明在错误的 `user32.dll`，实际导出在 `shell32.dll`，调用一直抛入口点缺失并被空 `catch` 吞掉，窗口从未接受拖放（#21）
- 补注册 OLE `IDropTarget`：先 `OleInitialize` 再 `RegisterDragDrop`，分层窗口也能稳定接收资源管理器拖拽；保留 `WM_DROPFILES` 兜底
- 拖放日志细化：`OLE 进入/放下`、`收到拖入 N 项`、注册失败警告；不再每次启动刷注册成功日志

### 验证

- 修复后 11 块看板 `RegisterDragDrop` 全部返回 `hr=0`
- 真实拖拽仍需人工确认一次（脚本合成拖拽启动不了资源管理器拖放循环）

## [1.3.1] - 2026-11-26

### 修复

- 拖入文件板移动原件时没有写入 `move-manifest.json`，导致「撤销上次整理」移不回该文件；现在每次拖入移动都追加清单记录（#17 后续）
- 拖入处理补一条 `[drop] 收到拖入 N 项，看板 …` 日志，便于排查拖放问题

### 验证

- 收纳后的文件看板单击「应用」条目：`ASUS DriverHub.lnk` 打开对应窗口（标题 `ASUS DriverHub - Google Chrome`）
- 单击「文档」条目：`26事业下岗位汇总（截止11月19号16点）.xlsx` 在 WPS 中打开（窗口标题含该文件名）
- 拖入移动本次仍未能自动化验证：跨进程伪造 `WM_DROPFILES` 的 HDROP 在目标进程内无效（全局内存句柄不跨进程共享），日志显示消息到达但文件列表为空；需用户真实拖拽确认
- 版本号 1.3.0 → 1.3.1

## [1.3.0] - 2026-11-26

### 新增

- 设置 → 看板新增「整理时移动原件」开关（默认关）：开启后一键整理会把桌面文件/文件夹真实移入 `桌面\Liuli\<分组>`，桌面不再保留副本（#17）
- 真实整理前弹确认框，明确目标目录、不留副本、可通过「撤销上次整理」移回；`data\move-manifest.json` 逐项记录原路径→新路径，中途中断也能按清单恢复
- 文件看板拖入：开启移动后，把桌面（用户桌面/公共桌面）上的文件拖进文件看板，会把原件移入 `桌面\Liuli\<看板标题>` 并入库；来自 `Liuli` 内部或其它位置的文件仍只生成入口
- 「撤销上次整理」同时移回原件：逐项容错，原目录不可写时退回用户桌面并记警告；全部成功后才删除移动清单与 `Liuli` 空目录

### 变更

- 公共桌面（`C:\Users\Public\Desktop`）条目只收入口、不移动，避免系统级目录写权限失败导致中断
- 整理/撤销日志补充移动与失败计数，便于事后核对
- 版本号 1.2.1 → 1.3.0

### 验证

- 真机 70 项桌面：开启移动整理 → 桌面顶层仅剩 `Liuli`，生成 7 块分类看板，日志 `移动原件 70 项`，清单存在
- 立即撤销：70 项全部回到原位，桌面顶层恢复 70 项，`Liuli` 目录与清单均自动删除，看板回到整理前 4 块
- 首轮测试曾因公共桌面不可写导致撤销中断（53+2 项手工恢复）；修复后公共桌面不再参与移动，撤销逐项回退，上述完整流程复测通过
- 拖文件入板触发真实移动仅代码实现，未做真实 OLE 拖放验证（见 `docs/TESTING.md`）

## [1.2.1] - 2026-11-26

### 修复

- 桌面控制条压住模态编辑窗的「取消/保存」按钮与设置页底部行：打开 `EditorForm` 或设置面板时自动收起控制条，关闭后按「显示控制条」配置恢复（#14）

### 变更

- 版本号 1.2.0 → 1.2.1

### 验证

- 打开「新增待办」编辑窗：底部胶囊消失，取消/保存完整可见；Esc 关闭后胶囊恢复
- 打开设置面板：胶囊消失；关闭设置后胶囊恢复
- 手势回归（同一版本）：待办新增、代码新增/复制、文件夹双击打开、右/左下/右下缩放均通过（详见 `docs/TESTING.md`）

## [1.2.0] - 2026-11-26

### 新增

- 设置 → 看板新增「开机自启」开关：写当前用户 `HKCU\Software\Microsoft\Windows\CurrentVersion\Run\Liuli`，值为带引号的 exe 全路径（#12）
- 启动时自动校准：配置为开但注册表缺失时补写，配置为关但注册表残留时清除，避免移动 exe 或手工改配置后状态不一致（#12）

### 变更

- 版本号 1.1.0 → 1.2.0

### 验证

- 打开开关：注册表值 `"E:\meihuaDesktop\bin\Liuli.exe"`，`autoStart=true`，日志 `[app] 开机自启 on …`
- 关闭开关：注册表值删除、`autoStart=false`，日志 `[app] 开机自启 off …`
- 手工改配置为 true 并删除注册表值后重启：自动补写成功
- v1.2.0 闲置：WS 53.6MB、私有 30.9MB、exe 151KB

## [1.1.0] - 2026-11-26

### 新增

- 图标整理支持「撤销上次整理」：整理前把 `boards.json` 快照到 `data\undo-organize.json`，撤销时恢复快照并重建看板；只保留最近一步（#3）
- 自定义图标分组：`data\groups.txt` 按 `扩展名=分组` 覆盖默认归类，分组名须在 应用/文件夹/文档/图片/影音/压缩包/代码/其他 之内，非法规则告警并忽略（#3）
- 设置 → 看板新增「撤销上次整理」按钮与自定义分组说明（#3）

### 变更

- 版本号 1.0.2 → 1.1.0
- 设置面板「看板」页行距随新按钮下移（不影响其它页）

### 验证

- 整理 4 块 → 11 块，快照生成；撤销后恢复 4 块、快照删除，日志可追溯
- `groups.txt` 规则加载有日志；非法分组名被忽略
- 设置项回归：#2 中滑块、壁纸来源/恢复系统壁纸、暂停/静音配置全部通过（详见 `docs/TESTING.md`）

## [1.0.2] - 2026-11-26

### 修复

- 隐藏桌面图标无效（Win11 26200）：`FindWindow("Progman")` 在该版本返回 0，且 `SysListView32` 是 DefView 的孙辈，`FindWindowEx` 查不到；改为 `EnumWindows` 定位 Progman/WorkerW → DefView，再用 `EnumChildWindows` 递归找图标列表（#8）
- 「已经是隐藏状态」误判：原判断把“当前可见”和“想要隐藏”直接比较，导致开关只写配置不发命令；改为目标态判断（可见且要显示 / 不可见且要隐藏）（#8）
- 图标显隐操作补日志（`[icons] 桌面图标 nowVisible=... wantHidden=...`），便于追溯（#8）

### 验证

- 设置页点击「隐藏桌面图标」：`hideDesktopIcons=true`，桌面图标实际消失，日志 `nowVisible=False` 通过
- 再次点击恢复：`hideDesktopIcons=false`，图标恢复，日志 `nowVisible=True` 通过
- 退出/重启后开关与实际状态一致

## [1.0.1] - 2026-11-25

### 修复

- 便签双击失效：第一次单击会进入拖拽并触发磁吸，看板位移后客户端坐标变化导致双击判定失败；改为按屏幕坐标判定，并让磁吸只在真实拖动（位移 > 3px）后生效（#6）
- 待办勾选框点不动：整行命中区后入栈，倒序命中时永远压住勾选框；调整为先登记整行、后登记勾选框（#6）
- 编辑窗保存重复触发：Ctrl+Enter 同时命中外层与文本框处理器，导致重复 Save/Close；加保存幂等（#6）

### 变更

- 图标整理结果写入日志（`[icons] 整理 N 个图标，新建 M 块文件看板`），便于追溯（#6）

### 验证

- 真机回归：双击便签 → 编辑窗 → Ctrl+Enter / 保存按钮 → `boards.json` 落盘通过
- 勾选待办首项 → `boards.json` `done=true` 通过
- 连续两次图标整理：看板总数与文件看板数保持不变（11 / 7）通过
- 导出 + 导入备份往返通过
- 闲置 CPU 0.06%、Working Set 56.0MB（70s 预热后 30s 采样）

### 已知问题

- 隐藏桌面图标开关在 Win11 26200 无效，仅写配置不改桌面，见 #8

## [1.0.0] - 2026-11-25

### 新增

- 六类玻璃看板：便签 / 待办 / 文件入口 / 代码片段 / 天气与系统状态 / 时钟
- 看板自由拖拽、八方向缩放、屏幕与看板间磁吸、折叠、置顶、锁定、配色、透明度
- 玻璃拟态渲染：壁纸同源烘焙磨砂、薄纱、上下渐变、强调色呼吸、噪点、顶边高光、底边折射
- 可选实时玻璃（DWM 亚克力，GPU 合成）
- 设置中心：玻璃浓度、磨砂强度、圆角、明暗、磁吸、总在最前、控制条、隐藏桌面图标、壁纸来源、压暗、暂停、静音
- 桌面控制条：新建看板 / 一键整理图标 / 设置；位置记忆
- 一键桌面图标整理：按类型生成入口看板，不改动桌面文件
- 壁纸引擎：图片走系统壁纸通道；视频尝试 WMP + WorkerW，失败自动回退
- 备份导出/导入（zip），原子保存与坏文件回退
- 托盘、单实例、Ctrl+Alt+L / Ctrl+Alt+G 热键
- 日志轮转、错误兜底

### 修复

- 中文源码无 BOM 导致 csc 按 ANSI 解析乱码：构建脚本统一补 BOM
- 看板 id 在同一毫秒内重复：改用 GUID 派生
- 控制条默认坐标 -1 被误判为副屏有效位置：改为未设置哨兵
- 天气 HTTPS 在旧 TLS 默认下失败：显式启用 TLS1.2
- 设置面板固定深色底与浅色主题文字冲突：改为跟随主题的玻璃底
- 状态看板 260px 高时底部统计被裁切：默认高度调到 300

### 已知问题

- 视频壁纸依赖 Media Feature Pack，缺组件环境未验证（#1）
- 设置项与编辑交互未全量真机回归（#2）
- Win10 1903 / 多 DPI 未验证（#4）


