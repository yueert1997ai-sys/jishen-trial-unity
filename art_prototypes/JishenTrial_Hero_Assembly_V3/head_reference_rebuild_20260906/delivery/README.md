# VALKYR 参考头部 · 2026-09-06

本次按用户提供的头部多视图制作了新的可编辑头部，并装入现有整机。原头部更换前的完整工程保存在上一级 `BODY_SOURCE_BEFORE_HEAD.blend`。

## 打开文件

- `JishenTrial_WITH_REFERENCE_HEAD.blend`：装机后的整机副本，与上两级的当前 `JishenTrial_ASSEMBLED_MASTER.blend` 内容一致。打开后相机对准头部与肩胸连接位置。
- `VALKYR_HEAD_MASTER.blend`：独立可编辑头部，保留 141 个分件、边缘修改器、材质、UV、参考图和检查相机。
- `VALKYR_HEAD_Reference.fbx`、`VALKYR_HEAD_Reference.glb`：合为一个网格的交换文件，33,272 三角面、10 个使用中的材质。保留身体装配坐标，头部轴点为 `(0, 0, 3.003)` 米。
- `VALKYR_HEAD_EXCHANGE.blend`：与交换文件对应的网格副本。

## 外观检查

共完成 8 轮实际 Blender 建模与渲染迭代。最后一轮的正面、左右侧面、背面、顶视、四分之三、仰视、头身近景和全身视图保存在 `renders/`。

`FRONT_ALIGNED_COMPARISON.jpg` 按相同坐标比例对齐参考图与实模，并标出头顶、额头传感器、眼线和下巴高度。`REFERENCE_COMPARISON.jpg` 提供正、侧、背三组对照。

本次制作覆盖深蓝分层头甲、灰色眉骨与分叉面罩、独立蓝色下巴、双蓝色眼部光学件、竖向额头传感器、银色长天线与金色根部、机械耳座、后脑检修槽和机械颈部。

## 文件与装配检查

- 身体和装配对象共 1,655 个，与更换头部前的工程逐项比较，几何、位置与材质引用一致。
- 新头部 141 个网格分件、33,272 个求值三角面，未发现非流形边或退化面。
- 交换文件已展开 UV，UV 坐标位于 0–1 范围。
- 检查了静止、左右转头 30°、抬头和低头 12°，未发现头部与身体表面的穿插。颈部原有安装座和对接环作为预期接合面排除在碰撞判定之外。
- 参考图已打包进两个主 `.blend` 文件。

具体检查结果见 `technical_audit.json`、`fit_audit.json`、`delivery_manifest.json` 和保存后重新打开生成的 `saved_file_verification.json`。

本次交付针对 Blender 头部与整机装配。工程里的历史 Unity Demo 和旧整机游戏导出包仍对应上一版头部。
