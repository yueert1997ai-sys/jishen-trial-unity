import bpy,pathlib,hashlib,json,shutil
W=pathlib.Path(__file__).resolve().parent;V=W/'iteration_v3';D=V/'weapon_pose';P=W.parent
audit=json.loads((D/'pose_audit.json').read_text());current=P/'JishenTrial_ASSEMBLED_MASTER.blend';staged=D/'VALKYR_V3_WEAPON_POSE.blend'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
assert sha(current)==audit['source_sha256'],'Current master changed; do not overwrite.'
bpy.ops.wm.open_mainfile(filepath=str(staged));sc=bpy.context.scene
errors=[]
for name,p in audit['mesh_proof'].items():
 o=bpy.data.objects[name]
 h=hashlib.sha256(repr(([tuple(v.co) for v in o.data.vertices],[tuple(f.vertices) for f in o.data.polygons],[[tuple(l.uv) for l in uv.data] for uv in o.data.uv_layers])).encode()).hexdigest()
 if h!=p['hash'] or [m.name if m else None for m in o.data.materials]!=p['materials']:errors.append(name)
assert not errors,errors
assert sc['weapon_pose_revision']==1 and sc['design_iteration']=='V3'
assert bpy.data.objects['AntiShip_Blade_Display_Root'].parent.name=='V3B_Sword_Grip_Socket'
assert bpy.data.objects['RAIKEN_BLADE_TIP'].matrix_world.translation.z>.08
assert sha(current)==audit['source_sha256']
assert sha(V/'VALKYR_ITERATION_V3_MASTER.blend')==audit['source_sha256']
shutil.copy2(staged,current)
# Keep the linked V3 native file synchronized; its prior state is archived in D.
shutil.copy2(staged,V/'VALKYR_ITERATION_V3_MASTER.blend')
assert sha(current)==sha(staged)
bpy.ops.wm.open_mainfile(filepath=str(current));assert bpy.context.scene['weapon_pose_revision']==1
report={'master':str(current),'sha256':sha(current),'source_sha256':audit['source_sha256'],'native_master_reopened':True,'weapon_pose_revision':1,'mesh_geometry_uv_material_errors':errors,'grip_retained':True,'weapon_lowest_point_m':audit['weapon_min_height_m'],'forearm_yaw_added_degrees':audit['forearm_yaw_added_degrees'],'wrist_yaw_added_degrees':audit['wrist_yaw_added_degrees'],'exports_run':False}
(D/'saved_master_check.json').write_text(json.dumps(report,indent=2),encoding='utf-8');print(json.dumps(report,indent=2),flush=True)
