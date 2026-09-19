"""Reference-led editable science-fiction cannon. Authored through Blender MCP."""
import bpy, bmesh, math, json
from pathlib import Path
from mathutils import Vector, Matrix

ROOT=Path(r'D:\project-mecha-design\MECH ROUGE\art_prototypes\ZZ_HyperCannon_20260908')
TARGET_LENGTH=3.5
SCALE=TARGET_LENGTH/6.919
PRESENTATION_SCALE=TARGET_LENGTH/1.8
scene=bpy.context.scene
if bpy.data.filepath and Path(bpy.data.filepath).parent != ROOT:
    raise RuntimeError('Refusing to replace a different Blender project')
for o in list(bpy.data.objects): bpy.data.objects.remove(o,do_unlink=True)
for c in list(bpy.data.collections): bpy.data.collections.remove(c)
for datablocks in (bpy.data.materials,bpy.data.meshes,bpy.data.curves,bpy.data.cameras,bpy.data.lights):
    for data in list(datablocks):
        if data.users==0:datablocks.remove(data)
scene.name='HC-09 | ZZ-inspired heavy cannon'
scene.unit_settings.system='METRIC'
scene.unit_settings.scale_length=1

def collection(name):
    c=bpy.data.collections.new(name);scene.collection.children.link(c);return c
COLS={key:collection(name) for key,name in [
    ('hull','01 | Primary silhouette and energy casing'),
    ('barrel','02 | Tapered barrel and open muzzle'),
    ('armor','03 | Ivory armor and layered cheek plates'),
    ('grip','04 | Primary grip and mechanical controls'),
    ('optic','05 | Optical module and raised sight'),
    ('detail','06 | Vents fasteners and stencils'),
    ('sockets','07 | Game attachment sockets'),
    ('studio','90 | Inspection studio')]}
root=bpy.data.objects.new('HC09_CANNON_ROOT',None);COLS['sockets'].objects.link(root)
root.scale=(SCALE,SCALE*.85,SCALE*.82)
root['asset']='TYPE-08 heavy mecha cannon; based on user-provided orthographic design sheet'
root['mount']='Right hand. X forward in native source; Grip is export origin.'

def socket_input(node,name):
    return next(s for s in node.inputs if s.identifier==name or s.name==name)
def material(name,color,metal=.0,rough=.4,emission=0):
    m=bpy.data.materials.new('HC09_'+name);m.use_nodes=True;m.diffuse_color=(*color,1)
    bs=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
    for key,value in [('Base Color',(*color,1)),('Metallic',metal),('Roughness',rough)]:socket_input(bs,key).default_value=value
    if emission:
        socket_input(bs,'Emission Color').default_value=(*color,1)
        socket_input(bs,'Emission Strength').default_value=emission
    m['metallic']=metal;m['roughness']=rough;m['emission']=emission
    return m
M={
 'shell':material('01_Graphite_shell',(.029,.039,.055),.65,.32),
 'panel':material('02_Blue_gray_panels',(.066,.084,.110),.55,.38),
 'ivory':material('03_Ivory_ceramic_armor',(.72,.755,.75),.26,.30),
 'steel':material('04_Machined_titanium',(.28,.33,.36),.86,.24),
 'dark':material('05_Recess_and_grip',(.008,.013,.020),.18,.55),
 'yellow':material('06_Amber_identification',(.95,.43,.045),.40,.32),
 'red':material('07_Service_red',(.46,.022,.018),.25,.36),
 'cyan':material('08_Optical_cyan',(.01,.52,.63),.30,.2,2.2),
 'mark':material('09_White_stencil',(.72,.77,.77),.10,.5),
}

def finish(o,name,mat,col,bevel=0):
    o.name=name
    for c in list(o.users_collection):c.objects.unlink(o)
    COLS[col].objects.link(o)
    if mat:o.data.materials.append(M[mat] if isinstance(mat,str) else mat)
    if col!='studio':o.parent=root
    if bevel:
        mod=o.modifiers.new('Manufactured edge radius','BEVEL');mod.width=bevel;mod.segments=3;mod.limit_method='ANGLE'
        mod.harden_normals=True
        normal=o.modifiers.new('Weighted face normals','WEIGHTED_NORMAL');normal.keep_sharp=True;normal.weight=50
    return o
def mesh(name,verts,faces,mat='shell',col='hull',bevel=.015):
    me=bpy.data.meshes.new(name+'_Geometry');me.from_pydata(verts,[],faces);me.update()
    bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free()
    return finish(bpy.data.objects.new(name,me),name,mat,col,bevel)
def box(name,p,s,mat='shell',col='detail',bevel=.012,rot=(0,0,0)):
    x,y,z=[v/2 for v in s]
    o=mesh(name,[(-x,-y,-z),(-x,-y,z),(-x,y,-z),(-x,y,z),(x,-y,-z),(x,-y,z),(x,y,-z),(x,y,z)],
        [(0,4,6,2),(1,3,7,5),(0,1,5,4),(2,6,7,3),(0,2,3,1),(4,5,7,6)],mat,col,bevel)
    o.location=p;o.rotation_euler=rot;return o
def prism(name,profile,center_y,thickness,mat='shell',col='armor',bevel=.012):
    n=len(profile);verts=[(x,y,z) for y in [center_y-thickness/2,center_y+thickness/2] for x,z in profile]
    faces=[tuple(range(n-1,-1,-1)),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    return mesh(name,verts,faces,mat,col,bevel)
def loft(name,sections,mat='shell',col='hull',bevel=.014):
    verts=[]
    for x,w,lo,hi in sections:
        c=min(w*.38,(hi-lo)*.19)
        verts.extend((x,y,z) for y,z in [(-w,lo+c),(-w+c,lo),(w-c,lo),(w,lo+c),(w,hi-c),(w-c,hi),(-w+c,hi),(-w,hi-c)])
    faces=[tuple(range(7,-1,-1))]
    for r in range(len(sections)-1):
        for k in range(8):faces.append((r*8+k,r*8+(k+1)%8,(r+1)*8+(k+1)%8,(r+1)*8+k))
    faces.append(tuple((len(sections)-1)*8+k for k in range(8)))
    return mesh(name,verts,faces,mat,col,bevel)
def cyl(name,p,r,d,mat='steel',col='detail',axis=(1,0,0),vertices=32,bevel=.006):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=r,depth=d,location=p)
    o=bpy.context.object;o.rotation_euler=Vector(axis).to_track_quat('Z','Y').to_euler()
    o=finish(o,name,mat,col,bevel)
    for poly in o.data.polygons:poly.use_smooth=len(poly.vertices)==4
    return o
def ring(name,x,r,inner,depth,z=.28,mat='steel',col='barrel',y=0):
    verts=[];n=48
    for px in (x-depth/2,x+depth/2):
        for radius in (r,inner):
            verts.extend((px,y+math.cos(i*math.tau/n)*radius,z+math.sin(i*math.tau/n)*radius) for i in range(n))
    faces=[]
    for i in range(n):
        j=(i+1)%n
        faces.extend([(i,j,2*n+j,2*n+i),(n+i,3*n+i,3*n+j,n+j),(i,n+i,n+j,j),(2*n+i,2*n+j,3*n+j,3*n+i)])
    return mesh(name,verts,faces,mat,col,.003)
def pipe(name,points,r,mat='dark',col='detail'):
    cu=bpy.data.curves.new(name,'CURVE');cu.dimensions='3D';cu.bevel_depth=r;cu.bevel_resolution=2;cu.resolution_u=2
    sp=cu.splines.new('POLY');sp.points.add(len(points)-1)
    for p,v in zip(sp.points,points):p.co=(*v,1)
    return finish(bpy.data.objects.new(name,cu),name,mat,col)
def label(name,text,p,size,side,mat='mark'):
    cu=bpy.data.curves.new(name,'FONT');cu.body=text;cu.size=size;cu.extrude=.0008;cu.resolution_u=3
    cu.align_x='CENTER';cu.align_y='CENTER'
    o=finish(bpy.data.objects.new(name,cu),name,mat,'detail');o.location=p
    o.rotation_euler=(math.pi/2,0,math.pi if side==1 else 0)
    return o
def screw(name,p,side,r=.025):
    cyl(name,p,r,.012,'steel',axis=(0,side,0),vertices=6,bevel=.002)
    box(name+'_slot',(p[0],p[1]+side*.008,p[2]),(r*.88,.005,.006),'dark',bevel=.001)

# Large rounded-chamfered rear energy casing, with a full longitudinal seam.
loft('Rear power casing - broad chamfered silhouette',[
 (-2.68,.24,.34,.99),(-2.53,.37,.16,1.21),(-2.27,.44,.04,1.31),
 (-1.08,.44,.02,1.30),(-.68,.38,.09,1.18),(-.43,.27,.20,.86)],bevel=.035)
loft('Rear cap seam', [(-2.56,.381,.16,1.205),(-2.52,.385,.15,1.214)],'dark',bevel=.008)
loft('Rear armored end cap',[(-2.75,.22,.36,.98),(-2.66,.258,.30,1.05),(-2.56,.369,.17,1.204)],'panel',bevel=.025)
loft('Receiver steel chassis',[(-1.0,.26,.02,.63),(-.48,.35,-.15,.69),(.45,.33,-.16,.65),(1.00,.25,.02,.61)],'panel',bevel=.025)
loft('Receiver lower structural keel',[(-1.64,.22,-.15,.08),(-.5,.24,-.21,.18),(.6,.22,-.25,.15),(1.27,.14,-.10,.20)],'dark',bevel=.012)

# Long narrowing forward lance, with a separate cylindrical bore.
loft('Long forward tapered armored barrel',[(.48,.29,-.01,.67),(.96,.29,-.06,.68),(1.63,.25,-.08,.63),(2.34,.215,-.07,.57),(2.96,.18,-.02,.52),(3.39,.16,.11,.47)],'shell','barrel',.02)
cyl('Breech upper cylindrical sleeve',(.38,0,.63),.252,.95,'shell','barrel',vertices=48,bevel=.015)
cyl('Forward recoil tube',(1.36,0,.54),.176,1.32,'panel','barrel',vertices=40)
for i,x in enumerate((.03,.67,.83,1.94)):
    cyl('Recoil locking collar %02d'%i,(x,0,.60 if x<.9 else .54),.265 if x<.9 else .185,.055,'dark','barrel',vertices=40)
cyl('Forward barrel carbon sleeve',(2.82,0,.13),.191,.43,'dark','barrel',vertices=40)
cyl('Long exposed lower bore',(3.13,0,.13),.174,.88,'steel','barrel',vertices=48)
ring('Muzzle outer metal collar',3.57,.209,.143,.16,z=.13)
ring('Muzzle interior dark rifling sleeve',3.545,.142,.108,.20,z=.13,mat='dark')
cyl('Deep black muzzle cavity',(3.442,0,.13),.109,.006,'dark','barrel',vertices=40,bevel=0)
cyl('Recessed emitter lens',(3.448,0,.13),.074,.004,'cyan','barrel',vertices=32,bevel=0)
ring('Barrel breech collar',2.81,.19,.169,.07,z=.13,mat='panel')
loft('Dorsal stepped forebarrel rail',[(1.01,.055,.679,.72),(1.46,.055,.623,.69),(2.16,.043,.557,.602),(2.62,.035,.509,.549)],'panel','barrel',.007)

# Distinctive off-white receiver armor, asymmetrical layered panel lines.
for side in (-1,1):
    y=side*.323
    prism('Ivory receiver cheek '+str(side),[(-.64,.29),(-.33,.56),(.22,.55),(.76,.30),(1.06,.25),(1.08,.02),(.56,-.27),(-.33,-.30),(-.65,-.12)],y,.064,'ivory',bevel=.019)
    prism('Receiver recessed black panel '+str(side),[(-.35,.18),(-.14,.37),(.35,.37),(.73,.16),(.73,.045),(.37,-.13),(-.30,-.13)],y+side*.036,.012,'dark',bevel=.006)
    prism('Inner ivory angular panel '+str(side),[(-.24,.14),(-.05,.29),(.33,.29),(.60,.13),(.56,.018),(.30,-.07),(-.24,-.07)],y+side*.045,.014,'ivory',bevel=.006)
    prism('Dark lower trigger-side panel '+str(side),[(-.64,-.12),(-.24,-.31),(.30,-.26),(.32,-.16),(-.21,-.16),(-.49,.03)],y+side*.036,.016,'panel',bevel=.006)
    prism('Forward narrow side armor '+str(side),[(1.18,.04),(1.43,.09),(1.43,.29),(2.31,.30),(2.51,.22),(2.50,.11),(1.54,-.035),(1.18,-.08)],side*.218,.038,'panel','barrel',.009)
    prism('Amber forward warning inlay '+str(side),[(2.63,.27),(2.68,.27),(3.18,.35),(3.14,.40),(2.72,.34),(2.63,.33)],side*.211,.014,'yellow','detail',.003)
    box('Forebody square service cover '+str(side),(1.91,side*.252,.09),(.52,.055,.29),'panel',bevel=.024)
    box('Forebody service cover recessed center '+str(side),(1.91,side*.283,.09),(.409,.012,.20),'shell',bevel=.007)
    for j,(dx,dz) in enumerate([(-.20,-.10),(.20,-.10),(-.20,.10),(.20,.10)]):
        screw('Forebody cover fastener %s %s'%(side,j),(1.91+dx,side*.296,.09+dz),side,.021)
    box('Barrel base service block '+str(side),(1.07,side*.29,.20),(.33,.19,.27),'dark',bevel=.026)
    box('Service block small red latch '+str(side),(1.04,side*.391,.20),(.08,.016,.05),'red',bevel=.003)
    pipe('Prow return cable '+str(side),[(2.13,side*.19,.21),(2.18,side*.23,.09),(2.55,side*.18,.065),(2.77,side*.145,.17)],.019,'dark')
    for i,(x,z) in enumerate([(-.42,.21),(-.25,-.20),(.39,-.18),(.86,.12),(1.50,.19),(2.25,.19)]):
        screw('Cheek captive fastener %s %s'%(side,i),(x,side*(.372 if x<1 else .246),z),side,.024 if x<1 else .019)
    label('Receiver micro stencil '+str(side),'LOCK / 09',(.18,side*.382,.12),.054,side,'shell')
    box('Receiver red safety line '+str(side),(.21,side*.383,-.025),(.14,.006,.018),'red',bevel=0)

# White dorsal recognition band and side service panels on the energy pod.
loft('White diagonal dorsal saddle',[(-1.83,.309,1.229,1.319),(-1.42,.369,1.204,1.34),(-1.31,.384,1.10,1.312)],'ivory','armor',.012)
for side in (-1,1):
    prism('Dorsal white swept flank '+str(side),[(-1.94,1.04),(-1.73,1.28),(-1.38,1.28),(-1.23,1.06),(-1.28,.84),(-1.43,.86),(-1.48,1.02)],side*.384,.048,'ivory',bevel=.014)
    prism('Energy-pod lateral plate '+str(side),[(-2.30,.36),(-2.32,.79),(-2.12,1.05),(-.96,1.00),(-.75,.70),(-.83,.30),(-1.20,.19),(-2.17,.20)],side*.449,.028,'shell',bevel=.014)
    prism('Energy-pod lower skirt '+str(side),[(-2.08,.10),(-.83,.14),(-.64,.39),(-.54,.28),(-.69,-.10),(-1.87,-.14),(-2.08,-.045)],side*.338,.095,'shell',bevel=.014)
    box('Pod recessed heat sink '+str(side),(-1.40,side*.391,-.075),(.94,.040,.11),'dark',bevel=.008)
    for i in range(12):
        box('Vent fin %s %02d'%(side,i),(-1.85+i*.079,side*.418,-.07),(.023,.032,.087),'steel',bevel=.002)
    pipe('Rear thermal return pipe '+str(side),[(-1.80,side*.32,-.19),(-1.78,side*.32,-.26),(-1.10,side*.33,-.26),(-1.02,side*.34,-.14)],.024,'steel')
    label('Pod major unit mark '+str(side),'TYPE-08',(-1.46,side*.469,.52),.105,side)
    label('Pod division stencil '+str(side),'HEAVY CANNON  /////',(-1.53,side*.470,.385),.065,side)
    label('Pod caution stencil '+str(side),'VALKYR  /  HEAVY ARMS',(-1.43,side*.470,.25),.032,side)
    for i in range(3):box('Pod amber tally %s %s'%(side,i),(-.97+i*.061,side*.474,.49),(.018,.008,.092),'yellow',bevel=.002)
    label('Rear technical stencil '+str(side),'08 / SERVICE',(-2.12,side*.438,.93),.043,side)
    box('Rear pod red release '+str(side),(-2.38,side*.395,.94),(.11,.04,.055),'red',bevel=.005)
    for i,(x,z) in enumerate([(-2.13,.32),(-.98,.34),(-2.13,.96),(-.95,.94)]):screw('Pod fastener %s %s'%(side,i),(x,side*.471,z),side,.021)
    box('Side power indicator '+str(side),(-.71,side*.394,.37),(.08,.019,.024),'cyan',bevel=.003)

# Pistol grip with visible finger clearance, trigger guard, selector and heel.
prism('Primary ivory grip chassis',[(.18,-.16),(.51,-.21),(.44,-.93),(.13,-.84),(.03,-.73),(.04,-.35)],0,.31,'ivory','grip',.018)
prism('Textured black inner pistol grip',[(.15,-.31),(.37,-.31),(.33,-.77),(.16,-.77),(.10,-.69)],0,.338,'dark','grip',.014)
for side in (-1,1):
    for i in range(6):box('Grip rib %s %s'%(side,i),(.225,side*.177,-.40-i*.054),(.17,.021,.015),'panel','grip',.003)
    box('Grip heel red mark '+str(side),(.26,side*.163,-.85),(.14,.008,.021),'red','grip',.002)
    cyl('Selector outer ring '+str(side),(.64,side*.402,-.12),.112,.057,'panel','grip',(0,side,0),32,.006)
    cyl('Selector inset '+str(side),(.64,side*.434,-.12),.065,.017,'dark','grip',(0,side,0),24,.003)
    cyl('Selector steel pin '+str(side),(.64,side*.447,-.12),.025,.008,'steel','grip',(0,side,0),6,.001)
    prism('Selector lever '+str(side),[(.68,-.14),(.72,-.15),(.74,-.34),(.69,-.40),(.65,-.37)],side*.45,.029,'panel','grip',.008)
    box('Selector amber index '+str(side),(.64,side*.438,.010),(.056,.010,.016),'yellow','grip',.003)
pipe('Open trigger guard',[(.50,0,-.30),(.80,0,-.35),(.86,0,-.62),(.74,0,-.74),(.42,0,-.68)],.032,'shell','grip')
prism('Recessed curved trigger',[(.56,-.29),(.62,-.30),(.66,-.46),(.61,-.53),(.58,-.47)],0,.065,'steel','grip',.008)
box('Support-grip rear mount',(.97,0,-.16),(.26,.34,.19),'shell','grip',.024)
prism('Folding forward auxiliary handle',[(1.03,-.15),(1.17,-.17),(1.28,-.51),(1.22,-.55),(1.09,-.53)],0,.21,'panel','grip',.016)
box('Support handle ivory lower cap',(1.20,0,-.50),(.17,.24,.072),'ivory','grip',.01)

# Raised sight assembly: small squared-off rear housing and pale cylindrical optic.
loft('Raised sight mechanical pedestal',[(-.26,.18,.64,1.20),(.12,.18,.71,1.18),(.25,.14,.76,1.00)],'shell','optic',.018)
prism('Sight tall side armor - starboard',[(-.38,.80),(-.38,1.37),(-.22,1.44),(.15,1.38),(.19,1.05),(.08,.82)],.20,.067,'panel','optic',.012)
prism('Sight tall side armor - port',[(-.38,.80),(-.38,1.37),(-.22,1.44),(.15,1.38),(.19,1.05),(.08,.82)],-.20,.067,'panel','optic',.012)
box('Ivory sight forward face',(.209,0,1.116),(.081,.49,.46),'ivory','optic',.014)
box('Sight dark optical aperture',(.255,0,1.161),(.012,.275,.238),'dark','optic',.006)
box('Sight tiny cyan reticle',(.264,0,1.161),(.006,.15,.058),'cyan','optic',.004)
box('Upper black sight rail',(-.135,0,1.435),(.54,.14,.047),'dark','optic',.007)
cyl('Offset ivory rangefinder body',(.72,.31,.836),.143,.74,'ivory','optic',vertices=40,bevel=.012)
cyl('Rangefinder dark front rim',(1.106,.31,.836),.148,.067,'panel','optic',vertices=40)
cyl('Rangefinder recessed dark lens',(1.142,.31,.836),.107,.008,'dark','optic',vertices=40,bevel=.001)
cyl('Rangefinder cyan central lens',(1.148,.31,.836),.064,.006,'cyan','optic',vertices=32,bevel=.001)
box('Rangefinder support cleat',(.52,.27,.70),(.36,.26,.16),'shell','optic',.018)
pipe('Top slim sensor antenna',[(-.17,0,1.45),(-.22,0,1.49),(-.91,0,1.49)],.021,'steel','optic')
box('Antenna foot',(-.16,0,1.452),(.15,.12,.055),'shell','optic',.006)
for side in (-1,1):
    box('Sight safety red dash '+str(side),(.05,side*.238,1.28),(.075,.010,.017),'red','optic',.002)
    screw('Sight fastener '+str(side),(-.23,side*.241,.95),side,.022)

# Articulated two-pivot aft block, as specified by the new turnaround.
loft('Aft upper docking block',[(-2.73,.27,.36,.64),(-2.50,.34,.35,.71),(-2.08,.34,.17,.64)],'panel','grip',.024)
prism('Rear diagonal coupling link',[(-2.38,.48),(-2.18,.41),(-2.01,.10),(-2.04,-.14),(-2.20,-.26),(-2.39,-.15),(-2.56,.20)],0,.51,'dark','grip',.025)
prism('Angled rear main socket housing',[(-2.41,.12),(-2.10,-.06),(-2.13,-.25),(-2.79,-.84),(-3.07,-.64),(-3.17,-.43),(-2.65,.06)],0,.49,'shell','grip',.036)
prism('Rear socket layered lid',[(-2.55,.05),(-2.26,-.15),(-2.40,-.34),(-2.87,-.75),(-3.12,-.52),(-3.04,-.34)],0,.56,'panel','grip',.017)
prism('Rear socket rubber end cap',[(-2.91,-.70),(-3.14,-.48),(-3.26,-.62),(-3.07,-.86)],0,.43,'dark','grip',.021)
box('Rear socket inset dark terminal',(-3.09,0,-.703),(.16,.30,.11),'dark','grip',.013,rot=(0,-.65,0))
for side in (-1,1):
    prism('External rear articulated bracket '+str(side),[(-2.48,.60),(-2.32,.72),(-2.15,.60),(-2.13,.20),(-1.98,.06),(-2.02,-.16),(-2.21,-.24),(-2.32,-.08),(-2.37,.18)],side*.47,.068,'dark','grip',.016)
    for i,(x,z,r) in enumerate([(-2.32,.48,.18),(-2.10,-.02,.143)]):
        cyl('Rear pivot collar %s %s'%(side,i),(x,side*.503,z),r,.041,'panel','grip',(0,side,0),12,.009)
        cyl('Rear pivot machined ring %s %s'%(side,i),(x,side*.529,z),r*.78,.015,'steel','grip',(0,side,0),32,.005)
        cyl('Rear pivot recessed axle %s %s'%(side,i),(x,side*.540,z),r*.56,.008,'dark','grip',(0,side,0),24,.003)
    prism('Rear coupling side shield '+str(side),[(-2.43,.08),(-2.17,-.11),(-2.25,-.28),(-2.69,-.65),(-2.93,-.45)],side*.300,.027,'shell','grip',.011)
    for i in range(3):box('Rear terminal tread %s %s'%(side,i),(-2.90+i*.11,side*.310,-.64+i*.10),(.064,.027,.075),'dark',bevel=.006,rot=(0,-.75,0))
    for i,(x,z) in enumerate([(-2.56,-.19),(-2.71,-.49)]):screw('Rear shield fastener %s %s'%(side,i),(x,side*.318,z),side,.024)
    box('Rear hinge amber stop '+str(side),(-2.31,side*.512,.674),(.094,.028,.037),'yellow',bevel=.004)
    pipe('Fore-end square cable guard '+str(side),[(2.01,side*.241,-.08),(2.03,side*.249,-.18),(2.33,side*.225,-.18),(2.43,side*.216,-.08)],.023,'dark')

# The turnaround places the long ivory grip forward of the rotary selector.
for o in list(COLS['grip'].objects):
    if o.name.startswith(('Primary ivory grip','Textured black inner','Grip rib','Grip heel')):
        o.location.x+=.34
    elif o.name.startswith(('Selector ',)):
        o.location.x-=.73
    elif o.name.startswith(('Folding forward','Support-grip rear','Support handle')):
        o.location.x-=.81
    elif o.name.startswith(('Open trigger guard','Recessed curved trigger')):
        o.location.x-=.10
# The ivory frame stays broad; the fingers wrap this narrow rear spine.
box('Right-hand grip upper bridge',(.25,0,-.275),(.46,.145,.11),'panel','grip',.022)
box('Right-hand black grip spine',(.15,0,-.555),(.102,.105,.47),'dark','grip',.026)
box('Right-hand grip lower bridge',(.30,0,-.805),(.40,.145,.082),'panel','grip',.018)
for i in range(7):
    box('Right-hand grip inset rib '+str(i),(.15,-.057,-.38-i*.055),(.074,.014,.018),'panel','grip',.004)
for name,p in [('Grip',(.15,0,-.575)),('Support',(.34,0,-.33)),('Muzzle',(3.659,0,.13)),('BackMount',(-1.57,0,.04)),('Eject',(-.42,-.40,.5))]:
    o=bpy.data.objects.new(name,None);COLS['sockets'].objects.link(o);o.parent=root;o.location=p;o.empty_display_size=.10;o.empty_display_type='ARROWS'
    o['role']=name

# Clean neutral studio is separate from the exportable weapon.
stage=material('Studio_background',(.041,.051,.065),.1,.58)
box('Studio shadow receiver',(0,0,-.254*PRESENTATION_SCALE),(200,200,.08),stage,'studio',.0)
world=scene.world or bpy.data.worlds.new('HC09 studio world');scene.world=world;world.use_nodes=True
background=next(n for n in world.node_tree.nodes if n.type=='BACKGROUND');background.inputs[0].default_value=(.09,.12,.16,1);background.inputs[1].default_value=.35
def camera(name,loc,target,ortho):
    data=bpy.data.cameras.new(name);o=bpy.data.objects.new(name,data);COLS['studio'].objects.link(o);o.location=loc
    o.rotation_euler=(Vector(target)-o.location).to_track_quat('-Z','Y').to_euler();data.type='ORTHO';data.ortho_scale=ortho;data.lens=55;return o
cam=camera('01_HERO_THREE_QUARTER',(4.2,7,3.3),(.04,0,.12),2.20*PRESENTATION_SCALE)
camera('02_SIDE_PROFILE',(0,7,.12),(.04,0,.12),2.00*PRESENTATION_SCALE)
camera('03_RECEIVER_DETAIL',(2.7,4,2.1),(.00,0,.09),.95*PRESENTATION_SCALE)
camera('04_REAR_THREE_QUARTER',(-4,-6,3),(-.04,0,.16),2.13*PRESENTATION_SCALE)
camera('05_TOP_ORTHOGRAPHIC',(0,0,7),(0,0,0),2.00*PRESENTATION_SCALE)
for name,loc,energy,size,color in [
 ('Key broad softbox',(0,3,5),1000,4,(.87,.94,1)),
 ('Front clean fill',(4,-3,2.5),800,3,(1,.88,.72)),
 ('Rear strip reflection',(-3,-2,3.5),1500,3,(.71,.84,1))]:
    data=bpy.data.lights.new(name,'AREA');data.energy=energy;data.shape='DISK';data.size=size;data.color=color
    o=bpy.data.objects.new(name,data);COLS['studio'].objects.link(o);o.location=loc;o.rotation_euler=(Vector((0,0,.15))-o.location).to_track_quat('-Z','Y').to_euler()
scene.camera=cam
scene.render.engine='CYCLES';scene.cycles.samples=48;scene.cycles.use_denoising=True
try:
    prefs=bpy.context.preferences.addons['cycles'].preferences;prefs.compute_device_type='OPTIX';prefs.get_devices()
    gpu=False
    for d in prefs.devices:d.use=d.type!='CPU';gpu|=d.type!='CPU'
    scene.cycles.device='GPU' if gpu else 'CPU'
except Exception:scene.cycles.device='CPU'
scene.render.resolution_x=1800;scene.render.resolution_y=1050;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG';scene.render.film_transparent=False
scene.view_settings.view_transform='AgX'
scene.render.filepath=str(ROOT/'renders/01_CANNON_HERO.png')
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            area.spaces.active.region_3d.view_distance=2.6*PRESENTATION_SCALE
            area.spaces.active.region_3d.view_location=(0,0,.1)
            area.spaces.active.region_3d.view_rotation=cam.rotation_euler.to_quaternion()
            area.spaces.active.shading.type='MATERIAL'
            area.spaces.active.overlay.show_overlays=False
bpy.context.view_layer.update()
weapon=[o for o in root.children_recursive if o.type in {'MESH','CURVE','FONT'}]
dg=bpy.context.evaluated_depsgraph_get();triangles=0
for o in weapon:
    ev=o.evaluated_get(dg);me=ev.to_mesh();me.calc_loop_triangles();triangles+=len(me.loop_triangles);ev.to_mesh_clear()
stats={'asset':'TYPE-08 / Heavy mecha cannon','right_hand':True,'parts':len(weapon),'evaluated_triangles':triangles,'native_axis':'X forward, Z up','scale':list(root.scale),'target_length_m':TARGET_LENGTH,'mecha_body_height_m':3.495401382446289,'cycles_device':scene.cycles.device,'shape_reference':'reference/MECHA_CANNON_Turnaround.png','scale_reference':'reference/ZZ_FullMecha_ScaleReference.png','scale_rule':'Gun length equals the current mech body height; ignore the sheet 1800 mm caption.'}
(ROOT/'work/model_stats.json').write_text(json.dumps(stats,indent=2),encoding='utf-8')
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'HC09_CANNON_MASTER.blend'))
print('CANNON_BUILT',json.dumps(stats),flush=True)
