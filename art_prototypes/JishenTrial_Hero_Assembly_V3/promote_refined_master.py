import bpy,pathlib,json,hashlib,shutil,ast
from mathutils import Vector
P=pathlib.Path(__file__).resolve().parent;W=P/'refinement';current=P/'JishenTrial_ASSEMBLED_MASTER.blend'
stats=json.loads((W/'logs'/'master_stats.json').read_text());expected=stats['master_source_sha256']
assert hashlib.sha256(current.read_bytes()).hexdigest()==expected,'Current master changed externally; do not overwrite.'
backup=P/'iterations'/'before_refinement_20260905';backup.mkdir(exist_ok=True)
if not (backup/current.name).exists():shutil.copy2(current,backup/current.name)
bpy.ops.wm.open_mainfile(filepath=str(W/'PHASE_07.blend'));sc=bpy.context.scene
tree=ast.parse((P/'render_refinement.py').read_text());shots=next(ast.literal_eval(n.value) for n in tree.body if isinstance(n,ast.Assign) and any(isinstance(t,ast.Name) and t.id=='shots' for t in n.targets))
for name,(existing,spec) in shots.items():
 if not spec:continue
 at,target,scale=spec;c=bpy.data.objects.new('REFINE_'+name,bpy.data.cameras.new('REFINE_'+name));bpy.data.collections['CAMERAS'].objects.link(c)
 c.location=at;c.rotation_euler=(Vector(target)-c.location).to_track_quat('-Z','Y').to_euler();c.data.type='ORTHO';c.data.ortho_scale=scale;c.data.clip_start=.01
sc.camera=bpy.data.objects['ASSEMBLY_3Q'];sc.render.resolution_x=1000;sc.render.resolution_y=1250;sc.render.resolution_percentage=100
sc['crown_height_m']=stats['crown_height_m'];sc['rigging_state']='Editable high-detail master with original rigid assembly pivots. Refined demo derivative has a Generic rigid armature.'
sc['current_delivery']='refinement/JishenTrial_REFINED_DEMO.blend and refinement/exports/JishenTrial_Refined_LOD0.fbx'
sc['refinement_backup']=str(backup/current.name)
root=bpy.data.objects['AntiShip_Blade_Display_Root'];root['Beam_On']=True;root.update_tag();sc.frame_set(sc.frame_current)
for layer in sc.view_layers:layer.material_override=None
for screen in bpy.data.screens:
 for area in screen.areas:
  if area.type=='VIEW_3D':
   a=area.spaces.active;a.shading.color_type='MATERIAL';a.shading.type='SOLID';a.overlay.show_relationship_lines=False;a.overlay.show_extras=False;a.region_3d.view_perspective='CAMERA'
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(current));shutil.copy2(current,W/'JishenTrial_REFINED_MASTER.blend')
(W/'logs'/'master_promotion.json').write_text(json.dumps({'master':str(current),'backup':str(backup/current.name),'prior_sha256':expected,'current_sha256':hashlib.sha256(current.read_bytes()).hexdigest(),'additional_closeup_cameras':sum(bool(s[1]) for s in shots.values())},indent=2))
print('PROMOTED_EXISTING_MASTER',str(current))
