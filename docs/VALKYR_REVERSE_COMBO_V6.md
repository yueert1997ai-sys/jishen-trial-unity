# 反手持刀与三连斩 · V6

开发目录：`D:/project-mecha-design/MECH ROUGE/handoff/GameplayLoop_V1`。

新版试玩：`Builds/ValkyrReverseComboV6/MECH_TRIAL_Valkyr.exe`。

## 这次改变

右手默认低位反握，手放在胯侧，刀尖向后下方，实体刀背在上、蓝色刃口在下。跑动和推进沿用该持刀方向。之前从刀的位置反推手臂、强行压低手腕角度的做法，会把整条手臂挤成不自然的姿势；现在先确定手腕与肘的位置，再把刀柄放进掌心。

按 Q / 右键逐次出刀：第一刀反手横斩；第二刀带侧甩、松指换成正握，再回斩；第三刀举过肩部，从正面重劈。左手只平衡，胸、腰、骨盆及支撑脚共同参与发力。每次最多缓存下一刀，按一次不会自动打完整套；停在第一或第二刀也会完整收招并回到反手待机。第三刀结束再次侧转回反握。冲刺可取消出刀及后续缓存。

游戏副本的右手拆成掌部和 15 段手指，换握时实际开合，刀柄保持在掌心。保留原军蓝、深灰和白色材质。没有编辑机体、头部或斩舰刀的 Blender 主工程。

蓝色光刃和拖地火花继续使用现有表现。刃口方向按真实蓝色光刃网格校准，待机时朝下，切割时转到运动方向。命中、刀光和碎光共用实际刀刃轨迹。枪炮请求清掉未执行的下一刀，当前切割结束后收刀；背挂后才允许开火，光刃与刀身伤害关闭。新按近战优先于持续射击。

## 操作

- 机库进入“武器试场”；Q / 右键分次按出三刀，R 重置白兵靶机。
- WASD 跑动；空格点按冲刺、按住低空推进。
- 左键收刀射击；E 收刀后使用炮击技能。松开射击保持远程状态。
- Q / 右键从远程自动拔刀。按近战后，左键须松开再按才切回射击。
- 白兵、步枪吸收与永久仓库保持 V5 的流程，试场不产出永久收益。

## 可调节参数

`Assets/Resources/ValkyrMotion/ReverseCombo.asset` 保存三段动作关键姿态、换握与开指程度、接续时间、命中窗口和踏进距离；可在 Unity Inspector 中编辑。`ValkyrComboProfile.cs` 提供初始默认值。手臂、躯干与腿部仍是适配现有分段机甲的程序动作，没有新增完整蒙皮绑定。

| 动作 | 有效切割 | 接下一刀 | 单独收招结束 | 踏进 |
|---|---|---|---|---|
| 反手起斩 | 0.17–0.33 秒 | 0.38 秒起 | 0.70 秒 | 0.34 米 |
| 侧甩换握回斩 | 0.30–0.49 秒 | 0.56 秒起 | 0.94 秒 | 0.30 米 |
| 正面重劈 | 0.32–0.51 秒 | 无 | 1.00 秒 | 0.48 米 |

以上时间从各段开始计算，不含命中停顿。连上三刀约 1.94 秒，命中另有短暂停顿。单刀基础伤害仍为 78，本轮没有另改伤害平衡。

## 实际运行与预览

Windows Player 输入回放验证了三刀顺序、停在一刀 / 两刀的收招、每刀只伤害一次、恢复反握、跑动时刃口朝下、移动 / 推进枪刀切换和冲刺取消。30 / 60 / 120 帧条件下同一靶机均受到三次 78 伤害，范围外靶机未受伤。

- 构建结果：`AuditEvidence/reverse-combo-v6/build-result.txt`。
- 回放报告：`AuditEvidence/reverse-combo-v6/film/quick-check.txt`。
- 实际游戏镜头：`AuditEvidence/reverse-combo-v6/gameplay.mp4`。
- 同步近景：`AuditEvidence/reverse-combo-v6/closeup.mp4`。
- 侧面持刀与握柄：`AuditEvidence/reverse-combo-v6/film/ready_right.png`、`hand_ready.png`。
- 连续帧和动作轨迹：`AuditEvidence/reverse-combo-v6/film/frames/`、`game-frames/`、`motion.csv`。

两条片段各 299 帧、约 9.97 秒，来自独立 Windows Player 的实际输入回放；近景只改变观察镜头。已查看反握侧面、握柄、换握连续帧、横斩和重劈画面，也用 computer-use 打开新版武器试场并触发了实际输入。测试使用独立装备存档。这是动作预览和局部行为验证，整套操作的主观手感仍留给本轮试玩评审。

重劈起手仍有短暂定势，侧甩换握为配合机甲尺寸做了夸张；下一步应根据正常游戏镜头下的试玩反馈调整节奏和幅度。原有 V5 的白兵掉落、墙体遮挡等记录在 `docs/VALKYR_COMBAT_V5.md`，本轮没有重复做整局回归。

## 复现

在开发目录执行：

```powershell
# 修改现有动作参数或脚本后，仅重新构建；保留 Inspector 中的姿态调整。
& 'C:/Python313/python.exe' tools/run_reverse_combo_v6.py rebuild
& 'C:/Python313/python.exe' tools/run_reverse_combo_v6.py film
& 'C:/Python313/python.exe' tools/package_reverse_combo_v6.py
```

需要重新生成右手游戏副本及恢复默认姿态时：

```powershell
& 'C:/Program Files/Blender Foundation/Blender 5.2/blender.exe' --background --python tools/export_v6_hand.py
& 'C:/Python313/python.exe' tools/run_reverse_combo_v6.py build
```

`build` 会重建 V6 手部副本并恢复 `ReverseCombo.asset` 默认姿态；正常迭代使用 `rebuild`。

手部来源为 `art_prototypes/JishenTrial_Hero_Assembly_V3/body_mass_refinement_20260906/iteration_v3/game_update/SOURCE_CURRENT_MASTER.blend`。实际导出 60 个原网格，整理为 16 个部件、4,816 个三角面；源文件路径和 SHA-256 记录在 `Assets/Art/ValkyrHand_V6/hand_meshes.json`。原工程只读，导出器不保存源 `.blend`。Unity 使用 `D:/Editor/6000.3.18f1/Editor/Unity.exe`。

Git 分支：`codex/equipment-loop-v1`。旧 V5 构建和原有未提交工作保留，本轮未提交、未推送；最终工作区状态见 `AuditEvidence/reverse-combo-v6/git-status.txt`。修改前的相关脚本和主角预制体副本保留在 `AuditEvidence/reverse-combo-v6/baseline/`。
