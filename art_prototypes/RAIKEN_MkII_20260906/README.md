# RAIKEN Mk-II 雷剑 · 斩舰刀重制

依据用户提供的独立武器设计图重建，并按追加反馈增加机械细节、加强尖锐轮廓。当前为第二轮细节及锐化版本。

- 非对称单刃刀身、延长的刺入尖端、连续实体金属切削斜面；四片护手使用逐渐收薄的尖翼结构。
- 分层蓝灰装甲、咬合接缝、刀脊承力桥、散热鳍片、嵌入式电容、液压回路、传动杆、锁扣、内六角紧固件和铭牌。
- 竖向光学核心；独立的青蓝光刃外层与白色细芯。光刃关闭后，实体刀身与刀根核心保持可见。

## 打开哪个文件

| 文件 | 用途 |
| --- | --- |
| `RAIKEN_MkII_MASTER.blend` | 独立高细节武器，可继续编辑分件与倒角 |
| `VALKYR_RAIKEN_MASTER.blend` | 最新机体握持新刀的完整可编辑整机 |
| `RAIKEN_MkII_GAME.blend` | 按材质合并后的独立导入资产 |
| `VALKYR_RAIKEN_GAME.blend` | 保留当前机体刚性关节层级的整机导入资产 |
| `exports/RAIKEN_MkII_GAME.fbx`、`.glb` | 独立武器导出 |
| `exports/VALKYR_RAIKEN_GAME.fbx`、`.glb` | 新刀与当前机体的整机导出 |
| `renders/` | 实模侧面光刃开/关、3/4、刀根、顶部、装机与握持近景、实际 FBX 重导入渲染 |

上级整机目录的 `JishenTrial_ASSEMBLED_MASTER.blend`、`JishenTrial_GAME_CANDIDATE.blend` 已更新为这一版。替换前的文件完整保存在 `backups/`，此前头部、身体交付目录保留。

## 比例、挂接与光刃

独立主文件使用设计标称 18.5 m 长度；生产版按当前约 3.50 m 机体与设计 18.4 m 机体比例缩放，刀长约 3.519 m。原点在握柄中心，独立资产的刀尖朝本地 -X，正面为 -Y，Z 向上。

整机中的刀挂接于 `V3B_Sword_Grip_Socket`。仅调整右前臂展示姿态 65°，使这一把接近机体等长的刀获得离地空间；当前刀尖约高于地面 0.218 m。机体原有的网格顶点及材质数据保持一致。

Blender 光刃开关：独立文件选择 `RAIKEN_MkII_Grip_Root`，整机选择 `AntiShip_Blade_Display_Root`，切换自定义属性 `Beam_On`。导入游戏后，对 `LOD0_AntiShipBlade_Beam` 整个子层级设置可见性；这是两个光刃网格的共同父级。`RAIKEN_BLADE_TIP` 和 `RAIKEN_GRIP_SOCKET` 保留为定位点。

## 快速验证

- 导入武器：12 个网格、11 个材质、69,724 个三角面。主文件保留独立细节，导入版仅减少细微倒角步数及文字曲线细分，并按材质合并。
- 导入版检查未发现非流形边或零面积面。
- 已实际重新打开武器文件，验证光刃开/关与实体刀保留。
- 已实际重新导入 FBX、GLB；FBX 边界最大差值约 `1.49e-8 m`，GLB 边界一致；刀尖定位点和光刃父级都保留。FBX 使用其导入材质直接渲染。
- 原机体的 1,858 个主文件网格、105 个导入版网格的顶点及材质数据校验一致。

本次交付更新模型与导出文件，现有 Unity 游戏构建仍是之前的版本。本轮展示姿态尚未做新的战斗动画接入。

`build_manifest.json`、`export_manifest.json`、`assembly_*_manifest.json`、`verification.json` 保存对应检查数据。重新生成按 `build_raiken.py` → `export_raiken.py` → `assemble_raiken.py` 顺序运行；装机使用本目录 `backups/` 中冻结的机体来源。
