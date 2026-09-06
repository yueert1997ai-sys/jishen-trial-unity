"""Editable E-01 assault rifle from the supplied weapon detail sheet.
Rifle construction coordinates: +X muzzle, +Z top, Y width. Whole model is rigid.
"""
import bpy,math
from mathutils import Vector,Matrix

def build(G):
    C=G.collection('05 E-01 ASSAULT RIFLE')
    P=G.empty('E01_RIFLE_ROOT',(0,0,0),G.root)
    B=G.box;H=G.hull;Y=G.cylinder;T=G.tube
    steel=G.material('RIFLE | parkerized receiver',(.052,.062,.067),.63,.39)
    panel=G.material('RIFLE | dark phosphate steel',(.082,.093,.097),.68,.43)
    black=G.material('RIFLE | polymer grip and seals',(.018,.024,.027),.12,.60)
    bore=G.material('RIFLE | bore cavity',(.004,.006,.008),.15,.80)

    def profile(name,points,width,mat=steel,bevel=.003):
        # X-Z section, finite extrusion along Y; all silhouette breaks are real mesh.
        n=len(points)
        verts=[(x,-width/2,z) for x,z in points]+[(x,width/2,z) for x,z in points]
        faces=[tuple(range(n)),tuple(range(n,2*n))[::-1]]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
        return G.mesh(name,verts,faces,mat,C,P,bevel)
    def fix(name,x,y,z,r=.004):
        side=-1 if y<0 else 1
        Y(name,(x,y,z),(x,y+side*.002,z),r,G.dark,C,P,12,.0004)
        B(name+' slot',(x,y+side*.0022,z),(r*.9,.001,r*.19),G.steel,C,P,.0002)
    # Receiver, rear latch housing, and large squared front handguard.
    profile('Rifle upper receiver',[(-.442,.026),(-.442,.158),(-.387,.210),(.069,.210),(.103,.184),(.444,.184),(.473,.135),(.441,.037),(.151,.037),(.126,.011),(-.320,.011)],.132,steel,.005)
    profile('Rifle lower receiver',[(-.432,.026),(.112,.026),(.164,-.012),(.114,-.073),(-.056,-.073),(-.112,-.041),(-.347,-.043),(-.384,-.080),(-.442,-.061)],.109,steel,.004)
    profile('Rifle squared forward barrel shroud',[(.068,.192),(.459,.192),(.490,.153),(.490,.013),(.444,-.059),(.107,-.059),(.079,-.028)],.159,panel,.005)
    B('Rifle front lower shroud raised reinforcement',(.292,0,-.053),(.319,.172,.062),steel,C,P,.004)
    B('Rifle barrel top armored ridge',(.272,0,.215),(.429,.096,.055),panel,C,P,.004)
    # Side armor, ejection port, stamped slots and a low fire selector.
    for s,L in ((-1,'right'),(1,'left')):
        B('Rifle '+L+' receiver side cover',(-.252,s*.073,.100),(.361,.014,.073),panel,C,P,.003)
        B('Rifle '+L+' ejection recessed port',(-.015,s*.074,.108),(.154,.013,.044),black,C,P,.0025)
        B('Rifle '+L+' ejection internal slide',(-.018,s*.082,.107),(.136,.004,.030),steel,C,P,.002)
        B('Rifle '+L+' forward long cooling channel',(.290,s*.084,.094),(.220,.008,.032),black,C,P,.002)
        for j in range(3):B('Rifle '+L+' internal vent fin '+str(j),(.215+j*.070,s*.091,.094),(.008,.007,.026),steel,C,P,.001)
        B('Rifle '+L+' shroud lower strengthening strip',(.286,s*.089,-.031),(.289,.010,.059),panel,C,P,.003)
        B('Rifle '+L+' forward top seam',(.263,s*.052,.224),(.316,.005,.004),black,C,P,.0007)
        B('Rifle '+L+' lower receiver service panel',(-.199,s*.061,-.014),(.065,.015,.033),panel,C,P,.002)
        for i,(x,z) in enumerate(((-.407,.132),(-.338,.129),(-.208,.129),(.083,.167),(.433,.170),(.139,-.037),(.399,-.037))):fix('Rifle '+L+' flush receiver fixing '+str(i),x,s*(.084 if x>0 else .083),z,.0035)
        Y('Rifle '+L+' selector pivot',(-.324,s*.061,.016),(-.324,s*.072,.016),.012,black,C,P,24,.001)
        B('Rifle '+L+' selector lever',(-.312,s*.076,.012),(.028,.007,.010),panel,C,P,.002,rot=(0,.25,0))
    # Carry handle is an open bridge with two short angled support plates.
    profile('Rifle carry handle rear riser',[(-.396,.207),(-.350,.307),(-.264,.307),(-.263,.276),(-.315,.276),(-.327,.213)],.084,panel,.003)
    B('Rifle carry handle top bar',(-.226,0,.306),(.243,.072,.034),panel,C,P,.003)
    B('Rifle carry handle underside',(-.209,0,.273),(.189,.055,.015),black,C,P,.002)
    B('Rifle carry handle front support',(-.110,0,.280),(.027,.062,.043),panel,C,P,.003)
    for s in (-1,1):fix('Rifle carry handle rivet',-.336,s*.045,.280,.004)
    # Folding skeletal stock: upper receiver tube, locking plate, diagonal lower brace.
    Y('Rifle stock guide tube',(-.454,0,.112),(-.663,0,.112),.033,steel,C,P,32,.002)
    B('Rifle stock hinge collar',(-.475,0,.063),(.067,.095,.086),panel,C,P,.004)
    profile('Rifle stock top beam',[(-.804,.122),(-.589,.147),(-.559,.115),(-.585,.046),(-.786,.028)],.104,panel,.004)
    profile('Rifle stock lower diagonal strut',[(-.786,-.186),(-.747,-.186),(-.561,.022),(-.576,.049),(-.613,.024)],.062,steel,.003)
    profile('Rifle buttstock rigid plate',[(-.847,-.202),(-.786,-.194),(-.766,.103),(-.812,.126),(-.854,.111)],.122,panel,.004)
    profile('Rifle stock rubber butt pad',[(-.866,-.204),(-.841,-.204),(-.840,.119),(-.862,.108)],.132,black,.003)
    for s in (-1,1):
        fix('Rifle stock upper fixing',-.772,s*.055,.096,.004)
        fix('Rifle stock joint pin',-.588,s*.055,.080,.004)
    # Pistol grip: slanted, separate polymer panels and a machined base plate.
    profile('Rifle pistol grip core',[(-.291,-.038),(-.204,-.038),(-.221,-.105),(-.269,-.274),(-.351,-.257),(-.305,-.112)],.072,black,.004)
    profile('Rifle pistol grip bottom cap',[(-.355,-.256),(-.271,-.275),(-.265,-.292),(-.361,-.273)],.086,panel,.0025)
    for s in (-1,1):
        for j in range(5):
            z=-.126-j*.025;x=-.29+(z+.15)*.28
            B('Rifle grip moulded rib '+str(s)+' '+str(j),(x,s*.039,z),(.053,.004,.004),steel,C,P,.001,rot=(0,-.15,0))
    # Open trigger guard, with a curved trigger inside the opening.
    profile('Rifle trigger guard front bar',[(-.177,-.050),(-.163,-.053),(-.164,-.125),(-.180,-.139),(-.192,-.125)],.034,panel,.002)
    profile('Rifle trigger guard bottom',[(-.263,-.138),(-.177,-.140),(-.163,-.125),(-.174,-.113),(-.191,-.123),(-.264,-.121)],.034,panel,.002)
    profile('Rifle trigger',[(-.229,-.048),(-.218,-.048),(-.218,-.082),(-.207,-.104),(-.216,-.110),(-.234,-.081)],.018,steel,.0012)
    # Curved detachable magazine follows the rifle sheet's long forward-curving profile.
    mag=profile('Rifle detachable magazine',[(-.073,-.057),(.052,-.057),(.072,-.205),(.123,-.336),(.006,-.384),(-.029,-.277)],.086,panel,.004)
    mag['separate_equipment_part']='magazine'
    profile('Rifle magazine extended base lip',[(.001,-.382),(.126,-.336),(.135,-.355),(.003,-.406),(-.009,-.393)],.108,steel,.0025)
    for s in (-1,1):
        # Long stamped ribs made from three connected straight segments.
        for off in (-.027,.028):
            line=[(off-.027,s*.045,-.091),(off-.009,s*.045,-.219),(off+.027,s*.045,-.343)]
            for j,(a,b) in enumerate(zip(line,line[1:])):
                G.cylinder('Rifle stamped magazine rib '+str(s)+' '+str(off)+' '+str(j),a,b,.0035,steel,C,P,12,.0005)
    # Barrel sleeve, hollow flash suppressor, front sight and short lower gas tube.
    Y('Rifle barrel neck',(.470,0,.133),(.560,0,.133),.044,steel,C,P,40,.003)
    Y('Rifle long barrel sleeve',(.549,0,.133),(.733,0,.133),.037,panel,C,P,48,.003)
    T('Rifle hollow muzzle brake',(.716,0,.133),(.868,0,.133),.041,.026,steel,C,P,48)
    T('Rifle muzzle machined lip',(.853,0,.133),(.875,0,.133),.042,.026,panel,C,P,48)
    Y('Rifle inner barrel darkness',(.743,0,.133),(.746,0,.133),.025,bore,C,P,40,.001)
    for s in (-1,1):
        B('Rifle muzzle side port '+str(s),(.803,s*.039,.133),(.068,.008,.016),black,C,P,.002)
    B('Rifle barrel front block',(.473,0,.146),(.063,.123,.203),panel,C,P,.004)
    profile('Rifle front sight base',[(.437,.234),(.457,.306),(.487,.296),(.485,.234)],.052,steel,.0025)
    B('Rifle front sight dark notch',(.468,0,.290),(.011,.035,.015),black,C,P,.001)
    Y('Rifle short lower gas cylinder',(.447,0,.029),(.584,0,.029),.018,panel,C,P,32,.002)
    Y('Rifle lower gas cylinder cap',(.568,0,.029),(.595,0,.029),.019,steel,C,P,32,.002)
    # Machine a small number of actual recesses into the finished editable meshes.
    def recess(target_name,loc,dimensions):
        target=bpy.data.objects[target_name]
        cut=B('TEMP machining tool',loc,dimensions,black,C,P,0)
        bpy.context.view_layer.objects.active=target
        mod=target.modifiers.new('Machined recess','BOOLEAN');mod.operation='DIFFERENCE';mod.solver='EXACT';mod.object=cut
        while target.modifiers.find(mod.name)>0:
            bpy.ops.object.modifier_move_up(modifier=mod.name)
        bpy.ops.object.modifier_apply(modifier=mod.name)
        bpy.data.objects.remove(cut,do_unlink=True)
    for s,L in ((-1,'right'),(1,'left')):
        recess('Rifle squared forward barrel shroud',(.290,s*.087,.094),(.234,.052,.035))
        recess('Rifle upper receiver',(-.015,s*.075,.108),(.157,.040,.046))
        recess('Rifle hollow muzzle brake',(.803,s*.040,.133),(.072,.043,.017))
        bpy.data.objects['Rifle '+L+' forward long cooling channel'].location.y=s*.060
        bpy.data.objects['Rifle '+L+' ejection recessed port'].location.y=s*.052
        bpy.data.objects['Rifle '+L+' ejection internal slide'].location.y=s*.057
        for j in range(3):bpy.data.objects['Rifle '+L+' internal vent fin '+str(j)].location.y=s*.074
        bpy.data.objects['Rifle muzzle side port '+str(s)].hide_render=True
        bpy.data.objects['Rifle muzzle side port '+str(s)].hide_viewport=True
    # Store working anchors for the final two-handed pose, without adding animation.
    P['primary_grip_local']=(-.285,0,-.178)
    P['support_grip_local']=(.279,0,-.096)
    P['muzzle_local']=(.875,0,.133)
    P['reference']='E-01 ASSAULT RIFLE in user four-view sheet'
    P['length_m']=1.741
    bpy.context.view_layer.update()
    # Right-hand lowered inspection pose, matching the sheet's neutral front view.
    rotation=Matrix.Rotation(math.radians(-42),3,'Z') @ Matrix(((0,0,-1),(0,-1,0),(-1,0,0)))
    grip=Vector(P['primary_grip_local']);target=Vector((-.672,-.085,1.802))
    P.rotation_euler=rotation.to_euler();P.location=target-rotation@grip
    bpy.context.view_layer.update()
    # A linked geometry copy is displayed separately for the two equipment detail shots.
    display=G.collection('06 RIFLE DETAIL DISPLAY')
    display_root=G.empty('E01_RIFLE_DETAIL_ROOT',(0,0,1.35),None)
    for obj in list(C.objects):
        cp=obj.copy();cp.data=obj.data;display.objects.link(cp)
        cp.parent=display_root;cp.matrix_parent_inverse=Matrix.Identity(4)
        cp.location=obj.location.copy();cp.rotation_euler=obj.rotation_euler.copy();cp.scale=obj.scale.copy()
    display.hide_render=True;display.hide_viewport=True
    # The detail display has its own orthographic cameras and neutral ground plane.
    G.camera('E01_RIFLE_SIDE',(0,-6,1.32),(0,0,1.32),1.95)
    G.camera('E01_RIFLE_3Q',(1.8,-5,2.4),(0,0,1.31),2.06)
