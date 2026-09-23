"""Enemy AI coordination + textured VFX round: build, hidden Player suites, packaging.
Follows the run_impact.py pattern; never overwrites the current playable build."""
from pathlib import Path
import datetime, hashlib, json, os, subprocess, sys
root=Path(__file__).resolve().parents[2]
mode=sys.argv[1] if len(sys.argv)>1 else 'foundation'
build=root/'Builds/EnemyAI_Vfx_R1'
out=root/'AuditEvidence/enemy-ai-vfx/release'
out.mkdir(parents=True,exist_ok=True)
env=os.environ.copy();env.update(MECH_LOOP_V2_BUILD=str(build),MECH_LOOP_V2_EVIDENCE=str(out),MECH_LOOP_V2_VERSION='enemy-ai-vfx-r1')
if mode=='build':
 args=['D:/Editor/6000.3.18f1/Editor/Unity.exe','-batchmode','-quit','-projectPath',str(root),'-executeMethod','CombatLoopV2Build.Build','-logFile',str(out/'unity-build.log')]
else:
 stamp=datetime.datetime.now().strftime('%Y%m%d-%H%M%S')
 suite=out/(mode+'-'+stamp)
 suite.mkdir(parents=True)
 # The check framework writes next to the profile: isolate each suite in its own dir.
 env.update(MECH_EQUIPMENT_PROFILE=str(suite/'profile.json'),MECH_LOOP_V2_EVIDENCE=str(suite))
 args=[str(build/'MECH_TRIAL_P0.exe'),'-batchmode','-noaudio','-p0Check','-combatSeed','9172026','-screen-width','1600','-screen-height','900','-logFile',str(suite/'player.log')]
 suites={'foundation':'-foundationCheck','tactics':'-tacticsCheck','impact':'-enemyImpactCheck','fullplay':None}
 if mode=='regression':args+=['-combatSlice']
 elif mode=='fullplay':args+=['-fullDemo']
 elif suites.get(mode):args+=['-combatSlice',suites[mode]]
 else:sys.exit('unknown mode '+mode)
 startup=subprocess.STARTUPINFO();startup.dwFlags|=subprocess.STARTF_USESHOWWINDOW;startup.wShowWindow=0
 out=suite
 result=subprocess.run(args,cwd=root,env=env,startupinfo=startup)
 (out/'run.json').write_text(json.dumps({'args':args,'exit':result.returncode,'build':str(build)},indent=2),encoding='utf-8')
 print(mode,result.returncode,flush=True);sys.exit(result.returncode)
result=subprocess.run(args,cwd=root,env=env)
print('build',result.returncode,flush=True);sys.exit(result.returncode)
