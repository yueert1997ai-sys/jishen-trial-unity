import os,sys,subprocess,uuid
from pathlib import Path
root=Path(__file__).resolve().parents[2]
mode=sys.argv[1] if len(sys.argv)>1 else 'build'
out=root/'AuditEvidence/p0-9-clear'/mode;out.mkdir(parents=True,exist_ok=True)
env=os.environ.copy();env['MECH_EQUIPMENT_PROFILE']=str(out/('profile-'+uuid.uuid4().hex[:8]+'.json'))
if mode=='build' or mode=='nav':
 cmd=['D:/Editor/6000.3.18f1/Editor/Unity.exe','-batchmode','-quit','-projectPath',str(root),'-executeMethod','P0CombatBuild.Build','-logFile',str(out/'unity.log')]
 if mode=='nav':cmd[cmd.index('P0CombatBuild.Build')]='P0LunarNavBuild.Rebuild'
else:
 cmd=[str(root/'Builds/P0_9_Clear/MECH_TRIAL_P0.exe'),'-batchmode','-noaudio','-p0Check','-screen-fullscreen','0','-screen-width','1600','-screen-height','900','-logFile',str(out/'player.log')]
 if mode=='terrain':cmd+=['-p0TerrainAudit']
 if mode=='visual':cmd+=['-p0VisualCheck']
startup=subprocess.STARTUPINFO();startup.dwFlags|=subprocess.STARTF_USESHOWWINDOW;startup.wShowWindow=subprocess.SW_HIDE
result=subprocess.run(cmd,cwd=root,env=env,startupinfo=startup)
print(f'{mode} exit={result.returncode} output={out}',flush=True)
sys.exit(result.returncode)
