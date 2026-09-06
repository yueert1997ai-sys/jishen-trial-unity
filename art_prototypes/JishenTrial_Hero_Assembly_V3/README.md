# 机神试炼 · 整机主工程

**2026-09-06 游戏接入追加：用户已授权“更新、装到游戏里”。已从当前第三版主工程生成独立游戏资源并接入 `handoff/GameplayLoop_V1`，最新试玩为 `Builds/ValkyrV3/MECH_TRIAL_Valkyr.exe`。导出副本在 `body_mass_refinement_20260906/iteration_v3/game_update/`，原生主工程保持不变。下文暂停导出的说明对应此前建模阶段。**

**2026-09-06 当前为第三版造型迭代：大腿改为长向蓝色折面主甲、较窄的白色侧甲和更深的分层侧壳；胸部、前臂、小腿再小幅加厚。保留双膝包甲、已认可头部与新版 RAIKEN Mk-II 斩舰刀。最新姿势依据用户图片改为双手在身前握刀、刀尖朝左上方、双腿一伸一收的跃击架势，四视图同步更新。按用户要求继续暂停导出，只保存 Blender 主工程。**

- `JishenTrial_ASSEMBLED_MASTER.blend`：当前可编辑整机，保留已认可头部。
- `body_mass_refinement_20260906/iteration_v3/VALKYR_ITERATION_V3_MASTER.blend`：第三版原生工程存档；同目录包含 `THIGH_BEFORE_AFTER.png`、局部及整机 `renders/`、`four_views/FOUR_VIEWS.png` 和检查记录。
- `body_mass_refinement_20260906/iteration_v3/reference_action_pose/`：最新参考图动作、握持近景、四视图和保存核对记录；主工程与第三版原生存档已同步此姿态。`SOURCE_BEFORE_REFERENCE_POSE.blend` 保留调整前原生文件。
- `body_mass_refinement_20260906/iteration_v3/weapon_pose/`：此前单手向前外侧斜持的历史姿态与图片。
- `body_mass_refinement_20260906/delivery/VALKYR_MASS_RAIKEN_MASTER.blend`：第三版之前，身体加厚、膝部包甲与新版刀具合并后的历史存档。
- `JishenTrial_GAME_CANDIDATE.blend`：此前身体与新版 RAIKEN 斩舰刀的 Demo 资产，尚未包含本轮身体和膝部修改。
- 新版斩舰刀及此前完整整机 FBX/GLB、实模渲染、开关说明位于 `../RAIKEN_MkII_20260906/`。这些导出尚未包含本轮身体修改；本轮没有重新导出。新刀按机体等长比例挂接右手，右前臂调整为展示姿态。
- 前一版身体贴图、渲染及检查记录保留在 `body_reference_rebuild_20260906/delivery/`。
- 单独头部的此前交付仍在 `head_reference_rebuild_20260906/delivery/`。

下文属于此前 V3 静态接装的历史记录。原 `exports/`、`refinement/` 和 Unity Demo 文件均不代表当前原生建模版本。

## 此前 V3 静态接装记录

本轮完成：将 `JishenTrial_HEAD_V2/HEAD_V2.blend` 的头部装到已有 V2 机身，补齐军蓝、深灰、少量灰白和蓝色双目，修正颈部连接，导出并在本机 Unity 中验证静态展示资产。

用户追加指出头太小后，头部所有外形零件统一放大 **1.50 倍**，整体下移 **20 mm**。头盔宽从 **0.19498 m** 改为 **0.29247 m**；机身保持原尺寸。头顶由 3.25 m 变为约 **3.351 m**（不含天线）。这是一版比例调整，不代表用户已批准侧面造型。

## 文件

- `JishenTrial_ASSEMBLED_MASTER.blend`：可编辑主文件，原机身修改器保留，22 个头部原始分件 + 1 个颈部承力座，材质和活动部件独立。
- `JishenTrial_GAME_CANDIDATE.blend`：优化后的静态导入候选，**110 个网格，129,824 三角面，5 个主要材质**，检查为 0 非流形边。
- `exports/JishenTrial_Assembly_V3.fbx`、`.glb`：同一最终比例的可导入模型。
- `exports/JishenTrial_Assembly_V3.unitypackage`：本机 Unity 实际导出的静态 prefab、材质、贴图和独立检查场景。
- `renders/HEAD_SIZE_BEFORE_AFTER.jpg`：同机身、同正面相机尺度的头部大小前后对照。
- `renders/ASSEMBLY_REVIEW_SHEET.jpg`：正、侧、背、四分之三、头身近景和灰模检查。
- `renders/UNITY_*.png`：Unity 对实际 FBX 的渲染，独立展示环境，不是主游戏战斗截图。
- `FBX_REIMPORT_CHECK.blend`：实际重新导入 FBX 后保存的检查副本。
- `ASSEMBLY_RENDER_SNAPSHOT.blend`：用于稳定渲染的求值网格快照；编辑请用 MASTER。
- `iterations/small_head_before_feedback/`：用户指出头太小之前的版本备份。
- `HEAD_GUI_BEFORE_ASSEMBLY.blend`：打开整机前，保留的 Blender 窗口头部工作副本。

## 实际执行与验证

Blender：`C:/Program Files/Blender Foundation/Blender 5.2/blender.exe`，5.2.1 LTS。Cycles/OPTIX，RTX 4070 Ti。
Unity：`D:/Editor/6000.3.18f1/Editor/Unity.exe`，6000.3.18f1，Built-in / Standard，与主项目渲染管线一致。

已实际执行的主要脚本：

1. `assemble_colored.py`：读入已有机身，移除新副本中的旧头，追加并着色新头，调整头身比例，补颈座，保存主文件和渲染快照。
2. `render_assembly.py`：重新打开快照，实际渲染 10 个检查画面。包括正/左/背/右/四分之三、3 张头部近景、无发光无贴图灰模，以及光刃关闭图。
3. `build_game_candidate.py`：更新已有优化机身的头部，UV、真实材质通道烘焙、导出 FBX/GLB。原有机身纹理保留，新头部占装甲图集新增的一个 2K 区域。
4. `verify_export.py`：重新打开主文件与游戏候选，再实际导入 FBX，校验网格数、边界尺寸、头部坐标、5 个材质和独立光刃。
5. `prepare_unity_review.py`：只准备本目录下的独立 Unity 工程，转换 Metallic/Roughness 为 Standard 使用的 R 金属度 + A 光滑度。
6. `UnityImportReview/Assets/Editor/JishenAssemblyReview.cs`：Unity 实际导入、材质设置、prefab 保存、5 张渲染、尺寸和层级检查、导出 unitypackage。
7. `make_review_sheets.py`：只排版真实渲染，不生成或重绘模型图像。
8. `configure_blender_review.py`：通过本机 Blender Python Console 执行，设置彩色编辑视口及渲染预览并保存。

逐项运行方式（在本目录 PowerShell 中，Blender 脚本例子）：

```powershell
& 'C:/Program Files/Blender Foundation/Blender 5.2/blender.exe' --background --threads 4 --python-exit-code 1 --python 'assemble_colored.py'
```

`run_pipeline.ps1` 汇总相同的顺序；上述各命令已逐项执行。脚本重新运行会更新本 V3 目录中的生成物，不覆盖 V2/HEAD_V2 原文件。Unity 验证使用已有本机许可证；初次沙箱环境返回许可证错误 198，改用正常本机环境后成功运行。没有安装软件或更改许可证。

## 材质与结构

5 个共用材质：Armor、Mechanics、Weapon、Sensor、Beam。Armor 为 4096×2048（左为原机身 2K、右为新头 2K），Mechanics/Weapon 为 2048×2048。主文件保留独立分件与更细的材质类别；游戏候选按刚性关节和材质合并。

Blender Z 向上、-Y 朝前、机体右侧为 -X。FBX 实测导入 Unity 后 Y 向上、+Z 朝前、机体右肩炮位于 +X。脚底为 0；整机总边界包含背包、天线和陈列在旁的刀，不能把总边界高度当成头顶高度。

`AntiShip_Blade_Display_Root` 的 `Beam_On` 控制 Blender 光刃。游戏光刃为独立 `LOD0_AntiShipBlade_Beam`，可单独启停。刀保持旁置展示状态，未假装已经握在手里或完成战斗挂接。

## 如何接入当前游戏

包内全部内容位于 `Assets/Art/JishenTrial_Assembly_V3/`。将包导入主项目后，可先打开 `Assembly_Import_Review.unity`，或把 `JishenTrial_Assembly_V3_STATIC.prefab` 拖入独立展示场景。它是可显示的静态 prefab，**没有骨架、蒙皮或动画**。

当前项目 `PlayerMechLoader` 的 Editor 自动选择路径为 `Assets/UserContent/PlayerMech/PlayerMech.prefab`；本包不创建该文件。本轮也没有更新当前角色 prefab 的 `defaultMechPrefab`。

进入可战斗主角状态，还需：

1. 给主要刚性段制作/适配 Generic 骨架，完成装甲刚性权重和关节活动测试，适配 Idle/Run/Dash/Slash/Death 等当前控制器状态。
2. 当前 `RiggedMechAnimator` 按既有角色的骨骼引用驱动身体，并通过左前臂瞄准炮口；新设计为右肩炮，需要适配炮座转轴与炮口方向。不能只替换 FBX 文件名。
3. 将刀的实际握柄挂到右手，把刀尖/拖尾与当前近战视觉事件接好；保持 `PlayerMeleeController` 的伤害权威。当前并行游戏任务已新增近战事件，本轮只检查，不修改该代码。
4. 对准 `PlayerMechRoot/Hardpoints`，校验碰撞体、镜头、武器出生点，以及放大头部后的 3.351 m 头顶尺度。
5. 完成播放模式和战斗实测后，再决定是否替换当前主角。这一步本轮未执行。

## 检查、取舍与剩余问题

- 首次追加后坐标未求值，导致头落在原点：渲染中发现，修正父级变换更新顺序，并增加世界坐标高度断言。失败图仅保存在 logs。
- 新颈圈底到胸部承力面有约 39 mm 空隙：增加一个有实体厚度的短承力座。没有加螺丝或三级细节。
- 用户指出整机头偏小：统一放大头部 1.5 倍并下移 20 mm；保留之前的备份。所有最终 Blender/FBX/Unity 输出同步更新。
- 旧游戏机身两侧背包各有一个极小三角薄片：仅移除该薄片，最终候选非流形边为 0；没有跨装甲分件焊接。
- 头部侧面仍有过于前伸的眉额和偏重的后脑/下颌轮廓，与概念差距明显；用户尚未通过，不能因着色和放大而标记完成造型验收。
- 机身、肩炮、背包与刀沿用已有版本。本轮不以新增细节掩盖轮廓问题。没有完整绑定、战斗动画或活动时穿插验收。

## Git 范围

本轮新增和写入都在 `art_prototypes/JishenTrial_Hero_Assembly_V3/`，未提交。开始时分支为 `codex/industrial-dash-polish`，HEAD `5159dd646248c450cdc870a479c02961ca4bbad4`，已跟踪文件当时干净。

执行期间另一个任务修改并提交了主项目的输入/近战/UI 等文件；最终 HEAD 为 `3b1f0546ae231ee6a7174c8bff76cb28e79b8ddd`，已跟踪文件干净。这些不是本模型任务的改动，未回滚或覆盖。本目录仍为未跟踪的新资产，未提交。最终状态快照见 `logs/git_after.txt`；本任务的生成记录见 `logs/assembly.json`、`game_candidate.json`、`reopen_fbx_verification.json`、`unity_import_report.json`。
