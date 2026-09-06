"""Stage 03: resculpted exterior, layered detail, longer main body; game art only."""
import bpy, bmesh, math, pathlib, json, random
from mathutils import Vector
P=pathlib.Path(__file__).resolve().parent
source=(P/'build_stage02.py').read_text(encoding='utf-8')
exec(compile(source.split('# Shared render studio.')[0],'stage02_asset','exec'))
OUT=P/'stage_03';(OUT/'renders').mkdir(parents=True,exist_ok=True)
root['stage']='03 / resculpted exterior and longer main body'

def progress(text): print('STAGE03 '+text,flush=True)

def rgb(h):
    v=[int(h[i:i+2],16)/255 for i in (0,2,4)]
    return tuple(c/12.92 if c<=.04045 else ((c+.055)/1.055)**2.4 for c in v)

def surface(key,h,metal=.65,rough=.36,grain=1900):
    m=M[key] if key in M else bpy.data.materials.new('AI_M7_'+key)
    m.use_nodes=True;n=m.node_tree.nodes;l=m.node_tree.links;n.clear()
    out=n.new('ShaderNodeOutputMaterial');bs=n.new('ShaderNodeBsdfPrincipled')
    bs.inputs['Metallic'].default_value=metal
    bs.inputs['Roughness'].default_value=rough
    c=rgb(h);m.diffuse_color=(*c,1)
    geo=n.new('ShaderNodeNewGeometry');noise=n.new('ShaderNodeTexNoise')
    noise.inputs['Scale'].default_value=grain;noise.inputs['Detail'].default_value=2
    l.new(geo.outputs['Position'],noise.inputs['Vector'])
    tint=n.new('ShaderNodeValToRGB')
    tint.color_ramp.elements[0].color=(*[v*.88 for v in c],1)
    tint.color_ramp.elements[1].color=(*[min(v*1.08,1) for v in c],1)
    l.new(noise.outputs['Fac'],tint.inputs[0]);l.new(tint.outputs['Color'],bs.inputs['Base Color'])
    roughness=n.new('ShaderNodeMapRange')
    roughness.inputs['To Min'].default_value=rough-.045
    roughness.inputs['To Max'].default_value=rough+.045
    l.new(noise.outputs['Fac'],roughness.inputs[0]);l.new(roughness.outputs[0],bs.inputs['Roughness'])
    bump=n.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.20
    bump.inputs['Distance'].default_value=.000038 if metal else .000085
    l.new(noise.outputs['Fac'],bump.inputs['Height']);l.new(bump.outputs[0],bs.inputs['Normal'])
    l.new(bs.outputs[0],out.inputs[0]);M[key]=m
    return m

surface('Desert','887356',.67,.39)
surface('Sand_Edge','927F64',.61,.43)
surface('Sand_Dark','675843',.61,.42)
surface('Black','171C21',.72,.34)
surface('Black_Hardware','30373E',.82,.32)
surface('Dark_Steel','49525C',.85,.31)
surface('Rubber','12181C',0,.58,950)
surface('Polymer','242B2D',.06,.48,1400)
surface('Edge_Alloy','8F9290',.83,.39)
material('Ink','C2C7C8',.05,.66)
material('Ink_Dark','393A34',.05,.63)

def lofted(name,outline,half=14,inset=.92,group='02',mat='Desert',bev=.35):
    # Face inset loops create real sloping shoulders and a thicker central body.
    cx=sum(x for x,z in outline)/len(outline);cz=sum(z for x,z in outline)/len(outline)
    face=[(cx+(x-cx)*.982,cz+(z-cz)*inset) for x,z in outline]
    loops=[(-half,face),(-half+3.5,outline),(half-3.5,outline),(half,face)]
    vs=[p(x,z,d) for d,loop in loops for x,z in loop];n=len(outline)
    fs=[tuple(range(n-1,-1,-1)),tuple(range(3*n,4*n))]
    for j in range(3):fs.extend([(j*n+k,j*n+(k+1)%n,(j+1)*n+(k+1)%n,(j+1)*n+k) for k in range(n)])
    ob=mesh(name,vs,fs,mat,group,0)
    ob.data.materials.append(M['Sand_Edge']);ob.data.materials.append(M['Sand_Dark'])
    for f in ob.data.polygons:
        if f.normal.z>.45:f.material_index=1
        elif f.normal.z<-.45:f.material_index=2
    return finish(ob,bev,3)

def pocket(ob,shape,d,depth=1.0):
    for m in list(ob.modifiers):ob.modifiers.remove(m)
    sg=1 if d>0 else -1
    cut(ob,[shape],d+sg*1.2,d-sg*depth)
    for i,mat in enumerate(ob.data.materials):
        if mat==M['Black']:ob.data.materials[i]=M['Sand_Dark']
    return finish(ob,.27,3)

def screenprint(name,text,x,z,d,size=2.0,mat='Ink_Dark',group='02'):
    ob=label(name,text,x,z,d,size,mat,group);ob.data.extrude=0
    return ob

def panel(name,shape,d,mat='Black',group='02',th=.5,bevel=.18):
    return poly(name,shape,d,d-(1 if d>0 else -1)*th,mat,group,bevel)

def edge_scuffs(name,points,group='02',seed=1):
    rng=random.Random(seed)
    for j,(x,z,d) in enumerate(points):
        for k in range(3):
            xx=x+rng.uniform(-1.4,1.4);zz=z+rng.uniform(-.14,.14)
            line(name+'_%d_%d'%(j,k),[(xx,zz,d),(xx+rng.uniform(.4,1.4),zz+rng.uniform(-.12,.12),d)],
                 rng.uniform(.055,.11),'Edge_Alloy',group)

drop(group='02')
upper=lofted('Sculpted_Upper_Receiver',[(141,241),(150,232),(298,232),(309,238),(310,260),
                (296,267),(184,267),(157,260),(149,255)],14.7,.78)
lower=lofted('Sculpted_Lower_Receiver',[(158,261),(182,266),(224,269),(240,264),(299,264),
                (305,298),(300,305),(243,315),(234,298),(208,296),(184,304),(170,293),(159,276)],15.3,.89)

# Shallow pockets and rail recesses are cut into the metal instead of drawn on it.
for sg in (-1,1):
    pocket(upper,[(171,237),(295,237),(299,240),(296,242),(210,242),(204,244),(176,243)],sg*14.7,.9)
    panel('Upper_Track_Shadow_'+str(sg),[(176,238),(293,238),(296,240),(210,240),(204,242),(177,241)],sg*13.92,'Sand_Dark',th=.12)
    pocket(upper,hexrect(216,246,291,261,2),sg*14.7,1.9)
    pocket(lower,[(244,280),(295,274),(298,299),(246,309),(240,299)],sg*15.3,1.0)
    panel('Magwell_Inner_Face_'+str(sg),[(245,280.5),(294.4,275.6),(296.4,298.5),(246.6,307.6),(241.3,298.6)],sg*14.52,'Desert',th=.25)
    # The receiver joint stays narrow; the two bodies have separate sloping shoulders.
    line('Receiver_Joint_'+str(sg),[(164,263,sg*13.1),(185,268,sg*13.1),(227,270,sg*13.1),(241,266,sg*13.1),(298,266,sg*13.1)],.18,'Seam')
    panel('Lower_Control_Rebate_'+str(sg),hexrect(193,275,230,286,1.8),sg*15.48,'Sand_Dark',th=.5)
    panel('Lower_Control_Plate_'+str(sg),hexrect(195,276,228,284.5,1.2),sg*15.76,'Desert',th=.35)

# Receiver cover, a recessed bright inner strip and individual hinge segments.
panel('Port_Inner_Frame',hexrect(219,247,289,260,1.4),13.65,'Black_Hardware',th=.8)
panel('Port_Deep_Shadow',hexrect(223,248.2,283,257.8,1),13.9,'Recess',th=.3)
panel('Port_Inner_Plate',hexrect(229,249.3,276,255.4,.7),14.0,'Dark_Steel',th=.3)
panel('Port_Inner_Plate_Rebate',hexrect(232,250.2,271,254.2,.4),14.13,'Black',th=.2)
rod('Port_Lower_Hinge',p(220,260,14.4),p(284,260,14.4),.7,'Dark_Steel','02',40,.04)
for j in range(6):ring('Port_Hinge_Knuckle_%d'%j,p(222+j*10.4,260,14.4),p(225.8+j*10.4,260,14.4),.94,.68,'Black_Hardware','02',32,.06)
for x in (221,286):screw('Port_Latch_'+str(x),x,253.5,14.2,1.05,'02',False)
panel('Left_Receiver_Service_Plate',hexrect(220,248,287,260,1.4),-14.1,'Desert',th=.5)
for j in range(5):panel('Left_Service_Rib_%d'%j,hexrect(231+j*9.8,250,236+j*9.8,251.1,.25),-14.5,'Sand_Dark',th=.2)

# Rear relief cluster creates a much less slab-like silhouette in three-quarter view.
for sg in (-1,1):
    chassis=panel('Rear_Control_Boss_'+str(sg),[(158,246),(167,242),(177,245),(183,252),(179,260),(166,262),(159,256)],sg*15.7,'Desert',th=3.3,bevel=.55)
    rod('Rear_Control_Collar_'+str(sg),p(168,253,sg*15.2),p(168,253,sg*18.2),4.65,'Black_Hardware','02',64,.25)
    rod('Rear_Control_Button_'+str(sg),p(168,253,sg*18.1),p(168,253,sg*19.9),3.4,'Black','02',48,.27)
    ring('Rear_Control_Edge_'+str(sg),p(168,253,sg*19.7),p(168,253,sg*20),3.35,2.45,'Dark_Steel','02',48,.06)
    panel('Rear_Angled_Release_'+str(sg),[(179,250),(194,252),(197,256),(194,261),(180,258)],sg*17.4,'Black',th=2.5,bevel=.32)
    for j in range(5):line('Release_Knurl_%d_%s'%(j,sg),[(184+j*2,253,sg*17.65),(185+j*2,257,sg*17.65)],.22,'Black_Hardware')
    panel('Charging_Carriage_'+str(sg),[(147,237),(158,235),(164,238),(161,243),(150,244)],sg*15.5,'Black',th=2,bevel=.32)
    panel('Charging_Handle_Paddle_'+str(sg),[(148,239),(156,237),(160,239),(156,243),(148,244)],sg*23,'Black_Hardware',th=8,bevel=.42)
    for j in range(4):line('Charging_Paddle_Grip_%d_%s'%(j,sg),[(150+j*1.6,239.5,sg*23.2),(150+j*1.6,242,sg*23.2)],.18,'Black')
    ring('Receiver_Rear_Screw_Washer_'+str(sg),p(150,250,sg*12.2),p(150,250,sg*12.6),2.2,1.2,'Sand_Dark','02',32,.08)
    screw('Receiver_Rear_Captive_'+str(sg),150,250,sg*12.7,1.45,'02',False)
    # Two different controls with protective rims and actual ridged thumb pads.
    rod('Selector_Guarded_Base_'+str(sg),p(187,284,sg*15.1),p(187,284,sg*16.1),3.7,'Sand_Dark','02',48,.22)
    rod('Selector_Pivot_'+str(sg),p(187,284,sg*16),p(187,284,sg*17),2.75,'Black_Hardware','02',48,.16)
    panel('Selector_Thumb_Lever_'+str(sg),[(186,282),(197,286),(198,289),(195,290),(184,286)],sg*17.8,'Black',th=1.4,bevel=.32)
    for j in range(3):line('Selector_Thumb_Rib_%d_%s'%(j,sg),[(191+j*1.8,286,sg*18),(191+j*1.8,288,sg*18)],.17,'Dark_Steel')
    panel('Release_Recess_Rim_'+str(sg),hexrect(222,272,235,278,1.2),sg*15.7,'Sand_Dark',th=.4)
    panel('Release_Ridged_Pad_'+str(sg),hexrect(225,273,234,277,.65),sg*16.45,'Black',th=.85)
    for j in range(4):line('Release_Pad_Texture_%d_%s'%(j,sg),[(226+j*1.8,273.8,sg*16.7),(226+j*1.8,276.2,sg*16.7)],.16,'Dark_Steel')
    screw('Receiver_Front_Pivot_'+str(sg),299,275,sg*14.7,1.9,'02',False)
    screw('Lower_Rear_Pivot_'+str(sg),177,277,sg*14.6,1.65,'02',False)
    screw('Lower_Control_Plate_'+str(sg),200,280,sg*16,1.05,'02',False)
    screw('Lower_Panel_Pin_'+str(sg),226,289,sg*15.3,1.25,'02',False)

screenprint('Receiver_Designation','E-01 / M7',250,287,14.82,2.65)
screenprint('Receiver_Secondary','AI INFANTRY',250,291,14.82,1.45)
screenprint('Receiver_Asset_ID','AR.071  /  III',250,295,14.82,1.4)
screenprint('Selector_Indices','I   II',185,278,15.7,1.3)
for sg in (-1,1):
    panel('Receiver_Vertical_Latch_'+str(sg),hexrect(204,247,211,260,.9),sg*15.1,'Black','02',1,.28)
    for j in range(5):line('Vertical_Latch_Ridge_%d_%s'%(j,sg),[(205.2,250+j*1.65,sg*15.35),(209.8,250+j*1.65,sg*15.35)],.16,'Dark_Steel')
    screw('Vertical_Latch_Captive_'+str(sg),207.5,248.3,sg*15.3,.75,'02',False)
for j in range(18):box('Receiver_ID_Bar_%d'%j,(251+j*.7,301,14.83),(.18 if j%3 else .36,1.7,.03),'Ink_Dark','02',0)
edge_scuffs('Receiver_Local_Wear',[(157,239,14.8),(211,241,14.8),(285,242,14.8),(248,307,14.95)],seed=21)
progress('receiver resculpted with recessed surfaces and layered controls')

# Keep the previously lengthened magazine; rebuild the grip with changing sections.
drop('Pistol_Grip','Grip_','Trigger_')
sections=[(302,182,11.6,9.4),(310,179,12.3,11),(325,173,13.3,11.8),
          (340,167,13.5,11),(351,163,13.2,10),(357,163,11.7,9.1)]
vs=[];n=24
for z,x,rx,rd in sections:
    for k in range(n):
        a=math.tau*k/n;ca,sa=math.cos(a),math.sin(a)
        vs.append(p(x+rx*math.copysign(abs(ca)**.62,ca),z,rd*math.copysign(abs(sa)**.62,sa)))
fs=[tuple(range(n-1,-1,-1)),tuple(range((len(sections)-1)*n,len(sections)*n))]
for j in range(len(sections)-1):fs.extend([(j*n+k,j*n+(k+1)%n,(j+1)*n+(k+1)%n,(j+1)*n+k) for k in range(n)])
grip=mesh('Ergonomic_Pistol_Grip',vs,fs,'Polymer','03',0)
for f in grip.data.polygons:f.use_smooth=len(f.vertices)==4
finish(grip,.3,3)
for sg in (-1,1):
    panel('Grip_Molded_Inset_'+str(sg),[(175,313),(185,314),(180,327),(173,349),(158,346),(165,325)],sg*11.5,'Rubber','03',.7,.65)
    for j in range(14):
        z=317+j*2.1;x=172-(z-317)*.36
        line('Grip_Diamond_A_%d_%s'%(j,sg),[(x,z,sg*11.72),(x+11,z+4,sg*11.72)],.14,'Polymer','03')
        line('Grip_Diamond_B_%d_%s'%(j,sg),[(x+11,z,sg*11.73),(x+1,z+4,sg*11.73)],.14,'Polymer','03')
    screw('Grip_Recessed_Fastener_'+str(sg),170,337,sg*11.7,1.1,'03',False)
poly('Grip_Rubber_Heel',[(153,350),(176,354),(175,359),(153,355)],9.5,-9.5,'Rubber','03',.8)
guard=[(195,296),(200,298),(201,306),(234,306),(240,298),(241,288),(245,289),(245,300),(237,311),(198,312),(194,308)]
poly('Trigger_Guard_Machined',guard,7.3,-7.3,'Black','03',.8)
poly('Trigger_Visual',[(216,288),(219,289),(214,299),(215,303),(212,302),(211,298)],2.0,-2.0,'Dark_Steel','03',.55)
for sg in (-1,1):
    for j in range(4):line('Trigger_Guard_Grip_%d_%s'%(j,sg),[(237.5+j*.7,302-j,sg*7.55),(239+j*.7,303-j,sg*7.55)],.18,'Black_Hardware','03')
edge_scuffs('Mag_Floor_Wear',[(263,404,13.2),(279,401,13.2),(299,398,13.2)],'03',33)

# Conform the grip insert to a dense quad surface instead of a bent ngon.
def grip_surface(x,z):
    z=max(sections[0][0],min(sections[-1][0],z))
    for aa,bb in zip(sections,sections[1:]):
        if aa[0]<=z<=bb[0]:
            t=(z-aa[0])/(bb[0]-aa[0]);cx=aa[1]*(1-t)+bb[1]*t
            rx=aa[2]*(1-t)+bb[2]*t;rd=aa[3]*(1-t)+bb[3]*t
            return rd*max(.05,1-min(.99,abs(x-cx)/rx)**(2/.62))**(.62/2)
    return 10
drop('Grip_Molded_Inset','Grip_Diamond_')
surface('Grip_Stipple','12181A',0,.58,1800)
gm=M['Grip_Stipple'];gn=gm.node_tree.nodes;gl=gm.node_tree.links
gbs=next(n for n in gn if n.type=='BSDF_PRINCIPLED')
geo=next(n for n in gn if n.type=='NEW_GEOMETRY')
voro=gn.new('ShaderNodeTexVoronoi');voro.inputs['Scale'].default_value=1900
gl.new(geo.outputs['Position'],voro.inputs['Vector'])
bump=gn.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.34;bump.inputs['Distance'].default_value=.00011
gl.new(voro.outputs['Distance'],bump.inputs['Height']);gl.new(bump.outputs[0],gbs.inputs['Normal'])
for sg in (-1,1):
    vs=[];nx=12;nz=28
    for iz in range(nz+1):
        t=iz/nz;z=313+t*36;cx=178-(z-310)*.405
        half=6.4*(.72+.28*math.sin(math.pi*t)**.38)
        for ix in range(nx+1):
            x=cx+half*(2*ix/nx-1)
            vs.append(p(x,z,sg*(grip_surface(x,z)+.12)))
    fs=[(iz*(nx+1)+ix,iz*(nx+1)+ix+1,(iz+1)*(nx+1)+ix+1,(iz+1)*(nx+1)+ix) for iz in range(nz) for ix in range(nx)]
    ob=mesh('Grip_Conforming_Stipple_'+str(sg),vs,fs,'Grip_Stipple','03',0)
    for face in ob.data.polygons:face.use_smooth=True
    solid=ob.modifiers.new('Thin molded insert','SOLIDIFY');solid.thickness=.12*S;solid.offset=-1

# Three fore-end shells and ventilated sloping shoulders replace the flat grill.
drop(group='04')
drop('Foreend_Dark_Inner_Core','Interior_Visible_Rib_')
for sg in (-1,1):
    for i,(a,b) in enumerate(((308,376),(377.2,455),(456.2,534))):
        outline=[(a+2,240),(a+7,236),(b-5,236),(b,241),(b,260),(b-5,265),(a+4,265),(a,261)]
        ob=poly('Segmented_Foreend_%d_%s'%(i,sg),outline,sg*14,sg*11.7,'Desert','04',0)
        holes=[]
        for j in range(3):
            x=a+13+j*18.5
            holes.append(hexrect(x,240,x+11.5,244,1.3))
            holes.append(hexrect(x-3,249,x+10.5,258,1.1,4.1))
        cut(ob,holes,sg*16,sg*10);finish(ob,.33,3)
        pocket(ob,hexrect(a+10,246.2,b-8,246.8,.2),sg*14,.32)
        for x,z in ((a+6,244),(b-5,261)):
            screw('Foreend_Panel_Captive_%d_%s_%s'%(i,x,sg),x,z,sg*14.25,1.25,'04',False)
        # Narrow top flange changes depth between the seam and the upper shoulder.
        panel('Foreend_Panel_Flange_%d_%s'%(i,sg),[(a+8,235.9),(b-5,235.9),(b-3,237.3),(a+7,237.3)],sg*14.18,'Sand_Edge','04',.28,.12)
        screenprint('Foreend_Section_%d_%s'%(i,sg),'0'+str(i+1),b-14,262.7,sg*14.1,1.35,'Ink_Dark','04')

def shoulder(name,cross,sg,upper=True):
    verts=[p(x,z,sg*d) for x in (312,532) for z,d in cross]
    fs=[(3,2,1,0),(4,5,6,7)]+[(i,(i+1)%4,(i+1)%4+4,i+4) for i in range(4)]
    ob=mesh(name,verts,fs,'Sand_Edge' if upper else 'Sand_Dark','04',0)
    holes=[]
    for j in range(11):
        x=323+j*18.3
        holes.append(hexrect(x,231.8 if upper else 266,x+11.2,235.7 if upper else 270.4,.65,0 if upper else -2.0))
    cut(ob,holes,sg*16,sg*6);finish(ob,.19,3)
    return ob
for sg in (-1,1):
    shoulder('Ventilated_Upper_Shoulder_'+str(sg),[(230,8),(237,14),(237.5,12.8),(231,7.8)],sg)
    shoulder('Ventilated_Lower_Shoulder_'+str(sg),[(264,14),(272,8),(271,7),(263.6,12.8)],sg,False)
    line('Lower_Foreend_Edge_'+str(sg),[(316,264.8,sg*14.1),(529,264.8,sg*14.1)],.13,'Sand_Edge','04')
box('Foreend_Lower_Spine',(421,271.5,0),(220,2.8,14),'Black','04',.3)
for j in range(15):box('Foreend_Underside_Segmentation_%d'%j,(329+j*13.4,273.5,0),(6.5,1.5,15),'Polymer','04',.25)

# Slim reinforcement collars have their own latches and countersunk hardware.
for sg in (-1,1):
    panel('Foreend_Root_Clamp_'+str(sg),[(307,235),(315,234),(315,247),(310,254),(313,267),(307,272),(302,266),(302,241)],sg*14.7,'Sand_Dark','04',3,.38)
    panel('Foreend_Root_Clamp_Face_'+str(sg),[(306,237),(311,236),(311,247),(306,253),(309,266),(306,267),(304,263),(304,240)],sg*15.2,'Desert','04',.8,.28)
    screw('Foreend_Root_Screw_'+str(sg),307,243,sg*15.7,1.55,'04',False)
    screw('Foreend_Root_Lower_Screw_'+str(sg),307,263,sg*15.7,1.35,'04',False)
    for x in (376.5,455.5):
        line('Foreend_Section_Joint_%s_%s'%(x,sg),[(x,238,sg*13.8),(x,263,sg*13.8)],.23,'Black','04')

# Local heat shield and a short accessory rail break the evenly repeated openings.
panel('Foreend_Heatshield_Gasket',[(338,256),(387,255),(393,260),(390,267),(340,268),(336,264)],15.3,'Black','04',1.2,.45)
panel('Foreend_Heatshield',[(341,257),(385,256.5),(390,260),(388,265),(342,266),(339,263)],16.1,'Polymer','04',1.1,.4)
for j in range(13):
    x=343+j*3.3
    line('Heatshield_Molded_Rib_%02d'%j,[(x,258.5,16.25),(x+2,263.6,16.25)],.28,'Black_Hardware','04')
for x in (342,386):screw('Heatshield_Captive_'+str(x),x,261,16.3,1.0,'04',False)
panel('Side_Accessory_Rail_Base',hexrect(407,252,451,261,1.7),-16.1,'Black','04',3.2,.32)
for j in range(8):box('Side_Accessory_Rail_Tooth_%d'%j,(412+j*4.7,256.5,-17.1),(2.9,8,2),'Black_Hardware','04',.15)
for x in (410,448):screw('Side_Accessory_Captive_'+str(x),x,256.5,-18.2,1.05,'04',False)

# Complete octagonal nose with a slim dark liner.
cap_outer=[(230,-8),(230,8),(237,14),(264,14),(272,8),(272,-8),(264,-14),(237,-14)]
cap_inner=[(237,-5),(237,5),(243,10),(260,10),(267,5),(267,-5),(260,-10),(243,-10)]
v=[p(x,z,d) for x,shape in ((530,cap_outer),(534,cap_outer),(530,cap_inner),(534,cap_inner)) for z,d in shape]
f=[]
for k in range(8):
    j=(k+1)%8;f.extend([(k,j,8+j,8+k),(16+k,24+k,24+j,16+j),(k,16+k,16+j,j),(8+k,8+j,24+j,24+k)])
mesh('Foreend_Octagonal_Front_Return',v,f,'Sand_Dark','04',.35)
rod('Foreend_Interior_Shadow_Core',p(308,255),p(536,255),7.1,'Black','05',64,.18)
for j in range(22):ring('Foreend_Interior_Heat_Band_%d'%j,p(315+j*10,255),p(316.4+j*10,255),7.55,7,'Black_Hardware','05',48,.06)

# Finer cross teeth and folded silhouettes at both ends of the upper rail.
box('Continuous_Rail_Base',(335,228,0),(394,3,14),'Black','04',.25)
for j in range(73):
    x=140+j*5.43
    verts=[p(xx,z,d) for xx in (x,x+3.0) for z,d in ((228,-6.5),(225.5,-8.6),(224,-8.0),(224,8),(225.5,8.6),(228,6.5))]
    fs=[(5,4,3,2,1,0),(6,7,8,9,10,11)]+[(i,(i+1)%6,(i+1)%6+6,i+6) for i in range(6)]
    mesh('Fine_Top_Rail_Tooth_%02d'%j,verts,fs,'Black_Hardware','04',.13)
for j,x in enumerate((179,328,376,424,473,520)):
    rod('Top_Rail_Fastener_'+str(j),p(x,225.3,0),p(x,224.1,0),1.15,'Black','04',32,.06)
for x in (163,515):
    box('Folded_Sight_Base_'+str(x),(x,221,0),(16,4,13),'Black','04',.35)
    rod('Folded_Sight_External_Pivot_'+str(x),p(x+4,218,-7),p(x+4,218,7),2.3,'Black_Hardware','04',40,.13)
    poly('Folded_Sight_Hood_'+str(x),[(x-6,219),(x-4,214),(x+4,214),(x+7,218),(x+5,220)],5,-5,'Black','04',.35)
    box('Folded_Sight_Inset_'+str(x),(x,216,0),(4,1.5,6),'Recess','04',.16)
for x in (330,381,471,513):screenprint('Rail_Locator_'+str(x),'R-'+str(x//10),x,238.3,14.3,1.05,'Ink_Dark','04')
edge_scuffs('Foreend_Local_Wear',[(315,237,14.35),(374,238,14.2),(457,238,14.2),(527,262,14.2)],'04',7)
progress('segmented ventilated fore-end and local attachments rebuilt')

# Stock: rounded shoulder surfaces, a recessed cheek pocket and a compact latch.
drop(group='01')
cheek=lofted('Stock_Sculpted_Cheek',[(5,237),(93,239),(108,247),(105,256),(66,260),(23,270),(8,271)],12.5,.68,'01','Desert',.75)
rear=lofted('Stock_Rear_Skeleton',[(8,260),(18,257),(24,289),(21,302),(12,301)],10.5,.85,'01','Desert',.65)
strut=lofted('Stock_Lower_Skeleton',[(20,291),(79,272),(102,267),(99,275),(31,299),(21,306),(16,302)],8.8,.83,'01','Desert',.6)
for sg in (-1,1):
    pocket(cheek,[(20,244),(89,245),(97,249),(92,252),(33,255),(21,260)],sg*12.5,.65)
    panel('Stock_Cheek_Pocket_Floor_'+str(sg),[(23,245),(88,246),(94,249),(91,251),(34,253),(23,257)],sg*12.0,'Sand_Dark','01',.12,.25)
    panel('Stock_Cheek_Pocket_Insert_'+str(sg),[(25,245.5),(87,247),(91,249),(35,252),(25,255)],sg*12.15,'Desert','01',.2,.25)
    line('Stock_Lower_Recess_'+str(sg),[(29,295,sg*8.9),(85,277,sg*8.9)],.3,'Sand_Dark','01')
    screw('Stock_Cheek_Captive_A_'+str(sg),24,247,sg*12.7,1.1,'01',False)
    screw('Stock_Cheek_Captive_B_'+str(sg),89,248,sg*12.7,1.05,'01',False)
    ring('Stock_Sling_Collar_'+str(sg),p(16.5,282,sg*10.25),p(16.5,282,sg*10.8),2.9,1.75,'Black_Hardware','01',48,.1)
    rod('Stock_Sling_Dark_Recess_'+str(sg),p(16.5,282,sg*10.3),p(16.5,282,sg*10.45),1.72,'Recess','01',40,.02)
    panel('Stock_Adjustment_Latch_'+str(sg),[(74,258),(101,254),(104,258),(83,269),(77,268)],sg*11.3,'Black','01',3.2,.38)
    for j in range(7):line('Stock_Latch_Grip_%d_%s'%(j,sg),[(79+j*2.7,260-j*.6,sg*11.55),(80+j*2.7,263-j*.6,sg*11.55)],.24,'Black_Hardware','01')
    screw('Stock_Latch_Pivot_'+str(sg),101,256,sg*11.7,1.2,'01',False)
lathe('Stock_Exposed_Tube',[(101,5.6),(107,6.8),(132,6.8),(137,8),(145,8),(149,6.7)],252,0,'Black','01',64)
for j in range(7):ring('Stock_Tube_Rib_%d'%j,p(113+j*2.6,252),p(114+j*2.6,252),7.15,6.5,'Black_Hardware','01',48,.06)
poly('Stock_Hinge_Chassis',hexrect(134,241,145,263,2),11,-11,'Black','01',.55)
screw('Stock_Hinge_Axle',139.5,252,11.5,2.75,'01')
pad=lofted('Stock_Molded_Buttpad',[(1,237),(6,236),(12,244),(13,299),(8,309),(2,306)],13,.9,'01','Rubber',1.0)
# Restore rubber across bevels; the stock pad should not inherit sand facets.
pad.data.materials.clear();pad.data.materials.append(M['Rubber'])
for face in pad.data.polygons:face.material_index=0
for j in range(13):box('Buttpad_Grip_Rib_%d'%j,(3,243+j*4.55,0),(4,1.1,26.5),'Rubber','01',.38)
screenprint('Stock_Asset_Mark','E-01  //  III',46,250.2,12.5,1.7,'Ink_Dark','01')
edge_scuffs('Stock_Local_Wear',[(23,247,12.8),(88,246,12.8),(30,296,8.9)],'01',11)
progress('stock and grip reshaped with recesses and molded surfaces')

# Lengthen the main receiver and fore-end; retain the optic, grip and magazine sizes.
def stretch_x(x):
    if x<=145:return x
    if x<=305:return 145+(x-145)*1.10
    if x<=534:return 321+(x-305)*1.28
    return 614.12+(x-534)

def mapped_world_x(x):return (stretch_x(x/S+300)-300)*S

for ob in list(root.children):
    group=next((key for key,c in cols.items() if ob.name in c.objects),'')
    delta=None
    if group=='01':delta=0
    elif group=='06':delta=(stretch_x(252)-252)*S
    elif group=='03' and ('Magazine' in ob.name or 'Mag_Floor' in ob.name):delta=(stretch_x(266)-266)*S
    elif group=='03' and ('Grip' in ob.name or 'grip' in ob.name):delta=(stretch_x(180)-180)*S
    if ob.type=='MESH':
        co=[v.co for v in ob.data.vertices]
        if not co:continue
        if delta is None:
            lo=min(v.x for v in co);hi=max(v.x for v in co)
            if (hi-lo)/S<12:
                centerx=(hi+lo)/2;delta=mapped_world_x(centerx)-centerx
        for v in co:v.x=v.x+delta if delta is not None else mapped_world_x(v.x)
    elif ob.type=='CURVE':
        for sp in ob.data.splines:
            for pt in sp.points:pt.co.x=pt.co.x+delta if delta is not None else mapped_world_x(pt.co.x)
    elif ob.type=='FONT':
        ob.location.x=ob.location.x+delta if delta is not None else mapped_world_x(ob.location.x)
        if ob.location.y>0:ob.rotation_euler=(math.pi/2,0,math.pi)

root['body_length_change']='Receiver +10 percent; fore-end +28 percent; combined main body +20.6 percent'
root['optic_finish']='Black and charcoal housing; no sand component'
root['scale_status']='Arbitrary game-art proportions; character fit still pending'
progress('main body stretched; separate optic and magazine dimensions retained')

# A neutral studio shows the smaller radius changes, metal and polymer finish.
sc.render.engine='CYCLES';sc.cycles.samples=80;sc.cycles.use_denoising=True
try:
    pref=bpy.context.preferences.addons['cycles'].preferences
    pref.compute_device_type='OPTIX';pref.get_devices()
    for device in pref.devices:device.use=device.type=='OPTIX'
    sc.cycles.device='GPU' if any(d.use for d in pref.devices) else 'CPU'
except Exception:sc.cycles.device='CPU'
sc.render.image_settings.file_format='PNG';sc.render.image_settings.color_mode='RGBA'
sc.render.film_transparent=False
sc.view_settings.view_transform='AgX';sc.view_settings.look='AgX - Medium High Contrast';sc.view_settings.exposure=-.15
w=bpy.data.worlds.new('Stage03_Studio');w.use_nodes=True;sc.world=w
n=w.node_tree.nodes;l=w.node_tree.links;n.clear()
output=n.new('ShaderNodeOutputWorld');mix=n.new('ShaderNodeMixShader');ray=n.new('ShaderNodeLightPath')
env=n.new('ShaderNodeBackground');env.inputs[0].default_value=(.43,.46,.5,1);env.inputs[1].default_value=.25
bg=n.new('ShaderNodeBackground');bg.inputs[0].default_value=(.11,.125,.14,1);bg.inputs[1].default_value=.85
l.new(ray.outputs['Is Camera Ray'],mix.inputs[0]);l.new(env.outputs[0],mix.inputs[1]);l.new(bg.outputs[0],mix.inputs[2]);l.new(mix.outputs[0],output.inputs[0])

def studio_light(name,loc,target,energy,size,color):
    data=bpy.data.lights.new(name,'AREA');data.energy=energy;data.shape='DISK';data.size=size;data.color=color
    ob=bpy.data.objects.new(name,data);cols['07'].objects.link(ob);ob.location=loc
    ob.rotation_euler=(Vector(target)-ob.location).to_track_quat('-Z','Y').to_euler()

center=Vector((.207,0,-.035))
studio_light('Broad_Upper_Key',(-.7,-1.4,1.6),center,180,1.7,(1,.97,.92))
studio_light('Front_Soft_Fill',(1.7,-1.1,.6),center,100,1.8,(.93,.97,1))
studio_light('Back_Edge_Light',(.6,.9,1.4),center,245,1.6,(.86,.93,1))
studio_light('Stock_Fill',(-1.5,.2,.2),center,65,1.6,(1,.95,.88))

def camera(name,loc,target,width):
    data=bpy.data.cameras.new(name);data.type='ORTHO';data.ortho_scale=width
    ob=bpy.data.objects.new(name,data);cols['07'].objects.link(ob);ob.location=loc
    ob.rotation_euler=(Vector(target)-ob.location).to_track_quat('-Z','Y').to_euler();return ob

camera('THREE_QUARTER',center+Vector((1.3,-5,1.65)),center,2.47)
camera('SIDE',center+Vector((0,-5,0)),center,2.47)
camera('OPPOSITE_SIDE',center+Vector((0,5,0)),center,2.47)
camera('TOP',center+Vector((0,0,5)),center,2.47)
camera('FRONT',(5,0,-.07),(0,0,-.07),.90)
t=p(stretch_x(230),254,0)
camera('RECEIVER_DETAIL',t+Vector((.34,-1.6,.6)),t,.91)
t=p(stretch_x(420),249,0)
camera('HANDGUARD_DETAIL',t+Vector((.63,-1.8,.75)),t,1.09)
t=p(252+stretch_x(252)-252,202,0)
camera('OPTIC_DETAIL',t+Vector((.43,-1.5,.55)),t,.54)

ref=bpy.data.images.load(str(P/'references/M7_reference.png'));ref.pack()
sc['reference_image']=ref.name
sc['stage']='03: main-body proportions and exterior refinement review'
sc.camera=bpy.data.objects['THREE_QUARTER']
sc.render.resolution_x=2900;sc.render.resolution_y=1300;sc.render.resolution_percentage=100
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            area.spaces.active.shading.type='MATERIAL'
            area.spaces.active.region_3d.view_location=center
            area.spaces.active.region_3d.view_distance=2.8
            area.spaces.active.region_3d.view_rotation=sc.camera.rotation_euler.to_quaternion()
            area.spaces.active.overlay.show_overlays=False
bpy.ops.object.select_all(action='DESELECT');root.select_set(True);bpy.context.view_layer.objects.active=root
bpy.context.preferences.filepaths.save_version=0
asset=OUT/'AI_INFANTRY_M7_STAGE03.blend'
bpy.ops.wm.save_as_mainfile(filepath=str(asset))
manifest={'stage':3,'status':'visual revision for review','previous_versions_preserved':['stage_01','stage_02'],
          'body_length_before':389,'body_length_after':469.12,'body_length_ratio':469.12/389,
          'not_real_manufacturing_units':True,'parts':len(root.children),
          'changes':['resculpted receiver shoulders','recessed chassis details','layered external controls',
          'three ventilated fore-end segments','angled top and bottom openings','local rail and heat shield',
          'rounded grip with conforming texture','recessed adjustable stock surfaces','satin metal and polymer finish'],
          'optic_materials':sorted({m.name for o in cols['06'].objects if hasattr(o.data,'materials') for m in o.data.materials}),
          'pending':['appearance feedback','character fitting','game export']}
(OUT/'stage03_manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
progress('master saved')
import os
render_names=os.environ.get('M7_STAGE03_RENDER_SET','THREE_QUARTER,SIDE,RECEIVER_DETAIL,HANDGUARD_DETAIL,OPTIC_DETAIL,OPPOSITE_SIDE,TOP,FRONT').split(',')
for name in render_names:
    sc.camera=bpy.data.objects[name]
    if name.endswith('DETAIL'):sc.render.resolution_x=2400;sc.render.resolution_y=1600
    elif name=='FRONT':sc.render.resolution_x=1100;sc.render.resolution_y=1500
    else:sc.render.resolution_x=2900;sc.render.resolution_y=1300
    sc.render.filepath=str(OUT/'renders'/(name+'.png'));bpy.ops.render.render(write_still=True)
    progress('rendered '+name)
progress('COMPLETE')
