# 开发流程执行清单 · Liuli

本文件把用户 AGENTS.md 的开发规范落到本仓库，v2.0 全程执行。规范冲突时，以用户当轮明确指令为准。

## 1. 阶段闸门

| 阶段 | 必须产物 | 未满足不进入下一阶段 |
| --- | --- | --- |
| 需求 | Issue（背景/目标/验收/环境）+ 需求文档 | 需求不清、验收不可测 |
| 设计 | `docs/DESIGN_V2.md`（模块、接口、数据、兼容、风险、回滚） | 无设计或关键取舍未记录 |
| 任务 | `docs/TASKS_V2.md`（原子任务、REQ 引用、验证方式） | 任务跨天、无验收 |
| 实现 | 功能分支 + 小步提交 | 直接在 main 改、巨型提交 |
| 测试 | 单元/手动记录、失败复现用例、构建日志 | 未跑就声称通过 |
| 评审 | PR（摘要/关联 Issue/测试/风险/回滚）+ 自检 | 无 PR 直接合入 |
| 发布 | tag、Release Notes、CHANGELOG | 无留痕、版本号不明 |

## 2. 分支与提交

- `main` 保持可发布，不直接提交；功能/修复/文档各开分支。
- 命名：`feat/v2-*`、`fix/v2-*`、`docs/v2-*`、`experiment/*`。
- 提交信息：Conventional Commits，关联 Issue，例如 `feat: 实现 Dock 默认组合 (#33)`。
- 一个提交只做一件事；不提交 `bin/`、`dist/`、`shots/`、密钥、`.env`、本地配置。
- 合并优先 squash；不 force push 共享分支；合并前解决冲突并跑通构建。
- 版本遵循 SemVer；发布打 tag；CHANGELOG 按 Keep a Changelog。

## 3. 代码与测试

- C# 5 语法；源码 UTF-8 BOM；构建走 `tools/build.ps1`。
- 新功能必须有测试或可复现的手动验证；修缺陷先补失败用例。
- 提交前跑构建、关键路径手动测试；结果如实写进 PR/Issue，失败不掩盖。
- 分层窗口、DPI、多屏、拖放等系统交互，必须在真机记录截图/日志。
- 性能预算：1080p < 80MB，4K/多屏 < 120MB，闲置 CPU < 1%。

## 4. 视觉与验收

- 每个里程碑出同角度截图，按“对齐/间距/字号/材质/配色/动效”六项评审。
- P0 视觉缺陷（文字看不清、控件重叠、材质破功）未修不得进入下一阶段。
- 功能完成但视觉不达标，不算完成；UI 美观是发布阻断项。

## 5. 留痕

- Issue：状态 待办 → 进行中 → 待评审 → 待验证 → 已关闭；关闭前写验收结论。
- 设计取舍：结论 + 原因 + 备选方案，写进设计文档或 Issue 评论。
- 测试：命令、环境、结果、截图路径；未验证的明确标注“未验证”。
- 会话：`CONTEXT.md` 记录当前任务、决策、开放问题、偏差。
- 发布：CHANGELOG、tag、Release Notes、资产校验。

## 6. 当前仓库状态（2026-11-26）

- `main` 已同步到 `19ed1bc`（PR #34 squash 合并，docs v2 基线）；Issue #33 已关闭。
- M1 Issue #35 OPEN；工作分支 `feat/v2-shell`，已推送；Draft PR #36。
- M2 Issue #37 OPEN；工作分支 `feat/v2-dock-autohide`，从 `feat/v2-shell` 切出；Draft PR #38（base `feat/v2-shell`）；自动隐藏代码已构建通过，真机热区/动画待验证。
- TASK-021 Issue #39 OPEN；工作分支 `feat/v2-dock-presets`，从 `feat/v2-dock-autohide` 切出；Draft PR #40（base `feat/v2-dock-autohide`）；三档尺寸与离屏对照已构建通过，真机切换待验证。
- TASK-022 Issue #41 OPEN；工作分支 `feat/v2-dock-running`，从 `feat/v2-dock-presets` 切出；Draft PR #42（base `feat/v2-dock-presets`）；运行指示已构建通过，真机状态变化待验证。
- TASK-023 Issue #43 OPEN；工作分支 `feat/v2-dock-reorder`，从 `feat/v2-dock-running` 切出；Draft PR #44（base `feat/v2-dock-running`）；拖拽排序与插入线已构建通过，真机拖动待验证。
- TASK-024 悬停放大 Issue #45 OPEN；工作分支 `feat/v2-dock-hover`，从 `feat/v2-dock-reorder` 切出；Draft PR #46（base `feat/v2-dock-reorder`）；已构建通过，真机手感待验证。
- 任务栏常显 bug Issue #47 OPEN；工作分支 `fix/v2-dock-taskbar-offset`，从 `feat/v2-dock-hover` 切出；修复 `BaseTopY` 双重扣除，真机 `top=1296` 验证通过，PR 待创建。
- 已提交 `3be11bb`、`62ce47c`、`7282f77`、`87c92b7`、`0bbc294`、`92eab42` 及文档提交。
- v1.6 Dock/灵动岛原型停在 `experiment/v1.6-dock-island`（`28e8cfd`），不合入 main，不作为 v2.0 基线。
- v2 外壳代码已构建成功（203.5 KB）；`--selftest` 验证默认 shell.json、任务栏自动隐藏状态、`dockAutoHide` 与 `dockPreset`；真机 UI/截图待做。
- `docs/REQUIREMENTS.md` 已确认；`docs/DESIGN_V2.md`、`docs/TASKS_V2.md` 已合入 main。

## 7. Definition of Ready / Done

**Ready**：Issue 有背景、目标、验收；需求文档确认；设计评审通过；任务拆分到可验证单元。

**Done**：REQ 验收逐条通过；测试与截图留档；性能未回退；配置可回退；文档更新；Issue 关闭；PR 合并；发布留痕齐全。

