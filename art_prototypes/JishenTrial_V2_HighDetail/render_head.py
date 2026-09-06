"""Actual isolated head review and whole-body context renders from the saved model."""
import bpy,pathlib,math,time,json,sys
from mathutils import Vector
OUT=pathlib.Path(__file__).resolve().parent;sc=bpy.context.scene
final='final' in sys.argv;revision=next((a.split('=')[1] for a in sys.argv if a.startswith('revision=')),'04')
dest=OUT/('head_renders' if final else 'iterations/head_'+revision);dest.mkdir(exist_ok=True,parents=True)
p=bpy.context.preferences.addons['cycles'].preferences;p.compute_device_type='OPTIX';p.get_devices()
for d in p.devices:d.use=d.type=='OPTIX'
sc.render.engine='CYCLES';sc.cycles.device='GPU';sc.cycles.samples=64 if final else 32;sc.cycles.use_denoising=True
sc.render.resolution_x=sc.render.resolution_y=2560 if final else 1280;sc.render.resolution_percentage=100
sc.render.image_settings.file_format='PNG';sc.render.image_settings.color_mode='RGB';sc.view_settings.view_transform='AgX'
sc.view_settings.exposure=-.15
root=bpy.data.objects['AntiShip_Blade_Display_Root'];root['Beam_On']=False;root.update_tag();sc.frame_set(sc.frame_current);bpy.context.view_layer.update()
clay=bpy.data.materials.get('CLAY_ONLY_No_Styling')
if not clay:
    clay=bpy.data.materials.new('CLAY_ONLY_No_Styling');clay.use_nodes=True
    pp=next(n for n in clay.node_tree.nodes if n.type=='BSDF_PRINCIPLED');pp.inputs['Base Color'].default_value=(.28,.28,.28,1);pp.inputs['Roughness'].default_value=.8
clay.use_fake_user=True
cam=bpy.data.objects.get('CAM_HEAD_INSPECTION')
if not cam:
    d=bpy.data.cameras.new('CAM_HEAD_INSPECTION');cam=bpy.data.objects.new(d.name,d);bpy.data.collections['CAMERAS'].objects.link(cam)
cam.data.type='ORTHO';sc.camera=cam
initial={o.name:o.hide_render for o in sc.objects if o.type in ('MESH','CURVE')}
shots=[('01_HEAD_FRONT',(0,-6,3.143),(0,-.01,3.143),.48,False),
 ('02_HEAD_FRONT_3Q',(-3.8,-6,3.85),(0,-.02,3.142),.51,False),
 ('03_HEAD_RIGHT',(-6,0,3.145),(0,0,3.145),.50,False),
 ('04_HEAD_REAR_3Q',(3.5,6,3.76),(0,.0,3.142),.50,False),
 ('05_HEAD_CLAY',(-3.8,-6,3.85),(0,-.02,3.142),.51,True),
 ('06_HEAD_IN_CONTEXT',(-7,-12,4.9),(-.16,0,1.74),4.25,False)]
only=next((a.split('=')[1] for a in sys.argv if a.startswith('only=')),None)
if only:shots=[s for s in shots if s[0].startswith(tuple(only.split(',')))]
report=[]
for name,at,target,scale,gray in shots:
    for o in sc.objects:
        if o.type in ('MESH','CURVE'):
            o.hide_render=initial.get(o.name,False) if name.startswith('06') else (not o.name.startswith('HEAD04_') or initial.get(o.name,False))
    # The separate beam is driven; keep it explicitly off during isolated inspection.
    root['Beam_On']=False;root.update_tag();sc.frame_set(sc.frame_current);bpy.context.view_layer.update()
    cam.location=at;cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=scale
    sc.render.resolution_y=(3200 if final else 1600) if name.startswith('06') else (2560 if final else 1280)
    for l in sc.view_layers:l.material_override=clay if gray else None
    sc.render.filepath=str(dest/(name+'.png'));t=time.time();bpy.ops.render.render(write_still=True)
    report.append({'name':name,'image':sc.render.filepath,'seconds':round(time.time()-t,2),'gray':gray,'pixels':[sc.render.resolution_x,sc.render.resolution_y]})
    print('HEAD_RENDERED',name,flush=True)
for o in sc.objects:
    if o.name in initial:o.hide_render=initial[o.name]
root['Beam_On']=True;root.update_tag();sc.frame_set(sc.frame_current);bpy.context.view_layer.update()
for l in sc.view_layers:l.material_override=None
(OUT/'logs'/('head_renders_final.json' if final else 'head_renders_'+revision+'.json')).write_text(json.dumps(report,indent=2),encoding='utf8')
print('HEAD_REVIEW_RENDERS_COMPLETE',flush=True)
