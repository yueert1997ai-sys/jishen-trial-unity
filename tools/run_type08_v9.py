"""Build and replay the V9 TYPE08 starter update with an isolated test save."""
import argparse
import os
from pathlib import Path
import subprocess
import uuid

root = Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser()
parser.add_argument('mode', choices=['build', 'check'])
args = parser.parse_args()
build = root / 'Builds/ValkyrHangarV9_Type08Starter'
evidence = root / 'AuditEvidence/type08-starter-v9' / args.mode
evidence.mkdir(parents=True, exist_ok=True)
environment = os.environ.copy()
temporary = Path('D:/Tools/BlenderUserData/Temp/type08-v9-unity')
temporary.mkdir(parents=True, exist_ok=True)
environment['TEMP'] = environment['TMP'] = str(temporary)
log = evidence / ('unity.log' if args.mode == 'build' else 'player.log')
if args.mode == 'build':
    command = ['D:/Editor/6000.3.18f1/Editor/Unity.exe', '-batchmode', '-quit',
               '-projectPath', str(root), '-executeMethod', 'Type08StarterIntegration.ConfigureAndBuild',
               '-companyBuildPath', str(build), '-logFile', str(log)]
else:
    environment['MECH_EQUIPMENT_PROFILE'] = str(evidence / ('test-profile-' + uuid.uuid4().hex[:8] + '.json'))
    command = [str(build / 'MECH_TRIAL_Valkyr.exe'), '-batchmode', '-screen-fullscreen', '0',
               '-screen-width', '1600', '-screen-height', '900', '-companyCheck', '-logFile', str(log)]
startup = subprocess.STARTUPINFO()
startup.dwFlags |= subprocess.STARTF_USESHOWWINDOW
startup.wShowWindow = subprocess.SW_HIDE
result = subprocess.run(command, cwd=root, env=environment, startupinfo=startup)
print(f'{args.mode} exit={result.returncode} log={log}', flush=True)
if log.exists():
    lines = log.read_text(encoding='utf-8', errors='replace').splitlines()
    print('\n'.join(s for s in lines if any(word in s for word in
        ['_PASS', '_FAIL', 'error CS', 'Exception:', 'Shader error', 'Build completed']))[-5000:])
if args.mode == 'check' and (evidence / 'quick-check.txt').exists():
    print((evidence / 'quick-check.txt').read_text(encoding='utf-8'))
raise SystemExit(result.returncode)
