import bpy,bmesh,json
from pathlib import Path
OUT=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(OUT/'VALKYR_PARALLEL_R01.blend'));dg=bpy.context.evaluated_depsgraph_get()
for target,source in [('Main delta pauldron.R','Main delta pauldron.L'),('Long quadriceps blue shield.L','Long quadriceps blue shield.R')]:
 ob=bpy.data.objects[target];src=bpy.data.objects[source];me=bpy.data.meshes.new_from_object(src.evaluated_get(dg),preserve_all_data_layers=True,depsgraph=dg)
 for v in me.vertices:
  pt=src.matrix_world@v.co;pt.x=-pt.x;v.co=ob.matrix_world.inverted()@pt
 bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free()
 for m in list(ob.modifiers):ob.modifiers.remove(m)
 old=ob.data;ob.data=me
 if old.users==0:bpy.data.meshes.remove(old)
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'VALKYR_PARALLEL_R01.blend'),compress=True)
print('Clean counterpart topology mirrored; asymmetric markings retained')
