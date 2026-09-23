# P0.10.1 — shoulder cannon removed

User requested removal of the built-in Valkyr shoulder cannon on 2026-09-11.
The two cannon/cradle MeshFilters and MeshRenderers were removed from the active VALKYR_V9 prefab. The empty pose and fallback muzzle transforms remain because existing animation code references them. There is no remaining visible cannon or mount mesh in those subtrees. Optional armory weapons are unchanged.

Build: Builds/P0_10_1_NoCannon/MECH_TRIAL_P0.exe.
Build version: p0.10.1-no-shoulder-cannon-20260911. Strict build: 0 errors, 0 warnings.
Commands: python tools/p0/build_no_cannon.py; python tools/p0/run_no_cannon.py check; python tools/p0/run_no_cannon.py m7.
Both standalone checks passed, code=0 errors=0. The full check includes five simulated minutes, ballistics, sword/dash/cover and restart; the focused check confirms M7 continuous brass ejection, floor settling, pause and expiry. Actual no-cannon screenshot: AuditEvidence/no-shoulder-cannon/m7/m7_equipped_close.png.

P0 demo/practice launchers and the existing direct desktop shortcut now target this build. Previous P0_10_M7 is retained.

A separate art line lives at ../../art_prototypes/VALKYR_Parallel_R01_20260911. Its new model is not assigned to the game Armory and does not replace the existing V3/VALKYR_V9 body.
