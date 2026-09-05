# MECH ROUGE Handoff Status

> Historical implementation log. Current source-of-truth: `AUDIT_2026-09-05.md`. Earlier full-flow passes use forced enemy kills; visual/performance claims are not blanket acceptance. Preserve this history, but do not repeat the old setup steps by default.

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
