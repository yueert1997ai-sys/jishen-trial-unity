"""TYPE E-01 | first silhouette / head approval gate.
Authored in Blender from the user's four-view sheet. No other asset is loaded.
Body = primary armor masses only. Head = secondary forms. No game export.
"""
from pathlib import Path
import bpy, bmesh, math, json, sys, time
from mathutils import Vector

ROOT = Path(__file__).resolve().parent
OUT = ROOT / 'stage_01'
OUT.mkdir(exist_ok=True)
(OUT / 'renders').mkdir(exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
sc = bpy.context.scene
sc.render.filepath = str(OUT / 'renders') + '/'
sc.unit_settings.system = 'METRIC'
sc.render.engine = 'CYCLES'
sc.cycles.samples = 24
sc.cycles.use_denoising = True
sc.render.image_settings.file_format = 'PNG'
sc.render.image_settings.color_mode = 'RGB'
sc.render.film_transparent = False
sc.view_settings.view_transform = 'AgX'
sc.view_settings.look = 'AgX - Medium High Contrast'
sc.render.resolution_percentage = 100
sc.render.threads_mode = 'FIXED'
sc.render.threads = 4

def collection(name):
    c = bpy.data.collections.new(name)
    sc.collection.children.link(c)
    return c

HEAD = collection('01 HEAD - review secondary forms')
BODY = collection('02 BODY - primary proportions only')
PACK = collection('03 BACKPACK - primary masses')
RIG = collection('04 EDITABLE PART ROOTS - no animation')
STUDIO = collection('90 REVIEW STUDIO')
CAMS = collection('91 REVIEW CAMERAS')
REF = collection('99 PACKED USER REFERENCE')
REF.hide_render = True

def empty(name, loc=(0,0,0), parent=None):
    o = bpy.data.objects.new(name, None)
    RIG.objects.link(o)
    o.location = loc
    o.empty_display_size = .07
    o.empty_display_type = 'PLAIN_AXES'
    if parent:
        o.parent = parent
        o.matrix_parent_inverse = parent.matrix_world.inverted()
    bpy.context.view_layer.update()
    return o

root = empty('TYPE_E01_MASTER_ROOT')
root['review_status'] = 'STAGE 01 - awaiting user silhouette and head approval'
root['source'] = 'User supplied Type-01 design sheet; visible unit marking TYPE E-01'
root['next_gate'] = 'Refine body and equipment only after user approval'
root['preview_scale'] = '3.23 metre crown datum; adjustable assembly scale, not gameplay scale'
root['coordinates'] = 'Z up / -Y forward / -X anatomical right'
head_root = empty('E01_HEAD', (0,0,2.89), root)
torso_root = empty('E01_CHEST', (0,0,2.48), root)
waist_root = empty('E01_WAIST', (0,0,2.08), root)
pack_root = empty('E01_BACKPACK', (0,.23,2.62), root)

def material(name, color, metal=.15, rough=.4, emission=0):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    p = m.node_tree.nodes.get('Principled BSDF')
    p.inputs['Base Color'].default_value = (*color,1)
    p.inputs['Metallic'].default_value = metal
    p.inputs['Roughness'].default_value = rough
    if emission:
        p.inputs['Emission Color'].default_value = (*color,1)
        p.inputs['Emission Strength'].default_value = emission
    m.diffuse_color = (*color,1)
    return m

white = material('E01 | warm grey-white armor', (.64,.66,.645), .3,.34)
edge = material('E01 | recessed sub armor', (.18,.205,.21), .55,.38)
dark = material('E01 | charcoal mechanical frame', (.043,.053,.059), .55,.36)
rubber = material('E01 | black seals', (.013,.019,.022), .1,.6)
steel = material('E01 | exposed machined metal', (.235,.26,.275), .78,.29)
red = material('E01 | red mono sensor', (.42,.001,.002), .10,.24, .16)
red_core = material('E01 | red optical core', (.64,.002,.003), .02,.24,.35)
ground = material('Studio | neutral graphite', (.082,.093,.105), 0,.81)

ASSET = []
def attach(o, mat, col, parent):
    for c in list(o.users_collection):
        c.objects.unlink(o)
    col.objects.link(o)
    if mat:
        o.data.materials.append(mat)
    if parent:
        o.parent = parent
        o.matrix_parent_inverse = parent.matrix_world.inverted()
    ASSET.append(o)
    return o

def finish(o, bevel):
    if bevel:
        m = o.modifiers.new('Editable manufactured edge bevel','BEVEL')
        m.width = bevel
        m.segments = 2
        m.limit_method = 'ANGLE'
        m.angle_limit = math.radians(22)
        m.harden_normals = True
        m.use_clamp_overlap = True
        n = o.modifiers.new('Panel corner normals','WEIGHTED_NORMAL')
        n.keep_sharp = True
        n.weight = 40
    return o

def mesh(name, verts, faces, mat=white, col=BODY, parent=None, bevel=.008):
    me = bpy.data.meshes.new(name + '_mesh')
    me.from_pydata(verts, [], faces)
    me.update()
    bm = bmesh.new()
    bm.from_mesh(me)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    bm.to_mesh(me)
    bm.free()
    o = bpy.data.objects.new(name, me)
    col.objects.link(o)
    o.data.materials.append(mat)
    if parent:
        o.parent = parent
        o.matrix_parent_inverse = parent.matrix_world.inverted()
    ASSET.append(o)
    return finish(o, bevel)

def box(name, loc, scale, mat=white, col=BODY, parent=None, bevel=.009, rot=None):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    o = bpy.context.object
    o.name = name
    o.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if rot:
        o.rotation_euler = rot
    attach(o, mat, col, parent)
    return finish(o, bevel)

def hull(name, verts, mat=white, col=BODY, parent=None, bevel=.008):
    bm = bmesh.new()
    for v in verts:
        bm.verts.new(v)
    bmesh.ops.convex_hull(bm, input=list(bm.verts), use_existing_faces=False)
    bm.verts.ensure_lookup_table()
    bm.verts.index_update()
    vs = [tuple(v.co) for v in bm.verts]
    fs = [tuple(v.index for v in f.verts) for f in bm.faces]
    bm.free()
    return mesh(name, vs, fs, mat,col,parent,bevel)

def plate(name, points, depth=(0,.035,0), mat=white, col=BODY, parent=None, bevel=.007):
    n = len(points)
    vs = [tuple(p) for p in points] + [tuple(Vector(p)+Vector(depth)) for p in points]
    fs = [tuple(range(n)),tuple(range(n,2*n))[::-1]]
    fs += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    return mesh(name,vs,fs,mat,col,parent,bevel)

def skin(name, points, surface_faces, depth=(0,.012,0), mat=white, col=HEAD, parent=None, bevel=.002):
    n=len(points)
    vs=list(points)+[tuple(Vector(p)+Vector(depth)) for p in points]
    fs=list(surface_faces)+[tuple(i+n for i in reversed(f)) for f in surface_faces]
    edges={}
    for f in surface_faces:
        for a,b in zip(f,f[1:]+f[:1]):
            key=tuple(sorted((a,b)))
            edges[key]=None if key in edges else (a,b)
    for e in edges.values():
        if e:
            a,b=e;fs.append((a,b,b+n,a+n))
    return mesh(name,vs,fs,mat,col,parent,bevel)

def rings(name, rows, mat=white, col=BODY, parent=None, bevel=.007, cap=True):
    n = len(rows[0])
    fs=[]
    for k in range(len(rows)-1):
        fs += [(k*n+i,k*n+(i+1)%n,(k+1)*n+(i+1)%n,(k+1)*n+i) for i in range(n)]
    if cap:
        fs += [tuple(range(n))[::-1],tuple(range((len(rows)-1)*n,len(rows)*n))]
    return mesh(name,[p for r in rows for p in r],fs,mat,col,parent,bevel)

def oct_ring(cx,cy,z,w,d,ch=.20):
    x=w/2; y=d/2; a=x*ch; b=y*ch
    return [(cx-x+a,cy-y,z),(cx+x-a,cy-y,z),(cx+x,cy-y+b,z),
            (cx+x,cy+y-b,z),(cx+x-a,cy+y,z),(cx-x+a,cy+y,z),
            (cx-x,cy+y-b,z),(cx-x,cy-y+b,z)]

def cylinder(name, a,b, radius, mat=dark, col=BODY, parent=None, vertices=32, bevel=.003, r2=None):
    a=Vector(a); b=Vector(b); d=b-a
    bpy.ops.mesh.primitive_cone_add(vertices=vertices, radius1=radius, radius2=radius if r2 is None else r2,depth=d.length, location=(a+b)/2)
    o=bpy.context.object; o.name=name
    o.rotation_euler=d.to_track_quat('Z','Y').to_euler()
    attach(o,mat,col,parent)
    for p in o.data.polygons:
        if len(p.vertices)==4:
            p.use_smooth=True
    return finish(o,bevel)

def tube(name,a,b,ro,ri,mat=steel,col=BODY,parent=None,n=40):
    a=Vector(a); b=Vector(b); d=(b-a).normalized()
    u=d.cross(Vector((0,0,1)))
    if u.length<.01: u=d.cross(Vector((0,1,0)))
    u.normalize(); v=d.cross(u)
    rows=[]
    for center,r in ((a,ro),(b,ro),(b,ri),(a,ri)):
        rows.append([tuple(center+r*(math.cos(i*math.tau/n)*u+math.sin(i*math.tau/n)*v)) for i in range(n)])
    fs=[]
    for k in range(4):
        fs += [(k*n+i,k*n+(i+1)%n,((k+1)%4)*n+(i+1)%n,((k+1)%4)*n+i) for i in range(n)]
    return mesh(name,[p for r in rows for p in r],fs,mat,col,parent,.0015)

def mirrored(points,s): return [(x*s,y,z) for x,y,z in points]

# Neck and compact head chassis. Eye, face housing, shell and jaw are separate.
cylinder('Neck armored collar', (0,0,2.81),(0,0,2.90),.115,dark,HEAD,head_root)
cylinder('Neck inner gimbal', (0,0,2.86),(0,0,2.945),.077,steel,HEAD,head_root)
box('Head dark internal chassis',(0,.007,3.037),(.256,.218,.237),dark,HEAD,head_root,.018)

# Swept, low round-edged helmet; the white forehead terminates above the lens.
lower=[(-.099,-.170,3.071),(.099,-.170,3.071),(.142,-.106,3.089),(.151,.053,3.062),(.103,.137,3.043),(-.103,.137,3.043),(-.151,.053,3.062),(-.142,-.106,3.089)]
middle=[(-.100,-.144,3.168),(.100,-.144,3.168),(.139,-.083,3.173),(.142,.065,3.147),(.097,.139,3.124),(-.097,.139,3.124),(-.142,.065,3.147),(-.139,-.083,3.173)]
top=[(-.077,-.091,3.218),(.077,-.091,3.218),(.111,-.050,3.224),(.105,.064,3.207),(.071,.100,3.188),(-.071,.100,3.188),(-.105,.064,3.207),(-.111,-.050,3.224)]
rings('Helmet continuous faceted shell',[lower,middle,top],white,HEAD,head_root,.004)
# Explicit bands avoid triangulation creases across the broad swept forehead.
forehead=[(-.077,-.103,3.219),(.077,-.103,3.219),(-.096,-.158,3.166),(.096,-.158,3.166),(-.076,-.187,3.080),(.076,-.187,3.080)]
skin('Forehead broad armored face',forehead,[(0,1,3,2),(2,3,5,4)],(0,.014,0),white,HEAD,head_root,.003)
plate('Face recessed black optical housing',[(-.082,-.183,3.072),(.082,-.183,3.072),(.098,-.177,3.029),(.060,-.173,2.917),(-.060,-.173,2.917),(-.098,-.177,3.029)],(0,.037,0),rubber,HEAD,head_root,.004)

for s,label in ((-1,'R'),(1,'L')):
    # Thick cowl surfaces converge towards the chin, instead of a human mouth.
    pts=[(.110,-.166,3.049),(.142,-.106,3.026),(.135,-.091,2.949),(.070,-.157,2.880),(.052,-.186,2.902),(.080,-.191,2.948)]
    plate(label+' white cheek and jaw rail',mirrored(pts,s),(0,.042,.004),white,HEAD,head_root,.004)
    hull(label+' rear lower cheek',mirrored([(.13,-.075,2.970),(.13,.071,2.969),(.10,.106,2.923),(.084,-.09,2.895),(.092,-.135,2.913),(.151,-.039,3.021),(.142,.064,3.024)],s),white,HEAD,head_root,.004)
    cylinder(label+' ear dark isolation ring',(s*.141,.007,3.061),(s*.159,.007,3.061),.070,rubber,HEAD,head_root,48,.002)
    cylinder(label+' circular ear cover',(s*.158,.007,3.061),(s*.176,.007,3.061),.061,white,HEAD,head_root,48,.003)
    cylinder(label+' ear recessed center',(s*.176,.007,3.061),(s*.178,.007,3.061),.051,white,HEAD,head_root,48,.001)
    # Small structural rear temple block visible in side view.
    box(label+' rear temple bracket',(s*.137,.112,3.029),(.030,.047,.086),edge,HEAD,head_root,.004)
    cylinder(label+' cheek hinge',(s*.133,-.076,2.982),(s*.147,-.076,2.982),.023,steel,HEAD,head_root,24,.001)
    # Only a handful of head fasteners at this gate; body detail is deferred.
    for j,(x,y,z) in enumerate(((.063,-.158,3.168),(.059,-.182,3.099),(.069,-.182,2.921))):
        cylinder(label+' head flush fastener '+str(j),(s*x,y,z),(s*x,y-.0025,z),.0038,steel,HEAD,head_root,12,.0004)

plate('Chin bridge armor',[(-.056,-.188,2.908),(.056,-.188,2.908),(.055,-.171,2.879),(-.055,-.171,2.879)],(0,.042,0),white,HEAD,head_root,.004)
tube('Monoeye armored bezel',(0,-.183,3.027),(0,-.202,3.027),.053,.039,dark,HEAD,head_root,64)
tube('Monoeye inner optical ring',(0,-.201,3.027),(0,-.209,3.027),.041,.035,rubber,HEAD,head_root,64)
cylinder('Monoeye red lens',(0,-.203,3.027),(0,-.212,3.027),.035,red,HEAD,head_root,64,.003)
cylinder('Monoeye optical core',(0,-.212,3.027),(0,-.214,3.027),.024,red_core,HEAD,head_root,64,.001)
box('Lower face service recess',(0,-.181,2.950),(.067,.013,.037),dark,HEAD,head_root,.005)
for z in (2.943,2.956):
    box('Face horizontal grille', (0,-.190,z),(.051,.004,.003),edge,HEAD,head_root,.0005)
box('Rear helmet equipment insert',(0,.139,3.083),(.108,.018,.057),dark,HEAD,head_root,.004)
box('Antenna base',(0,.071,3.212),(.021,.025,.020),dark,HEAD,head_root,.003)
cylinder('Single rear antenna',(0,.071,3.219),(0,.077,3.338),.0055,dark,HEAD,head_root,16,.001,r2=.003)

# Torso: reference barrel chest, sloping center plate, wraparound side armor.
rings('Torso internal hexagonal chassis',[oct_ring(0,.015,2.38,.39,.31),oct_ring(0,.022,2.69,.65,.37),oct_ring(0,.028,2.82,.58,.30)],dark,BODY,torso_root,.013)
plate('Chest central sloped breastplate',[(-.152,-.199,2.813),(.152,-.199,2.813),(.191,-.335,2.624),(.112,-.287,2.383),(-.112,-.287,2.383),(-.191,-.335,2.624)],(0,.047,.007),white,BODY,torso_root,.012)
for s,label in ((-1,'R'),(1,'L')):
    pts=[(.170,-.199,2.810),(.297,-.160,2.794),(.351,-.222,2.672),(.326,-.266,2.493),(.229,-.270,2.433),(.202,-.336,2.620)]
    plate(label+' pectoral armor lobe',mirrored(pts,s),(0,.050,0),white,BODY,torso_root,.010)
    hull(label+' chest side wrap',mirrored([(.315,-.210,2.771),(.357,-.170,2.707),(.341,.116,2.681),(.264,.17,2.775),(.326,-.234,2.484),(.303,.128,2.455),(.274,.15,2.570)],s),white,BODY,torso_root,.013)
    box(label+' collar armored support',(s*.218,.001,2.847),(.154,.269,.072),white,BODY,torso_root,.012,rot=(0,s*.11,0))
    plate(label+' abdominal side inner guard',mirrored([(.108,-.175,2.397),(.245,-.104,2.431),(.232,-.114,2.272),(.147,-.181,2.221)],s),(0,.058,0),dark,BODY,waist_root,.008)
rings('Waist central actuator',[oct_ring(0,.021,2.122,.285,.253),oct_ring(0,.004,2.393,.285,.26)],dark,BODY,waist_root,.01)
for z in (2.18,2.245,2.31):
    box('Abdominal armored segmented block',(0,-.140,z),(.143,.045,.049),edge,BODY,waist_root,.007)

# Pelvic armor: narrow center apron and flared hip plates.
box('Pelvis internal frame',(0,.005,2.011),(.519,.292,.272),dark,BODY,waist_root,.036)
plate('Central armored pelvis apron',[(-.084,-.209,2.165),(.084,-.209,2.165),(.104,-.237,1.958),(.071,-.210,1.826),(-.071,-.210,1.826),(-.104,-.237,1.958)],(0,.052,0),white,BODY,waist_root,.011)
for s,label in ((-1,'R'),(1,'L')):
    plate(label+' front waist flare',mirrored([(.104,-.195,2.164),(.268,-.136,2.160),(.320,-.182,1.991),(.132,-.256,1.985)],s),(0,.044,0),white,BODY,waist_root,.010)
    hull(label+' hip side armor',mirrored([(.275,-.11,2.13),(.352,-.04,2.109),(.374,.102,1.858),(.292,.165,1.881),(.299,-.143,1.964),(.378,.065,1.994)],s),edge,BODY,waist_root,.010)
    plate(label+' rear waist plate',mirrored([(.044,.189,2.150),(.275,.163,2.139),(.30,.209,1.972),(.093,.224,1.902)],s),(0,-.041,0),white,BODY,waist_root,.010)

# Arms have separate rigid segment roots. Armor remains thick independent mesh.
for s,label in ((-1,'R'),(1,'L')):
    shoulder=empty('E01_'+label+'_SHOULDER',(s*.465,.008,2.732),root)
    upper=empty('E01_'+label+'_UPPER_ARM',(s*.505,.005,2.677),root)
    forearm=empty('E01_'+label+'_FOREARM',(s*.596,-.013,2.311),root)
    hand=empty('E01_'+label+'_HAND',(s*.664,-.043,1.951),root)
    cylinder(label+' shoulder rotary joint',(s*.345,.018,2.736),(s*.520,.018,2.736),.133,dark,BODY,shoulder,40,.004)
    # Shoulder silhouette is a clipped trapezoid, with lower side return.
    pts=[(.397,-.151,2.925),(.592,-.183,2.967),(.715,-.133,2.885),(.743,-.140,2.699),(.659,-.191,2.576),(.417,-.194,2.628)]
    back=[(x,y+.306,z+.005) for x,y,z in pts]
    rings(label+' broad clipped shoulder shell',[mirrored(pts,s),mirrored(back,s)],white,BODY,shoulder,.014)
    plate(label+' shoulder front inset face',mirrored([(.429,-.204,2.892),(.587,-.223,2.923),(.680,-.187,2.865),(.687,-.190,2.720),(.622,-.216,2.636),(.447,-.220,2.675)],s),(0,.014,0),white,BODY,shoulder,.006)
    box(label+' shoulder inner top mount',(s*.397,.059,2.914),(.072,.139,.127),edge,BODY,shoulder,.011)
    cylinder(label+' upper arm internal frame',(s*.518,.006,2.669),(s*.594,-.008,2.348),.077,dark,BODY,upper,24,.005)
    rows=[oct_ring(s*.533,-.008,2.629,.184,.215),oct_ring(s*.587,-.014,2.395,.161,.183)]
    rings(label+' upper arm armor',rows,white,BODY,upper,.010)
    cylinder(label+' elbow transverse hinge',(s*.531,-.018,2.326),(s*.665,-.018,2.326),.083,steel,BODY,forearm,40,.003)
    cylinder(label+' elbow outer dark ring',(s*.659,-.018,2.326),(s*.681,-.018,2.326),.085,dark,BODY,forearm,40,.003)
    cylinder(label+' elbow end cap',(s*.680,-.018,2.326),(s*.687,-.018,2.326),.062,edge,BODY,forearm,40,.003)
    cylinder(label+' forearm inner frame',(s*.611,-.031,2.275),(s*.666,-.045,1.984),.074,dark,BODY,forearm,24,.003)
    rings(label+' long tapered forearm shell',[oct_ring(s*.620,-.034,2.272,.250,.244),oct_ring(s*.628,-.052,2.159,.235,.278),oct_ring(s*.665,-.050,1.963,.172,.218)],white,BODY,forearm,.014)
    cylinder(label+' wrist',(s*.667,-.040,1.981),(s*.668,-.040,1.890),.058,steel,BODY,hand,32,.003)
    box(label+' hand primary fist',(s*.670,-.062,1.841),(.163,.169,.157),dark,BODY,hand,.015)
    box(label+' hand armor back',(s*.670,-.143,1.863),(.139,.032,.122),white,BODY,hand,.008)
    box(label+' thumb mass',(s*.589,-.089,1.831),(.061,.10,.086),dark,BODY,hand,.015)

# Legs: high hips, separated knee hardware, full calf profile, broad boot wedge.
for s,label in ((-1,'R'),(1,'L')):
    thigh=empty('E01_'+label+'_THIGH',(s*.236,.011,1.967),root)
    calf=empty('E01_'+label+'_CALF',(s*.338,.012,1.231),root)
    foot=empty('E01_'+label+'_FOOT',(s*.420,.016,.380),root)
    cylinder(label+' hip axis',(s*.174,.008,1.941),(s*.318,.008,1.941),.104,dark,BODY,thigh,32,.004)
    cylinder(label+' thigh structural core',(s*.256,.013,1.921),(s*.338,.01,1.274),.094,dark,BODY,thigh,32,.004)
    rings(label+' long thigh primary armor',[oct_ring(s*.267,-.016,1.900,.289,.305),oct_ring(s*.296,-.025,1.729,.337,.347),oct_ring(s*.337,-.019,1.340,.222,.271)],white,BODY,thigh,.015)
    plate(label+' upper thigh floating front',[(s*.267-.103,-.184,1.895),(s*.267+.103,-.184,1.895),(s*.277+.103,-.206,1.805),(s*.277-.103,-.206,1.805)],(0,.02,0),white,BODY,thigh,.008)
    cylinder(label+' knee bearing axle',(s*.237,.014,1.240),(s*.436,.014,1.240),.111,dark,BODY,calf,48,.004)
    cylinder(label+' knee outer steel bearing',(s*.431,.014,1.240),(s*.449,.014,1.240),.094,steel,BODY,calf,48,.003)
    cylinder(label+' knee outer inset',(s*.449,.014,1.240),(s*.455,.014,1.240),.072,dark,BODY,calf,48,.002)
    # Front knee guard follows the top of the shin without covering the side joint.
    cx=s*.349
    plate(label+' knee armored frontal cap',[(cx-.074,-.158,1.367),(cx+.074,-.158,1.367),(cx+.083,-.212,1.183),(cx+.062,-.211,1.118),(cx-.062,-.211,1.118),(cx-.083,-.212,1.183)],(0,.055,0),white,BODY,calf,.010)
    cylinder(label+' shin rear structural strut',(s*.357,.066,1.171),(s*.416,.063,.432),.086,dark,BODY,calf,32,.004)
    rings(label+' lower leg main shell',[oct_ring(s*.367,.019,1.129,.324,.334),oct_ring(s*.376,.026,.944,.373,.385),oct_ring(s*.414,.022,.442,.209,.258)],white,BODY,calf,.014)
    # Distinct bulky calf upper shoulder over a narrow straight ankle.
    cx=s*.379
    plate(label+' outer calf layered shield',[(cx+s*.046,-.117,1.096),(cx+s*.155,-.062,1.140),(cx+s*.190,-.018,.989),(cx+s*.143,-.070,.834),(cx+s*.079,-.156,.899)],(0,.045,0),white,BODY,calf,.010)
    cylinder(label+' exposed ankle axis',(s*.324,.006,.394),(s*.516,.006,.394),.082,dark,BODY,foot,40,.004)
    cylinder(label+' ankle outer cover',(s*.502,.006,.394),(s*.528,.006,.394),.061,steel,BODY,foot,40,.003)
    cx=s*.427
    # Boot side silhouette: long low toe and a separate raised heel.
    hull(label+' broad dark boot sole',[(cx-.155,-.455,.060),(cx+.155,-.455,.060),(cx+.192,-.331,.056),(cx+.166,.202,.057),(cx-.166,.202,.057),(cx-.192,-.331,.056),(cx-.162,-.422,.151),(cx+.162,-.422,.151),(cx+.171,.136,.214),(cx-.171,.136,.214)],dark,BODY,foot,.012)
    hull(label+' white sloped foot instep',[(cx-.102,-.365,.160),(cx+.102,-.365,.160),(cx-.146,-.272,.173),(cx+.146,-.272,.173),(cx-.104,-.075,.340),(cx+.104,-.075,.340),(cx-.121,.068,.320),(cx+.121,.068,.320),(cx-.151,.137,.163),(cx+.151,.137,.163)],white,BODY,foot,.011)
    plate(label+' boot dark toe cap',[(cx-.129,-.452,.085),(cx+.129,-.452,.085),(cx+.145,-.380,.177),(cx+.109,-.319,.198),(cx-.109,-.319,.198),(cx-.145,-.380,.177)],(0,.042,-.008),edge,BODY,foot,.009)
    box(label+' square boot heel',(cx,.169,.143),(.280,.168,.178),dark,BODY,foot,.013)
    # White ankle wrap is angled down toward the front, like the sheet.
    plate(label+' slanted ankle front cuff',[(cx-.133,-.126,.465),(cx+.133,-.126,.465),(cx+.160,-.199,.327),(cx-.160,-.199,.327)],(0,.055,.017),white,BODY,foot,.012)
    pts=[(cx+s*.145,-.07,.442),(cx+s*.190,.004,.416),(cx+s*.184,.090,.295),(cx+s*.108,.086,.290),(cx+s*.117,-.033,.353)]
    plate(label+' ankle outer pentagonal cap',pts,(-s*.035,0,0),white,BODY,foot,.009)

# Modular backpack and two downward rear exhausts, still primary masses.
box('Backpack mounting spine',(0,.248,2.593),(.402,.104,.468),dark,PACK,pack_root,.018)
box('Backpack dark outer housing',(0,.350,2.615),(.506,.217,.513),dark,PACK,pack_root,.021)
box('Backpack main white rear panel',(0,.477,2.644),(.414,.058,.394),white,PACK,pack_root,.016)
for s,label in ((-1,'R'),(1,'L')):
    box(label+' backpack side shell',(s*.243,.352,2.640),(.063,.192,.426),white,PACK,pack_root,.012)
    box(label+' backpack upper mount',(s*.185,.299,2.917),(.057,.123,.115),edge,PACK,pack_root,.008)
    box(label+' backpack recessed upright',(s*.169,.510,2.621),(.026,.012,.205),dark,PACK,pack_root,.004)
    tube(label+' backpack thruster outer',(s*.111,.379,2.376),(s*.111,.479,2.262),.078,.059,edge,PACK,pack_root,40)
    tube(label+' backpack thruster machined lip',(s*.111,.465,2.277),(s*.111,.488,2.250),.076,.059,steel,PACK,pack_root,40)
    cylinder(label+' backpack exhaust darkness',(s*.111,.416,2.334),(s*.111,.426,2.320),.054,rubber,PACK,pack_root,32,.001)
box('Backpack lower service block',(0,.492,2.426),(.219,.048,.111),dark,PACK,pack_root,.009)

# Pack the original sheet into the editable project as an explicit visual source.
reference = next(Path('D:/project-mecha-design').glob('*.png'))
im=bpy.data.images.load(str(reference),check_existing=True)
im.name='USER REFERENCE | Type-01 design sheet'
im.pack()
ref=bpy.data.objects.new('REFERENCE - user supplied four-view sheet',None)
REF.objects.link(ref)
ref.empty_display_type='IMAGE'
ref.data=im
ref.empty_display_size=4
ref.location=(4,1,1.8)
ref.rotation_euler=(math.pi/2,0,0)
ref.hide_render=True

# Large softboxes keep true geometry legible; there are no artificial line overlays.
if sc.world is None:
    sc.world=bpy.data.worlds.new('E01 neutral studio world')
sc.world.color=(.1,.1,.1)
sc.world.use_nodes=True
sc.world.node_tree.nodes['Background'].inputs['Color'].default_value=(.36,.40,.45,1)
sc.world.node_tree.nodes['Background'].inputs['Strength'].default_value=.42
floor=mesh('Studio ground',[(-100,-100,0),(100,-100,0),(100,100,0),(-100,100,0)],[(0,1,2,3)],ground,STUDIO,None,0)
ASSET.remove(floor)

def light(name,loc,energy,size,color,target=(0,0,1.7)):
    d=bpy.data.lights.new(name,'AREA');d.energy=energy;d.shape='DISK';d.size=size;d.color=color
    o=bpy.data.objects.new(name,d);STUDIO.objects.link(o);o.location=loc
    o.rotation_euler=(Vector(target)-o.location).to_track_quat('-Z','Y').to_euler()

light('Large neutral key',(-3.5,-4.4,6.0),650,4.0,(1,.95,.88))
light('Cool frontal fill',(3.5,-2.6,3.7),430,3.0,(.79,.88,1))
light('Broad rear rim',(1.2,3.5,5.5),850,3.0,(.94,.97,1))
light('Soft face fill',(-.2,-3.4,3.2),60,1.7,(1,1,1),target=(0,0,3.0))

def camera(name,loc,target,scale):
    d=bpy.data.cameras.new(name);d.type='ORTHO';d.ortho_scale=scale;d.lens=60
    o=bpy.data.objects.new(name,d);CAMS.objects.link(o);o.location=loc
    o.rotation_euler=(Vector(target)-o.location).to_track_quat('-Z','Y').to_euler()
    return o

camera('E01_FRONT',(0,-8,1.67),(0,0,1.67),3.62)
camera('E01_BACK',(0,8,1.67),(0,0,1.67),3.62)
camera('E01_LEFT',(8,0,1.67),(0,0,1.67),3.62)
camera('E01_RIGHT',(-8,0,1.67),(0,0,1.67),3.62)
camera('E01_3Q',(5.2,-8,4.1),(0,0,1.65),3.78)
camera('E01_HEAD_FRONT',(0,-6,3.055),(0,0,3.055),.57)
camera('E01_HEAD_SIDE',(6,-.08,3.055),(0,0,3.055),.57)
camera('E01_HEAD_3Q',(3.7,-6,3.60),(0,-.008,3.057),.59)

sc.camera=bpy.data.objects['E01_3Q']
sc.render.resolution_x=1050;sc.render.resolution_y=1400
bpy.context.view_layer.update()
bpy.ops.object.select_all(action='DESELECT')
root.select_set(True);bpy.context.view_layer.objects.active=root
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            sp=area.spaces.active
            sp.shading.type='MATERIAL'
            sp.overlay.show_extras=False
            sp.region_3d.view_distance=5.9
            sp.region_3d.view_location=Vector((0,0,1.6))
            sp.region_3d.view_rotation=sc.camera.rotation_euler.to_quaternion()
readme=bpy.data.texts.new('READ ME - FIRST APPROVAL GATE')
readme.write('TYPE E-01 | Stage 01\n\nUser review: whole-body proportion and head structure.\nBody and backpack are intentionally at primary-form stage.\nNo rifle, fine body details, wear, final action pose or game export yet.\nAll meshes, materials and bevels are editable; user reference is packed.\nDo not continue to body detail until user confirms this gate.\n')
blend=OUT/'TYPE_E01_STAGE01.blend'
bpy.ops.wm.save_as_mainfile(filepath=str(blend),compress=True)
stats={'stage':'01 silhouette and head approval','file':str(blend),'mesh_objects':len([o for o in ASSET if o.type=='MESH']),'base_vertices':sum(len(o.data.vertices) for o in ASSET if o.type=='MESH'),'head_parts':len(HEAD.objects),'editable':True,'game_export':False,'awaiting_user_approval':True}
(OUT/'stage_report.json').write_text(json.dumps(stats,indent=2),encoding='utf8')
print('STAGE01 SAVED '+json.dumps(stats),flush=True)

if '--render' in sys.argv:
    prefs=bpy.context.preferences.addons['cycles'].preferences
    try:
        prefs.compute_device_type='OPTIX';prefs.get_devices()
        for device in prefs.devices:device.use=device.type=='OPTIX'
        sc.cycles.device='GPU'
    except Exception:
        sc.cycles.device='CPU'
    for shot in ('FRONT','LEFT','BACK','RIGHT','3Q','HEAD_FRONT','HEAD_SIDE','HEAD_3Q'):
        sc.camera=bpy.data.objects['E01_'+shot]
        if shot.startswith('HEAD'):
            sc.render.resolution_x=900;sc.render.resolution_y=900
        else:
            sc.render.resolution_x=840;sc.render.resolution_y=1120
        sc.render.filepath=str(OUT/'renders'/(shot+'.png'))
        bpy.ops.render.render(write_still=True)
        print('STAGE01 RENDERED '+shot,flush=True)
