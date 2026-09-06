# Jishen frozen-model game integration

This pipeline consumes the FBX already copied to `Assets/Art/JishenTrial_Assembly_V3`. It does not invoke Blender, read changing model work files, regenerate geometry, or rebuild the game scene.

Run from this project in PowerShell, with the Unity Editor for this project closed:

```powershell
& 'C:/Python313/python.exe' tools/jishen/run_integration.py build
& 'C:/Python313/python.exe' tools/jishen/run_integration.py validate
& 'C:/Python313/python.exe' tools/jishen/run_integration.py release
```

Unity is the existing `D:/Editor/6000.3.18f1/Editor/Unity.exe` (6000.3.18f1). Use the ordinary local account/license environment. `Build` generates the independent `JishenTrialVisual.prefab`, sets the guest player's visual reference, and renders six actual sampled animation poses. `Validate` runs the existing manual combat audit plus visible geometry/attachment/grounding/pause/death checks. `Release` verifies the saved scene depends on the new prefab and creates a new versioned Windows build. Every method is also available through Unity's `-executeMethod` argument as named in the script.

The Python runner uses the existing Python 3.13 installation. The optional `.ps1` wrapper is equivalent, but this machine's execution policy rejected it before execution; no system policy was changed. Use the Python commands above.

Source files:

- `Assets/Editor/JishenHeroIntegration.cs`: prefab construction, rigid bind calibration, actual mesh contact samples, hand grip, shoulder cannon and backpack effect sockets, preview rendering, release dependency check.
- `Assets/Scripts/Player/RigidMechPoseDriver.cs`: per-segment motion retargeting without altering mesh vertices, contact correction, separate beam control.
- `Assets/Scripts/Player/RiggedMechAnimator.cs`: existing animation/combat-event adapter with optional rigid armor support.
- `Assets/Scripts/Player/MechDashPresentation.cs`: existing dash effect, using actual backpack sockets for the Jishen prefab.

The prior Generic skeleton/controller and existing motion clips are retained as an invisible driver; new armor keeps its own pivots and limb lengths. This is an initial playable rigid binding, not a new full skinning/IK/animation-authoring pass. Source geometry must only be updated in an explicit later snapshot, with the segment mapping, grip and socket positions revalidated.

See `docs/JISHEN_GAME_INTEGRATION.md` for actual validation results and rollback. Logs and full screenshot sets live in `AuditEvidence/jishen`.
