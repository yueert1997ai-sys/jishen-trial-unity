"""Build or replay the V7 Windows Player with an isolated equipment profile."""
import argparse, os, pathlib, subprocess, uuid
root=pathlib.Path(__file__).resolve().parents[1]
p=argparse.ArgumentParser();p.add_argument('mode',choices=['build','rebuild','film']);args=p.parse_args()
out=root/'AuditEvidence/power-combo-v7'/args.mode;out.mkdir(parents=True,exist_ok=True)
env=os.environ.copy();env['MECH_EQUIPMENT_PROFILE']=str(out/('test-profile-'+uuid.uuid4().hex[:8]+'.json'))
if args.mode in ('build','rebuild'):
    command=['D:/Editor/6000.3.18f1/Editor/Unity.exe','-batchmode','-quit','-projectPath',str(root),'-executeMethod','PowerComboIntegration.'+('PrepareAndBuild' if args.mode=='build' else 'Build'),'-logFile',str(out/'unity.log')]
else:
    command=[str(root/'Builds/ValkyrPowerComboV7/MECH_TRIAL_Valkyr.exe'),'-batchmode','-screen-width','1600','-screen-height','900','-reverseComboCheck','-powerComboCheck','-comboFilm','-logFile',str(out/'player.log')]
si=subprocess.STARTUPINFO();si.dwFlags|=subprocess.STARTF_USESHOWWINDOW;si.wShowWindow=subprocess.SW_HIDE
raise SystemExit(subprocess.run(command,cwd=root,env=env,startupinfo=si).returncode)
