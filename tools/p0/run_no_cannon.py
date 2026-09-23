import subprocess,os,sys,uuid
from pathlib import Path
root=Path.cwd();mode=sys.argv[1] if len(sys.argv)>1 else 'm7'
out=root/'AuditEvidence/no-shoulder-cannon'/mode;out.mkdir(exist_ok=True)
env=os.environ.copy();env['MECH_EQUIPMENT_PROFILE']=str(out/('profile-'+uuid.uuid4().hex[:8]+'.json'))
startup=subprocess.STARTUPINFO();startup.dwFlags|=subprocess.STARTF_USESHOWWINDOW;startup.wShowWindow=0
args=[str(root/'Builds/P0_10_1_NoCannon/MECH_TRIAL_P0.exe'),'-batchmode','-noaudio','-p0Check','-screen-fullscreen','0','-screen-width','1600','-screen-height','900','-logFile',str(out/'player.log')]
if mode=='m7':args+=['-m7Check']
p=subprocess.run(args,env=env,startupinfo=startup);print(mode,'exit',p.returncode);sys.exit(p.returncode)
