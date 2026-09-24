# NEMESIS Eye R1

2026-09-23，已更新桌面「机神试炼」。承接 ExtractedHeroes_R1 的两台机体、新枪、瓦尔基里去肩炮和持枪防穿插修正。

## 问题和修正

原 R04B 眼部发光强度被设成 0.65，眼片只把四角投到原头部表面，内部大部分穿入眉甲/眼窝。LOD 继续独立简化细小眼片和眉甲，导致中、远档的眼片正面采样点全部被遮住。发光贴图和 Unity 的发光开关本身存在。

R06 删除四个埋入的附加眼片/眼窝对象，沿 HG_3101 实际眼睛的原始表面复用左右各 81 个三角形，向外避开重叠表面 0.0006 源单位；只给这两个镜片添加独立蓝白发光，强度 3.2。头盔、眉甲、面罩、天线及其余 79 个网格的原始顶点、面和父级变换签名保持不变。源验证：Evidence/source-eye-fix.json。

三档身体 LOD 都保留完整头部，使眉甲与镜片始终使用一致几何。身体其它部分继续使用简化模型；残影代理仍为 4,656 三角形，不使用完整身体副本。NEMESIS 主体 LOD 三角数为 85,450 / 44,919 / 27,635。

## 源文件、工程和重建

新 Blender 文件：E:/SteamLibrary/GameStudyExports/MechaRefinement_R04/outputs/R06_EYES/NEMESIS_EYE_FIXED_R06.blend。

SHA-256：`ac6475c6a5ed17c2e33df8f6789855b8679ec47793b2a92dde7f9af13cdf4383`。原 R05 文件未覆盖。脚本、图片、导出素材、构建均位于 E:/SteamLibrary/JishenBuildWork/NemesisEye_R1。项目内 Assets/Art/ExtractedHeroesR2、Builds/NemesisEye_R1 和 AuditEvidence/nemesis-eye-r1 为联接，旧 R1 资源和可执行文件保留。

work/fix_eyes.py 生成 R06；work/export_eyes.py -- --hero=NEMESIS --reuse-textures 导出并保留各 LOD 头部；work/run.py build 调用 ExtractedHeroesIntegration.Build 生成原生网格、材质、prefab、Armory 与 Demo_Main。源代码仍以 D:/project-mecha-design/MECH ROUGE/handoff/GameplayLoop_V1 为准，运行前同步到 E:/SteamLibrary/JishenBuildWork/GB4Motion_R1/Project。

## 验证

实际 Player 的 NemesisEyeChecks 对三档身体分别渲染发光关闭/开启帧，在左右真实眼片范围内比较像素。每只眼每档都检出 54 个亮度增加超过 0.08 的像素，跨档一致；这同时覆盖发光开关、表面遮挡和 LOD 丢失问题。正面与斜侧截图来自实际 Player；Evidence/head-front.png 和 head-quarter.png 另为 Blender 源预览，勿混淆。

修复前几何遮挡审计在 Evidence/lod-eye-audit.json；修复后在 lod-eye-audit-fixed.json。最终 22 组 / 2150 项 PASS，源码与 E 盘构建工程 234 个文件一致。构建 0 错误 / 0 警告，147 个文件完整哈希清单已由发布器验证。Assembly-CSharp SHA-256：`4521a0d36f0b6381275179d32fe8a203fcc9506d0b735450a5b8440efcb7cfc9`。

必需套件：imported、aiui、lunar、terrain、arena、punch、vfx、heroes、drones、nemesis、loopv2、foundation、tactics、impact、melee、beam、regression、fullplay、natural、fullrun、performance、punchperf。顺序运行，性能测试单独执行。保持静默，不弹出游戏窗口或播放声音。

通过 tools/p0/publish_playable.py 更新 Launcher/current.json；CheckOnly、Verify 通过，启动证据 `Launcher/Verification/20260923-221411`。永久存档 SHA-256 保持 `7DB4BF90BA758A4CF034DC2081969BC9DE413A41483F59C156D76BE7CDBF195D`。

用户交付位于 E:/SteamLibrary/GameStudyExports/MechaRefinement_R04/ChatWorkspace/outputs/Nemesis_Eye_Fix_R1，C 盘聊天 outputs 仅为目录联接。正面/斜侧实际 Player 图片已发个人飞书，回执在 Evidence/feishu。视觉喜好仍以用户反馈为准，本次未改动全屏泛光效果。
