import bpy,math,json,os
from mathutils import Vector,Quaternion,Matrix
O='D:/project-mecha-design/MECH ROUGE/art_prototypes/FRAME01_HeavyCombo_20260909/v3'
s=bpy.context.scene;r=bpy.data.objects['FRAME01_RIG'];w=bpy.data.objects['RAIKEN_MOTION'];r.animation_data_clear();w.animation_data_clear()
for o in s.objects:
 for m in o.modifiers:
  if m.type=='WEIGHTED_NORMAL':m.show_viewport=False;m.show_render=False
def curve(t,keys):
 i=0
 while i<len(keys)-2 and t>keys[i+1][0]:i+=1
 a,b=keys[i],keys[i+1];p=keys[max(i-1,0)];n=keys[min(i+2,len(keys)-1)]
 u=max(0,min(1,(t-a[0])/(b[0]-a[0])));h=b[0]-a[0]
 m0=(b[1]-p[1])/(b[0]-p[0]);m1=(n[1]-a[1])/(n[0]-a[0])
 return (2*u**3-3*u*u+1)*a[1]+(u**3-2*u*u+u)*h*m0+(-2*u**3+3*u*u)*b[1]+(u**3-u*u)*h*m1
def c(t,k):return curve(t,k)
def R(a):return Quaternion((0,0,1),math.radians(a))
def delta(n,v):r.pose.bones[n].location=r.data.bones[n].matrix_local.to_3x3().inverted()@Vector(v)
def target(n,v):delta(n,Vector(v)-r.data.bones[n].head_local)
def rot(n,pitch,yaw,roll=0):r.pose.bones[n].rotation_euler=[math.radians(x) for x in (pitch,yaw,roll)]
Y=[(1,-45),(8,-68),(14,-30),(19,55),(25,88),(31,60),(36,-15),(41,-80),(46,-96),(53,-40),(60,0),(66,0),(76,0),(92,-15)]
Z=[(1,-.65),(9,-.85),(19,-1.35),(26,-.95),(32,-.75),(40,-1.35),(47,-.9),(58,-.4),(62,-.65),(68,-1.75),(74,-1.9),(82,-1.55),(92,-.65)]
def blade(t):
 yaw=c(t,[(1,-100),(9,-118),(14,-72),(19,35),(24,115),(29,132),(34,92),(38,-10),(43,-115),(48,-145),(55,-160),(60,-180),(64,-240),(68,-275),(74,-286),(82,-300),(92,-335)])
 el=c(t,[(1,12),(10,-8),(21,-5),(29,28),(35,32),(43,-18),(49,35)])
 az=math.radians(yaw);e=math.radians(el);side=Vector((math.sin(az)*math.cos(e),-math.cos(az)*math.cos(e),math.sin(e)))
 if t<49:return side
 # Sweeping side follow-through rises into the overhead arc without resetting.
 blend=min(1,max(0,(t-49)/9));blend=blend*blend*(3-2*blend)
 ang=math.radians(c(t,[(49,-60),(59,-48),(62,-28),(65,48),(69,110),(76,117),(84,110),(92,80)]))
 overhead=Vector((0,-math.sin(ang),math.cos(ang)))
 return side.lerp(overhead,blend).normalized()
prev=None;records=[]
for f in range(1,93):
 s.frame_set(f)
 for p in r.pose.bones:p.location=(0,0,0);p.rotation_euler=(0,0,0)
 yaw=c(f,Y);hipyaw=c(f+2,Y)*.48;spinyaw=c(f+1,Y)*.24;chestyaw=yaw*.28
 z=c(f,Z);advance=c(f,[(1,0),(18,-.6),(30,-.65),(42,-1.15),(55,-1.3),(69,-1.9),(82,-1.9),(92,-1.75)])
 shift=c(f,[(1,-.25),(10,-.4),(22,.65),(32,.35),(43,-.65),(55,-.2),(65,0),(92,0)])
 delta('pelvis',(shift,advance,z));rot('pelvis',4,hipyaw)
 pitch=c(f,[(1,5),(10,-6),(21,16),(30,-3),(42,18),(57,-17),(62,-12),(68,27),(75,28),(92,5)])
 rot('spine',pitch*.55,spinyaw,-yaw*.08);rot('chest',pitch*.45,chestyaw,-yaw*.07);rot('head',-pitch*.5,-yaw*.6)
 # Alternating planted feet, with lifted travel only between plants.
 rf=c(f,[(1,.5),(24,.5),(35,-2.4),(53,-2.4),(62,-.3),(92,-.3)])
 lf=c(f,[(1,-.45),(15,-2),(45,-2),(62,-3.7),(92,-3.7)])
 for side,x,y in [('R',-1.5,rf),('L',1.5,lf)]:
  lift=0
  for start,end,which in [(4,15,'L'),(26,35,'R'),(52,62,'L'),(54,62,'R')]:
   if side==which and start<f<end:lift=.45*math.sin(math.pi*(f-start)/(end-start))
  target('IK_foot.'+side,(x,y,.77+lift));target('POLE_foot.'+side,(x*1.1,y-4,2.2))
 bpy.context.view_layer.update()
 for it in range(5):
  err=max((r.pose.bones['shin.'+side].tail-r.pose.bones['IK_foot.'+side].head).length for side in ['L','R'])
  if err<.003:break
  p=r.pose.bones['pelvis'];p.location+=p.bone.matrix_local.to_3x3().inverted()@Vector((0,0,-err*1.2));bpy.context.view_layer.update()
 d=blade(f);vel=blade(f+.08)-blade(f-.08)
 edge=(vel-d*vel.dot(d)).normalized()
 if edge.length<.1:edge=Vector((0,-1,0))
 if f>=58:edge=Vector((1,0,0)).cross(d).normalized()
 # Authored luminous cutting edge is +Z; blade tip is -Y. No arbitrary roll.
 yy=-d;xx=yy.cross(edge).normalized();edge=xx.cross(yy).normalized()
 q=Matrix(((xx.x,yy.x,edge.x),(xx.y,yy.y,edge.y),(xx.z,yy.z,edge.z))).to_quaternion()
 if prev and q.dot(prev)<0:q.negate()
 prev=q.copy();w.rotation_quaternion=q
 overhead=max(0,min(1,(f-48)/9));overhead=overhead*overhead*(3-2*overhead)
 shoulder=r.pose.bones['upper_arm.R'].head.copy()
 reach=R(yaw)@Vector((-.35,-2.35,-.45))
 hand=shoulder+reach
 heavy=Vector((-.35,advance+c(f,[(49,.1),(59,.5),(63,.25),(66,-1.1),(70,-1.8),(80,-1.8),(92,-1.1)]),c(f,[(49,8),(59,9.15),(63,8.85),(66,7.4),(70,4.8),(78,4.7),(92,6)])))
 hand=hand.lerp(heavy,overhead)
 target('IK_hand.R',hand)
 # Elbow poles follow torso, avoiding fixed world-space robot elbows.
 for side,sgn in [('R',-1),('L',1)]:
  pole=Vector((shift,advance,6.6))+R(yaw)@Vector((sgn*3.8,.7,.2));target('POLE_hand.'+side,pole)
  palm=q@Quaternion((0,0,1),math.pi/2 if side=='R' else -math.pi/2)
  p=r.pose.bones['IK_hand.'+side];p.rotation_euler=(p.bone.matrix_local.to_quaternion().inverted()@palm).to_euler()
 bpy.context.view_layer.update()
 p=r.pose.bones['hand.R'];w.location=p.head+p.matrix.to_quaternion()@Vector((0,.25,0))
 support=w.location+q@Vector((0,.48,0));palm=q@Quaternion((0,0,1),-math.pi/2)
 guard=r.pose.bones['upper_arm.L'].head+R(yaw)@Vector((.6,.8,-1.2))
 target('IK_hand.L',guard.lerp(support-palm@Vector((0,.25,0)),overhead))
 for p in r.pose.bones:
  if p.name.startswith(('finger','thumb')):p.rotation_euler.x=math.radians(60)
  p.keyframe_insert('location',frame=f);p.keyframe_insert('rotation_euler',frame=f)
 w.keyframe_insert('location',frame=f);w.keyframe_insert('rotation_quaternion',frame=f)
 records.append({'f':f,'edge_dot_motion':edge.dot(vel.normalized()),'yaw':yaw})
for ob in [r,w]:
 for layer in ob.animation_data.action.layers:
  for strip in layer.strips:
   for slot in ob.animation_data.action.slots:
    bag=strip.channelbag(slot)
    if bag:
     for fc in bag.fcurves:
      for k in fc.keyframe_points:k.interpolation='LINEAR'
s.timeline_markers.clear()
for f,n in [(9,'LOAD'),(19,'LEFT CUT'),(29,'CONTINUOUS REVERSAL'),(40,'RIGHT CUT'),(56,'OVERHEAD FLOW'),(67,'FINAL CLEAVE'),(76,'FOLLOW THROUGH')]:s.timeline_markers.new(n,frame=f)
s.frame_end=92;s.render.fps=30;s.render.resolution_x=1200;s.render.resolution_y=900;s.render.resolution_percentage=75
cam=s.camera;cam.location=(17,-30,16);cam.rotation_euler=(Vector((0,-1,6))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=24
s.frame_set(19);bpy.ops.wm.save_as_mainfile(filepath=O+'/FRAME01_COMBO_V3.blend')
open(O+'/edge_audit.json','w').write(json.dumps(records,indent=2))
for f in [59,67,70,73,76,82]:
 s.frame_set(f);s.render.filepath=O+'/pose_%03d.png'%f;bpy.ops.render.render(write_still=True)
s.render.filepath=O+'/frames/f_';bpy.ops.render.render(animation=True)

s.camera.location=(22,-9,10);s.camera.rotation_euler=(Vector((0,-2,6))-s.camera.location).to_track_quat('-Z','Y').to_euler();s.camera.data.ortho_scale=18
for f in [67,70,73,76,82]:
 s.frame_set(f);s.render.filepath=O+'/close_%03d.png'%f;bpy.ops.render.render(write_still=True)
