# Enemy AI coordination and textured VFX R1, 2026-09-19

本轮目标：用户反馈"特效和 AI 太弱、没有挑战性"。改动聚焦普通敌人的群体行为与敌方反馈表现，Boss 逻辑未动。设计参考为用户本地的 Hades 安装（EnemyAI.lua 的 SurroundAI / ProcessAttackSlots / FireAndQuit / LeapIntoRange 等模式），全部为项目原创 C# 实现，未复制任何 Hades 代码或资产。

## 敌方 AI（EnemyBase + 新增 EnemyTactics + CombatRules v4）

- **攻击位协调**：全场同时处于攻击状态的普通敌人不超过 `maxConcurrentAttackers`（3）。位次在起手前申请，发射/取消/死亡释放。没有位次的敌人不再排队直冲，而是保持包围环等待。
- **包围站位**：每个敌人按黄金角获得稳定的环绕扇区（`EnemyTactics.Anchor`），近战环 2.7m、远程环 7.6m，多敌自然散开包抄而不是叠成一条线。
- **近战突进**：普通近战敌人在 1.6–4.5m 且无位次时可发动突进（`meleeLunge*`，0.5s 预警线+落点盘），位移 2.6m 后落点范围伤害 16。风筝不再是免费解。
- **远程爆发**：基础枪械由单发改为 3 连发（间隔 0.26s）；后续弹按目标实际速度做有界提前量（`rangedAimLead` 0.55，限速 8m/s），站桩不受影响、匀速移动会被预判。缴获的 scatter/salvo 多发武器保持原多发逻辑并取代爆发。
- **打完换位**：普通远程敌人每次齐射后向环上随机 ±70° 侧移（`rangedRepositionSeconds` 0.55s），恢复硬直期间也在移动。
- **精英承诺方向**：精英爆发全部沿预警锁定方向发射，齐射期间不转身；"绕侧打背"的反制手段保持（tactics 契约测试为此约束）。
- 所有新参数进入 `CombatRules`（版本 3→4，含校验），遭遇编排未动。

## 敌方 VFX（新增 EnemyVfx + TelegraphVisual 填充）

- 素材为 Kenney Particle Pack 1.1（CC0）的 9 张灰度精灵，见 `docs/ASSET_PROVENANCE.md` 与 `docs/licenses/Kenney_particle-pack.txt`。
- 敌人死亡：贴图火球×5 + 白热闪光 + 扩张冲击环 + 电弧×2 + 贴图烟（原无贴图火花保留叠加）。
- 敌方枪口：按射击方向对齐的贴图枪口焰（屏幕空间旋转）+ 光晕；爆发每轮都触发。
- 敌方弹着：命中墙体/命中玩家的贴图爆点；补了墙体命中音。
- 精英破甲：星芒爆发 + 冲击环 + 电弧。
- 圆盘预警充能层：所有 `CombatEffects.Disc` 预警（含 Boss 迫击炮/冲撞/落点）获得贴图充能填充，透明度随预警进度上升并脉动。
- 新 shader `MECH ROUGE/Particle Additive`（`Assets/Resources/VFX/`），构建必定包含；全部系统沿用 CombatEffects 的运行时代码构建惯例，遵循 `CombatLabSettings.MinimalFeedback`。

## 验证与发布

构建 `Builds/EnemyAI_Vfx_R1`（build-result: Succeeded errors=0 warnings=0），五个隐藏 Player 套件全部通过（`AuditEvidence/enemy-ai-vfx/release/`）：

- foundation（规则契约，含 v4 校验与版本拒绝）
- tactics（精英护甲/承诺射击/闪避中断——首轮失败于"齐射期间转身"，修正为精英爆发锁定方向后通过）
- impact（敌方受击表现）
- regression（30 秒短战斗回归）
- fullplay（完整流程自然战斗）

`tools/p0/publish_playable.py --require foundation tactics impact regression fullplay` 已原子更新 `Launcher/current.json`（147 文件清单校验通过），桌面快捷方式入口不变。首轮失败证据保留在会话记录；本轮证据目录只含最终构建的通过结果。

## 限制

自动输入通过不构成真人手感、难度或音色认可；突进/爆发的强度参数（CombatRules v4 新字段）是首版拍板值，等待玩家反馈调整。Boss 特效仅间接受益（预警充能层），Boss 专项表现留给下一轮。
