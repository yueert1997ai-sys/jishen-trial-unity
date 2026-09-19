import bpy,json
from mathutils import Vector
hand=bpy.data.objects['Hand.R']
rows=[]
for o in hand.children_recursive:
    if o.type!='MESH' or o.hide_render:continue
    points=[hand.matrix_world.inverted()@o.matrix_world@Vector(v) for v in o.bound_box]
    rows.append({'name':o.name,'local_min':[min(p[i] for p in points) for i in range(3)],'local_max':[max(p[i] for p in points) for i in range(3)]})
print(json.dumps({'hand_meshes':rows,'socket':list(hand.matrix_world.inverted()@bpy.data.objects['V3B_Sword_Grip_Socket'].matrix_world.translation)}))
