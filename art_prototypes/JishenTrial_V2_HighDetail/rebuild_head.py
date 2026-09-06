"""Head revision 04. New curved panel topology, inset optics, machined face armor.
Actual Blender mesh construction from the user's two head-detail references.
Run on V2_HIGH_DETAIL_FINAL.blend; writes V3_HEAD_REFINED_MASTER.blend.
"""
import bpy,bmesh,math,pathlib,json,time
from mathutils import Vector,Matrix
from mathutils.geometry import tessellate_polygon
OUT=pathlib.Path(__file__).resolve().parent;S=.325;T=time.time();sc=bpy.context.scene
HEAD=bpy.data.objects['Head'];HC=bpy.data.collections['HEAD'];CUT=bpy.data.collections['Construction_Cutters | hidden']
M={k:bpy.data.materials[n] for k,n in {'navy':'01_Painted_Military_Navy','panel':'02_Navy_Secondary_Panels','white':'04_Offwhite_Ceramic_Armor','frame':'05_Black_Mechanical_Frame','gun':'06_Dark_Gunmetal','steel':'07_Bare_Machined_Steel','black':'12_Optical_Recess','glass':'10_Blue_Sensor_Glass','rubber':'09_Cable_Protection'}.items()}
archive=OUT/'iterations'/'pre_head_rebuild';archive.mkdir(exist_ok=True)
import shutil
for f in ('V2_HIGH_DETAIL_FINAL.blend','V2_GAME_READY_DEMO.blend'):
    if not (archive/f).exists():shutil.copy2(OUT/f,archive/f)
old=[o for o in HEAD.children_recursive if o.type in ('MESH','CURVE')]
for o in old:bpy.data.objects.remove(o,do_unlink=True)
new=[]

def obj(name,verts,faces,mat='navy',bevel=.005,smooth=False):
    me=bpy.data.meshes.new('HEAD04_'+name+'_Mesh');me.from_pydata([Vector(p)*S for p in verts],[],faces);me.update()
    bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=bm.faces);bm.to_mesh(me);bm.free()
    o=bpy.data.objects.new('HEAD04_'+name,me);HC.objects.link(o);me.materials.append(M[mat]);o.color=M[mat].diffuse_color
    w=o.matrix_world.copy();o.parent=HEAD;o.matrix_world=w
    if bevel:
        m=o.modifiers.new('Precision machined edge radius','BEVEL');m.width=bevel*S;m.segments=3;m.harden_normals=True
    for p in me.polygons:p.use_smooth=smooth
    if not smooth:
        m=o.modifiers.new('Area weighted machined normals','WEIGHTED_NORMAL');m.keep_sharp=True;m.weight=50
    o['structural_role']='head';o['revision']='04 / reference-driven rebuilt head';new.append(o)
    return o

def panel(name,points,depth=(0,.035,0),mat='navy',bevel=.004):
    # Retains concave outlines and deliberate corners, unlike a convex-hull block.
    n=len(points);vv=[Vector(p) for p in points];indices={tuple(v):i for i,v in enumerate(vv)}
    ff=[]
    for t in tessellate_polygon([vv]):ff.append(tuple(v if isinstance(v,int) else indices[tuple(v)] for v in t))
    ff+=[tuple(n+i for i in reversed(f)) for f in list(ff)]
    ff += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    return obj(name,points+[tuple(Vector(p)+Vector(depth)) for p in points],ff,mat,bevel)

def cylinder(name,a,b,r,mat='gun',n=32,r2=None,inner=None,bevel=.002):
    a,b=Vector(a),Vector(b);q=(b-a).to_track_quat('Z','Y');r2=r if r2 is None else r2;v=[]
    circles=[(a,r),(b,r2)] if inner is None else [(a,r),(b,r2),(a,inner),(b,inner)]
    for pt,rr in circles:v.extend(tuple(pt+q@Vector((rr*math.cos(i*math.tau/n),rr*math.sin(i*math.tau/n),0))) for i in range(n))
    f=[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    if inner is None:f += [tuple(reversed(range(n))),tuple(range(n,2*n))]
    else:
        for i in range(n):
            j=(i+1)%n;f.extend([(2*n+i,3*n+i,3*n+j,2*n+j),(i,2*n+i,2*n+j,j),(n+i,n+j,3*n+j,3*n+i)])
    return obj(name,v,f,mat,bevel,True)

def bolt(name,p,n,r=.016):
    p,n=Vector(p),Vector(n).normalized()
    cylinder(name+'_Retainer',p,p+n*.012,r*1.30,'gun',24,inner=r*.67,bevel=.001)
    cylinder(name+'_Flush_Socket',p+n*.005,p+n*.016,r,'steel',8,bevel=.001)
    cylinder(name+'_Recess',p+n*.016,p+n*.017,r*.48,'black',6,bevel=0)

def cable(name,points,r=.018,mat='rubber'):
    d=bpy.data.curves.new('HEAD04_'+name+'_Path','CURVE');d.dimensions='3D';d.resolution_u=16;d.bevel_depth=r*S;d.bevel_resolution=3;d.use_fill_caps=True
    p=d.splines.new('BEZIER');p.bezier_points.add(len(points)-1)
    for k,v in zip(p.bezier_points,points):k.co=Vector(v)*S;k.handle_left_type='AUTO';k.handle_right_type='AUTO'
    o=bpy.data.objects.new('HEAD04_'+name,d);HC.objects.link(o);d.materials.append(M[mat]);w=o.matrix_world.copy();o.parent=HEAD;o.matrix_world=w;new.append(o)
    return o

def patch(name,fn,mat='navy',nu=12,nv=10,thickness=.028):
    # An editable quad patch with curvature and support rings, smoothed before thickness.
    us=[0,.017]+[.05+.90*i/(nu-4) for i in range(nu-3)]+[.983,1]
    vs=[0,.02]+[.07+.86*i/(nv-4) for i in range(nv-3)]+[.98,1]
    verts=[fn(u,v) for v in vs for u in us];w=len(us)
    faces=[(j*w+i,j*w+i+1,(j+1)*w+i+1,(j+1)*w+i) for j in range(len(vs)-1) for i in range(w-1)]
    o=obj(name,verts,faces,mat,0,True)
    sub=o.modifiers.new('Curved armor support topology','SUBSURF');sub.subdivision_type='CATMULL_CLARK';sub.levels=1;sub.render_levels=1
    so=o.modifiers.new('Actual armor wall thickness','SOLIDIFY');so.thickness=thickness*S;so.offset=-1;so.use_even_offset=True
    be=o.modifiers.new('Machined shell perimeter','BEVEL');be.width=.0025*S;be.segments=3
    o['construction']='Supported quad patch / curvature / real shell thickness';return o

def lerp(a,b,t):return a+(b-a)*t
def profile(z):
    stations=[(9.34,.23,.265,.06),(9.58,.355,.345,.052),(9.78,.405,.345,.063),(9.94,.34,.29,.095),(10.035,.20,.205,.12),(10.075,.055,.10,.11)]
    z=max(stations[0][0],min(z,stations[-1][0]))
    for a,b in zip(stations,stations[1:]):
        if a[0]<=z<=b[0]:
            t=(z-a[0])/(b[0]-a[0]);t=t*t*(3-2*t);return tuple(lerp(a[i],b[i],t) for i in (1,2,3))
    return stations[-1][1:]

def shell_point(theta,z,inflate=0):
    rx,ry,cy=profile(z);return ((rx+inflate)*math.sin(theta),cy-(ry+inflate)*math.cos(theta),z)

# The dark structural cranium sits inside the armor; the front optical recess stays open.
for s,side in ((-1,'R'),(1,'L')):
    patch('Cranium_Internal_Frame.'+side,lambda u,v,s=s:shell_point(s*math.radians(3+174*u),lerp(9.35,10.032,v),-.034),'frame',16,13,.037)
    # Three overlapping curved courses on each side, separated by real service seams.
    patch('Crown_Front_Armored_Course.'+side,lambda u,v,s=s:shell_point(s*math.radians(17+49*u),lerp(9.645+.025*u,10.045-.015*u,v),.018),'navy',14,13,.037)
    patch('Crown_Parietal_Armored_Course.'+side,lambda u,v,s=s:shell_point(s*math.radians(69+52*u),lerp(9.625-.035*u,10.041-.036*u,v),.027),'navy',14,13,.034)
    patch('Crown_Rear_Overlapping_Course.'+side,lambda u,v,s=s:shell_point(s*math.radians(124+54*u),lerp(9.47+.06*u,10.006-.036*u,v),.025),'panel',14,12,.033)
    patch('Temporal_Lower_Curved_Shield.'+side,lambda u,v,s=s:shell_point(s*math.radians(63+72*u),lerp(9.395+.07*u,9.635+.015*u,v),.043),'navy',16,9,.031)
    patch('Occipital_Lower_Armored_Course.'+side,lambda u,v,s=s:shell_point(s*math.radians(138+40*u),lerp(9.37,9.502+.012*u,v),.025),'gun',10,8,.03)

# Raised narrow forehead crest with a proper recessed sensor channel down its center.
for s,side in ((-1,'R'),(1,'L')):
    patch('Forehead_Longitudinal_Armored_Ridge.'+side,
        lambda u,v,s=s:(s*lerp(.041+.011*v,.134-.078*v,u),lerp(-.419,-.092,v)-.036*math.sin(math.pi*v)-.034*(1-u),lerp(9.61,10.065,v)),
        'navy',10,18,.031)
    patch('Forehead_Transition_Gusset.'+side,
        lambda u,v,s=s:(s*lerp(.135,.292-.05*v,u),lerp(-.369,-.233,v)+.045*u,lerp(9.624,9.954-.046*u,v)),
        'panel',10,12,.026)
patch('Forehead_Recessed_Central_Channel',lambda u,v:(lerp(-.042,.042,u),lerp(-.424,-.072,v),lerp(9.66,10.041,v)),'black',8,16,.018)
panel('Forehead_Optical_Cartridge',[(-.041,-.451,9.80),(.041,-.451,9.80),(.047,-.472,9.693),(.025,-.487,9.655),(-.025,-.487,9.655),(-.047,-.472,9.693)],(0,.032,0),'gun',.005)
panel('Forehead_Inset_Blue_Aperture',[(-.018,-.460,9.773),(.018,-.460,9.773),(.021,-.479,9.695),(-.021,-.479,9.695)],(0,.012,0),'glass',.003)
panel('Forehead_Cartridge_Upper_Retainer',[(-.028,-.446,9.835),(.028,-.446,9.835),(.033,-.455,9.804),(-.033,-.455,9.804)],(0,.025,0),'steel',.002)

# One contoured V visor bridge; the band wraps back around each temple in 3D.
path=[(.013,-.516,9.584),(.11,-.511,9.624),(.23,-.463,9.681),(.34,-.361,9.744),(.435,-.178,9.758),(.438,.065,9.736)]
def visor(s,u,v):
    q=u*(len(path)-1);i=min(int(q),len(path)-2);t=q-i;a,b=path[i],path[i+1]
    x,y,z=[lerp(a[k],b[k],t) for k in range(3)]
    return (s*(x+.004*math.sin(v*math.pi)),y-.017*math.sin(v*math.pi),z+(v-.5)*lerp(.061,.039,u))
for s,side in ((-1,'R'),(1,'L')):
    patch('Visor_Continuous_Armored_Bridge.'+side,lambda u,v,s=s:visor(s,u,v),'white',24,6,.026)
    # The eye is 4 distinct layers: socket, seal, lens, and fine optical highlight rail.
    def mir(ps):return [(s*x,y,z) for x,y,z in ps]
    orbital=mir([(.038,-.497,9.563),(.095,-.501,9.594),(.284,-.413,9.657),(.328,-.355,9.646),(.291,-.422,9.570),(.107,-.518,9.502),(.044,-.532,9.52)])
    panel('Deep_Optical_Eye_Socket.'+side,orbital,(0,.10,0),'black',.007)
    panel('Optical_Seal_Frame.'+side,mir([(.059,-.525,9.557),(.102,-.517,9.578),(.284,-.430,9.637),(.305,-.393,9.621),(.275,-.441,9.577),(.115,-.527,9.524)]),(0,.029,0),'gun',.003)
    panel('Twin_Eye_Recessed_Lens.'+side,mir([(.073,-.532,9.554),(.115,-.523,9.568),(.282,-.439,9.623),(.280,-.443,9.600),(.115,-.530,9.539)]),(0,.010,0),'glass',.002)
    panel('Eye_Lower_Machined_Edge.'+side,mir([(.11,-.531,9.522),(.281,-.442,9.572),(.292,-.420,9.581),(.275,-.449,9.554),(.132,-.526,9.501)]),(0,.021,0),'steel',.002)
    # Nested zygomatic plates curve toward the ears instead of facing forward as rectangles.
    panel('Zygomatic_Inner_Machined_Armor.'+side,mir([(.282,-.434,9.585),(.415,-.243,9.665),(.461,-.073,9.614),(.442,-.185,9.43),(.316,-.399,9.315),(.254,-.476,9.407)]),(0,.050,0),'gun',.007)
    panel('Cheek_Upper_Swept_Armor.'+side,mir([(.312,-.389,9.565),(.458,-.12,9.618),(.463,-.059,9.542),(.41,-.247,9.429),(.32,-.418,9.363),(.282,-.468,9.429)]),(0,.033,0),'navy',.006)
    panel('Cheek_Lower_Overlapping_Armor.'+side,mir([(.30,-.424,9.391),(.405,-.225,9.446),(.447,-.118,9.394),(.326,-.356,9.214),(.219,-.459,9.204),(.253,-.472,9.299)]),(0,.047,0),'panel',.006)
    panel('Mandible_Narrow_Ceramic_Rail.'+side,mir([(.424,-.201,9.419),(.44,-.14,9.371),(.317,-.389,9.187),(.213,-.470,9.180),(.241,-.488,9.241),(.316,-.399,9.258)]),(0,.030,0),'white',.004)
    panel('Jaw_Inner_Overlapping_Return.'+side,mir([(.23,-.475,9.281),(.217,-.503,9.211),(.104,-.560,9.161),(.082,-.566,9.214),(.171,-.52,9.31)]),(0,.04,0),'gun',.004)
    # Small floating cheek plates have relief slots, backing and captive fasteners.
    for j in range(3):
        z=9.532-j*.048
        panel('Cheek_Recessed_Cooling_Channel_%d.%s'%(j,side),mir([(.333,-.372,z),(.414,-.217,z+.019),(.407,-.234,z-.001),(.33,-.38,z-.021)]),(0,.009,0),'black',.001)
        panel('Cheek_Channel_Lower_Lip_%d.%s'%(j,side),mir([(.331,-.381,z-.024),(.407,-.235,z-.003),(.407,-.235,z-.008),(.33,-.383,z-.029)]),(0,.01,0),'steel',.001)
    for x,y,z in ((.34,-.351,9.56),(.346,-.338,9.313),(.406,-.181,9.402)):
        bolt('Cheek_Captive_Screw_'+str(z)+'.'+side,(s*x,y,z),(s*.64,-.77,0),.012)

# Central hard face is a multi-plane machined respirator, with real recessed slits.
face=obj('Central_Hard_Face_Keel',[(-.17,-.453,9.545),(.17,-.453,9.545),(.221,-.458,9.377),(.082,-.549,9.182),
    (0,-.592,9.164),(-.082,-.549,9.182),(-.221,-.458,9.377),(0,-.602,9.515),(0,-.629,9.36),
    (-.14,-.354,9.519),(.14,-.354,9.519),(.173,-.366,9.361),(0,-.50,9.18),(-.173,-.366,9.361)],
    [(0,1,7),(1,2,8,7),(2,3,4,8),(4,5,6,8),(6,0,7,8),(9,13,12,11,10),
     (0,9,10,1),(1,10,11,2),(2,11,12,3),(3,12,4),(4,12,5),(5,12,13,6),(6,13,9,0)],'gun',.007,False)
panel('Nasal_Upper_Locking_Plate',[(-.037,-.608,9.535),(.037,-.608,9.535),(.060,-.631,9.438),(0,-.65,9.386),(-.060,-.631,9.438)],(0,.020,0),'navy',.003)
panel('Chin_Separate_Impact_Guard',[(-.063,-.571,9.242),(0,-.635,9.304),(.063,-.571,9.242),(.047,-.566,9.180),(0,-.607,9.132),(-.047,-.566,9.180)],(0,.061,0),'frame',.004)
for s,side in ((-1,'R'),(1,'L')):
    def mir(ps):return [(s*x,y,z) for x,y,z in ps]
    panel('Respirator_Lateral_Overlapping_Shell.'+side,mir([(.17,-.465,9.52),(.236,-.431,9.427),(.203,-.476,9.314),(.087,-.57,9.214),(.129,-.568,9.38)]),(0,.038,0),'gun',.004)
    for j in range(2):
        x=.132+j*.042;z=9.433-j*.017;y=-.575+j*.034
        # A through recess with retained Boolean cutter and an internal black chamber.
        cut=panel('Respirator_Slot_Cutter_%d.%s'%(j,side),mir([(x-.010,y-.08,z+.032),(x+.010,y-.08,z+.013),(x+.015,y-.08,z-.07),(x-.006,y-.08,z-.079)]),(0,.19,0),'black',0)
        for c in list(cut.users_collection):c.objects.unlink(cut)
        CUT.objects.link(cut);cut.hide_render=True;cut.hide_set(True);cut.display_type='WIRE'
        for target in (face,bpy.data.objects['HEAD04_Respirator_Lateral_Overlapping_Shell.'+side]):
            mod=target.modifiers.new('Machined respirator recess '+str(j),'BOOLEAN');mod.operation='DIFFERENCE';mod.solver='EXACT';mod.object=cut
            bpy.context.view_layer.objects.active=target
            while target.modifiers.find(mod.name)>0:bpy.ops.object.modifier_move_up(modifier=mod.name)
        panel('Respirator_Slot_Inner_Chamber_%d.%s'%(j,side),mir([(x-.008,y+.049,z+.025),(x+.014,y+.049,z+.009),(x+.015,y+.049,z-.073),(x-.008,y+.049,z-.082)]),(0,.010,0),'black',0)

# Ear drive modules are recessed under the helmet, with a protected outer shell.
for s,side in ((-1,'R'),(1,'L')):
    cylinder('Temporal_Servo_Outer_Race.'+side,(s*.348,.115,9.514),(s*.430,.115,9.514),.128,'gun',48,inner=.089,bevel=.002)
    cylinder('Temporal_Sealed_Bearing.'+side,(s*.431,.115,9.514),(s*.444,.115,9.514),.092,'steel',48,inner=.066,bevel=.001)
    cylinder('Temporal_Servo_Recess.'+side,(s*.441,.115,9.514),(s*.446,.115,9.514),.064,'frame',32,bevel=.001)
    def mir(ps):return [(s*x,y,z) for x,y,z in ps]
    panel('Temporal_Outer_Protective_Cap.'+side,mir([(.446,-.058,9.628),(.48,.079,9.623),(.458,.253,9.543),(.441,.248,9.425),(.454,.112,9.442),(.476,.027,9.492)]),(-s*.037,0,0),'navy',.005)
    panel('Temporal_Rear_Overlap_Plate.'+side,mir([(.37,.265,9.677),(.407,.301,9.587),(.36,.385,9.518),(.325,.397,9.621)]),(-s*.025,-.025,0),'panel',.004)
    for j in range(4):
        z=9.591-j*.051
        panel('Temporal_Rear_Cooling_Fin_%d.%s'%(j,side),mir([(.383,.276,z),(.413,.245,z+.012),(.397,.325,z-.008),(.357,.371,z-.024)]),(-s*.015,-.012,-.007),'gun',.002)
    bolt('Temporal_Service_Lock.'+side,(s*.48,.066,9.567),(s,0,0),.016)
    # Slender antenna blades mount in mechanical sockets and do not become big sails.
    cylinder('Antenna_Socket.'+side,(s*.284,.08,9.902),(s*.323,.12,9.959),.035,'gun',24,bevel=.002)
    panel('Primary_Sensor_Antenna.'+side,mir([(.298,.091,9.915),(.342,.126,9.915),(.438,.179,10.285),(.409,.147,10.286)]),(0,.025,0),'navy',.002)
    panel('Antenna_Machined_Edge.'+side,mir([(.411,.146,10.280),(.44,.178,10.278),(.365,.143,10.001),(.346,.123,10.001)]),(0,.006,0),'steel',.001)
    panel('Temple_Short_Swept_Sensor_Fin.'+side,mir([(.40,-.069,9.771),(.464,-.004,9.74),(.618,.069,9.93),(.468,-.103,9.851)]),(0,.023,0),'navy',.003)
    # Tiny rear sensor slot avoids the old exposed glowing disk on each ear.
    panel('Temporal_Rear_Optical_Aperture.'+side,mir([(.391,.304,9.683),(.417,.272,9.674),(.399,.315,9.632),(.38,.341,9.646)]),(-s*.007,-.008,0),'black',.002)

# The nape is a real service panel with inset indicator, articulated cervical structure.
panel('Nape_Service_Cover',[(-.148,.387,9.751),(.148,.387,9.751),(.168,.398,9.605),(.09,.419,9.526),(-.09,.419,9.526),(-.168,.398,9.605)],(0,-.041,0),'navy',.005)
panel('Nape_Optical_Recess',[(-.104,.410,9.697),(.104,.410,9.697),(.107,.418,9.661),(-.107,.418,9.661)],(0,-.018,0),'black',.003)
panel('Nape_Inset_Blue_Status_Slit',[(-.065,.419,9.686),(.065,.419,9.686),(.065,.423,9.677),(-.065,.423,9.677)],(0,-.009,0),'glass',.001)
for z in (9.59,9.53,9.47):
    panel('Nape_Lamellar_Heat_Shield_'+str(z),[(-.15,.382,z+.034),(.15,.382,z+.034),(.14,.414,z-.008),(-.14,.414,z-.008)],(0,-.038,-.006),'gun',.004)
cylinder('Cervical_Base_Gimbal',(0,.035,9.076),(0,.035,9.177),.17,'frame',48,bevel=.004)
cylinder('Cervical_Rotation_Race',(0,.035,9.133),(0,.035,9.188),.183,'steel',48,inner=.129,bevel=.002)
cylinder('Cervical_Central_Column',(0,.035,9.16),(0,.035,9.405),.115,'gun',32,bevel=.003)
for i,z in enumerate((9.206,9.262,9.318)):
    cylinder('Cervical_Articulated_Collar_'+str(i),(0,.035,z),(0,.035,z+.021),.144,'frame',32,inner=.11,bevel=.002)
for s,side in ((-1,'R'),(1,'L')):
    cylinder('Cervical_Tilt_Piston.'+side,(s*.113,-.093,9.102),(s*.201,-.027,9.348),.025,'steel',24,bevel=.002)
    cylinder('Cervical_Tilt_Sleeve.'+side,(s*.113,-.093,9.102),(s*.16,-.058,9.224),.038,'gun',24,bevel=.002)
    cylinder('Cervical_Clevis_Pin.'+side,(s*.176,-.027,9.345),(s*.229,-.027,9.345),.037,'gun',24,bevel=.002)
    cable('Cervical_Protected_Data_Loom.'+side,[(s*.087,.17,9.11),(s*.166,.256,9.20),(s*.235,.271,9.32),(s*.243,.231,9.47)],.018)
    cable('Cervical_Optics_Coolant.'+side,[(s*.13,.112,9.16),(s*.206,.183,9.26),(s*.287,.153,9.39)],.011,'gun')
    for x,y,z in ((.204,-.278,9.966),(.392,.153,9.83),(.12,.395,9.732)):
        normal=Vector((s*x,y-.05,.075)).normalized();bolt('Crown_Service_Lock_'+str(z)+'.'+side,(s*x,y,z),normal,.012)

sc['master_revision']='04: reference-specific curved layered head rebuilt from scratch'
sc['head_review']='Replaced large flat helmet, eyebrow and face blocks with curved armor courses, deep eye housings, contoured bridge, machined respirator and cervical mechanism'
for screen in bpy.data.screens:
    for a in screen.areas:
        if a.type=='VIEW_3D':
            a.spaces.active.region_3d.view_location=Vector((0,-.025,3.12));a.spaces.active.region_3d.view_distance=.9
            a.spaces.active.overlay.show_relationship_lines=False
for l in sc.view_layers:l.material_override=None
bpy.context.preferences.filepaths.save_version=0
text=bpy.data.texts.load(str(OUT/'rebuild_head.py'));text.name='rebuild_head.py | actual execution'
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'V3_HEAD_REFINED_MASTER.blend'))
report={'removed_old_head_objects':len(old),'new_head_objects':len(new),'new_meshes':sum(o.type=='MESH' and not o.hide_render for o in new),
        'techniques':['Curved quad armor patches','Subdivision support rings','Real shell wall thickness','Concave plate outlines','Machined Boolean face apertures','Nested recessed optics','Contoured visor bridge','Cervical drive with capped cable routes'],'seconds':round(time.time()-T,2)}
(OUT/'logs'/'head_rebuild_04.json').write_text(json.dumps(report,indent=2),encoding='utf8')
print('HEAD_REBUILD_COMPLETE',json.dumps(report),flush=True)
