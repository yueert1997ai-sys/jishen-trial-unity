"""R2 build/check launcher. Every run gets isolated evidence and save files."""
from pathlib import Path
import os,sys,subprocess,datetime,json
root=Path(__file__).resolve().parents[2]
mode=sys.argv[1] if len(sys.argv)>1 else 'check'
base=root/'AuditEvidence/combat-slice-r2';base.mkdir(parents=True,exist_ok=True)
out=base/(mode+'-'+datetime.datetime.now().strftime('%Y%m%d-%H%M%S'));out.mkdir()
env=os.environ.copy();env['MECH_EQUIPMENT_PROFILE']=str(out/'profile.json')
if mode=='build':
    args=['D:/Editor/6000.3.18f1/Editor/Unity.exe','-batchmode','-quit','-projectPath',str(root),'-executeMethod','CombatSliceR2Build.Build','-logFile',str(out/'unity.log')]
else:
    args=[str(root/'Builds/CombatSlice_R2_20260914/MECH_TRIAL_P0.exe'),'-batchmode','-noaudio','-p0Check','-screen-fullscreen','0','-screen-width','1600','-screen-height','900','-logFile',str(out/'player.log')]
    if mode=='replay':
        args.remove('-noaudio');args+=['-combatSlice','-sliceReplay']
    elif mode=='check':args+=['-combatSlice','-sliceCheck']
    elif mode=='terrain':args+=['-p0TerrainAudit']
    elif mode!='regression':raise SystemExit('unknown mode')
startup=subprocess.STARTUPINFO();startup.dwFlags|=subprocess.STARTF_USESHOWWINDOW;startup.wShowWindow=0
print('Output:',out,flush=True)
result=subprocess.run(args,cwd=root,env=env,startupinfo=startup)
(out/'run.json').write_text(json.dumps({'args':args,'exit':result.returncode,'profile':env['MECH_EQUIPMENT_PROFILE']},indent=2),encoding='utf-8')
print(mode,'exit',result.returncode,flush=True);sys.exit(result.returncode)
