import bpy,pathlib,sys
W=pathlib.Path(__file__).resolve().parent;sys.path.insert(0,str(W))
bpy.ops.wm.open_mainfile(filepath=str(W/'iteration_08/ASSEMBLED.blend'))
from mass_ops import *
for s,side in ((1,'L'),(-1,'R')):
 group('06 Tapered shin and calf','Shin.'+side);f=joint_frame('Shin','Foot',side)
 local_plate('Knee_Mass2_Anterior_Sliding_Underlap.'+side,[(-9,-18,-23),(9,-18,-23),(10,-23,-8),(8,-24,2),(-8,-24,2),(-10,-23,-8)],f,(0,3,0),'silver',.28)
 local_plate('Knee_Mass2_Upper_Blue_Sliding_Armor.'+side,[(-7,-20,-20),(7,-20,-20),(8,-22.8,-10),(6,-23.6,-6),(-6,-23.6,-6),(-8,-22.8,-10)],f,(0,1.1,0),'navy',.19)
 for ss in (-1,1):
  strip('Knee_Mass2_Crest_Machined_Seam_%d.'%ss+side,[f((ss*7,-25.9,7)),f((ss*3,-28.4,12))],.58,'steel')
save(9)
