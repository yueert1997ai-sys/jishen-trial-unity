# 机神试炼 / MECH ROUGE

2026-09-08 新增：**战前格纳库配装**已接入当前 `Assets/Scenes/Demo_Main.unity`。使用第三版厚装甲机体；进入格纳库默认空手，选择 M7、RAIKEN 巨剑或 M14 后才装备，点击出击沿用选择。支持卸下、近距离查看、拖动旋转和滚轮缩放。Mac 本地试玩入口为 `Builds/Mac/MECH_TRIAL_Hangar_20260908.app`（构建包不随 Git 同步）；实现、素材来源和验证范围见 [格纳库配装交接](docs/HANGAR_LOADOUT_20260908.md)。本次未更新下文 Windows 历史试玩包。同期制作的月球基地可编辑源文件见 [MARE-07 场景说明](art_prototypes/LunarBase_Sector01_20260908/README.md)。

## GitHub / Mac 接续

当前 Unity 工程的私有仓库：<https://github.com/yueert1997ai-sys/jishen-trial-unity>。默认分支为 `codex/industrial-dash-polish`。另一仓库 `mecha-marco` 是 JavaScript 网页版，不是此 Unity 工程。换电脑请使用有权限的 GitHub 账号克隆本仓库，安装 Unity `6000.3.18f1`；详见 [Mac 接续说明](docs/MAC_GITHUB_HANDOFF.md)。今晚继续制作的文件需保存后再次提交推送，未保存的编辑器状态不会自动进入 GitHub。

2026-09-06 最新试玩已在现有游戏副本中更新第三版厚装甲与双手持刀机甲：`handoff/GameplayLoop_V1/Builds/ValkyrV3/MECH_TRIAL_Valkyr.exe`。模型接入、动作检查和继续开发的位置见 [第三版游戏更新](handoff/GameplayLoop_V1/docs/VALKYR_V3_MODEL_UPDATE.md)；该副本保留已实现的装备收藏与推进飞行。下文为本目录此前版本记录。

玩法方向已于 2026-09-06 确认：固定机体，夺取并永久收藏敌方武器与背包；局内 buff 随机获取，结束后清空。后续迭代依据见 [核心玩法约定](docs/CORE_GAME_DESIGN.md)。这些新系统待实现，下文仍为现有原型说明。

最新一轮：右摇杆按住拖动瞄准射击、独立挥刀键，以及低饱和环境与 UI。桌面鼠标瞄准、左键按住射击，右键 / Q 挥刀；不再自动炮击。保留工业建筑、推进器冲刺和桌面不限帧，未动另一边的主角模型制作。见 [操作与配色交接](docs/MANUAL_COMBAT_HANDOFF.md)。

Unity 6 上帝视角机甲 Roguelite 作品原型。当前源码已接通手机横屏操作、手动射击与挥刀、两战区六场战斗、六次强化、两阶段 Boss 和一次存档续关。历史自动射击版本的 626.66 秒 Play Mode 回归不代表本轮新操作验证。当前交接以 [操作与配色交接](docs/MANUAL_COMBAT_HANDOFF.md) 为准，[项目审计](docs/AUDIT_2026-09-05.md) 保留改版前基线。当前流程：

`机库出击 -> 维修区三场（每场三选一）-> 反应堆区三场（每场三选一）-> Boss -> 胜负结算 -> 续关一次或返回机库`

现有内容包括原生绕障导航、分批增援、动态火花烟尘、池化弹药、成品音效和流式配乐。中英文、音量/震动/画质设置已通过 Play Mode 验证。真实手机多点触控、真人难度和人耳听感仍待评审。测试证据分别位于 `mobile-01`、`maintenance-02`、`progression-03`、`presentation-04`，素材来源见 `docs/ASSET_PROVENANCE.md`。

现用 joney_lol 的 Rigged robot（CC BY 3.0）及 Quaternius CC0 动作，旧 Meshy 模型保留回退。主角模型不在本轮重做。原本仅供预览的挥刀现已接入真实前方范围伤害，尚无连击系统。历史模型资料见 [主角交接](docs/HERO_MODEL_HANDOFF.md)，最新验证见操作与配色交接。

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
5. 在格纳库选择 M7、RAIKEN 巨剑或 M14，再点 `DEPLOY` 开始任务；空手时不能出击。

不要在普通启动时运行 `MECH ROUGE > Build Phase 1 Demo`：它会重建/覆盖生成的场景、Prefab 和材质。只有明确需要重新生成且已保护改动时才使用。

需要生成新版 Windows 64 位试玩包时，运行菜单 `MECH ROUGE > Build Versioned Mobile Slice (Windows)`，每次输出独立带版本号目录。旧 `Build Windows Demo` 为历史入口，不要用来覆盖旧试玩包。

## 当前源码操作

- `WASD` 或左下摇杆：移动
- 鼠标瞄准、按住左键射击；触屏按住拖动右摇杆瞄准射击，松手停火
- 右键 / Q 或独立挥刀键：近战，冲刺可取消
- `Space` 或右侧 DASH：冲刺
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
