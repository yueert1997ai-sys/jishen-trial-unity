"""MARE-07: authored Blender environment. Run in Blender's Python console.

Creates an independent scene and saves only inside this delivery directory.
The existing hero is appended as a scale reference; its source is never edited.
"""
import bpy
import math
import random
import json
import traceback
from pathlib import Path
from mathutils import Vector
from mathutils import noise

OUT = Path(__file__).resolve().parent
PROJECT = OUT.parent.parent
OUT.mkdir(parents=True, exist_ok=True)
(OUT / 'renders').mkdir(exist_ok=True)
random.seed(70908)
LOG = OUT / 'build_progress.log'
LOG.write_text('START\n')

def log(s):
    with LOG.open('a') as f:
        f.write(s + '\n')
    print(s, flush=True)

scene = bpy.data.scenes.new('MARE-07 | Abandoned Lunar Service Yard')
if bpy.context.window:
    bpy.context.window.scene = scene
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1.0
scene['design'] = 'Blue-grey armor / ivory structure / restrained amber & cyan. Dust, impact damage, abandonment.'
scene['scope'] = 'One Blender environment sector. Not integrated into Unity.'
scene['play_space'] = 'Central yard kept open; tall landmarks at perimeter. Unity collision and navigation not yet authored.'
COL = None

def collection(name):
    global COL
    COL = bpy.data.collections.new(name)
    scene.collection.children.link(COL)
    return COL

def put(obj):
    for c in list(obj.users_collection):
        c.objects.unlink(obj)
    COL.objects.link(obj)
    return obj

def material(name, color, metal=.3, rough=.65, weather=True, emission=0):
    m = bpy.data.materials.new('LUNA_' + name)
    m.diffuse_color = (*color, 1)
    m.use_nodes = True
    n = m.node_tree.nodes
    l = m.node_tree.links
    bs = n.get('Principled BSDF')
    bs.inputs['Base Color'].default_value = (*color, 1)
    bs.inputs['Metallic'].default_value = metal
    bs.inputs['Roughness'].default_value = rough
    if emission:
        bs.inputs['Emission Color'].default_value = (*color, 1)
        bs.inputs['Emission Strength'].default_value = emission
    if weather:
        tex = n.new('ShaderNodeTexNoise')
        tex.inputs['Scale'].default_value = 5.5
        tex.inputs['Detail'].default_value = 3
        coords = n.new('ShaderNodeTexCoord')
        l.new(coords.outputs['Object'], tex.inputs['Vector'])
        ramp = n.new('ShaderNodeValToRGB')
        ramp.color_ramp.elements[0].position = .24
        ramp.color_ramp.elements[0].color = (*(c * .46 for c in color), 1)
        ramp.color_ramp.elements[1].position = .78
        ramp.color_ramp.elements[1].color = (*(min(1, c * 1.12 + .025) for c in color), 1)
        l.new(tex.outputs['Fac'], ramp.inputs[0])
        l.new(ramp.outputs[0], bs.inputs['Base Color'])
        fine = n.new('ShaderNodeTexNoise')
        fine.inputs['Scale'].default_value = 95
        fine.inputs['Detail'].default_value = 2
        l.new(coords.outputs['Object'], fine.inputs['Vector'])
        bump = n.new('ShaderNodeBump')
        bump.inputs['Strength'].default_value = .21
        bump.inputs['Distance'].default_value = .035
        l.new(fine.outputs['Fac'], bump.inputs['Height'])
        l.new(bump.outputs[0], bs.inputs['Normal'])
    return m

M = {
    'navy': material('worn naval blue armor', (.12,.19,.29), .5),
    'blue': material('desaturated blue armor', (.25,.34,.43), .4),
    'ivory': material('dusty ivory ceramic', (.61,.64,.63), .25, .77),
    'steel': material('exposed gunmetal', (.13,.16,.18), .72,.46),
    'dark': material('recess and rubber', (.026,.034,.04), .16,.9),
    'orange': material('faded ochre safety paint', (.68,.35,.10), .28,.7),
    'white': material('worn stencil', (.72,.73,.67), .05,.85),
    'dust': material('lunar regolith', (.235,.248,.26), .03,.95),
    'rock': material('lunar basalt', (.115,.128,.146), .12,.94),
    'tracks': material('compressed regolith', (.13,.14,.15), .02,1,False),
    'glass': material('opaque dead cockpit glazing', (.025,.083,.106), .72,.24,False),
    'solar': material('photovoltaic cells', (.027,.066,.115), .67,.32,True),
    'cyan': material('emergency cyan', (.11,.69,.79), .2,.35,False,2.3),
    'amber': material('backup amber', (.96,.29,.055), .1,.4,False,2),
}

def finish(o, name, mat, bevel=0):
    o.name = name
    put(o)
    if mat:
        o.data.materials.append(M[mat] if isinstance(mat, str) else mat)
    if bevel:
        mod = o.modifiers.new('Machined edge bevel', 'BEVEL')
        mod.width = bevel
        mod.segments = 2
        mod.limit_method = 'ANGLE'
    return o

def box(name, p, s, mat='blue', bevel=.035, rot=(0,0,0)):
    x,y,z = [v/2 for v in s]
    verts = [(-x,-y,-z),(-x,-y,z),(-x,y,-z),(-x,y,z),(x,-y,-z),(x,-y,z),(x,y,-z),(x,y,z)]
    faces = [(0,4,6,2),(1,3,7,5),(0,1,5,4),(2,6,7,3),(0,2,3,1),(4,5,7,6)]
    me = bpy.data.meshes.new(name)
    me.from_pydata(verts,[],faces)
    o = bpy.data.objects.new(name,me)
    o.location = p
    o.rotation_euler = rot
    return finish(o,name,mat,bevel)

def mesh(name, verts, faces, mat, bevel=0):
    me = bpy.data.meshes.new(name)
    me.from_pydata(verts,[],faces)
    me.update()
    return finish(bpy.data.objects.new(name,me),name,mat,bevel)

def cyl(name,p,r,d,mat='steel',axis=(0,0,1),vertices=24,bevel=.025):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=r, depth=d, location=p)
    o=bpy.context.object
    o.rotation_euler=Vector(axis).to_track_quat('Z','Y').to_euler()
    return finish(o,name,mat,bevel)

def beam(name,a,b,r=.09,mat='steel'):
    a,b=Vector(a),Vector(b)
    return cyl(name,(a+b)/2,r,(b-a).length,mat,b-a,12,.009)

def bar(name,a,b,width=.15,mat='steel'):
    a,b=Vector(a),Vector(b)
    o=box(name,(a+b)/2,(width,width,(b-a).length),mat,.018)
    o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler()
    return o

def curve(name,points,r=.035,mat='dark',smooth=True):
    cu=bpy.data.curves.new(name,'CURVE')
    cu.dimensions='3D'
    cu.resolution_u=10
    cu.bevel_depth=r
    cu.bevel_resolution=2
    sp=cu.splines.new('BEZIER' if smooth else 'POLY')
    if smooth:
        sp.bezier_points.add(len(points)-1)
        for p,co in zip(sp.bezier_points,points):
            p.co=co
            p.handle_left_type='AUTO'
            p.handle_right_type='AUTO'
    else:
        sp.points.add(len(points)-1)
        for p,co in zip(sp.points,points):p.co=(*co,1)
    return finish(bpy.data.objects.new(name,cu),name,mat)

def label(name,body,p,size=.5,mat='white',rot=(math.pi/2,0,0),align='CENTER'):
    cu=bpy.data.curves.new(name,'FONT')
    cu.body=body
    cu.size=size
    cu.align_x=align
    cu.align_y='CENTER'
    cu.extrude=.0007
    cu.resolution_u=3
    o=finish(bpy.data.objects.new(name,cu),name,mat)
    o.location=p
    o.rotation_euler=rot
    return o

def ring(name,center,r,width,mat,z=.04,start=0,end=math.tau,steps=80):
    verts=[]
    for i in range(steps+1):
        a=start+(end-start)*i/steps
        for rr in (r-width/2,r+width/2):
            verts.append((center[0]+math.cos(a)*rr,center[1]+math.sin(a)*rr,z))
    return mesh(name,verts,[(2*i,2*i+1,2*i+3,2*i+2) for i in range(steps)],mat)

def bolt(p,axis=(0,-1,0),r=.055):
    return cyl('Hex fastener',p,r,.035,'steel',axis,6,.002)

def group(name,objects,p=(0,0,0),yaw=0):
    root=bpy.data.objects.new(name,None)
    COL.objects.link(root)
    for o in objects:
        if o.parent is None:o.parent=root
    root.location=p
    root.rotation_euler.z=yaw
    return root

def begin(): return set(COL.objects)
def added(before):return [o for o in COL.objects if o not in before]

def crate(name,p,size=(1.35,1.05,.95),yaw=0,mat='blue'):
    before=begin()
    sx,sy,sz=size
    box(name+' shell',(0,0,sz/2),size,mat,.09)
    box('Cargo lid',(0,0,sz+.025),(sx+.05,sy+.05,.09),'steel')
    for x in (-sx*.34,sx*.34):
        box('Cargo strap',(x,0,sz+.08),(.10,sy+.1,.045),'ivory',.012)
        for y in (-sy/2-.018,sy/2+.018):box('Cargo corner',(x,y,sz/2),(.1,.06,sz),'steel',.01)
    for x in (-sx*.28,sx*.28):box('Cargo lock',(x,-sy/2-.05,sz*.65),(.16,.08,.22),'orange')
    label('Cargo id','L-07',(0,-sy/2-.058,sz*.35),.17)
    return group(name,added(before),p,yaw)

collection('01 | Lunar terrain and impact field')
craters=[(-28,-6,6.2), (26,-20,5.5),(-9,30,4),(34,20,8),(-43,22,9),(12,-37,4.7)]
def height(x,y):
    d=max(abs(x)/31,abs(y)/29)
    flat=max(0,min(1,(d-.68)*2.5))
    h=-.17+noise.fractal(Vector((x*.075,y*.075,1.17)),1,2,3)*.55*flat
    h+=noise.noise_vector(Vector((x*.65,y*.65,.37))).z*.045
    for cx,cy,r in craters:
        t=math.hypot(x-cx,y-cy)/r
        if t<1.5:
            h+= -.95*max(0,1-t*t)**2 + .45*math.exp(-((t-.91)/.17)**2)
    return h
N=185
span=170
verts=[]
for iy in range(N+1):
    y=-span/2+span*iy/N
    for ix in range(N+1):
        x=-span/2+span*ix/N
        verts.append((x,y,height(x,y)))
faces=[]
for iy in range(N):
    for ix in range(N):
        a=iy*(N+1)+ix
        faces.append((a,a+1,a+N+2,a+N+1))
terrain=mesh('Sculpted regolith with impact basins',verts,faces,'dust')
for p in terrain.data.polygons:p.use_smooth=True
for i in range(190):
    x,y=random.uniform(-42,42),random.uniform(-38,36)
    if -11<x<11 and -16<y<12:continue
    r=random.uniform(.13,.7)
    if abs(x)>29:r*=2.6
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1,radius=1,location=(x,y,height(x,y)+r*.2))
    o=finish(bpy.context.object,'Angular impact ejecta %03d'%i,'rock')
    o.scale=(r*1.3,r*.9,r*.6)
    o.rotation_euler=(random.random(),random.random(),random.random()*6)
log('TERRAIN_OK')

collection('02 | Central service apron - clear combat space')
cyl('Octagonal maintenance apron',(0,0,-.01),10.3,.2,'steel',vertices=8,bevel=.045)
cyl('Inset landing surface',(0,0,.105),9.8,.07,'blue',vertices=8,bevel=.02)
for k in range(8):
    a=k*math.tau/8+math.pi/8
    bar('Apron expansion seam',(math.cos(a)*2.6,math.sin(a)*2.6,.155),(math.cos(a)*9.85,math.sin(a)*9.85,.155),.045,'dark')
    for t in range(4):
        q=a-.12+t*.08
        o=box('Perimeter safety tick',(math.cos(q)*9.25,math.sin(q)*9.25,.159),(.42,.17,.012),'orange',.005,rot=(0,0,q))
    o=box('Recessed marker housing',(math.cos(a)*10.08,math.sin(a)*10.08,.09),(.72,.22,.16),'dark',.025,rot=(0,0,a+math.pi/2))
    if k in (0,2,3,6):box('Surviving apron light',(math.cos(a)*10.08,math.sin(a)*10.08,.18),(.43,.08,.035),'cyan',.006,rot=(0,0,a+math.pi/2))
for k in range(12):
    ring('Faded segmented landing ring',(0,0),7.25,.14,'white',.159,k*math.tau/12+.04,(k+1)*math.tau/12-.09,10)
label('Apron stencil','07',(0,1,.161),3.3,'white',(0,0,0))
label('Apron small stencil','SERVICE  /  NO THRUST',(0,-5.5,.162),.48,'white',(0,0,0))
for side in (-1,1):
    for y in range(-23,-10,3):box('Approach paint',(side*3.8,y,height(side*3.8,y)+.024),(.11,1.7,.013),'white',.003)
for x,y,s in [(5.2,-4.5,1.3),(-4,5,1),(-7,-.5,.6)]:
    for t in range(7):
        angle=random.random()*6.28
        box('Scraped apron scar',(x+random.uniform(-s,s),y+random.uniform(-s,s),.164),(random.uniform(.16,.6),.018,.004),'steel',0,rot=(0,0,angle))
log('APRON_OK')

collection('03 | MARE-07 damaged maintenance hangar')
base=begin()
box('Hangar footing',(0,0,.2),(17.5,8.9,.4),'steel',.12)
box('Left armored wing',(-6.8,0,3.1),(3.6,7.7,5.8),'blue',.28)
box('Right armored wing',(6.8,0,3.1),(3.6,7.7,5.8),'blue',.28)
box('Hangar rear wall',(0,3.6,3.1),(10.2,.6,5.8),'steel',.08)
box('Hangar top header',(0,0,6.05),(16.8,8,1.1),'navy',.24)
box('Roof parapet',(0,.1,6.69),(17,8.2,.24),'ivory',.08)
box('Dusty flat roof',(0,0,6.85),(15.9,7.15,.12),'blue',.05)
box('Dark doorway interior',(0,1.5,2.4),(10.25,.3,4.9),'dark',.03)
for x in (-5.2,5.2):
    box('Door frame upright',(x,-3.95,2.85),(.44,.45,5.6),'ivory',.07)
    for z in (1,2.6,4.4):bolt((x,-4.19,z))
box('Door frame crossbeam',(0,-3.95,5.65),(10.8,.45,.42),'ivory',.08)
for side in (-1,1):
    # The right shutter is jammed up; a dark opening remains underneath.
    zoff=1.15 if side>0 else .23
    for j in range(7):
        z=.46+j*.67+zoff
        if z>5.4:continue
        box('Jammed shutter slat',(side*2.56,-3.67,z),(4.97,.22,.59),'steel',.035)
        box('Shutter reinforcing ridge',(side*2.56,-3.82,z+.18),(4.85,.12,.065),'blue',.018)
    box('Door hazard panel',(side*2.55,-3.84,3.1+zoff),(4.5,.045,.52),'orange',.018)
    for dx in (-1.7,-.8,.1,1):
        box('Black hazard stripe',(side*2.55+dx,-3.871,3.1+zoff),(.25,.013,.48),'dark',.0,rot=(0,-.45,0))
box('Entrance sign backing',(0,-4.04,6.08),(9.5,.12,.85),'dark',.05)
label('Main identity','MARE - 07',(0,-4.12,6.1),.67)
label('Service warning','MAINTENANCE  /  LOCKOUT',(0,-4.13,5.85),.13,'orange')
for x in (-7.4,7.4):
    box('Ivory outer plate',(x,-3.93,3.75),(1.65,.18,3.2),'ivory',.11)
    box('Lower impact guard',(x,-4.05,1.0),(2.05,.35,1.2),'navy',.1)
    label('Wing number','07' if x<0 else 'B',(x,-4.037,4),.86,'navy')
    for j in range(7):box('Recessed cooling louvre',(x,-4.05,2.1+j*.11),(1.05,.045,.035),'steel',.003)
for x in (-8.25,-4.25,4.25,8.25):
    bar('Sloped foundation buttress',(x,-4.8,.2),(x,-3.8,3.2),.36,'steel')
    box('Buttress toe',(x,-4.65,.24),(.74,.96,.38),'ivory',.08)
for x in (-5.5,5.5):
    box('Roof vent body',(x,1.15,7.05),(3.4,2.35,.42),'steel',.1)
    for i in range(11):box('Vent blade',(x,1.15-.92+i*.18,7.3),(2.96,.09,.08),'ivory',.01)
for x in (-2,1):
    cyl('Roof air scrubber',(x,1.6,7.35),.68,1.0,'ivory')
    cyl('Scrubber cap',(x,1.6,7.89),.77,.12,'steel')
    for j in range(8):box('Scrubber radiator',(x-.48+j*.14,1.02,7.35),(.055,.13,.6),'steel',.01)
for x in (-7.5,7.5):
    curve('External umbilical',[(x,3.9,.3),(x,4.3,2),(x,4.3,5.8),(x,2.8,7.2),(x,1.6,7.3)],.15,'steel')
    for z in (1,2.8,4.6):box('Pipe retaining clamp',(x,4.2,z),(.45,.2,.13),'orange')
box('Backup status housing',(-4.56,-4.1,4.85),(.3,.12,.68),'dark')
box('Active emergency lamp',(-4.56,-4.19,4.97),(.13,.06,.27),'amber',.01)
label('Vacuum warning','VACUUM',(-7.4,-4.039,2.85),.22,'navy')
for x in (-6.8,6.8):
    for j in range(12):
        xx=x+random.uniform(-.74,.74);zz=random.uniform(1.9,5.15)
        box('Exposed paint chip',(xx,-4.034,zz),(random.uniform(.02,.11),.011,random.uniform(.03,.12)),'steel',0,rot=(0,random.uniform(-.7,.7),0))
group('HANGAR | modular armored structure',added(base),(0,18.1,0))
# Detached roof/door plates, readable as damage rather than weathering alone.
for i,(p,s,r) in enumerate([((-6.1,11.8,.24),(2.7,1.3,.14),.3),((5.8,12.7,.4),(2,1.9,.14),-.32),((7,11.3,.2),(1.4,.8,.1),.65)]):
    box('Detached armor panel %d'%i,p,s,'ivory',.05,rot=(.12,.13,r))
curve('Severed door power cable',[(5.0,14.4,1.1),(5.4,12,.15),(7.1,11.8,.05),(8.3,12.4,.02)],.045,'dark')
log('HANGAR_OK')

collection('04 | Abandoned six-wheel lunar rover')
before=begin()
box('Rover lower chassis',(0,0,1.04),(3.15,5.7,.55),'steel',.17)
box('Rover upper hull',(0,.05,1.56),(3.1,5.35,.6),'ivory',.18)
box('Rover blue belt',(0,0,1.65),(3.2,5.26,.25),'blue',.04)
# Eight-vertex sloped pressurized cab, front at negative Y.
cv=[(-1.42,-2.48,1.76),(1.42,-2.48,1.76),(1.42,.0,1.76),(-1.42,0,1.76),
    (-1.22,-1.96,3.17),(1.22,-1.96,3.17),(1.3,-.12,3.42),(-1.3,-.12,3.42)]
mesh('Armored sloping rover cab',cv,[(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7),(4,5,6,7)],'ivory',.09)
wind=[(-1.12,-2.381,2.06),(1.12,-2.381,2.06),(1.03,-2.015,3.035),(-1.03,-2.015,3.035)]
mesh('Dead blue windshield',wind,[(0,1,2,3)],'glass')
bar('Windshield center mullion',(0,-2.397,2.04),(0,-2.025,3.08),.085,'steel')
for x in (-1,1):
    mesh('Side armored glazing',[(x*1.451,-2.05,2.08),(x*1.449,-.36,2.08),(x*1.32,-.35,3.1),(x*1.293,-1.82,2.95)],[(0,1,2,3)],'glass')
    box('Cab door lower inset',(x*1.49,-1.05,1.98),(.07,1.4,.34),'navy',.03)
    box('Cab door handle',(x*1.52,-.38,2.46),(.07,.25,.06),'steel',.012)
    bar('Exterior hand rail',(x*1.56,-1.9,2.0),(x*1.56,-.3,2.0),.075,'orange')
    box('Mudguard left/right',(x*1.63,0,1.73),(.46,5.3,.16),'navy',.06)
    for y in (-2,0,2):
        bar('Bogie upper suspension',(x*1.4,y,1.1),(x*1.84,y+.25,.7),.15)
        beam('Wheel axle',(x*1.1,y,.77),(x*2.03,y,.77),.13)
        beam('Exposed shock piston',(x*1.42,y-.43,1.33),(x*1.82,y+.2,.72),.09,'ivory')
        if x==1 and y==-2:continue
        cyl('Rover wheel rubber',(x*1.95,y,.82),.83,.51,'dark',(1,0,0),32,.05)
        cyl('Rover rim',(x*2.224,y,.82),.56,.07,'steel',(1,0,0),16)
        cyl('Rover hub',(x*2.275,y,.82),.27,.13,'orange',(1,0,0),12)
        for a in range(8):
            th=a*math.tau/8
            beam('Wheel spoke',(x*2.265,y,.82),(x*2.265,y+math.cos(th)*.48,.82+math.sin(th)*.48),.045,'ivory')
        for a in range(24):
            th=a*math.tau/24
            box('Wheel chevron tread',(x*1.95,y+math.sin(th)*.815,.82+math.cos(th)*.815),(.59,.15,.095),'steel',.01,rot=(-th,0,0))
box('Rover rear cargo well',(0,1.31,1.98),(2.54,2.32,.25),'dark',.05)
for x in (-1.23,1.23):
    for y in (.22,1.45,2.56):bar('Cargo rail stanchion',(x,y,1.87),(x,y,2.51),.09,'ivory')
    bar('Cargo rail',(x,.22,2.51),(x,2.56,2.51),.10,'ivory')
bar('Tailgate rail',(-1.23,2.56,2.51),(1.23,2.56,2.51),.10,'ivory')
crate('Rover sample container',(-.9,.65,2.04),(1.16,1.35,.61),0,'orange')
crate('Rover battery pack',(.35,1.6,2.03),(.75,.65,.72),0,'navy')
box('Rover roof rack',(0,-.86,3.5),(2.28,1.48,.16),'steel',.07)
for i in range(7):box('Rover roof heat sink',(-.9+i*.3,-.86,3.61),(.09,1.3,.09),'ivory',.01)
beam('Bent rover mast',(.9,-.3,3.59),(1.13,-.3,4.2),.05)
beam('Snapped antenna',(1.13,-.3,4.2),(1.58,-.18,4.42),.035)
box('Roof beacon',(-.9,-1.28,3.73),(.25,.23,.15),'amber',.04)
for x in (-1.06,1.06):
    box('Rover headlamp casing',(x,-2.65,1.68),(.47,.21,.25),'steel')
    box('Dead rover headlamp',(x,-2.773,1.7),(.29,.03,.14),'white',.02)
bar('Rover front bullbar',(-1.67,-2.9,1.25),(1.67,-2.9,1.25),.16,'steel')
for x in (-1.36,1.36):bar('Bullbar hanger',(x,-2.43,.9),(x,-2.93,1.26),.13,'steel')
label('Rover front stencil','SELENE / 04',(0,-2.636,1.96),.21,'navy')
label('Rover roof id','04',(0,-.88,3.70),.65,'white',(0,0,0))
# Splintered glazing lines lie on the windshield plane.
curve('Windshield impact fissure',[(-.83,-2.21,2.54),(-.54,-2.235,2.46),(-.31,-2.16,2.66),(-.1,-2.096,2.82)],.01,'white',False)
curve('Windshield branch',[(-.54,-2.235,2.46),(-.64,-2.317,2.24),(-.39,-2.337,2.18)],.007,'white',False)
for i in range(26):
    x=random.choice((-1,1))
    box('Rover scraped flank',(x*1.612,random.uniform(-2.3,2.3),random.uniform(1.45,1.74)),(.008,random.uniform(.05,.24),.018),'steel',0)
rover=group('SELENE-04 | immobilized recovery rover',added(before),(-14,-10,.02),math.radians(-27))
rover.rotation_euler.y=math.radians(3)
# Detached front-right wheel, tool roll and broken fender.
cyl('Detached rover wheel',(-9.6,-12.1,.34),.83,.56,'dark',(0,0,1),32,.05)
cyl('Detached wheel hub',(-9.6,-12.1,.64),.49,.08,'ivory',(0,0,1),16)
cyl('Detached hub cap',(-9.6,-12.1,.72),.21,.06,'orange',(0,0,1),12)
box('Torn fender',(-10.7,-13.2,.17),(.64,1.63,.15),'navy',.03,rot=(.16,.2,-.7))
box('Abandoned tool mat',(-10.7,-9.5,.03),(1.28,.77,.035),'dark',.025,rot=(0,0,.2))
for i in range(4):bar('Forgotten socket wrench',(-11+i*.2,-9.7,.085),(-11+i*.2,-9.3,.085),.045,'steel')
crate('Open maintenance kit',(-12,-7.5,.0),(.92,.62,.38),.4,'orange')
curve('Loose tow cable',[(-12.4,-12.2,.07),(-11.8,-14.1,.02),(-13,-15.1,.02),(-14.5,-14.8,.02),(-13.9,-13.9,.02)],.042,'dark')
log('ROVER_OK')

collection('05 | Tire ruts and abandoned worksite debris')
for side in (-1,1):
    for i in range(92):
        t=i/91
        y=-33+t*19
        x=-13.1+2.5*math.sin(t*1.3)+side*1.62
        z=height(x,y)+.017
        box('Old rover tread impression',(x,y,z),(.53,.074,.012),'tracks',0,rot=(0,0,-.14+t*.19))
for p,yaw in [((-18,-5,0),.12),((-20,-6,.0),-.19),((13,-7,.0),.21),((15,-7.5,.0),-.23),((13.1,-6.8,1),.07)]:
    crate('Dust-covered freight crate',p,(1.65,1.25,1.06),yaw,'blue')
for i in range(48):
    x,y=random.choice([(-18,-4),(13,-7),(6,12),(-21,8)])
    x+=random.uniform(-2.5,2.5);y+=random.uniform(-2,2)
    z=height(x,y)+.08
    box('Impact wreckage fragment',(x,y,z),(random.uniform(.09,.55),random.uniform(.07,.4),random.uniform(.025,.11)),random.choice(['steel','ivory','blue']),.015,rot=(random.uniform(0,.2),random.uniform(0,.2),random.random()*6))

collection('06 | Broken photovoltaic farm')
def solar(p,yaw,broken=False):
    before=begin()
    for x in (-2,2):
        box('Array ballast',(x,0,.17),(1.3,1.25,.34),'ivory',.06)
        bar('Array support post',(x,0,.3),(x,0,2),.14,'steel')
        bar('Array diagonal brace',(x,-.52,.3),(x,.85,2.5),.12,'steel')
    frame_before=begin()
    box('Solar frame',(0,0,0),(5.8,3.5,.16),'ivory',.035)
    box('Solar black substrate',(0,0,.09),(5.56,3.26,.04),'dark',.01)
    for x in range(6):
        for y in range(4):
            if broken and ((x==4 and y>=2) or (x==5 and y>=1)):continue
            xx=-2.32+x*.925;yy=-1.23+y*.81
            box('Individual blue PV cell',(xx,yy,.121),(.88,.77,.035),'solar',.006)
            for line in range(3):box('PV silver busbar',(xx-.29+line*.29,yy,.142),(.008,.73,.004),'blue',0)
    panel=group('Fractured panel' if broken else 'Solar panel',added(frame_before),(0,0,2.15),0)
    panel.rotation_euler.x=math.radians(28 if not broken else 11)
    if broken:panel.rotation_euler.y=.2
    for x in (-2,2):bar('Panel hinge crossbar',(x,-1.1,1.56),(x,1.1,2.6),.085,'steel')
    return group('PHOTOVOLTAIC ARRAY',added(before),p,yaw)
solar((18,3,0),-.17)
solar((21,9,0),-.17)
solar((17,-2.5,0),.06,True)
for i in range(4):
    p=(19+random.uniform(-1,2),-3.5-random.random()*2,.13)
    box('Shattered photovoltaic section',p,(1.3,.85,.045),'solar',.01,rot=(.1,.06,random.random()*3))
    bar('Broken panel frame',(p[0]-.6,p[1]-.45,p[2]+.03),(p[0]+.6,p[1]-.45,p[2]+.03),.065,'ivory')
curve('Solar power cable',[(18,3,.04),(16,1,.04),(16,7,.04),(13,10,.04),(13,15,.3)],.055,'dark')
box('Offline inverter cabinet',(15.1,6,1),(1.4,.9,2),'blue',.1)
box('Inverter door',(15.1,5.52,1.12),(1.15,.055,1.4),'ivory',.025)
label('Inverter label','PWR / 03',(15.1,5.48,1.5),.21,'navy')
label('Offline stencil','OFFLINE',(15.1,5.477,1.02),.16,'navy')
log('SOLAR_OK')

collection('07 | Damaged long-range communications')
before=begin()
box('Comms plinth',(0,0,.22),(4,4,.44),'steel',.12)
for x in (-1.25,1.25):
    for y in (-1.25,1.25):
        bar('Comms lattice tower leg',(x,y,.3),(x*.55,y*.55,7.9),.2,'ivory')
        for z in (.7,2.4,4.1,5.8):
            s=1-z*.057
            bar('Tower crossbrace',(x*s,y*s,z),(-x*(s-.096),y*(s-.096),z+1.65),.085,'steel')
for z in (1,3,5,7):
    s=1.3-z*.06
    for a,b in [((-s,-s,z),(s,-s,z)),((s,-s,z),(s,s,z)),((s,s,z),(-s,s,z)),((-s,s,z),(-s,-s,z))]:bar('Tower horizontal brace',a,b,.10,'blue')
cyl('Azimuth bearing',(0,0,7.7),1.07,.55,'steel')
bar('Dish tilt yoke',(-.7,0,7.8),(-.7,0,8.8),.3,'blue')
bar('Dish tilt yoke',(.7,0,7.8),(.7,0,8.8),.3,'blue')
dish_before=begin()
# Parabolic reflector with three missing radial sectors.
R=3.35
dv=[];df=[]
for i in range(9):
    rr=R*i/8
    for j in range(48):
        a=math.tau*j/48
        dv.append((rr*math.cos(a),rr*math.sin(a),rr*rr*.115))
for i in range(8):
    for j in range(48):
        if j in (4,5,6,7,8) and i>3:continue
        df.append((i*48+j,i*48+(j+1)%48,(i+1)*48+(j+1)%48,(i+1)*48+j))
dish=mesh('Damaged parabolic reflector',dv,df,'ivory')
sol=dish.modifiers.new('Reflector thickness','SOLIDIFY');sol.thickness=.04
for k in range(16):
    a=k*math.tau/16
    curve('Reflector radial ribs',[(math.cos(a)*rr,math.sin(a)*rr,rr*rr*.115+.025) for rr in (0,.7,1.4,2.1,2.8,3.35)],.027,'steel')
for k in range(3):
    a=k*math.tau/3
    beam('Dish feed strut',(math.cos(a)*2.75,math.sin(a)*2.75,.87),(0,0,2.5),.065,'steel')
cyl('Dish receiver',(0,0,2.4),.22,.55,'orange')
dr=group('Canted fractured dish',added(dish_before),(0,0,8.8),0)
dr.rotation_euler=(math.radians(46),math.radians(-17),0)
for j in range(16):bar('Access ladder rung',(-.31,-1.27,.4+j*.4),(.31,-1.27,.4+j*.4),.055,'orange')
for x in (-.39,.39):bar('Access ladder rail',(x,-1.27,.35),(x,-1.27,6.8),.065,'steel')
box('Tower service case',(1.85,0,1),(1,.95,1.6),'blue',.08)
group('COMMS-07 | fractured reflector',added(before),(-20,12,.0),math.radians(-15))
log('COMMS_OK')

collection('08 | Cryogenic storage and exposed plumbing')
before=begin()
box('Tank platform',(0,0,.17),(7.9,7,.34),'steel',.08)
for x in (-1.9,1.9):
    cyl('Cryogenic tank',(x,0,2.45),1.45,4.45,'ivory',vertices=32,bevel=.12)
    cyl('Tank blue upper band',(x,0,3.95),1.49,.48,'navy',vertices=32)
    cyl('Tank lower band',(x,0,.71),1.5,.24,'steel',vertices=32)
    cyl('Tank top cap',(x,0,4.73),1.28,.18,'blue',vertices=32)
    cyl('Top pressure valve',(x,0,5.03),.2,.5,'steel')
    for a in range(8):
        th=a*math.tau/8
        beam('Tank longitudinal seam',(x+1.46*math.cos(th),1.46*math.sin(th),.9),(x+1.46*math.cos(th),1.46*math.sin(th),3.5),.025,'steel')
    label('Tank marking','HE-3' if x<0 else 'O2',(x,-1.457,2.8),.64,'navy')
    label('Tank small marking','ISOLATED',(x,-1.465,2.16),.22,'navy')
    curve('Cryogenic transfer elbow',[(x,-.5,4.92),(x,-2,4.92),(x,-2.3,4.2),(x,-2.3,.85),(x,-3.0,.55)],.16,'steel')
    cyl('Handwheel valve',(x,-2.51,1.7),.28,.055,'orange',(0,1,0),16)
group('ISOLATED TANK FARM',added(before),(16.5,19,.0))
curve('Main supply line',[(13.4,15.8,.44),(10.8,15.8,.44),(10.8,19,.44),(8.8,19,.44)],.24,'steel')
for y in (16.5,17.4,18.3):box('Pipeline support',(10.8,y,.17),(.87,.4,.3),'ivory',.04)

collection('09 | Cargo gantry, low cover and maintenance props')
before=begin()
for x in (-3,3):
    box('Gantry foot',(x,0,.19),(1.5,1.8,.38),'steel',.09)
    box('Gantry upright',(x,0,3.65),(.42,.65,6.9),'blue',.06)
    bar('Gantry foot diagonal',(x,1,.2),(x,0,2.3),.18,'ivory')
box('Gantry top beam',(0,0,7.06),(7.2,.9,.6),'orange',.06)
box('Gantry rail',(0,-.51,7.14),(6.8,.13,.14),'steel',.015)
box('Abandoned trolley',(-.8,0,6.57),(1,.95,.6),'steel',.07)
curve('Hanging hoist cable',[(-.8,0,6.31),(-.8,.05,4.5),(-.65,.05,3.1)],.035,'dark',False)
curve('Hoist hook',[(-.65,.05,3.1),(-.45,.05,2.88),(-.7,.05,2.65),(-.9,.05,2.81)],.07,'steel')
label('Gantry capacity','12T / SERVICE',(.5,-.466,7.06),.24,'dark')
group('OUTER SERVICE GANTRY',added(before),(-21,-.4,0),math.radians(-6))
for p,yaw in [((-7,-11,0),.12),((8,-9,0),-.15),((-8,8,.0),0),((10,10,0),math.pi/2)]:
    before=begin()
    box('Low cover pedestal',(0,0,.15),(3.1,1.25,.3),'steel',.09)
    box('Low coolant unit',(0,0,.7),(2.8,1.12,1.05),'blue',.12)
    box('Coolant pale armor',(0,0,1.27),(2.9,1.18,.14),'ivory',.04)
    for i in range(12):box('Low cover vent',(-1.15+i*.21,-.583,.79),(.1,.04,.58),'dark',.005)
    for x in (-1.35,1.35):box('Low cover warning',(x,-.597,.84),(.08,.03,.67),'orange',.005)
    group('LOW COVER | no tall center obstruction',added(before),p,yaw)
for x,y in [(-9,13),(9,13),(-24,-9),(24,0)]:
    box('Hazard bollard foot',(x,y,.1),(.62,.62,.2),'steel',.06)
    cyl('Hazard bollard',(x,y,.75),.15,1.3,'ivory',vertices=8)
    cyl('Bollard safety band',(x,y,1.13),.157,.23,'orange',vertices=8)
    cyl('Bollard amber cap',(x,y,1.42),.18,.1,'amber',vertices=8)
for x,y,z in [(-11.8,10.5,1.4),(11.3,9.8,1.1)]:
    box('Site warning sign',(x,y,z),(1.6,.10,.86),'navy',.04)
    label('Site stencil','RESTRICTED',(x,y-.06,z+.1),.17,'white')
    label('Site subtext','MARE / 07',(x,y-.061,z-.19),.16,'orange')
    for xx in (-.55,.55):bar('Sign post',(x+xx,y,.05),(x+xx,y,z-.4),.085,'steel')
log('SET_DRESSING_OK')

collection('10 | Dust drifts against structures')
for i,(x,y,sx,sy) in enumerate([(-7.7,13.1,2.1,1.1),(7.3,13.6,1.9,.8),(-15,-11.7,2,.9),(-20,10,1.1,1.8),(17.6,-2.4,1.9,1),(18.8,17.4,1.6,.8),(11.2,15,1.7,.7)]):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=20,ring_count=10,radius=1,location=(x,y,-.12))
    o=finish(bpy.context.object,'Accumulated lunar dust drift %02d'%i,'dust')
    o.scale=(sx,sy,.3)
    for p in o.data.polygons:p.use_smooth=True

collection('90 | Existing VALKYR - removable scale reference')
hero_source=PROJECT/'art_prototypes/RAIKEN_MkII_20260906/VALKYR_RAIKEN_GAME.blend'
with bpy.data.libraries.load(str(hero_source),link=False) as (src,dst):
    dst.objects=src.objects
hero_objs=[o for o in dst.objects if o and o.type not in ('CAMERA','LIGHT')]
for o in hero_objs:put(o)
for o in dst.objects:
    if o and o.type in ('CAMERA','LIGHT') and o.users==0:bpy.data.objects.remove(o)
hero_root=bpy.data.objects.new('VALKYR scale reference - source preserved',None)
COL.objects.link(hero_root)
for o in hero_objs:
    if o.parent not in hero_objs:
        old=o.matrix_world.copy();o.parent=hero_root;o.matrix_world=old
bpy.context.view_layer.update()
points=[o.matrix_world@Vector(c) for o in hero_objs if o.type=='MESH' and not o.hide_render for c in o.bound_box]
lo=Vector(tuple(min(p[i] for p in points) for i in range(3)))
hi=Vector(tuple(max(p[i] for p in points) for i in range(3)))
scale=3.5/(hi.z-lo.z)
hero_root.scale=(scale,)*3
hero_root.location=(-(lo.x+hi.x)/2*scale,-3-(lo.y+hi.y)/2*scale,.2-lo.z*scale)
hero_root['note']='Appended copy. Source Blender file unchanged. Height normalized to 3.5 m including armor silhouette.'
log('HERO_REFERENCE_OK')

collection('99 | Cameras and lunar lighting')
world=bpy.data.worlds.new('Airless lunar ambient')
scene.world=world;world.use_nodes=True
world.node_tree.nodes.get('Background').inputs['Color'].default_value=(.075,.10,.16,1)
world.node_tree.nodes.get('Background').inputs['Strength'].default_value=.34
def light(name,kind,p,color,energy,size=1,target=(0,0,0)):
    d=bpy.data.lights.new(name,kind);d.energy=energy;d.color=color
    if kind=='AREA':d.shape='DISK';d.size=size
    if kind=='SUN':d.angle=.045
    o=bpy.data.objects.new(name,d);COL.objects.link(o);o.location=p
    o.rotation_euler=(Vector(target)-o.location).to_track_quat('-Z','Y').to_euler()
    return o
light('Hard lunar sunlight','SUN',(-30,-22,45),(1,.91,.78),2.6,target=(0,0,0))
light('Cool earth-bounce fill','AREA',(12,-18,22),(.4,.61,1),1900,24)
light('Far rim on armor','AREA',(-12,22,27),(.63,.78,1),2200,18)
light('Emergency door spill','AREA',(-4.5,13.5,4),(.12,.61,.8),110,3,target=(0,11,1))

def camera(name,p,target,ortho):
    d=bpy.data.cameras.new(name);d.type='ORTHO';d.ortho_scale=ortho;d.clip_end=500
    o=bpy.data.objects.new(name,d);COL.objects.link(o);o.location=p
    o.rotation_euler=(Vector(target)-o.location).to_track_quat('-Z','Y').to_euler()
    return o
hero_cam=camera('01 | Establishing view',(43,-57,48),(0,2,1),68)
camera('02 | Gameplay 60 degree',(0,-32,55.426),(0,0,0),59)
camera('03 | Rover story detail',(-2,-25,10.4),(-13,-10,1.7),18.6)
camera('04 | Hangar architecture',(23,-6,16),(0,16,3),31)
scene.camera=hero_cam
scene.render.engine='CYCLES'
scene.cycles.samples=32
scene.cycles.use_denoising=True
scene.cycles.max_bounces=6
scene.cycles.device='CPU'
scene.render.resolution_x=1800
scene.render.resolution_y=1350
scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
scene.render.film_transparent=False
scene.view_settings.view_transform='AgX'
scene.view_settings.look='AgX - Medium High Contrast'
scene.view_settings.exposure=.35
scene.render.filepath=str(OUT/'renders/01_ESTABLISHING.png')
scene.render.use_file_extension=True
bpy.context.view_layer.update()
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            area.spaces.active.region_3d.view_perspective='CAMERA'
            area.spaces.active.overlay.show_overlays=False
            area.spaces.active.shading.type='MATERIAL'
            area.spaces.active.shading.use_scene_world=True
            area.spaces.active.shading.use_scene_lights=True
            area.spaces.active.clip_end=500
if bpy.context.area and bpy.context.area.type=='CONSOLE':
    bpy.context.area.type='VIEW_3D'
    bpy.context.area.spaces.active.region_3d.view_perspective='CAMERA'
    bpy.context.area.spaces.active.overlay.show_overlays=False
    bpy.context.area.spaces.active.shading.type='MATERIAL'
    bpy.context.area.spaces.active.shading.use_scene_world=True
    bpy.context.area.spaces.active.shading.use_scene_lights=True
bpy.ops.object.select_all(action='DESELECT')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'MARE07_LUNAR_BASE_MASTER.blend'))
manifest={'scene':scene.name,'file':'MARE07_LUNAR_BASE_MASTER.blend','blender':bpy.app.version_string,
          'objects':len(scene.objects),'collections':{c.name:len(c.objects) for c in scene.collection.children},
          'hero_source':str(hero_source),'hero_reference_height':3.5,
          'cameras':[o.name for o in scene.objects if o.type=='CAMERA'],
          'scope':'One authored Blender sector; no Unity integration or runtime performance certification.',
          'required_props':['Abandoned six-wheel rover with missing wheel','Detached wheel and tools','Broken PV panels','Damaged communications dish','Jammed maintenance hangar','Cryogenic tanks','Cargo gantry','Low cover','Tire tracks','Lunar craters and dust drifts']}
(OUT/'scene_manifest.json').write_text(json.dumps(manifest,indent=2,ensure_ascii=False))
log('SAVED_OK '+str(OUT/'MARE07_LUNAR_BASE_MASTER.blend'))
