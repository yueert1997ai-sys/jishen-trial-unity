# VALKYR / RAIKEN Mk-II 实装与精英首领试玩

2026-09-06。最新四视图对应的整机与斩舰刀已进入实际游戏。昨天的 Jishen V3 机体作为“苍钢卫士 · 精英首领”，接在普通流程末尾，也可从机库直接试战。

本日后续动作修订已包含在同一个试玩程序中：新主角改用全身重斩、奔跑与推进飞行。最新动作参考、实机片段和验证边界见 [全身动作 V2](VALKYR_MOTION_V2.md)。

## 试玩

在本副本运行 `Builds/ValkyrCombat/MECH_TRIAL_Valkyr.exe`，同目录的 Data、UnityPlayer 等文件必须一起保留。

- 机库点击“斩舰刀 · 首领试战”直接体验新武器与旧机体首领；点击“开始任务”进入完整的六场战斗、随机强化、换装与 Boss 流程。
- WASD 奔跑，鼠标瞄准，Q / 右键全身挥刀，空格点按冲刺、按住推进飞行，左键射击，E 导弹齐射。屏幕冲刺键也支持长按推进。
- Tab / 装备仓库整理永久装备，F / 吸收部件回收敌方掉落。装备保持永久收藏，buff 每局随机且在结算清空。
- 新版和前一装备预览使用相同的正常仓库保存位置；自动检查使用单独的测试档，不会替正常试玩解锁收藏。

## 已实装

新主角使用交付的整机 FBX，保留蓝、白、深灰、少量金色及青色光刃。117 个网格、199,528 三角形；15 个主要刚性段现由全身动作与双臂双腿 IK 驱动，停用该主角的通用步行/挥刀片段。握柄至刀尖为 3.284m，沿用此次实际导出的比例，没有按概念图中的宣传文字重设尺寸。

RAIKEN 的实体刀身与两层光刃保持分离。光刃有独立开关接口，正常游戏默认开启。手部带动原始握柄关系，轨迹采样自实际刀尖和刀根；淡蓝残影、白青亮边、命中火花、音效和短促动画停顿组成第一版反馈。机库持刀向下，刀尖保留离地间隙；战斗视角适度拉近以看清武器。

挥刀动作约 0.74s，最短发起间隔 0.76s，命中时全身短停约 0.045s。双臂举刀，腰胯与胸部参与斜劈，双腿踏步支撑。基础伤害 78，最大判定中心距离 4.25m，接触窗口为 0.235–0.46s，使用实际刀根至刀尖的胶囊扫掠处理相邻帧轨迹。每次挥刀对同一目标只结算一次，可以扫到多个目标，墙体阻挡判定保留；冲刺可以取消挥刀。现有伤害 buff 仍会参与计算。碰撞是简化刀身体积，并非网格逐三角形碰撞。

旧机体保存为独立 `RivalVeteran.prefab`：装甲外观沿用昨天版，传感器和刀刃改红色，整体放大到 1.08 倍。生命值基数 1600，受难度倍率影响，装甲阶段承受 65% 伤害，暴露阶段为 100%。复用现有首领的扇形炮击、落点轰击、冲锋与增援，补上旧机体的行走、冲刺、挥刀和炮口瞄准。半血进入第二阶段，击败后正常结算。

## 实际验证

Unity 6000.3.18f1 已构建 Windows Player，构建结果为零错误、零警告。实际 Player 在 RTX 4070 Ti / Direct3D12 上执行了 19 项短检查，结果全部通过，运行错误为零。

覆盖最新玩家加载、光刃独立关闭、真实刀尖移动、伤害随扫掠触发、3.9m 单次命中、冲刺取消、首领导航与开火、实际冲锋、半血阶段切换、胜利结算、重开以及普通出击入口。动作 V2 下重新执行，测得本次刀尖最大位移 5.99m、8 个有效残影采样，对装甲首领单次造成 50.70 伤害。另有 15 项动作短检查和 204 帧实际游戏近景画面，见动作 V2 记录。

初次接入时已实际查看机库、挥砍、放大检查、敌方开火、冲锋、结算及普通出击截图，并修正刀光过于像实心扇面、机库刀尖出画、伤害早于刀锋到达的问题；这仍未解决用户指出的全身动作问题。动作 V2 已重做动作并查看连续运行帧，原静态截图不再作为动作质量结论。`06_raiken_sweep_close.png` 是实际同一帧游戏模型的临时近景镜头；其余编号图为正常游戏镜头。没有用概念图或生成图冒充运行结果。

检查通过短回放固定站位、直接调用按钮，并在验证第二阶段后给予致死伤害来覆盖结算；不代表完整人工通关、音效听感验收或最终难度平衡。尚未制作连招、专属处决、新的整套 Boss 动作或移动端性能验收；主角已有双手柄部 IK，但没有新增手指绑定。首领专属装备掉落也未在本轮扩展。

## 文件与复现

本副本根目录：`C:/Users/yue/Documents/MECH ROUGE/handoff/GameplayLoop_V1`。

| 内容 | 本副本路径 |
| --- | --- |
| 最新试玩 | `Builds/ValkyrCombat/MECH_TRIAL_Valkyr.exe` |
| 游戏场景 | `Assets/Scenes/Demo_Main.unity` |
| 新主角预制体 | `Assets/Prefabs/Player/ValkyrRaikenVisual.prefab` |
| 旧机体精英 | `Assets/Prefabs/Enemies/RivalVeteran.prefab` |
| 冻结模型、材质、贴图 | `Assets/Art/ValkyrRaiken_V1/` |
| 导入与场景接入 | `tools/prepare_valkyr.py`、`Assets/Editor/ValkyrCombatIntegration.cs` |
| 刀身轨迹与反馈 | `Assets/Scripts/Player/RaikenBladePresentation.cs`、`PlayerMeleeController.cs`、`Assets/Resources/RaikenEnergy.shader` |
| 全身动作、奔跑和飞行 | `Assets/Scripts/Player/ValkyrMotionDriver.cs`、`PlayerController.cs`、`MechDashPresentation.cs` |
| 敌人动作与攻击 | `Assets/Scripts/Enemy/MechRivalPresentation.cs`、`BossController.cs` |
| 构建与短检查 | `tools/run_valkyr.py`、`Assets/Scripts/Core/ValkyrCombatQuickCheck.cs` |
| 实际证据 | `AuditEvidence/valkyr/quick-check.txt`、`build-result.txt`、`player.log`、编号 PNG |

在副本根目录执行：

```powershell
& 'C:/Python313/python.exe' 'tools/prepare_valkyr.py'
& 'C:/Python313/python.exe' 'tools/run_valkyr.py' integrate
& 'C:/Python313/python.exe' 'tools/run_valkyr.py' build
& 'C:/Python313/python.exe' 'tools/run_valkyr.py' check
```

第一步会重新复制原工程交付的 FBX 并提取 GLB 材质；已有冻结资产、仅改代码时直接 build 即可。Unity 路径为 `D:/Editor/6000.3.18f1/Editor/Unity.exe`，构建需本机正常许可环境。所有运行均已实际执行，未调用 Blender 修改模型。

## Git 与并行制作边界

分支 `codex/equipment-loop-v1`，基于 `3b1f054`。变化保留在独立工作副本，未提交、未推送、未合回原工作目录。包含前轮装备玩法与本轮新模型接入，旧版试玩输出保留。

原工程 `C:/Users/yue/Documents/MECH ROUGE` 的 Blender、整机及斩舰刀源资产未编辑；两份来源 FBX 的结束时哈希与开工快照一致，结果在 `source-final-check.json`。原工程已有的未提交修改保留，结束时状态与开工所见相符。接续时打开本副本而非旧工程查看此次代码和场景。

之前 `handoff/equipment-loop-code.patch` 只覆盖旧装备玩法，不能代表本轮全部变化；不要用它覆盖最新模型分支。新模型需要更新时先重新冻结交付资产，再运行接入工具，避免与另一个建模进程直接共写源文件。
