from pathlib import Path
import json
root=Path(__file__).resolve().parents[2]
for path in [root/'Assets/Art/Enemies/TypeE01Elite/export_manifest.json',root/'art_prototypes/TYPE_E01_ELITE_20260906/game_ready/export_manifest.json']:
    report=json.loads(path.read_text(encoding='utf-8'))
    report['material_list']=[dict(name=name,**data) for name,data in report['materials'].items()]
    path.write_text(json.dumps(report,indent=2),encoding='utf-8')
print('MATERIAL_MANIFEST_READY')
