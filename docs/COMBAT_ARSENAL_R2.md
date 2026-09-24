# 机甲战斗改版 · CombatArsenal_R2

桌面「机神试炼」继续使用固定入口。新版保留旧版本与永久装备存档，镜头统一为 20。

## 这版的变化

- 瓦尔基里保留原移动和冲刺数值；步幅收至原来的 65%，骨盆起伏减半，低速落脚、高速悬浮。新落脚声缩短金属尾音并叠加低频，悬浮时不响脚步。
- 空格短按、长按与松开分别触发喷口爆发、持续尾焰和衰减。E 双背炮在 0.48 秒开火，保留总伤害 72；目标消失后仍按已提交方向发射。移动、冲刺和换枪后仍可正常开火。
- 持刀转向按待机、准备、收势分别限制为 720／540／360°每秒，有效挥砍锁定朝向。重武器以机体和肩部为支点，手臂再配合持握。
- 三连砍保留原攻击窗口与伤害，命中停顿改为 45／40／95 毫秒；普通敌人轻、重斩硬直为 0.18／0.30 秒。增加真实刃口刀弧、接触火花、第三刀冲击尘环及推进爆发。
- 冥隼独立移动数值为 13.5 米/秒、推进 21.6 米/秒、冲刺 0.13 秒／6.5 米；四个以内的残影随普通移动、推进与冲刺改变强度。
- 六枚浮游炮部署 7.5 秒，保留原总火力；在世界坐标独立飞行，分散索敌、环绕敌人、换位避障、实际炮口射线开火及召回。远景简化不会拉回或隐藏它们。
- 冥隼眼睛、发光部件、推进、残影、浮游炮、刀与所持能量武器统一使用现有浮游炮的紫蓝外层和浅色亮芯，保留金属底色。
- 随机战局加入白色 E01、吉姆、扎古Ⅱ、大魔与钢加农；实际使用 M7、M14、TYPE-08、AX-01、火箭、导弹与背炮。每场至少三种机体、四种武器，最多五个存活及预留名额、三个攻击者、一个重炮单位。回收、入库和装备对应真实武器。
- 使用昨天的 FAZZ_Rebuilt／HG_2250 成品，并转换十段原版动画与对应开火时点。仅有重型米加炮、双背炮、18 联装导弹三种攻击；前三关杂兵清完后出现，耐久 1800／2200／2600，额外间隔 1.2／0.85／0.55 秒。短战末尾使用第一关强度。
- 新出击重新随机；失败重试复用原关卡种子和入关状态。前三关 FAZZ 后的奖励、六关流程、真实武器收藏与旧装备记录继续兼容。

## 验证

同一最终游戏构建通过 **29 组检查、3285 项断言**；覆盖 30／60／120 帧下的操控、步态、技能、暂停、死亡和重开清理。两台机体的七类回收武器逐一验证实际开火伤害、持握及移动支点；400 组种子／关卡组合验证多样性与粒子随机独立。

普通输入回放分别以纯枪、纯刀和混合打法打通短战及 FAZZ；完整六关回放经过三台 FAZZ、六次奖励和最终 Boss。流程测试另外覆盖失败后重试当前关卡、奖励只提交一次和永久收藏保留。没有修改永久存档。

独立性能检查使用 RTX 4070 Ti、1600×900、镜头 20，逐帧同步渲染；无并发构建或其他游戏检查。下表是隐藏窗口中的模拟与渲染耗时，不能当作显示帧率。

| 场景 | 平均毫秒 | P95 毫秒 |
|---|---:|---:|
| idle | 2.09 | 2.39 |
| boost-no-ghost | 2.37 | 2.95 |
| boost-ghost | 2.50 | 2.91 |
| boost-support | 3.19 | 3.69 |
| arsenal-crossfire | 4.82 | 5.66 |
| fazz-missiles | 3.38 | 4.17 |

游戏内实际混音捕获峰值 0.6307（-4.00 dBFS），削波采样为 0；脚步、推进、炮击、破甲与背景音通道检查通过。检查过程保持静音，没有替代主观试听和手感验收。此前处理过的电子／低频枪声及新作 170 BPM BGM 沿用。


## 维护交接

权威源：D:/project-mecha-design/MECH ROUGE/handoff/GameplayLoop_V1。
缓存构建工程：E:/SteamLibrary/JishenBuildWork/GB4Motion_R1/Project。
新 Art、Player 和 Evidence 存放在 E:/SteamLibrary/JishenBuildWork/CombatArsenal_R2，通过项目内 junction 保留稳定路径。
构建及顺序检查工具：E:/SteamLibrary/JishenBuildWork/CombatArsenal_R2/work/run.py。
资产作者工具和修改前文件保存在同目录及 work/before。原始 FAZZ .blend 与旧构建未覆盖；本次只是目标机体与动作的定向转换，不代表完整解包整个 GB4 或还原其引擎代码。

源码同步校验：515 个文件，见 release/source-snapshot.json。
最终程序集 SHA256：b6dc929d01e01d235e562bed202c7235f853f37b57fe44fb1d6f81e75871933e。
永久存档 SHA256：7db4bf90ba758a4cf034dc2081969bc9de413a41483f59c156d76be7cdbf195d。
完整证据见 AuditEvidence/combat-arsenal-r2/release/final-verification.json；其 checks 字段精确标明每项最终测试目录。fullplay 使用 -fullRebuildCheck 运行真实进程中的关卡生命周期检查；natural/fullrun 使用正常输入，不注入敌方伤害或玩家无敌。
后续发布要求全部 29 模式：arsenal velocity drones sound playmix nemesis imported heroes raiken cannon melee impact aiui lunar terrain arena punch vfx loopv2 foundation tactics beam regression fullplay natural fullrun audio performance punchperf。performance/punchperf 必须单独运行。
未执行 Git commit 或 push。桌面入口仍为 Launcher/launch.ps1，存档仍为 Launcher/UserData/profile.json。

桌面发布已完成：CombatArsenal_R2，147 个构建文件完整性通过；原入口静默启动验证通过，永久存档校验未变。
