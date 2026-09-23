"""Collect the latest passing evidence for one exact V4 Player. Never promotes a build."""
from pathlib import Path
import array, hashlib, json, math, os, shutil, wave

root = Path(__file__).resolve().parents[2]
base = root / 'AuditEvidence/hades-rebuild-v4'
release = base / 'release'
release.mkdir(exist_ok=True)
build = root / 'Builds/HadesRebuild_V4_M13'
modes = 'check regression foundation rhythm contact impact gun break absorption loop tactics lab audio sound flow terrain slice detail replay playmix fullplay'.split()

def sha(path):
    h = hashlib.sha256()
    with path.open('rb') as stream:
        for data in iter(lambda: stream.read(4 * 1024 * 1024), b''):
            h.update(data)
    return h.hexdigest()

def save(path, data):
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding='utf-8')

def manifest(folder):
    return [{'path': p.relative_to(folder).as_posix(), 'size': p.stat().st_size, 'sha256': sha(p)}
            for p in sorted(folder.rglob('*')) if p.is_file() and 'UserData' not in p.relative_to(folder).parts]

def link(source, destination):
    # Immutable evidence on the same D: volume; avoid duplicating thousands of frame captures.
    try:
        os.link(source, destination)
    except OSError:
        shutil.copy2(source, destination)

assembly = sha(build / 'MECH_TRIAL_P0_Data/Managed/Assembly-CSharp.dll')
build_proof = sorted(base.glob('m13-build-*'))[-1]
build_result = (build_proof / 'build-result.txt').read_text(encoding='utf-8-sig')
assert build_result.startswith('Succeeded errors=0 warnings=0'), build_result
shutil.copy2(build_proof / 'build-result.txt', release / 'build-result.txt')
records = manifest(build)
save(release / 'build-manifest.json', records)
tests = []
selected = {}
for mode in modes:
    original = sorted(base.glob('m13-' + mode + '-*'))[-1]
    report = (original / 'quick-check.txt').read_text(encoding='utf-8-sig')
    run = json.loads((original / 'run.json').read_text(encoding='utf-8-sig'))
    assert 'P0_CHECK code=0 errors=0' in report and run['exit'] == 0, original
    assert run['assembly_sha256'] == assembly, f'Stale assembly: {original}'
    assert Path(run['args'][0]).resolve() == build / 'MECH_TRIAL_P0.exe', original
    target = release / original.name.removeprefix('m13-')
    if not target.exists():
        shutil.copytree(original, target, copy_function=link)
    tests.append({'mode': mode, 'passCount': sum(line.startswith('PASS ') for line in report.splitlines()),
                  'original': original.relative_to(root).as_posix(), 'release': target.relative_to(root).as_posix(),
                  'reportSha256': sha(original / 'quick-check.txt'), 'assemblySha256': assembly, 'exit': run['exit']})
    selected[mode] = original

mixes = []
for mode in ('playmix', 'sound', 'detail', 'replay'):
    for p in sorted(selected[mode].rglob('*.wav')):
        with wave.open(str(p), 'rb') as stream:
            assert stream.getsampwidth() == 2, p
            channels, rate, frames = stream.getnchannels(), stream.getframerate(), stream.getnframes()
            samples = array.array('h', stream.readframes(frames))
        peak = max((abs(v) for v in samples), default=0) / 32768
        rms = math.sqrt(sum(v * v for v in samples) / max(1, len(samples))) / 32768
        clipped = sum(v >= 32767 or v <= -32768 for v in samples)
        mixes.append({'path': p.relative_to(root).as_posix(), 'sha256': sha(p), 'seconds': frames / rate,
                      'channels': channels, 'sampleRate': rate, 'peak': peak, 'rmsDbfs': 20 * math.log10(max(1e-12, rms)),
                      'clippedSamples': clipped, 'sampleCount': len(samples)})
        assert clipped == 0, f'Clipped mix: {p}'
save(release / 'audio-readback.json', mixes)

summary = {'version': build.name, 'build': build.relative_to(root).as_posix(), 'buildResult': build_result,
           'assemblySha256': assembly, 'verifiedFiles': len(records), 'buildBytes': sum(r['size'] for r in records),
           'suiteCount': len(tests), 'passCount': sum(t['passCount'] for t in tests), 'tests': tests,
           'baseCombat': (selected['playmix'] / 'natural-results.txt').read_text(encoding='utf-8-sig'),
           'fullRun': (selected['fullplay'] / 'natural-results.txt').read_text(encoding='utf-8-sig'),
           'encounterSeconds': json.loads((selected['fullplay'] / 'encounter-seconds.json').read_text()),
           'audioFiles': len(mixes), 'clippedSamples': sum(m['clippedSamples'] for m in mixes),
           'humanFeelApproved': False, 'humanAudioApproved': False, 'firstHumanRunDurationVerified': False}
save(release / 'test-summary.json', summary)

modules = base / 'module-artifacts'
modules.mkdir(exist_ok=True)
index = []
for number in range(1, 14):
    folder = root / f'Builds/HadesRebuild_V4_M{number:02d}'
    proof = sorted(base.glob(f'm{number:02d}-build-*'))[-1]
    check = sorted(base.glob(f'm{number:02d}-check-*'))[-1]
    report = (check / 'quick-check.txt').read_text(encoding='utf-8-sig')
    assert 'P0_CHECK code=0 errors=0' in report, check
    records = manifest(folder)
    destination = modules / f'M{number:02d}'
    destination.mkdir(exist_ok=True)
    save(destination / 'build-manifest.json', records)
    run = json.loads((check / 'run.json').read_text(encoding='utf-8-sig'))
    actual_assembly = sha(folder / 'MECH_TRIAL_P0_Data/Managed/Assembly-CSharp.dll')
    if run.get('assembly_sha256'):
        assert run['assembly_sha256'] == actual_assembly, check
    else:
        assert check.name.split('-check-')[1] >= proof.name.split('-build-')[1], 'Check predates last module build'
    index.append({'module': number, 'build': folder.relative_to(root).as_posix(), 'files': len(records),
                  'assemblySha256': actual_assembly, 'recordedTestAssemblyMatched': True if run.get('assembly_sha256') else None,
                  'buildProof': proof.relative_to(root).as_posix(), 'buildResult': (proof / 'build-result.txt').read_text(),
                  'checks': check.relative_to(root).as_posix(), 'passCount': sum(x.startswith('PASS ') for x in report.splitlines()),
                  'screenshots': [p.relative_to(root).as_posix() for p in sorted(check.glob('*.png'))]})
save(modules / 'module-index.json', index)
print(json.dumps({k: summary[k] for k in ('version', 'assemblySha256', 'verifiedFiles', 'suiteCount', 'passCount', 'audioFiles', 'clippedSamples')}, indent=2))
