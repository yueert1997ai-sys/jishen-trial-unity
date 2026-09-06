import bpy,pathlib,json,bmesh
from mathutils import Vector
P=pathlib.Path(__file__).resolve().parent;W=P/'refinement';F=W/'exports'/'JishenTrial_Refined_LOD0.fbx'
def stats():
 sc=bpy.context.scene;dg=bpy.context.evaluated_depsgraph_get();ps=[o for o in sc.objects if o.type=='MESH' and o.name!='Studio_Ground'];vs=[];tr=0;nm=0;missing=0
 for o in ps:
  e=o.evaluated_get(dg);me=e.to_mesh();me.calc_loop_triangles();tr+=len(me.loop_triangles);vs.extend(o.matrix_world@v.co for v in me.vertices);e.to_mesh_clear()
  bm=bmesh.new();bm.from_mesh(o.data);nm+=sum(not e.is_manifold for e in bm.edges);bm.free()
  missing+=sum(not v.groups for v in o.data.vertices)
 return dict(meshes=len(ps),triangles=tr,materials=sorted({m.name for o in ps for m in o.data.materials}),nonmanifold_edges=nm,missing_skin_weights=missing,bounds_min=[min(v[i] for v in vs) for i in range(3)],bounds_max=[max(v[i] for v in vs) for i in range(3)],armatures=[{'name':o.name,'bones':len(o.data.bones)} for o in sc.objects if o.type=='ARMATURE'],uv_missing=[o.name for o in ps if not o.data.uv_layers],beam_objects=[o.name for o in ps if 'Beam' in o.name])
bpy.ops.wm.open_mainfile(filepath=str(W/'JishenTrial_REFINED_DEMO.blend'));expected=stats()
# An empty verification scene only; never replaces the editable project.
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(F),use_anim=False)
actual=stats();delta=max(abs(actual[k][i]-expected[k][i]) for k in ('bounds_min','bounds_max') for i in range(3))
assert actual['triangles']==expected['triangles'] and actual['meshes']==expected['meshes'],(expected,actual)
assert len(actual['materials'])==5 and delta<.0001,(delta,actual)
assert actual['armatures'][0]['bones']==56 and not actual['missing_skin_weights'] and not actual['uv_missing'],actual
assert len(actual['beam_objects'])==1 and actual['nonmanifold_edges']==0,actual
bpy.context.preferences.filepaths.save_version=0
for im in bpy.data.images:
 if im.source=='FILE' and im.has_data:im.pack()
bpy.ops.wm.save_as_mainfile(filepath=str(W/'FBX_REIMPORT_VERIFIED.blend'))
report={'file':str(F),'expected':expected,'actual_reimport':actual,'max_bounds_error_m':delta,'passed':True}
(W/'logs'/'fbx_reimport_validation.json').write_text(json.dumps(report,indent=2))
print('FBX_REIMPORT_VERIFIED',json.dumps(report))
