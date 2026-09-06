import bpy, pathlib, json, hashlib
from mathutils import Vector
W=pathlib.Path(__file__).resolve().parent
P=W.parent
bpy.ops.wm.open_mainfile(filepath=str(P/'JishenTrial_ASSEMBLED_MASTER.blend'))
sc=bpy.context.scene
root=bpy.data.objects.get('HEAD_V2_ROOT')
parts=set(root.children_recursive)|{root}
def row(o):
    b=[o.matrix_world@Vector(v) for v in o.bound_box] if o.type in ('MESH','CURVE') else []
    return {'name':o.name,'type':o.type,'parent':o.parent.name if o.parent else None,'location':list(o.location),'matrix':[list(r) for r in o.matrix_world], 'bounds':[[min(v[i] for v in b) for i in range(3)],[max(v[i] for v in b) for i in range(3)]] if b else None,'collections':[c.name for c in o.users_collection],'materials':[m.name for m in o.data.materials] if o.type in ('MESH','CURVE') else [],'hidden':o.hide_render}
data={'source_sha256':hashlib.sha256((P/'JishenTrial_ASSEMBLED_MASTER.blend').read_bytes()).hexdigest(),'head':[row(o) for o in parts], 'nearby':[row(o) for o in sc.objects if o not in parts and any(s in o.name.lower() for s in ('neck','head','collar'))], 'collections':[c.name for c in bpy.data.collections], 'cameras':[row(o) for o in sc.objects if o.type=='CAMERA']}
(W/'current_inventory.json').write_text(json.dumps(data,indent=2))
print(json.dumps({k:data[k] for k in ('source_sha256','nearby')},indent=2))
print('HEAD_PARENT',root.parent.name if root.parent else None, 'HEAD_PARTS',len(parts))
