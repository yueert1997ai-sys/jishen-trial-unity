import bpy,bmesh,json,math,hashlib
from pathlib import Path
from mathutils import Vector
OUT=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(OUT/'VALKYR_PARALLEL_R01.blend'))
sc=bpy.context.scene;asset=bpy.data.collections['VALKYR_R01_ASSET'];optional=set(bpy.data.collections['08_OPTIONAL_SWORD'].all_objects)
meshes=[o for o in asset.all_objects if o.type=='MESH' and o not in optional]
dg=bpy.context.evaluated_depsgraph_get();rows=[];points=[]
for o in meshes:
 ev=o.evaluated_get(dg);me=ev.to_mesh();bm=bmesh.new();bm.from_mesh(me);me.calc_loop_triangles()
 bad=sum(not all(math.isfinite(x) for x in v.co) for v in me.vertices)
 row={'name':o.name,'vertices':len(me.vertices),'triangles':len(me.loop_triangles),'invalid_vertices':bad,'degenerate_faces':sum(f.calc_area()<1e-12 for f in bm.faces),'boundary_edges':sum(e.is_boundary for e in bm.edges),'nonmanifold_edges':sum(not e.is_manifold for e in bm.edges)}
 rows.append(row);points.extend(o.matrix_world@v.co for v in me.vertices);bm.free();ev.to_mesh_clear()
bounds={'min':[min(v[k] for v in points) for k in range(3)],'max':[max(v[k] for v in points) for k in range(3)]}
report={'asset':'VALKYR PARALLEL R01','source_file':str(OUT/'VALKYR_PARALLEL_R01.blend'),'mesh_count':len(meshes),'triangles':sum(r['triangles'] for r in rows),'invalid_vertices':sum(r['invalid_vertices'] for r in rows),'degenerate_faces':sum(r['degenerate_faces'] for r in rows),'boundary_edges':sum(r['boundary_edges'] for r in rows),'nonmanifold_edges':sum(r['nonmanifold_edges'] for r in rows),'bounds':bounds,'problem_objects':[r for r in rows if r['invalid_vertices'] or r['degenerate_faces'] or r['nonmanifold_edges']],'parts':rows,'cannon_objects':[o.name for o in asset.all_objects if 'cannon' in o.name.lower()],'reference_images_packed':all(i.packed_file for i in bpy.data.images if i.name.startswith('VALKYR_ORIGINAL')),'limits':'No full self-intersection analysis, no skin/animation/Unity integration for this separate design line.'}
(OUT/'work/geometry_review.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
if report['invalid_vertices'] or report['cannon_objects']:raise RuntimeError('Invalid asset contents')
# Select the independent assembly only; studio/reference and optional sword are not part of GLB.
bpy.ops.object.select_all(action='DESELECT')
for o in asset.all_objects:
 if o not in optional and o.type in {'MESH','CURVE','FONT','EMPTY'}:o.hide_set(False);o.select_set(True)
bpy.context.view_layer.objects.active=bpy.data.objects['VALKYR_PARALLEL_R01_ROOT']
bpy.ops.export_scene.gltf(filepath=str(OUT/'exports/VALKYR_PARALLEL_R01.glb'),export_format='GLB',use_selection=True,export_apply=True,export_animations=False)
# Present master as a clean assembly on reopen; studio lights remain in the render.
for o in bpy.data.collections['90_REVIEW_STUDIO'].objects:o.hide_set(True)
for o in asset.all_objects:o.select_set(False)
bpy.context.view_layer.objects.active=bpy.data.objects['VALKYR_PARALLEL_R01_ROOT'];bpy.data.objects['VALKYR_PARALLEL_R01_ROOT'].select_set(True)
for screen in bpy.data.screens:
 for area in screen.areas:
  if area.type=='VIEW_3D':
   area.spaces.active.region_3d.view_location=Vector((0,0,3.2));area.spaces.active.region_3d.view_distance=10;area.spaces.active.region_3d.view_rotation=bpy.data.objects['R01_ThreeQuarter'].rotation_euler.to_quaternion();area.spaces.active.region_3d.view_perspective='ORTHO';area.spaces.active.shading.type='MATERIAL';area.spaces.active.overlay.show_overlays=False
sc.camera=bpy.data.objects['R01_ThreeQuarter'];bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'VALKYR_PARALLEL_R01.blend'),compress=True)
# Explicitly reopen that saved master before generating final views.
bpy.ops.wm.open_mainfile(filepath=str(OUT/'VALKYR_PARALLEL_R01.blend'));sc=bpy.context.scene
prefs=bpy.context.preferences.addons['cycles'].preferences
prefs.compute_device_type='OPTIX';prefs.get_devices()
for dev in prefs.devices:dev.use=dev.type!='CPU'
sc.cycles.device='GPU'
views={'01_THREE_QUARTER':'R01_ThreeQuarter','02_FRONT':'R01_Front','03_SIDE':'R01_Side','04_BACK':'R01_Back','05_HEAD':'R01_Head','06_HEAD_FRONT':'R01_HeadFront','07_LEG':'R01_Leg','08_BACK_DETAIL':'R01_BackDetail','09_HEAD_LOW':'R01_HeadLow'}
for filename,camname in views.items():
 sc.camera=bpy.data.objects[camname];sc.render.filepath=str(OUT/'renders'/(filename+'.png'));bpy.ops.render.render(write_still=True)
(OUT/'work/final_render_manifest.json').write_text(json.dumps({'source_sha256':hashlib.sha256((OUT/'VALKYR_PARALLEL_R01.blend').read_bytes()).hexdigest(),'views':list(views),'reopened_source_before_render':True},indent=2),encoding='utf-8')
print('R01_FINAL_REVIEW',json.dumps({k:report[k] for k in ['mesh_count','triangles','invalid_vertices','degenerate_faces','boundary_edges','nonmanifold_edges']}),flush=True)
