import bpy,pathlib,bmesh
P=pathlib.Path(__file__).resolve().parent
bpy.ops.wm.open_mainfile(filepath=str(P/'RAIKEN_MkII_MASTER.blend'))
for o in bpy.context.scene.objects:
    if not o.name.startswith('RK_') or o.type!='MESH':continue
    for m in o.modifiers:
        if m.type=='BEVEL':m.segments=1
bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get()
for o in bpy.context.scene.objects:
    if not o.name.startswith('RK_') or o.type!='MESH':continue
    me=bpy.data.meshes.new_from_object(o.evaluated_get(dg),preserve_all_data_layers=True,depsgraph=dg)
    bm=bmesh.new();bm.from_mesh(me);bmesh.ops.triangulate(bm,faces=list(bm.faces))
    bad=[f for f in bm.faces if f.calc_area()<1e-12]
    if bad:print(o.name,[(f.calc_area(),[tuple(v.co) for v in f.verts]) for f in bad],flush=True)
    bm.free();bpy.data.meshes.remove(me)
