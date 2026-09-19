import bpy,pathlib,json,math,bmesh
from mathutils import Vector
R=pathlib.Path(bpy.data.filepath).parent;s=bpy.context.scene
s.render.resolution_x=1050;s.render.resolution_y=1200;s.cycles.samples=24
for n in ['hero','head','rear','front']:
 s.camera=bpy.data.objects['CAM_'+n];s.render.filepath=str(R/'renders'/('pass2_'+n+'.png'));bpy.ops.render.render(write_still=True)
