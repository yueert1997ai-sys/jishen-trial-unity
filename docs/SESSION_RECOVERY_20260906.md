# 聊天丢失后的项目恢复入口

保存日期：2026-09-06。此文件是当前对话与磁盘状态的接续摘要，不是逐字聊天导出，也不是整个工程的备份。请先阅读本文再继续工作，禁止根据旧聊天印象回退工程。

## 用户要求与工作边界

- 项目：《机神试炼》，Unity 上帝视角机甲 Roguelite / Survivor-like 原型，面向未来手游。当前用途是香港理工大学 IME 申请作品，目标约 10-15 分钟可玩 Demo。
- UI 优先手机横屏；战斗要主动、响应快，场景有建筑和实体障碍，表现精细，避免幼稚高饱和配色和大面积纯色色块。
- 射击：左摇杆移动，右摇杆按住拖动瞄准射击，释放停火；独立挥刀键。桌面 WASD、鼠标瞄准、左键射击、右键/Q 挥刀、Space 冲刺、E 导弹、Esc 暂停。
- 冲刺要求明显的身体动作、背包推进器反馈；桌面不锁 60fps。不要把离屏渲染时序当成显示帧率或手机性能。
- 主角建模由另一边并行处理。不能覆盖、回退或随意重做其 Blender 源文件；现有已接入模型以实际工程和最新交接为准。
- 保留已有成果，不重开项目、不大规模重构。完成修改需实际运行，不能仅凭编译或 Animator 状态宣布通过。
- 尽量后台工作，不接管用户桌面。所有新增产出、构建、中间文件、日志和备份放 D 盘。

## 实际位置与环境

- 根目录：`D:\project-mecha-design\MECH ROUGE`。
- `C:\Users\yue\Documents\MECH ROUGE` 已核实是指向上述路径的目录连接，不要在 C 盘重建工程副本。
- Unity：`6000.3.18f1`；主场景：`Assets/Scenes/Demo_Main.unity`。
- 存储规则：根目录 `AGENTS.md`、`STORAGE_LOCATION.md`。软件安装位置仍可作为读取依赖。

## 已提交基线与最新未提交工作

当前分支：`codex/industrial-dash-polish`。本记录创建前 HEAD：`3b1f054`。

历史提交：

- `9cc9513`：手机优先 Demo 与完整流程验证。
- `bb7018e`：有授权的 Rigged robot 主角与骨骼动作。
- `5159dd6`：工业场景模块、实体掩体、推进器冲刺、桌面不限帧。
- `3b1f054`：手动双摇杆射击、真实近战挥刀、低饱和材质和运行验证。

**HEAD 不等于当前全部成果。** 2026-09-06 磁盘上已有后续主角接入工作，尚未提交：

- `Assets/Art/JishenTrial_Assembly_V3/`：冻结的 V3 整机资产。
- `Assets/Prefabs/Player/JishenTrialVisual.prefab` 与其 `.meta`。
- `Assets/Scripts/Player/RigidMechPoseDriver.cs` 与其 `.meta`。
- `Assets/Editor/JishenHeroIntegration.cs`、`tools/jishen/`。
- 原 `PlayerMech_Guest.prefab`、`RiggedMechAnimator.cs`、`MechDashPresentation.cs`、`HeroVisualAudit.cs` 及相关 README/交接已修改。
- `art_prototypes/`、`docs/CORE_GAME_DESIGN.md` 等也有未跟踪内容；不要直接清理或覆盖。

最新主角状态以 `docs/JISHEN_GAME_INTEGRATION.md` 为准：V3 整机通过刚性部件动作适配接入，右手实体刀/光刃、右肩炮口、现有背包喷口与当前战斗事件相连。不要再声称当前只是旧白色免费角色。

## 构建与验证边界

- 最新已核实存在的模型接入版：`Builds/Windows/MECH_TRIAL_20260905.154503/MECH_TRIAL.exe`。
- 本次读取其 `AuditEvidence/jishen/player-smoke/player-report.json`：95.003 秒正常速度战斗、71 击杀、51 冲刺、1 次强化、3 次重启；成功，0 Error / 0 Warning。**只是冒烟，不是完整 Boss 通关**。
- 新主角文档记录 ManualCombatAudit 与 HeroVisualAudit 通过；详细边界、动作问题及来源见其交接。本次保存记录没有重新运行这些测试。
- 上一版 `20260905.144944`：已验证完整 606.38 秒、六次强化、498 击杀、324 冲刺、两阶段 Boss、胜利与三次重开，0 Player Error / Warning。证据 `docs/audit-evidence/2026-09-05/manual-player-01/`。不可将其完整通关结论直接套到新主角版。
- 手动战斗交接：`docs/MANUAL_COMBAT_HANDOFF.md`。旧构建和旧角色保留回退，但不应默认恢复。

## 最新玩法设计，不等于已实现

磁盘已有 `docs/CORE_GAME_DESIGN.md`，记录另一边于 2026-09-06 确认的下一阶段约定：固定机体，武器/背包从敌人取得并永久收藏；装备是否替换由玩家选择；局内随机 buff 结束清空；战斗间隙整理装备。该文档明确这些新系统仍待开发，不要把永久仓库、装备吸收和新成长闭环报成已经完成。

## GitHub / MacBook 接续

用户今晚继续开发，明天要用 MacBook 接续。

- 本次实际执行 `git remote -v`，输出为空；工程没有配置远程仓库。
- 当前可见对话没有用户提供的私有仓库地址。不要猜账号、仓库名或擅自创建公开仓库；下一步请用户给私有仓库链接。
- 用户请求先保存记录，尚未授权将全部未提交模型工作推送上网。本次仅保存恢复文档，不提交他人正在制作的代码/资产，不推送。
- 收工时保存 Unity/Blender，核对所有新增模型、贴图、Prefab、脚本和 `.meta`；提交 `Assets`、`Packages`、`ProjectSettings`、必要建模源文件与交接。
- 排除缓存、构建、无关个人文件以及可能含敏感信息的原始聊天日志。不能直接 `git add .`。
- 尤其注意：`docs/chat-recovery-20260906/original_rollout_snapshot.jsonl` 是约 393 MB 的原始记录，不能误当普通源码上传 GitHub。此前“大文件检查”没有覆盖这个后来发现的聊天备份。
- 推送后做独立克隆检查，确保新资产不是仅存在于工作区。Mac 安装相同 Unity 版本、克隆正确分支，打开主场景。Windows EXE 不是 macOS 应用。
- 跨设备每次开工先拉取、收工提交推送，避免两台机器同时改同一 Scene/Prefab。

## 已有聊天备份与读取顺序

磁盘已有 `docs/chat-recovery-20260906/机甲建模师_聊天记录.md`、`visible_messages.json` 和原始日志快照。本次只确认文件存在，未审计其完整性；它们不能代替本任务的接续摘要，也不能保证包含所有任务的对话。

新任务读取顺序：

1. 根目录 `AGENTS.md` 和本文。
2. `docs/CORE_GAME_DESIGN.md`：最新未来玩法约定。
3. `docs/JISHEN_GAME_INTEGRATION.md`：当前新主角接入状态。
4. `docs/MANUAL_COMBAT_HANDOFF.md`：现有输入、近战、配色。
5. 执行 `git status`，再看对应代码、Prefab 和验证报告。

请区分“已提交版本”“未提交但已落盘的后续工作”“仅在设计文档中确认的未来内容”。任何聊天恢复都不能成为覆盖当前工作区的理由。
