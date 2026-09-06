import bpy,bmesh,pathlib,json,hashlib
from mathutils import Vector
W=pathlib.Path(__file__).resolve().parent;OUT=W/'delivery';manifest=json.loads((OUT/'delivery_manifest.json').read_text());result={}
def render(path):
 sc=bpy.context.scene;sc.cycles.samples=48;sc.render.resolution_x=sc.render.resolution_y=1200;sc.render.filepath=str(path);bpy.ops.render.render(write_still=True)
for name,path in [('assembled',pathlib.Path(manifest['current_master'])),('standalone',OUT/'VALKYR_HEAD_MASTER.blend')]:
 bpy.ops.wm.open_mainfile(filepath=str(path));sc=bpy.context.scene;hc=bpy.data.collections['HEAD_V3 | reference matched editable assembly'];parts=[o for o in hc.all_objects if o.type=='MESH'];root=bpy.data.objects['HEAD_V3_ROOT']
 packed=any(im.packed_file and im.name.startswith('REFERENCE') for im in bpy.data.images)
 result[name]={'path':str(path),'mesh_parts':len(parts),'missing_uv':[o.name for o in parts if not o.data.uv_layers],'root_parent':root.parent.name if root.parent else None,'packed_reference':packed,'head_renders_visible':all(not o.hide_render for o in parts),'cameras':len([o for o in sc.objects if o.type=='CAMERA'])}
 assert len(parts)==141 and packed and not result[name]['missing_uv']
 assert (root.parent and root.parent.name=='Head') if name=='assembled' else root.parent is None
 render(OUT/'renders'/('REOPENED_'+name.upper()+'.png'))
# Reimport the actual FBX into the head review scene, retaining its camera and lighting.
for o in list(bpy.context.scene.objects):
 if o.type in ('MESH','CURVE') or o.name=='HEAD_V3_ROOT':bpy.data.objects.remove(o,do_unlink=True)
bpy.ops.import_scene.fbx(filepath=str(OUT/'VALKYR_HEAD_Reference.fbx'))
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH'];assert len(meshes)==1
o=meshes[0];o.data.calc_loop_triangles();bm=bmesh.new();bm.from_mesh(o.data)
vertices=[o.matrix_world@v.co for v in o.data.vertices];bounds=[[min(v[i] for v in vertices) for i in range(3)],[max(v[i] for v in vertices) for i in range(3)]]
err=max(abs(bounds[j][i]-manifest['export']['bounds_m'][j][i]) for j in range(2) for i in range(3))
result['fbx_reimport']={'meshes':len(meshes),'triangles':len(o.data.loop_triangles),'materials':len(o.data.materials),'missing_uv':not bool(o.data.uv_layers),'nonmanifold_edges':sum(not e.is_manifold for e in bm.edges),'max_bounds_error_m':err,'bounds_m':bounds};bm.free()
assert len(o.data.loop_triangles)==manifest['export']['triangles'] and o.data.uv_layers and err<1e-5 and not result['fbx_reimport']['nonmanifold_edges']
render(OUT/'renders'/'FBX_REIMPORT.png')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'FBX_REIMPORT_VERIFIED.blend'))
(OUT/'saved_file_verification.json').write_text(json.dumps(result,indent=2))
print('SAVED_FILES_VERIFIED',json.dumps(result),flush=True)
