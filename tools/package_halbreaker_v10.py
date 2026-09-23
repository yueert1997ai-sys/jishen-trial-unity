import hashlib,json,shutil,zipfile
from pathlib import Path
root=Path(__file__).resolve().parents[1]
build=root/'Builds/ValkyrV10_HALBREAKER'
delivery=root.parents[1]/'deliveries/HALBREAKER_V10'
delivery.mkdir(parents=True,exist_ok=True)
report=root/'AuditEvidence/halbreaker-v10/check/quick-check.txt'
assert 'HALBREAKER_V10_CHECK code=0 errors=0' in report.read_text(encoding='utf-8')
shutil.copy2(root/'docs/HALBREAKER_V10.md',build/'README.md')
shutil.copy2(report,delivery/'verification.txt')
for name in ('01_clean_hangar.png','07_four_target_penetration.png'):
    shutil.copy2(report.parent/name,delivery/name)
files=sorted(p for p in build.rglob('*') if p.is_file())
manifest={'files':{str(p.relative_to(build)).replace('\\','/'):{'bytes':p.stat().st_size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in files},'verification':report.read_text(encoding='utf-8')}
(delivery/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
archive=delivery/'MECH_TRIAL_V10_HALBREAKER_Windows.zip'
with zipfile.ZipFile(archive,'w',zipfile.ZIP_DEFLATED,compresslevel=3) as z:
    for p in files:z.write(p,'ValkyrV10_HALBREAKER/'+str(p.relative_to(build)).replace('\\','/'))
with zipfile.ZipFile(archive) as z:
    assert z.testzip() is None
    assert len(z.infolist())==len(files)
    for p in ('MECH_TRIAL_Valkyr.exe','MECH_TRIAL_Valkyr_Data/Managed/Assembly-CSharp.dll','UnityPlayer.dll'):
        assert hashlib.sha256(z.read('ValkyrV10_HALBREAKER/'+p)).hexdigest()==manifest['files'][p]['sha256']
result={'zip':str(archive),'bytes':archive.stat().st_size,'sha256':hashlib.sha256(archive.read_bytes()).hexdigest(),'files':len(files),'zip_crc_check':'passed','runtime_key_files_match':True}
(delivery/'PACKAGE_VERIFIED.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print(json.dumps(result),flush=True)
