import bpy,pathlib,json,math,hashlib,shutil,sys
from mathutils import Vector,Matrix
W=pathlib.Path(__file__).resolve().parent;D=W/'delivery';R=D/'renders';D.mkdir(exist_ok=True);R.mkdir(exist_ok=True);P=W.parent
bpy.ops.wm.open_mainfile(filepath=str(W/'stage_09/ASSEMBLED.blend'));sc=bpy.context.scene
audit=json.loads((W/'stage_09/native_audit.json').read_text())
assert not audit['geometry_issues'] and not audit['head_geometry_material_errors'] and not audit['missing_uv_count']
assert all(not x for x in audit['head_clearance_intersections_by_yaw'].values())
wr=bpy.data.objects['AntiShip_Blade_Display_Root'];beam=[o for o in sc.objects if o.type=='MESH' and o.get('beam_component')]
switch=[]
for on in (False,True):
 wr['Beam_On']=on;wr.update_tag();sc.frame_set(sc.frame_current);bpy.context.view_layer.update()
 state={'Beam_On':on,'beam_meshes':len(beam),'hidden':sum(o.hide_render for o in beam),'physical_blade_present':not bpy.data.objects['V3B_Sword_Continuous_Physical_Diamond_Blade'].hide_render}
 assert state['hidden']==(0 if on else len(beam)),state
 assert state['physical_blade_present'];switch.append(state)

def cam(name,loc,target,scale):
 d=bpy.data.cameras.new('V3B_CAM_'+name);d.type='ORTHO';d.ortho_scale=scale
 o=bpy.data.objects.new(d.name,d);sc.collection.objects.link(o);o.location=loc;o.rotation_euler=(Vector(target)-o.location).to_track_quat('-Z','Y').to_euler();return o
cam('HAND',(-3,-5,2.7),(-.96,-.20,1.82),.63)
gc=Vector(sc['sword_grip_center_units'])*(2.974/348);a=Vector(sc['sword_grip_axis']).normalized();u=Vector((.9536,0,.301));u=(u-a*u.dot(a)).normalized();n=a.cross(u).normalized();center=gc+a*(98*(2.974/348))
wc=cam('WEAPON',center-n*6,center,2.27);wc.rotation_euler=Matrix((-a,u,-n)).transposed().to_euler()
sc['current_delivery']=str(D/'VALKYR_ASSEMBLED_MASTER.blend');sc['body_reference_delivery']=str(D)
sc['body_preservation']='Existing mechanical hierarchy retained; body armor completed from latest full-body reference; original source archived in ACCEPTED_HEAD_ASSEMBLY_SOURCE.blend.'
sc['body_completion_status']='Reference body and side-depth correction complete; render and topology reviewed. Demo derivative is separate.'
sc.camera=bpy.data.objects['V3B_CAM_THREE_QUARTER'];sc.cycles.samples=64
for screen in bpy.data.screens:
 for area in screen.areas:
  if area.type=='VIEW_3D':
   sp=area.spaces.active;sp.shading.type='SOLID';sp.shading.color_type='MATERIAL';sp.overlay.show_relationship_lines=False;sp.overlay.show_extras=False;sp.region_3d.view_perspective='CAMERA'
oldsha=hashlib.sha256((W/'ACCEPTED_HEAD_ASSEMBLY_SOURCE.blend').read_bytes()).hexdigest();current=P/'JishenTrial_ASSEMBLED_MASTER.blend'
assert hashlib.sha256(current.read_bytes()).hexdigest()==oldsha,'Master changed externally; keep staged delivery.'
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(D/'VALKYR_ASSEMBLED_MASTER.blend'));shutil.copy2(D/'VALKYR_ASSEMBLED_MASTER.blend',current)
manifest={'current_master':str(current),'master_sha256':hashlib.sha256(current.read_bytes()).hexdigest(),'source_sha256':oldsha,'master_triangles':audit['evaluated_triangles'],'master_meshes':audit['visible_meshes'],'approved_head_meshes_unchanged':audit['preserved_approved_head_meshes'],'beam_toggle_verified':switch,'reference':str(W/'REFERENCE.png')}
(D/'native_delivery_manifest.json').write_text(json.dumps(manifest,indent=2));shutil.copy2(W/'stage_09/native_audit.json',D/'native_audit.json')
for f in (W/'stage_09/renders').glob('*.png'):shutil.copy2(f,R/f.name)

# Reopen the actual promoted master, then render its evaluated scene.
bpy.ops.wm.open_mainfile(filepath=str(current));sc=bpy.context.scene;dg=bpy.context.evaluated_depsgraph_get()
snap=[(o,bpy.data.meshes.new_from_object(o.evaluated_get(dg),preserve_all_data_layers=True,depsgraph=dg)) for o in sc.objects if o.type=='MESH' and not o.hide_render]
for o,me in snap:o.animation_data_clear();o.modifiers.clear();o.data=me
p=bpy.context.preferences.addons['cycles'].preferences;p.compute_device_type='OPTIX';p.get_devices()
for dev in p.devices:dev.use=dev.type=='OPTIX'
sc.cycles.device='GPU';sc.cycles.samples=32;sc.render.resolution_percentage=100
hidden={o:o.hide_render for o in sc.objects}
clay=bpy.data.materials.new('Review clay');clay.diffuse_color=(.4,.43,.48,1);clay.use_nodes=True;bs=next(n for n in clay.node_tree.nodes if n.type=='BSDF_PRINCIPLED');bs.inputs['Base Color'].default_value=(.4,.43,.48,1);bs.inputs['Roughness'].default_value=.6
shots=('MASTER_REOPENED_3Q','CLAY_3Q','WAIST','LEGS','FOOT','ARM','HAND','BACKPACK','CANNON','WEAPON_BEAM_ON','WEAPON_BEAM_OFF','FULL_BODY_BEAM_OFF')
for name in shots:
 for o,h in hidden.items():o.hide_render=h
 off=name in ('CLAY_3Q','WEAPON_BEAM_OFF','FULL_BODY_BEAM_OFF')
 for o in sc.objects:
  if o.type!='MESH':continue
  if o.get('beam_component'):o.hide_render=off
  if name.startswith('WEAPON_') and not o.name.startswith('V3B_Sword_'):o.hide_render=True
 for vl in sc.view_layers:vl.material_override=clay if name=='CLAY_3Q' else None
 cn='WEAPON' if name.startswith('WEAPON_') else 'THREE_QUARTER' if name in ('MASTER_REOPENED_3Q','CLAY_3Q','FULL_BODY_BEAM_OFF') else name
 sc.camera=bpy.data.objects['V3B_CAM_'+cn];sc.render.resolution_x=1700;sc.render.resolution_y=600 if name.startswith('WEAPON_') else 1700
 sc.render.filepath=str(R/(name+'.png'));bpy.ops.render.render(write_still=True);print('NATIVE_REOPEN_RENDER',name,flush=True)
manifest['actual_promoted_master_reopened']=True;manifest['reopened_render_views']=list(shots);(D/'native_delivery_manifest.json').write_text(json.dumps(manifest,indent=2))
