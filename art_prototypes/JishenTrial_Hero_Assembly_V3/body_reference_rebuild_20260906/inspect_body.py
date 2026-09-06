import bpy,pathlib,json,hashlib
from mathutils import Vector
W=pathlib.Path(__file__).resolve().parent
bpy.ops.wm.open_mainfile(filepath=str(W/'ACCEPTED_HEAD_ASSEMBLY_SOURCE.blend'))
sc=bpy.context.scene; dg=bpy.context.evaluated_depsgraph_get()
data=[]
for o in sc.objects:
 d={'name':o.name,'type':o.type,'parent':o.parent.name if o.parent else None,'collections':[c.name for c in o.users_collection],'hidden':o.hide_render,'world':list(o.matrix_world.translation)}
 if o.type in ('MESH','CURVE'):
  b=[o.matrix_world@Vector(p) for p in o.evaluated_get(dg).bound_box]
  d['min']=[min(p[k] for p in b) for k in range(3)];d['max']=[max(p[k] for p in b) for k in range(3)]
  d['materials']=[m.name for m in o.data.materials if m]
 if o.type=='MESH':
  d['vertices']=len(o.data.vertices);d['polys']=len(o.data.polygons)
  if o.name.startswith('V3H_'):
   d['mesh_hash']=hashlib.sha256(repr(([tuple(v.co) for v in o.data.vertices],[tuple(f.vertices) for f in o.data.polygons])).encode()).hexdigest()
 data.append(d)
(W/'source_inventory.json').write_text(json.dumps(data,indent=2),encoding='utf8')
for o in data:
 if o['type']=='EMPTY':print(json.dumps(o),flush=True)
for c in bpy.data.collections:
 print('COLLECTION',c.name,len(c.all_objects),flush=True)
