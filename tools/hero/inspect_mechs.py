"""Inspect downloaded candidates and render their actual bind geometry in Blender."""
import bpy
import json
import math
import pathlib
import sys
from mathutils import Vector


root = pathlib.Path(sys.argv[sys.argv.index('--') + 1])
reports = []
for filename in ['George.fbx', 'Leela.fbx', 'Mike.fbx', 'Stan.fbx', 'joney-rigged-robot.glb']:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    path = root / filename
    if path.suffix == '.fbx':
        bpy.ops.import_scene.fbx(filepath=str(path))
    else:
        bpy.ops.import_scene.gltf(filepath=str(path))
    meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    points = [o.matrix_world @ Vector(c) for o in meshes for c in o.bound_box]
    lo = Vector(tuple(min(v[i] for v in points) for i in range(3)))
    hi = Vector(tuple(max(v[i] for v in points) for i in range(3)))
    armatures = [o for o in bpy.context.scene.objects if o.type == 'ARMATURE']
    report = dict(file=filename, bounds=[list(lo), list(hi)],
                  triangles=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in meshes),
                  meshes=[dict(name=o.name, vertices=len(o.data.vertices), groups=len(o.vertex_groups)) for o in meshes],
                  rigs=[dict(name=o.name, scale=list(o.scale), bones=[b.name for b in o.data.bones]) for o in armatures],
                  actions=[dict(name=a.name, frames=list(a.frame_range)) for a in bpy.data.actions],
                  images=[dict(name=i.name, path=i.filepath, packed=bool(i.packed_file)) for i in bpy.data.images])
    reports.append(report)
    for arm in armatures:
        arm.animation_data_clear()
        for bone in arm.pose.bones:
            bone.matrix_basis.identity()
    center = (hi + lo) / 2
    height = max(hi.z - lo.z, 1)
    bpy.ops.object.camera_add(location=center + Vector((0.8, -1.6, .6)) * height)
    camera = bpy.context.object
    camera.rotation_euler = (center - camera.location).to_track_quat('-Z', 'Y').to_euler()
    camera.data.type = 'ORTHO'
    camera.data.ortho_scale = height * 1.4
    bpy.context.scene.camera = camera
    for position, energy in [((1, -2, 3), 180), ((-2, -1, 1), 70), ((1, 2, 2), 140)]:
        bpy.ops.object.light_add(type='AREA', location=center + Vector(position) * height)
        light = bpy.context.object
        light.data.energy = energy * height * height
        light.data.shape = 'DISK'
        light.data.size = height * 2
        light.rotation_euler = (center - light.location).to_track_quat('-Z', 'Y').to_euler()
    scene = bpy.context.scene
    scene.world = bpy.data.worlds.new('InspectionWorld')
    scene.world.use_nodes = True
    scene.world.node_tree.nodes['Background'].inputs['Color'].default_value = (.14, .16, .19, 1)
    scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = .45
    scene.render.engine = 'CYCLES'
    scene.cycles.samples = 16
    scene.render.resolution_x = 640
    scene.render.resolution_y = 640
    scene.render.resolution_percentage = 100
    scene.render.filepath = str(root / (path.stem + '-geometry.png'))
    bpy.ops.render.render(write_still=True)
(root / 'inspection.json').write_text(json.dumps(reports, indent=2), encoding='utf-8')
print('HERO_INSPECTION ' + json.dumps(reports))
