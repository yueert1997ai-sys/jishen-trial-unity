"""Read back the actual delivery file; collect artifact evidence without resaving."""
import bpy
import json
import math
import hashlib
from pathlib import Path

out=Path(__file__).resolve().parent
scene=bpy.data.scenes['MARE-07 | Abandoned Lunar Service Yard']
bpy.context.window.scene=scene
meshes=[o for o in scene.objects if o.type=='MESH']
used_materials={m for o in meshes for m in o.data.materials if m}
images=set()
for m in used_materials:
    if m.use_nodes:
        for n in m.node_tree.nodes:
            if n.type=='TEX_IMAGE' and n.image:images.add(n.image)
missing=[]
for im in images:
    if im.source=='FILE' and not im.packed_file and not Path(bpy.path.abspath(im.filepath)).is_file():
        missing.append({'image':im.name,'path':im.filepath})
nonfinite=[o.name for o in scene.objects if any(not math.isfinite(v) for row in o.matrix_world for v in row)]
critical_names=['SELENE-04 | immobilized recovery rover','Detached rover wheel','Dead blue windshield',
                'Damaged parabolic reflector','Dark interior under breach','PRESSURE LOCK | quiet service annex',
                'COLLAPSED CABLE BRIDGE','Sculpted regolith with impact basins','Octagonal maintenance apron',
                'VALKYR scale reference - source preserved']
presence={n:n in scene.objects for n in critical_names}
source=out.parent/'RAIKEN_MkII_20260906/VALKYR_RAIKEN_GAME.blend'
report={
    'opened_file':bpy.data.filepath,'blender':bpy.app.version_string,'active_scene':scene.name,
    'objects':len(scene.objects),'mesh_objects':len(meshes),
    'base_mesh_triangles':sum(sum(len(f.vertices)-2 for f in o.data.polygons) for o in meshes),
    'materials_used':len(used_materials),'image_textures_used':len(images),
    'missing_used_image_textures':missing,'non_finite_transforms':nonfinite,'critical_object_presence':presence,
    'cameras':[o.name for o in scene.objects if o.type=='CAMERA'],
    'startup_camera':scene.camera.name,'hero_source_sha256':hashlib.sha256(source.read_bytes()).hexdigest(),
    'checks_passed':all(presence.values()) and not missing and not nonfinite,
    'scope':'Artifact readback and visual-review support only. No Unity playability or performance validation.'
}
(out/'verification.json').write_text(json.dumps(report,indent=2,ensure_ascii=False))
print(json.dumps(report,indent=2,ensure_ascii=False))
