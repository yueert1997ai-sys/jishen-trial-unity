# VALKYR TYPE-01 · 身体完成与侧面体积修正

继续使用当前整机工程，已完成胸肩、腰胯、手臂与机械手、腿脚、背包、肩炮和持剑。用户指出侧面太薄后，补足胸腔、骨盆和四肢的前后体积，延长并填实前掌，调整膝踝与手腕位置，清理胸部叠甲和侧壳穿插。

已认可的 141 个头部网格及材质槽保持一致。装机时仅围绕原颈部基准统一放大 1.35 倍；侧面体积修正没有再改头部。

## 当前文件

- `VALKYR_ASSEMBLED_MASTER.blend`：高细节可编辑整机；已同步至上级 `JishenTrial_ASSEMBLED_MASTER.blend`。
- `VALKYR_GAME_DEMO.blend`：保留刚性关节层级的 Demo 资产；已同步至上级 `JishenTrial_GAME_CANDIDATE.blend`。
- `exports/VALKYR_GAME_DEMO.fbx`、`exports/VALKYR_GAME_DEMO.glb`：当前模型的导出文件。
- `textures/`：装甲 4K、机械及武器 2K，包含基础色、粗糙度、金属度、切线法线；另附 Unity Standard 使用的金属度/光滑度合并图。
- `SIDE_DEPTH_COMPARISON.jpg`：设计图、同侧同方向的修改前渲染、修改后渲染。
- `VALKYR_FULL_BODY_REVIEW.jpg`、`VALKYR_DETAIL_REVIEW.jpg`、`VALKYR_WEAPON_REVIEW.jpg`：整机四视图、前 3/4、8 个局部以及光刃开关对照。
- `renders/`：原始渲染，含隐藏背包和武器后的身体侧面、灰模、实际主文件重新打开后的渲染、实际 FBX 重新导入后的渲染。

## 实际数据与检查

| 项目 | 当前结果 |
| --- | --- |
| 主文件可见网格 | 2,021 |
| 主文件控制网格多边形 | 50,659 |
| 主文件修改器求值后三角面 | 505,366 |
| Demo 网格 | 108 |
| Demo 三角面 / 三角多边形 | 137,706 |
| Demo 共用材质 | 5：Armor、Mechanics、Weapon、Sensor、Beam |
| 主文件与 Demo UV | 已有 UV，坐标在 0–1 内；Demo 各贴图族独立打包 |
| 拓扑检查 | 主文件求值网格、Demo 和实际重导入 FBX 均未检出非流形边或退化面 |
| 头部检查 | 141 个网格与来源一致；0°、±20° 转头未与非颈部配合件相交 |
| 光刃开关 | Blender 中 `AntiShip_Blade_Display_Root` → `Beam_On`；关闭后实体刀身仍在 |
| FBX 重导入尺寸最大误差 | 0.000000298 m |
| GLB 重导入尺寸最大误差 | 0.000000060 m |

模型继续使用当前工程的约 3.50 m 生产尺度，脚底为 0。Blender 为 Z 向上、-Y 朝前。设计图中的 18.4 m / 62.1 t 已记录为设计元数据。

Demo 外装甲、头部、光学件及细长饰条保留外形，主要压减机械件内部细分；未在主文件上做减面。贴图由实际材质烘焙，法线来自当前 LOD 表面和材质微表面，不代表完成了高模到低模的全部细节转移。

## 仍需继续的游戏接入工作

当前是可导入、带刚性装配层级的 Demo 资产。还需适配本轮新身体的战斗姿态、动作时关节穿插、武器轨迹和实机表现；尚未对这版模型执行完整骨架蒙皮或战斗动画验收。概念图与三维模型的细小曲面和材质表现仍有差异。

## 来源与 Git

源整机保存在 `../ACCEPTED_HEAD_ASSEMBLY_SOURCE.blend`，上一版 Demo 保存在 `../BEFORE_BODY_GAME_CANDIDATE.blend`。旧头部交付目录及历史导出均保留。

本轮写入限定于 `art_prototypes/JishenTrial_Hero_Assembly_V3/` 内的模型、导出、图像和说明。当前分支 `codex/industrial-dash-polish`，HEAD `3b1f0546ae231ee6a7174c8bff76cb28e79b8ddd`。资产目录仍未提交；工作区原有 Unity、角色控制和文档改动保持原状，详见 `git_status_at_delivery.txt`。主工程游戏接入文件由其他工作留下，本次未替换。

验证证据：`native_audit.json`、`native_delivery_manifest.json`、`demo_geometry_audit.json`、`demo_material_manifest.json`、`final_reimport_verification.json`。
