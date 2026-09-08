"""Render named review cameras from the saved environment, without editing it."""
import bpy
import sys
from pathlib import Path

out = Path(__file__).resolve().parent
scene = bpy.data.scenes['MARE-07 | Abandoned Lunar Service Yard']
bpy.context.window.scene = scene
args = sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else ['preview']
mode = args[0]
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.use_denoising = True
scene.cycles.samples = 20 if mode == 'preview' else 48
scene.render.resolution_percentage = 100
shots = [('01 | Establishing view', '01_ESTABLISHING', 1800, 1350),
         ('02 | Gameplay 60 degree', '02_GAMEPLAY_60DEG', 1800, 1350),
         ('03 | Rover story detail', '03_ROVER_DETAIL', 1600, 1200),
         ('04 | Hangar architecture', '04_HANGAR_DETAIL', 1600, 1200)]
if mode == 'preview':
    shots = [(shots[0][0], '00_PREVIEW', 1100, 825)]
elif mode not in ('all',):
    shots = [s for s in shots if s[1].startswith(mode)]
for cam, name, x, y in shots:
    scene.camera = bpy.data.objects[cam]
    scene.render.resolution_x = x
    scene.render.resolution_y = y
    scene.render.filepath = str(out / 'renders' / (name + '.png'))
    print('RENDER_START', name, flush=True)
    bpy.ops.render.render(write_still=True)
    print('RENDER_DONE', name, flush=True)
