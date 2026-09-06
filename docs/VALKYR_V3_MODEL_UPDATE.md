# 第三版机甲更新到游戏

后续动作已重做，最新试玩为 `Builds/ValkyrMotionV3/MECH_TRIAL_Valkyr.exe`，见 [动作重做 V3](VALKYR_MOTION_V3.md)。下文保留模型接入时的记录；当时握持点与功能检查通过，不代表握刀方向、发力与跑姿合适。

2026-09-06。当前 Blender 主工程中的第三版厚装甲、重做的大腿与膝部包甲、已认可头部和 RAIKEN Mk-II 已更新到本游戏副本。当前试玩为 `Builds/ValkyrV3/MECH_TRIAL_Valkyr.exe`，保留此前 `Builds/ValkyrCombat/`。

机库使用参考图校准的身前双手举刀姿势，游戏中保留现有重斩、奔跑、冲刺和持续推进飞行。地面站立由现有腿部动作负责；参考图的跃击展示没有被当作固定站姿。双手用模型内的准确握持位置与刀面方向约束，抬刀时将同一刀柄限制在两臂可达范围，修正左手脱柄。

## 本轮范围

- 来源：原项目 `art_prototypes/JishenTrial_Hero_Assembly_V3/JishenTrial_ASSEMBLED_MASTER.blend`，SHA256 `c7297dd9a4cfec2f9ef590237661beb3a1a43c291eb1f522f355feed32862f54`。原生主工程保持不变。
- 独立游戏副本：原项目 `body_mass_refinement_20260906/iteration_v3/game_update/`。保留全部可见部件，将倒角设为一步、按刚性关节合并，不做几何塌缩；Unity 导入后 29 个网格、375038 三角形、28 个材质。
- 当前冻结资源：`Assets/Art/ValkyrRaiken_V3/`；当前角色仍为 `Assets/Prefabs/Player/ValkyrRaikenVisual.prefab`，使用原有 15 个主要刚性关节。
- 只更新主角资源与持刀适配，未重新生成场景。旧机体精英、装备收藏、战斗流程、输入和伤害规则沿用当前游戏。

## 实机检查

最终 Windows 构建为 `Succeeded errors=0 warnings=0`。实际 Player 的 15 项动作短检查通过，运行错误为 0，保存 204 帧实际运行画面。检查包括双手挥刀、真实刀轨迹单次命中精英、奔跑、推进飞行及能量消耗、空中挥刀、松键落地和冲刺取消。

抬刀和挥刀的核心采样区间内，左手握持位置最大误差约 `0.00000137 m`，右手约 `0.00000133 m`。这是关节握持位置验证，不代表完整装甲运动范围或新连招验收。

最终证据：`AuditEvidence/v3-model-update/build-result.txt`、`motion/quick-check.txt`、`motion/01_ready.png`、`motion/frames/`、`final-update.json`。检查使用独立测试档；正常试玩沿用原有用户收藏位置。

## 继续使用

WASD 奔跑，鼠标瞄准；Q / 右键挥刀，空格点按冲刺、长按推进飞行，左键射击，E 导弹。机库点击“斩舰刀 · 首领试战”即可快速试战。

本副本执行 `tools/prepare_valkyr.py` 冻结当前 V3 游戏导出，`tools/run_valkyr.py update-hero` 只更新主角预制体，`build` 构建，`motion-check` 检查实际 Player。之前源文件和主角预制体备份保留在 `AuditEvidence/v3-model-update/before/`。
