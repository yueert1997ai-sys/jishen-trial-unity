"""Reference-led E-01 corruption sculpture. All generated meshes are real editable geometry.
This is the first modeling approval gate, not a finished game export or animation rig.
"""
from pathlib import Path
import bpy, bmesh, math, random, json, hashlib, shutil, time
from mathutils import Vector, Matrix

ROOT = Path(__file__).resolve().parent
OUT = ROOT/'stage_01'
OUT.mkdir(exist_ok=True)
(OUT/'renders').mkdir(exist_ok=True)
(ROOT/'reference').mkdir(exist_ok=True)
SOURCE = ROOT.parent/'Type_E01_20260906/stage_04_TYPE01/TYPE_E01_TYPE01_RIFLE_MASTER.blend'
REFERENCE = Path('C:/Users/yue/Downloads/ChatGPT Image 2026年9月6日 22_34_17.png')
REF = ROOT/'reference/TYPE_E01_ELITE_USER_REFERENCE.png'
if not REF.exists(): shutil.copy2(REFERENCE, REF)
source_hash = hashlib.sha256(SOURCE.read_bytes()).hexdigest()
bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
source_scene = bpy.data.scenes['01 NEUTRAL - editable assembly']
bpy.context.window.scene = source_scene
bpy.context.view_layer.update()
deps = bpy.context.evaluated_depsgraph_get()
body_cols = [bpy.data.collections[x] for x in ('01 HEAD - review secondary forms','02 BODY - primary proportions only','03 BACKPACK - primary masses')]
source_objects = [o for c in body_cols for o in c.objects if o.type in {'MESH','FONT'}]
rest_roots = {o.name:o.matrix_world.translation.copy() for o in source_scene.objects if o.type=='EMPTY' and o.name.startswith('E01_') and not any(x in o.name for x in ('RIFLE','TYPE01'))}
originals = []
for obj in source_objects:
    me = bpy.data.meshes.new_from_object(obj.evaluated_get(deps), preserve_all_data_layers=True, depsgraph=deps)
    me.transform(obj.matrix_world)
    originals.append((obj.name, me, obj.parent.name if obj.parent else 'E01_CHEST'))

sc = bpy.data.scenes.new('01 ELITE - editable corruption assembly')
sc.use_fake_user = True
bpy.context.window.scene = sc
for other in list(bpy.data.scenes):
    if other != sc: bpy.data.scenes.remove(other)
# Keep just the newly baked owned geometry; no source scene or source rifle remains.
for obj in list(bpy.data.objects): bpy.data.objects.remove(obj,do_unlink=True)
for col in list(bpy.data.collections): bpy.data.collections.remove(col)
sc.unit_settings.system = 'METRIC'
sc.render.engine = 'CYCLES'
sc.cycles.samples = 40
sc.cycles.use_denoising = True
sc.cycles.max_bounces = 6
sc.cycles.transparent_max_bounces = 4
sc.render.image_settings.file_format = 'PNG'
sc.render.image_settings.color_mode = 'RGB'
sc.render.resolution_percentage = 100
sc.render.threads_mode = 'FIXED'
sc.render.threads = 8
sc.view_settings.view_transform = 'AgX'
sc.view_settings.look = 'AgX - Medium High Contrast'
sc.view_settings.exposure = -.20
sc.render.film_transparent = False
sc.render.filepath = str(OUT/'renders')+'/'
rng = random.Random(10601)

def collection(name):
    c = bpy.data.collections.new(name); sc.collection.children.link(c); return c
ARMOR = collection('01 WHITE ARMOR - inherited and fractured')
FRAME = collection('02 E01 MECHANICAL ANCESTRY')
HEAD = collection('03 HALF BREACHED HEAD')
CORE = collection('04 LIVING THORAX AND RED CORE')
CLAW = collection('05 RIGHT ARM - layered living blades')
BACK = collection('06 DORSAL GROWTH - six primary tendrils')
LEGS = collection('07 HIP AND LEG INFILTRATION')
PARTS = collection('08 EDITABLE PART PIVOTS - not animation rig')
STUDIO = collection('90 STUDIO')
CAMERAS = collection('91 REVIEW CAMERAS')
REFERENCES = collection('99 PACKED REFERENCE'); REFERENCES.hide_render=True

def empty(name, loc=(0,0,0), parent=None):
    o=bpy.data.objects.new(name,None); PARTS.objects.link(o); o.location=loc
    o.empty_display_type='PLAIN_AXES'; o.empty_display_size=.06
    if parent:o.parent=parent
    return o
master=empty('TYPE_E01_ELITE_MASTER')
parts={name:empty('ELITE | '+name, loc,master) for name,loc in rest_roots.items()}
bpy.context.view_layer.update()
head=parts['E01_HEAD']; chest=parts['E01_CHEST']; waist=parts['E01_WAIST']; pack=parts['E01_BACKPACK']
arm=parts['E01_R_FOREARM']

def material(name, color, metal=0, rough=.4, emit=0):
    m=bpy.data.materials.new('ELITE | '+name); m.use_nodes=True
    p=next((n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None)
    if p is None:p=m.node_tree.nodes.new('ShaderNodeBsdfPrincipled')
    p.name='Principled BSDF'
    output=next((n for n in m.node_tree.nodes if n.type=='OUTPUT_MATERIAL'),None)
    if output is None:output=m.node_tree.nodes.new('ShaderNodeOutputMaterial')
    if not output.inputs['Surface'].is_linked:m.node_tree.links.new(p.outputs['BSDF'],output.inputs['Surface'])
    p.inputs['Base Color'].default_value=(*color,1)
    p.inputs['Metallic'].default_value=metal; p.inputs['Roughness'].default_value=rough
    if emit:
        p.inputs['Emission Color'].default_value=(*color,1); p.inputs['Emission Strength'].default_value=emit
    m.diffuse_color=(*color,1)
    return m
white=material('worn ivory ceramic paint',(.43,.455,.446),.17,.43)
alloy=material('exposed fractured alloy',(.16,.18,.185),.86,.30)
mechanic=material('original gunmetal frame',(.033,.042,.049),.73,.32)
rubber=material('black joint seals',(.009,.013,.016),.1,.55)
liquid=material('black liquid titanium',(.010,.013,.016),.79,.29)
ridge=material('polished living blade ridges',(.024,.029,.034),.88,.26)
recess=material('deep red interstitial membrane',(.032,.0008,.0015),.72,.28)
red=material('narrow vermilion life veins',(.70,.0015,.0025),.25,.26,3.5)
hot=material('red core and blade edge energy',(.95,.014,.008),.18,.22,6.5)
glass=material('red optical emitter',(.63,.001,.002),.25,.19,3.0)
core_energy=material('deep vermilion recessed core',(.43,.0001,.0004),.34,.24,2.5)

# Paint chips have several scales. Large seams and fracture lips are geometry below.
n=white.node_tree.nodes; l=white.node_tree.links; p=n.get('Principled BSDF')
coord=n.new('ShaderNodeTexCoord'); coord.object=master
noise=n.new('ShaderNodeTexNoise'); noise.inputs['Scale'].default_value=165; noise.inputs['Detail'].default_value=2.8; noise.inputs['Roughness'].default_value=.78
l.new(coord.outputs['Object'],noise.inputs['Vector'])
ramp=n.new('ShaderNodeValToRGB'); ramp.color_ramp.elements[0].position=.325; ramp.color_ramp.elements[0].color=(.025,.03,.033,1); ramp.color_ramp.elements[1].position=.407; ramp.color_ramp.elements[1].color=(.43,.455,.446,1)
l.new(noise.outputs['Fac'],ramp.inputs[0]);l.new(ramp.outputs['Color'],p.inputs['Base Color'])
micro=n.new('ShaderNodeTexNoise');micro.inputs['Scale'].default_value=1050;micro.inputs['Detail'].default_value=2
l.new(coord.outputs['Object'],micro.inputs['Vector'])
bump=n.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.12;bump.inputs['Distance'].default_value=.00025
l.new(micro.outputs['Fac'],bump.inputs['Height']);l.new(bump.outputs['Normal'],p.inputs['Normal'])
for mat in (liquid,ridge):
    n=mat.node_tree.nodes;l=mat.node_tree.links;p=n.get('Principled BSDF')
    p.inputs['Coat Weight'].default_value=.11;p.inputs['Coat Roughness'].default_value=.23
    anisotropic=p.inputs.get('Anisotropic') or p.inputs.get('Anisotropic IOR Level')
    if anisotropic is not None:anisotropic.default_value=.28
    tex=n.new('ShaderNodeTexNoise');tex.name='Living surface flow - animate W after rig approval';tex.noise_dimensions='4D';tex.inputs['Scale'].default_value=17;tex.inputs['Detail'].default_value=3
    tex.inputs['W'].default_value=.42
    b=n.new('ShaderNodeBump');b.inputs['Strength'].default_value=.075;b.inputs['Distance'].default_value=.0022
    l.new(tex.outputs['Fac'],b.inputs['Height']);l.new(b.outputs['Normal'],p.inputs['Normal'])
    r=n.new('ShaderNodeMapRange');r.inputs['To Min'].default_value=.255;r.inputs['To Max'].default_value=.365
    l.new(tex.outputs['Fac'],r.inputs['Value']);l.new(r.outputs['Result'],p.inputs['Roughness'])

def parent_to(o, parent):
    if parent:
        o.parent=parent;o.matrix_parent_inverse=parent.matrix_world.inverted()
    return o

owned=[]
def mesh(name, verts, faces, mat, col, parent, smooth=False, bevel=0):
    me=bpy.data.meshes.new(name+' mesh');me.from_pydata(verts,[],faces);me.update()
    bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free()
    o=bpy.data.objects.new(name,me);col.objects.link(o);me.materials.append(mat)
    for f in me.polygons:f.use_smooth=smooth
    parent_to(o,parent);owned.append(o)
    if bevel:
        mod=o.modifiers.new('Small real edge radii','BEVEL');mod.width=bevel;mod.segments=2
        norm=o.modifiers.new('Face weighted corner normals','WEIGHTED_NORMAL');norm.keep_sharp=True
    return o

def plate(name, points, depth, mat=white, col=ARMOR, parent=chest):
    nn=len(points);vv=[tuple(x) for x in points]+[tuple(Vector(x)+Vector(depth)) for x in points]
    faces=[tuple(range(nn-1,-1,-1)),tuple(range(nn,nn*2))]+[(i,(i+1)%nn,(i+1)%nn+nn,i+nn) for i in range(nn)]
    return mesh(name,vv,faces,mat,col,parent,False,.0014)

def ellipsoid(name, center, scale, mat, col, parent, seg=32, rings=18):
    verts=[];faces=[];center=Vector(center)
    for j in range(rings+1):
        phi=math.pi*j/rings
        for i in range(seg):
            theta=2*math.pi*i/seg
            verts.append(tuple(center+Vector((math.sin(phi)*math.cos(theta)*scale[0],math.sin(phi)*math.sin(theta)*scale[1],math.cos(phi)*scale[2]))))
    for j in range(rings):
        for i in range(seg):faces.append((j*seg+i,j*seg+(i+1)%seg,(j+1)*seg+(i+1)%seg,(j+1)*seg+i))
    return mesh(name,verts,faces,mat,col,parent,True)

def sample_path(points, steps=60):
    pp=[Vector(p) for p in points];out=[];count=len(pp)-1
    for k in range(steps):
        t=k/(steps-1)*count;i=min(count-1,int(t));u=t-i
        a=pp[max(0,i-1)];b=pp[i];c=pp[i+1];d=pp[min(count,i+2)]
        out.append((2*b+(-a+c)*u+(2*a-5*b+4*c-d)*u*u+(-a+3*b-3*c+d)*u*u*u)*.5)
    return out

def interpolate(values,t):
    f=t*(len(values)-1);i=min(len(values)-2,int(f));return values[i]+(values[i+1]-values[i])*(f-i)

def sweep(name,points,widths,mat=liquid,col=CORE,parent=chest,depth=.53,steps=56,sides=12,twist=0,vein=False,vein_width=.0023,phase=0,ridged=True):
    pts=sample_path(points,steps);vs=[];fs=[];frames=[]
    for j,pos in enumerate(pts):
        tangent=(pts[min(j+1,steps-1)]-pts[max(0,j-1)]).normalized()
        axis=Vector((0,-1,0))
        if abs(axis.dot(tangent))>.93:axis=Vector((1,0,0))
        v=(axis-tangent*axis.dot(tangent)).normalized();u=v.cross(tangent).normalized()
        t=j/(steps-1);a=twist*t+phase
        uu=u*math.cos(a)+v*math.sin(a);vv=-u*math.sin(a)+v*math.cos(a)
        w=max(.00035,interpolate(widths,t));frames.append((uu,vv,w))
        for i in range(sides):
            angle=math.tau*i/sides
            k=1+.13*math.cos(3*angle+t*5) if ridged else 1
            vs.append(tuple(pos+uu*(w*math.cos(angle)*k)+vv*(w*depth*math.sin(angle)*k)))
    for j in range(steps-1):
        for i in range(sides):fs.append((j*sides+i,j*sides+(i+1)%sides,(j+1)*sides+(i+1)%sides,(j+1)*sides+i))
    fs.extend([tuple(range(sides-1,-1,-1)),tuple((steps-1)*sides+i for i in range(sides))])
    ob=mesh(name,vs,fs,mat,col,parent,True)
    ob['form']='Solid tapered living-metal strand; editable mesh; pointed growth terminal'
    if vein:
        for sign in (-1,1):
            vpts=[]
            for j in range(0,steps,3):
                uu,vv,w=frames[j];t=j/(steps-1)
                vpts.append(pts[j]+vv*(w*depth*.985*sign)+uu*(math.sin(t*math.pi*3+phase)*w*.18))
            sweep(name+' | embedded red fissure '+str(sign),vpts,[vein_width*.28,vein_width,vein_width*.75,.00025],red,col,parent,.8,steps,6,0,False,ridged=False)
    return ob

def bundle(name,points,width=.045,count=6,spread=.07,col=CORE,parent=chest,seed=0):
    local=random.Random(1700+seed)
    for i in range(count):
        pp=[];a=math.tau*i/count+local.uniform(-.18,.18)
        for j,p in enumerate(points):
            t=j/(len(points)-1);q=Vector(p)
            q.x+=math.cos(a+t*1.7)*spread*(.65+.35*math.sin(t*math.pi))
            q.y+=math.sin(a+t*2.1)*spread*.64
            q.z+=local.uniform(-.018,.018)
            pp.append(q)
        w=width*local.uniform(.6,1.25)
        sweep(name+' / flow '+str(i+1),pp,[w*.5,w,w*.8,w*.03],liquid if i%4 else ridge,col,parent,local.uniform(.36,.65),60,12,local.uniform(-.7,.7),i%3==0,.0022,local.random()*2)

source_name_map={}
omitted=[]
for name, me, pn in originals:
    # Right forearm and hand no longer have standard external geometry.
    if pn in ('E01_R_FOREARM','E01_R_HAND') or (name.startswith('L H02') and any(x in name for x in ('ear armored','ear recessed','ear perimeter','jaw side','angular facial','flush screw','forehead narrow'))):
        omitted.append(name);bpy.data.meshes.remove(me);continue
    if any(x in name for x in ('Abdominal armored segmented','abdomen center dark service','compact internal skull')):
        omitted.append(name);bpy.data.meshes.remove(me);continue
    new_mats=[]
    for ma in me.materials:
        mn=ma.name.lower() if ma else ''
        if any(x in mn for x in ('warm grey-white','warm gray-white')):new_mats.append(white)
        elif any(x in mn for x in ('red optical','red recessed','optical emitter','optical glass','red mono')):new_mats.append(glass)
        elif 'machined' in mn:new_mats.append(alloy)
        elif any(x in mn for x in ('seals','rubber')):new_mats.append(rubber)
        else:new_mats.append(mechanic)
    me.materials.clear()
    for ma in new_mats or [mechanic]:me.materials.append(ma)
    is_head=pn=='E01_HEAD'
    col=HEAD if is_head else ARMOR if white in me.materials[:] else FRAME
    obj=bpy.data.objects.new('E01 ancestry | '+name,me);col.objects.link(obj)
    parent_to(obj,parts.get(pn,chest));owned.append(obj);source_name_map[name]=obj
    obj['source_part']=name

# Remove parts of the real helmet rather than drawing black paint over a white head.
def breach(obj, threshold):
    bm=bmesh.new();bm.from_mesh(obj.data)
    bmesh.ops.triangulate(bm,faces=list(bm.faces))
    faces=[f for f in bm.faces if threshold(f.calc_center_median())]
    bmesh.ops.delete(bm,geom=faces,context='FACES')
    loose=[v for v in bm.verts if not v.link_faces]
    if loose:bmesh.ops.delete(bm,geom=loose,context='VERTS')
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(obj.data);bm.free()
    obj['breached']='Real removed armor faces with exposed internal sculpture'

for key in ('H02 continuous swept helmet crown','H02 inset planar forehead shield'):
    obj=source_name_map.get(key)
    if obj:breach(obj,lambda p:p.x>.012+.022*math.sin(p.y*34)+.009*math.sin(p.z*76))
for key in ('L pectoral armor lobe','L chest side wrap'):
    obj=source_name_map.get(key)
    if obj:breach(obj,lambda p:p.z<2.62 and p.x<.34)

ellipsoid('HUSK | exposed left cranial substrate',(.060,.012,3.074),(.095,.132,.14),liquid,HEAD,head)
for i in range(12):
    z=2.95+i*.022
    sweep('HUSK | layered cranial lamella '+str(i),[(.062,-.164,z),(.112,-.109,z+.040),(.154,.007,z+.02),(.117,.116,z-.02)], [.015,.024,.018,.001],ridge if i%4==0 else liquid,HEAD,head,.32,38,10,vein=i in (3,7,10),vein_width=.0018)
bundle('HUSK | neck tendons',[(.088,.069,2.80),(.144,-.015,2.95),(.131,-.095,3.12),(.199,-.007,3.30)],.022,6,.032,HEAD,head,10)
for i,(endx,endz) in enumerate(((.19,3.46),(.33,3.36),(.28,3.22))):
    sweep('HUSK | broken temple blade '+str(i),[(.079,-.055,3.087),(.124,-.116,3.19),(.19,-.047,3.29),(endx,.02,endz)],[.013,.025,.018,.0004],liquid,HEAD,head,.29,60,12,.5,True,.0018)
plate('HUSK | fractured forehead remaining corner',[(-.025,-.193,3.131),(.004,-.200,3.146),(.027,-.186,3.201),(-.008,-.178,3.206),(-.013,-.184,3.172)],(0,.012,0),white,HEAD,head)
plate('HUSK | lower jaw surviving splinter',[(.031,-.189,2.866),(.058,-.176,2.907),(.053,-.173,2.946),(.038,-.187,2.923)],(.009,.012,0),white,HEAD,head)
# Bright, deeply seated monoeye remains the sole forward eye.
ellipsoid('HUSK | retained red monoeye hotspot',(0,-.201,3.018),(.014,.0028,.014),hot,HEAD,head,32,16)

# Organic thorax: interwoven load-bearing plates, not a uniformly ribbed exoskeleton.
bundle('THORAX | right shoulder into abdominal nexus',[(-.40,-.025,2.79),(-.35,-.230,2.61),(-.21,-.317,2.43),(-.075,-.317,2.23),(.012,-.217,2.04)],.051,9,.067,CORE,chest,20)
bundle('THORAX | exposed left parasitic channel',[(.108,.016,2.94),(.224,-.202,2.76),(.228,-.316,2.56),(.143,-.345,2.38),(.044,-.304,2.26)],.042,8,.057,CORE,chest,21)
bundle('THORAX | lower oblique sweep',[(-.279,-.004,2.34),(-.234,-.179,2.27),(-.118,-.269,2.14),(.03,-.195,1.95)],.037,6,.050,CORE,waist,22)
bundle('THORAX | left waist sweep',[(.302,.018,2.42),(.257,-.177,2.31),(.176,-.250,2.12),(.074,-.145,1.90)],.034,6,.045,CORE,waist,23)
for sign in (-1,1):
    for i in range(6):
        z=2.17+i*.077
        pp=[(sign*(.28+i*.012),.028,z+.13),(sign*.268,-.155,z+.092),(sign*.160,-.307,z),(sign*.060,-.298,z-.11)]
        sweep('THORAX | interleaved exposed rib '+str(sign)+'-'+str(i),pp,[.01,.041,.032,.0005],liquid,CORE,chest,.34,52,12,.2*sign,i%3==0,.0020)

corepos=Vector((.024,-.357,2.302))
ellipsoid('CORE | dark molten socket',corepos+Vector((0,.028,0)),(.127,.064,.151),recess,CORE,chest)
ellipsoid('CORE | recessed vermilion nucleus',corepos+Vector((0,-.044,0)),(.038,.012,.056),core_energy,CORE,chest,48,28)
ellipsoid('CORE | inner saturated light',corepos+Vector((0,-.054,0)),(.009,.003,.022),red,CORE,chest,40,24)
for i in range(9):
    ang=math.tau*i/9
    pp=[]
    for j in range(5):
        a=ang+j*.32;r=.125-j*.014
        pp.append(corepos+Vector((math.cos(a)*r,-.012-j*.006,math.sin(a)*r*1.18)))
    sweep('CORE | overlapping iris blade '+str(i+1),pp,[.028,.040,.022,.0006],ridge if i%3==0 else liquid,CORE,chest,.27,45,12,.4,True,.0027)
for i in range(11):
    angle=rng.uniform(0,math.tau)
    x=corepos.x+math.cos(angle)*.062;z=corepos.z+math.sin(angle)*.08
    endx=math.cos(angle)*rng.uniform(.18,.28);endz=corepos.z+math.sin(angle)*rng.uniform(.2,.3)
    sweep('CORE | branching arterial fissure '+str(i),[(x,-.407,z),((x+endx)*.5,-.342,(z+endz)*.5),(endx,-.259,endz)],[.006,.004,.0002],red,CORE,chest,.65,32,8,vein=False)
sweep('CORE | lower asymmetrical iris shutter',[(corepos.x-.109,-.352,2.355),(corepos.x-.060,-.408,2.286),(corepos.x+.010,-.421,2.251),(corepos.x+.086,-.361,2.204)],[.018,.022,.012,.0002],liquid,CORE,chest,.32,48,12,.4,vein=True,vein_width=.0016)

# Deliberate fractured white remnants along the breastplate / invasion boundary.
fractures=[
 [(-.149,-.350,2.610),(-.106,-.362,2.496),(-.090,-.356,2.526),(-.070,-.362,2.478),(-.046,-.335,2.591)],
 [(.172,-.298,2.713),(.257,-.287,2.723),(.285,-.304,2.638),(.244,-.322,2.628),(.233,-.308,2.669),(.199,-.319,2.650)],
 [(.226,-.275,2.530),(.294,-.264,2.578),(.325,-.246,2.519),(.288,-.265,2.469),(.283,-.282,2.505)],
 [(-.176,-.247,2.139),(-.111,-.263,2.143),(-.075,-.235,2.025),(-.121,-.237,2.052),(-.136,-.248,2.079)]
]
for i,pts in enumerate(fractures):
    plate('BREACH | real exposed alloy lip '+str(i),[(x,y+.005,z) for x,y,z in pts],(0,.021,0),alloy,CORE,chest)
    plate('BREACH | torn white lamina '+str(i),pts,(0,.014,0),white,CORE,chest)

# One outsized right forearm: layered tendon mass with six long interlocking blade fingers.
ellipsoid('CLAW | proximal living muscle',(-.622,-.010,2.251),(.143,.120,.251),liquid,CLAW,arm)
ellipsoid('CLAW | descending interstitial body',(-.769,-.080,1.909),(.145,.10,.368),liquid,CLAW,arm)
ellipsoid('CLAW | concealed palm substrate',(-.860,-.092,1.561),(.133,.088,.188),liquid,CLAW,arm)
bundle('CLAW | shoulder to outside elbow',[(-.504,.062,2.704),(-.644,-.065,2.492),(-.729,-.163,2.214),(-.858,-.145,1.803)],.045,8,.067,CLAW,arm,31)
for i in range(13):
    a=math.tau*i/13
    pp=[]
    for j,(x,z) in enumerate(((-.608,2.41),(-.697,2.14),(-.800,1.87),(-.857,1.58),(-.920,1.31))):
        rad=.116+.065*math.sin(j*math.pi/4)
        pp.append((x+math.cos(a+j*.58)*rad,-.068+math.sin(a+j*.58)*rad*.79,z+.026*math.sin(i+j)))
    sweep('CLAW | twisting longitudinal blade mass '+str(i+1),pp,[.019,.046,.042,.024,.0005],ridge if i%5==0 else liquid,CLAW,arm,.44,70,12,rng.uniform(-.6,.6),i%3==0,.0028)

# Broad interlocking molten lamellae interrupt the long tendons and enclose the wrist.
# Vary their paths, overlap, width, and tip direction so the limb has continuous living mass.
for i in range(58):
    t=rng.uniform(.03,.98);z=2.48-t*1.13;cx=-.602-t*.304
    a=math.tau*((i*.61803398875)%1)
    rad=.147+.035*math.sin(t*math.pi)
    xx=cx+math.cos(a)*rad;yy=-.072+math.sin(a)*rad*.89
    length=rng.uniform(.28,.54);sw=rng.uniform(-.1,.075)
    normal=Vector((math.cos(a),math.sin(a)*.89,0))
    start=Vector((xx,yy,z));mid=start+Vector((-.030+sw*.5,-.01,-length*.32))+normal*.032
    lower=start+Vector((-.065+sw,-.022,-length*.76))+normal*.010
    tip=start+Vector((-.105+sw*1.2,-.033,-length))+normal*rng.uniform(.025,.085)
    width=rng.uniform(.039,.073)
    sweep('CLAW | interlocking molten blade lamella %02d'%i,[start,mid,lower,tip],[.014,width,width*.49,.00025],ridge if i%7==0 else liquid,CLAW,arm,.31,55,12,rng.uniform(-.7,.7),i%4==0,rng.uniform(.0015,.0032),phase=a*.21)

# Fine, shorter ribbons connect the broad blade surfaces into a dense intertwined material.
for i in range(74):
    t=rng.uniform(.05,.98);z=2.48-t*1.15;cx=-.602-t*.304
    a=math.tau*((i*.61803398875+.22)%1);rad=.175+.025*math.sin(t*math.pi)
    start=Vector((cx+math.cos(a)*rad,-.074+math.sin(a)*rad*.94,z))
    normal=Vector((math.cos(a),math.sin(a),0));span=rng.uniform(.12,.29)
    pts=[start,start+normal*.012+Vector((.042,-.006,-span*.34)),start+Vector((-.02,-.018,-span*.74)),start+Vector((-.055,-.019,-span))]
    sweep('CLAW | fine crossing filament '+str(i),pts,[.003,.012,.010,.0002],liquid if i%6 else ridge,CLAW,arm,.32,38,10,rng.uniform(-.5,.5),i%7==0,.0014)

# A dark angular mantle replaces the visual ball at the fingers' shared root.
for i in range(14):
    a=math.tau*i/14
    pp=[(-.840+math.cos(a)*.15,-.087+math.sin(a)*.14,1.79),(-.915+math.cos(a+.3)*.18,-.082+math.sin(a+.3)*.16,1.59),(-.983+math.cos(a+.6)*.13,-.092+math.sin(a+.6)*.11,1.33),(-1.04+math.cos(a+.5)*.10,-.10+math.sin(a+.5)*.10,1.14)]
    sweep('CLAW | wrist blade mantle '+str(i),pp,[.023,.061,.029,.0002],liquid,CLAW,arm,.28,60,12,.3,vein=i%4==0,vein_width=.0023)

fingers=[
 [(-.774,-.120,1.74),(-.787,-.185,1.46),(-.728,-.216,1.16),(-.665,-.237,.934)],
 [(-.876,-.091,1.70),(-.980,-.126,1.39),(-1.074,-.154,1.08),(-1.110,-.177,.710)],
 [(-.951,-.049,1.71),(-1.114,-.071,1.45),(-1.208,-.091,1.11),(-1.254,-.146,.578)],
 [(-.970,.029,1.74),(-1.146,.033,1.43),(-1.190,.010,1.12),(-1.168,-.036,.820)],
 [(-.898,.086,1.70),(-1.003,.093,1.40),(-1.048,.062,1.12),(-1.032,.014,.780)],
 [(-.711,-.026,1.94),(-.627,-.112,1.64),(-.624,-.208,1.37),(-.689,-.257,1.165)],
]
for i,pp in enumerate(fingers):
    sweep('CLAW | primary talon %02d'%(i+1),pp,[.058,.081,.044,.00035],liquid,CLAW,arm,.28,78,16,(-.22+i*.15),True,.0042)
    for offset in (-1,1):
        pts=[Vector(p)+Vector((offset*.025,offset*.009,.043)) for p in pp[:-1]]+[Vector(pp[-1])+Vector((offset*.016,.008,.10))]
        sweep('CLAW | talon fluted edge %d %d'%(i+1,offset),pts,[.015,.028,.012,.0002],ridge,CLAW,arm,.28,62,10,.15,False)
    # The emitting edge is narrow, follows the actual geometry, and ends at a physical point.
    sweep('CLAW | heated cutting seam '+str(i+1),[Vector(p)+Vector((0,-.021,0)) for p in pp[-3:]],[.0045,.006,.0003],hot if i==2 else red,CLAW,arm,.38,52,8)
for i in range(21):
    t=i/20;z=2.36-t*.89;x=-.615-t*.275
    side=-1 if i%2 else 1
    y=-.091+(.14 if i%3==0 else -.09)
    pp=[(x,y,z),(x+side*.087,y-.025,z-.08),(x+side*.149,y-.013,z-.245)]
    sweep('CLAW | overlapping lateral spur '+str(i),pp,[.026,.034,.0003],liquid,CLAW,arm,.29,38,10,.2,vein=i%5==0,vein_width=.0019)

# Backpack still reads as E-01. Tendrils originate visibly through the opened spine.
bundle('DORSAL | living spine',[(.012,.174,1.92),(-.047,.297,2.20),(.074,.452,2.53),(.044,.329,2.86),(.105,.164,3.016)],.052,12,.105,BACK,pack,45)
for sign in (-1,1):
    for i in range(6):
        z=2.05+i*.127+rng.uniform(-.035,.035)
        sweep('DORSAL | wrapping root plate '+str(sign)+' '+str(i),[(sign*.04,.42,z),(sign*(.16+rng.random()*.07),.472+rng.random()*.034,z+.092),(sign*.329,.259,z+.15),(sign*.410,.070,z+rng.uniform(.18,.30))],[.035,.048,.027,.0005],liquid,BACK,pack,.38,60,12,.4*sign,i%2==0,.0024)

bundle('DORSAL | ruptured rear door left root',[(-.207,.294,2.943),(-.133,.552,2.811),(-.014,.556,2.657),(.118,.565,2.417),(.197,.342,2.168)],.028,7,.036,BACK,pack,47)
bundle('DORSAL | lower rear parasitic diagonal',[(.191,.337,2.716),(.126,.572,2.550),(-.112,.568,2.382),(-.211,.308,2.172),(-.270,.137,2.012)],.027,7,.041,BACK,pack,48)
for i in range(18):
    z=rng.uniform(2.16,2.80);x=rng.uniform(-.17,.17)
    sweep('DORSAL | torn dorsal lamella '+str(i),[(x,.507,z+.1),(x+rng.uniform(-.04,.04),.591,z),(x-.045,.557,z-.12),(x-.086,.450,z-.23)],[.010,.029,.021,.0002],liquid,BACK,pack,.25,44,10,.3,vein=i%5==0,vein_width=.0015)
primary_paths=[
 [( .04,.30,2.66),(.12,.48,3.04),(.33,.48,3.45),(.62,.31,3.48),(.88,.12,3.18),(.98,-.02,2.73)],
 [(-.07,.32,2.79),(-.12,.41,3.18),(.00,.28,3.62),(.32,.10,3.73),(.56,-.04,3.64)],
 [( .12,.36,2.55),(.40,.52,2.99),(.72,.36,3.14),(1.02,.08,2.98),(1.17,-.10,2.56),(1.07,-.17,2.26)],
 [(-.11,.27,2.73),(-.37,.44,3.05),(-.65,.31,3.16),(-.82,.07,2.98),(-.96,-.11,2.54)],
 [(-.09,.34,2.36),(-.44,.47,2.64),(-.72,.45,2.55),(-.96,.23,2.24),(-1.08,.00,1.82),(-1.00,-.10,1.53)],
 [( .15,.27,2.32),(.51,.48,2.60),(.79,.38,2.48),(.93,.14,2.13),(.85,-.05,1.73)],
]
for i,pp in enumerate(primary_paths):
    tend=empty('TENDRIL %02d | future chain root'%(i+1),pp[0],master);bpy.context.view_layer.update()
    sweep('DORSAL | primary tendril %02d'%(i+1),pp,[.046,.059,.037,.020,.00035],liquid,BACK,tend,.46,100,16,(-.3+i*.24),True,.0030)
    for j in (-1,1):
        pts=[Vector(p)+Vector((j*.028*math.sin(k+.4),j*.025,0)) for k,p in enumerate(pp)]
        sweep('DORSAL | braided tendon %d %d'%(i+1,j),pts,[.019,.026,.018,.0004],ridge if j<0 else liquid,BACK,tend,.44,90,12,-.3,vein=j==1,vein_width=.0015)
    for j in range(2):
        base=Vector(pp[1+j]);mid=Vector(pp[2+j]);tip=base+(mid-base)*.8+Vector(((-1 if i>2 else 1)*(.10+j*.04),-.055,-.26))
        sweep('DORSAL | secondary split %d %d'%(i,j),[base,base.lerp(mid,.48)+Vector((0,-.04,.02)),tip],[.027,.036,.0003],liquid,BACK,tend,.3,44,10,vein=j==0,vein_width=.0015)
for i in range(11):
    z=2.14+i*.065;x=.06*math.sin(i*1.2)
    ellipsoid('DORSAL | red interstitial nodule '+str(i),(x,.49,z),(.015,.012,.022),red,BACK,pack,20,12)

# Irregular tapered plates make the torso read as invaded tissue made of metal.
for i in range(32):
    sign=-1 if i%2 else 1
    z=rng.uniform(2.10,2.74);xx=sign*rng.uniform(.15,.31)
    yy=-.29-rng.uniform(0,.045)
    endx=xx*.40+rng.uniform(-.04,.04);length=rng.uniform(.16,.36)
    if z>2.51:xx=sign*rng.uniform(.25,.35);endx=xx-sign*.12
    points=[(xx,yy+.065,z+.08),(xx+sign*.045,yy-.015,z),(xx-sign*.035,yy-.033,z-length*.52),(endx,yy-.017,z-length)]
    w=rng.uniform(.022,.046)
    sweep('THORAX | swept invasion lamella '+str(i),points,[.008,w,w*.7,.00025],liquid,CORE,chest,.29,48,12,rng.uniform(-.35,.35),i%6==0,.0017)

for i in range(18):
    z=2.90+rng.random()*.34;x=rng.uniform(.056,.15);yy=rng.uniform(-.1,.06)
    sweep('HUSK | broken metal facial blade '+str(i),[(x,yy,z),(x+.043,yy-.035,z+.039),(x+.021,yy-.045,z+.100)],[.009,.019,.0002],liquid,HEAD,head,.25,34,10,.3,vein=i%6==0,vein_width=.0012)

# Lower body: keep broad frontal white planes; invasion hugs gaps, joints and outer flanks.
for sign,side in ((-1,'R'),(1,'L')):
    thigh=parts['E01_'+side+'_THIGH'];calf=parts['E01_'+side+'_CALF'];foot=parts['E01_'+side+'_FOOT']
    bundle(side+' HIP | trailing oblique sinew',[(sign*.065,-.132,2.10),(sign*.213,-.183,1.97),(sign*.398,-.126,1.82),(sign*.463,-.125,1.63)],.032,6,.036,LEGS,thigh,60+(sign+1))
    bundle(side+' LEG | outer thigh invasion',[(sign*.346,.050,1.96),(sign*.444,-.028,1.74),(sign*.436,-.032,1.50),(sign*.371,-.109,1.26)],.023,6,.038,LEGS,thigh,66+(sign+1))
    bundle(side+' LEG | back of thigh parasitic channel',[(sign*.177,.203,2.020),(sign*.254,.231,1.813),(sign*.368,.190,1.547),(sign*.385,.106,1.317)],.022,5,.032,LEGS,thigh,82+(sign+1))
    bundle(side+' LEG | lower external flow',[(sign*.402,.051,1.30),(sign*.505,.021,1.04),(sign*.551,-.047,.734),(sign*.504,-.063,.423),(sign*.508,-.177,.200)],.027,8 if sign>0 else 6,.041,LEGS,calf,71+(sign+1))
    bundle(side+' LEG | inside ankle tendons',[(sign*.364,.17,1.14),(sign*.327,.175,.837),(sign*.322,.090,.480),(sign*.327,-.044,.251)],.021,5,.030,LEGS,calf,76+(sign+1))
    for i in range(6):
        z=1.84-i*.112
        sweep(side+' LEG | thigh jagged outgrowth '+str(i),[(sign*.423,.022,z),(sign*(.486+i*.003),-.034,z-.068),(sign*(.494+i*.011),-.112,z-.229)],[.020,.030,.0003],liquid,LEGS,thigh,.30,42,10,.2*sign,i%3==0,.0018)
    for i in range(8):
        z=1.18-i*.09
        sweep(side+' LEG | shin marginal blade '+str(i),[(sign*.467,.022,z),(sign*.522,-.060,z-.086),(sign*(.56+math.sin(i)*.03),-.075,z-.229)],[.025,.033,.0002],liquid,LEGS,calf,.28,42,10,.4*sign,i%4==0,.002)
    for i in range(5):
        x=sign*.425+(i-2)*.072
        sweep(side+' FOOT | crawling metal root '+str(i),[(x,.081,.410),(x+sign*.012,-.064,.227),(x+sign*.025,-.209,.116),(x+sign*.028,-.370,.060)],[.026,.031,.018,.0005],liquid,LEGS,foot,.42,50,10,.1,i%3==0,.0014)
    for i in range(4):
        xx=sign*(.477+i*.020);zz=.27+i*.09
        plate(side+' LEG | torn ankle white splinter '+str(i),[(xx,-.14,zz+.059),(xx+sign*.055,-.15,zz+.088),(xx+sign*.030,-.19,zz-.010),(xx+sign*.016,-.17,zz+.012)],(0,.014,0),white,LEGS,foot)

# Small, irregular physical scratches and fissures placed on actual original armor surfaces.
# Ray casts preserve contact on sloping armor; these are not floating random decals.
bpy.context.view_layer.update()
scratch_verts=[];scratch_faces=[]
scar_targets=[o for o in ARMOR.objects if o.type=='MESH' and o.name.startswith('E01 ancestry') and len(o.data.polygons)>2]
for ob in scar_targets:
    if not any(x in ob.name for x in ('front armor','front armor','breastplate','thigh front','shin shield','pectoral','dorsal armored')):continue
    bounds=[Vector(v) for v in ob.bound_box];lo=Vector(tuple(min(v[k] for v in bounds) for k in range(3)));hi=Vector(tuple(max(v[k] for v in bounds) for k in range(3)))
    if hi.x-lo.x<.035 or hi.z-lo.z<.04:continue
    for k in range(14):
        x=rng.uniform(lo.x,hi.x);z=rng.uniform(lo.z,hi.z)
        found,pos,norm,_=ob.ray_cast(Vector((x,-1.2,z)),Vector((0,1,0)))
        if not found or norm.y>-.15:continue
        direction=Vector((rng.uniform(-.5,.5),0,1)).normalized();half=rng.uniform(.003,.020);width=rng.uniform(.0005,.0016)
        tangent=norm.cross(direction).normalized();a=pos-direction*half+norm*.0008;b=pos+direction*half*.4+norm*.0008
        idx=len(scratch_verts);scratch_verts.extend([tuple(a-tangent*width),tuple(a+tangent*width),tuple(b)])
        scratch_faces.append((idx,idx+1,idx+2))
if scratch_verts:mesh('WEAR | flush irregular paint cuts',scratch_verts,scratch_faces,mechanic,ARMOR,chest)

crack_targets=[
 ('H02 continuous swept helmet crown',(-.06,3.16),.11),
 ('R B02 shoulder broad front armor',(-.52,2.80),.16),
 ('L B02 shoulder broad front armor',(.56,2.85),.20),
 ('B02 central breastplate folded panels',(.035,2.58),.19),
 ('R B02 thigh front armor',(-.285,1.77),.17),
 ('L B02 thigh front armor',(.302,1.63),.19),
 ('L B02 long faceted shin shield',(.437,.91),.23),
 ('R B02 long faceted shin shield',(-.405,.79),.20),
]
for index,(key,(cx,cz),length) in enumerate(crack_targets):
    target=source_name_map.get(key)
    if target is None:continue
    for branch in range(3):
        points=[]
        for j in range(7):
            t=j/6;x=cx+(t-.5)*length*(.32 if branch==0 else (-.5 if branch==1 else .65))+rng.uniform(-.009,.009)
            z=cz+(t-.5)*length+(branch-1)*.023
            hit,pos,norm,_=target.ray_cast(Vector((x,-1.2,z)),Vector((0,1,0)))
            if hit:points.append(pos+norm*.0010)
        if len(points)>2:
            sweep('WEAR | fractured paint boundary %d %d'%(index,branch),points,[.0002,.0016,.0010,.00015],mechanic,HEAD if 'H02' in key else ARMOR,target.parent,.25,34,6,ridged=False)

# One coherent assembly scale, relative to the accepted grunt's actual head crown.
master.scale=(1.5,1.5,1.5);master.location.z=-.056*1.5
master['identity']='E-01 mechanical body taken over by living liquid metal; no flesh'
master['review_stage']='01 modeling approval required before rigging and game integration'
master['reference']=str(REF)
master['scale_relative_to_approved_grunt']=1.5
master['anatomy']='-X = anatomical right / -Y = forward / +Z = up'
master['deformation_plan']='Primary tendril chains; right claw spread; thorax iris opening. No animation rig yet.'
master['base_model']=str(SOURCE)
bpy.context.view_layer.update()

# Neutral studio: broad highlights describe glossy black flow without obscuring white armor.
floor=material('studio graphite',(.074,.091,.100),.18,.38)
mesh('STUDIO | ground',[(-100,-100,-.003),(100,-100,-.003),(100,100,-.003),(-100,100,-.003)],[(0,1,2,3)],floor,STUDIO,None)
world=bpy.data.worlds.new('ELITE studio world');sc.world=world;world.use_nodes=True
world_bg=next((n for n in world.node_tree.nodes if n.type=='BACKGROUND'),None)
if world_bg is None:world_bg=world.node_tree.nodes.new('ShaderNodeBackground')
world_out=next((n for n in world.node_tree.nodes if n.type=='OUTPUT_WORLD'),None)
if world_out is None:world_out=world.node_tree.nodes.new('ShaderNodeOutputWorld')
world.node_tree.links.new(world_bg.outputs[0],world_out.inputs['Surface'])
world_bg.inputs['Color'].default_value=(.27,.32,.38,1)
world_bg.inputs['Strength'].default_value=.36

def area(name,loc,power,color,size,target=(0,0,2.7),shape='DISK',size_y=None):
    d=bpy.data.lights.new(name,'AREA');d.energy=power;d.color=color;d.shape=shape;d.size=size
    if size_y is not None:d.size_y=size_y
    o=bpy.data.objects.new(name,d);STUDIO.objects.link(o);o.location=loc;o.rotation_euler=(Vector(target)-o.location).to_track_quat('-Z','Y').to_euler()
area('Key | tall softbox',(-4.5,-5.0,7.4),1250,(1,.94,.87),3.4,shape='RECTANGLE',size_y=5)
area('Fill | cool softbox',(4.1,-3.0,5.6),750,(.78,.86,1),3.0,shape='RECTANGLE',size_y=5)
area('Rim | neutral white',(1.5,4.0,7.0),1750,(.88,.95,1),2.4,shape='RECTANGLE',size_y=5)
area('Rim | left blade',(-4,2.0,3.8),1050,(.88,.92,1),1.6,shape='RECTANGLE',size_y=4)
area('Front | fine vertical reflection',(0,-6,3.4),300,(1,.86,.77),.6,shape='RECTANGLE',size_y=4.5)

S=1.5
def worldpoint(p):return Vector(p)*S+Vector((0,0,-.084))
def camera(name,loc,target,scale,w=1250,h=1550):
    d=bpy.data.cameras.new(name);d.type='ORTHO';d.ortho_scale=scale
    o=bpy.data.objects.new(name,d);CAMERAS.objects.link(o);o.location=worldpoint(loc)
    o.rotation_euler=(worldpoint(target)-o.location).to_track_quat('-Z','Y').to_euler();o['resolution']=[w,h]
    return o
camera('ELITE_HERO',(-3.8,-9.8,4.35),(-.06,.05,1.84),6.45,1550,1800)
camera('ELITE_FRONT',(0,-12,1.88),(0,0,1.88),6.1,1150,1600)
camera('ELITE_BACK',(0,12,1.88),(0,0,1.88),6.1,1150,1600)
camera('ELITE_LEFT',(12,0,1.88),(0,0,1.88),6.1,1150,1600)
camera('ELITE_RIGHT',(-12,0,1.88),(0,0,1.88),6.1,1150,1600)
camera('ELITE_HEAD',(-.85,-2.1,3.52),(.024,0,3.105),1.10,1200,1200)
camera('ELITE_CORE',(-.85,-2.8,2.87),(0,-.10,2.42),1.67,1200,1400)
camera('ELITE_CLAW',(-3.3,-5.4,2.80),(-.865,-.06,1.75),3.00,1100,1500)
camera('ELITE_DORSAL',(3.0,7.0,4.1),(.02,.29,2.76),3.60,1400,1500)
camera('ELITE_GAME_READ',(-5.2,-7,8.5),(-.10,.05,1.65),6.5,1450,1450)
sc.camera=bpy.data.objects['ELITE_HERO']
sc.render.resolution_x=1550;sc.render.resolution_y=1800

im=bpy.data.images.load(str(REF),check_existing=True);im.pack()
refob=bpy.data.objects.new('REFERENCE | user supplied orthographic concept',None);REFERENCES.objects.link(refob)
refob.empty_display_type='IMAGE';refob.data=im;refob.empty_display_size=6;refob.location=(7,3,3)
refob.rotation_euler=(math.pi/2,0,0);refob.hide_render=True;refob.hide_viewport=True
textblock=bpy.data.texts.new('READ ME | ELITE modeling gate')
textblock.write((ROOT/'DESIGN_DECISIONS.md').read_text(encoding='utf-8'))
for screen in bpy.data.screens:
    for area_ui in screen.areas:
        if area_ui.type=='VIEW_3D':
            area_ui.spaces.active.region_3d.view_distance=8.6
            area_ui.spaces.active.region_3d.view_location=(0,0,2.65)
            area_ui.spaces.active.clip_end=200
            area_ui.spaces.active.shading.type='MATERIAL'
# Remove all orphaned source gun data and old studio materials, not owned mesh parts.
bpy.ops.outliner.orphans_purge(do_local_ids=True,do_linked_ids=True,do_recursive=True)
try:
    prefs=bpy.context.preferences.addons['cycles'].preferences
    prefs.compute_device_type='OPTIX';prefs.get_devices()
    for d in prefs.devices:d.use=d.type=='OPTIX'
    sc.cycles.device='GPU' if any(d.use for d in prefs.devices) else 'CPU'
    devices=[(d.name,d.type,d.use) for d in prefs.devices]
except Exception as e:
    sc.cycles.device='CPU';devices=[str(e)]

model=OUT/'TYPE_E01_ELITE_STAGE01.blend'
bpy.ops.wm.save_as_mainfile(filepath=str(model),check_existing=False)
bpy.context.view_layer.update()
actor=[o for o in sc.objects if o.type=='MESH' and o not in STUDIO.objects[:]]
triangles=sum(sum(len(f.vertices)-2 for f in o.data.polygons) for o in actor)
bounds=[o.matrix_world@Vector(v) for o in actor for v in o.bound_box]
report={'model':str(model),'source':str(SOURCE),'source_sha256':source_hash,'source_unchanged':hashlib.sha256(SOURCE.read_bytes()).hexdigest()==source_hash,'reference':str(REF),'meshes':len(actor),'triangles':triangles,'relative_scale':1.5,'bounds_m':{'min':[min(v[i] for v in bounds) for i in range(3)],'max':[max(v[i] for v in bounds) for i in range(3)]},'collections':{c.name:len(c.objects) for c in sc.collection.children},'omitted_source_parts':omitted,'primary_tendrils':6,'animation_rig':False,'game_export':False,'devices':devices}
(OUT/'build_report.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print('BOSS_MODEL_SAVED '+json.dumps({k:v for k,v in report.items() if k!='omitted_source_parts'}),flush=True)
# First cheap actual-model image for an early visual check. Further views render from the reopened file.
sc.cycles.samples=24;sc.render.resolution_x=1000;sc.render.resolution_y=1200
sc.render.filepath=str(OUT/'renders/HERO_DRAFT.png')
bpy.ops.render.render(write_still=True)
print('FIRST_REVIEW_RENDER_COMPLETE',flush=True)
