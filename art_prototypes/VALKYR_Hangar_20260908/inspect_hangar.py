import bpy
import json
import math
import hashlib
from array import array
from pathlib import Path
from bpy_extras.object_utils import world_to_camera_view
from mathutils import Vector

out=Path(__file__).resolve().parent
scene=bpy.data.scenes['VALKYR | Pre-sortie Hangar'];bpy.context.window.scene=scene
scene.frame_set(1);bpy.context.view_layer.update()
objects=list(scene.objects)
meshes=[o for o in objects if o.type=='MESH']
materials={m for o in meshes for m in o.data.materials if m}
missing=[]
for mat in materials:
    if not mat.use_nodes:continue
    for n in mat.node_tree.nodes:
        if n.type=='TEX_IMAGE' and n.image:
            im=n.image
            if im.source=='FILE' and not im.packed_file and not Path(bpy.path.abspath(im.filepath)).is_file():missing.append(im.name)
manifest=json.loads((out/'manifest.json').read_text())
source=Path(manifest['source_hero'])
source_same=hashlib.sha256(source.read_bytes()).hexdigest()==manifest['source_hero_sha256']
def mesh_signature(ob):
    values=array('f',[0])*len(ob.data.vertices)*3
    ob.data.vertices.foreach_get('co',values)
    indices=array('i',[0])*len(ob.data.loops)
    ob.data.loops.foreach_get('vertex_index',indices)
    return hashlib.sha256(values.tobytes()+indices.tobytes()).hexdigest()
hero=[o for o in bpy.data.collections['90 | VALKYR - current model copy'].all_objects if o.type=='MESH']
expected=manifest.get('hero_geometry_signatures',{})
geometry_same=bool(expected) and {o.name:mesh_signature(o) for o in hero}==expected
revision_present=bool(bpy.data.objects.get(manifest.get('source_revision','INVALID_REVISION')))
body=[o for o in objects if o.type=='MESH' and o.name.startswith('LOD0_') and 'Blade' not in o.name and not o.hide_render]
corners=[o.matrix_world@Vector(v) for o in body for v in o.bound_box]
main=bpy.data.objects['01 | Pre-sortie hero - UI room at right']
projected=[world_to_camera_view(scene,main,v) for v in corners]
orbit=bpy.data.objects['05 | 360 degree inspection']
orbit_positions={}
for f in (1,31,61,91):
    scene.frame_set(f);bpy.context.view_layer.update()
    orbit_positions[f]=list(orbit.matrix_world.translation)
scene.frame_set(1)
blade=bpy.data.objects['AntiShip_Blade_Display_Root']
unarmed=all(ob.hide_render for ob in [blade,*blade.children_recursive])
relaxed=all(abs(v)<.0001 for v in bpy.data.objects['Forearm.R'].rotation_euler)
report={
 'opened_file':bpy.data.filepath,'scene':scene.name,'objects':len(objects),'mesh_objects':len(meshes),
 'base_triangles':sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in meshes),
 'missing_used_textures':sorted(set(missing)),'hero_source_unchanged':source_same,
 'hero_source':str(source),'hero_revision_marker_present':revision_present,
 'hero_meshes':len(hero),'hero_geometry_matches_appended_v3':geometry_same,
 'default_unarmed':unarmed,'right_forearm_relaxed':relaxed,
 'body_main_camera_bounds':{'x':[min(p.x for p in projected),max(p.x for p in projected)],'y':[min(p.y for p in projected),max(p.y for p in projected)]},
 'orbit_camera_positions':orbit_positions,'orbit_distinct_positions':len({tuple(round(v,3) for v in p) for p in orbit_positions.values()})==4,
 'checks_passed':not missing and source_same and geometry_same and revision_present and len(orbit_positions)==4 and unarmed and relaxed,
 'scope':'Blender artifact integrity, body framing and camera motion; visual renders checked separately. No Unity runtime claims.'
}
(out/'verification.json').write_text(json.dumps(report,indent=2,ensure_ascii=False))
print(json.dumps(report,indent=2,ensure_ascii=False))
