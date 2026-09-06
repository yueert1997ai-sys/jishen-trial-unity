# 当前第三版游戏副本

用户于 2026-09-06 明确要求更新并装到游戏，因此从当前原生主工程冻结游戏资源。

`SOURCE_CURRENT_MASTER.blend` 保留来源，`VALKYR_V3_GAME.blend` 为游戏专用副本，`exports/` 为 FBX/GLB，`export_manifest.json` 记录来源哈希与部件统计。来源主工程未被修改。

游戏副本恢复供动作驱动的站立关节基准，并保留参考图的准确双手握持、刀面与准备姿势标记。网格按原有关节合并、倒角采用一步，不做几何塌缩。游戏里的真实站姿与动作由现有 `ValkyrMotionDriver` 驱动。

最新实际游戏位于原项目 `handoff/GameplayLoop_V1/Builds/ValkyrV3/MECH_TRIAL_Valkyr.exe`；该副本 `docs/VALKYR_V3_MODEL_UPDATE.md` 与 `AuditEvidence/v3-model-update/` 包含接入和实机检查记录。原有 `JishenTrial_GAME_CANDIDATE.blend` 及旧导出保留为历史版本。
