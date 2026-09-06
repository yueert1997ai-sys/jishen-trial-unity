"""Fresh v1 geometry, built from reference silhouette rather than the rejected v0.
Local Blender 5.2.1. Phase A = mass review. Phase F = revised mass + major armor planes.
No existing model is loaded. No texture, decal, weathering, rig or game integration.
"""
import bpy, bmesh, math, pathlib, json, sys, time
from mathutils import Vector, Matrix
OUT=pathlib.Path(__file__).resolve().parent
PHASE='F' if 'final' in sys.argv else 'A'
S=.325
T=time.time()
bpy.ops.wm.read_factory_settings(use_empty=True)
scene=bpy.context.scene
scene.name='JISHEN_V1_GRAY_REVIEW'
scene.unit_settings.system='METRIC'
scene.unit_settings.scale_length=1
scene['stage']='Silhouette approval only; no material/detail approval'
scene['source']='Fresh meshes; no v0 geometry loaded'
scene['phase']=PHASE
scene['crown_height_m']=3.25
scene['coordinates']='Z up; -Y front; -X character right'
bpy.context.preferences.filepaths.save_version=0

def collection(name):
    c=bpy.data.collections.new(name);scene.collection.children.link(c);return c
C={k:collection(v) for k,v in {
 'frame':'10_INTERNAL_FRAME','primary':'20_PRIMARY_ARMOR','secondary':'30_SECONDARY_ARMOR',
 'joints':'40_JOINTS','propulsion':'50_PROPULSION_BACKPACK','cannon':'60_RIGHT_CANNON_AND_CRADLE',
 'weapon':'70_ANTISHIP_BLADE','controls':'00_ASSEMBLY_PIVOTS','studio':'90_GRAY_REVIEW_CAMERAS'}.items()}

def material(name,value):
    m=bpy.data.materials.new(name);m.diffuse_color=(value,value,value,1)
    m.use_nodes=True
    p=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
    p.inputs['Base Color'].default_value=(value,value,value,1)
    p.inputs['Roughness'].default_value=.75
    p.inputs['Metallic'].default_value=0
    return m
CLAY=material('CLAY_ONLY_No_Styling',.39)
EDGE=material('Independent_LightEdge_GRAY_placeholder',.39)
EYES=material('Twin_Sensors_GRAY_placeholder',.39)
EDGE['future_color']='Blue continuous beam, only after silhouette approval'
EYES['future_color']='Cold blue twin sensors, only after silhouette approval'

def parent_keep(o,parent):
    if parent:
        bpy.context.view_layer.update()
        m=o.matrix_world.copy();o.parent=parent;o.matrix_world=m
    return o

def pivot(name,at,parent=None):
    o=bpy.data.objects.new(name,None);C['controls'].objects.link(o)
    o.location=Vector(at)*S;o.empty_display_type='PLAIN_AXES';o.empty_display_size=.06
    parent_keep(o,parent)
    return o
ROOT=pivot('V1_Root_Ground',(0,0,0))
ROOT['height_crown_m']=3.25
ROOT['approval']='PENDING USER SILHOUETTE REVIEW'

def mesh(name,vertices,faces,role='primary',parent=ROOT,bevel=None,mat=CLAY):
    d=bpy.data.meshes.new(name+'_Mesh');d.from_pydata([Vector(v)*S for v in vertices],[],faces);d.update()
    bm=bmesh.new();bm.from_mesh(d);bmesh.ops.recalc_face_normals(bm,faces=bm.faces);bm.to_mesh(d);bm.free()
    o=bpy.data.objects.new(name,d);C[role].objects.link(o)
    centre=sum((v.co for v in d.vertices),Vector())/len(d.vertices)
    for v in d.vertices:v.co-=centre
    o.location=centre;d.materials.append(mat);o.color=mat.diffuse_color
    parent_keep(o,parent)
    width=(.010 if PHASE=='F' else .003) if bevel is None else bevel
    if width:
        m=o.modifiers.new('Structural edge chamfer','BEVEL');m.width=width*S;m.segments=1
    o['structural_role']=role
    return o

def hull(name,points,role='primary',parent=ROOT,bevel=None,mat=CLAY):
    bm=bmesh.new()
    for p in points:bm.verts.new(Vector(p)*S)
    bm.verts.ensure_lookup_table()
    result=bmesh.ops.convex_hull(bm,input=list(bm.verts),use_existing_faces=False)
    loose=[v for v in bm.verts if not v.link_faces]
    if loose:bmesh.ops.delete(bm,geom=loose,context='VERTS')
    bmesh.ops.dissolve_limit(bm,angle_limit=.001,verts=list(bm.verts),edges=list(bm.edges))
    bm.verts.ensure_lookup_table();bm.faces.ensure_lookup_table()
    vv=[tuple(v.co/S) for v in bm.verts];ff=[tuple(v.index for v in f.verts) for f in bm.faces];bm.free()
    return mesh(name,vv,ff,role,parent,bevel,mat)

def slab(name,outline,depth,role='primary',parent=ROOT,bevel=None,mat=CLAY):
    # Every point carries its own front depth: swept, thick 3D armor, not a flat patch.
    return hull(name,list(outline)+[tuple(Vector(v)+Vector(depth)) for v in outline],role,parent,bevel,mat)

def beam(name,a,b,rx,ry,role='frame',parent=ROOT):
    a,b=Vector(a),Vector(b);q=(b-a).to_track_quat('Z','Y');v=[]
    for pt in (a,b):
        v += [tuple(pt+q@Vector((x*rx,y*ry,0))) for x,y in ((-1,-.7),(-.7,-1),(.7,-1),(1,-.7),(1,.7),(.7,1),(-.7,1),(-1,.7))]
    return hull(name,v,role,parent)

def rings(name,stations,role='primary',parent=ROOT):
    # Stations (z, x, y, full width, forward depth, rear depth), with a central keel.
    pts=[]
    for z,x,y,w,fr,re in stations:
        pts += [(x,y-fr*1.10,z),(x+w*.38,y-fr*.91,z),(x+w*.5,y-fr*.52,z),
                (x+w*.5,y+re*.60,z),(x+w*.32,y+re,z),(x-w*.32,y+re,z),
                (x-w*.5,y+re*.6,z),(x-w*.5,y-fr*.52,z),(x-w*.38,y-fr*.91,z)]
    return hull(name,pts,role,parent)

def tube(name,a,b,outer,inner,role='joints',parent=ROOT,n=12):
    a,b=Vector(a),Vector(b);q=(b-a).to_track_quat('Z','Y');v=[]
    for pt,r in ((a,outer),(b,outer),(a,inner),(b,inner)):
        v += [tuple(pt+q@Vector((r*math.cos(i*2*math.pi/n),r*math.sin(i*2*math.pi/n),0))) for i in range(n)]
    f=[]
    for j in range(n):
        k=(j+1)%n;f += [(j,k,n+k,n+j),(2*n+j,3*n+j,3*n+k,2*n+k),(j,2*n+j,2*n+k,k),(n+j,n+k,3*n+k,3*n+j)]
    return mesh(name,v,f,role,parent)

def axial(name,a,b,r,role='frame',parent=ROOT,r2=None,n=10):
    a,b=Vector(a),Vector(b);q=(b-a).to_track_quat('Z','Y');v=[]
    for p,rr in ((a,r),(b,r if r2 is None else r2)):
        v += [tuple(p+q@Vector((rr*math.cos(i*2*math.pi/n),rr*math.sin(i*2*math.pi/n),0))) for i in range(n)]
    return hull(name,v,role,parent)

PELVIS=pivot('Pelvis',(0,0,6.05),ROOT)
WAIST=pivot('Waist',(0,0,6.7),PELVIS)
CHEST=pivot('Thorax',(0,.03,7.34),WAIST)
HEAD=pivot('Head',(0,0,9.24),CHEST)

# A wedge-shaped ribcage; the chest carries mass forward and folds around its sides.
rings('Thorax_Internal_Cage',[(7.2,0,.10,1.2,.43,.52),(8.0,0,.1,2.26,.60,.62),(8.74,0,.16,2.6,.59,.53),(9.01,0,.12,1.55,.38,.39)],'frame',CHEST)
rings('Waist_Narrow_Actuator_Core',[(6.35,0,.08,.90,.35,.36),(7.35,0,.08,1.07,.40,.4)],'frame',WAIST)
rings('Pelvis_Structure',[(5.64,0,.04,.81,.34,.32),(6.18,0,.02,1.62,.53,.47),(6.53,0,.08,1.40,.41,.42)],'frame',PELVIS)
for s,side in ((-1,'R'),(1,'L')):
    def mirror(pts):return [(s*x,y,z) for x,y,z in pts]
    slab('Thorax_Pectoral_Wrapped.'+side,mirror([
        (.055,-.86,8.85),(.44,-.93,9.03),(1.13,-.73,8.90),(1.45,-.37,8.63),
        (1.27,-.54,8.15),(.60,-.98,8.10),(.20,-1.02,8.38)]),(0,.46,0),'primary',CHEST)
    slab('Thorax_Lateral_Rib_Armor.'+side,mirror([(.49,-.67,8.09),(1.23,-.40,8.04),(1.23,.03,7.78),(.62,-.30,7.21),(.38,-.63,7.46)]),(0,.45,0),'primary',CHEST)
    slab('Clavicle_Swept_Carapace.'+side,mirror([(.22,-.26,9.03),(.91,-.28,9.15),(1.42,.03,8.92),(1.24,.39,8.81),(.51,.16,8.93)]),(0,.0,-.20),'primary',CHEST)
    beam('Waist_Diagonal_Link.'+side,(s*.40,.02,6.39),(s*.59,-.05,7.33),.085,.10,'frame',WAIST)
    tube('Hip_Bearing.'+side,(s*.45,.05,6.17),(s*.84,.05,6.17),.29,.18,'joints',PELVIS)
    sh=pivot('Skirt_Hinge.'+side,(s*.64,-.19,6.3),PELVIS)
    slab('Skirt_Front_Primary.'+side,mirror([(.28,-.49,6.40),(.79,-.55,6.39),(1.0,-.71,5.65),(.85,-.67,5.05),(.49,-.55,5.20),(.37,-.46,5.8)]),(0,.22,0),'primary',sh)
    slab('Skirt_Side_Primary.'+side,mirror([(.85,-.12,6.4),(1.18,.13,6.19),(1.49,.14,5.39),(1.28,-.07,4.96),(1.09,-.31,5.64)]),(0,.43,0),'primary',sh)
    slab('Skirt_Rear_Primary.'+side,mirror([(.29,.50,6.29),(.86,.59,6.26),(1.14,.64,5.64),(.88,.77,5.10),(.39,.63,5.3)]),(0,-.22,0),'primary',sh)
slab('Sternum_Forward_Keel',[(0,-.94,8.90),(.30,-1.09,8.52),(.29,-1.07,7.96),(0,-.76,7.33),(-.29,-1.07,7.96),(-.30,-1.09,8.52)],(0,.40,0),'primary',CHEST)
slab('Abdomen_Single_Tapered_Guard',[(-.42,-.45,7.21),(.42,-.45,7.21),(.35,-.44,6.63),(0,-.54,6.45),(-.35,-.44,6.63)],(0,.18,0),'primary',WAIST)
slab('Pelvis_Central_Armor',[(-.28,-.52,6.27),(.28,-.52,6.27),(.34,-.65,5.87),(0,-.59,5.45),(-.34,-.65,5.87)],(0,.25,0),'primary',PELVIS)

# Small angular head: a split face opening, helmet cheek blades and short crown.
axial('Neck_Gimbal',(0,0,9.07),(0,0,9.40),.17,'joints',HEAD)
rings('Helmet_Main_Faceted_Volume',[(9.34,0,.02,.51,.26,.24),(9.65,0,.025,.80,.32,.30),(9.86,0,.07,.69,.25,.30),(10,0,.09,.29,.17,.16)],'primary',HEAD)
slab('Helmet_Central_Forehead',[(0,-.32,9.99),(.17,-.34,9.81),(.13,-.46,9.60),(0,-.49,9.54),(-.13,-.46,9.60),(-.17,-.34,9.81)],(0,.10,0),'primary',HEAD)
slab('Head_Hard_Mask',[(-.145,-.36,9.55),(.145,-.36,9.55),(.21,-.39,9.39),(0,-.56,9.25),(-.21,-.39,9.39)],(0,.18,0),'primary',HEAD)
for s,side in ((-1,'R'),(1,'L')):
    def mir(pts):return [(s*x,y,z) for x,y,z in pts]
    slab('Helmet_Brow_Knife.'+side,mir([(.045,-.41,9.63),(.32,-.40,9.75),(.41,-.23,9.67),(.30,-.44,9.61),(.10,-.485,9.56)]),(0,.08,0),'secondary',HEAD)
    slab('Head_Twin_Sensor.'+side,mir([(.08,-.414,9.56),(.305,-.397,9.64),(.285,-.42,9.575),(.10,-.44,9.51)]),(0,.02,0),'secondary',HEAD,.002,EYES)
    slab('Helmet_Cheek_Blade.'+side,mir([(.29,-.29,9.61),(.46,-.10,9.64),(.40,-.21,9.29),(.17,-.37,9.21),(.23,-.38,9.41)]),(0,.19,0),'primary',HEAD)
    hull('Helmet_Short_Crown.'+side,mir([(.29,.05,9.79),(.36,.12,9.77),(.45,.10,10.13),(.34,.02,9.98)]),'secondary',HEAD,.002)

# Leg masses: slender femur housings, long enlarged calf nacelles, heavy split toes.
for s,side in ((-1,'R'),(1,'L')):
    def mir(pts):return [(s*x,y,z) for x,y,z in pts]
    hip=(s*.70,.03,6.20);knee=(s*.92,-.06,3.72);ankle=(s*1.10,.07,.83)
    thigh=pivot('Thigh.'+side,hip,PELVIS)
    shin=pivot('Shin.'+side,knee,thigh)
    foot=pivot('Foot.'+side,ankle,shin)
    beam('Femur_Load_Frame.'+side,hip,knee,.19,.19,'frame',thigh)
    rings('Thigh_Long_Inner_Housing.'+side,[(4.05,s*.90,.05,.46,.32,.31),(5.30,s*.74,.06,.68,.35,.35),(5.91,s*.70,.06,.59,.29,.33)],'frame',thigh)
    slab('Thigh_Main_Lateral_Shell.'+side,mir([(.54,-.31,5.80),(.86,-.32,5.89),(1.08,-.22,5.43),(1.15,-.20,4.67),(.96,-.41,4.02),(.70,-.35,4.15),(.60,-.34,4.80)]),(0,.36,0),'primary',thigh)
    slab('Thigh_Rear_Carapace.'+side,mir([(.56,.42,5.69),(.94,.45,5.61),(1.10,.37,4.55),(.85,.46,4.04),(.65,.29,4.25)]),(0,-.19,0),'primary',thigh)
    tube('Knee_Large_Hinge.'+side,(s*.60,-.055,3.73),(s*1.25,-.055,3.73),.27,.17,'joints',shin)
    axial('Knee_Axle.'+side,(s*.65,-.055,3.73),(s*1.25,-.055,3.73),.13,'frame',shin)
    slab('Knee_Angular_Floating_Shield.'+side,mir([(.64,-.36,3.94),(.99,-.48,4.05),(1.24,-.32,3.83),(1.18,-.35,3.47),(.97,-.56,3.34),(.68,-.38,3.57)]),(0,.21,0),'primary',shin)
    beam('Tibia_Main_Load_Strut.'+side,(s*.94,.12,3.63),ankle,.17,.19,'frame',shin)
    # Main calf shell moves rearward as it broadens; front shin keel remains slender.
    rings('Calf_Deep_Main_Nacelle.'+side,[(1.28,s*1.10,.14,.52,.28,.31),
        (2.08,s*1.10,.28,1.07,.49,.63),(2.96,s*1.02,.28,1.20,.53,.66),
        (3.49,s*.95,.12,.77,.38,.44)],'primary',shin)
    slab('Calf_Outer_Heavy_Carapace.'+side,mir([(1.14,-.17,3.37),(1.53,.06,3.17),(1.75,.21,2.54),(1.64,.18,1.93),(1.31,-.02,2.12),(1.15,-.22,2.54)]),(0,.50,0),'primary',shin)
    slab('Shin_Long_Forward_Keel.'+side,mir([(.69,-.39,3.19),(1.12,-.55,3.23),(1.34,-.40,2.79),(1.23,-.42,2.02),(1.20,-.35,1.18),(.99,-.53,.96),(.83,-.39,1.24),(.83,-.48,2.22)]),(0,.31,0),'primary',shin)
    slab('Calf_Rear_Spur_Armor.'+side,mir([(.73,.90,3.03),(1.21,.95,2.86),(1.47,.66,2.2),(1.24,.68,1.45),(.90,.74,1.76)]),(0,-.23,0),'primary',shin)
    axial('Calf_Rear_Actuator.'+side,(s*.82,.67,3.15),(s*.96,.50,1.14),.070,'frame',shin)
    axial('Calf_Actuator_Sleeve.'+side,(s*.82,.67,3.15),(s*.90,.60,2.20),.11,'frame',shin)
    tube('Ankle_Bearing.'+side,(s*.78,.07,.83),(s*1.40,.07,.83),.23,.145,'joints',foot)
    slab('Ankle_Armored_Bridge.'+side,mir([(.78,-.18,1.25),(1.13,-.36,1.34),(1.43,-.04,.89),(1.32,-.31,.53),(.94,-.38,.56),(.76,-.28,.93)]),(0,.31,0),'primary',foot)
    xx=s*1.11
    hull('Foot_Heel_Load_Block.'+side,[(xx-.40,.02,.06),(xx+.40,.02,.06),(xx-.39,.66,.06),(xx+.39,.66,.06),
        (xx-.28,-.02,.62),(xx+.28,-.02,.62),(xx-.32,.49,.51),(xx+.32,.49,.51)],'frame',foot)
    hull('Foot_Armored_Instep.'+side,[(xx-.39,-.92,.09),(xx+.39,-.92,.09),(xx-.35,.39,.15),(xx+.35,.39,.15),
        (xx-.31,-.71,.38),(xx+.31,-.71,.38),(xx-.26,-.13,.82),(xx+.26,-.13,.82),
        (xx-.27,.28,.67),(xx+.27,.28,.67)],'primary',foot)
    for j,dx in enumerate((-.215,.215)):
        hull('Foot_Split_Heavy_Toe_%d.%s'%(j,side),[(xx+dx-.19,-1.22,.025),(xx+dx+.19,-1.22,.025),
            (xx+dx-.20,-.34,.035),(xx+dx+.20,-.34,.035),(xx+dx-.15,-1.14,.29),
            (xx+dx+.15,-1.14,.29),(xx+dx-.18,-.45,.52),(xx+dx+.18,-.45,.52)],'primary',foot)
    hull('Foot_Heel_Spur.'+side,[(xx-.31,.42,.03),(xx+.31,.42,.03),(xx-.32,.77,.03),(xx+.32,.77,.03),
        (xx-.27,.40,.35),(xx+.27,.40,.35),(xx-.23,.72,.23),(xx+.23,.72,.23)],'primary',foot)

# Moderate shoulders and wrapped forearm armor. Hands are sized around a 0.18-unit grip.
for s,side in ((-1,'R'),(1,'L')):
    def mir(pts):return [(s*x,y,z) for x,y,z in pts]
    shoulder=(s*1.61,.10,8.60);elbow=(s*1.89,.02,7.31);wrist=(s*2.20,-.08,5.75)
    arm=pivot('UpperArm.'+side,shoulder,CHEST)
    forearm=pivot('Forearm.'+side,elbow,arm)
    hand=pivot('Hand.'+side,wrist,forearm)
    shoulder_shell=pivot('Shoulder_Armor_Floating_Pivot.'+side,shoulder,CHEST)
    tube('Shoulder_Gimbal.'+side,(s*1.27,.10,8.6),(s*1.85,.10,8.6),.33,.22,'joints',arm)
    beam('UpperArm_Load_Frame.'+side,shoulder,elbow,.17,.17,'frame',arm)
    slab('Shoulder_Main_Wrapped_Carapace.'+side,mir([
        (1.23,-.30,9.05),(1.68,-.41,9.18),(2.10,-.20,9.01),(2.24,-.14,8.62),
        (2.06,-.45,8.22),(1.48,-.54,8.32),(1.27,-.49,8.72)]),(0,.84,-.06),'primary',shoulder_shell)
    slab('Shoulder_Outer_Downward_Shell.'+side,mir([(1.96,-.25,8.89),(2.25,-.12,8.73),(2.30,-.04,8.18),(2.10,-.35,7.92),(1.89,-.30,8.31)]),(0,.52,0),'primary',shoulder_shell)
    slab('UpperArm_Swept_Armor.'+side,mir([(1.50,-.24,8.27),(1.91,-.32,8.18),(2.11,-.18,7.75),(1.93,-.32,7.44),(1.69,-.41,7.62)]),(0,.50,0),'primary',arm)
    tube('Elbow_Bearing.'+side,(s*1.68,.02,7.31),(s*2.12,.02,7.31),.23,.135,'joints',forearm)
    beam('Forearm_Frame.'+side,elbow,wrist,.145,.16,'frame',forearm)
    rings('Forearm_Main_Gauntlet_Volume.'+side,[(5.93,s*2.19,-.07,.43,.28,.28),
        (6.31,s*2.21,-.05,.77,.49,.39),(6.83,s*2.10,-.01,.79,.45,.40),(7.12,s*1.99,.0,.50,.32,.27)],'primary',forearm)
    slab('Forearm_Outer_Armored_Blade.'+side,mir([(2.17,-.20,7.1),(2.42,-.11,6.85),(2.63,-.01,6.27),(2.41,-.19,5.76),(2.21,-.38,6.06),(2.19,-.39,6.62)]),(0,.36,0),'primary',forearm)
    axial('Wrist_Rotator.'+side,(s*2.2,-.08,5.91),(s*2.22,-.08,5.50),.155,'joints',hand)
    rings('Hand_Palm_Heavy.'+side,[(5.14,s*2.26,-.055,.47,.17,.17),(5.52,s*2.23,-.055,.48,.19,.17)],'frame',hand)
    slab('Hand_Dorsal_Armor.'+side,mir([(1.99,-.21,5.48),(2.38,-.28,5.51),(2.53,-.17,5.23),(2.41,-.20,5.09),(2.08,-.28,5.12)]),(0,.16,0),'primary',hand)
    for j in range(4):
        xx=s*(2.07+j*.12)
        beam('Hand_Curled_Finger_%d.%s'%(j,side),(xx,.03,5.11),(xx,-.19,4.96),.052,.075,'frame',hand)
    beam('Hand_Thumb.'+side,(s*1.995,-.02,5.33),(s*1.95,-.19,5.10),.073,.084,'frame',hand)
    hand['grip_reference_diameter_m']=.18*S

# Tall, nested folded propulsion modules are part of the silhouette from every view.
PACK=pivot('Backpack_Structural_Mount',(0,.64,8.4),CHEST)
rings('Backpack_Central_Reactor_Spine',[(6.35,0,1.01,.52,.24,.32),(7.03,0,1.05,.69,.33,.47),
    (8.76,0,.96,.79,.27,.52),(9.29,0,.83,.42,.19,.26)],'propulsion',PACK)
beam('Backpack_Upper_Load_Crossmember',(-.88,.72,8.73),(.88,.72,8.73),.17,.18,'frame',PACK)
beam('Backpack_Lower_Load_Crossmember',(-.71,.74,7.26),(.71,.74,7.26),.14,.17,'frame',PACK)
for s,side in ((-1,'R'),(1,'L')):
    def mir(pts):return [(s*x,y,z) for x,y,z in pts]
    pod=pivot('Backpack_Main_Deploy_Hinge.'+side,(s*.67,.89,8.71),PACK)
    pod['state']='FOLDED; independent deploy pivot; no animation'
    tube('Backpack_Pod_Hinge.'+side,(s*.41,.93,8.64),(s*.86,.93,8.64),.24,.145,'joints',pod)
    rings('Backpack_Main_Thruster_Core.'+side,[(6.63,s*.74,1.35,.43,.23,.3),(7.20,s*.82,1.39,.68,.33,.36),
        (8.70,s*.72,1.23,.62,.27,.34),(9.32,s*.73,1.11,.41,.21,.29)],'propulsion',pod)
    hull('Backpack_Tall_Folded_Fin.'+side,mir([
        (.47,1.19,7.43),(.89,1.76,7.10),(1.06,1.59,8.67),(.98,1.16,10.40),
        (.72,.92,10.10),(.40,1.07,8.79),(.43,1.04,7.67),(.78,1.92,7.84),
        (.95,1.68,9.46),(.89,1.32,10.21)]),'propulsion',pod)
    tube('Backpack_Main_Exhaust.'+side,(s*.76,1.40,7.08),(s*.74,1.48,6.42),.285,.196,'propulsion',pod)
    axial('Backpack_Exhaust_Recess.'+side,(s*.75,1.45,6.79),(s*.75,1.46,6.74),.188,'propulsion',pod)
    vane=pivot('Backpack_Outer_Fold_Pivot.'+side,(s*1.07,1.02,8.49),pod)
    vane['state']='Independent folded stabilizer with vector thruster'
    hull('Backpack_Outer_Folded_Propulsion_Vane.'+side,mir([
        (.99,.88,9.47),(1.30,1.12,9.72),(1.46,1.49,8.51),(1.54,1.47,7.12),
        (1.22,1.16,5.90),(1.05,.94,6.92),(1.17,1.36,8.18),(1.36,1.72,7.40),
        (1.37,1.46,6.87),(1.19,1.15,9.22)]),'propulsion',vane)
    tube('Backpack_Outer_Vector_Nozzle.'+side,(s*1.27,1.33,7.16),(s*1.23,1.4,6.61),.17,.115,'propulsion',vane)
    beam('Backpack_Deploy_Link.'+side,(s*.55,.85,8.12),(s*1.11,1.09,7.66),.075,.085,'frame',pod)
tube('Backpack_Central_Exhaust',(0,1.10,6.72),(0,1.25,6.08),.24,.16,'propulsion',PACK)

# The cannon is supported by a real bridge from the backpack, with a clear gap above armor.
CANNON=pivot('Cannon_Right_Cradle_Mount',(-.71,.80,8.89),PACK)
beam('Cannon_Diagonal_Load_Brace',(-.66,.74,8.6),(-1.58,.42,9.41),.15,.16,'cannon',CANNON)
beam('Cannon_Upper_Cradle_Beam',(-.7,.77,9.03),(-1.62,.55,9.35),.16,.18,'cannon',CANNON)
beam('Cannon_Forward_Support_Fork',(-1.63,.53,9.35),(-1.63,-.02,9.38),.16,.16,'cannon',CANNON)
PITCH=pivot('Cannon_Pitch_Trunnion',(-1.63,.15,9.59),CANNON)
tube('Cannon_Pitch_Bearing',(-1.99,.15,9.58),(-1.28,.15,9.58),.205,.12,'cannon',PITCH)
beam('Cannon_Receiver',(-1.63,.66,9.62),(-1.63,-.23,9.62),.30,.275,'cannon',PITCH)
axial('Cannon_Recoil_Jacket',(-1.63,-.19,9.62),(-1.63,-.78,9.65),.267,'cannon',PITCH)
tube('Cannon_Short_Heavy_Barrel',(-1.63,-.58,9.64),(-1.63,-1.43,9.67),.242,.17,'cannon',PITCH)
tube('Cannon_Armored_Muzzle',(-1.63,-1.14,9.66),(-1.63,-1.54,9.675),.31,.20,'cannon',PITCH)
axial('Cannon_Bore_Interior',(-1.63,-.97,9.652),(-1.63,-.99,9.653),.166,'cannon',PITCH)
CANNON['connection']='Backpack crossmember -> diagonal brace -> clevis -> pitch trunnion -> receiver'

# Mechanical anti-ship cleaver. No crossguard silhouette, no symmetric knight-sword tip.
SWORD=pivot('AntiShip_Blade_Display_Root',(0,0,0),None)
SWORD['length_m']=2.275
SWORD['height_ratio']=.7
SWORD['grip_diameter_m']=.18*S
SWORD['approval']='Physical mass only; gray placeholder sensor and energy materials'
axial('Blade_Pommel',(0,0,0),(0,0,.17),.123,'weapon',SWORD,n=8)
axial('Blade_Grip',(0,0,.14),(0,0,.97),.09,'weapon',SWORD,n=8)
axial('Blade_Grip_Collar',(0,0,.83),(0,0,1.07),.14,'weapon',SWORD,n=8)
hull('Blade_Mechanical_Emitter_Block',[(-.32,-.18,.95),(.20,-.18,.95),(-.34,.18,1.04),(.22,.18,1.04),
    (-.31,-.20,1.56),(.43,-.13,1.62),(-.31,.20,1.56),(.43,.13,1.62),(.45,-.11,1.29),(.45,.11,1.29)],'weapon',SWORD)
slab('Blade_Thick_Physical_Body',[(-.28,-.105,1.44),(.34,-.105,1.62),(.32,-.072,5.92),(.22,-.064,6.37),
    (-.055,-.012,7.0),(-.32,-.081,6.13)],(0,.21,0),'weapon',SWORD,.008)
slab('Blade_Structural_Mechanical_Spine',[(-.39,-.16,1.41),(-.16,-.16,1.47),(-.13,-.13,5.89),(-.055,-.012,7.0),(-.40,-.13,6.11)],(0,.32,0),'weapon',SWORD,.012)
slab('Blade_Continuous_Energy_Edge',[(.341,-.091,1.68),(.432,-.089,1.82),(.421,-.062,5.95),(.303,-.035,6.47),
    (-.055,-.006,7.0),(.216,-.038,6.35),(.319,-.065,5.9)],(0,.16,0),'weapon',SWORD,.001,EDGE)
slab('Blade_Side_Load_Plate',[(-.13,-.218,1.78),(.205,-.217,1.91),(.19,-.157,5.75),(-.16,-.18,6.06)],(0,.09,0),'weapon',SWORD,.007)
for z in (1.72,3.87):
    beam('Blade_Spine_Brace_%.1f'%z,(-.28,-.195,z),(-.28,.195,z),.145,.09,'weapon',SWORD)
bpy.context.view_layer.update()
SWORD.location=Vector((-3.60,-1.85,7.68))*S
SWORD.rotation_euler[1]=math.pi

# Stage F adds only major secondary armor planes after Stage A four-view review.
if PHASE=='F':
    for s,side in ((-1,'R'),(1,'L')):
        def mir(pts):return [(s*x,y,z) for x,y,z in pts]
        slab('Chest_Upper_Armored_Brow.'+side,mir([(.31,-.995,8.98),(.95,-.82,8.98),(1.39,-.49,8.75),(1.23,-.68,8.62),(.40,-1.1,8.76)]),(0,.12,0),'secondary',CHEST)
        slab('Chest_Lower_Undercut_Rail.'+side,mir([(.48,-.98,8.11),(1.13,-.70,8.05),(1.10,-.69,7.80),(.60,-.94,7.84)]),(0,.13,0),'secondary',CHEST)
        slab('Rib_Interlocking_Lower_Plate.'+side,mir([(.66,-.45,7.56),(1.05,-.15,7.62),(.86,-.15,7.18),(.50,-.51,7.08)]),(0,.15,0),'secondary',CHEST)
        pa=bpy.data.objects['Shoulder_Armor_Floating_Pivot.'+side]
        slab('Shoulder_Swept_Roof_Layer.'+side,mir([(1.27,-.29,9.12),(1.64,-.44,9.24),(2.11,-.19,9.06),(1.99,-.29,8.9),(1.49,-.54,8.96)]),(0,.50,-.10),'secondary',pa)
        slab('Shoulder_Front_Lamella.'+side,mir([(1.43,-.6,8.65),(1.91,-.50,8.55),(2.06,-.52,8.22),(1.85,-.57,8.01),(1.50,-.6,8.17)]),(0,.18,0),'secondary',pa)
        fa=bpy.data.objects['Forearm.'+side]
        slab('Forearm_Dorsal_Main_Overlay.'+side,mir([(1.80,-.38,7.02),(2.13,-.55,6.94),(2.37,-.44,6.60),(2.40,-.35,6.03),(2.17,-.51,5.77),(1.97,-.46,6.13)]),(0,.16,0),'secondary',fa)
        slab('Forearm_Inner_Oblique_Layer.'+side,mir([(1.87,-.36,6.91),(1.93,-.50,6.65),(2.11,-.53,6.08),(2.00,-.28,5.93),(1.76,-.22,6.55)]),(0,.18,0),'secondary',fa)
        sk=bpy.data.objects['Skirt_Hinge.'+side]
        slab('Skirt_Front_Overlap.'+side,mir([(.35,-.55,6.43),(.74,-.59,6.41),(.91,-.77,5.94),(.55,-.67,5.99)]),(0,.10,0),'secondary',sk)
        sh=bpy.data.objects['Shin.'+side]
        slab('Calf_Upper_Armored_Crown.'+side,mir([(.67,-.35,3.43),(1.09,-.45,3.55),(1.46,-.15,3.19),(1.29,-.32,2.98),(.85,-.62,3.06)]),(0,.27,0),'secondary',sh)
        slab('Calf_Outer_Overlapping_Winglet.'+side,mir([(1.35,-.04,3.10),(1.70,.20,2.79),(1.76,.17,2.12),(1.57,.01,1.85),(1.36,-.14,2.37)]),(0,.21,0),'secondary',sh)
        slab('Shin_Lower_Secondary_Keel.'+side,mir([(.95,-.6,2.44),(1.18,-.46,2.39),(1.23,-.43,1.30),(1.10,-.57,.99),(.94,-.54,1.46)]),(0,.13,0),'secondary',sh)
        bp=bpy.data.objects['Backpack_Main_Deploy_Hinge.'+side]
        slab('Backpack_Rear_Overlapping_Shell.'+side,mir([(.57,1.83,8.98),(.82,1.84,9.45),(.99,1.79,8.38),(.90,2.01,7.68),(.63,1.88,7.34)]),(0,-.16,0),'propulsion',bp)

# Fixed neutral inspection setup. No stage, environment asset or dramatic light.
world=bpy.data.worlds.new('Neutral_Gray_World');world.use_nodes=True
bg=next(n for n in world.node_tree.nodes if n.type=='BACKGROUND')
bg.inputs['Color'].default_value=(.72,.72,.72,1);bg.inputs['Strength'].default_value=.55;scene.world=world
def aim(o,t):o.rotation_euler=(Vector(t)-o.location).to_track_quat('-Z','Y').to_euler()
for name,at,power,size in [('Key',(-3,-4,6),650,4),('Fill',(4,-2,4),420,4),('Back',(0,4,5),550,4)]:
    d=bpy.data.lights.new('Review_'+name,'AREA');d.energy=power;d.size=size
    o=bpy.data.objects.new(d.name,d);C['studio'].objects.link(o);o.location=at;aim(o,(0,0,1.7))
for name,at,target,scale in [
    ('FRONT',(-.18,-12,1.73),(-.18,0,1.73),3.96),
    ('RIGHT',(-12,-.1,1.73),(0,-.1,1.73),3.96),
    ('BACK',(-.18,12,1.73),(-.18,0,1.73),3.96),
    ('THREE_QUARTER',(-7,-12,4.9),(-.14,0,1.74),4.18)]:
    d=bpy.data.cameras.new('CAM_'+name);d.type='ORTHO';d.ortho_scale=scale
    o=bpy.data.objects.new(d.name,d);C['studio'].objects.link(o);o.location=at;aim(o,target)
scene.camera=bpy.data.objects['CAM_THREE_QUARTER']
scene.render.engine='CYCLES';scene.cycles.samples=32;scene.cycles.use_denoising=True
scene.render.resolution_x=1120;scene.render.resolution_y=1400;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG';scene.render.image_settings.color_mode='RGB'
scene.view_settings.view_transform='AgX'
for layer in scene.view_layers:layer.material_override=CLAY
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            area.spaces.active.shading.type='SOLID';area.spaces.active.shading.color_type='SINGLE'
            area.spaces.active.shading.single_color=(.55,.55,.55)
            area.spaces.active.overlay.show_extras=False
            area.spaces.active.region_3d.view_location=Vector((-.12,0,1.72))
            area.spaces.active.region_3d.view_distance=5.4
            area.spaces.active.region_3d.view_rotation=scene.camera.rotation_euler.to_quaternion()
            area.spaces.active.region_3d.view_perspective='ORTHO'
for o in scene.objects:o.select_set(False)
ROOT.select_set(True);bpy.context.view_layer.objects.active=ROOT
script=bpy.data.texts.load(str(OUT/'build_v1.py'));script.name='build_v1.py | executed'
name='v1_silhouette.blend' if PHASE=='F' else 'stage_a/v1_mass_stage_A.blend'
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/name))
(OUT/'logs'/('build_'+PHASE+'.json')).write_text(json.dumps({'phase':PHASE,'file':str(OUT/name),
    'blender':bpy.app.version_string,'meshes':sum(o.type=='MESH' for o in scene.objects),'seconds':time.time()-T,
    'fresh_geometry':True,'material_override':'CLAY_ONLY_No_Styling','no_game_integration':True},indent=2),encoding='utf8')
print('V1_BUILD_COMPLETE',PHASE,str(OUT/name),flush=True)
