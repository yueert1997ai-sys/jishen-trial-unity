"""Stage 01: exterior game-prop proportions and colour approval, not final detail."""
import bpy,bmesh,math,pathlib,json
from mathutils import Vector
P=pathlib.Path(__file__).resolve().parent
OUT=P/'stage_01';S=1.8/600
bpy.ops.wm.read_factory_settings(use_empty=True)
sc=bpy.context.scene;sc.unit_settings.system='METRIC'
bpy.context.preferences.filepaths.temporary_directory='D:/Tools/BlenderUserData/Temp'
M={};PARTS=[]
def material(key,col,metal=.45,rough=.35):
    m=bpy.data.materials.new('AI_M7_'+key);m.use_nodes=True
    rgb=[int(col[i:i+2],16)/255 for i in (0,2,4)];rgb=[c/12.92 if c<=.04045 else ((c+.055)/1.055)**2.4 for c in rgb]
    m.diffuse_color=(*rgb,1);bs=m.node_tree.nodes.get('Principled BSDF')
    bs.inputs['Base Color'].default_value=(*rgb,1);bs.inputs['Metallic'].default_value=metal;bs.inputs['Roughness'].default_value=rough
    M[key]=m
material('Desert','A28965',.48,.34);material('Sand_Edge','BEA581',.42,.3)
material('Black','20252A',.63,.32);material('Rubber','101418',.05,.54)
material('Steel','515961',.8,.29);material('Glass','123B40',.6,.16)
material('Recess','080C10',.05,.5)
root=bpy.data.objects.new('AI_Infantry_M7_Root',None);sc.collection.objects.link(root)
root['stage']='01 / silhouette, colour and optic mass review'
root['owner']='White AI infantry / TYPE E-01'
root['scale_status']='Provisional game-prop scale; final character grip fit follows approval'
cols={}
for n in ('01 Stock','02 Receiver','03 Grip and magazine','04 Handguard','05 Barrel exterior','06 Fire control optic','07 Review'):
    c=bpy.data.collections.new('AI_M7 | '+n);sc.collection.children.link(c);cols[n[:2]]=c
def p(x,z,d=0):return Vector(((x-300)*S,-d*S,(270-z)*S))
def mesh(name,vs,fs,mat='Desert',group='02',bev=.65):
    me=bpy.data.meshes.new(name+'_Mesh');me.from_pydata(vs,[],fs);me.update()
    bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free()
    ob=bpy.data.objects.new('M7_'+name,me);cols[group].objects.link(ob);ob.parent=root;me.materials.append(M[mat])
    if bev:
        b=ob.modifiers.new('Hard machined edge','BEVEL');b.width=bev*S;b.segments=2;b.harden_normals=True
        w=ob.modifiers.new('Weighted normals','WEIGHTED_NORMAL');w.keep_sharp=True
    PARTS.append(ob);return ob
def poly(name,xy,front,back,mat='Desert',group='02',bev=.65):
    n=len(xy);vs=[p(x,z,d) for d in (front,back) for x,z in xy]
    fs=[tuple(range(n-1,-1,-1)),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    return mesh(name,vs,fs,mat,group,bev)
def plate(name,xy,d,thick,mat='Desert',group='02',bev=.5):
    return [poly(name+suffix,xy,sg*d,sg*(d-thick),mat,group,bev) for sg,suffix in ((1,'_L'),(-1,'_R'))]
def rod(name,a,b,r,mat='Black',group='05',n=16,bev=.28,r2=None):
    a=Vector(a);b=Vector(b);q=(b-a).to_track_quat('Z','Y')
    vs=[v+q@Vector((rr*S*math.cos(k*math.tau/n),rr*S*math.sin(k*math.tau/n),0)) for v,rr in ((a,r),(b,r if r2 is None else r2)) for k in range(n)]
    fs=[tuple(range(n-1,-1,-1)),tuple(range(n,2*n))]+[(k,(k+1)%n,(k+1)%n+n,k+n) for k in range(n)]
    return mesh(name,vs,fs,mat,group,bev)
def ring(name,a,b,r,inner,mat='Steel',group='05',n=16,bev=.2):
    a=Vector(a);b=Vector(b);q=(b-a).to_track_quat('Z','Y')
    vs=[v+q@Vector((rr*S*math.cos(k*math.tau/n),rr*S*math.sin(k*math.tau/n),0)) for v,rr in ((a,r),(b,r),(a,inner),(b,inner)) for k in range(n)]
    fs=[]
    for i in range(n):
        j=(i+1)%n;fs.extend([(i,j,n+j,n+i),(2*n+i,3*n+i,3*n+j,2*n+j),(i,2*n+i,2*n+j,j),(n+i,n+j,3*n+j,3*n+i)])
    return mesh(name,vs,fs,mat,group,bev)
def box(name,c,dim,mat='Black',group='02',bev=.5):
    x,z,d=c;dx,dz,dd=dim
    return poly(name,[(x-dx/2,z-dz/2),(x+dx/2,z-dz/2),(x+dx/2,z+dz/2),(x-dx/2,z+dz/2)],d+dd/2,d-dd/2,mat,group,bev)
def bolt(name,x,z,d,r=1.8,group='02'):
    for sg in (-1,1):
        rod(name+str(sg),p(x,z,sg*d),p(x,z,sg*(d+.7)),r,'Steel',group,8,.1)
        rod(name+'_Socket'+str(sg),p(x,z,sg*(d+.72)),p(x,z,sg*(d+.82)),r*.42,'Recess',group,6,.02)

# M7-specific stock outline, with a genuinely open lower triangular area.
poly('Butt_Pad',[(1,236),(8,236),(13,246),(14,302),(10,309),(2,306)],12,-12,'Rubber','01',1)
poly('Stock_Cheek_Chassis',[(7,236),(103,239),(115,249),(105,258),(32,257),(16,268),(8,268)],11,-11,'Desert','01',1.1)
poly('Stock_Lower_Strut',[(17,292),(77,271),(103,266),(99,276),(29,300),(20,305),(15,302)],8,-8,'Desert','01',.8)
poly('Stock_Rear_Strut',[(8,259),(17,257),(23,290),(19,301),(11,300)],10,-10,'Desert','01',.8)
plate('Stock_Rubber_Cheek',[(19,241),(95,243),(102,249),(96,254),(33,252),(20,257)],12,2.5,'Sand_Edge','01',.7)
plate('Stock_Length_Latch',[(72,257),(105,253),(107,260),(83,270),(76,269)],11,2.5,'Black','01',.45)
rod('Stock_Axis_Exterior',p(104,252),p(153,252),7.3,'Black','01',12,.4)
ring('Stock_External_Hinge',p(124,252),p(138,252),10,6,'Steel','01',12,.4)
bolt('Stock_Pivot',117,249,10.5,2.5,'01')

# Layered receiver with a recognizable sloping rear and magazine well.
poly('Upper_Receiver',[(141,239),(151,231),(306,231),(314,237),(311,258),(298,272),(185,273),(152,261)],12,-12,'Desert','02',1)
plate('Upper_Receiver_Slab',[(154,236),(292,236),(303,240),(300,246),(209,246),(180,252),(155,247)],13.2,2.4,'Sand_Edge','02',.6)
poly('Lower_Receiver',[(152,260),(180,269),(227,270),(241,266),(303,267),(304,303),(294,310),(242,314),(232,293),(200,292),(179,303),(163,287)],12.5,-12.5,'Desert','02',.8)
plate('Magazine_Well_Facet',[(242,278),(297,272),(298,303),(243,310),(237,299)],13.2,1.8,'Sand_Edge','02',.6)
plate('Receiver_Dark_Service_Inset',[(219,246),(290,246),(293,254),(287,265),(218,265)],13,1.2,'Black','02',.35)
plate('Receiver_Metal_Cover',[(225,248),(253,248),(254,260),(224,260)],14.2,1.4,'Steel','02',.3)
plate('Receiver_Rear_Machinery_Seat',[(156,253),(201,253),(209,258),(206,266),(166,265)],13.3,1.3,'Desert','02',.45)
for j,(x,z) in enumerate(((149,248),(181,278),(223,281),(299,280))):bolt('Receiver_Pin_%d'%j,x,z,13.7,1.7)

# Pistol grip and separate open trigger-guard frame, all exterior prop geometry.
poly('Pistol_Grip',[(170,296),(198,303),(185,336),(181,361),(153,354),(150,348)],10.8,-10.8,'Black','03',1.0)
plate('Grip_Rubber_Inset',[(169,307),(189,311),(178,349),(158,345)],11.3,1.2,'Rubber','03',.5)
poly('Grip_Heel',[(153,348),(181,353),(182,362),(151,356)],11.8,-11.8,'Black','03',.65)
poly('Trigger_Guard_Lower',[(190,304),(201,307),(234,307),(241,300),(244,303),(237,313),(198,313),(188,309)],8,-8,'Black','03',.65)
poly('Trigger_Guard_Front',[(237,283),(243,285),(244,304),(237,309),(235,305)],8,-8,'Black','03',.45)
poly('Trigger_Visual',[(213,288),(218,287),(215,296),(216,302),(212,301),(210,295)],2,-2,'Steel','03',.3)
poly('Magazine',[(239,308),(292,306),(299,355),(305,370),(252,381),(244,355)],10.5,-10.5,'Black','03',1.0)
plate('Magazine_Stamping',[(246,315),(285,314),(292,361),(256,368),(250,347)],11.1,1.2,'Rubber','03',.45)
poly('Magazine_Floor_Plate',[(250,371),(303,360),(307,371),(253,383)],12,-12,'Black','03',.65)
for j in range(3):
    z=328+j*13;x=250+j*1.6
    plate('Magazine_Pressed_Rib_%d'%j,[(x,z),(291+j,z-3),(292+j,z),(x+1,z+3)],12,1.0,'Black','03',.2)

# Long open slotted fore-end: structural strips and diagonal bridges show depth.
rod('Dark_Barrel_Visual_Core',p(299,255),p(585,255),5.5,'Black','05',20,.3)
poly('Foreend_Root_Collar',[(304,230),(322,231),(326,237),(320,273),(313,278),(302,273)],14,-14,'Desert','04',.6)
plate('Handguard_Upper_Rail',[(316,232),(517,232),(525,237),(525,241),(316,241)],14.5,3,'Desert','04',.45)
plate('Handguard_Middle_Stringer',[(316,250),(523,250),(523,255),(314,255)],14.5,3,'Desert','04',.35)
plate('Handguard_Lower_Stringer',[(310,269),(523,269),(523,274),(307,278)],13.5,3,'Desert','04',.45)
for j in range(9):
    x=320+j*21.8
    plate('Upper_Slot_Bridge_%02d'%j,[(x,239),(x+4,239),(x+10,251),(x+6,251)],14.5,3,'Desert','04',.24)
    plate('Lower_Slot_Bridge_%02d'%j,[(x+4,254),(x+8,254),(x+2,270),(x-2,270)],13.9,3,'Desert','04',.24)
box('Handguard_Bottom_Spine',(417,275,0),(207,4,17),'Black','04',.4)
poly('Handguard_Front_Frame',[(518,232),(527,237),(527,272),(521,277),(516,272),(519,255)],14.5,-14.5,'Desert','04',.5)
for x in (316,510):bolt('Foreend_Captive_Pin_'+str(x),x,264,15,1.6,'04')
# Simplified continuous upper rail establishes the optic mounting height.
box('Continuous_Top_Rail',(330,229,0),(384,4.8,15.5),'Black','04',.25)
for j in range(48):box('Top_Rail_Tooth_%02d'%j,(145+j*7.9,225.8,0),(4.7,3.0,18),'Black','04',.14)
ring('Front_Barrel_Collar',p(526,255),p(537,255),7.2,5.0,'Steel','05',16,.35)
rod('Front_Visible_Barrel',p(536,255),p(579,255),5.0,'Black','05',20,.2)
ring('Muzzle_Collar',p(576,255),p(584,255),7.0,4.4,'Steel','05',16,.25)
ring('Muzzle_Exterior',p(583,255),p(598,255),7.3,4.7,'Black','05',12,.3)
rod('Muzzle_Dark_Aperture',p(592,255),p(593,255),4.65,'Recess','05',20,.02)

# Compact fire-control optic: sight tube plus a visibly separate armored side pod.
for x in (211,278):
    box('Optic_Rail_Clamp_'+str(x),(x,222,0),(17,6.4,23),'Black','06',.5)
    poly('Optic_Mount_'+str(x),[(x-6,222),(x-7,210),(x+7,210),(x+6,222)],7,-7,'Steel','06',.4)
rod('Optic_Main_Tube',p(196,203),p(307,203),8.0,'Black','06',24,.5)
rod('Optic_Ocular_Collar',p(189,203),p(213,203),10.2,'Rubber','06',20,.45)
ring('Optic_Objective_Housing',p(292,203),p(318,203),12.7,9.4,'Black','06',12,.5)
rod('Optic_Objective_Glass',p(313,203),p(313.5,203),9.3,'Glass','06',24,.08)
ring('Optic_Objective_Rim',p(317,203),p(319,203),12.8,10,'Steel','06',12,.15)
poly('Fire_Control_Armored_Pod',[(230,189),(274,189),(284,197),(281,213),(232,214),(225,205)],21,7,'Black','06',.9)
poly('Fire_Control_Sand_Side_Plate',[(233,191),(270,191),(278,198),(277,209),(234,210),(230,204)],22.3,19.8,'Desert','06',.5)
poly('Fire_Control_Recessed_Window',[(241,195),(265,195),(270,199),(268,205),(240,205)],22.8,21.8,'Recess','06',.25)
box('Fire_Control_Status_Window',(249,200,23.1),(12,4,.4),'Glass','06',.15)
rod('Optic_Upper_Dial',p(254,193),p(254,183),5.2,'Black','06',12,.3)
for x in (234,273):bolt('Optic_Pod_Fastener_'+str(x),x,207,23,1.05,'06')

# Save the actual editable asset before rendering its first review images.
im=bpy.data.images.load(str(P/'references/M7_reference.png'));im.pack()
sc['reference_image']=im.name;sc['approval_checkpoint']='Stage 01: overall silhouette, colour split and sight size. Await user before detail pass.'
sc.render.engine='CYCLES';sc.cycles.samples=24;sc.cycles.use_denoising=True
try:
    pref=bpy.context.preferences.addons['cycles'].preferences;pref.compute_device_type='OPTIX';pref.get_devices()
    for d in pref.devices:d.use=d.type=='OPTIX'
    sc.cycles.device='GPU'
except Exception:pass
w=bpy.data.worlds.new('Neutral_Studio');w.use_nodes=True;sc.world=w
w.node_tree.nodes['Background'].inputs['Color'].default_value=(.16,.18,.21,1);w.node_tree.nodes['Background'].inputs['Strength'].default_value=.35
sc.view_settings.view_transform='AgX';sc.view_settings.look='AgX - Medium High Contrast';sc.view_settings.exposure=.2
sc.render.image_settings.file_format='PNG';sc.render.image_settings.color_mode='RGBA';sc.render.film_transparent=True
def light(name,loc,power,size,col):
    d=bpy.data.lights.new(name,'AREA');d.energy=power;d.shape='RECTANGLE';d.size=size;d.size_y=size*.5;d.color=col
    o=bpy.data.objects.new(name,d);cols['07'].objects.link(o);o.location=loc;o.rotation_euler=(Vector((0,0,0))-o.location).to_track_quat('-Z','Y').to_euler()
light('Warm_Key',(-.7,-1.3,1.8),190,2.2,(1,.92,.8))
light('Cool_Fill',(1.4,-1.2,.6),100,1.7,(.84,.92,1))
light('Rim',(.4,1.1,1.6),220,2,(.89,.94,1))
def camera(name,loc,target,width):
    d=bpy.data.cameras.new(name);d.type='ORTHO';d.ortho_scale=width
    o=bpy.data.objects.new(name,d);cols['07'].objects.link(o);o.location=loc;o.rotation_euler=(Vector(target)-o.location).to_track_quat('-Z','Y').to_euler();return o
camera('SIDE',(0,-5,0),(0,0,0),2.02)
camera('THREE_QUARTER',(1.7,-4.6,1.7),(0,0,0),2.02)
camera('REAR_SIDE',(0,5,0),(0,0,0),2.02)
camera('TOP',(0,0,5),(0,0,0),2.02)
camera('FRONT',(5,0,0),(0,0,0),.65)
sc.camera=bpy.data.objects['THREE_QUARTER'];sc.render.resolution_x=1900;sc.render.resolution_y=850;sc.render.resolution_percentage=100
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            area.spaces.active.shading.type='MATERIAL';area.spaces.active.region_3d.view_location=Vector((0,0,0));area.spaces.active.region_3d.view_distance=2.4
            area.spaces.active.region_3d.view_rotation=sc.camera.rotation_euler.to_quaternion();area.spaces.active.overlay.show_overlays=False
bpy.ops.object.select_all(action='DESELECT');root.select_set(True);bpy.context.view_layer.objects.active=root
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'AI_INFANTRY_M7_STAGE01.blend'))
(OUT/'stage01_manifest.json').write_text(json.dumps({'stage':1,'status':'awaiting user direction approval','parts':len(PARTS),'materials':list(M),'check_focus':['M7 silhouette','desert and black colour split','fire-control optic proportions'],'not_yet_done':['surface detail pass','final grip fit to AI infantry','game exports']},indent=2),encoding='utf-8')
for name in ('SIDE','THREE_QUARTER'):
    sc.camera=bpy.data.objects[name];sc.render.filepath=str(OUT/'renders'/(name+'.png'));bpy.ops.render.render(write_still=True)
    print('STAGE01_RENDER_DONE',name,flush=True)
print('STAGE01_SAVED',str(OUT/'AI_INFANTRY_M7_STAGE01.blend'),flush=True)
