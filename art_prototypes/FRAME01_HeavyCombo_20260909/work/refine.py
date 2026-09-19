import bpy,math,json,os
from mathutils import Vector,Quaternion
out='D:/project-mecha-design/MECH ROUGE/art_prototypes/FRAME01_HeavyCombo_20260909'
s=bpy.context.scene;r=bpy.data.objects['FRAME01_RIG'];w=bpy.data.objects['RAIKEN_MOTION']
for o in s.objects:
 for m in o.modifiers:
  if m.type=='WEIGHTED_NORMAL':m.show_viewport=False;m.show_render=False
print('START REFINE',flush=True)
samples=[]
for f in range(1,121):
 s.frame_set(f);bpy.context.view_layer.update()
 samples.append((f,w.rotation_quaternion.copy(),r.pose.bones['IK_hand.L'].location.copy()))
for f,q,l in samples:
 s.frame_set(f)
 for side in ['R','L']:
  p=r.pose.bones['IK_hand.'+side]
  palm=q@Quaternion((0,0,1),math.pi/2 if side=='R' else -math.pi/2)
  p.rotation_euler=(p.bone.matrix_local.to_quaternion().inverted()@palm).to_euler()
  p.keyframe_insert('rotation_euler',frame=f)
 bpy.context.view_layer.update()
 p=r.pose.bones['hand.R'];w.location=p.head+p.matrix.to_quaternion()@Vector((0,.25,0))
 w.keyframe_insert('location',frame=f)
 if 54<=f<=89:
  support=w.location+q@Vector((0,.48,0))
  p=r.pose.bones['IK_hand.L'];palm=q@Quaternion((0,0,1),-math.pi/2)
  wrist=support-palm@Vector((0,.25,0))
  blend=min(1,(f-54)/8,(98-f)/9);blend=max(0,blend)
  loc=p.bone.matrix_local.to_3x3().inverted()@(wrist-p.bone.head_local)
  p.location=l.lerp(loc,blend);p.keyframe_insert('location',frame=f)
print('BAKED HANDS',flush=True)
report={'max_ik_error':{},'min_blade_z':999,'frames':120,'notes':'Pose and IK audit only; mechanical self collision not certified.'}
mesh=bpy.data.objects['RAIKEN_Mesh']
for f in range(1,121):
 s.frame_set(f);bpy.context.view_layer.update()
 for side in ['R','L']:
  for tip,ctrl in [('forearm','hand'),('shin','foot')]:
   err=(r.pose.bones[tip+'.'+side].tail-r.pose.bones['IK_'+ctrl+'.'+side].head).length
   n=ctrl+'.'+side;report['max_ik_error'][n]=max(err,report['max_ik_error'].get(n,0))
 report['min_blade_z']=min(report['min_blade_z'],min((mesh.matrix_world@v.co).z for v in mesh.data.vertices))
open(out+'/audit.json','w').write(json.dumps(report,indent=2));print('AUDIT',report)
s.frame_set(80);bpy.ops.wm.save_as_mainfile(filepath=out+'/FRAME01_HEAVY_TRIPLE.blend')
for f in [18,43,63,74,80]:
 s.frame_set(f);s.render.filepath=out+'/pose_%03d.png'%f;bpy.ops.render.render(write_still=True)
s.render.resolution_percentage=75;s.render.filepath=out+'/frames/f_';s.render.image_settings.file_format='PNG';bpy.ops.render.render(animation=True)
