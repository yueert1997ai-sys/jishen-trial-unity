"""Reference-led TYPE-01 fictional laser rifle. All dimensions are visual asset units.
Side-sheet coordinates: left muzzle, right stock, downward image Y. No firearm internals.
"""
import bpy, bmesh, math, pathlib, json, hashlib
from mathutils import Vector, Matrix

D = pathlib.Path(__file__).resolve().parent
REF = D/'references/TYPE01_LASER_RIFLE_REFERENCE.png'
bpy.ops.wm.read_factory_settings(use_empty=True)
sc = bpy.context.scene
sc.unit_settings.system = 'METRIC'
sc.unit_settings.length_unit = 'METERS'
bpy.context.preferences.filepaths.temporary_directory = 'D:/Tools/BlenderUserData/Temp'
S = .89 / 767.0
COL = {}; M = {}; PARTS = []; GROUPS = {}
for key, name in [('01','Receiver'),('02','Vented handguard'),('03','Emitter'),('04','Optic'),
                  ('05','Energy cell'),('06','Stock'),('07','Grip and controls'),('08','Markings'),('09','Sockets'),('10','Studio')]:
    c = bpy.data.collections.new('TYPE01 | '+name)
    sc.collection.children.link(c); COL[key] = c
root = bpy.data.objects.new('TYPE01_LASER_RIFLE_ROOT', None)
COL['09'].objects.link(root)
root.empty_display_size = .045
root['asset'] = 'TYPE-01 / AI infantry laser rifle'
root['reference_length_m'] = .89
root['coordinate_system'] = 'Muzzle -X; display side -Y; up +Z. Uniform root scale for mech use.'
root['purpose'] = 'Fictional game prop; external visual geometry only.'

def p(x,z,d=0):
    return Vector(((x-559)*S, -d*S, (542-z)*S))

def group(key, name, origin):
    o = bpy.data.objects.new('TYPE01_'+name, None); COL[key].objects.link(o)
    o.parent = root; o.location = p(*origin); o.empty_display_size = .022
    GROUPS[key] = o
    return o

group('01','RECEIVER',(490,490,0)); group('02','HANDGUARD',(300,489,0))
group('03','EMITTER',(136,489,0)); group('04','OPTIC',(443,450,0))
magroot = group('05','REMOVABLE_ENERGY_CELL',(433,543,0))
stockroot = group('06','ADJUSTABLE_STOCK',(622,490,0))
group('07','GRIP_AND_CONTROLS',(550,539,0))
magroot['remove_direction'] = 'Local -Z'
stockroot['extension_direction'] = 'Local +X'

def material(key, color, metal=.65, rough=.34, noise=0, emission=0):
    m = bpy.data.materials.new('T01_'+key); m.use_nodes = True
    rgb = [int(color[i:i+2],16)/255 for i in (0,2,4)]
    rgb = [c/12.92 if c<=.04045 else ((c+.055)/1.055)**2.4 for c in rgb]
    m.diffuse_color = (*rgb,1); m.metallic = metal; m.roughness = rough
    nt = m.node_tree; bs = next(n for n in nt.nodes if n.type=='BSDF_PRINCIPLED')
    bs.inputs['Base Color'].default_value=(*rgb,1)
    bs.inputs['Metallic'].default_value=metal; bs.inputs['Roughness'].default_value=rough
    if noise:
        tex = nt.nodes.new('ShaderNodeTexCoord'); tex.object=root
        n = nt.nodes.new('ShaderNodeTexNoise'); n.inputs['Scale'].default_value=noise
        n.inputs['Detail'].default_value=2.5; n.inputs['Roughness'].default_value=.68
        nt.links.new(tex.outputs['Object'], n.inputs['Vector'])
        ramp = nt.nodes.new('ShaderNodeValToRGB')
        ramp.color_ramp.elements[0].position=.15; ramp.color_ramp.elements[1].position=.85
        ramp.color_ramp.elements[0].color=(max(.03,rough-.10),)*3+(1,)
        ramp.color_ramp.elements[1].color=(min(.9,rough+.10),)*3+(1,)
        nt.links.new(n.outputs['Fac'],ramp.inputs[0]); nt.links.new(ramp.outputs[0],bs.inputs['Roughness'])
        color_ramp=nt.nodes.new('ShaderNodeValToRGB')
        color_ramp.color_ramp.elements[0].position=.18
        color_ramp.color_ramp.elements[1].position=.82
        color_ramp.color_ramp.elements[0].color=tuple(c*.84 for c in rgb)+(1,)
        color_ramp.color_ramp.elements[1].color=tuple(min(1,c*1.12) for c in rgb)+(1,)
        nt.links.new(n.outputs['Fac'],color_ramp.inputs[0]);nt.links.new(color_ramp.outputs[0],bs.inputs['Base Color'])
        bump=nt.nodes.new('ShaderNodeBump'); bump.inputs['Strength'].default_value=.17
        bump.inputs['Distance'].default_value=.000065 if key=='Polymer' else .000018
        nt.links.new(n.outputs['Fac'],bump.inputs['Height']); nt.links.new(bump.outputs[0],bs.inputs['Normal'])
    if emission:
        bs.inputs['Emission Color'].default_value=(*rgb,1)
        bs.inputs['Emission Strength'].default_value=emission
    M[key]=m; return m

material('Gunmetal','444B50',.78,.32,760)
material('Armor','5B6062',.72,.38,660)
material('Panel','353C41',.70,.38,840)
material('Steel','8B9499',.87,.27,950)
material('DarkSteel','232A30',.78,.34,950)
material('Recess','090D10',.20,.62)
material('Polymer','242B30',.05,.59,2300)
material('Brass','877958',.78,.36,600)
material('Lettering','CAD0CF',.08,.52)
material('Cyan','18C7DE',.20,.25,emission=3)
material('CyanCore','A3F8FF',.10,.18,emission=4)
material('OpticGlass','12394A',.55,.12)

def mesh(name,vs,fs,mat='Gunmetal',key='01',bevel=.30,smooth=False):
    me=bpy.data.meshes.new('T01_'+name+'_Mesh'); me.from_pydata(vs,[],fs); me.update()
    bm=bmesh.new(); bm.from_mesh(me)
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces)); bm.to_mesh(me); bm.free()
    o=bpy.data.objects.new('T01_'+name,me); COL[key].objects.link(o)
    par=GROUPS.get(key,root); o.parent=par
    o.matrix_parent_inverse=Matrix.Translation(-par.location) if par!=root else Matrix.Identity(4)
    me.materials.append(M[mat])
    if smooth:
        for f in me.polygons:
            if len(f.vertices)==4:f.use_smooth=True
    if bevel:
        b=o.modifiers.new('Machined edge','BEVEL'); b.width=bevel*S; b.segments=2; b.limit_method='ANGLE'; b.harden_normals=True
        n=o.modifiers.new('Weighted normals','WEIGHTED_NORMAL'); n.keep_sharp=True; n.weight=50
    o['part_role']=name; PARTS.append(o); return o

def poly(name,pts,front,back,mat='Gunmetal',key='01',bevel=.30):
    n=len(pts); vs=[p(x,z,d) for d in (front,back) for x,z in pts]
    fs=[tuple(range(n-1,-1,-1)),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    return mesh(name,vs,fs,mat,key,bevel)

def box(name,x,z,l,h,depth,mat='Gunmetal',key='01',d=0,bevel=.30):
    return poly(name,[(x-l/2,z-h/2),(x+l/2,z-h/2),(x+l/2,z+h/2),(x-l/2,z+h/2)],d+depth/2,d-depth/2,mat,key,bevel)

def panel(name,pts,d,thick,mat='Panel',key='01',both=True,bevel=.25):
    return [poly(name+('_L' if s==1 else '_R'),pts,s*d,s*(d-thick),mat,key,bevel) for s in ((1,-1) if both else (1,))]

def rod(name,a,b,r,mat='Steel',key='01',n=32,r2=None,bevel=.16):
    a,b=Vector(a),Vector(b);q=(b-a).to_track_quat('Z','Y')
    vs=[pt+q@Vector((rr*S*math.cos(i*math.tau/n),rr*S*math.sin(i*math.tau/n),0))
        for pt,rr in ((a,r),(b,r if r2 is None else r2)) for i in range(n)]
    fs=[tuple(range(n-1,-1,-1)),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    return mesh(name,vs,fs,mat,key,bevel,True)

def ring(name,a,b,r,ri,mat='Gunmetal',key='01',n=48,bevel=.12):
    a,b=Vector(a),Vector(b);q=(b-a).to_track_quat('Z','Y')
    vs=[pt+q@Vector((rr*S*math.cos(i*math.tau/n),rr*S*math.sin(i*math.tau/n),0))
        for pt,rr in ((a,r),(b,r),(a,ri),(b,ri)) for i in range(n)]
    fs=[]
    for i in range(n):
        j=(i+1)%n
        fs.extend([(i,j,n+j,n+i),(2*n+i,3*n+i,3*n+j,2*n+j),
                   (i,2*n+i,2*n+j,j),(n+i,n+j,3*n+j,3*n+i)])
    return mesh(name,vs,fs,mat,key,bevel,True)

def bolt(name,x,z,d,r=1.35,key='01',both=True,mat='Steel'):
    for s in ((1,-1) if both else (1,)):
        ring(name+('_L' if s==1 else '_R')+'_Seat',p(x,z,s*(d-.12)),p(x,z,s*(d+.15)),r*1.25,r*.75,'Recess',key,20,.02)
        ring(name+('_L' if s==1 else '_R')+'_Hex',p(x,z,s*d),p(x,z,s*(d+.7)),r,r*.40,mat,key,6,.035)

def trim(name,coords,d,width,mat='Steel',key='01',both=True):
    for i,(a,b) in enumerate(zip(coords,coords[1:])):
        a,b=Vector(a),Vector(b);v=(b-a).normalized();w=Vector((-v.y,v.x))*width/2
        panel(name+'_'+str(i),[a+w,b+w,b-w,a-w],d,.30,mat,key,both,.07)

def cut(target,holes,name):
    vs=[];fs=[]
    for pts in holes:
        n=len(pts);b=len(vs)
        vs.extend(p(x,z,d) for d in (-80,80) for x,z in pts)
        fs.extend([tuple(b+i for i in range(n-1,-1,-1)),tuple(b+i for i in range(n,2*n))])
        fs.extend((b+i,b+(i+1)%n,b+(i+1)%n+n,b+i+n) for i in range(n))
    cutter=mesh('TEMP_'+name,vs,fs,'Recess','01',0)
    bpy.context.view_layer.update();bpy.context.view_layer.objects.active=target
    mod=target.modifiers.new(name,'BOOLEAN');mod.operation='DIFFERENCE';mod.solver='EXACT';mod.object=cutter
    while target.modifiers.find(mod.name)>0:bpy.ops.object.modifier_move_up(modifier=mod.name)
    bpy.ops.object.modifier_apply(modifier=mod.name)
    PARTS.remove(cutter);bpy.data.objects.remove(cutter,do_unlink=True)

def cable(name,coords,r=1.7,mat='DarkSteel',key='01'):
    cu=bpy.data.curves.new(name,'CURVE');cu.dimensions='3D';cu.resolution_u=8;cu.bevel_resolution=2;cu.bevel_depth=r*S
    sp=cu.splines.new('BEZIER');sp.bezier_points.add(len(coords)-1)
    for a,c in zip(sp.bezier_points,coords):a.co=p(*c);a.handle_left_type='AUTO';a.handle_right_type='AUTO'
    o=bpy.data.objects.new('T01_'+name,cu);COL[key].objects.link(o);o.parent=GROUPS.get(key,root)
    if o.parent!=root:o.matrix_parent_inverse=Matrix.Translation(-o.parent.location)
    cu.materials.append(M[mat]);PARTS.append(o);return o

def label(name,text,x,z,d,size=3,mat='Lettering',key='01',both=True):
    for s in ((1,-1) if both else (1,)):
        cu=bpy.data.curves.new(name,'FONT');cu.body=text;cu.size=size*S;cu.extrude=.015*S;cu.align_x='CENTER'
        ob=bpy.data.objects.new('T01_'+name+('_L' if s==1 else '_R'),cu);COL[key].objects.link(ob)
        ob.parent=GROUPS.get(key,root)
        if ob.parent!=root:ob.matrix_parent_inverse=Matrix.Translation(-ob.parent.location)
        ob.location=p(x,z,s*d);ob.rotation_euler=(math.pi/2,0,0 if s==1 else math.pi)
        cu.materials.append(M[mat]);PARTS.append(ob)

# Main receiver and front structural spine, stepped in plan as well as side elevation.
poly('Receiver_Core',[(376,465),(391,460),(591,464),(618,473),(621,505),(611,515),(480,521),(472,536),(397,534),(388,510)],15,-15,'DarkSteel','01',.65)
poly('Upper_Receiver_Shell',[(386,459),(577,461),(605,466),(616,476),(600,483),(404,483),(385,476)],17,-17,'Gunmetal','01',.5)
poly('Lower_Receiver',[(397,503),(480,507),(504,513),(545,513),(563,529),(555,550),(538,551),(534,526),(480,526),(468,544),(399,543)],14,-14,'Gunmetal','01',.5)
side_pts=[(480,482),(491,476),(593,479),(612,483),(610,510),(598,517),(477,517),(473,509)]
panel('Receiver_Cover_Seal',side_pts,17.1,1.5,'Recess',bevel=.45)
panel('Receiver_Cover',[(483,483),(492,479),(593,482),(608,486),(607,508),(596,514),(480,514),(477,508)],19,2,'Armor',bevel=.42)
trim('Receiver_Inset_Perimeter',[(489,486),(590,486),(599,491),(598,509),(484,509)],19.1,.42,'Panel')
for i,(x,z) in enumerate([(484,485),(604,488),(601,508),(481,510),(396,468),(588,468),(467,518),(408,536)]):bolt('Receiver_Fixing_'+str(i),x,z,19.2,1.35)
label('Receiver_ID','01.',573,503,19.3,10)
label('Receiver_Name','TYPE-01 / LASER RIFLE',540,488,19.2,2.45)
label('Receiver_Serial','AI-019 / 8E 042',579,510.5,19.3,1.9)
box('Rear_Receiver_Seam',616,492,3,33,35,'Recess',bevel=.4)
box('Rear_Receiver_Lock',621,491,6,26,37,'Gunmetal',bevel=.5)

# An actual perforated cage, with air gaps around a separate internal laser conduit.
rod('Inner_Laser_Conduit',p(135,489),p(402,489),9.4,'DarkSteel','02',48)
for i in range(14):
    x=163+i*15
    ring('Inner_Heat_Sink_'+str(i),p(x,489),p(x+2.4,489),13.1,9.1,'Gunmetal','02',32,.1)
box('Handguard_Spine',266,460,240,7,29,'DarkSteel','02',bevel=.5)
box('Handguard_Lower_Spine',267,517,239,7,24,'DarkSteel','02',bevel=.4)
outer=[(148,474),(165,460),(356,458),(380,465),(395,475),(392,509),(377,521),(149,521),(143,515),(143,484)]
shells=panel('Perforated_Handguard',outer,20,3.5,'Gunmetal','02',bevel=.28)
holes=[]
for i in range(9):
    x=164+i*23
    holes.append([(x+3,466),(x+19,466),(x+15,474),(x,474)])
for i in range(11):
    x=150+i*20.5
    holes.append([(x+7,512),(x+18,512),(x+10,518),(x,518)])
for z in (484,496):
    for x in (155,179,203,227):holes.append([(x,z),(x+17,z),(x+19,z+7),(x,z+7)])
holes.extend([[(258,485),(293,482),(296,490),(258,494)],[(304,481),(344,479),(350,486),(306,492)],[(360,480),(377,480),(377,500),(360,500)]])
for ob in shells:cut(ob,holes,'Open cooling slots')
panel('Handguard_Front_Collar',[(142,478),(150,470),(155,474),(152,514),(146,519),(141,514)],22,5,'Armor','02')
panel('Upper_Guard_Reinforcement',[(158,477),(252,477),(250,481),(156,482)],22,3,'Armor','02')
panel('Lower_Guard_Reinforcement',[(154,508),(252,508),(264,503),(350,493),(350,498),(266,509),(253,514),(154,514)],22.1,2.8,'Armor','02')
panel('Power_Service_Cartridge',[(257,492),(296,488),(298,498),(257,502)],20.6,3,'Panel','02')
for s in (-1,1):
    box('Handguard_Cyan_Bus_'+str(s),230,479.5,74,1.15,1,'Cyan','02',s*22.25,.25)
    box('Inner_Power_Indicator_'+str(s),280,490,14,1.7,1,'Cyan','02',s*21.0,.2)
    box('Side_Laser_Inspection_'+str(s),367,490,7,14,1.4,'Recess','02',s*21.0,.5)
    box('Inspection_Cyan_Insert_'+str(s),367,489,4.2,10,1,'Cyan','02',s*21.7,.5)
for i,(x,z) in enumerate([(149,480),(149,510),(164,464),(254,474),(352,471),(383,476),(384,506),(253,513),(333,506)]):bolt('Guard_Fastener_'+str(i),x,z,22.5,1.1,'02',mat='Brass' if i in (2,6) else 'Steel')

# Receiver-side energy observation window, articulated frame and protected conduits.
window=[(402,480),(413,475),(467,477),(473,483),(471,504),(461,510),(404,507),(398,502)]
panel('Energy_Chamber_Housing',window,21,7,'Armor')
for s in (-1,1):
    box('Chamber_Recess_'+str(s),436,492,59,19,3,'Recess','01',s*22.3,1.1)
    box('Chamber_Inner_Glass_'+str(s),435,491.5,54,13,1,'OpticGlass','01',s*24,1)
    box('Chamber_Light_Base_'+str(s),435,491.6,49,5,1,'Cyan','01',s*24.8,.45)
    for i in range(9):box('Chamber_Segment_'+str(s)+'_'+str(i),414+i*5.4,491.6,3.8,5.4,1,'CyanCore','01',s*25.35,.22)
    box('Chamber_Upper_Frame_'+str(s),435,480,54,4,4,'Steel','01',s*24.5,.65)
    box('Chamber_Lower_Frame_'+str(s),435,505,54,4,4,'Gunmetal','01',s*24.5,.65)
    for x in (402,470):box('Chamber_End_Latch_'+str(s)+'_'+str(x),x,492,5,25,5,'Gunmetal','01',s*25,1)
for i,(x,z) in enumerate([(404,480),(469,481),(402,504),(467,505)]):bolt('Chamber_Frame_Pin_'+str(i),x,z,27,1.1)
label('Energy_Inspection_Caption','H.E.L.S / POWER MONITOR',437,475,22.5,2.2)
panel('Forward_Power_Coupler',[(375,476),(393,476),(401,484),(397,500),(375,501)],24,8,'Panel')
bolt('Power_Coupler_Fixing',385,485,25,2.3)
for s in (-1,1):
    cable('Lower_High_Voltage_Loop_'+str(s),[(344,507,s*17),(354,519,s*25),(367,526,s*27),(385,526,s*27),(400,514,s*18)],3.0)
    cable('Secondary_Return_'+str(s),[(374,505,s*16),(391,522,s*26),(413,523,s*25),(423,513,s*19)],2.1)
    for i in range(3):
        x=358+i*10
        ring('Cable_Clamp_'+str(s)+'_'+str(i),p(x,524,s*27),p(x+3,526,s*27),4.0,3.1,'Steel','01',20,.06)

# Long top rail, with separate teeth and a real stepped dovetail section.
poly('Rail_Dovetail_Base',[(151,459),(153,453),(597,453),(611,461),(599,465),(165,464)],12,-12,'DarkSteel','01',.3)
for i in range(41):
    x=159+i*10.4
    box('Rail_Tooth_%02d'%i,x,452.3,6.3,4.6,27,'Gunmetal','01',bevel=.35)
    box('Rail_Tooth_Top_%02d'%i,x,450.5,5.3,1.2,26.3,'Gunmetal','01',bevel=.15)
for x in (153,602):
    box('Rail_End_Stop_'+str(x),x,453,7,8,30,'Panel','01',bevel=.8)
    bolt('Rail_End_Fixing_'+str(x),x,454,16,2.2)

# Large cylindrical emitter: nested sleeve, recessed aperture, collars and cooling grooves.
ring('Emitter_Main_Shroud',p(32,489),p(123,489),18.1,15.2,'Armor','03',64,.35)
ring('Emitter_Back_Sleeve',p(116,489),p(130,489),18.6,13.8,'Gunmetal','03',64,.30)
ring('Emitter_Rear_Seal',p(125,489),p(129,489),18.7,13.8,'Recess','03',64,.12)
ring('Emitter_Cyan_Coupling',p(130,489),p(132,489),17.4,12.8,'Cyan','03',64,.18)
ring('Emitter_Machined_Coupling',p(132,489),p(140,489),16.8,11.5,'Steel','03',64,.25)
ring('Emitter_Neck',p(139,489),p(148,489),12.8,9.8,'DarkSteel','03',48,.3)
ring('Emitter_Front_Bumper',p(26,489),p(34,489),19.2,14.4,'DarkSteel','03',64,.55)
ring('Emitter_Mouth_Rim',p(25,489),p(27,489),17.9,14.5,'Steel','03',64,.18)
ring('Emitter_Inner_Tunnel',p(28,489),p(48,489),14.4,11.5,'Recess','03',64,.18)
ring('Emitter_Aperture_Cyan',p(26.7,489),p(27.8,489),13.6,11.7,'Cyan','03',64,.12)
rod('Emitter_Recessed_Lens',p(30,489),p(32,489),11.6,'OpticGlass','03',64,.15)
rod('Emitter_Laser_Core',p(29.1,489),p(29.6,489),10.3,'Cyan','03',64,.12)
for x in (36,112,121):ring('Emitter_Circumferential_Seam_'+str(x),p(x,489),p(x+.8,489),18.25,17.7,'Panel','03',64,.05)
for s in (-1,1):
    box('Emitter_Side_Slot_Seal_'+str(s),69,494,32,4,1.6,'Recess','03',s*17.5,.9)
    box('Emitter_Side_Power_Strip_'+str(s),69,494,28,1.7,1.2,'Cyan','03',s*18.4,.45)
for i in range(4):
    a=i*math.tau/4+math.pi/4;d=17.8*math.cos(a);zz=489-17.8*math.sin(a)
    box('Emitter_Front_Lock_'+str(i),30,zz,6,4.2,4,'Gunmetal','03',d,.7)
for x in (39,110):
    bolt('Emitter_Side_Captive_'+str(x),x,486,18.5,1.1,'03',mat='Brass')
for i in range(24):
    a=i*math.tau/24
    rod('Emitter_Collar_Grip_'+str(i),p(119,489-18.6*math.sin(a),18.6*math.cos(a)),p(123,489-18.6*math.sin(a),18.6*math.cos(a)),.34,'DarkSteel','03',8,bevel=0)

# Telescopic optic and two independent rail mounts.
for x in (402,484):
    box('Optic_Rail_Clamp_'+str(x),x,447,29,7,32,'Gunmetal','04',bevel=.7)
    poly('Optic_Riser_'+str(x),[(x-10,444),(x-7,426),(x+9,426),(x+11,444)],10,-10,'Panel','04',.6)
    bolt('Optic_Mount_Lock_'+str(x),x,444,18,2.5,'04',mat='Brass')
rod('Optic_Main_Tube',p(363,423),p(515,423),8.2,'DarkSteel','04',64,.0 if False else None)
rod('Optic_Objective_Bell',p(339,423),p(370,423),12.1,'Gunmetal','04',64,r2=10.3,bevel=.4)
ring('Optic_Objective_Hood',p(333,423),p(343,423),12.6,9.4,'Panel','04',64,.4)
ring('Optic_Objective_Inner',p(334,423),p(341,423),9.3,8.3,'Steel','04',64,.1)
rod('Optic_Objective_Lens',p(338,423),p(338.6,423),8.3,'OpticGlass','04',64,.0 if False else None)
ring('Optic_Objective_Cyan_Catchlight',p(336,423),p(336.5,423),8.5,7.7,'Cyan','04',64,.06)
rod('Optic_Ocular_Bell',p(493,423),p(548,423),10.3,'Gunmetal','04',64,r2=13.5,bevel=.45)
ring('Optic_Ocular_Cap',p(546,423),p(557,423),14.1,11.1,'Panel','04',64,.45)
rod('Optic_Ocular_Lens',p(553,423),p(553.7,423),11,'OpticGlass','04',64,bevel=.1)
for i,(x,r,l) in enumerate([(352,12.5,3),(377,10.2,5),(463,10.2,5),(493,12.3,6),(514,13.1,3),(547,14.2,3)]):
    ring('Optic_Retainer_'+str(i),p(x,423),p(x+l,423),r,r-2,'Steel' if i in (0,4) else 'Panel','04',48,.14)
box('Optic_Turret_Block',429,422,28,22,27,'Armor','04',bevel=1.8)
rod('Optic_Elevation_Dial',p(429,407),p(429,402),9.5,'Panel','04',48,bevel=.45)
rod('Optic_Windage_Dial',p(429,422,13),p(429,422,20),7.7,'Panel','04',48,bevel=.4)
for i in range(24):
    a=i*math.tau/24
    rod('Elevation_Knurled_Ridge_'+str(i),p(429+9.4*math.cos(a),407,9.4*math.sin(a)),p(429+9.4*math.cos(a),402,9.4*math.sin(a)),.35,'Steel','04',8,bevel=0)
for s in (-1,1):
    box('Optic_Data_Plate_'+str(s),451,423,13,10,1,'DarkSteel','04',s*9,.25)
for x in (419,439):bolt('Optic_Turret_Pin_'+str(x),x,414,14,1.1,'04',mat='Brass')
label('Optic_ID','T01 / 4X',429,426,14.1,2.2,'Lettering','04')

# Detachable energy module with rails, recessed charge indicator and service panels.
mag=[(402,541),(463,541),(468,550),(467,612),(460,622),(403,620),(397,614),(398,550)]
poly('Energy_Cell_Chassis',mag,17,-17,'Panel','05',.65)
poly('Energy_Cell_Top_Cap',[(398,542),(466,542),(469,549),(468,554),(397,554)],19,-19,'Gunmetal','05',.4)
poly('Energy_Cell_Base_Cap',[(397,611),(469,613),(468,621),(461,625),(404,623),(397,619)],20,-20,'Gunmetal','05',.65)
panel('Energy_Cell_Side_Armor',[(422,554),(460,555),(459,608),(453,615),(422,613),(418,604)],19.2,2.5,'Armor','05',bevel=.45)
panel('Energy_Cell_Service_Cover',[(435,559),(455,559),(455,591),(452,596),(437,595),(434,591)],20.6,1.6,'Panel','05',bevel=.4)
trim('Energy_Cell_Service_Seam',[(438,562),(451,562),(451,590),(439,590)],20.8,.35,'Steel','05')
for s in (-1,1):
    box('Cell_Indicator_Recess_'+str(s),409,580,13,56,3,'Recess','05',s*19,.9)
    box('Cell_Indicator_Inner_'+str(s),409,580,8,51,1.2,'OpticGlass','05',s*20.6,.7)
    box('Cell_Charge_Cyan_'+str(s),409,580,3,46,1.1,'Cyan','05',s*21.2,.6)
    box('Cell_Charge_Core_'+str(s),409.6,580,1.1,43,1,'CyanCore','05',s*21.7,.35)
    for j in range(5):box('Cell_Charge_Divider_'+str(s)+'_'+str(j),409,560+j*10,8,1.3,1,'Panel','05',s*22,.1)
for i,(x,z) in enumerate([(402,549),(462,550),(463,615),(402,616),(439,565),(451,590)]):bolt('Cell_Captive_'+str(i),x,z,21,1,'05',mat='Brass' if i<4 else 'Steel')
for z in (561,589,611):
    panel('Cell_Lock_Rail_'+str(z),[(462,z-3),(469,z-3),(469,z+3),(462,z+3)],21.5,3.5,'Gunmetal','05')
    bolt('Cell_Rail_Detent_'+str(z),466,z,22.3,.85,'05',mat='Brass')
label('Cell_Label','ENERGY CELL',444,603,20.1,2.2,'Lettering','05')
label('Cell_Serial','EC-01',447,610,20.2,3.8,'Lettering','05')

# Open trigger guard, articulated trigger silhouette and textured sloped grip.
guard=poly('Open_Trigger_Guard',[(483,518),(538,518),(550,529),(549,550),(539,565),(496,565),(483,556)],8,-8,'Gunmetal','07',.65)
cut(guard,[[(490,527),(533,527),(541,534),(540,548),(533,556),(498,556),(488,550)]],'Trigger opening')
poly('Curved_Trigger',[(521,523),(529,523),(529,535),(526,543),(520,549),(515,548),(521,539),(523,532)],3.8,-3.8,'DarkSteel','07',.35)
poly('Pistol_Grip_Core',[(540,539),(563,537),(570,547),(580,576),(599,614),(595,620),(572,627),(561,616),(554,584),(544,560)],13.5,-13.5,'Polymer','07',.85)
grip_panel=[(551,558),(565,553),(574,577),(591,612),(574,618),(566,607),(559,584)]
panel('Pistol_Grip_Textured_Panel',grip_panel,14.6,2,'Polymer','07',bevel=.45)
poly('Grip_Heel_Cap',[(566,616),(597,607),(605,617),(576,631),(568,628)],17,-17,'Gunmetal','07',.6)
trim('Grip_Panel_Border',grip_panel+[grip_panel[0]],15.0,.55,'Panel','07')
for s in (-1,1):
    # Diamond stipple is real shallow relief, collected into one mesh per side.
    vs=[];fs=[]
    a,b,c,d=Vector((553,561)),Vector((565,557)),Vector((588,610)),Vector((575,616))
    def surface(u,v):return a*(1-u)*(1-v)+b*u*(1-v)+c*u*v+d*(1-u)*v
    for iz in range(42):
        for ix in range(10):
            u=(ix+.5)/10;v=(iz+.5)/42
            diamond=[surface(u-.033,v),surface(u,v-.008),surface(u+.033,v),surface(u,v+.008)]
            center=surface(u,v);base=len(vs)
            vs.extend(p(q.x,q.y,s*15.0) for q in diamond);vs.append(p(center.x,center.y,s*15.24))
            fs.extend((base+i,base+(i+1)%4,base+4) for i in range(4))
    mesh('Grip_Diamond_Stipple_'+str(s),vs,fs,'Polymer','07',0)
    for j in range(4):box('Grip_Back_Strap_'+str(s)+'_'+str(j),581+j*3.6,592+j*6,3,4,1,'Panel','07',s*13.8,.45)
bolt('Grip_Panel_Screw',561,561,15.2,1.3,'07')
bolt('Grip_Heel_Screw',590,618,17,1.3,'07')
for s in (-1,1):
    rod('Selector_Detent_'+str(s),p(550,524,s*16),p(550,524,s*22),4.8,'Recess','07',32,bevel=.25)
    rod('Selector_Knob_'+str(s),p(550,524,s*21),p(550,524,s*24),3.8,'Gunmetal','07',32,bevel=.2)
    box('Selector_Lever_'+str(s),556,526,13,3.6,2,'Steel','07',s*24,.75)
    box('Receiver_Status_Recess_'+str(s),530,521,11,3.5,2,'Recess','07',s*17.5,.6)
    box('Receiver_Status_LED_'+str(s),530,521,7,1.5,1,'Cyan','07',s*19,.3)
label('Selector_Modes','S    A',551,519,22,2.2,'Lettering','07')

# Stock: sliding tube, cheek rest, genuine skeleton opening and serrated butt pad.
rod('Stock_Extension_Tube',p(623,490),p(678,490),8.5,'Steel','06',48,bevel=.25)
for x in (630,639,650):ring('Stock_Adjuster_Collar_'+str(x),p(x,490),p(x+4,490),10.5,8,'DarkSteel','06',48,.3)
poly('Stock_Cheek_Rest',[(651,476),(778,476),(787,482),(785,512),(779,519),(754,519),(746,514),(669,513),(647,501)],16,-16,'Gunmetal','06',.7)
panel('Stock_Cheek_Seal',[(667,481),(776,481),(779,486),(777,508),(769,512),(673,507),(664,501)],17.4,1.8,'Recess','06',bevel=.5)
panel('Stock_Cheek_Shell',[(669,483),(774,483),(776,487),(774,506),(768,509),(674,505),(667,500)],18.3,1.3,'Armor','06',bevel=.45)
stockframe=poly('Stock_Skeleton_Frame',[(669,509),(779,509),(787,577),(775,582),(741,558),(681,535)],10,-10,'Gunmetal','06',.6)
cut(stockframe,[[(694,519),(773,519),(776,565),(748,543)]],'Stock skeleton opening')
panel('Stock_Diagonal_Brace',[(683,521),(695,521),(757,558),(755,564),(743,554),(687,530)],12,3.5,'Armor','06')
poly('Stock_Butt_Pad',[(783,478),(793,480),(792,576),(784,584),(778,582)],19,-19,'Polymer','06',.8)
for j in range(16):
    z=486+j*5.6
    box('Butt_Pad_Rib_'+str(j),792.2,z,3,2.4,38,'DarkSteel','06',bevel=.5)
for i,(x,z) in enumerate([(659,490),(677,499),(770,489),(776,511),(687,526),(773,572)]):bolt('Stock_Captive_'+str(i),x,z,19 if z<515 else 13,1.2,'06')
for s in (-1,1):
    box('Stock_Release_Lever_'+str(s),693,516,20,3,2,'DarkSteel','06',s*15,.6)
    for i in range(6):box('Stock_Release_Rib_'+str(s)+'_'+str(i),685+i*3,516,1,4,1,'Steel','06',s*16,.15)
label('Stock_Adjust_Index','1  2  3  4',641,486,10,1.8,'Lettering','06')

# Exposed rear data/power hardpoint, plug and protected cable.
box('AI_Hardpoint_Base',585,460,47,10,23,'DarkSteel','01',bevel=.8)
rod('AI_Data_Coupling',p(566,451),p(595,451),5.7,'Gunmetal','01',40,bevel=.3)
for x in (572,580,590):ring('AI_Coupling_Band_'+str(x),p(x,451),p(x+2.5,451),6.2,4.5,'Steel','01',32,.1)
box('AI_Rear_Port',604,455,13,14,26,'Panel','01',bevel=1)
for s in (-1,1):
    cable('AI_Data_Cable_'+str(s),[(590,449,s*7),(604,447,s*11),(615,452,s*12),(619,460,s*8)],1.5)
    box('AI_Connector_LED_'+str(s),605,453,4,2,1,'Cyan','01',s*14,.25)
bolt('AI_Port_Lock',605,460,15,1.6)
label('AI_Hardpoint_Mark','AI / DATA',586,461,16,2.1)

# Inner receiver members are visible through the cage; the vents open onto structure.
box('Internal_Upper_Heat_Sink_Bus',268,471.8,212,4.8,13,'DarkSteel','02',bevel=.4)
box('Internal_Lower_Heat_Sink_Bus',254,501.5,192,4.8,15,'DarkSteel','02',bevel=.4)
for i in range(17):
    x=170+i*12
    box('Internal_Upper_Heat_Sink_Fin_'+str(i),x,474,2.3,11,20,'Gunmetal','02',bevel=.16)
    box('Internal_Lower_Heat_Sink_Fin_'+str(i),x,500,2.1,10.5,21,'Gunmetal','02',bevel=.16)
for s in (-1,1):
    cable('Internal_Cyan_Power_Conduit_'+str(s),[(158,497,s*10),(212,497,s*10),(243,495,s*10)],.65,'Cyan','02')
    web=poly('Stock_Center_Mechanical_Web_'+str(s),[(690,516),(752,516),(770,533),(768,546),(747,536),(698,528)],s*11.8,s*8.8,'Panel','06',.4)
    cut(web,[[(706,520),(737,520),(750,529),(744,532),(710,524)]],'Stock brace service slot')
    box('Stock_Adjustment_Cartridge_'+str(s),718,518,33,7.5,3.3,'Gunmetal','06',s*13.6,.7)
    box('Stock_Adjustment_Cartridge_Slot_'+str(s),718,518,22,2,1,'DarkSteel','06',s*15.6,.25)
for x in (697,757):bolt('Stock_Web_Bolt_'+str(x),x,520 if x<720 else 534,13.5,1.1,'06',mat='Brass')

# Secondary machining and distinct local construction features from the detail panels.
# These are layered plates and recesses, not surface lines standing in for structure.
for s in (-1,1):
    for x,z,depth in ((374,505,16),(423,513,19),(344,507,17),(400,514,18)):
        box('Hose_Anchor_Block_'+str(s)+'_'+str(x),x,z,8,7,6,'Gunmetal','01',s*(depth-1),.65)
        ring('Hose_Anchor_Socket_'+str(s)+'_'+str(x),p(x,z,s*(depth+1)),p(x,z,s*(depth+3)),3.6,2.3,'DarkSteel','01',24,.3)
    # Forward vent lips: small silver returns inside the large black openings.
    for i in range(9):
        x=164+i*23
        trim('Vent_Mouth_Inner_Edge_'+str(s)+'_'+str(i),[(x+3.5,466.4),(x+18,466.4)],s*20.25,.55,'Steel','02',False)
    box('Handguard_Top_Shoulder_'+str(s),267,463,191,2.7,3,'Armor','02',s*21.4,.32)
    box('Handguard_Center_Rail_Gasket_'+str(s),205,493,88,2.1,1.4,'Recess','02',s*22.7,.15)
    box('Handguard_Center_Rail_'+str(s),205,493,83,1.2,1.1,'Steel','02',s*23.3,.12)
    box('Handguard_Front_Side_Rail_'+str(s),149,491,5.4,25,4.2,'Gunmetal','02',s*22,.6)
    for j in range(4):
        box('Front_Rail_Notch_'+str(s)+'_'+str(j),149,482+j*6,6.5,1.2,1,'Recess','02',s*24.3,.15)
    # Chamfered end locks flanking the central handguard indicator.
    for x in (357,377):
        box('Handguard_Lock_Base_'+str(s)+'_'+str(x),x,495,6.2,14,3,'Recess','02',s*23,.3)
        box('Handguard_Lock_Toggle_'+str(s)+'_'+str(x),x,495,3.2,9,3.8,'Brass','02',s*25,.45)
        box('Handguard_Lock_Slot_'+str(s)+'_'+str(x),x,495,1.1,3.5,.7,'DarkSteel','02',s*27.2,.15)
    # Small raised service covers between the long top rail and receiver plate.
    for i,x in enumerate((493,526,558)):
        box('Receiver_Top_Service_Seal_'+str(s)+'_'+str(i),x,470,25,7.0,1.8,'Recess','01',s*18.2,.4)
        box('Receiver_Top_Service_Cap_'+str(s)+'_'+str(i),x,470,23,5.2,1.3,'Armor','01',s*19.4,.3)
        box('Receiver_Top_Cap_Slot_'+str(s)+'_'+str(i),x,470,9,.7,1,'DarkSteel','01',s*20.2,.1)
    # A lower control sub-panel and three distinct capped attachment holes.
    box('Receiver_Control_Recess_'+str(s),532,518.5,45,8,2,'Recess','01',s*18,.55)
    box('Receiver_Control_Panel_'+str(s),532,518.5,41,5.6,1.3,'Panel','01',s*19.4,.35)
    for j,x in enumerate((571,583,595)):
        ring('Receiver_Lower_Boss_'+str(s)+'_'+str(j),p(x,516,s*18),p(x,516,s*21),3.2,1.8,'DarkSteel','01',32,.22)
        rod('Receiver_Lower_Boss_Cap_'+str(s)+'_'+str(j),p(x,516,s*20.7),p(x,516,s*21.4),1.75,'Panel','01',20,bevel=.2)
    # Large lower-receiver battery-lock housing with an inset access cover.
    box('Cell_Release_Housing_'+str(s),432,521,51,16,4,'Panel','01',s*16.8,.8)
    box('Cell_Release_Cover_'+str(s),432,520.7,43,10.4,1.8,'Gunmetal','01',s*19.8,.45)
    box('Cell_Release_Button_Recess_'+str(s),449,520.7,6,6,1.3,'Recess','01',s*21,.65)
    box('Cell_Release_Button_'+str(s),449,520.7,3.7,3.7,1.7,'Brass','01',s*22,.45)
    # Receiver rear service plate split by two genuine stepped corner blocks.
    for x in (486,607):
        box('Receiver_Corner_Block_'+str(s)+'_'+str(x),x,501,5.5,16,3.6,'Gunmetal','01',s*20.6,.5)
        box('Receiver_Corner_Seal_'+str(s)+'_'+str(x),x,501,2.5,8,1,'DarkSteel','01',s*22.8,.25)
    # Magazine locking rail and vertically staggered cover details.
    box('Cell_Left_Armor_Rail_'+str(s),400.4,581,4,45,3,'Steel','05',s*19.4,.35)
    for j in range(3):
        z=563+j*17
        box('Cell_Rail_Window_'+str(s)+'_'+str(j),400.5,z,3.7,8,1.3,'Recess','05',s*21.2,.4)
        box('Cell_Rail_Insert_'+str(s)+'_'+str(j),400.5,z,1.7,5,1.5,'Brass','05',s*22,.25)
    box('Cell_Lower_Data_Recess_'+str(s),442,607,29,5.5,1.4,'Recess','05',s*20.5,.45)
    for j in range(11):
        box('Cell_ID_Barcode_'+str(s)+'_'+str(j),430+j*2,607,.6 if j%3 else 1,3,1,'Lettering','05',s*21.4,0)
    box('Cell_Top_Serial_Badge_'+str(s),437,555,27,3.5,1.2,'DarkSteel','05',s*20.4,.3)
    # Stock: a second diagonal is separated from the front brace and inset panel.
    poly('Stock_Inner_Reinforcement_'+str(s),[(708,520),(718,520),(766,548),(767,555),(756,548),(710,524)],s*8,s*5,'DarkSteel','06',.35)
    box('Stock_Brace_Lock_'+str(s),701,531,15,9,3,'Panel','06',s*14,.65)
    box('Stock_Brace_Lock_Inset_'+str(s),701,531,9,4.4,1,'Recess','06',s*16,.35)
    for j in range(5):
        box('Stock_Cheek_Microgroove_'+str(s)+'_'+str(j),717+j*5,502,1,3.5,.6,'DarkSteel','06',s*18.7,.1)
    # Fully modelled small optic service panel, including fastener recesses.
    box('Optic_Ocular_Service_Cover_'+str(s),532,423,19,14,2,'Panel','04',s*12.6,.45)
    box('Optic_Ocular_Inset_'+str(s),532,423,13,9,1,'Gunmetal','04',s*13.9,.3)

for i,(x,z) in enumerate([(492,471),(501,471),(528,471),(559,471),(411,522),(454,522)]):bolt('Service_Cover_Fixing_'+str(i),x,z,21,0.72,'01')
for x in (683,703,746,764):bolt('Stock_Brace_Rivet_'+str(x),x,522+(x-683)*.53,14,1.15,'06',mat='Brass')
for x in (525,539):bolt('Scope_Ocular_Screw_'+str(x),x,418,14.5,.85,'04')
for x in (422,457):bolt('Cell_Panel_Extra_'+str(x),x,559,21.2,.85,'05')
label('Cell_Release_Text','POWER / RELEASE',429,521.5,21,2,'Lettering','01')
label('Cell_Upper_Code','CELL-01 / 100%',437,555.6,21.4,1.9,'Lettering','05')
label('Scope_Ocular_Code','4X-12 / T01',532,425,14.6,1.9,'Lettering','04')
label('Handguard_Tiny_Serial','HSA / COOLING ARRAY',317,463.8,23.1,2,'Lettering','02')

# Scope circumferential grip ribs, scale ticks and engraved control faces.
for i in range(32):
    a=i*math.tau/32
    rod('Optic_Focus_Knurl_'+str(i),p(491,423-12.6*math.sin(a),12.6*math.cos(a)),p(496.5,423-12.6*math.sin(a),12.6*math.cos(a)),.28,'DarkSteel','04',8,bevel=0)
for i in range(24):
    a=i*math.tau/24
    rod('Optic_Rear_Knurl_'+str(i),p(548,423-14.4*math.sin(a),14.4*math.cos(a)),p(553,423-14.4*math.sin(a),14.4*math.cos(a)),.32,'DarkSteel','04',8,bevel=0)
for i in range(12):
    a=i*math.tau/12
    rod('Elevation_Scale_Mark_'+str(i),p(429+7*math.cos(a),401.65,7*math.sin(a)),p(429+9*math.cos(a),401.65,9*math.sin(a)),.17,'Lettering','04',6,bevel=0)
ring('Windage_Control_Face',p(429,422,20),p(429,422,20.8),6.4,3.9,'Gunmetal','04',40,.12)
trim('Windage_Adjust_Slot',[(426,422),(432,422)],21.1,.8,'Steel','04',False)
for i in range(10):
    x=355+i*2
    trim('Scope_Distance_Tick_'+str(i),[(x,417),(x,419 if i%5 else 420.3)],11.8,.32,'Lettering','04')

# Emitter face locking lugs, recessed longitudinal seam and circular screw anchors.
for s in (-1,1):
    trim('Emitter_Lower_Service_Seam_'+str(s),[(44,500),(108,500)],s*16.3,.4,'DarkSteel','03',False)
    box('Emitter_Power_Strip_Return_'+str(s),70,491.7,28,.6,1,'Steel','03',s*18.3,.15)
    for x in (36,115):
        box('Emitter_Locking_Key_'+str(s)+'_'+str(x),x,474,7,4,6,'DarkSteel','03',s*7.5,.6)
label('Emitter_Service_Marking','EMITTER / TYPE-01',83,481,17.6,1.85,'Lettering','03')
for i in range(6):
    a=i*math.tau/6
    ring('Emitter_Front_Axial_Bolt_'+str(i),p(24.5,489-16.2*math.sin(a),16.2*math.cos(a)),p(25.5,489-16.2*math.sin(a),16.2*math.cos(a)),.8,.35,'Gunmetal','03',6,.03)

# Restrained worn paint chips along selected service edges, each a physical thin mark.
import random
rng=random.Random(106)
for j in range(86):
    zone=j%4
    if zone==0:x=rng.uniform(164,369);z=rng.choice((461.9,520.0));d=20.7;key='02'
    elif zone==1:x=rng.uniform(493,595);z=rng.choice((479.7,513.6));d=19.3;key='01'
    elif zone==2:x=rng.uniform(406,459);z=rng.choice((548,619));d=20.7;key='05'
    else:x=rng.uniform(673,772);z=rng.choice((484,505));d=18.6;key='06'
    le=rng.uniform(.35,1.8)
    panel('Minor_Edge_Wear_'+str(j),[(x,z),(x+le,z-.1),(x+le*.65,z+.15),(x+.2,z+.25)],d,.055,'Steel',key,True,0)

print('TYPE01_GEOMETRY_COMPLETE',len(PARTS),flush=True)

# Asset attachment points and a packed copy of the exact source artwork.
for name,xyz,descr in [('SCK_PRIMARY_GRIP',(571,586,0),'Primary hand grip'),
                       ('SCK_SUPPORT_GRIP',(262,519,0),'Support hand under handguard'),
                       ('SCK_MUZZLE',(24,489,0),'Laser origin; forward -X'),
                       ('SCK_ENERGY_CELL',(433,543,0),'Energy cell insertion point'),
                       ('SCK_AI_HARDPOINT',(586,451,0),'AI data/power interface')]:
    ob=bpy.data.objects.new(name,None);COL['09'].objects.link(ob);ob.parent=root;ob.location=p(*xyz)
    ob.empty_display_type='ARROWS';ob.empty_display_size=.025;ob['purpose']=descr
img=bpy.data.images.load(str(REF));img.pack()
refobj=bpy.data.objects.new('REFERENCE | supplied TYPE-01 design',None);COL['09'].objects.link(refobj)
refobj.empty_display_type='IMAGE';refobj.data=img;refobj.empty_display_size=.89;refobj.hide_viewport=True;refobj.hide_render=True
sc['reference_sha256']=hashlib.sha256(REF.read_bytes()).hexdigest()
sc['asset_version']='TYPE01 LASER RIFLE / detailed external game prop v1'
sc['asset_source']='User supplied TYPE-01 LASER RIFLE design sheet, 2026-09-06'
sc['geometry_features']='Open vent cage, inner conduit fins, recessed emitter, scope lenses and dials, removable energy cell, open stock and trigger guard.'

# Neutral studio: the model, not a promotional layout, is the subject.
studio=bpy.data.materials.new('Studio_White');studio.diffuse_color=(.78,.79,.80,1);studio.use_nodes=True
bs=next(n for n in studio.node_tree.nodes if n.type=='BSDF_PRINCIPLED');bs.inputs['Base Color'].default_value=(.78,.79,.80,1);bs.inputs['Roughness'].default_value=.78
world=bpy.data.worlds.new('TYPE01 Studio');world.use_nodes=True;world.node_tree.nodes.clear()
bg=world.node_tree.nodes.new('ShaderNodeBackground');wo=world.node_tree.nodes.new('ShaderNodeOutputWorld')
bg.inputs[0].default_value=(.65,.70,.76,1);bg.inputs[1].default_value=.28;world.node_tree.links.new(bg.outputs[0],wo.inputs[0]);sc.world=world
for name,pos,power,size,color in [('Key',(-.2,-.7,1.0),30,.8,(.93,.96,1)),('Fill',(.6,-.3,.4),13,.65,(.83,.91,1)),('Rim',(-.3,.5,.6),32,.65,(1,.91,.80)),('Top',(.1,0,1),15,.5,(1,1,1))]:
    data=bpy.data.lights.new('STUDIO_'+name,'AREA');data.energy=power;data.shape='DISK';data.size=size;data.color=color
    ob=bpy.data.objects.new(data.name,data);COL['10'].objects.link(ob);ob.location=pos;ob.rotation_euler=(Vector((-.12,0,.01))-ob.location).to_track_quat('-Z','Y').to_euler()
bpy.context.view_layer.update()
def camera(name,outward,center,scale,res):
    data=bpy.data.cameras.new('CAM_'+name);data.type='ORTHO';data.sensor_fit='HORIZONTAL';data.ortho_scale=scale
    ob=bpy.data.objects.new(data.name,data);COL['10'].objects.link(ob)
    ob.location=Vector(center)+Vector(outward).normalized()*2;ob.rotation_euler=(Vector(center)-ob.location).to_track_quat('-Z','Y').to_euler()
    ob['resolution']=res;return ob
camera('HERO',(-.32,-1,.26),p(411,515),1.03,(3000,1300))
camera('SIDE',(0,-1,0),p(410,515),.97,(3000,1050))
camera('REVERSE',(.15,1,.12),p(410,515),1.0,(3000,1150))
camera('TOP',(0,-.04,1),p(410,492),.97,(3000,850))
camera('EMITTER',(-.58,-1,.25),p(92,488),.245,(1600,1100))
camera('ENERGY_CELL',(-.12,-1,.17),p(455,546),.30,(1700,1250))
camera('STOCK',(.25,-1,.16),p(702,522),.26,(1600,1150))
camera('OPTIC',(-.45,-1,.20),p(443,429),.285,(1700,1000))
sc.camera=bpy.data.objects['CAM_HERO'];sc.render.resolution_x=3000;sc.render.resolution_y=1300;sc.render.resolution_percentage=100
sc.render.engine='CYCLES';sc.cycles.samples=64;sc.cycles.use_denoising=True
sc.render.film_transparent=True;sc.render.image_settings.file_format='PNG';sc.render.image_settings.color_mode='RGBA'
sc.view_settings.view_transform='AgX';sc.view_settings.exposure=0;sc.view_settings.gamma=1
try:sc.view_settings.look='AgX - Medium High Contrast'
except TypeError:pass
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            space=area.spaces.active;space.overlay.show_overlays=False;space.shading.type='MATERIAL';space.region_3d.view_perspective='CAMERA'
bpy.ops.object.select_all(action='DESELECT');root.select_set(True);bpy.context.view_layer.objects.active=root
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(D/'TYPE01_LASER_RIFLE_MASTER.blend'))
report={'master':str(D/'TYPE01_LASER_RIFLE_MASTER.blend'),'mesh_objects':sum(o.type=='MESH' for o in PARTS),
        'separate_parts':len(PARTS),'materials':list(M),'reference_sha256':sc['reference_sha256'],'length_m':.89,
        'open_handguard_vents_per_side':len(holes),'editable_subassemblies':[o.name for o in GROUPS.values()]}
(D/'build_manifest.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('TYPE01_MASTER_SAVED',json.dumps(report),flush=True)
