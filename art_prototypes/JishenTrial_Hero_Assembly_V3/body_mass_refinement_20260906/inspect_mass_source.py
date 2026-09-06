import bpy,pathlib,json,hashlib
from mathutils import Vector
W=pathlib.Path(__file__).resolve().parent;S=2.974/348
bpy.ops.wm.open_mainfile(filepath=str(W/'SOURCE_BEFORE_MASS_PASS.blend'));sc=bpy.context.scene
def region(o):
 p=o;names=[]
 while p:names.append(p.name);p=p.parent
 if any(n.startswith('V3H_') or n in ('Head','HEAD_V3_ROOT') for n in names):return 'head'
 if 'AntiShip_Blade_Display_Root' in names:return 'weapon'
 if 'Cannon_Right_Cradle_Mount' in names:return 'cannon'
 if 'Backpack_Structural_Mount' in names:return 'pack'
 for nm in ('Hand','Forearm','UpperArm','Foot','Shin','Thigh','Shoulder_Armor_Floating_Pivot','Skirt_Hinge'):
  if any(n.startswith(nm+'.') for n in names):return nm
 return o.parent.name if o.parent else 'root'
items=[];heads=[]
for o in sc.objects:
 if o.type!='MESH' or o.hide_render:continue
 ps=[o.matrix_world@v.co/S for v in o.data.vertices]
 if not ps:continue
 item={'name':o.name,'region':region(o),'parent':o.parent.name if o.parent else None,'bounds_units':[[fn(p[k] for p in ps) for k in range(3)] for fn in (min,max)],'materials':[m.name for m in o.data.materials if m]};items.append(item)
 if item['region']=='head':
  heads.append({'name':o.name,'mesh_hash':hashlib.sha256(repr(([tuple(v.co) for v in o.data.vertices],[tuple(f.vertices) for f in o.data.polygons],[[tuple(l.uv) for l in uv.data] for uv in o.data.uv_layers])).encode()).hexdigest(),'materials':item['materials'],'matrix_world':[list(row) for row in o.matrix_world]})
out={'source_sha256':hashlib.sha256((W/'SOURCE_BEFORE_MASS_PASS.blend').read_bytes()).hexdigest(),'head_proof':heads,'objects':items,'pivots':{o.name:list(o.matrix_world.translation/S) for o in sc.objects if o.type=='EMPTY'}}
(W/'source_inventory.json').write_text(json.dumps(out,indent=2))
selected=('V3B_Thoracic_Upper','V3B_Sternum_Folded','V3B_Pectoral_Oblique','V3B_Forearm_Folded','V3B_Thigh_Ceramic_Wrapped','V3B_Shin_','V3B_Foot_','V3B_Pelvis_Deep','V3B_Waist_Deep')
print(json.dumps([i for i in items if i['name'].startswith(selected)],indent=2),flush=True)
