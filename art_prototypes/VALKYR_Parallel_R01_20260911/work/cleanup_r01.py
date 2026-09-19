import bpy,bmesh,json,shutil
from pathlib import Path
OUT=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(OUT/'VALKYR_PARALLEL_R01.blend'))
shutil.copy2(OUT/'VALKYR_PARALLEL_R01.blend',OUT/'work/VALKYR_R01_EDITABLE_UNBAKED.blend')
report=json.loads((OUT/'work/geometry_review.json').read_text());dg=bpy.context.evaluated_depsgraph_get();fixed=[]
for row in report['problem_objects']:
 ob=bpy.data.objects[row['name']];ev=ob.evaluated_get(dg);me=bpy.data.meshes.new_from_object(ev,preserve_all_data_layers=True,depsgraph=dg);old=ob.data
 for mod in list(ob.modifiers):ob.modifiers.remove(mod)
 bm=bmesh.new();bm.from_mesh(me)
 bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.000002)
 bmesh.ops.dissolve_degenerate(bm,edges=list(bm.edges),dist=.000001)
 bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
 bm.to_mesh(me);bm.free();me.update();ob.data=me
 if old.users==0:bpy.data.meshes.remove(old)
 fixed.append(ob.name)
(OUT/'work/geometry_cleanup.json').write_text(json.dumps({'fixed_objects':fixed,'source_modifier_version':'work/VALKYR_R01_EDITABLE_UNBAKED.blend','method':'Evaluated only flagged components; merged sub-2-micron duplicates, dissolved sub-micron degeneracies; part separation retained.'},indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'VALKYR_PARALLEL_R01.blend'),compress=True)
print('GEOMETRY_CLEANED',len(fixed))
