import bpy
from pathlib import Path
ROOT=Path(__file__).resolve().parent
scene=bpy.context.scene
for camera,file,height in [('01_HERO_THREE_QUARTER','01_CANNON_HERO.png',1050),('02_SIDE_PROFILE','02_CANNON_SIDE.png',750),('05_TOP_ORTHOGRAPHIC','03_CANNON_TOP.png',650),('03_RECEIVER_DETAIL','04_RECEIVER_DETAIL.png',1100)]:
    scene.camera=bpy.data.objects[camera]
    if camera=='05_TOP_ORTHOGRAPHIC':scene.camera.rotation_euler=(0,0,3.141592653589793)
    scene.render.resolution_y=height
    scene.render.filepath=str(ROOT/'renders'/file)
    bpy.ops.render.render(write_still=True)
scene.camera=bpy.data.objects['01_HERO_THREE_QUARTER'];scene.render.resolution_y=1050
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'HC09_CANNON_MASTER.blend'))
print('FOUR_REFERENCE_VIEWS_RENDERED')
