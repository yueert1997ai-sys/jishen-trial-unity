import bpy,pathlib,json
from mathutils import Vector
OUT=pathlib.Path(__file__).resolve().parent
bpy.ops.wm.open_mainfile(filepath=str(OUT/'JishenTrial_ASSEMBLED_MASTER.blend'))
sc=bpy.context.scene;dg=bpy.context.evaluated_depsgraph_get()
for x,y in ((0,.0168),(.025,.0168),(-.025,.0168),(0,0)):
    origin=Vector((x,y,2.5));hits=[]
    for i in range(20):
        ok,loc,nor,idx,obj,mw=sc.ray_cast(dg,origin,Vector((0,0,1)),distance=.6)
        if not ok or loc.z>3.15:break
        hits.append([obj.name,round(loc.z,6)]);origin=loc+Vector((0,0,.0001))
    print('NECK_VERTICAL',x,y,hits,flush=True)
for o in bpy.data.collections['HEAD_V2 | colored editable parts'].all_objects:
    if o.type=='MESH' and o.name.startswith(('13','14','15','16')):
        pp=[o.matrix_world@v.co for v in o.data.vertices]
        print('NECK_PART',o.name,[[round(fn(v[i] for v in pp),6) for i in range(3)] for fn in (min,max)],flush=True)
