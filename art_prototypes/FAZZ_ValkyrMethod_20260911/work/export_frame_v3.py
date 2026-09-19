import bpy,json,pathlib,contextlib,io,hashlib
from mathutils import Vector
R=pathlib.Path(bpy.data.filepath).parent
# Compare raw head mesh geometry with the prior delivered file; mounting transforms are intentionally different.
def sig(o):
 data=bytearray()
 for v in o.data.vertices:data.extend((','.join(format(c,'.9g') for c in v.co)+';').encode())
 for f in o.data.polygons:data.extend((','.join(str(i) for i in f.vertices)+';').encode())
 return hashlib.sha256(data).hexdigest()
cur={o.name:sig(o) for o in bpy.data.collections['FAZZ_HEAD_ASSET'].all_objects if o.type=='MESH'}
source=r'C:\Users\yue\Documents\Codex\2026-09-08\new-chat-2\outputs\FAZZ_HEAD\FAZZ_HEAD.blend'
with bpy.data.libraries.load(source,link=False) as (a,b):b.collections=['FAZZ_HEAD_ASSET']
c=b.collections[0];old=[sig(o) for o in c.all_objects if o.type=='MESH']
proof={'source':source,'reused_head_meshes':len(cur),'source_meshes':len(old),'raw_mesh_hashes_equal':sorted(cur.values())==sorted(old)}
(R/'work'/'frame_v3_head_identity.json').write_text(json.dumps(proof,indent=2));print(proof)
# Export only the actual assembled collection; original comparison copy is not linked to scene.
bpy.ops.object.select_all(action='DESELECT')
for o in bpy.data.collections['FAZZ_COMPLETE_ASSEMBLY'].all_objects:o.select_set(True)
with contextlib.redirect_stdout(io.StringIO()):bpy.ops.export_scene.gltf(filepath=str(R/'exports'/'FAZZ_V3_INNER_FRAME.glb'),export_format='GLB',use_selection=True,export_apply=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
with contextlib.redirect_stdout(io.StringIO()):bpy.ops.import_scene.gltf(filepath=str(R/'exports'/'FAZZ_V3_INNER_FRAME.glb'))
obs=[o for o in bpy.context.scene.objects if o.type=='MESH'];points=[o.matrix_world@Vector(p) for o in obs for p in o.bound_box];tri=0
for o in obs:o.data.calc_loop_triangles();tri+=len(o.data.loop_triangles)
result={'meshes':len(obs),'triangles':tri,'root_present':'FAZZ_MASTER_ROOT' in bpy.data.objects,'bounds':[[min(p[i] for p in points) for i in range(3)],[max(p[i] for p in points) for i in range(3)]]}
(R/'work'/'frame_v3_glb_readback.json').write_text(json.dumps(result,indent=2));print(result)

