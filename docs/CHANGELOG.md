# 变更日志

遵循 Keep a Changelog；版本遵循 SemVer。

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


