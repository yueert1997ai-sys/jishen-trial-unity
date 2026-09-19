# E-01 液态金属侵蚀体 · PC Demo 接入

本次将 R02 残破头部版整机接入正式 Boss 预制体 `Assets/Prefabs/Enemies/Boss_HeavyMech.prefab`。保留预制体 GUID，`Demo_Main` 与正常六场遭遇后的 Boss 流程直接使用新版外观。

双击构建目录的 `PLAY_BOSS.cmd`，直接进入 Boss 场地；角色通过原有奖励接口获得六次升级，可正常移动、攻击和冲刺。直接运行 `MECH_TRIAL.exe` 仍从机库开始完整流程。

WASD 移动，鼠标瞄准，左键射击，右键 / Q 挥刀，空格冲刺，E 导弹，Esc 暂停。直接挑战入口没有自动战斗或无敌；自动检查的无敌和半血设置仅由 `-e01BossSmoke` 开关启用。

## 资产

- 美术源：`art_prototypes/TYPE_E01_ELITE_20260906/stage_02_head/TYPE_E01_ELITE_HEAD_R02.blend`，只读保留。
- 游戏骨架工程：`art_prototypes/TYPE_E01_ELITE_20260906/game_ready/TYPE_E01_ELITE_GAME_R01.blend`。
- 固定 FBX：`Assets/Art/Enemies/TypeE01Elite/TypeE01Elite_R02.fbx`，无需在玩家端安装 Blender。
- 86 根通用骨骼，6 条背部触须链、6 条巨爪指刃链、10 个核心开合控制骨。
- LOD0 545,576 三角面；LOD1 229,141 三角面；单个 Boss 显示一个 LOD，18 个材质分区。本轮以 PC 近看还原为目标，尚未作为移动端资产验收。
- 保留实体破甲、侵蚀结构与 R02 焦黑断口蒙版；材质使用 Unity 标准光照加液态表面变化、红色脉冲。

## 行为连接

保留现有 Boss 的扇形齐射、区域轰击、冲锋、召唤四种攻击及原数值。动作按实际蓄力、执行、核心暴露和半血二阶段状态驱动。新增重步关节动作、触须摆动、爪刃张开、核心开合、受击回缩和短暂停机姿态。

本次没有增加新的近战连招伤害规则，也没有制作全身液体模拟。核心暴露仍沿用原 Boss 全身受伤倍率窗口；尚未改为独立可瞄准弱点碰撞体。

## 复现

工程根目录均为 `D:/project-mecha-design/MECH ROUGE`。

1. `C:/Python313/python.exe art_prototypes/TYPE_E01_ELITE_20260906/run_blender.py export_game.py`
2. `C:/Python313/python.exe tools/e01/run_unity.py E01EliteIntegration.Integrate`
3. `C:/Python313/python.exe tools/e01/run_unity.py E01EliteIntegration.BuildRelease`
4. 构建的 `MECH_TRIAL.exe -e01BossSmoke -e01Evidence <D盘证据目录> -logFile <D盘日志>` 自动检查实际玩家中的 Boss 生成、四种攻击、骨架变化、核心、二阶段、LOD、暂停、受伤、胜利及重开。

证据目录：`AuditEvidence/e01-elite`。`unity_*.png` 是 Unity 编辑器导入检查；`player/game_*.png` 与运行报告来自 Windows 独立构建。构建与玩家证据以 `build-path.txt`、`player/runtime-check.json` 为准，不把 Blender 预览当作游戏实测。
