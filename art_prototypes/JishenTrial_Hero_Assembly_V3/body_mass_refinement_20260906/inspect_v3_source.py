import bpy,pathlib,json,hashlib,shutil
W=pathlib.Path(__file__).resolve().parent;D=W/'iteration_v3';D.mkdir(exist_ok=True)
P=W.parent/'JishenTrial_ASSEMBLED_MASTER.blend';S=2.974/348
source_sha=hashlib.sha256(P.read_bytes()).hexdigest();backup=D/'SOURCE_BEFORE_V3.blend'
if not backup.exists():shutil.copy2(P,backup)
assert hashlib.sha256(backup.read_bytes()).hexdigest()==source_sha,'Source backup mismatch'
bpy.ops.wm.open_mainfile(filepath=str(backup));sc=bpy.context.scene;bpy.context.view_layer.update()
def proof(o):
 return {'type':o.type,'hash':hashlib.sha256(repr(([tuple(v.co) for v in o.data.vertices],[tuple(p.vertices) for p in o.data.polygons],[[tuple(l.uv) for l in uv.data] for uv in o.data.uv_layers])).encode()).hexdigest() if o.type=='MESH' else None,'matrix':[list(r) for r in o.matrix_world],'parent':o.parent.name if o.parent else None,'materials':[m.name if m else None for m in o.data.materials] if o.type=='MESH' else []}
objects={o.name:proof(o) for o in sc.objects if o.type in ('MESH','EMPTY')}
regions={}
for o in sc.objects:
 if o.type!='MESH' or not o.name.startswith('V3B_'):continue
 ps=[o.matrix_world@v.co/S for v in o.data.vertices]
 regions[o.name]={'parent':o.parent.name if o.parent else None,'bounds':[[fn(p[k] for p in ps) for k in range(3)] for fn in (min,max)]}
out={'source':str(P),'source_sha256':source_sha,'game_sha256':hashlib.sha256((W.parent/'JishenTrial_GAME_CANDIDATE.blend').read_bytes()).hexdigest(),'objects':objects,'body':regions}
(D/'source_proof.json').write_text(json.dumps(out,indent=2),encoding='utf-8')
from collections import Counter
print(json.dumps({'source_sha256':source_sha,'body_parents':dict(Counter(v['parent'] for v in regions.values())),'chest_parts':{n:v for n,v in regions.items() if v['parent']=='Thorax'}},indent=2),flush=True)
