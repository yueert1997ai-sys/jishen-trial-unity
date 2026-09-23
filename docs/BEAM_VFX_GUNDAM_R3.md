# Red enemy energy packets R3

User request: give the small enemy soldiers the same beam treatment, in red.

Enemy projectiles now use EnergyBoltVisual: white core, saturated red sheath, red halo, moving spiral and particle wake. Enemy packets are 1.7 m long with a 0.12 m base width; muzzle flashes and enemy hit/wall bursts use the same red palette. Player weapons keep their R2 colors. Shared pool reuse explicitly reapplies team color, with regression checks in both directions. Projectile speed, damage, AI burst cadence and collision rules are unchanged.

Candidate Builds/BeamVfx_Gundam_R3; previous R2 retained. Pre-change snapshot AuditEvidence/beam-vfx-r3/before-source.zip. Final Player suites: beam, foundation, tactics, impact, regression and fullplay. Evidence under AuditEvidence/beam-vfx-r3/release. All test launches are hidden/silent with isolated profiles.

Published through the permanent desktop launcher: all six suites passed, build errors=0 warnings=0, 149 files verified. Silent launcher check passed at Launcher/Verification/20260919-013159. Permanent profile SHA-256 unchanged.
