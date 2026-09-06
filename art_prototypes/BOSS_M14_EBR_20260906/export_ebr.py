"""Make portable geometry/PBR exports from the editable master, without changing it."""
import bpy, pathlib, json, hashlib, math
from mathutils import Vector, Matrix
D=pathlib.Path(__file__).resolve().parent
source=D/'BOSS_M14_EBR_MASTER.blend';sha=hashlib.sha256(source.read_bytes()).hexdigest()
bpy.ops.wm.open_mainfile(filepath=str(source));sc=bpy.context.scene
bpy.context.preferences.filepaths.temporary_directory='D:/Tools/BlenderUserData/Temp'
bpy.context.view_layer.update()
root=bpy.data.objects['BOSS_M14_EBR_ROOT']
original=[o for o in root.children_recursive if o.type in ('MESH','CURVE','FONT') and not o.hide_render]
dg=bpy.context.evaluated_depsgraph_get()
mat_map={}
for o in original:
    for m in o.data.materials:
        if m is None or m.name in mat_map:continue
        src=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
        dest=bpy.data.materials.new(m.name+'_PBR');dest.use_nodes=True
        bs=next(n for n in dest.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
        for key in ('Base Color','Metallic','Roughness','Emission Color','Emission Strength'):
            bs.inputs[key].default_value=src.inputs[key].default_value
        dest.diffuse_color=m.diffuse_color;dest.metallic=m.metallic;dest.roughness=m.roughness
        dest['procedural_surface_preserved_in_master_only']=any(n.type=='TEX_NOISE' for n in m.node_tree.nodes)
        mat_map[m.name]=dest
col=bpy.data.collections.new('Portable Asset');sc.collection.children.link(col)
groups={};bounds=[]
for o in original:
    me=bpy.data.meshes.new_from_object(o.evaluated_get(dg),preserve_all_data_layers=True,depsgraph=dg)
    me.transform(o.matrix_world)
    bounds.extend(v.co.copy() for v in me.vertices)
    for i,m in enumerate(list(me.materials)):
        if m:me.materials[i]=mat_map[m.name]
    ob=bpy.data.objects.new('EXPORT_'+o.name,me);col.objects.link(ob);ob.parent=o.parent
    ob.matrix_world=Matrix.Identity(4)
    groups.setdefault(o.parent.name,[]).append(ob)
for o in original:bpy.data.objects.remove(o,do_unlink=True)
joined=[]
for parent,parts in groups.items():
    bpy.ops.object.select_all(action='DESELECT')
    for o in parts:o.select_set(True)
    bpy.context.view_layer.objects.active=parts[0]
    bpy.ops.object.join()
    ob=bpy.context.object;ob.name=parent+'_MESH'
    tri=ob.modifiers.new('Portable triangles','TRIANGULATE');tri.keep_custom_normals=True
    bpy.ops.object.modifier_apply(modifier=tri.name)
    ob['subassembly']=parent
    joined.append(ob)
points=[o.matrix_world@v.co for o in joined for v in o.data.vertices]
lo=Vector([min(p[k] for p in points) for k in range(3)]);hi=Vector([max(p[k] for p in points) for k in range(3)])
sockets={o.name:list(o.matrix_world.translation) for o in root.children_recursive if o.name.startswith('SCK_')}
E=D/'exports';E.mkdir(exist_ok=True)
bpy.ops.object.select_all(action='DESELECT')
eligible=[root]+[o for o in root.children_recursive if o.type in ('MESH','EMPTY')]
for o in eligible:o.select_set(True)
bpy.context.view_layer.objects.active=root
bpy.ops.export_scene.gltf(filepath=str(E/'BOSS_M14_EBR.glb'),export_format='GLB',use_selection=True,export_apply=True,export_extras=True,export_animations=False)
bpy.ops.export_scene.fbx(filepath=str(E/'BOSS_M14_EBR.fbx'),use_selection=True,object_types={'MESH','EMPTY'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL',axis_forward='-Z',axis_up='Y',bake_anim=False,add_leaf_bones=False,use_custom_props=True)
report={'source_sha256':sha,'source_unchanged':hashlib.sha256(source.read_bytes()).hexdigest()==sha,
        'mesh_subassemblies':len(joined),'triangles':sum(len(o.data.polygons) for o in joined),
        'materials':len(mat_map),'dimensions_m':list(hi-lo),'sockets':sockets,
        'geometry_decimation':False,'surface_export':'Base PBR colors/roughness/metals/emission. Blender procedural micro-surface remains in editable master; not texture-baked.',
        'exports':{p.name:{'bytes':p.stat().st_size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in E.iterdir() if p.suffix.lower() in ('.glb','.fbx')}}
assert report['source_unchanged']
(D/'export_manifest.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
# Reimport both portable files to check complete geometry and attachment markers.
checks=[]
for name in ('BOSS_M14_EBR.glb','BOSS_M14_EBR.fbx'):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    if name.endswith('.glb'):bpy.ops.import_scene.gltf(filepath=str(E/name))
    else:bpy.ops.import_scene.fbx(filepath=str(E/name))
    bpy.context.view_layer.update()
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
    ps=[o.matrix_world@v.co for o in meshes for v in o.data.vertices]
    rlo=Vector([min(p[k] for p in ps) for k in range(3)]);rhi=Vector([max(p[k] for p in ps) for k in range(3)])
    err=max(abs((rhi-rlo)[k]-(hi-lo)[k]) for k in range(3))
    missing=[n for n in sockets if bpy.data.objects.get(n) is None]
    assert not missing,(name,missing)
    assert err<1e-4,(name,err)
    assert all(math.isfinite(c) for p in ps for c in p)
    checks.append({'file':name,'mesh_objects':len(meshes),'dimension_error_m':err,'all_sockets_present':True})
(D/'reimport_check.json').write_text(json.dumps(checks,indent=2),encoding='utf-8')
print('EBR_EXPORT_AND_REIMPORT_OK',json.dumps({'export':report,'checks':checks}),flush=True)

