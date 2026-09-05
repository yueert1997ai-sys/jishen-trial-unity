"""Render imported, skinned action poses for retarget inspection (not runtime evidence)."""
import bpy
import pathlib
import sys
from mathutils import Vector

project, output = map(pathlib.Path, sys.argv[sys.argv.index('--') + 1:])
bpy.ops.wm.open_mainfile(filepath=str(project / 'tools/hero/source/RiggedSentinel.blend'))
rig = bpy.data.objects['SentinelRig']
bpy.ops.object.camera_add(location=(4, -7, 4))
camera = bpy.context.object
camera.rotation_euler = (Vector((0, 0, 1.65)) - camera.location).to_track_quat('-Z', 'Y').to_euler()
camera.data.type = 'ORTHO'
camera.data.ortho_scale = 4.3
bpy.context.scene.camera = camera
for location, energy in [((3, -4, 6), 550), ((-3, -1, 3), 300), ((0, 3, 4), 600)]:
    bpy.ops.object.light_add(type='AREA', location=location)
    light = bpy.context.object
    light.data.energy = energy
    light.data.size = 4
    light.rotation_euler = (Vector((0, 0, 1.6)) - light.location).to_track_quat('-Z', 'Y').to_euler()
scene = bpy.context.scene
scene.world = bpy.data.worlds.new('InspectionWorld')
scene.world.color = (.15, .15, .15)
scene.render.engine = 'CYCLES'
scene.cycles.samples = 16
scene.render.resolution_x = scene.render.resolution_y = 640
scene.render.resolution_percentage = 100
for name, frame in [('Idle', 1), ('Run', 7), ('Run', 19), ('SwordSlash', 5), ('SwordSlash', 12), ('Shoot', 8), ('Jump', 5), ('Jump', 10), ('Jump', 15)]:
    action = bpy.data.actions[name]
    rig.animation_data.action = action
    rig.animation_data.action_slot = action.slots[0]
    scene.frame_set(frame)
    scene.render.filepath = str(output / (name + '-' + str(frame) + '.png'))
    bpy.ops.render.render(write_still=True)
