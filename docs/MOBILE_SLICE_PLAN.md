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
- [ ] 2. One polished 60-90 second maintenance-platform fight: motion, impacts, particles, licensed audio, environment, navigation and pooling. Review visuals before adding duration.
- [ ] 3. Reactor sector, six ranked choices, Boss telegraph correctness, pacing and one-time checkpoint continue. Run full-speed combat.
- [ ] 4. Bilingual presentation/settings, licensing, real Player performance/GC/restart checks, versioned Windows build, evidence and known limitations.

Every gate: inspect runtime/Console, fix regressions, update this record, inspect diff, commit separately. Work in the isolated Unity audit copy, never control the user's desktop. Preserve all old builds and unrelated files.

## Verification limits

Synthetic touch events and replay commands are not physical phone testing. Fast forced-kill tests only prove transitions. Actual runtime rendering is required for performance claims. Unverified listening, device behavior, or subjective quality must stay marked unverified.

## Gate 1 evidence (2026-09-05)

`docs/audit-evidence/2026-09-05/mobile-01/PlayMode.log`: actual Unity Play Mode, 27 focused checks plus real auto-fire victory (26.06s, 22 kills, HP175.03). Synthetic pointer ownership/two-finger dash, release/pause cleanup, normalized diagonal speed, braking, swept dash wall collision/cooldown, visible target selection/hysteresis/occlusion/range, ungated missile skill/cooldown/real damage, notch/letterbox calculations, Boss framing and restart all passed. Captures are camera+uGUI renders at explicit dimensions, not Player performance evidence.

`Regression.log`: previous live-fire victory/Boss phases, reward protection, defeat cleanup and restart still pass. Both runs exit 0. Unity Editor SearchDatabase startup exception remains an engine-only known issue; no game error observed.

Current source uses automatic aim/fire, WASD or stick, Space/dash, E/salvo. All active menus and HUD use 960x540 logical sizing and safe-area roots. Old archived Windows builds still use their old controls until Gate 4. The previous 26-second battle is intentionally retained until the next pacing gate. Menu polish/localization/settings, graphics, real audio and the 10-15 minute run are NOT complete.
