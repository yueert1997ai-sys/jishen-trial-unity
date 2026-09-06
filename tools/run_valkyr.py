"""Build and quickly preview the delivered Valkyr and Raiken in the isolated game."""
import argparse, os, pathlib, subprocess, uuid
ROOT = pathlib.Path(__file__).resolve().parents[1]
OUTPUT = ROOT / 'AuditEvidence/v3-model-update'
def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('mode', choices=['inspect', 'integrate', 'update-hero', 'build', 'check', 'motion-check'])
    mode = parser.parse_args().mode
    output = OUTPUT / 'motion' if mode == 'motion-check' else OUTPUT / 'combat' if mode == 'check' else OUTPUT
    output.mkdir(parents=True, exist_ok=True)
    env = os.environ.copy()
    env['MECH_EQUIPMENT_PROFILE'] = str(output / ('test-profile-' + uuid.uuid4().hex[:8] + '.json'))
    if mode in ('check', 'motion-check'):
        command = [str(ROOT/'Builds/ValkyrV3/MECH_TRIAL_Valkyr.exe'), '-batchmode', '-screen-width', '1600', '-screen-height', '900', '-motionQuickCheck' if mode == 'motion-check' else '-valkyrQuickCheck', '-logFile', str(output/'player.log')]
    else:
        method = {'inspect':'Inspect', 'integrate':'Build', 'update-hero':'UpdateHero', 'build':'BuildRelease'}[mode]
        command = ['D:/Editor/6000.3.18f1/Editor/Unity.exe', '-batchmode', '-quit', '-projectPath', str(ROOT), '-executeMethod', 'ValkyrCombatIntegration.'+method, '-logFile', str(OUTPUT/(mode+'.log'))]
    startup = subprocess.STARTUPINFO(); startup.dwFlags |= subprocess.STARTF_USESHOWWINDOW; startup.wShowWindow = subprocess.SW_HIDE
    raise SystemExit(subprocess.run(command, cwd=ROOT, env=env, startupinfo=startup).returncode)
if __name__ == '__main__': main()
