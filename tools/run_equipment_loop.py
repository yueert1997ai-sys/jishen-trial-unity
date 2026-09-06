"""Run the equipment prototype in this isolated Unity project."""
import argparse
import os
from pathlib import Path
import subprocess
import uuid

ROOT = Path(__file__).resolve().parents[1]
UNITY = Path('D:/Editor/6000.3.18f1/Editor/Unity.exe')
OUTPUT = ROOT / 'AuditEvidence/equipment-loop'

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('mode', choices=['import', 'check', 'build', 'preview-check'])
    args = parser.parse_args()
    OUTPUT.mkdir(parents=True, exist_ok=True)
    env = os.environ.copy()
    env['MECH_EQUIPMENT_PROFILE'] = str(OUTPUT / ('test-profile-' + uuid.uuid4().hex[:8] + '.json'))
    startup = subprocess.STARTUPINFO()
    startup.dwFlags |= subprocess.STARTF_USESHOWWINDOW
    startup.wShowWindow = subprocess.SW_HIDE
    if args.mode == 'preview-check':
        exe = ROOT / 'Builds/EquipmentLoop/MECH_TRIAL_Equipment.exe'
        command = [str(exe), '-batchmode', '-screen-width', '1600', '-screen-height', '900',
                   '-logFile', str(OUTPUT / 'player.log'), '-equipmentQuickCheck']
    else:
        command = [str(UNITY), '-batchmode', '-projectPath', str(ROOT), '-logFile', str(OUTPUT / (args.mode + '.log'))]
        if args.mode != 'check': command.append('-quit')
        if args.mode == 'check': command += ['-executeMethod', 'EquipmentLoopAudit.Run']
        if args.mode == 'build': command += ['-executeMethod', 'EquipmentLoopBuild.Build']
    result = subprocess.run(command, cwd=ROOT, env=env, startupinfo=startup)
    raise SystemExit(result.returncode)

if __name__ == '__main__':
    main()
