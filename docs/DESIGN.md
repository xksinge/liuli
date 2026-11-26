# 设计说明 · Liuli 琉璃

## 1. 目标与边界

目标：把桌面变成可自由划分的玻璃看板，长期常驻，内存 < 80MB、闲置 CPU < 1%，核心功能完全离线。

不做：

- 不做安装器、不做账号、不做云同步、不做广告位
- 默认不移动/删除用户的桌面文件；图标整理只生成“入口”。仅在用户打开「整理时移动原件」后，才把用户桌面文件真实移入 `桌面\Liuli\<分组>`（可撤销、有清单）
- 不整屏截屏做玻璃（会把窗口、图标、桌宠一起烤进去）
- 不从 GlassBoard 复制代码或资源，只借鉴“机制 + 参数 + 踩坑结论”

## 2. 技术选型

| 项 | 选择 | 原因 |
| --- | --- | --- |
| 运行时 | WinForms + GDI+ 全自绘 | 系统自带、零外部 DLL、启动快 |
| 窗口 | `UpdateLayeredWindow` 分层窗口 | 每块看板独立 alpha、圆角干净、无 WM_PAINT 闪烁 |
| 玻璃底 | 壁纸同源烘焙 + 低分辨率磨砂 | 只在换壁纸时算一次，逐帧零模糊 |
| 实时玻璃 | `SetWindowCompositionAttribute` + `ACCENT_ENABLE_ACRYLICBLURBEHIND` | DWM/GPU 合成，可开关 |
| 持久化 | 极简 JSON + 原子替换 | 无第三方库，坏文件可回退 |
| 输入 | 独立普通窗口编辑 | 分层窗口无法挂 IME，中文输入必须交给普通窗 |

## 3. 模块

```
Base.cs        路径、版本、日志（512KB 轮转）
Util.cs        颜色/缓动/几何/文字/文件/极简 JSON
Native.cs      全部 P/Invoke（ULW、DWM、WorkerW、DPI、热键、拖放、剪贴板）
ImageFx.cs     缩放、Cover、三遍盒滤近似高斯、Frost、均值
Theme.cs       token、明暗自适应、字体缓存、圆角/描边/投影/噪点、矢量图标
Model.cs       BoardKind / Item / Board / Settings / Workspace 默认四块
Store.cs       加载容错、原子保存、zip 导出/导入
Wallpaper.cs   注册表取壁纸路径与样式、按样式渲染到虚拟屏
Backdrop.cs    整屏磨砂底单例：烘焙、签名轮询、切片、帧烘焙
Glass.cs       玻璃主体绘制（投影/切片/薄纱/渐变/色光/噪点/描边/高光/底边折射）
BoardWindow.cs 看板窗体：拖动/缩放/吸附/折叠/右键菜单/拖入文件/命中区
BoardPaint.cs  六类内容绘制与命中区登记
SettingsForm.cs 设置中心（自绘开关/滑块/分段/按钮）
BarWindow.cs   桌面控制条（新建/整理/设置）
Weather.cs     Open-Meteo + 30 分钟缓存 + WMO 中文 + 矢量天气图标
Stats.cs       CPU/内存/磁盘采样
Icons.cs       桌面扫描、Shell 图标缓存、分类整理
EditorForm.cs  文字编辑窗（真 TextBox，保 IME）
MenuForm.cs    玻璃右键菜单
ToastForm.cs   玻璃胶囊提示
WallEngine.cs  图片壁纸（系统通道）/ 视频壁纸调度与回退
VideoWall.cs   WMP COM 控件挂 WorkerW 壁纸层
App.cs         控制器：窗口、托盘、热键、定时器、设置联动
Program.cs     入口、单实例、DPI、异常兜底
```

## 4. 玻璃管线

### 4.1 烘焙底（默认）

1. 从注册表读 `WallPaper`（回退 `TranscodedWallpaper`）与 `WallpaperStyle`/`TileWallpaper`
2. 按样式渲染到虚拟屏的 1/4 分辨率（双屏 5120×1440 → 1280×360）
3. `Frost`：去饱和 + 压暗 + lift；再三遍盒滤近似高斯，半径 `2 + BlurPct * 0.10`
4. 求均值 → 判断明暗与强调色（`PickTone`）
5. 每块看板绘制时，按屏幕坐标 1:1 切片贴到本地画布，不做模糊

这样模糊成本只发生在换壁纸/换分辨率时（实测约 235ms，一次性）。

### 4.2 实时玻璃（可选）

`SetWindowCompositionAttribute` → `ACCENT_ENABLE_ACRYLICBLURBEHIND`（state 4），tint 用 `0xAABBGGRR`。
探针对比结论（Win11 26200 + RX 6700）：state 4 有明显模糊；state 3（BLURBEHIND）几乎只是均匀灰；state 5 无模糊。
拖拽期间临时关闭，避免跟手延迟。`SetWindowCompositionAttribute` 失败时自动使用烘焙底。

### 4.3 玻璃表层

投影 → 薄纱（`14 + GlassPct * 0.56` alpha）→ 上下渐变 → 左上强调色光 → 128×128 噪点 → 上亮下暗描边 → 顶边高光 → 底边折射。
所有圆角/描边/投影只从 `Glass.cs` 与 `Theme.cs` 走，窗口里不再自创数值。

## 5. 数据

`bin\data\boards.json`：

```json
{ "app":"Liuli", "version":"1.2.1", "settings":{...}, "boards":[{ "id":"b...", "kind":0, "x":32, "y":32, "w":330, "h":300, ... }] }
```

- `W/H` 是含 16px 投影外留白的窗口尺寸，玻璃主体 = `Glass.Body`
- `Accent`：-1 跟随壁纸，0..5 六套固定色
- 保存：写 `*.tmp` 后 `File.Replace`；加载失败把坏文件改名为 `.broken-<时间戳>` 再回退默认
- 备份：`ZipArchive` 打包 boards/settings/weather/icons 索引，文件名 `Liuli-backup-YYYYMMDD-HHmm.zip`
- 导入前先自动备份当前状态

### 5.1 图标整理与撤销

- 扫描用户桌面 + 公共桌面（隐藏项跳过），按扩展名分组：内置表 + 可选 `data\groups.txt`（`扩展名=分组`）覆盖；只接受 应用/文件夹/文档/图片/影音/压缩包/代码/其他 八个组名，非法规则告警后忽略
- 默认整理不移动、不删除桌面文件，只生成文件看板入口；同名文件看板原地更新，否则靠主屏右侧两列新建，单板最多 40 项
- 可选真实移动（`settings.organizeMove=true`）：整理前弹确认框，确认后把用户桌面文件/文件夹移入 `文档\Liuli\<分组>`（收纳根可用 `settings.storeRoot` 改，默认 `Environment.SpecialFolder.MyDocuments\Liuli`），桌面不留副本；公共桌面（`C:\Users\Public\Desktop`）条目只收入口、不移动，避免系统目录写权限导致中断
- 移动逐项写 `data\move-manifest.json`（`{"moves":[{"from":…,"to":…}]}`），每移一项原子重写一次，崩溃/断电后可再次撤销或按清单恢复
- 整理前把 `boards.json` 复制为 `data\undo-organize.json`（只保留最近一步）；「撤销上次整理」先按清单把原件移回（逐项 try/catch，原目录不可写时退回用户桌面并记警告），再覆盖配置、`Store.Load` 重新解析、`RebuildAllWindows` 重建窗口；只有全部移回成功才删除清单与收纳根空目录
- 每个文件看板持久化 `folder`（绝对路径），默认 `文档\Liuli\<看板标题>`；标题只用于第一次推导，之后改名不再换目录
- 收纳目录每 4 秒做一次双向对齐：目录内已不存在的条目移除、目录内新增条目补进来；目录外的入口保持不动。只扫一层，不递归
- 文件看板采用图标网格（按宽度 4/3/2 列，40px 图标 + 下方单行文件名），视觉接近酷呆/Fences 盒子；单击 tile 打开，按住超过阈值才启动 `DoDragDrop`，拖到资源管理器即移出
- 文件看板拖入：桌面文件拖入会把原件移入看板绑定的 `folder` 后入库；已在收纳根内或非桌面路径的文件只加入口，避免二次移动
- 拖放实现：`DragAcceptFiles`（`shell32.dll`）+ OLE `IDropTarget`（`OleInitialize` + `RegisterDragDrop`）双通道，分层窗口优先走 OLE；两条通道都汇到 `OnFilesDropped`
- 取舍：撤销按整文件快照恢复，会把整理后对其它看板的改动一并回退；换来实现简单、可解释，适合“整理错了立刻反悔”的场景

### 5.2 开机自启

- 只用当前用户 `Run` 项（不需要管理员）：`HKCU\Software\Microsoft\Windows\CurrentVersion\Run`，值名 `Liuli`，内容为带引号的 `Application.ExecutablePath`
- 开关改配置后立即写/删注册表并落盘；启动时 `SyncAutoStart()` 以配置为准补写或清除，防止 exe 移动后仍指向旧路径
- 不做计划任务、不写 HKLM，卸载/删除 exe 后最多残留一个指向不存在路径的 Run 值，用户可在任务管理器启动项里关掉

## 6. 性能预算

| 指标 | 目标 | 手段 |
| --- | --- | --- |
| 常驻内存 | < 80MB | 单进程、单一整屏底图（约 1.8MB）、图标按需缓存并限量 |
| 闲置 CPU | < 1% | 定时器自适应：有动画 16ms，闲置 200ms；只在 dirty 时重绘 |
| 模糊 | 不在绘制帧里做 | 换壁纸时烘焙一次；实时玻璃交给 DWM |
| 启动 | 快 | 无外部依赖，先出看板再拉天气 |

## 7. 壁纸引擎

- 图片模式：调用 `SystemParametersInfo(SPI_SETDESKWALLPAPER)` 走 Windows 自己的壁纸通道，零常驻开销；退出时还原原壁纸与样式（原值同时写入 `HKCU\Software\Liuli` 兜底）
- 视频模式：`AxHost` 承载 WMP COM 控件，`SetParent` 到 `WorkerW` 壁纸层，循环播放、默认静音；全屏前台窗口时暂停；WMP 不可用时回退系统壁纸并提示

## 8. 与 GlassBoard 的关系

只参考了这些结论：壁纸必须从文件/专用壁纸层取，不能整屏截屏；玻璃要有顶边高光 + 统一描边 + 圆角三件套；分层窗口不适合直接输入。代码、目录、token 体系均为本项目独立实现。
