import bpy,pathlib,sys
W=pathlib.Path(__file__).resolve().parent
args={a.split('=',1)[0]:a.split('=',1)[1] for a in sys.argv if '=' in a}
D=W/'delivery';R=pathlib.Path(args.get('out',str(D/'renders')));R.mkdir(exist_ok=True,parents=True)
bpy.ops.wm.open_mainfile(filepath=args.get('source',str(D/'VALKYR_MASS_RAIKEN_MASTER.blend')));sc=bpy.context.scene
dg=bpy.context.evaluated_depsgraph_get();snap=[]
for o in sc.objects:
    if o.type=='MESH' and not o.hide_render:
        snap.append((o,bpy.data.meshes.new_from_object(o.evaluated_get(dg),preserve_all_data_layers=True,depsgraph=dg)))
for o,me in snap:o.animation_data_clear();o.modifiers.clear();o.data=me
p=bpy.context.preferences.addons['cycles'].preferences;p.compute_device_type='OPTIX';p.get_devices()
for d in p.devices:d.use=d.type=='OPTIX'
sc.cycles.device='GPU';sc.cycles.samples=32;sc.render.resolution_percentage=100
for name,cam,res in [('KNEES','V3B_CAM_KNEES',(1500,1500)),
    ('KNEE_SIDE','V3B_CAM_KNEE_SIDE',(1500,1500)),
    ('ASSEMBLED','RAIKEN_ASSEMBLED_3Q',(2000,1400)),
    ('RIGHT_ARM','RAIKEN_HAND_FIT',(1500,1300)),
    ('LEG_SIDE','V3B_CAM_LEG_SIDE',(1500,1500))]:
    if args.get('shots') and name not in args['shots'].split(','):continue
    sc.camera=bpy.data.objects[cam];sc.render.resolution_x,sc.render.resolution_y=res
    sc.render.filepath=str(R/(name+'.png'));bpy.ops.render.render(write_still=True)
    print('REVIEW_RENDER_DONE',name,flush=True)
