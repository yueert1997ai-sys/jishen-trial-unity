# P0.2: M7 kinetic fire and planar combat

Local build: `Builds/P0_CombatDemo/MECH_TRIAL_P0.exe`, version `p0.2-kinetic-planar-20260909`. Existing `PLAY_P0_DEMO.cmd` and desktop shortcut use this build.

- P0 aiming and M7 barrel direction use the XZ ground plane. Corrected the imported M7 muzzle marker orientation. Targets' cosmetic height does not affect hits; solid cover still blocks shots, including between the player and a protruding muzzle.
- M7 fires physical rounds at 72 units/s, with a short warm tracer, combustion flash, shell ejection and a separate rifle sound. Existing damage and fire interval are preserved.
- Sword sounds are shortened for fast / fast / heavy timing. Only the third strike has preparation audio; obsolete regrip audio and duplicate generic melee hit audio are suppressed.
- Original audio assets remain intact. `tools/p0/author_kinetic_audio.py` generates the new bank offline; `AuditEvidence/p0-ballistics/audio-manifest.json` records durations and levels. No audible audition was performed.

## Validation

Run `python tools/p0/run_p0.py build`, then `python tools/p0/run_p0.py check`. The latter is a silent, background standalone regression run.

The regression covers target heights -4 / 1.1 / 7 with and without boost, horizontal muzzle and projectile alignment, projectile appearance and height stability, exactly-once damage, close aim, cover obstruction, and sword cue counts/durations. Existing combo transition, input buffer, dash cancellation, collision and replay checks also run.

The five-minute simulation uses invulnerability and fixed simulation time: it validates stability and arena completion, not natural playthrough balance or subjective combat/audio quality. Reports: `AuditEvidence/p0-combat/build-result.txt` and `AuditEvidence/p0-combat/check/quick-check.txt`.
