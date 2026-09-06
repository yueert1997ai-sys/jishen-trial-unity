import bpy,bmesh,pathlib,sys,json,hashlib,math
from mathutils import Vector,Matrix
from mathutils.bvhtree import BVHTree
W=pathlib.Path(__file__).resolve().parent;stage=int(next((a.split('=')[1] for a in sys.argv if a.startswith('stage=')),'4'));D=W/f'stage_{stage:02d}'
bpy.ops.wm.open_mainfile(filepath=str(D/'ASSEMBLED.blend'));sc=bpy.context.scene;dg=bpy.context.evaluated_depsgraph_get()
source=json.loads((W/'source_inventory.json').read_text());source={o['name']:o for o in source};errors=[];head=[];issues=[];totaltri=0;model=[];vv=[];uvmissing=[]
for o in sc.objects:
 if o.type!='MESH' or o.hide_render:continue
 ev=o.evaluated_get(dg);me=ev.to_mesh();me.calc_loop_triangles();tri=len(me.loop_triangles);totaltri+=tri
 pts=[o.matrix_world@v.co for v in me.vertices];vv+=pts
 bm=bmesh.new();bm.from_mesh(me);bad=sum(not e.is_manifold for e in bm.edges);zero=sum(f.calc_area()<1e-13 for f in bm.faces);bm.free()
 if bad or zero:issues.append({'name':o.name,'nonmanifold_edges':bad,'degenerate_faces':zero})
 if not o.data.uv_layers:uvmissing.append(o.name)
 model.append({'name':o.name,'triangles':tri,'parent':o.parent.name if o.parent else None,'materials':[m.name for m in o.data.materials if m]})
 if o.name.startswith('V3H_'):
  hh=hashlib.sha256(repr(([tuple(v.co) for v in o.data.vertices],[tuple(f.vertices) for f in o.data.polygons])).encode()).hexdigest()
  same=hh==source[o.name]['mesh_hash'] and [m.name for m in o.data.materials]==source[o.name]['materials']
  head.append({'name':o.name,'approved_geometry_and_material_slots_equal':same})
  if not same:errors.append(o.name+' approved head changed')
 ev.to_mesh_clear()

def bounds(o):
 ev=o.evaluated_get(dg);me=ev.to_mesh();pts=[o.matrix_world@v.co for v in me.vertices];ev.to_mesh_clear()
 return Vector([min(p[k] for p in pts) for k in range(3)]),Vector([max(p[k] for p in pts) for k in range(3)])
headobjs=[o for o in sc.objects if o.type=='MESH' and o.name.startswith('V3H_')]
exclude=('14_','16_','17_','V3B_Neck_')
body=[o for o in sc.objects if o.type=='MESH' and not o.hide_render and not o.name.startswith(('V3H_',)+exclude) and bounds(o)[1].z>2.977]
def bvh(o):
 ev=o.evaluated_get(dg);me=ev.to_mesh();v=[o.matrix_world@p.co for p in me.vertices];f=[tuple(p.vertices) for p in me.polygons];tree=BVHTree.FromPolygons(v,f,all_triangles=False,epsilon=.00001);ev.to_mesh_clear();return tree
collision={};hp=bpy.data.objects['Head'];orig=hp.matrix_world.copy()
for yaw in (0,-20,20):
 hp.matrix_world=orig@Matrix.Rotation(math.radians(yaw),4,'Z');bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get()
 ht={o:(bvh(o),bounds(o)) for o in headobjs};bt={o:(bvh(o),bounds(o)) for o in body};pairs=[]
 for a,(ta,ba) in ht.items():
  for b,(tb,bb) in bt.items():
   if any(ba[1][k]<bb[0][k] or bb[1][k]<ba[0][k] for k in range(3)):continue
   overlap=ta.overlap(tb)
   if overlap:pairs.append({'head':a.name,'body':b.name,'triangle_intersections':len(overlap)})
 collision[str(yaw)]=pairs
hp.matrix_world=orig;bpy.context.view_layer.update()
out={'stage':stage,'visible_meshes':len(model),'evaluated_triangles':totaltri,'bounds':{'min':[min(p[k] for p in vv) for k in range(3)],'max':[max(p[k] for p in vv) for k in range(3)]},'preserved_approved_head_meshes':len(head),'head_geometry_material_errors':errors,'head_uniform_fit_scale':sc.get('body_head_fit_scale',1),'geometry_issues':issues,'missing_uv_count':len(uvmissing),'head_clearance_intersections_by_yaw':collision,'model':model}
(D/'native_audit.json').write_text(json.dumps(out,indent=2),encoding='utf8')
print(json.dumps({k:v for k,v in out.items() if k!='model'},indent=2),flush=True)
