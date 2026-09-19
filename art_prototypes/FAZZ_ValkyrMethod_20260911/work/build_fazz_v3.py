import bpy,bmesh,math,pathlib,json,sys
from mathutils import Vector,Matrix
from mathutils.geometry import tessellate_polygon
W=pathlib.Path(__file__).resolve().parent;ROOT=W.parent;S=.01
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
SC=bpy.context.scene;M={};PARTS=[];COLS={};COL=None;PARENT=None
asset=bpy.data.collections.new('FAZZ_COMPLETE_ASSEMBLY');SC.collection.children.link(asset)
root=bpy.data.objects.new('FAZZ_MASTER_ROOT',None);asset.objects.link(root)
root['method']='Adapted actual VALKYR V3 arbitrary-plane shell, recessed cassette and wrapped mechanical construction';root['reference']='MG FAZZ Ver.Ka; independent reconstruction'
def mat(key,h,metal=.2,rough=.36,em=0):
 rgb=[int(h[i:i+2],16)/255 for i in [0,2,4]];rgb=[c/12.92 if c<=.04045 else ((c+.055)/1.055)**2.4 for c in rgb]
 m=bpy.data.materials.new('FAZZ '+key);m.use_nodes=True;m.diffuse_color=(*rgb,1);p=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED');p.inputs['Base Color'].default_value=(*rgb,1);p.inputs['Metallic'].default_value=metal;p.inputs['Roughness'].default_value=rough
 p.inputs['Coat Weight'].default_value=.18;p.inputs['Coat Roughness'].default_value=.28
 if em:p.inputs['Emission Color'].default_value=(*rgb,1);p.inputs['Emission Strength'].default_value=em
 M[key]=m
for args in [('white','D1D5D4',.18,.32),('grey','89999C',.32,.37),('panel','B4BEBC',.23,.36),('edge','EDF0E9',.2,.3),('blue','363C7A',.28,.33),('navy','262B42',.4,.37),('frame','344045',.68,.35),('steel','66767A',.78,.3),('titanium','9AA6A7',.82,.26),('silver','BBC1C0',.65,.31),('black','131C21',.2,.45),('rubber','20282A',0,.58),('gold','D9AA36',.35,.3),('red','A82C29',.2,.36),('mark','434F56',.1,.48),('cyan','47DBC5',.35,.2,1.1)]:mat(*args)
exec((W/'v3_geometry.py').read_text(encoding='utf-8'))
def pivot(n,p,parent=root):
 o=bpy.data.objects.new(n,None);asset.objects.link(o);o.parent=parent;o.matrix_world=Matrix.Translation(U(p));bpy.context.view_layer.update();return o
def group(n,parent=root):
 global COL,PARENT
 if n not in COLS:
  c=bpy.data.collections.new(n);asset.children.link(c);COLS[n]=c
 COL=COLS[n];PARENT=parent

def loft(n,sections,mat='white',bevel=.3):
 k=len(sections[0]);vs=[p for r in sections for p in r];fs=[tuple(range(k-1,-1,-1)),tuple(range((len(sections)-1)*k,len(sections)*k))]
 for j in range(len(sections)-1):
  for i in range(k):fs.append((j*k+i,j*k+(i+1)%k,(j+1)*k+(i+1)%k,(j+1)*k+i))
 return mesh(n,vs,fs,mat,bevel)
def ring8(x,y,z,w,f,b):return [(x-w*.65,y-f,z),(x+w*.65,y-f,z),(x+w,y-f*.60,z),(x+w,y+b*.62,z),(x+w*.62,y+b,z),(x-w*.62,y+b,z),(x-w,y+b*.62,z),(x-w,y-f*.6,z)]
def shell(n,sections,thickness=2,mat='white'):
 k=len(sections[0]);L=len(sections);vs=[p for r in sections for p in r]
 for r in sections:
  c=sum((Vector(p) for p in r),Vector())/len(r)
  for p in r:
   p=Vector(p);d=p-c;d.z=0;vs.append(tuple(p-d.normalized()*thickness))
 fs=[]
 for off in [0,k*L]:
  for j in range(L-1):
   for i in range(k):fs.append((off+j*k+i,off+j*k+(i+1)%k,off+(j+1)*k+(i+1)%k,off+(j+1)*k+i))
 for j in [0,L-1]:
  for i in range(k):fs.append((j*k+i,j*k+(i+1)%k,(L+j)*k+(i+1)%k,(L+j)*k+i))
 return mesh(n,vs,fs,mat,.22)
def hose(n,points,r=1.1,mat='rubber'):
 pts=[Vector(p) for p in points];rs=[]
 for i,p in enumerate(pts):
  d=(pts[min(i+1,len(pts)-1)]-pts[max(i-1,0)]).normalized();q=d.to_track_quat('Z','Y');rs.append([tuple(p+q@Vector((r*math.cos(j*math.tau/12),r*math.sin(j*math.tau/12),0))) for j in range(12)])
 return loft(n,rs,mat,.025)
def hatch(n,face,poly,mat='white',dep=1.2):
 pts=[face(x,z,0) for x,z in poly];plate(n,pts,(0,dep,0),mat,.16)
 # A return at one panel edge clarifies the shell thickness.
 strip(n+' lower gasket',[face(*poly[-2],-.1),face(*poly[-1],-.1)],.35,'frame',(0,.2,0))
def bay(n,face,x0,x1,z0,z1,mat='white',depth=4):
 outer=[face(x0,z0,0),face(x1,z0,0),face(x1,z1,0),face(x0,z1,0)]
 inner=[face(x0+1.2,z0+1.2,-.12),face(x1-1.2,z0+1.2,-.12),face(x1-1.2,z1-1.2,-.12),face(x0+1.2,z1-1.2,-.12)]
 recess(n,outer,inner,depth,mat)
def stencil(n,p,txt,size=2,mat='mark',rear=False):text_mesh(n,txt,p,size,mat,(math.pi/2,0,math.pi if rear else 0))
# Load bearing spine and armour mounting frame.
torso=pivot('THORAX',(0,0,337));waist=pivot('PELVIS',(0,0,278));group('01 Inner frame',torso)
loft('Thoracic cast carrier',[ring8(0,0,308,25,22,22),ring8(0,0,342,37,25,25),ring8(0,0,365,30,18,20)],'frame',.55)
rod('Spine column',(0,8,281),(0,8,352),9,'frame',n=32)
for z in [292,302,312]:
 box('Spine traverse '+str(z),(0,10,z),(36,17,5),'steel',.4)
 piston('Spine side ram '+str(z),(17,6,z-7),(21,6,z+7),1.2)
rod('Neck gimbal',(0,0,366),(0,0,378),7,'steel',n=32)
for sign,side in [(-1,'L'),(1,'R')]:
 mi=lambda p:(sign*p[0],p[1],p[2])
 rod('Shoulder load shaft '+side,mi((23,0,351)),mi((66,0,351)),8,'steel')
 bearing('Shoulder race '+side,mi((58,0,351)),(1,0,0),10,10,False)
 piston('Waist lateral ram '+side,mi((21,7,309)),mi((22,11,280)),2.0)
 hose('Waist hydraulic '+side,[mi(p) for p in [(25,15,312),(29,20,301),(27,23,285),(22,19,279)]],1.4)
# FAZZ added armour is a deep sloped, broad chest cassette.
group('02 Chest armour',torso)
for sign,side in [(-1,'L'),(1,'R')]:
 mi=lambda p:(sign*p[0],p[1],p[2])
 def face(x,t,d=0):return mi((x,-42+.50*t+.11*x+d,356-t))
 # Side wall continuous with forward armour, not a detached box.
 skin('Chest wrapped side '+side,[mi(p) for p in [(16,-40,356),(49,-32,356),(51,-6,347),(45,18,337),(24,18,309),(20,-18,309),(48,-27,319)]],[(0,1,6,5),(1,2,3,6),(3,4,5,6)],(-sign*2,2,0),'grey',.35)
 poly=[(15,0),(45,0),(47,9),(42,33),(19,35),(14,22)]
 # Large removable top armour cover; visible mechanical slotted mouth below it.
 hatch('Pectoral ceramic panel '+side,face,poly,'white',3)
 for j in range(2):
  strip('Chest panel stepped seam '+side+str(j),[face(17,26+j*2,-.20),face(42,26+j*2,-.20)],.28,'grey',(0,.2,0))
 def lower(x,t,d=0):return mi((x,-28+.48*t+.12*x+d,325-t))
 bay('Chest lower oblique ventilation '+side,lower,22,43,0,8,'grey',4)
 for j in range(4):strip('Chest inset fin '+side+str(j),[lower(24+j*4,1,1.6),lower(24+j*4,7,1.6)],.8,'steel',(0,1.0,0))
 # Folded black joint skirt under the chest.
 plate('Thoracic underside trim '+side,[mi(p) for p in [(18,-26,316),(43,-17,319),(42,4,308),(23,4,301)]],(0,2,0),'navy',.25)
 for x,t in [(17,4),(41,29)]:bolt('Chest captive screw '+side+str(x),face(x,t,-.8),(0,-1,0),.5,'steel')
 stencil('Chest large unit '+side,face(31,12,-.7),'04' if side=='R' else 'E.F.S.F.',8 if side=='R' else 2.5)
 stencil('Chest small stencil '+side,face(30,22,-.8),'FA-010A / SENTINEL',1.3)
 for j in range(3):strip('Chest red warning '+side+str(j),[face(17,5+j*.7,-.6),face(21,5+j*.7,-.6)],.28,'red',(0,.1,0))
# Central front shell raked to the abdomen with glass camera window.
def cf(x,t,d=0):return (x,-45+.37*t+d,357-t)
bay('Central chest camera',cf,-11,11,0,12,'white',4)
plate('Chest green camera',[cf(-5,3,3.7),cf(6,3,3.7),cf(6,9,3.7),cf(-5,9,3.7)],(0,.3,0),'cyan',.15)
hatch('Central chest long ceramic keel',cf,[(-12,13),(12,13),(11,39),(7,44),(-7,44),(-11,39)],'white',3)
bay('Central torso narrow sensor',cf,4,9,23,37,'frame',1.2)
for j in range(5):box('Chest sensor grate'+str(j),(6.5,-45+.37*(25+j*2.2)+.8,357-(25+j*2.2)),(3,.25,.5),'cyan',.05)
# Visible small opened missile hatch offset on left top chest, MG launcher nesting.
def mf(x,t,d=0):return (-x,-41+.5*t+d,356-t)
bay('Left chest missile hatch',mf,18,42,1,21,'white',4.5)
for row in range(3):
 for col in range(3):
  p=Vector(mf(22+col*7,5+row*5.5,3.8));rod('Chest red missile nose',p,p+Vector((0,-1.3,0)),1.7,'red',n=20,bevel=.10)
# Pelvis front circular mount, white centre skirt, angular grey side skirts.
group('03 Pelvis and skirts',waist)
loft('Waist armoured bearing housing',[ring8(0,0,270,25,22,19),ring8(0,0,292,24,20,19)],'grey',.4)
bearing('Front waist cable socket',(0,-23,282),(0,1,0),10,5,False)
for j in range(3):box('Abdomen ceramic vertebra '+str(j),(0,-24+j,311-j*7),(21-j*2,7,5),'white',.5)
def pf(x,t,d=0):return (x,-29-.10*t+d,271-t)
hatch('Central elongated skirt',pf,[(-11,0),(11,0),(9,60),(5,66),(-5,66),(-9,60)],'white',3)
hatch('Central skirt inset frame',pf,[(-6,7),(6,7),(5,49),(-5,49)],'panel',.8)
strip('Skirt red central guide',[pf(-2,11,-1),pf(2,11,-1),pf(2,44,-1)],.5,'red',(0,.3,0))
for sign,side in [(-1,'L'),(1,'R')]:
 mi=lambda p:(sign*p[0],p[1],p[2])
 skin('Pelvis diagonal side shield '+side,[mi(p) for p in [(13,-23,274),(39,-16,277),(46,6,269),(47,12,229),(21,-12,219),(14,-30,250)]],[(0,1,5),(1,2,3,4,5)],(-sign*2,2,0),'grey',.3)
 plate('Hip blue flank inset '+side,[mi(p) for p in [(35,-14,266),(40,-8,265),(44,6,235),(37,-2,233)]],(-sign*1.5,1,0),'blue',.3)
 plate('Outer hip skirt white rail '+side,[mi(p) for p in [(42,-5,275),(50,0,272),(56,3,207),(51,-4,201),(44,-11,233)]],(-sign*2,3,0),'white',.3)
 plate('Rear hip skirt '+side,[mi(p) for p in [(6,26,275),(33,28,277),(39,37,219),(20,39,211),(5,28,243)]],(0,-3,0),'white',.35)
# Upper arms, vertical shoulder pods, thin horizontally extended stabiliser fins.
for sign,side in [(-1,'L'),(1,'R')]:
 mi=lambda p:(sign*p[0],p[1],p[2])
 upper=pivot('UPPER_ARM_'+side,mi((65,0,351)));fore=pivot('FOREARM_'+side,mi((70,-2,303)),upper);hand=pivot('HAND_'+side,mi((76,-6,251)),fore)
 group('04 Arms '+side,upper)
 rod('Humerus load rail '+side,mi((64,0,348)),mi((70,-2,306)),6,'frame')
 shell('Upper arm grey enclosure '+side,[ring8(sign*65,0,343,12,13,13),ring8(sign*67,-1,323,12,13,12),ring8(sign*70,-2,311,9,10,9)],2,'grey')
 plate('Upper arm long blue shield '+side,[mi(p) for p in [(58,-16,332),(72,-14,331),(80,-16,306),(74,-18,294),(61,-17,307)]],(0,3,0),'blue',.35)
 # Shoulder front vertical white cassette with rounded rotary cover and bottom intake.
 def sf(x,t,d=0):return mi((x,-20+.08*t+d,376-t))
 poly=[(57,0),(77,0),(81,8),(79,47),(64,52),(56,44)]
 hatch('Shoulder long ceramic cassette '+side,sf,poly,'white',6)
 bay('Shoulder bottom inlet '+side,sf,61,77,35,45,'grey',4)
 for j in range(3):strip('Shoulder inset inlet fin '+side+str(j),[sf(63,38+j*2,2),sf(75,38+j*2,2)],.9,'steel',(0,1,0))
 p=sf(68,25,-.8);rod('Shoulder round access cap '+side,p,Vector(p)+Vector((0,-.7,0)),5,'grey',n=32)
 strip('Shoulder cap diagonal slot '+side,[sf(65,22,-1.65),sf(71,28,-1.65)],.7,'frame',(0,.1,0))
 skin('Shoulder swept side wrap '+side,[mi(p) for p in [(77,-14,375),(88,-2,369),(88,17,350),(77,23,330),(77,-10,329)]],[(0,1,2,3,4)],(-sign*3,0,0),'white',.4)
 # Long thin shoulder wing lies nearly horizontal as on FAZZ, not a large Gundam shoulder spike.
 plate('Shoulder stabiliser fin '+side,[mi(p) for p in [(61,-1,378),(69,-4,382),(124,2,383),(114,3,376),(77,3,371),(61,2,372)]],(0,4,-1.0),'white',.25)
 plate('Shoulder stabiliser black inset '+side,[mi(p) for p in [(108,-1,379),(120,0,381),(114,1,377),(108,1,376)]],(0,.5,0),'frame',.1)
 bearing('Shoulder folding wing pivot '+side,mi((65,-4,376)),(0,1,0),4,6,False)
 stencil('Shoulder caution '+side,sf(69,16,-.7),'CAUTION',1.7)
 group('04 Arms '+side,fore)
 bearing('Elbow double bearing '+side,mi((70,-2,303)),(1,0,0),8,19,False)
 piston('Forearm actuator '+side,mi((73,8,296)),mi((78,6,258)),1.0)
 shell('Forearm closed inner jacket '+side,[ring8(sign*71,-3,295,12,12,12),ring8(sign*75,-5,269,14,13,12),ring8(sign*76,-6,254,11,10,10)],1.5,'grey')
 # White forearm shell wraps a broad inclined front and an outer return.
 skin('Forearm wrapped heavy cover '+side,[mi(p) for p in [(61,-20,292),(78,-19,293),(86,-6,288),(88,4,263),(79,-18,251),(64,-22,253)]],[(0,1,4,5),(1,2,3,4)],(-sign*1.2,2,0),'white',.35)
 def af(x,t,d=0):return mi((x,-22+.04*t+d,288-t))
 hatch('Forearm narrow upper hatch '+side,af,[(65,0),(75,0),(78,23),(68,27),(64,23)],'panel',1)
 for j in [5,19]:strip('Forearm hatch seam '+side+str(j),[af(66,j,-.25),af(75,j,-.25)],.35,'grey',(0,.2,0))
 p=af(71,29,-.6);rod('Forearm slotted service cap '+side,p,Vector(p)+Vector((0,-.4,0)),3.2,'grey',n=24)
 strip('Forearm service diagonal '+side,[af(69,27,-1.1),af(73,31,-1.1)],.5,'frame',(0,.12,0))
 group('05 Mechanical hands '+side,hand)
 box('Palm carrier '+side,mi((76,-6,245)),(16,13,15),'frame',.8)
 for j in range(4):
  x=sign*(70+j*4)
  bearing('Finger knuckle '+side+str(j),(x,-11,244),(1,0,0),2,3,False)
  box('Finger proximal '+side+str(j),(x,-13,240),(3.2,5,7),'steel',.35)
  box('Finger distal '+side+str(j),(x,-10.5,235),(3.1,6,3.5),'frame',.4)
 rod('Thumb proximal '+side,mi((66,-5,247)),mi((66,-13,242)),2.3,'steel',n=16)
 rod('Thumb tip '+side,mi((66,-13,242)),mi((69,-14,238)),2,'frame',n=16)
# Heavy legs: long white outer gaiters, recessed black vents, inner knee mechanisms.
for sign,side in [(-1,'L'),(1,'R')]:
 mi=lambda p:(sign*p[0],p[1],p[2])
 thigh=pivot('THIGH_'+side,mi((23,0,266)),waist);shin=pivot('SHIN_'+side,mi((32,0,205)),thigh);foot=pivot('FOOT_'+side,mi((41,-3,37)),shin)
 group('06 Leg frame '+side,thigh)
 bearing('Hip bearing '+side,mi((23,0,263)),(1,0,0),10,18,False)
 rod('Femur beam '+side,mi((23,0,261)),mi((32,0,207)),6,'frame')
 shell('Thigh underlying shroud '+side,[ring8(sign*24,0,256,14,15,13),ring8(sign*28,0,228,17,16,16),ring8(sign*32,0,212,13,13,13)],2,'grey')
 plate('Thigh blue central facing '+side,[mi(p) for p in [(17,-17,253),(29,-17,255),(37,-19,221),(30,-20,213),(23,-18,226)]],(0,3,0),'blue',.3)
 for j in range(4):box('Thigh vertical guide '+side+str(j),mi((22+j*2.4,-19,237)),(1.1,1,22),'panel',.08)
 group('07 Calf armour '+side,shin)
 bearing('Knee full double hinge '+side,mi((32,1,205)),(1,0,0),10,22,False)
 rod('Shin longitudinal beam '+side,mi((33,1,202)),mi((41,-2,40)),6,'frame')
 piston('Long posterior calf ram '+side,mi((40,14,185)),mi((49,13,52)),2)
 # A broad white outer shell swells through the calf then narrows over the ankle.
 shell('Calf faceted heavy gaiter '+side,[ring8(sign*33,2,202,18,18,20),ring8(sign*36,5,177,24,23,26),ring8(sign*39,5,135,28,27,27),ring8(sign*42,1,79,23,27,23),ring8(sign*42,-2,48,18,23,16)],2.6,'white')
 # Sloped frontal armour follows the leg volume with a deep top knee insert.
 def lf(x,t,d=0):return mi((x,-24-.043*t+d,197-t))
 poly=[(19,0),(42,0),(51,32),(57,70),(54,111),(44,137),(30,139),(20,116),(16,58)]
 hatch('Calf front multi-plane armour '+side,lf,poly,'white',3)
 bay('Calf narrow blue knee cassette '+side,lf,27,39,3,33,'grey',4)
 hatch('Knee blue top insert '+side,lf,[(29,2),(36,2),(38,18),(31,18)],'blue',1)
 for j in range(3):box('Knee inset guide '+side+str(j),lf(29+j*3,26,2),(1.4,2,9),'steel',.15)
 # Contrasting offset black vents set into ceramic, not stripes drawn on it.
 for x,t,ww,hh in [(20,47,12,15),(42,61,13,20),(29,115,16,13)]:
  bay('Calf recessed cooling cassette '+side+str(t),lf,x,x+ww,t,t+hh,'grey',5)
  for j in range(3):strip('Calf cooling inner fin '+side+str(t)+str(j),[lf(x+2,t+3+j*(hh-5)/3,2),lf(x+ww-2,t+3+j*(hh-5)/3,2)],1.0,'steel',(0,1.4,0))
 # White trapezoid service panels conform to front slope, leaving narrow seams.
 hatch('Calf lower long cover '+side,lf,[(24,69),(41,72),(47,102),(42,110),(26,110),(21,101)],'white',1.1)
 strip('Calf cover recessed guide '+side,[lf(25,73,-.35),lf(26,97,-.35),lf(40,104,-.35)],.32,'grey',(0,.18,0))
 p=lf(40,91,-.8);rod('Calf large round service cap '+side,p,Vector(p)+Vector((0,-.7,0)),5,'panel',n=32)
 strip('Calf service slotted screw '+side,[lf(36.6,87.6,-1.7),lf(43.4,94.4,-1.7)],.8,'frame',(0,.15,0))
 stencil('Calf Anaheim logo '+side,lf(36,78,-.7),'AE',4)
 stencil('Calf service text '+side,lf(34,101,-.7),'ANAHEIM / 04',1.35)
 for j in range(3):strip('Calf tiny red warn '+side+str(j),[lf(28,107+j*.6,-.45),lf(33,107+j*.6,-.45)],.24,'red',(0,.1,0))
 for x,t in [(22,37),(49,95),(27,131)]:bolt('Calf flush lock '+side+str(t),lf(x,t,-.6),(0,-1,0),.5,'steel')
 # Outside longitudinal armour fin with a true side plane and rear vent.
 plate('Calf side wrap reinforced rail '+side,[mi(p) for p in [(52,-9,196),(60,2,172),(66,11,99),(58,10,57),(50,-7,78),(52,-14,149)]],(-sign*2,1,0),'white',.35)
 plate('Calf rear long hatch '+side,[mi(p) for p in [(25,28,176),(49,30,176),(56,26,89),(42,22,63),(24,25,88)]],(0,-2,0),'panel',.3)
 for j in range(6):box('Calf rear radiator '+side+str(j),mi((39,31,143-j*5)),(18,3,2),'frame',.2)
 group('08 Feet '+side,foot)
 bearing('Ankle transverse '+side,mi((41,-3,38)),(1,0,0),7,24,False)
 # Broad low pointed FAZZ blue toe, segmented metal sole and ceramic ankle cuff.
 x=sign*43
 loft('Foot blue tapered toe '+side,[[(x-15,-49,5),(x+15,-49,5),(x+17,18,5),(x-17,18,5)],[(x-12,-47,13),(x+12,-47,13),(x+14,14,33),(x-14,14,33)]],'blue',.35)
 box('Foot sole '+side,(x,-14,4),(31,65,5),'frame',.55)
 plate('Foot instep grey armour '+side,[(x-8,-29,20),(x+8,-29,20),(x+10,0,34),(x-10,0,34)],(0,1,-2),'grey',.25)
 for j in range(3):box('Foot traction '+side+str(j),(x,-36+j*14,1.2),(24,7,1.5),'rubber',.15)
 shell('Ankle white encircling collar '+side,[ring8(x,-1,46,17,18,14),ring8(x,-5,30,15,20,13)],2,'panel')
# FAZZ head: separate sagittal crown, recessed face, side acoustic slots and four yellow prongs.
hp=pivot('HEAD_MOUNT',(0,-1,373),torso);group('09 Head shell',hp)
# Head is modelled in global reference units so all side depths are explicit.
outline=[(0,412),(12,410),(23,399),(25,383),(18,372),(0,369),(-18,372),(-25,383),(-23,399),(-12,410)]
sections=[]
for y,scale in [(-7,1),(5,1.03),(18,.88),(24,.63)]:sections.append([(x*scale,y,390+(z-390)*scale) for x,z in outline])
N=len(outline);vs=[p for sec in sections for p in sec];fs=[]
for j in range(3):
 for i in range(N):fs.append((j*N+i,j*N+(i+1)%N,(j+1)*N+(i+1)%N,(j+1)*N+i))
fs.append(tuple(range(3*N,4*N)))
# Sealed inner edge around the open front head enclosure.
inner=[(x*.88,-5,390+(z-390)*.86) for x,z in outline];vs+=inner
for i in range(N):fs.append((i,4*N+i,4*N+(i+1)%N,(i+1)%N))
fs.append(tuple(range(4*N,5*N)))
mesh('Helmet multi-section casting',vs,fs,'grey',.28)
box('Face inner core',(0,-7,386),(30,16,30),'frame',.5)
# Back crown panels and comb vents.
plate('Crown central grey cap',[(-9,2,414),(9,2,414),(10,20,409),(-10,20,409)],(0,-1,-2),'panel',.2)
for j in range(5):box('Occipital radiator '+str(j),(0,24,395-j*2),(18,2,1),'frame',.1)
# Facial mask takes multiple folded front planes instead of one box.
rows=[(393,11,-17),(387,10,-19),(379,8,-20),(373,5.5,-17)]
v=[]
for z,w,y in rows:v += [(-w,y,z),(0,y-2,z+.4),(w,y,z)]
faces=[]
for j in range(3):
 for i in range(2):faces.append((j*3+i,j*3+i+1,(j+1)*3+i+1,(j+1)*3+i))
skin('Face folded white mask',v,faces,(0,3,0),'panel',.12)
for s,side in [(-1,'L'),(1,'R')]:
 mi=lambda p:(s*p[0],p[1],p[2])
 # Eyes sit in a recessed eye tunnel with sloped eyebrow.
 out=[mi(p) for p in [(2,-17,394),(13,-14,398),(14,-14,402),(2,-17,398)]]
 inn=[mi(p) for p in [(3,-17,395),(12,-14.3,398.4),(12.5,-14.3,400.3),(3,-17,397)]]
 recess('Eye socket '+side,out,inn,2,'black')
 plate('Recessed emerald lens '+side,[tuple(Vector(p)+Vector((0,1.6,0))) for p in inn],(0,.35,0),'cyan',.05)
 plate('White angular brow '+side,[mi(p) for p in [(1,-18,399),(13,-15,403),(18,-12,402),(14,-15,399),(2,-18,396)]] ,(0,3,0),'panel',.2)
 # Tall cheek acoustic radiator, protective lip, separate side temple surface.
 def hf(x,t,d=0):return mi((x,-14+.07*t+d,395-t))
 bay('Cheek recessed acoustic slot '+side,hf,12,18,2,19,'panel',2.5)
 for j in range(6):strip('Cheek radiator vane '+side+str(j),[hf(13.5,4+j*2.3,1.2),hf(16.5,4+j*2.3,1.2)],.55,'steel',(0,.7,0))
 plate('Cheek white jaw guard '+side,[mi(p) for p in [(19,-13,395),(22,-7,395),(21,-10,372),(17,-15,371),(17,-16,382)]],(-s*1.2,2,0),'white',.22)
 plate('Temple layered outer cover '+side,[mi(p) for p in [(20,-6,400),(24,2,403),(26,12,395),(24,14,381),(22,0,380)]],(-s*1.3,0,0),'panel',.2)
 strip('Temple diagonal separation '+side,[mi(p) for p in [(23,-1,397),(24,5,394),(23,9,385)]],.25,'frame',(-s*.2,0,0))
 # FAZZ four-prong antenna: long lateral blade plus the taller inner blade.
 for label,p in [('outer',[(8,-16,400),(12,-16,402),(43,-6,423),(39,-7,419),(14,-17,394)]),('inner',[(12,-8,404),(15,-6,406),(20,0,438),(18,-1,433)])]:
  plate('Yellow '+label+' prong '+side,[mi(v) for v in p],(0,1.1,0),'gold',.10)
# Tall blue forehead clamshell slopes into the eye brow, its well has actual depth.
frontpts=[(-10,-16,405),(10,-16,405),(14,-10,424),(8,-8,432),(-8,-8,432),(-14,-10,424)]
skin('Blue forehead cowl',frontpts,[(0,1,2,3,4,5)],(0,5,0),'blue',.22)
def ff(x,t,d=0):return(x,-15+.30*t+d,409+t)
bay('Forehead recessed dark sensor',ff,-5,5,0,15,'blue',2)
plate('Forehead inner sensor glass',[ff(-2,3,1.7),ff(2,3,1.7),ff(2,12,1.7),ff(-2,12,1.7)],(0,.3,0),'navy',.1)
plate('Purple chin wedge',[(-3,-22,379),(3,-22,379),(5,-21,372),(0,-23,369),(-5,-21,372)],(0,3,0),'blue',.15)
for z in [387,383]:strip('Face twin short vent '+str(z),[(-2,-21.1,z),(0,-22,z+.8),(2,-21.1,z)],.45,'frame',(0,.25,0))
# Large full armour rear backpack and the recognisable twin white upper pylons.
bp=pivot('BACKPACK_MOUNT',(0,28,339),torso);group('10 Backpack',bp)
loft('Backpack structural engine body',[ring8(0,42,293,36,14,25),ring8(0,43,347,40,17,25),ring8(0,43,381,29,12,20)],'frame',.5)
for sign,side in [(-1,'L'),(1,'R')]:
 mi=lambda p:(sign*p[0],p[1],p[2])
 shell('White upper missile pylon '+side,[ring8(sign*40,37,364,6,7,7),ring8(sign*41,38,425,6,7,7),ring8(sign*41,38,434,4.8,5.5,5.5)],1.3,'white')
 for j in range(2):box('Pylon red band '+side+str(j),mi((41,30,425+j*2)),(9,.7,.5),'red',.04)
 plate('Backpack tall blue wing '+side,[mi(p) for p in [(27,63,368),(39,63,377),(43,65,333),(37,69,312),(26,62,329)]],(0,-4,0),'blue',.3)
 shell('Backpack side heavy enclosure '+side,[ring8(sign*37,50,356,14,15,16),ring8(sign*39,51,314,16,15,19),ring8(sign*35,55,286,12,13,15)],2,'grey')
 # Twin main nozzles per side: hollow bell, inset throat and centre cone.
 for x,z,rr in [(20,300,9),(35,285,8)]:
  a=mi((x,62,z));b=mi((x,80,z-4));ring('Main vernier bell '+side+str(x),a,b,rr,rr*.74,'steel',40,.13)
  rod('Thruster throat '+side+str(x),a,Vector(a)+Vector((0,2,0)),rr*.65,'black',n=32)
  rod('Thruster inner cone '+side+str(x),Vector(a)+Vector((0,2,0)),Vector(a)+Vector((0,10,-2)),rr*.3,'blue',r2=rr*.15,n=24)
 # Long external propellant cylinders beneath backpack.
 a=mi((49,62,293));b=mi((54,73,98));rod('Long propellant tank '+side,a,b,9,'navy',r2=8,n=40,bevel=.4)
 for frac in [.06,.50,.92]:
  c=Vector(a).lerp(Vector(b),frac);axis=(Vector(b)-Vector(a)).normalized();ring('Tank retaining band '+side+str(frac),c-axis*.9,c+axis*.9,9.25,8.3,'frame',32,.08)
 rod('Tank rounded end '+side,b,Vector(b)+Vector((.3,1,-5)),8,'navy',r2=5,n=32,bevel=.35)
 for j in range(5):box('Backpack radiator rib '+side+str(j),mi((21,70,346-j*4)),(22,4,1.4),'steel',.16)
 plate('Backpack stabiliser extension '+side,[mi(p) for p in [(31,51,293),(43,55,290),(58,92,239),(47,88,242)]],(0,-3,0),'frame',.25)
 stencil('Back unit number '+side,mi((36,72,333)),'04',5,rear=True)
# Left arm double beam rifle unit, barrels descending alongside leg.
group('11 Double beam rifle',bpy.data.objects['FOREARM_R'])
box('Twin rifle mounting saddle',(91,3,275),(14,23,20),'frame',.7)
for j in range(2):
 x=87+j*11
 rod('Twin long beam barrel '+str(j),(x,2,272),(x,-8,140),4.6,'navy',n=32,bevel=.15)
 ring('Twin muzzle lip '+str(j),(x,-8,141),(x,-8.3,137),5,3.3,'steel',32,.09)
 rod('Twin muzzle dark well '+str(j),(x,-7.8,145),(x,-8,142),3.3,'black',n=24)
 for z in [248,208,162]:box('Beam barrel reinforced sleeve'+str(j)+str(z),(x,-1,z),(11,11,4),'frame',.25)
# Hyper mega cannon: rebuilt to canonical long shoulder-supported silhouette.
cp=pivot('HYPER_MEGA_CANNON',(-83,35,411));group('12 Hyper mega cannon',cp)
origin=Vector((-83,35,411));axis=Vector((0,-.72,-.694)).normalized();ux=Vector((1,0,0));uy=axis.cross(ux).normalized()
def gun(x,y,t):return tuple(origin+ux*x+uy*y+axis*t)
def gr(t,w,h):return [gun(-w*.65,-h,t),gun(w*.65,-h,t),gun(w,-h*.60,t),gun(w,h*.65,t),gun(w*.65,h,t),gun(-w*.65,h,t),gun(-w,h*.65,t),gun(-w,-h*.60,t)]
loft('Hyper mega long rear casing',[gr(-20,8,12),gr(0,17,21),gr(38,20,22),gr(88,18,19),gr(118,13,15)],'navy',.6)
loft('Hyper mega main barrel jacket',[gr(110,11,13),gr(153,12,13),gr(175,9,10),gr(268,7,8),gr(309,5.5,7)],'frame',.35)
ring('Hyper mega muzzle actual bore',gun(0,0,306),gun(0,0,315),6.8,4.8,'steel',40,.13)
rod('Cannon black bore floor',gun(0,0,300),gun(0,0,303),4.8,'black',n=32)
# Segmented upper rail and slender yellow tracking strip.
for j,(t,w,h) in enumerate([(132,13,14),(174,10,11),(209,9,10),(251,8,9),(287,7,8)]):
 loft('Cannon collar '+str(j),[gr(t,w,h),gr(t+3,w,h)],'navy',.22)
plate('Cannon white middle saddle',[gun(-13,-14,100),gun(13,-14,100),gun(12,-14,138),gun(-12,-14,138)],tuple(uy*2),'white',.3)
strip('Cannon yellow long rail',[gun(-6,-9,184),gun(-5,-8,279)],1.4,'gold',tuple(uy*.7))
# Cannon side stepped armour, ribs, recessed power chamber with visible machinery.
for sign in [-1,1]:
 xx=sign*20
 plate('Cannon side upper service plate '+str(sign),[gun(xx,-12,10),gun(xx,-12,61),gun(xx,-5,88),gun(xx,13,82),gun(xx,15,21)],tuple(ux*(-sign*1.5)),'frame',.3)
 for j in range(8):
  rod('Cannon exposed side coolant rail '+str(sign)+str(j),gun(sign*18,-9,72+j*3.5),gun(sign*18,9,72+j*3.5),.8,'steel',n=12,bevel=.04)
 for t in [18,49,77]:bolt('Cannon service fastener '+str(sign)+str(t),gun(sign*20.5,0,t),(sign,0,0),.75,'steel')
 # Side circular energy chamber with locking blocks.
 ring('Cannon energy chamber lip '+str(sign),gun(sign*16,0,102),gun(sign*24,0,102),12,9,'steel',40,.15)
 rod('Cannon energy chamber dark core '+str(sign),gun(sign*20,0,102),gun(sign*23,0,102),8.5,'black',n=32)
 for j in range(6):
  a=j*math.tau/6;bolt('Cannon flange lock '+str(sign)+str(j),gun(sign*24.2,math.sin(a)*10,102+math.cos(a)*10),(sign,0,0),.65,'titanium')
# Shoulder support is a real two-axis yoke connected to torso and cannon.
group('13 Cannon support and cable',torso)
bearing('Cannon shoulder load gimbal',(-58,14,357),(1,0,0),10,20,False)
piston('Cannon shoulder support cylinder',(-56,18,358),(-82,17,390),2.8)
hull('Cannon shoulder saddle',[(-70,13,385),(-92,13,385),(-93,39,397),(-71,39,397),(-71,18,399),(-92,18,399)],'frame',.5)
hose('Cannon external power feed',[(-10,23,280),(-40,28,279),(-62,26,295),(-89,14,322),(-99,-2,338),(-96,-23,347),(-91,-37,350)],3.0)
for i,p in enumerate([(-10,23,280),(-91,-37,350)]):bearing('Cable end connector'+str(i),p,(0,1,0),4.3,5,False)
# Sparse clean industrial labels, powered components and a neutral studio.
group('14 Markings',root)
stencil('Waist small serial',(0,-36,260),'FA-010A',2)
# Embed references for review within editable source.
for path in (ROOT/'references').glob('*.png'):
 im=bpy.data.images.load(str(path),check_existing=True);im.pack()
SC['reference_method']='MG FAZZ Ver.Ka reference surfaces; VALKYR V3 geometric construction library reused, no FAZZ skill';SC['limits']='Static study, inferred hidden dimensions; not a rigged production asset'
stage=bpy.data.collections.new('STUDIO');SC.collection.children.link(stage)
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.03));floor=bpy.context.object;floor.name='Studio ground';floor.data.materials.append(M['frame'])
for c in list(floor.users_collection):c.objects.unlink(floor)
stage.objects.link(floor)
world=bpy.data.worlds.new('Studio world');SC.world=world;world.use_nodes=True;world.node_tree.nodes.get('Background').inputs[0].default_value=(.10,.12,.16,1);world.node_tree.nodes.get('Background').inputs[1].default_value=.45
for n,loc,power,size,col in [('Key',(-5,-7,9),1700,5,(.89,.94,1)),('Fill',(5,-3,5),1000,4,(1,.93,.84)),('Rim',(2,5,8),2200,3,(.84,.91,1))]:
 d=bpy.data.lights.new(n,'AREA');d.energy=power;d.shape='DISK';d.size=size;d.color=col;o=bpy.data.objects.new(n,d);stage.objects.link(o);o.location=loc;o.rotation_euler=(Vector((0,0,2.3))-o.location).to_track_quat('-Z','Y').to_euler()
for n,loc,aim,scale in [('hero',(7,-12,7),(0,-.45,2.2),5.8),('front',(0,-12,3.9),(0,-.35,2.2),5.7),('rear',(7,11,7),(0,.15,2.2),5.5),('head',(2,-7,4.75),(0,-.07,3.99),1.15),('chest',(3,-8,5),(0,-.1,3.34),2.3)]:
 d=bpy.data.cameras.new(n);d.type='ORTHO';d.ortho_scale=scale;o=bpy.data.objects.new('CAM_'+n,d);stage.objects.link(o);o.location=loc;o.rotation_euler=(Vector(aim)-o.location).to_track_quat('-Z','Y').to_euler()
SC.camera=bpy.data.objects['CAM_hero'];SC.render.engine='CYCLES';SC.cycles.samples=32;SC.cycles.use_denoising=True;SC.render.resolution_x=1400;SC.render.resolution_y=1600;SC.render.resolution_percentage=100;SC.view_settings.view_transform='AgX';SC.view_settings.look='AgX - Medium High Contrast'
bpy.context.view_layer.update();bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'FAZZ_V3_METHOD.blend'))
print('ASSEMBLY SAVED',len(PARTS),'mesh parts')
