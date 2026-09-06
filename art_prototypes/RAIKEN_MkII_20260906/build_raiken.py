"""RAIKEN Mk-II: editable geometry traced from the supplied weapon sheet.

Blender only. Reference pixels become a real 18.5 m weapon, X points to pommel,
Z is up, -Y is the presentation side. Origin is the centre of the grip.
"""
import bpy, bmesh, math, json, pathlib, sys
from mathutils import Vector, Matrix

P = pathlib.Path(__file__).resolve().parent
S = 18.5 / 985.0
bpy.ops.wm.read_factory_settings(use_empty=True)
sc = bpy.context.scene
sc.unit_settings.system = 'METRIC'
sc.unit_settings.length_unit = 'METERS'
M = {}
COL = {}
PARTS = []

def mat(key, color, metal, rough, emission=0):
    m = bpy.data.materials.new('RAIKEN_' + key)
    m.use_nodes = True
    rgb = [int(color[i:i+2], 16)/255 for i in (0, 2, 4)]
    rgb = [c/12.92 if c <= .04045 else ((c+.055)/1.055)**2.4 for c in rgb]
    m.diffuse_color = (*rgb, 1)
    b = m.node_tree.nodes.get('Principled BSDF')
    b.inputs['Base Color'].default_value = (*rgb, 1)
    b.inputs['Metallic'].default_value = metal
    b.inputs['Roughness'].default_value = rough
    if emission:
        b.inputs['Emission Color'].default_value = (*rgb, 1)
        b.inputs['Emission Strength'].default_value = emission
    M[key] = m
    return m

mat('01_Cobalt_Armor', '214B91', .65, .31)
mat('02_Deep_Navy', '10243D', .65, .33)
mat('03_Gunmetal_Panels', '667385', .77, .29)
mat('04_Machined_Titanium', 'BDCADA', .83, .26)
mat('05_Inner_Frame', '252D38', .78, .32)
mat('06_Recess_And_Grip', '0B1119', .12, .48)
mat('07_Brass_Fasteners', 'AA8E58', .79, .29)
mat('08_Cyan_Emitter', '00C8FF', .22, .22, 3.0)
mat('09_Beam_Sheath', '009BFF', .05, .19, 4.5)
mat('10_Beam_Core', '97F2FF', .02, .16, 6.0)
mat('11_Identification', 'DCE9F7', .12, .38)

blue, navy, panel, silver, frame, black, gold, cyan, beam, core, white = list(M)
for name in ('01 Structure', '02 Layered armor', '03 Cutting edge', '04 Reactor and guard', '05 Grip', '06 Fasteners and markings', '07 Switchable beam', '08 Presentation'):
    c = bpy.data.collections.new('RAIKEN | ' + name)
    sc.collection.children.link(c)
    COL[name[:2]] = c
root = bpy.data.objects.new('RAIKEN_MkII_Grip_Root', None)
COL['01'].objects.link(root)
root['Beam_On'] = True
root['design_length_m'] = 18.5
root['design_name'] = 'RAIKEN Mk-II / anti-ship beam blade'
root['grip_origin'] = 'Centre of grip; blade extends along local -X; front is -Y'
root['reference'] = 'references/RAIKEN_DESIGN.png'
root.empty_display_size = .3

def p(x, z, d=0):
    return Vector(((x-934)*S, -d*S, (201-z)*S))

def mesh(name, vs, fs, material, group='02', bevel=.35):
    me = bpy.data.meshes.new(name + '_Mesh')
    me.from_pydata(vs, [], fs)
    me.update()
    bm = bmesh.new(); bm.from_mesh(me)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    bm.to_mesh(me); bm.free()
    o = bpy.data.objects.new('RK_' + name, me)
    COL[group].objects.link(o); o.parent = root
    me.materials.append(M[material])
    if bevel:
        b = o.modifiers.new('Machined edge bevel', 'BEVEL')
        b.width = bevel*S; b.segments = 2; b.harden_normals = True
        n = o.modifiers.new('Weighted face normals', 'WEIGHTED_NORMAL')
        n.keep_sharp = True
    o['part_role'] = name
    PARTS.append(o)
    return o

def poly(name, xy, front, back, material, group='02', bevel=.35):
    n = len(xy)
    vs = [p(x,z,d) for d in (front, back) for x,z in xy]
    fs = [tuple(range(n-1,-1,-1)), tuple(range(n,2*n))]
    fs += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    return mesh(name,vs,fs,material,group,bevel)

def plate(name, xy, d, thick, material=blue, group='02', bevel=.35, both=True):
    obs=[]
    for sign in ((1,-1) if both else (1,)):
        obs.append(poly(name+('_Front' if sign==1 else '_Rear'),xy,sign*d,sign*(d-thick),material,group,bevel))
    return obs

def rod(name, a, b, r, material, group='01', n=16, r2=None, bevel=.15):
    a=Vector(a); b=Vector(b); q=(b-a).to_track_quat('Z','Y')
    vs=[pt+q@Vector((rr*S*math.cos(i*math.tau/n),rr*S*math.sin(i*math.tau/n),0)) for pt,rr in ((a,r),(b,r if r2 is None else r2)) for i in range(n)]
    fs=[tuple(range(n-1,-1,-1)),tuple(range(n,2*n))]
    fs += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    return mesh(name,vs,fs,material,group,bevel)

def ring(name,a,b,r,ri,material,group='04',n=16,bevel=.15):
    a=Vector(a);b=Vector(b);q=(b-a).to_track_quat('Z','Y')
    vs=[pt+q@Vector((rr*S*math.cos(i*math.tau/n),rr*S*math.sin(i*math.tau/n),0)) for pt,rr in ((a,r),(b,r),(a,ri),(b,ri)) for i in range(n)]
    fs=[]
    for i in range(n):
        j=(i+1)%n
        fs.extend([(i,j,n+j,n+i),(2*n+i,3*n+i,3*n+j,2*n+j),(i,2*n+i,2*n+j,j),(n+i,n+j,3*n+j,3*n+i)])
    return mesh(name,vs,fs,material,group,bevel)

def bolt(name,x,z,d,r=1.4,material=silver,both=True):
    signs=(1,-1) if both else ((1,) if d>=0 else (-1,))
    d=abs(d)
    for sign in signs:
        label=name+('_F' if sign==1 else '_B')
        rod(label+'_Seat',p(x,z,sign*(d-.10)),p(x,z,sign*(d+.15)),r*1.2,black,'06',16,bevel=.03)
        ring(label+'_Hex_Socket',p(x,z,sign*(d+.1)),p(x,z,sign*(d+.65)),r,r*.43,material,'06',6,.06)

def bar(name,coords,d,width,material,group='02',both=True):
    for j in range(len(coords)-1):
        a,b=Vector(coords[j]),Vector(coords[j+1]);v=(b-a).normalized();w=Vector((-v.y,v.x))*width/2
        plate(name+'_%02d'%j,[tuple(a+w),tuple(b+w),tuple(b-w),tuple(a-w)],d,.4,material,group,.07,both)

def rim(name,outer,inner,d,thick,material,group='04'):
    for j in range(len(outer)):
        k=(j+1)%len(outer)
        plate(name+'_%02d'%j,[outer[j],outer[k],inner[k],inner[j]],d,thick,material,group,.3)

def label(name,body,x,z,d,size,material=white):
    for sign in (1,-1):
        cu=bpy.data.curves.new(name,'FONT');cu.body=body;cu.size=size*S
        cu.extrude=.018*S;cu.bevel_depth=0;cu.space_character=1.15
        ob=bpy.data.objects.new('RK_'+name+('_F' if sign==1 else '_B'),cu)
        COL['06'].objects.link(ob);ob.parent=root
        ob.location=p(x,z,sign*d)
        if sign==1:ob.rotation_euler=(math.pi/2,0,0)
        else:ob.rotation_euler=(math.pi/2,0,math.pi)
        cu.materials.append(M[material]);PARTS.append(ob)

# Continuous asymmetric load-bearing blade. Broad base tapers into the hooked point.
stations=[(15,177,177.2,.05),(92,174,199,2.4),(208,172,219,4.5),(478,167,237,7.4),(665,159,247,10.2),(748,157,253,12),(783,179,240,13)]
vs=[]
for x,top,bot,dep in stations:
    mid=top+(bot-top)*.60
    vs += [p(x,top,0),p(x,top+2,dep*.75),p(x,mid,dep),p(x,bot-3,dep*.35),p(x,bot,0),p(x,bot-3,-dep*.35),p(x,mid,-dep),p(x,top+2,-dep*.75)]
fs=[tuple(range(7,-1,-1)),tuple(range(48,56))]
for j in range(len(stations)-1):
    for k in range(8):fs.append((j*8+k,j*8+(k+1)%8,(j+1)*8+(k+1)%8,(j+1)*8+k))
mesh('Continuous_Asymmetric_Structural_Blade',vs,fs,frame,'01',.12)

# One uninterrupted, ground metal edge with an actual wedge cross section.
edge=[(15,177),(105,204),(205,220),(508,239),(749,255),(791,267)]
inner=[(35,180),(109,195),(210,210),(514,226),(755,242),(771,248)]
vs=[]
for (x,z),(a,b) in zip(edge,inner):vs += [p(x,z,0),p(a,b,3.2),p(a,b,-3.2)]
fs=[(2,1,0),(15,16,17)]
for j in range(len(edge)-1):
    for k in range(3):fs.append((j*3+k,j*3+(k+1)%3,(j+1)*3+(k+1)%3,(j+1)*3+k))
mesh('Single_Continuous_Titanium_Cutting_Bevel',vs,fs,silver,'03',.06)

# Long dark receiver bed and inset load strip; visible gaps between armor leaves.
plate('Forward_Blade_Bed',[(78,180),(107,172),(269,171),(305,211),(207,215),(110,195)],3.5,3.5,navy)
plate('Middle_Blade_Bed',[(260,172),(480,167),(532,229),(305,214)],6.8,4.0,navy)
plate('Aft_Blade_Bed',[(478,167),(668,157),(748,158),(776,179),(770,236),(750,247),(510,232)],9.3,4.0,navy)
plate('Central_Mechanical_Channel',[(201,185),(552,185),(605,213),(745,225),(749,239),(305,214),(220,203)],6.0,2.2,black)
bar('Brass_Power_Bus',[(219,200),(511,223),(744,239)],10.7,1.4,gold,'01')

# Distinct non-repeating top armor plates and silver service leaves.
armor=[
 ('Tip_Blue_Cheek',[(78,180),(107,175),(171,175),(208,209),(200,216),(115,200)],4.2,blue),
 ('Tip_Silver_Cap',[(107,174),(173,174),(193,193),(181,199),(113,188),(101,180)],5.9,panel),
 ('Forward_Blue_Leaf',[(184,173),(260,172),(287,201),(225,201),(212,195)],7.5,blue),
 ('Forward_Lower_Return',[(221,205),(295,208),(312,214),(423,219),(411,211),(291,197)],9.9,blue),
 ('Long_Spine_Silver_Plate',[(276,173),(463,171),(480,184),(376,187),(289,188)],10.5,panel),
 ('Mid_Blue_Upper_Leaf',[(298,191),(376,190),(389,198),(414,200),(429,216),(422,221),(319,213)],10.9,navy),
 ('Mid_Lower_Blue_Leaf',[(427,201),(537,207),(557,224),(516,229),(439,222)],11.1,blue),
 ('Central_Silver_Service_Armor',[(465,169),(535,168),(557,181),(545,193),(492,191)],12.4,panel),
 ('Central_Upper_Armor',[(542,169),(591,168),(576,183),(557,181)],12.8,blue),
 ('Mid_Diagonal_Silver_Lock',[(558,194),(579,183),(618,183),(633,215),(605,215),(583,211)],13.6,panel),
 ('Lower_Receiver_Silver_Leaf',[(637,222),(697,223),(719,241),(641,237),(627,229)],14.0,panel),
 ('Root_Upper_Spine_Leaf',[(603,164),(671,163),(680,156),(692,155),(682,170),(658,177),(594,179)],13.4,blue),
 ('Root_Top_Silver_Clip',[(672,162),(683,157),(744,155),(752,160),(738,167),(684,168)],14.1,panel),
 ('Root_Blue_Nameplate',[(626,182),(658,172),(746,168),(776,182),(765,215),(747,224),(641,217)],16.4,blue),
 ('Root_Bottom_Blue_Return',[(724,224),(745,228),(771,245),(757,249),(723,241)],14.8,blue),
 ('Root_Lower_Navy_Keel',[(603,219),(624,219),(638,236),(623,235),(611,232)],12.9,navy),
]
for name,pts,depth,material in armor:
    plate(name+'_Dark_Gasket',pts,depth-.45,1.9,black,bevel=.48)
    # Slightly inset top face leaves an intentional rubber seam around each leaf.
    center=sum((Vector(pt) for pt in pts),Vector((0,0)))/len(pts)
    inset=[tuple(center+(Vector(pt)-center)*.974) for pt in pts]
    plate(name,inset,depth+1.1,1.7,material,bevel=.33)

bar('Top_Machined_Spine_Edge',[(103,173),(460,168),(532,165)],12.0,1.3,silver)
bar('Root_Spine_Edge',[(598,163),(669,161),(684,155),(741,154)],14.6,1.2,silver)
bar('Tip_Panel_Groove',[(115,190),(174,201),(185,207)],6.2,.75,black)
# Secondary recessed service cartridges run through the dark channel between armor.
for j,(x,z,le,dep) in enumerate(((344,198,58,10.5),(457,197,49,11.5),(529,199,30,12.8),(586,221,34,13.3))):
    plate('Channel_Service_Cartridge_%02d'%j,[(x-le/2,z-2),(x+le/2-5,z-2),(x+le/2,z+4),(x-le/2+4,z+3)],dep,2.3,frame,'01',.32)
    bar('Service_Cartridge_Trim_%02d'%j,[(x-le/2+5,z-1),(x+le/2-6,z-1)],dep+.5,.7,panel,'01')
    for a in (-1,1):bolt('Cartridge_Pin_%s_%s'%(j,a),x+a*(le/2-7),z+1,dep+.8,.75,gold)
for j,(coords,dep) in enumerate((([(311,194),(361,195),(368,201)],12.3), ([(437,207),(491,210),(499,216)],12.8), ([(501,181),(535,182),(543,187)],14), ([(655,227),(691,229),(702,236)],15.8))):
    bar('Armor_Inset_Seam_%02d'%j,coords,dep,.7,navy)
for j,(x,z,dep) in enumerate(((173,180,7),(217,182,9),(282,180,11),(376,179,12),(479,181,14),(539,177,15),(587,196,16),(641,229,16),(698,231,16),(746,178,19))):
    bolt('Armor_Captive_Pin_%02d'%j,x,z,dep,1.15)
for j,(x,z) in enumerate(((272,173),(375,175),(547,180),(614,220),(735,239))):
    plate('Brass_Dovetail_Latch_%02d'%j,[(x-4,z-1),(x+6,z-1),(x+8,z+2),(x-2,z+2)],13.2,1.7,gold,'06',.15)

# Spine seen in top view: a structural rail, interrupting ribs and keyed cartridges.
for j,(x,zz,le) in enumerate(((250,184,44),(352,182,54),(447,180,46),(543,178,35),(639,173,38),(717,170,31))):
    rod('Internal_Longitudinal_Rail_%02d'%j,p(x-le/2,zz,0),p(x+le/2,zz,0),5.2,frame,n=8,bevel=.2)
    for a in (-1,1):
        plate('Exposed_Spine_Clamp_%s_%s'%(j,a),[(x+a*le/2-2,zz-9),(x+a*le/2+2,zz-9),(x+a*le/2+3,zz+8),(x+a*le/2-1,zz+8)],11.6,2.0,panel,'01',.24)

# Two-part energy geometry only along the cutting side, including the hooked tip.
outer=[(14.8,177),(103,208),(205,225),(510,244),(752,260),(794,269)]
for material,coords1,coords2,depth,nm in (
    (beam,edge,outer,1.65,'Blue_Plasma_Envelope'),
    (core,[(x+(a-x)*.32,z+(b-z)*.32) for (x,z),(a,b) in zip(edge,outer)],[(x+(a-x)*.51,z+(b-z)*.51) for (x,z),(a,b) in zip(edge,outer)],1.2,'Cyan_White_Edge_Core')):
    points=coords1+list(reversed(coords2))
    ob=poly(nm,points,depth,-depth,material,'07',.045)
    ob['beam_component']=True
    for path in ('hide_render','hide_viewport'):
        dr=ob.driver_add(path).driver;dr.expression='not beam'
        v=dr.variables.new();v.name='beam';v.type='SINGLE_PROP';v.targets[0].id=root;v.targets[0].data_path='["Beam_On"]'

# Remaining assemblies and presentation are kept separate for easy revision.
exec(compile((P/'hilt_and_presentation.py').read_text(encoding='utf-8'),str(P/'hilt_and_presentation.py'),'exec'))
