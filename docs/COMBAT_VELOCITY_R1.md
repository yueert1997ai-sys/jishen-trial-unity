# CombatVelocity R1

Local source: `D:/project-mecha-design/MECH ROUGE/handoff/GameplayLoop_V1`.
Build workspace: `E:/SteamLibrary/JishenBuildWork/GB4Motion_R1/Project`.
New Player, work files, source-audio provenance and evidence: `E:/SteamLibrary/JishenBuildWork/CombatVelocity_R1`.
The retained Nemesis_Raiken_R1 Player remains separate. Launcher promotion follows the complete final Player verification.

## Requested behavior

- Normal combat orthographic size is **15**, including boss framing and the default comparison view. Camera edge calculations account for the actual wide viewport.
- NEMESIS and VALKYR hangar bodies use a dedicated Ka display pose: supported soles, a broad stance, absolute 15-degree outward toes, quiet torso, tucked chin and relaxed arms. Existing actual gun-grip and armor-clearance constraints remain active.
- Basic travel is **10.4 m/s** (was 7.4), acceleration **90 m/s²** (42), braking **125 m/s²** (65), opposite-direction acceleration **135 m/s²**. The serialized active PlayerMech_Guest values were updated too. Boost remains 1.6 times basic speed. Dash retains its 5 m integrated distance and costs, with duration **0.16 s** (0.20). Existing shared cooldown and damage rules are retained.
- NEMESIS ordinary movement blends into propulsion flight, with direction-dependent chassis pitch/bank and visible sustained exhaust. Backpack deployment reaches 82% during basic movement and 100% on dash/boost; it holds for 0.35 s after movement before folding. Moving support drones retain their independent world-space formation/recall owner.
- E01 white soldiers are **4.5 m** tall; actual rendered armor was measured at 4.510 m versus NEMESIS's 4.706 m body. The complete body, carried weapon and muzzle hierarchy scales together. Capsule/nav height, body clearance, melee reach and health-bar placement agree. Enemy HP and player weapon damage are unchanged; melee/ranged/elite traversal is faster without shortening attack warnings.
- Imported sword clips use separate preparation/active/recovery mappings. Existing strike windows, combo link times, damage and 240 Hz blade contact samples remain authoritative. Cancel-return blend is 70 ms instead of 180 ms; heavy contact hold is 60 ms instead of 95 ms. Light holds remain 28–32 ms. E01 directional recoil has a sharper peak/shorter decay; actual stagger also produces its camera tick on armor-contact hits.
- The bottom status panel sits lower. Hangar and battle weapon labels correctly identify the integrated dark RAIKEN blade. The equal-height NEMESIS blade, original VALKYR blade, purple eyes, four-joint VALKYR back cannon and six NEMESIS drones are retained.

## Reference use

Read the already extracted `D:/GameStudyExports/Hades/Readable/Content/Game/Weapons/PlayerWeapons.sjson` and `Scripts/CombatPresentation.lua`. Sword effects distinguish short uninterruptible phases from a longer cancelable phase; presentation checks simulation-slow state/cooldown before adding another hit accent. The Unity change follows phase separation and bounded contact accents. It does not import Hades artwork or claim recovered native engine behavior. All mecha-specific numeric tuning above is authored for this project.

Ka display reference: [Bandai RX-78-2 Ver.Ka](https://www.bandaispirits.co.jp/products/search/detail.php?grp_id=5325&prd_id=4543112142153000), and the modeler's [original pose explanation](https://www.sanyodo.co.jp/news/gds_gunpla_10). EXVS inspiration is the [official XBOOST operation guide](https://gundam-vs.jp/extreme/ac2xb/howto/operation/), especially sustained boost, short steps and movement cancellation; no original EXVS numeric parameters are claimed. Wing treatment follows the rigid unfolding silhouette shown in the [Freedom design interview](https://tamashiiweb.com/topics/detail/t_kokkaku_139.html).

## Audio

Source is yesterday's complete GB4 `UmodelSaved` export, not the older partial `Unpacked` folder. The mission SE bank is carved losslessly from `bnk_se_mission_reside.uexp`, retaining its cue metadata; vgmstream identifies 881 streams. Only named rifle, saber, impact and boost cues are selectively decoded. The full bank and raw decoded samples stay in the local round's AudioSources folder.

`Assets/Resources/Audio/Combat/VelocityR1` contains **36 processed mono PCM 48 kHz variants**. Each uses onset trimming, distinct pitch, highpass/lowpass filtering, original FM electronic transient, controlled 75/92 Hz body, saturation and short release. These are processed source derivatives, not unchanged GB4 playback. They replace the actual rifle, hostile gun, beam, slash, contact and dash cue banks after legacy-bank initialization. Gameplay RNG is not used for variants. Priority sound ownership is preserved; continuous firing makes a shallower music dip.

New BGM: **OVERDRIVE / 推进临界**, original 64-bar, 170 BPM, 90.353-second electronic-metal loop. Distorted double-tracked power chords, bass, drum and mechanical synth parts are locally composed/synthesized, with FFmpeg mastering. It samples no GB4/Hades music. The requested external ElevenLabs music submission through fal was rejected before generation because the linked account had no balance; it was not retried or billed. This release uses the completed local composition, not an AI-generated result. Stems and composition metadata remain in the round's Music folder.

Reproduction: `tools/p0/extract_velocity_audio.py`, `author_velocity_audio.py`, `verify_velocity_audio.py`. These reference the local extracted assets and the installed vgmstream/NumPy/SciPy/FFmpeg tools. `AudioSources/processed-manifest.json` records cue indices and both source/output hashes. `release/audio-asset-audit.json` verifies sample rates, peaks, transient ends and musical loop continuity.

## Verification and preservation

Final promotion requires the prior 24 suites plus **velocity, audio, sound, playmix**. All tests use isolated profiles and hidden Players, with native audio captured offline. Run performance/punchperf alone after other Player processes exit. Every final report must match the final Assembly-CSharp hash. Copy newly generated Unity metadata from the reused E project back to D.

Velocity probes exercise 30/60/120 Hz actual movement, braking/reversal, hover clearance, ordinary wing deployment/closure, both hangar poses, scaled armor/muzzle/grip/collider/navigation, camera convergence and runtime sound banks. Existing imported-motion tests distinguish actual hover from foot-contact walking. Drone follow/reacquisition now traverses a fixed clear distance instead of a fixed frame count, so changing motor speed does not accidentally move the scenario behind a lunar ridge; its separate cover-blocking tests remain intact.

Automated measurements and rendered inspection establish the listed behavior and regression checks. They do not substitute for the user's combat-feel, pose or listening judgment.

Final release verified: 28 suites / 2450 assertions, 147 hashed build files, 297 D/E source and asset matches. Launcher: `Launcher/Verification/20260924-105749`. Permanent profile SHA-256 unchanged. Native cue-bank peak 0.314; actual mixed battle capture peak 0.551. See `AuditEvidence/combat-velocity-r1/release/final-verification.json`. Local only; no Git commit/push.
