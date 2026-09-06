"""Reopen the delivered .blend and render approval views from its real geometry."""
from pathlib import Path
import bpy,json,time,sys,hashlib,re
from mathutils import Vector
ROOT=Path(__file__).resolve().parent;OUT=ROOT/'stage_01';R=OUT/'renders'
path=OUT/'TYPE_E01_ELITE_STAGE01.blend'
bpy.ops.wm.open_mainfile(filepath=str(path))
sc=bpy.data.scenes['01 ELITE - editable corruption assembly']
bpy.context.window.scene=sc;bpy.context.view_layer.update()
missing=[im.filepath for im in bpy.data.images if im.source=='FILE' and not im.packed_file and not im.packed_files and im.filepath and not Path(bpy.path.abspath(im.filepath)).exists()]
assert not missing,missing
master=bpy.data.objects['TYPE_E01_ELITE_MASTER']
assert all(abs(v-1.5)<1e-6 for v in master.scale)
assert len([o for o in sc.objects if re.fullmatch(r'DORSAL \| primary tendril \d{2}',o.name)])==6
assert not any('TYPE01_WEAPON' in o.name or 'RIFLE' in o.name.upper() for o in sc.objects if o.type=='MESH')
actor=[o for o in sc.objects if o.type=='MESH' and not o.name.startswith('STUDIO')]
verts=[o.matrix_world@Vector(v) for o in actor for v in o.bound_box]
report={'model':str(path),'model_sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'scene':sc.name,'mesh_parts':len(actor),'missing_images':missing,'packed_images':sum(bool(i.packed_file or i.packed_files) for i in bpy.data.images),'min_z':min(v.z for v in verts),'max_z':max(v.z for v in verts),'height_ratio':list(master.scale),'rigged':False,'game_integrated':False,'readback_passed':True}
(OUT/'saved_model_check.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('SAVED_MODEL_VERIFIED '+json.dumps(report),flush=True)
try:
    prefs=bpy.context.preferences.addons['cycles'].preferences;prefs.compute_device_type='OPTIX';prefs.get_devices()
    for d in prefs.devices:d.use=d.type=='OPTIX'
    sc.cycles.device='GPU' if any(d.use for d in prefs.devices) else 'CPU'
except Exception:sc.cycles.device='CPU'
shots=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
if not shots:shots=['HERO','FRONT','LEFT','BACK','RIGHT','HEAD','HEAD_INVADED','CORE','CLAW','DORSAL','GAME_READ']
sc.cycles.samples=56
for shot in shots:
    if shot=='HEAD_INVADED':
        original=bpy.data.objects['ELITE_HEAD'];cam=original.copy();cam.data=original.data.copy();sc.collection.objects.link(cam)
        cam.name='ELITE_HEAD_INVADED';cam.location.x=abs(cam.location.x)
        cam.rotation_euler=(Vector((.024,0,3.105))*1.5+Vector((0,0,-.084))-cam.location).to_track_quat('-Z','Y').to_euler()
    else:cam=bpy.data.objects['ELITE_'+shot]
    sc.camera=cam
    sc.render.resolution_x=cam['resolution'][0];sc.render.resolution_y=cam['resolution'][1]
    sc.render.filepath=str(R/(shot+'.png'))
    t=time.time();bpy.ops.render.render(write_still=True)
    print('RENDERED '+shot+' '+str(round(time.time()-t,2)),flush=True)
# Never save temporary rendering settings over the editable assembly.
