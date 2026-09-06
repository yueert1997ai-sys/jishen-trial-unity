"""Build/replay the current V7 player plus the E-01 liquid boss and homepage entry."""
import argparse
import os
import pathlib
import subprocess
import uuid

root = pathlib.Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser()
parser.add_argument('mode', choices=['build', 'rebuild', 'check'])
args = parser.parse_args()
out = root / 'AuditEvidence/liquid-boss-v8' / args.mode
out.mkdir(parents=True, exist_ok=True)
env = os.environ.copy()
env['MECH_EQUIPMENT_PROFILE'] = str(out / ('test-profile-' + uuid.uuid4().hex[:8] + '.json'))
if args.mode in ('build', 'rebuild'):
    command = ['D:/Editor/6000.3.18f1/Editor/Unity.exe', '-batchmode', '-quit',
               '-projectPath', str(root), '-executeMethod',
               'LiquidBossIntegration.' + ('IntegrateAndBuild' if args.mode == 'build' else 'Build'),
               '-logFile', str(out / 'unity.log')]
else:
    command = [str(root / 'Builds/ValkyrLiquidBossV8/MECH_TRIAL_Valkyr.exe'),
               '-batchmode', '-screen-width', '1600', '-screen-height', '900',
               '-liquidBossCheck', '-logFile', str(out / 'player.log')]
startup = subprocess.STARTUPINFO()
startup.dwFlags |= subprocess.STARTF_USESHOWWINDOW
startup.wShowWindow = subprocess.SW_HIDE
raise SystemExit(subprocess.run(command, cwd=root, env=env, startupinfo=startup).returncode)
