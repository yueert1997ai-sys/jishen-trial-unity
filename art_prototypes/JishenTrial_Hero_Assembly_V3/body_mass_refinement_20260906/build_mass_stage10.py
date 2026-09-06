import bpy,pathlib,sys
W=pathlib.Path(__file__).resolve().parent;sys.path.insert(0,str(W))
bpy.ops.wm.open_mainfile(filepath=str(W/'iteration_09/ASSEMBLED.blend'))
from mass_ops import *
remove(['V3B_Knee_Mass2_Anterior_Sliding_Underlap','V3B_Knee_Mass2_Upper_Blue_Sliding_Armor'])
for s,side in ((1,'L'),(-1,'R')):
 group('06 Tapered shin and calf','Shin.'+side);f=joint_frame('Shin','Foot',side)
 # Keep the sliding closure behind the white hood and in front of the internal axle.
 # Its top and bottom ends are hidden under the adjoining armor pieces.
 local_plate('Knee_Mass2_Anterior_Sliding_Underlap.'+side,[(-8,-10,-24),(8,-10,-24),(8,-15,-14),(8,-20,-7),(7,-22,2),(-7,-22,2),(-8,-20,-7),(-8,-15,-14)],f,(0,2.4,0),'silver',.22)
save(10)
