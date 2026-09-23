"""Snapshot V4 and diff against the frozen dirty worktree, preserving unrelated prior edits."""
from pathlib import Path
import difflib, hashlib, json, subprocess, zipfile

root = Path(__file__).resolve().parents[2]
base = root / 'AuditEvidence/hades-rebuild-v4'
out = base / 'release'
assert (out / 'test-summary.json').is_file(), 'Package verified evidence first'
baseline = json.loads((base / 'baseline.json').read_text(encoding='utf-8-sig'))
profile = root / 'Launcher/UserData/profile.json'
assert hashlib.sha256(profile.read_bytes()).hexdigest() == baseline['profileSha256'], 'Permanent profile changed'
before = {record['path']: record for record in baseline['files']}
folders = ['Assets', 'ProjectSettings', 'Packages', 'docs', 'tools', 'Launcher']
files = [p for name in folders for p in (root / name).rglob('*') if p.is_file() and '__pycache__' not in p.parts]
files.extend(p for p in root.iterdir() if p.is_file() and p.suffix.lower() in ('.md', '.json', '.cmd', '.ps1'))
files.append(root / '.gitignore')
after = {}
changes = []
patches = []
archive = out / 'final-source.zip'
with zipfile.ZipFile(base / 'before-v4-source.zip') as old, zipfile.ZipFile(archive, 'w', zipfile.ZIP_DEFLATED, compresslevel=3) as new:
    for p in sorted(set(files)):
        if not p.is_file():
            continue
        data = p.read_bytes()
        name = p.relative_to(root).as_posix()
        record = {'path': name, 'size': len(data), 'sha256': hashlib.sha256(data).hexdigest()}
        after[name] = record
        new.writestr(name, data)
        prior = before.get(name)
        if prior is None or prior['sha256'] != record['sha256']:
            changes.append({'path': name, 'status': 'added' if prior is None else 'modified',
                            'beforeSha256': prior['sha256'] if prior else None, 'afterSha256': record['sha256']})
            try:
                previous = old.read(name).decode('utf-8-sig') if prior else ''
                current = data.decode('utf-8-sig')
                patches.extend(difflib.unified_diff(previous.splitlines(True), current.splitlines(True), 'before/' + name, 'after/' + name))
            except (UnicodeDecodeError, KeyError):
                pass
    for name, record in before.items():
        if name not in after:
            changes.append({'path': name, 'status': 'missing', 'beforeSha256': record['sha256']})
assert not any(r['status'] == 'missing' for r in changes), 'Baseline file missing'
protected = [name for name in before if name.startswith('Assets/')
             and not name.startswith(('Assets/Scripts/', 'Assets/Editor/', 'Assets/Resources/Foundation/'))]
assert all(before[name]['sha256'] == after[name]['sha256'] for name in protected), 'Art or scene baseline changed'
with zipfile.ZipFile(archive) as check:
    assert check.testzip() is None
    for name, record in after.items():
        assert hashlib.sha256(check.read(name)).hexdigest() == record['sha256'], name
for filename, data in [('source-changes.json', changes), ('source-manifest.json', list(after.values())),
                       ('preservation.json', {'baselineFiles': len(before), 'finalFiles': len(after), 'changedOrAdded': len(changes),
                         'preservedArtAndSceneFiles': len(protected), 'permanentProfileSha256': baseline['profileSha256'],
                         'finalArchiveSha256': hashlib.sha256(archive.read_bytes()).hexdigest(), 'archiveBytes': archive.stat().st_size,
                         'comparisonBase': 'Frozen actual V3 working tree, including prior uncommitted files; not Git HEAD'})]:
    (out / filename).write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding='utf-8')
(out / 'source-delta.patch').write_text(''.join(patches), encoding='utf-8')
(out / 'git-status-final.txt').write_bytes(subprocess.check_output(['git', '-C', str(root), 'status', '--short', '--branch']))
print((out / 'preservation.json').read_text(encoding='utf-8'))
