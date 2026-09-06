"""Merge reviewed body armor into the newer weapon master; never export game files."""
import bpy, bmesh, pathlib, hashlib, json, shutil
from mathutils import Matrix, Vector

W = pathlib.Path(__file__).resolve().parent
P = W.parent
D = W / 'delivery'
CURRENT = P / 'JishenTrial_ASSEMBLED_MASTER.blend'
BODY = D / 'VALKYR_MASS_MASTER.blend'
OUT = D / 'VALKYR_MASS_RAIKEN_MASTER.blend'
EXPECTED = '96baba31bb643b03dd3344ca9f727ebce9518cf553b6e07f72d1d04e873fbb61'
def filehash(p): return hashlib.sha256(p.read_bytes()).hexdigest()
assert filehash(CURRENT) == EXPECTED, 'Current master changed again; stop and inspect.'
BACKUP = W / 'SOURCE_WITH_RAIKEN_BEFORE_BODY_MERGE.blend'
if not BACKUP.exists(): shutil.copy2(CURRENT, BACKUP)
assert filehash(BACKUP) == EXPECTED

def is_body(o):
    return o.type == 'MESH' and o.name.startswith('V3B_') and not o.name.startswith('V3B_Sword_')

def meshhash(o):
    return hashlib.sha256(repr(([tuple(v.co) for v in o.data.vertices],
        [tuple(f.vertices) for f in o.data.polygons],
        [[tuple(l.uv) for l in uv.data] for uv in o.data.uv_layers])).encode()).hexdigest()

def proof(o):
    return {'mesh': meshhash(o) if o.type == 'MESH' else None,
        'matrix': [list(r) for r in o.matrix_world],
        'parent': o.parent.name if o.parent else None,
        'materials': [m.name if m else None for m in o.data.materials] if o.type == 'MESH' else [],
        'hide_render': o.hide_render}

bpy.ops.wm.open_mainfile(filepath=str(BODY))
bpy.context.view_layer.update()
body = {}
for o in bpy.context.scene.objects:
    if not is_body(o): continue
    body[o.name] = {'world': [list(r) for r in o.matrix_world],
        'parent': o.parent.name if o.parent else None,
        'parent_world': [list(r) for r in o.parent.matrix_world] if o.parent else None,
        'collections': [c.name for c in o.users_collection],
        'materials': [m.name if m else None for m in o.data.materials],
        'hash': meshhash(o)}
camera_names = [o.name for o in bpy.context.scene.objects if o.type == 'CAMERA' and
    o.name in ('V3B_CAM_KNEES','V3B_CAM_KNEE_SIDE','V3B_CAM_LEG_SIDE','V3B_CAM_LEGS')]

bpy.ops.wm.open_mainfile(filepath=str(BACKUP))
sc = bpy.context.scene
bpy.context.view_layer.update()
oldbody = [o for o in sc.objects if is_body(o)]
oldbody_set = set(oldbody)
preserved = {o.name: proof(o) for o in sc.objects if o not in oldbody_set and o.type in ('MESH','EMPTY')}
existing = {o.name:o for o in bpy.data.objects if o not in oldbody_set}
canonical_materials = {m.name:m for m in bpy.data.materials}
for o in sc.objects:
    if o not in oldbody_set:
        assert o.parent not in oldbody_set, ('Retained object parent is being replaced',o.name)
for name,meta in body.items():
    assert not meta['parent'] or meta['parent'] in existing, ('Unmapped body parent',name,meta['parent'])
for o in oldbody: bpy.data.objects.remove(o,do_unlink=True)
before_objects = set(bpy.data.objects)
missing_cameras = [n for n in camera_names if n not in existing]
with bpy.data.libraries.load(str(BODY),link=False) as (src,dst):
    dst.objects = list(body) + missing_cameras
loaded = {n:o for n,o in zip(list(body)+missing_cameras,dst.objects)}
assert all(loaded.values())
for name,meta in body.items():
    o = loaded[name]
    assert o.name == name, ('Name collision',name,o.name)
    parent = existing.get(meta['parent'])
    world = Matrix(meta['world'])
    if parent: world = parent.matrix_world @ Matrix(meta['parent_world']).inverted() @ world
    o.parent = parent
    o.matrix_parent_inverse = Matrix.Identity(4)
    o.matrix_world = world
    for c in list(o.users_collection): c.objects.unlink(o)
    for cname in meta['collections']:
        col = bpy.data.collections.get(cname)
        if col is None:
            col = bpy.data.collections.new(cname);sc.collection.children.link(col)
        col.objects.link(o)
    for i,mname in enumerate(meta['materials']):
        if mname in canonical_materials: o.data.materials[i] = canonical_materials[mname]
    assert meshhash(o) == meta['hash'], ('Imported body data changed',name)

preview = bpy.data.collections.get('RAIKEN | Assembled preview')
for name in missing_cameras:
    o = loaded[name]
    o.parent = None
    preview.objects.link(o)
for o in list(set(bpy.data.objects)-before_objects-set(loaded.values())):
    assert not any(c in loaded.values() for c in o.children), ('Unremapped dependency',o.name)
    bpy.data.objects.remove(o,do_unlink=True)
bpy.context.view_layer.update()
after = {n:proof(bpy.data.objects[n]) for n in preserved}
differences = [n for n in preserved if preserved[n] != after[n]]
assert not differences, ('Retained head, weapon, frame or rig changed',differences)

geometry_errors = []
dg = bpy.context.evaluated_depsgraph_get()
for name in body:
    o = bpy.data.objects[name]
    assert o.data.uv_layers, ('Missing UV',name)
    ev = o.evaluated_get(dg);me = ev.to_mesh();bm = bmesh.new();bm.from_mesh(me)
    bad = [sum(not e.is_manifold for e in bm.edges),sum(f.calc_area()<1e-13 for f in bm.faces)]
    bm.free();ev.to_mesh_clear()
    if any(bad): geometry_errors.append([name,bad])
assert not geometry_errors, geometry_errors
for side in ('L','R'):
    assert any(n.startswith('V3B_Knee_Mass2_') and n.endswith('.'+side) for n in body)

sc['mass_revision'] = 10
sc['mass_revision_notes'] = 'Thicker torso, layered forearms, broad overlapping thigh and calf armor, enclosed knees with front sliding underlap and side/rear armor. Current RAIKEN weapon and right forearm pose retained.'
sc['export_status'] = 'Modeling review only. User requested no new demo/FBX/GLB export. Existing game candidate predates this body revision.'
sc['current_delivery'] = str(OUT)
sc.camera = bpy.data.objects['RAIKEN_ASSEMBLED_3Q']
bpy.ops.object.select_all(action='DESELECT');bpy.context.view_layer.objects.active=None
bpy.context.preferences.filepaths.save_version = 0
bpy.ops.wm.save_as_mainfile(filepath=str(OUT))
report = {'source_current_sha256':EXPECTED,'source_body_sha256':filehash(BODY),
    'merged_file':str(OUT),'replaced_body_meshes':len(oldbody),'imported_body_meshes':len(body),
    'retained_meshes_and_controllers':len(preserved),
    'retained_rk_meshes':sum(n.startswith('RK_') and v['mesh'] is not None for n,v in preserved.items()),
    'retained_head_meshes':sum(n.startswith('V3H_') and v['mesh'] is not None for n,v in preserved.items()),
    'retained_objects_differences':differences,'body_geometry_issues':geometry_errors,
    'body_uv_missing':0,'modeling_revision':10,'exports_run':False,
    'game_candidate_sha256':filehash(P/'JishenTrial_GAME_CANDIDATE.blend'),
    'preserved_proof':preserved}
(D/'merge_audit.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps({k:v for k,v in report.items() if k!='preserved_proof'},indent=2),flush=True)
