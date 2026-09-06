"""Quickly reopen the saved assembly and render the head without shoulder occlusion."""
from pathlib import Path
import bpy,json
ROOT=Path(__file__).resolve().parent;OUT=ROOT/'stage_02'
bpy.ops.wm.open_mainfile(filepath=str(OUT/'TYPE_E01_STAGE02.blend'))
sc=bpy.context.scene
root=bpy.data.objects['TYPE_E01_MASTER_ROOT']
head=bpy.data.collections['01 HEAD - review secondary forms']
body=bpy.data.collections['02 BODY - primary proportions only']
pack=bpy.data.collections['03 BACKPACK - primary masses']
rifle=bpy.data.collections['05 E-01 ASSAULT RIFLE']
assert 'STAGE 02' in root['review_status']
assert bpy.data.objects.get('H02 inset planar forehead shield')
assert bpy.data.objects.get('Rifle detachable magazine')
assert bpy.data.objects.get('Rifle hollow muzzle brake')
assert any(im.packed_file for im in bpy.data.images)
report={'saved_blend_reopened':True,'head_meshes':len([o for o in head.objects if o.type=='MESH']),
        'body_meshes':len([o for o in body.objects if o.type=='MESH']),
        'backpack_meshes':len([o for o in pack.objects if o.type=='MESH']),
        'rifle_meshes':len([o for o in rifle.objects if o.type=='MESH']),
        'reference_packed':True,'review_status':'awaiting user approval','action_pose_finalized':False}
(OUT/'quick_reopen_check.json').write_text(json.dumps(report,indent=2),encoding='utf8')
print('REOPEN CHECK '+json.dumps(report),flush=True)
body.hide_render=True;pack.hide_render=True;rifle.hide_render=True
bpy.data.collections['06 RIFLE DETAIL DISPLAY'].hide_render=True
sc.render.resolution_x=1000;sc.render.resolution_y=1000;sc.cycles.samples=32
prefs=bpy.context.preferences.addons['cycles'].preferences
prefs.compute_device_type='OPTIX';prefs.get_devices()
for d in prefs.devices:d.use=d.type=='OPTIX'
sc.cycles.device='GPU'
for shot in ('HEAD_FRONT','HEAD_3Q','HEAD_SIDE'):
    sc.camera=bpy.data.objects['E01_'+shot]
    sc.render.filepath=str(OUT/'renders'/(shot+'.png'))
    bpy.ops.render.render(write_still=True)
    print('HEAD REVIEW RENDERED '+shot,flush=True)
