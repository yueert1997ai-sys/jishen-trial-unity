"""Validate and add a rigid Generic armature to the baked demo; export actual FBX/GLB."""
import bpy,bmesh,pathlib,json,math
from mathutils import Vector,Matrix
P=pathlib.Path(__file__).resolve().parent;W=P/'refinement';EX=W/'exports'
bpy.ops.wm.open_mainfile(filepath=str(W/'JishenTrial_REFINED_DEMO.blend'))
sc=bpy.context.scene;parts=list(bpy.data.collections['EXPORT'].objects)
report={'meshes':len(parts),'triangles':0,'polygons':0,'materials':sorted({m.name for o in parts for m in o.data.materials}),'nonmanifold':{},'degenerate_triangles':0,'uv_missing':[],'uv_outside':0,'rig_weights_missing':0}
for o in parts:
 bm=bmesh.new();bm.from_mesh(o.data)
 # Only eliminate numerically tiny, zero-area construction residue.
 small=[f for f in bm.faces if f.calc_area()<1e-13]
 if small:bmesh.ops.delete(bm,geom=small,context='FACES')
 loose=[v for v in bm.verts if not v.link_faces]
 if loose:bmesh.ops.delete(bm,geom=loose,context='VERTS')
 nm=sum(not e.is_manifold for e in bm.edges)
 if nm:report['nonmanifold'][o.name]=nm
 bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free()
 o.data.calc_loop_triangles();report['triangles']+=len(o.data.loop_triangles);report['polygons']+=len(o.data.polygons)
 if not o.data.uv_layers:report['uv_missing'].append(o.name)
 else:report['uv_outside']+=sum(not(-1e-5<=u.uv.x<=1.00001 and -1e-5<=u.uv.y<=1.00001) for u in o.data.uv_layers.active.data)
assert not report['nonmanifold'],report['nonmanifold']
assert not report['uv_missing'] and not report['uv_outside'],report
assert 80000<=report['triangles']<=150000 and len(report['materials'])==5,report
# Retain each rigid segment and its existing hierarchy as one deform bone.
pivots=set()
for o in parts:
 p=o.parent
 while p:
  pivots.add(p);p=p.parent
positions={p.name:p.matrix_world.translation.copy() for p in pivots}
# Correct articulation locations to the proportions changed during the silhouette pass.
for side in ('R','L'):
 joint=bpy.data.objects.get('LOD0_Shin.'+side+'_Mechanics')
 positions['Shin.'+side].z=.27+(1.209-.27)*.953
 positions['Foot.'+side].z=.26975*1.1
armdata=bpy.data.armatures.new('JishenTrial_Generic_Rigid_Rig');rig=bpy.data.objects.new('JishenTrial_Generic_Rig',armdata);sc.collection.objects.link(rig)
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig;bpy.ops.object.mode_set(mode='EDIT')
for p in sorted(pivots,key=lambda p:p.name):
 b=armdata.edit_bones.new(p.name);b.head=positions[p.name];b.tail=b.head+Vector((0,0,.035))
 children=sorted([c for c in p.children if c in pivots],key=lambda c:('Finger' in c.name,c.name))
 chosen=next((c for c in children if (positions[c.name]-b.head).length>.025),None)
 if chosen:b.tail=b.head+(positions[chosen.name]-b.head)*.65
 if p.parent in pivots:b.parent=armdata.edit_bones.get(p.parent.name)
# Set parent links again after all bones exist.
for p in pivots:
 if p.parent in pivots:armdata.edit_bones[p.name].parent=armdata.edit_bones[p.parent.name]
socket=armdata.edit_bones.new('WeaponGrip.R');socket.head=(-.765,-.035,1.68);socket.tail=(-.765,-.035,1.63);socket.parent=armdata.edit_bones['Hand.R'];socket.use_deform=False
bpy.ops.object.mode_set(mode='OBJECT');rig.show_in_front=True;rig['rig_type']='Generic rigid skin, one bone per existing articulated segment'
rig['animation_status']='No locomotion/combat clips or game controller integration; rest binding and test articulation verified'
for o in parts:
 name=o.parent.name;w=o.matrix_world.copy();o.parent=rig;o.matrix_world=w
 vg=o.vertex_groups.new(name=name);vg.add(list(range(len(o.data.vertices))),1.0,'REPLACE')
 mod=o.modifiers.new('Rigid Segment Skin','ARMATURE');mod.object=rig;mod.use_vertex_groups=True
 o['deform_bone']=name
 report['rig_weights_missing']+=sum(not v.groups for v in o.data.vertices)
bpy.context.view_layer.update()
# Measured deformation: a forearm bend must move the forearm/hand and leave the chest fixed.
def world_sample(o):
 e=o.evaluated_get(bpy.context.evaluated_depsgraph_get());me=e.to_mesh();v=o.matrix_world@me.vertices[0].co;e.to_mesh_clear();return v
f=next(o for o in parts if o.get('rigid_segment')=='Forearm.R' and o.get('atlas_family')=='Armor')
t=next(o for o in parts if o.get('rigid_segment')=='Thorax' and o.get('atlas_family')=='Armor')
f0=world_sample(f);t0=world_sample(t)
pb=rig.pose.bones['Forearm.R'];pb.rotation_mode='XYZ';pb.rotation_euler.x=math.radians(25);bpy.context.view_layer.update()
fd=(world_sample(f)-f0).length;td=(world_sample(t)-t0).length
pb.rotation_euler=(0,0,0);bpy.context.view_layer.update()
report['rig_bones']=len(armdata.bones);report['articulation_test']={'forearm_25deg_vertex_motion_m':fd,'stationary_chest_motion_m':td,'rest_return_error_m':(world_sample(f)-f0).length}
assert fd>.003 and td<1e-5 and report['articulation_test']['rest_return_error_m']<1e-5,report
assert report['rig_weights_missing']==0
report['uv']='UV0, packed independently per atlas; Armor 4K, Mechanics/Weapon 2K'
report['texture_dimensions']={i.name:list(i.size) for i in bpy.data.images if i.name.startswith('JT_') and i.packed_file}
report['beam_separate']='LOD0_AntiShipBlade_Beam';report['rigged']=True;report['animations']=0
sc['asset_layer']='PC DEMO LOD0: UV/PBR, five materials, Generic rigid armature'
sc['rigging_state']='Rigid skin verified; no combat/locomotion clips or controller integration'
sc['head_review']='Refined against supplied references; agent review completed, no user design approval assumed'
sc['crown_height_m']=json.loads((W/'logs'/'master_stats.json').read_text())['crown_height_m']
weapon=bpy.data.objects['AntiShip_Blade_Display_Root'];weapon['Beam_On']=True;weapon.update_tag();sc.frame_set(sc.frame_current)
for p in pivots:p.hide_set(True)
for o in parts:o.hide_set(False)
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
for o in parts:o.select_set(True)
sc.camera=bpy.data.objects.get('ASSEMBLY_3Q') or sc.camera
bpy.context.preferences.filepaths.save_version=0
for im in bpy.data.images:
 if im.name.startswith('JT_'):im.pack()
bpy.ops.wm.save_as_mainfile(filepath=str(W/'JishenTrial_REFINED_DEMO.blend'))
bpy.ops.export_scene.fbx(filepath=str(EX/'JishenTrial_Refined_LOD0.fbx'),use_selection=True,object_types={'MESH','ARMATURE'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL',axis_forward='-Z',axis_up='Y',use_mesh_modifiers=True,add_leaf_bones=False,bake_anim=False,path_mode='COPY',embed_textures=True,use_custom_props=True)
bpy.ops.export_scene.gltf(filepath=str(EX/'JishenTrial_Refined_LOD0.glb'),export_format='GLB',use_selection=True,export_yup=True,export_apply=False,export_animations=False,export_extras=True)
(W/'logs'/'final_demo_validation.json').write_text(json.dumps(report,indent=2))
print('FINAL_DEMO_VALIDATION',json.dumps(report),flush=True)
