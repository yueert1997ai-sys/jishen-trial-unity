from mass_ops import *
remove(['V3B_Forearm_Folded_Main_Shell','V3B_Forearm_Central_Blue_Overlay','V3B_Forearm_Lower_Overlay','V3B_Forearm_Long_Seam','V3B_Forearm_Gold_Stencil','V3B_Forearm_Service_Stencil','V3B_Forearm_Side_Planar_Cartridge','V3B_Forearm_Side_Dark_Latch'])
for s,side in ((1,'L'),(-1,'R')):
 group('04 Arms and articulated hands','Forearm.'+side);f=joint_frame('Forearm','Hand',side)
 # The gauntlet is a folded shell with a deep central service trough, flanked by armor.
 def cy(t):
  if t<=29:return -30-(t-13)*5/16
  if t<=51:return -35+(t-29)*4/22
  return -31+(t-51)*8/7
 def face(x,t,dep=0):return f((x,cy(t)+abs(x)*.40+dep,t))
 rows=[(13,9),(29,13),(49,10),(58,6.5)];vs=[face(x,t,2) for t,w in rows for x in (-w,0,w)]
 fs=[(3*j+k,3*j+k+1,3*(j+1)+k+1,3*(j+1)+k) for j in range(3) for k in (0,1)]
 skin('Mass_Forearm_Folded_Underarmor.'+side,vs,fs,(0,3,0),'navy',.35)
 for ss in (-1,1):
  for j,(t0,t1,w0,w1) in enumerate(((15,28,8.7,12.5),(29.4,45,12.5,11),(46.4,55,10.4,7))):
   pts=[face(ss*x,t,-.2) for x,t in ((3.2,t0),(w0,t0+1),(w1,t1-1),(w1-1,t1),(3.2,t1-1))]
   plate('Mass_Forearm_Segmented_Armor_%d_%d.'%(ss,j)+side,pts,Vector(f((0,1.8,0)))-Vector(f((0,0,0))),'blue',.22)
   # These grooves occupy the gap between raised panels, with a machined bottom.
   strip('Mass_Forearm_Machined_Panel_Break_%d_%d.'%(ss,j)+side,[face(ss*4,t1+.3,.25),face(ss*(w1-.5),t1+.3,.25)],.47,'titanium')
  strip('Mass_Forearm_Inset_Rail_%d.'%ss+side,[face(ss*2.85,t,.4) for t in (16,28,44,54)],.45,'steel')
  # A slash-shaped secondary seam has a dark floor surrounded by separate armor.
  for j,t in enumerate((22,37)):
   strip('Mass_Forearm_Engraved_Slash_%d_%d.'%(ss,j)+side,[face(ss*6,t,-.48),face(ss*9.4,t+2.5,-.48)],.55,'black')
 # Long recessed cartridge has clamps, a chromed actuator and a narrow conduit.
 plate('Mass_Forearm_Service_Trough.'+side,[face(x,t,1.0) for x,t in ((-2.6,17),(2.6,17),(2.6,52),(-2.6,52))],Vector(f((0,.7,0)))-Vector(f((0,0,0))),'black',.14)
 for x in (-1.1,1.1):
  hose('Mass_Forearm_Recessed_Conductor_%s.'%x+side,[face(x,t,.32) for t in (19,29,40,50)],.47,'titanium' if x<0 else 'gold')
 for j,t in enumerate((19.5,33,49)):
  strip('Mass_Forearm_Cartridge_Clamp_%d.'%j+side,[face(-2.5,t,-.22),face(2.5,t,-.22)],1.1,'frame',(0,.8,0))
 ram('Mass_Forearm_Outer_Power_Ram.'+side,f((20,3,15)),f((17,3,49)),1.8)
 hose('Mass_Forearm_Outer_Return_Line.'+side,[f(p) for p in ((20,8,17),(22,10,24),(20,11,40),(16,7,50))],.82)
 local_plate('Mass_Forearm_Side_Armored_Rail.'+side,[(20,-13,18),(24,-9,22),(24,-2,26),(21,-2,42),(16,-8,49),(17,-15,39)],f,(-2.5,0,0),'blue',.28)
 local_plate('Mass_Forearm_Side_Service_Well.'+side,[(24.1,-8,26),(24.1,-2.5,27),(22.1,-2.5,39),(22.1,-8,38)],f,(-.75,0,0),'black',.13)
 for j in range(4):
  t=28+j*2.6;xx=24.4-(t-28)*.155
  rod('Mass_Forearm_Side_Recessed_Fin_%d.'%j+side,f((xx,-7.5,t)),f((xx,-3.0,t+.5)),.38,'steel',n=8,bevel=.04)
 for j,t in enumerate((17,46)):
  local_plate('Mass_Forearm_Ram_Anchor_%d.'%j+side,[(17,-1,t-2),(22,-1,t-2),(22,7,t+1),(17,7,t+1)],f,(-1.5,0,1),'silver',.25)
 # A wrap-around ceramic wrist guard locks the gauntlet into the circular wrist.
 local_plate('Mass_Forearm_Ceramic_Cuff.'+side,[(-8,-15,52),(-6,-22,55),(6,-22,55),(8,-15,52),(7,-13,57),(5,-18,59),(-5,-18,59),(-7,-13,57)],f,(0,1.5,0),'white',.25)
 group('04 Arms and articulated hands','UpperArm.'+side);f=joint_frame('UpperArm','Forearm',side)
 ram('Mass_Upperarm_External_Servo.'+side,f((16.8,0,12)),f((15,0,42)),1.7)
 hose('Mass_Upperarm_Rear_Flexible_Feed.'+side,[f(p) for p in ((12,11,9),(15,13,14),(16,13,29),(13,9,41))],.8)
 for j,t in enumerate((17,34)):
  local_plate('Mass_Upperarm_Ceramic_Ram_Clasp_%d.'%j+side,[(14,-8,t),(20,-5,t+1),(20,3,t+4),(16,5,t+5),(14,0,t+3)],f,(-1.8,0,0),'white',.25)
 local_plate('Mass_Upperarm_Biceps_Servo_Inlay.'+side,[(-2,-20,15),(3,-20,15),(3,-20,30),(-2,-20,31)],f,(0,1,0),'steel',.18)
 # Small dorsal knuckle guards add mass to the palm without changing the held grip.
 group('04 Arms and articulated hands','Hand.'+side)
 palm=bpy.data.objects.get('V3B_Palm_Armored_Chassis.'+side)
 if palm:
  ps=[palm.matrix_world@v.co/S for v in palm.data.vertices];c0=sum(ps,Vector())/len(ps)
  for j,x in enumerate((-4.5,-1.5,1.5,4.5)):
   p=c0+Vector((x,-6,-4))
   hull('Mass_Hand_Dorsal_Knuckle_%d.'%j+side,[p+Vector(a) for a in ((-1.1,0,1.5),(1.1,0,1.5),(1.25,0,-1.5),(-1.25,0,-1.5),(-1,-1.5,1),(1,-1.5,1),(1,-1.5,-1),(-1,-1.5,-1))],'titanium',.14)
