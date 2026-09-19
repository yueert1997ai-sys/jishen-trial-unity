import bpy, math, os, json
from mathutils import Vector, Quaternion, Matrix
OUT='D:/project-mecha-design/MECH ROUGE/art_prototypes/FRAME01_HeavyCombo_20260909'
os.makedirs(OUT+'/frames',exist_ok=True)
s=bpy.context.scene;r=bpy.data.objects['FRAME01_RIG']
r.animation_data_clear()
for o in list(s.objects):
 if o.name.startswith('DISPLAY'):bpy.data.objects.remove(o,do_unlink=True)
bpy.ops.import_scene.fbx(filepath='D:/project-mecha-design/MECH ROUGE/Assets/Art/HangarLoadout/RAIKEN.fbx')
mesh=bpy.data.objects['RAIKEN_Mesh'];world=mesh.matrix_world.copy()
mesh.parent=None;mesh.matrix_world=world
for v in mesh.data.vertices:v.co=world@v.co
mesh.matrix_world=Matrix.Identity(4)
bpy.ops.object.empty_add();weapon=bpy.context.object;weapon.name='RAIKEN_MOTION';mesh.parent=weapon
weapon.scale=(2,2,2);weapon.rotation_mode='QUATERNION'
for p in r.pose.bones:p.rotation_mode='XYZ'
def delta(n,v):r.pose.bones[n].location=r.data.bones[n].matrix_local.to_3x3().inverted()@Vector(v)
def target(n,v):delta(n,Vector(v)-r.data.bones[n].head_local)
def rot(n,v):r.pose.bones[n].rotation_euler=[math.radians(x) for x in v]
# f, pelvis xyz, spine pitch/twist, chest pitch/twist, right wrist, blade direction, left hand, right/left foot y
poses=[
(1,(0,0,-.35),(5,-12),(4,-12),(-2,-.65,5.9),(-.25,.75,-.35),(1.6,-1,6.6),0,0),
(10,(-.35,.35,-.65),(-4,-25),(-6,-28),(-2.7,.05,6.9),(-.9,.45,.05),(1.4,-.7,7),.25,-.2),
(14,(-.3,.1,-.8),(6,-15),(2,-29),(-2.8,-.8,6.9),(-1,-.1,.05),(1.4,-.8,7),.25,-1.3),
(18,(.1,-.25,-1.05),(12,10),(6,0),(-.7,-2.55,6.9),(0,-1,.02),(1.6,-.5,6.9),.25,-1.3),
(22,(.45,-.4,-1.1),(10,24),(5,28),(1.7,-1.45,6.6),(1,-.05,-.1),(2.2,.4,6.8),.25,-1.3),
(28,(.35,-.3,-.85),(3,25),(0,25),(1.8,-1.3,7.6),(.85,.2,.48),(1.7,.35,7),.25,-1.3),
(35,(.3,-.15,-.6),(-6,23),(-4,26),(1.1,-.8,8.25),(.8,.2,.6),(1.7,-.1,7.3),-.4,-1.3),
(39,(.15,-.3,-.8),(4,18),(0,26),(1.65,-1.4,7.7),(1,-.15,.05),(1.5,.3,6.7),-1.9,-1.3),
(43,(-.25,-.6,-1.1),(12,-10),(5,-4),(-.5,-2.5,7.0),(0,-1,-.12),(1.4,.2,6.4),-1.9,-1.3),
(47,(-.5,-.6,-1.1),(10,-25),(5,-26),(-2.5,-1.2,6.5),(-1,.1,-.15),(1.25,-.5,6.6),-1.9,-1.3),
(54,(-.25,-.5,-.7),(-8,-15),(-8,-16),(-1.35,-.5,8.4),(-.3,.7,.75),(.2,-.55,8.0),-1.9,-1.3),
(63,(0,-.5,-.35),(-15,0),(-12,-4),(-.5,-.2,9.2),(0,.82,.57),(.1,.15,8.9),-1.9,-1.3),
(68,(0,-.7,-.5),(-12,0),(-14,-4),(-.4,-.1,9.25),(0,.85,.53),(.1,.25,8.95),-1.9,-2.5),
(71,(0,-.95,-.9),(8,0),(-6,-2),(-.35,-.85,9.0),(0,.25,.97),(.1,-.65,8.6),-1.9,-2.5),
(74,(0,-1.2,-1.35),(20,0),(12,0),(-.3,-2.45,7.35),(0,-.99,.12),(.12,-2.05,7.4),-1.9,-2.5),
(77,(0,-1.35,-1.65),(25,0),(18,0),(-.3,-3.0,5.45),(0,-.9,-.44),(.1,-2.6,5.6),-1.9,-2.5),
(80,(0,-1.4,-1.72),(26,0),(18,0),(-.3,-3.05,5.25),(0,-.88,-.48),(.1,-2.65,5.45),-1.9,-2.5),
(89,(0,-1.4,-1.65),(23,0),(16,0),(-.3,-3,5.4),(0,-.9,-.44),(.1,-2.6,5.6),-1.9,-2.5),
(102,(0,-1.1,-1),(12,-8),(7,-8),(-1.1,-2.2,5.8),(-.6,-.6,-.4),(1.3,-1.5,6.2),-1.4,-2.5),
(120,(0,-1,-.35),(5,-12),(4,-12),(-2,-1.65,5.9),(-.25,.75,-.35),(1.6,-2,6.6),-1,-1),
]
for f,p,sp,ch,hand,d,lh,rf,lf in poses:
 s.frame_set(f)
 for b in r.pose.bones:b.location=(0,0,0);b.rotation_euler=(0,0,0)
 delta('pelvis',p);rot('spine',(sp[0],sp[1],0));rot('chest',(ch[0],ch[1],0))
 rot('head',(-sp[0]*.4,-ch[1]*.5,0))
 if f>=68 and f<=89:rf=.4;lf=-3.1
 target('IK_foot.R',(-1.25,rf,.77));target('IK_foot.L',(1.25,lf,.77))
 target('POLE_foot.R',(-1.5,rf-4,2));target('POLE_foot.L',(1.5,lf-4,2))
 target('IK_hand.R',hand);target('IK_hand.L',lh)
 target('POLE_hand.R',(-3.8,p[1]+.3,6.8));target('POLE_hand.L',(3.8,p[1]+.3,6.8))
 for side in ['R','L']:
  rot('IK_hand.'+side,(-70,0,0))
 for b in r.pose.bones:
  if b.name.startswith(('finger','thumb')):rot(b.name,(55,0,0))
 for b in r.pose.bones:
  b.keyframe_insert('location',frame=f);b.keyframe_insert('rotation_euler',frame=f)
 weapon.location=Vector(hand)+Vector((0,-.12,-.2))
 weapon.rotation_quaternion=Vector((0,-1,0)).rotation_difference(Vector(d).normalized())
 weapon.keyframe_insert('location',frame=f);weapon.keyframe_insert('rotation_quaternion',frame=f)
for ob in [r,weapon]:
 a=ob.animation_data.action
 a.name='FRAME01_HEAVY_TRIPLE' if ob==r else 'RAIKEN_TRIPLE_PATH'
 for layer in a.layers:
  for strip in layer.strips:
   for slot in a.slots:
    bag=strip.channelbag(slot)
    if bag:
     for fc in bag.fcurves:
      for k in fc.keyframe_points:k.interpolation='BEZIER';k.handle_left_type='AUTO_CLAMPED';k.handle_right_type='AUTO_CLAMPED'
s.timeline_markers.clear()
for f,n in [(10,'01 LOAD'),(18,'01 CUT'),(35,'02 REGRIP'),(43,'02 RETURN CUT'),(63,'03 OVERHEAD LOAD'),(68,'03 FOOT PLANT'),(74,'03 RELEASE'),(80,'03 FOLLOW THROUGH'),(120,'RECOVER')]:s.timeline_markers.new(n,frame=f)
s.frame_start=1;s.frame_end=120;s.render.fps=24
s.render.engine='BLENDER_WORKBENCH';s.render.resolution_x=960;s.render.resolution_y=720;s.render.resolution_percentage=100
s.display.shading.light='STUDIO';s.display.shading.color_type='MATERIAL';s.display.shading.show_shadows=True
s.world.color=(.055,.065,.08)
cam=s.camera;cam.location=(22,-28,15);cam.rotation_euler=(Vector((0,-1.5,6))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=24
for screen in bpy.data.screens:
 for a in screen.areas:
  if a.type=='VIEW_3D':a.spaces.active.region_3d.view_perspective='CAMERA'
s.frame_set(74)
bpy.ops.wm.save_as_mainfile(filepath=OUT+'/FRAME01_HEAVY_TRIPLE.blend')
for f in [10,18,43,63,74,80]:
 s.frame_set(f);s.render.filepath=OUT+'/pose_%03d.png'%f;bpy.ops.render.render(write_still=True)
print('COMPLETED',OUT)
