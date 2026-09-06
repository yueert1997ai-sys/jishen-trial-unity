import bpy,bmesh,pathlib,json,sys,hashlib,struct,math
from mathutils import Vector
W=pathlib.Path(__file__).resolve().parent;I=int(next((a.split('=')[1] for a in sys.argv if a.startswith('iter=')),'3'));D=W/f'iteration_{I:02d}'
def fingerprint(o):
 h=hashlib.sha256();h.update(o.type.encode());h.update(str([list(r) for r in o.matrix_world]).encode())
 if o.type=='MESH':
  for v in o.data.vertices:h.update(struct.pack('fff',*v.co))
  for p in o.data.polygons:h.update(str(list(p.vertices)).encode())
 if o.type=='CURVE':
  for s in o.data.splines:
   for p in s.bezier_points:h.update(str((tuple(p.co),tuple(p.handle_left),tuple(p.handle_right))).encode())
   for p in s.points:h.update(str(tuple(p.co)).encode())
 if o.type in ('MESH','CURVE'):h.update(str([m.name for m in o.data.materials]).encode())
 return h.hexdigest()
bpy.ops.wm.open_mainfile(filepath=str(W/'BODY_SOURCE_BEFORE_HEAD.blend'))
old=bpy.data.objects['HEAD_V2_ROOT'];omit=set(old.children_recursive)|{old}
before={o.name:fingerprint(o) for o in bpy.context.scene.objects if o not in omit and o.type in ('MESH','CURVE','EMPTY')}
current=next((a.split('=',1)[1] for a in sys.argv if a.startswith('file=')),str(D/'ASSEMBLED.blend'))
bpy.ops.wm.open_mainfile(filepath=current)
after={o.name:fingerprint(o) for o in bpy.context.scene.objects if o.name in before}
unchanged=before==after
dg=bpy.context.evaluated_depsgraph_get();parts=list(bpy.data.collections['HEAD_V3 | reference matched editable assembly'].all_objects)
audit=[];allv=[]
for o in parts:
 if o.type not in ('MESH','CURVE'):continue
 me=bpy.data.meshes.new_from_object(o.evaluated_get(dg),depsgraph=dg);me.calc_loop_triangles();bm=bmesh.new();bm.from_mesh(me)
 bounds=[o.matrix_world@v.co for v in me.vertices];allv.extend(bounds)
 audit.append({'name':o.name,'triangles':len(me.loop_triangles),'nonmanifold_edges':sum(not e.is_manifold for e in bm.edges),'degenerate_faces':sum(f.calc_area()<1e-13 for f in bm.faces),'min_dimension_m':min(o.dimensions),'modifiers':[m.type for m in o.modifiers]})
 bm.free();bpy.data.meshes.remove(me)
out={'iteration':I,'body_unchanged':unchanged,'body_objects_compared':len(before),'body_changed':[n for n in before if before[n]!=after.get(n)],'head_mesh_curve_objects':len(audit),'head_evaluated_triangles':sum(a['triangles'] for a in audit),'nonmanifold_parts':[a for a in audit if a['nonmanifold_edges']], 'degenerate_parts':[a for a in audit if a['degenerate_faces']], 'head_bounds_m':[[min(v[i] for v in allv) for i in range(3)],[max(v[i] for v in allv) for i in range(3)]], 'parts':audit}
out['audited_file']=current
(D/'technical_audit.json').write_text(json.dumps(out,indent=2))
print(json.dumps({k:v for k,v in out.items() if k!='parts'},indent=2))
assert unchanged,'Unrelated body geometry has changed'
