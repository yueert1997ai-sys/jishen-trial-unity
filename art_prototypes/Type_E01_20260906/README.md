# TYPE E-01 普通小兵

小兵依据用户提供的 `D:\project-mecha-design\Type-01 设计图.png` 制作。用户已确认第三阶段造型基本可以，并要求装入另一任务完成、专门供小兵使用的 TYPE-01 激光步枪。

## 当前交付：TYPE-01 激光步枪整合版

- `stage_04_TYPE01/TYPE_E01_TYPE01_RIFLE_MASTER.blend`：当前可编辑整机。
- `stage_04_TYPE01/TYPE01_INTEGRATION_REVIEW.jpg`：垂持站姿与双手持枪预览。
- `stage_04_TYPE01/TYPE01_INTEGRATION_FOUR_VIEWS.jpg`：更新后的正、左、背、右四视图。
- `stage_04_TYPE01/renders/READY_HANDS.png`：步枪与双手握持近景。
- `stage_04_TYPE01/renders/READY_SIDE.png`：持枪侧面预览。
- `stage_04_TYPE01/integration_report.json`：武器来源、原文件摘要、装配比例与握持位置。
- `stage_04_TYPE01/saved_integration_check.json`：实际保存工程重新打开后的核对记录。

### 本次整合

保留用户确认的机体造型，移除本工程原先的步枪及其检查副本，装入 `D:\project-mecha-design\MECH ROUGE\art_prototypes\TYPE01_LaserRifle_20260906\TYPE01_LASER_RIFLE_MASTER.blend` 的完整可编辑武器。

原枪的 1,148 个网格分件、七个组件、可编辑倒角、材质、瞄具、能量弹匣与发光细节均保留。武器按 1.85 倍等比适配本机体，总长约 1.65 米；双手对齐原枪提供的主握柄与前护木支撑挂点。仅调整手臂和肩部的姿态，不改变已确认的机体网格造型。

原始小兵第三阶段工程与独立步枪主工程保持不变。两份原始参考图均打包在整合工程中。

### 工程内场景

- `01 NEUTRAL - editable assembly`：右手垂持站姿。
- `02 TYPE01 READY - two hand pose`：双手持枪姿态。

在 Blender 顶部场景选择器切换。两种姿态共享网格和材质，零件根节点的姿态独立。武器的枪托、能量弹匣、握把和其他组件可继续单独编辑。

当前交付为可编辑建模整合与姿态预览，没有执行游戏接入、骨骼动画或面向大量同屏小兵的 LOD 优化。

## 历史工程

- `stage_03/TYPE_E01_STAGE03.blend`：用户确认的机体造型基础；保留原有步枪和两种姿态。
- `stage_03/STAGE03_HEAD_COMPARISON.jpg`：头部侧面修正记录。
- `stage_02/TYPE_E01_STAGE02.blend`：上一轮机体与装备细化。
- `stage_01/TYPE_E01_STAGE01.blend`：最初体块与头部探索。

## 制作与存储

所有工程、预览、渲染和交付文件均保存在 D 盘本目录。

- `integrate_type01_rifle.py`：读取已确认的小兵工程和完成版步枪，生成整合工程；可用 `--preview` 输出主要预览。
- `verify_type01_integration.py`：重新打开整合工程，核对分件、挂点与来源，生成四视图和侧面预览。
- `prepare_type01_integration_review.py`：将真实渲染排版为审核图，不修饰模型轮廓。
