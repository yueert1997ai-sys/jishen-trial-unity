from mass_ops import *
remove(['V3B_Thigh_','V3B_Mass_Thigh_','V3B_Femoral_Front_Ram_','V3B_Femur_Upper_Gold_Collar_','V3B_Hamstring_Exposed_Rod','V3B_Knee_Extensor_White_Fork'])

for s,side in ((1,'L'),(-1,'R')):
 group('05 Thigh and knee armor','Thigh.'+side);f=joint_frame('Thigh','Shin',side)
 # The femoral body now carries armor along its full length, with broad side returns.
 loft('Thigh_Mass2_Closed_Femoral_Load_Body.'+side,[oct_ring(5,9,-8,10),oct_ring(17,15,-13,16),oct_ring(32,18,-17,20),oct_ring(52,17.5,-17,19),oct_ring(65,13,-13,13),oct_ring(73,8.5,-8,8)],'frame',.65,f)
 def shell_ring(t,inn,out,fy,by,ss):
  return [(ss*inn,fy,t),(ss*(out-5),fy-1,t),(ss*out,fy*.52,t),(ss*out,by*.5,t),(ss*(out-4),by,t),(ss*inn,by*.75,t)]
 for ss in (-1,1):
  # Two thick ceramic clamshells overlap across an oblique waist in the thigh.
  loft('Thigh_Mass2_Upper_Ceramic_Clamshell_%d.'%ss+side,[shell_ring(t,inn,out,fy,by,ss) for t,inn,out,fy,by in ((13,8,14,-11,10),(24,6.5,20.5,-19,16),(37,6.5,22,-20.5,18),(44,7,20,-18,16))],'white',.42,f)
  loft('Thigh_Mass2_Lower_Ceramic_Clamshell_%d.'%ss+side,[shell_ring(t,inn,out,fy,by,ss) for t,inn,out,fy,by in ((41,7,21.7,-21,18.5),(52,6.5,21,-21,17),(64,8,16.5,-16,10),(70,8,12,-10,5))],'white',.38,f)
  # Machined dark joint between overlapping armor, visible around the outer side.
  local_plate('Thigh_Mass2_Oblique_Interlayer_Seal_%d.'%ss+side,[(ss*21.9,-10,39),(ss*22.3,6,40),(ss*18.3,17,43),(ss*18.1,15,45),(ss*21.5,5,42),(ss*21.3,-9,41)],f,(-ss*.5,0,0),'rubber',.09)
  # Independent blue side cartridge makes the outer and inner faces intentionally different.
  if ss>0:
   pts=[(22.5,-7,27),(24,3,30),(23.7,12,39),(22.5,11,50),(21,2,57),(21,-7,48)]
   local_plate('Thigh_Mass2_Outer_Blue_Service_Cassette.'+side,pts,f,(-2.7,0,0),'blue',.28)
   local_plate('Thigh_Mass2_Outer_Recessed_Channel.'+side,[(24.2,2,32),(24.2,8,34),(23.4,10,41),(23.4,3,40)],f,(-.8,0,0),'black',.13)
   for j in range(3):
    t=34+j*2.4;xx=24.4-(t-34)*.085
    rod('Thigh_Mass2_Recessed_Louver_%d.'%j+side,f((xx,3,t)),f((xx,7.8,t+1)),.43,'titanium',n=8,bevel=.05)
   local_plate('Thigh_Mass2_Outer_Silver_Terminal.'+side,[(22,-6,48),(23.2,0,51),(21.2,5,59),(18.5,-2,62)],f,(-1.0,0,0),'silver',.18)
  else:
   local_plate('Thigh_Mass2_Inner_Titanium_Armor_Return.'+side,[(-21,-6,29),(-22,4,31),(-19,13,41),(-20,10,49),(-20,-4,45)],f,(1.3,0,0),'silver',.22)
  for j,t in enumerate((25,55)):
   # Broad attachment ears replace isolated bolt dots on a flat sheet.
   local_plate('Thigh_Mass2_Armor_Mount_%d_%d.'%(ss,j)+side,[(ss*14,-20.5,t),(ss*18,-16.5,t+1),(ss*18,-14.5,t+4),(ss*14,-19,t+4)],f,(-ss*.7,.7,0),'steel',.14)
   bolt('Thigh_Mass2_Captive_Armor_Lock_%d_%d.'%(ss,j)+side,f((ss*15.6,-20,t+2)),(0,-1,0),.62)
 # The raised blue central cassette overlaps the ceramic chassis in three broad steps.
 def center_panel(name,t0,t1,w0,w1,y0,y1,mat):
  vs=[(-w0,y0+2,t0),(0,y0-1,t0-1),(w0,y0+2,t0),(-w1,y1+2,t1),(0,y1-1,t1+2),(w1,y1+2,t1)]
  skin(name,[f(p) for p in vs],[(0,1,4,3),(1,2,5,4)],Vector(f((0,3,0)))-Vector(f((0,0,0))),mat,.29)
 center_panel('Thigh_Mass2_Blue_Upper_Shield.'+side,22,38,11.5,12,-23,-24,'blue')
 center_panel('Thigh_Mass2_Blue_Lower_Shield.'+side,40,58,12,9.5,-24.3,-21,'blue')
 center_panel('Thigh_Mass2_Knee_Tapered_Tongue.'+side,59.5,68,8.5,4.5,-20.5,-14.5,'navy')
 local_plate('Thigh_Mass2_Recessed_Front_Datum.'+side,[(-5.5,-24.5,26),(5.5,-24.5,26),(5,-25,33),(-5,-25,33)],f,(0,.75,0),'panel',.13)
 for ss in (-1,1):
  strip('Thigh_Mass2_Front_Shield_Engraving_%d.'%ss+side,[f((ss*4.5,-24.8,45)),f((ss*8,-23.6,48))],.55,'black')
  # Large upper actuator is nested beneath the flared hip shoulder of the armor.
  ram('Thigh_Mass2_Upper_Femoral_Drive_%d.'%ss+side,f((ss*8,-12,4)),f((ss*8,-17,26)),1.75)
  ram('Thigh_Mass2_Rear_Damper_%d.'%ss+side,f((ss*9,21,15)),f((ss*8,21,61)),1.9)
 local_plate('Thigh_Mass2_Rear_Armored_Spine.'+side,[(-5,21,23),(5,21,23),(6,22,48),(2,20,66),(-5,20,58)],f,(0,-3,0),'blue',.3)
 # Thick split knee fork receives the circular knee assembly instead of ending as a thin hem.
 for ss in (-1,1):
  local_plate('Thigh_Mass2_Knee_Armored_Fork_%d.'%ss+side,[(ss*9,-12,65),(ss*14,-6,66),(ss*13,2,74),(ss*10,3,78),(ss*7,-5,77),(ss*8,-7,71)],f,(-ss*2.2,0,0),'white',.3)
