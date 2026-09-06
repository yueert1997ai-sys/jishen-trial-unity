import bpy,pathlib,sys
W=pathlib.Path(__file__).resolve().parent;sys.path.insert(0,str(W))
bpy.ops.wm.open_mainfile(filepath=str(W/'iteration_04/ASSEMBLED.blend'))
import mass_weapon,mass_pack
from mass_ops import save
save(5)
