"""Render the saved TYPE-01 asset with its actual materials and evaluated bevels."""
import bpy, pathlib, sys, json, hashlib
from mathutils import Vector
D=pathlib.Path(__file__).resolve().parent
args=dict(a.split('=',1) for a in sys.argv if '=' in a)
source=D/'BOSS_M14_EBR_MASTER.blend'
sha=hashlib.sha256(source.read_bytes()).hexdigest()
bpy.ops.wm.open_mainfile(filepath=str(source));sc=bpy.context.scene
bpy.context.preferences.filepaths.temporary_directory='D:/Tools/BlenderUserData/Temp'
pref=bpy.context.preferences.addons['cycles'].preferences
pref.compute_device_type='OPTIX';pref.get_devices()
for dev in pref.devices:dev.use=dev.type=='OPTIX'
sc.cycles.device='GPU';sc.cycles.samples=int(args.get('samples','64'))
sc.render.resolution_percentage=int(args.get('percent','100'))
sc.render.image_settings.color_mode='RGBA';sc.render.film_transparent=True
# Evaluate once for this disposable render process; the editable file is not saved.
dg=bpy.context.evaluated_depsgraph_get();snap=[]
for o in sc.objects:
    if o.type in ('MESH','CURVE','FONT') and not o.hide_render:
        me=bpy.data.meshes.new_from_object(o.evaluated_get(dg),preserve_all_data_layers=True,depsgraph=dg)
        snap.append((o,me))
for o,me in snap:
    ob=bpy.data.objects.new('Render_'+o.name,me);sc.collection.objects.link(ob);ob.matrix_world=o.matrix_world.copy()
    o.hide_render=True
R=D/args.get('out','renders');R.mkdir(parents=True,exist_ok=True)
shots=args.get('shots','HERO,SIDE,REFERENCE,REVERSE,TOP,STOCK,RECEIVER,HANDGUARD,MUZZLE').split(',')
manifest={'source_sha256':sha,'shots':[]}
for name in shots:
    cam=bpy.data.objects['CAM_'+name];sc.camera=cam
    sc.render.resolution_x,sc.render.resolution_y=cam['resolution']
    sc.render.filepath=str(R/(name+'.png'))
    bpy.ops.render.render(write_still=True)
    manifest['shots'].append({'name':name,'path':sc.render.filepath,'resolution':[sc.render.resolution_x,sc.render.resolution_y]})
    print('EBR_RENDER_DONE',name,flush=True)
assert hashlib.sha256(source.read_bytes()).hexdigest()==sha
manifest_path=R/'render_manifest.json'
if manifest_path.exists():
    previous=json.loads(manifest_path.read_text(encoding='utf-8'))
    if previous.get('source_sha256')==sha:
        retained=[s for s in previous.get('shots',[]) if s['name'] not in shots]
        manifest['shots']=retained+manifest['shots']
manifest_path.write_text(json.dumps(manifest,indent=2),encoding='utf-8')
print('EBR_RENDER_COMPLETE',flush=True)

