# NEMESIS M02 — 机库切换、推进性能与自动浮游炮

用户本轮要求：旧瓦尔基里加入机库切换，默认仍为冥隼；修正空格推进时的掉帧；E 改为浮游炮伴飞并自动追踪射击。最终已发布版本与逐项证据以 `Launcher/current.json` 为准。

## 机库

“切换机体”菜单提供 J-01 冥隼与 VALKYR 瓦尔基里。全新启动默认冥隼，当前会话的选择在战斗和返回机库之间保留，不改永久收藏存档。战斗中禁止换机。

瓦尔基里实际使用原 `VALKYR_V9.prefab` 与原 `M7.prefab`，恢复双手持枪。M14、TYPE-08、AX-01、RAIKEN 和原四枚导弹支援保留。冥隼使用设计图的步枪、剑、火箭炮与六枚翼片。切换会重新连接动作、握持、推进器、受击材质和枪口，并清掉上一台机体的残影池。界面名称和配装项随实际机体变化。

## 空格推进

M01 的残影直接复用了 268,425 三角面的机体 LOD1，最多四份；项目此前还使用 CPU 蒙皮。M02 改为批量 GPU 蒙皮，并导出独立的战斗与残影网格。展开角度、推进动作、紫色残影持续时间和池容量保留。

| 用途 | 三角面 |
| --- | ---: |
| 近景 / 机库 LOD0 | 645,062 |
| 中距离 LOD1 | 113,813 |
| 战斗远景 LOD2 | 29,512 |
| 每份机体残影 | 6,091 |
| 步枪 / 剑 / 火箭炮残影 | 382 / 420 / 454 |

浮游炮运动需要较大的剔除范围；LOD 距离改为按 4.7 米机体高度判断，避免扩大运动边界后战斗镜头始终选到高面数网格。残影共享已导出的单材质网格，不再运行时复制武器网格，也不逐帧烘焙蒙皮。

性能对照来自同一 RTX 4070 Ti、同一 1600×900 离屏场景、相同固定 60 Hz 指令。每阶段预热 45 帧，取 240 帧窗口中的 239 个帧间隔；每帧包含模拟、实际场景渲染和 GPU 同步。前后测试串行运行，没有与构建或其他 Player 检查并行。它不是屏幕呈现帧率，也不是最坏敌群的 FPS 保证。原始值与方法保存在基线及最终 release 的 `performance.json` 中。

## E 自动浮游炮

- 六枚真实背包翼片离体，在机体两侧伴飞，分别选择范围 22 米内可见的活敌人；有多个目标时分配火力，敌人死亡、出界或被遮挡后重新索敌。
- 无目标时也可按 E 部署，有敌人进入后自动开火。玩家可以同时开枪、挥剑和推进。
- 一次部署持续 7.5 秒，每枚最多六次射击，交错开火；最后 0.65 秒平滑接回当前运动中的安装座。
- 保留原支援的直接伤害总预算：基础四发 × 18 = 72，分配为 36 次 × 2。强化倍率继续从现有系统读取；光束不附带原导弹的范围爆炸。基础冷却仍为 10 秒，已被占用的六枚浮游炮不能重复生成；界面就绪时间同时考虑归位。
- 射击使用 18 个固定 LineRenderer：每枚有亮芯、紫色包层和较淡光晕。每次只结算一次实际光束接触，淡出不会持续伤害。没有复制机体、隐藏导弹或新增动态光源。
- 保持本项目的地平面战斗判定：使用真实炮口显示光束，碰撞在 PlanarCombat.Height 上查询。实体掩体挡住索敌和射击，普通地面敌人无需抬高即可命中。
- 暂停冻结飞行、光束寿命和射击；死亡、返库、重开和换机清理支援状态。

## 资产与验证边界

B20 外观源和 M01 旧构建保留。可编辑准备源为 `art_prototypes/J01_NEMESIS_20260920/animation/M02/J01_NEMESIS_M02.blend`，运行网格在 `Assets/Art/NemesisM02`。动作仍由 Unity 的现有关节姿势与接触系统执行，不声称 blend 中有烘焙动画片段。

本轮实际 Player 检查包含：机库真实按钮与往返换机、两台机体射击/挥刀/冲刺/支援、六枚浮游炮的目标分配/跟随/换目标/遮挡/暂停/死亡/返回/复用、30/120 FPS 的 36 次射击及 72 点总伤害、原 30/60/120 FPS 三连斩、原敌人 AI/护甲/弹丸/光束规则、基础零强化自然输入与六场完整流程。旧 SALVO 的闪避和兼容测试通过真正切换到瓦尔基里运行，未删掉原导弹断言。

所有测试使用隔离存档、隐藏窗口并关闭音频。自动验证与截图不替代用户对实际操作、动作美感和效果的认可。检查日志中的早期候选记录保留，发布只接受最终构建对应的最新通过结果。

## 最终验证记录

最终 Assembly-CSharp SHA-256：`44da13428116a603e8352e6fbecf4529e861806e89efd7a4dd0236738aabea90`。14 项实际 Player 检查均通过，准确路径见 `AuditEvidence/nemesis-m02/release/test-summary.json`。

| 状态 | M01 平均 ms | M02 平均 ms | M01 / M02 P95 ms |
| --- | ---: | ---: | ---: |
| 待机 | 25.15 | 3.29 | 26.69 / 3.81 |
| 推进 · 关闭残影 | 25.79 | 3.20 | 28.59 / 3.85 |
| 推进 · 开启残影 | 25.54 | 3.33 | 27.12 / 4.07 |
| 推进 · 残影与支援 | 25.23 | 3.40 | 27.79 / 3.97 |

支援行的技能已变化：旧版为导弹，新版为浮游炮。主要同场景对照使用推进加残影一行。每阶段最大值与全部原始记录保留在 JSON，未把 GC 计数为零当作零分配证明。

natural：

```text
Rifle: seed=9172026 full=False victory=True seconds=87.27 hp=176.69 lowestHP=176.69 kills=69 shots=352 bladeContacts=0 gunDamage=6006.2 bladeDamage=0.0 armorBreaks=3 maxOccupied=4 rewards=0 installed=False bossSeconds=0.00 peak=0.0000 playerRescues=0 enemyRescues=0 dashes=16
Blade: seed=9172026 full=False victory=True seconds=69.08 hp=80.55 lowestHP=80.55 kills=69 shots=0 bladeContacts=116 gunDamage=0.0 bladeDamage=6006.2 armorBreaks=3 maxOccupied=4 rewards=0 installed=False bossSeconds=0.00 peak=0.0000 playerRescues=0 enemyRescues=0 dashes=43
Mixed: seed=9172026 full=False victory=True seconds=79.90 hp=120.33 lowestHP=120.33 kills=69 shots=204 bladeContacts=56 gunDamage=3554.8 bladeDamage=2451.5 armorBreaks=3 maxOccupied=4 rewards=0 installed=False bossSeconds=0.00 peak=0.0000 playerRescues=0 enemyRescues=0 dashes=35

Ordinary PlayerCommand inputs only; no direct health edits, invulnerability, forced kills, timed waits to pad combat, or spawn suppression. Not human feel approval.

```

fullrun：

```text
Mixed: seed=9172026 full=True victory=True seconds=479.50 hp=94.36 lowestHP=94.36 kills=367 shots=1300 bladeContacts=313 gunDamage=23157.5 bladeDamage=16542.3 armorBreaks=26 maxOccupied=4 rewards=6 installed=True bossSeconds=88.18 peak=0.0000 playerRescues=0 enemyRescues=0 dashes=187

Ordinary PlayerCommand inputs only; no direct health edits, invulnerability, forced kills, timed waits to pad combat, or spawn suppression. Not human feel approval.

```

实际最终 Player 图集位于美术工程 `animation/M02/REVIEW.html`。发布入口与静音启动校验另以 Launcher 记录为准。

M02 已于 2026-09-22 01:06 发布，147 个构建文件通过哈希校验，14 项最新通过结果均对应上述最终程序集。原桌面入口已解析到 `Builds/Nemesis_M02`；静音启动验证通过，记录为 `Launcher/Verification/20260922-010712`。B20 SHA-256 保持 `f96ae5690dbda520fd940f72be7734d063f8f0363d8f354ee5ff8b495e8c94ca`，永久收藏保持 `7db4bf90ba758a4cf034dc2081969bc9de413a41483f59c156d76be7cdbf195d`。
