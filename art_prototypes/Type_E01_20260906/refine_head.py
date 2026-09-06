"""E-01 reference head reconstruction: swept helmet, inset monoeye, sharp jaw."""
import bpy,math
from mathutils import Vector

def build(G):
    C=G.HEAD; P=G.head_root
    for o in list(C.objects):
        bpy.data.objects.remove(o,do_unlink=True)
    M=G.mesh; S=G.skin; B=G.box; H=G.hull; Y=G.cylinder; T=G.tube
    white=G.white; dark=G.dark; steel=G.steel; rubber=G.rubber
    lens=G.material('E01 | deep red optical glass',(.40,.0005,.001),.12,.20,.08)
    core=G.material('E01 | focused red optical emitter',(.80,.001,.001),.0,.29,.38)
    # The collar is mostly hidden under the low armor cowl, as on the sheet.
    Y('H02 neck bearing',(0,.015,2.817),(0,.015,2.872),.089,dark,C,P,48,.002)
    Y('H02 neck concentric seal',(0,.015,2.826),(0,.015,2.852),.100,rubber,C,P,48,.002)
    Y('H02 neck column',(0,.015,2.849),(0,.015,2.94),.057,steel,C,P,32,.002)
    B('H02 compact internal skull',(0,.023,3.028),(.208,.220,.278),dark,C,P,.014)

    # Narrow flattened crown sweeps continuously to the low front rim.
    # Front -Y. Explicit crown panels avoid a box lid or arbitrary diagonals.
    v=[(-.061,-.061,3.240),(.061,-.061,3.240),(-.055,.066,3.229),(.055,.066,3.229),
       (-.082,.126,3.190),(.082,.126,3.190),(-.111,.095,3.128),(.111,.095,3.128),
       (-.107,-.033,3.186),(.107,-.033,3.186),(-.081,-.139,3.176),(.081,-.139,3.176)]
    S('H02 armored crown spine',v,[(0,1,3,2),(2,3,5,4),(2,4,6,8),(3,9,7,5),(0,2,8,10),(1,11,9,3)],(0,0,-.018),white,C,P,.003)
    # Long shield forehead. Angular descending tips frame, but do not cover, the lens.
    forehead=[(-.058,-.071,3.240),(.058,-.071,3.240),
              (-.082,-.146,3.175),(.082,-.146,3.175),
              (-.063,-.207,3.072),(.063,-.207,3.072),
              (-.045,-.214,3.055),(.045,-.214,3.055)]
    S('H02 long sloping forehead shield',forehead,[(0,1,3,2),(2,3,5,4),(4,5,7,6)],(0,.016,0),white,C,P,.0022)
    for s,L in ((-1,'R'),(1,'L')):
        # Broad temple panels taper into short sharp guards beside the brow.
        side=[(.067,-.064,3.238),(.108,-.038,3.191),(.129,.027,3.158),
              (.128,.103,3.093),(.124,-.029,3.051),(.076,-.197,3.028),
              (.047,-.216,3.054),(.067,-.208,3.077),(.086,-.144,3.176)]
        S(L+' H02 swept temple shell',G.mirrored(side,s),[(0,1,8),(1,2,4,8),(2,3,4),(8,4,5,7),(7,5,6)],(-s*.015,.009,0),white,C,P,.0025)
        # Rear shell is a low ledge, not a box hanging below the helmet.
        S(L+' H02 rear helmet return',G.mirrored([(.057,.068,3.226),(.084,.132,3.188),(.110,.121,3.078),(.127,.069,3.068),(.127,.035,3.154)],s),[(0,1,4),(1,2,3,4)],(-s*.014,-.012,0),white,C,P,.003)

    # Recessed face assembly: a small lens inside a deep angular optical opening.
    mask=[(-.057,-.179,3.061),(.057,-.179,3.061),(.076,-.174,3.010),
          (.042,-.154,2.898),(-.042,-.154,2.898),(-.076,-.174,3.010)]
    G.plate('H02 recessed tapered optical chassis',mask,(0,.029,0),rubber,C,P,.0025)
    for s,L in ((-1,'R'),(1,'L')):
        # White cowl blade runs downwards and inward, enclosing the black face.
        face=[(.105,-.127,3.038),(.111,-.092,2.982),(.074,-.154,2.875),
              (.036,-.182,2.855),(.038,-.195,2.902),(.061,-.194,2.942),(.080,-.174,3.017)]
        S(L+' H02 angular facial rail',G.mirrored(face,s),[(0,1,6),(1,2,5,6),(2,3,4,5)],(-s*.013,.027,0),white,C,P,.0023)
        # Oblique cheek band from the ear to the jaw; a real finite shell.
        pts=[(.122,-.017,3.015),(.128,.071,2.985),(.109,.110,2.933),
             (.058,-.079,2.850),(.074,-.155,2.878),(.105,-.110,2.981)]
        S(L+' H02 jaw side blade',G.mirrored(pts,s),[(0,1,5),(1,2,3,4,5)],(-s*.016,.008,.003),white,C,P,.0028)
        # Dark gasket is visible between the helmet and the isolated ear cover.
        Y(L+' H02 ear isolation gasket',(s*.122,.022,3.064),(s*.136,.022,3.064),.060,rubber,C,P,48,.0015)
        Y(L+' H02 ear armored disc',(s*.135,.022,3.064),(s*.147,.022,3.064),.052,white,C,P,48,.0025)
        Y(L+' H02 ear recessed center',(s*.147,.022,3.064),(s*.149,.022,3.064),.045,white,C,P,48,.001)
        for ang in (.6,3.0,4.9):
            y=.022+math.cos(ang)*.044;z=3.064+math.sin(ang)*.044
            B(L+' H02 ear perimeter latch',(s*.148,y,z),(.004,.011,.005),G.edge,C,P,.0006,rot=(ang,0,0))
        B(L+' H02 rear hinge recess',(s*.105,.117,3.028),(.023,.023,.063),rubber,C,P,.002)
        Y(L+' H02 small jaw pivot',(s*.111,-.046,2.994),(s*.120,-.046,2.994),.013,G.edge,C,P,24,.001)
        for j,(x,y,z) in enumerate(((.053,-.170,3.144),(.045,-.211,3.083),(.077,-.171,2.924))):
            Y(L+' H02 flush screw '+str(j),(s*x,y,z),(s*x,y-.002,z),.0028,steel,C,P,12,.0003)
        # Small vertical indicator recesses are clipped into the forehead edge.
        B(L+' H02 forehead narrow service slot',(s*.058,-.171,3.143),(.006,.003,.013),dark,C,P,.001,rot=(.48,0,s*.09))

    G.plate('H02 narrowed chin bridge',[(-.037,-.184,2.904),(.037,-.184,2.904),(.037,-.171,2.855),(-.037,-.171,2.855)],(0,.031,0),white,C,P,.003)
    # Eye surface sits behind the sharp brow lip. Concentric rings create depth.
    T('H02 monoeye deep socket',(0,-.168,3.018),(0,-.199,3.018),.041,.031,rubber,C,P,64)
    T('H02 inner dark lens rim',(0,-.186,3.018),(0,-.200,3.018),.033,.027,dark,C,P,64)
    Y('H02 red recessed lens',(0,-.183,3.018),(0,-.190,3.018),.0265,lens,C,P,64,.002)
    # Flattened optical dome: still a lens, without the projecting button from v1.
    bpy.ops.mesh.primitive_uv_sphere_add(segments=40,ring_count=20,radius=1,location=(0,-.191,3.018))
    o=bpy.context.object;o.name='H02 red optical dome';o.scale=(.022,.007,.022)
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    G.attach(o,core,C,P)
    for f in o.data.polygons:f.use_smooth=True
    B('H02 lower face dark intake',(0,-.172,2.935),(.048,.016,.041),dark,C,P,.003)
    for z in (2.926,2.939):
        B('H02 inset intake vane',(0,-.182,z),(.035,.003,.003),rubber,C,P,.0004)
    B('H02 rear nape armored insert',(0,.132,3.057),(.085,.028,.063),dark,C,P,.004)
    B('H02 antenna shoe',(0,.078,3.227),(.016,.022,.018),dark,C,P,.002)
    Y('H02 single short antenna',(0,.078,3.234),(0,.085,3.329),.0045,dark,C,P,16,.0008,r2=.0025)
    # A continuous manufactured crown replaces loose triangular temple roof planes.
    # Keep the sharp descending brow corners and the separate forehead shield.
    for name in ('H02 armored crown spine','H02 long sloping forehead shield','R H02 swept temple shell','L H02 swept temple shell','R H02 rear helmet return','L H02 rear helmet return'):
        o=bpy.data.objects.get(name)
        if o:bpy.data.objects.remove(o,do_unlink=True)
    stations=[(-.213,.069,3.035,3.073),(-.171,.095,3.058,3.155),(-.111,.115,3.063,3.207),(-.052,.129,3.061,3.235),(.013,.130,3.059,3.240),(.080,.119,3.074,3.218),(.128,.088,3.075,3.173),(.142,.047,3.084,3.113)]
    points=[];steps=12
    for y,width,base,top in stations:
        for i in range(steps+1):
            a=math.pi*i/steps
            roof=top if 3<=i<=9 else base+(top-base)*(math.sin(a)/math.sin(math.pi/4))**.80
            points.append((width*math.cos(a),y,roof))
    faces=[]
    for j in range(len(stations)-1):
        for i in range(steps):
            if j<3 and 3<=i<9:
                continue
            a=j*(steps+1)+i
            faces.append((a,a+1,a+steps+2,a+steps+1))
    cap=S('H02 continuous swept helmet crown',points,faces,(0,0,-.012),white,C,P,.0018)
    for face in cap.data.polygons:
        face.use_smooth=True
    # The forehead occupies the open strip in the crown, never overlaps a roof face.
    shield=[]
    for y,width,base,top in stations[:4]:
        w=width*math.sqrt(.5)*.994
        shield.extend([(-w,y-.0015,top+.001),(w,y-.0015,top+.001)])
    S('H02 inset planar forehead shield',shield,[(0,1,3,2),(2,3,5,4),(4,5,7,6)],(0,.009,-.003),white,C,P,.0018)
    P['head_revision']='02 - reference reconstruction, narrower recessed face and sharpened jaw'
    P['approval_status']='awaiting user review'
