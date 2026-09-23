"""Read the company Blender scene and export static game batches without saving the source."""
import bpy, pathlib, json, sys
root = pathlib.Path(__file__).resolve().parents[1]
source = root.parent.parent / 'art_prototypes/LunarBase_Sector01_20260908/MARE07_LUNAR_BASE_MASTER.blend'
out = root / 'Assets/Art/LunarBaseV9'
out.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(source))
scene = bpy.context.scene
deps = bpy.context.evaluated_depsgraph_get()
records, batches = [], []
for mat in bpy.data.materials:
    color, emission, metallic, roughness = list(mat.diffuse_color), [0,0,0], 0, .7
    if mat.use_nodes:
        nodes = list(mat.node_tree.nodes)
        bs = next((n for n in nodes if n.type == 'BSDF_PRINCIPLED'), None)
        if bs:
            color = list(bs.inputs['Base Color'].default_value)
            if bs.inputs['Base Color'].is_linked:
                ramp = next((n for n in nodes if n.type == 'VALTORGB'), None)
                if ramp: color = list(ramp.color_ramp.evaluate(.52))
            metallic = bs.inputs['Metallic'].default_value
            roughness = bs.inputs['Roughness'].default_value
            strength = bs.inputs['Emission Strength'].default_value
            emission = [v*min(strength,2) for v in bs.inputs['Emission Color'].default_value[:3]]
    records.append(dict(name=mat.name,color=color,emission=emission,metallic=metallic,roughness=roughness))
export_scene = bpy.data.scenes.new('MARE07_Game_Export')
for coll in list(bpy.data.collections):
    if not coll.name[:2].isdigit() or int(coll.name[:2]) >= 90: continue
    parts=[]
    for obj in list(coll.objects):
        if obj.type not in {'MESH','FONT','CURVE'} or obj.hide_render: continue
        mesh=bpy.data.meshes.new_from_object(obj.evaluated_get(deps), preserve_all_data_layers=False, depsgraph=deps)
        clone=bpy.data.objects.new(obj.name+'_game',mesh)
        clone.matrix_world=obj.matrix_world.copy()
        export_scene.collection.objects.link(clone);parts.append(clone)
    if not parts: continue
    bpy.context.window.scene=export_scene
    bpy.ops.object.select_all(action='DESELECT')
    for obj in parts: obj.select_set(True)
    bpy.context.view_layer.objects.active=parts[0]
    bpy.ops.object.join()
    joined=parts[0]; joined.name='MARE07_'+coll.name[:2]
    batches.append(dict(name=joined.name,vertices=len(joined.data.vertices),polygons=len(joined.data.polygons)))
bpy.context.window.scene=export_scene
bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.fbx(filepath=str(out/'MARE07_Game.fbx'), use_selection=True, object_types={'MESH'},
    apply_unit_scale=True, bake_space_transform=False, axis_forward='-Z', axis_up='Y',
    use_mesh_modifiers=True, add_leaf_bones=False, bake_anim=False, path_mode='AUTO')
(out/'materials.json').write_text(json.dumps(dict(materials=records),indent=2),encoding='utf8')
(out/'export.json').write_text(json.dumps(dict(source=str(source),batches=batches,source_modified=False),indent=2),encoding='utf8')
print('LUNAR_EXPORT_PASS',len(batches),sum(b['vertices'] for b in batches))
