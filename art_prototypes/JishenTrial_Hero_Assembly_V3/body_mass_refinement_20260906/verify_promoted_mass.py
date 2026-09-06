import bpy,pathlib,json,hashlib
W=pathlib.Path(__file__).resolve().parent;P=W.parent/'JishenTrial_ASSEMBLED_MASTER.blend'
bpy.ops.wm.open_mainfile(filepath=str(P));sc=bpy.context.scene
proof=json.loads((W/'source_inventory.json').read_text())['head_proof'];errors=[]
for p in proof:
 o=bpy.data.objects[p['name']]
 h=hashlib.sha256(repr(([tuple(v.co) for v in o.data.vertices],[tuple(f.vertices) for f in o.data.polygons],[[tuple(l.uv) for l in uv.data] for uv in o.data.uv_layers])).encode()).hexdigest()
 if h!=p['mesh_hash'] or [list(row) for row in o.matrix_world]!=p['matrix_world'] or [m.name for m in o.data.materials]!=p['materials']:errors.append(o.name)
assert not errors,errors
assert sc['mass_revision']==10,sc.get('mass_revision')
assert all('V3B_Knee_Mass2_Anterior_Sliding_Underlap.'+side in bpy.data.objects for side in ('L','R'))
merge=json.loads((W/'delivery/merge_audit.json').read_text())
retained_errors=[]
for name,p in merge['preserved_proof'].items():
 o=bpy.data.objects[name]
 h=hashlib.sha256(repr(([tuple(v.co) for v in o.data.vertices],[tuple(f.vertices) for f in o.data.polygons],[[tuple(l.uv) for l in uv.data] for uv in o.data.uv_layers])).encode()).hexdigest() if o.type=='MESH' else None
 actual={'mesh':h,'matrix':[list(r) for r in o.matrix_world],'parent':o.parent.name if o.parent else None,'materials':[m.name if m else None for m in o.data.materials] if o.type=='MESH' else [],'hide_render':o.hide_render}
 if actual!=p:retained_errors.append(name)
assert not retained_errors,retained_errors
assert hashlib.sha256((W.parent/'JishenTrial_GAME_CANDIDATE.blend').read_bytes()).hexdigest()==merge['game_candidate_sha256']
visible=[o for o in sc.objects if o.type=='MESH' and not o.hide_render]
out={'master':str(P),'sha256':hashlib.sha256(P.read_bytes()).hexdigest(),'reopened':True,'mass_revision':sc['mass_revision'],'visible_meshes':len(visible),'approved_head_meshes_unchanged':len(proof),'head_errors':errors,'export_status':sc['export_status'],'head_and_joint_parent_hierarchy_retained':True}
out.update(retained_rk_meshes=merge['retained_rk_meshes'],retained_objects_errors=retained_errors,game_candidate_unchanged=True)
(W/'delivery/promoted_master_check.json').write_text(json.dumps(out,indent=2));print(json.dumps(out,indent=2),flush=True)
