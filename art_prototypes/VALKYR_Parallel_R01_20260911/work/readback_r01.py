import bpy,json,math
from pathlib import Path
OUT=Path(__file__).resolve().parents[1]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(OUT/'exports/VALKYR_PARALLEL_R01.glb'))
meshes=[o for o in bpy.data.objects if o.type=='MESH'];points=[o.matrix_world@v.co for o in meshes for v in o.data.vertices]
report={'file':'exports/VALKYR_PARALLEL_R01.glb','mesh_count_including_text':len(meshes),'vertices':sum(len(o.data.vertices) for o in meshes),'triangles':sum(len(o.data.polygons) for o in meshes),'assembly_root_present':'VALKYR_PARALLEL_R01_ROOT' in bpy.data.objects,'head_mount_present':'HEAD_MOUNT' in bpy.data.objects,'invalid_coordinates':sum(not all(math.isfinite(x) for x in p) for p in points),'bounds':{'min':[min(p[k] for p in points) for k in range(3)],'max':[max(p[k] for p in points) for k in range(3)]}}
assert report['assembly_root_present'] and report['head_mount_present'] and not report['invalid_coordinates'] and report['mesh_count_including_text']>=371
(OUT/'work/glb_readback.json').write_text(json.dumps(report,indent=2));print(json.dumps(report),flush=True)
