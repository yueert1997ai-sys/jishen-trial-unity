"""Freeze the already-integrated E-01 boss into the current gameplay checkout.

The art checkout and its Blender masters are read-only. Existing V7 work is
snapshotted before integration; this does not reset or merge either Git branch.
"""
import hashlib
import json
import pathlib
import shutil

root = pathlib.Path(__file__).resolve().parents[1]
source = root.parents[1]
evidence = root / 'AuditEvidence/liquid-boss-v8'
baseline = evidence / 'baseline'
changed = [
    'Assets/Scripts/Enemy/BossController.cs',
    'Assets/Scripts/Enemy/EnemySpawner.cs',
    'Assets/Scripts/Core/GameManager.cs',
    'Assets/Scripts/Core/StageManager.cs',
    'Assets/Scripts/UI/HangarDeploymentUI.cs',
    'Assets/Scripts/UI/GameText.cs',
    'Assets/Scenes/Demo_Main.unity',
    'ProjectSettings/ProjectSettings.asset',
    'README.md', 'docs/ASSET_PROVENANCE.md',
]
for relative in changed:
    backup = baseline / relative
    if not backup.exists():
        backup.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(root / relative, backup)

entries = []

def freeze(src, dst):
    content = src.read_bytes()
    if dst.exists() and dst.read_bytes() != content:
        raise RuntimeError(f'Refusing to replace an existing different asset: {dst}')
    dst.parent.mkdir(parents=True, exist_ok=True)
    if not dst.exists():
        shutil.copy2(src, dst)
    entries.append({'source': str(src), 'game_copy': str(dst),
                    'bytes': len(content), 'sha256': hashlib.sha256(content).hexdigest()})

art = pathlib.Path('Assets/Art/Enemies/TypeE01Elite')
for src in sorted((source / art).rglob('*')):
    if src.is_file():
        freeze(src, root / src.relative_to(source))
for relative in [
    'Assets/Art/Enemies.meta', 'Assets/Art/Enemies/TypeE01Elite.meta',
    'Assets/Scripts/Enemy/E01ElitePoseDriver.cs',
    'Assets/Scripts/Enemy/E01ElitePoseDriver.cs.meta',
]:
    freeze(source / relative, root / relative)

# The boss prefab gets its own new Unity GUID; preserve its referenced rig/material GUIDs.
freeze(source / 'Assets/Prefabs/Enemies/Boss_HeavyMech.prefab',
       root / 'Assets/Prefabs/Enemies/LiquidE01Boss.prefab')
freeze(source / 'docs/E01_ELITE_GAME_HANDOFF.md', evidence / 'source-integration.md')
evidence.mkdir(parents=True, exist_ok=True)
(evidence / 'source-snapshot.json').write_text(json.dumps(entries, ensure_ascii=False, indent=2), encoding='utf-8')
print(f'Frozen {len(entries)} files from {source}; source assets unchanged.')
