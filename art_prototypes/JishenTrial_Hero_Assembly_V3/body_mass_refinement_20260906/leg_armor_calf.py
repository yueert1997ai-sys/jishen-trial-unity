from mass_ops import *
remove(['V3B_Calf_','V3B_Shin_','V3B_Mass_Calf_','V3B_Mass_Shin_'])
for s,side in ((1,'L'),(-1,'R')):
 group('06 Tapered shin and calf','Shin.'+side);f=joint_frame('Shin','Foot',side)
 loft('Calf_Mass2_Continuous_Internal_Cassette.'+side,[oct_ring(12,9,-9,12),oct_ring(28,15,-16,23),oct_ring(55,17,-18,26),oct_ring(77,15,-18,23),oct_ring(100,12,-16,18),oct_ring(121,8,-9,11)],'frame',.6,f)
 def sector(t,inn,out,fy,by,ss):
  return [(ss*inn,fy,t),(ss*(out-5),fy-1,t),(ss*out,fy*.18,t),(ss*(out+1),by*.56,t),(ss*(out-3),by,t),(ss*inn,by*.80,t)]
 # Broad swept nacelle, then a separate overlapping lower shell, both solid armor volumes.
 loft('Calf_Mass2_Upper_Outer_Armored_Nacelle.'+side,[sector(t,inn,out,fy,by,1) for t,inn,out,fy,by in ((14,9,18,-5,11),(30,10,27,-12,22),(46,11,26,-10,27),(59,11,19,-4,22))],'blue',.42,f)
 loft('Calf_Mass2_Lower_Outer_Overlapping_Shell.'+side,[sector(t,inn,out,fy,by,1) for t,inn,out,fy,by in ((46,10,25.2,-12,27.8),(63,10,23,-10,26),(78,9,18,-5,21),(86,9,14,-2,16))],'navy',.4,f)
 loft('Calf_Mass2_Inner_Armored_Return.'+side,[sector(t,inn,out,fy,by,-1) for t,inn,out,fy,by in ((26,8,15,-10,15),(44,8,19,-15,21),(61,8,18,-12,22),(80,8,13,-5,16))],'navy',.36,f)
 # Raised diamond facets create long converging lines, rather than a single flat wedge.
 local_plate('Calf_Mass2_Upper_Outer_Swept_Facet.'+side,[(25,-6,24),(29,5,30),(29,13,42),(26,20,48),(25,5,43),(21,-6,33)],f,(-2,0,0),'panel',.23)
 local_plate('Calf_Mass2_Lower_Outer_Blue_Shingle.'+side,[(24,-6,48),(27,7,48),(25.8,17,60),(20,23,70),(17,13,76),(20,-4,63)],f,(-2,0,0),'blue',.28)
 strip('Calf_Mass2_Outer_Interlayer_Machined_Seam.'+side,[f(p) for p in ((27,2,45),(28,13,47),(24,24,53))],.8,'steel')
 # Recessed heat exchanger on the visible outer face, surrounded by a thick blue rim.
 local_plate('Calf_Mass2_Outer_Cooling_Well.'+side,[(29.1,5,32),(29.1,13,35),(28.2,19,44),(28.1,9,42)],f,(-1.0,0,0),'black',.18)
 for j in range(4):
  t=34+j*2.2;xx=29.45-(t-34)*.09
  rod('Calf_Mass2_Recessed_Cooling_Fin_%d.'%j+side,f((xx,7,t)),f((xx,14.2,t+2)),.43,'titanium',n=8,bevel=.04)
 strip('Calf_Mass2_Outer_Gold_Convergence.'+side,[f(p) for p in ((26,18,52),(22,23,65),(18,18,74))],.7,'gold')
 # Front actuator pocket is framed by a large ceramic cheek and a blue outer wall.
 local_plate('Calf_Mass2_Front_Actuator_Pocket.'+side,[(-7,-25,25),(7,-25,25),(8,-25,56),(-7,-25,56)],f,(0,4,0),'black',.24)
 ram('Calf_Mass2_Front_Linear_Drive.'+side,f((1,-27,29)),f((1,-27,55)),1.55)
 vv=[(-17,-15,27),(-10,-24,29),(-9,-26,46),(-11,-23,60),(-17,-13,55),(-20,-8,36)]
 local_plate('Calf_Mass2_Upper_Ceramic_Armored_Cheek.'+side,vv,f,(1.4,3,0),'white',.35)
 local_plate('Calf_Mass2_Inner_Ceramic_Overlapping_Leaf.'+side,[(-18,-13,49),(-11,-23,49),(-10,-23,61),(-13,-15,69),(-18,-6,64)],f,(1.3,2,0),'white',.28)
 local_plate('Calf_Mass2_Front_Outer_Blue_Guide.'+side,[(8,-24,27),(15,-18,32),(17,-12,48),(14,-18,59),(8,-25,54)],f,(-1,3,0),'blue',.3)
 local_plate('Calf_Mass2_Front_Guide_Seam.'+side,[(9,-24.5,31),(11,-23,35),(12,-22,49),(9,-24.5,50)],f,(0,1,0),'titanium',.12)
 # Broad ceramic middle coupling sits over the greave instead of floating on a narrow rod.
 vv=[(-10,-23,57),(0,-28,56),(10,-23,57),(-10,-23,68),(0,-28,72),(10,-23,68)]
 skin('Calf_Mass2_Ceramic_Mid_Coupling.'+side,[f(p) for p in vv],[(0,1,4,3),(1,2,5,4)],Vector(f((0,3.5,0)))-Vector(f((0,0,0))),'white',.34)
 # Lower front and side armor retain a full cross-section down to the ankle.
 loft('Calf_Mass2_Lower_Blue_Greave_Volume.'+side,[oct_ring(72,11.5,-19,11),oct_ring(89,13,-23,13),oct_ring(107,10.5,-20,11),oct_ring(119,7.8,-13,8)],'navy',.4,f)
 vv=[(-10,-23,74),(0,-28,73),(10,-23,74),(-12,-25,88),(0,-30,90),(12,-25,88),(-8,-21,108),(0,-26,115),(8,-21,108)]
 skin('Calf_Mass2_Folded_Front_Spear_Armor.'+side,[f(p) for p in vv],[(0,1,4,3),(1,2,5,4),(3,4,7,6),(4,5,8,7)],Vector(f((0,3.2,0)))-Vector(f((0,0,0))),'blue',.33)
 local_plate('Calf_Mass2_Front_Inset_Arrow.'+side,[(-4,-28.2,81),(0,-29.5,78),(4,-28.2,81),(4,-28.4,92),(0,-27.6,103),(-4,-28.4,92)],f,(0,.9,0),'panel',.16)
 for ss in (-1,1):
  strip('Calf_Mass2_Front_Armor_Break_%d.'%ss+side,[f((ss*5.5,-27.8,87)),f((ss*9,-26.4,90))],.55,'black')
  local_plate('Calf_Mass2_Lower_Side_Return_%d.'%ss+side,[(ss*13,-11,78),(ss*17,1,81),(ss*15,10,93),(ss*11,10,111),(ss*7,1,121),(ss*8,-9,112)],f,(-ss*2,0,0),'blue' if ss>0 else 'navy',.3)
 # Long power rams sit in the exposed channel between the blue side return and rear guard.
 ram('Calf_Mass2_Outer_Long_Power_Ram.'+side,f((23,10,64)),f((13,11,116)),2.15)
 ram('Calf_Mass2_Inner_Long_Power_Ram.'+side,f((-17,10,67)),f((-10,11,115)),1.8)
 for ss in (-1,1):
  ram('Calf_Mass2_Rear_Long_Damper_%d.'%ss+side,f((ss*7.5,28,60)),f((ss*5.5,18,117)),1.85)
  hose('Calf_Mass2_Rear_Hydraulic_Hose_%d.'%ss+side,[f(p) for p in ((ss*10,29,62),(ss*12,30,75),(ss*10,25,92),(ss*7,21,108))],.8)
  local_plate('Calf_Mass2_Ankle_Actuator_Clevis_%d.'%ss+side,[(ss*13,4,111),(ss*16,8,111),(ss*14,15,115),(ss*10,15,118),(ss*9,8,118)],f,(-ss*2.4,0,0),'steel',.25)
  local_plate('Calf_Mass2_Rear_Ankle_Petal_%d.'%ss+side,[(ss*10,20,98),(ss*13,23,102),(ss*10,20,116),(ss*6,13,124),(ss*6,12,117)],f,(-ss*1.6,-1,0),'blue',.25)
 # A service cassette and directional louvers break up the rear calf volume.
 local_plate('Calf_Mass2_Rear_Ceramic_Service_Cassette.'+side,[(-8,29,31),(7,29,31),(9,31,42),(6,31,53),(-6,31,53),(-9,30,41)],f,(0,-2.5,0),'white',.3)
 local_plate('Calf_Mass2_Rear_Cooling_Recess.'+side,[(-5,31.5,36),(5,31.5,36),(5,32,48),(-5,32,48)],f,(0,-1,0),'black',.15)
 for j in range(4):
  strip('Calf_Mass2_Rear_Cooling_Louver_%d.'%j+side,[f((-4.5,32.4,38+j*2.4)),f((4.5,32.4,38+j*2.4))],.8,'steel',(0,-.7,0))
 # Only a few structural fasteners are exposed at service-panel mounting points.
 for j,p in enumerate(((-13,-23,35),(-12,-23,53),(7,-24,65),(6,-25,99))):
  bolt('Calf_Mass2_Service_Armor_Fastener_%d.'%j+side,f(p),(0,-1,0),.62,'steel')
