import bpy,pathlib,json,hashlib,sys,shutil
from mathutils import Vector
from mathutils.bvhtree import BVHTree
W=pathlib.Path(__file__).resolve().parent;V=W/'iteration_v3';D=V/'reference_action_pose';P=W.parent
args={a.split('=',1)[0]:a.split('=',1)[1] for a in sys.argv if '=' in a}
audit=json.loads((D/'pose_audit.json').read_text());staged=D/'VALKYR_V3_REFERENCE_ACTION.blend';current=P/'JishenTrial_ASSEMBLED_MASTER.blend';alias=V/'VALKYR_ITERATION_V3_MASTER.blend'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
bpy.ops.wm.open_mainfile(filepath=str(staged));sc=bpy.context.scene;bpy.context.view_layer.update()
errors=[]
for name,p in audit['mesh_proof'].items():
 o=bpy.data.objects[name]
 h=hashlib.sha256(repr(([tuple(v.co) for v in o.data.vertices],[tuple(f.vertices) for f in o.data.polygons],[[tuple(l.uv) for l in uv.data] for uv in o.data.uv_layers])).encode()).hexdigest()
 if h!=p['hash'] or [m.name if m else None for m in o.data.materials]!=p['materials']:errors.append(name)
assert not errors,errors
assert sum(o.type=='MESH' for o in sc.objects)==audit['mesh_count_unchanged']
assert sc['weapon_pose_revision']==2 and sc['design_iteration']=='V3'
O=bpy.data.objects;root=O['AntiShip_Blade_Display_Root'];axis=(root.matrix_world.to_3x3()@Vector((-1,0,0))).normalized()
assert axis.x<-.7 and axis.z>.5,list(axis)
assert root.parent.name=='V3B_Sword_Grip_Socket'
grips={s:list(root.matrix_world.inverted()@O[n].matrix_world.translation) for s,n in [('R','V3B_Sword_Grip_Socket'),('L','V3B_Left_Support_Grip_Socket')]}
assert abs(grips['R'][0]+.35)<1e-4 and abs(grips['L'][0]-.35)<1e-4
assert all(abs(grips[s][k])<1e-4 for s in ('R','L') for k in (1,2))
dg=bpy.context.evaluated_depsgraph_get();guards=[];arm_armor=[]
for o in sc.objects:
 if o.type!='MESH' or o.hide_render:continue
 guard=o.name.startswith('RK_Guard_') and 'Frame' in o.name
 armor=o.name.startswith('V3B_') and any(k in o.name for k in ('Forearm_','Upperarm_','Chest_','Thorax_'))
 if not (guard or armor):continue
 ev=o.evaluated_get(dg);me=ev.to_mesh();vs=[o.matrix_world@v.co for v in me.vertices];fs=[tuple(p.vertices) for p in me.polygons]
 lo=Vector([min(p[k] for p in vs) for k in range(3)]);hi=Vector([max(p[k] for p in vs) for k in range(3)])
 entry=(o.name,lo,hi,BVHTree.FromPolygons(vs,fs));(guards if guard else arm_armor).append(entry);ev.to_mesh_clear()
contacts=[]
for a in guards:
 for b in arm_armor:
  if all(a[1][k]<=b[2][k] and a[2][k]>=b[1][k] for k in range(3)):
   overlap=a[3].overlap(b[3])
   if overlap:contacts.append({'guard':a[0],'armor':b[0],'triangle_contacts':len(overlap)})
report={'staged':str(staged),'sha256':sha(staged),'source_sha256':audit['source_sha256'],'native_reopened':True,'weapon_pose_revision':2,'mesh_geometry_uv_material_errors':errors,'mesh_count':audit['mesh_count_unchanged'],'blade_axis_world':list(axis),'grip_centers_in_weapon':grips,'guard_to_arm_chest_surface_contacts':contacts,'game_candidate_sha256':sha(P/'JishenTrial_GAME_CANDIDATE.blend'),'exports_run':False}
if args.get('promote')=='1':
 assert not contacts,contacts
 manifest=json.loads((D/'four_views/render_manifest.json').read_text())
 assert manifest['source_sha256']==sha(staged),'Four views do not match native pose'
 assert sha(current)==audit['source_sha256'] and sha(alias)==audit['source_sha256'],'Current master changed; do not overwrite'
 assert report['game_candidate_sha256']=='3bfcf79c1b0b62f43feb4f575d0d2d32363e7448e849266a7c6233748ca64359'
 shutil.copy2(staged,current);shutil.copy2(staged,alias)
 bpy.ops.wm.open_mainfile(filepath=str(current));assert bpy.context.scene['weapon_pose_revision']==2
 assert sha(current)==sha(staged)==sha(alias)
 for name in ('FRONT.png','BACK.png','LEFT.png','RIGHT.png','FOUR_VIEWS.png','render_manifest.json'):shutil.copy2(D/'four_views'/name,V/'four_views'/name)
 shutil.copy2(D/'renders/ASSEMBLED.png',V/'renders/ASSEMBLED.png')
 report.update(master=str(current),master_sha256=sha(current),master_reopened_after_save=True,views_synced=True)
 (D/'saved_master_check.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
else:(D/'verification.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report,indent=2),flush=True)
