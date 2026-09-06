import bpy,pathlib,json
from mathutils import Vector
W=pathlib.Path(__file__).resolve().parent
bpy.ops.wm.open_mainfile(filepath=str(W/'iteration_v3/VALKYR_ITERATION_V3_MASTER.blend'));sc=bpy.context.scene
sc.render.resolution_x=sc.render.resolution_y=1500;sc.render.resolution_percentage=100
dg=bpy.context.evaluated_depsgraph_get()
out=[]
for camera,px,py in [('KNEE_SIDE',280,63),('KNEES',1039,1158),('KNEES',982,386)]:
 cam=bpy.data.objects['V3B_CAM_'+camera];fr=cam.data.view_frame(scene=sc)
 halfx=max(v.x for v in fr);halfy=max(v.y for v in fr)
 origin=cam.matrix_world@Vector(((px/1500*2-1)*halfx,(1-py/1500*2)*halfy,-.1))
 direction=cam.matrix_world.to_3x3()@Vector((0,0,-1))
 result=sc.ray_cast(dg,origin,direction)
 out.append({'camera':camera,'pixel':[px,py],'hit':result[4].name if result[0] else None,'location':list(result[1]) if result[0] else None})
print(json.dumps(out,indent=2),flush=True)
