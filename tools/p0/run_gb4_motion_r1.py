from pathlib import Path
import os, sys, subprocess, datetime, hashlib, json

root = Path(__file__).resolve().parents[2]
mode = sys.argv[1] if len(sys.argv) > 1 else 'vfx'
baseline = '--baseline' in sys.argv
build = root / ('Builds/Arena_Tactics_R1' if baseline else 'Builds/GB4Motion_R1')
candidate = '--candidate' in sys.argv
if candidate: build = root / 'Builds/Lunar_Basin_R1'
evidence = root / 'AuditEvidence/gb4-motion-r1' / ('candidate' if candidate else 'baseline' if baseline else 'release')
evidence.mkdir(parents=True, exist_ok=True)
env = os.environ.copy()
env.update(MECH_LOOP_V2_BUILD=str(build), MECH_LOOP_V2_EVIDENCE=str(evidence), MECH_LOOP_V2_VERSION='gb4-motion-r1')
if mode == 'build':
    if baseline or candidate:
        raise RuntimeError('This runner builds only GB4Motion_R1; baseline and map candidate builds remain retained.')
    out = evidence
    args = ['D:/Editor/6000.3.18f1/Editor/Unity.exe', '-batchmode', '-quit', '-projectPath', str(root), '-executeMethod', 'CombatLoopV2Build.Build', '-logFile', str(out/'unity-build.log')]
else:
    out = evidence / (mode + '-' + datetime.datetime.now().strftime('%Y%m%d-%H%M%S'))
    out.mkdir()
    env.update(MECH_EQUIPMENT_PROFILE=str(out/'profile.json'), MECH_LOOP_V2_EVIDENCE=str(out))
    args = [str(build/'MECH_TRIAL_P0.exe'), '-batchmode', '-noaudio', '-p0Check', '-combatSeed', '9172026', '-screen-width', '1600', '-screen-height', '900', '-logFile', str(out/'player.log')]
    flags = {'imported':'-importedMotionCheck','aiui':'-aiUiCheck', 'lunar':'-lunarBasinCheck', 'arena':'-arenaTacticsCheck', 'terrain':'-p0TerrainAudit', 'punch':'-combatPunchCheck', 'punchperf':'-combatPunchPerformance', 'vfx':'-nemesisDroneVfxCheck', 'nemesis':'-nemesisCheck', 'foundation':'-foundationCheck', 'tactics':'-tacticsCheck', 'impact':'-enemyImpactCheck', 'melee':'-meleeFeelCheck', 'beam':'-beamVfxCheck', 'performance':'-nemesisPerformance', 'drones':'-nemesisDronesCheck', 'heroes':'-heroSelectionCheck', 'loopv2':'-loopV2Check'}
    if baseline and mode == 'arena':
        raise RuntimeError('The retained baseline predates the arena suite.')
    if mode in flags:
        args += ['-combatSlice', flags[mode]]
    elif mode == 'regression': args += ['-combatSlice']
    elif mode == 'fullplay': args += ['-fullDemo']
    elif mode == 'natural': args += ['-combatSlice', '-naturalCheck']
    elif mode == 'fullrun': args += ['-fullDemo', '-fullPlayCheck']
    else: raise ValueError(mode)
    if mode == 'beam':
        args.remove('-p0Check'); args.remove('-combatSlice')
startup = subprocess.STARTUPINFO()
startup.dwFlags |= subprocess.STARTF_USESHOWWINDOW
startup.wShowWindow = 0
result = subprocess.run(args, cwd=root, env=env, startupinfo=startup)
record = {'args':args, 'exit':result.returncode, 'build':str(build)}
if mode != 'build':
    record['assembly_sha256'] = hashlib.sha256((build/'MECH_TRIAL_P0_Data/Managed/Assembly-CSharp.dll').read_bytes()).hexdigest()
(out/'run.json').write_text(json.dumps(record, indent=2), encoding='utf8')
print(mode, result.returncode, str(out), flush=True)
if mode == 'build' and result.returncode == 0:
    manifest = [dict(path=p.relative_to(build).as_posix(), size=p.stat().st_size, sha256=hashlib.sha256(p.read_bytes()).hexdigest()) for p in build.rglob('*') if p.is_file() and 'UserData' not in p.parts]
    (out/'build-manifest.json').write_text(json.dumps(manifest, indent=2), encoding='utf8')
sys.exit(result.returncode)

