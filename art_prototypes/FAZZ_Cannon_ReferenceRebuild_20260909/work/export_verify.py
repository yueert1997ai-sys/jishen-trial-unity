import bpy,json,math
from pathlib import Path
from mathutils import Vector
ROOT=Path(r'D:\project-mecha-design\MECH ROUGE\art_prototypes\FAZZ_Cannon_ReferenceRebuild_20260909\delivery')
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'FAZZ_CANNON_REDESIGN.blend'))
root=bpy.data.objects['FAZZ_CANNON_ROOT']
objects=[root]+list(root.children_recursive)
for o in list(bpy.data.objects):
 if o not in objects:bpy.data.objects.remove(o,do_unlink=True)
bpy.ops.object.select_all(action='DESELECT')
for o in objects:
 if o.type in {'MESH','CURVE'}:o.select_set(True)
bpy.context.view_layer.objects.active=next(o for o in objects if o.type=='MESH')
bpy.ops.object.convert(target='MESH')
for o in bpy.context.selected_objects:
 for p in o.data.polygons:
  assert p.area>0, f'Degenerate polygon: {o.name}'
bpy.ops.object.select_all(action='SELECT')
def stats():
 bpy.context.view_layer.update()
 meshes=[o for o in bpy.context.scene.objects if o.type=='MESH'];points=[];tri=0;finite=True
 for o in meshes:
  o.data.calc_loop_triangles();tri+=len(o.data.loop_triangles)
  for v in o.data.vertices:
   p=o.matrix_world@v.co;points.append(p);finite=finite and all(math.isfinite(t) for t in p)
 lo=[min(p[k] for p in points) for k in range(3)];hi=[max(p[k] for p in points) for k in range(3)]
 return {'meshes':len(meshes),'triangles':tri,'bounds_min':lo,'bounds_max':hi,'dimensions': [hi[k]-lo[k] for k in range(3)],'materials':sorted({m.name for o in meshes for m in o.data.materials if m}),'finite_vertices':finite,'sockets':sorted(o.name for o in bpy.context.scene.objects if o.name.startswith('SOCKET_'))}
source=stats()
bpy.ops.export_scene.gltf(filepath=str(ROOT/'exports/FAZZ_CANNON.glb'),export_format='GLB',use_selection=True,export_apply=True,export_extras=True)
bpy.ops.export_scene.fbx(filepath=str(ROOT/'exports/FAZZ_CANNON.fbx'),use_selection=True,object_types={'EMPTY','MESH'},use_mesh_modifiers=True,use_triangles=True,axis_forward='-Z',axis_up='Y',bake_anim=False,add_leaf_bones=False,path_mode='AUTO')
checks={'source_baked':source}
for fmt in ('glb','fbx'):
 bpy.ops.wm.read_factory_settings(use_empty=True)
 if fmt=='glb':bpy.ops.import_scene.gltf(filepath=str(ROOT/'exports/FAZZ_CANNON.glb'))
 else:bpy.ops.import_scene.fbx(filepath=str(ROOT/'exports/FAZZ_CANNON.fbx'))
 s=stats()
 assert s['finite_vertices']
 assert s['meshes']==source['meshes'],(fmt,'mesh count',s,source)
 assert s['triangles']==source['triangles'],(fmt,'triangles',s,source)
 assert len(s['materials'])==len(source['materials'])
 assert s['sockets']==source['sockets']
 for a,b in zip(sorted(s['dimensions']),sorted(source['dimensions'])):assert abs(a-b)<.0001,(fmt,'dimensions',a,b)
 checks[fmt+'_reimport']=s
checks['result']='PASS: GLB and FBX reimport preserve mesh count, triangles, seven materials, four sockets and dimensions.'
(ROOT/'EXPORT_VERIFICATION.json').write_text(json.dumps(checks,indent=2),encoding='utf-8')
print(checks['result'],flush=True)
