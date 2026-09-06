"""Revision 05: corrective head rebuild after front/side/clay review.
All visible front parts share a single anatomical surface. No free-floating optics
or hardware. Eyes are recessed slits, chin narrows, forehead follows helmet curvature.
"""
import bpy,bmesh,math,pathlib,json,ast,time
from mathutils import Vector,Matrix
from mathutils.geometry import tessellate_polygon
OUT=pathlib.Path(__file__).resolve().parent;S=.325;T=time.time();sc=bpy.context.scene
HEAD=bpy.data.objects['Head'];HC=bpy.data.collections['HEAD'];CUT=bpy.data.collections['Construction_Cutters | hidden']
M={k:bpy.data.materials[n] for k,n in {'navy':'01_Painted_Military_Navy','panel':'02_Navy_Secondary_Panels','white':'04_Offwhite_Ceramic_Armor','frame':'05_Black_Mechanical_Frame','gun':'06_Dark_Gunmetal','steel':'07_Bare_Machined_Steel','black':'12_Optical_Recess','glass':'10_Blue_Sensor_Glass','rubber':'09_Cable_Protection'}.items()}
import shutil
previous=OUT/'V3_HEAD_CORRECTED_MASTER.blend'
if previous.exists() and not (OUT/'iterations'/'head_05'/'HEAD05_REVIEWED.blend').exists():
    shutil.copy2(previous,OUT/'iterations'/'head_05'/'HEAD05_REVIEWED.blend')
M['gun']=M['gun'].copy();M['gun'].name='06_Head_Dark_Gunmetal'
pp=next(n for n in M['gun'].node_tree.nodes if n.type=='BSDF_PRINCIPLED');pp.inputs['Base Color'].default_value=(.025,.034,.046,1);pp.inputs['Metallic'].default_value=.62
M['glass']=M['glass'].copy();M['glass'].name='10_Head_Optical_Blue'
pp=next(n for n in M['glass'].node_tree.nodes if n.type=='BSDF_PRINCIPLED');pp.inputs['Base Color'].default_value=(.003,.055,.5,1);pp.inputs['Emission Color'].default_value=(.003,.055,.5,1);pp.inputs['Emission Strength'].default_value=3
for o in list(HEAD.children_recursive):
    if o.type in ('MESH','CURVE'):bpy.data.objects.remove(o,do_unlink=True)
new=[]
tree=ast.parse((OUT/'rebuild_head.py').read_text(encoding='utf8'))
exec(compile(ast.Module(body=[n for n in tree.body if isinstance(n,ast.FunctionDef)],type_ignores=[]),'head_helpers','exec'),globals())

# Smooth continuous skull envelope; no station-wise easing that produced waves.
def profile(z):
    t=max(-.12,min((z-9.55)/.53,.999))
    return (.405*math.sqrt(max(.015,1-.97*t*t)),.342*math.sqrt(max(.020,1-.94*t*t)),.055+.075*t)
def shell_point(theta,z,inflate=0):
    rx,ry,cy=profile(z);c=math.cos(theta)
    return ((rx+inflate)*math.sin(theta),cy-(ry+inflate)*c-.124*max(c,0)**4*(1-.45*max(0,(z-9.55)/.53)),z)
def front(x,z,offset=0):
    # offset > 0 is physically inward; the face mask and optics cannot drift forward.
    if z>=9.55:
        rx,ry,cy=profile(z);a=math.asin(max(-.997,min(x/rx,.997)));return shell_point(a,z)[1]+offset
    return -.414+.205*(abs(x)/.405)**1.8+offset
def front_panel(name,xz,offset=0,depth=.028,mat='navy',bevel=.003):
    return panel(name,[(x,front(x,z,offset),z) for x,z in xz],(0,depth,0),mat,bevel)
mounts=[]

# Closed dark cranium, with overlapping continuous shell courses surrounding it.
for s,side in ((-1,'R'),(1,'L')):
    patch('Cranium_Internal_Frame.'+side,lambda u,v,s=s:shell_point(s*math.radians(1+178*u),lerp(9.32,10.067,v),-.030),'frame',16,14,.033)
    for label,a,b,low,top,infl,mat in [('Frontal_Course',16,60,9.633,10.057,.015,'navy'),
      ('Parietal_Course',62,116,9.581,10.061,.022,'navy'),('Occipital_Course',118,178,9.475,10.044,.018,'navy')]:
        def boundary(u,label=label,a=a,b=b,lo=low):
            if label=='Frontal_Course':return 9.620+.149*math.sin(math.radians(lerp(a,b,u)))
            if label=='Parietal_Course':return lerp(9.747,9.600,u)
            return lo
        o=patch(label+'.'+side,lambda u,v,s=s,a=a,b=b,hi=top,inf=infl,boundary=boundary:shell_point(s*math.radians(lerp(a,b,u)),lerp(boundary(u),hi-.012*u,v),inf),mat,14,14,.033)
        mounts.append((label+'_Fastener.'+side,o.name,Vector(shell_point(s*math.radians((a+b)/2),lerp(low,top,.56),infl))))
    patch('Temporal_Wrapped_Lower_Course.'+side,lambda u,v,s=s:shell_point(s*math.radians(70+62*u),lerp(9.373+.024*u,9.598,v),.024),'panel',14,9,.028)
    patch('Nape_Overlapping_Lower_Course.'+side,lambda u,v,s=s:shell_point(s*math.radians(135+43*u),lerp(9.347,9.504,v),.015),'gun',12,8,.026)
    # Curved shoulder of the forehead wraps into the central channel, no rectangular patch.
    o=patch('Forehead_Flanking_Armor.'+side,lambda u,v,s=s:shell_point(s*math.radians(lerp(7+4*v,30-8*v,u)),lerp(9.620+.149*math.sin(math.radians(lerp(7,30,u))),9.963,v),.024),'navy',10,16,.030)
    mounts.append(('Forehead_Flush_Lock.'+side,o.name,Vector(shell_point(s*math.radians(18),9.852,.024))))

# One solid arched upper crest, with a protected sensor channel recessed beneath it.
patch('Crown_Central_Arched_Keel',lambda u,v:shell_point(math.radians(lerp(-14,14,u)),lerp(9.859,10.072,v),.026),'navy',12,13,.040)
# A cap closes the crown rather than leaving an open helmet bowl.
patch('Crown_Apex_Closure',lambda u,v:(lerp(-.082,.082,u),lerp(-.016,.216,v),10.054+.017*math.sin(math.pi*u)*math.sin(math.pi*v)),'navy',8,10,.027)
vv=[(0,.11,10.075)];nn=32;ff=[]
for j in range(1,8):
    a=j*math.pi/14
    vv += [(.18*math.sin(a)*math.cos(k*math.tau/nn),.11+.225*math.sin(a)*math.sin(k*math.tau/nn),10.005+.07*math.cos(a)) for k in range(nn)]
for k in range(nn):ff.append((0,1+k,1+(k+1)%nn))
for j in range(6):
    for k in range(nn):ff.append((1+j*nn+k,1+(j+1)*nn+k,1+(j+1)*nn+(k+1)%nn,1+j*nn+(k+1)%nn))
ff.append(tuple(reversed(range(1+6*nn,1+7*nn))))
obj('Crown_Continuous_Closed_Dome',vv,ff,'navy',0,True)
front_panel('Forehead_Recessed_Optical_Bay',[(-.047,9.876),(.047,9.876),(.046,9.72),(.025,9.64),(-.025,9.64),(-.046,9.72)],.021,.020,'black',.002)
front_panel('Forehead_Optical_Socket',[(-.031,9.793),(.031,9.793),(.034,9.714),(.020,9.69),(-.020,9.69),(-.034,9.714)],.006,.023,'gun',.003)
front_panel('Forehead_Inset_Optical_Lens',[(-.015,9.771),(.015,9.771),(.019,9.721),(-.019,9.721)],.002,.008,'glass',.001)
for z in (9.831,9.680):
    front_panel('Forehead_Bay_Retainer_'+str(z),[(-.023,z+.008),(.023,z+.008),(.023,z-.008),(-.023,z-.008)],-.001,.016,'gun',.002)

# White helmet brow is a flat armored ridge following the skull, no round spectacle frame.
def brow(s,u,v):
    x=.371*u;top=9.614+.380*x-.022*u**5;bottom=top-(.069-.015*u)
    z=lerp(bottom,top,v)
    return (s*x,front(x,z,-.043),z)
for s,side in ((-1,'R'),(1,'L')):
    b=patch('Fitted_Armored_Visor_Ridge.'+side,lambda u,v,s=s:brow(s,u,v),'white',22,6,.026)
    mounts.append(('Visor_Flush_Lock.'+side,b.name,Vector(brow(s,.70,.60))))
    # Eye geometry has a crescent outline recessed behind the visor surface.
    def eye(s,u,v,extra=0,inward=.025):
        x=.087+.194*u;ztop=9.545+.285*x+.007*math.sin(math.pi*u)
        thick=.004+.023*math.sin(math.pi*u)**.7+extra
        z=ztop-v*thick
        return (s*x,front(x,z,inward),z)
    patch('Deep_Orbital_Cavity.'+side,lambda u,v,s=s:eye(s,u,v,.035,.045),'black',22,7,.020)
    patch('Twin_Recessed_Sensor_Lens.'+side,lambda u,v,s=s:eye(s,.045+.91*u,.16+.64*v,0,.035),'glass',22,6,.009)
    # Dark lower lid occludes the lens base; its edge is mechanically thin and unlit.
    patch('Lower_Orbital_Seal.'+side,lambda u,v,s=s:eye(s,u,1.02+.24*v,.001,.013),'gun',20,5,.016)
    # Inner nose bridge joins the face; a recess is visible under the central visor.
    xz=[(s*.028,9.547),(s*.071,9.527),(s*.077,9.485),(s*.028,9.464)]
    front_panel('Inner_Orbital_Bridge.'+side,xz,.011,.030,'frame',.002)

# Tapered integrated face plate. Leading edge stays just behind the armored brow.
mask=obj('Integrated_Tapered_Hard_Mask',[
    (-.180,-.366,9.493),(0,-.429,9.535),(.180,-.366,9.493),(.163,-.366,9.362),(.054,-.411,9.223),
    (0,-.430,9.190),(-.054,-.411,9.223),(-.163,-.366,9.362),(0,-.450,9.344),
    (-.148,-.305,9.480),(.148,-.305,9.480),(.139,-.303,9.361),(0,-.360,9.195),(-.139,-.303,9.361)],
    [(0,1,8,7),(1,2,3,8),(3,4,5,8),(5,6,7,8),(9,13,12,11,10),(0,9,10,2,1),(2,10,11,3),(3,11,12,4),(4,12,5),(5,12,6),(6,12,13,7),(7,13,9,0)],'gun',.006)
mask['leading_edge_limit_m']=-.467*S
# Lower chin is a nested impact plate, not a projecting muzzle or pair of tusks.
panel('Chin_Recessed_Impact_Cap',[(-.061,-.422,9.242),(0,-.463,9.277),(.061,-.422,9.242),(.041,-.427,9.178),(0,-.447,9.135),(-.041,-.427,9.178)],(0,.048,0),'frame',.003)
for s,side in ((-1,'R'),(1,'L')):
    def mir(ps):return [(s*x,y,z) for x,y,z in ps]
    # Real narrow mask seams and overlapping side planes follow the same tapered face.
    panel('Mask_Outer_Overlapping_Plane.'+side,mir([(.170,-.374,9.482),(.201,-.316,9.422),(.168,-.346,9.323),(.068,-.418,9.221),(.124,-.407,9.347)]),(0,.027,0),'gun',.003)
    panel('Mask_Lateral_Recess.'+side,mir([(.178,-.357,9.400),(.192,-.335,9.368),(.108,-.404,9.269),(.116,-.405,9.30)]),(0,.012,0),'black',.002)
    # White cheek plates begin beside the eyes and narrow down toward the jaw.
    p=panel('Cheek_Ceramic_Contour.'+side,mir([(.278,-.274,9.551),(.356,-.179,9.529),(.339,-.224,9.429),(.371,-.17,9.345),(.221,-.348,9.181),(.216,-.363,9.256),(.292,-.282,9.361),(.251,-.322,9.429)]),(0,.040,0),'white',.004)
    mounts.append(('Cheek_Service_Lock.'+side,p.name,Vector((s*.292,-.285,9.459))))
    panel('Zygomatic_Dark_Subframe.'+side,mir([(.278,-.248,9.591),(.391,-.071,9.567),(.388,-.045,9.342),(.24,-.266,9.169),(.193,-.31,9.234),(.273,-.275,9.361)]),(0,.061,0),'frame',.005)
    p=panel('Temporal_Upper_Angular_Shroud.'+side,mir([(.365,-.175,9.602),(.44,-.01,9.611),(.457,.141,9.553),(.413,.099,9.449),(.393,-.060,9.421),(.346,-.20,9.482)]),(-s*.032,.018,0),'navy',.005)
    mounts.append(('Temporal_Shroud_Flush_Lock.'+side,p.name,Vector((s*.420,.001,9.538))))
    panel('Temporal_Lower_Overlapping_Shroud.'+side,mir([(.40,-.074,9.436),(.444,.117,9.463),(.395,.249,9.394),(.287,.066,9.204),(.242,-.194,9.188),(.311,-.235,9.310)]),(-s*.030,.024,0),'navy',.004)
    panel('Temporal_Rear_Service_Panel.'+side,mir([(.356,.226,9.572),(.401,.279,9.500),(.357,.367,9.449),(.286,.37,9.520),(.30,.315,9.596)]),(-s*.024,-.03,0),'panel',.004)
    # Cooling ports placed at the rear of the helmet, away from the visible face.
    for j in range(3):
        z=9.544-j*.046
        panel('Temporal_Rear_Recess_'+str(j)+'.'+side,mir([(.369,.273,z),(.391,.265,z-.01),(.365,.321,z-.03),(.343,.326,z-.02)]),(-s*.011,-.012,0),'black',.001)
    cylinder('Ear_Internal_Sealed_Bearing.'+side,(s*.337,.10,9.473),(s*.392,.10,9.473),.105,'gun',40,inner=.075,bevel=.002)
    cylinder('Ear_Inner_Actuator.'+side,(s*.369,.10,9.473),(s*.393,.10,9.473),.070,'frame',32,bevel=.002)
    # Pointed antennae are narrow at their tips, mounted into real sockets.
    panel('Antenna_Integrated_Load_Shoe.'+side,mir([(.243,-.023,9.856),(.308,.039,9.854),(.345,.141,9.942),(.295,.107,9.983)]),(0,.054,-.016),'navy',.003)
    panel('Antenna_Slender_Main_Fin.'+side,mir([(.257,.025,9.864),(.309,.079,9.863),(.434,.178,10.278),(.423,.153,10.298)]),(0,.019,0),'navy',.0015)
    panel('Antenna_Leading_Metal_Edge.'+side,mir([(.258,.021,9.867),(.268,.031,9.868),(.427,.157,10.282),(.423,.153,10.294)]),(0,.005,0),'gun',.001)
    panel('Temporal_Swept_Short_Fin.'+side,mir([(.305,-.045,9.640),(.359,.025,9.665),(.586,.073,9.901),(.578,.049,9.91)]),(0,.022,0),'navy',.002)

# Protected nape with compact, seated hardware and articulated cervical system.
p=panel('Nape_Service_Access',[(-.151,.389,9.747),(.151,.389,9.747),(.174,.399,9.596),(.10,.414,9.529),(-.10,.414,9.529),(-.174,.399,9.596)],(0,-.041,0),'navy',.004)
mounts += [('Nape_Lock.R',p.name,Vector((-.115,.402,9.643))),('Nape_Lock.L',p.name,Vector((.115,.402,9.643)))]
panel('Nape_Optical_Recess',[(-.085,.407,9.703),(.085,.407,9.703),(.092,.414,9.666),(-.092,.414,9.666)],(0,-.018,0),'black',.002)
panel('Nape_Recessed_Status_Slit',[(-.064,.414,9.689),(.064,.414,9.689),(.064,.417,9.680),(-.064,.417,9.680)],(0,-.009,0),'glass',.001)
for i,z in enumerate((9.54,9.481,9.422)):
    panel('Nape_Lamellar_Return_'+str(i),[(-.133,.354,z+.025),(.133,.354,z+.025),(.129,.394,z-.006),(-.129,.394,z-.006)],(0,-.035,-.006),'gun',.003)
cylinder('Neck_Base_Rotation_Gimbal',(0,.042,9.076),(0,.042,9.169),.161,'frame',48,bevel=.003)
cylinder('Neck_Sealed_Bearing_Race',(0,.042,9.132),(0,.042,9.175),.174,'gun',48,inner=.126,bevel=.002)
cylinder('Neck_Central_Column',(0,.042,9.151),(0,.042,9.382),.110,'gun',32,bevel=.002)
for i,z in enumerate((9.206,9.256,9.306)):
    cylinder('Neck_Articulated_Seal_'+str(i),(0,.042,z),(0,.042,z+.018),.135,'frame',32,inner=.109,bevel=.001)
for s,side in ((-1,'R'),(1,'L')):
    cylinder('Neck_Tilt_Rod.'+side,(s*.107,-.07,9.111),(s*.179,-.012,9.325),.022,'steel',24,bevel=.001)
    cylinder('Neck_Tilt_Sleeve.'+side,(s*.107,-.07,9.111),(s*.144,-.04,9.221),.033,'gun',24,bevel=.002)
    cylinder('Neck_Tilt_Clevis.'+side,(s*.156,-.012,9.325),(s*.201,-.012,9.325),.031,'gun',24,bevel=.001)
    cable('Neck_Protected_Data_Loom.'+side,[(s*.078,.166,9.104),(s*.141,.228,9.212),(s*.219,.239,9.382)],.015)
    cable('Neck_Optics_Cooling_Line.'+side,[(s*.109,.111,9.13),(s*.176,.165,9.244),(s*.238,.144,9.383)],.010,'gun')

# Derive every fixture position from the evaluated armor surface, never raw guessed coordinates.
bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get();attached=[];fixture_points=[]
for name,host,p in mounts:
    o=bpy.data.objects[host];ev=o.evaluated_get(dg);inv=o.matrix_world.inverted()
    ok,loc,n,idx=ev.closest_point_on_mesh(inv@(p*S))
    if not ok:raise RuntimeError('No mounting surface '+host)
    q=o.matrix_world@loc;normal=(o.matrix_world.to_3x3()@n).normalized()
    # Thin plate cap normals can point inward; choose the outside of the helmet.
    outward=Vector((q.x,q.y-.035*S,(q.z-9.60*S)*.4))
    if normal.dot(outward)<0:normal=-normal
    fixture_points.append((name,q/S+normal*.001,normal))
    attached.append({'fixture':name,'host':host,'surface_distance_m':0.000325})
for name,p,n in fixture_points:bolt(name,p,n,.0075)

sc['master_revision']='06: corrected visor occlusion, reduced inset lenses, narrower integrated face, seated antenna roots'
sc['head_rework_postmortem']='Revision 04 used unrelated feature coordinates, which floated the brow/optics/fasteners; large facemask and raised bright frames produced protruding spectacles. Revision 05 shares one front envelope, removes extra face vents and projects fixtures to their hosts.'
sc['head_review_pending']='Inspect front, side and clay before any final delivery'
for screen in bpy.data.screens:
    for a in screen.areas:
        if a.type=='VIEW_3D':
            a.spaces.active.region_3d.view_location=Vector((0,-.025,3.12));a.spaces.active.region_3d.view_distance=.9;a.spaces.active.overlay.show_relationship_lines=False
for l in sc.view_layers:l.material_override=None
bpy.context.preferences.filepaths.save_version=0
t=bpy.data.texts.load(str(OUT/'rebuild_head_v5.py'));t.name='rebuild_head_v5.py | executed'
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'V3_HEAD_CORRECTED_MASTER.blend'))
(OUT/'logs'/'head_rebuild_05.json').write_text(json.dumps({'fixtures':attached,'head_objects':len(new),'seconds':round(time.time()-T,2),'source':'Reference head detail enlarged and compared with failed front/side/clay renders'},indent=2),encoding='utf8')
print('CORRECTED_HEAD_SAVED',len(new),flush=True)
