# MECH ROUGE Handoff Status

2026-09-24 当前发布：**CombatContact_R1**。命中反馈及掩体规则第一阶段完成，见 [工程说明](COMBAT_CONTACT_R1.md)。31 组最终检查通过；追加 contact 套件，保留前 30 组。永久入口、存档、镜头 17 与最新 AI 保留。源码 D，构建及证据 E，未提交或推送 Git。

以下为历史记录。

2026-09-24 最新本地发布：**CombatInteraction_R1**，见 [更新与验证](COMBAT_INTERACTION_R1.md)。镜头 17、敌方统一白色、按武器与兵种分工的受压退避／掩体探头／侧翼支援／最后目击位置搜索已接入实际攻击系统。最终 30 组 / 3360 项检查、147 构建文件完整性、518 个源码文件同步及 `Launcher/Verification/20260924-163817` 静默验证通过。永久存档保持原 SHA256；CombatArsenal_R2 与固定桌面入口保留。D 源码权威；Builds/CombatInteraction_R1 与 AuditEvidence/combat-interaction-r1 联接 E 盘新发布目录。后续必须沿用 AGENTS.md 的全部 30 组发布门槛。没有 Git commit/push。

以下为历史记录。

2026-09-24 最新本地发布：**CombatArsenal_R2**，见 [完整更新与验证](COMBAT_ARSENAL_R2.md)。镜头 20、瓦尔基里背炮与推进修复、斩舰刀接触强化、冥隼高速移动及自主浮游炮、五种随机敌机／七类真实回收武器、前三关与短战末尾 FAZZ 已集成。最终 29 组 / 3285 项检查、147 构建文件完整性、515 个源码文件同步及 `Launcher/Verification/20260924-160644` 静默验证通过。永久存档保持原 SHA256；旧构建与固定桌面入口保留。D 源码权威，Builds/CombatArsenal_R2 与 AuditEvidence/combat-arsenal-r2 联接 E 盘新发布目录。没有 Git commit/push。后续必须沿用 AGENTS.md 的全部 29 组发布门槛。

以下为历史记录。

2026-09-24 最新本地发布：**CombatVelocity_R1**。战斗镜头 **15**；白色敌机按约 4.5 米机体统一体型、碰撞、枪口与血条；两台机体采用 Ka 站并完整展示。冥隼普通移动改为展翼推进滑行，基础速度从 7.4 提升到 **10.4**，加快起步、换向、冲刺与动作恢复；重击停顿缩短，受击反应重调，血量及武器伤害保持。接入 **36 个加工后的高破4音效变体**和新制作的 **170 BPM 电子金属循环 BGM**。保留等身深灰斩舰刀、紫色眼睛、瓦尔基里双背炮与六浮游炮。28 组 / 2450 项最终 Player 检查、147 文件完整性和原入口静默启动核验通过；永久存档未变。桌面继续打开「机神试炼」。D 为源码，构建及证据通过固定目录联接保存在 E 盘。本轮未提交或推送 GitHub。 见 [本轮说明](COMBAT_VELOCITY_R1.md)。

Historical checkpoints follow.

2026-09-24 最新本地发布：**Nemesis_Raiken_R1**。冥隼替换为原瓦尔基里 RAIKEN 的深灰斩舰刀，整刀长度 **4.70617**，与机体高度一致；保留三连斩，适配握持、收刀和刀尖离地。整合瓦尔基里背炮与离子推进表现。24 组最终 Player 套件、2210 项检查及原入口静默启动验证通过，147 个构建文件完成哈希核对，永久存档未变。桌面继续打开「机神试炼」。构建位于 D 盘 `Builds/Nemesis_Raiken_R1`，源码以本工程为准；后续 CombatVelocity 工作尚在独立进行。 见 [等身深灰斩舰刀](NEMESIS_RAIKEN_R1.md)。本轮未提交或推送 GitHub。

以下为历史记录。

2026-09-23 最新本地发布：**NemesisEye_R2**。冥隼眼睛恢复饱和紫色，降低发光强度以避免泛蓝白；保留原头型、眼片贴合和三档 LOD 可见性修复。仅修改眼睛材质，网格几何不变。22 组最终 Player 套件、2156 项检查及静默启动验证通过，永久存档未变。桌面仍使用「机神试炼」，新源文件和构建在 E 盘。 见 [紫色眼睛修正](NEMESIS_EYE_R2.md)。本轮未提交或推送 GitHub。

以下为历史记录。

2026-09-23 最新本地发布：**NemesisEye_R1**。修正灰色冥隼眼片被眉甲遮挡、远景模型简化后不可见以及亮度偏低的问题；发光贴合原始眼睛表面，保留用户选定的头型。近、中、远三档模型的双眼显示均通过实际 Player 像素检查。22 组最终套件、2150 项检查及静默启动验证通过，存档未变；桌面仍使用「机神试炼」。新源文件、模型和构建在 E 盘，见 [眼部修正](NEMESIS_EYE_R1.md)。本轮未提交或推送 GitHub。 启动证据 `Launcher/Verification/20260923-221411`。

以下为保留的历史记录。

2026-09-23 最新本地发布：**ExtractedHeroes_R1**。冥隼与瓦尔基里已换成用户选定的解包资源细化版；两把步枪换为实际提取的武器模型，瓦尔基里独立肩炮删除。修正瓦尔基里枪托与前臂穿胸甲、左手朝向和机库持枪，保留原始动作、冥隼后置腿部喷口与六浮游炮。22 组最终 Player 套件、2136 项检查及静默启动验证通过，永久存档未变。构建、素材、缓存和图片位于 E 盘，D 盘为现有源码工程；桌面仍用原来的「机神试炼」。见 [模型替换与持枪修正](EXTRACTED_HEROES_R1.md)。本轮未提交或推送 GitHub。 启动证据 `Launcher/Verification/20260923-195451`。

以下为保留的历史交接。

2026-09-23 最新本地发布：**GB4Motion_R1**，详见 [GB4 动作接入](GB4_MOTION_R1.md)。两台机体使用原始 GB4 动作数据，完成 22 组最终 Player 套件、2,112 项检查、147 文件哈希和静默启动验证。永久存档未变；桌面入口已更新。证据在 `AuditEvidence/gb4-motion-r1/release`，启动证据 `Launcher/Verification/20260923-172504`。新构建和证据通过目录联接存储在 E 盘，源代码仍以 D 盘项目为准。此次改动尚未提交/推送；以下为历史记录。

2026-09-23 上一版本：**AI_UI_R2 + Lunar_Basin_R1**。最终包 `Builds/AI_UI_R2` 整合敌人动作/攻击修复、简约全局 UI，以及开采盆地/中继区两张完整月面场地，旧环境保留但关闭。21 组最终 Player 套件、1,451 项检查全部通过；147 个构建文件经哈希核对。唯一桌面「机神试炼」仍指向 `Launcher/launch.ps1`，发布指针已更新；静默启动证据 `Launcher/Verification/20260923-140706`，永久存档 SHA-256 未变。证据在 `AuditEvidence/ai-ui-r2/release`，实现说明见 [AI/UI R2](AI_UI_R2.md) 和 [月面地图 R1](LUNAR_BASIN_R1.md)。完整六房间自动回放 302.22 秒、六次奖励、敌我脱困次数均 0；技术通过不代表真人难度或视觉认可。本轮最终源码改动尚未提交/推送。

2026-09-23 地图与敌人决策 **Arena_Tactics_R1**：两张战术布局原型、遮挡感知与绕行选射位、修复运动预判计时和包围目标漂移、修复突进名额泄漏、近战前沿及枪兵两翼入场。19 组最终 Player 检查通过。地图细部美术和真人体验仍待试玩反馈。详见 [地图与 AI R1](ARENA_TACTICS_R1.md)，实际桌面版本以 `Launcher/current.json` 为准。以下旧记录保留作历史。

2026-09-22 已发布主角：J-01 NEMESIS M01。详见 [冥隼动作与 demo 接入](NEMESIS_M01.md)。来源为用户当前 B20 装甲，保留源文件，动作、武器、展翼、残影在实际玩家上运行；10 项最终 Player 套件及静音启动校验通过，147 个构建文件通过哈希核对。证据在 `AuditEvidence/nemesis-m01/release`，桌面“机神试炼”已指向 `Builds/Nemesis_M01`；永久存档未变。完整自然回放 477.05 秒，基础枪 / 刀 / 混用分别 88.87 / 66.15 / 78.92 秒。用户已明确授权主角替换和本地 demo 更新；实际发布状态查看 `Launcher/current.json`。下列 V3、V2 等是历史交接，不作为当前主角身份。

2026-09-17 最新开发版本：Combat Foundation V3。见 [战斗底层重构](COMBAT_FOUNDATION_V3.md) 和 [Hades 分析](HADES_FOUNDATION_ANALYSIS.md)。构建 `CombatFoundation_V3_20260917`，20 组 Player 检查、1412 项 PASS；原生零强化战斗约 60.7 秒。实际桌面版本以 `Launcher/current.json` 为准。V2 Lab 保留回退。以下 R9 及更早段落为历史记录；本轮尚未获得玩家手感或音色认可。

上一版本 Combat Loop V2 · 30 秒战斗对照，见 [完整调研原文对应、改动与验证](COMBAT_LOOP_V2_LAB.md)。构建名 CombatLoop_V2_Lab_20260916；F9 进入，1 / 2 / 3 对照，R 重开。局部命中停顿统一战斗时钟；短战斗无掉落、强化或永久存档写入。19 组实际 Player 检查通过，完整回放约 63.5 秒。实际启动版本以 Launcher/current.json 为准；以下战术版、R9 等为历史记录。

Historical local work: [R9 combat audio](COMBAT_SLICE_R9_AUDIO.md), `Builds/CombatSlice_R9_20260916/MECH_TRIAL_P0.exe`. Always resolve the verified release through `Launcher/current.json` and the single permanent desktop shortcut. R8 is retained. Read `COMBAT_POLISH_STANDARD.md`; the user rejects the rough audio and wants an AC6-informed complete sound pass. 47 new PCM clips, 34 native cue captures, protected priority mixing, actual propulsion/foot/weapon/absorption events. Actual Player regression and native full-battle evidence live in `AuditEvidence/combat-slice-r9`. Tests are objective evidence, not subjective approval.

Previous P0.10.1: [no shoulder cannon](P0_10_1_NO_CANNON.md), `Builds/P0_10_1_NoCannon/MECH_TRIAL_P0.exe`. New Valkyr R01 art remains separate from this demo.


Previous local demo: [P0.10 M7 refinement](P0_10_M7.md), `Builds/P0_10_M7/MECH_TRIAL_P0.exe`, version `p0.10-m7-20260911`. Sand M7 coating/material detail, machined receiver fasteners, pointed physical rounds, and sustained brass ejection with bounce/rest/expiry are integrated. Launch via the main P0 demo/practice launchers. Build with `tools/p0/build_m7.py`; validate with `tools/p0/run_m7.py m7` and `tools/p0/run_m7.py check`. Existing unrelated local edits were preserved.

P0.9.1 clear-view rendering and P0.9 combat rules remain in force. Previous builds/evidence are retained: [clear view](P0_9_1_CLEAR.md), [OVERDRIVE](P0_9_OVERDRIVE.md). The historical `run_overdrive.py` targets P0_9_Clear. Earlier sections below are historical.

## Latest Manual Combat / Palette Update: 2026-09-05

Continue from [MANUAL_COMBAT_HANDOFF.md](MANUAL_COMBAT_HANDOFF.md). Beams now use explicit mouse/right-stick aim and held fire, with a separate playable slash button and actual bone animation/damage. Environment colors are muted and large colored floor panels use existing textured deck material. Hero modeling work is untouched. Earlier automatic-fire/no-melee statements below describe historical revisions only.

## Latest Industrial Scene / Dash / FPS Update: 2026-09-05

The hero-only revision below is now the baseline, not the latest scope. Continue from [INDUSTRIAL_DASH_HANDOFF.md](INDUSTRIAL_DASH_HANDOFF.md): industrial buildings and solid cover, rebuilt sector navigation, launch-heavy dash with gimballed twin thrusters, desktop framerate uncapped. Main hero FBX/controller/prefabs, encounter data and upgrades remain unchanged. Separate `art_prototypes/` work is untouched. New scene/boost screenshots are indexed in `INDUSTRIAL_DASH_EVIDENCE.md`.

## Latest Hero-Only Update: 2026-09-05

Continue from today's `9cc9513` mobile slice, not the historical setup below. The active player visual is now joney_lol's rigged robot with retargeted Quaternius CC0 animation, selected through the existing `PlayerMechLoader` prefab field. Detailed model changes, reproducible pipeline, test evidence and limits: [HERO_MODEL_HANDOFF.md](HERO_MODEL_HANDOFF.md). Old Meshy assets and visual prefab are retained unchanged.

At the hero-only baseline, beams were automatic and slash was diagnostic-only. This historical limitation has been superseded by the user-requested manual combat update above; there is still no combo system.

Final hero release `20260905.095954` passed 622.88 seconds of normal-speed live fire, six choices, both Boss phases, victory and three restarts with 0 Player errors/warnings. Actual motion sequences: `HERO_ACTION_EVIDENCE.md`; final full replay: `audit-evidence/2026-09-05/hero-player-02/`. Existing Editor SearchDatabase startup exception remains separately documented.

> Historical implementation log, frozen before the mobile-first redesign. Current source-of-truth: `MOBILE_SLICE_PLAN.md`, `ACCEPTANCE_CHECKLIST.md` and README. `AUDIT_2026-09-05.md` records the pre-redesign baseline. Earlier full-flow passes below use forced enemy kills; visual/performance claims are not blanket acceptance. Do not repeat old setup steps by default.

Last updated: 2026-08-01

## Current playable loop

- `Assets/Scenes/Demo_Main.unity` is rebuilt for the first playable Unity 6 demo loop.
- Flow verified in Play Mode smoke test:
  - Hangar/start mission
  - Stage 1 with three waves
  - Three-choice upgrade reward
  - Stage 2 with three waves
  - Boss
  - Victory result
  - Restart back to hangar
- Latest verification log: `UnityStage10_PlayModeSmoke.log`
- Pass marker: `MECH_ROUGE_SMOKE_PASS: Full flow reached victory result, restart returned to hangar.`
- The smoke test now fails when an Error/Exception/Assert stack originates from `Assets/` or `Assembly-CSharp`.

## Player mech visual

- The supplied GLB is present at:
  `Assets/UserContent/PlayerMech/Meshy_AI_Rose_Gold_Sentinel_0618172915_texture.glb`
- Unity's default importer keeps the original GLB as a raw asset, not a usable `GameObject`.
- glTFast 6.19.0 was tested, but its editor importer failed on this GLB and a runtime diagnostic hung for more than 15 minutes.
- To keep this demo playable and the Console clean, glTFast was removed from `Packages/manifest.json`.
- The GLB was decimated offline into Unity-importable OBJ assets:
  - `Assets/UserContent/PlayerMech/Decimated/RoseGoldSentinel_LOD_High.obj`
  - Source mesh: 941,741 triangles
  - Decimated high mesh: 116,398 triangles
- The player visual now uses a three-level `LODGroup`:
  - High: 116,398 triangles for close views
  - Medium: 32,942 triangles for normal top-down gameplay
  - Low: 8,646 triangles for distant views
- The current player prefab uses `Assets/Prefabs/Player/RoseGoldSentinelVisual.prefab`, which now references `RoseGoldSentinel_LOD_High`.
- The original GLB normal, emissive, and metallic/roughness textures are now extracted and assigned to `Assets/Materials/MR_RoseGoldSentinel_PBR.mat`.
- The glTF metallic/roughness map is repacked for Unity Standard shader metallic/smoothness channels.
- Texture import keeps the 2048 source detail uncompressed with mipmaps and trilinear filtering.
- Camera framing is now `(0, 16, -12.5)` at 41-degree FOV, with a cyan four-bracket ground marker for player readability.

## Stage 5 combat/readability pass

- Added enemy and Boss world-space health bars.
- Added hit flash, impact/death bursts, and camera shake.
- Dash now follows movement input first, grants 0.22 seconds of invulnerability, and leaves a cyan ground streak.
- Boss scatter, missile, and charge attacks now display ground telegraphs before firing.
- Combat HUD now includes prominent HP/EN bars, dash readiness, a top-center objective strip, and a damage overlay.
- Visual capture: `docs/VisualValidation_Stage5.png`.
- Rebuild log: `UnityStage5_Rebuild.log` (return code 0).
- Visual capture log: `UnityStage5_VisualCapture.log`.

## Stage 6 presentation/game-feel pass

- Added `MechMotionAnimator` for procedural idle sway, movement lean, step bob, dash impulse, and beam recoil without requiring a rig.
- Added cyan/orange/red projectile trails and muzzle flashes for player, enemy, and Boss weapons.
- Added floating damage numbers tied to actual post-mitigation damage.
- Normal waves now reserve their alive count, show colored ground spawn warnings, then instantiate after a short delay.
- Combat HUD now shows a brief upper-screen stage/wave announcement whenever progress changes.
- Arena presentation now uses a dark carrier-deck floor, restrained grid, blue hangar pad, cyan boundary lights, red Boss gate, and low perimeter structures.
- The player ground brackets were raised above the hangar pad so they remain visible in combat framing.
- Final visual capture: `docs/VisualValidation_Stage6.png`.
- Rebuild log: `UnityStage6_Rebuild.log` (return code 0).
- Full-loop log: `UnityStage6_PlayModeSmoke.log`.
- Visual capture log: `UnityStage6_VisualCapture.log`.

## Stage 7 fairness/audio pass

- Enabled Unity's built-in `com.unity.modules.audio` package and added an `AudioListener` to the generated main camera.
- Added runtime-generated beam, missile, hit, death, dash, warning, wave, reward, victory, and defeat clips via `GameAudio`; no external audio assets are required.
- Drone enemies no longer apply contact damage and explosion damage in the same frame. They stop, display a 0.55-second danger zone, then detonate once.
- Boss charge now checks the player along the full travel path and can damage at most once per charge.
- Regular enemies apply lightweight local separation while moving, reducing unreadable stacking.
- Boss phase 1/2 progress labels and phase-two warning feedback are now explicit.
- The smoke test now verifies `GameAudio`, `MechMotionAnimator`, projectile trails, applied upgrades, elite spawning, Boss spawning, and project-owned error logs in addition to the full flow.
- Final visual capture: `docs/VisualValidation_Stage7.png`.
- Rebuild log: `UnityStage7_Rebuild.log` (return code 0).
- Full-loop log: `UnityStage7_PlayModeSmoke.log`.
- Visual capture log: `UnityStage7_VisualCapture.log`.

## Stage 8 combat UI/pause pass

- Escape now pauses safely during Hangar or Combat, suspending gameplay time, player control, enemy behavior, and audio while preserving the previous time scale for resume.
- Added a clickable pause menu with Resume and Restart Run; runtime canvases now ensure an `EventSystem` exists so reward and pause buttons accept mouse input.
- Combat HUD now reports the current hostile count and uses a cyan combat cursor at the mouse aim point.
- Added a top-center Boss health bar with exact HP and phase 1/2 state.
- The full-loop smoke test now verifies pause UI visibility, frozen control/combat state, audio pause, and exact time-scale restoration.
- Combat visual capture: `docs/VisualValidation_Stage8.png`.
- Pause visual capture: `docs/VisualValidation_Stage8_Pause.png`.
- Rebuild log: `UnityStage8_Rebuild.log` (return code 0).
- Full-loop log: `UnityStage8_PlayModeSmoke.log`.
- Visual capture log: `UnityStage8_VisualCapture.log`.

## Stage 9 enemy identity/fairness pass

- Rebuilt melee, ranged, drone, elite, and Boss visuals as separate multi-part procedural mech silhouettes instead of generic block placeholders.
- Added enemy steel plus red, orange, and magenta emissive materials so archetypes remain readable at the top-down gameplay scale.
- Added `EnemyMotionAnimator` for biped stride weight, heavy-unit sway, drone hover/banking, and rotor motion without changing AI or colliders.
- Default player incoming damage is buffered to 85%, and the player receives 0.13 seconds of post-hit invulnerability to prevent same-frame projectile stacking.
- Clearing a wave now restores 12% max HP when damaged and briefly reports the actual repair amount in the objective strip.
- Smoke coverage now verifies all four enemy visual archetypes, a 24-plus-renderer Boss hierarchy, reduced incoming damage, same-frame hit protection, and effective wave repair.
- Visual validation now renders at 1920x1080 and preserves world-space health bars while compositing screen UI.
- Combat visual capture: `docs/VisualValidation_Stage9.png`.
- Pause visual capture: `docs/VisualValidation_Stage9_Pause.png`.
- Boss visual capture: `docs/VisualValidation_Stage9_Boss.png`.
- Rebuild log: `UnityStage9_Rebuild.log` (return code 0).
- Full-loop log: `UnityStage9_PlayModeSmoke.log`.
- Visual capture log: `UnityStage9_VisualCapture.log`.

## Stage 10 LOD/performance pass

- Wired the existing high, medium, and low Rose Gold Sentinel OBJ assets into one cross-fading `LODGroup` while retaining the same PBR material on all levels.
- Normal top-down framing transitions to the 32,942-triangle medium mesh; close views retain the 116,398-triangle high mesh and distant views use 8,646 triangles.
- Full-loop smoke validation reads the imported meshes at runtime and records `MECH_ROUGE_LOD_PASS: high=116398 medium=32942 low=8646`.
- Added `DemoPerformanceSmoke`, which creates 16 extra mixed enemy actors and explicitly renders 120 synchronized 1920x1080 frames to a RenderTexture.
- Validation-machine result: 579.1 average synchronized renders/second, 1.1 ms 95th percentile, 64.6 ms slowest frame, and 191.4 MB allocated memory.
- These batchmode numbers are a regression gate on this machine, not a general hardware promise; `UnityStats` does not report triangle/batch counters in batchmode, so mesh budgets are verified separately through the LOD runtime assertion.
- Combat visual capture: `docs/VisualValidation_Stage10.png`.
- Pause visual capture: `docs/VisualValidation_Stage10_Pause.png`.
- Boss visual capture: `docs/VisualValidation_Stage10_Boss.png`.
- Rebuild log: `UnityStage10_Rebuild.log` (return code 0).
- Full-loop/LOD log: `UnityStage10_PlayModeSmoke.log`.
- Performance log: `UnityStage10_PerformanceSmoke.log`.
- Visual capture log: `UnityStage10_VisualCapture.log`.

## Stage 11 Windows standalone pass

- Added `DemoBuildPipeline.BuildWindowsDemo`, available from `MECH ROUGE > Build Windows Demo`, for a deterministic Windows 64-bit build.
- The verified playable build is located at `Builds/Windows/MECH_ROUGE_Demo/MECH_ROUGE.exe`.
- Added `StandaloneRuntimeSmoke`, which remains disabled during normal play and activates only with the `-mechSmoke` command-line flag.
- The built-player smoke test verifies pause/resume, the three-level player LOD, Stage 1, reward application, Stage 2 elite spawning, Boss spawning, victory, and restart back to Hangar.
- Windows build result: 185,578,186 bytes across 143 files; Unity reported a 24.49-second build.
- The built executable completed the full automated flow in 7.4 seconds and exited with code 0.
- Standalone pass marker: `MECH_ROUGE_STANDALONE_PASS: Full built-player flow reached victory and restart returned to Hangar.`
- Rebuild log: `UnityStage11_Rebuild.log` (return code 0).
- Editor full-loop log: `UnityStage11_PlayModeSmoke.log`.
- Windows build log: `UnityStage11_WindowsBuild.log`.
- Built-player full-loop log: `UnityStage11_StandaloneSmoke.log`.

## Stage 12 deployment/difficulty pass

- Replaced the bare hangar prompt with a clickable sortie deployment interface that keeps the player mech visible and presents the full mission route.
- Added three run difficulty profiles selectable before deployment:
  - Cadet (default/recommended): 78% enemy health, 65% incoming damage, 20% wave repair.
  - Standard: baseline enemy health/damage and 12% wave repair.
  - Veteran: 125% enemy health, 120% incoming damage, 8% wave repair.
- Difficulty now affects every spawned normal enemy, summoned enemy, and Boss; the combat HUD and result screen retain the selected profile label.
- The deployment UI hides atomically when combat begins and the combat HUD replaces it. Restart restores the deployment screen and default Cadet profile.
- Editor and built-player smoke coverage now switches between Veteran and Cadet, validates all multipliers, checks the deployment-to-combat UI transition, and asserts the selected health scale on live normal enemies and the Boss.
- 1080p stress result after this pass: 784.9 average synchronized renders/second, 0.9 ms 95th percentile, 57.4 ms slowest frame, and 191.9 MB allocated memory.
- Updated Windows build size: 185,584,810 bytes across 143 files. The built-player full-flow self-test exited with code 0.
- Hangar visual capture: `docs/VisualValidation_Stage12_Hangar.png`.
- Combat visual capture: `docs/VisualValidation_Stage12.png`.
- Pause visual capture: `docs/VisualValidation_Stage12_Pause.png`.
- Boss visual capture: `docs/VisualValidation_Stage12_Boss.png`.
- Rebuild log: `UnityStage12_Rebuild.log` (return code 0).
- Editor full-loop log: `UnityStage12_PlayModeSmoke.log`.
- Performance log: `UnityStage12_PerformanceSmoke.log`.
- Visual capture log: `UnityStage12_VisualCapture.log`.
- Windows build log: `UnityStage12_WindowsBuild.log`.
- Built-player full-loop log: `UnityStage12_StandaloneSmoke.log`.

## Equipment/model scope

- Modular equipment visual upgrades are paused for this pass.
- `PlayerMech_Guest.prefab` has an empty `startingEquipment` list.
- Stage reward is now stat/projectile-only via `RunUpgradeSystem`, not equipment mounting.

## Main files touched

- `Assets/Scripts/Core/RunUpgradeSystem.cs`
- `Assets/Scripts/Core/GameManager.cs`
- `Assets/Scripts/Core/StageManager.cs`
- `Assets/Scripts/Core/StandaloneRuntimeSmoke.cs`
- `Assets/Scripts/Combat/Damageable.cs`
- `Assets/Scripts/Enemy/EnemyMotionAnimator.cs`
- `Assets/Scripts/Player/PlayerStats.cs`
- `Assets/Scripts/Combat/WeaponController.cs`
- `Assets/Scripts/UI/RewardUI.cs`
- `Assets/Scripts/UI/CombatHUD.cs`
- `Assets/Scripts/UI/HangarDeploymentUI.cs`
- `Assets/Scripts/UI/PauseUI.cs`
- `Assets/Scripts/UI/RuntimeUIFactory.cs`
- `Assets/Scripts/UI/ResultUI.cs`
- `Assets/Editor/DemoSceneBuilder.cs`
- `Assets/Editor/DemoPlayModeSmoke.cs`
- `Assets/Editor/DemoPerformanceSmoke.cs`
- `Assets/Editor/DemoBuildPipeline.cs`
- `Packages/manifest.json`

## Remaining known issue

- The actual player visual is derived from the uploaded GLB via decimated OBJ rather than direct GLB import.
- The source texture resolution is 2048x2048, so close-up sharpness cannot exceed the supplied texture without replacing or reauthoring it.
- The current mech has no animation rig; movement is root motion only.
- A clean validation clone can log one Unity 6 `UnityEditor.Search.SearchDatabase` startup indexing exception before Play Mode. It does not involve project assemblies; the project-error-aware smoke test still passes.
