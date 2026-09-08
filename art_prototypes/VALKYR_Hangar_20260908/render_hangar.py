import bpy
import sys
from pathlib import Path
out=Path(__file__).resolve().parent
scene=bpy.data.scenes['VALKYR | Pre-sortie Hangar'];bpy.context.window.scene=scene
args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else ['preview']
mode=args[0]
shots=[('01 | Pre-sortie hero - UI room at right','01_PRESORTIE_HERO',1600,1000,1),
       ('02 | Complete maintenance bay','02_BAY_OVERVIEW',1600,1000,1),
       ('03 | Head and chest inspection','03_HEAD_CHEST',1400,1100,1),
       ('04 | Backpack and hardpoints','04_REAR_EQUIPMENT',1500,1100,1)]
if mode=='preview':shots=[(shots[0][0],'00_PREVIEW',1000,625,1)]
elif mode=='orbit':shots=[('05 | 360 degree inspection','ORBIT_%03d'%f,800,600,f) for f in (1,31,61,91)]
elif mode!='all':shots=[s for s in shots if s[1].startswith(mode)]
scene.cycles.samples=20 if mode in ('preview','orbit') else 48
scene.cycles.use_denoising=True
for name,file,w,h,frame in shots:
    scene.camera=bpy.data.objects[name];scene.frame_set(frame)
    scene.render.resolution_x=w;scene.render.resolution_y=h
    scene.render.filepath=str(out/'renders'/f'{file}.png')
    print('RENDER_START',file,flush=True)
    bpy.ops.render.render(write_still=True)
    print('RENDER_DONE',file,flush=True)
