"""One fast read-back of the actual saved .blend; no mesh changes or export."""
from pathlib import Path
import bpy,json
out=Path(__file__).resolve().parent/'stage_01'
bpy.ops.wm.open_mainfile(filepath=str(out/'TYPE_E01_STAGE01.blend'))
head=bpy.data.collections['01 HEAD - review secondary forms']
meshes=[o for o in bpy.data.objects if o.type=='MESH' and o.name!='Studio ground']
packed=[i.name for i in bpy.data.images if i.packed_file]
assert len(meshes)==143
assert len(head.objects)==37
assert packed
assert bpy.data.objects['TYPE_E01_MASTER_ROOT']['review_status'].startswith('STAGE 01')
report={'reopened_saved_blend':True,'editable_meshes':len(meshes),'head_meshes':len(head.objects),'packed_reference':packed,'review_status':'awaiting user approval'}
(out/'quick_reopen_check.json').write_text(json.dumps(report,indent=2),encoding='utf8')
print('QUICK_REOPEN '+json.dumps(report),flush=True)
