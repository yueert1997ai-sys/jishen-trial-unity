import bpy,pathlib,json,sys
from mathutils import Vector
W=pathlib.Path(__file__).resolve().parent;I=int(next((a.split('=')[1] for a in sys.argv if a.startswith('iter=')),'7'));D=W/f'iteration_{I:02d}'
bpy.ops.wm.open_mainfile(filepath=str(D/'ASSEMBLED.blend'));sc=bpy.context.scene;dg=bpy.context.evaluated_depsgraph_get();gold=bpy.data.objects['V3H_Gold_Antenna_Root.L'];g=gold.evaluated_get(dg);inv=gold.matrix_world.inverted();out=[]
for z in range(154,243,4):
 seen=[];hidden=[]
 for x in range(66,109):
  origin=Vector((x*.0012,-1,2.974+z*.0012));direction=Vector((0,1,0))
  ok,p,n,i=g.ray_cast(inv@origin,inv.to_3x3()@direction)
  if not ok:continue
  hit,loc,norm,idx,obj,mat=sc.ray_cast(dg,origin,direction)
  if obj.name==gold.name:seen.append(x)
  else:hidden.append({'x':x,'occluder':obj.name,'depth_units':round((p.y-loc.y)/.0012,2)})
 out.append({'z':z,'visible_x':seen,'hidden':hidden})
(D/'gold_visibility.json').write_text(json.dumps(out,indent=2))
print(json.dumps([{'z':a['z'],'visible':len(a['visible_x']),'hidden_by':sorted(set(b['occluder'] for b in a['hidden']))} for a in out],indent=2))
