"""Repose the existing right hand and blade together; no geometry edits or exports."""
import bpy,pathlib,hashlib,json,shutil,math,sys
from mathutils import Vector,Matrix
W=pathlib.Path(__file__).resolve().parent;V=W/'iteration_v3';D=V/'weapon_pose';D.mkdir(exist_ok=True)
P=W.parent/'JishenTrial_ASSEMBLED_MASTER.blend';B=D/'SOURCE_BEFORE_WEAPON_POSE.blend'
args={a.split('=',1)[0]:a.split('=',1)[1] for a in sys.argv if '=' in a}
if not B.exists():shutil.copy2(P,B)
source_sha=hashlib.sha256(B.read_bytes()).hexdigest()
bpy.ops.wm.open_mainfile(filepath=str(B));sc=bpy.context.scene;bpy.context.view_layer.update()
forearm=bpy.data.objects['Forearm.R'];hand=bpy.data.objects['Hand.R']
grip=bpy.data.objects['V3B_Sword_Grip_Socket'];root=bpy.data.objects['AntiShip_Blade_Display_Root'];tip=bpy.data.objects['RAIKEN_BLADE_TIP']
moving=set(forearm.children_recursive)|{forearm}
fixed={o.name:[list(row) for row in o.matrix_world] for o in sc.objects if o not in moving}
def mesh_proof():
 out={}
 for o in sc.objects:
  if o.type!='MESH':continue
  out[o.name]={'hash':hashlib.sha256(repr(([tuple(v.co) for v in o.data.vertices],[tuple(f.vertices) for f in o.data.polygons],[[tuple(l.uv) for l in uv.data] for uv in o.data.uv_layers])).encode()).hexdigest(),'materials':[m.name if m else None for m in o.data.materials]}
 return out
before=mesh_proof();grip_relative=grip.matrix_world.inverted()@root.matrix_world
old_tip=list(tip.matrix_world.translation);old_grip=list(grip.matrix_world.translation)
forearm_yaw=float(args.get('forearm_yaw','20'));wrist_yaw=float(args.get('wrist_yaw','12'))
def turn_at_pivot(o,degrees):
 p=o.matrix_world.translation.copy();o.matrix_world=Matrix.Translation(p)@Matrix.Rotation(math.radians(degrees),4,'Z')@Matrix.Translation(-p)@o.matrix_world
 bpy.context.view_layer.update()
turn_at_pivot(forearm,forearm_yaw);turn_at_pivot(hand,wrist_yaw)
axis=(tip.matrix_world.translation-grip.matrix_world.translation).normalized()
sc['sword_grip_axis']=list(axis);sc['sword_grip_center_units']=list(grip.matrix_world.translation/(2.974/348))
sc['weapon_display_pose']='Blade aimed diagonally forward and outward; right forearm yaw +20 degrees and wrist yaw +12 degrees from previous display pose. Grip attachment and weapon geometry preserved.'
sc['weapon_pose_revision']=1
root['display_pose']=sc['weapon_display_pose']
assert mesh_proof()==before,'Mesh geometry, UVs or materials changed'
fixed_errors=[n for n,m in fixed.items() if [list(row) for row in bpy.data.objects[n].matrix_world]!=m]
assert not fixed_errors,fixed_errors
relative=grip.matrix_world.inverted()@root.matrix_world
grip_error=max(abs(relative[i][j]-grip_relative[i][j]) for i in range(4) for j in range(4))
assert grip_error<1e-5,grip_error
dg=bpy.context.evaluated_depsgraph_get();points=[];weapon_points=[]
for o in sc.objects:
 if o.type!='MESH' or o.hide_render:continue
 ev=o.evaluated_get(dg);me=ev.to_mesh();ps=[o.matrix_world@v.co for v in me.vertices]
 points.extend(ps)
 if o.name.startswith('RK_'):weapon_points.extend(ps)
 ev.to_mesh_clear()
lo=Vector([min(p[k] for p in points) for k in range(3)]);hi=Vector([max(p[k] for p in points) for k in range(3)])
wlo=Vector([min(p[k] for p in weapon_points) for k in range(3)]);whi=Vector([max(p[k] for p in weapon_points) for k in range(3)])
assert wlo.z>.08,('Weapon intersects ground',wlo.z)

def frame_camera(name,outward,pts,res,pad=1.12):
 cam=bpy.data.objects.get(name)
 if cam is None:
  data=bpy.data.cameras.new(name);cam=bpy.data.objects.new(name,data);sc.collection.objects.link(cam)
 outward=Vector(outward).normalized();rotation=(-outward).to_track_quat('-Z','Y')
 right=rotation@Vector((1,0,0));up=rotation@Vector((0,1,0))
 rlo,rhi=min(p.dot(right) for p in pts),max(p.dot(right) for p in pts)
 ulo,uhi=min(p.dot(up) for p in pts),max(p.dot(up) for p in pts)
 depth=sum(p.dot(outward) for p in pts)/len(pts)
 target=right*((rlo+rhi)/2)+up*((ulo+uhi)/2)+outward*depth
 cam.location=target+outward*15;cam.rotation_euler=rotation.to_euler();cam.data.type='ORTHO'
 cam.data.ortho_scale=max(rhi-rlo,(uhi-ulo)*res[0]/res[1])*pad
 return cam
frame_camera('RAIKEN_ASSEMBLED_3Q',(.28,-1,.24),points,(1800,1500))
frame_camera('RAIKEN_ASSEMBLED_FRONT',(0,-1,0),points,(1800,1500))
hand_points=[]
for o in hand.children_recursive:
 if o.type=='MESH' and not o.name.startswith('RK_'):
  hand_points.extend(o.matrix_world@v.co for v in o.data.vertices)
target=grip.matrix_world.translation+axis*.28
cam=bpy.data.objects['RAIKEN_HAND_FIT'];cam.location=target+Vector((.8,-4,1.2));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=1.65
frame_camera('RAIKEN_POSED_WEAPON_3Q',(.25,-1,.33),weapon_points,(2000,1200),1.14)
sc.camera=bpy.data.objects['RAIKEN_ASSEMBLED_3Q'];sc.render.resolution_x=1800;sc.render.resolution_y=1500;sc.render.resolution_percentage=100
for screen in bpy.data.screens:
 for area in screen.areas:
  if area.type=='VIEW_3D':
   space=area.spaces.active;space.shading.type='MATERIAL';space.overlay.show_overlays=False
   space.region_3d.view_perspective='CAMERA'
sc['current_delivery']=str(D/'VALKYR_V3_WEAPON_POSE.blend')
sc['export_status']='Native model and preview images only. User requested no game/FBX/GLB export.'
bpy.context.preferences.filepaths.save_version=0;bpy.ops.wm.save_as_mainfile(filepath=str(D/'VALKYR_V3_WEAPON_POSE.blend'))
report={'source_sha256':source_sha,'forearm_yaw_added_degrees':forearm_yaw,'wrist_yaw_added_degrees':wrist_yaw,'old_tip_m':old_tip,'new_tip_m':list(tip.matrix_world.translation),'old_grip_m':old_grip,'new_grip_m':list(grip.matrix_world.translation),'weapon_min_height_m':wlo.z,'mesh_count_unchanged':len(before),'geometry_uv_materials_unchanged':True,'fixed_body_matrix_errors':fixed_errors,'grip_relative_matrix_max_error':grip_error,'exports_run':False,'mesh_proof':before}
(D/'pose_audit.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps({k:v for k,v in report.items() if k!='mesh_proof'},indent=2),flush=True)
