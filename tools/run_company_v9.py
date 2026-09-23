"""Build or quickly replay the integrated company update using local Unity."""
import argparse, pathlib, subprocess, os, uuid
root=pathlib.Path(__file__).resolve().parents[1]
p=argparse.ArgumentParser();p.add_argument('mode',choices=['integrate','build','check']);args=p.parse_args()
out=root/'AuditEvidence/company-update-v9'/args.mode;out.mkdir(parents=True,exist_ok=True)
env=os.environ.copy();env['MECH_EQUIPMENT_PROFILE']=str(out/('test-profile-'+uuid.uuid4().hex[:8]+'.json'))
if args.mode=='check':
    command=[str(root/'Builds/ValkyrHangarV9/MECH_TRIAL_Valkyr.exe'),'-batchmode','-screen-width','1600','-screen-height','900','-companyCheck','-logFile',str(out/'player.log')]
else:
    command=['D:/Editor/6000.3.18f1/Editor/Unity.exe','-batchmode','-quit','-projectPath',str(root),'-executeMethod','CompanyUpdateIntegration.'+('IntegrateAndBuild' if args.mode=='integrate' else 'Build'),'-logFile',str(out/'unity.log')]
startup=subprocess.STARTUPINFO();startup.dwFlags|=subprocess.STARTF_USESHOWWINDOW;startup.wShowWindow=subprocess.SW_HIDE
raise SystemExit(subprocess.run(command,cwd=root,env=env,startupinfo=startup).returncode)
