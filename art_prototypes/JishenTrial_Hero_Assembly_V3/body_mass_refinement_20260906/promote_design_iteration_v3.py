import bpy,pathlib,json,hashlib,shutil
W=pathlib.Path(__file__).resolve().parent;D=W/'iteration_v3';P=W.parent
proof=json.loads((D/'source_proof.json').read_text());audit=json.loads((D/'build_audit.json').read_text())
staged=D/'VALKYR_ITERATION_V3_MASTER.blend';current=P/'JishenTrial_ASSEMBLED_MASTER.blend'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
assert sha(current)==proof['source_sha256'],'Current master has changed; do not overwrite it.'
assert sha(P/'JishenTrial_GAME_CANDIDATE.blend')==proof['game_sha256'],'Game candidate changed externally.'
bpy.ops.wm.open_mainfile(filepath=str(staged));sc=bpy.context.scene
assert sc['design_iteration']=='V3' and sc['mass_revision']==11
assert not audit['geometry_issues'] and not audit['preserved_errors']
changed=set(audit['changed_objects']);removed=set(audit['removed_objects']);errors=[]
for name,p in proof['objects'].items():
 if name in changed or name in removed:continue
 o=bpy.data.objects[name]
 h=hashlib.sha256(repr(([tuple(v.co) for v in o.data.vertices],[tuple(f.vertices) for f in o.data.polygons],[[tuple(l.uv) for l in uv.data] for uv in o.data.uv_layers])).encode()).hexdigest() if o.type=='MESH' else None
 actual={'type':o.type,'hash':h,'matrix':[list(r) for r in o.matrix_world],'parent':o.parent.name if o.parent else None,'materials':[m.name if m else None for m in o.data.materials] if o.type=='MESH' else []}
 if actual!=p:errors.append(name)
assert not errors,errors
assert all('V3B_Knee_Mass2_Anterior_Sliding_Underlap.'+s in bpy.data.objects for s in ('L','R'))
new=[o for o in sc.objects if o.type=='MESH' and o.name.startswith('V3B_Thigh_V3_')]
assert len(new)==audit['new_thigh_meshes'] and all(o.data.uv_layers for o in new),(len(new),audit['new_thigh_meshes'])
assert sha(current)==proof['source_sha256'],'Current master changed while verifying.'
shutil.copy2(staged,current)
assert sha(current)==sha(staged)
bpy.ops.wm.open_mainfile(filepath=str(current));assert bpy.context.scene['design_iteration']=='V3'
report={'master':str(current),'sha256':sha(current),'source_sha256':proof['source_sha256'],'design_iteration':'V3','mass_revision':11,'master_reopened':True,'head_meshes_unchanged':sum(n.startswith('V3H_') and p['type']=='MESH' for n,p in proof['objects'].items()),'weapon_meshes_unchanged':sum(n.startswith('RK_') and p['type']=='MESH' for n,p in proof['objects'].items()),'unchanged_objects_errors':errors,'knee_enclosures_preserved':True,'new_thigh_meshes':len(new),'geometry_issues':audit['geometry_issues'],'game_candidate_unchanged':sha(P/'JishenTrial_GAME_CANDIDATE.blend')==proof['game_sha256'],'exports_run':False,'validation_scope':'Static design, real front/side/detail renders and native geometry; full motion clearance not tested.'}
(D/'saved_master_check.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report,indent=2),flush=True)
