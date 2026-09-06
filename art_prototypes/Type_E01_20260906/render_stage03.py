"""Read the actual saved stage 03 file once, then render the review views."""
from pathlib import Path
import bpy,json,time,sys
from mathutils import Vector
ROOT=Path(__file__).resolve().parent
OUT=ROOT/'stage_03';R=OUT/'renders'
bpy.ops.wm.open_mainfile(filepath=str(OUT/'TYPE_E01_STAGE03.blend'))
print('REOPENED_SCENES '+str([(s.name,s.users) for s in bpy.data.scenes]),flush=True)
neutral=bpy.data.scenes['01 NEUTRAL - editable assembly']
ready=bpy.data.scenes['02 RIFLE READY - two hand pose']
head=bpy.data.collections['01 HEAD - review secondary forms']
body=bpy.data.collections['02 BODY - primary proportions only']
pack=bpy.data.collections['03 BACKPACK - primary masses']
gun=bpy.data.collections['05 E-01 ASSAULT RIFLE']
detail=bpy.data.collections['06 RIFLE DETAIL DISPLAY']
missing=[]
for image in bpy.data.images:
    if image.source in {'GENERATED','VIEWER'} or image.packed_file or image.packed_files:continue
    if image.filepath and not Path(bpy.path.abspath(image.filepath)).exists():missing.append(image.filepath)
assert not missing,missing
nm={o.data for o in neutral.objects if o.type=='MESH'}
pm={o.data for o in ready.objects if o.type=='MESH'}
report={'model':bpy.data.filepath,'scenes':{s.name:len(s.objects) for s in (neutral,ready)},'mesh_datablocks':len(bpy.data.meshes),'shared_mesh_datablocks':len(nm&pm),'missing_external_images':missing,'packed_references':sum(bool(i.packed_file or i.packed_files) for i in bpy.data.images)}
assert len(nm&pm)>500 and report['packed_references']>=1
bpy.context.window.scene=neutral
bpy.context.view_layer.update()
neutral_hand_matrix=bpy.data.objects['E01_L_HAND'].matrix_world.copy()
bpy.context.window.scene=ready
bpy.context.view_layer.update()
rifle=bpy.data.objects['READY | E01_RIFLE_ROOT']
left=bpy.data.objects['READY | E01_L_HAND']
# The support point is defined in the neutral model's construction coordinates.
hand_point=left.matrix_world@neutral_hand_matrix.inverted()@Vector((.670,.037,1.805))
gun_point=rifle.matrix_world@Vector((.23,0,-.093))
report['left_support_contact_error_m']=(hand_point-gun_point).length
assert report['left_support_contact_error_m']<.002, (report['left_support_contact_error_m'],list(hand_point),list(gun_point),str(left.matrix_world),str(bpy.data.objects['E01_L_HAND'].matrix_world),str(rifle.matrix_world))
for side in ('L','R'):
    foot=bpy.data.objects['READY | E01_'+side+'_FOOT']
    sole=bpy.data.objects['READY | '+side+' broad dark boot sole']
    minimum=min((sole.matrix_world@v.co).z for v in sole.data.vertices)
    report[side+'_sole_min_z']=minimum
    assert .050<minimum<.065
(OUT/'saved_model_check.json').write_text(json.dumps(report,indent=2),encoding='utf8')
print('SAVED_MODEL_CHECK '+json.dumps(report),flush=True)
prefs=bpy.context.preferences.addons['cycles'].preferences
try:
    prefs.compute_device_type='OPTIX';prefs.get_devices()
    for device in prefs.devices:device.use=device.type=='OPTIX'
    neutral.cycles.device='GPU'
except Exception:neutral.cycles.device='CPU'
bpy.context.window.scene=neutral
shots=('3Q','FRONT','LEFT','BACK','RIGHT','HEAD_FRONT','HEAD_SIDE','HEAD_3Q','RIFLE_SIDE','BACKPACK_DETAIL')
for shot in shots:
    head_only=shot.startswith('HEAD')
    rifle_only=shot.startswith('RIFLE')
    head.hide_render=rifle_only
    for col in (body,pack,gun):col.hide_render=head_only or rifle_only
    detail.hide_render=not rifle_only
    detail.hide_viewport=True
    neutral.camera=bpy.data.objects['E01_'+shot]
    neutral.render.resolution_x=1500 if rifle_only else 1000 if head_only or shot.endswith('DETAIL') else 1050
    neutral.render.resolution_y=650 if rifle_only else 1000 if head_only or shot.endswith('DETAIL') else 1400
    neutral.render.filepath=str(R/(shot+'.png'))
    t=time.time();bpy.ops.render.render(write_still=True)
    print('FINAL_RENDER '+shot+' '+str(round(time.time()-t,2)),flush=True)
# Do not persist temporary isolation flags or per-shot camera changes into the master.
