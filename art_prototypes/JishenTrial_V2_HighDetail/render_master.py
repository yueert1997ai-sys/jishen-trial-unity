"""Actual Blender renders for visual iteration and final handoff. No generated pictures.
Use -- preview for bounded iteration; omit preview for full 2560-pixel outputs.
"""
import bpy, pathlib, json, sys, time, math
from mathutils import Vector
OUT=pathlib.Path(__file__).resolve().parent
scene=bpy.data.scenes['JISHEN_V2_MASTER'];bpy.context.window.scene=scene
preview='preview' in sys.argv
iteration=next((a.split('=')[1] for a in sys.argv if a.startswith('iteration=')),'01')
dest=OUT/('iterations/render_'+iteration if preview else 'renders');dest.mkdir(parents=True,exist_ok=True)
devices=[]
try:
    p=bpy.context.preferences.addons['cycles'].preferences;p.compute_device_type='OPTIX';p.get_devices()
    for d in p.devices:
        d.use=d.type=='OPTIX'
        if d.use:devices.append(d.name)
    if devices:scene.cycles.device='GPU'
except Exception as e:devices=['CPU: '+str(e)]

def aim(o,t):o.rotation_euler=(Vector(t)-o.location).to_track_quat('-Z','Y').to_euler()
def cam(name,at,target,scale):
    o=bpy.data.objects.get(name)
    if not o:
        d=bpy.data.cameras.new(name);o=bpy.data.objects.new(name,d);bpy.data.collections['CAMERAS'].objects.link(o)
    o.location=at;aim(o,target);o.data.type='ORTHO';o.data.ortho_scale=scale
    return o
cam('CAM_FRONT',(-.16,-12,1.74),(-.16,0,1.74),4.08)
cam('CAM_BACK',(-.16,12,1.74),(-.16,0,1.74),4.08)
cam('CAM_LEFT',(12,0,1.74),(0,0,1.74),4.08)
cam('CAM_RIGHT',(-12,-.07,1.74),(0,-.07,1.74),4.08)
cam('CAM_THREE_QUARTER',(-7,-12,4.9),(-.16,0,1.74),4.25)
cam('CAM_BACK_THREE_QUARTER',(7,12,4.9),(-.05,.12,1.75),4.25)
cam('CAM_HEAD',(-2.6,-6,3.8),(0,-.045,3.07),.66)
cam('CAM_CHEST',(-3.0,-7,3.7),(-.025,-.13,2.68),1.45)
cam('CAM_ARM',(-4,-6,2.9),(-.69,-.04,2.17),1.44)
cam('CAM_LEG',(-4,-7,1.98),(-.37,.05,.94),1.96)
cam('CAM_BACKPACK',(-4,7,3.9),(0,.32,2.57),1.98)
cam('CAM_WEAPON',(-3.5,-8,3.2),(-1.17,-.60,1.35),2.82)
cam('CAM_CANNON',(-3.8,-6,4.1),(-.45,.00,3.04),1.32)
if not bpy.data.objects.get('Studio_Ground'):
    d=bpy.data.meshes.new('Studio_Ground_Mesh');d.from_pydata([(-30,-30,-.004),(30,-30,-.004),(30,30,-.004),(-30,30,-.004)],[],[(0,1,2,3)])
    o=bpy.data.objects.new('Studio_Ground',d);bpy.data.collections['LIGHTS'].objects.link(o)
    m=bpy.data.materials.new('Studio_Ground_Gray');m.use_nodes=True;m.diffuse_color=(.48,.49,.51,1)
    pp=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED');pp.inputs['Base Color'].default_value=(.48,.49,.51,1);pp.inputs['Roughness'].default_value=.85
    d.materials.append(m)
scene.render.engine='CYCLES';scene.cycles.samples=32 if preview else 96
scene.cycles.use_denoising=True;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG';scene.render.image_settings.color_mode='RGB';scene.render.image_settings.color_depth='8'
scene.view_settings.view_transform='AgX';scene.view_settings.exposure=0
clay_material=bpy.data.materials.get('CLAY_ONLY_No_Styling')
if not clay_material:
    clay_material=bpy.data.materials.new('CLAY_ONLY_No_Styling');clay_material.use_nodes=True
    pp=next(n for n in clay_material.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
    pp.inputs['Base Color'].default_value=(.39,.39,.39,1);pp.inputs['Roughness'].default_value=.75
    pp.inputs['Metallic'].default_value=0
clay_material.use_fake_user=True
shots=[
 ('01_FRONT','CAM_FRONT','body',False,True),('02_BACK','CAM_BACK','body',False,True),
 ('03_LEFT','CAM_LEFT','body',False,True),('04_RIGHT','CAM_RIGHT','body',False,True),
 ('05_FRONT_3Q','CAM_THREE_QUARTER','body',False,True),('06_BACK_3Q','CAM_BACK_THREE_QUARTER','body',False,True),
 ('07_HEAD_CLOSEUP','CAM_HEAD','square',False,True),('08_CHEST_CLOSEUP','CAM_CHEST','square',False,True),
 ('09_ARM_CLOSEUP','CAM_ARM','square',False,True),('10_LEG_CLOSEUP','CAM_LEG','square',False,True),
 ('11_BACKPACK_CLOSEUP','CAM_BACKPACK','square',False,True),('12_WEAPON_CLOSEUP','CAM_WEAPON','weapon',False,True),
 ('13_FULL_BODY_BEAM_ON','CAM_THREE_QUARTER','body',False,True),('14_FULL_BODY_BEAM_OFF','CAM_THREE_QUARTER','body',False,False),
 ('15_SHOULDER_CANNON_CLOSEUP','CAM_CANNON','square',False,True),('16_CLAY_FRONT_3Q','CAM_THREE_QUARTER','body',True,False),
 ('17_CLAY_BACK_3Q','CAM_BACK_THREE_QUARTER','body',True,False)]
if preview:
    shots=[shots[i] for i in (4,6,7,9,10,11,15)]
    if 'quick' in sys.argv:shots=shots[:3]
selected=next((a.split('=')[1].split(',') for a in sys.argv if a.startswith('only=')),None)
if selected:shots=[s for s in shots if any(s[0].startswith(p) for p in selected)]
root=bpy.data.objects['AntiShip_Blade_Display_Root']
report={'file_reopened':bpy.data.filepath,'iteration':iteration,'devices':devices,'renders':[]}
for name,camera,kind,clay,on in shots:
    root['Beam_On']=on;root.update_tag();scene.frame_set(scene.frame_current);bpy.context.view_layer.update()
    scene.camera=bpy.data.objects[camera]
    scene.render.resolution_x=1120 if preview else 2560
    scene.render.resolution_y=(1400 if preview else 3200) if kind in ('body','weapon') else (1120 if preview else 2560)
    for layer in scene.view_layers:layer.material_override=clay_material if clay else None
    hidden=[]
    if kind=='weapon':
        wp=set(root.children_recursive)
        for o in scene.objects:
            if o.type in ('MESH','CURVE') and o not in wp and o.name!='Studio_Ground' and not o.hide_render:
                hidden.append(o);o.hide_render=True
    path=dest/(name+'.png');scene.render.filepath=str(path)
    t=time.time();bpy.ops.render.render(write_still=True)
    for o in hidden:o.hide_render=False
    report['renders'].append({'file':str(path),'camera':camera,'width':scene.render.resolution_x,'height':scene.render.resolution_y,'clay':clay,'beam_on':on,'seconds':round(time.time()-t,2)})
    (OUT/'logs'/('renders_'+iteration+'.json' if preview else 'final_renders.json')).write_text(json.dumps(report,indent=2),encoding='utf8')
    print('V2_RENDER_SAVED',str(path),flush=True)
root['Beam_On']=True
root.update_tag();scene.frame_set(scene.frame_current);bpy.context.view_layer.update()
for layer in scene.view_layers:layer.material_override=None
scene.camera=bpy.data.objects['CAM_THREE_QUARTER']
# Save cameras, inspection setup and beam toggle to the working/master file.
if not preview:bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath)
print('V2_RENDER_SET_COMPLETE',flush=True)
