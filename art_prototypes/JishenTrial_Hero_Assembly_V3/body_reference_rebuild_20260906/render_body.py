import bpy,pathlib,sys
W=pathlib.Path(__file__).resolve().parent
stage=int(next((a.split('=')[1] for a in sys.argv if a.startswith('stage=')),'1'))
D=W/f'stage_{stage:02d}';R=D/'renders';R.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(D/'ASSEMBLED.blend'));sc=bpy.context.scene
# Freeze the evaluated geometry only inside this disposable render process.
dg=bpy.context.evaluated_depsgraph_get();snap=[]
for o in sc.objects:
 if o.type in ('MESH','CURVE') and not o.hide_render:
  snap.append((o,bpy.data.meshes.new_from_object(o.evaluated_get(dg),preserve_all_data_layers=True,depsgraph=dg),o.matrix_world.copy()))
for o,me,mw in snap:
 o.animation_data_clear()
 if o.type=='MESH':o.modifiers.clear();o.data=me
 else:
  nm=o.name;pa=o.parent;cols=list(o.users_collection);bpy.data.objects.remove(o,do_unlink=True);o=bpy.data.objects.new(nm,me)
  for c in cols:c.objects.link(o)
  o.parent=pa;o.matrix_world=mw
p=bpy.context.preferences.addons['cycles'].preferences;p.compute_device_type='OPTIX';p.get_devices()
for d in p.devices:d.use=d.type=='OPTIX'
sc.cycles.device='GPU';sc.cycles.samples=32
sc.render.resolution_x=sc.render.resolution_y=int(next((a.split('=')[1] for a in sys.argv if a.startswith('res=')),'1400'))
hidden={o:o.hide_render for o in sc.objects}
if any(a=='aligned=1' for a in sys.argv):
 from mathutils import Vector
 old=bpy.data.objects['V3B_CAM_LEFT'];cam=old.copy();cam.data=old.data.copy();cam.name='V3B_CAM_ALIGNED_LEFT';sc.collection.objects.link(cam);cam.location=(12,0,1.82);cam.rotation_euler=(Vector((0,0,1.82))-cam.location).to_track_quat('-Z','Y').to_euler()
for name in next((a.split('=')[1] for a in sys.argv if a.startswith('shots=')),'FRONT,THREE_QUARTER').split(','):
 for o,h in hidden.items():
  o.hide_render=h
  if name=='SIDE_CORE' and o.type=='MESH' and any(c.name.startswith(('BODY_V3 | 08','BODY_V3 | 09','BODY_V3 | 10')) for c in o.users_collection):o.hide_render=True
 sc.camera=bpy.data.objects['V3B_CAM_'+name];sc.render.filepath=str(R/(name+'.png'));bpy.ops.render.render(write_still=True);print('RENDER_DONE',name,flush=True)
