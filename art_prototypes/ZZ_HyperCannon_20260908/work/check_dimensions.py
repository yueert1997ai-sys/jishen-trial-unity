import bpy,json
from pathlib import Path
from mathutils import Vector
col=bpy.data.collections['81 | VALKYR V3 fitting source']
obs=[o for o in col.objects if o.type=='MESH' and all(k not in o.name for k in ('Blade','Cannon','Backpack'))]
pts=[o.matrix_world@Vector(v) for o in obs for v in o.bound_box]
result={'body_height':max(p.z for p in pts)-min(p.z for p in pts),'zmin':min(p.z for p in pts),'zmax':max(p.z for p in pts),'xmin':min(p.x for p in pts),'xmax':max(p.x for p in pts)}
Path(__file__).with_name('body-dimensions.json').write_text(json.dumps(result,indent=2))
print(json.dumps(result))
