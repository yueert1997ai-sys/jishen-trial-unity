import bpy,pathlib,math,json
from mathutils import Vector
P=pathlib.Path(__file__).resolve().parent;W=P/'refinement'
bpy.ops.wm.open_mainfile(filepath=str(W/'FBX_REIMPORT_VERIFIED.blend'))
sc=bpy.context.scene
with bpy.data.libraries.load(str(W/'PHASE_07.blend'),link=False) as (src,dst):
 dst.collections=['CAMERAS','LIGHTS'];dst.worlds=src.worlds;dst.objects=['Studio_Ground']
for c in dst.collections:sc.collection.children.link(c)
ground=dst.objects[0]
if ground and ground.name not in sc.objects:sc.collection.objects.link(ground)
if dst.worlds:sc.world=dst.worlds[0]
sc.camera=bpy.data.objects['ASSEMBLY_3Q'];sc.render.engine='CYCLES';sc.cycles.samples=32;sc.cycles.use_denoising=True
pr=bpy.context.preferences.addons['cycles'].preferences;pr.compute_device_type='OPTIX';pr.get_devices()
for d in pr.devices:d.use=d.type=='OPTIX'
sc.cycles.device='GPU';sc.view_settings.view_transform='AgX';sc.view_settings.exposure=0
sc.render.resolution_x=1000;sc.render.resolution_y=1250;sc.render.resolution_percentage=100;sc.render.image_settings.file_format='PNG'
beam=bpy.data.objects['LOD0_AntiShipBlade_Beam'];out=W/'renders'/'demo';out.mkdir(exist_ok=True)
for name,cam,on in [('FBX_3Q','ASSEMBLY_3Q',True),('FBX_BEAM_OFF','ASSEMBLY_3Q',False),('FBX_BACK','ASSEMBLY_BACK',True)]:
 sc.camera=bpy.data.objects[cam];beam.hide_render=not on;sc.render.filepath=str(out/(name+'.png'));bpy.ops.render.render(write_still=True);print('FBX_RENDER',name,flush=True)
beam.hide_render=True;rig=next(o for o in sc.objects if o.type=='ARMATURE')
for name,angle in [('Forearm.R',25),('Forearm.L',-18),('Thigh.R',12),('Shin.R',-20)]:
 pb=rig.pose.bones[name];pb.rotation_mode='XYZ';pb.rotation_euler.x=math.radians(angle)
bpy.context.view_layer.update();sc.camera=bpy.data.objects['ASSEMBLY_3Q'];sc.render.filepath=str(out/'FBX_ARTICULATION_CHECK.png');bpy.ops.render.render(write_still=True)
for pb in rig.pose.bones:pb.rotation_euler=(0,0,0)
beam.hide_render=False;bpy.context.view_layer.update()
bpy.ops.wm.save_as_mainfile(filepath=str(W/'FBX_REIMPORT_VERIFIED.blend'))
