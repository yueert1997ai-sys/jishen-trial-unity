# Beam VFX R2: barrel alignment and energy packets

User scope (2026-09-19): keep emitted beams straight along the weapon, and give the other player projectiles the same layered beam treatment.

## Alignment

LoadoutVisual now poses the weapon immediately before firing, using the same pose path as LateUpdate without advancing recoil twice. AX-01 shoulder pose uses the planar aim rule instead of pitching toward cursor height. WeaponController launches from the posed muzzle axis. TYPE-08 and AX-01 sample that axis at discharge, then keep the visible connected beam on the moving socket throughout its afterglow. Damage is still applied only once at discharge; turning a fading beam cannot deal extra sweeping damage. The prior 1.1 m collision plane fix remains.

## Other player projectiles

EnergyBoltVisual uses four pooled world-space layers: hue-shifted halo, weapon-color sheath, white core and animated spiral. M7 is green, M14 gold/orange, collection guns keep their configured color, support missiles use orange energy packets. Existing speeds, damage, cadence, piercing, impact classification, audio and missile homing remain. Free-flying packets follow their flight direction after leaving the barrel. Enemy projectile visuals retain their prior presentation; reuse of a former player packet explicitly restores the enemy mesh/trail and disables all energy lines.

The old M7 ballistics assertion requiring a brass mesh has been replaced by an assertion requiring active layered energy visuals. The physics, muzzle alignment, altitude-independent hits and cover tests remain.

## Evidence and release

Candidate: Builds/BeamVfx_Gundam_R2. Evidence: AuditEvidence/beam-vfx-r2/release. Pre-change source snapshot: AuditEvidence/beam-vfx-r2/before-source.zip. R1 playable is retained.

The beam suite captures TYPE-08, AX-01, M7 and M14 in the actual Player renderer; verifies moving/turning barrel collinearity, floor hits, afterglow damage, low-cover blocking, four-layer packets, collection and missile configuration, and player-to-enemy pool reset. The final assembly also requires foundation, tactics, impact, regression and fullplay suites. Tests are hidden, silent and isolated from the permanent profile. Screenshots verify the actual rendered treatment; user visual preference remains unconfirmed.

Published successfully: all six final-assembly suites passed, errors=0 warnings=0, 149 files verified. Moving/turning beam checks measured 0 degrees axis error and 0 m socket gap for both weapons. Launcher CheckOnly and silent Verify passed; permanent profile hash unchanged. Exact evidence in release/summary.json.
