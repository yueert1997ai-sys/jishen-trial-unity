# Asset provenance

## E-01 liquid boss integration V8, 2026-09-07

The user-authored R02 E-01 liquid-metal boss was previously integrated in the separate `codex/industrial-dash-polish` checkout. `tools/import_liquid_boss_v8.py` freezes its existing FBX, living-metal shader, materials and `E01ElitePoseDriver` into this gameplay checkout; it does not save or modify the original Blender projects. `Assets/Prefabs/Enemies/LiquidE01Boss.prefab` has its own GUID and retains the existing 86-bone rig, two skinned LODs, core and tendril controls. The underlying source is `art_prototypes/TYPE_E01_ELITE_20260906/stage_02_head/TYPE_E01_ELITE_HEAD_R02.blend` in the main art checkout. No additional third-party asset was downloaded. Source-copy hashes are recorded locally in `AuditEvidence/liquid-boss-v8/source-snapshot.json`.

The current scene uses this prefab for the normal final encounter and the homepage challenge. The veteran mech retains its independent prefab and trial entry; the V7 protagonist, sword, audio and E01 rifle soldiers remain active. Existing BossController attacks and health values are retained, with the imported core/phase/death presentation merged into the current controller. This is animated living-metal surface and bone deformation, not fluid simulation. See `LIQUID_BOSS_V8.md` for current validation and its limits.

## Sword power combo and audio V7, 2026-09-07

The V7 key poses, planted steps and elbow/weapon alignment are project-authored runtime animation in `ValkyrComboProfile.PowerDefaults` and `ValkyrMotionDriver`. The editable asset is `Assets/Resources/ValkyrMotion/PowerComboV7.asset`; V6's profile and hand geometry are retained. No model master, third-party animation, or anime media is added.

`tools/author_raiken_v7_audio.py` creates eight 48 kHz stereo PCM clips in `Assets/Resources/Audio/Combat/RaikenV7`. Air movement, low-frequency body, servo/energy layers and metallic resonances are synthesized in the project. The armor-contact layer reuses the existing Kenney CC0 metal-impact recordings listed below, with filtering, retiming and mixing; their original OGG files remain unchanged. The output manifest, signal levels and hashes are in `AuditEvidence/power-combo-v7/audio/bank-manifest.json`. Existing Kenney notices are included with the new build. These clips are authored sound design, not newly recorded Foley or downloaded Gundam sounds.

The V7 preview sound is captured from the actual Unity Player mixer by the opt-in `PlayerAudioCapture` replay helper and muxed with matching rendered frames. Clip loading, cue timing and non-clipped mixer levels were checked. The current agent tool cannot return audible input for listening, so signal checks do not establish subjective sound quality. See `VALKYR_POWER_COMBO_V7.md` for reproduction and evidence.

## Reverse grip and three-hit combo V6, 2026-09-06

The V6 right hand is derived from the user's frozen `SOURCE_CURRENT_MASTER.blend` under `art_prototypes/JishenTrial_Hero_Assembly_V3/body_mass_refinement_20260906/iteration_v3/game_update`. `tools/export_v6_hand.py` reads the source without saving it, separates the existing palm and finger geometry into 16 rigid parts (4,816 triangles), and adjusts the finger cage around the grip. The game copy lives in `Assets/Art/ValkyrHand_V6`; existing V3 materials are reused. Source path and SHA-256 are recorded in `hand_meshes.json`. The body, head and sword source assets are not remodeled. Reverse carry, side regrip, the three cuts and finger articulation are project-authored runtime poses. No additional third-party motion or model assets are imported. See `VALKYR_REVERSE_COMBO_V6.md` for the editable profile and actual Player previews.

## Sword and movement VFX V4, 2026-09-06

The blue edge halo, swept blade light, cut particles, metal contact sparks, impact flash/recoil, nozzle particles and footfall dust are project-authored runtime effects. Their soft glow textures are generated in code; no third-party effect textures, anime clips or motion files are added. The frozen V3 character/weapon geometry and material assets remain unchanged. See `VALKYR_ACTION_V4.md` for the current build and preview.

## Current Valkyr V3 model and authored motion V3, 2026-09-06

The current protagonist uses the user's V3 game export from `art_prototypes/JishenTrial_Hero_Assembly_V3/body_mass_refinement_20260906/iteration_v3/game_update/exports/`, frozen in `Assets/Art/ValkyrRaiken_V3`. Source hashes are recorded in `AuditEvidence/v3-model-update/source-snapshot.json`. The motion revision adjusts the runtime weapon mount and rigid joints; it does not edit the Blender source, mesh geometry, or materials.

Motion V3 is authored project code and editable key poses. Continuous Sword Impulse anime frames were studied from a [fan repost of SEED DESTINY clips](https://mecha-gifs.tumblr.com/post/135327510383/sword-impulse-gundam); this is not an official upload and its GIF timing is not a verified episode timecode. See `VALKYR_MOTION_V3.md` for exact clip references, observations, and original adaptations. No Gundam models, animations, textures, or video files are bundled in the game. Existing enemy animation, font, and audio attribution below remains applicable. The following V1 integration entry is historical.

## Valkyr / Raiken combat integration, 2026-09-06

The current gameplay worktree uses the user's delivered `art_prototypes/RAIKEN_MkII_20260906/exports/VALKYR_RAIKEN_GAME.fbx` as the player. Its companion GLB supplies the original material values and six embedded textures. A frozen copy is under `Assets/Art/ValkyrRaiken_V1`; `tools/prepare_valkyr.py` records source hashes in `AuditEvidence/valkyr/source-snapshot.json`. No Blender source or geometry was remodeled in this integration.

Yesterday's frozen Jishen V3 visual is retained as the `RivalVeteran` enemy, with red sensor/edge materials. That enemy keeps the existing animation skeleton and retargeted Quaternius motions described below. As of motion V2, the Valkyr player disables those generic motion clips and uses project-authored whole-body rigid-joint poses and two-bone IK in `ValkyrMotionDriver`. All existing attribution files remain bundled. The new swept blade ribbon and runtime pose/AI adapters are project code. Sword Impulse official posed photos and public video storyboard frames were studied as visual motion references; no Gundam animation or model assets were imported. See `VALKYR_MOTION_V2.md` for the sources and the limits of the reference study. Historical references to the active hero below describe earlier versions.

## Industrial modules / 2026-09-05

Service buildings, coolant pumps, reservoirs, cargo crates and drain grates are project-authored procedural meshes, generated with Blender by `tools/environment/build_industrial.py`. The deck bitmap is generated by `IndustrialPolishBuilder.MakeDeckTexture`; no downloaded image or third-party building model is used. No additional third-party asset attribution is required for these additions. Existing hero, animation, font and audio licenses below still apply.

## Existing user hero

Rose Gold Sentinel GLB and converted OBJ/PBR files remain untouched as a rollback option, no longer the active player visual after the 2026-09-05 hero pass. The project does not establish their generation-service redistribution terms. Confirm those before distributing these fallback source assets or older builds containing them.

## Active rigged hero (2026-09-05)

- **Rigged robot**, by **joney_lol**: https://poly.pizza/m/BwjA6Thdzd . Creator listing and its CC BY 3.0 link verified; actual 1,551,944-byte GLB downloaded and inspected in Blender. 48-bone skin, six material slots, no source animations. License: https://creativecommons.org/licenses/by/3.0/ . Attribution, source, changes and license URL are in `docs/licenses/Rigged-Robot-Attribution.txt` and the in-game credits.
- Adaptation: GLB -> normalized FBX; eight missing vertex weights repaired from nearest weighted surface vertex; existing rig retained; palette changed to white/graphite/cyan with copper accents; right-hand grip and feet corrected. New blade/cannon are small project-authored attachments, not a replacement character made from primitives.
- Motions: **Quaternius Animated Mech Pack**, `Stan.fbx`, CC0 1.0: https://quaternius.com/packs/animatedmech.html . Actual FBX contains 18 actions including SwordSlash. Nine motions retargeted to the chosen rig. `Shoot` is mirrored to the left arm. Source pack license copied verbatim to `docs/licenses/Quaternius-Animated-Mech.txt`.
- Sources and reproducible Blender pipeline: `tools/hero/source/` and `tools/hero/prepare_hero.py`. Unity output: `Assets/Art/Hero/`, `RiggedSentinelVisual.prefab`. No new runtime model importer or paid service.
- This choice permits reuse under the stated attribution conditions; the uploader's license is not an independent warranty of all third-party rights. Keep attribution with portfolio screenshots and distributed builds. Do not imply these third-party models were modeled from scratch by Yue.

## E-01 infantry game integration (2026-09-06)

User-provided project asset: `art_prototypes/Type_E01_20260906/stage_02/TYPE_E01_STAGE02.blend` in the main D-drive project. Read with local Blender 5.2.1 LTS by `tools/export_e01_game.py`; no source .blend save. Evaluated armor is combined into 19 rigid joint meshes and 11 shared materials in `Assets/Art/E01_Stage02_Game`. The studio, reference boards and duplicate rifle display are excluded. The rifle mesh is reused for enemy carry, drops and the player's recovered equipment. V5 single-hand hero motion, stance arbitration and E01 rigid-part motion are project-authored. Existing third-party notices below continue to apply to retained assets.

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
