"""Replace the mistaken early export with the documented V3 game asset."""
import bpy
import hashlib
import json
import shutil
from array import array
from pathlib import Path
from mathutils import Vector
from bpy_extras.object_utils import world_to_camera_view

out=Path(__file__).resolve().parent
source=out.parent/'JishenTrial_Hero_Assembly_V3/body_mass_refinement_20260906/iteration_v3/game_update/VALKYR_V3_GAME.blend'
scene=bpy.data.scenes['VALKYR | Pre-sortie Hangar']
bpy.context.window.scene=scene
scene.frame_set(1)
backup=out/'iterations/incorrect_early_model'
backup.mkdir(parents=True,exist_ok=True)
for name in ('VALKYR_HANGAR_MASTER.blend','manifest.json','verification.json'):
    target=backup/name
    if not target.exists():shutil.copy2(out/name,target)
if not (backup/'renders').exists():shutil.copytree(out/'renders',backup/'renders')

collection=bpy.data.collections['90 | VALKYR - current model copy']
old_arm=(0,0,0)
for ob in list(collection.all_objects):bpy.data.objects.remove(ob,do_unlink=True)
inventory=json.loads((out/'v3_source_inventory.json').read_text())
names={o['name'] for o in inventory['objects'] if o['name']!='V3H_REFERENCE_front_side_back_top'}
with bpy.data.libraries.load(str(source),link=False) as (src,dst):
    dst.objects=[name for name in src.objects if name in names]
objects=[o for o in dst.objects if o]
for ob in objects:collection.objects.link(ob)
assert bpy.data.objects.get('VALKYR_Model_Revision_V3_c7297dd9'), 'Missing V3 revision marker'
meshes=[o for o in objects if o.type=='MESH']
assert len(meshes)==29, 'Unexpected V3 mesh set'

def signature(ob):
    values=array('f',[0])*len(ob.data.vertices)*3
    ob.data.vertices.foreach_get('co',values)
    indices=array('i',[0])*len(ob.data.loops)
    ob.data.loops.foreach_get('vertex_index',indices)
    return hashlib.sha256(values.tobytes()+indices.tobytes()).hexdigest()
signatures={ob.name:signature(ob) for ob in meshes}
root=bpy.data.objects.new('VALKYR_INSPECTION_ROOT',None)
collection.objects.link(root)
for ob in objects:
    if ob.parent not in objects:
        world=ob.matrix_world.copy();ob.parent=root;ob.matrix_world=world
# The service dock defaults to a relaxed, unarmed standing pose.
bpy.data.objects['Forearm.R'].rotation_euler=old_arm
blade_root=bpy.data.objects.get('AntiShip_Blade_Display_Root')
if blade_root:
    for ob in [blade_root,*blade_root.children_recursive]:ob.hide_render=True;ob.hide_set(True)
for ob in meshes:
    if 'Blade_Beam' in ob.name:ob.hide_render=True;ob.hide_set(True)
body=[o for o in meshes if 'Blade' not in o.name]
bpy.context.view_layer.update()
points=[o.matrix_world@Vector(c) for o in body for c in o.bound_box]
lo=Vector(tuple(min(v[i] for v in points) for i in range(3)))
hi=Vector(tuple(max(v[i] for v in points) for i in range(3)))
scale=3.5/(hi.z-lo.z)
root.scale=(scale,)*3
root.location=(-(lo.x+hi.x)/2*scale,-(lo.y+hi.y)/2*scale,.379-lo.z*scale)
root['source']=str(source)
root['source_revision']='V3_c7297dd9'
root['height_m']=3.5
root['display_state']='V3 armor and head, relaxed standing, unarmed until the player selects a weapon.'
bpy.context.view_layer.update()
blade=bpy.data.objects['LOD0_AntiShip_Blade_Display_Root_V3']
blade_points=[blade.matrix_world@Vector(v) for v in blade.bound_box]
print('BLADE_BOUNDS',[[min(p[i] for p in blade_points),max(p[i] for p in blade_points)] for i in range(3)],flush=True)
print('FOREARM_POSE',list(old_arm),flush=True)

# Refit the existing inspection cameras to the correct body.
points=[o.matrix_world@Vector(c) for o in body for c in o.bound_box]
main=bpy.data.objects['01 | Pre-sortie hero - UI room at right']
main.data.lens=46
orbit=bpy.data.objects['05 | 360 degree inspection']
orbit.data.lens=23
extent=0
for frame in range(1,121):
    scene.frame_set(frame);bpy.context.view_layer.update()
    for v in points:
        p=world_to_camera_view(scene,orbit,v)
        extent=max(extent,abs(p.x-.5),abs(p.y-.5))
orbit.data.lens=23*min(1,.435/extent)
scene.frame_set(1);scene.camera=main
source_hash=hashlib.sha256(source.read_bytes()).hexdigest()
manifest={
 'scene':scene.name,'objects':len(scene.objects),
 'collections':{c.name:len(c.all_objects) for c in scene.collection.children},
 'source_hero':str(source),'source_hero_sha256':source_hash,
 'source_revision':'VALKYR_Model_Revision_V3_c7297dd9',
 'source_document':'art_prototypes/JishenTrial_Hero_Assembly_V3/body_mass_refinement_20260906/iteration_v3/README.md',
 'hero_meshes':len(meshes),'hero_geometry_signatures':signatures,
 'height_m':3.5,'cameras':[o.name for o in scene.objects if o.type=='CAMERA'],
 'scope':'Editable Blender hangar, unarmed default. Unity loadout integration documented in docs/HANGAR_LOADOUT_20260908.md.'
}
(out/'manifest.json').write_text(json.dumps(manifest,indent=2,ensure_ascii=False))
bpy.ops.wm.save_as_mainfile(filepath=str(out/'VALKYR_HANGAR_MASTER.blend'))
print('V3_REPLACEMENT_SAVED',len(meshes),source_hash,flush=True)
