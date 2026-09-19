import bpy
from pathlib import Path
bpy.ops.wm.open_mainfile(filepath=str(Path(__file__).resolve().parents[1]/'HC09_CANNON_MASTER.blend'))
