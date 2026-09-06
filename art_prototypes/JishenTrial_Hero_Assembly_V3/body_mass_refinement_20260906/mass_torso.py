from mass_ops import *
group('01 Chest and abdomen','Thorax')
remove(['V3B_Pectoral_','V3B_Gold_Intake_','V3B_Intake_Inset_','V3B_Lower_Intake_','V3B_Clavicle_Facing','V3B_Ceramic_Oblique_Rib_','V3B_Chest_Warning_Tick','V3B_Sloping_Clavicle_Shell','V3B_Clavicle_Trim','V3B_Clavicle_Armor_Pin'])
for s,side in ((1,'L'),(-1,'R')):
 def mi(p):return (s*p[0],p[1],p[2])
 # The intake and armor follow one raked chest plane, with physical walls and a deep floor.
 def face(x,t,dep=0):return mi((15+x,-46+.31*x+.42*t+dep,333+.36*x-t))
 vv=[mi(p) for p in ((13,-15,344),(27,-9,346),(44,-5,340),(42,-37,344),(16,-46,333),(13,-30,340),(28,20,342),(44,16,333))]
 skin('Pectoral_Mass_Clavicle_Armored_Hood.'+side,vv,[(0,1,4,5),(1,3,4),(1,2,3),(1,6,7,2)],(0,2,-1.0),'blue',.32)
 outer=[face(x,t) for x,t in ((2,0),(26,0),(29,4),(25,22),(4,27),(0,20))]
 inner=[face(x,t,-.12) for x,t in ((4,6),(24,6),(25,8),(24,15),(4,15),(3,13))]
 recess('Pectoral_Mass_Raked_Intake.'+side,outer,inner,7.0,'navy')
 lip=[face(4,15,-.30),face(24,15,-.30)]
 strip('Pectoral_Mass_Gold_Lower_Lip.'+side,lip,1.25,'gold',(0,.55,0))
 strip('Pectoral_Mass_Gold_Upper_Lip.'+side,[face(4,6,-.3),face(24,6,-.3)],.85,'gold',(0,.55,0))
 for j in range(3):
  strip('Pectoral_Mass_Recessed_Vane_%d.'%j+side,[face(4,8+j*2,2.1),face(24,8+j*2,2.1)],.75,'titanium',(0,1.6,0))
 # Continuous upper armor lip and an angled lower clasp break the old rectangular panel.
 plate('Pectoral_Mass_Upper_Fold.'+side,[face(x,t,-.65) for x,t in ((2,0),(26,0),(28,4),(24,4),(4,4),(1,2))],(0,3,1),'blue',.28)
 plate('Pectoral_Mass_Lower_Blue_Beak.'+side,[face(x,t,-.7) for x,t in ((4,17),(24,17),(25,21),(13,26),(5,25))],(0,2,0),'blue',.25)
 plate('Pectoral_Mass_Lower_Recess.'+side,[face(x,t,-.92) for x,t in ((7,18.5),(21,18.5),(20,21.2),(8,22))],(0,.45,0),'black',.1)
 for j in range(2):strip('Pectoral_Mass_Lower_Vane_%d.'%j+side,[face(8,19+j*1.4,-1.05),face(20,19+j*1.4,-1.05)],.45,'steel')
 strip('Pectoral_Mass_Edge_Return.'+side,[face(28,5,-.9),face(24,22,-.9),face(13,26,-.9)],.55,'edge')
 # Wrap from the front cassette into the side rib cage; front and side share load ribs.
 hull('Pectoral_Mass_Wrapped_Rear_Cage.'+side,[mi(p) for p in ((17,-25,333),(40,-19,337),(45,1,330),(36,24,324),(26,21,302),(17,-18,305),(35,-21,312),(39,12,310))],'frame',.45)
 plate('Pectoral_Mass_Outer_Wrapped_Armor.'+side,[mi(p) for p in ((43,-24,334),(47,-7,333),(40,17,321),(32,18,306),(33,-6,308),(41,-25,318))],(-s*3,1,0),'blue',.35)
 plate('Pectoral_Mass_Side_Fold.'+side,[mi(p) for p in ((43,-5,329),(41,9,325),(35,14,315),(35,1,316))],(-s*1.4,0,0),'panel',.2)
 for j in range(3):
  z=309-j*9;xx=24-j*2.0
  plate('Mass_Interlocking_Ceramic_Rib_%d.'%j+side,[mi(p) for p in ((xx,-31+j*2,z),(xx+6,-22+j*2,z+3),(xx+6,-8,z-2),(xx+1,-7,z-9),(xx-3,-26+j*2,z-7))],(-s*.4,2.5,0),'white',.28)
 for x,t in ((3,3),(24,19)):
  bolt('Pectoral_Mass_Captive_Lock_%d.'%t+side,face(x,t,-1),(0,-1,0),.62,'steel')
 group('02 Waist and pelvis','Waist')
 ram('Mass_Waist_Lateral_Actuator.'+side,mi((26,7,302)),mi((22,10,274)),2.0)
 hose('Mass_Waist_Hydraulic_Return.'+side,[mi(p) for p in ((28,9,297),(29,14,291),(26,17,278),(24,12,272))],.85)
 group('01 Chest and abdomen','Thorax')

group('02 Waist and pelvis','Waist')
remove(['V3B_Abdominal_Main_Ceramic','V3B_Abdominal_Center_Facet','V3B_Abdominal_Fastener'])
loft('Mass_Abdominal_Closed_Spine',[oct_ring(273,10,-25,18),oct_ring(286,12,-29,20),oct_ring(301,13,-30,20)],'frame',.5)
for j,(z,w,y) in enumerate(((300,9.5,-36),(290,8.4,-35),(280,7,-33))):
 vv=[(-w,y+2,z),(0,y-1,z+1),(w,y+2,z),(w*.82,y+3,z-6),(0,y+.4,z-8),(-w*.82,y+3,z-6)]
 skin('Mass_Abdominal_Ceramic_Vertebra_%d'%j,vv,[(0,1,4,5),(1,2,3,4)],(0,3,0),'white',.23)
 plate('Mass_Abdominal_Machined_Key_%d'%j,[(-w*.34,y-.3,z-.3),(w*.34,y-.3,z-.3),(w*.27,y+.1,z-4),(-w*.27,y+.1,z-4)],(0,.5,0),'silver',.12)
 for ss in (-1,1):
  strip('Mass_Abdominal_Seal_%d_%d'%(j,ss),[(ss*w*.83,y+2,z-6),(0,y+.4,z-8)],.55,'rubber')

# Load-bearing shoulder return and cheek guards nest behind the retained badge faces.
for s,side in ((1,'L'),(-1,'R')):
 group('03 Swept shoulder armor','Shoulder_Armor_Floating_Pivot.'+side)
 f=lambda p:(s*p[0],p[1],p[2])
 hull('Mass_Shoulder_Armored_Suspension_Fork.'+side,[f(p) for p in ((51,-5,346),(63,-5,354),(82,6,362),(85,12,351),(68,15,328),(55,8,331))],'frame',.6)
 plate('Mass_Shoulder_Rear_Armor_Return.'+side,[f(p) for p in ((73,17,353),(85,18,357),(88,18,347),(78,20,332),(69,17,331))],(0,-3,0),'blue',.3)
