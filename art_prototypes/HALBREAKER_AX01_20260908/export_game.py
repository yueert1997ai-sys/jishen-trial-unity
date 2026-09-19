"""Export evaluated copies of the approved master; never save or alter the source."""
import bpy, math, json, hashlib
from pathlib import Path
from mathutils import Matrix, Vector
ROOT=Path(__file__).resolve().parent
OUT=ROOT/'game_ready';OUT.mkdir(exist_ok=True)
source=ROOT/'VALKYR_AX01_SHOULDER_MASTER.blend'
source_hash=hashlib.sha256(source.read_bytes()).hexdigest()
bpy.ops.wm.open_mainfile(filepath=str(source))
weapon=bpy.data.objects['AX01_SHOULDER_CANNON_ROOT']
assert abs(weapon['exposed_barrel_length_m']-2.2)<1e-6
assert 'Flat' in weapon['front_receiver']
grip=bpy.data.objects['AX01_RIGHT_HAND_U_GRIP'].matrix_world.translation.copy()
turn=Matrix.Translation(-grip)
col=bpy.data.collections.new('HALBREAKER export copies');bpy.context.scene.collection.children.link(col)
dg=bpy.context.evaluated_depsgraph_get();copies=[]
for ob in weapon.children_recursive:
    if ob.type not in {'MESH','CURVE','FONT'} or ob.hide_render:continue
    mesh=bpy.data.meshes.new_from_object(ob.evaluated_get(dg),preserve_all_data_layers=True,depsgraph=dg)
    if not mesh.vertices:continue
    mesh.transform(turn@ob.matrix_world)
    copy=bpy.data.objects.new('Export_'+ob.name,mesh);col.objects.link(copy);copies.append(copy)
bpy.ops.object.select_all(action='DESELECT')
for ob in copies:ob.select_set(True)
bpy.context.view_layer.objects.active=copies[0];bpy.ops.object.join()
base=bpy.context.object;base.name='HALBREAKER_LOD0'
root=bpy.data.objects.new('HALBREAKER_AX01',None);col.objects.link(root);base.parent=root
lods=[base]
for index,ratio in [(1,.40),(2,.12)]:
    ob=base.copy();ob.data=base.data.copy();col.objects.link(ob);ob.name='HALBREAKER_LOD'+str(index);ob.parent=root
    dec=ob.modifiers.new('Distance simplification','DECIMATE');dec.ratio=ratio;dec.use_collapse_triangulate=True
    bpy.context.view_layer.objects.active=ob;bpy.ops.object.modifier_apply(modifier=dec.name);lods.append(ob)
markers={'Grip':grip,'Muzzle':bpy.data.objects['AX01_MUZZLE'].matrix_world.translation.copy(),
         'ShoulderAnchor':Vector((grip.x,0,3.20))}
for name,position in markers.items():
    ob=bpy.data.objects.new(name+'_Export',None);col.objects.link(ob);ob.parent=root;ob.location=turn@position
bpy.ops.object.select_all(action='DESELECT')
for ob in col.objects:ob.select_set(True)
bpy.context.view_layer.objects.active=root
bpy.ops.export_scene.fbx(filepath=str(OUT/'HALBREAKER_AX01.fbx'),use_selection=True,object_types={'MESH','EMPTY'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL',bake_anim=False,add_leaf_bones=False)
palette=[]
for m in base.data.materials:
    if not m:continue
    bs=next((n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None) if m.use_nodes else None
    color=list(bs.inputs['Base Color'].default_value) if bs else list(m.diffuse_color)
    emission=list(bs.inputs['Emission Color'].default_value) if bs else [0,0,0,1]
    strength=float(bs.inputs['Emission Strength'].default_value) if bs else 0
    palette.append({'name':m.name,'color':color,'metallic':float(bs.inputs['Metallic'].default_value) if bs else 0,'roughness':float(bs.inputs['Roughness'].default_value) if bs else .5,'emission':[v*strength for v in emission[:3]]})
(OUT/'materials.json').write_text(json.dumps({'materials':palette},indent=2),encoding='utf-8')
stats=[]
for ob in lods:
    ob.data.calc_loop_triangles();stats.append({'name':ob.name,'vertices':len(ob.data.vertices),'triangles':len(ob.data.loop_triangles)})
assert hashlib.sha256(source.read_bytes()).hexdigest()==source_hash
manifest={'source':str(source),'source_sha256':source_hash,'exposed_barrel_m':2.2,'overall_length_m':weapon['overall_length_m'],'lods':stats,'source_parts':len(copies),'sockets_blender':{n:list(turn@v) for n,v in markers.items()},'front_receiver':weapon['front_receiver'],'left_hand_bound':False,'files':{p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in OUT.glob('*') if p.suffix in ('.fbx','.json')}}
(OUT/'export_manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
print('HALBREAKER_EXPORT_PASS',json.dumps(manifest),flush=True)
