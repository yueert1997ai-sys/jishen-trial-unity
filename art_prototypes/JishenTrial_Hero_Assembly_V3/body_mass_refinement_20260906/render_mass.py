import bpy,pathlib,sys
from mathutils import Vector
W=pathlib.Path(__file__).resolve().parent
args={a.split('=',1)[0]:a.split('=',1)[1] for a in sys.argv if '=' in a}
D=W/('iteration_'+args.get('stage','01').zfill(2));R=D/'renders';R.mkdir(parents=True,exist_ok=True)
source=args.get('source',str(D/'ASSEMBLED.blend'))
bpy.ops.wm.open_mainfile(filepath=source);sc=bpy.context.scene
dg=bpy.context.evaluated_depsgraph_get();snap=[]
for o in sc.objects:
 if o.type=='MESH' and not o.hide_render:snap.append((o,bpy.data.meshes.new_from_object(o.evaluated_get(dg),preserve_all_data_layers=True,depsgraph=dg)))
for o,me in snap:o.animation_data_clear();o.modifiers.clear();o.data=me
p=bpy.context.preferences.addons['cycles'].preferences;p.compute_device_type='OPTIX';p.get_devices()
for d in p.devices:d.use=d.type=='OPTIX'
sc.cycles.device='GPU';sc.cycles.samples=int(args.get('samples','24'))
sc.render.resolution_x=sc.render.resolution_y=int(args.get('res','1200'))
hidden={o:o.hide_render for o in sc.objects}
for name in args.get('shots','FRONT,THREE_QUARTER,LEFT').split(','):
 for o,h in hidden.items():
  o.hide_render=h
  if name.startswith('WEAPON') and o.type=='MESH' and not o.name.startswith('V3B_Sword_'):o.hide_render=True
  if ('OFF' in name or name=='WEAPON_EDGE') and o.get('beam_component'):o.hide_render=True
  if name=='SIDE_CORE' and o.type=='MESH' and any(c.name.startswith(('BODY_V3 | 08','BODY_V3 | 09','BODY_V3 | 10')) for c in o.users_collection):o.hide_render=True
 if name.startswith('CLAY'):
  mat=bpy.data.materials.get('MASS_REVIEW_CLAY') or bpy.data.materials.new('MASS_REVIEW_CLAY');mat.diffuse_color=(.34,.39,.46,1);mat.use_nodes=True
  node=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED');node.inputs['Base Color'].default_value=(.34,.39,.46,1);node.inputs['Roughness'].default_value=.55
  sc.view_layers[0].material_override=mat;cam='THREE_QUARTER'
 else:
  sc.view_layers[0].material_override=None;cam=name
  if name in ('WEAPON_ON','WEAPON_OFF'):cam='WEAPON_3Q'
  if name in ('FULL_BODY_BEAM_ON','FULL_BODY_BEAM_OFF'):cam='THREE_QUARTER'
 sc.camera=bpy.data.objects['V3B_CAM_'+cam];sc.render.filepath=str(R/(name+'.png'));bpy.ops.render.render(write_still=True);print('RENDER_DONE',name,flush=True)
