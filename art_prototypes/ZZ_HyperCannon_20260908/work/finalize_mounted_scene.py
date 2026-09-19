import bpy,json,hashlib
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[1]
assert Path(bpy.data.filepath).name=='VALKYR_TYPE08_MASTER.blend'
gun=bpy.data.objects['TYPE08_RIGHT_HAND_MOUNT'];hand=bpy.data.objects['Hand.R']
world=gun.matrix_world.copy();gun.parent=hand;gun.matrix_world=world
bpy.context.view_layer.update()
report=json.loads((root/'work/fitting_report.json').read_text(encoding='utf-8'))
report['gun_parent']=hand.name
report['right_grip_error_m']=(gun.matrix_world.translation-bpy.data.objects['V3B_Sword_Grip_Socket'].matrix_world.translation).length
report['source_hero_unchanged']=hashlib.sha256(Path(report['source_hero']).read_bytes()).hexdigest()==report['source_sha256']
assert report['right_grip_error_m']<.001 and report['source_hero_unchanged']
visible_cannons=[o.name for o in bpy.context.scene.objects if o.type=='MESH' and o.name.startswith('TYPE08') and not o.hide_render]
assert visible_cannons==['TYPE08_RIGHT_HAND_CANNON'],visible_cannons
bpy.ops.object.select_all(action='DESELECT')
gun.select_set(True);bpy.context.view_layer.objects.active=gun
scene=bpy.context.scene
scene.camera=bpy.data.objects['FIT_01_FULL_MECHA']
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            area.spaces.active.use_local_camera=False
            area.spaces.active.camera=scene.camera
            area.spaces.active.region_3d.view_perspective='CAMERA'
            area.spaces.active.region_3d.view_camera_zoom=0
            area.spaces.active.region_3d.view_camera_offset=(0,0)
            area.spaces.active.overlay.show_overlays=False
            area.tag_redraw()
scene['delivery']='TYPE-08 full-height cannon equipped on the original VALKYR V3 right hand; 2026-09-08'
(root/'work/fitting_report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
bpy.ops.wm.save_as_mainfile(filepath=str(root/'VALKYR_TYPE08_MASTER.blend'))
print(json.dumps(report))
