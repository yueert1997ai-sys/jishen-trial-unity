# Extracted Heroes R1

2026-09-23，已发布到现有桌面「机神试炼」。源代码工程为 `D:/project-mecha-design/MECH ROUGE/handoff/GameplayLoop_V1`，分支 `codex/equipment-loop-v1`。本轮未提交或推送 GitHub。

## 最终变化

- 冥隼采用用户选定的 R04B 头部延续版本、R04C 后置腿部喷口、R04D 握枪修正，在 R05 更换真实武器 `EqHG_Lrw607`。灰黑长枪保留提取模型的分层枪身和枪管。
- 瓦尔基里采用用户选定的 R04 机体，在 R05 删除 31 个独立肩炮零件，更换真实双枪管步枪 `EqHG_Lrw038`。旧程序生成步枪不再作为这两台机体的默认步枪。
- 瓦尔基里战斗改为 48 度侧身双手持枪：在 ImportedMotion 的胸部姿态上叠加侧身，保留跑动、飞行、受击及动作时序；头部仍维持原瞄准框架。左手沿实际垂直前握把重新定向，使腕部处在握把后方。枪跟随移动肩部中点，双肘向外。机库改为右手垂枪。
- 闭合拳孔轴与真实握把对齐；双手可达范围约束保留。旧版“手掌对上枪即可通过”的检查已补充实际胸甲、前臂、拳头及步枪凸包相交检查。
- 删除的肩炮必须从所有 LOD 的原生 Mesh buffer 消失。Importer 显式更新顶点、法线、UV、索引和蒙皮缓冲，避免仅 CopySerialized 后原生缓冲仍保留旧炮。
- 冥隼六个实际喷口驱动喷流，瓦尔基里两个背部喷口；不再因喷口数量多于 2 而生成悬空的备用圆柱喷嘴。
- 保留 14 段 GB4 动作、现有输入/伤害/枪刀/AI/地图/界面、默认冥隼与机库切换、冥隼六浮游炮和瓦尔基里四发导弹支援，以及限定数量残影池和身体 LOD。

## 资产和空间

新源文件在 `E:/SteamLibrary/GameStudyExports/MechaRefinement_R04/outputs/R05_WEAPONS`：

| 文件 | SHA-256 |
| --- | --- |
| NEMESIS_EXTRACTED_GUN_R05.blend | c106a47b88ba36abcdff8a8e031761a99e01f0dfbc9a7f62757b0b843f19bd61 |
| VALKYR_EXTRACTED_GUN_R05.blend | def0a2eeeddf72780cbbf1ba55b6a6c0450869de978e89e24bc4e08af2884c74 |

所有先前 .blend 保留；`Evidence/source-geometry-audit.json` 核对保留网格、局部变换和父级绑定未被改动，删除项仅为旧冥隼步枪和瓦尔基里独立肩炮。隐藏参考集合的 matrix_world 是求值缓存，不作为原始几何哈希。

模型贴图、网格、Player、日志、测试图片、构建 TEMP/TMP 位于 `E:/SteamLibrary/JishenBuildWork/ExtractedHeroes_R1`。项目内 `Assets/Art/ExtractedHeroesR1`、`Builds/ExtractedHeroes_R1`、`AuditEvidence/extracted-heroes-r1` 为目录联接。可复用构建工程位于 `E:/SteamLibrary/JishenBuildWork/GB4Motion_R1/Project`；旧 `GB4Motion_R1/Player` 未覆盖。

本聊天的 work / outputs 也已迁入 `E:/SteamLibrary/GameStudyExports/MechaRefinement_R04/ChatWorkspace`，C 盘旧路径仅保留联接。迁移约 378 MiB；截图交付目录为 ChatWorkspace/outputs/Demo_ExtractedHeroes_R1，Models 联接到 R05 源文件。

## 实现与重建

导出及材料准备脚本保留在 E 盘本轮 `work`：prepare_gun_materials.py、replace_guns.py、export_heroes.py。真实武器贴图从提取的调色/线条/自发光/法线通道还原；export_heroes.py 输出 model.ehm、manifest.json 和烘焙贴图。

`Assets/Editor/ExtractedHeroesIntegration.cs` 读取 EHM，生成机体、武器及相同 GUID 的网格，更新 Armory 与 Demo_Main。执行 Build 会导入并构建。`work/run.py build` 执行该入口，`rebuild` 只构建代码；运行前必须把 D 盘修改同步到 E 盘构建工程。生成资源和新 .meta 同步回 D 盘。

关键代码：LoadoutVisual、ValkyrMotionDriver.Imported、MechDashPresentation、ExtractedHeroIdentity。HeroSelectionChecks 与 ExtractedArmorClearanceProbe 覆盖本轮问题；探针仅在测试路径创建，在远离场景的位置临时驻留，查询实际骨骼变换，不参与普通玩法。

## 验证和发布

构建：Succeeded errors=0 warnings=0。147 个发布文件覆盖完整哈希清单。最终 22 组测试、2136 项 PASS，均指向同一 Assembly-CSharp：`d88d746072c7434aab82052f4ca4bd9310d6957730145584e9495d3adfc10012`。

必需测试：imported、aiui、lunar、terrain、arena、punch、vfx、heroes、drones、nemesis、loopv2、foundation、tactics、impact、melee、beam、regression、fullplay、natural、fullrun、performance、punchperf。全部顺序执行；两个性能套件运行时无并行构建或 Player。

`Evidence/release/final-verification.json` 记录测试目录与源码/构建工程比对，`build-manifest.json` 为整包哈希。瓦尔基里机库、八向移动、推进和后坐的胸甲相交深度均为 0；故意把枪移进胸甲的反例测出约 0.538 m 相交，证明探针确实检测到了穿入。已人工查看真实 Player 的正面、左右侧、握枪近照、机库和冲刺截图；检测针对所测几何与动作，不代表任意全身姿态绝对无穿模，也不代替用户审美反馈。

经 tools/p0/publish_playable.py 更新 Launcher/current.json。固定快捷方式仍指向 Launcher/launch.ps1，CheckOnly 和静默 Verify 通过；启动证据 `Launcher/Verification/20260923-195451`。永久存档 SHA-256 保持 `7DB4BF90BA758A4CF034DC2081969BC9DE413A41483F59C156D76BE7CDBF195D`。

飞书收件为用户本人，修正后的近照、侧面、冲刺和机库 4 张实际 Player 截图发送回执保留在 Evidence/feishu/clearance-*.json。
