"""Refine the accepted E-01 assembly and create an editable two-hand ready pose."""
from pathlib import Path
import bpy, math, json, sys
from mathutils import Vector, Matrix

ROOT=Path(__file__).resolve().parent
OUT=ROOT/'stage_03'
OUT.mkdir(exist_ok=True)
(OUT/'renders').mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'stage_02/TYPE_E01_STAGE02.blend'))
neutral=bpy.context.scene
neutral.name='01 NEUTRAL - editable assembly'
HEAD=bpy.data.collections['01 HEAD - review secondary forms']
BODY=bpy.data.collections['02 BODY - primary proportions only']
PACK=bpy.data.collections['03 BACKPACK - primary masses']
RIG=bpy.data.collections['04 EDITABLE PART ROOTS - no animation']
GUN=bpy.data.collections['05 E-01 ASSAULT RIFLE']
DETAIL=bpy.data.collections['06 RIFLE DETAIL DISPLAY']
master=bpy.data.objects['TYPE_E01_MASTER_ROOT']

# Add depth behind the brow. Circular equipment translates rigidly.
def y_shift(y):
    return .055*max(0.,min(1.,(y+.15)/.292))
head_changes=[]
for obj in HEAD.objects:
    if obj.type!='MESH' or 'neck ' in obj.name:
        continue
    if any(tag in obj.name for tag in ('ear ', 'pivot','antenna','screw','service slot')):
        center=sum((obj.matrix_world@v.co for v in obj.data.vertices),Vector())/len(obj.data.vertices)
        shift=y_shift(center.y)
        matrix=obj.matrix_world.copy();matrix.translation.y+=shift
        obj.matrix_world=matrix
    else:
        inv=obj.matrix_world.inverted()
        for vertex in obj.data.vertices:
            p=obj.matrix_world@vertex.co
            p.y+=y_shift(p.y)
            vertex.co=inv@p
        obj.data.update()
        shift='graduated +0..55 mm'
    head_changes.append({'object':obj.name,'profile_shift':shift})
bpy.context.view_layer.update()
bpy.data.objects['E01_HEAD']['head_revision']='03 - extended helmet depth; front silhouette retained'
bpy.data.objects['E01_HEAD']['approval_status']='stage 02 continuation authorized; stage 03 ready for visual review'

# Restrained manufactured paint grain. Scale is consistent across separate plates.
for material in bpy.data.materials:
    if not material.use_nodes or not material.name.startswith(('E01 | warm','E01 | recessed','RIFLE | parkerized','RIFLE | dark phosphate')):
        continue
    nodes=material.node_tree.nodes;links=material.node_tree.links
    principled=nodes.get('Principled BSDF')
    if not principled:continue
    coord=nodes.new('ShaderNodeTexCoord');coord.name='Finish coordinates';coord.location=(-820,-80);coord.object=master
    noise=nodes.new('ShaderNodeTexNoise');noise.name='Subtle paint micrograin';noise.location=(-600,-80)
    noise.inputs['Scale'].default_value=950
    noise.inputs['Detail'].default_value=2
    links.new(coord.outputs['Object'],noise.inputs['Vector'])
    bump=nodes.new('ShaderNodeBump');bump.name='Microscopic manufactured surface';bump.location=(-220,-160)
    bump.inputs['Strength'].default_value=.075;bump.inputs['Distance'].default_value=.00015
    links.new(noise.outputs['Fac'],bump.inputs['Height']);links.new(bump.outputs['Normal'],principled.inputs['Normal'])
    rough=nodes.new('ShaderNodeMapRange');rough.name='Satin paint roughness';rough.location=(-370,70)
    base=principled.inputs['Roughness'].default_value
    rough.inputs['From Min'].default_value=0;rough.inputs['From Max'].default_value=1
    rough.inputs['To Min'].default_value=max(.3,base-.045);rough.inputs['To Max'].default_value=base+.045
    links.new(noise.outputs['Fac'],rough.inputs['Value']);links.new(rough.outputs['Result'],principled.inputs['Roughness'])

# A small field stencil and short technical markings echo the mass-production sheet.
ink=bpy.data.materials['E01 | charcoal mechanical frame']
def stencil(name,text,loc,size,normal,parent):
    cu=bpy.data.curves.new(name,'FONT');cu.body=text;cu.size=size;cu.align_x='CENTER';cu.align_y='CENTER';cu.extrude=.00008
    obj=bpy.data.objects.new(name,cu);BODY.objects.link(obj);obj.data.materials.append(ink)
    normal=Vector(normal).normalized();right=Vector((1,0,0));right=(right-normal*right.dot(normal)).normalized();up=normal.cross(right)
    if up.z<0: right=-right;up=-up
    obj.location=loc;obj.rotation_euler=Matrix((right,up,normal)).transposed().to_euler()
    obj.parent=parent;obj.matrix_parent_inverse=parent.matrix_world.inverted()
    return obj
stencil('L shoulder production designation','E-01',(.545,-.224,2.818),.038,(.06,-1,.03),bpy.data.objects['E01_L_SHOULDER'])
stencil('L shoulder small batch code','01 / STD',(.543,-.224,2.784),.010,(.06,-1,.03),bpy.data.objects['E01_L_SHOULDER'])

master['review_status']='STAGE 03 - finished modeling pass and two-hand pose; awaiting visual review'
master['user_feedback']='Continue refinement; head side needs more front-to-back depth. All outputs on D.'
master['editing']='Switch scenes for neutral and ready pose. Rigid part roots remain editable; shared meshes, no animation rig.'
master['storage']=str(OUT)
for c in (HEAD,BODY,PACK,GUN):c.hide_render=False;c.hide_viewport=False
DETAIL.hide_render=True;DETAIL.hide_viewport=True
neutral.render.filepath=str(OUT/'renders')+'/'
neutral.camera=bpy.data.objects['E01_3Q']

# Duplicate transforms while linking geometry: editing armor is reflected in both scenes.
ready=neutral.copy();ready.name='02 RIFLE READY - two hand pose'
actor_cols=[HEAD,BODY,PACK,RIG,GUN]
for collection in list(ready.collection.children):
    if collection in actor_cols or collection==DETAIL:
        ready.collection.children.unlink(collection)
col_map={};obj_map={};rest={}
for collection in actor_cols:
    target=bpy.data.collections.new('READY | '+collection.name);ready.collection.children.link(target);col_map[collection]=target
    for obj in collection.objects:
        if obj.name=='E01_RIFLE_DETAIL_ROOT':continue
        new=obj.copy();new.name='READY | '+obj.name
        target.objects.link(new);obj_map[obj]=new;rest[obj.name]=obj.matrix_world.copy()
for old,new in obj_map.items():
    if old.parent in obj_map:new.parent=obj_map[old.parent]
    new.matrix_parent_inverse=old.matrix_parent_inverse.copy()
    new.matrix_basis=old.matrix_basis.copy()
bpy.context.window.scene=ready
bpy.context.view_layer.update()
posed={old.name:new for old,new in obj_map.items()}

T=Matrix.Translation
I=Matrix.Identity(4)
def root_move(name,new_joint,rotation):
    old=rest[name]
    posed[name].matrix_world=T(Vector(new_joint))@rotation.to_4x4()@T(-old.translation)@old

def solve_two_bone(start,end,rest_start,rest_mid,rest_end,pole):
    a=(rest_mid-rest_start).length;b=(rest_end-rest_mid).length
    axis=end-start;d=axis.length
    assert abs(a-b)+1e-5<d<a+b-1e-5, ('unreachable limb',d,a,b)
    axis.normalize();along=(a*a-b*b+d*d)/(2*d)
    perpendicular=Vector(pole);perpendicular-=axis*perpendicular.dot(axis);perpendicular.normalize()
    joint=start+axis*along+perpendicular*math.sqrt(max(0,a*a-along*along))
    q1=(rest_mid-rest_start).rotation_difference(joint-start)
    q2=(rest_end-rest_mid).rotation_difference(end-joint)
    return joint,q1.to_matrix(),q2.to_matrix()

old_waist=rest['E01_WAIST'].translation
pelvis_offset=Vector((0,.03,-.33));new_waist=old_waist+pelvis_offset
lean=Matrix.Rotation(math.radians(8),4,'X')
upper_delta=T(new_waist)@lean@T(-old_waist)
posed['E01_WAIST'].matrix_world=T(pelvis_offset)@rest['E01_WAIST']
for name in ('E01_CHEST','E01_BACKPACK'):
    posed[name].matrix_world=upper_delta@rest[name]
pose_data={'legs':{},'arms':{}}
for side,sign in (('R',-1),('L',1)):
    names=['E01_'+side+'_'+x for x in ('THIGH','CALF','FOOT')]
    hip,knee,ankle=[rest[n].translation for n in names]
    newhip=hip+pelvis_offset
    newankle=Vector((-.51,-.58,.380)) if side=='R' else Vector((.50,.40,.380))
    newknee,qr,qc=solve_two_bone(newhip,newankle,hip,knee,ankle,(sign*.15,-1,.05))
    root_move(names[0],newhip,qr);root_move(names[1],newknee,qc)
    root_move(names[2],newankle,Matrix.Rotation(math.radians(-15 if side=='R' else 20),3,'Z'))
    pose_data['legs'][side]={'hip':list(newhip),'knee':list(newknee),'ankle':list(newankle)}
    # Hip flaps articulate outward/forward to follow the raised thigh.
    for original,new in obj_map.items():
        if original.name==side+' front waist flare':
            hinge=Vector((sign*.20,-.15,2.15))+pelvis_offset
            current=T(pelvis_offset)@rest[original.name]
            new.matrix_world=T(hinge)@Matrix.Rotation(math.radians(-18 if side=='R' else -5),4,'X')@T(-hinge)@current

forward=Vector((math.sin(math.radians(50)),-math.cos(math.radians(50)),0))
up=Vector((0,0,1));width=up.cross(forward)
rifle_rotation=Matrix((forward,width,up)).transposed()
primary=Vector((-.285,0,-.178));support=Vector((.23,0,-.093))
grip=Vector((.08,-.51,2.18))
rifle_matrix=T(grip-rifle_rotation@primary)@rifle_rotation.to_4x4()
posed['E01_RIFLE_ROOT'].matrix_world=rifle_matrix
posed['E01_RIFLE_ROOT']['ready_support_contact_local']=list(support)

# Map two direction frames to orient the closed right hand around the pistol grip.
def orient_contact(source_contact,target_contact,source_back,target_back):
    def frame(a,b):
        a=Vector(a).normalized();b=Vector(b);b=(b-a*a.dot(b)).normalized();c=a.cross(b)
        return Matrix((a,b,c)).transposed()
    return frame(target_contact,target_back)@frame(source_contact,source_back).transposed()
right_wrist=grip+Vector((-.09,.08,.10))
right_contact=Vector((-.008,-.042,-.149))
right_rot=orient_contact(right_contact,grip-right_wrist,(0,-1,0),-width)
left_rot=Matrix((-width,up,-forward)).transposed()
left_contact=Vector((.006,.080,-.146))
support_world=rifle_matrix@support
left_wrist=support_world-left_rot@left_contact
for side,wrist,hand_rotation,sign in (('R',right_wrist,right_rot,-1),('L',left_wrist,left_rot,1)):
    names=['E01_'+side+'_'+x for x in ('UPPER_ARM','FOREARM','HAND')]
    shoulder,elbow,rest_wrist=[rest[n].translation for n in names]
    newshoulder=upper_delta@shoulder
    newelbow,qu,qf=solve_two_bone(newshoulder,wrist,shoulder,elbow,rest_wrist,(sign*.60,.10,-.85))
    root_move(names[0],newshoulder,qu);root_move(names[1],newelbow,qf);root_move(names[2],wrist,hand_rotation)
    shell='E01_'+side+'_SHOULDER'
    shell_joint=upper_delta@rest[shell].translation
    shell_rotation=lean.to_quaternion().slerp(qu.to_quaternion(),.26).to_matrix()
    root_move(shell,shell_joint,shell_rotation)
    pose_data['arms'][side]={'shoulder':list(newshoulder),'elbow':list(newelbow),'wrist':list(wrist)}
posed['E01_HEAD'].matrix_world=upper_delta@rest['E01_HEAD']@Matrix.Rotation(math.radians(50),4,'Z')@Matrix.Rotation(math.radians(4),4,'X')
pose_data['gun']={'primary_contact':list(grip),'support_contact':list(support_world),'forward':list(forward)}
bpy.context.view_layer.update()

# Pose cameras have their own datablocks; neutral orthographic cameras are preserved.
pose_cameras=bpy.data.collections.new('READY | CAMERAS');ready.collection.children.link(pose_cameras)
def camera(name,position,target,scale):
    data=bpy.data.cameras.new(name);data.type='ORTHO';data.ortho_scale=scale
    obj=bpy.data.objects.new(name,data);pose_cameras.objects.link(obj);obj.location=position
    obj.rotation_euler=(Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()
    return obj
ready.camera=camera('E01_READY_3Q',(.5,-8,3.6),(.05,-.22,1.42),3.48)
camera('E01_READY_SIDE',(6,2.6,3.5),(.20,-.35,1.42),3.95)
camera('E01_READY_HANDS',(.5,-6,3.3),(.12,-.44,2.29),1.85)
ready.render.filepath=str(OUT/'renders')+'/'
for scene in (neutral,ready):
    scene.render.resolution_x=1050;scene.render.resolution_y=1400
    scene.render.resolution_percentage=100
    scene.cycles.samples=32;scene.cycles.use_denoising=True
    scene.render.image_settings.file_format='PNG';scene.render.image_settings.color_mode='RGB'
    scene['review_note']='Real editable Blender geometry. No image-generation repainting.'
    scene['pose_type']='Rigid part-root assembly; no skeletal animation binding'
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            area.spaces.active.shading.type='MATERIAL';area.spaces.active.overlay.show_extras=False
            area.spaces.active.region_3d.view_perspective='CAMERA'
readme=bpy.data.texts.new('READ ME - STAGE 03')
readme.write('TYPE E-01 / Stage 03\n\n01 NEUTRAL: four-view modeling inspection.\n02 RIFLE READY: two-hand aimed pose.\n\nMesh and material data are shared across these scenes. Move the named rigid part-root empties to adjust the pose. Armor and weapon remain individual editable pieces.\n\nHead profile extended 55 mm at the rear, with the face and front width retained. Subtle satin finish added. Original user reference is packed.\n\nThis is the modeling and pose review gate. No game export, animation rig, or game integration is included.\nAll production files are stored on D drive.\n')
bpy.context.window.scene=ready
bpy.context.view_layer.update()
for obj in bpy.context.selected_objects:obj.select_set(False)
posed['TYPE_E01_MASTER_ROOT'].select_set(True)
bpy.context.view_layer.objects.active=posed['TYPE_E01_MASTER_ROOT']
neutral.use_fake_user=True
ready.use_fake_user=True
print('SCENE_PERSISTENCE '+str([(s.name,s.users) for s in bpy.data.scenes]),flush=True)
blend=OUT/'TYPE_E01_STAGE03.blend'
bpy.ops.wm.save_as_mainfile(filepath=str(blend),compress=True)
report={'saved':str(blend),'scenes':[neutral.name,ready.name],'objects':len(bpy.data.objects),'mesh_datablocks':len(bpy.data.meshes),'head_changes':head_changes,'pose':pose_data,'status':'ready for actual render review'}
(OUT/'stage_report.json').write_text(json.dumps(report,indent=2),encoding='utf8')
print('STAGE03_SAVED '+str(blend),flush=True)

if '--preview' in sys.argv:
    prefs=bpy.context.preferences.addons['cycles'].preferences
    try:
        prefs.compute_device_type='OPTIX';prefs.get_devices()
        for device in prefs.devices:device.use=device.type=='OPTIX'
        ready.cycles.device='GPU'
    except Exception:ready.cycles.device='CPU'
    for shot in ('READY_3Q','READY_HANDS','READY_SIDE'):
        ready.camera=bpy.data.objects['E01_'+shot]
        ready.render.resolution_x=1000;ready.render.resolution_y=1000 if shot=='READY_HANDS' else 1300
        ready.render.filepath=str(OUT/'renders'/(shot+'.png'))
        bpy.ops.render.render(write_still=True)
        print('STAGE03_RENDERED '+shot,flush=True)
