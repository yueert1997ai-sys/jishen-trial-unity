import bpy,pathlib,sys
W=pathlib.Path(__file__).resolve().parent;sys.path.insert(0,str(W))
bpy.ops.wm.open_mainfile(filepath=str(W/'iteration_03/ASSEMBLED.blend'))
import mass_arms
from mass_ops import save
save(4)
