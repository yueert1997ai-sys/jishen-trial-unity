"""Combat Loop V2 build/check launcher. Every run gets isolated evidence and save files."""
from pathlib import Path
import os,sys,subprocess,datetime,json
root=Path(__file__).resolve().parents[2]
mode=sys.argv[1] if len(sys.argv)>1 else 'check'
base=root/os.environ.get('MECH_LOOP_V2_EVIDENCE','AuditEvidence/combat-loop-v2');base.mkdir(parents=True,exist_ok=True)
build=root/os.environ.get('MECH_LOOP_V2_BUILD','Builds/CombatLoop_V2_20260916')
out=base/(mode+'-'+datetime.datetime.now().strftime('%Y%m%d-%H%M%S'));out.mkdir()
env=os.environ.copy();env['MECH_EQUIPMENT_PROFILE']=str(out/'profile.json')
if mode=='build':
    args=['D:/Editor/6000.3.18f1/Editor/Unity.exe','-batchmode','-quit','-projectPath',str(root),'-executeMethod','CombatLoopV2Build.Build','-logFile',str(out/'unity.log')]
else:
    args=[str(build/'MECH_TRIAL_P0.exe'),'-batchmode','-noaudio','-p0Check','-screen-fullscreen','0','-screen-width','1600','-screen-height','900','-logFile',str(out/'player.log')]
    if mode=='replay':
        args.remove('-noaudio');args+=['-combatSlice','-sliceReplay','-sliceReplayC','-rhythmReplay']
    elif mode=='foundation':args+=['-combatSlice','-foundationCheck']
    elif mode=='lab':args+=['-combatSlice','-combatLabCheck']
    elif mode=='labreplay':
        args.remove('-noaudio');args+=['-combatSlice','-combatLabCheck','-combatLabReplay']
    elif mode=='tactics':args+=['-combatSlice','-tacticsCheck']
    elif mode=='compare':args+=['-combatSlice','-loopV2Playtest','-loopV2BossCompare']
    elif mode=='play':args+=['-combatSlice','-loopV2Playtest']
    elif mode=='loop':args+=['-combatSlice','-loopV2Check']
    elif mode=='check':args+=['-combatSlice','-sliceCheck']
    elif mode=='detail':
        args.remove('-noaudio');args+=['-combatSlice','-contactPolishReplay']
    elif mode=='sound':
        args.remove('-noaudio');args+=['-combatSlice','-soundMixCheck']
    elif mode=='contact':args+=['-combatSlice','-contactPolishCheck']
    elif mode=='gun':args+=['-combatSlice','-m7Check']
    elif mode=='break':args+=['-combatSlice','-armorBreakCheck']
    elif mode=='absorption':args+=['-combatSlice','-absorptionCheck']
    elif mode=='audio':args+=['-combatSlice','-combatAudioCheck']
    elif mode=='impact':args+=['-combatSlice','-enemyImpactCheck']
    elif mode=='rhythm':args+=['-combatSlice','-rhythmCheck']
    elif mode=='terrain':args+=['-p0TerrainAudit']
    elif mode!='regression':raise SystemExit('unknown mode')
startup=subprocess.STARTUPINFO();startup.dwFlags|=subprocess.STARTF_USESHOWWINDOW;startup.wShowWindow=0
print('Output:',out,flush=True)
result=subprocess.run(args,cwd=root,env=env,startupinfo=startup)
(out/'run.json').write_text(json.dumps({'args':args,'exit':result.returncode,'profile':env['MECH_EQUIPMENT_PROFILE']},indent=2),encoding='utf-8')
print(mode,'exit',result.returncode,flush=True);sys.exit(result.returncode)
