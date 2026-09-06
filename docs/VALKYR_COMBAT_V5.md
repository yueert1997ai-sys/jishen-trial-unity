# 单手斩舰刀、枪刀切换与 E-01 步枪兵 · V5

开发目录：`D:/project-mecha-design/MECH ROUGE/handoff/GameplayLoop_V1`。

新版试玩：`Builds/ValkyrCombatV5/MECH_TRIAL_Valkyr.exe`。旧 V4、V3 和更早构建保留。

## 操作

- WASD 奔跑；空格点按冲刺，按住推进。
- Q / 右键：右手单手挥刀。远程状态会先拔刀。左臂只做平衡，不扶刀。
- 左键：先收刀至背包后方，再持续射击。松开保持远程状态。
- E：先收刀，再使用炮击技能。已装备 E-01 步枪时，普通射击从手持步枪枪口发出，炮击仍使用肩炮位置。
- 同时持续左键并按近战，近战优先；需要松开、重新按左键才切回射击。
- 机库“武器试场”：白兵靶机不反击，R 重置，按钮返回机库。这里不产生永久装备、金币或击杀收益。
- 普通出击击败白兵后，靠近按 F 吸收其步枪，永久进入仓库；机库和战后可换装。局内 buff 仍随机且不保存。

## 已实装

单次挥砍 0.72 秒，0–0.20 秒蓄力、0.20–0.40 秒切割，随后惯性收招。胸、腰、骨盆和支撑腿一起参与；保留踏进、命中停顿和冲刺取消。手腕与刀柄接触由实际刚性关节求解，校正过中段过折问题。

持刀、收刀、远程、拔刀共用一个状态。收拔初始各约 0.16 秒；远程请求仅在 0.48 秒后的收招阶段允许打断。实际背挂姿态完成后才开火；背挂关闭光刃、刀光、拖地火花与刀身伤害，跟随躯干且保持刀尖离地。

刀光与伤害读取相同的实际刀刃采样。有效时间内按不大于 1/240 秒的间隔求姿态，覆盖跨帧切割；判定使用刀身在地面的投影与碰撞体边缘，接触容差 0.22 米，没有原先 4.25 米的中心距离截断。同一刀对同一目标只结算一次，墙体阻挡仍生效。

四种普通出生入口统一使用 `E01RifleSoldier.prefab`。基础血量 26、移速 2.9、射击间隔 1.65 秒，继续使用原波次数量和难度倍率；旧机体首领保持独立。白兵具有分段肢体动作、端枪、射击后坐、受击及倒地；普通敌人只掉新 `e01_rifle` 部件，沿用原先“一种装备入库一次”的收藏方式。已有装备没有移除，本轮没有新增其他装备来源或首领掉落。

## 模型与复现

源文件：`D:/project-mecha-design/MECH ROUGE/art_prototypes/Type_E01_20260906/stage_02/TYPE_E01_STAGE02.blend`。

实际使用本机 Blender 5.2.1 LTS 读取、导出。该源文件在导出前后 SHA-256 一致：`4cfbcc891b2dfdb3a8cf9edd4749db16614049269f0a5e1a19105876788ee20f`。主角与刀的 Blender 模型未编辑。

游戏副本位于 `Assets/Art/E01_Stage02_Game`，高度 2.8 米，19 个刚性分组、11 个共享材质、133426 个三角面。每个关节合并为一个网格，排除了摄影棚、参考图及步枪展示副本。白色装甲针对现有场景光照调亮；游戏动作为独立程序姿态，没有给源工程添加完整绑定。

在开发目录执行：

```powershell
& 'C:/Program Files/Blender Foundation/Blender 5.2/blender.exe' --background --python tools/export_e01_game.py
& 'C:/Python313/python.exe' tools/run_combat_v5.py integrate
& 'C:/Python313/python.exe' tools/run_combat_v5.py build
& 'C:/Python313/python.exe' tools/run_combat_v5.py check
& 'C:/Python313/python.exe' tools/run_combat_v5.py preview
& 'C:/Python313/python.exe' tools/package_combat_v5_preview.py
```

Unity：`D:/Editor/6000.3.18f1/Editor/Unity.exe`。导入器只修改游戏副本；测试程序强制使用独立仓库存档，未修改玩家正式存档。

## 验证与范围

独立 Windows Player 已验证：单手接触、手腕、背挂开火、同时按键、收招缓冲、连续切换、移动和低空推进切换、近距与刀尖边缘、范围外、墙后、多目标、跨过完整有效窗口的长帧、冲刺取消、白兵射击与掉落、吸收入库、重新读取仓库、装备步枪及炮击切换。

30 / 60 / 120 帧条件下，近身和中段每刀各命中一次；实测刀尖扫掠边缘可命中，范围外不命中。测试刀尖位置由当前模型实际运动测得，避免把被贴身靶机挡住的踏进距离算入额外刀长。

最终录制回放 34 项检查通过、0 运行错误；构建 0 错误、0 警告。详细结果：`AuditEvidence/combat-v5/check/quick-check.txt`；最终录制时的结果：`AuditEvidence/combat-v5/preview/quick-check.txt`；构建结果：`AuditEvidence/combat-v5/build-result.txt`。

正常游戏镜头：`AuditEvidence/combat-v5/gameplay.mp4`；同步近景：`AuditEvidence/combat-v5/closeup.mp4`；白兵实际游戏截图：`AuditEvidence/combat-v5/preview/e01_in_game.png`。两条短片各 368 帧、约 12.27 秒，来自真实 Windows Player 的输入回放，按 30 fps 合成。已查看实际普通游戏镜头的连续挥砍画面，并检查近景握持、背挂、奔跑、白兵外观及装备步枪姿态。它用于本轮造型和动作预览，不是整局真人手感或手机性能验收。

当前仍是单段斩击，没有增加连招。白兵尚未制作面向大规模同屏的 LOD；先保留 Stage 02 的外形。后续可根据试玩反馈调整动作幅度和节奏。

Git 分支：`codex/equipment-loop-v1`。原有未提交改动保留；本轮新增/修改均未提交、未推送。最终状态另存 `AuditEvidence/combat-v5/git-status.txt`。
