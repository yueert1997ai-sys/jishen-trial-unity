# 大幅度三连斩与新音效 · V7

开发目录：`D:/project-mecha-design/MECH ROUGE/handoff/GameplayLoop_V1`。

试玩：`Builds/ValkyrPowerComboV7/MECH_TRIAL_Valkyr.exe`。

## 动作改动

这一版针对“幅度小、没有气势”和旧斩击音效重做。右手单手反握待机、实际蓝色刃口朝下，以及反手横斩 → 换正握回斩 → 正面重劈的三段顺序保持不变。

旧版身体转动和踏进较小，放大手腕轨迹后又暴露出肘部约束把刀路扭成斜向的问题。现在在保持上臂、前臂长度和目标手腕位置的前提下，根据刀的方向选择肘部弯曲位置；第一刀完成横扫，第二刀调整腕部位置后反向扫回，第三刀从高举转到前压重劈。左手随胸部转动向另一侧平衡。

胸部转身关键姿态由约 ±50° 增加到最高约 −78° / +76°，骨盆横移、支撑脚开距和前后错步同时放大。重劈加入约 −18° 后展到 +42° 前压的胸部变化，手腕关键姿态从 3.58 米落到 1.63 米，收招保留身体前倾和回稳过程。以上为姿态参数，不等于受碰撞限制后的每帧测量值。

| 动作 | V6 踏进 | V7 踏进 | 有效切割 | 连招接续 / 单独收招结束 |
| --- | --- | --- | --- | --- |
| 反手横斩 | 0.34 米 | 0.68 米 | 0.17–0.33 秒 | 0.38 / 0.70 秒 |
| 换握回斩 | 0.30 米 | 0.60 米 | 0.30–0.49 秒 | 0.56 / 0.94 秒 |
| 正面重劈 | 0.48 米 | 0.88 米 | 0.32–0.51 秒 | 无 / 1.00 秒 |

时间从各段开始计算，不含命中停顿。保留原出刀节奏；前两刀命中停顿为 0.075 秒，第三刀为 0.095 秒。实际前移仍受角色碰撞限制，不会按表内距离穿过敌人或墙。单刀基础伤害仍为 78。

## 音效改动

原先起刀与命中复用了 `forceField_001`，缺少空挥、接触和重击的区别。当前斩舰刀改用八个独立音频文件：短促蓄力、换握锁定、反手破风、回斩破风、重劈破风、两种装甲切击和重劈装甲冲击。破风在刀进入有效切割前触发，装甲声只在真实命中时触发。第三刀增加低频冲击和较长的金属衰减。

音频由项目内脚本合成气流、低频、机械与能量层，并叠加现有 Kenney CC0 金属撞击采样。没有把旧护盾音简单放大。多目标连续命中限制声音堆叠，命中时短暂压低配乐与环境声；取消攻击会停止尚在播放的蓄力 / 换握声。

声音银行：`Assets/Resources/Audio/Combat/RaikenV7/`。动作参数：`Assets/Resources/ValkyrMotion/PowerComboV7.asset`，可在 Unity Inspector 中调整。原 `ReverseCombo.asset` 和 V6 手部资产保留。

## 试玩操作

- 机库进入“武器试场”，Q / 右键逐次按出三刀；R 重置白兵靶机。
- WASD 移动；空格点按冲刺、按住推进。冲刺取消挥刀和未执行的下一刀。
- 左键自动收刀再射击，E 自动收刀再炮击；近战键自动拔刀。
- 每次最多缓存下一刀。停按会收招回反手待机；新按近战优先于持续射击。

## 实际预览与验证

- 正常游戏镜头，带实际游戏声音：`AuditEvidence/power-combo-v7/gameplay.mp4`。
- 同步近景，带实际游戏声音：`AuditEvidence/power-combo-v7/closeup.mp4`。
- Player 报告：`AuditEvidence/power-combo-v7/film/quick-check.txt`。
- 构建报告：`AuditEvidence/power-combo-v7/build-result.txt`。
- 原始混音录音、音效触发记录和刀路：`film/game-mix.wav`、`film/audio-cues.txt`、`film/motion.csv`，均位于上述证据目录。

Unity 6.3 的 Windows 构建完成，0 errors / 0 warnings。独立 Player 输入回放通过三段顺序、单 / 双刀停止收招、反握刃口朝下、移动 / 推进枪刀切换、近战优先和冲刺取消。30 / 60 / 120 帧模拟条件下，靶机均受到三次各 78 伤害，范围外目标未受伤；这不是实际呈现帧率或整局性能验收。

已查看实际模型的反握侧面、横斩、回斩、举刀和重劈，以及正常游戏镜头下的关键帧。片段约 10 秒，画面与音轨均来自此次实际 Windows Player；近景只改变观察镜头，音效没有在后期另配。原始混音时长 9.963 秒，峰值 0.7805（约 −2.15 dBFS），无满幅削波；触发记录确认三种破风和重劈装甲声进入实际播放路径。

当前工具无法回传可听的音频输入，因此尚未完成耳听音色验收。音量与时序检查不能代替用户试听。完整主观手感也以新版试玩为准。本轮未重复白兵掉落、永久仓库或完整战役回归，相关历史记录保留在 V5 文档。

## 复现与文件状态

在上述开发目录执行：

```powershell
# 修改现有动作资产 / 代码后的常规构建，不恢复姿态默认值。
& 'C:/Python313/python.exe' tools/run_power_combo_v7.py rebuild
& 'C:/Python313/python.exe' tools/run_power_combo_v7.py film
& 'C:/Python313/python.exe' tools/package_power_combo_v7.py
```

重制音频并恢复 V7 初始姿态时：

```powershell
& 'C:/Program Files/Blender Foundation/Blender 5.2/5.2/python/bin/python.exe' tools/author_raiken_v7_audio.py
& 'C:/Python313/python.exe' tools/run_power_combo_v7.py build
```

`build` 会将 V7 资产恢复为 `ValkyrComboProfile.PowerDefaults()`，并设置新声音的导入方式；不重新导出手部或模型。Unity 路径为 `D:/Editor/6000.3.18f1/Editor/Unity.exe`。回放使用证据目录内的隔离装备存档，不写正常仓库；音频捕获只在专项回放启用。

Git 分支为 `codex/equipment-loop-v1`。机体 / 头部 / 刀的 Blender 主工程未编辑，旧构建和原有未提交改动均保留，未自动提交或推送。修改前相关源码和 V6 动作资产副本位于 `AuditEvidence/power-combo-v7/baseline/`。当前完整工作区状态见 `AuditEvidence/power-combo-v7/git-status.txt`，其中也包含本轮之前已有的改动。
