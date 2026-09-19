import bpy,math,json
from mathutils import Vector
out='D:/project-mecha-design/MECH ROUGE/art_prototypes/FRAME01_HeavyCombo_20260909'
s=bpy.context.scene;r=bpy.data.objects['FRAME01_RIG'];w=bpy.data.objects['RAIKEN_MOTION']
s.frame_set(89);a=r.pose.bones['IK_hand.L'].location.copy();s.frame_set(102);b=r.pose.bones['IK_hand.L'].location.copy()
for f in range(90,103):
 t=(f-89)/13;t=t*t*(3-2*t);p=r.pose.bones['IK_hand.L'];p.location=a.lerp(b,t);p.keyframe_insert('location',frame=f)
base=[]
for f in range(1,121):
 s.frame_set(f);base.append((f,r.pose.bones['pelvis'].location.copy()))
for f,loc in base:
 s.frame_set(f);p=r.pose.bones['pelvis'];p.location=loc
 bpy.context.view_layer.update()
 # Keep planted feet reachable without changing leg length.
 for n in range(6):
  err=max((r.pose.bones['shin.'+side].tail-r.pose.bones['IK_foot.'+side].head).length for side in ['R','L'])
  if err<.002:break
  p.location+=p.bone.matrix_local.to_3x3().inverted()@Vector((0,0,-err*1.3-.005));bpy.context.view_layer.update()
 p.keyframe_insert('location',frame=f)
 # Tiny clavicle translations preserve the secondary grip at full extension.
 for side in ['L']:
  p=r.pose.bones['clavicle.'+side]
  err=r.pose.bones['IK_hand.'+side].head-r.pose.bones['forearm.'+side].tail
  if err.length>.001:
   p.location+=p.bone.matrix_local.to_3x3().inverted()@err;p.keyframe_insert('location',frame=f)
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.03));ground=bpy.context.object;ground.name='Ground reference'
mat=bpy.data.materials.new('Ground');mat.diffuse_color=(.09,.11,.14,1);ground.data.materials.append(mat)
cam=s.camera;cam.location=(28,-17,13);cam.rotation_euler=(Vector((0,-1.6,6.5))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=22
s.render.resolution_percentage=100;s.frame_set(80)
report={'frames':120,'max_ik_error':{},'scope':'Animation study; original rig and RAIKEN geometry. Not integrated into Unity. Mechanical collision not certified.'}
for f in range(1,121):
 s.frame_set(f);bpy.context.view_layer.update()
 for side in ['R','L']:
  for tip,ctrl in [('forearm','hand'),('shin','foot')]:
   err=(r.pose.bones[tip+'.'+side].tail-r.pose.bones['IK_'+ctrl+'.'+side].head).length
   n=ctrl+'.'+side;report['max_ik_error'][n]=max(err,report['max_ik_error'].get(n,0))
print(report,flush=True);open(out+'/audit_final.json','w').write(json.dumps(report,indent=2))
s.frame_set(80);bpy.ops.wm.save_as_mainfile(filepath=out+'/FRAME01_HEAVY_TRIPLE.blend')
for f in [10,18,43,63,68,74,80,94]:
 s.frame_set(f);s.render.filepath=out+'/pose_%03d.png'%f;bpy.ops.render.render(write_still=True)
s.render.resolution_percentage=75;s.render.filepath=out+'/frames/f_';bpy.ops.render.render(animation=True)
