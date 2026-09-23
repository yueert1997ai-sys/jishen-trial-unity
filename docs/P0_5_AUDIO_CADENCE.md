# P0.5: material replacement and shot-synchronous audio

The user rejected the previous saber excerpt and asked for coherent automatic fire with one sound per single shot.

## Saber

Replaced the anime excerpt with **光ライトサーベル剣３**, by **稿屋 隆 (Waraya Takashi)**, DOVA-SYNDROME:

- Material: https://dova-s.jp/se/detail/1086
- Creator: https://dova-s.jp/creator/detail/3
- License: https://dova-s.jp/help/articles/license/

The creator describes electric-guitar-slide-based energy sword swings intended for games. The site's license permits game use and editing; do not redistribute these as standalone sound assets. Unity imports them into the local game's resource data.

The entire swing and decay are retained, at original speed and pitch. A new combo stroke retires the preceding swing with a 35 ms fade. Dash cancellation also fades the swing. Armor contact remains a separate, hit-triggered cue using existing Kenney CC0 impact recordings. Empty swings contain no embedded collision. The two downloadable swing tracks currently have identical decoded samples; no audible variation between them is claimed.

## M7

The three randomly selected, differently sized P0.4 excerpts are replaced by one consistent isolated M7 shot, separated into complementary attack and tail assets. Source remains the locally downloaded Delta Force reference described in `P0_4_REFERENCE_AUDIO.md`; it is not a cleared production gun asset.

- Actual successful fire events trigger audio once, immediately.
- No audio loop, automatic prerecorded burst, independent sound timer or generic cue throttle.
- Reserved body/tail voices prevent enemy effects from stealing rifle voices.
- A new shot fades the previous tail over 25 ms. After trigger release, only the last tail finishes.
- The complementary attack and tail reconstruct the original isolated shot; pitch and level stay fixed across a burst.
- Sustained fire preserves the weapon's 0.18-second cadence instead of accumulating frame rounding. Fresh taps and long interruptions start a new cooldown; no catch-up burst is emitted. Damage, base interval and projectile behavior are unchanged.
- Effects mute applies to all rifle voices.

## Validation

`P0AudioCadenceChecks` drives real input at 30/60/120 FPS: three taps, 1.2 seconds of held fire, release, and a temporary faster-rate probe. It compares actual bullet events to audio events at the same simulation timestamps, checks accumulated cadence error and checks mute/no-loop behavior. Existing combat checks also run.

Authoring: `tools/p0/prepare_audio_v5.py`. Source download: `tools/p0/fetch_saber_materials.py`. Manifest: `AuditEvidence/p0-audio-v5/audio-manifest.json`. Runtime/build reports: `AuditEvidence/p0-combat/`.

Version: `p0.5-shot-synchronous-audio-20260909`. Same desktop launcher and local executable. No visible playtest, audio-device playback or Computer Use. Technical validation is not subjective listening approval.
