"""Stage 02: layered rigid armor and inspectable mechanical structure."""
import bpy,math
from mathutils import Vector,Matrix

def build(G):
    M=G.mesh;S=G.skin;B=G.box;Y=G.cylinder;P=G.plate;H=G.hull
    C=G.BODY;W=G.white;D=G.dark;E=G.edge;R=G.rubber;ST=G.steel
    def remove(name):
        o=bpy.data.objects.get(name)
        if o:bpy.data.objects.remove(o,do_unlink=True)
    def change_core(name,factor=.91):
        o=bpy.data.objects.get(name)
        if not o:return
        center=sum((v.co for v in o.data.vertices),Vector())/len(o.data.vertices)
        for v in o.data.vertices:v.co=center+(v.co-center)*factor
        o.data.materials.clear();o.data.materials.append(D)
        for mod in o.modifiers:
            if mod.type=='BEVEL':mod.width=min(mod.width,.005)
        o.name=name+' | recessed frame'
    def bolt(name,pos,normal=(0,-1,0),r=.0045,parent=None,col=C):
        p=Vector(pos);n=Vector(normal).normalized()
        Y(name+' dark seating',p,p+n*.0018,r*1.35,R,col,parent,16,.0004)
        Y(name+' hex head',p+n*.0018,p+n*.003,r,ST,col,parent,6,.00035)
    def rod(name,a,b,r=.007,mat=ST,parent=None,col=C):
        return Y(name,a,b,r,mat,col,parent,20,.001)
    def label(name,body,loc,size,parent,normal=(0,-1,0),col=C):
        cu=bpy.data.curves.new(name,'FONT');cu.body=body;cu.size=size;cu.extrude=.0002;cu.align_x='CENTER';cu.align_y='CENTER'
        cu.space_character=1.06
        ob=bpy.data.objects.new(name,cu);col.objects.link(ob);cu.materials.append(D)
        ob.location=loc
        n=Vector(normal).normalized();u=Vector((1,0,0))
        if abs(u.dot(n))>.9:u=Vector((0,-1,0))
        u=(u-n*u.dot(n)).normalized();v=n.cross(u)
        if v.z<0:u=-u;v=-v
        ob.rotation_euler=Matrix((u,v,n)).transposed().to_euler()
        ob.parent=parent;ob.matrix_parent_inverse=parent.matrix_world.inverted()
        return ob
    def shell_panels(name,rows,parent):
        # Four finite shells separated by dark working gaps around a structural core.
        for tag,ids,depth in (('front',[7,0,1,2],(0,.024,0)),('right side',[2,3],(-.024,0,0)),('rear',[3,4,5,6],(0,-.024,0)),('left side',[6,7],(.024,0,0))):
            points=[rows[j][i] for j in range(len(rows)) for i in ids]
            center=sum((Vector(p) for p in points),Vector())/len(points)
            points=[tuple(center+(Vector(p)-center)*.986) for p in points]
            width=len(ids);faces=[]
            for j in range(len(rows)-1):
                for k in range(width-1):faces.append((j*width+k,j*width+k+1,(j+1)*width+k+1,(j+1)*width+k))
            S(name+' '+tag+' armor',points,faces,depth,W,C,parent,.004)

    # Chest: two broad manufacturing planes with a clear lower keel.
    remove('Chest central sloped breastplate')
    chest=G.torso_root
    pts=[(-.150,-.207,2.813),(.150,-.207,2.813),(-.188,-.343,2.626),(.188,-.343,2.626),(-.107,-.295,2.389),(.107,-.295,2.389)]
    S('B02 central breastplate folded panels',pts,[(0,1,3,2),(2,3,5,4)],(0,.038,0),W,C,chest,.005)
    for pos in ((-.129,-.229,2.784),(.129,-.229,2.784),(-.151,-.345,2.638),(.151,-.345,2.638),(-.087,-.300,2.419),(.087,-.300,2.419)):
        bolt('Chest recessed fixing',pos,parent=chest,r=.0038)
    # A short lip and black gasket enclose the neck opening.
    P('B02 collar central armor lip',[(-.11,-.126,2.850),(.11,-.126,2.850),(.118,-.173,2.808),(-.118,-.173,2.808)],(0,.02,0),W,C,chest,.004)
    B('B02 collar continuous dark support',(0,.0,2.813),(.458,.267,.065),R,C,chest,.009)
    for s,L in ((-1,'R'),(1,'L')):
        for x,z,y in ((.277,2.755,-.217),(.304,2.557,-.285),(.229,2.473,-.290)):
            bolt(L+' pectoral fastener',(s*x,y,z),parent=chest,r=.0035)
        # Lower oblique ribs are functional load paths from chest to waist.
        parent=G.waist_root
        rod(L+' oblique abdominal piston',(s*.208,.015,2.392),(s*.157,-.052,2.208),.025,D,parent)
        rod(L+' abdominal polished piston rod',(s*.172,-.042,2.260),(s*.143,-.071,2.174),.012,ST,parent)
        for z in (2.197,2.261,2.326):
            B(L+' waist side rib',(s*.159,-.083,z),(.047,.102,.026),E,C,parent,.004)
        for pos in ((s*.243,-.179,2.11),(s*.144,-.229,2.064)):
            bolt(L+' hip armor fixing',pos,parent=parent,r=.004)
        B(L+' hip top seam insert',(s*.272,-.050,2.167),(.101,.122,.015),R,C,parent,.003)
    label('B02 chest E-01 unit marking','E-01',(.268,-.272,2.695),.045,chest,normal=(.38,-.86,.33))
    B('B02 abdomen center dark service insert',(0,-.169,2.241),(.084,.008,.017),D,C,G.waist_root,.002)
    bolt('B02 front apron service screw',(0,-.244,1.985),parent=G.waist_root,r=.004)

    for s,L in ((-1,'R'),(1,'L')):
        shoulder=bpy.data.objects['E01_'+L+'_SHOULDER'];upper=bpy.data.objects['E01_'+L+'_UPPER_ARM']
        fore=bpy.data.objects['E01_'+L+'_FOREARM'];hand=bpy.data.objects['E01_'+L+'_HAND']
        # Shoulder becomes a structural dark shell enclosed by separate armor panels.
        change_core(L+' broad clipped shoulder shell',.94)
        remove(L+' shoulder front inset face')
        pp=[(.397,-.166,2.925),(.592,-.198,2.967),(.715,-.148,2.885),(.743,-.155,2.699),(.659,-.206,2.576),(.417,-.209,2.628)]
        front=G.mirrored(pp,s);back=[(x,y+.365,z+.005) for x,y,z in front]
        P(L+' B02 shoulder broad front armor',front,(0,.026,0),W,C,shoulder,.005)
        P(L+' B02 shoulder rear armor',back,(0,-.026,0),W,C,shoulder,.005)
        S(L+' B02 shoulder top return',[front[i] for i in (0,1,2)]+[back[i] for i in (0,1,2)],[(0,1,4,3),(1,2,5,4)],(0,0,-.023),W,C,shoulder,.005)
        # Split side panels leave a rectangular dark ventilation bay in the outer flank.
        S(L+' B02 shoulder outer upper lip',[front[2],back[2],(s*.753,.109,2.822),(s*.753,-.101,2.817)],[(0,1,2,3)],(-s*.019,0,0),W,C,shoulder,.004)
        S(L+' B02 shoulder outer lower return',[(s*.754,-.098,2.720),(s*.754,.112,2.725),back[4],front[4]],[(0,1,2,3)],(-s*.022,0,0),W,C,shoulder,.004)
        B(L+' B02 shoulder vent dark recess',(s*.730,.004,2.766),(.022,.213,.101),R,C,shoulder,.006)
        for j in range(4):
            B(L+' B02 shoulder vent blade '+str(j),(s*.746,-.072+j*.048,2.765),(.016,.018,.080),E,C,shoulder,.002)
        for idx in (0,1,3,4,5):
            q=Vector(front[idx]);cent=sum((Vector(v) for v in front),Vector())/len(front)
            q=cent+(q-cent)*.85;q.y-=.002
            bolt(L+' shoulder armor fastener '+str(idx),q,parent=shoulder,r=.0045)
        B(L+' B02 shoulder bottom stop',(s*.577,-.023,2.591),(.110,.170,.026),D,C,shoulder,.004)
        # Upper-arm plate gaps expose the elbow linkage.
        change_core(L+' upper arm armor',.91)
        shell_panels(L+' B02 upper arm',[G.oct_ring(s*.533,-.008,2.625,.184,.215),G.oct_ring(s*.587,-.014,2.404,.161,.183)],upper)
        for x in (-.042,.042):
            rod(L+' elbow double hinge link '+str(x),(s*.579+x,-.097,2.430),(s*.594+x,-.109,2.307),.016,D,fore)
            rod(L+' elbow exposed piston '+str(x),(s*.594+x,-.109,2.358),(s*.601+x,-.106,2.283),.009,ST,fore)
        # Tapered forearm panels with an open black collar around the elbow.
        change_core(L+' long tapered forearm shell',.89)
        shell_panels(L+' B02 forearm',[G.oct_ring(s*.620,-.034,2.259,.246,.237),G.oct_ring(s*.628,-.052,2.159,.235,.278),G.oct_ring(s*.665,-.050,1.970,.172,.218)],fore)
        B(L+' B02 forearm elbow recess',(s*.620,-.157,2.263),(.081,.025,.047),R,C,fore,.006)
        rod(L+' forearm elbow link',(s*.604,-.130,2.319),(s*.620,-.147,2.257),.026,E,fore)
        for zz in (2.207,2.075,1.996):
            xc=s*(.620+(2.259-zz)*.155)
            bolt(L+' forearm corner fixing',(xc+s*.069,-.175 if zz>2.12 else -.167,zz),parent=fore,r=.0035)
        B(L+' B02 forearm side service rail',(s*.744,-.026,2.105),(.013,.067,.120),E,C,fore,.003)
        # Articulated closed mechanical fingers replace the block hands.
        remove(L+' hand primary fist');remove(L+' hand armor back');remove(L+' thumb mass')
        B(L+' B02 hand palm chassis',(s*.670,-.028,1.848),(.142,.136,.128),D,C,hand,.010)
        P(L+' B02 hand dorsal armored plate',[(s*.670-.068,-.105,1.907),(s*.670+.068,-.105,1.907),(s*.670+.077,-.125,1.849),(s*.670+.055,-.124,1.812),(s*.670-.055,-.124,1.812),(s*.670-.077,-.125,1.849)],(0,.019,0),W,C,hand,.004)
        for j in range(4):
            x=s*.670+(j-1.5)*.036
            B(L+' finger '+str(j)+' curled middle',(x,-.082,1.787),(.030,.067,.065),D,C,hand,.006)
            B(L+' finger '+str(j)+' knuckle',(x,-.124,1.806),(.030,.034,.049),E,C,hand,.005)
            B(L+' finger '+str(j)+' fingertip',(x,-.024,1.776),(.028,.040,.038),D,C,hand,.006)
        B(L+' B02 thumb proximal',(s*.585,-.048,1.851),(.046,.070,.055),D,C,hand,.008,rot=(0,s*.42,0))
        B(L+' B02 thumb curled tip',(s*.597,-.097,1.824),(.047,.042,.050),E,C,hand,.008,rot=(.1,s*.3,0))
        for x in (-.05,.05):bolt(L+' hand dorsal screw',(s*.670+x,-.130,1.864),parent=hand,r=.0025)

    # Leg panels keep the accepted limb lengths; back openings reveal rigid frames.
    for s,L in ((-1,'R'),(1,'L')):
        thigh=bpy.data.objects['E01_'+L+'_THIGH'];calf=bpy.data.objects['E01_'+L+'_CALF'];foot=bpy.data.objects['E01_'+L+'_FOOT']
        change_core(L+' long thigh primary armor',.91)
        remove(L+' upper thigh floating front')
        shell_panels(L+' B02 thigh',[G.oct_ring(s*.267,-.016,1.9,.289,.305),G.oct_ring(s*.296,-.025,1.729,.337,.347),G.oct_ring(s*.337,-.019,1.340,.222,.271)],thigh)
        cx=s*.271
        P(L+' B02 upper thigh access lid',[(cx-.081,-.171,1.883),(cx+.081,-.171,1.883),(cx+.078,-.185,1.802),(cx-.078,-.185,1.802)],(0,.009,0),W,C,thigh,.0025)
        for zz,x,y in ((1.765,.279,-.201),(1.456,.323,-.171)):
            bolt(L+' thigh upper structural screw',(s*x+s*.073,y,zz),parent=thigh,r=.0037)
        B(L+' B02 thigh lower knee recess',(s*.337,-.145,1.368),(.114,.052,.100),R,C,thigh,.006)
        for x in (-.052,.052):
            rod(L+' knee front ram '+str(x),(s*.336+x,-.125,1.384),(s*.346+x,-.163,1.266),.015,E,calf)
        # Reduce the smooth solid calf to an inner spar, then wrap layered shells.
        remove(L+' lower leg main shell')
        remove(L+' outer calf layered shield')
        rows=[G.oct_ring(s*.367,.019,1.129,.324,.334),G.oct_ring(s*.376,.026,.944,.373,.385),G.oct_ring(s*.414,.022,.442,.209,.258)]
        # Main front shield: long, folded, tapered and separated from the calf sides.
        front=[rows[j][i] for j in range(3) for i in (7,0,1,2)]
        S(L+' B02 long faceted shin shield',front,[(0,1,5,4),(1,2,6,5),(2,3,7,6),(4,5,9,8),(5,6,10,9),(6,7,11,10)],(0,.025,0),W,C,calf,.005)
        # Outer calf: an angular high shoulder, long taper and an exposed ankle cutout.
        pts=[(.494,-.073,1.134),(.557,.018,.999),(.518,.155,.927),(.459,.161,.482),(.422,.083,.417),(.416,-.015,.549),(.435,-.070,.789),(.480,-.121,.916)]
        S(L+' B02 outside calf return',G.mirrored(pts,s),[(0,1,7),(1,2,6,7),(2,3,4,5,6)],(-s*.026,0,0),W,C,calf,.005)
        pts2=[(.265,-.080,1.097),(.244,.102,1.027),(.316,.153,.457),(.341,.037,.435),(.306,-.067,.857)]
        S(L+' B02 inside calf return',G.mirrored(pts2,s),[(0,1,4),(1,2,3,4)],(s*.022,0,0),W,C,calf,.004)
        # Forward diamond plate rides above the calf shell, with a dark gap at its base.
        cx=s*.378
        P(L+' B02 outer shin diamond',[ (cx+s*.025,-.165,1.077),(cx+s*.128,-.119,1.131),(cx+s*.164,-.124,.997),(cx+s*.098,-.186,.852),(cx+s*.042,-.185,.916)],(0,.025,0),W,C,calf,.004)
        # Exposed twin rear actuators and modest end brackets.
        for x in (-.042,.042):
            rod(L+' rear calf piston housing '+str(x),(s*.367+x,.173,1.101),(s*.395+x,.151,.757),.019,D,calf)
            rod(L+' rear calf polished rod '+str(x),(s*.395+x,.151,.806),(s*.416+x,.132,.474),.011,ST,calf)
        for z,x,y in ((1.096,.369,.172),(.502,.411,.142)):
            B(L+' B02 rear calf actuator bridge',(s*x,y,z),(.148,.048,.046),E,C,calf,.006)
        for side in (-1,1):
            vv=[(s*.371+side*.081,.189,1.064),(s*.371+side*.145,.168,1.078),
                (s*.414+side*.113,.141,.504),(s*.414+side*.060,.148,.478)]
            P(L+' B02 rear calf narrow armor rail '+str(side),vv,(0,-.019,0),W,C,calf,.004)
        for z,x,y in ((1.074,.345,-.157),(.746,.391,-.163),(.488,.414,-.124)):
            bolt(L+' shin plate edge screw',(s*x+s*.065,y,z),parent=calf,r=.0035)
        # Kneecap has a visible gasket and a terminal fastener, not an extra loose tile.
        B(L+' B02 kneecap lower mechanical shoe',(s*.349,-.172,1.107),(.089,.066,.034),E,C,calf,.003)
        bolt(L+' kneecap panel screw',(s*.349,-.216,1.291),parent=calf,r=.0035)
        # Toe plates and instep seams make the foot a mechanical boot rather than a shoe.
        cx=s*.427
        for j in (-1,0,1):
            xx=cx+j*.089
            P(L+' B02 toe cap segment '+str(j),[(xx-.038,-.464,.078),(xx+.038,-.464,.078),(xx+.038,-.400,.166),(xx-.038,-.400,.166)],(0,.014,0),E,C,foot,.003)
        for dx in (-.083,.083):
            rod(L+' instep panel seam '+str(dx),(cx+dx,-.332,.190),(cx+dx,-.091,.330),.0018,R,foot)
        for pos in ((cx-.110,-.194,.357),(cx+.110,-.194,.357)):
            bolt(L+' ankle wrap fixing',pos,parent=foot,r=.0034)
        B(L+' B02 heel inset plate',(cx,.257,.159),(.148,.012,.073),E,C,foot,.004)
        for j in (-1,0,1):B(L+' heel rib '+str(j),(cx+j*.047,.266,.159),(.014,.006,.063),D,C,foot,.002)

    # Backpack secondary equipment, cooling vents, latches, cables and turbine cones.
    C=G.PACK;pack=G.pack_root
    remove('Backpack main white rear panel')
    B('B02 backpack rear dark recessed cavity',(0,.481,2.650),(.403,.047,.397),R,C,pack,.008)
    B('B02 backpack central armored rear door',(0,.509,2.683),(.264,.024,.287),W,C,pack,.004)
    for s,L in ((-1,'R'),(1,'L')):
        # Physical frame bars make the tall recessed slots rather than painting black lines.
        B(L+' B02 backpack outer upright',(s*.191,.506,2.650),(.033,.027,.389),W,C,pack,.005)
        B(L+' B02 backpack inner upright',(s*.140,.507,2.650),(.025,.027,.389),W,C,pack,.004)
        for z in (2.822,2.468):B(L+' backpack slot end',(s*.166,.508,z),(.039,.028,.043),W,C,pack,.004)
        for z in (2.779,2.534):bolt(L+' backpack hatch fixing',(s*.111,.525,z),(0,1,0),.004,pack,C)
        B(L+' B02 backpack bottom latch',(s*.166,.513,2.421),(.058,.034,.064),E,C,pack,.006)
        bolt(L+' pack latch screw',(s*.166,.533,2.421),(0,1,0),.005,pack,C)
        for j in range(4):
            B(L+' B02 backpack side radiator '+str(j),(s*.280,.299+j*.033,2.650),(.009,.015,.205),D,C,pack,.002)
        Y(L+' B02 thruster internal central cone',(s*.111,.413,2.340),(s*.111,.457,2.292),.036,D,C,pack,40,.002,r2=.011)
        for angle in (0,2.094,4.189):
            # Slim fixed struts visible in the exhaust cavity.
            x=s*.111+math.cos(angle)*.039;y=.456+math.sin(angle)*.026;z=2.296+math.sin(angle)*.023
            rod(L+' thruster internal brace '+str(angle),(s*.111,.443,2.308),(x,y,z),.004,E,pack,C)
        rod(L+' rear mounting hydraulic pipe',(s*.204,.245,2.420),(s*.226,.266,2.764),.008,ST,pack,C)
    B('B02 backpack lower central service door',(0,.531,2.443),(.172,.024,.106),E,C,pack,.005)
    B('B02 backpack horizontal top bridge',(0,.483,2.886),(.385,.076,.036),E,C,pack,.006)
    for z in (2.427,2.445,2.463):B('B02 pack service vent',(0,.546,z),(.117,.005,.005),R,C,pack,.001)
    G.camera('E01_BACKPACK_DETAIL',(2.0,5.4,3.38),(0,.33,2.58),1.04)
    G.camera('E01_BODY_DETAIL',(2.1,-4.9,3.14),(0,-.02,2.24),1.71)
