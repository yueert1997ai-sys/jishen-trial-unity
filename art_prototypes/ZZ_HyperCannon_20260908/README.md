# TYPE-08 等身长重型加农炮

按用户提供的武器照片和三视图制作；尺寸以最后一张 ZZ 机体持炮图及“与机体等长”的明确要求为准。

炮长 **3.491269 m**，当前 VALKYR V3 身高 **3.495401 m**，比例 **0.9988 : 1**。右手持用，保留长炮管、深色大后壳、白色中段装甲、顶部瞄准模块、警示色条和后部双关节结构。握把后方增加适合当前机甲手掌的细握持结构。

## 打开和使用

- **VALKYR_TYPE08_MASTER.blend**：炮已装到 VALKYR V3 的右手；`TYPE08_RIGHT_HAND_MOUNT` 作为 `Hand.R` 的子级跟随右手。打开后是整机检查视角，数字键盘 0 切换相机视图。
- **HC09_CANNON_MASTER.blend**：炮的独立可编辑源文件，共 256 个制作零件，按外壳、炮管、装甲、握把、瞄准模块和细节分组。选择 `HC09_CANNON_ROOT` 可整体移动、缩放。
- **exports/TYPE08_CANNON.fbx**：给 Unity 等软件使用，含三个 LOD 和握把、炮口、辅助握点等标记。
- **exports/TYPE08_CANNON.glb**：含 LOD0 和标记，适合支持 glTF 的工具查看、导入。
- **renders/**：四张武器视图、整机持炮、握持近景、另一侧和整机侧面图，均来自实际 Blender 模型渲染。

## 当前 Unity 项目

已接入主工程和实际 Windows V9 试玩工程 `D:\project-mecha-design\MECH ROUGE\handoff\GameplayLoop_V1`。**TYPE-08 已设为起始默认装备**，启动即在机库右手持炮，直接点击出击即可使用。返回机库或重新开始也会恢复这门起始武器；仍可手动卸下或切换其他武器。

双击本目录的 **PLAY_TYPE08_V9.cmd** 启动新版。主项目和试玩工程原有的 `PLAY_V9.cmd` 以及 `机神试炼_V9_新机库.lnk` 也已指向新版。构建目录为试玩工程的 `Builds/ValkyrHangarV9_Type08Starter`。左键发射粉色粒子光束，E 使用肩炮技能，WASD 移动，空格冲刺或推进。

在 Unity 中打开 `Assets/Scenes/Demo_Main.unity` 并进入 Play，同样默认装备 TYPE-08。两份工程各自保留原有主角和战斗逻辑，V9 的收藏仓库、斩舰刀三连斩、月面和液态 Boss 继续可用。

独立预制体：`Assets/Resources/Hangar/TYPE08.prefab`。武器原点是右手握点，Unity 前向为 +Z，炮口对象名为 `Muzzle`。游戏以独立右手姿势持炮，左手沿用机体动作。

当前游戏原型参数：伤害 120，射击间隔 1.1 秒，最大射程 42，穿透 2（最多 3 个直接命中目标），粒子冲击范围 1.8。聚光后沿射线即时判定命中，墙体会阻挡光束。上述数值仅为游戏参数。武器已有三个显示精度级别；Unity 导入后三角面数为 73,628 / 35,226 / 13,067，9 个材质。

2026-09-08 粉色粒子光束效果：炮口先聚光约 0.12 秒，随后放出约 0.25 秒的白粉色亮芯及浓粉色光束，外围光晕继续消散约 0.22 秒；包含炮口收束环、粉色粒子、命中扩散环及对机体的粉色照明。余辉不会每帧重复造成伤害，暂停时整套效果冻结。

效果实现位于 `Assets/Scripts/Combat/MinovskyBeam.cs`，加法发光材质位于 `Assets/Resources/VFX/MinovskyGlow.shader`。此效果已接入当前 Unity 游戏工程。

重新导入当前炮模型可用 Unity 菜单 `MECH ROUGE > TYPE-08 > Import Heavy Cannon`。原有机库重建逻辑会保留这门独立添加的武器。

## 验证

`unity_evidence/runtime-report.json` 是 Unity 6000.3.18f1 的实际 Play 模式验证结果，包含原三把武器选择与握点、加农炮选择、卸下、出击、握持、炮口出弹、冷却、命中伤害及返回机库复位。`unity_evidence/01_HANGAR_TYPE08.png` 和 `02_COMBAT_TYPE08.png` 是游戏内截图。

`unity_evidence/pink_beam/runtime-report.json` 验证粉色光束的聚光、材质、炮口方向、三目标贯穿、单次伤害、暂停冻结、余辉清理和墙体遮挡。该目录的 `01_CHARGE.png`、`02_PINK_BEAM.png`、`03_AFTERGLOW.png` 是 Unity 运行时的近景测试截图；画面中的方块为验证用靶标。

`work/fitting_report.json` 记录机体版本、身高、炮长、右手握点误差和原机体文件哈希。源机体为 `VALKYR_Model_Revision_V3_c7297dd9`；源文件未被改写。`exports/export_manifest.json` 记录导出文件哈希及 LOD 数据。

`unity_evidence/starter_v9/quick-check.txt` 是新版 Windows 独立游戏的实际回放结果：全新启动默认持炮、无需选武器直接出击、返回机库恢复默认炮、粉色光束聚光与真实命中、炮口对齐、冷却、余辉清理、卸下限制、收藏仓库、M7/M14 握持与命中、实体刀三连斩、液态 Boss、月面白兵导航以及持炮推进全部通过，运行错误为 0。该目录的截图直接来自这个 Windows 构建。构建版本 `hangar-lunar-v9-20260908.140123`，构建 0 错误、0 警告。测试使用独立仓库存档，未改动正常玩家存档；这是短流程检查，不代表完整通关。

此次交付包含 Blender 工程、模型导出、两份 Unity 工程接入和新版独立 Windows 游戏。炮体是按参考制作的可编辑硬表面资产；运动部件还未制作单独的开合动画。V9 持炮沿用已有后坐和推进动作。

实际文件都保存在 D 盘项目内；Codex outputs 中的 TYPE08_Cannon 入口是指向本目录的目录联接。
