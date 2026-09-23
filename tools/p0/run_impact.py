"""Hidden Player probes for the three requested combat-feel changes. Never overwrites V4."""
from pathlib import Path
import datetime, hashlib, json, os, subprocess, sys
root=Path(__file__).resolve().parents[2]
mode=sys.argv[1] if len(sys.argv)>1 else 'feel'
variant=sys.argv[2] if len(sys.argv)>2 else 'After'
build=root/('Builds/Impact_R2_'+variant)
out=root/'AuditEvidence/impact-r2'/(variant.lower()+'-'+mode+'-'+datetime.datetime.now().strftime('%Y%m%d-%H%M%S'))
out.mkdir(parents=True)
env=os.environ.copy();env.update(MECH_EQUIPMENT_PROFILE=str(out/'profile.json'),MECH_LOOP_V2_BUILD=str(build),MECH_LOOP_V2_EVIDENCE=str(out),MECH_LOOP_V2_VERSION='impact-r2-'+variant.lower())
if mode=='build':
 args=['D:/Editor/6000.3.18f1/Editor/Unity.exe','-batchmode','-quit','-projectPath',str(root),'-executeMethod','CombatLoopV2Build.Build','-logFile',str(out/'unity.log')]
else:
 args=[str(build/'MECH_TRIAL_P0.exe'),'-batchmode','-noaudio','-p0Check','-combatSlice','-combatSeed','9172026','-screen-width','1600','-screen-height','900','-logFile',str(out/'player.log')]
 suites={'recovery':'-recoveryCheck','visual':'-impactPolishCheck','feel':'-meleeFeelCheck','rhythm':'-rhythmCheck','contact':'-contactPolishCheck','impact':'-enemyImpactCheck','audio':'-combatAudioCheck','sound':'-soundMixCheck','detail':'-contactPolishReplay','check':'-rebuildCheck','foundation':'-foundationCheck','break':'-armorBreakCheck','tactics':'-tacticsCheck','regression':None,'playmix':'-naturalCheck','fullplay':'-fullPlayCheck'}
 if suites[mode]:args.append(suites[mode])
 if mode=='check':args+=['-rebuildModule','13']
 if mode in ('feel','sound','detail','playmix'):args.remove('-noaudio')
 if mode=='playmix':args.append('-playMixCapture')
 if variant=='Before':args.append('-feelBaseline')
 if mode=='fullplay':args.remove('-combatSlice');args.append('-fullDemo')
startup=subprocess.STARTUPINFO();startup.dwFlags|=subprocess.STARTF_USESHOWWINDOW;startup.wShowWindow=0
assembly=build/'MECH_TRIAL_P0_Data/Managed/Assembly-CSharp.dll'
before_hash=hashlib.sha256(assembly.read_bytes()).hexdigest() if mode!='build' else None
print(out,flush=True)
result=subprocess.run(args,cwd=root,env=env,startupinfo=startup)
after_hash=hashlib.sha256(assembly.read_bytes()).hexdigest() if assembly.exists() else None
(out/'run.json').write_text(json.dumps({'args':args,'exit':result.returncode,'build':str(build),'assembly_sha256':after_hash,'assembly_before_sha256':before_hash},indent=2),encoding='utf-8')
if mode!='build' and before_hash!=after_hash:raise RuntimeError('Build changed during Player verification; rerun this suite')
print(mode,result.returncode,flush=True);sys.exit(result.returncode)
