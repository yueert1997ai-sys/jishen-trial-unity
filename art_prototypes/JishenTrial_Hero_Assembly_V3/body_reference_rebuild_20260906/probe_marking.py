import bpy,bmesh,pathlib,collections
W=pathlib.Path(__file__).resolve().parent
bpy.ops.wm.open_mainfile(filepath=str(W/'stage_08/ASSEMBLED.blend'))
o=bpy.data.objects['V3B_Shoulder_Valkyr_Wordmark']
for dist in (0,.000001,.000005,.00001):
 bm=bmesh.new();bm.from_mesh(o.data)
 if dist:bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=dist)
 print('WORDMARK',dist,len(bm.verts),len(bm.faces),collections.Counter(len(e.link_faces) for e in bm.edges),flush=True)
 bm.free()
print('COMPOSITOR',hasattr(bpy.context.scene,'compositing_node_group'),hasattr(bpy.context.scene,'node_tree'),flush=True)
