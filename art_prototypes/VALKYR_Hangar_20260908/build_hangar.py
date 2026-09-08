"""Independent pre-sortie presentation environment. No Unity/source-model edits."""
import ast
import bpy
import math
import random
import json
import hashlib
from pathlib import Path
from mathutils import Vector

OUT=Path(__file__).resolve().parent
PROJECT=OUT.parent.parent
OUT.mkdir(parents=True,exist_ok=True)
(OUT/'renders').mkdir(exist_ok=True)
LOG=OUT/'build.log';LOG.write_text('START\n')
helpers=OUT/'geometry_helpers.py'
if not helpers.exists():
    src=(OUT.parent/'LunarBase_Sector01_20260908/build_lunar_base.py').read_text()
    tree=ast.parse(src)
    helpers.write_text('\n\n'.join(ast.get_source_segment(src,n) for n in tree.body if isinstance(n,ast.FunctionDef)))
exec(compile(helpers.read_text(),str(helpers),'exec'))
scene=bpy.data.scenes.new('VALKYR | Pre-sortie Hangar')
bpy.context.window.scene=scene
scene.unit_settings.system='METRIC'
scene['purpose']='Unarmed pre-sortie inspection. Unity loadout integration lives in Assets/Resources/Hangar.'
random.seed(901)
COL=None
M={
 'navy':material('Hangar deep navy',(.045,.069,.105),.65,.45,False),
 'blue':material('Hangar blue gray',(.12,.19,.28),.55,.46,False),
 'ivory':material('Hangar ivory enamel',(.51,.57,.59),.35,.44,False),
 'steel':material('Hangar brushed metal',(.14,.17,.2),.78,.32,False),
 'dark':material('Hangar black recess',(.014,.021,.03),.25,.64,False),
 'orange':material('Hangar safety brass',(.72,.40,.12),.48,.46,False),
 'white':material('Hangar stencils',(.69,.75,.77),.12,.62,False),
 'glass':material('Hangar smoked glass',(.035,.082,.105),.7,.23,False),
 'cyan':material('Hangar cyan status',(.10,.62,.80),.2,.28,False,3.0),
 'amber':material('Hangar amber status',(.9,.31,.055),.2,.35,False,2.4),
 'light':material('Hangar neutral inspection light',(.79,.87,1),.1,.3,False,4),
}
# Broad, restrained variation on large surfaces, without dirtying the protagonist.
floor=material('Hangar worn graphite floor',(.072,.093,.117),.62,.38,True)
for n in floor.node_tree.nodes:
    if n.type=='VALTORGB':
        n.color_ramp.elements[0].color=(.047,.064,.085,1)
        n.color_ramp.elements[1].color=(.089,.109,.132,1)
    if n.type=='BUMP':n.inputs['Strength'].default_value=.1;n.inputs['Distance'].default_value=.006
M['floor']=floor

collection('01 | Hangar shell - open front for inspection')
box('Structural floor',(0,0,-.15),(17,18,.3),'dark',.035)
for x in range(-8,9,2):
    for y in range(-8,9,2):box('Floor plate',(x,y,.015),(1.97,1.97,.06),'floor',.009)
box('Rear acoustic wall',(0,7.2,3.5),(16.8,.35,7),'navy',.08)
for side in (-1,1):
    box('Side wall',(side*8.3,.3,3.1),(.32,14.2,6.2),'navy',.07)
    for y in (-5,-1,3,6.6):
        box('Deep wall structural rib',(side*7.98,y,3.1),(.46,.56,6.2),'steel',.045)
        box('Wall rib pale insert',(side*7.70,y,3.2),(.07,.25,4.2),'ivory',.018)
        box('Side wall running light',(side*7.66,y,3.2),(.035,.09,3.0),'light',.006)
for x in (-7.2,-4.8,-2.4,0,2.4,4.8,7.2):
    box('Rear wall segmented panel',(x,6.96,3.1),(2.28,.15,5.95),'blue',.035)
    box('Rear wall lower recess',(x,6.855,1.1),(1.7,.09,1.75),'dark',.022)
    for j in range(8):box('Wall cooling louvre',(x,6.787,.48+j*.16),(1.5,.07,.062),'steel',.01)
box('Large rear identity plaque',(0,6.81,5.76),(5.5,.15,1.15),'dark',.04)
label('Bay identity','BAY  07',(0,6.70,5.83),.74,'white')
label('Bay subtitle','VALKYR  /  SORTIE PREPARATION',(0,6.7,5.37),.16,'cyan')
for side in (-1,1):
    box('Overhead rail',(side*5.8,1.8,6.0),(.38,12,.38),'steel',.05)
    box('Ceiling inspection strip',(side*4.7,.8,5.86),(.23,8,.045),'light',.01)
for y in (-3,2,6):
    box('Open roof crossbeam',(0,y,6.3),(16.5,.4,.46),'navy',.055)
    for x in (-6,-3,0,3,6):bar('Roof diagonal stiffener',(x,y,6.48),(x+2.5,y,6.85),.09,'steel')
log('SHELL_OK')

collection('02 | Central turntable and maintenance spine')
cyl('Turntable foundation',(0,0,.10),3.10,.2,'dark',vertices=64,bevel=.035)
cyl('Turntable machined rim',(0,0,.24),3.0,.16,'steel',vertices=64,bevel=.025)
cyl('Turntable surface',(0,0,.335),2.82,.055,'floor',vertices=64,bevel=.018)
for i in range(16):
    a=i*math.tau/16
    ring('Turntable cyan calibration',(0,0),2.92,.028,'cyan',.331,a+.04,a+.22,7)
    for j in range(3):
        q=a+j*.028
        box('Turntable angle graduation',(math.cos(q)*2.64,math.sin(q)*2.64,.368),(.18 if j==0 else .08,.014,.004),'white',0,rot=(0,0,q))
    bolt((math.cos(a)*2.96,math.sin(a)*2.96,.339),(0,0,1),.042)
ring('Turntable inner scoring',(0,0),1.65,.024,'steel',.365,steps=96)
for x in (-.43,.43):
    box('Foot alignment marking',(x,-.08,.367),(.56,1.0,.005),'blue',0)
    for y in (-.62,.54):box('Foot stop marking',(x,y,.37),(.64,.06,.006),'orange',0)
label('Turntable floor stencil','07 / VALKYR',(0,-2.11,.37),.31,'white',(0,0,0))
for side in (-1,1):
    box('Rear support footing',(side*2.9,2.9,.24),(1.15,1.55,.48),'steel',.07)
    box('Rear maintenance spine',(side*2.9,3.15,2.8),(.66,.63,5.3),'navy',.085)
    box('Spine inset inspection light',(side*2.9,2.808,3.02),(.24,.055,3.9),'light',.018)
    box('Spine protective frame',(side*3.22,3.14,2.65),(.18,.9,4.5),'ivory',.04)
    for z in (.72,1.35,2,2.65,3.3,3.95,4.6):box('Spine ladder bracket',(side*2.56,3.2,z),(.16,.85,.095),'steel',.01)
    bar('Spine sloped foot brace',(side*2.9,2.38,.35),(side*2.9,3.13,1.8),.22,'steel')
box('Overhead service crosshead',(0,3.13,5.1),(6.6,.83,.47),'blue',.09)
box('Crosshead pale facing',(0,2.692,5.1),(5.8,.06,.18),'ivory',.015)
label('Crosshead marking','VALKYR  //  SERVICE DOCK',(0,2.655,5.1),.20,'navy')
box('Central umbilical carriage',(0,3.02,4.74),(1.13,.82,.34),'steel',.045)
curve('Retracted umbilical loom',[(0,3.03,4.58),(.25,3.05,3.94),(.2,3.15,3.27)],.065,'dark')
for x in (-.14,.14):curve('Twin umbilical line',[(x,3.18,4.64),(x+.22,3.22,4.0),(x+.18,3.29,3.5)],.03,'orange')
for side in (-1,1):
    for y in (-1.4,0,1.4):
        box('Flush cable access panel',(side*4.05,y,.079),(1.2,1.3,.045),'dark',.015)
        for i in range(9):box('Cable trench grating',(side*4.05-.5+i*.125,y,.11),(.047,1.2,.028),'steel',.004)
log('DOCK_OK')

collection('03 | Articulated service arms - retracted behind hero')
def arm(side):
    a=Vector((side*3.55,2.05,1.4))
    b=Vector((side*2.64,2.0,2.84))
    c=Vector((side*1.63,1.93,2.65))
    cyl('Robot arm base',(side*3.55,2.05,.22),.55,.4,'steel',vertices=32)
    cyl('Arm pedestal',(side*3.55,2.05,.81),.32,.8,'blue',vertices=16)
    for p in (a,b,c):
        cyl('Actuator rotary housing',p,.22,.44,'steel',(0,1,0),24)
        cyl('Amber actuator end cap',p+Vector((0,-.25,0)),.15,.045,'orange',(0,1,0),16)
    for start,end in ((a,b),(b,c)):
        mid=(start+end)/2
        o=box('Service arm chamfered link',mid,(.26,.30,(end-start).length-.18),'ivory',.065)
        o.rotation_euler=(end-start).to_track_quat('Z','Y').to_euler()
        beam('Arm external piston',start+Vector((0,-.25,.04)),end+Vector((0,-.25,-.04)),.055,'steel')
        curve('Actuator cable',[start+Vector((0,.23,.07)),mid+Vector((0,.36,.15)),end+Vector((0,.23,.06))],.028,'dark')
    tip=c+Vector((-side*.24,-.16,-.1))
    beam('Retracted inspection probe',c,tip,.078,'steel')
    box('Inspection probe head',tip,(.19,.19,.24),'blue',.04)
    box('Probe status diode',tip+Vector((0,-.105,.03)),(.09,.016,.045),'cyan',.003)
arm(-1);arm(1)
log('ARMS_OK')

collection('04 | Rear service catwalk and stairs')
box('Raised service catwalk',(0,4.85,2.22),(13.4,1.65,.19),'steel',.035)
for x in (-6,-4,-2,0,2,4,6):
    box('Catwalk support',(x,4.85,1.06),(.16,.26,2.12),'blue',.02)
    for y in (4.08,5.62):
        bar('Catwalk guardrail stanchion',(x,y,2.3),(x,y,3.12),.064,'steel')
for y in (4.08,5.62):
    bar('Catwalk upper safety rail',(-6.4,y,3.13),(6.4,y,3.13),.065,'orange')
    bar('Catwalk intermediate rail',(-6.4,y,2.74),(6.4,y,2.74),.04,'steel')
for i in range(22):box('Catwalk deck seam',(-6.25+i*.6,4.85,2.326),(.024,1.45,.007),'dark',0)
for side in (-1,1):
    for i in range(10):
        box('Stair tread',(side*6.45,3.8-i*.33,2.17-i*.21),(1.0,.35,.11),'steel',.012)
        box('Stair nosing',(side*6.45,3.61-i*.33,2.233-i*.21),(1.0,.028,.018),'orange',.002)
    for x in (side*6.45-.46,side*6.45+.46):
        bar('Stair handrail',(x,.55,1.0),(x,3.82,3.1),.055,'steel')
        for y,z in ((.65,1.0),(2,1.86),(3.7,3.0)):bar('Stair rail post',(x,y,z-.8),(x,y,z),.045,'steel')

collection('05 | Workbench, equipment racks and diagnostics')
def rack(p,yaw=0):
    before=begin()
    box('Rack backing',(0,0,1.35),(1.4,.20,2.7),'navy',.045)
    for x in (-.76,.76):box('Rack upright',(x,-.1,1.42),(.13,.5,2.83),'steel',.025)
    for z in (.16,1.2,2.6):box('Rack shelf',(0,-.31,z),(1.55,.66,.11),'ivory',.018)
    for x in (-.42,.42):
        cyl('Stored cylindrical actuator',(x,-.3,1.81),.19,.95,'blue',vertices=16)
        cyl('Actuator steel head',(x,-.3,2.31),.20,.14,'steel',vertices=16)
        box('Actuator identifier',(x,-.502,1.79),(.17,.014,.28),'orange',.005)
    crate('Rack power module',(-.46,-.32,.22),(.79,.58,.59),0,'blue')
    group('SPARE ACTUATOR RACK',added(before),p,yaw)
rack((-5.35,6.32,0));rack((5.3,6.32,0))
for x in (-5.25,5.25):
    box('Workbench cabinet',(x,2.1,.57),(1.85,.86,1.14),'blue',.045)
    box('Workbench top',(x,2.05,1.2),(2.02,1.03,.13),'steel',.035)
    for z in (.23,.48,.73,.98):
        box('Tool drawer',(x,1.651,z),(1.66,.06,.2),'ivory',.015)
        box('Drawer recessed pull',(x,1.609,z+.035),(.52,.017,.035),'dark',.005)
    for dx in (-.65,-.32,0,.32,.65):bar('Organized maintenance tool',(x+dx,1.8,1.283),(x+dx,2.17,1.283),.023,'steel')
    box('Workbench tasklight',(x,2.33,1.96),(1.65,.16,.09),'light',.018)
    for dx in (-.74,.74):bar('Tasklight upright',(x+dx,2.34,1.25),(x+dx,2.34,1.91),.04,'steel')
before=begin()
box('Diagnostic station base',(0,0,.1),(1.35,1.1,.2),'steel',.055)
box('Diagnostic pedestal',(0,.08,.78),(.58,.47,1.35),'blue',.065)
box('Diagnostic monitor frame',(0,0,1.54),(1.47,.14,.85),'steel',.055,rot=(math.radians(15),0,0))
box('Diagnostic screen',(0,-.092,1.555),(1.29,.018,.67),'dark',.015,rot=(math.radians(15),0,0))
label('Diagnostic title','VALKYR / LINKED',(-.53,-.105,1.76),.082,'cyan',align='LEFT')
for i,(txt,value) in enumerate([('CORE  /  NOMINAL',.88),('ACTUATORS  /  READY',.79),('HARDPOINTS  /  LOCK',.93)]):
    z=1.59-i*.135
    label('Diagnostic line',txt,(-.51,-.115,z),.053,'white',align='LEFT')
    box('Diagnostic progress bar',(.31,-.116,z),(.29*value,.014,.024),'cyan',.002)
group('PRE-SORTIE DIAGNOSTICS',added(before),(4.12,-1.15,0),math.radians(-21))
for side in (-1,1):
    curve('Floor power umbilical',[(side*3.35,2.2,.08),(side*3.65,1.0,.07),(side*4.4,.5,.07),(side*4.72,2.0,.27)],.042,'dark')
    crate('Labeled spare battery',(side*5.25,-1.4,.05),(1.05,.74,.8),side*.1,'blue')
for x in (-3.55,3.55):
    for i in range(8):box('Pedestrian exclusion marking',(x,-4.2+i*.29,.077),(.53,.13,.008),'orange',0,rot=(0,0,.5))
label('Floor safety lettering','AUTHORIZED SERVICE PERSONNEL',(0,-4.3,.08),.27,'white',(0,0,0))
log('SET_DRESSING_OK')

collection('90 | VALKYR - current model copy')
source=PROJECT/'art_prototypes/JishenTrial_Hero_Assembly_V3/body_mass_refinement_20260906/iteration_v3/game_update/VALKYR_V3_GAME.blend'
with bpy.data.libraries.load(str(source),link=False) as (src,dst):dst.objects=src.objects
objects=[o for o in dst.objects if o and o.type in ('MESH','EMPTY') and not (o.type=='EMPTY' and o.empty_display_type=='IMAGE')]
for ob in objects:put(ob)
root=bpy.data.objects.new('VALKYR_INSPECTION_ROOT',None);COL.objects.link(root)
for ob in objects:
    if ob.parent not in objects:
        world=ob.matrix_world.copy();ob.parent=root;ob.matrix_world=world
bpy.data.objects['Forearm.R'].rotation_euler=(0,0,0)
blade=bpy.data.objects.get('AntiShip_Blade_Display_Root')
if blade:
    for ob in [blade,*blade.children_recursive]:ob.hide_render=True;ob.hide_set(True)
bpy.context.view_layer.update()
# Center on the machine itself, excluding the long carried sword.
body=[o for o in objects if o.type=='MESH' and o.name.startswith('LOD0_') and 'Blade' not in o.name and not o.hide_render]
points=[o.matrix_world@Vector(c) for o in body for c in o.bound_box]
lo=Vector(tuple(min(v[i] for v in points) for i in range(3)))
hi=Vector(tuple(max(v[i] for v in points) for i in range(3)))
scale=3.5/(hi.z-lo.z)
root.scale=(scale,)*3
root.location=(-(lo.x+hi.x)/2*scale,-(lo.y+hi.y)/2*scale,.379-lo.z*scale)
for ob in objects:
    if ob.name.startswith('RAIKEN_Beam_') or 'Blade_Beam' in ob.name:ob.hide_render=True;ob.hide_set(True)
root['source']=str(source)
root['height_m']=3.5
root['display_state']='V3 armor, relaxed standing, unarmed until the player selects a weapon in Unity.'
log('HERO_OK')

collection('99 | Inspection lighting and cameras')
world=bpy.data.worlds.new('Hangar dark ambient');world.use_nodes=True;scene.world=world
world.node_tree.nodes['Background'].inputs['Color'].default_value=(.12,.16,.23,1)
world.node_tree.nodes['Background'].inputs['Strength'].default_value=.13
def area(name,p,target,energy,color,size,sy=None):
    d=bpy.data.lights.new(name,'AREA');d.energy=energy;d.color=color;d.shape='RECTANGLE';d.size=size;d.size_y=sy or size
    o=bpy.data.objects.new(name,d);COL.objects.link(o);o.location=p;o.rotation_euler=(Vector(target)-o.location).to_track_quat('-Z','Y').to_euler()
    return o
area('Neutral softbox for armor readability',(-3.8,-4.8,5.1),(0,0,2),950,(.84,.91,1),3.8,4.5)
area('Warm frontal balance',(3.8,-3.3,3.2),(0,0,2),570,(1,.87,.71),3,3.5)
area('Cool backpack rim',(1.8,3.7,4.8),(0,0,2.4),1250,(.36,.65,1),2.6,3)
area('Left white contour',(-3,1.9,4.1),(0,0,2.1),700,(.75,.9,1),2.1,3.2)
area('Ceiling service wash',(0,0,5.5),(0,1,0),450,(.74,.83,1),4,6)
area('Backdrop low intensity',(-.3,3.3,5),(0,7,3.4),340,(.39,.58,.78),4,2)
area('Side workbench light',(-5.3,1.9,3),(-5.3,2,0),90,(1,.72,.39),1,2)
def cam(name,p,target,lens):
    d=bpy.data.cameras.new(name);d.lens=lens;d.clip_start=.05;d.clip_end=200
    o=bpy.data.objects.new(name,d);COL.objects.link(o);o.location=p;o.rotation_euler=(Vector(target)-o.location).to_track_quat('-Z','Y').to_euler()
    return o
main=cam('01 | Pre-sortie hero - UI room at right',(4.8,-9.7,4.2),(1.0,0,2.0),51)
cam('02 | Complete maintenance bay',(10,-15,10),(0,1.2,2.0),38)
cam('03 | Head and chest inspection',(2.6,-4.7,3.7),(0,0,2.82),77)
cam('04 | Backpack and hardpoints',(-4.3,5.1,3.9),(0,0,2.4),60)
orbit=bpy.data.objects.new('ORBIT | 5 second inspection loop',None);COL.objects.link(orbit);orbit.location=(0,0,2.12)
orbitcam=cam('05 | 360 degree inspection',(0,-6.6,3.45),(0,0,2.12),43)
matrix=orbitcam.matrix_world.copy();orbitcam.parent=orbit;orbitcam.matrix_world=matrix
for frame,angle in [(1,0),(121,math.tau)]:
    orbit.rotation_euler.z=angle;orbit.keyframe_insert(data_path='rotation_euler',frame=frame)
if orbit.animation_data and orbit.animation_data.action:
    action=orbit.animation_data.action
    for layer in action.layers:
        for strip in layer.strips:
            for bag in strip.channelbags:
                for fc in bag.fcurves:
                    for k in fc.keyframe_points:k.interpolation='LINEAR'
scene.frame_start=1;scene.frame_end=120;scene.render.fps=24;scene.frame_set(1)
scene.camera=main
scene.render.engine='CYCLES';scene.cycles.device='CPU';scene.cycles.samples=48;scene.cycles.use_denoising=True;scene.cycles.max_bounces=6
scene.render.resolution_x=1600;scene.render.resolution_y=1000;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
scene.view_settings.view_transform='AgX';scene.view_settings.look='AgX - Medium High Contrast';scene.view_settings.exposure=-.25
scene.render.filepath=str(OUT/'renders/01_PRESORTIE_HERO.png')
for ob in scene.objects:
    if ob.type=='FONT':ob.visible_shadow=False
for screen in bpy.data.screens:
    for a in screen.areas:
        if a.type=='VIEW_3D':
            a.spaces.active.region_3d.view_perspective='CAMERA'
            a.spaces.active.region_3d.view_camera_zoom=12
            a.spaces.active.overlay.show_overlays=False
            a.spaces.active.shading.type='MATERIAL'
            a.spaces.active.shading.use_scene_world=True
            a.spaces.active.shading.use_scene_lights=True
bpy.ops.object.select_all(action='DESELECT')
scene['camera_note']='Camera 01 keeps room for right-side deployment UI; cameras 03/04 inspect armor. Camera 05 has a 120-frame orbit.'
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'VALKYR_HANGAR_MASTER.blend'))
(OUT/'manifest.json').write_text(json.dumps({'scene':scene.name,'objects':len(scene.objects),
    'collections':{c.name:len(c.objects) for c in scene.collection.children},
    'source_hero_sha256':hashlib.sha256(source.read_bytes()).hexdigest(),
    'source_hero':str(source),'height_m':3.5,'cameras':[o.name for o in scene.objects if o.type=='CAMERA'],
    'scope':'Independent Blender pre-sortie presentation scene. Unity code unchanged.'},indent=2,ensure_ascii=False))
log('SAVED_OK')
