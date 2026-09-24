# VALKYR Back Cannon R1

2026-09-24: VALKYR E now deploys the two long cannons of the existing HG_2316 backpack. It replaces the four support missiles. NEMESIS keeps its six drones.

- The original source is the R05 VALKYR Blender asset recorded in `Assets/Art/ExtractedHeroesR2/VALKYR/manifest.json`. No body vertices, triangles, materials or silhouette were replaced. The original backpack weights are recovered from its source vertex groups, including reduced LODs by nearest source vertex, and mapped to two mount joints and two barrel joints.
- `work/valkyr-cannon/rebind.py` writes the separate `Assets/Art/ValkyrCannonR1` EHM and manifest. The original Art and Blender files remain unchanged. All three body LODs retain 149228 / 123252 / 65736 vertices.
- `ValkyrCannonBuild.Build` updates only the VALKYR prefab and its separate body meshes, then builds. Use the explicit `ExtractedHeroesIntegration.Store` native-buffer replacement; `CopySerialized` alone was observed to retain stale rendered bone weights. Regular builds can use the saved prefab without running the importer.
- The mount hinges open before the barrels aim forward. The torso braces while not in an active blade swing; hand IK still runs afterward. Charge tracks the cursor until discharge at 0.48 seconds; two beam hits commit once, then the light fades and the mechanism returns by 1.35 seconds. Default total damage is 72 and cooldown 10 seconds, retaining pack damage/count and cooldown upgrade scaling. Empty manual aim is supported. Cover queries use the existing combat plane.
- `IonThrusterVfx` follows the two actual backpack nozzles: three compressed exhaust cells, blue-white ion particles and the existing jet cores/trails. Each nozzle owns at most 96 additional particles. Existing GB4 flight/body clips remain the locomotion source; the back-cannon deployment is authored for this model, not a claimed imported GB4 weapon animation.
- Pause freezes charge/pose/effects. Death, reset, hangar return and hero replacement cancel the mechanism. Active-cycle cooldown reporting prevents duplicate deployment after a raw cooldown reset. HUD and touch controls identify the new skill.

## Evidence and integration

Independent candidate: `Builds/ValkyrCannon_R1`, built directly from the authoritative D project to avoid the concurrent E build. Evidence: `AuditEvidence/valkyr-cannon-r1/release`. The cannon suite runs with `-p0Check -combatSlice -valkyrCannonCheck`; it verifies actual E input at 30/60/120 Hz, two impacts totaling 72 damage, no damage during charge or afterglow, real LOD bindings, empty aim, cover, pause, restart, hangar cleanup, flight ions and switching back to NEMESIS. Actual Player screenshots were inspected after fixing the native mesh buffers.

HeroSelection and CombatLoopV2 assertions now check physical cannons instead of missiles. CombatTactics retains missile-evasion coverage using actual projectile fixtures, independently of E's new behavior.

The concurrent NEMESIS RAIKEN and CombatVelocity tasks were given the stable 10:20 source/prefab/Art set and cannon test flag. The canonical launcher is deliberately coordinated with their combined release: read `Launcher/current.json` for the published version. This standalone candidate must not overwrite a newer combined release. Full promotion still requires the existing final-Player suites plus cannon and any other integrated feature suite. Automated checks do not establish the user's approval of animation or visual feel.

Independent final candidate validation: cannon, loopv2, imported, heroes and tactics all passed on one assembly (5 suites / 941 assertions). See release/cannon-summary.json and geometry-preservation.json. The binary comparison confirms positions, normals, UVs, triangle indices and weight values are unchanged on all six original surfaces; only bone indices differ.


Published integration verified at 2026-09-24 10:28 +08:00: Launcher/current.json = Nemesis_Raiken_R1, 24 required suites including cannon. The cannon report assembly SHA matches the published Player. Silent verification: Launcher/Verification/20260924-102826. Permanent profile SHA-256 remains 7db4bf90ba758a4cf034dc2081969bc9de413a41483f59c156d76be7cdbf195d. CombatVelocity may publish a later combined successor; it has received this feature's stable assets and test contract.
