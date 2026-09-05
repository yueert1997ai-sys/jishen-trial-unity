# Asset provenance

## Existing user hero

Rose Gold Sentinel GLB and converted OBJ/PBR files remain user-provided assets. The project does not establish their generation-service redistribution terms. Confirm those before public submission/distribution. No new hero geometry or Blender output was created in this iteration.

## Maintenance platform

Project-authored geometric layout/materials, reproduced by `Assets/Editor/SliceEnvironmentBuilder.cs`. Uses Unity primitives, built-in Standard shader, and Unity NavMesh. Generated output is confined to `Assets/Art/SliceEnvironment` and the named scene root `SliceEnvironment`. Existing blockout is disabled, not deleted. Run this builder deliberately, not on every import.

## Kenney audio

Downloaded from the creator's official website on 2026-09-05. Original license files are in `docs/licenses/`. Selected audio is unmodified OGG; playback gain/pitch varies at runtime. All three packs explicitly allow commercial/educational use under CC0.

| Source | Selected files in Assets/Resources/Audio/Combat | Use |
| --- | --- | --- |
| https://kenney.nl/assets/sci-fi-sounds | laserLarge_001/002, explosionCrunch_000/002, thrusterFire_000/002, computerNoise_000, forceField_001, spaceEngineLow_000 | beams, kills, dash/missiles, warning, ambience; forceField reserved |
| https://kenney.nl/assets/impact-sounds | impactMetal_heavy_000/001, impactMetal_medium_000 | hit variants |
| https://kenney.nl/assets/music-jingles | jingles_HIT00/04/08/13 | wave, reward, victory, defeat stingers |

These are sound effects/stingers and an engine bed. Automatic load/signal checks do not establish subjective listening quality.

## Subspace music

- Author: Vitalezzz (vitalezzz), **Subspace**. Creator's upload: https://opengameart.org/content/subspace . Page explicitly lists CC0 and the credit "Vitalezzz - Subspace". Verified 2026-09-05.
- Download: https://opengameart.org/sites/default/files/subspace_loop.mp3 . Converted to 44.1 kHz Vorbis quality 5 with FFmpeg; no composition changes. Runtime uses a 2.5-second crossfade because the author notes the supplied loop is not perfect.
- Game asset: `Assets/Resources/Audio/Music/Subspace_Loop.ogg`. Provisional electronic/orchestral battle score, lower gain in menus and ducking beneath warnings/rewards. Listening fit remains a human review item.

## Noto Sans CJK SC

- Noto CJK project, Simplified Chinese Regular OTF, SIL Open Font License 1.1. Unmodified font bundled for consistent Chinese/English glyph coverage without depending on installed system fonts.
- Official source: https://github.com/notofonts/noto-cjk/tree/main/Sans/OTF/SimplifiedChinese ; license: https://github.com/notofonts/noto-cjk/blob/main/Sans/LICENSE . Retrieved 2026-09-05.
- Asset: `Assets/Resources/Fonts/NotoSansCJKsc-Regular.otf`. SHA-256: `2C76254F6FC379FDDFCE0A7E84FB5385BB135D3E399294F6EEB6680D0365B74B`.
- Full copyright/license notice: `docs/licenses/Noto-OFL.txt`; copied into every versioned build by the build pipeline.
