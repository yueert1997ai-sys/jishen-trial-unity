import bpy,pathlib,sys,json,time
from mathutils import Vector
P=pathlib.Path(__file__).resolve().parent;sys.path.insert(0,str(P))
phase=int(next(a.split('=')[1] for a in sys.argv if a.startswith('phase=')))
bpy.ops.wm.open_mainfile(filepath=str(P/'refinement'/f'PHASE_{phase:02d}.blend'))
from refine_common import *
# Preserve the master modifiers; only freeze evaluated meshes in this render process.
dg=bpy.context.evaluated_depsgraph_get();snap=[]
for o in SC.objects:
 if o.type in ('MESH','CURVE') and not o.hide_render:snap.append((o,bpy.data.meshes.new_from_object(o.evaluated_get(dg),preserve_all_data_layers=True,depsgraph=dg),o.matrix_world.copy()))
for o,me,w in snap:
 if o.type=='MESH':o.modifiers.clear();o.data=me
 else:
  nm=o.name;pa=o.parent;cs=list(o.users_collection);bpy.data.objects.remove(o,do_unlink=True);o=bpy.data.objects.new(nm,me)
  for c in cs:c.objects.link(o)
  o.parent=pa;o.matrix_world=w
SC.render.engine='CYCLES';SC.cycles.samples=32;SC.cycles.use_denoising=True
p=bpy.context.preferences.addons['cycles'].preferences;p.compute_device_type='OPTIX';p.get_devices()
for d in p.devices:d.use=d.type=='OPTIX'
SC.cycles.device='GPU';SC.view_settings.view_transform='AgX'
clay=bpy.data.materials.new('REFINEMENT_CLAY');clay.diffuse_color=(.32,.32,.32,1);clay.use_nodes=True
bs=next(n for n in clay.node_tree.nodes if n.type=='BSDF_PRINCIPLED');bs.inputs['Base Color'].default_value=(.32,.32,.32,1);bs.inputs['Roughness'].default_value=.75
def cam(name,at,target,scale):
 c=bpy.data.objects.get('REFINE_'+name)
 if not c:c=bpy.data.objects.new('REFINE_'+name,bpy.data.cameras.new('REFINE_'+name));bpy.data.collections['CAMERAS'].objects.link(c)
 c.location=at;c.rotation_euler=(Vector(target)-c.location).to_track_quat('-Z','Y').to_euler();c.data.type='ORTHO';c.data.ortho_scale=scale;c.data.clip_start=.01
 return c
shots={
 'FRONT':('ASSEMBLY_FRONT',None),'LEFT':('ASSEMBLY_LEFT',None),'BACK':('ASSEMBLY_BACK',None),'RIGHT':('ASSEMBLY_RIGHT',None),'FRONT_3Q':('ASSEMBLY_3Q',None),'CLAY_3Q':('ASSEMBLY_3Q',None),
 'HEAD_FRONT':(None,((0,-6,3.18),(0,0,3.18),.54)),'HEAD_SIDE':(None,((6,0,3.18),(0,0,3.18),.54)),'HEAD_3Q':(None,((-2,-5,3.8),(0,0,3.18),.56)),'HEAD_ON_BODY':('ASSEMBLY_HEAD',None),
 'CHEST_FRONT':(None,((0,-6,2.69),(0,0,2.69),1.40)),'CHEST_3Q':(None,((-3,-6,3.5),(0,0,2.66),1.45)),'WAIST_3Q':(None,((-2,-5,2.8),(0,0,2.16),1.04)),
 'LEG_FRONT':(None,((0,-6,1.0),(0,0,1.0),2.15)),'LEG_SIDE':(None,((-6,-.2,1),(0,0,1),2.15)),'FOOT_CLOSEUP':(None,((-2,-4,1.5),(-.38,-.04,.23),.82)),'FULL_BODY_CLAY_AFTER_LEGS':('ASSEMBLY_3Q',None),
 'ARM_CLOSEUP':(None,((-3,-6,3.0),(-.70,0,2.27),1.40)),'HAND_CLOSEUP':(None,((-2,-4,2.2),(-.76,-.015,1.69),.40)),
 'BACKPACK_CLOSEUP':(None,((-3,6,3.8),(0,.33,2.56),1.8)),'SHOULDER_CANNON_CLOSEUP':(None,((-3,-5,4.4),(-.50,0,3.0),.94)),
 'WEAPON_BEAM_ON':(None,((-2.3,-7,3.2),(-1.17,-.60,1.4),2.65)),'WEAPON_BEAM_OFF':(None,((-2.3,-7,3.2),(-1.17,-.60,1.4),2.65)),
 'FULL_BODY_BEAM_ON':('ASSEMBLY_3Q',None),'FULL_BODY_BEAM_OFF':('ASSEMBLY_3Q',None)}
default={1:'FRONT,LEFT,BACK,RIGHT,FRONT_3Q,CLAY_3Q',2:'HEAD_FRONT,HEAD_SIDE,HEAD_3Q,HEAD_ON_BODY',3:'CHEST_FRONT,CHEST_3Q,WAIST_3Q',4:'LEG_FRONT,LEG_SIDE,FOOT_CLOSEUP,FULL_BODY_CLAY_AFTER_LEGS',5:'ARM_CLOSEUP,HAND_CLOSEUP',6:'BACKPACK_CLOSEUP,SHOULDER_CANNON_CLOSEUP,WEAPON_BEAM_ON,WEAPON_BEAM_OFF,FULL_BODY_BEAM_ON,FULL_BODY_BEAM_OFF',7:'FRONT,LEFT,BACK,RIGHT,FRONT_3Q,CLAY_3Q,HEAD_FRONT,HEAD_SIDE,HEAD_3Q,HEAD_ON_BODY,CHEST_FRONT,CHEST_3Q,WAIST_3Q,LEG_FRONT,LEG_SIDE,FOOT_CLOSEUP,ARM_CLOSEUP,HAND_CLOSEUP,BACKPACK_CLOSEUP,SHOULDER_CANNON_CLOSEUP,WEAPON_BEAM_ON,WEAPON_BEAM_OFF,FULL_BODY_BEAM_ON,FULL_BODY_BEAM_OFF'}
only=next((a.split('=')[1] for a in sys.argv if a.startswith('only=')),default.get(phase,'FRONT_3Q')).split(',')
folder=WORK/'renders'/f'phase_{phase:02d}';folder.mkdir(exist_ok=True)
base_hidden={o:o.hide_render for o in SC.objects if o.type=='MESH'}
weapon_parts=set(obj('AntiShip_Blade_Display_Root').children_recursive)
head_parts=set(obj('HEAD_V2_ROOT').children_recursive)|{obj(n) for n in ('14_Low_Profile_Neck_Yaw_Ring','16_Neck_Armor_Collar','17_Neck_Load_Bearing_Pedestal')}
for name in only:
 cn,spec=shots[name];SC.camera=obj(cn) if cn else cam(name,*spec)
 isclay='CLAY' in name;on='OFF' not in name and not isclay and name not in ('ARM_CLOSEUP','HAND_CLOSEUP','HEAD_FRONT','HEAD_SIDE','HEAD_3Q')
 root=obj('AntiShip_Blade_Display_Root');root['Beam_On']=on;root.update_tag();SC.frame_set(SC.frame_current)
 for o,h in base_hidden.items():
  o.hide_render=h
  if name in ('ARM_CLOSEUP','HAND_CLOSEUP') and o in weapon_parts:o.hide_render=True
  if name.startswith('WEAPON_') and o not in weapon_parts and o.name!='Studio_Ground':o.hide_render=True
  if name in ('HEAD_FRONT','HEAD_SIDE','HEAD_3Q') and o not in head_parts and o.name!='Studio_Ground':o.hide_render=True
 for vl in SC.view_layers:vl.material_override=clay if isclay else None
 for o in SC.objects:
  if o.type=='MESH' and any(m.name.startswith('11_') for m in o.data.materials):o.hide_render=not on or name in ('ARM_CLOSEUP','HAND_CLOSEUP','HEAD_FRONT','HEAD_SIDE','HEAD_3Q')
 SC.render.resolution_x=1000;SC.render.resolution_y=1250 if name in ('FRONT','BACK','LEFT','RIGHT','FRONT_3Q','CLAY_3Q') or name.startswith(('FULL_BODY','WEAPON')) else 1000
 SC.render.resolution_percentage=100;SC.render.image_settings.file_format='PNG';SC.render.filepath=str(folder/(name+'.png'))
 bpy.ops.render.render(write_still=True);print('RENDERED',phase,name,flush=True)
