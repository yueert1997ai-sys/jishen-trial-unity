import bpy
from pathlib import Path
root=Path(__file__).resolve().parent
scene=bpy.context.scene
for camera,file,width,height in [('FIT_02_GRIP_CLOSEUP','06_RIGHT_HAND_GRIP',1500,1100),('FIT_03_OPPOSITE_SIDE','07_VALKYR_OPPOSITE',1600,1350),('FIT_04_WEAPON_SIDE','08_FULL_HEIGHT_PROFILE',1600,1400)]:
    scene.camera=bpy.data.objects[camera]
    scene.render.resolution_x=width;scene.render.resolution_y=height
    scene.render.filepath=str(root/'renders'/f'{file}.png')
    bpy.ops.render.render(write_still=True)
scene.camera=bpy.data.objects['FIT_01_FULL_MECHA'];scene.render.resolution_x=1600;scene.render.resolution_y=1350
scene.render.filepath=str(root/'renders/05_VALKYR_CANNON_EQUIPPED.png')
bpy.ops.wm.save_as_mainfile(filepath=str(root/'VALKYR_TYPE08_MASTER.blend'))
print('FITTING_DETAIL_VIEWS_RENDERED')
