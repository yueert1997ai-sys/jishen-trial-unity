# TYPE E-01 ELITE / 液态生命金属侵蚀体

当前交付：R02 头部版整机已接入 PC Demo，作为正式 Boss。用户已授权“先录入吧，装到游戏里做 boss”。全部工程与输出保存在 D 盘。

## 游戏版本

- `game_ready/TYPE_E01_ELITE_GAME_R01.blend`：独立的游戏骨架工程；86 根骨骼，六条触须链、六条爪刃链与胸核开合。
- `game_ready/export_manifest.json`：源文件指纹、骨骼、材质与两档网格数据。
- 游戏交接：项目根目录 `docs/E01_ELITE_GAME_HANDOFF.md`。
- 实际游戏证据：项目根目录 `AuditEvidence/e01-elite/player`。
- 正式 Boss 预制体：`Assets/Prefabs/Enemies/Boss_HeavyMech.prefab`。主场景的原 Boss 引用已沿用该预制体，正常流程无需额外导入。

下列 `stage_01` 与 `stage_02_head` 文件作为可编辑雕刻源保留，历史报告中的“未绑定、未接入”描述对应当时的版本。

## 最新版本：头部 R02

- `stage_02_head/TYPE_E01_ELITE_HEAD_R02.blend`：当前可编辑整机。已重做头部的残留装甲、侵蚀结构、眼窝和下颌。
- `stage_02_head/HEAD_R02_COMPARISON.jpg`：上一版与本次修改，同角度、同灯光的实际模型对比。
- `stage_02_head/HEAD_R02_REVIEW.jpg`：头部正面、侵蚀侧、侧面及装回整机的预览。
- `stage_02_head/saved_head_check.json`：重新打开本次交付文件的检查。

用户反馈为“头部不够自然、太白、没有残破感、不够邪恶、有点呆”。头部 R02 将规则侧鳍改为跨方向交织的金属，白甲收成有焦黑断缘的碎片，缩小独眼并加深眼窝，降低眉部和打碎下颌。身体、巨爪与触须保持上一版的几何；该源文件继续保留，游戏绑定在独立工程中制作。

## 上一版整机与历史参考

- `stage_01/TYPE_E01_ELITE_STAGE01.blend`：可编辑整机，含分组的原机装甲、机械结构、半毁头部、核心、异化右臂、背部触须、腿部侵蚀和审核相机。用户参考图已内嵌。
- `stage_01/ELITE_MODEL_REVIEW.jpg`：整机与重点局部。
- `stage_01/ELITE_FOUR_VIEWS.jpg`：同一真实模型的正、左、背、右四视图。
- `stage_01/ELITE_DETAILS.jpg`：头、胸核、右臂、背部近景。
- `stage_01/renders/GAME_READ.png`：建模场景中的俯视辨识度预览，尚不是 Unity 实战截图。
- `DESIGN_DECISIONS.md`：用户确认项与后续动作、接入约定。

## 本轮重点

保留 E-01 的独眼、肩甲、左臂、机械关节与背包轮廓；破坏头部左侧，异化右臂；胸腹与背部生长交织的实体黑色金属和窄红脉；背部 6 条主触须有独立根部。头顶相对原小兵放大 1.5 倍，触须另算。

这些 stage 文件是建模用源文件。触须、巨爪和核心按可变形区域分组，骨骼与减面版本另存于 `game_ready`。Blender 预览与 Windows 构建截图分别标记，不混用验收证据。

## 原资产保护

基底来自 `Type_E01_20260906/stage_04_TYPE01/TYPE_E01_TYPE01_RIFLE_MASTER.blend` 中已确认的小兵机体，不携带步枪。制作在独立工程中进行，原小兵整机与步枪文件未被修改。

`build_report.json` 记录来源、部件与尺度；`saved_model_check.json` 记录重新打开交付文件的检查结果。预览图从重新打开的工程直接渲染。

## 下一审核节点

直接运行 PC 构建中的 `PLAY_BOSS.cmd` 审核实战表现。当前沿用旧 Boss 的四种攻击规则，姿态跟随实际战斗状态；新的巨爪近战连招与独立核心弱点碰撞仍属于后续玩法工作。
