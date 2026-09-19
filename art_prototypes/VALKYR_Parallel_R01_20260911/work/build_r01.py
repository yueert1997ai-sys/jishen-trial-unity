import bpy,bmesh,math,json,hashlib
from pathlib import Path
from mathutils import Vector,Matrix
OUT=Path(__file__).resolve().parents[1]
bpy.ops.wm.read_factory_settings(use_empty=True)
sc=bpy.context.scene
full=bpy.data.collections.new('VALKYR_R01_ASSET');sc.collection.children.link(full)
def collection(n):
 c=bpy.data.collections.new(n);full.children.link(c);return c
rig=collection('01_ASSEMBLY_PIVOTS');core=collection('02_INNER_FRAME');body=collection('03_BODY_ARMOUR');hardware=collection('04_MECHANICAL_HARDWARE');head=collection('05_HEAD_SHELLS');optics=collection('06_OPTICS');pack=collection('07_FLIGHT_PACK');weapon=collection('08_OPTIONAL_SWORD')
studio=bpy.data.collections.new('90_REVIEW_STUDIO');sc.collection.children.link(studio)
refs=bpy.data.collections.new('99_ORIGINAL_REFERENCE');sc.collection.children.link(refs);refs.hide_render=True;refs.hide_viewport=True
root=bpy.data.objects.new('VALKYR_PARALLEL_R01_ROOT',None);rig.objects.link(root)
root['line']='Independent new VALKYR design; no V3 geometry reused';root['front_axis']='-Y';root['unit_note']='6.5 design units including antenna; source card 18.4m is fictional setting, not CAD';root['shoulder_cannon']='Absent per user 2026-09-11';root['method']='FAZZ: explicit shell/eye occlusion; Justice: inner frame, open armour shells, assembly pivots'
for fname in ('VALKYR_ORIGINAL_BODY.png','VALKYR_ORIGINAL_HEAD.png'):
 img=bpy.data.images.load(str(OUT/'references'/fname));img.pack();ob=bpy.data.objects.new(fname,None);ob.empty_display_type='IMAGE';ob.data=img;refs.objects.link(ob)
def mat(n,h,metal=.3,rough=.38,em=0):
 c=tuple(int(h[i:i+2],16)/255 for i in (0,2,4));linear=tuple(v/12.92 if v<=.04045 else ((v+.055)/1.055)**2.4 for v in c)
 m=bpy.data.materials.new(n);m.use_nodes=True;m.diffuse_color=(*linear,1);nodes=m.node_tree.nodes;bs=next(n for n in nodes if n.type=='BSDF_PRINCIPLED');bs.inputs['Base Color'].default_value=(*linear,1);bs.inputs['Metallic'].default_value=metal;bs.inputs['Roughness'].default_value=rough
 if em:bs.inputs['Emission Color'].default_value=(*linear,1);bs.inputs['Emission Strength'].default_value=em
 else:
  noise=nodes.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=210;noise.inputs['Detail'].default_value=2
  bump=nodes.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.1;bump.inputs['Distance'].default_value=.001
  m.node_tree.links.new(noise.outputs['Fac'],bump.inputs['Height']);m.node_tree.links.new(bump.outputs['Normal'],bs.inputs['Normal'])
 return m
blue=mat('Valkyr blue ceramic alloy','305DA6',.42,.32);navy=mat('Deep navy under-armour','14263F',.4,.38);edge=mat('Blue chamfer highlight','527CC0',.48,.31);white=mat('Cool white ceramic','D2DAE3',.18,.33);dark=mat('Graphite inner chassis','252E39',.65,.35);steel=mat('Machined titanium','79858F',.83,.27);black=mat('Cavity and joint seals','0B111A',.08,.6);gold=mat('Satin gold accents','D6A753',.65,.29);cyan=mat('Recessed cyan optics','39DAFF',.28,.25,2.2);labelmat=mat('Technical markings','C1D3E8',.1,.5)
def parent_keep(ob,p):
 if p:ob.parent=p;ob.matrix_parent_inverse=p.matrix_world.inverted()
def pivot(n,loc,p=root):
 o=bpy.data.objects.new(n,None);rig.objects.link(o);o.location=loc;bpy.context.view_layer.update();parent_keep(o,p);bpy.context.view_layer.update();return o
pelvis=pivot('HIP_MOUNT',(0,0,3.35));torso=pivot('TORSO_MOUNT',(0,0,3.9),pelvis);neck=pivot('HEAD_MOUNT',(0,0,5.4),torso);back=pivot('BACKPACK_MOUNT',(0,.46,4.9),torso)
def mesh(n,v,f,m=blue,c=body,p=torso,b=.012):
 me=bpy.data.meshes.new(n);me.from_pydata(v,[],f);me.update();bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free();ob=bpy.data.objects.new(n,me);c.objects.link(ob);me.materials.append(m);parent_keep(ob,p)
 if b:
  mod=ob.modifiers.new('Local scale edge bevel','BEVEL');mod.width=b;mod.segments=3;mod.affect='EDGES';mod=ob.modifiers.new('Area weighted normals','WEIGHTED_NORMAL');mod.keep_sharp=True;mod.weight=30
 return ob
def plate(n,pts,depth=.06,m=blue,c=body,p=torso,b=.01,bulge=.02):
 pts=[Vector(v) for v in pts];N=len(pts);ctr=sum(pts,Vector())/N
 inset=[ctr+(v-ctr)*.87+Vector((0,-bulge,0)) for v in pts];v=pts+inset+[q+Vector((0,depth,0)) for q in pts]
 f=[tuple(range(N,2*N)),tuple(range(3*N-1,2*N-1,-1))]
 for i in range(N):
  j=(i+1)%N;f.extend([(i,j,N+j,N+i),(i,2*N+i,2*N+j,j)])
 return mesh(n,v,f,m,c,p,b)
def panel(n,q,y,depth=.06,m=blue,c=body,p=torso,b=.01,bulge=.02):return plate(n,[(x,y,z) for x,z in q],depth,m,c,p,b,bulge)
def box(n,loc,size,m=dark,c=core,p=torso,b=.015):
 x,y,z=loc;w,d,h=[s/2 for s in size];return panel(n,[(x-w,z-h),(x+w,z-h),(x+w,z+h),(x-w,z+h)],y-d,2*d,m,c,p,b,0)
def cylinder(n,a,b,r,m=steel,c=hardware,p=torso,N=24,r2=None):
 a=Vector(a);b=Vector(b);axis=(b-a).normalized();u=axis.cross(Vector((0,1,0)))
 if u.length<.01:u=axis.cross(Vector((1,0,0)))
 u.normalize();w=axis.cross(u);r2=r if r2 is None else r2
 v=[q+rr*(u*math.cos(i*math.tau/N)+w*math.sin(i*math.tau/N)) for q,rr in ((a,r),(b,r2)) for i in range(N)]
 f=[tuple(range(N-1,-1,-1)),tuple(range(N,2*N))]+[(i,(i+1)%N,(i+1)%N+N,i+N) for i in range(N)]
 o=mesh(n,v,f,m,c,p,min(.009,r*.12))
 for poly in o.data.polygons:poly.use_smooth=len(poly.vertices)==4
 return o
def pipe(n,points,r,m=steel,c=hardware,p=torso):
 for i in range(len(points)-1):cylinder(n+'_%02d'%i,points[i],points[i+1],r,m,c,p,16)
def shell(n,rows,m=blue,c=body,p=torso,wall=.035):
 # Octagonal section loft with explicit inside, rim and return; open ends retain real wall thickness.
 v=[];N=8;L=len(rows)
 uv=[(-.7,-1),(.7,-1),(1,-.65),(1,.65),(.7,1),(-.7,1),(-1,.65),(-1,-.65)]
 for inner in (False,True):
  for x,y,z,wx,dy in rows:
   for a,b in uv:v.append((x+a*(wx-(wall if inner else 0)),y+b*(dy-(wall if inner else 0)),z))
 f=[]
 for offset in (0,L*N):
  for j in range(L-1):
   for i in range(N):f.append((offset+j*N+i,offset+j*N+(i+1)%N,offset+(j+1)*N+(i+1)%N,offset+(j+1)*N+i))
 for j in (0,L-1):
  for i in range(N):f.append((j*N+i,j*N+(i+1)%N,(L+j)*N+(i+1)%N,(L+j)*N+i))
 return mesh(n,v,f,m,c,p,.008)
def bolt(n,x,y,z,p=torso):
 cylinder(n,(x,y+.014,z),(x,y-.012,z),.018,steel,hardware,p,16)
 cylinder(n+'_hex_socket',(x,y-.013,z),(x,y-.015,z),.008,black,hardware,p,6)
def vent(n,q,y,p=torso,m=gold,count=5,c=hardware):
 panel(n+'_deep_well',q,y,.065,black,c,p,.005,0);xs=[v[0] for v in q];zs=[v[1] for v in q];lo,hi=min(xs),max(xs);bottom,top=min(zs),max(zs)
 for i in range(count):
  z=bottom+(i+1)*(top-bottom)/(count+1);box(n+'_recessed_louvre_%d'%i,((lo+hi)/2,y-.012,z),(hi-lo-.04,.045,.023),m,c,p,.004)
def text(n,bodytext,loc,size=.06,m=labelmat,p=torso,rotate=0):
 cu=bpy.data.curves.new(n,'FONT');cu.body=bodytext;cu.size=size;cu.extrude=.0004;cu.align_x='CENTER';ob=bpy.data.objects.new(n,cu);hardware.objects.link(ob);ob.location=loc;ob.rotation_euler=(math.pi/2,0,rotate);cu.materials.append(m);parent_keep(ob,p);return ob
# Load-bearing core: neck, shoulder axes, sternum, waist pistons and pelvis.
box('Closed thoracic carrier',(0,.08,4.78),(1.14,.63,.88));shell('Waist ribbed bearing',[(0,0,3.78,.32,.26),(0,0,4.21,.36,.29)],dark,core,torso)
for z in [3.87,3.98,4.09]:shell('Abdominal bearing seal %.2f'%z,[(0,0,z,.36,.29),(0,0,z+.045,.36,.29)],black,core,torso,.025)
cylinder('Waist rotation spindle',(0,0,3.38),(0,0,3.84),.29,steel,core,pelvis)
box('Pelvis enclosed gearbox',(0,.02,3.43),(.78,.57,.40),dark,core,pelvis)
cylinder('Neck bearing',(0,0,5.26),(0,0,5.59),.16,steel,core,neck)
shell('Collar docking rim',[(0,0,5.25,.30,.29),(0,0,5.39,.25,.24)],navy,body,torso,.06)
for s in (-1,1):
 lab='L' if s>0 else 'R'
 def mp(q):return [(s*x,z) for x,z in q]
 cylinder('Shoulder cross axle.'+lab,(s*.40,0,5.03),(s*1.16,0,5.03),.145,steel,core,torso)
 cylinder('Abdomen piston sleeve.'+lab,(s*.35,-.14,3.9),(s*.49,-.10,4.31),.063,dark,core,torso)
 cylinder('Abdomen exposed rod.'+lab,(s*.39,-.17,4.03),(s*.48,-.14,4.45),.029,steel,hardware,torso)
 # Each pectoral has sloping carrier faces and a physically recessed, slanted intake.
 plate('Chest deep side carrier.'+lab,[(s*.17,-.45,5.12),(s*.72,-.29,5.15),(s*.86,-.28,4.69),(s*.48,-.51,4.40),(s*.23,-.52,4.57)],.30,navy)
 plate('Pectoral swept upper armour.'+lab,[(s*.16,-.52,5.12),(s*.74,-.35,5.22),(s*.83,-.38,4.95),(s*.33,-.60,4.76)],.10,blue)
 panel('Chest intake shadow.'+lab,mp([(.26,4.91),(.72,5.08),(.70,4.79),(.34,4.64)]),-.615,.09,black,b=.007)
 for j in range(3):
  z=4.77+j*.065
  panel('Gold slanted cooling vane.%s.%d'%(lab,j),mp([(.30,z),(.69,z+.15),(.69,z+.125),(.30,z-.025)]),-.64,.036,gold,hardware,b=.003,bulge=0)
 plate('Pectoral lower ceramic blade.'+lab,[(s*.31,-.63,4.61),(s*.71,-.48,4.75),(s*.77,-.41,4.57),(s*.47,-.53,4.35)],.065,white)
 plate('Floating abdominal flank.'+lab,[(s*.33,-.41,4.32),(s*.56,-.34,4.44),(s*.56,-.27,4.01),(s*.38,-.40,3.84)],.10,blue)
 for x,z in ((.73,5.16),(.69,4.62),(.42,4.04)):bolt('Chest captive fastener.'+lab,s*x,-.47 if z>5 else -.54,z)
# Central blue cockpit keystone.
panel('Sternum central underlying spine',[(-.19,5.19),(.19,5.19),(.29,4.78),(.13,4.18),(0,4.05),(-.13,4.18),(-.29,4.78)],-.56,.23,navy)
panel('Cockpit blue upper arrow',[(-.13,5.13),(.13,5.13),(.23,4.82),(0,4.65),(-.23,4.82)],-.65,.075,blue)
panel('Cockpit white chevron',[(-.22,4.76),(0,4.62),(.22,4.76),(.15,4.51),(0,4.39),(-.15,4.51)],-.655,.065,white)
panel('Cockpit lower blue keel',[(-.12,4.46),(0,4.33),(.12,4.46),(.13,4.23),(0,4.12),(-.13,4.23)],-.57,.07,blue)
text('Chest ID','V A L K Y R',(0,-.678,4.95),.038)
# Pelvis: independent front skirts, side skirt hinges, recessed plates.
panel('Pelvic upper belt',[(-.52,3.73),(.52,3.73),(.57,3.49),(.27,3.35),(-.27,3.35),(-.57,3.49)],-.39,.20,navy,p=pelvis)
panel('Pelvic center blue shield',[(-.19,3.72),(.19,3.72),(.25,3.40),(0,3.12),(-.25,3.40)],-.46,.10,blue,p=pelvis)
for s in (-1,1):
 lab='L' if s>0 else 'R'
 def mp(q):return [(s*x,z) for x,z in q]
 hip=pivot('HIP_SWING.'+lab,(s*.49,0,3.27),pelvis);thigh=pivot('THIGH_MOUNT.'+lab,(s*.52,0,3.20),hip);shin=pivot('KNEE_PIVOT.'+lab,(s*.67,-.025,1.94),thigh);foot=pivot('ANKLE_PIVOT.'+lab,(s*.80,-.03,.55),shin)
 arm=pivot('SHOULDER_PIVOT.'+lab,(s*1.04,0,5.03),torso);fore=pivot('ELBOW_PIVOT.'+lab,(s*1.32,-.04,4.13),arm);handp=pivot('WRIST_PIVOT.'+lab,(s*1.52,-.12,3.35),fore)
 pivot('GRIP_SOCKET.'+lab,(s*1.52,-.15,3.10),handp)
 # Swept shoulder silhouette is a real folded shell, with a lower carrier and separate top ridge.
 plate('Shoulder inner return shell.'+lab,[(s*.91,-.14,5.27),(s*1.63,-.07,5.48),(s*1.58,-.26,4.95),(s*1.27,-.35,4.70),(s*.99,-.26,4.90)],.41,navy,p=arm)
 shoulder=plate('Main delta pauldron.'+lab,[(s*.92,-.28,5.30),(s*1.70,-.20,5.64),(s*1.59,-.50,5.02),(s*1.26,-.51,4.87),(s*1.04,-.40,5.04)],.105,blue,p=arm,bulge=.035)
 plate('Pauldron upper silver knife edge.'+lab,[(s*.93,-.295,5.31),(s*1.70,-.215,5.65),(s*1.63,-.32,5.51),(s*1.08,-.405,5.22)],.030,white,p=arm,b=.004)
 plate('Pauldron gold inset.'+lab,[(s*1.62,-.30,5.42),(s*1.55,-.36,5.17),(s*1.51,-.41,5.21),(s*1.57,-.345,5.43)],.021,gold,p=arm,b=.003)
 plate('Shoulder lower layered vane.'+lab,[(s*1.37,-.11,5.02),(s*1.66,-.02,5.16),(s*1.57,-.10,4.76),(s*1.39,-.24,4.65)],.17,blue,p=arm)
 text('Shoulder designation.'+lab,'01' if s<0 else 'V', (s*1.31,-.56,5.08),.17,p=arm)
 for x,z in ((1.08,5.26),(1.55,5.18)):bolt('Pauldron fastener.'+lab,s*x,-.43,z,arm)
 # Upper arm ceramic envelope and exposed elbow axis.
 cylinder('Upper arm frame.'+lab,(s*1.06,0,4.93),(s*1.31,-.02,4.19),.105,dark,core,arm)
 shell('Upper arm ceramic shell.'+lab,[(s*1.13,0,4.81,.22,.22),(s*1.18,0,4.59,.25,.25),(s*1.26,-.02,4.28,.18,.20)],white,body,arm)
 panel('Bicep recessed long plate.'+lab,mp([(1.04,4.68),(1.22,4.71),(1.35,4.34),(1.16,4.29)]),-.251,.03,navy,p=arm)
 cylinder('Elbow hinge dark outer.'+lab,(s*1.11,-.04,4.12),(s*1.48,-.04,4.12),.18,dark,core,fore)
 cylinder('Elbow bearing face.'+lab,(s*1.46,-.04,4.12),(s*1.50,-.04,4.12),.128,steel,hardware,fore)
 cylinder('Forearm load strut.'+lab,(s*1.34,-.03,4.08),(s*1.50,-.10,3.43),.12,dark,core,fore)
 shell('Forearm tapered housing.'+lab,[(s*1.35,-.05,4.09,.25,.25),(s*1.42,-.08,3.91,.28,.29),(s*1.52,-.12,3.42,.19,.22)],navy,body,fore)
 plate('Forearm long blue front plate.'+lab,[(s*1.20,-.33,4.06),(s*1.46,-.37,4.15),(s*1.66,-.38,3.53),(s*1.47,-.39,3.37),(s*1.32,-.35,3.64)],.075,blue,p=fore)
 plate('Forearm ceramic lateral blade.'+lab,[(s*1.62,-.22,3.92),(s*1.76,-.07,4.04),(s*1.76,-.04,3.45),(s*1.56,-.22,3.24)],.05,white,p=fore)
 panel('Forearm sensor seat.'+lab,mp([(1.34,3.93),(1.43,3.96),(1.47,3.73),(1.38,3.70)]),-.412,.03,black,p=fore,b=.003)
 panel('Forearm sensor lens.'+lab,mp([(1.37,3.90),(1.42,3.92),(1.45,3.77),(1.40,3.75)]),-.418,.012,cyan,optics,fore,.002,0)
 for z in (3.65,3.54):bolt('Forearm captive service pin.'+lab,s*1.47,-.424,z,fore)
 cylinder('Wrist cuff.'+lab,(s*1.51,-.11,3.44),(s*1.52,-.12,3.22),.13,steel,core,handp)
 box('Hand palm carrier.'+lab,(s*1.53,-.13,3.11),(.28,.23,.29),dark,core,handp)
 panel('Hand back armour.'+lab,mp([(1.39,3.23),(1.65,3.23),(1.67,2.98),(1.42,2.93)]),.015,.055,blue,p=handp)
 for j in range(4):
  xx=s*(1.42+j*.07)
  for k in range(3):
   box('Articulated finger.%s.%d.%d'%(lab,j,k),(xx,-.25-k*.014,3.08-k*.071),(.058,.115,.065),steel if k==0 else dark,hardware,handp,.009)
 cylinder('Opposable thumb joint.'+lab,(s*1.36,-.15,3.15),(s*1.36,-.30,3.03),.055,dark,hardware,handp,16)
 box('Thumb distal.'+lab,(s*1.39,-.32,2.99),(.08,.11,.10),dark,hardware,handp,.016)
 # Hip and floating skirts.
 cylinder('Hip bearing axle.'+lab,(s*.29,0,3.29),(s*.65,0,3.29),.18,steel,core,hip)
 skirt=pivot('FRONT_SKIRT_HINGE.'+lab,(s*.37,-.31,3.59),pelvis)
 plate('Front skirt blue armour.'+lab,[(s*.27,-.42,3.60),(s*.55,-.40,3.60),(s*.76,-.42,3.11),(s*.52,-.55,2.99),(s*.33,-.54,3.19)],.10,blue,p=skirt)
 panel('Front skirt inset face.'+lab,mp([(.35,3.49),(.51,3.49),(.65,3.15),(.52,3.11),(.41,3.25)]),-.565,.034,navy,p=skirt,b=.004)
 plate('Side skirt scabbard fin.'+lab,[(s*.61,-.02,3.60),(s*.90,.02,3.55),(s*1.05,-.04,2.92),(s*.84,-.14,3.08)],.24,blue,p=pelvis)
 # Long quadriceps: slim functional section under one dominant blue shield.
 cylinder('Thigh structural spar.'+lab,(s*.51,0,3.22),(s*.67,-.025,2.02),.13,dark,core,thigh)
 shell('Thigh structural ceramic shell.'+lab,[(s*.53,.04,3.21,.24,.24),(s*.58,.02,2.96,.29,.29),(s*.64,0,2.43,.25,.27),(s*.67,-.02,2.15,.17,.21)],white,body,thigh)
 plate('Long quadriceps blue shield.'+lab,[(s*.38,-.25,3.12),(s*.71,-.25,3.17),(s*.90,-.30,2.52),(s*.73,-.35,2.22),(s*.46,-.33,2.43)],.08,blue,p=thigh)
 panel('Thigh panel recess.'+lab,mp([(.47,2.95),(.66,2.98),(.79,2.56),(.70,2.40),(.56,2.52)]),-.381,.022,navy,p=thigh,b=.005)
 panel('Thigh central enamel panel.'+lab,mp([(.49,2.91),(.64,2.94),(.75,2.57),(.69,2.46),(.59,2.55)]),-.389,.025,blue,p=thigh,b=.004)
 bolt('Thigh panel pin.'+lab,s*.59,-.413,2.83,thigh);text('Thigh stencil.'+lab,'R-01',(s*.63,-.425,2.59),.048,p=thigh)
 cylinder('Knee transverse hinge.'+lab,(s*.45,-.02,1.98),(s*.91,-.02,1.98),.205,dark,core,shin)
 cylinder('Knee lateral bearing.'+lab,(s*.9,-.02,1.98),(s*.94,-.02,1.98),.15,steel,hardware,shin)
 cylinder('Knee inset hub.'+lab,(s*.942,-.02,1.98),(s*.956,-.02,1.98),.098,black,hardware,shin)
 panel('Knee impact pointed guard.'+lab,mp([(.48,2.14),(.70,2.28),(.90,2.10),(.80,1.86),(.61,1.78),(.46,1.95)]),-.405,.18,navy,p=shin)
 panel('Knee blue cap.'+lab,mp([(.52,2.12),(.69,2.20),(.83,2.08),(.76,1.96),(.60,1.94)]),-.49,.055,blue,p=shin)
 # Calf shells bulge in side view, while the frontal shin blade remains narrow.
 cylinder('Shin structural spine.'+lab,(s*.69,.04,1.88),(s*.80,0,.57),.13,dark,core,shin)
 shell('Calf white closed shell.'+lab,[(s*.70,.12,1.91,.22,.27),(s*.73,.15,1.68,.32,.39),(s*.78,.10,1.21,.31,.37),(s*.81,.02,.72,.18,.22)],white,body,shin,.04)
 plate('Shin long blue keel.'+lab,[(s*.54,-.29,1.90),(s*.79,-.32,1.92),(s*.94,-.40,1.46),(s*.91,-.36,.89),(s*.76,-.37,.62),(s*.63,-.39,.93)],.09,blue,p=shin)
 panel('Shin inset long channel.'+lab,mp([(.65,1.64),(.73,1.70),(.85,1.38),(.81,.93),(.75,.81),(.70,1.06)]),-.447,.025,navy,p=shin,b=.004)
 plate('Calf external swept blade.'+lab,[(s*.97,.02,1.85),(s*1.12,.22,1.74),(s*1.12,.32,1.14),(s*.94,.04,.82)],.09,blue,p=shin)
 plate('Calf outside gold notch.'+lab,[(s*1.00,-.01,1.62),(s*1.065,.05,1.61),(s*1.06,.04,1.43),(s*.995,-.04,1.44)],.021,gold,p=shin,b=.003)
 for y in (.28,.40):cylinder('Achilles piston.'+lab,(s*.78,y,.68),(s*.76,y,1.50),.028,steel,hardware,shin)
 cylinder('Calf rear actuator sleeve.'+lab,(s*.78,.43,1.05),(s*.76,.43,1.55),.065,dark,hardware,shin)
 # Feet have a sloped instep, articulated toe and heel block.
 cylinder('Ankle transverse joint.'+lab,(s*.61,0,.57),(s*.98,0,.57),.14,steel,core,foot)
 xc=s*.81
 shell('Ankle split housing.'+lab,[(xc,0,.44,.23,.24),(xc,0,.72,.20,.22)],navy,body,foot)
 box('Foot sole rail.'+lab,(xc,-.23,.115),(.56,1.09,.12),black,core,foot,.025)
 v=[(xc-.26,-.77,.17),(xc+.26,-.77,.17),(xc+.26,.27,.17),(xc-.26,.27,.17),(xc-.20,-.70,.30),(xc+.20,-.70,.30),(xc+.20,-.13,.53),(xc-.20,-.13,.53),(xc+.22,.24,.33),(xc-.22,.24,.33)]
 f=[(0,3,2,1),(0,1,5,4),(0,4,7,9,3),(1,2,8,6,5),(3,9,8,2),(4,5,6,7),(7,6,8,9)]
 mesh('Foot sloped blue armour.'+lab,v,f,blue,body,foot,.02)
 panel('Toe white bumper.'+lab,[(xc-.25,.16),(xc+.25,.16),(xc+.21,.28),(xc-.21,.28)],-.785,.055,white,p=foot,b=.007)
 for j in (-1,1):box('Toe longitudinal rib.%s.%d'%(lab,j),(xc+j*.11,-.52,.34),(.027,.29,.035),steel,hardware,foot,.004)
 box('Heel stabiliser.'+lab,(xc,.29,.22),(.48,.22,.27),navy,body,foot,.019)
# Back: two compact vector thrusters, layered flight vanes; no cannon or barrel geometry.
box('Backpack central spine',(0,.59,4.91),(.47,.39,.97),navy,pack,back)
box('Backpack luminous seat',(0,.805,4.99),(.15,.065,.43),black,pack,back)
box('Backpack recessed sensor',(0,.844,4.99),(.074,.018,.32),cyan,optics,back,.003)
for s in (-1,1):
 lab='L' if s>0 else 'R'
 pod=pivot('VECTOR_THRUSTER.'+lab,(s*.47,.56,4.75),back)
 shell('Thruster pod outer shell.'+lab,[(s*.47,.61,5.37,.24,.25),(s*.49,.64,5.12,.29,.29),(s*.48,.66,4.46,.23,.24)],blue,pack,pod)
 cylinder('Thruster gimbal.'+lab,(s*.48,.67,4.51),(s*.48,.69,4.38),.21,steel,pack,pod)
 shell('Thruster nozzle open lip.'+lab,[(s*.48,.70,4.46,.19,.19),(s*.48,.74,4.20,.25,.25)],dark,pack,pod,.04)
 cylinder('Thruster deep cavity.'+lab,(s*.48,.73,4.28),(s*.48,.73,4.32),.16,black,pack,pod)
 cylinder('Thruster inner iris.'+lab,(s*.48,.73,4.29),(s*.48,.73,4.305),.075,cyan,optics,pod)
 fin=pivot('FLIGHT_VANE_HINGE.'+lab,(s*.69,.63,5.14),back)
 plate('Primary swept backpack vane.'+lab,[(s*.69,.76,5.37),(s*1.05,.87,5.58),(s*1.00,1.00,4.94),(s*1.49,1.04,3.71),(s*1.15,.88,4.06),(s*.65,.76,4.90)],.10,navy,pack,fin)
 plate('Backpack upper blue blade.'+lab,[(s*.73,.70,5.36),(s*1.04,.80,5.63),(s*.93,.83,5.12),(s*.75,.74,4.84)],.055,blue,pack,fin)
 plate('Backpack long outer silver edge.'+lab,[(s*.96,.94,4.95),(s*1.49,.99,3.70),(s*1.37,.94,3.94),(s*.91,.86,4.79)],.025,white,pack,fin,.004)
 for j in range(4):box('Backpack cooling fin.%s.%d'%(lab,j),(s*.49,.925,4.73+j*.105),(.34,.038,.033),steel,pack,pod,.004)
# Head: newly constructed section shell, recessed eye cavities, forehead volume and silver/gold antennae.
shell('Head inner cranial carrier',[(0,.015,5.48,.15,.15),(0,.035,5.65,.26,.22),(0,.04,5.99,.31,.25),(0,.06,6.20,.20,.17)],dark,core,neck,.035)
for s in (-1,1):
 lab='L' if s>0 else 'R'
 # Longitudinal shell strips connect crown to temple with genuine lateral depth.
 rows=[(5.55,.16,.27,-.02,.22),(5.72,.22,.36,-.20,.27),(6.02,.17,.38,-.20,.27),(6.22,.10,.28,-.15,.22),(6.30,.04,.16,-.08,.12)]
 vs=[]
 for z,xi,xo,yf,yb in rows:vs.extend([(s*xi,yf,z),(s*xo*.90,yf*.5,z),(s*xo,.07,z),(s*xo*.88,yb,z),(s*xi,yb+.03,z)])
 faces=[(j*5+k,j*5+k+1,(j+1)*5+k+1,(j+1)*5+k) for j in range(len(rows)-1) for k in range(4)]
 ob=mesh('Hollow helmet outer half.'+lab,vs,faces,blue,head,neck,0);so=ob.modifiers.new('Explicit inward helmet wall','SOLIDIFY');so.thickness=.032;so.offset=-1;be=ob.modifiers.new('Fine helmet chamfer','BEVEL');be.width=.006;be.segments=3;ob.modifiers.new('Helmet face normals','WEIGHTED_NORMAL')
 def mp(q):return [(s*x,z) for x,z in q]
 panel('Eye chamber rim.'+lab,mp([(.035,5.87),(.31,5.99),(.29,5.82),(.12,5.72)]),-.366,.13,black,head,neck,.004,0)
 panel('Recessed angular cyan lens.'+lab,mp([(.07,5.855),(.283,5.947),(.258,5.869),(.125,5.785)]),-.379,.018,cyan,optics,neck,.002,0)
 panel('White eye brow knife.'+lab,mp([(.025,5.90),(.31,6.045),(.32,5.986),(.082,5.871)]),-.407,.049,white,head,neck,.003,.008)
 plate('Temple deep angular armour.'+lab,[(s*.28,-.21,6.15),(s*.42,.04,6.18),(s*.42,.09,5.86),(s*.31,-.22,5.77)],.055,blue,head,neck,.005)
 plate('Cheek face support.'+lab,[(s*.27,-.27,5.86),(s*.35,-.10,5.82),(s*.29,-.10,5.52),(s*.16,-.34,5.48),(s*.19,-.38,5.68)],.045,white,head,neck,.004)
 plate('Cheek outer layered blade.'+lab,[(s*.32,-.10,5.84),(s*.41,.09,5.87),(s*.38,.14,5.60),(s*.25,-.02,5.44)],.035,blue,head,neck,.004)
 cylinder('Temple bearing.'+lab,(s*.35,.045,5.99),(s*.40,.045,5.99),.072,dark,hardware,neck)
 cylinder('Temple axle cover.'+lab,(s*.40,.045,5.99),(s*.416,.045,5.99),.045,steel,hardware,neck)
 plate('Swept silver antenna.'+lab,[(s*.24,-.28,5.99),(s*.30,-.235,6.02),(s*.49,.035,6.52),(s*.38,-.045,6.10),(s*.32,-.16,5.92)],.028,white,head,neck,.002)
 plate('Antenna gold inner insert.'+lab,[(s*.285,-.282,6.03),(s*.325,-.218,6.02),(s*.423,-.085,6.29),(s*.375,-.162,6.10)],.018,gold,head,neck,.002)
 for j in range(3):box('Head rear recessed louvre.%s.%d'%(lab,j),(s*.18,.328,5.77+j*.088),(.16,.033,.029),steel,hardware,neck,.004)
panel('Forehead inner sensor channel',[(-.11,6.17),(.11,6.17),(.12,5.99),(0,5.89),(-.12,5.99)],-.281,.07,black,head,neck,.004)
panel('Forehead cyan sensor',[(-.039,6.085),(.039,6.085),(.037,5.979),(0,5.945),(-.037,5.979)],-.302,.018,cyan,optics,neck,.002,0)
plate('Crown forward blue keystone',[(-.10,-.16,6.29),(.10,-.16,6.29),(.15,-.25,6.15),(.10,-.31,6.09),(0,-.27,6.13),(-.10,-.31,6.09),(-.15,-.25,6.15)],.12,blue,head,neck,.007)
plate('Central folded white mask',[(-.13,-.395,5.78),(0,-.454,5.83),(.13,-.395,5.78),(.12,-.40,5.59),(0,-.468,5.45),(-.12,-.40,5.59)],.065,white,head,neck,.004,.008)
panel('Blue pointed chin',[(-.057,5.56),(0,5.49),(.057,5.56),(.055,5.43),(0,5.36),(-.055,5.43)],-.43,.13,navy,head,neck,.003)
box('Rear head service spine',(0,.30,5.97),(.115,.075,.37),navy,head,neck,.008)
# Optional independent anti-ship blade: display alongside assembly, hidden by default to inspect body.
sword=pivot('OPTIONAL_SWORD_MOUNT',(-1.52,-.20,3.1),root);sword['optional']='Independent sword, no hand animation/skin binding claimed'
for n in [o for o in rig.objects if o==sword]:pass
cylinder('Sword grip',(-1.52,-.20,3.32),(-1.52,-.20,2.92),.063,dark,weapon,sword,16)
for z in [2.98,3.04,3.10,3.16,3.22,3.28]:cylinder('Grip ring %.2f'%z,(-1.52,-.20,z),(-1.52,-.20,z+.013),.068,black,weapon,sword,16)
cylinder('Sword emitter hub',(-1.52,-.24,2.85),(-1.52,-.15,2.85),.16,steel,weapon,sword,24)
cylinder('Sword emitter iris',(-1.52,-.251,2.85),(-1.52,-.247,2.85),.093,cyan,weapon,sword,24)
panel('Sword long solid edge',[(-1.66,2.73),(-1.36,2.73),(-1.37,.52),(-1.54,.20),(-1.67,.50)],-.21,.10,navy,weapon,sword,.006,.008)
panel('Sword silver cutting edge',[(-1.36,2.72),(-1.29,2.59),(-1.30,.50),(-1.54,.19),(-1.37,.54)],-.233,.025,white,weapon,sword,.002,0)
weapon.hide_render=True;weapon.hide_viewport=True
# Clean review stage and fixed orthographic cameras, neutral functional standing pose.
world=bpy.data.worlds.new('Neutral studio');sc.world=world;world.use_nodes=True;world.node_tree.nodes['Background'].inputs[0].default_value=(.07,.095,.15,1);world.node_tree.nodes['Background'].inputs[1].default_value=.45
floor=box('Studio floor',(0,0,-.05),(200,200,.08),mat('Studio slate','171F2C',.15,.6),studio,None,0)
def light(n,loc,power,size,color):
 d=bpy.data.lights.new(n,'AREA');o=bpy.data.objects.new(n,d);studio.objects.link(o);o.location=loc;o.rotation_euler=(Vector((0,0,3.2))-o.location).to_track_quat('-Z','Y').to_euler();d.energy=power;d.shape='DISK';d.size=size;d.color=color
light('Large softbox',(4,-6,10),2100,5,(.83,.9,1));light('Warm frontal fill',(-5,-4,5),1400,4,(1,.87,.73));light('Back edge',(2,5,8),2600,3,(.43,.66,1))
def camera(n,loc,target,scale):
 d=bpy.data.cameras.new(n);o=bpy.data.objects.new(n,d);studio.objects.link(o);o.location=loc;o.rotation_euler=(Vector(target)-o.location).to_track_quat('-Z','Y').to_euler();d.type='ORTHO';d.ortho_scale=scale;d.lens=75;return o
cams={
 '01_THREE_QUARTER':camera('R01_ThreeQuarter',(9,-15,9),(0,0,3.2),7.9),
 '02_FRONT':camera('R01_Front',(0,-16,3.30),(0,0,3.30),7.1),
 '03_SIDE':camera('R01_Side',(16,0,3.30),(0,0,3.30),7.1),
 '04_BACK':camera('R01_Back',(0,16,3.30),(0,0,3.30),7.1),
 '05_HEAD':camera('R01_Head',(2.5,-5,7.6),(0,-.03,5.97),1.7),
 '06_HEAD_FRONT':camera('R01_HeadFront',(0,-8,5.97),(0,-.03,5.97),1.48),
 '07_LEG':camera('R01_Leg',(5,-8,4),(0,0,1.85),3.8),
 '08_BACK_DETAIL':camera('R01_BackDetail',(4,8,7),(0,.4,4.92),3.3),
 '09_HEAD_LOW':camera('R01_HeadLow',(1.8,-4,4.9),(0,0,5.94),1.7)
}
sc.render.engine='CYCLES';sc.cycles.samples=48;sc.cycles.use_denoising=True
try:
 prefs=bpy.context.preferences.addons['cycles'].preferences;prefs.compute_device_type='OPTIX';prefs.get_devices()
 for dev in prefs.devices:dev.use=dev.type!='CPU'
 sc.cycles.device='GPU'
except Exception:sc.cycles.device='CPU'
sc.render.resolution_x=1500;sc.render.resolution_y=1700;sc.render.resolution_percentage=100
sc.view_settings.view_transform='AgX';sc.camera=cams['01_THREE_QUARTER'];sc.render.image_settings.file_format='PNG';sc.render.film_transparent=False
# Normalise pivots only once after all parenting; keep saved whole-mech assembly and all source modifiers.
bpy.context.view_layer.update();bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'VALKYR_PARALLEL_R01.blend'),compress=True)
manifest={'asset':'VALKYR PARALLEL R01','geometry_created_from_scratch':True,'old_V3_modified':False,'shoulder_cannon':False,'mesh_count':len([o for o in full.all_objects if o.type=='MESH']),'reference_images':['references/VALKYR_ORIGINAL_BODY.png','references/VALKYR_ORIGINAL_HEAD.png'],'pivots':[o.name for o in rig.objects],'limits':['Design study with assembly pivots, not skinned animation rig','Reference-inferred rear and internal details','No Unity integration for this separate line']}
(OUT/'work/build_manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
for name in ['01_THREE_QUARTER','02_FRONT','05_HEAD']:
 sc.camera=cams[name];sc.render.filepath=str(OUT/'renders'/f'{name}.png');bpy.ops.render.render(write_still=True)
print('VALKYR_R01_BUILT',manifest['mesh_count'],flush=True)
