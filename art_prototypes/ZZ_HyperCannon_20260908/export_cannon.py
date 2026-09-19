import bpy, math, json, hashlib
from pathlib import Path
from mathutils import Matrix, Vector

ROOT=Path(__file__).resolve().parent
scene=bpy.context.scene
weapon=bpy.data.objects['HC09_CANNON_ROOT']
grip=bpy.data.objects['Grip'].matrix_world.translation.copy()
turn=Matrix.Rotation(-math.pi/2,4,'Z')@Matrix.Translation(-grip)
dg=bpy.context.evaluated_depsgraph_get()
col=bpy.data.collections.new('80 | Game-ready LODs and sockets');scene.collection.children.link(col)
copies=[]
for ob in weapon.children_recursive:
    if ob.type not in {'MESH','CURVE','FONT'}:continue
    me=bpy.data.meshes.new_from_object(ob.evaluated_get(dg),preserve_all_data_layers=True,depsgraph=dg)
    if len(me.vertices)==0:continue
    me.transform(turn@ob.matrix_world)
    copy=bpy.data.objects.new('Export_'+ob.name,me);col.objects.link(copy);copies.append(copy)
bpy.ops.object.select_all(action='DESELECT')
for ob in copies:ob.select_set(True)
bpy.context.view_layer.objects.active=copies[0];bpy.ops.object.join()
base=bpy.context.object;base.name='TYPE08_LOD0'
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT')
bpy.ops.uv.smart_project(angle_limit=math.radians(66),island_margin=.008)
bpy.ops.object.mode_set(mode='OBJECT')
game_root=bpy.data.objects.new('TYPE08_CANNON',None);col.objects.link(game_root);base.parent=game_root
lods=[base]
for index,ratio in [(1,.48),(2,.18)]:
    ob=base.copy();ob.data=base.data.copy();col.objects.link(ob);ob.name='TYPE08_LOD'+str(index);ob.parent=game_root
    dec=ob.modifiers.new('Distance LOD simplification','DECIMATE');dec.ratio=ratio;dec.use_collapse_triangulate=True
    bpy.context.view_layer.objects.active=ob;bpy.ops.object.modifier_apply(modifier=dec.name);lods.append(ob)
markers=[]
for name in ('Grip','Support','Muzzle','BackMount','Eject'):
    marker=bpy.data.objects.new(name+'_Export',None);col.objects.link(marker);marker.parent=game_root
    marker.location=turn@bpy.data.objects[name].matrix_world.translation
    marker['socket_name']=name;markers.append(marker)
bpy.ops.object.select_all(action='DESELECT')
for ob in [game_root,*lods,*markers]:ob.select_set(True)
bpy.context.view_layer.objects.active=game_root
fbx=ROOT/'exports/TYPE08_CANNON.fbx'
bpy.ops.export_scene.fbx(filepath=str(fbx),use_selection=True,object_types={'MESH','EMPTY'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL',bake_anim=False,add_leaf_bones=False,use_custom_props=True)
for ob in lods[1:]:ob.select_set(False)
bpy.ops.export_scene.gltf(filepath=str(ROOT/'exports/TYPE08_CANNON.glb'),export_format='GLB',use_selection=True,export_apply=True,export_yup=True,export_extras=True)
palette=[]
for m in base.data.materials:
    if not m:continue
    palette.append({'name':m.name,'color':list(m.diffuse_color),'metallic':float(m.get('metallic',0)),'roughness':float(m.get('roughness',.4)),'emission':[v*float(m.get('emission',0)) for v in m.diffuse_color[:3]]})
(ROOT/'exports/materials.json').write_text(json.dumps({'materials':palette},indent=2),encoding='utf-8')
stats=[]
for ob in lods:
    ob.data.calc_loop_triangles()
    stats.append({'name':ob.name,'vertices':len(ob.data.vertices),'triangles':len(ob.data.loop_triangles),'materials':len(ob.data.materials),'uv_layers':len(ob.data.uv_layers)})
manifest={'source':'HC09_CANNON_MASTER.blend','primary_reference':'reference/MECHA_CANNON_Turnaround.png','scale_reference':'reference/ZZ_FullMecha_ScaleReference.png','right_hand':True,'lods':stats,'sockets':{o['socket_name']:list(o.location) for o in markers},'forward':'Blender -Y / Unity +Z after import wrapper','dimensions_m':list(base.dimensions),'files':{p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in (ROOT/'exports').glob('*') if p.is_file() and p.name!='export_manifest.json'}}
(ROOT/'exports/export_manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
for o in col.objects:o.hide_render=True;o.hide_set(True)
bpy.context.view_layer.objects.active=weapon
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'HC09_CANNON_MASTER.blend'))
print('CANNON_EXPORTED',json.dumps(manifest),flush=True)
