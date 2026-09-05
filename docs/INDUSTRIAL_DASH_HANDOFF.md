# Industrial Scene And Dash Polish

## Scope

Continues from `bb7018e` on `codex/industrial-dash-polish`. The user requested richer buildings/obstacles, stronger dash/thruster action and no 60fps lock. The separate hero concept/model work, `art_prototypes/`, hero FBX/controller/prefab and existing unrelated untracked work are untouched. No Build cards, enemy stats, encounter schedules or melee/combo rules are added in this pass.

## Changes

- Five project-authored Blender modules: service building with recessed blast door, mullions, buttresses, roof fans and external pipework; low coolant pump; ribbed cargo crate; laddered reservoir; drain grate. Bevelled geometry and shared instanced materials replace a subset of old cover visuals. Existing old cover is disabled, retained in the scene.
- Each sector has four peripheral service buildings, four tanks, four low pump covers, six cargo crates and fourteen grates. Central movement lanes and all four enemy entry routes remain accessible. Buildings and low covers have physical colliders. Both navigation datasets are rebuilt from active environment collision only, not character render meshes.
- Deterministic deck surface bitmap and adjusted directional/ambient lighting. Tall buildings remain peripheral; low cover occupies the battle area. These are modular stylized assets, not a claim of finished photoreal environment art.
- New `MechDashPresentation` on the player: current rig's animated hips/chest lean into the dash, cannon orientation retained, two chest-mounted nozzle housings gimbal toward exhaust direction, white/cyan flame cores and short twin wakes. It never moves the CharacterController. This is real animated skeleton plus additive body motion, not root translation alone. The current rig binding is explicit; a future replacement hero needs its own attachment/animation compatibility check.
- Dash movement integrates a quadratic ease-out over each frame: faster launch and decelerating finish, same 5m / 0.2s defaults. Cooldown, energy cost, collision and invulnerability rules are unchanged. No input buffer or chain dash was added.
- Desktop `targetFrameRate=-1`, `vSyncCount=0`, on startup and both quality presets. Mobile follows reported display refresh with a 60fps minimum target, not a validated device performance guarantee. Raising the cap cannot guarantee a particular FPS.

## Reproduce

1. Run Blender in background: `blender.exe -b --python tools/environment/build_industrial.py -- "<project root>"`.
2. In a protected project copy, run `IndustrialPolishBuilder.Build`. This modifies the existing scene's environmental children/materials/navigation, never invokes DemoSceneBuilder or regenerates encounter data. Preserve imported FBX axis correction: the placement wrapper owns yaw/collision; the imported model keeps its original local rotation.
3. `IndustrialPolishAudit.Run` enters real Play Mode and saves screenshots/checks to `AuditEvidence/industrial/`. Run without `-quit`; it exits itself. Graphics required.
4. The existing `HeroVisualAudit.Run` and Windows `SlicePlayerAudit` cover motion and the existing game loop respectively. Final results below must refer to the new build, not the prior capped baseline.

## Validation

Focused suite `Industrial02Play.log`: pass, zero game runtime errors, one separately classified existing Editor SearchDatabase exception. Assertions cover upright/grounded building meshes, flat grates, all four entry paths in both sectors, linecast blocked by pump, NavMesh route around pump, collision-stopped dash, full twin wakes, >20-degree body change, pose recovery and pause stability. Real motor dash distance is the same at 30/60/120/240 fixed simulation capture rates. Fixed pose capture is not an FPS measurement.

The first screenshot review found overwritten FBX axis correction; that output was rejected and the wrapper fix plus geometry-bounds assertions were added. Final screenshots must be from the corrected second run or later.

`IndustrialHeroRegression.log` also passes the existing actual skin/body/real projectile damage, death, pause and adapter disable/re-enable suite with zero game errors and one known Editor Search exception. The independent main scene object-ID check retained all 1,910 baseline serialized objects (2,206 now); zero baseline objects were removed. Main scene/runtime code/active player assets were hash-matched against the isolated test copy.

## Final Uncapped Player

Release **20260905.133754**, Windows x64 Mono, strict build: **0 build errors / warnings**. Full normal-speed release replay with `-sliceAuditDash` passed: **608.766 seconds, 324 successful dashes, six choices, 498 kills, both Boss phases, victory, three subsequent reloads**, zero Player errors/warnings. Boss took 51.876 seconds, finishing HP168.052. No forced enemy kills, time acceleration, health cheats or continue. Evidence: `audit-evidence/2026-09-05/industrial-player-02/player-report.json`, `Player.log` and 17 actual rendered captures.

Framerate policy in that Player: `targetFrameRate=-1`, `vSyncCount=0`. RTX4070Ti/D3D12, 1920x1080 scene/UI continuously rendered offscreen: 484,140 timing samples, mean **1.244583 ms**, P95 **1.504606 ms**, max **56.095806 ms**, 496,194 rendered frames. This excludes OS presentation and is not a guarantee of 800 displayed FPS, mobile performance, zero stutter or a controlled comparison with the former capped baseline. Long-frame cause remains unprofiled. GC count incremented 57; per-frame allocation bytes are unavailable, not zero. After return plus three reloads, allocated memory 100,439,100 ->101,247,732 bytes and managed 5,758,976 ->5,943,296 bytes passed existing regression tolerances; explicit unload/GC sampling is not peak memory.

The earlier candidate `133401` was deliberately stopped before completion to add repeated dash commands and correct diagnostic render ordering; it is not an accepted full replay. Final renderer executes after all camera/rig/boost LateUpdates. Diagnostic inputs/counters are opt-in and absent from normal gameplay.

Playable folder: `Builds/Windows/MECH_TRIAL_20260905.133754/`. ZIP: same path plus `.zip`, 53,209,197 bytes (50.7 MiB), SHA256 `17096D2FBAEFF9280C7A42F993D6BFC528DA6C4172B204FA8D7D831FFB4C161E`. Existing versions are not overwritten. Source licenses and startup instructions are included.

No human-playfeel, phone performance or finished-art approval is claimed. Difficulty, Build depth and melee/combo are not resolved by this visual/movement pass. Current beam/salvo rules are intact; future hero replacement still needs animation and nozzle socket fitting.
