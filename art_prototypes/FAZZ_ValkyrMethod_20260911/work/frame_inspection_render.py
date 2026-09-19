import bpy,pathlib
R=pathlib.Path(bpy.data.filepath).parent;asset=bpy.data.collections['FAZZ_COMPLETE_ASSEMBLY'];COL=bpy.data.collections['17 V3 INTERNAL FRAME'];heads=set(bpy.data.collections['FAZZ_HEAD_ASSET'].all_objects)
for o in list(asset.all_objects):
 if o is None or o.type!='MESH' or o in heads:continue
 keep=COL in o.users_collection or any(c.name.startswith(('01 ','05 ','06 ','08 ')) for c in o.users_collection)
 if not keep:o.hide_render=True;o.hide_set(True)
sc=bpy.context.scene;sc.camera=bpy.data.objects['CAM_hero'];sc.render.resolution_x=1300;sc.render.resolution_y=1500;sc.cycles.samples=36
bpy.ops.wm.save_as_mainfile(filepath=str(R/'FAZZ_V3_FRAME_INSPECTION.blend'))
sc.render.filepath=str(R/'renders'/'frame_v3_unarmoured.png');bpy.ops.render.render(write_still=True)
