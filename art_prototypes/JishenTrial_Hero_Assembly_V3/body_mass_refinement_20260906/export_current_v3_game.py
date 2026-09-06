"""Freeze the current V3 geometry for the existing game; preserve editable source."""
import bpy,pathlib,hashlib,json,shutil
from mathutils import Matrix,Vector
W=pathlib.Path(__file__).resolve().parent;D=W/'iteration_v3/game_update';D.mkdir(exist_ok=True)
E=D/'exports';E.mkdir(exist_ok=True)
source=W.parent/'JishenTrial_ASSEMBLED_MASTER.blend'
snapshot=D/'SOURCE_CURRENT_MASTER.blend'
if not snapshot.exists():shutil.copy2(source,snapshot)
sha=hashlib.sha256(snapshot.read_bytes()).hexdigest()
assert sha=='c7297dd9a4cfec2f9ef590237661beb3a1a43c291eb1f522f355feed32862f54',sha
rest_source=W/'iteration_v3/reference_action_pose/SOURCE_BEFORE_REFERENCE_POSE.blend'
bpy.ops.wm.open_mainfile(filepath=str(rest_source))
rest={o.name:o.matrix_basis.copy() for o in bpy.context.scene.objects if o.type=='EMPTY'}
bpy.ops.wm.open_mainfile(filepath=str(snapshot));sc=bpy.context.scene;O=bpy.data.objects;bpy.context.view_layer.update()
original=[o for o in sc.objects if o.type in ('MESH','FONT') and not o.hide_render]
root=O['AntiShip_Blade_Display_Root'];master=O['JISHEN_Master_Root']
weapon_branch=set(root.children_recursive)|{root}
col=bpy.data.collections.new('V3 Game Import');sc.collection.children.link(col)
def empty(name,parent,matrix):
 o=bpy.data.objects.new(name,None);col.objects.link(o);o.parent=parent;o.matrix_world=matrix;o.empty_display_size=.03;return o
# Runtime uses these exact fitted hand frames, rather than estimating a fist center.
empty('V3B_Left_Hand_Pose_Guide',root,O['Hand.L'].matrix_world.copy())
empty('V3B_Left_Grip_Pose_Guide',root,O['V3B_Left_Support_Grip_Socket'].matrix_world.copy())
center=root.matrix_world.translation.copy();tip=O['RAIKEN_BLADE_TIP'].matrix_world.translation.copy()
axis=(tip-center).normalized()
beam_objects=[o for o in original if o.get('beam_component')]
assert beam_objects,'Missing switchable light blade'
beam_center=sum((sum((o.matrix_world@Vector(c) for c in o.bound_box),Vector())/8 for o in beam_objects),Vector())/len(beam_objects)
edge=beam_center-(center+tip)/2;edge=(edge-axis*edge.dot(axis)).normalized()
empty('V3B_Blade_Edge_Frame',root,Matrix.Translation(center+edge))
empty('V3B_Ready_Grip',master,Matrix.Translation(center))
empty('V3B_Ready_Tip',master,Matrix.Translation(tip))
empty('V3B_Ready_Edge',master,Matrix.Translation(center+edge))
empty('VALKYR_Model_Revision_V3_c7297dd9',master,Matrix.Identity(4))
# The game animates a standing base. Restore joint rest values only; retain the
# new surfaces and the approved right-hand/sword relationship and left-hand guides.
for name,m in rest.items():
 o=O.get(name)
 if o and o not in weapon_branch:o.matrix_basis=m
bpy.context.view_layer.update()
for name in ('Forearm.R','Hand.R'):
 o=O[name];o.matrix_world=Matrix.Translation(o.matrix_world.translation);bpy.context.view_layer.update()
source_count=len(original)
for o in original:
 for modifier in o.modifiers:
  if modifier.type=='BEVEL':modifier.segments=1
 if o.type=='FONT':o.data.resolution_u=2
bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get()
beamroot=empty('LOD0_AntiShipBlade_Beam',root,root.matrix_world.copy());beamroot['beam_component']=True
snap=[];groups={};input_tris=0
for o in original:
 me=bpy.data.meshes.new_from_object(o.evaluated_get(dg),preserve_all_data_layers=True,depsgraph=dg)
 me.transform(o.matrix_world);me.calc_loop_triangles();input_tris+=len(me.loop_triangles)
 parent=o.parent
 if o in weapon_branch:parent=beamroot if o.get('beam_component') else root
 else:
  while parent and parent.name.startswith('Finger_'):parent=parent.parent
 ob=bpy.data.objects.new('GAME_'+o.name,me);col.objects.link(ob);ob.parent=parent;ob.matrix_world=Matrix.Identity(4)
 ob['beam_component']=bool(o.get('beam_component'));snap.append(ob);groups.setdefault(parent.name if parent else 'Root',[]).append(ob)
for o in original:bpy.data.objects.remove(o,do_unlink=True)
joined=[]
for parent,parts in groups.items():
 bpy.ops.object.select_all(action='DESELECT')
 for o in parts:o.select_set(True)
 bpy.context.view_layer.objects.active=parts[0]
 if len(parts)>1:bpy.ops.object.join()
 o=bpy.context.object;o.name='LOD0_'+parent+'_V3';o['rigid_segment']=parent
 tri=o.modifiers.new('Game triangle faces','TRIANGULATE');tri.keep_custom_normals=True
 bpy.ops.object.modifier_apply(modifier=tri.name)
 joined.append(o)
sc['source_master_sha256']=sha;sc['game_model_revision']='V3 armor and reference two-hand grip / 2026-09-06'
sc['export_status']='User authorized game integration on 2026-09-06; separate game derivative only.'
bpy.context.preferences.filepaths.save_version=0;bpy.ops.wm.save_as_mainfile(filepath=str(D/'VALKYR_V3_GAME.blend'))
bpy.ops.object.select_all(action='DESELECT')
for o in sc.objects:
 if o.type in ('MESH','EMPTY') and not o.hide_render:o.select_set(True)
bpy.context.view_layer.objects.active=master
bpy.ops.export_scene.gltf(filepath=str(E/'VALKYR_RAIKEN_GAME.glb'),export_format='GLB',use_selection=True,export_apply=True,export_extras=True,export_animations=False)
bpy.ops.export_scene.fbx(filepath=str(E/'VALKYR_RAIKEN_GAME.fbx'),use_selection=True,object_types={'MESH','EMPTY'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL',axis_forward='-Z',axis_up='Y',bake_anim=False,add_leaf_bones=False,use_custom_props=True,path_mode='COPY',embed_textures=True)
# The separate weapon geometry has not changed; carry its existing import asset.
old_weapon=W.parent.parent/'RAIKEN_MkII_20260906/exports/RAIKEN_MkII_GAME.fbx'
shutil.copy2(old_weapon,E/'RAIKEN_MkII_GAME.fbx')
report={'source_master':str(source),'source_sha256':sha,'source_meshes':source_count,'game_meshes':len(joined),'triangles':sum(len(o.data.polygons) for o in joined),'input_triangles_at_one_step_bevel':input_tris,'materials':sorted({m.name for o in joined for m in o.data.materials if m}),'geometry_decimation':False,'standing_animation_base':True,'reference_hand_and_ready_guides':True,'source_master_unchanged':hashlib.sha256(source.read_bytes()).hexdigest()==sha,'exports':{p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in E.iterdir() if p.is_file()}}
assert report['source_master_unchanged']
(D/'export_manifest.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('CURRENT_V3_GAME_EXPORT',json.dumps(report),flush=True)
