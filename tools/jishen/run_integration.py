"""Run the installed Unity against the frozen Jishen snapshot. No Blender/model edits."""
import argparse
from pathlib import Path
import shutil
import subprocess

ROOT = Path(__file__).resolve().parents[2]
UNITY = Path('D:/Editor/6000.3.18f1/Editor/Unity.exe')
OUTPUT = ROOT / 'AuditEvidence/jishen'


def invoke(method, log, quit_editor):
    arguments = [str(UNITY), '-batchmode', '-projectPath', str(ROOT), '-executeMethod', method,
                 '-logFile', str(OUTPUT / log)]
    if quit_editor:
        arguments.append('-quit')
    print('Executing ' + method, flush=True)
    startup = subprocess.STARTUPINFO()
    startup.dwFlags |= subprocess.STARTF_USESHOWWINDOW
    startup.wShowWindow = subprocess.SW_HIDE
    subprocess.run(arguments, cwd=ROOT, startupinfo=startup, check=True)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('mode', choices=['build', 'validate', 'release'])
    args = parser.parse_args()
    OUTPUT.mkdir(parents=True, exist_ok=True)
    if args.mode == 'build':
        invoke('JishenHeroIntegration.Build', 'integration.log', True)
    elif args.mode == 'validate':
        invoke('ManualCombatAudit.Run', 'manual-final.log', False)
        shutil.copytree(ROOT / 'AuditEvidence/manual', OUTPUT / 'manual-final', dirs_exist_ok=True)
        invoke('HeroVisualAudit.Run', 'visual-final.log', False)
        shutil.copytree(ROOT / 'AuditEvidence/hero', OUTPUT / 'visual-final', dirs_exist_ok=True)
    else:
        invoke('JishenHeroIntegration.BuildRelease', 'release.log', True)


if __name__ == '__main__':
    main()
