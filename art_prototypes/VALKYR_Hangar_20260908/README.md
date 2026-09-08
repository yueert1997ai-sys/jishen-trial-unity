# VALKYR 战前格纳库

打开 `VALKYR_HANGAR_MASTER.blend`。这是可独立编辑的 Blender 5.2.1 LTS 场景。2026-09-08 已导入当前 Unity 游戏，支持默认空手、M7/巨剑/M14 配装、近距离查看与出击，见 [游戏接入说明](../../docs/HANGAR_LOADOUT_20260908.md)。

2026-09-08 版本纠正：初版误用了 `RAIKEN_MkII_20260906/VALKYR_RAIKEN_GAME.blend`，现已替换为第三版造型对应的 `JishenTrial_Hero_Assembly_V3/body_mass_refinement_20260906/iteration_v3/game_update/VALKYR_V3_GAME.blend`。模型保留 `VALKYR_Model_Revision_V3_c7297dd9` 标记，包含新版头部、厚装甲和腿部造型。旧误版存档与图像留在 `iterations/incorrect_early_model/`。

中央圆形整备台配有标尺、脚部定位区、检修背架和两台收拢的机械臂。外围有检修走台、楼梯、工具柜、备件架、诊断台与线缆。采用蓝灰金属、灰白结构和少量黄色警示，检修灯优先照清机甲装甲层次。

## 查看机甲

- `01 | Pre-sortie hero - UI room at right`：默认近距离全身展示，右侧预留未来战前界面的构图余量。
- `02 | Complete maintenance bay`：格纳库全景。
- `03 | Head and chest inspection`：头部、胸甲与肩炮特写。
- `04 | Backpack and hardpoints`：背包与背部机械结构。
- `05 | 360 degree inspection`：5 秒环绕相机，帧 1–120，24 fps。

在 Blender 中选中相机，按 Ctrl + 小键盘 0 设为当前相机；小键盘 0 进入相机视图。使用第 5 台相机时按空格播放，可环绕查看。也可直接在三维视窗中旋转、缩放观察机甲。游戏中直接用鼠标拖动旋转、滚轮缩放，或点击全身/近距离查看按钮。

## 文件

`renders/01_PRESORTIE_HERO.png` 已重新渲染为当前空手状态。其余全景、头胸特写、背部和环绕图保留配装接入前的视觉检查记录；当前游戏画面以 `docs/audit-evidence/2026-09-08/hangar/MAC_*.png` 为准。

主要设施按 Collection 分组，机甲在 `90 | VALKYR - current model copy` 中；`VALKYR_INSPECTION_ROOT` 管理机甲整体位置。使用第三版 `VALKYR_V3_GAME.blend` 的复制件，保留 29 个网格的全部几何。采用自然站立姿势，右前臂恢复基础角度，实体剑与光刃整组隐藏。Unity 中从机体预制体移除剑，玩家选择后单独装备。原生参考图双手持刀动作没有被改写。

机甲和武器源文件、月球基地场景保持原样。Unity 已接入镜头、界面、材质和实际攻击逻辑；Mac 独立试玩包已运行，具体测试范围与版本差异见游戏接入说明。

`build_hangar.py` 为搭建脚本，`refine_hangar.py` 为镜头和诊断屏调整，`clear_inspection_views.py` 调整机械臂位置并校准环绕镜头，`replace_with_v3.py` 完成第三版替换和来源记录，`geometry_helpers.py` 为本地几何工具，`render_hangar.py` 为渲染脚本。`manifest.json` 记录第三版来源、版本标记和网格指纹，`verification.json` 记录保存后回读、纹理与模型身份检查。
