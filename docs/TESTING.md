# 测试记录

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


