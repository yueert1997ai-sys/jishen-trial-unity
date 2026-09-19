"""Freeze R02 into an independent skinned PC asset. The sculpt is read-only."""
from pathlib import Path
import ast, bpy, math, json, hashlib, re, time
from mathutils import Vector

ROOT = Path(__file__).resolve().parent
PROJECT = ROOT.parents[1]
OUT = ROOT / 'game_ready'
ART = PROJECT / 'Assets/Art/Enemies/TypeE01Elite'
OUT.mkdir(exist_ok=True)
ART.mkdir(parents=True, exist_ok=True)
SOURCE = ROOT / 'stage_02_head/TYPE_E01_ELITE_HEAD_R02.blend'
source_hash = hashlib.sha256(SOURCE.read_bytes()).hexdigest()
bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
sc = bpy.context.scene
bpy.context.view_layer.update()
originals = [o for o in sc.objects if o.type == 'MESH' and any(c.name[:2] in ('01','02','03','04','05','06','07') for c in o.users_collection)]
source_count = len(originals)
game = bpy.data.collections.new('GAME - deformable PC export')
sc.collection.children.link(game)

def part_name(name):
    raw = name.replace('ELITE | E01_', '')
    if raw in ('HEAD','CHEST','WAIST','BACKPACK'): return raw.title()
    match = re.match(r'([LR])_(.+)', raw)
    if match:
        return {'SHOULDER':'Shoulder','UPPER_ARM':'UpperArm','FOREARM':'Forearm','HAND':'Hand','THIGH':'Thigh','CALF':'Calf','FOOT':'Foot'}[match[2]] + '.' + match[1]
    return 'Chest'

positions = {part_name(o.name): o.matrix_world.translation.copy() for o in sc.objects if o.name.startswith('ELITE | E01_') and o.type == 'EMPTY'}
parents = {'Waist':'Root', 'Chest':'Waist','Head':'Chest','Backpack':'Chest'}
for side in ('L','R'):
    for name,parent in [('Shoulder','Chest'),('UpperArm','Chest'),('Forearm','UpperArm.'+side),('Hand','Forearm.'+side),('Thigh','Waist'),('Calf','Thigh.'+side),('Foot','Calf.'+side)]:
        parents[name+'.'+side] = parent
positions['Root'] = Vector((0,0,0))

tree = ast.parse((ROOT/'build_boss.py').read_text(encoding='utf-8'))
paths = {}
for node in tree.body:
    if isinstance(node, ast.Assign):
        for target in node.targets:
            if isinstance(target, ast.Name) and target.id in ('primary_paths','fingers'):
                paths[target.id] = ast.literal_eval(node.value)

def world(p): return Vector(p)*1.5 + Vector((0,0,-.084))
chains = {}
for prefix,key,parent in [('Tendril','primary_paths','Backpack'),('Talon','fingers','Forearm.R')]:
    for i,path in enumerate(paths[key]):
        points = [world(p) for p in path]
        names = []
        for j,point in enumerate(points):
            name = f'{prefix}.{i+1:02d}.{j:02d}'
            positions[name] = point
            parents[name] = names[-1] if names else parent
            names.append(name)
        chains[f'{prefix}.{i+1:02d}'] = (points,names)
core = world((.024,-.357,2.302))
for i in range(9):
    name = f'Iris.{i+1:02d}'
    positions[name] = core
    parents[name] = 'Chest'
positions['Iris.Lower'] = core
parents['Iris.Lower'] = 'Chest'

arm_data = bpy.data.armatures.new('E01Elite_Generic_Skeleton')
rig = bpy.data.objects.new('E01Elite_Rig', arm_data)
game.objects.link(rig)
bpy.context.view_layer.objects.active = rig
rig.select_set(True)
bpy.ops.object.mode_set(mode='EDIT')
for name,pos in positions.items():
    bone = arm_data.edit_bones.new(name)
    bone.head = pos
    bone.tail = pos + Vector((0,0,.12))
for name,parent in parents.items():
    arm_data.edit_bones[name].parent = arm_data.edit_bones[parent]
bpy.ops.object.mode_set(mode='OBJECT')
rig.show_in_front = True

def chain_weights(point, chain):
    points,names = chains[chain]
    best = None
    for i,(a,b) in enumerate(zip(points,points[1:])):
        d = b-a
        t = max(0,min(1,(point-a).dot(d)/max(d.length_squared,1e-8)))
        dist = (point-a-d*t).length_squared
        if best is None or dist<best[0]: best=(dist,i,t)
    _,i,t = best
    return [(names[i],1-t),(names[i+1],t)]

def nearest_body(point):
    # Physical scratches are a single source object spanning the entire machine.
    z = (point.z+.084)/1.5
    x = point.x/1.5
    side = 'R' if x<0 else 'L'
    if z>2.90 and abs(x)<.20: return 'Head'
    if abs(x)>.43 and z>1.80: return ('UpperArm.' if z>2.40 else 'Forearm.')+side
    if z<.54: return 'Foot.'+side
    if z<1.35: return 'Calf.'+side
    if z<2.02: return 'Thigh.'+side
    return 'Chest' if z>2.18 else 'Waist'

materials = {}
meshes = []
input_tris = 0
for index,src in enumerate(originals):
    # Two-segment sculpt bevels can be reduced without changing the silhouette.
    for modifier in src.modifiers:
        if modifier.type == 'BEVEL': modifier.segments=1
    deps = bpy.context.evaluated_depsgraph_get()
    me = bpy.data.meshes.new_from_object(src.evaluated_get(deps), preserve_all_data_layers=True, depsgraph=deps)
    me.transform(src.matrix_world)
    ob = bpy.data.objects.new('GAME | '+src.name, me)
    game.objects.link(ob)
    input_tris += sum(len(p.vertices)-2 for p in me.polygons)
    # Preserve the actual edge-distance mask used by R02's burned armor shader.
    attr = me.attributes.get('fracture_edge')
    mask = me.color_attributes.new(name='FractureMask',type='FLOAT_COLOR',domain='CORNER')
    values = []
    for loop in me.loops:
        value = attr.data[loop.vertex_index].value if attr else 0
        values.extend((1,1,1,value))
    mask.data.foreach_set('color',values)
    me.color_attributes.active_color = mask
    me.color_attributes.render_color_index = me.color_attributes.find(mask.name)
    for mat in me.materials:
        if not mat or mat.name in materials: continue
        p = next((n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None) if mat.use_nodes else None
        col = list(p.inputs['Base Color'].default_value) if p else list(mat.diffuse_color)
        emit = list(p.inputs['Emission Color'].default_value) if p else [0,0,0,1]
        strength = p.inputs['Emission Strength'].default_value if p else 0
        materials[mat.name] = {'color':col,'metallic':p.inputs['Metallic'].default_value if p else 0,'smoothness':1-p.inputs['Roughness'].default_value if p else .4,'emission':[v*strength for v in emit[:3]],'living':any(s in mat.name for s in ('liquid','living','cranial','buried red','life veins','energy','recessed core')),'wear':2 if 'ash-grey' in mat.name else 1 if 'worn ivory' in mat.name else 0}
    src_parent = src.parent.name if src.parent else ''
    rigid = part_name(src_parent) if src_parent.startswith('ELITE | E01_') else 'Chest'
    chain = None
    tend = re.match(r'TENDRIL (\d+)',src_parent)
    if tend: chain=f'Tendril.{int(tend[1]):02d}'
    talon = re.search(r'CLAW \| (?:primary talon|talon fluted edge|heated cutting seam) (\d+)', src.name)
    if talon: chain=f'Talon.{int(talon[1]):02d}'
    iris = re.search(r'CORE \| overlapping iris blade (\d+)',src.name)
    if iris: rigid=f'Iris.{int(iris[1]):02d}'
    if 'lower asymmetrical iris shutter' in src.name: rigid='Iris.Lower'
    groups = {name:ob.vertex_groups.new(name=name) for name in positions}
    if chain or src.name.startswith('WEAR |') or (src.name.startswith('CLAW |') and not talon):
        for v in me.vertices:
            if chain: weights=chain_weights(v.co,chain)
            elif src.name.startswith('WEAR |'): weights=[(nearest_body(v.co),1)]
            else:
                # Upper parasitic strands bridge the shoulder and elbow continuously.
                z=(v.co.z+.084)/1.5
                t=max(0,min(1,(z-2.25)/.43))
                weights=[('Forearm.R',1-t),('UpperArm.R',t)]
            for bone,weight in weights:
                if weight>.0001: groups[bone].add([v.index],weight,'REPLACE')
    else: groups[rigid].add(list(range(len(me.vertices))),1,'REPLACE')
    bpy.ops.object.select_all(action='DESELECT')
    ob.select_set(True); bpy.context.view_layer.objects.active=ob
    triangles=sum(len(p.vertices)-2 for p in me.polygons)
    if triangles>350:
        dec=ob.modifiers.new('PC surface reduction','DECIMATE')
        dec.ratio=.62 if src.name.startswith('R02 |') else .48 if (chain or iris or 'WHITE ARMOR' in str(src.users_collection)) else .28
        if triangles<1400: dec.ratio=max(dec.ratio,.55)
        bpy.ops.object.modifier_apply(modifier=dec.name)
    meshes.append(ob)
    if index%150==0: print('Converted',index,'/',source_count,flush=True)

bpy.ops.object.select_all(action='DESELECT')
for ob in meshes: ob.select_set(True)
bpy.context.view_layer.objects.active=meshes[0]
bpy.ops.object.join()
lod0 = bpy.context.object
lod0.name='E01Elite_LOD0'
lod0.parent=rig
mod=lod0.modifiers.new('Generic skin - max two influences','ARMATURE');mod.object=rig
lod1=lod0.copy();lod1.data=lod0.data.copy();game.objects.link(lod1);lod1.name='E01Elite_LOD1'
for mod in list(lod1.modifiers): lod1.modifiers.remove(mod)
bpy.ops.object.select_all(action='DESELECT');lod1.select_set(True);bpy.context.view_layer.objects.active=lod1
dec=lod1.modifiers.new('Distant PC silhouette','DECIMATE');dec.ratio=.42
bpy.ops.object.modifier_apply(modifier=dec.name)
mod=lod1.modifiers.new('Generic skin','ARMATURE');mod.object=rig
lod1.hide_render=True;lod1.hide_set(True)

# Only the owned export objects remain in the independent game scene.
for obj in list(bpy.data.objects):
    if obj not in (rig,lod0,lod1): bpy.data.objects.remove(obj,do_unlink=True)
for col in list(bpy.data.collections):
    if col != game: bpy.data.collections.remove(col)
sc.name='E01 Elite - PC skinned asset R02'
sc['source_sha256']=source_hash
sc['purpose']='Frozen runtime geometry with generic armature; source R02 sculpture preserved.'
sc.render.filepath=str(OUT/'renders')+'/'
sc.camera=None
bpy.context.view_layer.update()
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'TYPE_E01_ELITE_GAME_R01.blend'),compress=True)
bpy.ops.object.select_all(action='DESELECT')
for ob in (rig,lod0,lod1): ob.hide_set(False);ob.hide_render=False;ob.select_set(True)
bpy.context.view_layer.objects.active=rig
fbx=ART/'TypeE01Elite_R02.fbx'
bpy.ops.export_scene.fbx(filepath=str(fbx),use_selection=True,object_types={'MESH','ARMATURE'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_space_transform=False,use_mesh_modifiers=True,add_leaf_bones=False,bake_anim=False,mesh_smooth_type='FACE',use_tspace=False,path_mode='STRIP',use_custom_props=True)
tris=lambda obj:sum(len(p.vertices)-2 for p in obj.data.polygons)
report={'source':str(SOURCE),'source_sha256':source_hash,'source_unchanged':hashlib.sha256(SOURCE.read_bytes()).hexdigest()==source_hash,'source_objects':source_count,'input_triangles':input_tris,'lod0_triangles':tris(lod0),'lod1_triangles':tris(lod1),'bones':len(positions),'primary_tendril_chains':6,'claw_chains':6,'iris_bones':10,'materials':materials,'bounds':{'min':[min(v.co[i] for v in lod0.data.vertices) for i in range(3)],'max':[max(v.co[i] for v in lod0.data.vertices) for i in range(3)]},'fbx':str(fbx),'fbx_sha256':hashlib.sha256(fbx.read_bytes()).hexdigest(),'rig_bone_positions_blender':{k:list(v) for k,v in positions.items()}}
report['material_list'] = [dict(name=name,**data) for name,data in materials.items()]
(OUT/'export_manifest.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
(ART/'export_manifest.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('E01_EXPORT_PASS',report['lod0_triangles'],report['lod1_triangles'],len(positions),flush=True)
