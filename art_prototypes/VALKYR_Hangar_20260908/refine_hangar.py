import bpy
from pathlib import Path
from mathutils import Vector
out=Path(__file__).resolve().parent
scene=bpy.data.scenes['VALKYR | Pre-sortie Hangar'];bpy.context.window.scene=scene
camera=bpy.data.objects['01 | Pre-sortie hero - UI room at right']
camera.data.lens=46
camera.rotation_euler=(Vector((.5,0,2.0))-camera.location).to_track_quat('-Z','Y').to_euler()
camera=bpy.data.objects['03 | Head and chest inspection']
camera.data.lens=75
camera.rotation_euler=(Vector((0,0,3.05))-camera.location).to_track_quat('-Z','Y').to_euler()
scene.view_settings.exposure=-.6
# Align the diagnostic text with its screen instead of floating through the bezel.
for name in ('Diagnostic monitor frame','Diagnostic screen'):
    bpy.data.objects[name].rotation_euler=(0,0,0)
screen=bpy.data.objects['Diagnostic screen'];screen.location.z=1.54;screen.location.y=-.081
for ob in scene.objects:
    if ob.name.startswith(('Diagnostic title','Diagnostic line','Diagnostic progress bar')):
        ob.location.y=-.098
        if ob.type=='FONT':ob.visible_shadow=False
scene.frame_set(1)
scene.camera=bpy.data.objects['01 | Pre-sortie hero - UI room at right']
bpy.ops.wm.save_as_mainfile(filepath=str(out/'VALKYR_HANGAR_MASTER.blend'))
print('REFINED_SAVED')
