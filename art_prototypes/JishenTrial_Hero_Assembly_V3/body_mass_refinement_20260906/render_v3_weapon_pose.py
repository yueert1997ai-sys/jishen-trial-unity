import bpy,pathlib,sys
args={a.split('=',1)[0]:a.split('=',1)[1] for a in sys.argv if '=' in a}
W=pathlib.Path(__file__).resolve().parent;D=W/'iteration_v3/weapon_pose';R=pathlib.Path(args.get('out',str(D/'renders')));R.mkdir(exist_ok=True,parents=True)
bpy.ops.wm.open_mainfile(filepath=args.get('source',str(D/'VALKYR_V3_WEAPON_POSE.blend')));sc=bpy.context.scene
dg=bpy.context.evaluated_depsgraph_get();snap=[]
for o in sc.objects:
 if o.type=='MESH' and not o.hide_render:snap.append((o,bpy.data.meshes.new_from_object(o.evaluated_get(dg),preserve_all_data_layers=True,depsgraph=dg)))
for o,me in snap:o.animation_data_clear();o.modifiers.clear();o.data=me
pref=bpy.context.preferences.addons['cycles'].preferences;pref.compute_device_type='OPTIX';pref.get_devices()
for dev in pref.devices:dev.use=dev.type=='OPTIX'
sc.cycles.device='GPU';sc.cycles.samples=int(args.get('samples','24'));sc.render.resolution_percentage=int(args.get('percent','100'))
reference=sc.get('weapon_pose_revision')==2
for name,cam,res in [('ASSEMBLED','RAIKEN_ASSEMBLED_3Q',(1800,1500)),('HAND_FIT','RAIKEN_HAND_FIT',((1600,1300) if reference else (1500,1300))),('WEAPON','RAIKEN_POSED_WEAPON_3Q',((1800,1500) if reference else (2000,1200)))]:
 if name not in args.get('shots','ASSEMBLED,HAND_FIT,WEAPON').split(','):continue
 if name=='WEAPON':
  for o in sc.objects:
   if o.type=='MESH' and not o.name.startswith('RK_'):o.hide_render=True
 sc.camera=bpy.data.objects[cam];sc.render.resolution_x,sc.render.resolution_y=res;sc.render.filepath=str(R/(name+'.png'))
 bpy.ops.render.render(write_still=True);print('POSE_RENDER_DONE',name,flush=True)
