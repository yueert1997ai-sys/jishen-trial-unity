# MARE-07 废弃月球基地 · 单战区

打开 `MARE07_LUNAR_BASE_MASTER.blend`。当前为 Blender 5.2.1 LTS 场景，尚未接入 Unity。

沿用 VALKYR 机甲的蓝灰装甲、灰白结构、少量黄色警示和青色灯光。中央维修坪留作战斗空间，大型设施布置在外围。

主要内容：破损维修机库、气闸附属舱、六轮月球车（缺失前轮、碎裂风挡、车顶货架）、脱落车轮与维修工具、断裂通信天线、破碎太阳能板、储罐、货运龙门架、倒塌管线桥、低掩体、货箱、车辙、撞击坑与月尘堆积。

## 看图

- `renders/01_ESTABLISHING.png`：整体展示。
- `renders/02_GAMEPLAY_60DEG.png`：60° 游戏俯视角布局检查。
- `renders/03_ROVER_DETAIL.png`：月球车与损坏细节。
- `renders/04_HANGAR_DETAIL.png`：机库、破损屋顶与气闸。

Blender 中也保存了上述四台相机。按小键盘 0 进入/退出当前相机视图；模型按设施分 Collection，主要物件保留独立部件和材质，可继续编辑。

`90 | Existing VALKYR - removable scale reference` 是从现有机甲文件追加的复制件，用于比例与风格检查，可整组隐藏。机甲源文件未在本次制作中编辑。环境使用本地原创几何与程序材质，无新增外部素材下载。

## 制作与范围

首次搭建通过正在运行的 Blender 界面控制台执行 `build_lunar_base.py`；`polish_lunar_base.py` 增加屋顶实质破口、建筑装甲和废弃细节；`finalize_scene.py` 完成地面材质与默认展示设置。`render_scene.py` 从保存文件渲染四个角度。

`iterations/` 保留第一版场景和预览。`scene_manifest.json` 为对象清单，`verification.json` 为重新打开文件后的检查结果。

本轮交付是可编辑的 Blender 场景。Unity 的材质转换、碰撞、导航、LOD 与实机性能检查留在后续接入阶段；俯视构图检查不等于已经验证游戏可玩性。
