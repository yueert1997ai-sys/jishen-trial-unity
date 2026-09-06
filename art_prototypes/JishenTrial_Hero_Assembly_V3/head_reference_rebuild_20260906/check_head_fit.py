import bpy,pathlib,sys,json,math
from mathutils import Vector
from mathutils.bvhtree import BVHTree
W=pathlib.Path(__file__).resolve().parent;I=int(next((a.split('=')[1] for a in sys.argv if a.startswith('iter=')),'7'));D=W/f'iteration_{I:02d}'
bpy.ops.wm.open_mainfile(filepath=str(D/'ASSEMBLED.blend'));sc=bpy.context.scene;head=bpy.data.objects['Head'];rest=head.matrix_basis.copy()
parts=set(bpy.data.collections['HEAD_V3 | reference matched editable assembly'].all_objects)
allowed={'14_Low_Profile_Neck_Yaw_Ring','16_Neck_Armor_Collar','17_Neck_Load_Bearing_Pedestal'}
def geom(o,dg):
 me=bpy.data.meshes.new_from_object(o.evaluated_get(dg),depsgraph=dg);vv=[o.matrix_world@v.co for v in me.vertices];ff=[tuple(p.vertices) for p in me.polygons]
 bpy.data.meshes.remove(me)
 if not vv:return None
 lo=Vector((min(v[i] for v in vv) for i in range(3)));hi=Vector((max(v[i] for v in vv) for i in range(3)))
 return {'lo':lo,'hi':hi,'tree':None,'v':vv,'f':ff}
def overlap(a,b):return all(a['lo'][i]<=b['hi'][i] and b['lo'][i]<=a['hi'][i] for i in range(3))
dg=bpy.context.evaluated_depsgraph_get();body={}
for o in sc.objects:
 if o in parts or o.name in allowed or o.type not in ('MESH','CURVE') or o.hide_render:continue
 bb=[o.matrix_world@Vector(v) for v in o.bound_box]
 if max(v.z for v in bb)<2.92:continue
 body[o.name]=geom(o,dg)
results=[]
for yaw,pitch in [(0,0),(30,0),(-30,0),(0,12),(0,-12)]:
 head.matrix_basis=rest.copy();head.rotation_euler.z+=math.radians(yaw);head.rotation_euler.x+=math.radians(pitch);bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get();hits=[];candidates=0
 for o in parts:
  if o.type not in ('MESH','CURVE'):continue
  g=geom(o,dg)
  for name,b in body.items():
   if not g or not b or not overlap(g,b):continue
   candidates+=1
   if not g['tree']:g['tree']=BVHTree.FromPolygons(g['v'],g['f'])
   if not b['tree']:b['tree']=BVHTree.FromPolygons(b['v'],b['f'])
   n=len(g['tree'].overlap(b['tree']))
   if n:hits.append({'head_part':o.name,'body_part':name,'overlapping_triangle_pairs':n})
 results.append({'yaw_degrees':yaw,'pitch_degrees':pitch,'broad_phase_pairs':candidates,'surface_intersections':hits})
head.matrix_basis=rest;bpy.context.view_layer.update()
(D/'fit_audit.json').write_text(json.dumps({'checks':results,'excluded_mating_interfaces':sorted(allowed),'restored_to_rest':True,'method':'Evaluated mesh BVH surface intersections; no animation timing claim'},indent=2))
print(json.dumps(results,indent=2))
