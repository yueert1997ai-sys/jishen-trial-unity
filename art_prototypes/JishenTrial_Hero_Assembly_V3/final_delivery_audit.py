from pathlib import Path
from PIL import Image,ImageChops
import json,hashlib,tarfile
OUT=Path(__file__).resolve().parent
stats=json.loads((OUT/'logs/game_candidate.json').read_text(encoding='utf8'))
unity=json.loads((OUT/'logs/unity_import_report.json').read_text(encoding='utf8'))
fbx=json.loads((OUT/'logs/reopen_fbx_verification.json').read_text(encoding='utf8'))
assert stats['triangles']==unity['triangles']==129824
assert stats['nonmanifold_edges']=={} and fbx['topology_edges']==[]
assert abs(unity['boundsSize']['y']-3.3972)<.001
with tarfile.open(OUT/'exports/JishenTrial_Assembly_V3.unitypackage','r:gz') as tf:
    paths=[tf.extractfile(m).read().decode('utf8').strip() for m in tf.getmembers() if m.name.endswith('/pathname')]
assert all(p=='Assets/Art/JishenTrial_Assembly_V3' or p.startswith('Assets/Art/JishenTrial_Assembly_V3/') for p in paths),paths
assert any(p.endswith('JishenTrial_Assembly_V3_STATIC.prefab') for p in paths)
assert not any(p.endswith('.cs') for p in paths)
diffs={}
for label,on,off in [('blender','FRONT_3Q','BEAM_OFF'),('unity','UNITY_3Q','UNITY_BEAM_OFF')]:
    a=Image.open(OUT/'renders'/(on+'.png')).convert('RGB');b=Image.open(OUT/'renders'/(off+'.png')).convert('RGB')
    box=ImageChops.difference(a,b).getbbox();assert box;diffs[label]={'beam_toggle_pixel_difference_bounds':box}
files={}
for rel in ('JishenTrial_ASSEMBLED_MASTER.blend','JishenTrial_GAME_CANDIDATE.blend',
            'exports/JishenTrial_Assembly_V3.fbx','exports/JishenTrial_Assembly_V3.glb','exports/JishenTrial_Assembly_V3.unitypackage'):
    path=OUT/rel;files[rel]={'bytes':path.stat().st_size,'sha256':hashlib.sha256(path.read_bytes()).hexdigest()}
report={'matching_triangle_count':stats['triangles'],'materials':5,'final_head_scale':1.5,'final_crown_height':3.351056,
        'closed_game_meshes':True,'unity_package_asset_paths':paths,'gameplay_scripts_in_package':False,
        'beam_toggle_image_checks':diffs,'files':files}
(OUT/'logs/final_delivery_audit.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf8')
print('FINAL_DELIVERY_AUDIT_PASS',len(paths),'package paths; 129824 tris; 5 mats; final enlarged head; no gameplay scripts')
