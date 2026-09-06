"""Build and render the authored action revision on the frozen Valkyr V3."""
import argparse,os,pathlib,subprocess,uuid
root=pathlib.Path(__file__).resolve().parents[1]
mode=argparse.ArgumentParser();mode.add_argument('mode',choices=['build','preview','check']);mode.add_argument('--effects',action='store_true');args=mode.parse_args()
out=root/'AuditEvidence'/('action-vfx' if args.effects else 'motion-v3')/('combat' if args.mode=='check' else '')
out.mkdir(parents=True,exist_ok=True)
env=os.environ.copy();env['MECH_EQUIPMENT_PROFILE']=str(out/('test-profile-'+uuid.uuid4().hex[:8]+'.json'))
if args.mode=='build':
    command=['D:/Editor/6000.3.18f1/Editor/Unity.exe','-batchmode','-quit','-projectPath',str(root),'-executeMethod','ValkyrMotionInspection.BuildActionEffectsRelease' if args.effects else 'ValkyrMotionInspection.BuildMotionRelease','-logFile',str(out/'build.log')]
else:
    command=[str(root/'Builds'/('ValkyrActionV4' if args.effects else 'ValkyrMotionV3')/'MECH_TRIAL_Valkyr.exe'),'-batchmode','-screen-width','1600','-screen-height','900','-valkyrQuickCheck' if args.mode=='check' else '-motionQuickCheck','-logFile',str(out/'player.log')]
    if args.effects:command.append('-actionVfxPreview')
si=subprocess.STARTUPINFO();si.dwFlags|=subprocess.STARTF_USESHOWWINDOW;si.wShowWindow=subprocess.SW_HIDE
raise SystemExit(subprocess.run(command,cwd=root,env=env,startupinfo=si).returncode)
