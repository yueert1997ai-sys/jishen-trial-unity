"""Revision 02, responding to inspected render_01 images. Run on V2_WORKING.blend.
Split armor with capped cross-sections, open obscured intakes, rework helmet and
load-bearing trim. All operations remain inside this isolated asset directory.
"""
import bpy,bmesh,math,pathlib,json,ast,time
from mathutils import Vector,Matrix
OUT=pathlib.Path(__file__).resolve().parent;S=.325;PHASE='F';scene=bpy.context.scene
ROOT=bpy.data.objects['JISHEN_Master_Root'];CLAY=bpy.data.materials[0]
C={k:bpy.data.collections[v] for k,v in {'primary':'ARMOR','secondary':'Secondary_Armor','frame':'FRAME','joints':'Joint_Housings','controls':'BODY','propulsion':'BACKPACK','cannon':'SHOULDER_CANNON','weapon':'WEAPON'}.items()}
# Reuse the executed construction helpers without running either construction again.
for file in ('build_core.py','build_master.py'):
    tree=ast.parse((OUT/file).read_text(encoding='utf8'))
    if file=='build_master.py':
        if not bpy.data.materials.get('03_Painted_Edge_Exposure'):
            m=bpy.data.materials.new('03_Painted_Edge_Exposure');m.use_nodes=True;m.use_fake_user=True
        NAVY,PANEL,EDGEPAINT,WHITE,FRAME,GUN,STEEL,NOZZLE,RUBBER,GLASS,BEAM,DEEP=[bpy.data.materials[n] for n in
        ['01_Painted_Military_Navy','02_Navy_Secondary_Panels','03_Painted_Edge_Exposure','04_Offwhite_Ceramic_Armor','05_Black_Mechanical_Frame','06_Dark_Gunmetal','07_Bare_Machined_Steel','08_Heat_Resistant_Thruster_Metal','09_Cable_Protection','10_Blue_Sensor_Glass','11_Blue_Beam_Plasma','12_Optical_Recess']]
    funcs=[n for n in tree.body if isinstance(n,ast.FunctionDef)]
    exec(compile(ast.Module(body=funcs,type_ignores=[]),file,'exec'),globals())
ctx=globals();CUTTERS=bpy.data.collections['Construction_Cutters | hidden']
CHEST=bpy.data.objects['Thorax'];HEAD=bpy.data.objects['Head'];WAIST=bpy.data.objects['Waist'];PACK=bpy.data.objects['Backpack_Structural_Mount']
SWORD=bpy.data.objects['AntiShip_Blade_Display_Root']
changes=[]

def delete_prefix(prefix):
    for o in list(scene.objects):
        if o.name.startswith(prefix):bpy.data.objects.remove(o,do_unlink=True)

def split_shell(name,height,gap=.032):
    """Actual two closed armor pieces separated along a service boundary."""
    o=bpy.data.objects[name];mw=o.matrix_world.copy();parts=[]
    for i in range(2):
        d=o.data.copy();no=o.copy();no.data=d;no.name=name+('_Upper' if i else '_Lower')
        for c in o.users_collection:c.objects.link(no)
        no.matrix_world=mw
        bm=bmesh.new();bm.from_mesh(d);bmesh.ops.transform(bm,matrix=mw,verts=bm.verts)
        z=(height+(gap/2 if i else -gap/2))*S
        bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=1e-7,
            plane_co=(0,0,z),plane_no=(0,0,1),clear_inner=bool(i),clear_outer=not bool(i))
        border=[e for e in bm.edges if e.is_boundary]
        if border:bmesh.ops.holes_fill(bm,edges=border,sides=0)
        bmesh.ops.recalc_face_normals(bm,faces=bm.faces)
        bmesh.ops.transform(bm,matrix=mw.inverted(),verts=bm.verts);bm.to_mesh(d);bm.free()
        no['function']='Separate removable armor with capped service seam'
        parts.append(no)
    bpy.data.objects.remove(o,do_unlink=True);return parts

def newfinish(o):
    if o.type!='MESH' or o.hide_render:return
    for m in o.modifiers:
        if m.type=='BEVEL':m.segments=3;m.harden_normals=True
    bm=bmesh.new();bm.from_mesh(o.data)
    for f in bm.faces:f.smooth=True
    for e in bm.edges:
        if len(e.link_faces)==2:e.smooth=e.calc_face_angle()<math.radians(35)
    bm.to_mesh(o.data);bm.free()
    if not any(m.type=='WEIGHTED_NORMAL' for m in o.modifiers):
        m=o.modifiers.new('Weighted production normals','WEIGHTED_NORMAL');m.keep_sharp=True

before=set(scene.objects)
# Darker paint with restrained, actual bevel wear. Noise is manufacturing roughness.
for mat,color in ((NAVY,(.011,.020,.043)),(PANEL,(.019,.034,.066)),(EDGEPAINT,(.041,.058,.088)),(WHITE,(.36,.39,.43))):
    mat.diffuse_color=(*color,1);p=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED');p.inputs['Base Color'].default_value=(*color,1)
for mat in (NAVY,PANEL):
    n=mat.node_tree.nodes;l=mat.node_tree.links;p=next(n for n in n if n.type=='BSDF_PRINCIPLED')
    tex=n.new('ShaderNodeTexNoise');tex.inputs['Scale'].default_value=620;tex.inputs['Detail'].default_value=2
    ramp=n.new('ShaderNodeValToRGB');ramp.name='Sparse finish scuff variation'
    col=tuple(p.inputs['Base Color'].default_value)
    ramp.color_ramp.elements[0].position=.2;ramp.color_ramp.elements[0].color=tuple(v*.80 for v in col[:3])+(1,)
    ramp.color_ramp.elements[1].position=.81;ramp.color_ramp.elements[1].color=tuple(v*1.18 for v in col[:3])+(1,)
    l.new(tex.outputs['Fac'],ramp.inputs[0]);l.new(ramp.outputs['Color'],p.inputs['Base Color'])
changes.append('Darkened navy paint and restrained finish variation')

# Less human eyebrow, more narrow framed optical assembly. Refit layered crown.
for s,side in ((-1,'R'),(1,'L')):
    def mir(p):return [(s*x,y,z) for x,y,z in p]
    delete_prefix('Visor_Upper_Armored_Rim.'+side)
    plate('Visor_Armored_Optical_Bridge.'+side,mir([(.02,-.506,9.61),(.27,-.474,9.701),(.38,-.342,9.742),(.31,-.449,9.632),(.065,-.525,9.558)]),(0,.034,0),HEAD,WHITE,bevel=.003)
    plate('Helmet_Crown_Front_Side_Shell.'+side,mir([(.09,-.286,9.982),(.235,-.223,9.985),(.344,-.208,9.868),(.389,-.247,9.768),(.292,-.368,9.723),(.199,-.384,9.858)]),(0,.036,-.008),HEAD,NAVY,bevel=.004)
    plate('Helmet_Crown_Temple_Layer.'+side,mir([(.303,-.178,9.948),(.356,.034,9.91),(.411,.12,9.772),(.440,-.016,9.724),(.40,-.22,9.79)]),(-s*.045,0,0),HEAD,PANEL,bevel=.004)
    plate('Helmet_Nape_Armor.'+side,mir([(.075,.396,9.68),(.285,.34,9.78),(.355,.28,9.55),(.23,.306,9.35),(.075,.327,9.40)]),(0,-.052,0),HEAD,NAVY,bevel=.005)
    # Narrow angular cheek inset and two purposeful optical cooling slots.
    plate('Face_Lateral_Ceramic_Facet.'+side,mir([(.15,-.46,9.44),(.215,-.425,9.42),(.16,-.481,9.31),(.065,-.54,9.26)]),(0,.03,0),HEAD,WHITE,bevel=.003)
    for j in range(2):
        plate('Cheek_Cooling_Slit_%d.%s'%(j,side),mir([(.31,-.363,9.54-j*.053),(.4,-.258,9.53-j*.053),(.395,-.27,9.51-j*.053),(.307,-.376,9.52-j*.053)]),(0,.009,0),HEAD,FRAME,bevel=.002)
changes.append('Layered helmet shell, angular continuous optical bridge and face facets')

for s,side in ((-1,'R'),(1,'L')):
    def mir(p):return [(s*x,y,z) for x,y,z in p]
    sh=bpy.data.objects['Shin.'+side];th=bpy.data.objects['Thigh.'+side];fa=bpy.data.objects['Forearm.'+side];pa=bpy.data.objects['Shoulder_Armor_Floating_Pivot.'+side];foot=bpy.data.objects['Foot.'+side]
    # Move the buried cooling cassette to the actual outermost shell: previous vent was occluded.
    delete_prefix('Calf_Flank_Heat_Exchanger.'+side)
    outer=bpy.data.objects['Calf_Outer_Heavy_Carapace.'+side]
    for mod in list(outer.modifiers):
        if mod.type=='BOOLEAN' and not mod.object:outer.modifiers.remove(mod)
    surface_module('Calf_Outer_Visible_Cooling.'+side,'Calf_Outer_Overlapping_Winglet.'+side,s*1.58,2.46,.23,.44,sh,bars=5)
    for name,z in [('Calf_Deep_Main_Nacelle.',2.40),('Calf_Outer_Heavy_Carapace.',2.03),('Shin_Long_Forward_Keel.',2.34),('Shin_Lower_Secondary_Keel.',1.62),('Shoulder_Main_Wrapped_Carapace.',8.73),('Shoulder_Outer_Downward_Shell.',8.39),('Thigh_Main_Lateral_Shell.',4.98)]:
        parts=split_shell(name+side,z)
        if name=='Calf_Deep_Main_Nacelle.':
            for o in parts:assign(o,GUN)
    # Front calf armor now floats above a dark actual inner nacelle, with exposure at the seam.
    plate('Calf_Upper_Oblique_Front_Panel.'+side,mir([(.57,-.238,3.10),(.71,-.42,3.08),(.82,-.49,2.59),(.77,-.36,2.46),(.48,-.17,2.59)]),(0,.10,0),sh,NAVY)
    plate('Calf_Lower_Oblique_Front_Panel.'+side,mir([(.51,-.18,2.35),(.78,-.38,2.40),(.83,-.36,1.95),(.93,-.29,1.45),(.72,-.1,1.76)]),(0,.09,0),sh,NAVY)
    surface_module('Shin_Front_Access_Recess.'+side,'Shin_Long_Forward_Keel.'+side+'_Upper',s*1.03,2.72,.15,.26,sh,bars=3)
    # Knee armour lip, separate soft seal and two transmission rails.
    plate('Knee_Leading_Armor_Lip.'+side,mir([(.69,-.405,3.92),(.985,-.509,4.03),(1.17,-.39,3.87),(1.11,-.45,3.80),(.94,-.552,3.92),(.72,-.442,3.83)]),(0,.054,0),sh,WHITE,bevel=.007)
    for dx in (-.12,.12):
        rod('Shin_Exposed_Transmission_'+str(dx)+'.'+side,(s*(.99+dx),-.44,2.37),(s*(1.06+dx),-.33,1.82),.029,sh,GUN,n=16)
    # Shoulder cap seam and load-aligned trim.
    plate('Shoulder_Rear_Ceramic_Lip.'+side,mir([(1.38,.497,8.96),(1.95,.603,8.9),(2.13,.50,8.67),(1.97,.54,8.63),(1.45,.55,8.84)]),(0,-.044,0),pa,WHITE)
    surface_module('Shoulder_Front_Recess.'+side,'Shoulder_Main_Wrapped_Carapace.'+side+'_Upper',s*1.65,8.87,.32,.092,pa,bars=2)
    # Cover the upper arm only partially, leaving an actual servo cavity visible.
    ua=bpy.data.objects['UpperArm_Swept_Armor.'+side]
    surface_module('UpperArm_Cooled_Servo_Window.'+side,ua.name,s*1.86,7.89,.20,.22,bpy.data.objects['UpperArm.'+side],bars=3)
    surface_module('Thigh_Upper_Service_Cassette.'+side,'Thigh_Main_Lateral_Shell.'+side+'_Upper',s*.86,5.27,.20,.22,th,bars=3)
    # Cut the forearm at a protected service seam, away from its already fitted inlet.
    split_shell('Forearm_Main_Gauntlet_Volume.'+side,6.20,.025)
    plate('Forearm_Leading_Armor_Rail.'+side,mir([(1.83,-.403,6.94),(1.915,-.51,6.80),(2.12,-.58,6.23),(2.08,-.535,6.08),(1.93,-.48,6.4)]),(0,.050,0),fa,WHITE)
    # Toe seams follow flexion and show a bridge; retain the continuous structural sole.
    xx=s*1.11
    for j in range(2):
        split_shell('Foot_Split_Heavy_Toe_%d.%s'%(j,side),.215,.011)
    for dx in (-.245,.245):
        rod('Foot_Toe_Flex_Pin_'+str(dx)+'.'+side,(xx+dx-.13,-.72,.28),(xx+dx+.13,-.72,.28),.044,foot,STEEL,n=20)
    plate('Foot_Forward_Dorsal_Ceramic.'+side,[(xx-.28,-.925,.382),(xx+.28,-.925,.382),(xx+.23,-.68,.55),(xx-.23,-.68,.55)],(0,.034,-.035),foot,WHITE,bevel=.007)
changes.append('Exposed calf and arm intakes; physically split shoulder, thigh, calf, shin and foot armor')

# Overlapping stomach plates separate from the narrow actuator core.
split_shell('Abdomen_Single_Tapered_Guard',6.94,.033)
plate('Abdomen_Lower_Armor_Ridge',[(-.315,-.483,6.87),(.315,-.483,6.87),(.32,-.484,6.78),(0,-.577,6.66),(-.32,-.484,6.78)],(0,.04,0),WAIST,GUN)

# Folded thruster nacelles are modular engine shrouds, not continuous decorative wings.
for s,side in ((-1,'R'),(1,'L')):
    pod=bpy.data.objects['Backpack_Main_Deploy_Hinge.'+side];vane=bpy.data.objects['Backpack_Outer_Fold_Pivot.'+side]
    def mir(p):return [(s*x,y,z) for x,y,z in p]
    split_shell('Backpack_Tall_Folded_Fin.'+side,9.06,.055)
    split_shell('Backpack_Outer_Folded_Propulsion_Vane.'+side,7.95,.050)
    plate('Backpack_Reactor_Upper_Ceramic_Inlay.'+side,mir([(.56,1.839,8.99),(.66,1.86,9.13),(.73,1.879,8.93),(.665,1.895,8.69)]),(0,-.036,0),pod,WHITE,role='propulsion')
    for z in (8.50,7.77):
        # Visible side load bands connect shroud to engine, not random surface decoration.
        plate('Backpack_Engine_Load_Band_'+str(z)+'.'+side,mir([(.48,1.84,z),(.63,1.94,z+.065),(.93,2.03,z+.02),(.94,2.03,z-.05),(.62,1.94,z-.02),(.48,1.84,z-.08)]),(0,-.10,0),pod,GUN,role='propulsion')
    surface_module('Backpack_Outer_Vector_Access.'+side,'Backpack_Outer_Folded_Propulsion_Vane.'+side+'_Upper',s*1.38,8.48,.15,.26,vane,side='back',bars=3)
changes.append('Folded backpack segmented at engine service boundaries with exposed load bands')

# Blade service segmentation leaves the physical main body and single cutting edge continuous.
sm=SWORD.matrix_world.copy();SWORD.matrix_world=Matrix.Identity(4);bpy.context.view_layer.update()
split_shell('Blade_Side_Load_Plate',3.90,.038)
for z in (2.75,4.42,5.35):
    plate('Blade_Energy_Coupler_'+str(z),[(-.12,-.236,z),(.18,-.20,z+.07),(.18,-.20,z+.11),(-.12,-.236,z+.045)],(0,.022,0),SWORD,GUN,role='weapon',bevel=.004)
SWORD.matrix_world=sm
changes.append('Segmented blade load plate; physical blade and independent continuous beam retained')

for o in scene.objects:
    if o not in before:newfinish(o)
for o in list(scene.objects):
    if o.parent==HEAD and o.type=='MESH':
        for c in list(o.users_collection):c.objects.unlink(o)
        bpy.data.collections['HEAD'].objects.link(o)
scene['master_revision']='02: reviewed functional armor layering and silhouette polish'
scene['visual_review_01']='Actual 3Q, head, chest, leg, backpack, blade renders inspected; see iteration report'
for screen in bpy.data.screens:
    for a in screen.areas:
        if a.type=='VIEW_3D':a.spaces.active.overlay.show_relationship_lines=False
dg=bpy.context.evaluated_depsgraph_get();tri=0;poly=0
for o in scene.objects:
    if o.type in ('MESH','CURVE') and not o.hide_render:
        e=o.evaluated_get(dg);me=e.to_mesh();me.calc_loop_triangles();tri+=len(me.loop_triangles);poly+=len(me.polygons);e.to_mesh_clear()
report={'revision':'02','observations_and_corrections':changes,'evaluated_triangles':tri,'evaluated_polygons':poly,'visible_objects':sum(o.type in ('MESH','CURVE') and not o.hide_render for o in scene.objects)}
(OUT/'logs'/'iteration_02.json').write_text(json.dumps(report,indent=2),encoding='utf8')
text=bpy.data.texts.load(str(OUT/'refine_master.py'));text.name='refine_master.py | executed'
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'V2_REFINED.blend'))
print('REFINEMENT_COMPLETE',json.dumps(report),flush=True)
