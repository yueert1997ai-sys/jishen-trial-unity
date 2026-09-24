# Enemy behavior and shared UI R2

User request: repair enemy actions/attack logic, remove bare circular markers, and provide a simple usable UI throughout the game.

## Design

Combat instruments use a slate panel (#0E141B), off-white text (#EBF2F5), muted captions (#A1B3C2), pale green health (#C9E0D1), blue energy (#80B3D6), and coral danger (#F0614D). Noto Sans CJK provides both ordinary labels and larger health numerals. The identifying element is a broad horizontal hull bar with a brief amber damage trail. Information stays at the perimeter: objective and time top-left, hull/energy bottom-left, weapons and cooldowns bottom-right. No permanent ground rings; the reticle uses small corners and a center dot. Range warnings use soft filled areas while directional attacks keep their warning lines.

Shared menu surfaces and buttons carry this palette into the hangar, settings, pause, reward selection, collection, and result screens. The existing mechanisms and controls remain authoritative.

## Behavior changes

- Canceling an attack also resets the drone detonation latch and tactical movement plan. Target loss, death, pause/action-generation changes release attack reservations and telegraphs.
- Melee actors with available attack capacity close inside strike range during lunge cooldown. The surround anchor remains a waiting position. Turns are bounded; navigation steers the body around cover. Close attacks require facing the target.
- Rifle retreats use different enter/exit distances to avoid rapid reversal, retain a reachable firing goal until it is invalidated, and close into useful range before stopping to shoot.
- Lunge damage is resolved against each actual movement segment once per victim. Walking into a previously traversed trail no longer causes a delayed hit. Solid cover blocks drone blast damage as it does melee attacks.
- Every burst round refreshes the actual muzzle origin and poses the rifle along the shot direction. Elite volleys retain the announced direction. Melee anticipation and forward thrust use the existing E01 skeleton.
- The normal HUD is enabled in short combat. Temporary immediate-mode overlays are removed. Short-combat results stay visible; retry restarts the same mode, and pause-return actually returns to the hangar.

## Validation

`AiUiChecks` exercises interrupted drones and melee follow-up at 30/60/120 Hz, real muzzle/projectile alignment, pause and lost-target cancellation, marker removal and warning-pool reuse, actual health display, and button-driven pause/resume/retry/return. Real Player captures include combat, pause, results and hangar. Existing combat, navigation, hero, drone, weapon and full-run suites must pass on the final assembly; performance suites run alone.

Candidate runner: `tools/p0/run_ai_ui_r2.py`. Baseline snapshot and permanent profile hash: `AuditEvidence/ai-ui-r2/baseline.json` and `before-source.zip`. Source began on Arena_Tactics_R1; a concurrent user-requested Lunar_Basin_R1 map pass is integrated without overwriting its layout ownership. Final build/evidence/launcher state are recorded after validation. No technical pass is a claim of human approval of combat feel or visual style.

## Final local release, 2026-09-23

- Published `Builds/AI_UI_R2` through the existing permanent launcher at 14:07:05 +08:00. Strict build succeeded with zero errors and zero warnings; all 147 release files matched the complete hash manifest.
- All 21 required suites passed 1,451 assertions against assembly SHA-256 `8628042d9ebaaeef6d06fe0f8de4055f475169c22e7b28718bf4ab907cf95311`. The `aiui` suite includes 69 assertions; actual viewports were 1280x720, 1280x1024, 1600x720, 1920x1080 and 1600x900, with visible HP glyphs and panels inside each viewport. Combat, hangar, pause, results and a natural mixed-combat frame were visually inspected.
- Final lunar / terrain / arena suites passed; the saved scene includes both lunar arenas, shared rock-ridge collision and the inactive retained old environments. The terrain audit exercised 1,176 unobstructed routes.
- Ordinary-command short replays completed in 49.72s with rifle, 29.42s with blade and 32.20s mixed. Each defeated 34 enemies and respected the five live/reserved cap. The full six-room replay completed in 302.22s with six rewards, real equipment installation and zero player/enemy rescues.
- Performance and punchperf ran sequentially with no concurrent Unity build or other Player. Synchronized offscreen scene measurements are recorded in their reports and are not a displayed-FPS guarantee.
- `Launcher/launch.ps1 -CheckOnly` resolved to AI_UI_R2, and silent `-Verify` passed at `Launcher/Verification/20260923-140706`. Desktop `机神试炼.lnk` still targets this launcher. Permanent profile SHA-256 remains `7db4bf90ba758a4cf034dc2081969bc9de413a41483f59c156d76be7cdbf195d`; the old playable build and baseline snapshot remain retained.

Final evidence: `AuditEvidence/ai-ui-r2/release/test-summary.json`, `source-manifest.json`, `build-manifest.json`, `publication.json`, per-suite reports and production UI screenshots. Source HEAD during integration was `70341854f00463408153140d637ac623883c85d0`, with final local changes not committed or pushed by this task.
