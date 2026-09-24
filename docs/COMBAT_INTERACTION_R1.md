# 镜头 17、白色敌机与敌人交互改版

CombatInteraction_R1，2026-09-24。继续使用桌面「机神试炼」。

## 已改内容

- 普通战斗、短战和 Boss 战镜头统一为 **17**，继续按地形边界跟随机体。
- 敌方主装甲统一白色，包括 E01、吉姆、扎古Ⅱ、大魔、钢加农和 FAZZ；保留深色关节、面板细节、传感器及敌方红色攻击提示。
- 敌人根据机体、装备和前排职责采用不同的接近与开火距离：突击、精准射击、近战突进、侧移袭扰、后排重炮。进攻路径会参考你的实际移动、掩体和队友占用的射击位置。
- 实际命中会触发受压反应；合适时短暂脱离或找掩体，再出来开火。你贴近远程敌人，它会拉开距离，但有退避冷却及停下反击的窗口。
- 附近能看到战况的队友会换一个角度施压，射击位置互相留出空间。开火后再换位，避免总沿同一条路线前进。
- 失去视线后，按最后看到的位置搜索、绕过障碍；到不了目标点会限时重新寻路。不会隔着掩体持续更新你的准确位置。
- 重炮与近战突进保留明确预警和已提交的方向；躲开突进后，敌人会进入可以反击的收招阶段。命中、打断、暂停、死亡及重开仍由原有战斗系统执行。

普通敌人的耐久等级、我方移动与伤害沿用上一版。五个存活及预留名额、三个同时攻击名额和一个重炮上限保留；随机阵容与粒子随机继续分开。

## Hades 参考落实

直接查阅之前已定位的本地 `EnemyAI.lua` 与 `EnemyData.lua`：攻击前后移动、后退缓冲距离、躲藏后探头、按武器配置射程、寻路超时及攻击后摇。本轮用新的 C# 决策与寻路实现这些行为关系，没有导入 Hades 引擎或美术资产。

## 验证

同一最终构建通过 **30 组检查、3360 项断言**。新增检查在 30／60／120 帧下验证实际中弹后的换位、队友支援、贴近后的有限退避、射击承诺、躲突进后反击、绕真实掩体、暂停与退出清理。敌机白色材质复用及普通／Boss 镜头 17 均通过实机检查。

纯枪、纯刀、混合短战及六关完整流程使用正常输入回放验证；新增专门的 AI 检查使用隔离测试场景和受保护的玩家，以检查行为因果。自动检查不替代实际手感验收。

RTX 4070 Ti、1600×900、镜头 17 的独立模拟与同步渲染测量如下；隐藏窗口测试数据不是显示帧率。

| 场景 | 平均毫秒 | P95 毫秒 |
|---|---:|---:|
| idle | 2.25 | 2.78 |
| boost-no-ghost | 2.33 | 2.81 |
| boost-ghost | 2.78 | 3.43 |
| boost-support | 3.62 | 4.35 |
| arsenal-crossfire | 5.46 | 7.04 |
| fazz-missiles | 3.49 | 4.19 |

实际游戏内混音捕获峰值 0.5760，削波采样为 0。测试保持静音，永久存档未改动，上一版保留。


## 维护

权威源码仍为 D:/project-mecha-design/MECH ROUGE/handoff/GameplayLoop_V1；缓存构建工程仍为 E:/SteamLibrary/JishenBuildWork/GB4Motion_R1/Project。新发布文件在 E:/SteamLibrary/JishenBuildWork/CombatInteraction_R1，并通过项目 Builds/CombatInteraction_R1 与 AuditEvidence/combat-interaction-r1 联接。
主要接口：EnemyCombatBrain / EnemyBattleProfile / EnemySquad；EnemyBase 继续执行伤害、预警、攻击时序与取消。EnemySpawnSpec.closeAssault 与耐久/护甲类型分开。EnemyArmorPalette 与 Resources/EnemyWhiteArmor.shader 共享白色材质，原始模型贴图及玩家材质不变。
EnemySquad 只广播可见战况，决策随机使用独立种子。攻击之间至少间隔 .16 秒，原三攻击者上限继续有效。退避决策有 .2 秒受压观察时间、2.5 秒冷却和有限移动时长；躲掩体后等待 .45-.65 秒再探头。寻路候选有限，目标点有占用成本和超时重试。
Hades 阅读位置与源文件 SHA256：release/hades-ai-reference.json。源码同步校验 518 个文件，见 release/source-snapshot.json。最终程序集 047f69274a17eabc0cc7572a5c5935d31cbb67d380d1f76b8c7e4cd3344f0e09。永久存档 9a27a31f29b08073a7a6866020a851b424385ac77822ab787f3c6e9c417c29a2。
全部最终报告由 release/final-verification.json 的 checks 字段索引。后续发布要求：interaction arsenal velocity drones sound playmix nemesis imported heroes raiken cannon melee impact aiui lunar terrain arena punch vfx loopv2 foundation tactics beam regression fullplay natural fullrun audio performance punchperf；performance/punchperf 单独运行。未执行 Git commit/push。

发布已完成：桌面固定入口指向 CombatInteraction_R1，静默启动核验通过。147 个构建文件已核对，永久存档 SHA256 与改动前相同，CombatArsenal_R2 保留。
