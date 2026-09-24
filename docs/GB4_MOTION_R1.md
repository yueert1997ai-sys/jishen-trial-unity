# GB4 Motion R1

2026-09-23：按用户要求，将已提取的《高达破坏者 4》动作数据直接用于本地 Demo 两台玩家机体（NEMESIS、VALKYR）。

## 实际替换范围

- 剑与步枪各自的待机、前跑、后跑、飞行，共 8 段；三连斩的起手与收招，共 6 段。
- 从原始 PSA 保留 27 个身体/武器关节的采样轨迹，离线转换为 `Assets/Resources/GB4Motion/body.bytes`。运行时根据目标机体关节长度重定向，保留现有模型。
- 原始动画控制骨盆、腰胸、头部、手臂、腿和持剑方向。瞄准、武器切换、移动距离、攻击时间窗、命中停顿与伤害继续由本项目现有控制器负责。
- 剑柄对齐真实掌心，真实刀刃轨迹继续进入 240 Hz 接触采样。落地修正、攻击支撑脚约束及脚步声适配新动画。
- 机库与死亡仍使用现有姿态。本次未移植原游戏敌人 AI、行为树或整套原生运行逻辑。

## 来源与复现

本机来源：`E:\SteamLibrary\GameStudyExports\GB4\Converted`。动作使用 `Motion/MS/Basic/Type_Saber/Standard`、`Type_Rifle/Standard` 及 `Motion/MS/Attack/Close/Saber/sbr_H01..03_s/e.psa`；骨架层级使用 `Model/MS/BasePose/basepose_0000.gltf`。

执行 `python tools/p0/import_gb4_motion.py`（需要 numpy、scipy）。导出 PSA 的 BONENAMES 父节点被压平，不能用它还原 FK；导入器使用模型骨架，并校验绑定姿态。动画键与绑定数据的轴向约定分别转换。来源文件 SHA-256、帧率、时长、绑定误差和输出哈希在 `AuditEvidence/gb4-motion-r1/import.json`。

## 构建位置

权威源码仍为 D 盘本项目。构建时 D 盘空间不足，因此使用 E 盘隔离构建副本：

- 构建工程：`E:\SteamLibrary\JishenBuildWork\GB4Motion_R1\Project`
- Player：`E:\SteamLibrary\JishenBuildWork\GB4Motion_R1\Player`
- 本项目 `Builds/GB4Motion_R1` 和 `AuditEvidence/gb4-motion-r1` 为目录联接。迁移电脑时需要重新构建，不能只复制联接。
- 桌面入口继续为 `Launcher/launch.ps1`，发布状态以 `Launcher/current.json` 为准；旧包保留。

## 验证与发布

最终结果：22 组套件 / 2,112 项检查全部通过，147 个构建文件哈希一致。桌面指针已发布为 GB4Motion_R1，静默启动验证通过（`Launcher/Verification/20260923-172504`），永久存档 SHA-256 未变。汇总为 `AuditEvidence/gb4-motion-r1/release/summary.json`。

运行 `tools/p0/run_gb4_motion_r1.py` 的 imported 与原有 21 个模式：aiui、lunar、terrain、arena、punch、vfx、heroes、drones、nemesis、loopv2、foundation、tactics、impact、melee、beam、regression、fullplay、natural、fullrun、performance、punchperf。

全部 Player 顺序运行；性能检查必须独占。imported 检查两台真实机体、27 关节数据加载、跑动脚掌高度、脚步事件、飞行动作、30/60/120 Hz 三连斩、剑柄接触、暂停和重置。截图及数值记录位于 `AuditEvidence/gb4-motion-r1/release`。本地测试通过不代表用户认可动作观感。

本次原始动作提取物及新修改尚未提交或推送 GitHub。此前远端快照不能代表本轮最终 Demo 状态。
