# 斩舰刀与移动表现 V4

2026-09-06。最新试玩：`Builds/ValkyrActionV4/MECH_TRIAL_Valkyr.exe`。在动作 V3 上加入用户要求的刀光、挥砍粒子、受击表现、拖刀火花和移动反馈。

## 本轮效果

- 蓝色光刃：沿当前刀刃网格取样，增加蓝色外光与亮芯；挥刀时略增强亮度和宽度。实体刀身保留，光刃仍可独立关闭。
- 大剑挥砍：加宽实际刀轨迹形成的蓝白尾迹，切入过程中从运动刀身释放蓝色碎光。尾迹与粒子随时间消散，没有额外增加攻击范围或伤害。
- 敌方受击：真实命中触发蓝白接触闪光、橙黄色金属飞溅、蓝色碎光和少量烟尘；敌方装甲短闪，躯干短暂后仰。粒子从碰撞体接触面外侧产生，避免埋在装甲内部。受击后仰仅作用于显示关节，不改变 AI、导航或伤害逻辑。
- 拖刀：运行时调整携刀姿态，让实际刀尖接近地面；仅在地面移动、未挥刀/推进/冲刺，且刀尖附近射线命中地板时产生金属摩擦火花。静止、抬刀和飞行时停止新增摩擦火花，已有粒子自然消失。这是贴地距离检测，不是刀身刚体摩擦模拟。
- 移动：增加喷口蓝色粒子、短推进尾迹和落脚扬尘；增强胸胯反向转动、身体起伏、空手摆臂，以及起步前倾和减速回摆。支撑脚继续保持地面位置，粒子方向跟随实际移动。

使用现有 V3 外观和刀身；原始 Blender、冻结 FBX、材质文件均未修改。新特效为项目代码和运行时生成的光晕纹理，没有新增外部素材。

## 实际预览与验证

正常游戏镜头在 `AuditEvidence/action-vfx/gameplay_effects.mp4`，近景在 `effects_closeup.mp4`。由实际 Windows Player 生成的 204 帧编码，30fps、6.8 秒，内容依次为重斩命中精英、奔跑拖刀、持续推进、空中挥刀和落地。使用独立测试档与脚本输入，不是真人通关录像。

已查看正常游戏镜头、挥刀连续帧、受击与拖刀近景；实际修正了粒子在碰撞体内生成的问题。动作/特效短检查 20 项通过；既有战斗短检查 19 项通过，运行错误 0。Windows 构建为 0 错误、0 警告。具体报告在 `quick-check.txt`、`combat/quick-check.txt`、`build-result.txt`。

本轮没有做手机性能测试或完整连招；当前仍是一套重斩。正常镜头的动作和特效强度可以继续根据试玩反馈调整。

## 使用与继续修改

WASD 移动，Q / 右键挥刀，空格点按冲刺、长按推进。机库点击“斩舰刀 · 首领试战”可快速预览。

```powershell
& 'C:/Python313/python.exe' tools/run_motion_v3.py build --effects
& 'C:/Python313/python.exe' tools/run_motion_v3.py preview --effects
& 'C:/Python313/python.exe' tools/run_motion_v3.py check --effects
```

主要代码：`RaikenCombatVfx.cs`、`MechBladeHitReaction.cs`、`MechActionGlow.shader`；携刀和移动姿势继续由 `ValkyrMotionDriver.cs` 驱动，喷口表现由 `MechDashPresentation.cs` 驱动。所有输出留在独立 V4 试玩目录，旧构建保留。

构建曾因 C 盘空间不足失败。已将动作 V2、V3、模型 V3 与本轮的逐帧预览目录移到 `D:/CodexRenderCache/MECH_ROUGE/20260906_action_vfx/`，原项目位置保留目录链接；没有删除帧、成片、模型或旧程序。后续复制工程时，逐帧预览缓存依赖这个 D 盘路径；源码和试玩程序本身不依赖它。

Git 分支 `codex/equipment-loop-v1`，改动未提交或推送。保留先前任务的改动；本轮前后状态与 64 个冻结资源文件的核对记录在 `AuditEvidence/action-vfx/`。
