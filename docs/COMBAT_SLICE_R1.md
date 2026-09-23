# R1：镜头与基础战斗对照

2026-09-14。本轮基于本地P0.10.1继续开发，GDD仍是体验准则。状态为实现与自测交付，等待用户比较视野，不代表基础战斗爽感已通过。

## 试玩

完整包：`Builds/CombatSlice_R1_20260914/`。

- `PLAY_VIEW_A.cmd`：当前视野9。
- `PLAY_VIEW_B.cmd`：扩大15%，10.35。
- `PLAY_VIEW_C.cmd`：扩大25%，11.25。

游戏内F5/F6/F7随时切换；R重开同一配置；Esc暂停。WASD移动、左键射击、右键/Q三连斩、空格闪击/按住推进。三个启动器使用同一可执行程序、隔离试玩存档，不改变旧包/原仓库。

## 改了什么

镜头的P0尺寸、俯角和移动前瞻有了明确入口；R1只对比尺寸，运行时俯角仍68°，前瞻1.25，跟随9。Boss继续由CameraFollow统一构图，遵守当前对照尺寸下限与旧17上限。

复用P0CombatDemo建立显式`-combatSlice`入口：两轮固定方向混合进攻，共46名现有敌人、12个小组，每组3–4，预约存活上限7。第二轮反向进攻；敌人仍用已有0.48秒出生预警。清场后0.8秒可接下一组，不等完整排程；90秒没清完判未完成，不假胜利。快速熟练打法允许提前完成。

固定M7＋当前斩舰刀、零buff，保留本地7.4移速、1.32推进倍率、5m/0.2s闪击、0.16s无敌、25EN、0.48s冷却；不改伤害、三刀时序、受击、音色、掉落规则或模型。新增简短试场操作提示；原五分钟P0模式仍保留。

E关闭、F无掉落、第三刀早段不能冲刺等是已识别的旧本地限制，按后续R2–R6顺序解决，不能因本轮PASS就认定满足GDD。

## 文件范围

现有文件改动：

- `Assets/Scripts/Core/CameraFollow.cs`：集中P0镜头参数，接入A/B/C。
- `Assets/Scripts/Core/P0CombatDemo.cs`：显式短片段、固定编排、重开、视野热键与简短HUD。
- `Assets/Scripts/Enemy/EnemySpawner.cs`：复用现有预警出生的小入口，不改AI。
- `Assets/Scripts/Core/P0CombatCheck.cs`：挂接新专项与原声回放，旧回归入口保留。
- `README.md`：区分本地当前版本与远端历史说明。

新增：CombatSliceSettings、CombatSliceChecks、CombatSliceReplay、CombatSliceBuild及其meta；tools/p0/run_slice.py、package_slice.py；本说明、DEVELOPMENT_BASELINE.md与原样GDD副本。

与冻结清单逐文件比较，场景、Prefab、模型、原音频、ProjectSettings与其余既有文件没有改变。未提交、未推送。

## 验证方式

Unity 6000.3.18f1，Windows Player、RTX4070Ti，1600×900测试。每次独立证据目录与隔离存档。

执行 `python tools/p0/run_slice.py build` 构建独立包；`check`检查三档运行参数、中心/四边位置、中央5m闪击落点、暂停、液态Boss镜头、零buff重开、全部击破结算、90秒未完成及死亡；`regression`跑既有枪弹/刀路/取消/五分钟P0检查；`terrain`跑原地形检查。

结构专项使用保护玩家与强制杀敌来测试状态，Boss入口沿用旧六buff随后验证切片清零。这类PASS不证明难度/爽感。无保护输入回放使用`replay`：真实武器伤害、无无敌或强制清敌、固定60Hz输入；逐帧对同一世界状态渲染A/B/C，所以不是三次不同操作冒充对照。采集30fps画面与Unity混音，视频不包括叠加HUD。模拟速度/录制渲染耗时不当作实际呈现FPS。

先测版本只有23敌人；无保护脚本29.70秒清完，HP155.14，说明时长不足。这一失败依据促成第二轮现有敌群接续；没有增加血量或等候时长。原录制与报告继续保留在`AuditEvidence/combat-slice-r1/replay-20260914-214816/`。

中途尝试额外屏幕HUD捕获时，工程未包含ScreenCapture模块导致一次编译失败；已移除该可选调用，未扩工程依赖。保留失败日志。最终画面证据使用工程已有的Camera.Render路径，HUD外观不算已经完成截图验收。

最终测试与回放数字见同目录`COMBAT_SLICE_R1_RESULTS.md`。

## 验收问题

先比较A/B/C：能否看清当前威胁、下一目标与闪击落点？是否有某档角色过小？是否愿意马上再玩一局？客观可见范围更大不自动代表速度感更强。

下一项只在视野选择后进入R2：保留选定镜头，比较推进1.32/1.6/1.9，处理方向响应与不足能量反馈。当前没有提前实现R2或追加养成。
