# 机神试炼 / MECH ROUGE

Unity 6 上帝视角机甲肉鸽射击原型。2026-09-05 已修复实测发现的枪口偏射、强化受伤、结算刷怪与重开交互问题，并在 Play Mode 用真实子弹自动跑通胜负流程。尚未达到真人手感、Build 深度与完整性能验收。详情见 [项目审计](docs/AUDIT_2026-09-05.md)。当前流程：

`机库开始任务 -> 第一关三波敌人 -> 三选一强化 -> 第二关 -> Boss -> 胜负结算 -> 重新开始`

## 直接试玩 Windows 版

运行：

`Builds/Windows/MECH_TRIAL_20260905/MECH_ROUGE.exe`

这是本轮新构建，旧 `MECH_ROUGE_Demo` 目录保留不覆盖。验证日志：

- `docs/audit-evidence/2026-09-05/P0LivePlay.log`：Editor 实弹通关与阶段保护，自动瞄准，不使用清敌作弊。
- `docs/audit-evidence/2026-09-05/P0WindowsBuild.log`：Windows64 构建成功。
- `docs/audit-evidence/2026-09-05/P0Standalone.log`：独立播放器流程冒烟，仍使用强制清敌；不是独立播放器自然通关或性能验收。

## 在 Unity 中运行

1. 使用 Unity 6.3 LTS 打开项目。
2. 打开 `Assets/Scenes/Demo_Main.unity`。
3. 等待脚本与资源导入完成。
4. 进入 Play Mode。
5. 在机库按 `E` 开始任务。

不要在普通启动时运行 `MECH ROUGE > Build Phase 1 Demo`：它会重建/覆盖生成的场景、Prefab 和材质。只有明确需要重新生成且已保护改动时才使用。

需要生成 Windows 64 位试玩包时，运行菜单 `MECH ROUGE > Build Windows Demo`。

## 操作

- `WASD`：移动
- 鼠标：瞄准
- 鼠标左键：光束步枪
- `Space`：冲刺
- `E`：开始任务
- `1`、`2`、`3`：选择强化
- `Esc`：暂停或继续
- `R`：结算后重新开始

## 当前内容

- 第一关三波普通敌人
- 关后随机三选一强化
- 第二关三波敌人，包含精英单位
- 两阶段 Boss 战
- 近战、远程、自爆、精英和 Boss 五类机甲轮廓
- 玩家 HP、能量、冲刺、目标、敌人数和 Boss 血条 HUD
- 命中反馈、伤害数字、相机震动、弹道、出生预警和攻击预警
- 暂停菜单、胜负结算和重新开始
- 机库部署界面与 Cadet、Standard、Veteran 三档本局难度
- 运行时生成的战斗音效

Cadet 是默认推荐档：敌人生命为 78%，玩家承受敌方伤害为 65%，每波恢复 20% 最大生命。Standard 保持基准战斗数值，Veteran 提高敌人生命和伤害并降低波次维修。

## 主角模型

原始用户模型保留在：

`Assets/UserContent/PlayerMech/Meshy_AI_Rose_Gold_Sentinel_0618172915_texture.glb`

由于 Unity 默认不能直接实例化该 GLB，当前可玩角色由其离线转换出的 OBJ 网格驱动，并保留原始 PBR 贴图：

- High：116,398 三角面
- Medium：32,942 三角面
- Low：8,646 三角面

三档网格已接入交叉淡化 `LODGroup`。正常上帝视角主要使用 Medium 档，近景使用 High 档，远景使用 Low 档。

## 验证

- 实弹与阶段保护：`Assets/Editor/ProjectAudit.cs`，入口 `ProjectAudit.Run`；枪口回归入口 `ProjectAudit.RunShotDiagnostics`。
- 编辑器流程冒烟（强制清敌）：`Assets/Editor/DemoPlayModeSmoke.cs`
- 局部压力渲染（不代表完整游戏性能）：`Assets/Editor/DemoPerformanceSmoke.cs`
- Windows 构建：`Assets/Editor/DemoBuildPipeline.cs`
- 独立播放器完整流程：`Assets/Scripts/Core/StandaloneRuntimeSmoke.cs`
- 详细交接记录：`docs/HANDOFF.md`
- 验收清单：`docs/ACCEPTANCE_CHECKLIST.md`

验证副本的 Unity 6 启动索引阶段重复记录 `UnityEditor.Search.SearchDatabase` 异常。堆栈不来自游戏程序集，未阻断本轮编译与运行，但 Console 不能标记为完全无错误。完整限制见审计报告。
