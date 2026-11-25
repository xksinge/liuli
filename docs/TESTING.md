# 测试记录

## 环境

| 项 | 值 |
| --- | --- |
| 系统 | Windows 11 专业版 10.0.26200 |
| CPU / GPU | 本机（AMD RX 6700 4GB） |
| 屏幕 | 双屏 2560×1440，副屏 X=-2560，缩放 100% |
| 壁纸 | `C:\WINDOWS\web\wallpaper\Windows\img0.jpg`，样式 10（填充） |
| 编译器 | `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe` |
| 构建 | `pwsh -File tools\build.ps1` → `bin\Liuli.exe`（约 147KB） |

## 性能实测（2026-11-25）

| 指标 | 结果 | 测量方式 |
| --- | --- | --- |
| 常驻内存（Working Set） | 52.7MB | `(Get-Process Liuli).WorkingSet64` |
| 私有内存 | 29.7MB | `PrivateMemorySize64` |
| 闲置 CPU | 0.39%（单核） | 进程 `TotalProcessorTime` 8 秒增量 |
| 磨砂烘焙 | 235~266ms | 日志 `[wall] 磨砂重烘 ... ms=` |
| exe 体积 | 146.5KB | 构建输出 |

结论：内存与闲置 CPU 均达到既定目标（<80MB / <1%）。

## 已跑通

- 编译（csc，C# 5，UTF-8 BOM）
- 启动、四块默认看板、托盘、控制条、Ctrl+Alt+G 打开设置
- 天气拉取（Open-Meteo，`道孚 -3° 晴`），TLS1.2 修复后成功
- 设置面板：明暗分段切换（auto → dark 生效并保存）
- 实时玻璃：DWM 亚克力开启后看板变为实时模糊的深色玻璃
- 导出备份：生成 `data\backups\Liuli-backup-20261125-2325.zip`
- 图标整理：扫描桌面 → 生成文件入口看板（不移动桌面文件）

## 未验证 / 待验证

- 编辑器（便签双击、待办回车新增、代码卡片编辑）未做真机输入回归
- 导入备份未跑
- 视频壁纸（WMP + WorkerW）未在本机实测
- Win10 1903、125%/150%/200% 缩放、负坐标多屏之外的布局
- 隐藏桌面图标开关未点击验证
- 拖拽/八方向缩放/磁吸未做逐项手势回归

以上均已在 GitHub Issues 建单跟踪，不在此处标记为“通过”。
