# 战前格纳库与主武器配装

当前仓库的 `Assets/Scenes/Demo_Main.unity` 已接入配装流程。入口为 `Builds/Mac/MECH_TRIAL_Hangar_20260908.app`，也可用 Unity 打开该场景进入 Play Mode。无需运行旧的场景重建菜单。

## 玩家实际体验

- 进入或返回格纳库时默认空手，使用第三版厚装甲和新版头部，双臂自然垂下。
- M7、RAIKEN 巨剑、M14 三选一。点击后才生成对应武器，旧武器立即退出显示；可以卸下重选。
- 步枪在机库低位双手持握，巨剑斜向下持握。握点由手臂求解保持连接，出击后继续使用所选武器。
- 未选主武器时出击按钮禁用，游戏逻辑也拒绝空手开始；战斗中禁止切换配装。
- M7 连续射击：基础伤害 18，间隔 0.18 秒；M14 基础伤害 46，间隔 0.48 秒，自带一次穿透。已有局内强化继续参与计算。
- 巨剑使用现有真实范围近战，基础伤害 42；左键/射击输入与独立挥刀键均可触发。装备枪械时不能触发无形巨剑攻击，装备巨剑时不能发射无形步枪子弹。原有肩部导弹技能保留。
- 提供全身/头胸近景切换、鼠标拖动环绕和滚轮缩放，机库阶段停止移动与战斗输入。难度选择、设置和中英文界面保留。

## 代码与资产

`PlayerLoadout` 管理选择和出击条件；`HangarArmory` 保存三种武器配置；`LoadoutVisual` 管理武器实例、机库站姿和双手握点；`HangarPresentation` 管理整备环境与镜头。现有 `PlayerMechLoader`、`GameManager`、输入、射击和近战逻辑已接入。

资源位于 `Assets/Art/HangarLoadout/` 与 `Assets/Resources/Hangar/`，Unity `.meta` 已保留。`tools/jishen/export_hangar_assets.py` 从 Blender 源文件导出；`HangarIntegration.Build` 重新生成 Unity 材质与预制体；`HangarIntegration.BuildMac` 构建 Mac 试玩。后两项也提供 `MECH ROUGE/Hangar` 菜单。

机体来自 `JishenTrial_Hero_Assembly_V3/body_mass_refinement_20260906/iteration_v3/game_update/VALKYR_V3_GAME.blend`，版本标记为 `VALKYR_Model_Revision_V3_c7297dd9`。M7 使用最终 stage04，M14 使用 `BOSS_M14_EBR_MASTER.blend`，RAIKEN 使用同版机体配套实体剑。完整来源、路径和哈希见 `Assets/Art/HangarLoadout/sources.json`。未修改这些机体/武器源文件。

Blender 格纳库主文件也已改为默认空手。旧抬臂持剑版本保留在 `art_prototypes/VALKYR_Hangar_20260908/iterations/before_unarmed_default.blend`。

## 验证

证据在 `docs/audit-evidence/2026-09-08/hangar/`。

- Play Mode 通过：默认空手、禁止空手出击、三个 UI 选择与真实模型替换、卸下、战斗锁定、返回重置、两种枪械实际子弹参数、巨剑真实命中伤害和武器类型攻击限制。
- 三种武器双手握点误差均小于 0.000001 米，检查阈值为 0.06 米。此项检查接触位置，不代表手指逐关节精修或完整动作质量验收。
- Mac 独立 Player 构建成功，报告 0 错误、0 警告。实际原生窗口已检查中文设置、装备 M7、近景、拖动旋转、出击及返回机库。`MAC_*.png` 是原生窗口截图，编号 PNG 是 Editor 离屏检查图。
- 当前电脑只有 Unity `6000.3.17f1`。导入、Play Mode 和 Mac 构建均在独立验证副本完成；主仓库保持要求的 `6000.3.18f1`，未降级项目设置。
- Editor 启动出现既有 `UnityEditor.Search.SearchDatabase` 索引异常，记录在 `runtime-report.json` 的 `editorErrors`；游戏运行检查没有异常，构建也没有错误。该记录不算作手机真机、Windows 新包或全流程通关验收。

这些改动针对当前已下载的仓库。README 中历史引用的 `handoff/GameplayLoop_V1` 副本不在当前克隆中，其 Windows `.exe` 没有被本轮改写。换机时拉取 `codex/industrial-dash-polish` 分支，使用 Unity 6000.3.18f1 打开 `Assets/Scenes/Demo_Main.unity`；Git 同步源码与资源，不包含本地 Mac `.app`、Unity 缓存和构建日志。
