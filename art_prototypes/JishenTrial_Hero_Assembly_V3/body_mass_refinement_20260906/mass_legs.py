from mass_ops import *
remove(['V3B_Calf_Long_Rear_Tendon_','V3B_Mass_Shin_Longitudinal_Armor_Rail_'])
for s,side in ((1,'L'),(-1,'R')):
 group('06 Tapered shin and calf','Shin.'+side);f=joint_frame('Shin','Foot',side)
 # A visible cylinder sits between the forward armor ridge and its rear protective rail.
 ram('Mass_Calf_Outer_Power_Ram.'+side,f((16.8,7.5,61)),f((12.5,7.5,115)),1.9)
 ram('Mass_Calf_Inner_Power_Ram.'+side,f((-13.6,8,65)),f((-9.6,8.5,115)),1.5)
 for ss in (-1,1):
  ram('Mass_Calf_Rear_Damper_%d.'%ss+side,f((ss*6.8,22,58)),f((ss*5.5,14.5,117)),1.65)
  hose('Mass_Calf_Return_Hose_%d.'%ss+side,[f(p) for p in ((ss*8,23,60),(ss*9.5,25,70),(ss*8,21,90),(ss*6,17,109))],.72)
 # Ridged, tapered closed side shells. The hydraulic slot remains exposed between them.
 for ss in (-1,1):
  pts=[(ss*14,-9,65),(ss*19,-1,67),(ss*17,3,71),(ss*17,-6,89),(ss*15,1,97),(ss*9,-1,116),(ss*8,-9,108),(ss*12,-12,84)]
  local_plate('Mass_Calf_Folded_Long_Armor_%d.'%ss+side,pts,f,(-ss*2.2,0,0),'blue',.35)
  local_plate('Mass_Calf_Machined_Lip_%d.'%ss+side,[(ss*18.2,-1.4,69),(ss*16.2,2.6,74),(ss*14.4,.6,98),(ss*9.3,-1.1,113)],f,(-ss*.65,0,0),'titanium',.14)
  local_plate('Mass_Calf_Rear_Ram_Guard_%d.'%ss+side,[(ss*14,17,72),(ss*17,18,78),(ss*13.5,17,101),(ss*8,12,116),(ss*9,9,106)],f,(-ss*1.8,-.6,0),'navy',.25)
  # Captive yokes attach at both ends of the drive cartridge.
  for j,t in enumerate((66,110)):
   u=ss*(16.4 if j==0 else 12.7)
   local_plate('Mass_Calf_Ram_Clevis_%d_%d.'%(ss,j)+side,[(u,3,t-2),(u,12,t-2),(u,12,t+2),(u,3,t+2)],f,(-ss*2.8,0,0),'silver',.18)
 # The upper nacelle carries an inset radiator, not isolated painted stripes.
 local_plate('Mass_Calf_Radiator_Recess.'+side,[(23,3,29),(23,12,33),(22,18,45),(22,8,48)],f,(-2,0,0),'black',.2)
 for j in range(5):
  t=33+j*2.7
  rod('Mass_Calf_Radiator_Fin_%d.'%j+side,f((23.3,6,t)),f((22.8,14,t+3)),.42,'steel',n=8,bevel=.05)
 local_plate('Mass_Shin_Upper_Secondary_Splint.'+side,[(-4,-20,30),(3,-20,30),(4,-20,45),(0,-21,48),(-4,-20,44)],f,(0,1.5,0),'silver',.19)
 group('05 Thigh and knee armor','Thigh.'+side);f=joint_frame('Thigh','Shin',side)
 # Stacked load straps and machined femoral guides keep the thigh slender.
 for j,t in enumerate((36,51)):
  local_plate('Mass_Thigh_Ceramic_Clasp_%d.'%j+side,[(9,-15,t),(15,-9,t+1),(15,-2,t+5),(12,-3,t+9),(9,-13,t+7)],f,(-1.5,1,0),'white',.23)
  local_plate('Mass_Thigh_Clasp_Recess_%d.'%j+side,[(12,-10,t+2),(14,-7,t+3),(13,-5,t+6),(11,-9,t+5)],f,(-.5,.4,0),'frame',.09)
 for x in (-8.5,8.5):
  hose('Mass_Femoral_Flexible_Return_%s.'%x+side,[f(p) for p in ((x,5,6),(x*1.25,8,12),(x*1.2,9,24),(x,8,30))],.7)
 group('07 Feet and ankle couplings','Foot.'+side)
 q=Matrix.Rotation(math.radians(19*s),3,'Z');origin=Vector((s*93,5,0))
 ft=lambda p:tuple(origin+q@Vector((s*p[0],p[1],p[2])))
 vv=[ft(p) for p in ((-9,-20,29),(0,-24,35),(9,-20,29),(-8,-32,20),(0,-35,25),(8,-32,20))]
 skin('Mass_Foot_Arched_Tarsal_Cover.'+side,vv,[(0,1,4,3),(1,2,5,4)],(0,1,-2),'blue',.3)
 for ss in (-1,1):
  local_plate('Mass_Foot_Instep_White_Clasp_%d.'%ss+side,[(ss*6.5,-23,31),(ss*9.8,-21,27),(ss*10,-28,20),(ss*7,-30,23)],ft,(-ss*.8,0,-.8),'white',.18)
  ram('Mass_Foot_Heel_Recoil_%d.'%ss+side,ft((ss*10,4,26)),ft((ss*10,20,10)),1.1)
