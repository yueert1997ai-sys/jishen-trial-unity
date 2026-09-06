"""Pose the existing native assembly from the user's two-handed action reference."""
import bpy,pathlib,hashlib,json,shutil,math,sys
from mathutils import Matrix,Vector,Quaternion
from mathutils.bvhtree import BVHTree
W=pathlib.Path(__file__).resolve().parent;V=W/'iteration_v3';D=V/'reference_action_pose';D.mkdir(exist_ok=True)
P=W.parent/'JishenTrial_ASSEMBLED_MASTER.blend';B=D/'SOURCE_BEFORE_REFERENCE_POSE.blend'
args={a.split('=',1)[0]:a.split('=',1)[1] for a in sys.argv if '=' in a}
if not B.exists():shutil.copy2(P,B)
source_sha=hashlib.sha256(B.read_bytes()).hexdigest()
reference=pathlib.Path('C:/Users/yue/AppData/Local/Temp/codex-clipboard-7a2fbd66-4b2c-486b-b424-11d5aeb701cf.png')
if reference.exists() and not (D/'USER_POSE_REFERENCE.png').exists():shutil.copy2(reference,D/'USER_POSE_REFERENCE.png')
bpy.ops.wm.open_mainfile(filepath=str(B));sc=bpy.context.scene;bpy.context.view_layer.update()
O=bpy.data.objects;root=O['AntiShip_Blade_Display_Root'];grip=O['V3B_Sword_Grip_Socket'];S=2.974/348
def proof():
 return {o.name:{'hash':hashlib.sha256(repr(([tuple(v.co) for v in o.data.vertices],[tuple(f.vertices) for f in o.data.polygons],[[tuple(l.uv) for l in uv.data] for uv in o.data.uv_layers])).encode()).hexdigest(),'materials':[m.name if m else None for m in o.data.materials]} for o in sc.objects if o.type=='MESH'}
before=proof();parents={o.name:o.parent.name if o.parent else None for o in sc.objects}
old_matrices={o.name:o.matrix_world.copy() for o in sc.objects}
root_to_hand=root.matrix_world.inverted()@O['Hand.R'].matrix_world
scale=root.matrix_world.to_scale().copy()
links=[('UpperArm.'+s,'Forearm.'+s) for s in ('R','L')]+[('Forearm.'+s,'Hand.'+s) for s in ('R','L')]+[('Thigh.'+s,'Shin.'+s) for s in ('R','L')]+[('Shin.'+s,'Foot.'+s) for s in ('R','L')]
lengths={a+'>'+b:(O[b].matrix_world.translation-O[a].matrix_world.translation).length for a,b in links}
def update():bpy.context.view_layer.update()
def rotate(o,rotation):
 p=o.matrix_world.translation.copy();o.matrix_world=Matrix.Translation(p)@rotation.to_4x4()@Matrix.Translation(-p)@o.matrix_world;update()
def turn(name,angle,axis):rotate(O[name],Matrix.Rotation(math.radians(angle),3,axis))
def point_segment(name,child,direction):
 o=O[name];v=(O[child].matrix_world.translation-o.matrix_world.translation).normalized()
 rotate(o,v.rotation_difference(Vector(direction).normalized()).to_matrix())

# Remove the old one-handed display swing at its actual articulation pivots.
for name in ('Forearm.R','Hand.R'):
 o=O[name];p=o.matrix_world.translation.copy();o.matrix_world=Matrix.Translation(p);update()

# Counterturn through the torso, then extend one leg and tuck the other.
turn('Pelvis',-5,'Z');turn('Waist',10,'X');turn('Waist',12,'Z');turn('Head',-8,'Z')
point_segment('Thigh.R','Shin.R',(-.77,-.19,-.61))
point_segment('Shin.R','Foot.R',(-.79,.02,-.61))
point_segment('Thigh.L','Shin.L',(.84,.03,-.54))
point_segment('Shin.L','Foot.L',(.32,.69,-.65))
turn('Foot.R',17,'X');turn('Foot.L',12,'X')
turn('Skirt_Hinge.R',24,'Y');turn('Skirt_Hinge.L',-27,'Y')
turn('Shoulder_Armor_Floating_Pivot.R',-8,'Y');turn('Shoulder_Armor_Floating_Pivot.L',9,'Y')

# Both palms meet the existing handle. Blade points to the upper left in front view.
axis=Vector((-.76,-.23,.61)).normalized()
center=Vector((-.10,-.56,2.40))
x=-axis;y=Vector((0,1,0));y=(y-x*y.dot(x)).normalized();z=x.cross(y).normalized()
weapon_rot=Matrix((x,y,z)).transposed()
weapon_world=Matrix.Translation(center)@weapon_rot.to_4x4()@Matrix.Diagonal((*scale,1))
# Right hand leads by 66.6 mm; left hand occupies the remaining rear grip.
right_hand=weapon_world@Matrix.Translation(Vector((-.35,0,0)))@Matrix.Rotation(math.radians(float(args.get('right_roll','90'))),4,'X')@root_to_hand
left_grip=weapon_world@Vector((.35,0,0))
left_grip_local=Vector((3,-4,-21))*S
lx=axis;lz=Vector((.4,1,0));lz=(lz-lx*lz.dot(lx)).normalized();ly=lz.cross(lx).normalized()
left_rotation=Matrix((lx,ly,lz)).transposed()
left_hand=left_rotation.to_4x4();left_hand.translation=left_grip-left_rotation@left_grip_local
ik=[]
def arm_ik(side,hand_world,pole):
 a=O['UpperArm.'+side];b=O['Forearm.'+side];h=O['Hand.'+side]
 A=a.matrix_world.translation.copy();T=hand_world.translation.copy()
 l1=(b.matrix_world.translation-A).length;l2=(h.matrix_world.translation-b.matrix_world.translation).length
 delta=T-A;distance=delta.length;d=delta.normalized()
 assert abs(l1-l2)<distance<l1+l2,('Arm target outside reach',side,distance,l1+l2)
 along=(l1*l1-l2*l2+distance*distance)/(2*distance)
 bend=(Vector(pole)-A);bend=(bend-d*bend.dot(d)).normalized()
 E=A+d*along+bend*math.sqrt(max(0,l1*l1-along*along))
 point_segment(a.name,b.name,E-A);point_segment(b.name,h.name,T-b.matrix_world.translation)
 endpoint_error=(h.matrix_world.translation-T).length
 assert endpoint_error<1e-5,(side,endpoint_error)
 h.matrix_world=hand_world;update()
 ik.append({'side':side,'shoulder':list(A),'elbow':list(E),'wrist':list(T),'endpoint_error_m':endpoint_error,'extension_ratio':distance/(l1+l2)})
arm_ik('R',right_hand,(-1.1,-.3,2.15));arm_ik('L',left_hand,(1.1,-.2,2.15))
root.matrix_world=weapon_world;update()
# Let the gauntlet swivel around its own axis while the wrist and grip stay fixed.
# Choose the smallest roll that clears the long rear guard fins.
def eval_shape(o):
 ev=o.evaluated_get(bpy.context.evaluated_depsgraph_get());me=ev.to_mesh()
 data=([v.co.copy() for v in me.vertices],[tuple(p.vertices) for p in me.polygons]);ev.to_mesh_clear();return data
def world_shape(o,data):
 vs=[o.matrix_world@v for v in data[0]]
 return (Vector([min(v[k] for v in vs) for k in range(3)]),Vector([max(v[k] for v in vs) for k in range(3)]),BVHTree.FromPolygons(vs,data[1]))
guard_shapes=[world_shape(o,eval_shape(o)) for o in sc.objects if o.type=='MESH' and o.name.startswith('RK_Guard_') and 'Frame' in o.name]
clearance=[]
for side in ('R','L'):
 b=O['Forearm.'+side];h=O['Hand.'+side];base=b.matrix_world.copy();hm=h.matrix_world.copy()
 ax=(h.matrix_world.translation-b.matrix_world.translation).normalized();hand_branch=set(h.children_recursive)
 armor=[(o,eval_shape(o)) for o in b.children_recursive if o.type=='MESH' and o not in hand_branch and o.name.startswith('V3B_')]
 trials=[]
 for degrees in (0,-15,15,-30,30,-45,45,-60,60):
  b.matrix_world=base;update();rotate(b,Quaternion(ax,math.radians(degrees)).to_matrix());h.matrix_world=hm;root.matrix_world=weapon_world;update()
  contact_count=0
  for o,data in armor:
   shape=world_shape(o,data)
   for other in guard_shapes:
    if all(shape[0][k]<=other[1][k] and shape[1][k]>=other[0][k] for k in range(3)) and shape[2].overlap(other[2]):contact_count+=1
  trials.append({'roll_degrees':degrees,'contact_pairs':contact_count})
  if contact_count==0:break
 assert contact_count==0,('Gauntlet clearance unresolved',side,trials)
 clearance.append({'side':side,'selected_roll_degrees':degrees,'trials':trials})
left_socket=O.get('V3B_Left_Support_Grip_Socket')
if left_socket is None:
 left_socket=bpy.data.objects.new('V3B_Left_Support_Grip_Socket',None);sc.collection.objects.link(left_socket)
left_socket.parent=O['Hand.L'];left_socket.matrix_world=Matrix.Translation(left_grip);left_socket.empty_display_size=.025
left_socket['purpose']='Second hand support on the existing rear handle; static reference action pose.'
update()

assert proof()==before,'Mesh geometry, UVs or materials changed during pose'
parent_errors=[n for n,p in parents.items() if (O[n].parent.name if O[n].parent else None)!=p]
assert not parent_errors,parent_errors
link_errors={a+'>'+b:abs((O[b].matrix_world.translation-O[a].matrix_world.translation).length-lengths[a+'>'+b]) for a,b in links}
assert max(link_errors.values())<1e-5,link_errors
dg=bpy.context.evaluated_depsgraph_get();points=[];weapon_points=[];hands=[]
hand_parts=set(O['Hand.R'].children_recursive)|set(O['Hand.L'].children_recursive)
for o in sc.objects:
 if o.type!='MESH' or o.hide_render:continue
 ev=o.evaluated_get(dg);me=ev.to_mesh();ps=[o.matrix_world@v.co for v in me.vertices];points.extend(ps)
 if o.name.startswith('RK_'):weapon_points.extend(ps)
 elif o in hand_parts:hands.extend(ps)
 ev.to_mesh_clear()
def frame(name,outward,pts,res,pad=1.13):
 cam=O.get(name)
 if cam is None:cam=bpy.data.objects.new(name,bpy.data.cameras.new(name));sc.collection.objects.link(cam)
 out=Vector(outward).normalized();q=(-out).to_track_quat('-Z','Y');right=q@Vector((1,0,0));up=q@Vector((0,1,0))
 rlo,rhi=min(p.dot(right) for p in pts),max(p.dot(right) for p in pts)
 ulo,uhi=min(p.dot(up) for p in pts),max(p.dot(up) for p in pts)
 target=right*((rlo+rhi)/2)+up*((ulo+uhi)/2)+out*(sum(p.dot(out) for p in pts)/len(pts))
 cam.location=target+out*16;cam.rotation_euler=q.to_euler();cam.data.type='ORTHO';cam.data.sensor_fit='HORIZONTAL'
 cam.data.ortho_scale=max(rhi-rlo,(uhi-ulo)*res[0]/res[1])*pad
 return cam
frame('RAIKEN_ASSEMBLED_3Q',(.22,-1,.16),points,(1800,1500))
frame('RAIKEN_ASSEMBLED_FRONT',(0,-1,0),points,(1800,1500))
frame('RAIKEN_HAND_FIT',(.15,-1,.35),hands,(1600,1300),1.55)
frame('RAIKEN_POSED_WEAPON_3Q',(.22,-1,.16),weapon_points,(1800,1500))
sc.camera=O['RAIKEN_ASSEMBLED_3Q'];sc.render.resolution_x=1800;sc.render.resolution_y=1500;sc.render.resolution_percentage=100
sc['weapon_pose_revision']=2;sc['pose_reference']='USER_POSE_REFERENCE.png: two-handed upper-left blade presentation, counterturned torso, split airborne legs.'
sc['weapon_display_pose']='Two hands stacked on the existing handle in front of the body, blade raised toward upper left, right leg extended and left leg bent back.'
sc['sword_grip_axis']=list(axis);sc['sword_grip_center_units']=list(grip.matrix_world.translation/S)
sc['current_delivery']=str(D/'VALKYR_V3_REFERENCE_ACTION.blend');sc['export_status']='Native model and preview images only; no game, FBX or GLB export.'
root['display_pose']=sc['weapon_display_pose']
for screen in bpy.data.screens:
 for area in screen.areas:
  if area.type=='VIEW_3D':
   space=area.spaces.active;space.shading.type='MATERIAL';space.overlay.show_overlays=False;space.region_3d.view_perspective='CAMERA'
bpy.context.preferences.filepaths.save_version=0;bpy.ops.wm.save_as_mainfile(filepath=str(D/'VALKYR_V3_REFERENCE_ACTION.blend'))
audit={'source_sha256':source_sha,'source_reference':str(D/'USER_POSE_REFERENCE.png'),'weapon_pose_revision':2,'mesh_count_unchanged':len(before),'geometry_uv_materials_unchanged':True,'parent_errors':parent_errors,'limb_length_errors_m':link_errors,'arm_ik':ik,'gauntlet_clearance':clearance,'blade_axis_world':list(axis),'blade_tip_m':list(O['RAIKEN_BLADE_TIP'].matrix_world.translation),'right_grip_weapon_local':list(root.matrix_world.inverted()@grip.matrix_world.translation),'left_grip_weapon_local':list(root.matrix_world.inverted()@left_socket.matrix_world.translation),'weapon_scale':list(root.matrix_world.to_scale()),'changed_pose_objects':[n for n,m in old_matrices.items() if O[n].type=='EMPTY' and max(abs(m[i][j]-O[n].matrix_world[i][j]) for i in range(4) for j in range(4))>1e-5],'exports_run':False,'mesh_proof':before}
(D/'pose_audit.json').write_text(json.dumps(audit,indent=2),encoding='utf-8')
print(json.dumps({k:v for k,v in audit.items() if k!='mesh_proof'},indent=2),flush=True)
