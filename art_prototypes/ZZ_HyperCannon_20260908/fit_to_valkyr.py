import bpy,math,json,hashlib
from pathlib import Path
from mathutils import Vector,Matrix,Euler
ROOT=Path(__file__).resolve().parent
source=ROOT.parent/'JishenTrial_Hero_Assembly_V3/body_mass_refinement_20260906/iteration_v3/game_update/VALKYR_V3_GAME.blend'
source_hash=hashlib.sha256(source.read_bytes()).hexdigest()
assert Path(bpy.data.filepath).name=='HC09_CANNON_MASTER.blend', 'Open the weapon master in a separate MCP call before fitting.'
scene=bpy.context.scene;scene.name='VALKYR V3 | Full-height TYPE-08 cannon'
for o in list(bpy.data.objects):
    o.hide_render=True
    if o.name in bpy.context.view_layer.objects:o.hide_set(True)

col=bpy.data.collections.new('VALKYR | Original V3 body with right-hand cannon');scene.collection.children.link(col)
with bpy.data.libraries.load(str(source),link=False) as (a,b):b.objects=list(a.objects)
for o in b.objects:
    if o:col.objects.link(o);o.hide_set(False)
blade=bpy.data.objects['AntiShip_Blade_Display_Root']
for o in [blade,*blade.children_recursive]:o.hide_render=True;o.hide_set(True)
for o in col.objects:
    if o.type in {'LIGHT','CAMERA'}:o.hide_render=True;o.hide_set(True)
bpy.context.view_layer.update()
body=[o for o in col.objects if o.type=='MESH' and not o.hide_render]
height_objects=[o for o in body if all(k not in o.name for k in ('Blade','Cannon','Backpack'))]
pts=[o.matrix_world@Vector(v) for o in height_objects for v in o.bound_box]
height=max(v.z for v in pts)-min(v.z for v in pts)
hand=bpy.data.objects['Hand.R'];shoulder=bpy.data.objects['UpperArm.R'];elbow=bpy.data.objects['Forearm.R']
socket=bpy.data.objects['V3B_Sword_Grip_Socket']
offset=hand.matrix_world.inverted()@socket.matrix_world.translation
rest_rotation=hand.matrix_world.to_quaternion()

gun=bpy.data.objects.new('TYPE08_RIGHT_HAND_MOUNT',None);col.objects.link(gun)
gun.location=(-1.06,-.48,2.25)
gun.rotation_euler=(math.radians(8),0,math.radians(12))
mesh=bpy.data.objects['TYPE08_LOD0'].copy();mesh.data=mesh.data.copy();col.objects.link(mesh)
mesh.name='TYPE08_RIGHT_HAND_CANNON';mesh.parent=gun;mesh.matrix_basis=Matrix.Identity(4);mesh.hide_render=False;mesh.hide_set(False)
for name in ('Grip','Muzzle','Support'):
    src=bpy.data.objects[name+'_Export'];ob=bpy.data.objects.new('Equipped_'+name,None);col.objects.link(ob);ob.parent=gun;ob.location=src.location
bpy.context.view_layer.update()
target=gun.matrix_world.translation-rest_rotation@offset

def rotate_about(obj,pivot,rotation):
    obj.matrix_world=Matrix.Translation(pivot)@rotation.to_matrix().to_4x4()@Matrix.Translation(-pivot)@obj.matrix_world
    bpy.context.view_layer.update()
start=shoulder.matrix_world.translation.copy();ep=elbow.matrix_world.translation.copy();hp=hand.matrix_world.translation.copy()
a=(ep-start).length;b=(hp-ep).length;delta=target-start
distance=min(a+b-.001,max(abs(a-b)+.001,delta.length));axis=delta.normalized()
hint=Vector((-1.38,.02,2.01));bend=(hint-start)-axis*(hint-start).dot(axis);bend.normalize()
along=(a*a-b*b+distance*distance)/(2*distance)
elbow_target=start+axis*along+bend*math.sqrt(max(0,a*a-along*along))
rotate_about(shoulder,start,(ep-start).rotation_difference(elbow_target-start))
ep=elbow.matrix_world.translation.copy();hp=hand.matrix_world.translation.copy()
rotate_about(elbow,ep,(hp-ep).rotation_difference(target-ep))
location=hand.matrix_world.translation.copy()
hand.matrix_world=Matrix.LocRotScale(location,rest_rotation,hand.matrix_world.to_scale())
bpy.context.view_layer.update()
grip_error=(socket.matrix_world.translation-gun.matrix_world.translation).length
assert grip_error<.001,grip_error
mount_world=gun.matrix_world.copy();gun.parent=hand;gun.matrix_world=mount_world
bpy.context.view_layer.update()
gun['mount']='Right-hand primary grip';gun['grip_error_m']=grip_error
gun['body_height_m']=height;gun['cannon_length_m']=mesh.dimensions.y

studio=bpy.data.collections.new('Studio | Mounted weapon inspection');scene.collection.children.link(studio)
def put(ob):
    for c in list(ob.users_collection):c.objects.unlink(ob)
    studio.objects.link(ob);return ob
def mat(name,color,metal=0,rough=.45):
    m=bpy.data.materials.new(name);m.use_nodes=True;m.diffuse_color=(*color,1)
    n=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
    n.inputs['Base Color'].default_value=(*color,1);n.inputs['Metallic'].default_value=metal;n.inputs['Roughness'].default_value=rough;return m
floor_mat=mat('Mounted inspection floor',(.035,.044,.061),.25,.5)
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.095));ground=put(bpy.context.view_layer.objects.active);ground.name='Ground';ground.data.materials.append(floor_mat)
bpy.ops.mesh.primitive_cylinder_add(vertices=96,radius=2.22,depth=.085,location=(0,0,-.05))
pad=put(bpy.context.view_layer.objects.active);pad.name='Inspection platform';pad.data.materials.append(mat('Platform graphite',(.018,.027,.037),.55,.33))
bevel=pad.modifiers.new('Platform bevel','BEVEL');bevel.width=.028;bevel.segments=3
for p in pad.data.polygons:p.use_smooth=len(p.vertices)==4
def camera(name,loc,target,ortho):
    data=bpy.data.cameras.new(name);o=bpy.data.objects.new(name,data);studio.objects.link(o);o.location=loc
    o.rotation_euler=(Vector(target)-o.location).to_track_quat('-Z','Y').to_euler();data.type='ORTHO';data.ortho_scale=ortho;return o
camera('FIT_01_FULL_MECHA',(-7.5,-5.0,4.15),(-.40,-.10,1.66),5.35)
camera('FIT_02_GRIP_CLOSEUP',(-4.7,-4.9,3.7),(-.92,-.4,2.35),2.42)
camera('FIT_03_OPPOSITE_SIDE',(6,-5,3.7),(-.25,-.02,1.75),5.3)
camera('FIT_04_WEAPON_SIDE',(-8,-.4,3.2),(-.25,-.1,1.72),5.0)
for name,loc,energy,size,color in [('Mounted key',(-3,-4,6),1600,5,(.88,.94,1)),('Mounted fill',(3,-1,4),1250,4,(1,.9,.78)),('Mounted rim',(-2,4,5),2200,3,(.7,.84,1))]:
    data=bpy.data.lights.new(name,'AREA');data.energy=energy;data.shape='DISK';data.size=size;data.color=color
    o=bpy.data.objects.new(name,data);studio.objects.link(o);o.location=loc;o.rotation_euler=(Vector((-.2,0,1.8))-o.location).to_track_quat('-Z','Y').to_euler()
scene.camera=bpy.data.objects['FIT_01_FULL_MECHA'];scene.render.resolution_x=1600;scene.render.resolution_y=1350
scene.cycles.samples=64;scene.view_settings.exposure=-.8;scene.render.filepath=str(ROOT/'renders/05_VALKYR_CANNON_EQUIPPED.png')
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            area.spaces.active.region_3d.view_perspective='CAMERA'
            area.spaces.active.shading.type='MATERIAL'
            area.spaces.active.overlay.show_overlays=False
report={'source_hero':str(source),'source_sha256':source_hash,'model_revision':'VALKYR_Model_Revision_V3_c7297dd9','body_height_m':height,'cannon_length_m':mesh.dimensions.y,'length_to_body_ratio':mesh.dimensions.y/height,'right_grip_error_m':grip_error,'gun_parent':gun.parent.name,'gun_world_position':list(gun.matrix_world.translation),'gun_euler_degrees':[math.degrees(v) for v in gun.matrix_world.to_euler()],'right_hand_rest_rotation':list(rest_rotation),'source_hero_unchanged':source_hash==hashlib.sha256(source.read_bytes()).hexdigest()}
(ROOT/'work/fitting_report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'VALKYR_TYPE08_MASTER.blend'))
bpy.ops.render.render(write_still=True)
print('FITTED_CANNON',json.dumps(report),flush=True)
