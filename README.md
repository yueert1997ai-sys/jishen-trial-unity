# M1 / iPhone 接续入口

本分支是 2026-09-24 当前源码开发快照。今晚在 M1 Mac 继续请先读 [M1_START_HERE](docs/apple/M1_START_HERE.md)。包含源码、场景、模型和 iPhone 导出工具；尚未完成 iOS 真机编译或上传 TestFlight。

---

# 机神试炼 / MECH ROUGE

2026-09-24 最新发布：**CombatContact_R1**。修正爆炸穿墙、光束枪口越墙开火及未造成伤害时的普通命中闪光；统一光束穿透与溅射，保留镜头 17、白色敌机和最新 AI。**31 组 / 3438 项检查、147 文件与固定入口静默验证通过**，旧版本和永久存档保留。继续打开桌面「机神试炼」。见 [更新与验证](docs/COMBAT_CONTACT_R1.md)。

以下为历史记录。

2026-09-24 最新本地发布：**CombatInteraction_R1**。镜头统一改为 **17**；普通敌机、敌方武器与 FAZZ 主装甲统一白色，保留深色结构及红色攻击提示。参考本地 Hades 敌人行为设计，增加按兵种与武器决定的接近路线、侧翼换位、受击退避、掩体探头和队友支援；保留可躲避的攻击方向及反击窗口。普通敌人耐久与我方移动、伤害沿用上一版。**30 组 / 3360 项检查、147 文件完整性及桌面入口静默验证通过**，旧版本及永久存档保留。直接打开桌面「机神试炼」。见 [更新与验证](docs/COMBAT_INTERACTION_R1.md)。本轮未提交或推送 Git。

以下为历史记录。

2026-09-24 最新本地发布：**CombatArsenal_R2**。镜头 **20**；瓦尔基里喷气、双背炮和重武器握持修复，步态与落脚声重调；三连斩强化真实接触、停顿和粒子。冥隼独立高速移动、四个以内残影、世界坐标自主浮游炮和统一紫蓝能量色。五种敌机、七类真实武器进入有约束的随机战局；前三关与短战末尾加入原版动作的 HG2250 FAZZ。失败重试恢复原关卡与种子，真实武器可回收、入库、再装备。**29 组 / 3285 项检查、147 文件完整性及桌面入口静默验证通过**，永久存档未变，旧构建保留。直接打开桌面「机神试炼」。见 [完整更新与验证](docs/COMBAT_ARSENAL_R2.md)。本轮未提交或推送 Git。

以下为历史记录。

2026-09-24 最新本地发布：**CombatVelocity_R1**。战斗镜头 **15**；白色敌机按约 4.5 米机体统一体型、碰撞、枪口与血条；两台机体采用 Ka 站并完整展示。冥隼普通移动改为展翼推进滑行，基础速度从 7.4 提升到 **10.4**，加快起步、换向、冲刺与动作恢复；重击停顿缩短，受击反应重调，血量及武器伤害保持。接入 **36 个加工后的高破4音效变体**和新制作的 **170 BPM 电子金属循环 BGM**。保留等身深灰斩舰刀、紫色眼睛、瓦尔基里双背炮与六浮游炮。28 组 / 2450 项最终 Player 检查、147 文件完整性和原入口静默启动核验通过；永久存档未变。桌面继续打开「机神试炼」。D 为源码，构建及证据通过固定目录联接保存在 E 盘。本轮未提交或推送 GitHub。 见 [本轮说明](docs/COMBAT_VELOCITY_R1.md)。

Historical checkpoints follow.

2026-09-24 最新本地发布：**Nemesis_Raiken_R1**。冥隼替换为原瓦尔基里 RAIKEN 的深灰斩舰刀，整刀长度 **4.70617**，与机体高度一致；保留三连斩，适配握持、收刀和刀尖离地。整合瓦尔基里背炮与离子推进表现。24 组最终 Player 套件、2210 项检查及原入口静默启动验证通过，147 个构建文件完成哈希核对，永久存档未变。桌面继续打开「机神试炼」。构建位于 D 盘 `Builds/Nemesis_Raiken_R1`，源码以本工程为准；后续 CombatVelocity 工作尚在独立进行。 见 [等身深灰斩舰刀](docs/NEMESIS_RAIKEN_R1.md)。本轮未提交或推送 GitHub。

以下为历史记录。

2026-09-23 最新本地发布：**NemesisEye_R2**。冥隼眼睛恢复饱和紫色，降低发光强度以避免泛蓝白；保留原头型、眼片贴合和三档 LOD 可见性修复。仅修改眼睛材质，网格几何不变。22 组最终 Player 套件、2156 项检查及静默启动验证通过，永久存档未变。桌面仍使用「机神试炼」，新源文件和构建在 E 盘。 见 [紫色眼睛修正](docs/NEMESIS_EYE_R2.md)。本轮未提交或推送 GitHub。

以下为历史记录。

2026-09-23 最新本地发布：**NemesisEye_R1**。修正灰色冥隼眼片被眉甲遮挡、远景模型简化后不可见以及亮度偏低的问题；发光贴合原始眼睛表面，保留用户选定的头型。近、中、远三档模型的双眼显示均通过实际 Player 像素检查。22 组最终套件、2150 项检查及静默启动验证通过，存档未变；桌面仍使用「机神试炼」。新源文件、模型和构建在 E 盘，见 [眼部修正](docs/NEMESIS_EYE_R1.md)。本轮未提交或推送 GitHub。

以下为保留的历史记录。

2026-09-23 最新本地发布：**ExtractedHeroes_R1**。冥隼与瓦尔基里已换成用户选定的解包资源细化版；两把步枪换为实际提取的武器模型，瓦尔基里独立肩炮删除。修正瓦尔基里枪托与前臂穿胸甲、左手朝向和机库持枪，保留原始动作、冥隼后置腿部喷口与六浮游炮。22 组最终 Player 套件、2136 项检查及静默启动验证通过，永久存档未变。构建、素材、缓存和图片位于 E 盘，D 盘为现有源码工程；桌面仍用原来的「机神试炼」。见 [模型替换与持枪修正](docs/EXTRACTED_HEROES_R1.md)。本轮未提交或推送 GitHub。

以下为保留的历史版本记录。

当前本地试玩版：**GB4Motion_R1**（2026-09-23）。冥隼与瓦尔基里已接入《高达破坏者 4》提取的 14 段待机、前后跑、飞行和三连斩动作；沿用月面地图与 AI/UI R2。22 组最终 Player 套件、2,112 项检查及静默启动验证通过，存档未变。直接打开桌面「机神试炼」即可试玩。见 [原始动作接入](docs/GB4_MOTION_R1.md)。本轮新改动尚未推送 GitHub。

上一版本：**AI_UI_R2 + Lunar_Basin_R1**（2026-09-23）。修复敌人受击打断、近战追击、突进判定及连续射击动作，移除主角脚下标记与圆形准星，统一血量、能量、冷却和机库/暂停/结算界面；整合月面开采盆地与中继区两张完整场地。21 组最终 Player 套件、1,451 项检查及静默启动校验通过，147 个发布文件完成哈希核对。桌面继续使用原来的「机神试炼」，永久存档未变。见 [AI 与界面 R2](docs/AI_UI_R2.md)、[月面地图 R1](docs/LUNAR_BASIN_R1.md)。实际体验与视觉喜好仍以试玩反馈为准。

## 从 GitHub 在另一台电脑继续开发

项目源码使用 `codex/equipment-loop-v1` 分支同步；AI_UI_R2 最终整合后的改动仍在本机工作区，本轮没有提交或推送，远端此前的同步不能代表最终发布状态。克隆私有仓库时启用 Git LFS，确认大型 J-01 模型资源已下载，然后使用 `ProjectSettings/ProjectVersion.txt` 指定的 Unity 版本打开项目和 `Assets/Scenes/Demo_Main.unity`。源码范围包括 `Assets`、`Packages`、`ProjectSettings`、工具和说明文档；Unity 会在另一台电脑重建 `Library`。

`Builds`、测试证据、本机存档和 `Launcher/current.json` 是每台电脑自己的运行数据，未随源码同步。在公司电脑要试玩新版时，需要从源码构建并在当地发布；仅克隆仓库不会带来本机桌面快捷方式指向的 Windows 可执行文件。

2026-09-23 地图与敌人决策 **Arena_Tactics_R1**：两张战术布局原型、遮挡感知与绕行选射位、修复运动预判计时和包围目标漂移、修复突进名额泄漏、近战前沿及枪兵两翼入场。19 组最终 Player 检查通过。地图细部美术和真人体验仍待试玩反馈。详见 [地图与 AI R1](docs/ARENA_TACTICS_R1.md)，实际桌面版本以 `Launcher/current.json` 为准。以下旧记录保留作历史。

当前主角动作检查点：**J-01 NEMESIS M01**。B20 冥隼、原设计的步枪 / 光束剑 / 火箭炮、六枚浮游炮已接入；掌心朝内，双脚各外开 12°，单手伸臂持枪、更大幅度三连斩，空格展翼并留下暗紫残影。见 [冥隼接入与验证](docs/NEMESIS_M01.md)。桌面实际版本仍以 `Launcher/current.json` 为准，入口继续使用唯一的“机神试炼”快捷方式。

沿用的战斗底层：**Enemy AI/VFX R1 + Metal Impact R1 · 群体敌人协调、贴图特效与金属打击感**。普通敌人获得攻击位协调（全场最多 3 名同时攻击）、黄金角包围站位、近战突进与三连爆发（带提前量预判）、打完换位；精英爆发锁定预警方向不跟踪。敌方死亡/枪口/弹着/破甲接入 Kenney CC0 贴图特效，所有圆盘预警获得充能填充。金属打击感：分层合成命中音效（瞬态+金属余振+钝响）、受击点白热火花与翻滚碎屑、枪击机体后坐与硬直镜头微震。参数全部进 CombatRules v4，详见 [R1 说明](docs/ENEMY_AI_VFX_R1.md) 与 [金属打击感说明](docs/METAL_IMPACT_R1.md)。

正常试玩沿用桌面唯一的 **机神试炼** 快捷方式，直接进入零强化短战斗。R 重开，F4 脱困，Esc 暂停菜单也有脱困按钮，Backspace 返回机库。旧完整流程保留。测试过程中不自动打开可见游戏或通过扬声器播放声音。

- 敌方群体行为：没有"排队挨打"——多余的敌人会保持包围环等待攻击位；近战敌人会在中距离突进（有预警线+落点盘）；远程敌人三连发且会预判移动目标，打完侧移换位。精英仍然承诺射击方向，绕侧是反制手段。
- 始终携带当前远程武器、光束剑和固定 E 支援；鼠标手动瞄准，左键射击，右键 / Q 连斩，空格冲刺 / 按住推进，E 支援。
- 普通敌人直接扣生命；重装敌人的护甲可由枪刀耗尽，破甲当击不溢出生命、不自动回复。Boss 在招式后摇露核。
- 清场后保留残骸与 F 操作；可试装，也可按 Enter 或界面继续，只收藏新枪。随后三选一，再进入下一场。
- 六种当局强化：刀距、刀速、射速、贯穿、推进效率、E 回转；各最多三级，换枪按基础值重算。失败 / 重开清空当局强化，永久收藏保留。
- 正式流程按 Esc 暂停，失焦暂停；结算按 R 立即重开，或返回出击准备。试场按 Backspace 返回机库，F9 是独立的 30 秒表现对照。

此前 Enemy AI/VFX R1 的 5 个隐藏 Player 套件（foundation / tactics / impact / regression / fullplay）通过后发布到 Launcher。冥隼本轮的最终验证以其独立证据目录和 `Launcher/current.json` 为准；自动通过不代表已获得真人对难度与表现的认可。

## 历史版本记录（以下不作为当前玩法规则）

2026-09-18 本地开发版本：**Impact R2 · 60 秒战斗打磨：脱困、刀光、命中与音效**。重点为基础装备、零强化短战斗。具体 Hades 数据依据与改动见 [R2 说明](docs/IMPACT_R2_HADES_REFERENCE.md)。源码与版本化构建保存在 D 盘工作树，尚未提交 Git。

2026-09-17 本地开发版本：**Combat Foundation V3 · 战斗底层重构**。依据用户本地 Hades 玩法脚本，接入只读配置、统一动作请求、不可变伤害结果、敌人攻击阶段、遭遇状态机与出生预留取消。20 组实际 Player 检查、1412 项 PASS，原生基础装备战斗约 60.7 秒。见 [落地与验证](docs/COMBAT_FOUNDATION_V3.md)、[Hades 分析与迁移图](docs/HADES_FOUNDATION_ANALYSIS.md)。

上一版本：**Combat Loop V2 · 30 秒战斗对照**。已完整读取用户上传的 AC6 调研原文。F9 进入短战斗，1 / 2 / 3 比较基础反馈、完整反馈、关闭 Break；修复刀击停顿混用时钟、重开残留和出生点被占造成的空场。19 组实际 Player 检查通过，原生完整战斗约 63.5 秒。沿用桌面唯一快捷方式，实际构建以 Launcher/current.json 为准，见 [原文对应、改动与验证](docs/COMBAT_LOOP_V2_LAB.md)。以下战术版、V2 初版、R9 等为历史记录。

2026-09-16 本地开发版本：**R9 战斗音效重整**。正常体验使用桌面唯一的 `机神试炼.lnk`；它通过 `Launcher/current.json` 指向已验证的构建，后续版本沿用这个入口。最远 C 镜头、基础 M7 + 斩舰刀、零强化。见 [R9 声音参考、整改与验证](docs/COMBAT_SLICE_R9_AUDIO.md)、[战斗细节标准](docs/COMBAT_POLISH_STANDARD.md)。
47 个新音源覆盖枪刀、推进、敌枪、装甲承压、破防、贯穿斩杀、自机受击、机械切换及 F 四阶段。34 类事件有 Unity 原生混音逐项记录；63.3 秒脚本输入实战完成 46 击杀。自动测试通过不代表已获得玩家的音色或爽感认可。R8 与更早构建保留回退。
本地改动尚未提交；以下 V8/P0 等说明属于历史记录，不代表当前桌面启动包。

2026-09-07 最新版本：**V8 液态 Boss 接入**。首页点击“挑战液态 Boss”，携带当前仓库装备和六项临时随机强化直达首领战；正常出击的最终首领同样使用 E-01 液态金属侵蚀体。保留 V7 的大幅度三连斩、新斩舰刀音效、白兵和苍钢卫士试战。Windows 本地试玩：`Builds/ValkyrLiquidBossV8/MECH_TRIAL_Valkyr.exe`。接入、构建方式和验证记录见 [液态 Boss 与首页入口 V8](docs/LIQUID_BOSS_V8.md)。当前游戏源码保存于 GitHub 的 `codex/equipment-loop-v1` 分支。

前一轮 V7：`Builds/ValkyrPowerComboV7/MECH_TRIAL_Valkyr.exe`。放大三连斩的转身、踏进、横扫和重劈幅度，并更换斩舰刀音效：蓄力、换握、三种破风、装甲命中分别触发。右手反握与刃口朝下保留。操作、实际短片及动作参数见 [大幅度三连斩与新音效 V7](docs/VALKYR_POWER_COMBO_V7.md)。当前工作目录位于 D 盘，下文较早版本均保留。

前一轮 V6：`Builds/ValkyrReverseComboV6/MECH_TRIAL_Valkyr.exe`。右手低位反握，蓝色刃口朝下；Q / 右键逐次按出反手斩、侧甩换握回斩、正面重劈。停按后收回反手待机，保留枪刀切换、白兵及永久装备仓库。历史接入记录见 [反手持刀与三连斩 V6](docs/VALKYR_REVERSE_COMBO_V6.md)。

前一轮 V5：`Builds/ValkyrCombatV5/MECH_TRIAL_Valkyr.exe`。单手持刀、枪刀切换、刀刃轨迹命中和 E-01 白色步枪兵的历史接入记录见 [单手斩舰刀与白兵 V5](docs/VALKYR_COMBAT_V5.md)。

前一轮 V4：`Builds/ValkyrActionV4/MECH_TRIAL_Valkyr.exe`。蓝色光刃、挥刀碎光、敌方受击闪光、真实刀尖贴地火花和移动粒子记录见 [斩舰刀与移动表现 V4](docs/VALKYR_ACTION_V4.md)。

前一轮试玩：`Builds/ValkyrMotionV3/MECH_TRIAL_Valkyr.exe`。在已接入的第三版机体上重做刀柄握持、刃口方向、胸腰胯发力、踏进与收招、支撑脚和奔跑摆臂。实际游戏镜头与关闭刀光的动作片段见 [动作重做 V3](docs/VALKYR_MOTION_V3.md)。模型外观未编辑；以下 `ValkyrV3` 和 `ValkyrCombat` 为此前构建。

此前第三版厚装甲、大腿与膝甲、当前头部已装入游戏，模型接入记录见 [第三版模型更新](docs/VALKYR_V3_MODEL_UPDATE.md)。当时的动作检查不代表本轮用户认可动作表现。

此前 `Builds/ValkyrCombat/MECH_TRIAL_Valkyr.exe` 接入 VALKYR 与 RAIKEN Mk-II 斩舰刀，昨天的 Jishen V3 机体改为敌方精英首领。进入机库可点“斩舰刀 · 首领试战”。WASD 奔跑，右键 / Q 全身重斩，空格点按冲刺、按住推进飞行；支持空中挥刀。历史动作见 [全身动作 V2](docs/VALKYR_MOTION_V2.md)，模型接入见 [新主角与斩舰刀接入](docs/VALKYR_COMBAT_HANDOFF.md)。

本版保留永久武器与背包仓库、敌人部件吸收、战后换装及局内随机 buff；本局结束 buff 清空。前一版程序 `Builds/EquipmentLoop/MECH_TRIAL_Equipment.exe` 保留，操作见 [装备玩法交接](docs/EQUIPMENT_LOOP_HANDOFF.md)。以下为旧版原型记录。

最新一轮：右摇杆按住拖动瞄准射击、独立挥刀键，以及低饱和环境与 UI。桌面鼠标瞄准、左键按住射击，右键 / Q 挥刀；不再自动炮击。保留工业建筑、推进器冲刺和桌面不限帧，未动另一边的主角模型制作。见 [操作与配色交接](docs/MANUAL_COMBAT_HANDOFF.md)。

Unity 6 上帝视角机甲 Roguelite 作品原型。当前源码已接通手机横屏操作、手动射击与挥刀、两战区六场战斗、六次强化、两阶段 Boss 和一次存档续关。历史自动射击版本的 626.66 秒 Play Mode 回归不代表本轮新操作验证。当前交接以 [操作与配色交接](docs/MANUAL_COMBAT_HANDOFF.md) 为准，[项目审计](docs/AUDIT_2026-09-05.md) 保留改版前基线。当前流程：

`机库出击 -> 维修区三场（每场三选一）-> 反应堆区三场（每场三选一）-> Boss -> 胜负结算 -> 续关一次或返回机库`

现有内容包括原生绕障导航、分批增援、动态火花烟尘、池化弹药、成品音效和流式配乐。中英文、音量/震动/画质设置已通过 Play Mode 验证。真实手机多点触控、真人难度和人耳听感仍待评审。测试证据分别位于 `mobile-01`、`maintenance-02`、`progression-03`、`presentation-04`，素材来源见 `docs/ASSET_PROVENANCE.md`。

较早版本使用 joney_lol 的 Rigged robot（CC BY 3.0）及 Quaternius CC0 动作，旧 Meshy 模型保留回退；当时只有单段范围斩击。现用 VALKYR / RAIKEN 的模型接入和三连斩以上方 V7 记录为准。历史模型资料见 [主角交接](docs/HERO_MODEL_HANDOFF.md)。

## 直接试玩 Windows 版

最新手动操作 / 配色版：`Builds/Windows/MECH_TRIAL_20260905.144944/MECH_TRIAL.exe`，同名 ZIP 已打包。鼠标左键按住射击，右键 / Q 挥刀。专项 Play Mode 与骨骼回归通过；独立 Player 正常速度通关 606.38 秒、六次强化、两阶段 Boss、三次重开，0 运行错误 / 警告。以下为保留的上一版记录，不要混同新操作测试。

运行：

`Builds/Windows/MECH_TRIAL_20260905.133754/MECH_TRIAL.exe`

这是场景/冲刺/不限帧版，压缩包同名位于 `Builds/Windows/`，约 50.7 MiB。构建与最终 Player 回放均为 0 Error / 0 Warning；正常速度实弹通关 **608.77 秒、324 次冲刺、六次强化、498 次击破、Boss 两阶段、三次重开**。专项证据在 `industrial-01/`，最终发布版证据在 `industrial-player-02/`。桌面 `targetFrameRate=-1`、`vSyncCount=0`；后台 1080p 连续离屏渲染均值/P95 为 **1.245/1.505 ms**，最长帧 **56.096 ms**。这不是桌面呈现 FPS、手机性能或真人手感验收，不能宣称稳定 800fps 或完全无卡顿。见 [场景/冲刺动作截图](docs/INDUSTRIAL_DASH_EVIDENCE.md)。

上一版主角构建 `MECH_TRIAL_20260905.095954` 和 48.3 MiB 压缩包保留；其 622.88 秒通关证据仍在 `hero-player-02/`，不是本轮不限帧性能结果。

早上版本 `MECH_TRIAL_20260905.054835`、旧 `MECH_TRIAL_20260905` 和 `MECH_ROUGE_Demo` 均未覆盖。以下数字属于早上基线 `docs/audit-evidence/2026-09-05/player-04/`，不是本轮新主角的通关或性能结论：

- 实弹自动移动通关 **623.74 秒、六次强化、501 次击破、Boss 61.63 秒**，无强制清敌、加速或续关。
- 1920x1080 完整 Player 离屏持续渲染，均值/P95 **16.74/17.02 ms**，运行 **0 Error / 0 Warning**，三次重开无持续内存增长。
- 记录 32 次 GC 回收；本机发布版的逐帧 GC 字节计数器不可用，不能把报告的零字段理解为零分配。
- 这不是物理手机或桌面呈现延迟测试。方法和边界见 [Player 验证](docs/PLAYER_VALIDATION.md)。早上版本包含的用户主角模型公开分发授权仍需确认；新主角的署名和授权见 `docs/ASSET_PROVENANCE.md`。

下列为手机改版前的历史验证，不能混同新版：

- `docs/audit-evidence/2026-09-05/P0LivePlay.log`：Editor 实弹通关与阶段保护，自动瞄准，不使用清敌作弊。
- `docs/audit-evidence/2026-09-05/P0WindowsBuild.log`：Windows64 构建成功。
- `docs/audit-evidence/2026-09-05/P0Standalone.log`：独立播放器流程冒烟，仍使用强制清敌；不是独立播放器自然通关或性能验收。

## 在 Unity 中运行

1. 使用 Unity 6.3 LTS 打开项目。
2. 打开 `Assets/Scenes/Demo_Main.unity`。
3. 等待脚本与资源导入完成。
4. 进入 Play Mode。
5. 在机库点 `DEPLOY` 开始任务。

不要在普通启动时运行 `MECH ROUGE > Build Phase 1 Demo`：它会重建/覆盖生成的场景、Prefab 和材质。只有明确需要重新生成且已保护改动时才使用。

需要生成新版 Windows 64 位试玩包时，运行菜单 `MECH ROUGE > Build Versioned Mobile Slice (Windows)`，每次输出独立带版本号目录。旧 `Build Windows Demo` 为历史入口，不要用来覆盖旧试玩包。

## 当前源码操作

- `WASD` 或左下摇杆：奔跑
- 鼠标瞄准、按住左键射击；触屏按住拖动右摇杆瞄准射击，松手停火
- 右键 / Q 或独立挥刀键：逐次按出三连斩，冲刺可取消；远程状态会先拔刀
- `Space` 或右侧 DASH：点按冲刺，按住持续低空推进
- `E` 或右侧 SALVO：四发追踪导弹，冷却 10 秒，无装备前置
- `1`、`2`、`3`：选择强化
- `Esc`：暂停或继续
- `R`：结算后重新开始

触控验证使用 EventSystem 合成指针，不等于手机真机验收。暂无 Android/iOS 构建。

## 当前内容

- 维修平台、反应堆两战区，各三场有时间编排和存活上限的连续增援
- 每场结束随机三选一，共六次；八种强化，有等级上限与分裂/爆炸、射速/穿透组合
- 第二战区加入更多远程/精英压力
- 两阶段 Boss 战
- 近战、远程、自爆、精英和 Boss 五类机甲轮廓
- 玩家 HP、能量、冲刺、目标、敌人数和 Boss 血条 HUD
- 命中反馈、伤害数字、相机震动、弹道、出生预警和攻击预警
- 暂停菜单、胜负结算和重新开始
- 机库部署界面与 DEMO、Standard、Veteran 三档本局难度
- 展示难度一次战区/Boss 入口存档续关，回滚失败段收益
- 中英文切换、主音量/音乐/音效、震动开关、两档画质和制作名单
- Kenney CC0 战斗音效与 Vitalezzz《Subspace》流式配乐；人耳听感尚待确认

DEMO（代码枚举 Cadet）是默认推荐档：敌人生命为 78%，玩家承受敌方伤害为 65%，提供一次续关。Standard 保持基准战斗数值，Veteran 提高敌人生命和伤害并降低维修量。

## 主角模型

当前角色使用 `Assets/Prefabs/Player/RiggedSentinelVisual.prefab`：48 骨骼蒙皮、约 1.8 万三角面和重定向动作，由现有 `PlayerMechLoader` 加载，不需要运行时 GLB 导入器。来源、授权、动画边界和可重复 Blender/Unity 管线见 [主角交接](docs/HERO_MODEL_HANDOFF.md)。

旧用户模型仅作为回退，原始文件保留在：

`Assets/UserContent/PlayerMech/Meshy_AI_Rose_Gold_Sentinel_0618172915_texture.glb`

旧 `RoseGoldSentinelVisual.prefab` 使用该 GLB 离线转换出的 OBJ 网格，并保留原始 PBR 贴图：

- High：116,398 三角面
- Medium：32,942 三角面
- Low：8,646 三角面

旧模型的三档网格保留原有交叉淡化 `LODGroup`，不是新主角的 LOD 配置。旧资产不在本轮新版试玩包的场景/Resources 依赖中，未删除或覆盖。

## 验证

- 实弹与阶段保护：`Assets/Editor/ProjectAudit.cs`，入口 `ProjectAudit.Run`；枪口回归入口 `ProjectAudit.RunShotDiagnostics`。
- 手机操作和自动瞄准：`ProjectAudit.RunMobileTests`；证据 `docs/audit-evidence/2026-09-05/mobile-01/`。
- 编辑器流程冒烟（强制清敌）：`Assets/Editor/DemoPlayModeSmoke.cs`
- 局部压力渲染（不代表完整游戏性能）：`Assets/Editor/DemoPerformanceSmoke.cs`
- Windows 构建：`Assets/Editor/DemoBuildPipeline.cs`
- 旧独立播放器强制清敌冒烟：`Assets/Scripts/Core/StandaloneRuntimeSmoke.cs`
- 新独立播放器实弹/渲染诊断：`Assets/Scripts/Core/SlicePlayerAudit.cs`，只有 `-sliceAudit` 参数启用，非正常玩法入口
- 中英文/设置/安全区：`ProjectAudit.RunPresentationTests`
- 详细交接记录：`docs/HANDOFF.md`
- 验收清单：`docs/ACCEPTANCE_CHECKLIST.md`

验证副本的 Unity 6 启动索引阶段重复记录 `UnityEditor.Search.SearchDatabase` 异常。堆栈不来自游戏程序集，未阻断本轮编译与运行，但 Console 不能标记为完全无错误。完整限制见审计报告。
