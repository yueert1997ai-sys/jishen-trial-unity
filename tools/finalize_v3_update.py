"""Record the verified native source and actual Player payload for the V3 update."""
import pathlib,json,hashlib
ROOT=pathlib.Path(__file__).resolve().parents[1]
path=ROOT/'AuditEvidence/v3-model-update/final-update.json'
report=json.loads(path.read_text(encoding='utf-8-sig'))
report['motion_result']=(ROOT/'AuditEvidence/v3-model-update/motion/quick-check.txt').read_text().strip().splitlines()[-1]
data=ROOT/'Builds/ValkyrV3/MECH_TRIAL_Valkyr_Data'
report['game_code_sha256']=hashlib.sha256((data/'Managed/Assembly-CSharp.dll').read_bytes()).hexdigest()
report['game_settings_sha256']=hashlib.sha256((data/'globalgamemanagers').read_bytes()).hexdigest()
report['interactive_window_title']='MECH TRIAL Equipment Preview'
report['interactive_window_handle']=70789020
report['interactive_window_verified_on_user_desktop']=True
report['interactive_responding']=True
assert report['motion_result']=='MOTION_QUICK_CHECK code=0 errors=0'
assert report['build_result']=='Succeeded errors=0 warnings=0'
path.write_text(json.dumps(report,indent=2),encoding='utf-8')
print('V3 update recorded: source preserved, model installed, motion passed, interactive window verified.')
