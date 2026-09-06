from body_common import *
group('01 Chest and abdomen','Thorax')
# Compact thorax cage terminates under the accepted neck, with two sloping clavicles.
hull('Thoracic_Upper_Bulkhead',[(-27,-5,340),(27,-5,340),(-20,12,344),(20,12,344),(-23,-9,307),(23,-9,307),(-15,12,304),(15,12,304)],'frame',.45)
ring('Neck_Recess_Mating_Rim',(0,2,344.3),(0,2,347.0),7,5.8,'titanium')
for s,side in ((1,'L'),(-1,'R')):
 mi=lambda p:mirror(p,s)
 rod('Clavicle_Load_Axle.'+side,(s*25,2,336),(s*62,2,334),4,'frame',n=24)
 ring('Clavicle_Gold_Collar.'+side,(s*43,2,334.5),(s*45,2,334.5),4.6,3.8,'gold')
 v=[(15,-14,343),(25,-8,345),(42,-5,337),(42,-16,325),(18,-25,329),(15,-25,337), (31,-19,337),(28,12,339),(43,10,334)]
 skin('Sloping_Clavicle_Shell.'+side,mi(v),[(0,1,6,5),(1,2,3,6),(3,4,5,6),(1,7,8,2)],(-s*.6,2,-1),'blue',.25)
 front('Clavicle_Facing.'+side,[(18,168),(23,168),(42,174),(41,181),(17,175)],-23,2,'panel',s)
 strip('Clavicle_Trim.'+side,mi([(17,-23.2,334),(41,-18,327)]),.6,'gold')
 # High oblique gold intake and separate lower cooling cartridge.
 outer=mi([(16,-27,509-181),(39,-21,509-174),(42,-21,509-195),(19,-27,509-204)])
 inner=mi([(19,-27.2,509-184),(36,-22,509-179),(37.5,-22,509-188),(20.5,-27.2,509-194)])
 recess('Pectoral_Oblique_Intake.'+side,outer,inner,3.4,'navy')
 strip('Gold_Intake_Lower_Lip.'+side,[inner[3],inner[2]],1.65,'gold',(0,.7,0))
 strip('Gold_Intake_Upper_Lip.'+side,[inner[0],inner[1]],.8,'gold',(0,.7,0))
 for j in range(2):
  a=Vector(inner[0]).lerp(Vector(inner[3]),.29+j*.23)+Vector((0,1.8,0));b=Vector(inner[1]).lerp(Vector(inner[2]),.29+j*.23)+Vector((0,1.8,0))
  strip('Intake_Inset_Baffle_%d.'%j+side,[a,b],.65,'titanium',(0,1,0))
 front('Lower_Intake_Armor.'+side,[(20,200),(39,192),(40,207),(24,214),(19,210)],-22.5,5,'blue',s)
 front('Lower_Intake_Well.'+side,[(23,201),(36,196),(37,204),(25,209)],-22.8,1,'black',s)
 for j in range(2):strip('Lower_Intake_Baffle_%d.'%j+side,mi([(24,-23,306-j*3),(35,-23,310-j*3)]),.85,'steel')
 # Segmented side ribs leave the narrow black abdominal drive exposed.
 for j in range(2):
  v=[(12+j*1.5,-22+j,509-203-j*11),(18+j*1.8,-20+j,509-198-j*11),(21+j,-13,509-205-j*11),(13+j,-18,509-217-j*11),(10+j,-21,509-211-j*11)]
  plate('Ceramic_Oblique_Rib_%d.'%j+side,mi(v),(0,3,0),'white',.28)
 plate('Thorax_Side_Wrapped_Armor.'+side,mi([(35,-11,326),(44,0,327),(38,15,317),(27,15,296),(27,3,300),(31,-10,315)]),(-s*3,1,0),'navy',.3)
 plate('Axillary_Floating_Blade.'+side,mi([(38,3,309),(43,8,308),(36,15,284),(31,11,292)]),(-s*2,1,1),'blue',.2)
 piston('Anterior_Rib_Tendon.'+side,(s*17,-8,309),(s*12,-10,278),1.4)
 bolt('Clavicle_Armor_Pin.'+side,(s*36,-20,337),r=.6)
 # Back thoracic return has the same segmented armor vocabulary.
 plate('Rear_Scapular_Plate.'+side,mi([(9,15,339),(31,15,336),(39,15,322),(24,16,312),(11,16,318)]),(0,-3,0),'navy',.22)
 plate('Rear_Rib_Ceramic.'+side,mi([(18,15,309),(31,14,316),(32,13,306),(20,14,296)]),(0,-2,0),'white',.22)
# Central V plate is a folded shell, not an oversized rectangular chest block.
v=[(-13,-23,339),(0,-20,344),(13,-23,339),(15,-25,327),(7,-25,297),(-7,-25,297),(-15,-25,327),(0,-32,328),(0,-30,297)]
skin('Sternum_Folded_V_Keel',v,[(0,1,7,6),(1,2,3,7),(6,7,8,5),(7,3,4,8)],(0,3,0),'blue',.22)
front('Sternum_Crown_Secondary', [(-12,171),(-7,170),(0,174),(7,170),(12,171),(10,181),(0,185),(-10,181)],-30.8,1.2,'panel',bevel=.12)
for s in (-1,1):
 strip('Sternum_Gold_Chevron'+str(s),[(s*2,-31.3,323),(s*12,-29,331)],.6,'gold')
 front('Sternum_Deep_Facet'+str(s),[(s*.9,188),(s*10,186),(s*7,212),(s*.9,212)],-30.2,.8,'navy',bevel=.1)
front('Sternum_Lower_Titanium_Key',[(-6,216),(6,216),(4,222),(-4,222)],-24,2,'steel')
marker('Chest_Warning_Tick',(0,-31.5,323),1.6)
group('02 Waist and pelvis','Waist')
for j in range(3):
 z=292-j*8
 box('Abdominal_Intervertebral_Core_%02d'%j,(0,-9,z),(12,12,6),'frame',.5)
 front('Abdominal_Center_Lock_%02d'%j,[(-6,509-z-2),(6,509-z-2),(5,509-z+3),(-5,509-z+3)],-17,2,'steel' if j==1 else 'black')
front('Abdominal_Main_Ceramic',[(-10,220),(-7,219),(7,219),(10,220),(8,238),(5,241),(-5,241),(-8,238)],-22,4,'white',bevel=.35)
front('Abdominal_Center_Facet',[(-5,224),(5,224),(4,237),(-4,237)],-22.5,1,'silver',bevel=.15)
for s,side in ((1,'L'),(-1,'R')):
 plate('Waist_Lateral_Ceramic.'+side,mirror([(18,-8,290),(23,-2,287),(21,3,273),(15,-5,272),(14,-10,278)],s),(0,2,0),'white',.25)
 piston('Waist_Front_Piston.'+side,(s*16,-10,296),(s*13,-9,269),1.15)
 bearing('Waist_Lateral_Pivot.'+side,(s*18,1,272),(1,0,0),3.5,4)
 bolt('Abdominal_Fastener.'+side,(s*6,-22.6,286),r=.5)
group('02 Waist and pelvis','Pelvis')
front('Belt_Central_Buckle',[(-7,242),(7,242),(5,250),(-5,250)],-21,3,'white',bevel=.25)
front('Belt_Gold_Catch',[(-3,244),(3,244),(2.3,249),(-2.3,249)],-21.3,1,'gold')
for s,side in ((1,'L'),(-1,'R')):
 mi=lambda p:mirror(p,s)
 hull('Iliac_Sloped_Core.'+side,mi([(7,-9,256),(19,-13,266),(39,-7,272),(43,9,267),(26,13,251),(14,5,242)]),'frame',.4)
 plate('Blue_Iliac_Belt.'+side,mi([(9,-21,257),(16,-20,269),(36,-15,276),(43,-10,271),(39,-15,260),(17,-24,249)]),(0,3,0),'blue',.25)
 plate('Iliac_Upper_Overlay.'+side,mi([(17,-21,268),(36,-16,274),(39,-15,267),(20,-23,260)]),(0,1.2,0),'panel',.14)
 strip('Iliac_White_Upper_Seam.'+side,mi([(17,-21.4,270),(36,-16.4,276)]),.55,'silver')
 piston('Exposed_Hip_Actuator.'+side,(s*33,-2,260),(s*40,-3,226),1.7)
 bolt('Hip_Armor_Catch.'+side,(s*36,-17,270),r=.7,mat='gold')
group('02 Waist and pelvis','Pelvis')
v=[(-7,-23,258),(7,-23,258),(13,-20,247),(6,-23,211),(-6,-23,211),(-13,-20,247),(0,-27,249),(0,-26,213)]
skin('Pelvis_Long_Central_Codpiece',v,[(0,1,6),(1,2,3,7,6),(0,6,7,4,5),(3,4,7)],(0,3,0),'blue',.22)
front('Pelvis_Central_Inset',[(-4,262),(4,262),(3.2,285),(-3.2,285)],-27.2,.8,'navy',bevel=.1)
front('Pelvis_Terminal_Cap',[(-3,293),(3,293),(2.1,299),(-2.1,299)],-26.6,1,'silver')
for x,v in ((0,267),(0,282),(-7,281),(7,281)):
 bolt('Pelvis_Small_Key_%s_%s'%(x,v),(x,-27,509-v),r=.43,mat='gold')
for s,side in ((1,'L'),(-1,'R')):
 group('02 Waist and pelvis','Skirt_Hinge.'+side);mi=lambda p:mirror(p,s)
 # Long narrow side skirts, open around the hip rather than broad blocky front aprons.
 v=[(36,4,271),(44,7,269),(58,16,231),(72,22,200),(61,22,213),(43,14,248)]
 skin('Hip_Swept_Side_Skirt.'+side,mi(v),[(0,1,5),(1,2,3,4,5)],(-s*2,-1,0),'blue',.2)
 strip('Skirt_Gold_Outer_Key.'+side,mi([(44,5,266),(63,20,216)]),.8,'gold')
 plate('Skirt_White_Edge.'+side,mi([(54,14,236),(61,16,228),(73,22,199),(66,21,210)]),(-s*.7,-.5,0),'white',.16)
 # Rear articulated skirt reaches below the codpiece in the orthographic drawing.
 v=[(6,15,254),(24,16,254),(30,25,217),(15,29,165),(7,27,174),(7,21,217)]
 skin('Rear_Tail_Skirt.'+side,mi(v),[(0,1,2,5),(2,3,4,5)],(-s*1,-2,0),'navy',.2)
 plate('Rear_Tail_Blue_Facet.'+side,mi([(10,28,215),(24,26,224),(17,30,175),(12,30,176)]),(0,-1.2,0),'blue',.16)

for s,side in ((1,'L'),(-1,'R')):
 group('03 Swept shoulder armor','Shoulder_Armor_Floating_Pivot.'+side);mi=lambda p:mirror(p,s)
 bearing('Shoulder_Shell_Suspension.'+side,(s*56,0,340),(1,0,0),6,6)
 piston('Shoulder_Floating_Link.'+side,(s*50,4,342),(s*65,6,357),1.2)
 # Upper wing, upturned outer point and a wrapped inner return.
 v=[(43,-11,354),(57,-8,365),(103,5,390),(96,-2,371),(54,-17,349), (49,16,351),(58,16,363),(100,14,387),(94,13,369)]
 skin('Shoulder_Upper_Swept_Wing.'+side,mi(v),[(0,1,3,4),(1,2,3),(1,6,7,2),(2,7,8,3),(0,5,6,1)],(-s*.5,1,-1.6),'blue',.2)
 strip('Shoulder_Wing_Silver_Leading_Edge.'+side,mi([(57,-8.3,365),(103,4.7,390)]),.6,'silver')
 plate('Shoulder_Wing_Dark_Insert.'+side,mi([(90,-2,370),(98,1,379),(94,1,362),(86,-7,346)]),(-s*.5,1,0),'black',.12)
 strip('Shoulder_Gold_Outer_Spar.'+side,mi([(98,.3,378),(90,-3,360),(87,-6,349)]),.8,'gold')
 # Large trapezoid badge plate and shallow manufactured face, exactly separate from the top wing.
 poly=[(48,154),(82,140),(91,146),(82,178),(51,196),(45,180)]
 front('Shoulder_Badge_Shell.'+side,poly,-21,13,'navy',s,bevel=.3)
 front('Shoulder_Badge_Facing.'+side,[(49,156),(81,143),(87,148),(79,176),(52,191),(48,179)],-22,1.7,'blue',s)
 front('Shoulder_Badge_Upper_Inset.'+side,[(51,157),(81,145),(83,151),(52,163)],-23.0,.8,'panel',s,bevel=.12)
 strip('Shoulder_Badge_Lower_Seam.'+side,mi([(52,-23.1,321),(79,-23.1,336)]),.5,'edge')
 if side=='L':text_mesh('Shoulder_Unit_01','01',(s*66,-23.05,341),17.0)
 else:
  # Faceted wing mark from the supplied VALKYR emblem, built as small white mesh polygons.
  for ss in (-1,1):
   pp=[(ss*0,-23.15,344),(ss*11,-23.15,352),(ss*7,-23.15,344),(ss*3,-23.15,339)]
   pp=[(s*66+x,y,z) for x,y,z in pp];plate('Shoulder_Valkyr_Emblem_'+str(ss),pp,(0,.12,0),'mark',.02)
  text_mesh('Shoulder_Valkyr_Wordmark','VALKYR',(s*66,-23.08,334),3.4)
 for x,v in ((51,157),(81,151),(55,185),(78,176)):
  marker('Shoulder_Corner_Tick_%s.'%v+side,(s*x,-23.25,509-v),2)
 for j in range(2):
  v=[(84-j*5,3,349-j*19),(100-j*5,10,365-j*32),(96-j*5,16,340-j*23),(84-j*5,9,325-j*13),(77-j*5,2,331-j*15)]
  plate('Shoulder_Outer_Cascade_%d.'%j+side,mi(v),(-s*2,-1,0),'blue' if j==0 else 'navy',.22)
 v=[(48,13,355),(78,15,369),(88,17,354),(74,21,332),(52,16,325)]
 plate('Shoulder_Rear_Badge_Shell.'+side,mi(v),(0,-4,0),'blue',.23)
 plate('Shoulder_Rear_Inset.'+side,mi([(51,16.1,349),(78,18.1,362),(75,20.1,351),(55,17.1,340)]),(0,-1.5,0),'panel',.17)
 for x,z in ((51,349),(77,359),(55,333)):
  bolt('Shoulder_Rear_Pin_%d.'%x+side,(s*x,21,z),(0,1,0),.55)
