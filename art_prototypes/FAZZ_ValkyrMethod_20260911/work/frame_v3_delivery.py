import bpy,pathlib
R=pathlib.Path(bpy.data.filepath).parent
for o in list(bpy.data.objects):
 if o.name.startswith('FAZZ_V2 segmented toe'):bpy.data.objects.remove(o,do_unlink=True)
sc=bpy.context.scene;sc.camera=bpy.data.objects['CAM_hero'];sc.render.resolution_x=1300;sc.render.resolution_y=1500;sc.cycles.samples=36
bpy.ops.wm.save_as_mainfile(filepath=str(R/'FAZZ_V3_INNER_FRAME.blend'))
exec((R/'work'/'audit_frame_v3.py').read_text(encoding='utf-8-sig'))
sc.render.filepath=str(R/'renders'/'frame_v3_hero.png');bpy.ops.render.render(write_still=True)
exec((R/'work'/'export_frame_v3.py').read_text(encoding='utf-8-sig'))
