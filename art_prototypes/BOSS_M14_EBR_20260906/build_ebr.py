"""External visual game prop traced from the user's M14 EBR photo.
Pixel coordinates are artwork landmarks, never fabrication dimensions.
The 44.3-inch standard M14 is used only as the requested scale reference.
"""
import bpy, bmesh, math, pathlib, json, hashlib, random
from mathutils import Vector, Matrix
D=pathlib.Path(__file__).resolve().parent
REF=D/'references/BOSS_M14_EBR_REFERENCE.png'
bpy.ops.wm.read_factory_settings(use_empty=True)
sc=bpy.context.scene;sc.unit_settings.system='METRIC';sc.unit_settings.length_unit='METERS'
bpy.context.preferences.filepaths.temporary_directory='D:/Tools/BlenderUserData/Temp'
LENGTH=44.3*.0254
S=LENGTH/622
COL={};M={};PARTS=[];GROUPS={}
for key,name in [('01','Receiver and chassis'),('02','Formed handguard and rails'),('03','Barrel exterior and muzzle'),('04','Iron sights'),('05','Magazine'),('06','Telescoping stock'),('07','Grip and trigger guard'),('09','Scale and attachment points'),('10','Studio')]:
    c=bpy.data.collections.new('M14 EBR | '+name);sc.collection.children.link(c);COL[key]=c
root=bpy.data.objects.new('BOSS_M14_EBR_ROOT',None);COL['09'].objects.link(root);root.empty_display_size=.06
root['reference_scale_length_m']=LENGTH
root['length_basis']='44.3 in standard M14 visual scale datum; NOT a claim of exact EBR variant OAL.'
root['scale_method']='Uniform XYZ only. Native origin is center of primary grip.'
root['purpose']='External appearance mesh for a virtual game. No functional internals.'
root['axes']='Muzzle +X; reference side -Y; up +Z.'
def p(x,z,d=0):return Vector(((x-197)*S,-d*S,(254-z)*S))
def group(key,name,origin):
    o=bpy.data.objects.new('EBR_'+name,None);COL[key].objects.link(o);o.parent=root;o.location=p(*origin);o.empty_display_size=.025;GROUPS[key]=o;return o
group('01','RECEIVER',(279,199,0));group('02','HANDGUARD',(415,193,0));group('03','BARREL_EXTERIOR',(538,185,0))
group('04','IRON_SIGHTS',(253,175,0));group('05','MAGAZINE',(316,219,0));group('06','STOCK',(175,211,0));group('07','GRIP',(197,254,0))
def material(key,color,metal,rough,noise=0):
    m=bpy.data.materials.new('EBR_'+key);m.use_nodes=True
    srgb=[int(color[i:i+2],16)/255 for i in (0,2,4)];rgb=[c/12.92 if c<=.04045 else ((c+.055)/1.055)**2.4 for c in srgb]
    m.diffuse_color=(*rgb,1);m.metallic=metal;m.roughness=rough;bs=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
    bs.inputs['Base Color'].default_value=(*rgb,1);bs.inputs['Metallic'].default_value=metal;bs.inputs['Roughness'].default_value=rough
    if noise:
        nt=m.node_tree;tc=nt.nodes.new('ShaderNodeTexCoord');tc.object=root;n=nt.nodes.new('ShaderNodeTexNoise');n.inputs['Scale'].default_value=noise;n.inputs['Detail'].default_value=2;nt.links.new(tc.outputs['Object'],n.inputs['Vector'])
        ramp=nt.nodes.new('ShaderNodeValToRGB');ramp.color_ramp.elements[0].color=(*[v*.85 for v in rgb],1);ramp.color_ramp.elements[1].color=(*[v*1.12 for v in rgb],1)
        nt.links.new(n.outputs['Fac'],ramp.inputs[0]);nt.links.new(ramp.outputs[0],bs.inputs['Base Color'])
        bump=nt.nodes.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.18;bump.inputs['Distance'].default_value=.000055 if metal<.1 else .000015
        nt.links.new(n.outputs['Fac'],bump.inputs['Height']);nt.links.new(bump.outputs[0],bs.inputs['Normal'])
        rr=nt.nodes.new('ShaderNodeMapRange');rr.inputs['From Min'].default_value=0;rr.inputs['From Max'].default_value=1;rr.inputs['To Min'].default_value=rough-.08;rr.inputs['To Max'].default_value=rough+.06
        nt.links.new(n.outputs['Fac'],rr.inputs[0]);nt.links.new(rr.outputs[0],bs.inputs['Roughness'])
        # Low-contrast finish variation sits below the fine surface grain.
        broad=nt.nodes.new('ShaderNodeTexNoise');broad.inputs['Scale'].default_value=58 if metal>.1 else 93;broad.inputs['Detail'].default_value=3;nt.links.new(tc.outputs['Object'],broad.inputs['Vector'])
        br=nt.nodes.new('ShaderNodeValToRGB');br.color_ramp.elements[0].color=(.64,.64,.64,1);br.color_ramp.elements[1].color=(1,1,1,1)
        nt.links.new(broad.outputs['Fac'],br.inputs[0]);mix=nt.nodes.new('ShaderNodeMixRGB');mix.blend_type='MULTIPLY';mix.inputs[0].default_value=.55
        nt.links.new(ramp.outputs[0],mix.inputs[1]);nt.links.new(br.outputs[0],mix.inputs[2]);nt.links.new(mix.outputs[0],bs.inputs['Base Color'])
    M[key]=m;return m
material('Gunmetal','595959',.72,.43,1200)
material('Steel','747474',.82,.34,1250)
material('DarkSteel','2F2F2F',.70,.44,1350)
material('Chassis','444444',.64,.46,1050)
material('Kydex','404040',.02,.56,1750)
material('Polymer','1D1D1D',.01,.67,2900)
material('GripRubber','202020',.01,.67,2900)
material('Recess','0F120F',.18,.63)
material('Lettering','9C9E95',.15,.53)
exec((D/'geometry_helpers.py').read_text(encoding='utf-8-sig'),globals())

def screw(name,x,z,d,r=1,key='01',both=True):
    for sign in ((1,-1) if both else (1,)):
        ring(name+'_seat_'+str(sign),p(x,z,sign*d),p(x,z,sign*(d+.16)),r*1.16,r*.82,'Recess',key,24,.035)
        rod(name+'_head_'+str(sign),p(x,z,sign*(d+.1)),p(x,z,sign*(d+.48)),r*.91,'Steel',key,24,bevel=.1)
        trim(name+'_slot_'+str(sign),[(x-r*.63,z-.12),(x+r*.63,z+.12)],sign*(d+.50),.27,'Recess',key,False)

def loft(name,sections,mat,key,bevel=.1,smooth=True):
    # Cross sections along X: list of (x, [(imageY, depth), ...]).
    n=len(sections[0][1]);vs=[p(x,z,d) for x,cross in sections for z,d in cross]
    fs=[tuple(range(n-1,-1,-1)),tuple((len(sections)-1)*n+i for i in range(n))]
    for j in range(len(sections)-1):
        fs.extend((j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i) for i in range(n))
    return mesh(name,vs,fs,mat,key,bevel,smooth)

def xy_rounded(x0,y0,x1,y1,r=1.5,n=5):
    pts=[]
    for cx,cy,a in [(x1-r,y0+r,-90),(x1-r,y1-r,0),(x0+r,y1-r,90),(x0+r,y0+r,180)]:
        for i in range(n+1):
            th=math.radians(a+i*90/n);pts.append((cx+r*math.cos(th),cy+r*math.sin(th)))
    return pts

# Skeleton sliding butt stock. The clear air gap under the cheek pad is deliberate.
padpts=[(52,194),(61,194),(65,198),(65,273),(62,279),(58,281),(50,279),(47,273),(46,211),(47,202)]
pad=poly('Recoil_pad',padpts,14,-14,'Polymer','06',2.5);pad.modifiers.get('Machined edge').segments=4
poly('Butt_backing_plate',[(61,197),(66,198),(68,274),(64,277),(61,273)],11,-11,'DarkSteel','06',.65)
for i in range(15):
    z=206+i*4.25
    rod('Butt_contact_rib_'+str(i),p(46.8,z,-10),p(46.8,z,10),.48,'Polymer','06',12,bevel=.1)
for z in (204,267):screw('Butt_backing_fastener_'+str(z),64,z,11.3,1.1,'06')
for sign in (-1,1):
    rod('Stock_slide_rod_'+str(sign),p(64,211,sign*8),p(205,211,sign*8),2.25,'Steel','06',32,bevel=.15)
    rod('Stock_slide_sleeve_'+str(sign),p(64,211,sign*8),p(99,211,sign*8),3.15,'Gunmetal','06',32,bevel=.18)
    rod('Stock_lock_sleeve_'+str(sign),p(166,211,sign*8),p(205,211,sign*8),3.3,'DarkSteel','06',32,bevel=.18)
    for x in (74,92,174,192):
        ring('Stock_sleeve_ring_'+str(sign)+'_'+str(x),p(x,211,sign*8),p(x+1.2,211,sign*8),3.4,2.95,'Gunmetal','06',32,.08)
    # Perforated diagonal cheek support; holes cut through the actual mesh.
    ob=poly('Stock_support_web_'+str(sign),[(62.5,216),(94,216),(70,264),(63.5,268),(62.5,256)],sign*8.8,sign*7.1,'DarkSteel','06',.25)
    holes=[[(68,221),(84.5,221),(69,253)]]
    for i in range(8):
        z=222+i*5;x=94-(z-216)*.5-2.8
        holes.append([(x+.88*math.cos(j*math.tau/16),z+.88*math.sin(j*math.tau/16)) for j in range(16)])
    cut(ob,holes,'Stock lightening openings')
    rod('Cheek_riser_post_'+str(sign),p(80,203,sign*7),p(80,185,sign*7),2.2,'Gunmetal','06',24)
    bracket=poly('Cheek_height_bracket_'+str(sign),[(69,187),(84,187),(86,192),(82,202),(69,202)],sign*10,sign*7,'DarkSteel','06',.35)
    cut(bracket,[xy_rounded(x-1,190,x+1,198.5,.9,5) for x in (73,80)],'Cheek height slots')
    screw('Cheek_height_lock_'+str(sign),77,201,sign*10.1,1.2,'06',False)
    for x,z in [(71,188.4),(82,188.4)]:screw('Cheek_bracket_'+str(sign)+'_'+str(x),x,z,sign*10,.65,'06',False)
cheek=[]
for x,z,w in [(68,180.3,10),(71,178.5,12.5),(155,178.5,12.5),(165,179,11),(168,181.5,8)]:
    cheek.append((x,[(z,-w*.8),(z-.4,0),(z,w*.8),(z+1.3,w),(z+7,w*.85),(z+8,0),(z+7,-w*.85),(z+1.3,-w)]))
loft('Adjustable_cheek_pad',cheek,'Polymer','06',.65)
poly('Stock_chassis_socket',[(178,196),(204,196),(214,203),(218,218),(201,225),(175,219),(172,209)],13,-13,'Chassis','06',.65)
for z in (201,206,216):
    trim('Stock_socket_step_'+str(z),[(177,z),(207,z)],13.15,.6,'DarkSteel','06')
for x in (178,185,193,201):screw('Socket_pin_'+str(x),x,214,13.2,.78,'06')
box('Stock_lock_latch',180,219,15,3.2,19,'DarkSteel','06',bevel=.5)
for i in range(7):box('Stock_socket_top_rail_'+str(i),178+i*4.65,196.5,2.9,3.5,19,'DarkSteel','06',bevel=.3)
for x in (181,193):
    for sign in (-1,1):
        ring('Stock_socket_lock_ring_'+str(sign)+'_'+str(x),p(x,210,sign*13.1),p(x,210,sign*13.8),2.5,1.75,'DarkSteel','06',28,.1)
        rod('Stock_socket_lock_pin_'+str(sign)+'_'+str(x),p(x,210,sign*13.5),p(x,210,sign*13.9),1.75,'Steel','06',28,bevel=.2)

# Curved ergonomic pistol grip with an inset stippled field.
grippts=[(182,219),(204,218),(213,223),(221,229),(220,235),(215,239),(210,251),(205,265),(198,281),(202,286),(200,290),(188,291),(181,289),(170,290),(164,287),(165,280),(170,268),(178,250),(187,237),(187,232),(181,228)]
g=poly('Ergonomic_pistol_grip',grippts,9,-9,'Polymer','07',1.65)
g.modifiers.get('Machined edge').segments=4
inset=[(192,236),(207,237),(206,248),(200,263),(193,279),(185,285),(173,284),(178,269),(186,249)]
panel('Grip_stipple_field',inset,9.15,.7,'Polymer','07',bevel=1.4)
for j in range(4):
    # Small transverse finger scallop lips, integrated into the front grip curve.
    x=213-j*4.4;z=244+j*11
    rod('Grip_finger_swell_'+str(j),p(x,z,-6),p(x,z,6),1.25,'Polymer','07',24,bevel=.1)
box('Grip_bottom_plug',183,287.7,22,2,14,'DarkSteel','07',bevel=.8)

# Stamped removable magazine; subtle taper and rolled seams are visible in the source.
magsections=[]
for z,x0,x1,w in [(216,292,338,10.2),(221,293,339,10.7),(264,295,341,11),(274,295,340,10.6)]:
    # Use a custom vertical loft below for non-boxy stamped faces.
    magsections.append((z,x0,x1,w))
vs=[]
for z,x0,x1,w in magsections:
    cross=[(x0+1,-w),(x1-2,-w),(x1,-w+1.8),(x1,w-1.8),(x1-2,w),(x0+1,w),(x0,w-1),(x0,-w+1)]
    vs.extend(p(x,z-max(0,z-221)/53*4.7*(x-295)/46,d) for x,d in cross)
fs=[tuple(range(7,-1,-1)),tuple(range(24,32))]
for j in range(3):fs.extend((j*8+i,j*8+(i+1)%8,(j+1)*8+(i+1)%8,(j+1)*8+i) for i in range(8))
mesh('Magazine_stamped_body',vs,fs,'Gunmetal','05',.4,True)
for sign in (-1,1):
    trim('Magazine_rear_fold_'+str(sign),[(297,222),(299,270)],sign*10.9,.55,'DarkSteel','05',False)
    trim('Magazine_front_fold_'+str(sign),[(332,220),(335,270)],sign*10.9,.55,'Steel','05',False)
    trim('Magazine_shallow_spine_'+str(sign),[(318,220),(320,271)],sign*11.05,.75,'Gunmetal','05',False)
poly('Magazine_floorplate',[(294,273),(341,268.2),(341.3,270),(294.5,275)],11.5,-11.5,'DarkSteel','05',.3)
box('Magazine_upper_collar',315,218.5,48,2.8,23,'DarkSteel','05',bevel=.4)

# Chassis and receiver exterior, with left/right-specific exposed controls.
poly('Lower_chassis_spine',[(212,202),(231,199),(347,194),(361,197),(359,218),(342,220),(339,225),(289,226),(282,222),(225,224),(215,219)],13,-13,'Chassis','01',.55)
recv=poly('Receiver_left_wall',[(211,188),(219,182),(240,179),(250,176),(284,177),(316,175),(342,177),(356,182),(356,194),(345,199),(327,204),(320,214),(226,215),(217,207)],13.6,-8,'Gunmetal','01',1.0);recv.modifiers.get('Machined edge').segments=3
poly('Receiver_top_rear_ridge',[(216,185),(240,177),(263,176),(283,178),(284,181),(233,184),(218,190)],8,-10,'Steel','01',.35)
poly('Receiver_top_crown',[(268,178),(315,174),(337,174),(346,177),(341,182),(290,184),(273,183)],9,-12,'Gunmetal','01',.45)
box('Ejection_recess_exterior',310,183,39,10,2,'Recess','01',d=-13,bevel=.6)
poly('Bolt_visible_surface',[(294,177.5),(324,177),(330,181),(328,188),(307,188),(296,184)],-13.2,-14.7,'Steel','01',.4)
poly('Right_receiver_rail',[(219,191),(244,187),(323,190),(344,190),(343,201),(317,205),(232,204),(217,199)],-12,-15,'DarkSteel','01',.5)
rod('Charging_handle_stem',p(298,190,-15),p(299,190,-25),2.2,'Gunmetal','01',24)
poly('Charging_handle_paddle',[(297,185),(302,184),(308,187),(306,193),(301,195),(297,192)],-23,-29,'Gunmetal','01',.75)
poly('Receiver_side_plate',[(222,192),(302,192),(306,194),(306,207),(304,209),(226,209),(222,207)],13.95,13.35,'Chassis','01',.55)
trim('Receiver_upper_seam',[(220,187),(306,187)],13.8,.6,'DarkSteel','01',False)
trim('Receiver_secondary_step',[(229,190),(301,190)],14.0,.7,'Gunmetal','01',False)
trim('Receiver_side_milling',[(232,194),(298,194)],14.1,.55,'DarkSteel','01',False)
for x in (233,243):
    panel('Receiver_service_lug_'+str(x),[(x,196),(x+4,196),(x+4,201),(x+2,204),(x,204)],14.3,.5,'DarkSteel','01',False,.45)
for x in (237,253,272,287,305):screw('Receiver_visible_pin_'+str(x),x,211,14.6,.85,'01',False)
for i,x in enumerate((297,304,311,318)):
    for sign in (-1,1):
        ring('Chassis_lower_recess_'+str(sign)+'_'+str(i),p(x,211,sign*14.1),p(x,211,sign*14.45),1.8,1.45,'Gunmetal','01',28,.08)
        rod('Chassis_recess_shadow_'+str(sign)+'_'+str(i),p(x,211,sign*14.2),p(x,211,sign*14.35),1.45,'Recess','01',28,bevel=0)
for x,z in [(222,194),(287,180),(340,198),(316,205),(336,211)]:screw('Receiver_external_fastener_'+str(x),x,z,14.3,1.1,'01')
ring('Receiver_side_selector_boss',p(326,210,14),p(326,210,17.2),5.8,2.6,'DarkSteel','01',40,.2)
rod('Receiver_selector_cap',p(326,210,17),p(326,210,17.8),2.5,'Gunmetal','01',32,bevel=.2)
poly('Selector_lever',[(326,208),(335,207),(337,210),(329,214),(326,214)],18,16.8,'Gunmetal','01',.4)
rod('Operating_rod_exterior',p(212,211,17),p(273,211,17),2.4,'Steel','01',32)
box('Operating_rod_end',274,211,6,6,5,'Gunmetal','01',d=17,bevel=.65)
for x in (218,248,267):ring('Operating_rod_band_'+str(x),p(x,211,17),p(x+1.1,211,17),2.65,2.2,'DarkSteel','01',24,.08)
poly('Magwell_left_lip',[(279,218),(343,216),(343,224),(292,226),(290,224),(279,224)],14.7,12.2,'Gunmetal','01',.4)
poly('Magwell_right_lip',[(279,218),(343,216),(343,224),(292,226),(290,224),(279,224)],-14.7,-12.2,'Gunmetal','01',.4)
poly('Magazine_release_paddle',[(285,225),(291,225),(290,237),(287,238),(287,231)],5,-5,'DarkSteel','01',.3)

# Open trigger guard and a separate curved trigger. These are visual surfaces only.
def cubic_xy(a,b,c,d,n=20):
    return [tuple((1-t)**3*a[k]+3*(1-t)**2*t*b[k]+3*(1-t)*t*t*c[k]+t**3*d[k] for k in (0,1)) for t in (i/n for i in range(n+1))]
outer=[(224.2,219.5),(266.7,219.5)]
outer+=cubic_xy((266.7,219.5),(267,240.4),(257.2,244.8),(244,245))[1:]
outer+=cubic_xy((244,245),(224,245.6),(222.7,232),(224.2,219.5))[1:-1]
guard=poly('Open_trigger_guard',outer,3.4,-3.4,'Gunmetal','07',.45)
inner=[(226.2,221.7),(264.6,221.7)]
inner+=cubic_xy((264.6,221.7),(264.2,238.2),(256.5,242.8),(244.2,243))[1:]
inner+=cubic_xy((244.2,243),(226.3,243.1),(225.2,232.1),(226.2,221.7))[1:-1]
cut(guard,[inner],'Trigger guard opening')
poly('Trigger_curve',[(247,221),(250,221),(249,224),(245.5,226.5),(243.5,230),(242.8,234),(244,238),(242.5,238.5),(240.5,235),(240,231),(241.2,227),(243,224.5)],1.5,-1.5,'Steel','07',.45)
poly('Trigger_guard_front_tab',[(262,220),(267,221),(265,230),(263,231)],7,-7,'DarkSteel','07',.3)
poly('Safety_visible_tab',[(230,214),(238,214),(237,224),(234,228),(230,228)],4,-4,'DarkSteel','07',.35)

# Long upper handguard: a shallow, formed shell with compound rounded lower cheeks.
box('Handguard_top_structure',443,178.5,183,7,24,'DarkSteel','02',bevel=.5)
box('Handguard_lower_structure',444,211.7,186,5.5,21,'DarkSteel','02',bevel=.5)
rod('Hidden_barrel_visual_core',p(347,185),p(540,185),4.7,'DarkSteel','02',40)
for sign in (-1,1):
    # Forward cage reveals real gaps beneath and above the short side rail.
    cage=poly('Forward_open_cage_'+str(sign),[(469,183),(530,181),(537,185),(537,211),(532,214),(470,214)],sign*14.2,sign*11.5,'DarkSteel','02',.4)
    holes=[]
    for x in (479,491,503,515,527):
        holes.append(xy_rounded(x-3,185,x+4.8,191,1.1,3));holes.append(xy_rounded(x-3,203,x+4.8,209,1.2,3))
    cut(cage,holes,'Handguard open ventilation')
    box('Forward_accessory_rail_base_'+str(sign),506,196.5,59,6,3.0,'Chassis','02',d=sign*16,bevel=.3)
    for i in range(12):
        x=477.5+i*5.15
        poly('Forward_side_rail_tooth_'+str(sign)+'_'+str(i),[(x,192.9),(x+3.2,192.9),(x+3.5,194.3),(x+3.5,199.6),(x,199.6)],sign*19.5,sign*16,'Gunmetal','02',.22)
    for x in (479,531):screw('Forward_rail_fastener_'+str(sign)+'_'+str(x),x,196,sign*20.1,1.1,'02',False)

# Center formed Kydex cover: left and right are connected by the underside.
# Radius changes gradually along its length; upper shell edge follows the photo.
crosses=[]
for x,top,bottom,w in [(355,185,215,12.5),(360,181,218,16),(371,178.9,219,18.2),(424,177.8,219.7,19),(453,177.5,219.5,18.6),(466,181,219,18),(476,187,218,17),(479,192,216,14.5)]:
    h=bottom-top
    c=[(top,-w*.52),(top,w*.52),(top+h*.12,w*.88),(top+h*.32,w),(top+h*.67,w),
       (bottom-2.3,w*.89),(bottom,w*.62),(bottom+.3,0),(bottom,-w*.62),(bottom-2.3,-w*.89),(top+h*.67,-w),(top+h*.32,-w),(top+h*.12,-w*.88)]
    crosses.append((x,c))
cover=loft('Formed_Kydex_handguard',crosses,'Kydex','02',.35,True)
for sign in (-1,1):
    # Actual small recessed screw seats, avoiding large decorative sci-fi panels.
    for j,(x,z) in enumerate([(367,191),(384,193),(416,185),(439,181.5),(452,184),(459,191),(468,205),(416,201),(439,202)]):
        dd=18.5 if z>=191 else (18.1 if z>=184 else 15.8)
        rod('Kydex_recess_'+str(sign)+'_'+str(j),p(x,z,sign*dd),p(x,z,sign*(dd+.08)),.9 if j<4 else .73,'Recess','02',20,bevel=0)
    for x,z,d in [(367,191,18.2),(468,204,18.3)]:screw('Kydex_attachment_'+str(sign)+'_'+str(x),x,z,sign*d,.75,'02',False)
    trim('Kydex_upper_mold_edge_'+str(sign),[(373,181),(419,180),(450,180.2)],sign*12.8,.45,'Kydex','02',False)

# Top rail teeth have the characteristic stepped cross-section and genuine gaps.
box('Top_rail_longitudinal_spine',444,173.4,184,4.4,11.5,'DarkSteel','02',bevel=.25)
for i in range(35):
    x=352+i*5.28
    c=[(174.4,-6),(171.7,-9),(169.8,-8.5),(169.8,8.5),(171.7,9),(174.4,6)]
    loft('Top_rail_tooth_'+str(i),[(x,c),(x+3.4,c)],'Gunmetal','02',.2,False)
for i in range(31):
    x=370+i*5.32
    box('Lower_rail_tooth_'+str(i),x,215.8,3.3,3.9,16,'DarkSteel','02',bevel=.22)
for x in (360,529):
    rod('Top_rail_screw_'+str(x),p(x,169.5),p(x,169),1.25,'DarkSteel','02',24,bevel=.1)
poly('Handguard_rear_transition',[(339,183),(355,181),(358,187),(357,214),(343,217),(337,212)],14.5,-14.5,'Chassis','02',.55)
for z in (188,209):
    for sign in (-1,1):
        box('Transition_slot_'+str(sign)+'_'+str(z),346,z,5,2,1,'Recess','02',d=sign*14.8,bevel=.45)
for x,z in [(348,196),(534,204)]:screw('Chassis_guard_lock_'+str(x),x,z,15,1.2,'02')

# Exposed muzzle-side exterior. Front sight is separate from the long slotted tip.
rod('Exposed_barrel',p(537,185),p(624,185),4.1,'Gunmetal','03',48,bevel=.2)
for x,le,r in [(538,5,5.3),(560,5,5.1),(592,8,5),(615,6,4.7)]:
    rod('Barrel_external_step_'+str(x),p(x,185),p(x+le,185),r,'DarkSteel','03',40,bevel=.22)
rod('Gas_cylinder_exterior',p(537,200),p(614,200),3.45,'DarkSteel','03',40,bevel=.2)
rod('Gas_cylinder_end_cap',p(610,200),p(617,200),3.8,'Gunmetal','03',32,bevel=.2)
poly('Barrel_lower_band',[(565,187),(574,187),(575,198),(571,203),(565,202)],5.5,-5.5,'Gunmetal','03',.55)
poly('Front_sight_block',[(589,181),(604,181),(606,199),(601,202),(588,202)],7.2,-7.2,'DarkSteel','04',.55)
for sign in (-1,1):
    poly('Front_sight_protective_ear_'+str(sign),[(593,181),(593,165),(595,161),(598,161),(600,164),(600,181)],sign*6.6,sign*4.7,'Gunmetal','04',.45)
rod('Front_sight_post',p(596,182),p(596,166.5),.85,'DarkSteel','04',16,bevel=.12)
box('Front_sight_base',596,180,15,4,15.5,'Gunmetal','04',bevel=.4)
screw('Front_band_pin',597,187,7.4,1.05,'04')
rod('Muzzle_external_collar',p(619,185),p(627,185),5.0,'Gunmetal','03',48,bevel=.2)
muzzle_body=ring('Muzzle_tip_body',p(625,185),p(667.7,185),5.8,3.6,'Gunmetal','03',48,.14)
# Shallow blind cap makes this only an exterior display cavity, not a functional bore.
rod('Muzzle_dark_blind_cap',p(654,185),p(655,185),3.6,'Recess','03',40,bevel=0)
for i in range(5):
    a=i*math.tau/5;radial=Vector((0,-math.cos(a),-math.sin(a)));tangent=Vector((0,math.sin(a),-math.cos(a)))
    vs=[p(x,185)+radial*rr*S+tangent*ww*S for x in (632,665) for rr in (2.6,7) for ww in (-.82,.82)]
    cutter=mesh('TEMP_muzzle_slot_'+str(i),vs,[(0,1,3,2),(4,6,7,5),(0,4,5,1),(2,3,7,6),(0,2,6,4),(1,5,7,3)],'Recess','03',0)
    bpy.context.view_layer.update();bpy.context.view_layer.objects.active=muzzle_body
    mod=muzzle_body.modifiers.new('Muzzle exterior slots','BOOLEAN');mod.operation='DIFFERENCE';mod.solver='EXACT';mod.object=cutter
    while muzzle_body.modifiers.find(mod.name)>0:bpy.ops.object.modifier_move_up(modifier=mod.name)
    bpy.ops.object.modifier_apply(modifier=mod.name);PARTS.remove(cutter);bpy.data.objects.remove(cutter,do_unlink=True)
ring('Muzzle_end_rim',p(665.9,185),p(668,185),5.95,3.6,'DarkSteel','03',48,.12)

# Rear aperture sight, adjustment drums and tiny exterior witness marks.
poly('Rear_sight_base',[(236,178),(238,174),(267,174),(273,178),(270,183),(236,184)],8.5,-8.5,'DarkSteel','04',.4)
poly('Rear_aperture_upright',[(254,178),(254,172),(256,169),(259,169),(261,172),(261,178)],2,-2,'Gunmetal','04',.3)
ring('Rear_aperture_ring',p(256,170),p(258,170),2.8,1.15,'Gunmetal','04',40,.12)
for sign in (-1,1):
    rod('Rear_adjustment_drum_'+str(sign),p(253,170,sign*6.6),p(253,170,sign*11.5),6.9,'Gunmetal','04',48,bevel=.25)
    ring('Rear_adjustment_ring_'+str(sign),p(253,170,sign*11.2),p(253,170,sign*12.1),6.15,3.5,'Gunmetal','04',48,.16)
    for i in range(24):
        th=i*math.tau/24;x=253+6.8*math.cos(th);z=170+6.8*math.sin(th)
        rod('Rear_dial_knurl_'+str(sign)+'_'+str(i),p(x,z,sign*7.3),p(x,z,sign*11.4),.21,'Steel','04',8,bevel=0)
    screw('Rear_sight_dial_center_'+str(sign),253,170,sign*12.1,1.3,'04',False)
poly('Rear_receiver_sight_lug',[(268,177),(273,174),(281,174),(282,171),(290,171),(291,173),(286,177),(277,179)],6,-6,'Gunmetal','04',.35)
box('Rear_sight_lug_recess',285,171.5,4,1,5,'Recess','04',bevel=.2)
poly('Rear_receiver_hook',[(217,197),(211,188),(212,182),(215,182),(220,191),(225,195)],6,-6,'Gunmetal','01',.4)

# Stipple is geometry, grouped into two meshes rather than thousands of objects.
def inside(x,z,pts):
    flag=False;j=len(pts)-1
    for i,(xi,yi) in enumerate(pts):
        xj,yj=pts[j]
        if ((yi>z)!=(yj>z)) and x<(xj-xi)*(z-yi)/(yj-yi)+xi:flag=not flag
        j=i
    return flag
rng=random.Random(1406)
for sign in (-1,1):
    vs=[];fs=[]
    for row in range(64):
        z=237+row*.73
        for col in range(46):
            x=173+col*.78+(row%2)*.39
            if not inside(x,z,inset):continue
            xx=x+rng.uniform(-.13,.13);zz=z+rng.uniform(-.13,.13);rad=rng.uniform(.13,.24);base=len(vs)
            vs.extend([p(xx-rad,zz,sign*9.22),p(xx,zz-rad,sign*9.22),p(xx+rad,zz,sign*9.22),p(xx,zz+rad,sign*9.22),p(xx,zz,sign*(9.31+rad*.25))])
            fs.extend([(base+i,base+(i+1)%4,base+4) for i in range(4)])
    mesh('Grip_fine_stipple_'+str(sign),vs,fs,'Polymer','07',0)

for obj in PARTS:
    if obj.parent==GROUPS['07'] and obj.type=='MESH':
        for i,mat in enumerate(obj.data.materials):
            if mat==M['Polymer']:obj.data.materials[i]=M['GripRubber']

# Restrained etched identifiers and wear; no invented large logos or attachments.
label('Receiver_small_identifier','M14',272,186,14,1.6,'Lettering','01',False)
label('Chassis_small_identifier','EBR',346,203,15.1,1.4,'Lettering','02',False)
for i in range(44):
    zone=i%3
    if zone==0:x=rng.uniform(228,305);z=214.8;d=14.6;key='01'
    elif zone==1:x=rng.uniform(300,334);z=273;d=11.65;key='05'
    else:x=rng.uniform(356,529);z=171;d=9.05;key='02'
    le=rng.uniform(.35,1.3)
    panel('Fine_edge_scuff_'+str(i),[(x,z),(x+le,z),(x+le*.6,z+.16)],d,.025,'Steel',key,True,0)

print('EBR_GEOMETRY_BUILT',len(PARTS),flush=True)

for name,xyz,descr in [('SCK_PRIMARY_GRIP',(197,254,0),'Primary hand grip center'),('SCK_SUPPORT_GRIP',(416,209,0),'Support palm at underside of handguard'),('SCK_MUZZLE',(668,185,0),'Visual game effect origin; forward +X'),('SCK_STOCK_CONTACT',(49,226,0),'Stock contact surface'),('SCK_MAGAZINE',(316,220,0),'Magazine attachment')]:
    ob=bpy.data.objects.new(name,None);COL['09'].objects.link(ob);ob.parent=root;ob.location=p(*xyz);ob.empty_display_type='ARROWS';ob.empty_display_size=.028;ob['purpose']=descr
img=bpy.data.images.load(str(REF));img.pack()
refobj=bpy.data.objects.new('REFERENCE | exact user image',None);COL['09'].objects.link(refobj);refobj.empty_display_type='IMAGE';refobj.data=img;refobj.empty_display_size=LENGTH;refobj.hide_viewport=True;refobj.hide_render=True
sc['reference_sha256']=hashlib.sha256(REF.read_bytes()).hexdigest()
sc['asset_version']='BOSS M14 EBR / reference-replica v1'
sc['reference_fidelity']='Visible silhouette traced from supplied 720x480 image. Unseen widths and reverse surfaces inferred; not a scan.'
sc['length_source']='US Army FM 23-8: M14 44.3 inches. Scaling datum only; EBR stock settings vary.'
sc['length_source_url']='https://rdl.train.army.mil/catalog-ws/view/100.ATSC/710C9E3A-308E-49A1-A22E-CECA92C3F89A-1274548901937/fm23-8/fm23_8.pdf'
world=bpy.data.worlds.new('EBR neutral studio');world.use_nodes=True;world.node_tree.nodes.clear()
bg=world.node_tree.nodes.new('ShaderNodeBackground');wo=world.node_tree.nodes.new('ShaderNodeOutputWorld');bg.inputs[0].default_value=(.75,.75,.75,1);bg.inputs[1].default_value=.30;world.node_tree.links.new(bg.outputs[0],wo.inputs[0]);sc.world=world
target=p(355,217)
for name,offset,power,size,color in [('Key',(-.3,-.7,1.0),28,1.0,(1,.99,.98)),('Fill',(.6,-.8,.2),12,1.1,(.98,.99,1)),('Rim',(.15,.55,.7),28,.85,(1,1,1)),('Front',(-.7,-.3,.4),10,.7,(.97,.98,1))]:
    dat=bpy.data.lights.new('STUDIO_'+name,'AREA');dat.energy=power;dat.shape='DISK';dat.size=size;dat.color=color;ob=bpy.data.objects.new(dat.name,dat);COL['10'].objects.link(ob);ob.location=target+Vector(offset);ob.rotation_euler=(target-ob.location).to_track_quat('-Z','Y').to_euler()
def camera(name,outward,center,scale,res):
    dat=bpy.data.cameras.new('CAM_'+name);dat.type='ORTHO';dat.sensor_fit='HORIZONTAL';dat.ortho_scale=scale;ob=bpy.data.objects.new(dat.name,dat);COL['10'].objects.link(ob)
    ob.location=Vector(center)+Vector(outward).normalized()*2.5;ob.rotation_euler=(Vector(center)-ob.location).to_track_quat('-Z','Y').to_euler();ob['resolution']=res;return ob
camera('HERO',(.25,-1,.22),p(357,221),1.25,(3200,1250))
camera('SIDE',(0,-1,0),p(357,226),1.225,(3200,930))
camera('REFERENCE',(.013,-1,.06),p(357,226),1.225,(3200,930))
camera('REVERSE',(-.19,1,.18),p(357,221),1.25,(3200,1250))
camera('TOP',(0,-.03,1),p(357,203),1.225,(3200,830))
camera('STOCK',(-.18,-1,.19),p(127,230),.36,(1800,1150))
camera('RECEIVER',(.17,-1,.26),p(273,215),.32,(1800,1250))
camera('HANDGUARD',(.28,-1,.23),p(447,193),.40,(2000,1100))
camera('MUZZLE',(.36,-1,.22),p(601,187),.30,(1800,1000))
sc.camera=bpy.data.objects['CAM_HERO'];sc.render.resolution_x=3200;sc.render.resolution_y=1250;sc.render.resolution_percentage=100
sc.render.engine='CYCLES';sc.cycles.samples=64;sc.cycles.use_denoising=True;sc.render.film_transparent=True;sc.render.image_settings.file_format='PNG';sc.render.image_settings.color_mode='RGBA'
sc.view_settings.view_transform='AgX';sc.view_settings.exposure=.30
try:sc.view_settings.look='AgX - Medium High Contrast'
except TypeError:pass
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            space=area.spaces.active;space.overlay.show_overlays=False;space.shading.type='MATERIAL';space.region_3d.view_perspective='CAMERA'
bpy.ops.object.select_all(action='DESELECT');root.select_set(True);bpy.context.view_layer.objects.active=root
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(D/'BOSS_M14_EBR_MASTER.blend'))
report={'master':str(D/'BOSS_M14_EBR_MASTER.blend'),'mesh_objects':sum(o.type=='MESH' for o in PARTS),'separate_parts':len(PARTS),'materials':list(M),'scale_datum_m':LENGTH,'reference_sha256':sc['reference_sha256'],'editable_subassemblies':[o.name for o in GROUPS.values()], 'boss_fit_status':'Pending target BOSS identification; unscaled native human-reference model.'}
(D/'build_manifest.json').write_text(json.dumps(report,indent=2),encoding='utf-8');print('EBR_MASTER_SAVED',json.dumps(report),flush=True)
