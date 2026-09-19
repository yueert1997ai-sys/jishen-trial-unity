import bpy,pathlib,json,math,bmesh
from mathutils import Vector
R=pathlib.Path(bpy.data.filepath).parent;s=bpy.context.scene
s.render.resolution_x=1400;s.render.resolution_y=1600;s.cycles.samples=40
for n in ['hero','head','rear']:
 s.camera=bpy.data.objects['CAM_'+n];s.render.filepath=str(R/'renders'/('final_'+n+'.png'));bpy.ops.render.render(write_still=True)
