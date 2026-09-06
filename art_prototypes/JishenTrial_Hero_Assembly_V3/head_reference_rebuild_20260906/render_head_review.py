import bpy,pathlib,sys,json
W=pathlib.Path(__file__).resolve().parent
I=int(next((a.split('=')[1] for a in sys.argv if a.startswith('iter=')),'1'))
D=W/f'iteration_{I:02d}';R=D/next((a.split('=')[1] for a in sys.argv if a.startswith('folder=')),'renders');R.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(D/'ASSEMBLED.blend'));sc=bpy.context.scene
dg=bpy.context.evaluated_depsgraph_get();snap=[]
for o in sc.objects:
 if o.type in ('MESH','CURVE') and not o.hide_render:snap.append((o,bpy.data.meshes.new_from_object(o.evaluated_get(dg),preserve_all_data_layers=True,depsgraph=dg),o.matrix_world.copy()))
for o,me,w in snap:
 if o.type=='MESH':o.modifiers.clear();o.data=me
 else:
  nm=o.name;pa=o.parent;cs=list(o.users_collection);bpy.data.objects.remove(o,do_unlink=True);o=bpy.data.objects.new(nm,me)
  for c in cs:c.objects.link(o)
  o.parent=pa;o.matrix_world=w
sc.render.engine='CYCLES';sc.cycles.samples=40;sc.cycles.use_denoising=True
p=bpy.context.preferences.addons['cycles'].preferences;p.compute_device_type='OPTIX';p.get_devices()
for d in p.devices:d.use=d.type=='OPTIX'
sc.cycles.device='GPU';sc.view_settings.view_transform=next((a.split('=')[1] for a in sys.argv if a.startswith('transform=')),'Standard')
for o in sc.objects:
 if o.type=='LIGHT' and o.name.startswith('V3H_'):o.data.energy*=float(next((a.split('=')[1] for a in sys.argv if a.startswith('lights=')),'.22'))
sc.render.resolution_x=sc.render.resolution_y=int(next((a.split('=')[1] for a in sys.argv if a.startswith('res=')),'1100'))
sc.render.resolution_percentage=100;sc.render.image_settings.file_format='PNG';sc.render.image_settings.color_mode='RGBA'
shots=next((a.split('=')[1] for a in sys.argv if a.startswith('shots=')),'FRONT,LEFT,THREE_QUARTER,ON_BODY').split(',')
hidden={o:o.hide_render for o in sc.objects}
head=set(bpy.data.collections['HEAD_V3 | reference matched editable assembly'].all_objects)
body_world=sc.world
review_world=bpy.data.worlds.new('V3H_Review_World');review_world.use_nodes=True
bg=review_world.node_tree.nodes.get('Background') or next(n for n in review_world.node_tree.nodes if n.type=='BACKGROUND')
bg.inputs['Color'].default_value=(.075,.085,.11,1);bg.inputs['Strength'].default_value=.32
for name in shots:
 body=name in ('ON_BODY','FULL_BODY')
 sc.world=body_world if body else review_world
 for o,h in hidden.items():
  o.hide_render=h
  if o.type in ('MESH','CURVE') and not body and o not in head:o.hide_render=True
  if o.type=='LIGHT':o.hide_render=(o.name.startswith('V3H_')) if body else not o.name.startswith('V3H_')
 sc.camera=bpy.data.objects['ASSEMBLY_3Q'] if name=='FULL_BODY' else bpy.data.objects['V3H_CAM_'+name]
 sc.render.film_transparent=not body
 sc.render.filepath=str(R/(name+'.png'))
 bpy.ops.render.render(write_still=True);print('RENDER_DONE',name,flush=True)
