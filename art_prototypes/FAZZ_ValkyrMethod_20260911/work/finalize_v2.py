import bpy,bmesh,pathlib
R=pathlib.Path(bpy.data.filepath).parent
o=bpy.data.objects['FAZZ_Pectoral ceramic panel R'];dg=bpy.context.evaluated_depsgraph_get()
me=bpy.data.meshes.new_from_object(o.evaluated_get(dg));bm=bmesh.new();bm.from_mesh(me)
bmesh.ops.triangulate(bm,faces=list(bm.faces));bmesh.ops.dissolve_degenerate(bm,dist=1e-5,edges=list(bm.edges));bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=1e-6)
bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
assert all(e.is_manifold for e in bm.edges)
bm.to_mesh(me);bm.free();o.modifiers.clear();o.data=me
bpy.context.scene.camera=bpy.data.objects['CAM_hero']
bpy.ops.wm.save_as_mainfile(filepath=str(R/'FAZZ_V2_REFINED.blend'))

