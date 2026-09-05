"""Reproducible joney_lol rig + CC0 Quaternius motion retarget, Blender 5.2."""
import bpy
import json
import math
import pathlib
import struct
import sys
from mathutils import Matrix, Quaternion, Vector
from mathutils.kdtree import KDTree

project = pathlib.Path(sys.argv[sys.argv.index('--') + 1])
source_dir = project / 'tools/hero/source'
out = project / 'Assets/Art/Hero'
out.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(source_dir / 'joney-rigged-robot.glb'))
target = next(o for o in bpy.context.scene.objects if o.type == 'ARMATURE')
meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH' and o.vertex_groups]
for obj in list(bpy.context.scene.objects):
    if obj != target and obj not in meshes:
        bpy.data.objects.remove(obj, do_unlink=True)
world = {o: o.matrix_world.copy() for o in [target] + meshes}
for obj, matrix in world.items():
    obj.parent = None
    obj.data.transform(matrix)
    obj.matrix_world.identity()
points = [v.co for o in meshes for v in o.data.vertices]
low, high = min(v.z for v in points), max(v.z for v in points)
scale = 3.25 / (high - low)
normalization = Matrix.Scale(scale, 4) @ Matrix.Translation((0, 0, -low))
for obj in [target] + meshes:
    obj.data.transform(normalization)
target.name = 'SentinelRig'
for mesh in meshes:
    mesh.name = 'SentinelArmor'
    mesh.parent = target
    mesh.matrix_parent_inverse.identity()
    weighted = [v for v in mesh.data.vertices if sum(g.weight for g in v.groups) > .00001]
    tree = KDTree(len(weighted))
    for vertex in weighted:
        tree.insert(vertex.co, vertex.index)
    tree.balance()
    repaired = 0
    for vertex in mesh.data.vertices:
        if sum(g.weight for g in vertex.groups) > .00001:
            continue
        _, nearest, _ = tree.find(vertex.co)
        for group in mesh.data.vertices[nearest].groups:
            mesh.vertex_groups[group.group].add([vertex.index], group.weight, 'REPLACE')
        repaired += 1
    print('REPAIRED_UNWEIGHTED_VERTICES ' + str(repaired))
bpy.ops.import_scene.fbx(filepath=str(source_dir / 'Stan.fbx'))
source = next(o for o in bpy.context.scene.objects if o.type == 'ARMATURE' and o != target)
source_actions = list(bpy.data.actions)
mapping = {'spine.002': 'Body', 'spine.003': 'Chest', 'neck': 'Neck', 'head': 'Head'}
for side in ['L', 'R']:
    mapping.update({f'upper_arm.{side}': f'UpperArm.{side}', f'forearm.{side}': f'LowerArm.{side}',
                    f'hand.{side}': f'LowerArm.{side}', f'thigh.{side}': f'UpperLeg.{side}',
                    f'shin.{side}': f'LowerLeg.{side}', f'foot.{side}': f'Foot.{side}'})
report = {'source': [], 'target': [], 'clips': [], 'materials': []}
for label, rig in [('source', source), ('target', target)]:
    report[label] = [dict(name=b.name, parent=b.parent.name if b.parent else None,
                         head=list(b.head_local), tail=list(b.tail_local)) for b in rig.data.bones]
glb_bytes = (source_dir / 'joney-rigged-robot.glb').read_bytes()
gltf = json.loads(glb_bytes[20:20 + struct.unpack_from('<I', glb_bytes, 12)[0]])
report['materials'] = [dict(name=m['name'], source_base_color=m['pbrMetallicRoughness']['baseColorFactor']) for m in gltf['materials']]

clips = ['Idle', 'Run', 'Walk', 'Run_Holding', 'Shoot', 'SwordSlash', 'Jump', 'HitRecieve_1', 'Death']
fps = 30
bpy.context.scene.render.fps = fps
target.animation_data_create()
source.animation_data_create()
for name in clips:
    original = next(a for a in source_actions if a.name.split('|')[-1] == name)
    source.animation_data.action = original
    source.animation_data.action_slot = original.slots[0]
    action = bpy.data.actions.new(name)
    action.use_fake_user = True
    target.animation_data.action = action
    start, end = map(int, original.frame_range)
    for frame in range(start, end + 1):
        bpy.context.scene.frame_set(frame)
        for bone in target.pose.bones:
            bone.matrix_basis.identity()
        bpy.context.view_layer.update()
        # Transfer rest-relative global rotations, preserve target limb lengths.
        for bone in target.pose.bones:
            source_name = mapping.get(bone.name)
            if source_name is None:
                continue
            mirror = Matrix.Identity(4)
            if name == 'Shoot':
                if source_name.endswith(('.L', '.R')):
                    source_name = source_name[:-1] + ('R' if source_name.endswith('L') else 'L')
                mirror[0][0] = -1
            src = source.pose.bones[source_name]
            source_pose = mirror @ src.matrix @ mirror
            source_rest = mirror @ src.bone.matrix_local @ mirror
            rest = bone.bone.matrix_local
            calibration = Quaternion()
            if bone.name.startswith(('upper_arm.', 'forearm.', 'hand.')):
                rest_arm = target.data.bones['forearm.' + bone.name[-1]] if bone.name.startswith('hand.') else bone.bone
                calibration = (rest_arm.tail_local - rest_arm.head_local).normalized().rotation_difference(
                    (mirror.to_3x3() @ (src.bone.tail_local - src.bone.head_local)).normalized())
            rotation = source_pose.to_quaternion() @ source_rest.to_quaternion().inverted() @ calibration @ rest.to_quaternion()
            position = rest.translation.copy()
            if bone.parent:
                position = bone.parent.matrix @ bone.parent.bone.matrix_local.inverted() @ position
            else:
                # Vertical weight shifts are visual; all horizontal root motion is removed.
                position.z += (src.matrix.translation.z - src.bone.matrix_local.translation.z) * scale * 2
            bone.matrix = Matrix.Translation(position) @ rotation.to_matrix().to_4x4()
            bpy.context.view_layer.update()
        for bone in target.pose.bones:
            if bone.name.endswith('.R') and bone.name.startswith(('f_index.', 'f_middle.', 'f_ring.', 'f_pinky.')):
                bone.rotation_mode = 'QUATERNION'
                bone.rotation_quaternion = Quaternion((1, 0, 0), math.radians(60 if '.01.' in bone.name else 70))
        bpy.context.view_layer.update()
        if name != 'Jump':
            depsgraph = bpy.context.evaluated_depsgraph_get()
            floor = min((o.matrix_world @ v.co).z for o in meshes for v in o.evaluated_get(depsgraph).data.vertices)
            root_bone = target.pose.bones['spine.002']
            matrix = root_bone.matrix.copy()
            matrix.translation.z += .045 - floor
            root_bone.matrix = matrix
            bpy.context.view_layer.update()
        for bone in target.pose.bones:
            bone.rotation_mode = 'QUATERNION'
            bone.keyframe_insert(data_path='rotation_quaternion', frame=frame)
            bone.keyframe_insert(data_path='location', frame=frame)
    report['clips'].append(dict(name=name, frames=[start, end]))
    track = target.animation_data.nla_tracks.new()
    track.name = name
    strip = track.strips.new(name, start, action)
    strip.action_frame_start = start
    strip.action_frame_end = end
    track.mute = True

# Explicit held poses avoid engine-specific zero-speed clip offset semantics.
for pose_name, clip_name, frame in [('DashPose', 'Jump', 5), ('CannonPose', 'Shoot', 8)]:
    action = bpy.data.actions[clip_name]
    target.animation_data.action = action
    target.animation_data.action_slot = action.slots[0]
    bpy.context.scene.frame_set(frame)
    bpy.context.view_layer.update()
    basis = {bone.name: bone.matrix_basis.copy() for bone in target.pose.bones}
    held = bpy.data.actions.new(pose_name)
    held.use_fake_user = True
    target.animation_data.action = held
    for key in [1, 2]:
        for bone in target.pose.bones:
            bone.matrix_basis = basis[bone.name]
            bone.keyframe_insert(data_path='rotation_quaternion', frame=key)
            bone.keyframe_insert(data_path='location', frame=key)
    track = target.animation_data.nla_tracks.new()
    track.name = pose_name
    track.strips.new(pose_name, 1, held)
    track.mute = True
    report['clips'].append(dict(name=pose_name, frames=[1, 2], sampled_from=clip_name, source_frame=frame))
target.animation_data.action = None
for obj in list(bpy.context.scene.objects):
    if obj != target and obj not in meshes:
        bpy.data.objects.remove(obj, do_unlink=True)
for action in source_actions:
    bpy.data.actions.remove(action)
for track in target.animation_data.nla_tracks:
    track.mute = False
bpy.ops.object.select_all(action='DESELECT')
for obj in [target] + meshes:
    obj.select_set(True)
bpy.context.view_layer.objects.active = target
bpy.ops.export_scene.fbx(filepath=str(out / 'RiggedSentinel.fbx'), use_selection=True,
                         object_types={'ARMATURE', 'MESH'}, add_leaf_bones=False,
                         apply_scale_options='FBX_SCALE_ALL',
                         axis_forward='-Z', axis_up='Y', bake_anim=True,
                         bake_anim_use_nla_strips=True, bake_anim_use_all_actions=False,
                         bake_anim_simplify_factor=0, mesh_smooth_type='FACE')
for track in target.animation_data.nla_tracks:
    track.mute = True
target.animation_data.action = bpy.data.actions['Idle']
target.animation_data.action_slot = bpy.data.actions['Idle'].slots[0]
bpy.context.scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=str(source_dir / 'RiggedSentinel.blend'))
(out / 'retarget-report.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print('HERO_RETARGET_OK')
