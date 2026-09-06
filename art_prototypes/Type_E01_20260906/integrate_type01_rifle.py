"""Integrate the completed editable TYPE-01 rifle into the approved E-01 body."""
from pathlib import Path
import bpy,math,json,hashlib,sys
from mathutils import Vector,Matrix
ROOT=Path(__file__).resolve().parent
OUT=ROOT/'stage_04_TYPE01';OUT.mkdir(exist_ok=True);(OUT/'renders').mkdir(exist_ok=True)
SOURCE=ROOT.parent/'TYPE01_LaserRifle_20260906/TYPE01_LASER_RIFLE_MASTER.blend'
BASE=ROOT/'stage_03/TYPE_E01_STAGE03.blend'
source_hash=hashlib.sha256(SOURCE.read_bytes()).hexdigest()
base_hash=hashlib.sha256(BASE.read_bytes()).hexdigest()
bpy.ops.wm.open_mainfile(filepath=str(BASE))
neutral=bpy.data.scenes['01 NEUTRAL - editable assembly']
ready=bpy.data.scenes['02 RIFLE READY - two hand pose']
ready.name='02 TYPE01 READY - two hand pose'
T=Matrix.Translation;I=Matrix.Identity(4)

# Evaluate the neutral scene before copying its joint transforms.
bpy.context.window.scene=neutral;bpy.context.view_layer.update()
rest={name:bpy.data.objects[name].matrix_world.copy() for name in ['E01_'+side+'_'+part for side in ('R','L') for part in ('UPPER_ARM','FOREARM','HAND','SHOULDER')]}
low_rotation=bpy.data.objects['E01_RIFLE_ROOT'].matrix_world.to_quaternion().to_matrix()
body_fingerprint={o.name:{'vertices':len(o.data.vertices),'polygons':len(o.data.polygons)} for o in neutral.objects if o.type=='MESH' and any(c.name.startswith(('01 HEAD','02 BODY','03 BACKPACK')) for c in o.users_collection)}
bpy.context.window.scene=ready;bpy.context.view_layer.update()
ready_rotation=bpy.data.objects['READY | E01_RIFLE_ROOT'].matrix_world.to_quaternion().to_matrix()
ready_hands={side:bpy.data.objects['READY | E01_'+side+'_HAND'].matrix_world.to_quaternion().to_matrix() for side in ('R','L')}
shoulders={side:bpy.data.objects['READY | E01_'+side+'_UPPER_ARM'].matrix_world.translation.copy() for side in ('R','L')}
shell_positions={side:bpy.data.objects['READY | E01_'+side+'_SHOULDER'].matrix_world.translation.copy() for side in ('R','L')}
chest_rotation=bpy.data.objects['READY | E01_CHEST'].matrix_world.to_quaternion()

# Remove the prior weapon from both scenes and its isolated equipment display.
removed=[]
for name in ('05 E-01 ASSAULT RIFLE','06 RIFLE DETAIL DISPLAY','READY | 05 E-01 ASSAULT RIFLE'):
    collection=bpy.data.collections.get(name)
    if collection:
        for obj in list(collection.objects):
            removed.append(obj.name);bpy.data.objects.remove(obj,do_unlink=True)
        bpy.data.collections.remove(collection)
for name in ('E01_RIFLE_ROOT','E01_RIFLE_DETAIL_ROOT','READY | E01_RIFLE_ROOT','E01_RIFLE_SIDE','E01_RIFLE_3Q'):
    obj=bpy.data.objects.get(name)
    if obj:removed.append(obj.name);bpy.data.objects.remove(obj,do_unlink=True)

# Append the actual high-detail master, retaining all parts, modifiers and materials.
bpy.context.window.scene=neutral
with bpy.data.libraries.load(str(SOURCE),link=False) as (src,dst):
    dst.collections=[name for name in src.collections if name.startswith('TYPE01 | ') and name!='TYPE01 | Studio']
weapon_cols=[c for c in dst.collections if c]
for collection in weapon_cols:neutral.collection.children.link(collection)
bpy.context.view_layer.update()
weapon_root=bpy.data.objects['TYPE01_LASER_RIFLE_ROOT']
primary=bpy.data.objects['SCK_PRIMARY_GRIP'].location.copy()
support=bpy.data.objects['SCK_SUPPORT_GRIP'].location.copy()
muzzle=bpy.data.objects['SCK_MUZZLE'].location.copy()
source_parts=list(weapon_root.children_recursive)
assert sum(o.type=='MESH' for o in source_parts)==1148
scale=1.85
flip=Matrix.Rotation(math.pi,3,'Z')
low_R=low_rotation@flip
low_target=Vector((-.672,-.085,1.802))
weapon_root.parent=bpy.data.objects['TYPE_E01_MASTER_ROOT'];weapon_root.matrix_parent_inverse=I
weapon_root.matrix_world=T(low_target)@low_R.to_4x4()@Matrix.Scale(scale,4)@T(-primary)
weapon_root['assembly_scale']=scale
weapon_root['source_master']=str(SOURCE)
weapon_root['source_sha256']=source_hash
weapon_root['equipped_unit']='TYPE E-01 approved stage 03 white infantry'

# Create an independently poseable, linked-data instance in the firing scene.
weapon_map={};ready_cols=[]
for collection in weapon_cols:
    new_col=bpy.data.collections.new('READY | '+collection.name);ready.collection.children.link(new_col);ready_cols.append(new_col)
    for obj in collection.objects:
        copy=obj.copy();copy.name='READY | '+obj.name
        new_col.objects.link(copy);weapon_map[obj]=copy
for old,new in weapon_map.items():
    if old.parent in weapon_map:new.parent=weapon_map[old.parent]
    new.matrix_parent_inverse=old.matrix_parent_inverse.copy();new.matrix_basis=old.matrix_basis.copy()
ready_root=weapon_map[weapon_root]
ready_root.parent=bpy.data.objects['READY | TYPE_E01_MASTER_ROOT'];ready_root.matrix_parent_inverse=I
primary_world=Vector((-.02,-.43,2.18))
ready_R=ready_rotation@flip
ready_root.matrix_world=T(primary_world)@ready_R.to_4x4()@Matrix.Scale(scale,4)@T(-primary)
bpy.context.window.scene=ready;bpy.context.view_layer.update()
support_world=ready_root.matrix_world@support

# Fit both hands to the source rifle's supplied sockets, preserving limb lengths.
def solve(start,end,a0,b0,c0,pole):
    a=(b0-a0).length;b=(c0-b0).length;direction=end-start;d=direction.length
    assert abs(a-b)+.00001<d<a+b-.00001,('target outside arm reach',d,a+b)
    direction.normalize();along=(a*a-b*b+d*d)/(2*d)
    perp=Vector(pole);perp=(perp-direction*direction.dot(perp)).normalized()
    elbow=start+direction*along+perp*math.sqrt(a*a-along*along)
    return elbow,(b0-a0).rotation_difference(elbow-start).to_matrix(),(c0-b0).rotation_difference(end-elbow).to_matrix()
def move_root(name,position,rotation):
    bpy.data.objects['READY | '+name].matrix_world=T(position)@rotation.to_4x4()
contacts={'R':Vector((-.008,-.042,-.149)),'L':Vector((.006,.080,-.146))}
fit_report={}
for side,sign,target in (('R',-1,primary_world),('L',1,support_world)):
    rotation=ready_hands[side]
    wrist=target-rotation@contacts[side]
    names=['E01_'+side+'_'+part for part in ('UPPER_ARM','FOREARM','HAND')]
    a0,b0,c0=[rest[n].translation for n in names]
    elbow,upper_R,fore_R=solve(shoulders[side],wrist,a0,b0,c0,(sign*.60,.10,-.85))
    move_root(names[0],shoulders[side],upper_R);move_root(names[1],elbow,fore_R);move_root(names[2],wrist,rotation)
    shell_R=chest_rotation.slerp(upper_R.to_quaternion(),.26).to_matrix()
    move_root('E01_'+side+'_SHOULDER',shell_positions[side],shell_R)
    fit_report[side]={'contact_world':list(target),'contact_relative_to_wrist':list(contacts[side]),'wrist':list(wrist),'elbow':list(elbow),'arm_length':(b0-a0).length+(c0-b0).length}
bpy.context.view_layer.update()

# Keep the approved body untouched; the new rifle carries its own material palette.
for name in ('TYPE_E01_MASTER_ROOT','READY | TYPE_E01_MASTER_ROOT'):
    obj=bpy.data.objects[name]
    obj['review_status']='Stage 03 body approved by user; completed TYPE-01 rifle integration for review'
    obj['equipped_weapon']='TYPE-01 LASER RIFLE / external editable master'
    obj['weapon_source_sha256']=source_hash
    obj['storage']=str(OUT)
for scene in (neutral,ready):
    scene.use_fake_user=True
    scene.render.filepath=str(OUT/'renders')+'/'
    scene['equipped_weapon']='TYPE-01 LASER RIFLE'
    scene['source_weapon_master']=str(SOURCE)
    scene['body_review']='Stage 03 accepted by user before weapon replacement'
    scene.cycles.samples=32;scene.cycles.use_denoising=True
neutral.camera=bpy.data.objects['E01_3Q'];ready.camera=bpy.data.objects['E01_READY_3Q']
for txt in list(bpy.data.texts):
    if txt.name.startswith('READ ME -'):bpy.data.texts.remove(txt)
text=bpy.data.texts.new('READ ME - E01 WITH TYPE01 LASER RIFLE')
text.write('TYPE E-01 / TYPE-01 LASER RIFLE INTEGRATION\n\nThe stage 03 white infantry body was accepted by the user. The prior rifle has been replaced by the completed TYPE-01 master.\n\n01 NEUTRAL: lowered carry pose.\n02 TYPE01 READY: two-hand aimed pose.\n\nWeapon root scale: 1.85 uniform. Both hands use the source rifle grip sockets. All 1148 source rifle mesh parts, editable bevels, 7 subassemblies and source materials are retained.\nThe two scenes share geometry; transforms can be edited independently. Original mech and rifle reference images remain packed.\n\nSource: '+str(SOURCE)+'\nSHA256: '+source_hash+'\n\nThis is the editable modeling assembly and pose preview. No game integration or crowd LOD optimization was performed.\n')
bpy.context.window.scene=ready;bpy.context.view_layer.update()
for obj in bpy.context.selected_objects:obj.select_set(False)
ready_root.select_set(True);bpy.context.view_layer.objects.active=ready_root
FILE=OUT/'TYPE_E01_TYPE01_RIFLE_MASTER.blend'
bpy.ops.wm.save_as_mainfile(filepath=str(FILE),compress=True)
assert hashlib.sha256(SOURCE.read_bytes()).hexdigest()==source_hash,'Source weapon changed during integration'
assert hashlib.sha256(BASE.read_bytes()).hexdigest()==base_hash,'Approved mech source changed during integration'
report={'model':str(FILE),'base':str(BASE),'base_sha256':base_hash,'source_weapon':str(SOURCE),'source_sha256':source_hash,'source_unchanged':True,'body_geometry':body_fingerprint,'weapon_mesh_parts':1148,'weapon_scale':scale,'weapon_length_m':.8925527930259705*scale,'removed_prior_weapon_objects':len(removed),'contacts':fit_report,'source_sockets_local':{'primary':list(primary),'support':list(support),'muzzle':list(muzzle)},'scenes':[neutral.name,ready.name]}
(OUT/'integration_report.json').write_text(json.dumps(report,indent=2),encoding='utf8')
print('TYPE01_INTEGRATED '+str(FILE),flush=True)
if '--preview' in sys.argv:
    prefs=bpy.context.preferences.addons['cycles'].preferences
    try:
        prefs.compute_device_type='OPTIX';prefs.get_devices()
        for device in prefs.devices:device.use=device.type=='OPTIX'
        for scene in (neutral,ready):scene.cycles.device='GPU'
    except Exception:
        for scene in (neutral,ready):scene.cycles.device='CPU'
    for scene,shot,cam in ((neutral,'NEUTRAL_3Q','E01_3Q'),(ready,'READY_3Q','E01_READY_3Q'),(ready,'READY_HANDS','E01_READY_HANDS')):
        bpy.context.window.scene=scene;scene.camera=bpy.data.objects[cam]
        scene.render.resolution_x=1050;scene.render.resolution_y=1050 if shot=='READY_HANDS' else 1400
        scene.render.filepath=str(OUT/'renders'/(shot+'.png'))
        bpy.ops.render.render(write_still=True);print('INTEGRATION_RENDER '+shot,flush=True)
