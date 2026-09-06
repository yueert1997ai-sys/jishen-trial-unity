# 参考图双手持刀姿势

沿用第三版整机和现有 RAIKEN Mk-II，依据 `USER_POSE_REFERENCE.png` 调整为身前双手握刀、刀尖朝左上方、右腿伸展和左腿后收的静态跃击姿势。

- `VALKYR_V3_REFERENCE_ACTION.blend`：本次可编辑原生姿势，已同步到整机主工程和第三版原生存档。
- `SOURCE_BEFORE_REFERENCE_POSE.blend`：本次调整前的单手姿势备份。
- `renders/ASSEMBLED.png`、`renders/HAND_FIT.png`：最终整机与双手握柄实模渲染。
- `four_views/FOUR_VIEWS.png`：同一原生模型的正、背、左、右正交视图。
- `pose_audit.json`：3152 个网格的几何、UV、材质及四肢长度保持不变；姿势沿用原有关节层级。双手位于刀柄前后两段，腕部与前臂回转避开后护翼。
- `saved_master_check.json`：主工程保存后重开检查，模型与四视图来源核对，后护翼和前臂、胸甲的表面穿插检查通过。

本次仅调整静态姿势及检查相机，保留第三版造型、头部、刀具尺寸和材质。未导出 FBX、GLB、Demo 或更新游戏资产；没有进行动画制作或全动作范围验收。`preview/` 为调整过程的早期预览，最终效果以 `renders/` 和 `four_views/` 为准。
