"""Read back the integrated file and render updated orthographic views."""
from pathlib import Path
from mathutils import Vector
import bpy,json,hashlib,sys
ROOT=Path(__file__).resolve().parent;OUT=ROOT/'stage_04_TYPE01'
report=json.loads((OUT/'integration_report.json').read_text(encoding='utf8'))
bpy.ops.wm.open_mainfile(filepath=report['model'])
neutral=bpy.data.scenes[report['scenes'][0]];ready=bpy.data.scenes[report['scenes'][1]]
assert neutral.use_fake_user and ready.use_fake_user
check={'model':report['model'],'scenes':{},'contacts':{},'body_geometry_counts_preserved':True,'missing_images':[]}
for scene,prefix in ((neutral,''),(ready,'READY | ')):
    bpy.context.window.scene=scene;bpy.context.view_layer.update()
    root=bpy.data.objects[prefix+'TYPE01_LASER_RIFLE_ROOT']
    children=root.children_recursive
    count=sum(o.type=='MESH' for o in children)
    assert count==1148,(scene.name,count)
    groups=[o.name for o in children if o.type=='EMPTY' and o.name.replace(prefix,'').startswith('TYPE01_')]
    assert len(groups)==7,groups
    check['scenes'][scene.name]={'weapon_mesh_parts':count,'editable_subassemblies':len(groups),'socket_names':[o.name for o in children if 'SCK_' in o.name]}
    assert all(bpy.data.objects.get(prefix+'SCK_'+key) for key in ('PRIMARY_GRIP','SUPPORT_GRIP','MUZZLE','ENERGY_CELL','AI_HARDPOINT'))
    if scene==neutral:
        for name,expected in report['body_geometry'].items():
            obj=bpy.data.objects[name]
            assert {'vertices':len(obj.data.vertices),'polygons':len(obj.data.polygons)}==expected,name
    else:
        for side,socket in (('R','SCK_PRIMARY_GRIP'),('L','SCK_SUPPORT_GRIP')):
            hand=bpy.data.objects['READY | E01_'+side+'_HAND']
            actual=hand.matrix_world@Vector(report['contacts'][side]['contact_relative_to_wrist'])
            target=bpy.data.objects['READY | '+socket].matrix_world.translation
            error=(actual-target).length
            assert error<.00001,(side,error)
            check['contacts'][side]={'socket':socket,'error_m':error}
for image in bpy.data.images:
    if image.source in {'GENERATED','VIEWER'} or image.packed_file or image.packed_files:continue
    if image.filepath and not Path(bpy.path.abspath(image.filepath)).exists():check['missing_images'].append(image.filepath)
assert not check['missing_images'],check['missing_images']
check['packed_images']=sum(bool(i.packed_file or i.packed_files) for i in bpy.data.images)
assert check['packed_images']>=2
assert not bpy.data.objects.get('E01_RIFLE_ROOT') and not bpy.data.objects.get('READY | E01_RIFLE_ROOT')
check['prior_weapon_removed']=True
check['source_weapon_unchanged']=hashlib.sha256(Path(report['source_weapon']).read_bytes()).hexdigest()==report['source_sha256']
check['approved_mech_source_unchanged']=hashlib.sha256(Path(report['base']).read_bytes()).hexdigest()==report['base_sha256']
assert check['source_weapon_unchanged'] and check['approved_mech_source_unchanged']
check['passed']=True
(OUT/'saved_integration_check.json').write_text(json.dumps(check,indent=2),encoding='utf8')
print('INTEGRATION_REOPENED '+json.dumps(check),flush=True)
prefs=bpy.context.preferences.addons['cycles'].preferences
try:
    prefs.compute_device_type='OPTIX';prefs.get_devices()
    for device in prefs.devices:device.use=device.type=='OPTIX'
    for scene in (neutral,ready):scene.cycles.device='GPU'
except Exception:
    for scene in (neutral,ready):scene.cycles.device='CPU'
for scene,shot,cam in [(neutral,key,'E01_'+key) for key in ('FRONT','LEFT','BACK','RIGHT')]+[(ready,'READY_SIDE','E01_READY_SIDE')]:
    bpy.context.window.scene=scene;scene.camera=bpy.data.objects[cam]
    scene.render.resolution_x=1050;scene.render.resolution_y=1400
    scene.render.filepath=str(OUT/'renders'/(shot+'.png'))
    bpy.ops.render.render(write_still=True);print('INTEGRATION_FINAL_RENDER '+shot,flush=True)
