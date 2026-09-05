# Windows Player Validation

## Reproduce

Build with `DemoBuildPipeline.BuildMobileSlice` in Unity 6000.3.18f1. It creates a new `Builds/Windows/MECH_TRIAL_<UTC version>` directory, using the existing saved main scene, Windows x64 Mono, release and StrictMode. No scene/model rebuild runs. Licenses and `START_HERE.md` are copied into the build.

Launch the executable with `-screen-fullscreen 0 -screen-width 1920 -screen-height 1080 -sliceAudit <absolute output directory>`. Add `-sliceAuditSmoke` for a 95-second first-sector smoke. `Start-Process -WindowStyle Hidden` keeps these automated runs off the user's desktop. Do not add `-nographics`.

Without `-sliceAudit`, the diagnostic GameObject is never created. Normal game focus loss still clears input and pauses. The diagnostic explicitly opts into unfocused command replay, mutes only its in-memory master volume and does not save preferences. It does not send OS input.

## Measurement Boundaries

- The full replay uses the same movement router, real auto-aim/automatic weapon/skill damage and uGUI raycast buttons as gameplay. Seed 5092026, rectangular movement route. No forced enemy kills, health cheats, time acceleration or continue in the full victory run. Only offered cards can be selected.
- The hidden Windows window does not present a usable backbuffer on this machine. D3D12 `ScreenCapture` failed; D3D11 produced black PNGs. Those experiments are NOT rendering/performance passes.
- The accepted renderer explicitly calls the actual scene camera and a screen-space UI camera every LateUpdate in the release Player, at 1920x1080, 4x MSAA. This runs the complete simulation and rendering workload continuously, not just isolated screenshot frames. Readback occurs after both cameras render. Every captured PNG must contain more than 100 sampled colors. No OS composition/presentation cost or physical display latency is measured.
- `Time.unscaledDeltaTime` samples the full Player frame interval at a 60fps cap. Initial five combat seconds, transition setup, screenshot encoding and eight surrounding frames are excluded. The additional diagnostic camera/UI force-update work is included; these numbers are not a phone estimate or GPU-only time.
- Unity's release `GC Allocated In Frame` ProfilerRecorder is unavailable in the tested build. The final harness probes Mono's `GC.GetAllocatedBytesForCurrentThread` with a known 4096-byte allocation before using it. The fallback measures main-thread deltas, including diagnostic work, excluding worker threads. An unavailable metric is never interpreted as zero allocation. `GC.CollectionCount(0)` is recorded separately.
- Restart memory is sampled after explicit `Resources.UnloadUnusedAssets` and `GC.Collect`, first after combat return, then after three fresh scene reloads. This detects persistent accumulation, not natural-GC behavior or peak device memory. Tolerances: <16 MiB additional Unity allocations and <4 MiB additional managed heap across those three reloads; also require one GameManager and no enemies in the hangar.

## Evidence

Presentation: `audit-evidence/2026-09-05/presentation-04/PlayMode.log` with bilingual, settings, safe-area and text-fit checks. Button raycasts are real uGUI routing; slider/toggle assignments are synthetic, not physical touch.

`audit-evidence/2026-09-05/player-04/player-report.json` and `Player.log` are the accepted release run, version **20260905.054835**. Build and Player both report **0 errors/warnings**. The source build is 205,223,576 bytes before packaging.

| Measurement | Result |
| --- | --- |
| Full live-fire victory | 623.742 seconds, 6 choices, 501 kills, HP164.452 |
| Boss | 61.634 seconds, both phases |
| Build from offered cards | AMP1 / RATE3 / PIERCE1 / REPAIR1 |
| Complete offscreen render | RTX4070Ti, D3D12, 1920x1080, 4xMSAA, 60fps cap |
| Frame intervals | 36,790 samples; mean16.741 / P9517.015 / max30.204 ms |
| Render/capture checks | 37,731 rendered frames; 17 nonblank PNG captures |
| GC | 32 collection-count increments; allocation bytes unavailable |
| Three scene reloads after return | Unity allocated86,948,166 ->86,915,502 bytes; managed2,179,072 ->2,363,392 bytes |

Both the release ProfilerRecorder and the Mono allocation-byte probe were unavailable on this runtime. The report deliberately labels `gcSource` as `unavailable`; byte fields must not be read as zero allocation. Collection count and repeated-restart heap checks are the available GC evidence. Use a separate instrumented development build before making per-allocation claims or device optimization decisions.

The earlier passing 95-second smoke (`smoke-report.json`) rendered 6170 frames, captured six images, killed 71 enemies and completed three restarts with 0 runtime errors/warnings. Its mean/p95 frame interval was 16.75/17.02 ms. Native-window and early diagnostic-camera experiments are excluded from acceptance; their failures led to the explicit continuous-render method described above.

Known external issue: Unity Editor SearchDatabase startup indexing still throws an engine-only exception. It is separate from Player logs. Human listening, physical multi-touch, Android/iOS performance and playtest difficulty remain unverified.
