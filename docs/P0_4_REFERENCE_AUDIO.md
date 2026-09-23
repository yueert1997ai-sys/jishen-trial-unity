# P0.4 local reference-audio comparison

The user rejected P0.3's cartoon-like synthetic timbre. Their clarification concerned sound character, not a game crash. The inspected latest normal Player.log had a normal shutdown and no crash trace.

The P0 weapon bank now uses `Audio/Combat/P0Reference`. All blade and rifle cues in this bank are trimmed reference-recording excerpts. No FM synthesis, EQ, added sound layers, resampling or random gun pitch is applied. Editing consists of mono conversion, onset alignment for gunshots, 1 ms / 18 ms fades and level adjustment. Existing BGM, combat controls and ballistics are retained.

- Gundam recording: https://www.youtube.com/watch?v=lWRdEW7b7vE
- Delta Force M7 recording: https://www.youtube.com/watch?v=scnYF_Lizlg&t=307s
- Input files: `AuditEvidence/p0-audio-v3/references/`.
- Reproducible edits and sample ranges: `tools/p0/prepare_reference_audio_v4.py` and `AuditEvidence/p0-audio-v4/reference-edit-manifest.json`.

These are reference excerpts for the local comparison demo, not original compositions or cleared production assets. The previous P0.3 CC0 attribution description does not apply to this replacement bank. No GitHub or external distribution was performed.

Validation checks the declared PCM edit, loaded resources, actual playback-source clip identity and original gun pitch, plus existing combat regression. It does not establish subjective sound quality. No audio-device playback, Computer Use or visible playtest was performed.

Build version: `p0.4-reference-audio-20260909`. Existing desktop shortcut continues to use `Builds/P0_CombatDemo/MECH_TRIAL_P0.exe`.
