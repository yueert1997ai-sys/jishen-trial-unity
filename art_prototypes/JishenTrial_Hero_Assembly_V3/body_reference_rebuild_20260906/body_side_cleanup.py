from body_common import *
from body_limbs import limb_basis
from body_depth_pass import mapping
# These side sheets have been superseded by the closed side-elevation housings.
prefixes=('Upperarm_Ceramic_Outer_Return.','Upperarm_Rear_Ceramic.','Upperarm_Side_Gray_Insert.',
 'Thigh_Outer_Ceramic_Return.','Thigh_Outer_Closed_Ceramic_Volume.','Thigh_Outer_Upper_Silver_Layer.','Thigh_Outer_Lower_Ceramic_Layer.','Thigh_Deep_Silver_Service_Panel.',
 'Forearm_Closed_Outer_Housing.','Forearm_Outer_Overlapping_Panel.','Forearm_Machined_Side_Inset.','Forearm_Deep_Closed_Side_Pod.','Forearm_Deep_Side_Overlapping_Cartridge.',
 'Calf_Closed_Rear_Outer_Carapace.','Calf_Rear_Upper_Blue_Leaf.','Calf_Rear_Middle_Blue_Leaf.','Calf_Rear_White_Highlight.','Calf_Deep_Upper_Overlapping_Plate.','Calf_Deep_Lower_Overlapping_Plate.')
for o in list(SC.objects):
 if o.name.startswith(tuple('V3B_'+p for p in prefixes)):bpy.data.objects.remove(o,do_unlink=True)

for s,side in ((1,'L'),(-1,'R')):
 group('04 Arms and articulated hands','UpperArm.'+side)
 f=limb_basis((s*64,5,330),(s*87,-4,281),s)
 plate('Upperarm_Planar_Side_Service_Panel.'+side,[f(p) for p in [(13,1,24),(13,12,25),(13,13,34),(13,1,36)]],(-s*.9,0,0),'silver',.22)
 for y,t in ((3,26),(11,32)):bolt('Upperarm_Side_Service_Fastener_%s.'%t+side,f((13.2,y,t)),(s,0,0),.5)
 group('04 Arms and articulated hands','Forearm.'+side)
 f=limb_basis((s*87,-4,281),(s*112,-18,226),s)
 # Deliberate planar break lines around the side pod avoid many intersecting small leaves.
 vv=[(8,-11,19),(12,-4,19),(13.5,11,21),(11,21,28),(7,21,49),(7,5,57),(8,-9,50),
     (13.5,11,44),(12,-4,47)]
 skin('Forearm_Solid_Side_Housing.'+side,[f(p) for p in vv],[(0,1,8,6),(1,2,7,8),(2,3,4,7),(7,4,5,8),(8,5,6)],(-s*2,0,0),'blue',.27)
 plate('Forearm_Side_Planar_Cartridge.'+side,[f(p) for p in [(14.2,-2,23),(14.2,10,25),(14.2,10,41),(14.2,2,48),(14.2,-2,42)]],(-s*.8,0,0),'panel',.17)
 plate('Forearm_Side_Dark_Latch.'+side,[f(p) for p in [(14.6,2,29),(14.6,6,30),(14.6,6,36),(14.6,2,37)]],(-s*.5,0,0),'navy',.1)
 for y,t in ((1,26),(7,40)):bolt('Forearm_Side_Cartridge_Fastener_%s.'%t+side,f((14.5,y,t)),(s,0,0),.5)
 group('05 Thigh and knee armor','Thigh.'+side)
 f=limb_basis((s*31,1,242),(s*58,-15,167),s)
 plate('Thigh_Planar_Lateral_Service_Panel.'+side,[f(p) for p in [(14.5,0,34),(14.5,11,34),(14.5,13,48),(14.5,8,56),(14.5,0,53)]],(-s*1,0,0),'silver',.22)
 for y,t in ((2,37),(9,50)):bolt('Thigh_Planar_Panel_Fastener_%s.'%t+side,f((14.7,y,t)),(s,0,0),.5)
 group('06 Tapered shin and calf','Shin.'+side)
 f=limb_basis((s*58,-15,167),(s*93,5,40),s)
 plate('Calf_Planar_Outer_Cartridge.'+side,[f(p) for p in [(18.7,16,32),(18.7,28,35),(18.7,30,45),(18.7,24,54),(18.7,16,49)]],(-s*1,0,0),'panel',.21)
 strip('Calf_Planar_Panel_Leading_Seam.'+side,[f((19,17,33)),f((19,28,36)),f((19,29,44))],.42,'edge',(-s*.3,0,0))
 for y,t in ((18,36),(27,45)):bolt('Calf_Planar_Panel_Fastener_%s.'%t+side,f((19,y,t)),(s,0,0),.55)
 group('07 Feet and ankle couplings','Foot.'+side)
 q=Matrix.Rotation(math.radians(19*s),3,'Z');c=Vector((s*93,2,0))
 def foot(p):return tuple(mapping('foot',c+q@Vector((s*p[0],p[1],p[2]))))
 # Continuous wedge beneath the long instep: no hollow triangular side gap.
 for ss in (-1,1):
  pp=[(ss*.7,-31,2.8),(ss*9.8,-29,2.8),(ss*11,-17,3),(ss*10,-10,8),(ss*7,-14,20),(ss*7,-24,12),(ss*.7,-28,10)]
  hull('Foot_Closed_Forefoot_Wedge_%s.'%ss+side,[foot(p) for p in pp],'blue',.25)
  pp=[(ss*9,-28,4),(ss*11,-22,5),(ss*10.5,-15,9),(ss*8.7,-18,15)]
  plate('Foot_Lower_Side_Overlapping_Armor_%s.'%ss+side,[foot(p) for p in pp],(-s*ss*.8,0,0),'navy',.17)
  bolt('Foot_Lower_Side_Captive_Pin_%s.'%ss+side,foot((ss*10,-20,9)),(s*ss,0,0),.6,'gold')
SC['side_surface_cleanup']='Superseded intersecting side sheets replaced by deliberate closed housings and planar service panels.'
