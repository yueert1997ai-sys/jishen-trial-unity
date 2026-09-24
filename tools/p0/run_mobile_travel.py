"""Silent Windows Player checks for touch logic; does not certify an iOS build."""
from pathlib import Path
import argparse
import datetime
import hashlib
import json
import os
import subprocess

root = Path(__file__).resolve().parents[2]
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('mode', choices=['build', 'mobile', 'foundation', 'regression', 'weaponhud'])
parser.add_argument('--unity', default='D:/Editor/6000.3.18f1/Editor/Unity.exe')
options = parser.parse_args()
build = root / 'Builds/MobileTravel_R1'
out = root / 'AuditEvidence/mobile-travel-r1' / (options.mode + '-' + datetime.datetime.now().strftime('%Y%m%d-%H%M%S'))
out.mkdir(parents=True)
env = os.environ.copy()
env.update(MECH_LOOP_V2_BUILD=str(build), MECH_LOOP_V2_EVIDENCE=str(out),
           MECH_LOOP_V2_VERSION='mobile-travel-r1', MECH_EQUIPMENT_PROFILE=str(out / 'profile.json'))
if options.mode == 'build':
    args = [options.unity, '-batchmode', '-nographics', '-quit', '-projectPath', str(root),
            '-executeMethod', 'MobileTravelBuild.Build', '-logFile', str(out / 'build.log')]
else:
    args = [str(build / 'MECH_TRIAL_P0.exe'), '-batchmode', '-noaudio', '-p0Check',
            '-combatSeed', '9172026', '-screen-width', '1600', '-screen-height',
            '740' if options.mode == 'mobile' else '900', '-logFile', str(out / 'player.log')]
    args += {'mobile': ['-mobileTravelCheck'], 'foundation': ['-combatSlice', '-foundationCheck'],
             'regression': ['-combatSlice'], 'weaponhud': ['-combatSlice', '-weaponHudCheck']}[options.mode]
startup = subprocess.STARTUPINFO()
startup.dwFlags |= subprocess.STARTF_USESHOWWINDOW
startup.wShowWindow = 0
result = subprocess.run(args, cwd=root, env=env, startupinfo=startup, timeout=900)
record = dict(args=args, exit=result.returncode, build=str(build))
assembly = build / 'MECH_TRIAL_P0_Data/Managed/Assembly-CSharp.dll'
if assembly.exists():
    record['assembly_sha256'] = hashlib.sha256(assembly.read_bytes()).hexdigest()
(out / 'run.json').write_text(json.dumps(record, indent=2), encoding='utf-8')
print(options.mode, result.returncode, out, flush=True)
raise SystemExit(result.returncode)
