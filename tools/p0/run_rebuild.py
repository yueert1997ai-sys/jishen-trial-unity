"""Versioned, hidden Player builds/checks for every rebuild module. No launcher promotion here."""
from pathlib import Path
import os,sys,subprocess,datetime,json,hashlib
root=Path(__file__).resolve().parents[2]
mode=sys.argv[1] if len(sys.argv)>1 else 'check'
module=int(sys.argv[2]) if len(sys.argv)>2 else 13
label=f'HadesRebuild_V4_M{module:02d}'
base=root/'AuditEvidence/hades-rebuild-v4';base.mkdir(parents=True,exist_ok=True)
out=base/(f'm{module:02d}-{mode}-'+datetime.datetime.now().strftime('%Y%m%d-%H%M%S'));out.mkdir()
build=root/'Builds'/label
env=os.environ.copy();env.update(MECH_EQUIPMENT_PROFILE=str(out/'profile.json'),MECH_LOOP_V2_BUILD=str(build),MECH_LOOP_V2_EVIDENCE=str(out),MECH_LOOP_V2_VERSION=f'hades-rebuild-v4-m{module:02d}')
if mode=='build':
    args=['D:/Editor/6000.3.18f1/Editor/Unity.exe','-batchmode','-quit','-projectPath',str(root),'-executeMethod','CombatLoopV2Build.Build','-logFile',str(out/'unity.log')]
else:
    args=[str(build/'MECH_TRIAL_P0.exe'),'-batchmode','-noaudio','-p0Check','-combatSlice','-rebuildCheck','-rebuildModule',str(module),'-combatSeed','9172026','-screen-fullscreen','0','-screen-width','1600','-screen-height','900','-logFile',str(out/'player.log')]
    suites={'sound':'-soundMixCheck','audio':'-combatAudioCheck','foundation':'-foundationCheck','rhythm':'-rhythmCheck','contact':'-contactPolishCheck','impact':'-enemyImpactCheck','gun':'-m7Check','absorption':'-absorptionCheck','break':'-armorBreakCheck','loop':'-loopV2Check','tactics':'-tacticsCheck','lab':'-combatLabCheck','terrain':'-p0TerrainAudit','slice':'-sliceCheck','detail':'-contactPolishReplay','replay':'-sliceReplay','regression':None}
    if mode in suites:
        args.remove('-rebuildCheck')
        if suites[mode]:args.append(suites[mode])
        if mode in ('sound','detail','replay'):args.remove('-noaudio')
        if mode=='replay':args.append('-sliceReplayC')
    if mode in ('play','playmix','fullplay'):
        args.remove('-rebuildCheck');args+=['-fullPlayCheck' if mode=='fullplay' else '-naturalCheck']
        if mode=='fullplay':args.remove('-combatSlice');args.append('-fullDemo')
        if mode=='playmix':args.remove('-noaudio');args.append('-playMixCapture')
    if mode=='flow':
        args.remove('-rebuildCheck');args.remove('-combatSlice');args+=['-fullDemo','-fullRebuildCheck']
startup=subprocess.STARTUPINFO();startup.dwFlags|=subprocess.STARTF_USESHOWWINDOW;startup.wShowWindow=0
print('Output:',out,flush=True)
result=subprocess.run(args,cwd=root,env=env,startupinfo=startup)
(out/'run.json').write_text(json.dumps({'args':args,'exit':result.returncode,'profile':env['MECH_EQUIPMENT_PROFILE'],'module':module,'build':str(build),'assembly_sha256':hashlib.sha256((build/'MECH_TRIAL_P0_Data/Managed/Assembly-CSharp.dll').read_bytes()).hexdigest() if (build/'MECH_TRIAL_P0_Data/Managed/Assembly-CSharp.dll').exists() else None},indent=2),encoding='utf-8')
print(mode,'exit',result.returncode,flush=True)
sys.exit(result.returncode)
