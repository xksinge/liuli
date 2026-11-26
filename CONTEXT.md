# CONTEXT · Liuli

## Resume

- 当前任务：Issue #45 / M2 Dock 悬停放大
- 状态：M1 在 `feat/v2-shell`（PR #36 Draft）；M2 自动隐藏 `feat/v2-dock-autohide`（PR #38 Draft）；尺寸预设 `feat/v2-dock-presets`（PR #40 Draft）；运行指示 `feat/v2-dock-running`（PR #42 Draft）；拖拽排序 `feat/v2-dock-reorder`（PR #44 Draft）；悬停放大 `feat/v2-dock-hover` 已实现并构建通过（209 KB）；**真机悬停/拖动/点击未验证**
- 分支：`feat/v2-dock-hover`（从 `feat/v2-dock-reorder` 切出）
- PR：自动隐藏 `#38`、尺寸预设 `#40`、运行指示 `#42`、拖拽排序 `#44`；悬停放大 PR 待创建；M1 PR #36 仍 Draft
- 下一步：提交悬停放大并开 Draft PR（base `feat/v2-dock-reorder`），再做 M2 真机验收
- 阻塞：无；用户此前要求“不运行”，因此未启动 UI，不能声称视觉验收通过
- 约束：旧看板功能封存；不改动 `boards.json` 与 `Documents\Liuli`

## Session Log

| 日期 | 摘要 | 产物 |
| --- | --- | --- |
| 2026-11-26 | v1.5.0 目录绑定与 4 秒同步发布 | Issue #31 / PR #32 / Release v1.5.0 |
| 2026-11-26 | 发现产品方向偏离，重建 v2.0 GlassBoard 功能对齐需求 | `docs/REQUIREMENTS.md`、Issue #33 |
| 2026-11-26 | 补充 Dock 系统入口、桌面图标默认隐藏、任务栏自动隐藏、壁纸取色、屏幕/文字适配、UI 视觉闸门 | Requirements / Issue #33 评论 |
| 2026-11-26 | 建立项目开发流程执行清单 | `docs/DEVELOPMENT.md` |
| 2026-11-26 | 需求文档落分支；v1.6 原型停到 experiment 分支，工作区恢复干净 | `docs/v2-requirements` @ adbcd6f；`experiment/v1.6-dock-island` @ 28e8cfd |
| 2026-11-26 | PR #34 以 squash 合入 main，Issue #33 关闭 | main `19ed1bc` |
| 2026-11-26 | 创建 M1 Issue #35，开 `feat/v2-shell` | Issue #35 |
| 2026-11-26 | 实现 v2 配置/任务栏/Dock/灵动岛/入口切换，构建通过 | 提交 3be11bb、62ce47c、7282f77、87c92b7 |
| 2026-11-26 | Dock/灵动岛增加点击按压、回弹、波纹反馈；生成按压态对照 | 提交 a7b7b3e |
| 2026-11-26 | Dock 应用图标改用 256px Shell 图像列表，图标探针通过 | 提交 f3f5a1b |
| 2026-11-26 | 创建 M2 Issue #37，开 `feat/v2-dock-autohide`；实现 Dock 自动隐藏/热区/170ms 动画 | Issue #37；构建 201.5 KB |
| 2026-11-26 | 创建 Issue #39，开 `feat/v2-dock-presets`；实现三档尺寸与离屏对照 | Issue #39；构建 203.5 KB |
| 2026-11-26 | 创建 Issue #41，开 `feat/v2-dock-running`；实现运行指示圆点与放大对照 | Issue #41；构建 205 KB |
| 2026-11-26 | 创建 Issue #43，开 `feat/v2-dock-reorder`；实现拖拽排序与三插入位对照 | Issue #43；构建 207.5 KB |
| 2026-11-26 | 创建 Issue #45，开 `feat/v2-dock-hover`；实现悬停 1.08 放大与普通/悬停对照 | Issue #45；构建 209 KB |

## Key Decisions

- D-001：v2.0 基线为 GlassBoard 功能对齐，不是文件看板工具。
- D-002：v1.x 看板/整理/收纳同步封存，代码与数据保留。
- D-003：保留玻璃底、分层窗口、Theme/Glass、配置、天气/系统读数等底层能力。
- D-006：桌面图标默认隐藏；系统图标分桌面/Dock 两套开关；Dock 默认混合系统入口 + 自带应用。
- D-007：任务栏优先持久化自动隐藏；不做永久移除/替换 Explorer。
- D-008：壁纸自动取色默认开，6 套预设 + 自定义，全局统一 Theme token。
- D-009：Per-Monitor V2；默认单屏；多屏先做“所有屏 Dock + 主屏灵动岛”。
- D-010：UI 美观是 P0 发布闸门。

## Open Questions

1. Dock 默认常显还是自动隐藏：M1 按常显实现，自动隐藏/动画留 M2。
2. 灵动岛首版只做时间/天气/系统读数，媒体与通知留 M3。
3. 多屏首版“所有屏 Dock + 主屏灵动岛”尚未实现，留 Phase 7。
4. 任务栏完全隐藏仍作为 P1 实验项，M1 只做自动隐藏。
5. 图标收纳仍按 P1。
6. GlassBoard 版本/截图/推荐配置：现有 10 张参考图已用于需求，M1 未逐像素对标。

## Divergences

- v1.6 Dock/灵动岛原型已停在 `experiment/v1.6-dock-island`（28e8cfd），不作为 v2.0 实现基线，不合入 main。
- v2 外壳已切到 `feat/v2-shell`；`Program.Main` 不再 `App.Boot()`，旧看板窗口不会创建。
- `bin\Liuli.exe` 已是 v2 构建（199 KB），但**未启动 UI 验证**；`--selftest` 只验证配置/任务栏状态/图标提取。
- `bin\data\shell.json` 已由自检生成，含 8 个默认 Dock 项；`boards.json` 未改动。
- 坏 `shell.json` 回退已测：非对象 JSON → `.bad-*` 备份 + 重建默认 8 项。
- 离屏预览已生成 `docs/screens/v2/m1-preview.png` 与按压态 `m1-preview-pressed.png` / `m1-feedback-compare.png`（`--preview`，不显示窗口）；真机截图仍待做。
- 任务栏完全隐藏、桌面图标隐藏、Dock 自动隐藏动画、主题预设、多屏均未在 M1 实现，按 TASKS_V2 后续阶段处理。

## M1 已知限制（未验证 ≠ 已完成）

- Dock 常显，未做自动隐藏/唤出动画（M2）。
- 桌面图标仍按系统现状显示；默认隐藏与系统图标开关在 M5。
- 灵动岛只有时间/天气/CPU/内存/磁盘与 3 个快捷动作；媒体/通知在 M3。
- 文字缩放已按窗口 DPI 计算，但只在代码层验证；125% 真机未测。
- 任务栏自动隐藏设置、恢复、Dock 避让均为代码实现；selftest 只读取当前状态，未做开关回归。
