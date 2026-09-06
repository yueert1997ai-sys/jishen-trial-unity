import bpy,json,pathlib
from mathutils import Vector
O=pathlib.Path(__file__).resolve().parent
bpy.ops.wm.open_mainfile(filepath=str(O/'JishenTrial_ASSEMBLED_MASTER.blend'))
bpy.context.view_layer.update()
data=[]
for o in bpy.context.scene.objects:
 if o.type not in ('MESH','CURVE','EMPTY'):continue
 vs=[o.matrix_world@Vector(v) for v in o.bound_box] if o.type!='EMPTY' else [o.matrix_world.translation]
 data.append(dict(name=o.name,type=o.type,parent=o.parent.name if o.parent else None,collections=[c.name for c in o.users_collection],min=[round(min(v[i] for v in vs),4) for i in range(3)],max=[round(max(v[i] for v in vs),4) for i in range(3)],mat=[m.name for m in o.data.materials] if o.type!='EMPTY' else [],mods=[m.type for m in o.modifiers],hidden=o.hide_render))
(O/'logs'/'refinement_inventory.json').write_text(json.dumps(data,indent=2))
print('INVENTORY',len(data))
