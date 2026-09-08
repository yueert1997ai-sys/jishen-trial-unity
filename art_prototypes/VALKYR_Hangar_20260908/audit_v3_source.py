import bpy
import json
from pathlib import Path
from mathutils import Vector

out=Path(__file__).resolve().parent
objects=list(bpy.context.scene.objects)
rows=[]
for ob in objects:
    if ob.type not in ('MESH','EMPTY'): continue
    row={'name':ob.name,'type':ob.type,'parent':ob.parent.name if ob.parent else None,'hidden':ob.hide_render}
    if ob.type=='MESH':
        pts=[ob.matrix_world@Vector(v) for v in ob.bound_box]
        row.update(vertices=len(ob.data.vertices),bounds=[[min(p[i] for p in pts) for i in range(3)],[max(p[i] for p in pts) for i in range(3)]])
    rows.append(row)
report={'file':bpy.data.filepath,'collections':{c.name:len(c.all_objects) for c in bpy.data.collections},'objects':rows}
(out/'v3_source_inventory.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report,indent=2))
