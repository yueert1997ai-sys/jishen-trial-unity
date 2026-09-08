import bpy
from pathlib import Path
from mathutils import Vector
from bpy_extras.object_utils import world_to_camera_view
out=Path(__file__).resolve().parent
scene=bpy.data.scenes['VALKYR | Pre-sortie Hangar'];bpy.context.window.scene=scene
scene.frame_set(1);bpy.context.view_layer.update()
arm_collection=bpy.data.collections['03 | Articulated service arms - retracted behind hero']
if not bpy.data.objects.get('Retracted left arm assembly'):
    groups={}
    for side in (-1,1):
        root=bpy.data.objects.new('Retracted left arm assembly' if side<0 else 'Retracted right arm assembly',None)
        arm_collection.objects.link(root);groups[side]=root
    for ob in list(arm_collection.objects):
        if ob.type=='EMPTY':continue
        points=[ob.matrix_world@Vector(v) for v in ob.bound_box]
        side=-1 if sum(p.x for p in points)<0 else 1
        matrix=ob.matrix_world.copy();ob.parent=groups[side];ob.matrix_world=matrix
    for side,root in groups.items():root.location=(side*.85,.85,0)
station=bpy.data.objects['Diagnostic pedestal']
station.location.y=.31;station.location.z=.64;station.scale.z=.8
camera=bpy.data.objects['02 | Complete maintenance bay']
camera.location=(9,-14,5.5);camera.data.lens=36
camera.rotation_euler=(Vector((0,1.5,2.3))-camera.location).to_track_quat('-Z','Y').to_euler()
camera=bpy.data.objects['04 | Backpack and hardpoints']
camera.location=(-1.5,2.7,3.4);camera.data.lens=35
camera.rotation_euler=(Vector((0,0,2.7))-camera.location).to_track_quat('-Z','Y').to_euler()
camera=bpy.data.objects['05 | 360 degree inspection']
# Orbit remains inside the maintenance equipment, with a wide lens for the whole body.
camera.location=(0,-3.2,1.15)
camera.rotation_euler=(-camera.location).to_track_quat('-Z','Y').to_euler()
camera.data.lens=23
# Fit the complete armored silhouette throughout the orbit in the saved wide viewport.
body=[o for o in scene.objects if o.type=='MESH' and o.name.startswith('LOD0_') and 'Blade' not in o.name and not o.hide_render]
bpy.context.view_layer.update()
corners=[o.matrix_world@Vector(v) for o in body for v in o.bound_box]
extent=0
for f in range(1,121):
    scene.frame_set(f);bpy.context.view_layer.update()
    for v in corners:
        p=world_to_camera_view(scene,camera,v)
        extent=max(extent,abs(p.x-.5),abs(p.y-.5))
camera.data.lens=23*min(1,.435/extent)
camera['framing']='Whole armored body fitted across all 120 orbit frames; carried sword may extend outside inspection crop.'
print('ORBIT_FIT_LENS',camera.data.lens)
scene.frame_set(1)
scene.camera=bpy.data.objects['01 | Pre-sortie hero - UI room at right']
bpy.ops.wm.save_as_mainfile(filepath=str(out/'VALKYR_HANGAR_MASTER.blend'))
print('INSPECTION_SPACE_CLEARED')
