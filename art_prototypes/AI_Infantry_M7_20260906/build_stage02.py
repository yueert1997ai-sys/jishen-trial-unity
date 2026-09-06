"""Stage 02: detailed exterior game asset, longer silhouette, all-black optic."""
import bpy, bmesh, math, pathlib, json, time
from mathutils import Vector

P = pathlib.Path(__file__).resolve().parent
source = (P / 'build_stage01.py').read_text(encoding='utf-8')
exec(compile(source.split('# Save the actual editable asset')[0], 'stage01_base', 'exec'))
OUT = P / 'stage_02'
(OUT / 'renders').mkdir(parents=True, exist_ok=True)
root['stage'] = '02 / mechanical detail and requested proportions'
root['revision'] = 'Longer exposed barrel; longer magazine; entirely black optic housing'

def progress(s):
    print('STAGE02 ' + s, flush=True)

def drop(*prefixes, group=None):
    victims = [o for o in list(bpy.data.objects)
               if (group is not None and o.name in cols[group].objects)
               or any(o.name.startswith('M7_' + s) for s in prefixes)]
    for o in victims:
        if o in PARTS:
            PARTS.remove(o)
        bpy.data.objects.remove(o, do_unlink=True)

def finish(ob, width=.35, segments=3):
    for m in list(ob.modifiers):
        ob.modifiers.remove(m)
    if width:
        b = ob.modifiers.new('Small machined edge radii', 'BEVEL')
        b.width = width * S
        b.segments = segments
        b.harden_normals = True
        b.use_clamp_overlap = True
        w = ob.modifiers.new('Face weighted normals', 'WEIGHTED_NORMAL')
        w.keep_sharp = True
    return ob

old_rod, old_ring = rod, ring
def rod(name, a, b, r, mat='Black', group='05', n=64, bev=.16, r2=None):
    ob = old_rod(name, a, b, r, mat, group, n, bev, r2)
    axis = (Vector(b)-Vector(a)).normalized()
    for f in ob.data.polygons:
        f.use_smooth = abs(f.normal.dot(axis)) < .5 and n >= 24
    return finish(ob, bev)

def ring(name, a, b, r, inner, mat='Dark_Steel', group='05', n=64, bev=.12):
    ob = old_ring(name, a, b, r, inner, mat, group, n, bev)
    axis = (Vector(b)-Vector(a)).normalized()
    for f in ob.data.polygons:
        f.use_smooth = abs(f.normal.dot(axis)) < .5 and n >= 24
    return finish(ob, bev)

def hexrect(x0, y0, x1, y1, c=.8, skew=0):
    return [(x0+c+skew,y0),(x1-c+skew,y0),(x1+skew,y0+c),
            (x1,y1-c),(x1-c,y1),(x0+c,y1),(x0,y1-c),(x0+skew,y0+c)]

def cut(ob, shapes, front, back):
    vs, fs = [], []
    for xy in shapes:
        n, offset = len(xy), len(vs)
        vs.extend([p(x,z,d) for d in (front,back) for x,z in xy])
        fs.extend([tuple(offset+i for i in range(n-1,-1,-1)),
                   tuple(offset+i for i in range(n,2*n))])
        fs.extend([(offset+i,offset+(i+1)%n,offset+(i+1)%n+n,offset+i+n) for i in range(n)])
    cutter = mesh('Temporary_Opening_Cutter', vs, fs, 'Black', '04', 0)
    mod = ob.modifiers.new('Actual exterior apertures', 'BOOLEAN')
    mod.operation = 'DIFFERENCE'
    mod.solver = 'EXACT'
    mod.object = cutter
    bpy.context.view_layer.objects.active = ob
    bpy.ops.object.modifier_apply(modifier=mod.name)
    PARTS.remove(cutter)
    bpy.data.objects.remove(cutter, do_unlink=True)
    return ob

def line(name, coords, rad=.3, mat='Seam', group='02'):
    cu = bpy.data.curves.new(name, 'CURVE')
    cu.dimensions = '3D'
    cu.resolution_u = 2
    cu.bevel_depth = rad*S
    cu.bevel_resolution = 2
    sp = cu.splines.new('POLY')
    sp.points.add(len(coords)-1)
    for pt, pos in zip(sp.points, coords):
        pt.co = (*p(*pos), 1)
    ob = bpy.data.objects.new('M7_'+name, cu)
    cols[group].objects.link(ob)
    cu.materials.append(M[mat])
    ob.parent = root
    return ob

def screw(name, x, z, d, r=1.5, group='02', both=True):
    signs = (-1,1) if both else ((1,) if d>=0 else (-1,))
    d = abs(d)
    for sg in signs:
        ring(name+'_Seat_'+str(sg),p(x,z,sg*(d-.15)),p(x,z,sg*(d+.13)),r*1.26,r*.9,'Seam',group,32,.05)
        rod(name+'_Head_'+str(sg),p(x,z,sg*d),p(x,z,sg*(d+.6)),r,'Black_Hardware',group,32,.1)
        rod(name+'_Hex_'+str(sg),p(x,z,sg*(d+.61)),p(x,z,sg*(d+.66)),r*.48,'Recess',group,6,.015)

def label(name, text, x, z, d, size=2.1, mat='Marking', group='02'):
    cu = bpy.data.curves.new(name, 'FONT')
    cu.body = text
    cu.size = size*S
    cu.space_character = 1.12
    cu.extrude = .006*S
    cu.resolution_u = 2
    ob = bpy.data.objects.new('M7_Label_'+name, cu)
    cols[group].objects.link(ob)
    ob.parent = root
    ob.location = p(x,z,d)
    ob.rotation_euler = (math.pi/2,0,0)
    cu.materials.append(M[mat])
    return ob

def lathe(name, profile, z=255, d=0, mat='Black', group='05', n=64):
    vs = [p(x,z+r*math.sin(k*math.tau/n),d+r*math.cos(k*math.tau/n)) for x,r in profile for k in range(n)]
    fs = [tuple(range(n-1,-1,-1))]
    for j in range(len(profile)-1):
        fs.extend([(j*n+k,j*n+(k+1)%n,(j+1)*n+(k+1)%n,(j+1)*n+k) for k in range(n)])
    fs.append(tuple(range((len(profile)-1)*n,len(profile)*n)))
    ob = mesh(name,vs,fs,mat,group,0)
    for f in ob.data.polygons:
        f.use_smooth = len(f.vertices)==4
    return finish(ob,.1)

material('Desert','977D59',.52,.34)
material('Sand_Edge','A58B67',.48,.32)
material('Sand_Dark','725E45',.5,.36)
material('Black','191D22',.64,.29)
material('Black_Hardware','30363C',.82,.25)
material('Dark_Steel','434B53',.83,.27)
material('Rubber','111519',.02,.54)
material('Seam','242624',.35,.46)
material('Marking','9DA7AC',.32,.42)
material('Sand_Marking','493F30',.25,.47)
material('Glass','10323A',.56,.13)
material('Lens_Coating','28434C',.68,.17)
material('Display','405E67',.4,.26)

# Controlled micro finish, visible mainly in closeups instead of a noisy coating.
for key in ('Desert','Sand_Edge','Sand_Dark','Black','Black_Hardware','Dark_Steel','Rubber'):
    m = M[key]
    nodes, links = m.node_tree.nodes, m.node_tree.links
    bs = nodes.get('Principled BSDF')
    coord = nodes.new('ShaderNodeTexCoord')
    noise = nodes.new('ShaderNodeTexNoise')
    noise.inputs['Scale'].default_value = 820 if key=='Rubber' else 1650
    noise.inputs['Detail'].default_value = 2
    links.new(coord.outputs['Object'], noise.inputs['Vector'])
    bump = nodes.new('ShaderNodeBump')
    bump.inputs['Strength'].default_value = .22 if key=='Rubber' else .13
    bump.inputs['Distance'].default_value = .000095 if key=='Rubber' else .00004
    links.new(noise.outputs['Fac'],bump.inputs['Height'])
    links.new(bump.outputs['Normal'],bs.inputs['Normal'])
    rough = nodes.new('ShaderNodeMapRange')
    r = bs.inputs['Roughness'].default_value
    rough.inputs['To Min'].default_value = r-.035
    rough.inputs['To Max'].default_value = r+.035
    links.new(noise.outputs['Fac'],rough.inputs['Value'])
    links.new(rough.outputs['Result'],bs.inputs['Roughness'])

# Reassign retained geometry to the richer but restrained material palette.
for o in PARTS:
    if o.data.materials:
        old = o.data.materials[0].name.removeprefix('AI_M7_').split('.')[0]
        if old in M:
            o.data.materials[0] = M[old]
    finish(o,.6 if 'Chassis' in o.name else .4)

progress('base retained, material finish updated')

# Stock: proper layered cheek piece, channelled struts, hinge and rubber tread.
drop('Stock_Rubber_Cheek','Stock_Length_Latch','Stock_External_Hinge','Stock_Axis_Exterior','Stock_Pivot')
rod('Stock_Exposed_Spine',p(98,252),p(153,252),6.3,'Black','01')
for j in range(6):
    ring('Stock_Adjustment_Band_%02d'%j,p(115+j*3,252),p(116+j*3,252),6.6,6.1,'Black_Hardware','01',48,.07)
poly('Stock_Hinge_Block',hexrect(133,239,144,263,2),10,-10,'Black','01',.55)
screw('Stock_Hinge_Axle',139,252,10.6,3,'01')
plate('Stock_Cheek_Inlay',[(19,240),(94,242),(102,247),(97,252),(34,251),(20,256)],11.8,1.2,'Desert','01',.35)
for sg in (-1,1):
    line('Stock_Cheek_Parting_'+str(sg),[(20,258,sg*11.9),(33,253,sg*11.9),(96,254,sg*11.9),(104,248,sg*11.9)],.28,'Sand_Dark','01')
    line('Stock_Strut_Channel_'+str(sg),[(24,297,sg*8.5),(88,276,sg*8.5)],.68,'Sand_Dark','01')
    poly('Stock_Length_Lever_'+str(sg),[(75,258),(104,253),(105,258),(83,268),(78,267)],sg*12,sg*10,'Black','01',.4)
    for j in range(6):
        line('Stock_Lever_Grip_%d_%s'%(j,sg),[(79+j*3,260,sg*12.4),(80+j*3,263,sg*12.4)],.32,'Black_Hardware','01')
    ring('Stock_Sling_Recess_'+str(sg),p(17,282,sg*10.1),p(17,282,sg*10.5),2.8,1.8,'Black_Hardware','01',40,.1)
    rod('Stock_Sling_Shadow_'+str(sg),p(17,282,sg*10.1),p(17,282,sg*10.25),1.78,'Recess','01',40,.02)
screw('Stock_Latch_Pivot',102,256,12,1.7,'01')
for j in range(11):
    box('Buttpad_Rubber_Tread_%02d'%j,(3.5,243+j*5.3,0),(6,1.5,26),'Rubber','01',.45)
for x,z in ((20,246),(92,247)):
    screw('Cheek_Captive_'+str(x),x,z,12.1,1.1,'01')
label('Stock','E-01   /   07',49,248,12.5,2,'Sand_Marking','01')

# Receiver: remove broad toy-like overlays, introduce thin cover edges and seams.
drop('Upper_Receiver_Slab','Magazine_Well_Facet','Receiver_Dark_Service_Inset',
     'Receiver_Metal_Cover','Receiver_Rear_Machinery_Seat','Receiver_Pin')
plate('Upper_Cover_Parting',[(151,235),(297,235),(305,240),(304,244),(212,244),(179,251),(155,248)],12.55,.35,'Sand_Dark','02',.18)
plate('Upper_Cover_Machined',[(153,235.5),(295,235.5),(302,240),(301,242.5),(212,242.5),(179,249),(156,246.5)],13.05,.85,'Desert','02',.32)
plate('Upper_Cover_Narrow_Facet',[(154,234),(296,234),(302,237),(155,237)],12.8,1.1,'Sand_Edge','02',.22)
for sg in (-1,1):
    line('Upper_Lower_Receiver_Seam_'+str(sg),[(153,260,sg*12.9),(179,268,sg*12.9),(229,268,sg*12.9),(240,264,sg*12.9),(300,264,sg*12.9)],.31,'Sand_Dark')
    line('Receiver_Tail_Rebate_'+str(sg),[(153,240,sg*12.8),(158,256,sg*12.8),(180,262,sg*12.8)],.29,'Sand_Dark')
    poly('Magwell_Shadow_'+str(sg),[(243,279),(296,274),(297,302),(245,310),(239,299)],sg*12.8,sg*12.5,'Sand_Dark','02',.3)
    poly('Magwell_Bevelled_Face_'+str(sg),[(246,280),(293,277),(294,301),(246,307),(242,298)],sg*13.1,sg*12.7,'Desert','02',.45)
    line('Magwell_Feed_Lip_'+str(sg),[(243,308,sg*13.3),(293,301,sg*13.3)],.4,'Sand_Edge')

# Asymmetric visible receiver covers with captive fasteners and a fine hinge line.
poly('Right_Port_Recess',hexrect(218,246,292,263,2),12.7,11.8,'Recess','02',.25)
poly('Right_Port_Internal_Shadow',hexrect(224,248,287,260,1.2),13,12.5,'Black','02',.2)
poly('Right_Port_Dust_Cover',hexrect(226,249,280,260,1),13.5,12.8,'Dark_Steel','02',.3)
poly('Right_Port_Cover_Rebate',hexrect(232,251,274,258,.6),13.68,13.4,'Black_Hardware','02',.15)
rod('Right_Port_Cover_Hinge',p(226,261,13.8),p(280,261,13.8),.65,'Black_Hardware','02',32,.05)
for j in range(5):
    ring('Cover_Hinge_Sleeve_%d'%j,p(229+j*10,261,13.8),p(231+j*10,261,13.8),.87,.63,'Dark_Steel','02',24,.03)
for x in (225,282):
    screw('Port_Captive_'+str(x),x,254,13.45,1.05,'02',False)
poly('Left_Service_Cover',hexrect(220,246,290,262,2),-12.9,-12.2,'Desert','02',.3)
for j in range(4):
    box('Left_Cooling_Rebate_%d'%j,(233+j*11,254,-13.03),(7,.65,.3),'Sand_Dark','02',.1)
plate('Receiver_Rear_Control_Plate',hexrect(166,252,205,265,2),13.1,.8,'Desert','02',.35)
for j,(x,z,r) in enumerate(((149,249,2),(179,275,2.2),(226,280,2),(299,280,2))):
    screw('Receiver_Captive_%02d'%j,x,z,13.5,r)
for sg in (-1,1):
    rod('Selector_Pivot_'+str(sg),p(184,281,sg*13.2),p(184,281,sg*14.7),3.2,'Black_Hardware','02',40,.2)
    poly('Selector_Lever_'+str(sg),[(183,280),(193,284),(193,287),(181,284)],sg*15.1,sg*14.3,'Black','02',.28)
    poly('Release_Button_Base_'+str(sg),hexrect(223,272,234,277,.8),sg*14,sg*12.8,'Black','02',.22)
    for j in range(4):
        line('Release_Button_Groove_%d_%s'%(j,sg),[(225+j*2,273,sg*14.15),(225+j*2,276,sg*14.15)],.17,'Dark_Steel')
for z in (272.4,287):
    label('Selector_Index_'+str(z),'•',181,z,14,2,'Marking')
label('Receiver_Title','E-01  /  M7',248,286,13.5,3,'Sand_Marking')
label('Receiver_Serial','AI INFANTRY  -  071',248,291,13.5,1.5,'Sand_Marking')
label('Receiver_Serial2','FIELD ASSET   /   02',248,294.5,13.5,1.25,'Sand_Marking')
for j in range(16):
    box('Receiver_Serial_Bar_%d'%j,(250+j*.75,299,13.5),(.25 if j%3 else .45,2.4,.04),'Sand_Marking','02',0)

# Grip panel with fine pressed texture, actual border and a restrained rear tread.
drop('Grip_Rubber_Inset','Grip_Heel')
plate('Grip_Panel_Seam',[(169,306),(191,310),(179,352),(156,346)],11.2,.7,'Recess','03',.7)
plate('Grip_Textured_Panel',[(170,308),(188,311),(177,349),(159,344)],11.6,.9,'Rubber','03',.6)
for sg in (-1,1):
    for j in range(10):
        z=313+j*3.1
        x=168-(z-313)*.29
        line('Grip_Fine_Texture_%d_%s'%(j,sg),[(x,z,sg*11.85),(x+16,z+4,sg*11.85)],.22,'Rubber','03')
poly('Grip_Endcap',[(153,349),(181,354),(181,359),(152,354)],11.4,-11.4,'Black','03',.55)
screw('Grip_Panel_Fastener',169,338,11.7,1.3,'03')

# Lengthened curved exterior magazine with pressed side flutes, witnesses and lip.
drop('Magazine')
magshape=[(239,308),(292,306),(297,349),(302,372),(310,394),(305,400),
          (258,408),(253,400),(247,372),(243,343)]
mag=poly('Magazine_Lengthened_Shell',magshape,11.4,-11.4,'Black','03',0)
for sg in (-1,1):
    shapes=[]
    for off in (0,16):
        shapes.append([(250+off,318),(259+off,317),(262+off,350),(267+off,378),
                       (271+off,391),(263+off,393),(259+off,380),(253+off,351)])
    cut(mag,shapes,sg*13,sg*10.8)
finish(mag,.42)
for sg in (-1,1):
    line('Magazine_Upper_Crimp_'+str(sg),[(243,313,sg*11.7),(290,311,sg*11.7)],.45,'Black_Hardware','03')
    line('Magazine_Back_Spine_'+str(sg),[(288,316,sg*11.7),(293,351,sg*11.7),(299,378,sg*11.7),(304,393,sg*11.7)],.65,'Black_Hardware','03')
    for j in range(3):
        z=340+j*17
        x=279+(z-340)*.17
        rod('Magazine_Witness_%s_%s'%(j,sg),p(x,z,sg*11.35),p(x,z,sg*11.58),1.2,'Recess','03',28,.05)
    for j in range(2):
        z=380+j*10
        line('Magazine_Lower_Reinforcement_%s_%s'%(j,sg),[(258+(z-380)*.4,z,sg*11.7),(296+(z-380)*.3,z-5,sg*11.7)],.8,'Black','03')
poly('Magazine_Floor_Lip',[(257,399),(308,390),(312,397),(308,403),(259,411),(256,406)],12.6,-12.6,'Black','03',.7)
plate('Magazine_Floor_Grip',[(260,403),(307,395),(308,400),(261,408)],13,.5,'Rubber','03',.25)
for sg in (-1,1):
    for j in range(8):
        line('Magazine_Floor_Texture_%s_%s'%(j,sg),[(263+j*5,403-j*.83,sg*13.1),(264+j*5,405-j*.83,sg*13.1)],.24,'Black_Hardware','03')
label('Magazine_Batch','07 / B',267,334,11.65,2.2,'Marking','03')
progress('receiver, stock, grip and lengthened magazine complete')

# Rebuild the long fore-end as a real chamfered sleeve with staggered apertures.
drop(group='04')
drop(group='05')
outer=[(307,234),(316,230),(526,230),(534,237),(534,269),(528,276),(311,276),(304,269)]
for sg in (-1,1):
    side=poly('Handguard_Machined_Shell_'+str(sg),outer,sg*16,sg*13.4,'Desert','04',0)
    holes=[]
    for j in range(9):
        x=325+j*22
        holes.append(hexrect(x,237.5,x+14,242,1.2))
        holes.append(hexrect(x-1,247.5,x+13.5,255,1.15,4))
        holes.append(hexrect(x-3,261,x+13,269,1.15,-4))
    cut(side,holes,sg*18,sg*11)
    finish(side,.29)
    line('Foreend_Longitudinal_Seam_'+str(sg),[(317,258.2,sg*16.2),(524,258.2,sg*16.2)],.2,'Sand_Dark','04')
    # Slightly inset reinforcement behind the exterior panel creates readable depth.
    line('Foreend_Upper_Rim_'+str(sg),[(319,232.5,sg*16.1),(525,232.5,sg*16.1)],.36,'Sand_Edge','04')
    line('Foreend_Lower_Rim_'+str(sg),[(315,273,sg*16.1),(527,273,sg*16.1)],.35,'Sand_Edge','04')
    for x,z in ((312,241),(312,267),(528,242),(528,265)):
        screw('Handguard_Captive_%s_%s_%s'%(x,z,sg),x,z,sg*16.3,1.35,'04',False)

# Upper and lower bevel walls bridge the two side shells.
for sg in (-1,1):
    mesh('Foreend_Upper_Bevel_'+str(sg),[p(313,230,sg*16),p(527,230,sg*16),p(527,226,sg*9),p(313,226,sg*9)],[(0,1,2,3)],'Desert','04',0)
    mesh('Foreend_Lower_Bevel_'+str(sg),[p(311,276,sg*16),p(529,276,sg*16),p(529,280,sg*8),p(311,280,sg*8)],[(0,1,2,3)],'Sand_Dark','04',0)
box('Handguard_Under_Spine',(419,278.5,0),(210,3,14),'Black','04',.32)
# A chamfered open end frame closes the shell edges around the barrel silhouette.
cap_outer=[(231,-9),(231,9),(237,15),(269,15),(277,8),(277,-8),(269,-15),(237,-15)]
cap_inner=[(239,-6),(239,6),(244,10),(264,10),(271,5),(271,-5),(264,-10),(244,-10)]
cap_vs=[p(x,z,d) for x,shape in ((529,cap_outer),(533,cap_outer),(529,cap_inner),(533,cap_inner)) for z,d in shape]
cap_fs=[]
for k in range(8):
    j=(k+1)%8
    cap_fs.extend([(k,j,8+j,8+k),(16+k,24+k,24+j,16+j),(k,16+k,16+j,j),(8+k,8+j,24+j,24+k)])
mesh('Handguard_Front_Return_Frame',cap_vs,cap_fs,'Sand_Dark','04',.35)
for j in range(12):
    box('Underside_Grip_Rib_%02d'%j,(333+j*12.5,280.4,0),(4,1.4,15),'Black_Hardware','04',.18)
poly('Foreend_Receiver_Clamp',[(305,230),(315,230),(318,236),(315,276),(308,280),(302,274)],14,-14,'Sand_Dark','04',.45)
for sg in (-1,1):
    poly('Foreend_Clamp_Side_'+str(sg),[(305,236),(312,233),(314,239),(312,272),(307,275),(304,269)],sg*17,sg*15,'Desert','04',.35)
    screw('Foreend_Root_Captive_'+str(sg),308.7,261,sg*17.4,1.6,'04',False)
rod('Foreend_Dark_Inner_Core',p(305,255),p(536,255),7.6,'Black','05',64,.15)
for j in range(18):
    ring('Interior_Visible_Rib_%02d'%j,p(323+j*10.5,255),p(324.3+j*10.5,255),8.0,7.5,'Black_Hardware','05',40,.08)

# Rail teeth have a shallow undercut profile instead of uniform cubes.
box('Continuous_Rail_Base',(333,227.5,0),(389,3.6,15),'Black','04',.28)
for j in range(55):
    x=142+j*7.05
    verts=[p(xx,z,d) for xx in (x,x+4.1) for z,d in ((227,-7),(224.6,-9.3),(222.8,-8.6),(222.8,8.6),(224.6,9.3),(227,7))]
    fs=[(5,4,3,2,1,0),(6,7,8,9,10,11)]+[(i,(i+1)%6,(i+1)%6+6,i+6) for i in range(6)]
    mesh('Rail_Cross_Tooth_%02d'%j,verts,fs,'Black_Hardware','04',.12)
for x in (171,206,326,368,411,453,495):
    rod('Rail_Recess_'+str(x),p(x,223.5,0),p(x,222.6,0),1.1,'Recess','04',24,.04)
for i,x in enumerate((329,371,413,455,497)):
    label('Rail_Index_'+str(i),'R'+str(i+1),x,233,16.4,1.25,'Sand_Marking','04')

# Exposed barrel extends to a short vented tip. This is exterior prop geometry.
lathe('Lengthened_Barrel_Exterior',[(530,7),(536,7),(538,5.1),(550,5.1),(551,4.7),
      (617,4.7),(619,5.5),(627,5.5),(628,5)],255,0,'Black')
for j in range(5):
    ring('Barrel_Base_Band_%02d'%j,p(539+j*2,255),p(539.7+j*2,255),5.25,4.8,'Black_Hardware','05',56,.05)
ring('Barrel_Shoulder',p(620,255),p(623,255),6.2,4.7,'Dark_Steel','05',64,.18)
ring('Barrel_Tip_Seat',p(626,255),p(632,255),6.1,4.5,'Black_Hardware','05',48,.16)
muzzle=ring('Long_Barrel_Muzzle_Exterior',p(632,255),p(657,255),6.6,3.4,'Black','05',64,0)
slots=[hexrect(637,251.7,650,254,1),hexrect(637,256,650,258.3,1)]
cut(muzzle,slots,8,-8)
finish(muzzle,.17)
ring('Muzzle_Front_Lip',p(655.5,255),p(658,255),6.8,3.8,'Black_Hardware','05',64,.18)
rod('Muzzle_Aperture_Shadow',p(647,255),p(647.3,255),3.3,'Recess','05',48,.04)
progress('staggered fore-end apertures and longer barrel complete')

# Entirely black fire-control optic: lenses, knurling, pods and low profile clamps.
drop(group='06')
for x in (212,278):
    poly('Optic_Dovetail_Clamp_'+str(x),[(x-9,221),(x-8,216),(x+8,216),(x+9,221),(x+6,224),(x-6,224)],11.5,-11.5,'Black','06',.4)
    poly('Optic_Riser_'+str(x),[(x-6,218),(x-6,207),(x+6,207),(x+6,218)],6.5,-6.5,'Black_Hardware','06',.45)
    screw('Optic_Clamp_Captive_'+str(x),x,220,12,1.9,'06')
lathe('Optic_Smooth_Tube',[(190,7.8),(206,7.8),(209,6.8),(279,6.8),(284,9),(298,10.8),(307,10.8)],202,0,'Black','06',80)
lathe('Optic_Ocular_Shroud',[(189,9.7),(192,10),(207,10),(211,8.1)],202,0,'Black','06',80)
ring('Optic_Ocular_Lip',p(187,202),p(190,202),10,7.1,'Black_Hardware','06',64,.18)
rod('Optic_Ocular_Glass',p(188.6,202),p(189,202),7,'Glass','06',64,.05)
for x in (193,196,199,203,207):
    ring('Ocular_Fine_Band_'+str(x),p(x,202),p(x+.6,202),10.15,9.7,'Rubber','06',64,.06)
lathe('Optic_Focus_Collar',[(214,7.4),(216,9.2),(225,9.2),(227,7.4)],202,0,'Black_Hardware','06',64)
for j in range(40):
    a=j*math.tau/40
    z,d=202+9.2*math.sin(a),9.2*math.cos(a)
    rod('Focus_Knurl_%02d'%j,p(216.5,z,d),p(224.5,z,d),.25,'Black','06',12,.05)
ring('Optic_Objective_Armored_Hood',p(299,202),p(319,202),12.3,10.2,'Black','06',80,.22)
ring('Optic_Objective_Lip',p(317.7,202),p(320,202),12.5,10.5,'Black_Hardware','06',80,.14)
ring('Optic_Objective_Internal_Seat',p(306,202),p(309,202),10.3,9.5,'Black_Hardware','06',64,.08)
rod('Optic_Objective_Lens',p(308,202),p(308.25,202),9.45,'Glass','06',80,.02)
ring('Optic_Lens_Coated_Edge',p(308.3,202),p(308.45,202),9.45,9.12,'Lens_Coating','06',80,.03)
for x in (302,314):
    ring('Objective_Hood_Rebate_'+str(x),p(x,202),p(x+.5,202),12.4,12.05,'Rubber','06',64,.04)
for sg in (-1,1):
    line('Objective_Hood_Side_Line_'+str(sg),[(303,202,sg*12.4),(314,202,sg*12.4)],.26,'Black_Hardware','06')

pod=[(232,188),(269,188),(279,194),(279,207),(273,213),(233,213),(227,207),(227,194)]
poly('FCU_Main_Armored_Body',pod,17,5,'Black','06',.65)
poly('FCU_Panel_Gasket',hexrect(231,191,275,210,2),17.35,16.9,'Rubber','06',.22)
poly('FCU_Black_Service_Panel',hexrect(232,192,274,209,1.7),17.7,17.1,'Black_Hardware','06',.3)
poly('FCU_Display_Recess',hexrect(240,195,261,203,1),17.95,17.6,'Recess','06',.15)
poly('FCU_Display_Glass',hexrect(241,196,260,202,.6),18.05,17.8,'Glass','06',.08)
label('FCU_Display','FC / 07',243,200.5,18.15,2.2,'Display','06')
label('FCU_Designation','FIRE CONTROL',237,207,17.96,1.15,'Marking','06')
for z in (197,201,205):
    box('FCU_Button_'+str(z),(266.5,z,18),(4.1,2.2,1),'Rubber','06',.28)
for x,z in ((234,194),(272,194),(234,207.5),(272,207.5)):
    screw('FCU_Panel_Captive_%s_%s'%(x,z),x,z,18,1.0,'06',False)
for j in range(5):
    box('FCU_Cooling_Notch_%02d'%j,(235+j*6.3,189.3,17.2),(3.2,.65,.35),'Recess','06',.15)
lathe('FCU_Auxiliary_Sensor_Housing',[(277,3.1),(284,3.7),(291,3.7)],198,12.2,'Black','06',48)
ring('FCU_Sensor_Rim',p(289.7,198,12.2),p(292,198,12.2),3.9,2.7,'Black_Hardware','06',48,.12)
rod('FCU_Sensor_Glass',p(290.7,198,12.2),p(291,198,12.2),2.65,'Glass','06',48,.03)

# Top turret uses a circular cap with radial knurling and discreet etched ticks.
rod('Optic_Turret_Base',p(251,197),p(251,187),5.6,'Black','06',64,.25)
rod('Optic_Turret_Knob',p(251,188),p(251,181),6.5,'Black_Hardware','06',64,.22)
rod('Optic_Turret_Cap',p(251,181.4),p(251,180),5.6,'Black','06',64,.16)
for j in range(32):
    a=j*math.tau/32
    x,d=251+6.5*math.cos(a),6.5*math.sin(a)
    rod('Turret_Knurl_%02d'%j,p(x,182,d),p(x,187,d),.3,'Black','06',12,.06)
for j in range(8):
    a=j*math.tau/8
    line('Turret_Tick_%d'%j,[(251+4.1*math.cos(a),179.95,4.1*math.sin(a)),(251+5*math.cos(a),179.95,5*math.sin(a))],.12,'Marking','06')
line('FCU_Protected_Cable',[(234,212,9),(233,215,8),(241,216,8),(246,213,7)],.7,'Rubber','06')
progress('all-black fire-control optic rebuilt')

# Shared render studio. Gray camera backdrop makes black components easy to inspect.
sc.render.engine='CYCLES'
sc.cycles.samples=64
sc.cycles.use_denoising=True
try:
    pref=bpy.context.preferences.addons['cycles'].preferences
    pref.compute_device_type='OPTIX'
    pref.get_devices()
    gpu_found=False
    for device in pref.devices:
        device.use=device.type=='OPTIX'
        gpu_found |= device.use
    sc.cycles.device='GPU' if gpu_found else 'CPU'
except Exception:
    sc.cycles.device='CPU'
sc.render.image_settings.file_format='PNG'
sc.render.image_settings.color_mode='RGBA'
sc.render.film_transparent=False
sc.view_settings.view_transform='AgX'
sc.view_settings.look='AgX - Medium High Contrast'
sc.view_settings.exposure=0
world=bpy.data.worlds.new('Stage02_Neutral_Studio')
world.use_nodes=True
sc.world=world
n,l=world.node_tree.nodes,world.node_tree.links
n.clear()
out=n.new('ShaderNodeOutputWorld')
env=n.new('ShaderNodeBackground');env.inputs['Color'].default_value=(.42,.46,.52,1);env.inputs['Strength'].default_value=.38
back=n.new('ShaderNodeBackground');back.inputs['Color'].default_value=(.115,.14,.17,1);back.inputs['Strength'].default_value=.8
mix=n.new('ShaderNodeMixShader');ray=n.new('ShaderNodeLightPath')
l.new(ray.outputs['Is Camera Ray'],mix.inputs[0]);l.new(env.outputs[0],mix.inputs[1]);l.new(back.outputs[0],mix.inputs[2]);l.new(mix.outputs[0],out.inputs[0])

def light(name,loc,power,size,col,shape='DISK'):
    data=bpy.data.lights.new(name,'AREA');data.energy=power;data.shape=shape;data.size=size;data.color=col
    ob=bpy.data.objects.new(name,data);cols['07'].objects.link(ob);ob.location=loc
    ob.rotation_euler=(Vector((.06,0,-.02))-ob.location).to_track_quat('-Z','Y').to_euler()
light('Large_Softbox',(-.65,-1.6,1.6),210,2.0,(1,.96,.9))
light('Front_Strip',(1.3,-1.4,.45),120,1.8,(.93,.97,1))
light('Edge_Softbox',(.7,.9,1.4),250,1.6,(.85,.92,1))
light('Rear_Gentle_Fill',(-1.4,.1,.5),70,1.6,(1,.94,.84))

def cam(name,loc,target,width):
    data=bpy.data.cameras.new(name);data.type='ORTHO';data.ortho_scale=width
    ob=bpy.data.objects.new(name,data);cols['07'].objects.link(ob);ob.location=loc
    ob.rotation_euler=(Vector(target)-ob.location).to_track_quat('-Z','Y').to_euler()
    return ob
center=Vector((.082,0,-.028))
cam('SIDE',center+Vector((0,-5,0)),center,2.20)
cam('THREE_QUARTER',center+Vector((1.7,-4.6,1.6)),center,2.20)
cam('OPPOSITE_SIDE',center+Vector((0,5,0)),center,2.20)
cam('TOP',center+Vector((0,0,5)),center,2.20)
cam('FRONT',(5,0,-.07),(0,0,-.07),.90)
detail_target=p(249,226,0)
cam('OPTIC_DETAIL',detail_target+Vector((.46,-1.5,.64)),detail_target,.66)
detail2=p(420,254,0)
cam('HANDGUARD_DETAIL',detail2+Vector((.7,-1.8,.75)),detail2,.90)

im=bpy.data.images.load(str(P/'references/M7_reference.png'));im.pack()
sc['reference_image']=im.name
sc['approval_checkpoint']='Stage 02 visual feedback before character fit and game export'
sc.camera=bpy.data.objects['THREE_QUARTER']
sc.render.resolution_x=2600;sc.render.resolution_y=1200;sc.render.resolution_percentage=100
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            area.spaces.active.shading.type='MATERIAL'
            area.spaces.active.region_3d.view_location=center
            area.spaces.active.region_3d.view_distance=2.55
            area.spaces.active.region_3d.view_rotation=sc.camera.rotation_euler.to_quaternion()
            area.spaces.active.overlay.show_overlays=False
bpy.ops.object.select_all(action='DESELECT')
root.select_set(True);bpy.context.view_layer.objects.active=root
bpy.context.preferences.filepaths.save_version=0
asset=OUT/'AI_INFANTRY_M7_STAGE02.blend'
bpy.ops.wm.save_as_mainfile(filepath=str(asset))
optic_materials=sorted({m.name for ob in cols['06'].objects if hasattr(ob.data,'materials') for m in ob.data.materials})
manifest={'stage':2,'status':'rendered for user feedback','source':'stage_01 kept unchanged',
          'changes':['longer exposed barrel','longer magazine','black-only optic exterior','machined handguard apertures','receiver and stock detailing','fine surface finish'],
          'shape_units_are_arbitrary':True,'barrel_tip_before':598,'barrel_tip_after':658,
          'handguard_front_before':527,'handguard_front_after':534,'magazine_bottom_before':383,'magazine_bottom_after':411,
          'optic_materials':optic_materials,
          'parts':len(root.children),'mesh_parts':sum(o.type=='MESH' for o in root.children),
          'pending':['user appearance feedback','character grip fit','game asset exports']}
(OUT/'stage02_manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
progress('editable master saved: '+str(asset))
for name in ('THREE_QUARTER','SIDE','OPTIC_DETAIL','HANDGUARD_DETAIL','OPPOSITE_SIDE','TOP','FRONT'):
    sc.camera=bpy.data.objects[name]
    if name in ('OPTIC_DETAIL','HANDGUARD_DETAIL'):
        sc.render.resolution_x=2200;sc.render.resolution_y=1500
    elif name=='FRONT':
        sc.render.resolution_x=1100;sc.render.resolution_y=1500
    else:
        sc.render.resolution_x=2600;sc.render.resolution_y=1200
    sc.render.filepath=str(OUT/'renders'/(name+'.png'))
    bpy.ops.render.render(write_still=True)
    progress('rendered '+name)
progress('COMPLETE')
