"""Second art pass on the saved sector: damage, armor silhouette and ground story."""
import ast
import bpy
import math
import random
import json
import shutil
from pathlib import Path
from mathutils import Vector, noise

OUT=Path(__file__).resolve().parent
LOG=OUT/'polish_progress.log'
LOG.write_text('POLISH_START\n')
scene=bpy.data.scenes['MARE-07 | Abandoned Lunar Service Yard']
bpy.context.window.scene=scene
random.seed(742)
# Reuse only geometry helper definitions; do not execute the scene builder again.
tree=ast.parse((OUT/'build_lunar_base.py').read_text())
exec(compile(ast.Module(body=[n for n in tree.body if isinstance(n,ast.FunctionDef)],type_ignores=[]),'lunar_geometry_helpers','exec'))
names={'navy':'worn naval blue armor','blue':'desaturated blue armor','ivory':'dusty ivory ceramic',
       'steel':'exposed gunmetal','dark':'recess and rubber','orange':'faded ochre safety paint',
       'white':'worn stencil','dust':'lunar regolith','rock':'lunar basalt','tracks':'compressed regolith',
       'glass':'opaque dead cockpit glazing','solar':'photovoltaic cells','cyan':'emergency cyan','amber':'backup amber'}
M={k:bpy.data.materials['LUNA_'+v] for k,v in names.items()}
COL=None
(OUT/'iterations').mkdir(exist_ok=True)
shutil.copy2(OUT/'MARE07_LUNAR_BASE_MASTER.blend',OUT/'iterations/01_INITIAL_SCENE.blend')
shutil.copy2(OUT/'renders/00_PREVIEW.png',OUT/'iterations/01_INITIAL_RENDER.png')

# Keep material variation broad and quiet; geometric wear supplies the fine damage.
for key in ('navy','blue','ivory','orange','steel','white','solar'):
    mat=M[key]
    for n in mat.node_tree.nodes:
        if n.type=='VALTORGB':
            c=mat.diffuse_color[:3]
            n.color_ramp.elements[0].color=(*(v*.74 for v in c),1)
            n.color_ramp.elements[1].color=(*(v*1.04+.009 for v in c),1)
        if n.type=='BUMP':n.inputs['Strength'].default_value=.12;n.inputs['Distance'].default_value=.018
for key in ('dust','rock'):
    for n in M[key].node_tree.nodes:
        if n.type=='TEX_NOISE':
            if n.inputs['Scale'].default_value<10:n.inputs['Scale'].default_value=.4 if key=='rock' else .14
        if n.type=='BUMP':n.inputs['Strength'].default_value=.23;n.inputs['Distance'].default_value=.025
        if n.type=='VALTORGB' and key=='dust':
            n.color_ramp.elements[0].color=(.145,.16,.18,1)
            n.color_ramp.elements[1].color=(.29,.30,.30,1)
log('MATERIAL_PASS_OK')

collection('11 | Breached roof, buckled armor and exposed trusses')
# A physical irregular void through the existing roof, with internal structure beneath.
cx,cy=5.0,18.95
outline=[]
for i in range(12):
    a=i*math.tau/12
    rr=[2.35,1.96,2.12,2.5,2.17,2.29,1.92,2.4,2.3,1.95,2.35,2.15][i]
    outline.append((cx+math.cos(a)*rr,cy+math.sin(a)*rr))
vv=[(x,y,z) for z in (4.2,9) for x,y in outline]
ff=[tuple(reversed(range(12))),tuple(range(12,24))]+[(i,(i+1)%12,(i+1)%12+12,i+12) for i in range(12)]
cutter=mesh('Temporary physical breach cutter',vv,ff,None)
for name in ('Roof parapet','Dusty flat roof','Hangar top header','Right armored wing'):
    ob=bpy.data.objects.get(name)
    mod=ob.modifiers.new('Impact breach','BOOLEAN');mod.operation='DIFFERENCE';mod.solver='EXACT';mod.object=cutter
    bpy.context.view_layer.objects.active=ob
    bpy.ops.object.modifier_apply(modifier=mod.name)
bpy.data.objects.remove(cutter,do_unlink=True)
# Move the right roof radiator off its mount; individual blades stay with its wreck.
roof_objs=[]
for ob in list(scene.objects):
    if ob.name.startswith(('Roof vent body','Vent blade')) and ob.parent and ob.location.x>3:
        mw=ob.matrix_world.copy();ob.parent=None;ob.matrix_world=mw;roof_objs.append(ob)
for ob in roof_objs:
    ob.location += Vector((2.75,.75,-.2))
    ob.rotation_euler.y += .20
for i in range(3):
    y=17.2+i*1.15
    bar('Exposed roof I-beam',(2.7,y,5.7),(7.3,y,5.7),.16,'steel')
    for j in range(3):
        x=3.15+j*1.35
        bar('Exposed roof diagonal',(x,y,5.7),(x+.65,y+.65,5.4),.07,'steel')
box('Dark interior under breach',(5.1,18.7,4.26),(3.3,3,.07),'dark',.02)
for i in (0,2,4,7,9):
    a=i*math.tau/12
    x,y=outline[i]
    flap=box('Peeled roof armor',(x,y,6.99),(.95,.72,.075),'blue',.023,rot=(.22*math.sin(a),.36*math.cos(a),a))
    bar('Torn bright armor edge',(x-.32*math.sin(a),y+.32*math.cos(a),7.13),(x+.32*math.sin(a),y-.32*math.cos(a),7.13),.028,'ivory')
curve('Hanging roof wiring',[(6.2,17.7,6.8),(6.05,18.1,5.4),(5.4,18.2,4.7)],.038,'dark')
curve('Bent cooling line',[(4.8,20.8,6.95),(3.9,20.4,6.7),(4.1,19.6,7.5)],.10,'steel')
# Large angular armor cheek plates match the protagonist's panel language.
for side in (-1,1):
    x=side*8.0
    vs=[(x-side*.7,14.0,.43),(x+side*.62,14.0,.43),(x+side*.82,14.25,3.7),
        (x+side*.18,14.03,5.61),(x-side*.75,13.86,5.10),
        (x-side*.7,14.34,.43),(x+side*.62,14.34,.43),(x+side*.82,14.59,3.7),
        (x+side*.18,14.37,5.61),(x-side*.75,14.2,5.10)]
    mesh('Chamfered exterior armor cheek',vs,[(0,1,2,3,4),(5,9,8,7,6),(0,5,6,1),(1,6,7,2),(2,7,8,3),(3,8,9,4),(4,9,5,0)],'navy',.06)
    bar('Thin ivory armor reveal',(x-side*.47,13.82,1.45),(x-side*.47,13.82,4.7),.065,'ivory')
    box('Cheek hazard index',(x+side*.16,13.87,2.5),(.23,.036,.91),'orange',.014)
# Faint irregular scoring radiates from a strike on the front-right fascia.
for i in range(16):
    x=5.95+random.uniform(-1.25,1.15);z=4.7+random.uniform(-.7,.62)
    box('Shrapnel scoring',(x,14.085,z),(random.uniform(.06,.34),.018,random.uniform(.015,.052)),'steel',0,rot=(0,random.uniform(-.55,.55),0))
log('PHYSICAL_ROOF_BREACH_OK')

collection('12 | Pressure-lock annex and base infrastructure')
before=begin()
box('Airlock foundation',(0,0,.15),(4.7,6.2,.30),'steel',.09)
box('Airlock lower body',(0,0,1.35),(4.0,5.5,2.35),'blue',.16)
box('Airlock upper armored body',(0,0,3.03),(3.6,5.5,1.5),'ivory',.20)
box('Airlock sloped roof',(0,0,3.85),(3.75,5.66,.28),'navy',.11)
cyl('Octagonal pressure seal',(0,-2.88,2.03),1.46,.38,'steel',(0,1,0),8,.06)
cyl('Pressure door',(0,-3.105,2.03),1.25,.10,'blue',(0,1,0),8,.045)
cyl('Recessed porthole',(0,-3.169,2.43),.34,.045,'dark',(0,1,0),12,.02)
cyl('Porthole glazing',(0,-3.199,2.43),.26,.015,'glass',(0,1,0),12,.006)
bar('Locking bar',(-.71,-3.194,1.61),(.71,-3.194,1.61),.085,'ivory')
cyl('Pressure wheel',(0,-3.255,1.61),.25,.08,'orange',(0,1,0),16)
label('Airlock label','LOCK / 02',(0,-2.87,3.42),.28,'navy')
for x in (-1.65,1.65):
    box('Airlock armor rib',(x,0,2.1),(.2,5.78,2.8),'steel',.03)
    box('Airlock lateral panel',(x*1.2,0,2.55),(.08,2.9,.84),'navy',.04)
    for y in (-1,0,1):bolt((x*1.24,y,2.55),(1,0,0),.055)
box('Top airlock radiator',(0,.2,4.02),(2.6,3.8,.10),'steel',.04)
for j in range(12):box('Airlock heat fin',(0,-1.4+j*.27,4.12),(2.4,.085,.13),'ivory',.012)
group('PRESSURE LOCK | quiet service annex',added(before),(-12.9,19.4,0),0)
curve('Raised insulated transfer conduit',[(-10.7,20.2,2.5),(-9.7,20.2,2.5),(-9.7,19,2.5),(-8.1,19,2.5)],.36,'ivory')
for x in (-10.7,-10.3,-9.9):cyl('Conduit dark collar',(x,20.2,2.5),.40,.13,'steel',(1,0,0),16)
for i in range(9):
    y=10.4+i*.88
    box('Dust-filled utility walkway',(-12.8,y,.045),(2.2,.81,.13),'steel',.02)
    for j in range(6):box('Walkway drainage slit',(-13.65+j*.34,y,.116),(.05,.63,.018),'dark',.001)
# Equipment plates, recessed service panels and an external status terminal.
for x,y in [(8.79,18),(8.79,20.6)]:
    box('Hangar exterior equipment tray',(x,y,2.6),(.12,1.8,2.4),'dark',.035)
    box('Hangar removable side plate',(x+.09,y,2.6),(.10,1.6,2.1),'blue',.05)
    for yy in (y-.6,y+.6):
        for z in (1.8,3.4):bolt((x+.16,yy,z),(1,0,0),.07)
box('Broken control pedestal',(-5.7,12.1,.8),(.64,.56,1.6),'steel',.07,rot=(0,.13,-.15))
box('Control terminal hood',(-5.7,12.05,1.62),(.91,.68,.20),'ivory',.05,rot=(.25,.13,-.15))
box('Dead terminal screen',(-5.7,11.94,1.71),(.61,.41,.025),'glass',.01,rot=(.25,.13,-.15))
log('ANNEX_AND_INFRASTRUCTURE_OK')

collection('13 | Collapsed service truss and impact wreckage')
before=begin()
for x in (-.45,.45):
    for z in (.1,.92):bar('Collapsed cable bridge chord',(x,-4,z),(x,4,z),.13,'steel')
for i in range(8):
    y=-3.8+i*1.05
    for x in (-.45,.45):
        bar('Buckled lattice diagonal',(x,y,.1),(x,y+.95,.92),.08,'ivory')
        bar('Broken bridge cross tie',(x,y,.12),(-x,y,.12),.09,'steel')
    if i not in (3,6):box('Dislodged overhead cable cover',(0,y,.99),(1.15,.88,.045),'blue',.012,rot=(random.uniform(-.17,.17),random.uniform(-.2,.2),.04))
group('COLLAPSED CABLE BRIDGE',added(before),(12.6,-18,.04),math.radians(-52))
for i in range(3):
    curve('Severed cable in fallen bridge',[(9.1,-20+i*.17,.08),(11,-18.5+i*.17,.25),(13.2,-17.1+i*.17,.19),(15.6,-17.4+i*.17,.08)],.035,'dark')
box('Broken gantry end plate',(9.4,-21.1,.17),(1.3,1.1,.14),'orange',.04,rot=(.17,.1,.2))
for x,y in [(-22,-4.7),(-19,-4.0)]:
    cyl('Abandoned pressure bottle',(x,y,.30),.3,1.4,'ivory',(1,.3,.1),16,.06)
    cyl('Pressure bottle base',(x-.6,y-.18,.24),.31,.12,'navy',(1,.3,.1),16)
curve('Discarded pneumatic hose',[(-20.5,-5,.09),(-19.4,-6.1,.06),(-18.3,-5.9,.06),(-18.9,-4.7,.06),(-20,-4.8,.06)],.032,'orange')

# A readable impact bowl and raised lip in the front-right regolith.
ground=bpy.data.objects['Sculpted regolith with impact basins']
for v in ground.data.vertices:
    t=math.hypot(v.co.x-22,v.co.y+19)/4.2
    if t<1.5:
        v.co.z += -1.15*max(0,1-t*t)**2+.61*math.exp(-((t-.94)/.145)**2)
ground.data.update()
for i in range(18):
    a=random.random()*math.tau;r=random.uniform(4.2,5.4)
    x,y=22+math.cos(a)*r,-19+math.sin(a)*r
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1,radius=1,location=(x,y,.04))
    ob=finish(bpy.context.object,'Fresh crater lip ejecta','rock')
    sz=random.uniform(.18,.57);ob.scale=(sz,sz*.7,sz*.47);ob.rotation_euler.z=a
for i,(x,y,rx,ry,rz) in enumerate([(-15,-10.4,1.9,1.1,.33),(-7.5,13.7,2.2,.6,.38),(6.2,13.9,1.8,.62,.29),
    (-12.9,16.5,1.9,.7,.22),(14,-7,1.8,.8,.22),(12.4,-17.6,2.5,.7,.22),(-20.8,11.6,1.4,.8,.27)]):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=20,ring_count=10,radius=1,location=(x,y,-.08))
    ob=finish(bpy.context.object,'Windless dust accumulation %02d'%i,'dust');ob.scale=(rx,ry,rz)
    for f in ob.data.polygons:f.use_smooth=True

collection('14 | Perimeter utility cabling and low retaining remnants')
for side in (-1,1):
    for j in range(5):
        y=-9+j*6.2
        x=side*(25.4+random.uniform(-.6,.6))
        if side==1 and j in (1,2):continue
        box('Low perimeter footing',(x,y,.16),(1.1,3.8,.3),'steel',.05)
        box('Broken retaining block',(x,y,.6),(.65,3.5,.95),'blue',.11,rot=(0,random.uniform(-.06,.06),random.uniform(-.06,.06)))
        box('Retaining block ivory cap',(x,y,1.1),(.74,3.58,.12),'ivory',.04)
        for yy in (-1,1):box('Retaining safety tab',(x-side*.345,y+yy,.81),(.025,.23,.32),'orange',.009)
curve('Peripheral trunk cable',[(-24,1,.05),(-23,6,.05),(-23,13,.06),(-17,24,.07),(7,24,.08),(22,20,.07)],.075,'dark')
# Retired pole is deliberately leaning, with lamp head broken off beside it.
bar('Bent lighting pole',(24,-7,.02),(23.5,-7.2,3.4),.15,'steel')
bar('Bent lighting pole tip',(23.5,-7.2,3.4),(22.4,-7.25,4.2),.13,'steel')
box('Fallen light housing',(23,-8.2,.12),(1.1,.54,.22),'ivory',.04,rot=(.08,.2,.3))
log('ABANDONMENT_STORY_OK')

# A closer establishing frame and a subtle colder lunar fill.
cam=bpy.data.objects['01 | Establishing view']
cam.data.ortho_scale=65
cam.rotation_euler=(Vector((0,3.0,1))-cam.location).to_track_quat('-Z','Y').to_euler()
scene.camera=cam
scene.cycles.samples=48
bpy.context.view_layer.update()
# Remove appended unused image-guide empties from the reference collection only.
reference=bpy.data.collections['90 | Existing VALKYR - removable scale reference']
for ob in list(reference.objects):
    if ob.type=='EMPTY' and ob.empty_display_type=='IMAGE':
        bpy.data.objects.remove(ob,do_unlink=True)
scene['art_pass']='02: physical roof breach, annex, angular armor, collapsed truss, craters, dust, utility dressing.'
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'MARE07_LUNAR_BASE_MASTER.blend'))
manifest=json.loads((OUT/'scene_manifest.json').read_text())
manifest['objects']=len(scene.objects)
manifest['collections']={c.name:len(c.objects) for c in scene.collection.children}
manifest['art_pass']=scene['art_pass']
(OUT/'scene_manifest.json').write_text(json.dumps(manifest,indent=2,ensure_ascii=False))
log('POLISH_SAVED_OK')
