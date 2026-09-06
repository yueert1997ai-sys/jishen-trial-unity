"""HEAD_V2 primary forms. Local Blender 5.2, entirely editable polygon meshes.
Run: blender.exe -b --factory-startup --python build_head.py -- --config iterations/ITER01.json
No body import, detail scattering, texture, emission, sphere, or subdivision surface.
Coordinates are anatomical landmark units; -X anatomical right, -Y forward, Z up.
One unit = 0.26 metres; crown datum = existing asset's 3.25 m.
"""
import bpy, bmesh, math, json, sys, pathlib, shutil
from mathutils import Vector
from mathutils.geometry import tessellate_polygon

ROOT = pathlib.Path(__file__).resolve().parent
arg = sys.argv[sys.argv.index('--')+1:]
cfg_path = ROOT / arg[arg.index('--config')+1]
C = json.loads(cfg_path.read_text(encoding='utf-8'))
ITER = C['iteration']; OUT = ROOT / 'iterations' / ('HEAD_V2_ITER%02d' % ITER)
OUT.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
sc = bpy.context.scene
sc.unit_settings.system='METRIC'; sc.unit_settings.scale_length=1
U=.26; BASE=2.9848
def world(p): return Vector((p[0]*U,p[1]*U,BASE+p[2]*U))
def collection(name):
    c=bpy.data.collections.new(name); sc.collection.children.link(c); return c
HC=collection('HEAD_V2 | PRIMARY FORMS ONLY')
RC=collection('REFERENCE | packed concept images'); RC.hide_render=True
CC=collection('CAMERAS | fixed across iterations')
LC=collection('STUDIO | neutral clay review')
LM=collection('LANDMARKS | inspection datums'); LM.hide_render=True
root=bpy.data.objects.new('HEAD_V2_ROOT',None); HC.objects.link(root)
root['stage']='PRIMARY FORM ONLY - awaiting silhouette review'
root['source']='User HEAD DETAIL; reference lettering is not a dimensional specification.'
root['iteration']=ITER
root['crown_height_m']=3.25
root['head_only']=True
clay=bpy.data.materials.new('CLAY | neutral gray, no emission, no textures')
clay.use_nodes=True; clay.diffuse_color=(.34,.34,.34,1)
p=next(n for n in clay.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
p.inputs['Base Color'].default_value=(.34,.34,.34,1)
p.inputs['Roughness'].default_value=.78; p.inputs['Metallic'].default_value=0
new=[]

def mesh(name, verts, faces, bevel=.004):
    # ITER02: lower-face length is corrected at the same eye-line datum.
    if name.startswith('09_'):
        verts=[(x,y,.409+(z-.409)*C.get('mask_height_factor',1)) for x,y,z in verts]
    if name.startswith('12_'):
        fi=C.get('fin_inset',0)
        verts=[(x-math.copysign(fi,x),y,z) for x,y,z in verts]
    width=C.get('head_width_factor',1)
    if name.startswith(('13_','14_','15_','16_')):
        verts=[(x*width,.075+(y-.075)*width,z) for x,y,z in verts]
        if name.startswith('13_'):
            verts=[(x,y,-.049+(z+.049)*width) for x,y,z in verts]
    else:verts=[(x*width,y,z) for x,y,z in verts]
    k=C.get('lower_face_factor',1)
    if name.startswith(('04_','05_','07_','08_','09_','10_','11_')):
        verts=[(x,y,.47+(z-.47)*k if z<.47 else z) for x,y,z in verts]
    elif name.startswith(('13_','14_','15_','16_')):
        verts=[(x,y,z+.53*(1-k)) for x,y,z in verts]
    me=bpy.data.meshes.new(name+'_EditableMesh')
    me.from_pydata([world(v) for v in verts],[],faces); me.update()
    bm=bmesh.new(); bm.from_mesh(me); bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces)); bm.to_mesh(me); bm.free()
    o=bpy.data.objects.new(name,me); HC.objects.link(o); o.parent=root
    o.data.materials.append(clay)
    if bevel:
        m=o.modifiers.new('Controlled edge chamfer','BEVEL'); m.width=bevel*U; m.segments=2
        m.limit_method='ANGLE'; m.angle_limit=math.radians(25); m.harden_normals=True
        n=o.modifiers.new('Planar weighted normals','WEIGHTED_NORMAL'); n.keep_sharp=True; n.weight=40
    o['construction']='Closed editable primary armor volume; no micro detail'
    new.append(o); return o

def skin(name, verts, faces, inward, bevel=.004):
    """Close a connected faceted panel with explicit inward thickness and side walls."""
    n=len(verts); vs=list(verts)+[tuple(Vector(v)+Vector(inward)) for v in verts]
    fs=list(faces)+[tuple(i+n for i in reversed(f)) for f in faces]
    edges={}
    for f in faces:
        for a,b in zip(f, f[1:]+f[:1]):
            key=tuple(sorted((a,b)))
            if key in edges: edges[key]=None
            else: edges[key]=(a,b)
    for edge in edges.values():
        if edge:
            a,b=edge; fs.append((a,b,b+n,a+n))
    return mesh(name,vs,fs,bevel)

def plate(name, points, thickness=(0,.026,0), bevel=.003):
    vv=[Vector(p) for p in points]
    ts=tessellate_polygon([vv]); fs=[]
    for t in ts:
        fs.append(tuple(i if isinstance(i,int) else min(range(len(vv)),key=lambda j:(vv[j]-i).length_squared) for i in t))
    return skin(name,points,fs,thickness,bevel)

def mirror(points,s): return [(s*x,y,z) for x,y,z in points]
def hull(name,vs,bevel=.003):
    bm=bmesh.new()
    for v in vs:bm.verts.new(v)
    bmesh.ops.convex_hull(bm,input=list(bm.verts),use_existing_faces=False)
    bm.verts.ensure_lookup_table();bm.verts.index_update()
    pts=[tuple(v.co) for v in bm.verts];fs=[tuple(v.index for v in f.verts) for f in bm.faces]
    bm.free();return mesh(name,pts,fs,bevel)
def join_objects(objects,name):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects:o.select_set(True)
    bpy.context.view_layer.objects.active=objects[0]; bpy.ops.object.join()
    obj=objects[0];obj.name=name
    for o in objects[1:]:
        if o in new:new.remove(o)
    return obj

def axial(name,a,b,r,vertices=48):
    aa=Vector(a);bb=Vector(b);d=(bb-aa).normalized()
    v=d.cross(Vector((0,0,1)))
    if v.length<.01:v=d.cross(Vector((0,1,0)))
    v.normalize();w=d.cross(v)
    vs=[tuple(p+r*(v*math.cos(i*math.tau/vertices)+w*math.sin(i*math.tau/vertices))) for p in (aa,bb) for i in range(vertices)]
    fs=[tuple(reversed(range(vertices))),tuple(range(vertices,2*vertices))]
    fs += [(i,(i+1)%vertices,(i+1)%vertices+vertices,i+vertices) for i in range(vertices)]
    return mesh(name,vs,fs,.002)

def ring(name,z,ro,ri,height,cy=.075,n=48):
    vs=[(r*math.cos(i*math.tau/n),cy+r*math.sin(i*math.tau/n),zz) for zz in (z-height/2,z+height/2) for r in (ro,ri) for i in range(n)]
    fs=[]
    for i in range(n):
        j=(i+1)%n
        fs.extend([(i,j,2*n+j,2*n+i),(n+j,n+i,3*n+i,3*n+j),(2*n+i,2*n+j,3*n+j,3*n+i),(j,i,n+i,n+j)])
    return mesh(name,vs,fs,.002)

# The crown is a narrow ridge and two chamfered planes, never a rounded cap.
crown_width=C.get('crown_width',1)
cv=[(-.112*crown_width,-.033,.985),(.112*crown_width,-.033,.985),(-.100*crown_width,.12,1.02),(.100*crown_width,.12,1.02),(-.090*crown_width,.279,.925),(.090*crown_width,.279,.925)]
skin('01_Crown_Central_Keel',cv,[(0,1,3,2),(2,3,5,4)],(0,0,-.039),.0035)
fw=C.get('forehead_width',1)
fv=[(-.105*fw,-.045,.975),(.105*fw,-.045,.975),(-.103*fw,-.265,.797),(.103*fw,-.265,.797),(-.065*fw,-.406,.597),(.065*fw,-.406,.597)]
skin('02_Forehead_Armored_Wedge',fv,[(0,1,3,2),(2,3,5,4)],(0,.035,-.006),.004)

# Shared front/temple surface landmarks. Each side is a connected, thick armor panel.
temple=C.get('temple_width',1)
for s,side in ((-1,'R'),(1,'L')):
    if C.get('continuous_shell'):
        frontcoords=[(.124,.976),(.255,.895),(.365*temple,.700),(.350*temple,.615),(.080,.550),(.115,.789)]
        vs=[(x,-.42+(z-.55)*.82+.22*(x-.08),z) for x,z in frontcoords]
        vs += [(.344*temple,.197,.815),(.436*temple,.150,.604),(.428*temple,-.001,.534),(.109,.274,.918)]
        fs=[(0,1,2,3,4,5),(1,6,7,2),(2,7,8,3),(0,9,6,1)]
        if C.get('integrated_face'):
            vs += [(.113,.119,1.009)]
            fs=fs[:-1]+[(0,10,1),(10,6,1),(10,9,6)]
    else:
        vs=[(.124,-.026,.976),(.255,.038,.885),(.389*temple,-.126,.700),(.355*temple,-.289,.628),(.080,-.418,.550),(.115,-.278,.789),(.251,-.240,.779),(.344*temple,.197,.815),(.436*temple,.150,.604),(.428*temple,-.001,.534)]
        fs=[(0,1,6,5),(1,2,6),(2,3,4,6),(4,5,6),(1,7,8,2),(2,8,9,3)]
    skin('03_Helmet_FrontoTemporal.'+side,mirror(vs,s),fs,(-s*.025,.017,-.009),.004)

# Faceted occipital shield with sloping nape return; no round rear helmet.
rear=C.get('rear_depth',1)
rv=[(-.097,.294*rear,.917),(.097,.294*rear,.917),(-.34*temple,.217*rear,.809),(.34*temple,.217*rear,.809),(-.410*temple,.279*rear,.598),(.410*temple,.279*rear,.598),(-.350,.361*rear,.374),(.350,.361*rear,.374),(-.230,.245*rear,.181),(.230,.245*rear,.181),(-.21,.383*rear,.667),(.21,.383*rear,.667)]
if C.get('continuous_shell'):
    rv[4]=(-.432*temple,.167*rear,.598);rv[5]=(.432*temple,.167*rear,.598)
rf=[(0,1,11,10),(0,10,4,2),(1,3,5,11),(10,11,7,6),(10,6,4),(11,5,7),(6,7,9,8)]
skin('04_Rear_Head_Occipital_Armor',rv,rf,(0,-.035,.008),.004)

# Integral inner cheek-bearing chassis, with a real front opening for mask/optics.
frameparts=[]
for s,side in ((-1,'R'),(1,'L')):
    if C.get('integrated_face'):
        pp=[(.318,-.186,.529),(.348,.142,.548),(.314,.274,.301),(.247,.195,.115),(.179,-.055,.058),(.232,-.225,.145),(.299,-.253,.317)]
        vs=pp+[(x-.055,y,z) for x,y,z in pp]
        frameparts.append(hull('05_Chassis_Inner_CheekBearing.'+side,mirror(vs,s),.003))
    elif C.get('continuous_shell'):
        yz=[(-.171,.528),(.148,.548),(.290,.300),(.204,.099),(-.060,.014),(-.247,.125),(-.272,.307)]
        pp=[(.337,y,z) for y,z in yz]
        frameparts.append(plate('05_Chassis_Inner_CheekBearing.'+side,mirror(pp,s),(-s*.080,0,0),.004))
    else:
        pp=[(.285,-.192,.55),(.405,-.093,.526),(.391,.206,.404),(.227,.224,.098),(.142,.055,.008),(.214,-.215,.052),(.275,-.270,.295)]
        frameparts.append(plate('05_Chassis_Inner_CheekBearing.'+side,mirror(pp,s),(-s*.045,.012,0),.004))
if C.get('integrated_face'):
    # Closed, faceted internal cranium: backs the panel gaps, no exposed spherical core.
    loops=[[(.071,-.058,.958),(.164,-.015,.926),(.225,.11,.884),(.16,.255,.872),(-.16,.255,.872),(-.225,.11,.884),(-.164,-.015,.926),(-.071,-.058,.958)],
           [(.079,-.367,.675),(.304,-.235,.681),(.390,.111,.621),(.224,.33,.575),(-.224,.33,.575),(-.390,.111,.621),(-.304,-.235,.681),(-.079,-.367,.675)],
           [(.07,-.40,.513),(.29,-.26,.513),(.36,.09,.484),(.22,.28,.411),(-.22,.28,.411),(-.36,.09,.484),(-.29,-.26,.513),(-.07,-.40,.513)]]
    if C.get('surface_order_fix'):
        # Front panel's analytic plane is the hard outer limit for its inner lining.
        loops=[[(x,max(y+.035,-.42+(z-.55)*.82+.22*(abs(x)-.08)+.042) if y<0 else y,z) for x,y,z in loop] for loop in loops]
    vs=sum(loops,[]);fs=[tuple(reversed(range(8))),tuple(range(16,24))]
    for j in range(2):
        for i in range(8):fs.append((j*8+i,j*8+(i+1)%8,(j+1)*8+(i+1)%8,(j+1)*8+i))
    frameparts.append(mesh('05_Inner_Cranial_Bridge',vs,fs,.002))
join_objects(frameparts,'05_Inner_Chassis_Cranium_And_Cheek_Bearings')

for s,side in ((-1,'R'),(1,'L')):
    # The brow is one broad angular armor piece, tied back to the temple corner.
    bpts=[(0,-.501,.498),(.099,-.445,.577),(.254,-.340,.662),(.364*temple,-.241,.727),(.393*temple,-.174,.686),(.335*temple,-.278,.570),(.190,-.407,.517),(0,-.507,.431)]
    drop=C.get('brow_drop',0)
    bpts=[(x,y,z+drop) for x,y,z in bpts]
    if C.get('continuous_shell'):
        bpts=[(0,-.51,.497),(.353*temple,-.277,.657),(.401*temple,-.148,.719),(.416*temple,-.129,.650),(.353*temple,-.280,.574),(0,-.514,.425)]
        skin('06_Outer_Armor_Brow.'+side,mirror(bpts,s),[(0,1,4,5),(1,2,3,4)],(0,.034,.003),.003)
    else:plate('06_Outer_Armor_Brow.'+side,mirror(bpts,s),(0,.034,.003),.003)

    # Closed recessed housing. Straight 15-degree optic, no curved expressive lens.
    eye_length=C.get('eye_length',1)
    outer=[(.044,.443),(.064,.473),(.267*eye_length,.523),(.294*eye_length,.495),(.262*eye_length,.427),(.070,.384)]
    inner=[(.075,.441),(.088,.451),(.248*eye_length,.491),(.262*eye_length,.477),(.240*eye_length,.452),(.095,.416)]
    if C.get('buried_eyes'):
        outer=[(.050,.451),(.079,.472),(.265*eye_length,.513),(.290*eye_length,.490),(.273*eye_length,.443),(.080,.396)]
        inner=[(.083,.442),(.096,.459),(.248*eye_length,.492),(.268*eye_length,.473),(.251*eye_length,.452),(.091,.414)]
    if C.get('integrated_face'):
        # The outer loop follows facial support boundaries, not the optic outline.
        # Thus the aperture is cut into a full orbital bed rather than a floating rim.
        outer=[(.018,.473),(.285,.569),(.359,.464),(.258,.272),(.108,.276),(.036,.353)]
        inner=[(.078,.448),(.100,.463),(.243*eye_length,.496),(.269*eye_length,.478),(.247*eye_length,.458),(.094,.422)]
    depth=C.get('eye_depth',.047)
    front=lambda x:-.476+.51*x+C.get('eye_inset',0)
    if C.get('integrated_face'):front=lambda x:-.452+.40*x
    face_y=lambda x,z:front(x)
    if C.get('surface_order_fix'):
        # Lower and lateral orbital support tucks behind mask/cheek contours.
        # Recessing only the optic itself is not sufficient: its entire carrier must seat.
        face_y=lambda x,z:-.473+.55*x+.70*max(0,.46-z)+.60*max(0,x-.245)
    a=[(x,face_y(x,z),z) for x,z in outer]
    b=[(x,face_y(x,z)+.004,z) for x,z in inner]
    d=[(x,face_y(x,z)+depth,z) for x,z in inner]
    back=[(x,face_y(x,z)+depth+.022,z) for x,z in outer]
    n=6;vs=a+b+d+back;fs=[]
    for i in range(n):
        j=(i+1)%n
        fs += [(i,j,n+j,n+i),(n+i,n+j,2*n+j,2*n+i),(i,3*n+i,3*n+j,j)]
    fs += [tuple(range(2*n,3*n)),tuple(reversed(range(3*n,4*n)))]
    mesh('07_Orbital_Bed_With_Recessed_Cavity.'+side,mirror(vs,s),fs,.002)
    lp=[(.103,.445),(.236*eye_length,.478),(.244*eye_length,.466),(.110,.433)]
    plate('08_Inner_Optic_Clay_NoEmission.'+side,mirror([(x,face_y(x,z)+depth-.006,z) for x,z in lp],s),(0,.009,0),.001)

# Narrow shield; the central ridge stays behind the brow in side elevation.
mask_width=C.get('mask_width',1); mask_y=C.get('mask_y',0)
mv=[(0,-.460,.377),(.193*mask_width,-.335,.409),(.166*mask_width,-.330,.215),(.083*mask_width,-.337,.065),(0,-.418,.035),(-.083*mask_width,-.337,.065),(-.166*mask_width,-.330,.215),(-.193*mask_width,-.335,.409),(0,-.466,.222)]
mv=[(x,y+mask_y,z) for x,y,z in mv]
mf=[(0,1,2,8),(2,3,4,8),(4,5,6,8),(6,7,0,8)]
skin('09_Face_Mask_Independent_Faceted_Shield',mv,mf,(0,.030,0),.003)

for s,side in ((-1,'R'),(1,'L')):
    cp=[(.348,-.269,.453),(.418*temple,-.133,.530),(.409*temple,-.139,.338),(.333,-.249,.182),(.247,-.342,.033),(.244,-.369,.172),(.317,-.320,.321)]
    if C.get('integrated_face'):
        cp=[(x,-.68+1.26*x,z) for x,z in [(.341,.455),(.413*temple,.536),(.408*temple,.360),(.34,.230),(.24,.060),(.245,.190),(.315,.342)]]
    cp=[(x*C.get('cheek_width',1),y,z) for x,y,z in cp]
    plate('10_Outer_Cheek_Armor.'+side,mirror(cp,s),(-s*.020,.024,0),.003)

jv=[(-.090,-.305,.087),(-.072,-.382,-.025),(.072,-.382,-.025),(.090,-.305,.087),(-.098,-.202,.067),(-.064,-.240,-.060),(.064,-.240,-.060),(.098,-.202,.067)]
if C.get('surface_order_fix'):
    jv[4]=(-.146,-.106,.067);jv[7]=(.146,-.106,.067)
if C.get('mask_height_factor',1)<1:
    jv[0]=(-.09,-.305,.135);jv[3]=(.09,-.305,.135)
mesh('11_Jaw_Chin_Armored_Cradle',jv,[(0,1,2,3),(4,7,6,5),(0,4,5,1),(3,2,6,7),(1,5,6,2),(0,3,7,4)],.003)

# Exactly two short directional fins with solid keyed roots, no extra aerials.
for s,side in ((-1,'R'),(1,'L')):
    f=C.get('fin_height',1)
    pts=[(.292,-.055,.808),(.347,-.008,.793),(.401,.099,.836),(.413,.094,.866+f*.318),(.368,.038,.970)]
    o=plate('12_Short_Sensor_Fin_With_Keyed_Mount.'+side,mirror(pts,s),(s*.026,.012,0),.002)
    o['attachment']='Integral keyed lower root seats inside temporal armor'

axial('13_Central_Neck_Pitch_Joint',(-.150,.063,-.049),(.150,.063,-.049),.093)
neck_raise=C.get('neck_ring_raise',0)
ring('14_Low_Profile_Neck_Yaw_Ring',-.199+neck_raise,.194,.139,.046)
forks=[]
for s,side in ((-1,'R'),(1,'L')):
    pts=[(.151,-.033,-.188),(.151,-.033,-.035),(.151,.038,.046),(.151,.133,.028),(.151,.167,-.180)]
    pts=[(x,y,z+neck_raise if z<-.15 else z) for x,y,z in pts]
    forks.append(plate('15_Neck_Pitch_Support.'+side,mirror(pts,s),(s*.042,0,0),.004))
join_objects(forks,'15_Neck_Twin_Pitch_Supports')
if C.get('integrated_face'):
    ring('16_Neck_Armor_Collar',-.197+neck_raise,.239,.185,.074,n=8)
else:
    collar=[(-.221,-.077,-.239),(-.206,-.155,-.209),(-.147,-.171,-.119),(.147,-.171,-.119),(.206,-.155,-.209),(.221,-.077,-.239)]
    plate('16_Neck_Armor_Collar',collar,(0,.076,-.004),.004)

# Fixed eight reference landmarks, editable empties hidden from renders.
landmarks={'TOP':(0,.12,1.02),'ANTENNA ROOT':(-.327,-.013,.834),'FOREHEAD':(0,-.332,.70),'EYE LINE':(-.175,-.346,.459),'CHEEK WIDTH':(-.418*temple,-.133,.53),'MASK TIP':(0,-.418+mask_y,.035),'CHIN':(0,-.382,-.025),'REAR HEAD':(0,.383*rear,.55)}
for name,(x,y,z) in list(landmarks.items()):
    if name=='ANTENNA ROOT':x+=C.get('fin_inset',0)
    if name=='MASK TIP':z=.409+(z-.409)*C.get('mask_height_factor',1)
    landmarks[name]=(x*C.get('head_width_factor',1),y,z)
for name in ('EYE LINE','MASK TIP','CHIN'):
    x,y,z=landmarks[name];landmarks[name]=(x,y,.47+(z-.47)*C.get('lower_face_factor',1) if z<.47 else z)
for name,at in landmarks.items():
    o=bpy.data.objects.new('LM_'+name,None);LM.objects.link(o);o.location=world(at);o.empty_display_type='SPHERE';o.empty_display_size=.002
LM.hide_viewport=True

# Pack the supplied image into the .blend as a genuine viewport reference.
for idx,filename in enumerate(('REFERENCE_HEAD_DETAIL.png','REFERENCE_ORTHO_HEADS.png')):
    path=ROOT/'references'/filename
    if path.exists():
        im=bpy.data.images.load(str(path));im.pack()
        o=bpy.data.objects.new('REFERENCE_'+filename,None);RC.objects.link(o)
        o.empty_display_type='IMAGE';o.data=im;o.empty_display_size=.42
        o.location=(-.49-idx*.5,.16,3.12);o.rotation_euler=(math.pi/2,0,0);o.color[3]=.9
        o.empty_image_depth='BACK';o.hide_render=True

target=world((0,0,.465))
camera_positions={'HEAD_FRONT':(0,-7,.465),'HEAD_SIDE':(-7,0,.465),'HEAD_BACK':(0,7,.465),'HEAD_3Q':(-4.4,-7,2.75)}
for name,at in camera_positions.items():
    d=bpy.data.cameras.new(name);o=bpy.data.objects.new(name,d);CC.objects.link(o)
    o.location=world(at);o.rotation_euler=(target-o.location).to_track_quat('-Z','Y').to_euler()
    d.type='ORTHO';d.ortho_scale=1.70*U;d.lens=90;d.clip_start=.01;d.clip_end=100
sc.camera=bpy.data.objects['HEAD_3Q']

for name,at,power,size in [('Key',(-3,-4,5),105,2.8),('Fill',(4,-3,2),65,3.2),('TopRear',(1,3,4),85,2.4)]:
    d=bpy.data.lights.new(name,'AREA');d.energy=power*C.get('light_energy_factor',1);d.shape='DISK';d.size=size*U
    o=bpy.data.objects.new(name,d);LC.objects.link(o);o.location=world(at);o.rotation_euler=(world((0,0,.4))-o.location).to_track_quat('-Z','Y').to_euler()
sc.world=bpy.data.worlds.new('Light gray neutral environment');sc.world.use_nodes=True
sc.world.node_tree.nodes['Background'].inputs[0].default_value=(.58,.58,.58,1)
sc.world.node_tree.nodes['Background'].inputs[1].default_value=.65
sc.render.engine='CYCLES';sc.cycles.samples=48;sc.cycles.use_denoising=True
pref=bpy.context.preferences.addons['cycles'].preferences
try:
    pref.compute_device_type='OPTIX';pref.get_devices()
    for d in pref.devices:d.use=d.type=='OPTIX'
    sc.cycles.device='GPU'
except Exception:sc.cycles.device='CPU'
sc.render.resolution_x=sc.render.resolution_y=1440;sc.render.resolution_percentage=100
sc.render.image_settings.file_format='PNG';sc.render.image_settings.color_mode='RGB'
sc.view_settings.view_transform='AgX';sc.view_settings.look='AgX - Medium High Contrast';sc.view_settings.exposure=-.45
sc.render.film_transparent=False
for a in bpy.context.screen.areas:
    if a.type=='VIEW_3D':
        a.spaces.active.region_3d.view_distance=.70
        a.spaces.active.region_3d.view_location=target
        a.spaces.active.region_3d.view_rotation=sc.camera.rotation_euler.to_quaternion()
        a.spaces.active.overlay.show_floor=False
        a.spaces.active.clip_start=.001
        a.spaces.active.shading.type='SOLID';a.spaces.active.shading.color_type='MATERIAL'
        a.spaces.active.overlay.show_extras=False
        a.spaces.active.region_3d.view_perspective='ORTHO'
bpy.ops.object.select_all(action='DESELECT')
bpy.context.view_layer.objects.active=bpy.data.objects['09_Face_Mask_Independent_Faceted_Shield']
sc['review']='Primary form only. Not approved, no secondary or tertiary detailing.'
sc['iteration']=ITER
sc['camera_rig']='Fixed orthographic FRONT/SIDE/BACK/3Q, same neutral clay and lights'
sc['parameters_json']=json.dumps(C,ensure_ascii=False)
bpy.data.texts.load(str(pathlib.Path(__file__).resolve()))
blend=OUT/('HEAD_V2_ITER%02d.blend'%ITER)
bpy.ops.wm.save_as_mainfile(filepath=str(blend))
report={'iteration':ITER,'blend':str(blend),'visible_primary_meshes':len([o for o in sc.objects if o.type=='MESH']),'parameters':C,'landmarks_design_units':landmarks,'camera_rig':camera_positions,'materials':1,'body_imported':False,'emission':False,'textures':False,'micro_details':False}
(OUT/'build_report.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf8')
shutil.copy2(__file__,OUT/'executed_build_head.py')
print('HEAD_V2_SAVED',json.dumps(report,ensure_ascii=False),flush=True)
