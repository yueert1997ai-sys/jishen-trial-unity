"""Export only gameplay-loop changes; exclude the frozen art/integration snapshot."""
from pathlib import Path
import json
import subprocess

ROOT = Path(__file__).resolve().parents[1]
TRACKED = [
    'Assets/Scripts/Combat/WeaponController.cs', 'Assets/Scripts/Core/GameManager.cs',
    'Assets/Scripts/Core/GamePreferences.cs', 'Assets/Scripts/Core/RunUpgradeSystem.cs',
    'Assets/Scripts/Core/StageManager.cs', 'Assets/Scripts/Enemy/EnemyBase.cs',
    'Assets/Scripts/Player/PlayerStats.cs', 'Assets/Scripts/UI/HangarDeploymentUI.cs',
    'Assets/Scripts/UI/ResultUI.cs', 'Assets/Scripts/UI/RewardUI.cs',
]
NEW = [
    'Assets/Editor/EquipmentLoopBuild.cs', 'Assets/Scripts/Core/EquipmentLoopQuickCheck.cs',
    'Assets/Scripts/Equipment/EquipmentLoop.cs', 'Assets/Scripts/Equipment/SalvageGear.cs',
    'Assets/Scripts/UI/EquipmentWarehouseUI.cs',
]
NEW += [path + '.meta' for path in list(NEW)]
NEW += ['tools/run_equipment_loop.py', 'tools/export_equipment_patch.py', 'docs/EQUIPMENT_LOOP_HANDOFF.md']

def main():
    output = ROOT / 'handoff'
    output.mkdir(exist_ok=True)
    diff = subprocess.run(['git', 'diff', '--binary', '--', *TRACKED], cwd=ROOT, capture_output=True, check=True).stdout
    for path in NEW:
        result = subprocess.run(['git', 'diff', '--no-index', '--binary', '--', '/dev/null', path], cwd=ROOT, capture_output=True)
        if result.returncode not in (0, 1):
            raise RuntimeError(result.stderr.decode(errors='replace'))
        diff += result.stdout
    (output / 'equipment-loop-code.patch').write_bytes(diff)
    (output / 'equipment-loop-files.json').write_text(json.dumps(TRACKED + NEW, indent=2), encoding='utf-8')
    print(f'Exported {len(TRACKED) + len(NEW)} files to {output / "equipment-loop-code.patch"}')

if __name__ == '__main__':
    main()
