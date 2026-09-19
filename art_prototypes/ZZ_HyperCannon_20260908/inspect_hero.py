import bpy, json
from pathlib import Path
ROOT=Path(__file__).resolve().parent
source=ROOT.parent/'JishenTrial_Hero_Assembly_V3/body_mass_refinement_20260906/iteration_v3/game_update/VALKYR_V3_GAME.blend'
with bpy.data.libraries.load(str(source),link=False) as (a,b):b.objects=list(a.objects)
col=bpy.data.collections.new('81 | VALKYR V3 fitting source');bpy.context.scene.collection.children.link(col)
for o in b.objects:
    if o:col.objects.link(o)
bpy.context.view_layer.update()
records=[]
for o in col.objects:
    if o.type=='EMPTY' or any(k in o.name for k in ('Socket','socket','Hand','Forearm','UpperArm','Revision')):
        records.append({'name':o.name,'type':o.type,'parent':o.parent.name if o.parent else None,'position':list(o.matrix_world.translation),'rotation':list(o.rotation_euler),'scale':list(o.scale)})
(ROOT/'work/hero-inventory.json').write_text(json.dumps(records,indent=2),encoding='utf-8')
print(json.dumps(records,indent=2))
