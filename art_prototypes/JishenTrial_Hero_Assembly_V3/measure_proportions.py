import bpy,pathlib,json
from mathutils import Vector
OUT=pathlib.Path(__file__).resolve().parent
bpy.ops.wm.open_mainfile(filepath=str(OUT/'JishenTrial_ASSEMBLED_MASTER.blend'))
def bb(objects):
    pts=[o.matrix_world@v.co for o in objects for v in o.data.vertices]
    return [[round(fn(v[i] for v in pts),6) for i in range(3)] for fn in (min,max)]
for label,parts in (
 ('HEAD_SHELL',[o for o in bpy.context.scene.objects if o.type=='MESH' and o.name.startswith(('01_','02_','03_','04_','10_'))]),
 ('HEAD_JAW',[o for o in bpy.context.scene.objects if o.type=='MESH' and o.name.startswith(('09_','11_'))]),
 ('HEAD_ALL',[o for o in bpy.data.collections['HEAD_V2 | colored editable parts'].all_objects if o.type=='MESH' and int(o.name[:2])<=12]),
 ('BODY_SHOULDERS',[o for o in bpy.context.scene.objects if o.type=='MESH' and 'Shoulder' in o.name and 'Armor' in o.name])):
    print(label,bb(parts),flush=True)
