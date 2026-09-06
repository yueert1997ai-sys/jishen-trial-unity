import bpy,pathlib,json,hashlib,shutil,math
from mathutils import Vector,Matrix
W=pathlib.Path(__file__).resolve().parent;P=W.parent;D=W/'iteration_08';OUT=W/'delivery';OUT.mkdir(exist_ok=True)
audit=json.loads((D/'technical_audit.json').read_text());fit=json.loads((D/'fit_audit.json').read_text())
assert audit['body_unchanged'] and not audit['nonmanifold_parts'] and not audit['degenerate_parts']
assert all(not s['surface_intersections'] for s in fit['checks'])
original_sha=hashlib.sha256((W/'BODY_SOURCE_BEFORE_HEAD.blend').read_bytes()).hexdigest()
current=P/'JishenTrial_ASSEMBLED_MASTER.blend'
assert hashlib.sha256(current.read_bytes()).hexdigest()==original_sha,'Current master changed externally; promotion stopped.'
bpy.ops.wm.open_mainfile(filepath=str(D/'ASSEMBLED.blend'));sc=bpy.context.scene
hc=bpy.data.collections['HEAD_V3 | reference matched editable assembly'];root=bpy.data.objects['HEAD_V3_ROOT']
# Pack a useful UV map on the editable control meshes, preserving all armor modifiers.
bpy.ops.object.select_all(action='DESELECT');meshes=[o for o in hc.all_objects if o.type=='MESH']
for o in meshes:o.hide_set(False);o.select_set(True)
bpy.context.view_layer.objects.active=meshes[0];bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(angle_limit=math.radians(60),island_margin=.004);bpy.ops.object.mode_set(mode='OBJECT')
root['reviewed_iteration']=8;root['evaluated_triangles']=audit['head_evaluated_triangles'];root['native_parts']=len(meshes)
sc['head_reference_delivery']=str(OUT);sc['current_delivery']=str(OUT/'JishenTrial_WITH_REFERENCE_HEAD.blend')
sc['head_revision']='Supplied VALKYR reference recreation, 8 rendered iterations, installed on existing Head pivot'
sc['body_preservation']='1655 existing body/assembly objects compared unchanged against BODY_SOURCE_BEFORE_HEAD.blend'
sc.camera=bpy.data.objects['V3H_CAM_ON_BODY'];sc.view_settings.view_transform='Standard'
sc.render.engine='CYCLES';sc.cycles.samples=64;sc.cycles.use_denoising=True
prefs=bpy.context.preferences.addons['cycles'].preferences;prefs.compute_device_type='OPTIX';prefs.get_devices()
for dev in prefs.devices:dev.use=dev.type=='OPTIX'
sc.cycles.device='GPU';sc.render.resolution_x=sc.render.resolution_y=1600;sc.render.resolution_percentage=100
sc.render.image_settings.file_format='PNG';sc.render.image_settings.color_mode='RGBA';sc.render.film_transparent=False
for o in sc.objects:
 if o.type=='LIGHT' and o.name.startswith('V3H_'):o.hide_render=True;o.data.energy*=.22
for screen in bpy.data.screens:
 for area in screen.areas:
  if area.type=='VIEW_3D':
   space=area.spaces.active;space.shading.type='SOLID';space.shading.color_type='MATERIAL';space.overlay.show_relationship_lines=False;space.overlay.show_extras=False;space.region_3d.view_perspective='CAMERA'
bpy.ops.object.select_all(action='DESELECT');root.select_set(True);bpy.context.view_layer.objects.active=root
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'JishenTrial_WITH_REFERENCE_HEAD.blend'))
shutil.copy2(OUT/'JishenTrial_WITH_REFERENCE_HEAD.blend',current)
# The independent head is derived from that same assembled state.
keep=set(hc.all_objects)
for cname in ('HEAD_V3 | review lights','HEAD_V3 | inspection cameras','HEAD_V3 | packed reference'):keep.update(bpy.data.collections[cname].all_objects)
w=root.matrix_world.copy();root.parent=None;root.matrix_world=w
for o in list(sc.objects):
 if o not in keep:bpy.data.objects.remove(o,do_unlink=True)
for o in sc.objects:
 if o.type=='LIGHT':o.hide_render=False
world=bpy.data.worlds.new('V3H_Neutral_Review_World');world.use_nodes=True
bg=next(n for n in world.node_tree.nodes if n.type=='BACKGROUND');bg.inputs['Color'].default_value=(.075,.085,.11,1);bg.inputs['Strength'].default_value=.32
sc.world=world;sc.camera=bpy.data.objects['V3H_CAM_THREE_QUARTER'];sc.render.film_transparent=False
sc['current_delivery']=str(OUT/'VALKYR_HEAD_MASTER.blend');sc['head_standalone_coordinate_note']='Original body coordinates retained. Mount pivot at (0,0,3.002999782562256) metres.'
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'VALKYR_HEAD_MASTER.blend'))
# Single-mesh exchange copy. Control-mesh master stays editable in the file above.
bpy.ops.object.select_all(action='DESELECT')
for o in meshes:o.select_set(True)
bpy.context.view_layer.objects.active=meshes[0];bpy.ops.object.convert(target='MESH');bpy.ops.object.join();head=bpy.context.object;head.name='VALKYR_HEAD_Reference_Mesh';head.parent=None
head.matrix_world=Matrix.Identity(4);pivot=Vector((0,0,3.002999782562256));head.data.transform(Matrix.Translation(-pivot));head.location=pivot
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(angle_limit=math.radians(60),island_margin=.002);bpy.ops.object.mode_set(mode='OBJECT')
tri=head.modifiers.new('Explicit exchange triangulation','TRIANGULATE');bpy.ops.object.modifier_apply(modifier=tri.name)
head.data.calc_loop_triangles();uv=head.data.uv_layers.active
export_audit={'mesh_objects':1,'triangles':len(head.data.loop_triangles),'materials':[m.name for m in head.data.materials],'uv_loops':len(uv.data),'uv_outside_0_1':sum(any(x < -1e-6 or x >1+1e-6 for x in loop.uv) for loop in uv.data),'mount_pivot_m':list(pivot),'bounds_m':audit['head_bounds_m']}
assert not export_audit['uv_outside_0_1']
bpy.ops.export_scene.fbx(filepath=str(OUT/'VALKYR_HEAD_Reference.fbx'),use_selection=True,object_types={'MESH'},use_mesh_modifiers=True,add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,path_mode='AUTO')
bpy.ops.export_scene.gltf(filepath=str(OUT/'VALKYR_HEAD_Reference.glb'),export_format='GLB',use_selection=True,export_apply=True)
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'VALKYR_HEAD_EXCHANGE.blend'))
R=OUT/'renders';R.mkdir(exist_ok=True)
for p in (D/'renders').glob('*.png'):shutil.copy2(p,R/p.name)
for n in ('REFERENCE_COMPARISON.jpg','FRONT_ALIGNED_COMPARISON.jpg'):shutil.copy2(D/n,OUT/n)
for n in ('technical_audit.json','fit_audit.json'):shutil.copy2(D/n,OUT/n)
(OUT/'delivery_manifest.json').write_text(json.dumps({'source_backup':str(W/'BODY_SOURCE_BEFORE_HEAD.blend'),'source_sha256':original_sha,'current_master':str(current),'current_master_sha256':hashlib.sha256(current.read_bytes()).hexdigest(),'assembled_copy':str(OUT/'JishenTrial_WITH_REFERENCE_HEAD.blend'),'standalone_head':str(OUT/'VALKYR_HEAD_MASTER.blend'),'reference_image_packed':True,'reviewed_iterations':8,'rendered_views':[p.stem for p in R.glob('*.png')],'export':export_audit},indent=2))
print('DELIVERY_SAVED',json.dumps(export_audit),flush=True)
