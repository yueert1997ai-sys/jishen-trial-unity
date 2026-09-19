from pathlib import Path
import json,hashlib,shutil,datetime
root=Path(__file__).resolve().parents[2]
evidence=root/'AuditEvidence/e01-elite'
build=Path((evidence/'build-path.txt').read_text().strip())
runtime=json.loads((evidence/'player/runtime-check.json').read_text())
assert runtime['passed'] and runtime['victory'] and runtime['restart']
manifest=json.loads((root/'Assets/Art/Enemies/TypeE01Elite/export_manifest.json').read_text())
files=[Path(manifest['source']),root/'art_prototypes/TYPE_E01_ELITE_20260906/game_ready/TYPE_E01_ELITE_GAME_R01.blend',Path(manifest['fbx']),root/'Assets/Prefabs/Enemies/Boss_HeavyMech.prefab',build/'MECH_TRIAL_Data/Managed/Assembly-CSharp.dll']
hashes={str(p):hashlib.sha256(p.read_bytes()).hexdigest() for p in files}
assert hashes[manifest['source']]==manifest['source_sha256']
assert hashes[manifest['fbx']]==manifest['fbx_sha256']
report={'completed_utc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'build':str(build),'preview_launcher':str(root/'挑战侵蚀Boss.lnk'),'source_unchanged':True,'runtime':runtime,'hashes':hashes,'gameplay_scope':'Existing scatter, mortar, charge and reinforcements with E01 skeletal presentation. Core exposure uses existing whole-body vulnerability window.','validation_scope':'Windows player rendering and scripted encounter exercise; subjective playfeel remains user review.'}
(evidence/'delivery.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
shutil.copy2(evidence/'player/runtime-check.json',build/'BOSS_VERIFIED.json')
shutil.copy2(root/'docs/E01_ELITE_GAME_HANDOFF.md',build/'BOSS_README.md')
shutil.copy2(evidence/'player/boss_phase2_close.png',build/'BOSS_PREVIEW.png')
(root/'PLAY_E01_BOSS.cmd').write_text('@echo off\r\ncall "%~dp0Builds\\Windows\\'+build.name+'\\PLAY_BOSS.cmd"\r\n',encoding='utf-8')
print('DELIVERY_READY',build)
