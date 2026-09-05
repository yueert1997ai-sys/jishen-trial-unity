# 机神试炼 / MECH ROUGE

Unity 6 上帝视角机甲肉鸽射击原型。2026-09-05 实测发现命中与阶段收尾问题；旧流程测试通过不等于真人完整试玩验收。当前审计与后续优先级见 [项目审计](docs/AUDIT_2026-09-05.md)。已有流程骨架：

`机库开始任务 -> 第一关三波敌人 -> 三选一强化 -> 第二关 -> Boss -> 胜负结算 -> 重新开始`

## 直接试玩 Windows 版

运行：

`Builds/Windows/MECH_ROUGE_Demo/MECH_ROUGE.exe`

以下为历史构建验证，使用自动清敌，不代表当前源码的实弹与性能验收。历史日志：

- `UnityStage11_WindowsBuild.log`
- `UnityStage11_StandaloneSmoke.log`

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

- 编辑器完整流程：`Assets/Editor/DemoPlayModeSmoke.cs`
- 1080p 压力渲染：`Assets/Editor/DemoPerformanceSmoke.cs`
- Windows 构建：`Assets/Editor/DemoBuildPipeline.cs`
- 独立播放器完整流程：`Assets/Scripts/Core/StandaloneRuntimeSmoke.cs`
- 详细交接记录：`docs/HANDOFF.md`
- 验收清单：`docs/ACCEPTANCE_CHECKLIST.md`

验证副本偶尔会在 Unity 6 启动索引阶段记录一条 `UnityEditor.Search.SearchDatabase` 异常。它不来自项目程序集，项目自有错误检查和完整流程测试均通过。
