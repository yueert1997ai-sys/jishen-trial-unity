# Hero Model Pass / 2026-09-05

## Scope And Starting Point

Based on `9cc951339f6054fb28c2795e101fda5ec816a6e2`, today's latest saved checkout. No scene rebuild, gameplay rule, damage/cooldown/stat, input or camera-system change. Unity's prefab save additionally serializes the existing movement defaults (42 acceleration, 65 braking, 0.2 dash duration, automatic fire true); these match the unchanged PlayerController defaults. Work is isolated on `codex/rigged-hero-20260905`; no merge or push. Existing unrelated untracked files are not part of this work.

The old Rose Gold Sentinel is a static OBJ/LOD conversion without skin or skeletal clips. Turning that mesh into the requested animated hero would require a new rig and skinning pass, beyond this task's small-repair constraint. Original GLB, OBJ/LOD/PBR, and `RoseGoldSentinelVisual.prefab` are unchanged.

## Three Candidates

| Candidate | Actual inspection | License / decision |
| --- | --- | --- |
| Quaternius Animated Mech Pack | Four author FBXs downloaded and rendered in Blender. Stan: 6,110 triangles, existing rig and 18 clips, including SwordSlash. Other bodies are also stylized utility robots. | Author download includes CC0 notice. Rejected as hero for the cartoon/utility silhouette; Stan retained only as motion source. |
| joney_lol, Rigged robot | Actual GLB: 48 joints, six material slots, no animations. Blender render shows slender limbs, compact head/torso and light shoulder fins. Eight unweighted vertices found during Unity import. | CC BY 3.0 creator listing. **Selected**, repaired weights and retargeted existing motions. |
| SideFX, Mech Rigged Animated | Author advertises rig plus baked idle/walk. Files are 190.8 MB animation archive / 542.6 MB Unreal archive; not downloaded after license gate failed. File-level rig/animation usability is therefore **unverified**. | Content Library standard terms restrict third-party redistribution/publication of content/adaptations. Excluded, not described as cleared for this release. |

Sources: https://quaternius.com/packs/animatedmech.html ; https://poly.pizza/m/BwjA6Thdzd ; https://www.sidefx.com/contentlibrary/mech-rigged-animated/ ; https://www.sidefx.com/legal/terms-and-conditions/ . Licenses and modifications: `ASSET_PROVENANCE.md`, `docs/licenses/Rigged-Robot-Attribution.txt`.

## Integration

- `Assets/Art/Hero/RiggedSentinel.fbx`: existing 48-bone skin, not an unrigged mesh moved as one piece. Generic Animator and the built-in Animation module, no external runtime importer.
- Nine retargeted motions: Idle, Walk, Run, Run_Holding, Shoot, SwordSlash, Jump, HitRecieve_1, Death; plus two held-pose clips, DashPose (Jump frame 5) and CannonPose (Shoot frame 8). Held poses are explicitly baked, not implemented with zero-speed state-time offsets. The controller uses idle/run blending, dash, death, a masked left cannon arm, and an explicit visual-only slash state.
- `RiggedMechAnimator`: uses actual `PlayerController.Velocity` / `IsDashing`, BeamFired, notification-only SkillFired and Damageable events. Root motion disabled so animation never drives movement or damage. Legacy whole-mesh bob disabled only while this rig owns the visual.
- Left forearm cannon provides the real projectile muzzle. Existing hardpoint objects stay under `Hardpoints` and follow cached bones; they are not reparented into a hierarchy that `EnsureDefaultHardpoints` would duplicate. Right-hand blade and tip/trail move with the hand.
- Corrected A-pose vs source T-pose arm calibration, left/right shooting, original helper sphere, eight missing weights, units, foot floor, right grip and material palette. `FBX_SCALE_ALL` is essential: local FBX unit scaling caused 100x attachments in the first test and is not accepted output.
- Pause does not accumulate procedural offsets. Disabling/re-enabling the adapter restores/rebinds the old muzzle and legacy motion without changing combat scripts beyond the notification event.

## Reproduce

1. Blender 5.2.1: run `blender.exe -b --python tools/hero/prepare_hero.py -- "<project root>"` from the project. Fixed source files live in `tools/hero/source/`. The script does not download anything. It outputs `RiggedSentinel.fbx`, a retarget report, and an inspectable `.blend` with all actions.
2. Unity 6000.3.18f1: explicitly run `MECH ROUGE > Hero > Build Rigged Sentinel Visual` (`HeroVisualBuilder.Build`). This updates only the hero controller/materials/attachments/prefab and the active guest player's visual reference. **Never use DemoSceneBuilder for this pass.**
3. Run `HeroVisualAudit.Run` in an isolated copy with `-batchmode` and graphics enabled; omit `-quit` because the test exits itself. Output: `AuditEvidence/hero/`.
4. For rollback, set `PlayerMech_Guest`'s `PlayerMechLoader.defaultMechPrefab` back to `RoseGoldSentinelVisual.prefab` and reload the scene. Old assets are not deleted. A rigged model automatically disables legacy bob; the old visual uses the original adapter as before.

## Validation And Limits

Latest focused Play Mode suite: `HeroPlay08.log`, exit 0. Actual screenshot sequence, not Blender renders or Animator state alone. Skin vertex positions are sampled in hero-local space, removing root translation/rotation; screenshots are also manually inspected. Idle, running, dash transition, cannon, salvo, hit and death deform the body. Weapon parenting, real beam target damage (7 beams plus 1 four-missile salvo, 11 target hits), actual missile event, foot grounding, pause stability and disable/re-enable binding checks pass. The earlier zero-speed offset approach did not produce the intended held pose; the accepted explicit clips produce a 63.46-degree knee bend versus 48.52 at idle.

The closeups change only the diagnostic camera; the normal game-camera captures are separate. Death captures temporarily hide the result overlay solely to expose the body, restoring it immediately afterward. Fixed capture delta 1/60 is used for reproducible pose sampling, not performance measurement. There is still one known Unity Editor SearchDatabase startup exception; it is reported separately, not called a clean Console. Final FBX import has no unweighted-vertex warnings. `HeroPresentation01.log` also passes the existing bilingual/settings/credits UI suite.

**Not implemented / not accepted:** player melee damage, melee input and combo synchronization. The current checkout contains none of those rules. `PreviewSlash()` is explicitly a diagnostic and has no damage or player input binding. No slash motion is secretly mapped onto beam fire to simulate a working melee system. The user was asked whether the referenced morning melee version is in another directory; no path was available at the time of this pass.

Physical-phone rendering/performance, subjective aesthetic approval and human playfeel are not certified by these tests. This is a free rigged replacement, not a recreation of the earlier four-view concept.

## Final Release Replay

The final Windows x64 Mono release **20260905.095954** passed the unchanged `SlicePlayerAudit` full normal-speed replay. Build and Player each report **0 errors / 0 warnings**. Evidence: [player-report.json](audit-evidence/2026-09-05/hero-player-02/player-report.json), [Player.log](audit-evidence/2026-09-05/hero-player-02/Player.log), and 17 actual scene/UI screenshots in that directory.

- 622.882 seconds (10:23), six offered-card choices, 500 kills, victory with HP163.252. Boss took 61.324 seconds and entered both phases. Three subsequent scene reloads returned to the hangar correctly.
- Real movement-router input, automatic beams and missile damage; no forced kills, time acceleration, health cheats or continue. The focused target-health setup above is not used for this full run.
- 37,682 continuously rendered frames at 1920x1080; 36,741 timing samples, mean16.743 / P9517.018 / max47.883 ms on RTX4070Ti/D3D12 at a 60fps cap. Hidden Player scene and UI cameras render every LateUpdate; OS presentation cost is excluded. This is not phone performance or a human playtest.
- 30 GC collection-count increments; per-frame allocation-byte counters remain unavailable, not zero allocation. After combat return and three reloads, Unity allocated memory was 90,909,687 ->90,827,775 bytes, managed heap 2,187,264 ->2,375,680 bytes, within the existing regression tolerances. Sampling explicitly unloads unused assets and collects GC; it is not peak memory measurement.

Playable directory: `Builds/Windows/MECH_TRIAL_20260905.095954/`. ZIP: `Builds/Windows/MECH_TRIAL_20260905.095954.zip`, 50,663,387 bytes (48.3 MiB), SHA256 `85C75761C98FA6E4261077299D8ECF212305F2ED78810FC0D8D1B237F13773FE`. Licenses and startup instructions are included. The release dependency gate checks the saved scene plus Resources: the new rigged prefab is included, old RoseGoldSentinel/Meshy assets are not. See `hero-01/build-dependencies.txt` and `HeroRelease02.log`.

The main scene remains unchanged (SHA256 `5DBA5E77ACC00ECD83F5A2955482AF1A5C40A297C2C8DCA89914B437B92A438A`). Main workspace and test-copy runtime source/active assets were hash-matched before the final replay. The final FBX SHA256 is `7A2B9EDFBFD7F3F3954B782E4F9B6158B73B062E64680B0ED34C64E6B090E2FD`. Later changes only correct documentation and non-runtime retarget-report material metadata.

Earlier builds and unrelated untracked work are preserved. No merge or push. Playable melee/combo remains the explicitly unaccepted item above; passing the current beam/salvo loop does not close that gap.
