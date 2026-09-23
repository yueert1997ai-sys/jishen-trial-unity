# P0.6 lunar traversal — 2026-09-09

Local build: `Builds/P0_CombatDemo/MECH_TRIAL_P0.exe`, version `p0.6-lunar-traversal-20260909`. Existing desktop P0 launcher remains the entry point.

## Fix

- P0 player step offset increased from 0.25 to 0.45 m and collision skin from 0.04 to 0.08 m, allowing low lunar curbs and surface details to be crossed.
- Side contact cancels P0 Dash only when planar movement is almost entirely blocked. Tangential motion continues along walls; frontal impact still stops Dash.
- Lunar navigation rebaked for radius 0.8 m, height 3.4 m, climb 0.45 m and voxel size 0.1 m. Previously the navigation agent was smaller than the player capsule and admitted routes the player could not fit through. Both lunar bake entry points use the new settings.
- Render geometry, scene layout and solid cover colliders preserved. No teleport or collision bypass added.

## Background verification

`python tools/p0/run_p0.py nav`, `build`, `terrain`, `check` run hidden, without audio or desktop input.

- Build succeeded: zero errors and warnings.
- On the updated navigation, 1,084 sampled clear routes: original motor settings blocked 26 routes; new settings blocked zero, within endpoint tolerance. This is sampled coverage, not proof of every possible trajectory.
- Physical fixtures pass: low curb crossing, full cover blocking, grazing Dash sliding without tunnelling, frontal Dash stopping, and reversing away from contact.
- Full P0 regression passed with zero errors, including combat chaining, blade obstruction, replay and the simulated 300-second arena flow. This uses invulnerability and fixed simulation delta, not a natural human playthrough.

Evidence: `AuditEvidence/p0-combat/terrain/{quick-check,lunar-traversal}.txt`, `AuditEvidence/p0-combat/check/quick-check.txt`, and `AuditEvidence/p0-combat/build-result.txt`. Original navigation backup: `AuditEvidence/p0-lunar-fix/MARE07_Navigation.before.asset`.
