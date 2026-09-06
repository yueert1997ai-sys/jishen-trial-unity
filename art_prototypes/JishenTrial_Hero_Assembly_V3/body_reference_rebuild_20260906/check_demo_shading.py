import bpy,pathlib
W=pathlib.Path(__file__).resolve().parent;D=W/'delivery';R=D/'renders'
bpy.ops.wm.open_mainfile(filepath=str(D/'VALKYR_FBX_REIMPORT_CHECK.blend'));sc=bpy.context.scene
for mat in bpy.data.materials:
 if mat.name.startswith('VALKYR_DEMO_'):
  bs=next((n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None)
  if bs:
   for link in list(bs.inputs['Normal'].links):mat.node_tree.links.remove(link)
sc.render.resolution_x=sc.render.resolution_y=1300;sc.render.filepath=str(R/'DEMO_FBX_GEOMETRY_NORMALS.png');bpy.ops.render.render(write_still=True)
