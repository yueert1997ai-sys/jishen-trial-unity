"""Promote a verified build without changing the permanent desktop shortcut."""
from pathlib import Path
import argparse, datetime, hashlib, json, shutil, os

root = Path(__file__).resolve().parents[2]
parser = argparse.ArgumentParser()
parser.add_argument('--build', required=True)
parser.add_argument('--evidence', required=True)
parser.add_argument('--require', nargs='+', required=True)
parser.add_argument('--check-only', action='store_true')
args = parser.parse_args()
# Entries stay inside the project's named containers. Directory junctions can
# place large build/evidence payloads on another local volume; verify the resolved
# payloads but retain stable project-relative entries in the launcher manifest.
build_entry = Path(os.path.abspath(root / args.build))
evidence_entry = Path(os.path.abspath(root / args.evidence))
assert build_entry.is_relative_to(root / 'Builds'), 'Build entry must be inside project Builds'
assert evidence_entry.is_relative_to(root / 'AuditEvidence'), 'Evidence entry must be inside project AuditEvidence'
build = build_entry.resolve()
evidence = evidence_entry.resolve()
assert (build / 'MECH_TRIAL_P0.exe').is_file(), 'Missing executable'
assert (build / 'MECH_TRIAL_P0_Data/Managed/Assembly-CSharp.dll').is_file(), 'Missing game assembly'
assert (evidence / 'build-result.txt').read_text(encoding='utf-8-sig').startswith('Succeeded errors=0'), 'Build failed'
manifest = json.loads((evidence / 'build-manifest.json').read_text(encoding='utf-8-sig'))
listed = [record['path'] for record in manifest if 'UserData' not in Path(record['path']).parts]
actual = {path.relative_to(build).as_posix() for path in build.rglob('*') if path.is_file() and 'UserData' not in path.relative_to(build).parts}
assert listed and len(listed) == len(set(listed)) and set(listed) == actual, 'Manifest must cover every build file exactly once'
checked = 0
for record in manifest:
    path = (build / record['path']).resolve()
    assert path.is_relative_to(build), 'Invalid manifest path'
    if 'UserData' in path.relative_to(build).parts:
        continue
    if 'size' in record:
        assert path.stat().st_size == record['size'], f'Build size changed: {path}'
    assert hashlib.sha256(path.read_bytes()).hexdigest() == record['sha256'], f'Build changed after verification: {path}'
    checked += 1
tests = {}
for mode in args.require:
    candidates = sorted(p for p in evidence.glob(mode + '-*') if p.is_dir())
    assert candidates, f'Missing {mode} test'
    latest = candidates[-1]
    assert 'P0_CHECK code=0 errors=0' in (latest / 'quick-check.txt').read_text(encoding='utf-8-sig'), f'Latest {mode} did not pass'
    run = json.loads((latest / 'run.json').read_text(encoding='utf-8-sig'))
    assert run['exit'] == 0 and Path(run['args'][0]).resolve() == build / 'MECH_TRIAL_P0.exe', f'Wrong tested build for {mode}'
    if 'assembly_sha256' in run:
        assert run['assembly_sha256'] == hashlib.sha256((build / 'MECH_TRIAL_P0_Data/Managed/Assembly-CSharp.dll').read_bytes()).hexdigest(), f'Tested assembly changed for {mode}'
    tests[mode] = (evidence_entry / latest.relative_to(evidence)).relative_to(root).as_posix()
release = {'version': build_entry.name, 'build': build_entry.relative_to(root).as_posix(),
           'verifiedFiles': checked, 'tests': tests,
           'publishedAt': datetime.datetime.now().astimezone().isoformat()}
if not args.check_only:
    launcher = root / 'Launcher'
    launcher.mkdir(exist_ok=True)
    current = launcher / 'current.json'
    if current.exists():
        backup = launcher / 'History'
        backup.mkdir(exist_ok=True)
        shutil.copy2(current, backup / (datetime.datetime.now().strftime('%Y%m%d-%H%M%S-%f') + '.json'))
    profile = launcher / 'UserData/profile.json'
    old_profile = build / 'UserData/profile.json'
    if not profile.exists() and old_profile.is_file():
        profile.parent.mkdir(exist_ok=True)
        shutil.copy2(old_profile, profile)
    temporary = launcher / 'current.pending.json'
    temporary.write_text(json.dumps(release, ensure_ascii=False, indent=2), encoding='utf-8')
    temporary.replace(current)
print(json.dumps(release, ensure_ascii=False, indent=2))
