"""Render the revised saved assembly for a focused head approval."""
from pathlib import Path
import bpy,json,hashlib,time
ROOT=Path(__file__).resolve().parent;OUT=ROOT/'stage_02_head';R=OUT/'renders'
model=OUT/'TYPE_E01_ELITE_HEAD_R02.blend'
bpy.ops.wm.open_mainfile(filepath=str(model))
sc=bpy.context.scene;bpy.context.view_layer.update()
missing=[i.filepath for i in bpy.data.images if i.source=='FILE' and not i.packed_file and not i.packed_files and i.filepath and not Path(bpy.path.abspath(i.filepath)).exists()]
assert not missing,missing
head=bpy.data.collections['03 HALF BREACHED HEAD']
assert not any('layered cranial lamella' in o.name for o in head.objects)
assert 'R02 | thin shattered visor ledge' in head.objects
assert 'R02 | small intense emitter' in head.objects
try:
    prefs=bpy.context.preferences.addons['cycles'].preferences;prefs.compute_device_type='OPTIX';prefs.get_devices()
    for device in prefs.devices:device.use=device.type=='OPTIX'
    sc.cycles.device='GPU' if any(d.use for d in prefs.devices) else 'CPU'
except Exception:sc.cycles.device='CPU'
report={'model':str(model),'sha256':hashlib.sha256(model.read_bytes()).hexdigest(),'reopened':True,'missing_images':missing,'head_meshes':sum(o.type=='MESH' for o in head.objects),'body_source':str(ROOT/'stage_01/TYPE_E01_ELITE_STAGE01.blend'),'rigged':False,'game_integrated':False}
(OUT/'saved_head_check.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('HEAD_FILE_REOPENED '+json.dumps(report),flush=True)
sc.cycles.samples=48
for shot,camname in [('HEAD_INVADED','R02_HEAD_INVADED'),('HEAD_FRONT','R02_HEAD_FRONT'),('HEAD_PROFILE','R02_HEAD_PROFILE'),('HEAD_REVERSE','R02_HEAD_REVERSE'),('HERO','ELITE_HERO'),('GAME_READ','ELITE_GAME_READ')]:
    camera=bpy.data.objects[camname];sc.camera=camera
    sc.render.resolution_x=camera['resolution'][0];sc.render.resolution_y=camera['resolution'][1]
    sc.render.filepath=str(R/(shot+'.png'));t=time.time();bpy.ops.render.render(write_still=True)
    print('R02_RENDER '+shot+' '+str(round(time.time()-t,2)),flush=True)
# Camera changes are temporary and do not alter the saved head assembly.
