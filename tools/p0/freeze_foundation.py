"""Freeze the actual working tree before the foundation migration, without resetting Git."""
from pathlib import Path
import hashlib, json, subprocess, zipfile

root = Path(__file__).resolve().parents[2]
out = root / 'AuditEvidence/combat-foundation-v3'
out.mkdir(parents=True, exist_ok=True)
archive = out / 'before-foundation-source.zip'
if archive.exists():
    raise SystemExit('Baseline already exists; refusing to replace it')
folders = ['Assets/Scripts', 'Assets/Editor', 'Assets/Scenes', 'Assets/Resources/ValkyrMotion',
           'ProjectSettings', 'Packages', 'docs', 'tools/p0']
files = [p for folder in folders for p in (root / folder).rglob('*') if p.is_file()]
files += [root / p for p in ['README.md', 'AGENTS.md', '.gitignore', 'Launcher/current.json']]
records = []
with zipfile.ZipFile(archive, 'w', zipfile.ZIP_DEFLATED) as z:
    for p in sorted(set(files)):
        if not p.is_file():
            continue
        data = p.read_bytes()
        name = p.relative_to(root).as_posix()
        z.writestr(name, data)
        records.append({'path': name, 'size': len(data), 'sha256': hashlib.sha256(data).hexdigest()})
for name, args in [('git-status.txt', ['status', '--short', '--branch']),
                   ('git-diff.patch', ['diff', '--binary']), ('git-head.txt', ['rev-parse', 'HEAD'])]:
    (out / name).write_bytes(subprocess.check_output(['git', '-C', str(root), *args]))
profile = root / 'Launcher/UserData/profile.json'
manifest = {'files': records, 'release': json.loads((root / 'Launcher/current.json').read_text('utf-8-sig')),
            'profileSha256': hashlib.sha256(profile.read_bytes()).hexdigest() if profile.exists() else None}
(out / 'baseline.json').write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding='utf-8')
with zipfile.ZipFile(archive) as z:
    assert z.testzip() is None
    for record in records:
        assert hashlib.sha256(z.read(record['path'])).hexdigest() == record['sha256']
source = Path('D:/BaiduNetdiskDownload/Hades.Multi.9/Content')
refs = ['Scripts/Main.lua', 'Scripts/Combat.lua', 'Scripts/CombatPresentation.lua', 'Scripts/EnemyAI.lua',
        'Scripts/RunManager.lua', 'Scripts/RoomManager.lua', 'Scripts/WeaponData.lua',
        'Scripts/EncounterData.lua', 'Scripts/RunData.lua', 'Game/Weapons/PlayerWeapons.sjson']
(out / 'reference-inventory.json').write_text(json.dumps([
    {'path': str(source / p), 'bytes': (source / p).stat().st_size,
     'sha256': hashlib.sha256((source / p).read_bytes()).hexdigest()} for p in refs
], indent=2), encoding='utf-8')
print(json.dumps({'baselineFiles': len(records), 'archiveBytes': archive.stat().st_size,
                  'release': manifest['release']['version'], 'evidence': str(out)}, indent=2))
