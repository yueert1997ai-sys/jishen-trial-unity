"""Freeze the real V3 checkout, including untracked assets, before the approved rebuild."""
from pathlib import Path
import hashlib, json, subprocess, zipfile

root=Path(__file__).resolve().parents[2]
out=root/'AuditEvidence/hades-rebuild-v4'
out.mkdir(parents=True,exist_ok=True)
archive=out/'before-v4-source.zip'
if archive.exists():raise SystemExit('Baseline already exists; refusing to overwrite it')
folders=['Assets','ProjectSettings','Packages','docs','tools','Launcher']
files=[p for folder in folders for p in (root/folder).rglob('*') if p.is_file() and '__pycache__' not in p.parts]
files.extend(p for p in root.iterdir() if p.is_file() and p.suffix.lower() in ('.md','.json','.cmd','.ps1'))
files.append(root/'.gitignore')
records=[]
with zipfile.ZipFile(archive,'w',zipfile.ZIP_DEFLATED,compresslevel=3) as z:
    for p in sorted(set(files)):
        if not p.is_file():continue
        data=p.read_bytes();name=p.relative_to(root).as_posix()
        z.writestr(name,data);records.append({'path':name,'size':len(data),'sha256':hashlib.sha256(data).hexdigest()})
for name,args in [('git-status.txt',['status','--short','--branch']),('git-diff.patch',['diff','--binary']),('git-head.txt',['rev-parse','HEAD'])]:
    (out/name).write_bytes(subprocess.check_output(['git','-C',str(root),*args]))
profile=root/'Launcher/UserData/profile.json'
manifest={'files':records,'release':json.loads((root/'Launcher/current.json').read_text('utf-8-sig')),
          'profileSha256':hashlib.sha256(profile.read_bytes()).hexdigest() if profile.exists() else None}
(out/'baseline.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
with zipfile.ZipFile(archive) as z:
    assert z.testzip() is None
    for r in records:assert hashlib.sha256(z.read(r['path'])).hexdigest()==r['sha256']
print(json.dumps({'files':len(records),'bytes':archive.stat().st_size,'release':manifest['release']['version']},indent=2))
