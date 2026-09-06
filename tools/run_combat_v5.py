"""Build/preview the V5 game with an isolated equipment profile."""
import argparse, os, pathlib, subprocess, uuid
root=pathlib.Path(__file__).resolve().parents[1]
p=argparse.ArgumentParser();p.add_argument('mode',choices=['integrate','build','check','preview']);args=p.parse_args()
out=root/'AuditEvidence/combat-v5'/args.mode;out.mkdir(parents=True,exist_ok=True)
env=os.environ.copy();env['MECH_EQUIPMENT_PROFILE']=str(out/('test-profile-'+uuid.uuid4().hex[:8]+'.json'))
if args.mode in ('integrate','build'):
    command=['D:/Editor/6000.3.18f1/Editor/Unity.exe','-batchmode','-quit','-projectPath',str(root),'-executeMethod','E01CombatIntegration.'+('Integrate' if args.mode=='integrate' else 'Build'),'-logFile',str(out/'unity.log')]
else:
    command=[str(root/'Builds/ValkyrCombatV5/MECH_TRIAL_Valkyr.exe'),'-batchmode','-screen-width','1600','-screen-height','900','-combatV5Check','-logFile',str(out/'player.log')]
    if args.mode=='preview':command.append('-combatV5Preview')
si=subprocess.STARTUPINFO();si.dwFlags|=subprocess.STARTF_USESHOWWINDOW;si.wShowWindow=subprocess.SW_HIDE
raise SystemExit(subprocess.run(command,cwd=root,env=env,startupinfo=si).returncode)
