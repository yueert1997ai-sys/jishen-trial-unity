from body_common import *
from body_depth_pass import region,clamp
from body_finish import deform
# The aligned side sheet shows a forward-projecting chest wedge and long armored feet.
for o in list(SC.objects):
 if o.type!='MESH':continue
 reg=region(o)
 if reg=='thorax' and not o.name.startswith(('14_','16_','17_','V3B_Neck_')):
  deform(o,lambda p:Vector((p.x,p.y*(1+.38*clamp((348-p.z)/12)) if p.y<0 else p.y,p.z)))
 elif reg=='waist':deform(o,lambda p:Vector((p.x,p.y*1.20 if p.y<0 else p.y,p.z)))
 elif reg=='foot':
  deform(o,lambda p:Vector((p.x,5+(p.y-5)*(1+.29*clamp((43-p.z)/22)) if p.y<5 else 5+(p.y-5)*(1+.20*clamp((43-p.z)/22)),p.z)))
group('01 Chest and abdomen','Thorax')
for s,side in ((1,'L'),(-1,'R')):
 mi=lambda p:mirror(p,s)
 # A sealed pectoral wedge connects the projecting intake to the rib cage.
 v=[(17,-39,337),(33,-26,337),(39,-10,331),(36,11,325),(24,15,306),(18,-21,302),(18,-41,315),(32,-29,314)]
 skin('Pectoral_Closed_Forward_Wedge.'+side,mi(v),[(0,1,7,6),(1,2,3,7),(3,4,5,6,7)],(-s*2,1,0),'navy',.28)
 plate('Pectoral_Lateral_Blue_Carapace.'+side,mi([(32,-24,333),(39,-7,329),(36,10,322),(32,4,313),(27,-13,307),(29,-25,316)]),(-s*2,0,0),'blue',.24)
 plate('Pectoral_Side_Lower_Silver_Layer.'+side,mi([(29,-18,309),(33,-6,313),(30,6,305),(24,8,295),(23,-6,300)]),(-s*1.8,0,0),'silver',.22)
 strip('Pectoral_Side_Machined_Seam.'+side,mi([(36,-8,326),(34,7,319)]),.6,'steel',(-s*.5,0,0))
 bolt('Pectoral_Side_Service_Bolt.'+side,(s*35,-4,318),(s,0,0),.65)
SC['side_profile_fit_pass']=8
