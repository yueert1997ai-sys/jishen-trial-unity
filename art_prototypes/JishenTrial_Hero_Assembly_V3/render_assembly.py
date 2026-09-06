"""Reopens the actual assembly snapshot; renders neutral-lit inspection images."""
import bpy,pathlib,sys,json,time
OUT=pathlib.Path(__file__).resolve().parent
bpy.ops.wm.open_mainfile(filepath=str(OUT/'ASSEMBLY_RENDER_SNAPSHOT.blend'))
sc=bpy.context.scene
p=bpy.context.preferences.addons['cycles'].preferences;p.compute_device_type='OPTIX';p.get_devices()
for d in p.devices:d.use=d.type=='OPTIX'
sc.cycles.device='GPU';sc.cycles.samples=48;sc.cycles.use_denoising=True
sc.render.image_settings.file_format='PNG';sc.render.image_settings.color_mode='RGB'
clay=bpy.data.materials.new('ASSEMBLY_PLAIN_CLAY');clay.use_nodes=True
bs=next(n for n in clay.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
bs.inputs['Base Color'].default_value=(.36,.36,.36,1);bs.inputs['Roughness'].default_value=.8
shots=[('FRONT','ASSEMBLY_FRONT',False,True),('LEFT','ASSEMBLY_LEFT',False,True),
 ('BACK','ASSEMBLY_BACK',False,True),('RIGHT','ASSEMBLY_RIGHT',False,True),('FRONT_3Q','ASSEMBLY_3Q',False,True),
 ('HEAD_ON_BODY','ASSEMBLY_HEAD',False,True),('HEAD_SIDE','ASSEMBLY_HEAD_SIDE',False,True),
 ('HEAD_FRONT','ASSEMBLY_HEAD_FRONT',False,True),('CLAY_3Q','ASSEMBLY_3Q',True,False),('BEAM_OFF','ASSEMBLY_3Q',False,False)]
only=next((a.split('=')[1].split(',') for a in sys.argv if a.startswith('only=')),None)
if only:shots=[s for s in shots if s[0] in only]
shots.sort(key=lambda s: {'FRONT':0,'FRONT_3Q':1,'HEAD_ON_BODY':2}.get(s[0],3))
report={'reopened':bpy.data.filepath,'gpu':[d.name for d in p.devices if d.use],'renders':[]}
root=bpy.data.objects['AntiShip_Blade_Display_Root']
for name,cam,isclay,on in shots:
    root['Beam_On']=on;root.update_tag();sc.frame_set(sc.frame_current);bpy.context.view_layer.update()
    sc.camera=bpy.data.objects[cam]
    for layer in sc.view_layers:layer.material_override=clay if isclay else None
    sc.render.resolution_x=1440 if name.startswith('HEAD') else 1280
    sc.render.resolution_y=1440 if name.startswith('HEAD') else 1600
    sc.render.resolution_percentage=100;sc.render.filepath=str(OUT/'renders'/(name+'.png'))
    t=time.time();bpy.ops.render.render(write_still=True)
    report['renders'].append({'file':sc.render.filepath,'seconds':round(time.time()-t,2),'clay':isclay,'beam_on':on})
    (OUT/'logs'/'render_assembly.json').write_text(json.dumps(report,indent=2),encoding='utf8')
    print('RENDERED',name,flush=True)
