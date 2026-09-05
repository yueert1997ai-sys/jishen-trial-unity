# Mobile-first vertical slice

Approved 2026-09-05. Start from commit 65ca4d9, never rebuild the game from scratch.

## Locked decisions

- Windows delivery, landscape touch-first UI; no APK or iOS claim this iteration.
- One movement stick, automatic visible-target fire, dash and one missile skill.
- Bright anime hard-surface science fiction. Keep the current hero source; no Blender or new hero model yet.
- English presentation default, Chinese toggle. Redistributable assets only, with provenance.
- Two sectors, three encounters each, six upgrade decisions, two-phase Boss; normal successful play targets 10-15 minutes.
- Encounter targets: 75/90/105 seconds then 90/105/105 seconds, Boss 90-150 seconds. No empty waiting or health inflation to pad time.
- Demo difficulty allows one sector/Boss-entry checkpoint continue; Standard restarts the run.
- Keep built-in rendering, uGUI, existing combat events. No meta-progression, networking, monetization, or framework rewrite.

## Execution gates

- [x] 1. Unified input, collision-aware motor, mobile controls/HUD, safe-area layout, automatic targeting and camera framing. Real Play Mode tests.
- [x] 2. Maintenance-platform implementation and automated gate: motion, impacts, particles, licensed audio, environment, navigation and pooling. Visual review done; subjective polish/listening remain review items.
- [ ] 3. Reactor sector, six ranked choices, Boss telegraph correctness, pacing and one-time checkpoint continue. Run full-speed combat.
- [ ] 4. Bilingual presentation/settings, licensing, real Player performance/GC/restart checks, versioned Windows build, evidence and known limitations.

Every gate: inspect runtime/Console, fix regressions, update this record, inspect diff, commit separately. Work in the isolated Unity audit copy, never control the user's desktop. Preserve all old builds and unrelated files.

## Verification limits

Synthetic touch events and replay commands are not physical phone testing. Fast forced-kill tests only prove transitions. Actual runtime rendering is required for performance claims. Unverified listening, device behavior, or subjective quality must stay marked unverified.

## Gate 1 evidence (2026-09-05)

`docs/audit-evidence/2026-09-05/mobile-01/PlayMode.log`: actual Unity Play Mode, 27 focused checks plus real auto-fire victory (26.06s, 22 kills, HP175.03). Synthetic pointer ownership/two-finger dash, release/pause cleanup, normalized diagonal speed, braking, swept dash wall collision/cooldown, visible target selection/hysteresis/occlusion/range, ungated missile skill/cooldown/real damage, notch/letterbox calculations, Boss framing and restart all passed. Captures are camera+uGUI renders at explicit dimensions, not Player performance evidence.

`Regression.log`: previous live-fire victory/Boss phases, reward protection, defeat cleanup and restart still pass. Both runs exit 0. Unity Editor SearchDatabase startup exception remains an engine-only known issue; no game error observed.

Current source uses automatic aim/fire, WASD or stick, Space/dash, E/salvo. All active menus and HUD use 960x540 logical sizing and safe-area roots. Old archived Windows builds still use their old controls until Gate 4. The previous 26-second battle is intentionally retained until the next pacing gate. Menu polish/localization/settings, graphics, real audio and the 10-15 minute run are NOT complete.

## Gate 2 evidence (2026-09-05)

The first encounter now lasts **75.11 seconds** in real-time Play Mode, using 23 authored entry beats and a ten-hostile cap. It feeds into the old remaining encounters for a **102.99-second** full run (79 kills, one upgrade, victory). This is deliberately not yet the 10-15 minute six-upgrade run.

- Evidence: `docs/audit-evidence/2026-09-05/maintenance-02/`.
- Native NavMesh route around a repair bank passed; moving enemies no longer allocate an overlap array every frame. Existing blockout retained but disabled. New environment/encounter data has a reproducible scoped builder.
- Player projectiles and ordinary enemy fire use Unity ObjectPool; thin-wall sweeps, reuse/reset, phase cleanup, compound-collider explosion deduplication passed. Boss projectile pooling awaits Gate 3.
- Found/fixed a real Burst Core regression: a blast's direct target now takes full base damage once, including actors larger than the blast radius. The separate splash still falls off.
- Dynamic sparks/smoke use ParticleSystem emission; warning lines and damage numbers are pooled. Motion uses eased lean/yaw, recoil and thrust trails, not a new skeletal animation.
- Ten licensed audio cues and a streamed 181-second score load successfully. Music has gain ducking and a 2.5-second loop crossfade; subjective listening not verified. Sources/hashes: `ASSET_PROVENANCE.md`, `licenses/`.
- Current shaders/scene/HUD captured and inspected. Floor z-fighting and low-contrast objective text were corrected. The existing hero's topology/texture style remains a known visual limitation; no replacement model made.
- No game exception in the final suite. Unity Editor SearchDatabase startup exception still present. These captures are not a full Player performance benchmark.
- Final regression also exits 0: live-fire victory, reward damage protection, defeat cleanup and return to hangar. Inventory has no missing-script/reference findings; `NOT_SCENE_DEPENDENCY` includes intentionally runtime-created components and Resources-loaded audio, not just unused assets.
