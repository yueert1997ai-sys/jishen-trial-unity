# MECH TRIAL / 机神试炼

AI-assisted portfolio prototype by Yue. Windows x64, Unity 6.3 LTS. This is a mobile-first landscape design delivered as a Windows demo, not an Android/iOS release.

## Play

Open `MECH_TRIAL.exe`. Keep the accompanying `_Data`, `MonoBleedingEdge`, DLLs and other files together. Choose **DEMO** and **Deploy**. A successful run targets 10-15 minutes: two sectors, six encounters and upgrades, then the two-phase Boss.

- Move: WASD or the on-screen left stick. Aim/fire are automatic at visible targets.
- Dash: Space or the right dash control. Missile salvo: E or the salvo control.
- Current polish: desktop FPS is uncapped (including after quality changes). Dash now launches quickly and eases out over the same distance, with body lean, gimballed twin thrusters and short boost wakes. Both sectors contain solid industrial buildings, pumps and cargo cover.
- Pause: Escape or the pause control. Upgrades: click a card's Install button, or keys 1/2/3.
- Settings: gear in the hangar, or Settings while paused. English/Chinese, master/music/effects volume, shake and quality are saved locally.
- DEMO allows one sector/Boss-entry continue after defeat. Standard/Veteran do not. Return to hangar starts fresh.

## 试玩

运行 `MECH_TRIAL.exe`，保留同目录所有文件。齿轮设置可切换中文。推荐选择展示难度，开始出击。

WASD / 左侧摇杆移动，自动瞄准射击；空格冲刺，E 发射导弹，Esc 暂停。通关两战区六波、选择六次强化后挑战 Boss。展示难度失败可从战区或 Boss 入口续关一次，失败段的收益不保留。

## Scope And Credits

Gameplay/code/layout and geometric environment: Yue with AI assistance. Hero: **Rigged robot by joney_lol (CC BY 3.0)**, adapted with normalized units, repaired skin weights, recolored materials, CC0 Quaternius motions and project-authored weapon attachments. Source: https://poly.pizza/m/BwjA6Thdzd . License: https://creativecommons.org/licenses/by/3.0/ . This is not a newly hand-modeled character. Enemy animation remains procedural.

The hero now has skeletal idle, running, dash pose, left-arm cannon and death motion. This gameplay revision remains an automatic-shooting Survivor-like; the carried blade has an editor diagnostic slash animation, but no player melee/combo damage system. Do not present the visual slash diagnostic as playable melee.

Sound: Kenney (CC0). Music: Vitalezzz, Subspace (CC0). Font: Noto Sans CJK SC (SIL OFL 1.1). Full notices and source details are in `Licenses/`.

Keep the included hero attribution and license notices with distribution. The old user-supplied Meshy files remain in the source project for rollback and have unresolved generation-service redistribution rights; this caveat still applies to older builds using them. Mobile device performance, physical multi-touch and subjective sound/feel still need human testing. Keyboard and synthetic touch tests are not substitutes for that review.
