# VALKYR 全身重斩、奔跑与推进飞行

本文为历史 V2 记录。后续用户指出握持和发力仍不正确，已按 [动作重做 V3](VALKYR_MOTION_V3.md) 更新，最新程序在 `Builds/ValkyrMotionV3/`。

2026-09-06。试玩仍在 `Builds/ValkyrCombat/MECH_TRIAL_Valkyr.exe`，这次更新的是游戏动作，交付的模型、头部和 RAIKEN 刀身几何未编辑。旧 Jishen 机体继续作为精英首领。

## 试玩操作

- WASD：奔跑；鼠标：瞄准。
- Q / 右键：双手全身重斩；空格点按：冲刺并取消挥刀。
- 按住空格：持续低空推进，消耗能量；松开后落地。推进中也能挥刀。
- 屏幕冲刺键同样支持点按和长按，挥刀键维持独立。
- 机库点击“斩舰刀 · 首领试战”可以直接体验。正常六场战斗、装备吸收、永久仓库与单局 buff 流程保留。

## 复盘与动作变化

上一版的核心问题是右手腕控制刀锋扫弧，通用骨架控制身体，两套动作没有形成同一条发力链。通用移动循环和每帧贴地又消除了推进离地姿态。此前截图与命中检查能证明功能工作，不能证明动作合适。

现在新主角由 `ValkyrMotionDriver` 统一驱动交付模型的 15 个主要刚性关节，停用该主角的通用步行/挥刀动画。保留原有几何、段长及装甲父子关系。

重斩包含收势准备、双臂抬起蓄力、踏步转胯带肩、斜劈和低位收刀。双臂使用两段 IK 到达同一根真实刀柄，刀锋方向与刃面方向一起约束；脚步和膝关节参与支撑。动作约 0.74 秒，命中短停约 0.045 秒作用于全身动作时间。持刀攻击时降低普通移动速度，冲刺仍可取消。当前只有一套重斩，没有新增连招。

伤害采样改为实际刀根到刀尖的胶囊扫掠，处理相邻帧之间的移动，主要接触窗口为 0.235–0.46 秒。刀光跟随实际几何轨迹。基础伤害 78、最大中心距离 4.25m、墙体遮挡、同次挥刀单目标只命中一次及现有伤害 buff 保留。这是简化刀身体积碰撞，不是网格逐三角形碰撞。

奔跑使用前倾重心、蹬地、收膝、短暂腾空、腰胸反向摆动及空手摆臂。步频跟随实际位移速度，持刀臂维持负重姿势。推进使用独立姿态：机体抬升、前倾、双腿向后不对称收拢、喷口持续工作，停止步行循环。推进速度为基础移动的 1.32 倍，耗能 24/秒，推进和冲刺时停止能量恢复；耗尽后需松开再按，避免反复起落。

本轮的飞行是适配现有俯视战斗的低空推进：视觉机体离地，移动和碰撞仍在现有地面平面，不能越过墙体，也没有自由升降操作。

## 参考依据

使用了 [Bandai Spirits 的 MG 剑装脉冲高达产品页](https://www.bandaispirits.co.jp/products/search/detail.php?grp_id=5325&prd_id=4573102582515000) 和官方实物姿势照片：[双臂举刀及空中屈腿](https://bandai-a.akamaihd.net/bc/img/model/b/1000107052_4.jpg)、[地面宽站姿举刀](https://bandai-a.akamaihd.net/bc/img/model/b/1000107052_7.jpg)。参考的是肩肘抬起、双手负重、身体转动、地面支撑和空中腿部姿态。

也查看了 Danikk04 的 [《SD Gundam G-Generation Cross Rays》剑装脉冲动作录像](https://www.youtube.com/watch?v=rllR6FWAVgM) 中约 0:47–1:10 的公开视频分镜关键帧。没有完整播放原视频，不能称为逐帧复刻动画；官方照片中的双刀/合并长柄也没有照搬到本项目单把 RAIKEN 上。奔跑循环和具体时间曲线由本项目编写，没有导入高达的模型、动画或特效文件。

## 实际验证与预览

Unity 6000.3.18f1 构建 Windows Player，零构建错误/警告。实际 Player 使用 RTX 4070 Ti 渲染；动作短检查 15 项通过，既有战斗/首领短检查 19 项通过，均零运行错误。使用独立测试档，未修改正常仓库进度。

动作检查覆盖双手握持、胸部转动、真实刀锋单次命中、跑步腾空、持续推进耗能、空中挥刀、屏幕按键松手、落地、能量耗尽不反复起飞、松开再推进、冲刺取消。地面重斩胸部最大相对旋转约 64.8 度；测试段内右手目标误差最大约 2.3cm，左手掌到支撑点误差小于 1mm。这些数值只验证关节目标，不代表装甲逐面无穿插或最终动画审美验收。

已实际查看运行生成的重斩、奔跑、推进和空中重斩连续帧；根据画面缩短跑步支撑段、加入踏步，并调整检查镜头以完整容纳空中刀尖。没有用概念图或生成图替代运行画面。模型动画仍是刚性关节动作，没有新增手指绑定、动捕或首领专属全套动画。

- `AuditEvidence/motion-v2/whole_body_motion.mp4`：204 帧游戏画面组成的 30fps 原速无声短片，顺序为重斩、奔跑、推进、空中重斩、落地。使用较近的检查镜头，不是常规战斗镜头。
- `AuditEvidence/motion-v2/01_ready.png` 至 `06_boost_flight.png`：正常游戏镜头和 UI。
- `AuditEvidence/motion-v2/*_pose_*.png`、`frames/`：实际关节动作的连续帧。
- `AuditEvidence/motion-v2/quick-check.txt`、`motion.csv`、`player.log`：动作运行记录。
- `AuditEvidence/valkyr/quick-check.txt`、`build-result.txt`：战斗回归与构建记录。

本轮是快速动作预览，没有人工完整通关、真实手机操作、听感或最终平衡验收。已检查连续帧并提供短片，手感仍以实际试玩反馈为准。

## 文件与复现

所有路径相对 `C:/Users/yue/Documents/MECH ROUGE/handoff/GameplayLoop_V1`。

| 内容 | 文件 |
| --- | --- |
| 全身动作、双臂双腿 IK | `Assets/Scripts/Player/ValkyrMotionDriver.cs` |
| 实际刀身碰撞与命中停顿 | `PlayerMeleeController.cs`、`RaikenBladePresentation.cs`（同目录） |
| 奔跑/推进输入和能量 | `PlayerController.cs`、`PlayerInputRouter.cs`、`PlayerStats.cs`、`MechDashPresentation.cs`（同目录） |
| 屏幕长按推进、操作提示 | `Assets/Scripts/UI/MobileActionButton.cs`、`MobileControls.cs`、`HangarDeploymentUI.cs` |
| 模型接入与动作挂载 | `Assets/Editor/ValkyrCombatIntegration.cs`、`Assets/Prefabs/Player/ValkyrRaikenVisual.prefab` |
| 实机动作短检查 | `Assets/Scripts/Core/ValkyrMotionQuickCheck.cs` |

```powershell
& 'C:/Python313/python.exe' 'tools/run_valkyr.py' build
& 'C:/Python313/python.exe' 'tools/run_valkyr.py' motion-check
& 'C:/Python313/python.exe' 'tools/run_valkyr.py' check
& 'D:/Tools/ffmpeg/bin/ffmpeg.exe' -y -framerate 30 -i 'AuditEvidence/motion-v2/frames/frame_%04d.png' -c:v libx264 -crf 18 -pix_fmt yuv420p -movflags +faststart 'AuditEvidence/motion-v2/whole_body_motion.mp4'
```

以上动作已经执行。若从尚未挂载新动作组件的旧预制体开始，先执行一次 `tools/run_valkyr.py integrate`。正常代码迭代只需 build。Unity 构建需要本机正常许可环境。

Git：`codex/equipment-loop-v1`，未提交、未推送、未合回原工作目录。保留前轮装备玩法与接入改动。原项目的模型和斩舰刀来源资产未编辑，结束时哈希记录为 `AuditEvidence/motion-v2/source-final-check.json`。
