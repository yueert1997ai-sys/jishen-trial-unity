import bpy,bmesh,pathlib,sys,json,hashlib,math
from mathutils import Vector
W=pathlib.Path(__file__).resolve().parent;D=W/'delivery';D.mkdir(exist_ok=True)
stage=int(next((a.split('=')[1] for a in sys.argv if a.startswith('stage=')),'5'))
bpy.ops.wm.open_mainfile(filepath=str(W/f'iteration_{stage:02d}/ASSEMBLED.blend'));sc=bpy.context.scene
for source_name,target_name in (('01 Chest and layered thorax','01 Chest and abdomen'),('08 Backpack and propulsion','08 Backpack spine and twin thrusters')):
 src=bpy.data.collections.get('BODY_V3 | '+source_name);dst=bpy.data.collections.get('BODY_V3 | '+target_name)
 if src and dst:
  for o in list(src.objects):
   if o.name not in dst.objects:dst.objects.link(o)
   src.objects.unlink(o)
  if not src.objects and not src.children:bpy.data.collections.remove(src)
visible=[o for o in sc.objects if o.type=='MESH' and not o.hide_render]
def issues(me):
 bm=bmesh.new();bm.from_mesh(me);out=(sum(not e.is_manifold for e in bm.edges),sum(f.calc_area()<1e-13 for f in bm.faces));bm.free();return out
bevel_changes=[];errors=[]
for o in visible:
 dg=bpy.context.evaluated_depsgraph_get();ev=o.evaluated_get(dg);me=ev.to_mesh();bad=issues(me);ev.to_mesh_clear()
 if not any(bad):continue
 if o.name.startswith('V3H_') or any(issues(o.data)):errors.append([o.name,bad]);continue
 bmods=[m for m in o.modifiers if m.type=='BEVEL' and m.show_viewport]
 for m in bmods:m.show_render=False;m.show_viewport=False
 bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get();ev=o.evaluated_get(dg);me=ev.to_mesh();after=issues(me);ev.to_mesh_clear()
 if any(after):errors.append([o.name,after])
 else:bevel_changes.append({'part':o.name,'before':bad,'action':'Disabled collapsing decorative bevel; closed control geometry retained'})
assert not errors,errors
missing=[o for o in visible if not o.data.uv_layers]
if missing:
 bpy.ops.object.select_all(action='DESELECT')
 for o in missing:o.select_set(True)
 bpy.context.view_layer.objects.active=missing[0];bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT')
 bpy.ops.uv.smart_project(angle_limit=math.radians(66),island_margin=.003,area_weight=.3,correct_aspect=True,scale_to_bounds=True)
 bpy.ops.object.mode_set(mode='OBJECT')
 for o in missing:o.data.uv_layers.active.name='UV0'
proof=json.loads((W/'source_inventory.json').read_text())['head_proof'];head_error=[]
for p in proof:
 o=bpy.data.objects[p['name']]
 hh=hashlib.sha256(repr(([tuple(v.co) for v in o.data.vertices],[tuple(f.vertices) for f in o.data.polygons],[[tuple(l.uv) for l in uv.data] for uv in o.data.uv_layers])).encode()).hexdigest()
 if hh!=p['mesh_hash'] or [list(row) for row in o.matrix_world]!=p['matrix_world'] or [m.name for m in o.data.materials]!=p['materials']:head_error.append(o.name)
assert not head_error,head_error
dg=bpy.context.evaluated_depsgraph_get();tri=0;polys=0;controltri=0;points=[];model=[]
for o in visible:
 o.data.calc_loop_triangles();controltri+=len(o.data.loop_triangles);polys+=len(o.data.polygons)
 ev=o.evaluated_get(dg);me=ev.to_mesh();me.calc_loop_triangles();tri+=len(me.loop_triangles)
 ps=[o.matrix_world@v.co for v in me.vertices];points+=ps
 model.append({'name':o.name,'triangles':len(me.loop_triangles),'bounds_units':[[fn(p[k] for p in ps)/(2.974/348) for k in range(3)] for fn in (min,max)]})
 ev.to_mesh_clear()
report={'visible_meshes':len(visible),'control_polygons':polys,'control_triangles':controltri,'evaluated_triangles':tri,'approved_head_meshes_unchanged':len(proof),'head_errors':head_error,'geometry_issues':errors,'bevel_repairs':bevel_changes,'added_uv_meshes':len(missing),'missing_uv_count':sum(not o.data.uv_layers for o in visible),'bounds_m':[[fn(p[k] for p in points) for k in range(3)] for fn in (min,max)],'model':model}
sc['mass_revision_notes']='Continuous torso armor; detailed forearms; broad overlapping thigh clamshells; layered solid calf armor; enclosed knee caps, rotary housings, anterior and rear overlaps; raised insteps; solid asymmetric blade.'
sc['export_status']='Modeling review only. User requested no new demo/FBX/GLB export.'
sc['current_delivery']=str(D/'VALKYR_MASS_MASTER.blend');sc.camera=bpy.data.objects['V3B_CAM_THREE_QUARTER']
for screen in bpy.data.screens:
 for area in screen.areas:
  if area.type=='VIEW_3D':
   space=area.spaces.active;space.shading.type='MATERIAL';space.region_3d.view_rotation=sc.camera.rotation_euler.to_quaternion();space.region_3d.view_location=Vector((0,0,1.78));space.region_3d.view_distance=5.35;space.region_3d.view_perspective='PERSP'
bpy.ops.object.select_all(action='DESELECT');bpy.context.view_layer.objects.active=None
bpy.context.preferences.filepaths.save_version=0;bpy.ops.wm.save_as_mainfile(filepath=str(D/'VALKYR_MASS_MASTER.blend'))
(D/'master_audit.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps({k:v for k,v in report.items() if k!='model'},indent=2),flush=True)
