"""Reference-led external fire-control optic replacement for a fictional game prop."""
import bpy, bmesh, math, pathlib, json, hashlib, struct, os
from mathutils import Vector
P=pathlib.Path(__file__).resolve().parent;OUT=P/'stage_04';S=1.8/600
bpy.ops.wm.open_mainfile(filepath=str(P/'stage_03/AI_INFANTRY_M7_STAGE03.blend'))
sc=bpy.context.scene
bpy.context.preferences.filepaths.temporary_directory='D:/Tools/BlenderUserData/Temp'
root=bpy.data.objects['AI_Infantry_M7_Root']
cols={c.name.split(' | ')[1][:2]:c for c in bpy.data.collections if c.name.startswith('AI_M7 | ')}

def body_signature():
    h=hashlib.sha256()
    for key in ('01','02','03','04','05'):
        for ob in sorted(cols[key].objects,key=lambda o:o.name):
            h.update(ob.name.encode())
            if ob.type=='MESH':
                for v in ob.data.vertices:h.update(struct.pack('<3f',*v.co))
            h.update(str(tuple(ob.matrix_world)).encode())
    return h.hexdigest()
before_body=body_signature()
for ob in list(cols['06'].objects):bpy.data.objects.remove(ob,do_unlink=True)
optic=bpy.data.objects.new('FCS_Reference_Optic_Root',None);cols['06'].objects.link(optic);optic.parent=root
optic['reference']='FireControl_reference.png';optic['purpose']='Game-art exterior'
optic['finish']='Black and graphite exterior as previously requested'
PARTS=[];M={}

def log(t):print('STAGE04 '+t,flush=True)
def p(x,z,d=0):return Vector(((x-300)*S,-d*S,(270-z)*S))
def material(key,h,metal=.7,rough=.35):
    m=bpy.data.materials.new('FCS_'+key);m.use_nodes=True
    srgb=[int(h[i:i+2],16)/255 for i in (0,2,4)]
    rgb=[c/12.92 if c<=.04045 else ((c+.055)/1.055)**2.4 for c in srgb]
    m.diffuse_color=(*rgb,1)
    nodes=m.node_tree.nodes
    bs=next((n for n in nodes if n.type=='BSDF_PRINCIPLED'),None)
    if bs is None:bs=nodes.new('ShaderNodeBsdfPrincipled')
    bs.name='Principled BSDF'
    out=next((n for n in nodes if n.type=='OUTPUT_MATERIAL'),None)
    if out is None:out=nodes.new('ShaderNodeOutputMaterial')
    m.node_tree.links.new(bs.outputs[0],out.inputs['Surface'])
    bs.inputs['Base Color'].default_value=(*rgb,1);bs.inputs['Metallic'].default_value=metal;bs.inputs['Roughness'].default_value=rough
    M[key]=m;return m
material('Body','22292E',.77,.35)
material('Armor','343D43',.72,.38)
material('Edge','4A555D',.85,.31)
material('Recess','080D11',.08,.5)
material('Rubber','13191D',.02,.57)
material('Glass','0C3747',.47,.115)
material('SensorGlass','1C354E',.5,.1)
material('Coating','285869',.64,.16)
material('Marking','9FAEB5',.08,.58)
material('SubtleMark','6D7F86',.15,.51)
for key in ('Glass','SensorGlass'):
    bs=M[key].node_tree.nodes.get('Principled BSDF')
    bs.inputs['Coat Weight'].default_value=.65;bs.inputs['Coat Roughness'].default_value=.065
for key in ('Body','Armor','Edge','Rubber'):
    m=M[key];n=m.node_tree.nodes;l=m.node_tree.links;bs=n.get('Principled BSDF')
    coord=n.new('ShaderNodeTexCoord');noise=n.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=1800
    l.new(coord.outputs['Object'],noise.inputs['Vector'])
    bump=n.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.16;bump.inputs['Distance'].default_value=.000035
    l.new(noise.outputs['Fac'],bump.inputs['Height']);l.new(bump.outputs[0],bs.inputs['Normal'])

def finish(ob,width=.18,segments=3):
    if width:
        b=ob.modifiers.new('Fine machined radii','BEVEL');b.width=width*S;b.segments=segments;b.harden_normals=True
        w=ob.modifiers.new('Weighted face normals','WEIGHTED_NORMAL');w.keep_sharp=True
    return ob
def mesh(name,verts,faces,mat='Body',bev=.18):
    me=bpy.data.meshes.new('FCS_'+name+'_Mesh');me.from_pydata(verts,[],faces);me.update()
    bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free()
    ob=bpy.data.objects.new('FCS_'+name,me);cols['06'].objects.link(ob);ob.parent=optic;me.materials.append(M[mat]);PARTS.append(ob)
    return finish(ob,bev)
def poly(name,shape,front,back,mat='Body',bev=.18):
    n=len(shape);v=[p(x,z,d) for d in (front,back) for x,z in shape]
    f=[tuple(range(n-1,-1,-1)),tuple(range(n,2*n))]+[(k,(k+1)%n,(k+1)%n+n,k+n) for k in range(n)]
    return mesh(name,v,f,mat,bev)
def box(name,x,z,d,w,h,depth,mat='Body',bev=.18):
    return poly(name,[(x-w/2,z-h/2),(x+w/2,z-h/2),(x+w/2,z+h/2),(x-w/2,z+h/2)],d+depth/2,d-depth/2,mat,bev)
def hr(x0,z0,x1,z1,c=1):
    return [(x0+c,z0),(x1-c,z0),(x1,z0+c),(x1,z1-c),(x1-c,z1),(x0+c,z1),(x0,z1-c),(x0,z0+c)]
def rod(name,a,b,r,mat='Body',n=64,bev=.12,r2=None):
    a=Vector(a);b=Vector(b);q=(b-a).to_track_quat('Z','Y')
    v=[pt+q@Vector((rr*S*math.cos(k*math.tau/n),rr*S*math.sin(k*math.tau/n),0)) for pt,rr in ((a,r),(b,r if r2 is None else r2)) for k in range(n)]
    f=[tuple(range(n-1,-1,-1)),tuple(range(n,2*n))]+[(k,(k+1)%n,(k+1)%n+n,k+n) for k in range(n)]
    ob=mesh(name,v,f,mat,bev);axis=(b-a).normalized()
    for face in ob.data.polygons:face.use_smooth=n>=24 and abs(face.normal.dot(axis))<.5
    return ob
def ring(name,a,b,r,inner,mat='Body',n=64,bev=.12):
    a=Vector(a);b=Vector(b);q=(b-a).to_track_quat('Z','Y')
    v=[pt+q@Vector((rr*S*math.cos(k*math.tau/n),rr*S*math.sin(k*math.tau/n),0)) for pt,rr in ((a,r),(b,r),(a,inner),(b,inner)) for k in range(n)]
    f=[]
    for k in range(n):
        j=(k+1)%n;f.extend([(k,j,n+j,n+k),(2*n+k,3*n+k,3*n+j,2*n+j),(k,2*n+k,2*n+j,j),(n+k,n+j,3*n+j,3*n+k)])
    ob=mesh(name,v,f,mat,bev);axis=(b-a).normalized()
    for face in ob.data.polygons:face.use_smooth=n>=24 and abs(face.normal.dot(axis))<.5
    return ob
def lathe(name,profile,z=201,d=0,mat='Body',n=96,bev=.08):
    v=[p(x,z+r*math.sin(k*math.tau/n),d+r*math.cos(k*math.tau/n)) for x,r in profile for k in range(n)]
    f=[tuple(range(n-1,-1,-1)),tuple(range((len(profile)-1)*n,len(profile)*n))]
    for j in range(len(profile)-1):f.extend([(j*n+k,j*n+(k+1)%n,(j+1)*n+(k+1)%n,(j+1)*n+k) for k in range(n)])
    ob=mesh(name,v,f,mat,bev)
    for face in ob.data.polygons:face.use_smooth=len(face.vertices)==4
    return ob
def lens(name,x,z,d,r,mat='Glass',bulge=.7,front=True):
    n=96;nr=10;v=[p(x+(bulge if front else -bulge),z,d)]
    for j in range(1,nr+1):
        rr=r*j/nr;xx=x+(1 if front else -1)*bulge*(1-(rr/r)**2)
        v.extend([p(xx,z+rr*math.sin(k*math.tau/n),d+rr*math.cos(k*math.tau/n)) for k in range(n)])
    f=[(0,1+k,1+(k+1)%n) for k in range(n)]
    for j in range(nr-1):f.extend([(1+j*n+k,1+j*n+(k+1)%n,1+(j+1)*n+(k+1)%n,1+(j+1)*n+k) for k in range(n)])
    ob=mesh(name,v,f,mat,0)
    for face in ob.data.polygons:face.use_smooth=True
    return ob
def cut(ob,shapes,front,back):
    for m in list(ob.modifiers):ob.modifiers.remove(m)
    vs=[];fs=[]
    for xy in shapes:
        n=len(xy);o=len(vs);vs.extend([p(x,z,d) for d in (front,back) for x,z in xy])
        fs.extend([tuple(o+k for k in range(n-1,-1,-1)),tuple(o+k for k in range(n,2*n))])
        fs.extend([(o+k,o+(k+1)%n,o+(k+1)%n+n,o+k+n) for k in range(n)])
    cutter=mesh('Temporary_Apertures',vs,fs,'Body',0)
    mod=ob.modifiers.new('Real exterior cutouts','BOOLEAN');mod.operation='DIFFERENCE';mod.solver='EXACT';mod.object=cutter
    bpy.context.view_layer.objects.active=ob;bpy.ops.object.modifier_apply(modifier=mod.name)
    PARTS.remove(cutter);bpy.data.objects.remove(cutter,do_unlink=True);finish(ob,.16)
def screw(name,x,z,d,r=1.15):
    sg=1 if d>=0 else -1
    ring(name+'_Seat',p(x,z,d-sg*.15),p(x,z,d+sg*.12),r*1.24,r*.95,'Recess',40,.04)
    rod(name+'_Head',p(x,z,d),p(x,z,d+sg*.55),r,'Edge',48,.09)
    rod(name+'_Socket',p(x,z,d+sg*.56),p(x,z,d+sg*.6),r*.5,'Recess',6,.018)
def line(name,coords,r=.12,mat='Edge'):
    cu=bpy.data.curves.new('FCS_'+name,'CURVE');cu.dimensions='3D';cu.bevel_depth=r*S;cu.bevel_resolution=2
    sp=cu.splines.new('POLY');sp.points.add(len(coords)-1)
    for pt,pos in zip(sp.points,coords):pt.co=(*p(*pos),1)
    ob=bpy.data.objects.new('FCS_'+name,cu);cols['06'].objects.link(ob);ob.parent=optic;cu.materials.append(M[mat]);PARTS.append(ob)
    return ob
def text(name,body,x,z,d,size=1.1,mat='Marking'):
    cu=bpy.data.curves.new('FCS_Label_'+name,'FONT');cu.body=body;cu.size=size*S;cu.space_character=1.05;cu.resolution_u=2
    ob=bpy.data.objects.new('FCS_Label_'+name,cu);cols['06'].objects.link(ob);ob.parent=optic;ob.location=p(x,z,d)
    ob.rotation_euler=(math.pi/2,0,0 if d>=0 else math.pi);cu.materials.append(M[mat]);PARTS.append(ob)
    return ob

# Integrated open mounting base, with two feet visibly seated on the existing rail.
box('Mount_Central_Spine',278,218.2,0,140,5.3,16,'Body',.3)
for sg in (-1,1):
    outer=[(208,218),(216,213.8),(337,213.8),(351,219),(347,224),(212,224)]
    plate=poly('Mount_Skeleton_Side_'+str(sg),outer,sg*13.2,sg*10.3,'Armor',0)
    cut(plate,[hr(224,216,245,221,1.2),[(258,216),(282,216),(288,221),(257,221)],
               [(303,216),(319,216),(329,221),(303,221)]],sg*15,sg*8)
    line('Mount_Lower_Edge_'+str(sg),[(215,223.8,sg*13.3),(345,223.8,sg*13.3)],.15,'Edge')
    for x in (218,324):
        poly('Mount_Clamp_Foot_%s_%s'%(x,sg),[(x-6,218.5),(x+6,218.5),(x+7,223),(x+4,228),(x-6,226.5)],sg*14,sg*8.3,'Body',.35)
        screw('Mount_Large_Clamp_%s_%s'%(x,sg),x,221.8,sg*14.3,2.55)
        poly('Mount_Clamp_Lip_%s_%s'%(x,sg),[(x-4,225),(x+4,225),(x+3,227),(x-5,227)],sg*12,sg*8,'Edge',.15)
text('Mount_ID','E-01  /  FCS',267,223.1,13.4,1.05,'SubtleMark')

# Lower primary optical axis and rubber ocular bell.
lathe('Lower_Optical_Core',[(195,9.9),(225,9.9),(231,10.8),(276,10.8),(282,11.8),(313,11.8),(321,10.2),(330,12.8),(354,13.8)],201,0,'Body')
lathe('Rear_Ocular_Bell',[(184,12.9),(190,13.9),(208,13.9),(215,11.3),(218,10.4)],201,0,'Armor')
ring('Rear_Ocular_Lip',p(182.4,201),p(187,201),13.9,10.3,'Rubber',96,.3)
lens('Ocular_Optical_Glass',184.5,201,0,10.2,'SensorGlass',.45,False)
for j in range(28):
    a=j*math.tau/28;z=201+13.9*math.sin(a);d=13.9*math.cos(a)
    rod('Ocular_Edge_Knurl_%02d'%j,p(184.2,z,d),p(188,z,d),.47,'Armor',12,.08)
ring('Ocular_Case_Seam',p(208,201),p(209,201),13.8,13.2,'Rubber',80,.08)
lathe('Broad_Focus_Collar',[(215,11.2),(217,14.4),(231,14.4),(233,12),(237,10.8)],201,0,'Body')
for j in range(52):
    a=j*math.tau/52;z=201+14.35*math.sin(a);d=14.35*math.cos(a)
    rod('Focus_Collar_Knurl_%02d'%j,p(218,z,d),p(230,z,d),.36,'Armor',12,.07)
for x in (217,231):ring('Focus_Collar_Edge_'+str(x),p(x,201),p(x+.65,201),14.6,14,'Edge',96,.07)
log('mounting base and primary optic built')

# Upper rectangular sensor module: the reference's defining stacked silhouette.
case=poly('Upper_Sensor_Armored_Case',[(226,172),(232,165),(279,164),(294,168),(291,181),
              (289,190),(239,190),(227,184)],18,-18,'Body',.6)
for sg in (-1,1):
    cut(case,[hr(234,170,277,184,2.2)],sg*20,sg*17)
    poly('Sensor_Side_Inset_'+str(sg),hr(235.5,171.3,275.5,182.8,1.6),sg*17.16,sg*16.7,'Armor',.22)
    # The reference uses a plain inset cover with corner hardware, not a side screen.
    for x,z in ((236,173),(274,180.4)):
        screw('Sensor_Cover_Captive_%s_%s_%s'%(x,z,sg),x,z,sg*17.4,.8)
    rod('Sensor_Side_Button_Base_'+str(sg),p(283,178,sg*18),p(283,178,sg*18.9),2.8,'Armor',48,.18)
    rod('Sensor_Side_Button_'+str(sg),p(283,178,sg*18.8),p(283,178,sg*20),2.05,'Rubber',48,.17)
    line('Sensor_Lower_Case_Seam_'+str(sg),[(232,187,sg*18.1),(280,186.6,sg*18.1)],.14,'Edge')
    text('Sensor_Side_ID_'+str(sg),'FCS  /  E-01',239,180.9,sg*17.4,1.35,'SubtleMark')
box('Sensor_Roof_Gasket',256,164,0,49,1.7,30,'Rubber',.5)
poly('Sensor_Roof_Armor',[(231,164),(237,161.7),(277,161.7),(283,164.2),(281,165.2),(234,165.2)],15,-15,'Armor',.4)
box('Sensor_Roof_Center_Plate',258,161.15,0,29,1.5,21,'Body',.38)
for x in (240,274):
    for d in (-11,11):
        rod('Roof_Captive_%s_%s'%(x,d),p(x,162.3,d),p(x,161.8,d),.9,'Edge',32,.05)
        rod('Roof_Captive_Socket_%s_%s'%(x,d),p(x,161.78,d),p(x,161.7,d),.42,'Recess',6,.015)
for sg in (-1,1):
    ear=poly('Sensor_Rear_Protective_Ear_'+str(sg),[(229,172),(228,164),(230,159),(233,158.4),(236,161),(237,170)],sg*17,sg*14,'Armor',0)
    circle=[(232.4+1.9*math.cos(k*math.tau/32),163.3+1.9*math.sin(k*math.tau/32)) for k in range(32)]
    cut(ear,[circle],sg*19,sg*12)
    poly('Sensor_Front_Roof_Lug_'+str(sg),[(279,164),(281,160.5),(285,161),(286.5,167)],sg*15,sg*12,'Body',.35)

# Canted shared front window with two distinct optical apertures.
def frontx(z):return 293.5-.22*(z-169)
def front_panel(name,outline,offset,thick,mat='Body',bev=.1):
    n=len(outline);v=[p(frontx(z)+xx,z,d) for xx in (offset-thick,offset) for z,d in outline]
    f=[tuple(range(n-1,-1,-1)),tuple(range(n,2*n))]+[(k,(k+1)%n,(k+1)%n+n,k+n) for k in range(n)]
    return mesh(name,v,f,mat,bev)
def front_frame(name,outer,inner,offset,thickness,mat='Armor'):
    n=len(outer);v=[p(frontx(z)+xx,z,d) for xx,shape in ((offset-thickness,outer),(offset,outer),(offset-thickness,inner),(offset,inner)) for z,d in shape]
    f=[]
    for k in range(n):
        j=(k+1)%n;f.extend([(k,j,n+j,n+k),(2*n+k,3*n+k,3*n+j,2*n+j),(k,2*n+k,2*n+j,j),(n+k,n+j,3*n+j,3*n+k)])
    return mesh(name,v,f,mat,.16)
outer=hr(168.4,-17.7,188.6,17.7,2.1)
inner=hr(171,-14.5,186,14.5,1.2)
# Open a real canted window in the pod, so the lenses do not intersect its face.
window_cutter=front_panel('Temporary_Front_Window_Cutter',inner,1.5,4.8,'Body',0)
for modifier in list(case.modifiers):case.modifiers.remove(modifier)
mod=case.modifiers.new('Front optical window opening','BOOLEAN');mod.operation='DIFFERENCE';mod.solver='EXACT';mod.object=window_cutter
bpy.context.view_layer.objects.active=case;bpy.ops.object.modifier_apply(modifier=mod.name)
PARTS.remove(window_cutter);bpy.data.objects.remove(window_cutter,do_unlink=True);finish(case,.28)
front_panel('Shared_Sensor_Window_Shadow',inner,-.3,1.2,'Recess',.07)
front_frame('Sensor_Front_Window_Frame',outer,inner,.6,1.8,'Armor')
front_frame('Sensor_Front_Window_Seal',hr(170.65,-14.85,186.35,14.85,1.2),hr(171.1,-14.4,185.9,14.4,1.1),.35,.45,'Rubber')
for j,(z,d,r) in enumerate(((178.4,7.1,5.6),(177,-7.5,4.4))):
    x=frontx(z)
    ring('Sensor_Objective_Seat_'+str(j),p(x-.6,z,d),p(x+.10,z,d),r+.65,r,'Body',72,.09)
    lens('Sensor_Objective_Glass_'+str(j),x-.05,z,d,r-.06,'Glass' if j==0 else 'SensorGlass',.42)
    ring('Sensor_Objective_Coating_'+str(j),p(x+.05,z,d),p(x+.12,z,d),r-.03,r-.20,'Coating',72,.025)
front_panel('Sensor_Window_Divider',hr(171.1,-.45,185.8,.45,.2),.2,.45,'Body',.05)
ring('Sensor_Small_Port',p(frontx(183),183,-7.5),p(frontx(183)+.2,183,-7.5),1.35,.8,'Edge',40,.04)
lens('Sensor_Small_Port_Glass',frontx(183)+.1,183,-7.5,.78,'SensorGlass',.12)
# Align each optical surface with the canted front plate.
for ob in list(cols['06'].objects):
    if ob.type!='MESH':continue
    center_z=None
    for j,zz in ((0,178.4),(1,177)):
        if ob.name in ('FCS_Sensor_Objective_Seat_'+str(j),'FCS_Sensor_Objective_Glass_'+str(j),'FCS_Sensor_Objective_Coating_'+str(j)):center_z=zz
    if ob.name in ('FCS_Sensor_Small_Port','FCS_Sensor_Small_Port_Glass'):center_z=183
    if center_z is not None:
        for vertex in ob.data.vertices:
            zz=270-vertex.co.z/S;vertex.co.x-=.22*(zz-center_z)*S
for z in (170.5,186.5):
    for d in (-15.8,15.8):
        x=frontx(z)+.62
        rod('Sensor_Front_Fastener_%s_%s'%(z,d),p(x,z,d),p(x+.38,z,d),.85,'Edge',32,.06)
        rod('Sensor_Front_Hex_%s_%s'%(z,d),p(x+.4,z,d),p(x+.44,z,d),.4,'Recess',6,.01)

# Trussed support under the sensor pod, with genuine open slots and diagonal webs.
for sg in (-1,1):
    support=poly('Sensor_Trussed_Support_'+str(sg),[(227,188),(273,187),(283,200),(287,209),
                  (279,216),(236,215),(226,210),(223,202)],sg*17.2,sg*13.8,'Armor',0)
    holes=[hr(230,191,250,197,2.4),hr(232,203,250,209.5,2.4),
           [(256,191),(267,190),(278,205),(273,210),(260,205)],
           [(253,200),(258,201),(260,206),(256,209),(251,206)]]
    cut(support,holes,sg*19,sg*11)
    line('Truss_Diagonal_Stiffener_'+str(sg),[(271,189,sg*17.45),(284,208,sg*17.45),(279,213,sg*17.45)],.3,'Edge')
    for x,z in ((229,201),(273,213)):
        screw('Truss_Captive_%s_%s_%s'%(x,z,sg),x,z,sg*17.5,1.2)
    ring('Rear_Collar_Side_Pin_'+str(sg),p(227,190,sg*16.6),p(227,190,sg*18.5),3.1,1.65,'Body',48,.15)
    rod('Rear_Collar_Pivot_Cap_'+str(sg),p(227,190,sg*17.9),p(227,190,sg*18.2),1.5,'Edge',32,.08)
log('stacked sensor pod, dual front windows and open truss built')

# Large central adjustment drums, deliberately separated from the forward bell.
lathe('Central_Turret_Housing',[(282,11.7),(287,13.1),(312,13.1),(318,11.6)],201,0,'Armor',96,.18)
rod('Top_Adjustment_Pedestal',p(303,191),p(303,185),10.1,'Body',80,.3)
rod('Top_Adjustment_Drum',p(303,186.3),p(303,183.1),9.65,'Armor',80,.24)
ring('Top_Adjustment_Recessed_Rim',p(303,183.5),p(303,182.6),8.7,6.7,'Edge',80,.09)
rod('Top_Adjustment_Face',p(303,183.6),p(303,183),6.5,'Body',80,.09)
ring('Top_Adjustment_Center',p(303,183),p(303,182.7),3.2,1.7,'Armor',56,.08)
rod('Top_Adjustment_Hex',p(303,182.8),p(303,182.65),1.65,'Recess',6,.03)
for j in range(32):
    a=j*math.tau/32
    x,d=303+9.67*math.cos(a),9.67*math.sin(a)
    rod('Top_Drum_Fine_Knurl_%02d'%j,p(x,184,d),p(x,185.8,d),.23,'Edge',12,.045)
for j in range(24):
    a=j*math.tau/24;r0=7 if j%3==0 else 7.5
    line('Top_Dial_Etched_Tick_%02d'%j,[(303+r0*math.cos(a),182.52,r0*math.sin(a)),(303+8.1*math.cos(a),182.52,8.1*math.sin(a))],.075,'Marking')

for sg in (-1,1):
    rod('Side_Dial_Pedestal_'+str(sg),p(302,202,sg*10),p(302,202,sg*18.6),10.8,'Body',96,.3)
    rod('Side_Dial_Drum_'+str(sg),p(302,202,sg*18.2),p(302,202,sg*21),10.3,'Armor',96,.22)
    ring('Side_Dial_Bezel_'+str(sg),p(302,202,sg*20.7),p(302,202,sg*21.6),9.75,7.2,'Edge',96,.11)
    rod('Side_Dial_Inner_Face_'+str(sg),p(302,202,sg*21),p(302,202,sg*21.3),7.12,'Body',80,.08)
    ring('Side_Dial_Center_Ring_'+str(sg),p(302,202,sg*21.4),p(302,202,sg*21.8),4.65,3.25,'Armor',72,.09)
    rod('Side_Dial_Center_Cap_'+str(sg),p(302,202,sg*21.65),p(302,202,sg*22.1),3.12,'Body',72,.11)
    rod('Side_Dial_Hex_'+str(sg),p(302,202,sg*22.15),p(302,202,sg*22.2),1.25,'Recess',6,.025)
    for j in range(36):
        a=j*math.tau/36
        x,z=302+10.35*math.cos(a),202+10.35*math.sin(a)
        rod('Side_Dial_Knurl_%s_%s'%(j,sg),p(x,z,sg*18.8),p(x,z,sg*20.6),.22,'Body',12,.04)
    for j in range(30):
        a=j*math.tau/30;rr=7.9 if j%5==0 else 8.5
        line('Side_Dial_Tick_%s_%s'%(j,sg),[(302+rr*math.cos(a),202+rr*math.sin(a),sg*21.72),
              (302+9.05*math.cos(a),202+9.05*math.sin(a),sg*21.72)],.075,'Marking')
    for j,(dx,dz) in enumerate(((0,-6),(5.5,0),(0,6),(-5.5,0))):
        text('Dial_Index_%s_%s'%(j,sg),str(j*2),301.5+dx,202.45+dz,sg*21.5,.92,'SubtleMark')
log('central top and side adjustment dials built')

# Forward objective: a broad optical bell inside shaped protective cheeks.
lathe('Forward_Objective_Bell',[(314,10.2),(320,11.2),(328,13.5),(336,15.1),(354,15.1)],201,0,'Body',112,.15)
for sg in (-1,1):
    cheek=poly('Objective_Protective_Cheek_'+str(sg),[(318,193),(326,187),(350,186),(359,193),
                    (361,207),(356,216),(333,218),(319,213)],sg*15.2,sg*11.7,'Armor',0)
    cut(cheek,[[(326,187.5),(337,187.5),(340,195),(337,200),(330,197.7),(327,193)],
               hr(321,202,325.3,208,1.8)],sg*17,sg*10)
    line('Objective_Lower_Case_Joint_'+str(sg),[(326,212.8,sg*15.35),(349,212.8,sg*15.35),(359,207,sg*15.35)],.17,'Edge')
    line('Objective_Upper_Edge_'+str(sg),[(340,187.3,sg*15.3),(349,187.3,sg*15.3),(357,193.2,sg*15.3)],.17,'Edge')
    for x,z in ((328,207),(354,200.5)):
        screw('Objective_Cheek_Captive_%s_%s_%s'%(x,z,sg),x,z,sg*15.5,1.15)
    # A stepped lower return joins the bell protection to the base structure.
    poly('Objective_Lower_Return_'+str(sg),[(321,213),(336,216),(355,214),(360,218),(355,222),(326,222),(319,219)],sg*12.6,sg*9.3,'Body',.32)
    poly('Objective_Lower_Inset_'+str(sg),hr(328,216.4,352,220.4,1.3),sg*12.95,sg*12.4,'Armor',.18)
ring('Objective_Front_Outer_Collar',p(353,201),p(361,201),16.1,14.1,'Body',112,.2)
ring('Objective_Deep_Internal_Seal',p(357,201),p(360.3,201),14.15,13.85,'Rubber',112,.08)
lens('Main_Objective_Convex_Lens',361.3,201,0,13.88,'Glass',.82)
ring('Objective_Coated_Periphery',p(361.4,201),p(361.55,201),13.9,13.62,'Coating',112,.025)
ring('Objective_Protective_Front_Lip',p(360.3,201),p(364,201),16.6,14.2,'Armor',112,.18)
ring('Objective_Fine_Front_Rim',p(363.45,201),p(364.4,201),16.75,14.45,'Edge',112,.1)
for j in range(40):
    a=j*math.tau/40;z=201+16.65*math.sin(a);d=16.65*math.cos(a)
    rod('Objective_Rim_Fine_Notch_%02d'%j,p(361.7,z,d),p(363,z,d),.12,'Body',10,.022)

# Recessed front accessory socket with a short retaining tether, as in the reference.
box('Forward_Connector_Block',358.7,219.4,8,10,7.4,8,'Body',.6)
ring('Front_Connector_Protective_Collar',p(362,220.6,8),p(368,220.6,8),3.5,2.15,'Body',64,.22)
ring('Front_Connector_Rim',p(366.5,220.6,8),p(368.4,220.6,8),3.6,2.4,'Edge',64,.11)
rod('Front_Connector_Deep_Socket',p(366.7,220.6,8),p(367,220.6,8),2.08,'Recess',48,.03)
for j in range(4):
    a=j*math.tau/4
    rod('Connector_Visible_Pin_'+str(j),p(367.1,220.6+.95*math.sin(a),8+.95*math.cos(a)),
        p(367.3,220.6+.95*math.sin(a),8+.95*math.cos(a)),.16,'Edge',16,.018)
tether=line('Connector_Retaining_Tether',[(365,221.8,11.2),(369,223.5,13.5),(371,227.5,16.5),
       (370,231,18.2),(367,233,18.8),(363.6,230.8,18),(361.5,226.8,15.8),(365,220.7,11.3)],.42,'Rubber')
points=[Vector(pt.co[:3]) for pt in tether.data.splines[0].points]
tether.data.splines.clear();spline=tether.data.splines.new('BEZIER');spline.bezier_points.add(len(points)-1)
for pt,co in zip(spline.bezier_points,points):
    pt.co=co;pt.handle_left_type='AUTO';pt.handle_right_type='AUTO'
tether.data.resolution_u=20
text('Objective_Asset_Marking','E01 / OPTICS',334,211.6,15.5,1.0,'SubtleMark')
log('forward objective, protective housing and tether complete')

# Preserve the source image in the native file and verify the rest of the rifle.
im=bpy.data.images.load(str(P/'references/FireControl_reference.png'));im.pack()
sc['optic_reference_image']=im.name
after_body=body_signature()
assert before_body==after_body,'Unexpected change to rifle body geometry'
root['stage']='04 / reference fire-control optic installed'
root['optic_finish']='Black and graphite with optical glass; reference silhouette'
sc['stage']='04: reference-led fire-control optic, installed on stage03 rifle'

def aim(cam,target,offset,width):
    cam.location=Vector(target)+Vector(offset);cam.rotation_euler=(-Vector(offset)).to_track_quat('-Z','Y').to_euler()
    cam.data.type='ORTHO';cam.data.ortho_scale=width
def camera(name,target,offset,width):
    ob=bpy.data.objects.get(name)
    if ob is None:
        data=bpy.data.cameras.new(name);ob=bpy.data.objects.new(name,data);cols['07'].objects.link(ob)
    aim(ob,target,offset,width);return ob
camera('OPTIC_MOUNTED',p(275,199,0),(.92,-1.5,.84),.70)
camera('OPTIC_ISOLATED',p(275,194,0),(.92,-1.5,.84),.67)
camera('OPTIC_DETAIL',p(275,194,0),(.92,-1.5,.84),.67)
camera('OPTIC_SIDE',p(275,194,0),(0,-1.8,0),.66)
camera('FRONT',(0,0,-.035),(5,0,0),.97)

sc.render.engine='CYCLES';sc.cycles.samples=96;sc.cycles.use_denoising=True
# A single tile covers every review image, avoiding external tile-buffer files.
sc.cycles.tile_size=4096
try:
    pref=bpy.context.preferences.addons['cycles'].preferences;pref.compute_device_type='OPTIX';pref.get_devices()
    for device in pref.devices:device.use=device.type=='OPTIX'
    sc.cycles.device='GPU' if any(d.use for d in pref.devices) else 'CPU'
except Exception:sc.cycles.device='CPU'
sc.render.image_settings.file_format='PNG';sc.render.image_settings.color_mode='RGBA'
sc.render.resolution_x=2900;sc.render.resolution_y=1300;sc.render.resolution_percentage=100
sc.camera=bpy.data.objects['THREE_QUARTER']
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            area.spaces.active.shading.type='MATERIAL'
            area.spaces.active.region_3d.view_location=Vector((.207,0,-.015))
            area.spaces.active.region_3d.view_distance=2.8
            area.spaces.active.region_3d.view_rotation=sc.camera.rotation_euler.to_quaternion()
            area.spaces.active.overlay.show_overlays=False
bpy.ops.object.select_all(action='DESELECT');optic.select_set(True);bpy.context.view_layer.objects.active=optic
bpy.context.preferences.filepaths.save_version=0
asset=OUT/'AI_INFANTRY_M7_STAGE04.blend'
bpy.ops.wm.save_as_mainfile(filepath=str(asset))
manifest={'stage':4,'status':'reference optic installed; rendered for visual review',
          'source_master':'stage_03/AI_INFANTRY_M7_STAGE03.blend','reference':'references/FireControl_reference.png',
          'rifle_body_geometry_unchanged':before_body==after_body,'body_signature':after_body,
          'optic_parts':len(PARTS),'optic_materials':[m.name for m in M.values()],
          'reference_features':['upper rectangular sensor pod','shared dual front optical window','lower main optical axis',
          'wide central adjustment drums','open diagonal support','integrated skeleton base and clamp feet',
          'armored objective bell','recessed connector and retaining tether'],
          'colour':'black and graphite exterior per previous user instruction',
          'pending':['visual feedback','white infantry character fitting','game export']}
(OUT/'stage04_manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
log('master saved; rifle body signature verified')
names=os.environ.get('M7_STAGE04_RENDER_SET','THREE_QUARTER,OPTIC_MOUNTED,OPTIC_ISOLATED,SIDE,OPPOSITE_SIDE,TOP,FRONT').split(',')
for name in names:
    isolated=name in ('OPTIC_ISOLATED','OPTIC_SIDE')
    for key in ('01','02','03','04','05'):cols[key].hide_render=isolated
    sc.camera=bpy.data.objects[name]
    if name.startswith('OPTIC_'):sc.render.resolution_x=2600;sc.render.resolution_y=1900
    elif name=='FRONT':sc.render.resolution_x=1100;sc.render.resolution_y=1600
    else:sc.render.resolution_x=2900;sc.render.resolution_y=1300
    sc.render.filepath=str(OUT/'renders'/(name+'.png'));bpy.ops.render.render(write_still=True)
    log('rendered '+name)
for key in ('01','02','03','04','05'):cols[key].hide_render=False
log('COMPLETE')
