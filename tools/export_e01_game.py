"""Blender 5.2: open Stage 02 read-only and export evaluated rigid parts for Unity.
Run: blender --background --python tools/export_e01_game.py
The source .blend is never saved. Unity's E01CombatIntegration creates mesh assets.
"""
import bpy, json, pathlib, hashlib
from mathutils import Vector

GAME = pathlib.Path(__file__).resolve().parents[1]
SOURCE = GAME.parents[1] / 'art_prototypes/Type_E01_20260906/stage_02/TYPE_E01_STAGE02.blend'
OUT = GAME / 'Assets/Art/E01_Stage02_Game'
OUT.mkdir(parents=True, exist_ok=True)
before = hashlib.sha256(SOURCE.read_bytes()).hexdigest()
bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
root = bpy.data.objects['TYPE_E01_MASTER_ROOT']
rifle = bpy.data.objects['E01_RIFLE_ROOT']
parts = [o for o in root.children_recursive if o.type == 'EMPTY' and o.name.startswith('E01_')]
parts = [o for o in parts if 'DETAIL' not in o.name]
def part_of(o):
    while o and o not in parts: o = o.parent
    return o
objects = [o for o in root.children_recursive if o.type == 'MESH' and not o.hide_render and part_of(o)]
body = [o for o in objects if part_of(o) != rifle]
points = [o.matrix_world @ Vector(v) for o in body for v in o.bound_box]
low = min(v.z for v in points); height = max(v.z for v in points)-low
scale = 2.8 / height
def bodyv(v): return Vector((-v.x, v.z, -v.y))*scale
def gunv(v): return Vector((-v.y, v.z, v.x))*scale
materials = []
mat_ids = {}
def material_id(m):
    if m.name not in mat_ids:
        p = next((n for n in m.node_tree.nodes if n.type == 'BSDF_PRINCIPLED'), None) if m.use_nodes else None
        col = list(p.inputs['Base Color'].default_value if p else m.diffuse_color)
        mat_ids[m.name] = len(materials)
        materials.append(dict(name=m.name, color=col, metallic=float(p.inputs['Metallic'].default_value) if p else .2,
                              roughness=float(p.inputs['Roughness'].default_value) if p else .45,
                              emission=list(p.inputs['Emission Color'].default_value) if p else [0,0,0,1],
                              strength=float(p.inputs['Emission Strength'].default_value) if p else 0))
    return mat_ids[m.name]
deps = bpy.context.evaluated_depsgraph_get()
result = []
for part in parts:
    meshes = [o for o in objects if part_of(o) == part]
    if not meshes: continue
    gun = part == rifle
    origin = Vector(rifle['primary_grip_local']) if gun else part.matrix_world.translation
    pos = Vector((0,0,0)) if gun else bodyv(origin - Vector((0,0,low)))
    batches = {}
    for obj in meshes:
        ev = obj.evaluated_get(deps); mesh = ev.to_mesh(); mesh.calc_loop_triangles()
        mx = rifle.matrix_world.inverted() @ obj.matrix_world if gun else obj.matrix_world
        norm = mx.to_3x3().inverted().transposed()
        for tri in mesh.loop_triangles:
            mat = mesh.materials[tri.material_index] if mesh.materials else bpy.data.materials[0]
            mid = material_id(mat)
            batch = batches.setdefault(mid, dict(material=mid, vertices=[], normals=[], triangles=[]))
            # Split normals preserve the source's machined bevels and flat armor faces.
            base = len(batch['vertices'])
            for li in tri.loops:
                vert = mesh.vertices[mesh.loops[li].vertex_index]
                v = (gunv if gun else bodyv)(mx @ vert.co - origin)
                n = norm @ mesh.corner_normals[li].vector
                n = (gunv if gun else bodyv)(n).normalized()
                batch['vertices'].append(dict(zip('xyz', [round(x,6) for x in v])))
                batch['normals'].append(dict(zip('xyz', [round(x,6) for x in n])))
            batch['triangles'].extend([base,base+2,base+1])
        ev.to_mesh_clear()
    result.append(dict(name=part.name, position=dict(zip('xyz',pos)), batches=list(batches.values())))
    print('Exported', part.name, sum(len(b['triangles'])//3 for b in batches.values()), flush=True)
def anchor(key): return dict(zip('xyz',gunv(Vector(rifle[key])-Vector(rifle['primary_grip_local']))))
payload = dict(parts=result,materials=materials,height=2.8,scale=scale,muzzle=anchor('muzzle_local'),support=anchor('support_grip_local'))
(OUT/'e01_meshes.json').write_text(json.dumps(payload,separators=(',',':')),encoding='utf-8')
after = hashlib.sha256(SOURCE.read_bytes()).hexdigest()
assert before == after
report = dict(source=str(SOURCE),source_sha256=before,source_unchanged=True,height=2.8,parts=len(result),materials=len(materials),triangles=sum(len(b['triangles'])//3 for p in result for b in p['batches']))
(OUT/'export_report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report),flush=True)
