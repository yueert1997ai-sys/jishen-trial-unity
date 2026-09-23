"""Build/replay the final shoulder cannon and quiet hangar in the latest playable checkout."""
import argparse,os,subprocess,uuid
from pathlib import Path
root=Path(__file__).resolve().parents[1]
p=argparse.ArgumentParser();p.add_argument('mode',choices=['build','check']);args=p.parse_args()
build=root/'Builds/ValkyrV10_HALBREAKER';evidence=root/'AuditEvidence/halbreaker-v10'/args.mode
evidence.mkdir(parents=True,exist_ok=True)
env=os.environ.copy();temp=Path('D:/Tools/BlenderUserData/Temp/halbreaker-unity');temp.mkdir(parents=True,exist_ok=True)
env['TEMP']=env['TMP']=str(temp)
log=evidence/('unity.log' if args.mode=='build' else 'player.log')
if args.mode=='build':
    cmd=['D:/Editor/6000.3.18f1/Editor/Unity.exe','-batchmode','-quit','-projectPath',str(root),'-executeMethod','HalbreakerIntegration.ConfigureAndBuild','-companyBuildPath',str(build),'-logFile',str(log)]
else:
    env['MECH_EQUIPMENT_PROFILE']=str(evidence/('test-profile-'+uuid.uuid4().hex[:8]+'.json'))
    cmd=[str(build/'MECH_TRIAL_Valkyr.exe'),'-batchmode','-screen-fullscreen','0','-screen-width','1600','-screen-height','900','-halbreakerCheck','-logFile',str(log)]
startup=subprocess.STARTUPINFO();startup.dwFlags|=subprocess.STARTF_USESHOWWINDOW;startup.wShowWindow=subprocess.SW_HIDE
result=subprocess.run(cmd,cwd=root,env=env,startupinfo=startup)
print(f'{args.mode} exit={result.returncode} log={log}',flush=True)
if log.exists():
    print('\n'.join(s for s in log.read_text(encoding='utf-8',errors='replace').splitlines() if any(x in s for x in ['_PASS','_FAIL','error CS','Exception:','Shader error','Build completed']))[-5000:])
raise SystemExit(result.returncode)
