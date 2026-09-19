# AX-01 HALBREAKER — Blender 肩扛炮

依据用户提供的两张 HALBREAKER 参考图，在可见 Blender 5.2.1 窗口中通过 MCP 分阶段建模。参考素材位于 references/，阶段存档位于 stages/。

## 当前最终要求

- **外露炮管 2.2 米**；整炮约 **4.1202 米**。以用户最后的 2.2 米要求为准。
- 肩部承重，**右手只抓前下方原有 U 形横握把**。
- 左手恢复原始姿势，不与炮建立握持约束；后续游戏接入需保留其移动动作。
- 已移除额外控制柄和左手稳定架。
- 已移除前下方的圆形辅助口，换成平面装甲及直槽。
- 炮管增加法兰、环向加工槽、真实切出的纵向槽、加强轨、径向紧固件及炮口内衬。

## 工程

- `AX01_HALBREAKER_MASTER.blend`：独立炮体展示，可编辑零件按集合组织。
- `VALKYR_AX01_SHOULDER_MASTER.blend`：现有 VALKYR V3 副本的单右手肩扛展示；当前 Blender 停留在此工程。
- `renders/01_CANNON_INDUSTRIAL.png`：最终独立炮体渲染。
- `renders/02_VALKYR_RIGHT_HAND_SHOULDER.png`：最终整机肩扛渲染。
- `renders/03_RIGHT_HAND_U_GRIP.png`、`04_SHOULDER_CONTACT.png`：接触部位检查图。

整体选择 `AX01_SHOULDER_CANNON_ROOT`。肩部承托节点为 `AX01_SHOULDER_MOUNT_FRAME`，父级为 `Thorax`，不是右手。右手握点为 `AX01_RIGHT_HAND_U_GRIP`，炮口为 `AX01_MUZZLE`。

## 检查与范围

七段主炮管两端中心同轴，测得倾角均为 0 度，记录于 `work/barrel_axis_audit.json`。实际胶质横握把几何中心与右手接触点误差小于 0.000001 米。肩垫按肩甲表面射线采样定位。记录见 `work/fitting_report.json`。

Blender 可编辑模型及肩扛姿势共 630 个炮体子对象。机甲来自既有 V3 工程的副本，源机甲文件未改写。阶段存档反映制作过程，应打开上述两个 MASTER 文件查看最新状态。

## V10 游戏接入（2026-09-08）

已从最终肩扛 MASTER 导出 `game_ready/HALBREAKER_AX01.fbx`、材质及来源校验清单，原 Blender 文件保持不变。已接入现有 `handoff/GameplayLoop_V1`，新增独立 AX-01 装备、肩部承重和右手 U 形握把跟随、蓝色贯穿激光，并简化机库 UI。

试玩入口为项目根目录 `PLAY_V10.cmd` 或 `机神试炼_最新试玩.lnk`，构建位于 `handoff/GameplayLoop_V1/Builds/ValkyrV10_HALBREAKER`。完整 Windows 压缩包位于 `deliveries/HALBREAKER_V10/`。

实际独立程序验证通过：四台靶机单发直接贯穿、掩体阻挡、冷却和余辉清理、奔跑和推进握点、左手自由动作、机库菜单及正常出击。详细结果在 `handoff/GameplayLoop_V1/docs/HALBREAKER_V10.md` 和 `AuditEvidence/halbreaker-v10/`。
