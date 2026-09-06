"""Jishen Trial hero V01. Run in local Blender, no downloads or game integration.
Coordinates: metres, +Z up, -Y forward, character right = -X.
All modeling functions use a ten-unit design grid, baked into 3.25 metres.
"""
import bpy
import bmesh
import math
import json
import pathlib
import time
from mathutils import Vector, Quaternion

OUT = pathlib.Path(__file__).resolve().parent
S = 0.325
HEIGHT = 10 * S
START = time.time()
SCENE_NAME = 'JT_HERO_V01'
if bpy.data.scenes.get(SCENE_NAME):
    raise RuntimeError('This model already exists in this session. Run in a fresh Blender session.')
scene = bpy.data.scenes.new(SCENE_NAME)
if bpy.context.window:
    bpy.context.window.scene = scene
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1
scene['asset_stage'] = 'V01 shape approval; not integrated into Unity'
scene['height_to_crown_m'] = HEIGHT
scene['blade_total_length_m'] = HEIGHT * .7
scene['axes'] = '+Z up; -Y forward; character right -X'
bpy.context.preferences.filepaths.save_version = 0

def coll(name, parent=None):
    c = bpy.data.collections.new(name)
    (parent.children if parent else scene.collection.children).link(c)
    return c

MODEL = coll('01_MECH | editable parts')
GUIDES = coll('90_PROPORTION_GUIDES | hidden')
STUDIO = coll('80_STUDIO | cameras and light')
REFS = coll('91_REFERENCE_IMAGES | hidden')

def empty(name, at=(0,0,0), parent=None, collection=MODEL):
    o = bpy.data.objects.new(name, None)
    collection.objects.link(o)
    o.location = Vector(at) * S
    o.empty_display_type = 'PLAIN_AXES'
    o.empty_display_size = .07
    if parent:
        bpy.context.view_layer.update()
        m = o.matrix_world.copy()
        o.parent = parent
        o.matrix_world = m
    return o

ROOT = empty('JT_Hero_ROOT | ground pivot')
ROOT['crown_height_m'] = HEIGHT
ROOT['blade_ratio'] = .7
ROOT['right_side'] = '-X'
ROOT['pose'] = 'Neutral A stance; arms 12 degrees from vertical'

def mat(name, rgb, metallic=.35, roughness=.42, emission=0):
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*rgb, 1)
    m.use_nodes = True
    p = next(n for n in m.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
    p.inputs['Base Color'].default_value = (*rgb, 1)
    p.inputs['Metallic'].default_value = metallic
    p.inputs['Roughness'].default_value = roughness
    p.inputs['Emission Color'].default_value = (*rgb,1)
    p.inputs['Emission Strength'].default_value = emission
    return m

NAVY = mat('M01_Military_Navy',(.048,.081,.142),.48,.4)
NAVY_LIGHT = mat('M02_Navy_Upper_Facets',(.082,.13,.214),.42,.4)
DARK = mat('M03_Graphite_Armor',(.058,.067,.082),.48,.44)
FRAME = mat('M04_Joint_Frame',(.026,.032,.041),.58,.4)
STEEL = mat('M05_Mechanical_Steel',(.19,.225,.26),.72,.33)
WHITE = mat('M06_Offwhite_Accent',(.64,.67,.7),.32,.4)
SENSOR = mat('M07_Twin_Sensor',(.04,.57,.86),.18,.28,.8)
ENERGY = mat('M08_Continuous_Light_Edge',(.025,.48,1),.05,.26,2.2)
CLAY = mat('M90_Inspection_Clay',(.42,.44,.46),0,.72)
FLOOR = mat('M91_Light_Grey_Background',(.66,.68,.7),0,.82)

def link_obj(o, collection, parent):
    for c in list(o.users_collection):
        c.objects.unlink(o)
    collection.objects.link(o)
    if parent:
        bpy.context.view_layer.update()
        matrix = o.matrix_world.copy()
        o.parent = parent
        o.matrix_world = matrix
    return o

def finish(o, material, parent=ROOT, collection=MODEL, bevel=.025):
    o.data.materials.append(material)
    o.color = material.diffuse_color
    link_obj(o,collection,parent)
    if bevel:
        b = o.modifiers.new('Edge chamfer | editable', 'BEVEL')
        b.width = bevel * S
        b.segments = 2
        b.limit_method = 'ANGLE'
    return o

def mesh(name, verts, faces, material, parent=ROOT, collection=MODEL, bevel=.025):
    d = bpy.data.meshes.new(name + '_Mesh')
    d.from_pydata([tuple(Vector(v)*S) for v in verts], [], faces)
    d.update()
    bm = bmesh.new()
    bm.from_mesh(d)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(d)
    bm.free()
    o = bpy.data.objects.new(name,d)
    scene.collection.objects.link(o)
    centre = sum((v.co for v in d.vertices), Vector()) / len(d.vertices)
    for v in d.vertices: v.co -= centre
    o.location = centre
    return finish(o,material,parent,collection,bevel)

def box(name, at, size, material, parent=ROOT, collection=MODEL, bevel=.035):
    x,y,z=at; a,b,c=[n/2 for n in size]
    v=[(x+i*a,y+j*b,z+k*c) for k in (-1,1) for j in (-1,1) for i in (-1,1)]
    f=[(0,2,3,1),(4,5,7,6),(0,1,5,4),(2,6,7,3),(0,4,6,2),(1,3,7,5)]
    return mesh(name,v,f,material,parent,collection,bevel)

def loft(name, rings, material, parent=ROOT, bevel=.025, facets=False):
    # Eight-sided chamfered sections: (z, centreX, centreY, width, depth).
    v=[]
    for z,x,y,w,d in rings:
        v.extend([(x+a*w/2,y+b*d/2,z) for a,b in
                  [(-.7,-1),(.7,-1),(1,-.7),(1,.7),(.7,1),(-.7,1),(-1,.7),(-1,-.7)]])
    f=[tuple(reversed(range(8)))]
    for r in range(len(rings)-1):
        for j in range(8):
            f.append((r*8+j,r*8+(j+1)%8,(r+1)*8+(j+1)%8,(r+1)*8+j))
    f.append(tuple(range((len(rings)-1)*8,len(rings)*8)))
    o=mesh(name,v,f,material,parent,bevel=bevel)
    if facets:
        o.data.materials.append(NAVY_LIGHT)
        for p in o.data.polygons:
            if p.normal.z>.2: p.material_index=1
    return o

def plate(name, outline, front, back, material=NAVY, parent=ROOT, bulge=.075, bevel=.018):
    # Solid armor with broad face and sloping perimeter, not a flat decal.
    n=len(outline); cx=sum(p[0] for p in outline)/n; cz=sum(p[1] for p in outline)/n
    v=[(x,back,z) for x,z in outline]+[(x,front,z) for x,z in outline]
    v += [(cx+(x-cx)*.8, front-bulge,cz+(z-cz)*.8) for x,z in outline]
    f=[tuple(reversed(range(n))),tuple(range(2*n,3*n))]
    for j in range(n):
        k=(j+1)%n
        f.extend([(j,k,n+k,n+j),(n+j,n+k,2*n+k,2*n+j)])
    o=mesh(name,v,f,material,parent,bevel=bevel)
    o['armor_thickness_m']=abs(back-front)*S
    return o

def cylinder(name, a, b, radius, material, parent=ROOT, vertices=12, r2=None):
    a,b=Vector(a)*S,Vector(b)*S
    bpy.ops.mesh.primitive_cone_add(vertices=vertices,radius1=radius*S,radius2=(radius if r2 is None else r2)*S,
                                    depth=(b-a).length,location=(a+b)/2)
    o=bpy.context.object; o.name=name
    o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler()
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    return finish(o,material,parent,MODEL,.012)

def tube(name, a,b,outer,inner,material,parent=ROOT):
    a,b=Vector(a),Vector(b); q=(b-a).to_track_quat('Z','Y'); n=16; v=[]
    for centre,rad in ((a,outer),(b,outer),(a,inner),(b,inner)):
        v += [tuple(centre+q@Vector((rad*math.cos(j*2*math.pi/n),rad*math.sin(j*2*math.pi/n),0))) for j in range(n)]
    f=[]
    for j in range(n):
        k=(j+1)%n
        f += [(j,k,n+k,n+j),(2*n+j,3*n+j,3*n+k,2*n+k),(j,2*n+j,2*n+k,k),(n+j,n+k,3*n+k,3*n+j)]
    return mesh(name,v,f,material,parent,bevel=.012)

def segment(name, a,b,radius,material,parent=ROOT):
    return cylinder(name,a,b,radius,material,parent)

def pivot(o, at):
    # Set origin to an articulation point without moving geometry.
    bpy.context.view_layer.update()
    point=Vector(at)*S
    old=o.matrix_world.copy()
    new=old.copy(); new.translation=point
    o.data.transform(new.inverted()@old)
    o.matrix_world=new

def save(name):
    scene['build_elapsed_seconds']=round(time.time()-START,2)
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/name))

# Stage 1: retain the initial simple proportion volumes in a hidden guide collection.
for name,at,size in [
    ('Head',(0,0,9.55),(1.02,.8,.9)),('Chest',(0,0,8.1),(2.9,1.28,1.65)),
    ('Waist',(0,0,6.6),(1.28,.92,.86)),('Pelvis',(0,0,5.98),(1.65,1.05,.6)),
    ('Thigh.R',(-.86,0,4.68),(.69,.78,2.08)),('Thigh.L',(.86,0,4.68),(.69,.78,2.08)),
    ('Shin.R',(-1.01,0,2.13),(.69,.83,2.23)),('Shin.L',(1.01,0,2.13),(.69,.83,2.23))]:
    box('Guide_'+name,at,size,CLAY,parent=None,collection=GUIDES,bevel=0)
save('progress/01_proportions.blend')
GUIDES.hide_render=True
GUIDES.hide_viewport=True

# Articulation hierarchy. Origins are at intended future joint locations.
PELVIS = empty('Body_Pelvis', (0,0,5.88), ROOT)
WAIST = empty('Body_Waist', (0,0,6.35), PELVIS)
CHEST = empty('Body_Thorax', (0,0,7.15), WAIST)
HEAD = empty('Head_Assembly', (0,0,9.06), CHEST)
ARMOR = empty('Armor_Torso', (0,0,7.9), CHEST)

loft('Thorax_Structural_Core',[(7.04,0,.05,1.18,.86),(7.65,0,.05,2.02,1.22),(8.5,0,.08,2.72,1.3),(8.89,0,.11,1.82,1.03)],DARK,CHEST)
loft('Waist_Flexible_Core',[(6.1,0,.06,1.08,.73),(6.68,0,.03,1.23,.84),(7.11,0,.02,1.38,.94)],FRAME,WAIST)
for k,(z,w) in enumerate(((6.38,1.12),(6.65,1.25),(6.92,1.38))):
    plate('Abdomen_Overlapping_Plate_%02d'%k,[(-w/2,z+.17),(w/2,z+.17),(w*.39,z-.09),(0,z-.18),(-w*.39,z-.09)],-.48,-.29,DARK,WAIST,.06)
loft('Pelvis_Casing',[(5.56,0,.04,1.15,.87),(5.95,0,.02,1.72,1.12),(6.25,0,.03,1.5,.98)],DARK,PELVIS)
plate('Pelvis_Central_Keel',[(-.3,6.13),(.3,6.13),(.34,5.81),(0,5.35),(-.34,5.81)],-.59,-.37,NAVY,PELVIS,.1)
for sign,side in ((-1,'R'),(1,'L')):
    p=lambda points:[(sign*x,z) for x,z in points]
    plate('Chest_Pectoral.'+side,p([(.12,8.77),(1.10,8.86),(1.43,8.57),(1.23,8.05),(.35,8.12)]),-.69,-.40,NAVY,ARMOR,.13)
    plate('Chest_Clavicle.'+side,p([(.35,8.95),(1.05,9),(1.39,8.71),(1.16,8.62),(.4,8.74)]),-.48,-.18,NAVY_LIGHT,ARMOR,.025)
    plate('Chest_Vent_Recess.'+side,p([(.43,8.03),(1.2,7.96),(1.13,7.68),(.4,7.78)]),-.60,-.44,FRAME,ARMOR,.005)
    for j in range(2):
        plate('Chest_Vent_Louver_%d.%s'%(j,side),p([(.48,7.94-j*.1),(1.1,7.88-j*.1),(1.07,7.82-j*.1),(.46,7.88-j*.1)]),-.64,-.58,WHITE if j==0 else STEEL,ARMOR,.003,.008)
    plate('Thorax_Side_Rib.'+side,p([(.44,7.70),(1.11,7.60),(.99,7.21),(.60,7.03),(.43,7.25)]),-.46,-.13,DARK,ARMOR,.09)
    plate('Waist_Oblique.'+side,p([(.48,6.92),(.76,7.13),(.83,6.8),(.58,6.31),(.41,6.44)]),-.3,.05,WHITE,WAIST,.03)
    cylinder('Pelvis_Hip_Axle.'+side,(sign*.56,0,5.76),(sign*.97,0,5.76),.29,STEEL,PELVIS)
    hinge=empty('Skirt_Front_Hinge.'+side,(sign*.67,-.33,5.98),PELVIS)
    plate('Skirt_Front_Armor.'+side,p([(.30,6.01),(.87,6.05),(1.13,5.46),(.98,5.00),(.55,5.2),(.38,5.60)]),-.77,-.50,NAVY,hinge,.08)
    plate('Skirt_Front_Upper_Layer.'+side,p([(.39,5.98),(.82,5.99),(.96,5.64),(.47,5.7)]),-.89,-.78,NAVY_LIGHT,hinge,.014)
    sidehinge=empty('Skirt_Side_Hinge.'+side,(sign*1.01,.08,5.94),PELVIS)
    loft('Skirt_Side_Flared.'+side,[(5.03,sign*1.19,.14,.31,.79),(5.43,sign*1.32,.12,.39,1.0),(5.98,sign*.99,.05,.35,.69)],NAVY,sidehinge,facets=True)
    plate('Skirt_Rear.'+side,p([(.20,5.98),(.83,6.04),(1.03,5.48),(.82,5.08),(.33,5.2)]),.71,.44,DARK,PELVIS,-.08)
plate('Chest_Central_Armored_Keel',[(0,8.82),(.36,8.48),(.32,7.88),(0,7.39),(-.32,7.88),(-.36,8.48)],-.82,-.42,NAVY,ARMOR,.16)
plate('Chest_Keel_Upper_Inset',[(-.14,8.62),(.14,8.62),(.18,8.37),(0,8.22),(-.18,8.37)],-1.01,-.94,DARK,ARMOR,.02)
plate('Abdominal_Lower_Accent',[(-.16,6.27),(.16,6.27),(.14,6.04),(0,5.96),(-.14,6.04)],-.66,-.57,WHITE,PELVIS,.015)
loft('Collar_Armored_Ring',[(8.79,0,.07,1.1,.84),(9.02,0,.1,1.18,.9)],DARK,CHEST)
cylinder('Neck_Pivot',(0,0,8.92),(0,0,9.29),.23,STEEL,HEAD)

# Compact helmet, two distinct eye apertures and a solid angular mask.
loft('Head_Helmet_Main',[(9.17,0,.02,.68,.62),(9.48,0,.02,1.06,.86),(9.82,0,.09,.94,.8),(10,0,.11,.49,.57)],NAVY,HEAD,facets=True)
plate('Head_Visor_Dark_Recess',[(-.46,9.62),(.46,9.62),(.39,9.34),(0,9.28),(-.39,9.34)],-.48,-.34,FRAME,HEAD,.02,.008)
for sign,side in ((-1,'R'),(1,'L')):
    p=lambda points:[(sign*x,z) for x,z in points]
    plate('Head_Eye_Sensor.'+side,p([(.065,9.48),(.365,9.57),(.335,9.47),(.10,9.41)]),-.552,-.515,SENSOR,HEAD,.003,.005)
    plate('Head_Brow_Guard.'+side,p([(.015,9.61),(.41,9.73),(.54,9.68),(.40,9.58),(.11,9.49)]),-.57,-.43,WHITE,HEAD,.025,.012)
    plate('Head_Cheek_Armor.'+side,p([(.36,9.42),(.57,9.57),(.50,9.18),(.23,9.08),(.25,9.3)]),-.46,-.09,NAVY,HEAD,.055)
    cylinder('Head_Antenna.'+side,(sign*.40,.06,9.69),(sign*.60,.10,10.29),.037,DARK,HEAD,vertices=6,r2=.008)
    plate('Head_Temple_Fin.'+side,p([(.36,9.71),(.62,9.90),(.59,9.53),(.45,9.37)]),-.13,.12,NAVY,HEAD,.012)
plate('Head_Hard_Face_Mask',[(-.18,9.43),(.18,9.43),(.24,9.24),(0,9.035),(-.24,9.24)],-.60,-.39,DARK,HEAD,.075,.012)
plate('Head_Mask_Central_Ridge',[(0,9.46),(.10,9.29),(0,9.08),(-.10,9.29)],-.692,-.62,STEEL,HEAD,.025,.009)
plate('Head_Crown_Plate',[(-.16,9.99),(.16,9.99),(.22,9.70),(0,9.59),(-.22,9.70)],-.36,-.22,NAVY_LIGHT,HEAD,.04)

# Long humanoid legs, separated hip / knee / ankle pivots and solid wedge feet.
for sign,side in ((-1,'R'),(1,'L')):
    x=lambda v:sign*v
    p=lambda points:[(sign*a,z) for a,z in points]
    hip=(x(.82),.02,5.77); knee=(x(.96),0,3.35); ankle=(x(1.045),.015,.78)
    thigh=empty('Leg_Thigh.'+side,hip,PELVIS)
    shin=empty('Leg_Shin.'+side,knee,thigh)
    foot=empty('Leg_Foot.'+side,ankle,shin)
    segment('Thigh_Inner_Frame.'+side,hip,knee,.20,FRAME,thigh)
    loft('Thigh_Armored_Volume.'+side,[(3.72,x(.94),.03,.51,.6),(4.10,x(.92),.02,.66,.76),(5.03,x(.86),.03,.83,.85),(5.53,x(.83),.02,.65,.73)],DARK,thigh)
    plate('Thigh_Front_Sloped_Plate.'+side,p([(.55,5.25),(.99,5.32),(1.22,4.80),(1.18,4.21),(.97,3.69),(.70,3.84),(.57,4.44)]),-.43,-.20,NAVY,thigh,.075)
    plate('Thigh_Inner_White_Layer.'+side,p([(.53,5.0),(.68,4.95),(.74,4.08),(.66,3.87),(.48,4.30)]),-.34,-.18,WHITE,thigh,.025)
    plate('Thigh_Upper_Facet.'+side,p([(.65,5.25),(.97,5.27),(1.13,4.92),(.67,4.99)]),-.54,-.44,NAVY_LIGHT,thigh,.015)
    cylinder('Knee_Exposed_Hinge.'+side,(x(.64),0,3.35),(x(1.26),0,3.35),.27,STEEL,shin)
    cylinder('Knee_Outer_Cap.'+side,(x(1.23),0,3.35),(x(1.31),0,3.35),.18,DARK,shin)
    plate('Knee_Shield.'+side,p([(.67,3.55),(.98,3.68),(1.25,3.47),(1.15,3.02),(.95,2.9),(.72,3.09)]),-.45,-.18,NAVY,shin,.15)
    plate('Knee_Shield_Inset.'+side,p([(.8,3.48),(.99,3.54),(1.1,3.39),(.96,3.24),(.81,3.29)]),-.63,-.57,DARK,shin,.01)
    segment('Shin_Main_Strut.'+side,(x(.96),.10,3.29),(x(1.045),.09,.75),.20,FRAME,shin)
    loft('Calf_Armored_Volume.'+side,[(1.0,x(1.045),.06,.48,.57),(1.70,x(1.06),.16,.65,.82),(2.55,x(1.01),.17,.96,1.09),(3.00,x(.98),.12,.72,.87)],NAVY,shin,facets=True)
    plate('Shin_Front_Ridge.'+side,p([(.70,2.95),(1.17,2.90),(1.36,2.5),(1.21,1.95),(1.14,1.10),(.96,.85),(.79,1.05),(.77,1.77),(.64,2.25)]),-.45,-.24,NAVY,shin,.13)
    plate('Shin_Upper_Overlay.'+side,p([(.72,2.88),(1.17,2.86),(1.28,2.57),(1.05,2.42),(.76,2.53)]),-.61,-.49,NAVY_LIGHT,shin,.025)
    plate('Calf_Outer_Blade_Armor.'+side,p([(1.20,2.91),(1.50,2.64),(1.55,1.92),(1.30,2.1),(1.16,2.43)]),-.08,.30,DARK,shin,.06)
    cylinder('Calf_Rear_Damper.'+side,(x(.86),.69,2.62),(x(.94),.46,1.25),.07,STEEL,shin)
    cylinder('Calf_Rear_Damper_Sleeve.'+side,(x(.86),.69,2.62),(x(.9),.59,1.94),.105,DARK,shin)
    cylinder('Ankle_Hinge.'+side,(x(.78),.015,.76),(x(1.30),.015,.76),.21,STEEL,foot)
    plate('Ankle_White_Guard.'+side,p([(.82,1.02),(1.16,1.08),(1.33,.72),(1.18,.49),(.95,.55),(.77,.79)]),-.40,-.10,WHITE,foot,.055)
    # Foot hull has a low toe, higher instep, and heel; it is an actual sloped solid.
    xx=x(1.06)
    vv=[(xx-.35,-.94,.07),(xx+.35,-.94,.07),(xx+.38,.47,.07),(xx-.38,.47,.07),
        (xx-.25,-.94,.30),(xx+.25,-.94,.30),(xx+.30,-.21,.61),(xx-.30,-.21,.61),
        (xx+.30,.40,.43),(xx-.30,.40,.43)]
    ff=[(0,3,2,1),(0,1,5,4),(4,5,6,7),(7,6,8,9),(3,9,8,2),(0,4,7,9,3),(1,2,8,6,5)]
    mesh('Foot_Main_Wedge.'+side,vv,ff,NAVY,foot)
    box('Foot_Sole.'+side,(xx,-.23,.08),(.77,1.48,.16),DARK,foot,bevel=.045)
    plate('Foot_Toe_Cap.'+side,[(xx-.29,.13),(xx+.29,.13),(xx+.27,.31),(xx,.40),(xx-.27,.31)],-.96,-.72,NAVY_LIGHT,foot,.025)
    plate('Foot_Instep_Inset.'+side,[(xx-.08,.28),(xx+.08,.28),(xx+.09,.52),(xx-.09,.52)],-.66,-.53,DARK,foot,.025)

# Arms hang 12 degrees out from the torso. Large segments remain independent.
for sign,side in ((-1,'R'),(1,'L')):
    p=lambda points:[(sign*x,z) for x,z in points]
    shoulder=(sign*1.68,.07,8.51); elbow=(sign*1.97,.01,7.12); wrist=(sign*2.26,-.04,5.72)
    upper=empty('Arm_Upper.'+side,shoulder,CHEST)
    lower=empty('Arm_Forearm.'+side,elbow,upper)
    hand=empty('Arm_Hand.'+side,wrist,lower)
    shell=empty('Shoulder_Armor_Pivot.'+side,shoulder,CHEST)
    cylinder('Shoulder_Joint.'+side,(sign*1.23,.07,8.5),(sign*1.83,.07,8.5),.34,STEEL,upper)
    segment('UpperArm_Frame.'+side,shoulder,elbow,.19,FRAME,upper)
    loft('UpperArm_Armor.'+side,[(7.39,sign*1.9,.03,.42,.55),(7.71,sign*1.88,.02,.64,.71),(8.17,sign*1.77,.04,.62,.68)],DARK,upper)
    plate('UpperArm_White_Plate.'+side,p([(1.59,8.13),(1.92,8.11),(2.11,7.61),(1.9,7.39),(1.67,7.56)]),-.38,-.15,WHITE,upper,.065)
    loft('Shoulder_Main_Volume.'+side,[(8.13,sign*1.83,.08,.89,.99),(8.73,sign*1.77,.1,1.13,1.16),(9.0,sign*1.63,.13,.80,.93)],NAVY,shell,facets=True)
    plate('Shoulder_Front_Faceted_Armor.'+side,p([(1.29,8.81),(1.63,9.02),(2.21,8.86),(2.32,8.44),(2.10,8.05),(1.56,8.17)]),-.58,-.28,NAVY,shell,.105)
    plate('Shoulder_Upper_Layer.'+side,p([(1.4,8.86),(1.67,8.99),(2.18,8.83),(2.12,8.66),(1.6,8.76)]),-.7,-.6,NAVY_LIGHT,shell,.015)
    plate('Shoulder_Lower_Floating_Layer.'+side,p([(1.63,8.24),(2.14,8.14),(2.18,7.9),(1.87,7.77),(1.69,7.92)]),-.4,-.10,DARK,shell,.035)
    plate('Shoulder_Small_White_Accent.'+side,p([(1.56,8.5),(1.85,8.43),(1.80,8.34),(1.55,8.41)]),-.716,-.674,WHITE,shell,.005,.007)
    cylinder('Elbow_Exposed_Hinge.'+side,(sign*1.76,.01,7.11),(sign*2.18,.01,7.11),.235,STEEL,lower)
    cylinder('Elbow_Outer_Cap.'+side,(sign*2.13,.01,7.11),(sign*2.23,.01,7.11),.15,DARK,lower)
    segment('Forearm_Frame.'+side,elbow,wrist,.16,FRAME,lower)
    loft('Forearm_Tapered_Armor.'+side,[(5.91,sign*2.23,-.05,.48,.52),(6.23,sign*2.21,-.05,.67,.75),(6.78,sign*2.10,-.01,.76,.8),(6.93,sign*2.05,0,.58,.61)],NAVY,lower,facets=True)
    plate('Forearm_Front_Shield.'+side,p([(1.80,6.90),(2.25,6.94),(2.53,6.47),(2.48,6.08),(2.25,5.80),(1.96,5.99),(1.94,6.37)]),-.49,-.2,NAVY,lower,.10)
    plate('Forearm_Long_White_Inset.'+side,p([(1.91,6.66),(2.02,6.68),(2.20,6.14),(2.16,5.98),(2.04,6.08)]),-.61,-.51,WHITE,lower,.015)
    plate('Forearm_Upper_Facet.'+side,p([(1.90,6.89),(2.20,6.89),(2.39,6.55),(2.05,6.59)]),-.62,-.52,NAVY_LIGHT,lower,.016)
    cylinder('Wrist_Coupling.'+side,(sign*2.26,-.04,5.83),(sign*2.29,-.04,5.51),.16,STEEL,hand)
    palm=loft('Hand_Palm.'+side,[(5.15,sign*2.33,-.02,.40,.28),(5.50,sign*2.30,-.035,.42,.31)],DARK,hand)
    plate('Hand_Dorsal_Armor.'+side,p([(2.11,5.5),(2.46,5.48),(2.51,5.25),(2.26,5.17),(2.12,5.31)]),-.23,-.12,NAVY,hand,.025)
    # Simple separated curled digits; no dense small mechanical detail.
    for j in range(4):
        xx=sign*(2.16+j*.105)
        box('Hand_Finger_%d_Proximal.%s'%(j,side),(xx,-.015,5.06),(.083,.24,.24),STEEL,hand,bevel=.022)
        box('Hand_Finger_%d_Curl.%s'%(j,side),(xx,-.12,4.98),(.083,.19,.13),DARK,hand,bevel=.02)
    cylinder('Hand_Thumb.'+side,(sign*2.08,-.035,5.37),(sign*2.01,-.18,5.13),.09,STEEL,hand,vertices=8)

# Compact cannon on the character's RIGHT shoulder (-X), directed forward (-Y).
CANNON=empty('Cannon_Right_Yaw_Pivot',(-1.66,.19,9.05),CHEST)
CANNON['mount_side']='character right (-X); front view viewer left'
CANNON['future_motion']='Yaw Z at this pivot; pitch X at child'
cylinder('Cannon_Right_Turntable',(-1.66,.19,8.93),(-1.66,.19,9.19),.24,DARK,CANNON)
PITCH=empty('Cannon_Right_Pitch_Pivot',(-1.66,.17,9.38),CANNON)
cylinder('Cannon_Pitch_Trunnion',(-1.99,.17,9.36),(-1.34,.17,9.36),.16,STEEL,PITCH)
loft('Cannon_Receiver',[(9.17,-1.66,.20,.51,.86),(9.59,-1.66,.20,.51,.94)],DARK,PITCH)
box('Cannon_Receiver_Top',(-1.66,.21,9.61),(.40,.67,.08),NAVY,PITCH,bevel=.025)
cylinder('Cannon_Short_Barrel',(-1.66,-.14,9.45),(-1.66,-1.03,9.49),.18,STEEL,PITCH,vertices=12)
tube('Cannon_Muzzle_Hollow',(-1.66,-.95,9.485),(-1.66,-1.30,9.502),.235,.157,DARK,PITCH)
tube('Cannon_Muzzle_Rim',(-1.66,-1.27,9.502),(-1.66,-1.325,9.506),.246,.155,STEEL,PITCH)
cylinder('Cannon_Bore_Recess',(-1.66,-.86,9.48),(-1.66,-.89,9.482),.155,FRAME,PITCH)
box('Cannon_Side_Service_Cover',(-1.955,.16,9.4),(.085,.52,.26),NAVY,PITCH,bevel=.025)
box('Cannon_Side_Small_Accent',(-2.002,.19,9.4),(.013,.22,.08),WHITE,PITCH,bevel=.008)

# Folded backpack: central spine + independently movable pods and outer vanes.
PACK=empty('Backpack_Main_Mount',(0,.69,8.20),CHEST)
loft('Backpack_Central_Spine',[(6.75,0,1.01,.56,.58),(7.28,0,1.06,.72,.79),(8.53,0,1.00,.79,.82),(8.98,0,.87,.52,.55)],DARK,PACK)
box('Backpack_Mounting_Yoke',(0,.72,8.23),(1.52,.23,.54),STEEL,PACK,bevel=.08)
for sign,side in ((-1,'R'),(1,'L')):
    pod=empty('Backpack_Deploy_Hinge.'+side,(sign*.66,.96,8.67),PACK)
    pod['folded_state']=True
    pod['future_motion']='Rotate local Y around top hinge; no deployed animation in V01'
    cylinder('Backpack_Hinge_Axle.'+side,(sign*.48,.92,8.64),(sign*.95,.92,8.64),.17,STEEL,pod)
    loft('Backpack_Folded_Thruster_Pod.'+side,[(6.62,sign*.79,1.22,.42,.56),(7.12,sign*.8,1.28,.63,.83),(8.7,sign*.66,1.11,.65,.83),(9.2,sign*.58,.99,.33,.49)],NAVY,pod,facets=True)
    p=lambda points:[(sign*x,z) for x,z in points]
    plate('Backpack_Pod_Rear_Armor.'+side,p([(.34,8.83),(.63,9.09),(.98,8.62),(1.13,7.35),(.89,6.92),(.54,7.22)]),1.74,1.48,NAVY,pod,-.07)
    plate('Backpack_Rear_Inset.'+side,p([(.60,8.53),(.82,8.53),(.97,7.54),(.74,7.38)]),1.827,1.77,DARK,pod,-.018)
    tube('Backpack_Main_Nozzle.'+side,(sign*.8,1.32,7.0),(sign*.81,1.38,6.53),.235,.158,STEEL,pod)
    cylinder('Backpack_Nozzle_Interior.'+side,(sign*.805,1.35,6.78),(sign*.805,1.36,6.75),.15,FRAME,pod)
    vane=empty('Backpack_Folded_Vane_Hinge.'+side,(sign*.98,1.02,8.38),pod)
    vane['future_motion']='Independent outer vane; folded flush along pod'
    loft('Backpack_Outer_Folded_Vane.'+side,[(6.75,sign*1.05,1.1,.12,.35),(7.32,sign*1.19,1.12,.24,.54),(8.48,sign*1.09,1.03,.32,.60),(8.86,sign*.99,.97,.12,.35)],DARK,vane)
    tube('Backpack_Small_Vector_Nozzle.'+side,(sign*1.1,1.1,7.31),(sign*1.1,1.14,6.98),.10,.063,STEEL,vane)

# The ship-cleaver is a physical asymmetric blade with one continuous light edge.
WEAPON_COLLECTION=coll('02_WEAPON | body and light edge separate')
SWORD=empty('Weapon_ShipCleaver_ROOT',(0,0,0),None,WEAPON_COLLECTION)
SWORD['total_length_m']=7*S
SWORD['ratio_to_crown']=.7
SWORD['presentation']='Detached upright beside neutral mech for proportion inspection'
SWORD['toggle_light']='Hide Weapon_Light_Edge only; blade and spine stay visible'
old_model=MODEL
MODEL=WEAPON_COLLECTION
cylinder('Weapon_Pommel',(0,0,.0),(0,0,.18),.12,DARK,SWORD,vertices=8)
cylinder('Weapon_Grip_Core',(0,0,.15),(0,0,1.12),.082,FRAME,SWORD,vertices=8)
for j in range(5):
    cylinder('Weapon_Grip_Band_%02d'%j,(0,0,.23+j*.16),(0,0,.29+j*.16),.10,STEEL,SWORD,vertices=8)
plate('Weapon_Emitter_Housing',[(-.23,.99),(.20,.99),(.29,1.34),(.21,1.66),(-.21,1.52)],-.15,.15,DARK,SWORD,.04)
box('Weapon_Compact_Guard',(0,0,1.10),(.66,.26,.12),STEEL,SWORD,collection=MODEL,bevel=.035)
plate('Weapon_Physical_Blade',[(-.18,1.43),(.20,1.52),(.20,5.98),(.16,6.45),(.055,7),(-.18,6.51)],-.074,.074,DARK,SWORD,.032,.008)
plate('Weapon_Mechanical_Spine',[(-.22,1.43),(-.07,1.49),(-.06,6.42),(.055,7),(-.22,6.52)],-.11,.11,STEEL,SWORD,.018,.01)
plate('Weapon_Navy_Body_Panel',[(-.055,1.74),(.14,1.80),(.14,5.95),(-.055,6.26)],-.119,-.078,NAVY,SWORD,.01,.007)
# One watertight independent mesh spanning the entire cutting side, including point.
plate('Weapon_Light_Edge',[(.202,1.57),(.26,1.67),(.26,5.99),(.216,6.48),(.055,7),(.161,6.42),(.199,5.95)],-.081,.081,ENERGY,SWORD,.006,.003)
plate('Weapon_Emitter_Indicator',[(.10,1.22),(.21,1.26),(.22,1.48),(.11,1.45)],-.202,-.159,SENSOR,SWORD,.002,.004)
for j in range(3):
    box('Weapon_Spine_Clamp_%d'%j,(-.15,0,1.87+j*1.40),(.19,.27,.12),DARK,SWORD,collection=MODEL,bevel=.015)
MODEL=old_model
for part in list(SWORD.children_recursive):
    link_obj(part,WEAPON_COLLECTION,part.parent)
bpy.context.view_layer.update()
SWORD.location=Vector((-3.25,-.08,7.68))*S
SWORD.rotation_euler[1]=math.pi

# Pack the original user reference images in the file, hidden from render.
for i,name in enumerate(('concept_multiview.png','concept_perspective.png')):
    im=bpy.data.images.load(str(OUT/'references'/name),check_existing=True)
    im.pack()
    ref=empty('Reference_%02d'%i,(8+i*5,3,5),None,REFS)
    ref.empty_display_type='IMAGE'; ref.data=im
    ref.empty_display_size=4
REFS.hide_render=True
REFS.hide_viewport=True

# Collections also expose the assembly structure without traversing every parent.
for cname,prefixes in (
    ('11_Head',('Head_', 'Neck_')),
    ('12_Torso_And_Skirt',('Body_','Armor_','Thorax_','Chest_','Collar_','Waist_','Abdom','Pelvis_','Skirt_')),
    ('13_Arms',('Arm_','Shoulder_','UpperArm_','Forearm_','Elbow_','Wrist_','Hand_')),
    ('14_Legs',('Leg_','Thigh_','Knee_','Shin_','Calf_','Ankle_','Foot_')),
    ('15_Right_Shoulder_Cannon',('Cannon_',)),
    ('16_Folded_Backpack',('Backpack_',))):
    group=coll(cname,MODEL)
    for o in list(MODEL.objects):
        if o.name.startswith(prefixes):
            MODEL.objects.unlink(o); group.objects.link(o)

world=bpy.data.worlds.new('Studio_Light_Grey_World')
world.use_nodes=True
background=next(n for n in world.node_tree.nodes if n.type=='BACKGROUND')
background.inputs['Color'].default_value=(.66,.68,.72,1)
background.inputs['Strength'].default_value=.65
scene.world=world
box('Studio_Floor',(0,0,-.09),(200,200,.16),FLOOR,None,STUDIO,bevel=0)

def aim(obj,at):
    obj.rotation_euler=(Vector(at)-obj.location).to_track_quat('-Z','Y').to_euler()

def area(name,at,power,size,target):
    d=bpy.data.lights.new(name,'AREA');d.energy=power;d.shape='DISK';d.size=size
    o=bpy.data.objects.new(name,d);STUDIO.objects.link(o);o.location=at;aim(o,target)

area('Light_Large_Key',(-3.7,-4.2,6.0),700,4.0,(0,0,1.8))
area('Light_Front_Fill',(3.4,-3.0,4.0),480,3.5,(0,0,1.8))
area('Light_Rear_Fill',(0,4.0,5.6),800,4.0,(0,0,1.8))
area('Light_Top_Soft',(0,0,7.0),350,3.0,(0,0,1.3))

def camera(name,at,target,scale):
    d=bpy.data.cameras.new(name);d.type='ORTHO';d.ortho_scale=scale;d.lens=55
    o=bpy.data.objects.new(name,d);STUDIO.objects.link(o);o.location=at;aim(o,target)
    return o

camera('Camera_FRONT',(-.15,-10,1.73),(-.15,0,1.73),3.90)
camera('Camera_RIGHT_SIDE',(-10,0,1.73),(0,0,1.73),3.90)
camera('Camera_BACK',(-.15,10,1.73),(-.15,0,1.73),3.90)
camera('Camera_THREE_QUARTER',(-6.3,-10.0,5.0),(-.14,0,1.68),4.15)
camera('Camera_DETAIL_HEAD',(-3.5,-7.0,4.3),(-.12,-.035,2.93),1.52)
camera('Camera_DETAIL_BLADE',(-3.1,-8.0,3.5),(-1.07,0,1.35),2.9)
scene.camera=bpy.data.objects['Camera_THREE_QUARTER']
scene.render.engine='CYCLES'
scene.cycles.samples=48
scene.cycles.use_denoising=True
scene.render.resolution_x=1200
scene.render.resolution_y=1500
scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
scene.render.image_settings.color_mode='RGB'
scene.render.film_transparent=False
scene.view_settings.view_transform='AgX'
scene.render.filepath=str(OUT/'renders'/'04_three_quarter.png')
scene.render.image_settings.color_depth='8'

# Leave a neutral material-colour three-quarter inspection viewport in the saved file.
for screen in bpy.data.screens:
    for a in screen.areas:
        if a.type=='VIEW_3D':
            a.spaces.active.shading.type='SOLID'
            a.spaces.active.shading.light='STUDIO'
            a.spaces.active.shading.color_type='MATERIAL'
            a.spaces.active.clip_end=100
            a.spaces.active.region_3d.view_distance=5.5
            a.spaces.active.region_3d.view_location=Vector((-.12,0,1.7))
            a.spaces.active.region_3d.view_rotation=scene.camera.rotation_euler.to_quaternion()
            a.spaces.active.region_3d.view_perspective='ORTHO'
            a.spaces.active.overlay.show_extras=False

for o in scene.objects: o.select_set(False)
ROOT.select_set(True)
bpy.context.view_layer.objects.active=ROOT
source=bpy.data.texts.load(str(OUT/'build_hero.py'))
source.name='build_hero.py | executed source'
scene['reference_decisions']='Front silhouette priority. Folded twin backpack instead of inconsistent deployed views. Ignore printed statistics and markings.'
save('JT_Hero_V01.blend')
with (OUT/'logs'/'build_result.json').open('w',encoding='utf8') as f:
    json.dump({'saved':str(OUT/'JT_Hero_V01.blend'),'blender':bpy.app.version_string,
               'scene':scene.name,'mesh_objects':sum(o.type=='MESH' for o in scene.objects),
               'elapsed_seconds':round(time.time()-START,2),'height_to_crown_m':HEIGHT,
               'blade_total_m':7*S,'blade_ratio':.7},f,ensure_ascii=False,indent=2)
print('JT_BUILD_COMPLETE',str(OUT/'JT_Hero_V01.blend'),flush=True)
