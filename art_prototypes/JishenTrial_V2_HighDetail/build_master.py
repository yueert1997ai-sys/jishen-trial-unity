"""Executed high-detail construction: engineered armor openings, joints, actuators,
segmented manipulators, propulsion assemblies and anti-ship weapon systems.
Run with Blender --background --python build_master.py, working in this directory.
"""
import bpy, bmesh, math, pathlib, json, runpy, time
from mathutils import Vector, Matrix
OUT=pathlib.Path(__file__).resolve().parent
T=time.time()
ctx=runpy.run_path(str(OUT/'build_core.py'))
S=ctx['S'];C=ctx['C'];ROOT=ctx['ROOT'];scene=ctx['scene']
hull=ctx['hull'];slab=ctx['slab'];tube=ctx['tube'];axial=ctx['axial'];beam=ctx['beam'];pivot=ctx['pivot'];rawmesh=ctx['mesh']
CHEST=ctx['CHEST'];PELVIS=ctx['PELVIS'];HEAD=ctx['HEAD'];WAIST=ctx['WAIST'];PACK=ctx['PACK'];SWORD=ctx['SWORD'];PITCH=ctx['PITCH']
scene['stage']='High-detail master: industrial hard-surface asset'

def pbr(name,rgb,metal,rough,emission=0,scratch=False):
    m=bpy.data.materials.new(name);m.use_nodes=True;m.diffuse_color=(*rgb,1)
    n=m.node_tree.nodes;l=m.node_tree.links
    p=next(n for n in n if n.type=='BSDF_PRINCIPLED')
    p.inputs['Base Color'].default_value=(*rgb,1);p.inputs['Metallic'].default_value=metal
    p.inputs['Roughness'].default_value=rough
    p.inputs['Emission Color'].default_value=(*rgb,1);p.inputs['Emission Strength'].default_value=emission
    if scratch:
        tex=n.new('ShaderNodeTexNoise');tex.name='Fine manufacturing variation';tex.inputs['Scale'].default_value=210
        ramp=n.new('ShaderNodeMapRange');ramp.inputs['From Min'].default_value=0;ramp.inputs['From Max'].default_value=1
        ramp.inputs['To Min'].default_value=rough-.045;ramp.inputs['To Max'].default_value=rough+.055
        l.new(tex.outputs['Fac'],ramp.inputs['Value']);l.new(ramp.outputs['Result'],p.inputs['Roughness'])
        bump=n.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.07;bump.inputs['Distance'].default_value=.000065
        l.new(tex.outputs['Fac'],bump.inputs['Height']);l.new(bump.outputs['Normal'],p.inputs['Normal'])
    return m

NAVY=pbr('01_Painted_Military_Navy',(.020,.035,.071),.24,.38,scratch=True)
PANEL=pbr('02_Navy_Secondary_Panels',(.037,.060,.109),.26,.42,scratch=True)
EDGEPAINT=pbr('03_Painted_Edge_Exposure',(.08,.11,.15),.50,.41)
WHITE=pbr('04_Offwhite_Ceramic_Armor',(.53,.57,.61),.22,.4,scratch=True)
FRAME=pbr('05_Black_Mechanical_Frame',(.012,.017,.022),.48,.44,scratch=True)
GUN=pbr('06_Dark_Gunmetal',(.052,.070,.084),.73,.32,scratch=True)
STEEL=pbr('07_Bare_Machined_Steel',(.235,.278,.315),.85,.25,scratch=True)
NOZZLE=pbr('08_Heat_Resistant_Thruster_Metal',(.085,.091,.098),.78,.46,scratch=True)
RUBBER=pbr('09_Cable_Protection',(.009,.013,.017),.05,.66)
GLASS=pbr('10_Blue_Sensor_Glass',(.025,.34,.75),.2,.17,2.0)
BEAM=pbr('11_Blue_Beam_Plasma',(.014,.33,1),.05,.19,5.0)
DEEP=pbr('12_Optical_Recess',(.001,.002,.004),.1,.3)
MATS=[NAVY,PANEL,EDGEPAINT,WHITE,FRAME,GUN,STEEL,NOZZLE,RUBBER,GLASS,BEAM,DEEP]

def assign(o,mat):
    o.data.materials.clear();o.data.materials.append(mat);o.color=mat.diffuse_color
    return o

for o in list(scene.objects):
    if o.type!='MESH':continue
    r=o.get('structural_role','primary')
    material={'primary':NAVY,'secondary':PANEL,'frame':FRAME,'joints':GUN,'propulsion':NAVY,'weapon':GUN,'cannon':GUN}.get(r,NAVY)
    if any(n in o.name for n in ('Exhaust','Nozzle','Bore_','Grip','Bearing','Gimbal','Trunnion')):material=NOZZLE if any(n in o.name for n in ('Exhaust','Nozzle')) else GUN
    if o.name.startswith(('UpperArm_Swept','Ankle_Armored','Forearm_Inner_Oblique')):material=WHITE
    if o.name.startswith('Head_Twin_Sensor'):material=GLASS
    if o.name=='Blade_Continuous_Energy_Edge':material=BEAM
    assign(o,material)

def solid(name,points,role='secondary',parent=ROOT,mat=PANEL,bevel=.012):
    return assign(hull(name,points,role,parent,bevel),mat)

def plate(name,points,depth=(0,.10,0),parent=ROOT,mat=PANEL,role='secondary',bevel=.009):
    return assign(slab(name,points,depth,role,parent,bevel),mat)

def rod(name,a,b,r,parent=ROOT,mat=STEEL,role='frame',n=24,r2=None):
    return assign(axial(name,a,b,r,role,parent,r2=r2,n=n),mat)

def ring(name,a,b,outer,inner,parent=ROOT,mat=GUN,role='frame',n=32):
    return assign(tube(name,a,b,outer,inner,role,parent,n=n),mat)

def block(name,at,dims,parent=ROOT,mat=GUN,role='frame',bevel=.014):
    x,y,z=at;w,d,h=[v*.5 for v in dims]
    return solid(name,[(x+a*w,y+b*d,z+c*h) for a in (-1,1) for b in (-1,1) for c in (-1,1)],role,parent,mat,bevel)

def fastener(name,at,normal,r=.034,parent=ROOT,mat=STEEL):
    p=Vector(at);n=Vector(normal).normalized()
    ring(name+'_Washer',p-n*.006,p+n*.008,r*1.32,r*.66,parent,GUN,n=16)
    head=rod(name+'_HexSocket',p,p+n*.028,r,parent,mat,n=6)
    rod(name+'_DarkSocket',p+n*.028,p+n*.029,r*.48,parent,DEEP,n=6)
    head['function']='Captive service fastener'

def piston(name,a,b,r=.07,parent=ROOT):
    a,b=Vector(a),Vector(b);u=(b-a).normalized();d=(b-a).length
    rod(name+'_PistonRod',a,b,r*.62,parent,STEEL)
    rod(name+'_HydraulicSleeve',a,a+u*d*.55,r*1.15,parent,GUN)
    ring(name+'_SealCollar',a+u*d*.49,a+u*d*.58,r*1.36,r*.64,parent,STEEL,n=24)
    for pt,k in ((a,'A'),(b,'B')):
        rod(name+'_ClevisPin_'+k,pt+Vector((-.10,0,0)),pt+Vector((.10,0,0)),r*.95,parent,GUN,n=16)

def cable(name,pts,r=.027,parent=ROOT,mat=RUBBER):
    d=bpy.data.curves.new(name+'_Path','CURVE');d.dimensions='3D';d.resolution_u=16;d.bevel_depth=r*S;d.bevel_resolution=3
    sp=d.splines.new('BEZIER');sp.bezier_points.add(len(pts)-1)
    for p,v in zip(sp.bezier_points,pts):p.co=Vector(v)*S;p.handle_left_type='AUTO';p.handle_right_type='AUTO'
    o=bpy.data.objects.new(name,d);C['frame'].objects.link(o);d.materials.append(mat);ctx['parent_keep'](o,parent)
    o['function']='Protected cable route between actuator housings'
    return o

def joint(name,centre,axis,radius,width,parent):
    p=Vector(centre);n=Vector(axis).normalized();q=n.to_track_quat('Z','Y')
    ring(name+'_Outer_Housing',p-n*width/2,p+n*width/2,radius,radius*.70,parent,GUN,n=48)
    ring(name+'_Bearing_Race',p+n*(width/2+.007),p+n*(width/2+.042),radius*.89,radius*.62,parent,STEEL,n=48)
    rod(name+'_Central_Servo',p-n*width*.3,p+n*(width/2+.019),radius*.56,parent,FRAME,n=32)
    ring(name+'_Drive_Ring',p+n*(width/2+.024),p+n*(width/2+.048),radius*.52,radius*.37,parent,GUN,n=32)
    rod(name+'_Axle_Cap',p+n*(width/2+.027),p+n*(width/2+.07),radius*.30,parent,STEEL,n=12)
    for i in range(8):
        a=i*math.tau/8;pt=p+q@Vector((radius*.82*math.cos(a),radius*.82*math.sin(a),width/2+.043))
        fastener(name+'_ServiceBolt_%02d'%i,pt,n,radius*.066,parent)

CUTTERS=bpy.data.collections.new('Construction_Cutters | hidden');C['primary'].children.link(CUTTERS)

def surface_module(name,target,x,z,width,height,parent,side='front',bars=4):
    # Cast onto the actual armor plane, cut a real cavity, then fit the intake hardware.
    o=bpy.data.objects[target];bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get();ev=o.evaluated_get(dg)
    ray=Vector((0,1,0)) if side=='front' else Vector((0,-1,0))
    origin=Vector((x,-10 if side=='front' else 10,z))*S
    inv=o.matrix_world.inverted();ok,loc,nor,idx=ev.ray_cast(inv@origin,inv.to_3x3()@ray)
    if not ok:raise RuntimeError('Cannot locate armor surface: '+name+' / '+target)
    pos=o.matrix_world@loc/S;n=(o.matrix_world.to_3x3()@nor).normalized()
    xx=(Vector((1,0,0))-n*n.dot(Vector((1,0,0)))).normalized();yy=-n;zz=xx.cross(yy).normalized()
    if zz.z<0:xx=-xx;zz=-zz
    def tr(p):return tuple(pos+xx*p[0]+yy*p[1]+zz*p[2])
    def localbox(label,c,d,mat,depthbevel=.01,role='secondary'):
        return solid(label,[tr((c[0]+a*d[0]/2,c[1]+b*d[1]/2,c[2]+k*d[2]/2))
                            for a in (-1,1) for b in (-1,1) for k in (-1,1)],role,parent,mat,depthbevel)
    cutter=localbox(name+'_BooleanVolume',(0,.09,0),(width,.40,height),DEEP,.022)
    # Cutters are retained for fully editable Boolean stacks.
    for c in list(cutter.users_collection):c.objects.unlink(cutter)
    CUTTERS.objects.link(cutter);cutter.hide_render=True;cutter.hide_set(True);cutter.display_type='WIRE'
    m=o.modifiers.new(name+'_RecessCut','BOOLEAN');m.operation='DIFFERENCE';m.solver='EXACT';m.object=cutter
    # Place Boolean before bevel/shading modifiers.
    while o.modifiers.find(m.name)>0:
        bpy.context.view_layer.objects.active=o
        bpy.ops.object.modifier_move_up(modifier=m.name)
    localbox(name+'_RecessFloor',(0,.245,0),(width*.97,.055,height*.95),DEEP,.008)
    lip=.032
    for p,d in [((0,-.013,height/2+.009),(width+.09,.075,lip)),((0,-.013,-height/2-.009),(width+.09,.075,lip)),
                ((width/2+.012,-.013,0),(lip,.075,height+.05)),((-width/2-.012,-.013,0),(lip,.075,height+.05))]:
        localbox(name+'_Rim_%d'%len([n for n in bpy.data.objects if n.name.startswith(name+'_Rim')]),p,d,GUN,.01)
    for i in range(bars):
        h=(i-(bars-1)/2)*height/bars
        bar=localbox(name+'_Louver_%02d'%i,(0,.065,h),(width*.89,.09,height/bars*.30),STEEL,.005)
        bar['function']='Angled heat exchanger louver inside cut recess'
    return pos

# Mechanically complete major joints; concentric races and fasteners have a purpose.
joint('Waist_Yaw_Drive',(0,.05,6.65),(0,0,1),.47,.24,WAIST)
joint('Neck_Rotation',(0,.02,9.21),(0,0,1),.205,.14,HEAD)
for s,side in ((-1,'R'),(1,'L')):
    arm=bpy.data.objects['UpperArm.'+side];fa=bpy.data.objects['Forearm.'+side];hand=bpy.data.objects['Hand.'+side]
    thigh=bpy.data.objects['Thigh.'+side];shin=bpy.data.objects['Shin.'+side];foot=bpy.data.objects['Foot.'+side]
    joint('Shoulder_Servo.'+side,(s*1.62,.1,8.6),(s,0,0),.32,.38,arm)
    joint('Elbow_Servo.'+side,(s*1.90,.02,7.31),(s,0,0),.238,.43,fa)
    joint('Wrist_Bearing.'+side,(s*2.22,-.08,5.67),(0,0,-1),.17,.13,hand)
    joint('Hip_Load_Bearing.'+side,(s*.66,.05,6.17),(s,0,0),.295,.30,thigh)
    joint('Knee_Servo.'+side,(s*.96,-.055,3.73),(s,0,0),.275,.57,shin)
    joint('Ankle_Rotator.'+side,(s*1.1,.07,.83),(s,0,0),.231,.56,foot)
    piston('Waist_Stabilizer.'+side,(s*.43,.31,6.35),(s*.64,.44,7.3),.073,WAIST)
    piston('Arm_Elbow_Actuator.'+side,(s*1.68,.45,8.1),(s*2.06,.43,6.97),.067,fa)
    piston('Thigh_Posterior_Actuator.'+side,(s*.57,.41,5.92),(s*.85,.44,4.02),.075,thigh)
    piston('Calf_Long_Damper.'+side,(s*.75,.94,3.0),(s*.94,.48,1.12),.08,shin)
    cable('Waist_Power_Bundle.'+side,[(s*.55,.26,7.35),(s*.53,.42,6.95),(s*.38,.43,6.46),(s*.56,.26,6.23)],.039,WAIST)
    cable('Hip_Control_Loom.'+side,[(s*.45,.17,6.32),(s*.68,.34,6.12),(s*.83,.27,5.91)],.027,thigh)
    cable('Elbow_Flex_Loom.'+side,[(s*1.83,.32,7.58),(s*2.08,.41,7.29),(s*2.16,.26,6.98)],.023,fa)

# Large functional chest intakes, central core access and lower heat exchangers.
for s,side in ((-1,'R'),(1,'L')):
    surface_module('Pectoral_Main_Intake.'+side,'Thorax_Pectoral_Wrapped.'+side,s*.85,8.47,.62,.205,CHEST,bars=3)
    surface_module('Rib_Heat_Exchanger.'+side,'Thorax_Lateral_Rib_Armor.'+side,s*.81,7.61,.35,.26,CHEST,bars=4)
    def mir(ps):return [(s*x,y,z) for x,y,z in ps]
    plate('Pectoral_Armored_Top_Cap.'+side,mir([(.22,-.965,8.95),(.50,-1.034,8.91),(1.21,-.75,8.79),(1.12,-.78,8.67),(.43,-1.105,8.74)]),(0,.10,0),CHEST,PANEL)
    plate('Chest_Lower_Intake_Armored_Lip.'+side,mir([(.49,-.98,8.10),(1.12,-.71,8.03),(1.10,-.73,7.96),(.51,-1.0,8.0)]),(0,.07,0),CHEST,WHITE)
    beam_o=beam('Chest_Diagonal_Support.'+side,(s*.64,.50,7.4),(s*1.11,.35,8.43),.09,.10,'frame',CHEST);assign(beam_o,GUN)
    for x,z in ((.34,8.90),(1.20,8.65),(.66,7.44)):
        fastener('Chest_Armor_Lock_%.2f_%s'%(z,side),(s*x,-.55 if x>1 else -.96 if z>8 else -.53,z),(0,-1,0),.026,CHEST)
plate('Core_Upper_Access_Shell',[(-.12,-1.122,8.55),(.12,-1.122,8.55),(.17,-1.183,8.32),(0,-1.205,8.13),(-.17,-1.183,8.32)],(0,.065,0),CHEST,GUN)
plate('Core_Narrow_Sensor_Window',[(-.035,-1.198,8.42),(.035,-1.198,8.42),(.04,-1.217,8.25),(-.04,-1.217,8.25)],(0,.012,0),CHEST,GLASS,bevel=.004)

# Head rebuild details: layered visor rims, segmented hard face, neck servos and optics.
bpy.data.objects.remove(bpy.data.objects['Head_Hard_Mask'],do_unlink=True)
for s,side in ((-1,'R'),(1,'L')):
    def mir(ps):return [(s*x,y,z) for x,y,z in ps]
    plate('Face_Mask_Half.'+side,mir([(.018,-.468,9.54),(.15,-.394,9.55),(.205,-.427,9.39),(.06,-.527,9.255),(.015,-.57,9.33)]),(0,.13,0),HEAD,GUN)
    plate('Visor_Upper_Armored_Rim.'+side,mir([(.054,-.484,9.638),(.292,-.438,9.722),(.355,-.372,9.699),(.304,-.463,9.653),(.089,-.501,9.592)]),(0,.046,0),HEAD,WHITE,bevel=.006)
    plate('Visor_Lower_Optical_Seal.'+side,mir([(.09,-.458,9.512),(.27,-.434,9.577),(.31,-.373,9.54),(.15,-.475,9.47)]),(0,.03,0),HEAD,FRAME,bevel=.004)
    plate('Cheek_Upper_Overlapping_Plate.'+side,mir([(.275,-.366,9.59),(.45,-.159,9.64),(.42,-.225,9.43),(.29,-.433,9.37)]),(0,.077,0),HEAD,PANEL)
    plate('Jaw_Lower_Armored_Edge.'+side,mir([(.31,-.377,9.39),(.387,-.26,9.30),(.18,-.386,9.185),(.14,-.49,9.248)]),(0,.085,0),HEAD,WHITE,bevel=.006)
    joint('Head_Temple_Servo.'+side,(s*.384,.078,9.60),(s,0,0),.116,.068,HEAD)
    plate('Head_Temple_Armor.'+side,mir([(.405,.015,9.77),(.456,.055,9.68),(.433,.249,9.62),(.373,.279,9.79)]),(0,-.025,-.045),HEAD,NAVY)
    rod('Head_Side_Sensor_Housing.'+side,(s*.39,.23,9.68),(s*.475,.23,9.68),.062,HEAD,FRAME,n=16)
    rod('Head_Side_Sensor_Glass.'+side,(s*.475,.23,9.68),(s*.482,.23,9.68),.043,HEAD,GLASS,n=24)
    piston('Neck_Tilt_Link.'+side,(s*.17,-.09,9.12),(s*.19,.07,9.38),.033,HEAD)
    cable('Neck_Data_Conduit.'+side,[(s*.115,.13,9.16),(s*.155,.245,9.29),(s*.16,.235,9.43)],.016,HEAD)
plate('Rear_Head_Sensor_Mount',[(-.22,.365,9.69),(.22,.365,9.69),(.17,.325,9.88),(-.17,.325,9.88)],(0,-.065,0),HEAD,GUN)
plate('Rear_Head_Blue_Strip',[(-.13,.374,9.747),(.13,.374,9.747),(.13,.367,9.785),(-.13,.367,9.785)],(0,-.01,0),HEAD,GLASS,bevel=.003)
plate('Forehead_Sensor_Aperture',[(-.032,-.418,9.85),(.032,-.418,9.85),(.042,-.46,9.72),(-.042,-.46,9.72)],(0,.018,0),HEAD,FRAME,bevel=.006)
plate('Forehead_Sensor_Lens',[(-.018,-.436,9.83),(.018,-.436,9.83),(.023,-.471,9.73),(-.023,-.471,9.73)],(0,.01,0),HEAD,GLASS,bevel=.002)

# Shoulder armor mounting, forearm service cartridges, articulated mechanical fingers.
for s,side in ((-1,'R'),(1,'L')):
    def mir(ps):return [(s*x,y,z) for x,y,z in ps]
    pa=bpy.data.objects['Shoulder_Armor_Floating_Pivot.'+side]
    arm=bpy.data.objects['UpperArm.'+side];fa=bpy.data.objects['Forearm.'+side];hand=bpy.data.objects['Hand.'+side]
    for z in (8.53,8.19):
        rod('Shoulder_Shell_Mount_%.2f.%s'%(z,side),(s*1.66,-.28,z),(s*1.67,-.55,z),.058,pa,GUN,n=16)
    plate('Shoulder_Front_Service_Armor.'+side,mir([(1.52,-.632,8.55),(1.81,-.574,8.53),(1.96,-.59,8.32),(1.84,-.62,8.17),(1.57,-.65,8.23)]),(0,.054,0),pa,NAVY)
    plate('Shoulder_Identification_Band.'+side,mir([(1.62,-.664,8.35),(1.82,-.636,8.33),(1.81,-.643,8.28),(1.62,-.672,8.30)]),(0,.014,0),pa,WHITE,bevel=.003)
    for x,z in ((1.54,8.49),(1.87,8.36),(2.12,8.54)):
        fastener('Shoulder_Lock_%.2f.%s'%(z,side),(s*x,-.64 if x<2 else -.34,z),(0,-1,0),.025,pa)
    block('UpperArm_Servo_Cartridge.'+side,(s*1.77,.28,7.74),(.35,.22,.54),arm,GUN)
    for j in range(3):
        block('UpperArm_Cartridge_Cooling_%d.%s'%(j,side),(s*1.77,.41,7.57+j*.14),(.29,.026,.038),arm,STEEL,bevel=.004)
    surface_module('Forearm_Service_Intake.'+side,'Forearm_Dorsal_Main_Overlay.'+side,s*2.19,6.54,.28,.24,fa,bars=4)
    plate('Forearm_Blade_Edge_Cap.'+side,mir([(2.34,-.14,6.92),(2.57,-.025,6.32),(2.43,-.18,5.91),(2.35,-.29,6.23)]),(0,.12,0),fa,GUN)
    for z in (6.89,6.14):fastener('Forearm_Armor_Lock_%.2f.%s'%(z,side),(s*2.18,-.55,z),(0,-1,0),.028,fa)
    block('Wrist_Connector_Block.'+side,(s*2.21,.14,5.77),(.23,.19,.20),hand,GUN)
    # Replace the v1 curled finger placeholders completely.
    for o in list(scene.objects):
        if (o.name.startswith('Hand_Curled_Finger_') or o.name.startswith('Hand_Thumb.')) and o.name.endswith('.'+side):
            bpy.data.objects.remove(o,do_unlink=True)
    block('Palm_Internal_Manifold.'+side,(s*2.25,.16,5.32),(.36,.12,.24),hand,GUN)
    for j in range(4):
        xx=s*(2.065+j*.12);delta=.015*abs(j-1.5)
        points=[Vector((xx,.015,5.125-delta)),Vector((xx,-.012,4.945-delta)),
                Vector((xx,-.185,4.887-delta)),Vector((xx,-.297,4.986-delta))]
        par=hand
        for k in range(3):
            piv=pivot('Finger_%d_Joint_%d.%s'%(j,k,side),points[k],par)
            obj=beam('Finger_%d_Phalanx_%d.%s'%(j,k,side),points[k],points[k+1],.047,.055,'frame',piv);assign(obj,GUN)
            joint_c=points[k]
            rod('Finger_%d_Pin_%d.%s'%(j,k,side),joint_c+Vector((-.056,0,0)),joint_c+Vector((.056,0,0)),.034,piv,STEEL,n=16)
            mid=(points[k]+points[k+1])/2
            block('Finger_%d_Armor_%d.%s'%(j,k,side),mid+Vector((0,.051,0)),(.085,.031,.094 if k==0 else .060),piv,PANEL,role='secondary',bevel=.010)
            par=piv
        ring('Finger_%d_MCP_Seal.%s'%(j,side),points[0]+Vector((-.058,0,0)),points[0]+Vector((.058,0,0)),.045,.025,hand,FRAME,n=16)
    thumbpts=[(s*2.025,-.015,5.35),(s*1.915,-.16,5.205),(s*2.00,-.31,5.09)]
    for k in range(2):
        assign(beam('Thumb_Phalanx_%d.%s'%(k,side),thumbpts[k],thumbpts[k+1],.068,.072,'frame',hand),GUN)
        rod('Thumb_Pivot_%d.%s'%(k,side),Vector(thumbpts[k])+Vector((0,-.082,0)),Vector(thumbpts[k])+Vector((0,.082,0)),.045,hand,STEEL,n=20)
    for j in range(2):
        fastener('Hand_Dorsal_Fastener_%d.%s'%(j,side),(s*(2.10+j*.22),-.287,5.40),(0,-1,0),.020,hand)

# Layered thigh accents, open calf cooling, vector thrusters and mechanically segmented soles.
for s,side in ((-1,'R'),(1,'L')):
    def mir(ps):return [(s*x,y,z) for x,y,z in ps]
    thigh=bpy.data.objects['Thigh.'+side];shin=bpy.data.objects['Shin.'+side];foot=bpy.data.objects['Foot.'+side]
    plate('Thigh_Inner_Ceramic_Armor.'+side,mir([(.45,-.25,5.59),(.61,-.33,5.53),(.70,-.34,4.35),(.60,-.21,4.12),(.44,-.16,4.63)]),(0,.13,0),thigh,WHITE)
    plate('Thigh_Forward_Access_Panel.'+side,mir([(.65,-.378,5.60),(.90,-.355,5.58),(1.03,-.289,5.09),(.87,-.389,4.87),(.64,-.414,5.18)]),(0,.064,0),thigh,PANEL)
    for x,z in ((.69,5.48),(.86,4.39)):
        fastener('Thigh_Service_Latch_%.2f.%s'%(z,side),(s*x,-.425,z),(0,-1,0),.026,thigh)
    surface_module('Calf_Flank_Heat_Exchanger.'+side,'Calf_Outer_Heavy_Carapace.'+side,s*1.52,2.65,.29,.40,shin,bars=5)
    surface_module('Calf_Rear_Exhaust_Grille.'+side,'Calf_Rear_Spur_Armor.'+side,s*1.03,2.43,.35,.42,shin,side='back',bars=5)
    plate('Shin_Inner_Ceramic_Splint.'+side,mir([(.58,-.20,2.97),(.75,-.38,3.00),(.83,-.4,2.54),(.80,-.27,1.91),(.69,-.13,2.13),(.55,-.075,2.72)]),(0,.17,0),shin,WHITE)
    plate('Calf_Forward_Upper_Service_Panel.'+side,mir([(.79,-.645,3.15),(1.10,-.56,3.2),(1.23,-.45,2.99),(1.04,-.618,2.80),(.80,-.56,2.91)]),(0,.064,0),shin,PANEL)
    block('Calf_Vector_Thruster_Bracket.'+side,(s*1.40,.69,2.4),(.23,.36,.32),shin,FRAME)
    ring('Calf_Vector_Nozzle_Outer.'+side,(s*1.43,.69,2.52),(s*1.47,1.02,2.20),.164,.112,shin,NOZZLE,n=32)
    ring('Calf_Vector_Nozzle_Rim.'+side,(s*1.466,.99,2.23),(s*1.473,1.055,2.17),.176,.119,shin,STEEL,n=32)
    rod('Calf_Vector_Nozzle_Core.'+side,(s*1.45,.87,2.35),(s*1.455,.89,2.33),.107,shin,DEEP,n=24)
    cable('Ankle_Control_Conduit.'+side,[(s*.88,.38,1.60),(s*.77,.48,1.20),(s*.88,.40,.84)],.025,foot)
    for x,z,y in ((.88,3.16,-.66),(1.42,2.29,-.12),(1.10,1.52,-.57)):
        fastener('Calf_Armor_Catch_%.2f.%s'%(z,side),(s*x,y,z),(0,-1,0),.028,shin)
    xx=s*1.11
    pts=[(xx-.44,-1.19,0),(xx+.44,-1.19,0),(xx+.44,.58,0),(xx+.31,.79,0),(xx-.31,.79,0),(xx-.44,.58,0),
         (xx-.38,-1.24,.14),(xx+.38,-1.24,.14),(xx+.41,.52,.17),(xx+.28,.73,.16),(xx-.28,.73,.16),(xx-.41,.52,.17)]
    solid('Foot_Continuous_Structural_Sole.'+side,pts,'frame',foot,FRAME,.018)
    for j,y in enumerate((-.97,-.64,-.29,.10,.44)):
        block('Foot_Sole_Grip_Segment_%d.%s'%(j,side),(xx,y,.067),(.86,.18,.134),foot,RUBBER,bevel=.018)
    for dx in (-.215,.215):
        plate('Foot_Toe_Protective_Cap_%s.%s'%(dx,side),[(xx+dx-.14,-1.159,.25),(xx+dx+.14,-1.159,.25),(xx+dx+.17,-.87,.38),(xx+dx-.17,-.87,.38)],(0,.025,-.035),foot,NAVY)
    plate('Foot_Instep_Ceramic_Overlay.'+side,[(xx-.16,-.50,.54),(xx+.16,-.50,.54),(xx+.16,-.20,.81),(xx-.16,-.20,.81)],(0,.05,-.045),foot,WHITE)
    piston('Foot_Heel_Stabilizer.'+side,(xx,.38,.64),(xx,.66,.28),.053,foot)

def exhaust(name,a,b,r,parent):
    a,b=Vector(a),Vector(b);n=(b-a).normalized();q=n.to_track_quat('Z','Y')
    ring(name+'_Throat',a,b,r*.84,r*.63,parent,NOZZLE,n=48)
    ring(name+'_Exit_Lip',b-n*.05,b+n*.03,r,r*.75,parent,STEEL,n=48)
    ring(name+'_Gimbal_Collar',a-n*.02,a+n*.10,r*1.08,r*.84,parent,GUN,n=48)
    rod(name+'_Deep_Recess',a+n*.04,a+n*.06,r*.62,parent,DEEP,n=32)
    rod(name+'_Pilot_Core',a+n*.07,a+n*.071,r*.27,parent,GLASS,n=32)
    for j in range(12):
        aa=j*math.tau/12;pts=[]
        for p,rr in ((a+n*.09,r*.94),(b-n*.07,r*1.02)):
            pts += [tuple(p+q@Vector((rr*math.cos(aa+d),rr*math.sin(aa+d),0))) for d in (-.19,.19)]
        pts += [tuple(Vector(p)+n*.026) for p in pts]
        solid(name+'_Segmented_Nozzle_Petal_%02d'%j,pts,'propulsion',parent,NOZZLE,.006)

# Backpack becomes a protected propulsion plant with service cavities and vectoring engines.
for s,side in ((-1,'R'),(1,'L')):
    pod=bpy.data.objects['Backpack_Main_Deploy_Hinge.'+side]
    vane=bpy.data.objects['Backpack_Outer_Fold_Pivot.'+side]
    def mir(ps):return [(s*x,y,z) for x,y,z in ps]
    joint('Backpack_Deployment_Servo.'+side,(s*.67,.93,8.64),(s,0,0),.245,.45,pod)
    piston('Backpack_Deploy_Actuator.'+side,(s*.47,.86,7.77),(s*1.11,1.25,8.34),.083,pod)
    surface_module('Backpack_Main_Cooling_Recess.'+side,'Backpack_Rear_Overlapping_Shell.'+side,s*.79,8.19,.27,.52,pod,side='back',bars=6)
    surface_module('Backpack_Fin_Upper_Intake.'+side,'Backpack_Tall_Folded_Fin.'+side,s*.85,9.46,.24,.37,pod,side='back',bars=4)
    plate('Backpack_Fin_Armored_Leading_Edge.'+side,mir([(.49,1.04,8.8),(.71,.921,10.08),(.90,1.03,10.31),(.83,1.04,9.67),(.63,1.15,8.65)]),(0,.075,0),pod,PANEL,role='propulsion')
    plate('Backpack_Lower_Engine_Service_Shell.'+side,mir([(.49,1.73,7.28),(.95,1.87,7.26),(1.02,1.80,6.93),(.81,1.71,6.70),(.52,1.55,6.86)]),(0,-.09,0),pod,NAVY,role='propulsion')
    plate('Backpack_Outer_Vane_Protective_Trim.'+side,mir([(1.10,1.21,8.87),(1.27,1.42,8.91),(1.43,1.60,7.45),(1.33,1.47,6.77),(1.23,1.38,7.30)]),(0,-.085,0),vane,PANEL,role='propulsion')
    plate('Backpack_Outer_Vane_White_Accent.'+side,mir([(1.21,1.45,7.82),(1.36,1.59,7.75),(1.40,1.57,7.38),(1.26,1.44,7.42)]),(0,-.018,0),vane,WHITE,role='propulsion',bevel=.005)
    exhaust('Main_Thruster.'+side,(s*.76,1.40,7.10),(s*.74,1.49,6.34),.30,pod)
    exhaust('Vector_Thruster.'+side,(s*1.28,1.33,7.19),(s*1.23,1.42,6.51),.178,vane)
    cable('Backpack_Power_Trunk.'+side,[(s*.26,1.42,8.57),(s*.43,1.64,8.54),(s*.56,1.62,8.9)],.043,pod)
    cable('Backpack_Vector_Control_Hose.'+side,[(s*.87,1.26,8.1),(s*1.0,1.43,7.93),(s*1.18,1.43,8.11)],.026,vane)
    for x,y,z in ((.65,1.84,8.84),(.85,1.89,7.55),(1.3,1.54,7.99)):
        fastener('Backpack_Captive_Latch_%.2f.%s'%(z,side),(s*x,y,z),(0,1,0),.03,pod)
surface_module('Backpack_Central_Control_Cooling','Backpack_Central_Reactor_Spine',0,8.26,.33,.63,PACK,side='back',bars=5)
exhaust('Central_Thruster',(0,1.1,6.76),(0,1.26,6.0),.25,PACK)
block('Backpack_Control_Optics_Housing',(0,1.52,8.80),(.35,.16,.16),PACK,GUN)
block('Backpack_Control_Optics_Lens',(0,1.616,8.80),(.22,.015,.065),PACK,GLASS,bevel=.008)

# Compact heavy cannon: recoil rails, armored receiver, optical rangefinder and supported mount.
cm=bpy.data.objects['Cannon_Right_Cradle_Mount']
joint('Cannon_Yaw_Base',(-1.62,.35,9.31),(0,0,1),.22,.16,cm)
joint('Cannon_Pitch_Servo',(-1.63,.15,9.58),(-1,0,0),.20,.74,PITCH)
block('Cannon_Receiver_Top_Armor',(-1.63,.22,9.925),(.49,.74,.12),PITCH,NAVY,role='cannon',bevel=.03)
for s in (-1,1):
    x=-1.63+s*.319
    plate('Cannon_Receiver_Side_Armor_%s'%s,[(x,-.21,9.82),(x,.62,9.82),(x,.73,9.60),(x,.49,9.38),(x,-.14,9.39)],(s*.07,0,0),PITCH,NAVY,role='cannon',bevel=.024)
    piston('Cannon_Recoil_Rail_%s'%s,(-1.63+s*.213,-.13,9.79),(-1.63+s*.213,-.94,9.80),.051,PITCH)
    for j in range(3):
        block('Cannon_Receiver_Cooling_%s_%s'%(s,j),(x+s*.076,.02+j*.15,9.69),(.023,.074,.105),PITCH,FRAME,role='cannon',bevel=.006)
    for y in (-.13,.53):fastener('Cannon_Receiver_Latch_%s_%s'%(s,y),(x+s*.08,y,9.47),(s,0,0),.028,PITCH)
for j,y in enumerate((-.37,-.74,-1.01)):
    ring('Cannon_Barrel_Support_Collar_%d'%j,(-1.63,y+.04,9.64),(-1.63,y-.04,9.64),.268,.237,PITCH,NOZZLE,n=32)
for j in range(4):
    a=j*math.tau/4+math.pi/4;v=Vector((.253*math.cos(a),0,.253*math.sin(a)))
    assign(beam('Cannon_Vented_Heat_Shroud_Rib_%d'%j,Vector((-1.63,-.3,9.64))+v,Vector((-1.63,-1.1,9.66))+v,.035,.044,'cannon',PITCH),GUN)
block('Cannon_Rangefinder_Housing',(-2.03,-.08,9.87),(.15,.37,.18),PITCH,FRAME,role='cannon',bevel=.026)
rod('Cannon_Rangefinder_Bezel',(-2.03,-.25,9.87),(-2.03,-.292,9.87),.073,PITCH,STEEL,n=24)
rod('Cannon_Rangefinder_Lens',(-2.03,-.293,9.87),(-2.03,-.299,9.87),.052,PITCH,GLASS,n=32)
cable('Cannon_Control_Loom',[(-.89,.84,8.93),(-1.29,.77,9.35),(-1.70,.62,9.50)],.038,cm)

# Work in weapon local coordinates while adding hardware, then restore the display transform.
sword_matrix=SWORD.matrix_world.copy();SWORD.matrix_world=Matrix.Identity(4);bpy.context.view_layer.update()
assign(bpy.data.objects['Blade_Thick_Physical_Body'],NAVY)
assign(bpy.data.objects['Blade_Side_Load_Plate'],PANEL)
assign(bpy.data.objects['Blade_Structural_Mechanical_Spine'],GUN)
surface_module('Blade_Generator_Vent','Blade_Mechanical_Emitter_Block',.04,1.31,.25,.19,SWORD,bars=3)
plate('Blade_Emitter_Armored_Guard',[(-.38,-.17,1.03),(-.13,-.23,1.00),(.25,-.21,1.10),(.38,-.13,1.28),(.27,-.25,1.31),(-.31,-.24,1.21)],(0,.12,0),SWORD,WHITE,role='weapon')
block('Blade_Generator_Power_Core',(.25,.03,1.47),(.24,.38,.20),SWORD,GUN,role='weapon',bevel=.033)
rod('Blade_Generator_Core_Bezel',(.24,-.225,1.47),(.24,-.271,1.47),.094,SWORD,STEEL,n=32)
rod('Blade_Generator_Core_Lens',(.24,-.273,1.47),(.24,-.28,1.47),.069,SWORD,GLASS,n=32)
for j in range(6):
    ring('Blade_Grip_Insulator_%d'%j,(0,0,.22+j*.098),(0,0,.247+j*.098),.098,.088,SWORD,RUBBER,n=20)
ring('Blade_Pommel_Retainer',(0,0,.025),(0,0,.08),.13,.097,SWORD,STEEL,n=24)
block('Blade_Hand_Stop',(0,.04,.99),(.43,.22,.09),SWORD,GUN,role='weapon',bevel=.022)
for j,z in enumerate((2.18,3.54,4.91)):
    plate('Blade_Spine_Service_Module_%d'%j,[(-.375,-.181,z-.21),(-.12,-.178,z-.12),(-.105,-.161,z+.3),(-.386,-.176,z+.23)],(0,.075,0),SWORD,FRAME,role='weapon')
    block('Blade_Spine_Load_Rail_%d'%j,(-.26,-.226,z),(.24,.046,.075),SWORD,STEEL,role='weapon',bevel=.008)
    fastener('Blade_Spine_Captive_Bolt_%d'%j,(-.254,-.255,z),(0,-1,0),.032,SWORD)
plate('Blade_Emitter_Conduit_Cover',[(-.015,-.253,1.56),(.15,-.238,1.69),(.21,-.17,2.32),(.07,-.22,2.22)],(0,.065,0),SWORD,GUN,role='weapon')
plate('Blade_Energy_Channel_Window',[(.06,-.267,1.80),(.102,-.251,1.82),(.147,-.191,2.15),(.095,-.214,2.09)],(0,.014,0),SWORD,GLASS,role='weapon',bevel=.004)
plate('Blade_Secondary_Speed_Edge',[(-.035,-.231,2.35),(.179,-.211,2.42),(.179,-.16,5.66),(-.08,-.209,5.96)],(0,.035,0),SWORD,NAVY,role='weapon',bevel=.009)
for z in (1.16,1.73):fastener('Blade_Generator_Lock_%.2f'%z,(-.18,-.26,z),(0,-1,0),.026,SWORD)
SWORD.matrix_world=sword_matrix
SWORD['Beam_On']=True
SWORD.id_properties_ui('Beam_On').update(description='Switch the continuous independent plasma cutting edge on or off')
edge=bpy.data.objects['Blade_Continuous_Energy_Edge']
for prop in ('hide_render','hide_viewport'):
    d=edge.driver_add(prop).driver;d.type='SCRIPTED';v=d.variables.new();v.name='enabled';v.type='SINGLE_PROP'
    v.targets[0].id=SWORD;v.targets[0].data_path='["Beam_On"]';d.expression='not enabled'

# Every visible hard-surface edge receives real bevel geometry and clean custom normals.
for o in list(scene.objects):
    if o.type!='MESH' or o.name.endswith('BooleanVolume'):continue
    for mod in o.modifiers:
        if mod.type=='BEVEL':
            mod.segments=3;mod.harden_normals=True
            if mod.width<.0013:mod.width=.0013
    for p in o.data.polygons:p.use_smooth=True
    bm=bmesh.new();bm.from_mesh(o.data)
    for e in bm.edges:
        if len(e.link_faces)==2:e.smooth=e.calc_face_angle()<math.radians(40)
    bm.to_mesh(o.data);bm.free()
    wn=o.modifiers.new('Weighted production normals','WEIGHTED_NORMAL');wn.keep_sharp=True;wn.weight=50

# Clear project structure. Helpers are retained and hidden for non-destructive editing.
C['controls'].name='BODY';C['frame'].name='FRAME';C['primary'].name='ARMOR';C['secondary'].name='Secondary_Armor'
scene.collection.children.unlink(C['secondary']);C['primary'].children.link(C['secondary'])
C['joints'].name='Joint_Housings';scene.collection.children.unlink(C['joints']);C['frame'].children.link(C['joints'])
C['propulsion'].name='BACKPACK';C['cannon'].name='SHOULDER_CANNON';C['weapon'].name='WEAPON'
headcoll=bpy.data.collections.new('HEAD');scene.collection.children.link(headcoll)
for o in list(scene.objects):
    if o.type=='MESH' and any(o.name.startswith(k) for k in ('Head_','Helmet_','Face_','Visor_','Cheek_','Jaw_','Forehead_','Rear_Head','Neck_')):
        for c in list(o.users_collection):c.objects.unlink(o)
        headcoll.objects.link(o)
lights=bpy.data.collections.new('LIGHTS');cameras=bpy.data.collections.new('CAMERAS')
scene.collection.children.link(lights);scene.collection.children.link(cameras)
for o in list(C['studio'].objects):
    C['studio'].objects.unlink(o);(lights if o.type=='LIGHT' else cameras).objects.link(o)
scene.collection.children.unlink(C['studio'])
materials=bpy.data.collections.new('MATERIALS');scene.collection.children.link(materials)
palette=bpy.data.objects.new('Material_Palette_and_Usage',None);materials.objects.link(palette)
for m in MATS:palette[m.name]='Master material; bake into demo atlas'
for layer in scene.view_layers:layer.material_override=None
scene.render.resolution_x=2560;scene.render.resolution_y=1440;scene.render.resolution_percentage=100
scene.cycles.samples=64
scene['master_revision']='01: mechanical construction and functional surface details'
scene['user_briefs']='USER_HIGH_DETAIL_BRIEF.txt + USER_DEMO_ASSET_BRIEF.txt'
for a in bpy.context.screen.areas if bpy.context.screen else []:
    if a.type=='VIEW_3D':a.spaces.active.shading.color_type='MATERIAL'
for o in scene.objects:o.select_set(False)
ROOT.select_set(True);bpy.context.view_layer.objects.active=ROOT
script=bpy.data.texts.load(str(OUT/'build_master.py'));script.name='build_master.py | executed'
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'V2_WORKING.blend'))
(OUT/'logs'/'master_construction.json').write_text(json.dumps({'file':str(OUT/'V2_WORKING.blend'),
    'meshes':sum(o.type=='MESH' and not o.hide_render for o in scene.objects),
    'curves':sum(o.type=='CURVE' for o in scene.objects),'materials':len(MATS),
    'elapsed_seconds':time.time()-T,'techniques':['custom armor polyhedra','non-destructive Boolean cavities','multi-segment bevels',
    'weighted normals','concentric rotary joints','hydraulic actuators','swept cable routes','articulated phalanges','segmented engine petals']},indent=2),encoding='utf8')
print('V2_MASTER_CONSTRUCTION_COMPLETE',flush=True)
