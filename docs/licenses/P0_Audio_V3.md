# P0.3 audio sources and processing

## Runtime music

**Heavy Battle 2**, by **MintoDog**. CC0 1.0.

- Source: https://opengameart.org/content/heavy-battle-2
- Download: https://opengameart.org/sites/default/files/heavy_battle_2_bpm185_0.ogg
- Runtime file: `Assets/Resources/Audio/Music/P0_HeavyBattle.ogg`
- Processing: highpass 38 Hz, presence reduction 2 dB at 2.4 kHz, loudness normalization to -18 LUFS / -2 dBTP, 4/8 ms boundary fades to avoid a loop click. Complete musical loop length retained.

## Runtime rifle basis

**M4 Assault rifle firing.wav**, by **mnslugger20**. CC0 1.0.

- Source and license: https://freesound.org/people/mnslugger20/sounds/259758/
- Public high-quality preview: https://cdn.freesound.org/previews/259/259758_4121395-hq.mp3
- Author describes a recorded .223 shot with bass, shell and echo processing. This is a recorded rifle basis, not an authentic Delta Force M7 asset.
- Three isolated 72 ms transient excerpts, EQ, short diffuse tails, tiny pitch differences and a quiet mechanical layer form `P0EnergyKinetic/rifle_shot_1..3.wav`. No complete automatic burst is embedded in a single shot.

License: https://creativecommons.org/publicdomain/zero/1.0/

## Runtime sword and mechanical layers

Beam swing and heavy preparation are original FM/harmonic/noise synthesis. Plasma impact adds existing Kenney CC0 impact recordings (`impactMetal_heavy_000/001.ogg`); rifle mechanism uses `impactMetal_medium_000.ogg`. See `Kenney_impact-sounds.txt`.

## Reference analysis only (not included in Resources or build)

- Gundam sound effect - Beam saber sound, Laplus White: https://www.youtube.com/watch?v=lWRdEW7b7vE
- Delta Force Hawk Ops All Weapon Animations, GameCompareCentral, M7 chapter 05:07: https://www.youtube.com/watch?v=scnYF_Lizlg&t=307s

These recordings remain under `AuditEvidence/p0-audio-v3/references` for analysis only. They are not samples in the authored runtime audio. Waveform/spectral analysis is recorded in `reference-analysis.json`; no acoustic audition or playback on the user's device occurred.

Authoring: `tools/p0/author_combat_audio_v3.py`. Output hashes, levels and selected recorded shot times: `AuditEvidence/p0-audio-v3/audio-manifest.json`. Original P0.2 bank remains intact for comparison.
