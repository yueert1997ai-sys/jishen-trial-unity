# 冥隼等身深灰斩舰刀

2026-09-24：将原 VALKYR 的 RAIKEN 宽刃斩舰刀改为冥隼专用深灰版本，替换当前 Demo 的短刀。用户在实机预览后要求继续放大，最终尺寸以整把刀和机体等高为准。

- 原型：`VALKYR_V9.prefab` 的 `AntiShip_Blade_Display_Root`，沿用 `VALKYR_RAIKEN_GAME.fbx` 中原始网格与细节。
- 整刀长度 **4.70617**，等于当前 `J01_Body_LOD0` 的模型高度；握柄至刀尖 **4.38914**。尺寸从实际网格计算，统一缩放保持刀身比例。
- 11 个独立深灰金属材质；灰黑面板、稍亮的金属边缘。原来的青色发光材质已换成灰色，沿用冥隼现有紫色刀光表现。
- 保留当前三连斩、输入缓冲、攻击时间窗、命中停顿及伤害。实际刀尖仍参与 240 Hz 扫掠采样；加长刀身的向下姿态和拔收刀过渡做地面避让，握柄贴合真实掌心。
- 背部收纳随刀长提高；新增约 1,142 三角形的共享武器残影，继续使用四个已有残影对象。
- 保留紫眼冥隼机身、原 VALKYR、原刀资产、前一版可玩包与永久存档。

Hades 仅作为剑攻击节奏的本地数据参考：`D:/BaiduNetdiskDownload/Hades.Multi.9/Content/Game/Weapons/PlayerWeapons.sjson` 的 SwordWeapon、SwordWeapon2、SwordWeapon3 描述连段切换、转向限制及第三击蓄力/推进。保留现有三连斩，没有替换为 Hades 动画资源。

## 复现

权威源码、生成材质和最终 Player 在 D 盘本工程。`tools/p0/run_nemesis_raiken_r1.py build` 将源码同步到已有 E 盘构建工程后调用 `NemesisRaikenIntegration.Build`，复用 Library；生成的刀资产和角色预制体随后同步回 D 盘。`ExtractedHeroesIntegration` 重新导入机体时也会保留本轮武器替换。

已发布包：`Builds/Nemesis_Raiken_R1`。证据：`AuditEvidence/nemesis-raiken-r1/release`。新 raiken 套件验证真实挂载刀长与机身高度、灰色材质、30/60/120 FPS 三连斩和收招、握柄误差、暂停及刀身顶点离地距离。同一最终程序集已通过 raiken、cannon 及原有 22 套 Player 检查，共 **24 套 / 2210 项**。本包同时纳入已完成的瓦尔基里四关节背炮及离子推进，详见 `VALKYR_BACK_CANNON_R1.md`。

发布状态与最终结果以 `Launcher/current.json` 和证据目录的 `final-verification.json` 为准；自动检查不替代用户对外观和手感的评价。

## 最终发布验证

- 发布：2026-09-24T10:28:20.070476+08:00，`Launcher/current.json` 指向 `Nemesis_Raiken_R1`。
- 原桌面「机神试炼」仍指向固定 `Launcher/launch.ps1`；`-CheckOnly` 和隔离存档静默 `-Verify` 均通过。启动证据：`Launcher/Verification/20260924-102826`。
- 147 个构建文件完整核验；24 套件使用相同程序集 `c4502db86d9202a1fac5a5350f221f280b57b96699bf0247de1fbfe76e685d52`。
- 永久存档 SHA-256 与发布前一致；保留原可玩包。
- 实测握柄最大误差 **0.000000**，三连斩及收招全程刀身最低点 **0.17270**，机库收纳最低点 **0.55788**。
- `source-snapshot.json` 记录 10:21 的最终构建输入；之后的 CombatVelocity 改动属于后续独立开发，不代表本包内容。
