# 机神试炼：当前模型接入游戏

用户要求停止模型修改，先接入游戏。因此本次从既有 Unity 导入检查目录复制了冻结的彩色 V3 快照，并将 `PlayerMech_Guest.prefab` 的 `PlayerMechLoader.defaultMechPrefab` 指向新建的 `JishenTrialVisual.prefab`。另一个进程正在制作的 Blender 文件和模型脚本未再编辑。

## 当前实现

- 游戏实际显示军蓝、深灰、灰白配色的 V3 整机，包括当前放大后的头、右肩短炮、收拢背包和独立实体刀/光刃。模型保持 110 个网格、129,824 三角面、5 个材质。
- 既有 48 骨架 Generic 动作作为不可见驱动，15 个主要刚性段按自己的关节位置运动，肩甲独立跟随。装甲网格没有重新造型或蒙皮变形。
- 动作由当前移动速度、冲刺、实际近战、开火、受击和死亡事件驱动。死亡在非缩放时间下继续，暂停时不累积变换。
- 刀已从旁置展示改为右手挂接，刀尖拖尾沿用实际近战时间窗。`RigidMechPoseDriver.SetBeamActive` 只切换独立光刃，实体刀保留。
- 子弹由右肩炮口发出并朝现有手动瞄准点收敛。背包喷焰使用已有主喷口，避免旧角色额外圆柱喷口重叠。十个原有 Hardpoints 保持原层级并跟随新部件。
- 当前输入、伤害、冷却、冲刺距离、场景、敌人和相机逻辑保持原版本。没有运行场景生成器。

## 已执行检查

本机 Unity 6000.3.18f1 实际导入、生成 prefab、采样六个动作渲染，并重新加载保存场景运行检查。

- `ManualCombatAudit`：手动射击 6 发、前方目标实际中弹 5 次，后方 0 次；释放停火，独立移动/瞄准，真实挥刀前摇和单次伤害、排除友方/远处/后方、墙体阻挡、冲刺取消、暂停清除和导弹技能通过。检查期间 0 错误、0 警告。
- `HeroVisualAudit`：采样实际可见装甲顶点，而非只检查 Animator 状态。移动、冲刺、挥刀、肩炮、受击、死亡、暂停、解绑/重绑和脚底接地通过；实际肩炮/导弹目标损伤通过；独立光刃关闭保留实体刀，背包使用已有喷口。运行错误 0。
- 已实际查看游戏相机截图，以及诊断近景中的正面、冲刺、挥刀、射击和死亡。近景只移动诊断相机，不修改保存场景的游戏镜头。
- 验收中发现并修正旧冲刺系统在新背包外生成附加喷口、动作倾身晚于刚性姿态更新的问题。编辑器检查代码的一次变量重名编译失败已修正，失败日志保留。

完整检查图片和报告：`AuditEvidence/jishen/`。模型源冻结校验：`frozen-source.json`。

## 实际可玩构建

`Builds/Windows/MECH_TRIAL_20260905.154503/MECH_TRIAL.exe`，Windows x64 / Mono / Strict release；构建 0 错误、0 警告，约 376 MB 未压缩构建数据。旧构建目录保留。

对这个实际 exe 执行 `-sliceAudit <输出目录> -sliceAuditSmoke -sliceAuditDash`：95.003 秒正常时间战斗、71 次击杀、51 次冲刺、1 次升级选择、3 次场景重启；最终 HP 180，回放通过，Player 0 错误、0 警告。未强制杀敌、未加速战斗、未修改伤害和生命规则。

RTX 4070 Ti / D3D12，持续渲染 32,810 帧，28,149 个时序样本：平均 3.18 ms、P95 3.68 ms。测量是隐藏窗口的实际场景和 UI 相机 1920×1080 连续渲染，不含系统窗口呈现成本，不当作手机或人工试玩帧率。GC 字节计数不可用，不能将 JSON 中的占位 0 解读为零分配。完整报告：`AuditEvidence/jishen/player-smoke/player-report.json`。

本机 `.ps1` 执行策略拒绝了最初的包装脚本，改用已经安装的 Python 调用 Unity 后完成；没有改系统策略。`run_integration.py validate` 和 `release` 已实际执行。构建触发的 Unity ProjectSettings 序列化升级已保存到验收目录后恢复到本次开始时的干净版本，避免把无关平台设置变化留在项目中。

## 仍需后续制作

头部及侧面外观继续由另一个模型进程处理，此次不代表造型验收通过。动作沿用当前游戏素材，尚无专门制作的斩舰刀连招、全身 IK 或精细关节防穿插；快速动作中的装甲重叠和刀轨迹仍需后续美术/动作打磨。背包保持收拢，不增加展开动画。当前验证面向本机 Windows，未认证手机性能或完整通关平衡。

## 路径与恢复

- 活动视觉：`Assets/Prefabs/Player/JishenTrialVisual.prefab`
- 冻结 FBX/PBR/静态参考：`Assets/Art/JishenTrial_Assembly_V3/`
- 接入脚本和执行方式：`tools/jishen/README.md`
- 恢复旧角色：将 `Assets/Prefabs/Player/PlayerMech_Guest.prefab` 上的 `PlayerMechLoader.defaultMechPrefab` 设回 `RiggedSentinelVisual.prefab` 并重新进入场景。旧角色资产保留，空的 `rigidPose` 继续走旧动画分支。

## Git 与历史记录

接入开始于分支 `codex/industrial-dash-polish`、HEAD `3b1f0546ae231ee6a7174c8bff76cb28e79b8ddd`。新增独立美术目录、主角 prefab、姿态适配器、接入编辑器脚本和文档；更新原主角的视觉引用、动画/冲刺表现适配和视觉检查。未提交，未合并，未覆盖另一个进程的模型文件。

早前 `art_prototypes/JishenTrial_Hero_Assembly_V3/README.md` 记录的是静态导入阶段，其中“未替换主角”已被本次用户授权的接入所取代。另外，其 `configure_blender_review.py` 条目曾误写为已通过 Console 执行；实际只写入了文件，未执行。用户要求停止模型修改后没有再补执行该脚本。
