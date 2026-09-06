from body_common import *
group('08 Backpack spine and twin thrusters','Backpack_Structural_Mount')
box('Backpack_Upper_Crossmember',(0,21,349),(53,8,7),'frame',.4)
box('Backpack_Lower_Crossmember',(0,21,303),(45,8,6),'frame',.4)
hull('Backpack_Central_Spine_Chassis',[(-9,21,372),(9,21,372),(-11,31,375),(11,31,375),(-10,42,364),(10,42,364),(-8,43,288),(8,43,288),(-8,24,282),(8,24,282)],'frame',.3)
# Chamfered central blue equipment spine, split around an actual luminous service well.
for s,side in ((1,'L'),(-1,'R')):
 mi=lambda p:mirror(p,s)
 skin('Backpack_Spine_Side_Rail.'+side,mi([(7,42,372),(10,35,376),(13,31,368),(12,37,297),(9,43,286),(7,44,299)]),[(0,1,2),(0,2,3,5),(3,4,5)],(-s*2,-1,0),'blue',.25)
 plate('Spine_Silver_Side_Return.'+side,mi([(11,34,366),(13,28,362),(13,29,308),(11,37,301)]),(-s*1,-1,0),'silver',.18)
plate('Backpack_Top_Crown',[(-7,43,371),(7,43,371),(8,43,363),(-8,43,363)],(0,-3,0),'blue',.22)
plate('Backpack_Upper_Service_Panel',[(-7,43.5,359),(7,43.5,359),(7,44,339),(-7,44,339)],(0,-3,0),'panel',.2)
plate('Backpack_Upper_Service_Recess',[(-3.5,43.7,355),(3.5,43.7,355),(3.5,43.7,352),(-3.5,43.7,352)],(0,-.8,0),'black',.1)
outer=[(-8.5,44.5,335),(8.5,44.5,335),(9,45,300),(-9,45,300)]
inner=[(-5.7,44.6,332),(5.7,44.6,332),(5.7,45.1,303),(-5.7,45.1,303)]
recess('Backpack_Central_Optical_Well',outer,inner,-4,'blue')
plate('Backpack_Central_Cyan_Window',[(-2,41.8,329),(2,41.8,329),(2,42.2,307),(-2,42.2,307)],(0,-.7,0),'cyan',.35)
plate('Backpack_Central_Cyan_Filament',[(-.7,42,327),(.7,42,327),(.7,42.4,309),(-.7,42.4,309)],(0,-.2,0),'core',.2)
plate('Spine_Lower_Titanium_Latch',[(-5,40,294),(5,40,294),(4.4,40,282),(-4.4,40,282)],(0,-3,0),'titanium',.28)
bolt('Spine_Lower_Latch_Bolt',(0,40.4,288),(0,1,0),.75,'gold')
for z in (345,361):
 for s in (-1,1):bolt('Backpack_Spine_Pin_%s_%s'%(s,z),(s*6,44,z),(0,1,0),.5)
for s,side in ((1,'L'),(-1,'R')):
 mi=lambda p:mirror(p,s)
 # A real pair of independently shrouded vector thrusters, with dark open throats.
 top=Vector((s*21,32,342));bot=Vector((s*27,41,274));d=(bot-top).normalized()
 rod('Thruster_Core_Housing.'+side,top,top+(bot-top)*.79,5.7,'frame',r2=4.6,n=32)
 ring('Thruster_Upper_Gimbal.'+side,top-d*4,top+d*1,6.5,4.6,'gold',32)
 for j in (-1,1):piston('Thruster_Vector_Link_%d.'%j+side,(s*(16+j*3),24,340),(s*(24+j*3),32,297),1.1)
 v=[(15,35,338),(21,43,341),(28,35,335),(30,40,298),(27,45,289),(22,47,289),(17,40,300),(21,47,320)]
 skin('Thruster_Blue_Folded_Cowling.'+side,mi(v),[(0,1,7,6),(1,2,3,7),(3,4,5,7),(5,6,7)],(-s*.5,-2,0),'blue',.25)
 plate('Thruster_Upper_Blue_Insert.'+side,mi([(18,44.3,333),(22,46.3,335),(24,46.3,320),(20,47.3,318)]),(0,-1.2,0),'panel',.18)
 plate('Thruster_Cowling_Seam.'+side,mi([(19,46,316),(24,46,317),(26,46,300),(23,47,295),(20,46,299)]),(0,-1.2,0),'blue',.15)
 # Tapered white heat shield over the nozzle; the center remains an open dark exhaust.
 v=[(22,47,297),(26,47,297),(29,43,289),(29,43,276),(26,44,269),(23,44,278)]
 plate('Thruster_White_Heat_Shield.'+side,mi(v),(0,-2,0),'white',.2)
 strip('Thruster_Gold_Heat_Shield_Trim.'+side,mi([(23,47.3,293),(25,46.3,280)]),.7,'gold',(0,-.5,0))
 throat=bot-d*2
 ring('Thruster_Exhaust_Lip.'+side,throat,bot+d*1.2,4.7,3.35,'titanium',32)
 ring('Thruster_Deep_Exhaust_Throat.'+side,bot-d*9,bot,3.7,2.95,'black',32,.03)
 rod('Thruster_Deep_Recess.'+side,bot-d*10,bot-d*9.7,2.95,'black',n=32)
 rod('Thruster_Pilot_Optic.'+side,bot-d*9.5,bot-d*9.3,1.2,'cyan',n=24,bevel=.03)
 for z in (330,308):bolt('Thruster_Cowling_Pin_%s.'%z+side,(s*21,47.4,z),(0,1,0),.5)
 group('08 Backpack spine and twin thrusters','Backpack_Main_Deploy_Hinge.'+side)
 bearing('Backpack_Fin_Main_Bearing.'+side,(s*25,29,351),(0,1,0),5,7)
 # Inner tall spine fin and outer diagonal wing share visible hinged, keyed bases.
 v=[(22,30,351),(26,32,378),(34,36,407),(32,39,367),(28,38,346)]
 skin('Backpack_Inner_Ascending_Fin.'+side,mi(v),[(0,1,3,4),(1,2,3)],(-s*1,-1,0),'blue',.15)
 plate('Backpack_Inner_Fin_Gold_Edge.'+side,mi([(27,32.3,378),(33,36.3,403),(30,35.6,375),(28,34,364)]),(-s*.5,-.5,0),'gold',.1)
 hull('Backpack_Inner_Fin_Base_Cap.'+side,mi([(22,29,358),(29,31,362),(31,37,357),(29,39,344),(23,36,342)]),'blue',.25)
 v=[(30,34,350),(38,34,362),(66,40,390),(49,45,356),(38,44,338)]
 skin('Backpack_Outer_Ascending_Wing.'+side,mi(v),[(0,1,3,4),(1,2,3)],(-s*1,-1,0),'blue',.17)
 strip('Backpack_Outer_Fin_Silver_Leading_Edge.'+side,mi([(39,34.3,363),(66,40.3,390)]),.5,'silver',(0,-.3,0))
 plate('Backpack_Outer_Fin_Gold_Inlay.'+side,mi([(36,41,350),(50,42,369),(46,43,354),(37,44,342)]),(0,-1,0),'gold',.1)
 plate('Backpack_Outer_Fin_Base_Leaf.'+side,mi([(36,42,344),(49,43,353),(45,47,332),(36,44,321),(33,42,332)]),(0,-2,0),'blue',.2)
 group('08 Backpack spine and twin thrusters','Backpack_Outer_Fold_Pivot.'+side)
 piston('Backpack_Deploy_Piston.'+side,(s*30,24,337),(s*43,41,299),1.5)
 v=[(32,32,335),(41,37,335),(55,50,290),(66,60,224),(59,56,240),(42,42,294)]
 skin('Backpack_Long_Descending_Vane.'+side,mi(v),[(0,1,2,5),(2,3,4,5)],(-s*2,-1,0),'blue',.2)
 plate('Backpack_Descending_White_Tip.'+side,mi([(52,48,282),(59,53,272),(67,61,221),(61,57,240)]),(-s*.8,-.5,0),'white',.17)
 strip('Backpack_Descending_Gold_Seam.'+side,mi([(42,40,323),(51,47,289),(56,53,254)]),.7,'gold',(0,-.5,0))
 plate('Backpack_Secondary_Descending_Blade.'+side,mi([(36,28,319),(43,32,313),(50,43,262),(47,44,252),(40,33,288)]),(-s*1.5,-1,0),'navy',.17)
 for j in range(3):
  z=321-j*14
  rod('Backpack_Fin_Root_Rib_%d.'%j+side,(s*34,25,z),(s*39,31,z-4),1.8,'titanium',n=16)

group('09 Shoulder cannon','Cannon_Right_Cradle_Mount')
box('Cannon_Backpack_Anchor',(-30,22,348),(16,13,13),'frame',.5)
for s in (-1,1):
 plate('Cannon_Cradle_Load_Fork_'+str(s),[(-37+s*6,20,350),(-37+s*6,6,355),(-37+s*6,6,369),(-37+s*6,21,364)],(s*2,0,0),'titanium',.3)
 piston('Cannon_Cradle_Elevation_Ram_'+str(s),(-37+s*7,26,349),(-37+s*7,7,362),1.5)
group('09 Shoulder cannon','Cannon_Pitch_Trunnion')
bearing('Cannon_Elevation_Trunnion',(-37,8,367),(1,0,0),5.5,21)
c=Vector((-37,8,367));d=Vector((-.52,-.72,.46)).normalized();u=Vector((.81,-.585,0)).normalized();v=d.cross(u).normalized()
def cp(p):return tuple(c+u*p[0]+v*p[1]+d*p[2])
def cb(name,center,dims,mat='frame',bevel=.2):
 return hull(name,[cp((center[0]+x*dims[0]/2,center[1]+y*dims[1]/2,center[2]+z*dims[2]/2)) for x in(-1,1) for y in(-1,1) for z in(-1,1)],mat,bevel)
cb('Cannon_Rectangular_Receiver',(0,3,-1),(14,13,31),'frame',.65)
cb('Cannon_Rear_Breech_Lock',(0,3,-18),(11,10,4),'titanium',.3)
cb('Cannon_Upper_Receiver_Armor',(0,10,-2),(12,2,26),'silver',.3)
cb('Cannon_Receiver_Top_Channel',(0,11.2,-2),(6,1,18),'black',.16)
cb('Cannon_Receiver_Top_Rib',(0,11.8,-2),(2.5,.8,17),'steel',.1)
for s in (-1,1):
 cb('Cannon_Receiver_Side_Plate_'+str(s),(s*7.1,3,0),(2,9,24),'steel',.32)
 cb('Cannon_Receiver_Inset_'+str(s),(s*8.3,3,-3),(.4,4,13),'black',.12)
 for j in range(3):cb('Cannon_Side_Cooling_%s_%s'%(s,j),(s*8.65,3,-7+j*4),(.5,3,1.3),'titanium',.06)
 rod('Cannon_Recoil_Rod_'+str(s),cp((s*5.3,5,10)),cp((s*5.3,5,38)),.85,'titanium',n=20)
 rod('Cannon_Recoil_Sleeve_'+str(s),cp((s*5.3,5,10)),cp((s*5.3,5,23)),1.35,'frame',n=20)
 for t in (-11,9):bolt('Cannon_Receiver_Pin_%s_%s'%(s,t),cp((s*8.5,6,t)),u*s,.55,'gold')
 # Rectangular bright blue ranging optic inset at the receiver's front shoulder.
 cb('Cannon_Optic_Seat_'+str(s),(s*8.9,4,11),(1.1,7,5.2),'black',.4)
 cb('Cannon_Blue_Optic_'+str(s),(s*9.55,4,11),(.5,4.4,2.5),'cyan',.25)
 cb('Cannon_Optic_Filament_'+str(s),(s*9.85,4,11),(.15,3,1.2),'core',.1)
rod('Cannon_Black_Barrel',cp((0,3,14)),cp((0,3,64)),5.7,'frame',n=48)
for t in (16,25,43,54):ring('Cannon_Reinforcing_Ring_'+str(t),cp((0,3,t)),cp((0,3,t+1.6)),6.25,5.3,'titanium' if t in(16,54) else 'steel',48)
for j in range(6):
 angle=math.tau*j/6;vv=[]
 for t in (27,41):
  for rr,aa in ((6.2,angle-.38),(6.5,angle),(6.2,angle+.38)):
   vv.append(cp((math.cos(aa)*rr,3+math.sin(aa)*rr,t)))
 skin('Cannon_Six_Facet_Barrel_Shroud_'+str(j),vv,[(0,1,4,3),(1,2,5,4)],tuple(-v*.55),'silver' if j in(1,2) else 'steel',.18)
ring('Cannon_Muzzle_Outer_Shroud',cp((0,3,60)),cp((0,3,69)),7.1,5.65,'steel',48,.17)
ring('Cannon_Muzzle_Machined_Lip',cp((0,3,68.3)),cp((0,3,70)),7.15,5.7,'titanium',48,.09)
ring('Cannon_Deep_Open_Bore',cp((0,3,46)),cp((0,3,69.9)),5.6,5.0,'black',48,.04)
rod('Cannon_Bore_Dark_End',cp((0,3,44)),cp((0,3,44.2)),5.0,'black',n=48,bevel=.03)
for j in range(12):
 a=math.tau*j/12
 rod('Cannon_Recessed_Bore_Rifling_%02d'%j,cp((4.9*math.cos(a),3+4.9*math.sin(a),47)),cp((4.9*math.cos(a+.07),3+4.9*math.sin(a+.07),64)),.18,'steel',n=8,bevel=.02)

# Reuse the existing weapon root, reparented to the actual hand grip socket.
wr=bpy.data.objects['AntiShip_Blade_Display_Root'];wr.parent=bpy.data.objects['V3B_Sword_Grip_Socket'];wr.matrix_world=Matrix.Identity(4);wr['Beam_On']=True;wr['attachment']='Right hand / fitted cylindrical grip';wr['weapon_type']='Anti-ship beam sword, dual luminous edges over physical blade'
group('10 Anti-ship beam sword',wr)
gc=Vector(SC['sword_grip_center_units']);a=Vector(SC['sword_grip_axis']).normalized();u=Vector((.9536,0,.301)).normalized();n=a.cross(u).normalized()
def wp(p):return tuple(gc+u*p[0]+n*p[1]+a*p[2])
def wb(name,center,dims,mat='frame',bevel=.2):return hull(name,[wp((center[0]+x*dims[0]/2,center[1]+y*dims[1]/2,center[2]+z*dims[2]/2)) for x in(-1,1) for y in(-1,1) for z in(-1,1)],mat,bevel)
def wplate(name,pts,thick=.8,mat='blue',bev=.16):return plate(name,[wp(p) for p in pts],tuple(-n*thick),mat,bev)
rod('Sword_Grip_Continuous_Core',wp((0,0,-17)),wp((0,0,17)),2.7,'frame',n=32)
for j in range(8):
 t=-14+j*3.7
 ring('Sword_Grip_Rib_%02d'%j,wp((0,0,t)),wp((0,0,t+2.7)),3.0,2.45,'rubber',16,.15)
 wb('Sword_Grip_Inset_%02d'%j,(0,2.93,t+1.25),(2.1,.55,1.8),'steel',.12)
 bolt('Sword_Grip_Pin_%02d'%j,wp((0,3.3,t+1.25)),n,.3)
ring('Sword_Pommel_Silver_Collar',wp((0,0,-19)),wp((0,0,-16)),3.7,2.4,'titanium',16,.18)
rod('Sword_Pommel_Closure',wp((0,0,-19.4)),wp((0,0,-19)),3.3,'frame',n=16)
wb('Sword_Pommel_Gold_Key',(0,3.1,-18),(1.4,.6,1.0),'gold',.08)
ring('Sword_Grip_Guard_Collar',wp((0,0,13)),wp((0,0,17)),4.2,2.45,'titanium',16)
octo=[(-6,-10),(6,-10),(12,-4),(12,4),(6,11),(-6,11),(-12,4),(-12,-4)]
vs=[wp((x,y,t+23)) for y in(-4,4) for x,t in octo]
fs=[tuple(range(7,-1,-1)),tuple(range(8,16))]+[(i,(i+1)%8,(i+1)%8+8,i+8) for i in range(8)]
mesh('Sword_Octagonal_Emitter_Chassis',vs,fs,'frame',.5)
for side in (-1,1):
 nn=n*side
 ring('Sword_Circular_Emitter_Race_'+str(side),wp((0,side*4.1,23)),wp((0,side*5.1,23)),8.8,6.6,'titanium',48,.15)
 rod('Sword_Emitter_Dark_Core_'+str(side),wp((0,side*4.6,23)),wp((0,side*5.35,23)),6.5,'black',n=48,bevel=.15)
 rod('Sword_Cyan_Emitter_Lens_'+str(side),wp((0,side*5.45,23)),wp((0,side*5.65,23)),4.8,'cyan',n=48,bevel=.2)
 rod('Sword_Emitter_Luminous_Center_'+str(side),wp((0,side*5.68,23)),wp((0,side*5.78,23)),3.5,'core',n=48,bevel=.12)
 # Broad guard cover masks half the emitter, retaining the C-shaped luminous aperture.
 pts=[(-6,side*6.0,17),(-1.5,side*6.0,17),(-1.5,side*6.0,29),(-6,side*6.0,30),(-10,side*5.0,25),(-10,side*5.0,21)]
 wplate('Sword_Emitter_White_Guard_'+str(side),pts,side*1.3,'white',.3)
 wb('Sword_Emitter_Front_Lock_'+str(side),(-3,side*6.4,24),(4,1.2,5),'steel',.25)
 for j,(x,t) in enumerate(((0,12),(10,22),(0,35),(-10,22))):bolt('Sword_Emitter_Bolt_%s_%s'%(side,j),wp((x,side*4.8,t)),nn,.75,'gold' if j==0 else 'titanium')
 wb('Sword_Root_Armor_Block_'+str(side),(0,side*2.8,38),(11,2.2,12),'navy',.4)
# Diamond-section physical blade with a dark inset machinery spine and silver sharpened edges.
stations=[(35,7.7),(58,7.3),(124,6.7),(180,5.0),(205,2.3),(216,.08)]
vs=[]
for t,w in stations:vs.extend([wp((-w,0,t)),wp((-w*.50,1.35,t)),wp((w*.50,1.35,t)),wp((w,0,t)),wp((w*.50,-1.35,t)),wp((-w*.50,-1.35,t))])
fs=[tuple(range(5,-1,-1)),tuple(range(30,36))]
for j in range(len(stations)-1):
 for i in range(6):fs.append((j*6+i,j*6+(i+1)%6,(j+1)*6+(i+1)%6,(j+1)*6+i))
mesh('Sword_Continuous_Physical_Diamond_Blade',vs,fs,'silver',.08)
for side in (-1,1):
 wplate('Sword_Dark_Machinery_Spine_'+str(side),[(-3.7,side*1.5,41),(3.7,side*1.5,41),(3.3,side*1.5,165),(1.6,side*1.5,182),(0,side*.6,200),(-1.6,side*1.5,182),(-3.3,side*1.5,165)],side*.7,'frame',.13)
 for j,(t,le) in enumerate(((48,15),(68,21),(95,28),(128,25),(157,17))):
  wb('Sword_Spine_Cartridge_%s_%s'%(side,j),(0,side*2.25,t),(5.6,1.5,le),'steel' if j in(0,2) else 'frame',.2)
  if j<3:
   wb('Sword_Gold_Conductor_%s_%s'%(side,j),(1.8,side*3.1,t),(1.0,.4,le*.55),'gold',.05)
   for off in (-le*.35,le*.35):bolt('Sword_Cartridge_Pin_%s_%s_%s'%(side,j,off),wp((-1.5,side*3.2,t+off)),n*side,.4)
 for x in (-3,3):wplate('Sword_Edge_Rail_%s_%s'%(side,x),[(x,side*1.7,88),(x+.4,side*1.7,88),(x*.75+.4,side*1.7,160),(x*.75,side*1.7,160)],side*.25,'titanium',.04)
# Two thin cyan energy edges wrap the same physical blade; both can be switched off.
for sign in (-1,1):
 beampts=[(sign*(w+1.0),0,t) for t,w in stations]
 for j in range(len(beampts)-1):
  aa=Vector(beampts[j]);bb=Vector(beampts[j+1]);direction=(bb-aa).normalized();perp=Vector((direction.z,0,-direction.x))*sign
  for mat,width,dep in (('beam',1.2,.55),('beam_core',.30,.60)):
   pts=[wp(aa+perp*width/2+Vector((0,dep,0))),wp(bb+perp*width/2+Vector((0,dep,0))),wp(bb-perp*width/2+Vector((0,dep,0))),wp(aa-perp*width/2+Vector((0,dep,0)))]
   ob=plate('Sword_%s_Edge_%s_%02d'%(mat,sign,j),pts,tuple(-n*dep*2),mat,.03);ob['beam_component']=True
   for path in ('hide_render','hide_viewport'):
    dr=ob.driver_add(path).driver;dr.expression='not beam';va=dr.variables.new();va.name='beam';va.type='SINGLE_PROP';va.targets[0].id=wr;va.targets[0].data_path='["Beam_On"]'
wr['blade_tip_world_m']=list(U(wp((0,0,216))))
