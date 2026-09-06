from body_common import *
def limb_basis(a,b,s):
 a=Vector(a);d=(Vector(b)-a).normalized();u=Vector((s,0,0));u=(u-d*u.dot(d)).normalized();v=d.cross(u)
 if v.y<0:v=-v
 return lambda p:tuple(a+u*p[0]+v*p[1]+d*p[2])
def segment(name,a,b,width,depth,mat='frame'):
 a=Vector(a);b=Vector(b);q=(b-a).to_track_quat('Z','Y')
 vv=[tuple(p+q@Vector((xx*width/2,yy*depth/2,0))) for p in (a,b) for xx,yy in ((-1,-1),(1,-1),(1,1),(-1,1))]
 return mesh(name,vv,[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)],mat,.3)

for s,side in ((1,'L'),(-1,'R')):
 group('04 Arms and articulated hands','UpperArm.'+side)
 A=(s*64,3,330);B=(s*87,0,281);f=limb_basis(A,B,s)
 def lp(name,pts,depth=(0,2,0),mat='white',bevel=.2):return plate(name,[f(p) for p in pts],depth,mat,bevel)
 # Ceramic upper-arm forks wrap around the retained shoulder drive and upper-arm frame.
 lp('Upperarm_Main_Ceramic.'+side,[(-8,-10,13),(2,-11,12),(7,-10,20),(7,-10,42),(2,-10,45),(-7,-10,40)],(0,4,0),'white',.3)
 lp('Upperarm_Ceramic_Outer_Return.'+side,[(7,-10,20),(11,-4,20),(11,8,26),(9,10,39),(6,-5,43)],(-s*2,0,0),'white',.26)
 lp('Upperarm_Ceramic_Inner_Rail.'+side,[(-10,-7,18),(-8,-10,18),(-7,-10,40),(-10,-5,41),(-12,2,34)],(s*2,0,0),'silver',.24)
 lp('Upperarm_Crown_Blue_Cover.'+side,[(-8,-9,4),(7,-9,4),(8,-8,13),(3,-11,17),(-6,-11,15)],(0,4,0),'blue')
 lp('Biceps_Dark_Servo_Window.'+side,[(-3,-11.2,17),(4,-11.2,17),(4,-11.2,31),(-2,-11.2,34)],(0,1,0),'frame',.12)
 lp('Upperarm_Ceramic_Overlap.'+side,[(-7,-11.4,31),(-1,-11.4,29),(1,-11.4,43),(-5,-11.4,44)],(0,1,0),'white',.17)
 lp('Upperarm_Rear_Ceramic.'+side,[(-7,9,17),(5,10,16),(8,11,31),(3,11,42),(-7,9,40)],(0,-3,0),'white',.25)
 for t in (22,27,32):
  p=f((2,12,t));box('Upperarm_Rear_Cooling_%d.'%t+side,p,(6,1,1),'black',.08)
 for x,t in ((-5,20),(4,37)):bolt('Upperarm_Ceramic_Pin_%d.'%t+side,f((x,-11.8,t)),r=.48)
 piston('Upperarm_Exposed_Silver_Tendon.'+side,f((-10,8,13)),f((-9,9,42)),1.1)

 group('04 Arms and articulated hands','Forearm.'+side)
 A=(s*87,0,281);B=(s*112,-2,226);f=limb_basis(A,B,s)
 lp('Elbow_White_Travel_Shield.'+side,[(-7,-8,2),(5,-9,2),(8,-9,10),(5,-12,18),(-5,-12,17),(-9,-8,11)],(0,3,0),'white',.25)
 lp('Elbow_Travel_Inset.'+side,[(-3,-12.2,7),(3,-12.2,7),(3,-12.2,13),(-3,-12.2,13)],(0,1,0),'titanium',.1)
 # The gauntlet is long, narrow and layered, with a dorsal ridge and separate side vanes.
 vv=[(-8,-11,17),(0,-15,17),(9,-11,18),(-7,-10,51),(0,-14,54),(7,-10,53),(-5,-10,59),(5,-10,59)]
 skin('Forearm_Folded_Main_Shell.'+side,[f(p) for p in vv],[(0,1,4,3),(1,2,5,4),(3,4,6),(4,5,7,6)],(0,3,0),'blue',.23)
 lp('Forearm_Central_Blue_Overlay.'+side,[(-5,-14.8,23),(1,-16,20),(5,-14.8,24),(4,-14.8,43),(-3,-14.8,43)],(0,1.3,0),'panel',.13)
 lp('Forearm_Lower_Overlay.'+side,[(-3,-14.8,44),(4,-14.8,44),(4,-13.8,52),(-3,-13.8,54)],(0,1,0),'blue',.13)
 lp('Forearm_Inner_Slender_Armor.'+side,[(-9,-7,21),(-8,-11,19),(-7,-10,52),(-8,-6,55),(-11,0,45)],(s*1.5,1,0),'navy',.18)
 lp('Forearm_Outer_Lance.'+side,[(10,-3,-1),(15,2,-8),(15,6,13),(12,6,44),(8,0,56),(10,-3,22)],(-s*2,1,0),'blue',.18)
 lp('Forearm_Lance_White_Edge.'+side,[(14,2,-4),(15,3,-8),(14,4,18),(12,3,26)],(-s*.6,0,0),'white',.1)
 lp('Forearm_Outer_Rear_Cap.'+side,[(8,8,16),(10,9,14),(11,12,39),(6,10,49),(3,8,42)],(-s*1,-1,0),'white',.22)
 lp('Forearm_Rear_Shell.'+side,[(-6,9,19),(5,11,19),(8,12,42),(4,11,55),(-5,9,53)],(0,-3,0),'blue',.24)
 lp('Forearm_Wrist_White_Coupling.'+side,[(-6,-11,53),(5,-11,53),(7,-9,58),(6,-8,63),(-5,-8,63),(-7,-9,59)],(0,4,0),'white',.23)
 strip('Forearm_Long_Seam.'+side,[f((-5,-15.1,24)),f((-4,-15.1,43))],.33,'edge')
 for x,t in ((-6,20),(5,49),(-4,58)):bolt('Forearm_Fastener_%d.'%t+side,f((x,-15.3 if t<53 else -11.3,t)),r=.48)
 for t in (29,47):marker('Forearm_Gold_Stencil_%d.'%t+side,f((4,-15.4,t)),1.5)

 group('04 Arms and articulated hands','Hand.'+side)
 c=Vector((s*115,-3,212))
 rod('Wrist_Swivel_New.'+side,(s*112,-2,229),(s*114,-3,219),4,'frame')
 ring('Wrist_Exposed_Seal.'+side,(s*113,-2.7,224),(s*113.3,-2.9,222),4.6,3.6,'titanium')
 hull('Palm_Armored_Chassis.'+side,[tuple(c+Vector((x,y,z))) for x,y,z in ((-7,-5,6),(7,-5,6),(7,-5,-6),(-6,-5,-7),(-6,5,5),(6,5,5),(5,5,-5),(-5,5,-6))],'frame',.6)
 plate('Hand_Dorsal_Blue_Shield.'+side,[tuple(c+Vector((x,y,z))) for x,y,z in ((-5,-5.8,5),(5,-5.8,5),(6,-6,-2),(2,-6,-5),(-5,-6,-3))],(0,1.2,0),'blue',.27)
 strip('Hand_Wrist_Trim.'+side,[tuple(c+Vector((-4,-6.1,4))),tuple(c+Vector((4,-6.1,4)))],.55,'silver')
 for dx in (-4,4):bolt('Hand_Fastener_%d.'%dx+side,c+Vector((dx,-6.3,2)),r=.45)
 def finger_segment(name,a,b,axis,width=2.2):
  segment(name+'_Inner_Bone',a,b,width,width*.9,'frame')
  a=Vector(a);b=Vector(b);delta=(b-a).normalized();q=delta.to_track_quat('Z','Y');off=q@Vector((0,-.8,0))
  segment(name+'_Knuckle_Armor',a+delta*.3+off,b-delta*.4+off,width*.82,width*.65,'silver')
  ax=Vector(axis).normalized();rod(name+'_Pivot',a-ax*width*.53,a+ax*width*.53,width*.48,'titanium',n=16,bevel=.07)
 if side=='L':
  for i,x in enumerate((-4.7,-1.6,1.6,4.7)):
   ps=[c+Vector((x,1,-5)),c+Vector((x,0,-10)),c+Vector((x,-4,-11)),c+Vector((x,-7,-7))]
   for j in range(3):
    e=bpy.data.objects[f'Finger_{i}_Joint_{j}.{side}'];m=Matrix.Identity(4);m.translation=U(ps[j]);e.matrix_world=m;bpy.context.view_layer.update()
    old=PARENT;group('04 Arms and articulated hands',e);finger_segment(f'Finger_{i}_Phalanx_{j}.{side}',ps[j],ps[j+1],(1,0,0),2.6)
  group('04 Arms and articulated hands','Hand.'+side)
  ps=[c+Vector((-6,0,3)),c+Vector((-9,-3,-1)),c+Vector((-7,-7,-5)),c+Vector((-4,-8,-4))]
  for j in range(3):finger_segment('Thumb_%d.'%j+side,ps[j],ps[j+1],(0,1,0),2.8)
 else:
  # Four independently articulated fingers close around the actual sword grip axis.
  gc=Vector((-116,-8,209));axis=Vector((.30,-.08,-.9506)).normalized();u=Vector((.9536,0,.301)).normalized();v=axis.cross(u).normalized()
  SC['sword_grip_center_units']=list(gc);SC['sword_grip_axis']=list(axis)
  for i in range(4):
   cc=gc+axis*((i-1.5)*3.25)
   ps=[cc+(u*math.cos(math.radians(t))+v*math.sin(math.radians(t)))*4.0 for t in (183,133,83,30)]
   for j in range(3):
    e=bpy.data.objects[f'Finger_{i}_Joint_{j}.{side}'];m=Matrix.Identity(4);m.translation=U(ps[j]);e.matrix_world=m;bpy.context.view_layer.update()
    group('04 Arms and articulated hands',e);finger_segment(f'Grip_Finger_{i}_Phalanx_{j}.{side}',ps[j],ps[j+1],axis,2.8)
  group('04 Arms and articulated hands','Hand.'+side)
  ps=[gc-axis*8-u*5,gc-axis*8+u*1,gc-axis*4+u*4+v*2,gc-axis*1+u*2+v*4]
  for j in range(3):finger_segment('Opposed_Grip_Thumb_%d.'%j+side,ps[j],ps[j+1],axis,3)
  so=bpy.data.objects.new('V3B_Sword_Grip_Socket',None);COLS['BODY_V3 | 04 Arms and articulated hands'].objects.link(so);so.parent=bpy.data.objects['Hand.R'];m=Matrix.Identity(4);m.translation=U(gc);so.matrix_world=m;so['grip_axis']=list(axis)

for s,side in ((1,'L'),(-1,'R')):
 group('05 Thigh and knee armor','Thigh.'+side)
 a=(s*31,1,242);b=(s*58,-1,167);f=limb_basis(a,b,s)
 def lp(name,pts,depth=(0,2,0),mat='white',bevel=.2):return plate(name,[f(p) for p in pts],depth,mat,bevel)
 # Exposed femoral rams and titanium shafts above the white split armor sleeve.
 for x in (-6.7,7.3):
  piston('Femoral_Front_Ram_%s.'%x+side,f((x,-5,6)),f((x,-7,41)),1.4)
  ring('Femur_Upper_Gold_Collar_%s.'%x+side,f((x,-5,9)),f((x,-5,12)),2.1,1.4,'gold',24)
 lp('Thigh_Dark_Upper_Channel.'+side,[(-5,-10,9),(5,-10,9),(5,-11,25),(-5,-11,26)],(0,3,0),'frame')
 lp('Thigh_Upper_Titanium_Insert.'+side,[(-3,-10.3,12),(3,-10.3,12),(3,-11.3,23),(-3,-11.3,23)],(0,1,0),'titanium',.12)
 # The white housing remains visible around a narrower blue central armor cartridge.
 vv=[(-11,-8,28),(-7,-13,27),(7,-13,27),(12,-7,29),(-13,-6,61),(-8,-12,69),(7,-12,68),(12,-5,60)]
 skin('Thigh_Ceramic_Wrapped_Housing.'+side,[f(p) for p in vv],[(0,1,5,4),(1,2,6,5),(2,3,7,6)],(0,2.4,0),'white',.28)
 lp('Thigh_Outer_Ceramic_Return.'+side,[(12,-6,30),(14,2,33),(13,10,52),(9,7,65),(11,-5,62)],(-s*2,0,0),'white',.3)
 lp('Thigh_Inner_Silver_Return.'+side,[(-12,-7,33),(-14,3,35),(-12,9,61),(-8,-4,71),(-12,-7,60)],(s*1.8,0,0),'silver',.25)
 lp('Thigh_Blue_Upper_Cartridge.'+side,[(-6.5,-14,29),(6.5,-14,29),(8,-13,34),(7,-14,46),(-7,-14,46),(-8,-13,35)],(0,2,0),'blue',.2)
 lp('Thigh_Blue_Lower_Cartridge.'+side,[(-7,-14,47),(7,-14,47),(6,-14,62),(2,-15,66),(-6,-14,63)],(0,2,0),'blue',.2)
 lp('Thigh_Panel_Inset.'+side,[(-4,-14.3,31),(4,-14.3,31),(4,-14.3,41),(-4,-14.3,42)],(0,1,0),'panel',.13)
 strip('Thigh_Blue_Edge_Seam.'+side,[f((6.4,-14.4,31)),f((6.4,-14.4,44))],.32,'edge')
 for x,t in ((-5,32),(5,60),(-10,54)):bolt('Thigh_Ceramic_Pin_%s_%s.'%(x,t)+side,f((x,-14.4 if abs(x)<8 else -10,t)),r=.5)
 marker('Thigh_Gold_Stencil.'+side,f((2,-14.5,35)),2.5)
 lp('Thigh_Rear_Ceramic_Left.'+side,[(-11,5,32),(-5,11,29),(-4,12,65),(-9,8,64)],(0,-2,0),'white',.24)
 lp('Thigh_Rear_Ceramic_Right.'+side,[(5,11,28),(11,5,32),(10,8,62),(5,12,67)],(0,-2,0),'white',.24)
 lp('Thigh_Rear_Blue_Spine.'+side,[(-4,12,34),(4,12,34),(5,13,58),(1,13,69),(-4,12,63)],(0,-2,0),'blue',.2)
 piston('Hamstring_Exposed_Rod.'+side,f((0,11,9)),f((0,14,44)),1.5)
 lp('Knee_Extensor_White_Fork.'+side,[(-7,-11,68),(5,-11,68),(8,-9,74),(6,-8,80),(2,-9,80),(1,-11,73),(-3,-11,73),(-4,-9,80),(-8,-8,78)],(0,2,0),'white',.2)

 group('06 Tapered shin and calf','Shin.'+side)
 a=(s*58,-1,167);b=(s*93,2,40);f=limb_basis(a,b,s)
 bearing('Knee_Gold_Race.'+side,a,(1,0,0),5.9,21)
 # Separate knee, white shin insert, and long tapering blue ankle spear.
 lp('Knee_Leading_Blue_Shield.'+side,[(-8,-12,7),(0,-15,3),(8,-12,8),(10,-12,19),(3,-15,31),(-5,-15,29),(-10,-12,19)],(0,3,0),'blue',.25)
 lp('Knee_Titanium_Extensor.'+side,[(-4,-15.3,-2),(4,-15.3,-2),(4,-15.3,10),(-4,-15.3,11)],(0,1.5,0),'titanium',.15)
 lp('Knee_Crown_Panel.'+side,[(-4,-15.4,12),(4,-15.4,11),(6,-15.4,22),(-3,-15.4,25)],(0,1.2,0),'panel',.15)
 lp('Shin_Recessed_Linear_Drive.'+side,[(-5,-10,29),(5,-10,29),(6,-11,52),(-5,-11,54)],(0,4,0),'black',.2)
 lp('Shin_Actuator_Cartridge.'+side,[(-2,-11.3,32),(3,-11.3,32),(3,-11.3,47),(-2,-11.3,48)],(0,2,0),'titanium',.15)
 lp('Shin_Upper_White_Oblique_Panel.'+side,[(-13,-8,25),(-6,-12,29),(-5,-12,48),(-9,-10,59),(-13,-5,51),(-15,-4,32)],(0,3,0),'white',.25)
 lp('Shin_Lower_White_Coupling.'+side,[(-5,-12,55),(6,-12,55),(6,-12,70),(3,-13,75),(-5,-12,71)],(0,3,0),'white',.25)
 lp('Shin_Blue_Long_Spear.'+side,[(-4,-12,76),(6,-12,73),(7,-11,89),(5,-11,103),(0,-15,116),(-6,-11,110),(-7,-11,96)],(0,3,0),'blue',.23)
 lp('Shin_Long_Spear_Facet.'+side,[(-2,-12.6,78),(4,-12.6,77),(4,-12,101),(0,-15.4,112)],(0,.8,0),'panel',.13)
 # Outer calf armor is broken into a swept upper wing and an overlapping narrow lower blade.
 lp('Calf_Upper_Swept_Armor.'+side,[(8,-5,12),(17,-1,11),(22,4,29),(21,7,47),(16,1,43),(9,-7,27)],(-s*2,1,0),'blue',.25)
 lp('Calf_Upper_Outer_Facet.'+side,[(16,-1.2,15),(19,1,22),(20,2,36),(16,-.2,32)],(-s*.8,1,0),'panel',.15)
 lp('Calf_Lower_Overlapping_Blade.'+side,[(10,-5,43),(18,0,39),(18,7,57),(11,8,80),(10,3,69),(7,-5,59)],(-s*2,1,0),'blue',.22)
 strip('Calf_Lance_Gold_Leading_Line.'+side,[f((18,0,43)),f((15,5,65)),f((11,7,77))],.65,'gold')
 lp('Calf_Inner_Armor.'+side,[(-12,2,33),(-14,8,32),(-15,10,52),(-9,7,76),(-8,0,61)],(s*2,0,0),'navy',.22)
 lp('Calf_Inner_White_Shield.'+side,[(-11,-4,38),(-8,-8,41),(-8,-8,54),(-11,-3,64)],(s*1.2,1,0),'white',.2)
 # Rear variable damper and segmented ankle chain, visible in the rear and side elevations.
 lp('Calf_Rear_Damper_Housing.'+side,[(-9,8,23),(7,11,23),(11,16,39),(9,18,61),(-7,17,63),(-10,13,44)],(0,-4,0),'frame',.3)
 lp('Calf_Rear_Blue_Cover.'+side,[(-7,16,27),(5,16,26),(8,19,40),(4,20,58),(-6,18,55)],(0,-2,0),'blue',.2)
 lp('Calf_Rear_Ceramic_Insert.'+side,[(-3,19,30),(3,19,30),(4,20,47),(-3,20,47)],(0,-1.5,0),'white',.18)
 for x in (-6,6):piston('Calf_Long_Rear_Tendon_%s.'%x+side,f((x,13,60)),f((x,10,118)),1.25)
 lp('Calf_Rear_Ankle_Gaiter.'+side,[(-6,9,96),(6,9,96),(7,9,110),(4,9,119),(-4,9,119),(-7,9,110)],(0,-3,0),'navy',.22)
 for x,t in ((-10,34),(-11,49),(3,69),(13,50),(1,102)):
  bolt('Shin_Service_Pin_%s_%s.'%(x,t)+side,f((x,-12.4 if x<8 else -2,t)),r=.5,mat='gold' if x>8 else 'titanium')
 marker('Calf_Caution_Mark.'+side,f((15,1,33)),2)

 group('07 Feet and ankle couplings','Foot.'+side)
 # Slight toe-out, open ankle bridge, split pointed forefoot, and independently layered heels.
 q=Matrix.Rotation(math.radians(19*s),3,'Z');c=Vector((s*93,2,0))
 def foot(p):return tuple(c+q@Vector((s*p[0],p[1],p[2])))
 def ft(name,pts,d=(0,1,0),mat='blue',bev=.2):return plate(name,[foot(p) for p in pts],d,mat,bev)
 sole=[(-11,-31,0),(8,-36,0),(12,-27,0),(12,13,0),(8,20,0),(-9,19,0),(-12,9,0),(-12,-19,0)]
 hull('Foot_Continuous_Tapered_Sole.'+side,[foot(p) for p in sole]+[foot((x,y,z+3.2)) for x,y,z in sole],'rubber',.35)
 hull('Foot_Internal_Heel_Chassis.'+side,[foot(p) for p in [(-7,-13,3),(7,-13,3),(8,14,3),(-8,14,3),(-5,-9,21),(5,-9,21),(6,8,28),(-6,8,28)]],'frame',.45)
 bearing('Ankle_Gold_Gimbal.'+side,foot((0,1,30)),(1,0,0),5.5,20)
 # Blue side cheeks fan around the gold ankle axis.
 for ss in (-1,1):
  pp=[(ss*10,-4,40),(ss*12,4,37),(ss*13,10,24),(ss*11,7,14),(ss*9,-6,19),(ss*8,-9,31)]
  ft('Ankle_Blue_Side_Cheek_%s.'%ss+side,pp,(-s*ss*2,0,0),'blue',.26)
  ft('Heel_Upper_Layer_%s.'%ss+side,[(ss*9,7,30),(ss*11,13,25),(ss*12,20,10),(ss*10,15,7),(ss*8,6,18)],(-s*ss*2,0,0),'blue',.22)
  ft('Heel_Lower_Layer_%s.'%ss+side,[(ss*9,12,12),(ss*12,18,12),(ss*13,21,3),(ss*8,21,3)],(-s*ss*2,0,0),'navy',.2)
  bolt('Ankle_Side_Catch_%s.'%ss+side,foot((ss*12,4,33)),(s*ss,0,0),.9,'gold')
 # Open inverted-U ceramic cuff straddles the shin, leaving its center visibly mechanical.
 vv=[(-9,-11,52),(-6,-15,56),(6,-15,56),(9,-11,52),(-11,-8,38),(-7,-11,35),(-5,-14,48),(5,-14,48),(7,-11,35),(11,-8,38)]
 skin('Ankle_Open_Ceramic_Horseshoe.'+side,[foot(p) for p in vv],[(0,1,6,5,4),(1,2,7,6),(2,3,9,8,7)],(0,3,0),'white',.3)
 ft('Ankle_Ceramic_Top_Inset.'+side,[(-4,-15.4,55),(4,-15.4,55),(4,-14.8,50),(-4,-14.8,50)],(0,1,0),'silver',.14)
 piston('Ankle_Anterior_Tendon.'+side,foot((0,-9,43)),foot((0,-17,22)),1.8)
 # Swept blue instep, white toe claws, fine dark split line and exposed machined toe hinge.
 vv=[(-8,-15,23),(0,-18,28),(8,-15,23),(-8,-28,12),(0,-31,17),(8,-29,12),(-7,-35,4),(7,-37,4)]
 skin('Foot_Folded_Blue_Instep.'+side,[foot(p) for p in vv],[(0,1,4,3),(1,2,5,4),(3,4,6),(4,5,7,6)],(0,1,-2),'blue',.25)
 ft('Foot_Upper_Blue_Overlap.'+side,[(-5,-16,25),(0,-19,28),(5,-16,25),(5,-24,19),(0,-28,20),(-5,-24,19)],(0,.6,-.8),'panel',.15)
 for ss in (-1,1):
  ft('Foot_Side_Break_%s.'%ss+side,[(ss*8,-16,22),(ss*12,-18,15),(ss*13,-25,7),(ss*10,-30,3),(ss*8,-25,10)],(-s*ss*2,0,0),'blue',.23)
  pp=[(ss*.9,-33,10),(ss*6.5,-31,12),(ss*9,-34,6),(ss*8,-37,2),(ss*.9,-38,2)]
  ft('Foot_White_Split_Toe_%s.'%ss+side,pp,(0,2,0),'white',.2)
  ft('Toe_Machined_Silver_Insert_%s.'%ss+side,[(ss*2,-38.2,2.4),(ss*6,-37.1,2.4),(ss*6,-35,7.1),(ss*2,-36.6,6.6)],(0,.8,0),'titanium',.1)
  rod('Toe_Articulation_Pin_%s.'%ss+side,foot((ss*7,-26,8)),foot((ss*11,-26,8)),1.4,'titanium',n=20)
 for i,y in enumerate((-27,-18,-8,4,14)):
  for ss in (-1,1):ft('Sole_Side_Tread_%s_%d.'%(ss,i)+side,[(ss*11.3,y,1),(ss*11.3,y+3,1),(ss*11.5,y+3,3),(ss*11.5,y,3)],(-s*ss*.6,0,0),'steel',.06)
 for x,y,z in ((-8,-24,13),(8,-24,13),(-7,-13,43),(7,-13,43)):
  bolt('Foot_Captive_Pin_%s_%s.'%(x,z)+side,foot((x,y,z)),(0,-1,0),.5)
