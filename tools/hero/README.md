# Rigged Hero Asset Pipeline

Only `prepare_hero.py` is required to regenerate the shipped model. It reads the fixed files in `source/`, uses Blender's existing FBX/glTF import/export APIs, retargets the animations and writes `Assets/Art/Hero/RiggedSentinel.fbx`. No download, desktop automation, auto-rig service or paid plugin runs as part of regeneration.

```
"C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" -b --python tools/hero/prepare_hero.py -- "C:\Users\yue\Documents\MECH ROUGE"
```

Run `HeroVisualBuilder.Build` explicitly in Unity 6000.3.18f1 afterward. It does not rebuild the scene. Retain `.meta` files when transferring generated assets. `HeroVisualBuilder.BuildRelease` checks scene/Resources dependencies exclude the old Meshy visual, then calls the existing versioned Windows build pipeline.

- `source/joney-rigged-robot.glb`: untouched selected source, joney_lol, CC BY 3.0.
- `source/Stan.fbx`: untouched CC0 Quaternius motion source. Its mesh is not included in the Unity hero.
- `source/RiggedSentinel.blend`: inspectable generated rig/actions before Unity material recoloring; regenerated, not hand-edited.
- `render_motion.py`: optional Blender pose-check renders. These are not runtime acceptance evidence.
- `inspect_mechs.py`: optional intake check, requires the independent download directory with the four Quaternius FBXs and robot GLB. Unselected models are intentionally not imported into the Unity project.

The original model has no animation. The adaptation preserves its skeleton/skin, repairs eight unweighted vertices, normalizes coordinates and bakes nine retargeted motions plus two held poses. No horizontal animation root motion is applied to gameplay. The model uses Generic animation; it is not a certified Humanoid avatar or a general-purpose retargeting system.

Source attribution, modification notice and license URLs: `docs/licenses/Rigged-Robot-Attribution.txt`, `docs/licenses/Quaternius-Animated-Mech.txt`. Validation, limitations and rollback: `docs/HERO_MODEL_HANDOFF.md`.
