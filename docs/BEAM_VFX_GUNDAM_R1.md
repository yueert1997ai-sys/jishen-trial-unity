# Beam VFX Gundam R1

Round goal: the beam weapons read as monochrome tubes. Reference stills from
Gundam SEED-style particle beams call for three-layer brightness (overexposed
white core, saturated weapon color, diffuse hue-shifted halo), entwined
white/color helix ribbons, radial star glare at muzzle and impact, and
asymmetric directional explosions. This round rebuilds the beam presentation on
those principles. The takeover also fixes beam collision height to match the existing planar combat rule; damage values, charge timing and penetration budgets are retained.

## Why no Gundam assets

The Gundam visuals are Bandai Namco IP, so nothing is traced or downloaded.
The look is recreated from CC0 Kenney Particle Pack sprites already shipped for
EnemyVfx (see docs/ASSET_PROVENANCE.md), plus procedural LineRenderer layers
and shader work. Every texture stays in the existing license envelope.

## What changed

### BeamFxKit (new, Assets/Scripts/Combat/BeamFxKit.cs)

Shared runtime-authored particle kit, mirroring the EnemyVfx singleton
pattern. Eleven world-space systems built from "MECH ROUGE/Particle Additive"
and the CC0 sprites:

- `StarGlare`: three-layer glare (white-hot core flare, star rays, wide halo).
- `MuzzleBlast`: bore-aligned muzzle_02 sprite plus a full glare.
- `BeamStream`: helix motes emitted around the beam axis with tangential
  velocity, flowing muzzle-to-impact, with occasional yellow-white crackle.
- `ChargeConverge`: charge-up motes spiraling inward around the bore.
- `Twinkles`: residual ionization stars along the fired path.
- `ImpactBurst`: directional explosion - star glare, expanding ring, fire
  puffs torn along the beam axis, stretched white-to-color spark streaks,
  tumbling debris, smoke wisps.

All calls are gated on `CombatLabSettings.MinimalFeedback` like EnemyVfx.
Pause freezes systems through Time.timeScale; no manual pause wiring needed.

### Minovsky Glow shader (Assets/Resources/VFX/MinovskyGlow.shader)

Added `_Pulses`, `_Flow`, `_PulseAmp`: traveling bright lobes stream along the
beam UV so every layer pulses in motion. Defaults keep the previous static
look for any other consumer; both beam line materials opt in.

### TYPE-08 MinovskyBeam (SEED entwined-spiral style)

- Chroma split: violet halo -> hot pink sheath -> white-pink core -> new
  overexposed pure-white filament.
- Three counter-wound helix ribbons wrap the core (pink/magenta/white-pink).
- Two traveling energy packets race muzzle-to-impact.
- Charge: inward spiraling convergence motes.
- Discharge: MuzzleBlast + large StarGlare, ImpactBurst per impact point,
  Twinkles along the path, shake .13/.14 -> .15/.16.
- Muzzle/impact lights brighter on discharge (4.5/3 -> 6.5/4.5).
- Damage values, timings, ranges and the public API are retained. Collision now queries at PlanarCombat.Height (1.1 m), independently of the visual muzzle height.

### AX-01 HalbreakerBeam (high-density razor style)

- Chroma split: indigo halo -> cyan sheath -> ice core -> new pure-white
  filament; tighter edges, halo alpha down.
- Faster denser shader lobes (8 lobes, flow 3.4) than the pink beam.
- Ion spirals 2 -> 3 strands, counter-rotating, tighter radius.
- ImpactBurst at every penetration point plus MuzzleBlast/StarGlare on
  discharge; penetration flare quads kept.
- LineRenderer count 11 -> 13, still satisfying HalbreakerQuickCheck (>=11,
  supported Minovsky Glow shader on every line).
- Damage values, timings, ranges and the public API are retained. Collision now queries at PlanarCombat.Height (1.1 m), independently of the visual muzzle height.

### Rifle bolts

- `ProjectileVisuals.SpawnMuzzleFlash` adds a scaled StarGlare.
- Player bolt wall hits add a small StarGlare; explosive bolt impacts add a
  directional ImpactBurst alongside the existing pulse.

## Standing constraints kept

- CompanyUpdateQuickCheck: every LineRenderer under MinovskyBeam uses
  "MECH ROUGE/Minovsky Glow" (unchanged shader name); count 11.
- HalbreakerQuickCheck: >=11 LineRenderers under HalbreakerBeam (now 13).
- Beam damage/timing constants unchanged, so all damage and pacing suites are
  unaffected by presentation.

## Verification

tools/p0/run_beam_vfx.py (builds Builds/BeamVfx_Gundam_R1, then foundation /
tactics / impact / regression / fullplay suites into
AuditEvidence/beam-vfx-gundam/release), promoted with
tools/p0/publish_playable.py. Launcher/current.json switches only after all
six suites pass, including the dedicated beam suite.


## Codex takeover verification (2026-09-19)

The inherited visual replay failed because AX-01 mounted at y=3.22 m was sphere-casting above normal E01 bodies. Both beam casts now use the established 1.1 m gameplay plane, while rendering from their real weapon sockets. The original floor-level targets remain unchanged; no raised test targets are used. Low 2.2 m cover is also tested to prevent high sockets bypassing gameplay cover.

The dedicated `beam` Player suite checks hangar equipment selection, both beam shaders/layer counts, floor-level damage (TYPE-08 120; AX-01 160 on each of four targets), no repeated afterglow damage, low-cover occlusion and beam termination, plus M14 live-round damage. Captures use the actual RTX 4070 Ti Player renderer. This is technical and visual inspection, not user aesthetic acceptance.

This project already has CombatBloom; the inherited claim that the built-in render pipeline precludes bloom was incorrect. The new sprite glare and layered beam shader supplement that existing effect.

All six suites must run against the final assembly. Each run records its Assembly-CSharp SHA-256, and publication verifies it. Earlier failed checks are retained. Tests use hidden windows, `-noaudio`, and isolated profiles; the permanent profile must keep its pre-takeover hash.

Final outcome: published BeamVfx_Gundam_R1 through the permanent launcher. All six final-assembly suites passed; build errors=0 warnings=0; 149 build files verified. Launcher CheckOnly and silent Verify passed (Launcher/Verification/20260919-011740). Permanent profile SHA-256 stayed 7DB4BF90BA758A4CF034DC2081969BC9DE413A41483F59C156D76BE7CDBF195D. Exact evidence and pass counts: release/takeover-summary.json.
