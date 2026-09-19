import bpy,bmesh,json,pathlib,math
R=pathlib.Path(bpy.data.filepath).parent;dg=bpy.context.evaluated_depsgraph_get();out=[]
for o in bpy.data.collections['FAZZ_COMPLETE_ASSEMBLY'].all_objects:
 if o.type!='MESH':continue
 ev=o.evaluated_get(dg);me=ev.to_mesh();me.calc_loop_triangles();bm=bmesh.new();bm.from_mesh(me)
 invalid=sum(not math.isfinite(c) for v in me.vertices for c in v.co)
 zero=sum((me.vertices[t.vertices[1]].co-me.vertices[t.vertices[0]].co).cross(me.vertices[t.vertices[2]].co-me.vertices[t.vertices[0]].co).length<1e-12 for t in me.loop_triangles)
 out.append(dict(marking=bool(o.get('surface_marking')),name=o.name,triangles=len(me.loop_triangles),invalid=invalid,zero=zero,boundary=sum(e.is_boundary for e in bm.edges),nonmanifold=sum(not e.is_manifold for e in bm.edges)))
 bm.free();ev.to_mesh_clear()
r={'file':str(bpy.data.filepath),'objects':len(out),'triangles':sum(x['triangles'] for x in out),'markings':[x['name'] for x in out if x['marking']], 'issues':[x for x in out if any(x[k] for k in ['invalid','zero']) or (not x['marking'] and (x['boundary'] or x['nonmanifold']))]}
(R/'work'/'geometry_audit.json').write_text(json.dumps(r,indent=2));print(json.dumps(r,indent=2))
