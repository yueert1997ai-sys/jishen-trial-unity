"""Swap only the weapon in saved copies of the accepted mech; pose the right forearm."""
import bpy,pathlib,json,sys,math,hashlib
from mathutils import Vector,Matrix
P=pathlib.Path(__file__).resolve().parent
mode=next((a.split('=')[1] for a in sys.argv if a.startswith('mode=')),'MASTER')
src='JishenTrial_ASSEMBLED_MASTER.blend' if mode=='MASTER' else 'JishenTrial_GAME_CANDIDATE.blend'
bpy.ops.wm.open_mainfile(filepath=str(P/'backups'/src))
sc=bpy.context.scene
old=bpy.data.objects['AntiShip_Blade_Display_Root']
old_parts=list(old.children_recursive)
preserved=[o for o in sc.objects if o.type=='MESH' and o not in old_parts]
def digest(objects):
    result={}
    for o in objects:
        h=hashlib.sha256()
        for v in o.data.vertices:h.update(bytes(str(tuple(v.co)),'ascii'))
        h.update('|'.join(m.name if m else 'None' for m in o.data.materials).encode())
        result[o.name]=h.hexdigest()
    return result
before=digest(preserved)
for o in old_parts+[old]:bpy.data.objects.remove(o,do_unlink=True)
weapon=P/('RAIKEN_MkII_MASTER.blend' if mode=='MASTER' else 'RAIKEN_MkII_GAME.blend')
with bpy.data.libraries.load(str(weapon),link=False) as (source,dest):
    dest.collections=[n for n in source.collections if n.startswith('RAIKEN |') and '08 Presentation' not in n]
for c in dest.collections:
    if c and c.name not in sc.collection.children:sc.collection.children.link(c)
root=bpy.data.objects['RAIKEN_MkII_Grip_Root'];root.name='AntiShip_Blade_Display_Root'
grip=bpy.data.objects['V3B_Sword_Grip_Socket']
forearm=bpy.data.objects['Forearm.R'];origin=forearm.matrix_world.translation.copy()
turn=Matrix.Rotation(math.radians(65),4,'Y')
forearm.matrix_world=Matrix.Translation(origin)@turn@Matrix.Translation(-origin)@forearm.matrix_world
bpy.context.view_layer.update()
axis=turn.to_3x3()@Vector(sc['sword_grip_axis']).normalized()
u=turn.to_3x3()@Vector((.9536,0,.301)).normalized();u=(u-axis*u.dot(axis)).normalized()
n=axis.cross(u).normalized()
rot=Matrix((tuple(-axis),tuple(-n),tuple(-u))).transposed().to_4x4()
scale=3.5/18.4
root.parent=grip;root.matrix_world=Matrix.Translation(grip.matrix_world.translation)@rot@Matrix.Scale(scale,4)
root['attachment']='Right hand; production scale matched to 18.5 m blade / 18.4 m mech'
root['display_pose']='Right forearm raised 65 degrees for ground clearance; authored joints retained'
root['Beam_On']=True;root.update_tag();bpy.context.view_layer.update()
sc['current_weapon']='RAIKEN Mk-II / sharpened detail revision 02'
sc['weapon_source']='art_prototypes/RAIKEN_MkII_20260906/RAIKEN_MkII_MASTER.blend'
sc['sword_grip_axis']=list(axis)
sc['sword_grip_center_units']=list(grip.matrix_world.translation/(2.974/348))
# Save all existing mech cameras; add an explicitly framed assembled showcase.
col=bpy.data.collections.new('RAIKEN | Assembled preview');sc.collection.children.link(col)
def cam(name,pos,target,width):
    d=bpy.data.cameras.new(name);d.type='ORTHO';d.ortho_scale=width
    o=bpy.data.objects.new(name,d);col.objects.link(o);o.location=pos;o.rotation_euler=(Vector(target)-o.location).to_track_quat('-Z','Y').to_euler();return o
cam('RAIKEN_ASSEMBLED_3Q',(2.5,-11,5.3),(-1.35,0,1.85),6.3)
cam('RAIKEN_ASSEMBLED_FRONT',(-1.35,-15,1.85),(-1.35,0,1.85),6.3)
target=grip.matrix_world.translation+axis*.25
cam('RAIKEN_HAND_FIT',target+Vector((.3,-6,2.3)),target,1.65)
d=bpy.data.lights.new('RAIKEN_Assembled_Weapon_Key','AREA');d.energy=400;d.shape='DISK';d.size=4
o=bpy.data.objects.new('RAIKEN_Assembled_Weapon_Key',d);col.objects.link(o);o.location=(-4,-5,4);o.rotation_euler=(Vector((-2,0,1.5))-o.location).to_track_quat('-Z','Y').to_euler()
sc.camera=bpy.data.objects['RAIKEN_ASSEMBLED_3Q'];sc.render.resolution_x=2000;sc.render.resolution_y=1400;sc.render.resolution_percentage=100
sc.render.image_settings.file_format='PNG';sc.render.image_settings.color_mode='RGBA';sc.render.film_transparent=True
sc.render.engine='CYCLES';sc.cycles.samples=24;sc.cycles.use_denoising=True
try:
    pref=bpy.context.preferences.addons['cycles'].preferences;pref.compute_device_type='OPTIX';pref.get_devices()
    for d in pref.devices:d.use=d.type=='OPTIX'
    sc.cycles.device='GPU'
except Exception:pass
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            area.spaces.active.shading.type='MATERIAL';area.spaces.active.region_3d.view_distance=7.5
            area.spaces.active.region_3d.view_location=Vector((-1.2,0,1.8));area.spaces.active.region_3d.view_rotation=sc.camera.rotation_euler.to_quaternion()
            area.spaces.active.overlay.show_overlays=False
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(P/('VALKYR_RAIKEN_'+mode+'.blend')))
after=digest(preserved)
assert before==after,'Non-weapon mesh data or materials were changed'
tip=bpy.data.objects['RAIKEN_BLADE_TIP'].matrix_world.translation
report={'mode':mode,'source':src,'preserved_nonweapon_meshes':len(preserved),'nonweapon_mesh_data_and_materials_identical':before==after,'old_weapon_parts_removed':len(old_parts),'new_weapon_meshes':sum(o.type=='MESH' for o in root.children_recursive),'grip_world':list(grip.matrix_world.translation),'tip_world':list(tip),'tip_above_ground':tip.z>0,'forearm_pose_degrees':65,'production_length_m':18.5*scale}
(P/('assembly_'+mode.lower()+'_manifest.json')).write_text(json.dumps(report,indent=2),encoding='utf-8')
if mode=='GAME':
    bpy.ops.object.select_all(action='DESELECT')
    for o in sc.objects:
        if o.type in ('MESH','EMPTY') and not o.hide_render:o.select_set(True)
    bpy.context.view_layer.objects.active=root
    bpy.ops.export_scene.gltf(filepath=str(P/'exports/VALKYR_RAIKEN_GAME.glb'),export_format='GLB',use_selection=True,export_apply=True,export_extras=True,export_animations=False)
    bpy.ops.export_scene.fbx(filepath=str(P/'exports/VALKYR_RAIKEN_GAME.fbx'),use_selection=True,object_types={'MESH','EMPTY'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL',axis_forward='-Z',axis_up='Y',bake_anim=False,add_leaf_bones=False,use_custom_props=True,path_mode='COPY',embed_textures=True)
if 'render=1' in sys.argv:
    sc.render.filepath=str(P/'renders'/('07_ASSEMBLED_'+mode+'.png'));bpy.ops.render.render(write_still=True)
    sc.camera=bpy.data.objects['RAIKEN_HAND_FIT'];sc.render.resolution_x=1500;sc.render.resolution_y=1300
    sc.render.filepath=str(P/'renders'/('08_HAND_FIT_'+mode+'.png'));bpy.ops.render.render(write_still=True)
print('ASSEMBLY_COMPLETE',json.dumps(report),flush=True)
