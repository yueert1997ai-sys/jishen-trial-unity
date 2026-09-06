"""Create presentation poses from the current assembly, without editing meshes."""
import bpy, pathlib, hashlib, json, math
from mathutils import Matrix, Vector, Quaternion
from mathutils.bvhtree import BVHTree

W = pathlib.Path(__file__).resolve().parent
D = W / 'iteration_v3/social_showcase_20260906'
D.mkdir(parents=True, exist_ok=True)
source = W.parent / 'JishenTrial_ASSEMBLED_MASTER.blend'
rest_source = W / 'iteration_v3/reference_action_pose/SOURCE_BEFORE_REFERENCE_POSE.blend'
sha = hashlib.sha256(source.read_bytes()).hexdigest()
bpy.ops.wm.open_mainfile(filepath=str(rest_source))
rest = {o.name: o.matrix_basis.copy() for o in bpy.context.scene.objects if o.type == 'EMPTY'}
bpy.ops.wm.open_mainfile(filepath=str(source))
sc = bpy.context.scene
O = bpy.data.objects
root = O['AntiShip_Blade_Display_Root']
weapon_branch = set(root.children_recursive) | {root}
bpy.context.view_layer.update()
grips = {s: root.matrix_world.inverted() @ O['Hand.' + s].matrix_world for s in ('R', 'L')}
scale = root.matrix_world.to_scale().copy()

def update():
    bpy.context.view_layer.update()

def rotate(o, rotation):
    p = o.matrix_world.translation.copy()
    o.matrix_world = Matrix.Translation(p) @ rotation.to_4x4() @ Matrix.Translation(-p) @ o.matrix_world
    update()

def point_segment(name, child, direction):
    o = O[name]
    v = (O[child].matrix_world.translation - o.matrix_world.translation).normalized()
    rotate(o, v.rotation_difference(Vector(direction).normalized()).to_matrix())

# Recover the existing, grounded display stance with the V3 surfaces intact.
for name, mat in rest.items():
    o = O.get(name)
    if o and o not in weapon_branch:
        o.matrix_basis = mat
update()
for name in ('Forearm.R', 'Hand.R'):
    o = O[name]
    o.matrix_world = Matrix.Translation(o.matrix_world.translation)
    update()

# An unobstructed, neutral front pose for the concept/body comparison.
weapon_visibility = {o.name: o.hide_render for o in weapon_branch}
for o in weapon_branch:
    o.hide_render = True
sc['presentation_purpose'] = 'Neutral body comparison against the original front concept. Weapon omitted from this body-only view.'
sc['presentation_source_sha256'] = sha
bpy.context.preferences.filepaths.save_version = 0
bpy.ops.wm.save_as_mainfile(filepath=str(D / 'VALKYR_V3_BODY_COMPARISON.blend'))
for o in weapon_branch:
    o.hide_render = weapon_visibility[o.name]

# A compact, two-handed guard. The existing, fitted palm/handle transforms stay fixed.
axis = Vector((-.49, -.045, .87)).normalized()
center = Vector((-.03, -.44, 2.42))
x = -axis
y = Vector((0, 1, 0))
y = (y - x * y.dot(x)).normalized()
y = Quaternion(axis, math.radians(12)) @ y
z = x.cross(y).normalized()
weapon_rot = Matrix((x, y, z)).transposed()
weapon_world = Matrix.Translation(center) @ weapon_rot.to_4x4() @ Matrix.Diagonal((*scale, 1))
ik = []

def arm_ik(side, hand_world, pole):
    a, b, h = (O[n + '.' + side] for n in ('UpperArm', 'Forearm', 'Hand'))
    A, T = a.matrix_world.translation.copy(), hand_world.translation.copy()
    l1 = (b.matrix_world.translation - A).length
    l2 = (h.matrix_world.translation - b.matrix_world.translation).length
    delta = T - A
    distance, d = delta.length, delta.normalized()
    assert abs(l1 - l2) < distance < l1 + l2, (side, distance, l1 + l2)
    along = (l1*l1 - l2*l2 + distance*distance) / (2*distance)
    bend = Vector(pole) - A
    bend = (bend - d*bend.dot(d)).normalized()
    E = A + d*along + bend*math.sqrt(max(0, l1*l1 - along*along))
    point_segment(a.name, b.name, E - A)
    point_segment(b.name, h.name, T - b.matrix_world.translation)
    error = (h.matrix_world.translation - T).length
    assert error < 1e-5, error
    h.matrix_world = hand_world
    update()
    ik.append({'side': side, 'wrist_error_m': error, 'reach_fraction': distance/(l1+l2)})

arm_ik('R', weapon_world @ grips['R'], (-1.2, -.25, 1.85))
arm_ik('L', weapon_world @ grips['L'], (1.2, -.25, 1.85))
root.matrix_world = weapon_world
update()

def eval_shape(o):
    ev = o.evaluated_get(bpy.context.evaluated_depsgraph_get())
    me = ev.to_mesh()
    data = ([v.co.copy() for v in me.vertices], [tuple(p.vertices) for p in me.polygons])
    ev.to_mesh_clear()
    return data

def world_shape(o, data):
    vs = [o.matrix_world @ v for v in data[0]]
    return (Vector([min(v[k] for v in vs) for k in range(3)]),
            Vector([max(v[k] for v in vs) for k in range(3)]),
            BVHTree.FromPolygons(vs, data[1]))

# Only check the known long rear-guard/gauntlet intersection risk for this pose.
guards = [world_shape(o, eval_shape(o)) for o in sc.objects
          if o.type == 'MESH' and o.name.startswith('RK_Guard_') and 'Frame' in o.name]
clearance = []
for side in ('R', 'L'):
    b, h = O['Forearm.'+side], O['Hand.'+side]
    base, hm = b.matrix_world.copy(), h.matrix_world.copy()
    ax = (h.matrix_world.translation-b.matrix_world.translation).normalized()
    hand_branch = set(h.children_recursive)
    armor = [(o, eval_shape(o)) for o in b.children_recursive
             if o.type == 'MESH' and o not in hand_branch and o.name.startswith('V3B_')]
    trials = []
    for degrees in (0, -15, 15, -30, 30, -45, 45, -60, 60, -90, 90):
        b.matrix_world = base
        update()
        rotate(b, Quaternion(ax, math.radians(degrees)).to_matrix())
        h.matrix_world, root.matrix_world = hm, weapon_world
        update()
        contacts = 0
        for o, data in armor:
            shape = world_shape(o, data)
            for other in guards:
                if all(shape[0][k] <= other[1][k] and shape[1][k] >= other[0][k] for k in range(3)) and shape[2].overlap(other[2]):
                    contacts += 1
        trials.append([degrees, contacts])
        if contacts == 0:
            break
    assert contacts == 0, (side, trials)
    clearance.append({'side': side, 'roll_degrees': degrees, 'trials': trials})

grip_errors = {s: max(abs((root.matrix_world.inverted() @ O['Hand.'+s].matrix_world)[i][j]-grips[s][i][j])
                         for i in range(4) for j in range(4)) for s in ('R','L')}
assert max(grip_errors.values()) < 1e-4, grip_errors
sc['presentation_purpose'] = 'Grounded, compact two-handed guard for social four-view presentation.'
sc['presentation_source_sha256'] = sha
sc['presentation_only'] = True
bpy.ops.wm.save_as_mainfile(filepath=str(D / 'VALKYR_V3_SOCIAL_DISPLAY.blend'))
assert hashlib.sha256(source.read_bytes()).hexdigest() == sha
report = {'source': str(source), 'source_sha256': sha, 'source_unchanged': True,
          'mesh_count': sum(o.type == 'MESH' for o in sc.objects),
          'arm_ik': ik, 'gauntlet_clearance': clearance, 'grip_transform_errors': grip_errors,
          'blade_axis': list(axis), 'body_reference': str(W.parent/'body_reference_rebuild_20260906/REFERENCE.png')}
(D/'presentation_manifest.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print('SOCIAL_PRESENTATION_READY', json.dumps(report), flush=True)
