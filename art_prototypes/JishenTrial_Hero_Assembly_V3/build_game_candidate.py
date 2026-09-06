"""Update the existing optimized body with the colored HEAD_V2 and export FBX/GLB.
Blender 5.2.1: blender -b --threads 4 --python-exit-code 1 --python build_game_candidate.py
The source body textures are preserved at their original pixel resolution. An additional
2K head tile shares the Armor material in a 4096 x 2048 atlas, so there are still 5 materials.
"""
import bpy,bmesh,pathlib,json,math,numpy as np
from mathutils import Matrix,Vector
OUT=pathlib.Path(__file__).resolve().parent;TEX=OUT/'textures';EX=OUT/'exports'
SRC=OUT.parent/'JishenTrial_V2_HighDetail'
bpy.ops.wm.open_mainfile(filepath=str(SRC/'V2_GAME_READY_DEMO.blend'))
sc=bpy.context.scene;export=bpy.data.collections['EXPORT'];head_mount=bpy.data.objects['Head'];thorax=bpy.data.objects['Thorax']
for o in list(head_mount.children_recursive):bpy.data.objects.remove(o,do_unlink=True)
body=list(export.objects);oldset=set(bpy.data.objects)
with bpy.data.libraries.load(str(OUT/'JishenTrial_ASSEMBLED_MASTER.blend'),link=False) as (src,dst):
    dst.collections=['HEAD_V2 | colored editable parts']
hc=dst.collections[0];sc.collection.children.link(hc);bpy.context.view_layer.update()
dg=bpy.context.evaluated_depsgraph_get();snap=[]
for o in hc.all_objects:
    if o.type=='MESH':snap.append((o.name,bpy.data.meshes.new_from_object(o.evaluated_get(dg),preserve_all_data_layers=True,depsgraph=dg),o.matrix_world.copy()))
assert len(snap)==23,len(snap)
for o in list(set(bpy.data.objects)-oldset):bpy.data.objects.remove(o,do_unlink=True)
bpy.data.collections.remove(hc)
newhead=[]
for name,mesh,w in snap:
    o=bpy.data.objects.new('LOD0_HEADV2_'+name,mesh);export.objects.link(o);o.matrix_world=w
    o.parent=thorax if name.startswith(('14_','16_','17_')) else head_mount;o.matrix_world=w
    o['atlas_family']='Sensor' if name.startswith('08_') else 'Armor';o['rigid_segment']=o.parent.name
    bm=bmesh.new();bm.from_mesh(mesh);bmesh.ops.triangulate(bm,faces=list(bm.faces));bm.to_mesh(mesh);bm.free()
    newhead.append(o)
bpy.context.view_layer.update()
assert min((o.matrix_world@v.co).z for o in newhead for v in o.data.vertices)>2.9
armor=[o for o in newhead if o['atlas_family']=='Armor'];sensors=[o for o in newhead if o['atlas_family']=='Sensor']
bpy.ops.object.select_all(action='DESELECT')
for o in armor:o.select_set(True)
bpy.context.view_layer.objects.active=armor[0]
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT')
bpy.ops.uv.smart_project(angle_limit=math.radians(66),island_margin=.006,area_weight=.4,scale_to_bounds=True)
bpy.ops.uv.pack_islands(rotate=True,margin=.008,shape_method='AABB',scale=True)
bpy.ops.object.mode_set(mode='OBJECT')
for o in armor:o.data.uv_layers.active.name='UV0'

# A temporary joined copy is used only for the head tile bake.
copies=[]
for o in armor:
    q=bpy.data.objects.new('BAKE_'+o.name,o.data.copy());sc.collection.objects.link(q);q.matrix_world=o.matrix_world.copy();copies.append(q)
bpy.ops.object.select_all(action='DESELECT')
for q in copies:q.select_set(True)
bpy.context.view_layer.objects.active=copies[0];bpy.ops.object.join();target=copies[0]
target_mats=set(target.data.materials)
sc.render.engine='CYCLES';sc.cycles.samples=8;sc.cycles.use_denoising=False;sc.cycles.device='GPU'
prefs=bpy.context.preferences.addons['cycles'].preferences;prefs.compute_device_type='OPTIX';prefs.get_devices()
for d in prefs.devices:d.use=d.type=='OPTIX'
sc.render.bake.use_selected_to_active=False;sc.render.bake.use_clear=True;sc.render.bake.margin=16
for v in sc.view_layers:v.material_override=None
headimages={}
for channel in ('Normal','BaseColor','Roughness','Metallic'):
    im=bpy.data.images.new('HEAD_TILE_'+channel,2048,2048,alpha=False)
    im.colorspace_settings.name='sRGB' if channel=='BaseColor' else 'Non-Color'
    for m in target_mats:
        n=m.node_tree.nodes;tn=n.get('HEAD_BAKE_TARGET') or n.new('ShaderNodeTexImage');tn.name='HEAD_BAKE_TARGET';tn.image=im
        for a in n:a.select=False
        tn.select=True;n.active=tn
    restore=[]
    if channel!='Normal':
        for m in target_mats:
            ns=m.node_tree.nodes;ls=m.node_tree.links;p=next(n for n in ns if n.type=='BSDF_PRINCIPLED');out=next(n for n in ns if n.type=='OUTPUT_MATERIAL')
            old=out.inputs['Surface'].links[0].from_socket;e=ns.new('ShaderNodeEmission');e.inputs['Strength'].default_value=1
            sock=p.inputs[{'BaseColor':'Base Color','Roughness':'Roughness','Metallic':'Metallic'}[channel]]
            if sock.is_linked:ls.new(sock.links[0].from_socket,e.inputs['Color'])
            else:
                val=sock.default_value;e.inputs['Color'].default_value=val if channel=='BaseColor' else (val,val,val,1)
            ls.new(e.outputs[0],out.inputs['Surface']);restore.append((m,out,old,e))
    bpy.ops.object.select_all(action='DESELECT');target.select_set(True);bpy.context.view_layer.objects.active=target
    bpy.ops.object.bake(type='NORMAL' if channel=='Normal' else 'EMIT',normal_space='TANGENT')
    for m,out,old,e in restore:m.node_tree.links.new(old,out.inputs['Surface']);m.node_tree.nodes.remove(e)
    headimages[channel]=im
    print('HEAD_BAKED',channel,flush=True)
bpy.data.objects.remove(target,do_unlink=True)

# Combine image pixels in linear data space, without rescaling either source tile.
armor_mat=bpy.data.materials['JT_DEMO_Armor']
for channel in ('Normal','BaseColor','Roughness','Metallic'):
    old=bpy.data.images.get('JT_Armor_'+channel);assert old and tuple(old.size)==(2048,2048)
    a=np.empty(2048*2048*4,dtype=np.float32);b=np.empty_like(a)
    old.pixels.foreach_get(a);headimages[channel].pixels.foreach_get(b)
    px=np.concatenate((a.reshape(2048,2048,4),b.reshape(2048,2048,4)),axis=1)
    im=bpy.data.images.new('JT_V3_Armor_'+channel,4096,2048,alpha=False)
    im.colorspace_settings.name='sRGB' if channel=='BaseColor' else 'Non-Color';im.pixels.foreach_set(px.ravel());im.update()
    im.filepath_raw=str(TEX/(im.name+'.png'));im.file_format='PNG';im.save();im.pack()
    for n in armor_mat.node_tree.nodes:
        if n.type=='TEX_IMAGE' and n.image==old:n.image=im
for o in body:
    if o.get('atlas_family')=='Armor':
        for v in o.data.uv_layers.active.data:v.uv.x*=.5
for o in armor:
    for v in o.data.uv_layers.active.data:v.uv.x=.5+.5*v.uv.x
    o.data.materials.clear();o.data.materials.append(armor_mat)
    for p in o.data.polygons:p.material_index=0
for o in sensors:
    o.data.materials.clear();o.data.materials.append(bpy.data.materials['JT_DEMO_Sensor'])
    for p in o.data.polygons:p.material_index=0
    if not o.data.uv_layers:o.data.uv_layers.new(name='UV0')
for fam in ('Mechanics','Weapon'):
    for channel in ('Normal','BaseColor','Roughness','Metallic'):
        im=bpy.data.images['JT_'+fam+'_'+channel];im.filepath_raw=str(TEX/(im.name+'.png'));im.file_format='PNG';im.save();im.pack()

# Only merge within a rigid joint and a material family. Neck interface and optics stay separate.
groups={}
for o in newhead:groups.setdefault((o.parent.name,o['atlas_family']),[]).append(o)
for (parent,fam),items in groups.items():
    bpy.ops.object.select_all(action='DESELECT')
    for o in items:o.select_set(True)
    bpy.context.view_layer.objects.active=items[0];bpy.ops.object.join();o=items[0]
    o.name='LOD0_HEADV2_'+parent+'_'+fam;o['rigid_segment']=parent;o['atlas_family']=fam

assembly=bpy.data.objects.new('JishenTrial_Hero_Assembly_V3',None);sc.collection.objects.link(assembly)
for name in ('JISHEN_Master_Root','AntiShip_Blade_Display_Root'):
    o=bpy.data.objects[name];w=o.matrix_world.copy();o.parent=assembly;o.matrix_world=w
bpy.context.view_layer.update()
weapon=bpy.data.objects['AntiShip_Blade_Display_Root'];weapon['Beam_On']=True;weapon.update_tag();sc.frame_set(sc.frame_current)
parts=list(export.objects)
stats={'meshes':len(parts),'triangles':0,'materials':sorted({m.name for o in parts for m in o.data.materials}),'source_head_parts':22,
       'head_side_approved':False,'crown_height_m':3.351056,'head_scale_after_user_feedback':1.5,'rigged':False,'animations':0,'uv':'Body Armor in left atlas tile, newly baked head in right tile; UV0',
       'atlas_dimensions':{'Armor':[4096,2048],'Mechanics':[2048,2048],'Weapon':[2048,2048]},'nonmanifold_edges':{}}
for o in parts:
    # The old backpack contains one tiny triangular flap on each side, with two
    # border edges and a third edge already shared by two valid faces. Remove that
    # flap only. Do not weld separate armor shells at coincident mating edges.
    if o.name in ('LOD0_Backpack_Main_Deploy_Hinge.R_Armor','LOD0_Backpack_Main_Deploy_Hinge.L_Armor'):
        bm=bmesh.new();bm.from_mesh(o.data)
        flaps=[f for f in bm.faces if sum(e.is_boundary for e in f.edges)==2 and any(len(e.link_faces)>2 for e in f.edges) and f.calc_area()<1e-7]
        assert len(flaps)==1,(o.name,len(flaps))
        bmesh.ops.delete(bm,geom=flaps,context='FACES')
        loose=[v for v in bm.verts if not v.link_faces]
        if loose:bmesh.ops.delete(bm,geom=loose,context='VERTS')
        bm.to_mesh(o.data);bm.free()
    o.data.calc_loop_triangles();stats['triangles']+=len(o.data.loop_triangles)
    bm=bmesh.new();bm.from_mesh(o.data);nm=sum(not e.is_manifold for e in bm.edges);bm.free()
    if nm:stats['nonmanifold_edges'][o.name]=nm
assert len(stats['materials'])==5 and 80000<=stats['triangles']<=150000,stats
sc['rigging_state']='No skeleton/animation. Static import candidate only. Retain rigid pivots for future rig.'
sc['head_review']='User rejected side form; this file is assembled/colorized for review.'
sc['material_strategy']='5 shared materials; Armor 4096x2048 with original body 2K tile + newly baked 2K head tile.'
for s in bpy.data.screens:
    for a in s.areas:
        if a.type=='VIEW_3D':a.spaces.active.clip_start=.02;a.spaces.active.clip_end=25
sc.cycles.samples=48;sc.cycles.use_denoising=True
bpy.context.preferences.filepaths.save_version=0
bpy.data.texts.load(str(OUT/'build_game_candidate.py'))
bpy.ops.object.select_all(action='DESELECT')
parents=set()
for o in parts:
    o.select_set(True);p=o.parent
    while p:parents.add(p);p=p.parent
for p in parents:p.hide_set(False);p.select_set(True)
bpy.context.view_layer.objects.active=assembly
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'JishenTrial_GAME_CANDIDATE.blend'))
bpy.ops.export_scene.fbx(filepath=str(EX/'JishenTrial_Assembly_V3.fbx'),use_selection=True,object_types={'MESH','EMPTY'},
    apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL',axis_forward='-Z',axis_up='Y',use_mesh_modifiers=True,
    add_leaf_bones=False,bake_anim=False,path_mode='COPY',embed_textures=True,use_custom_props=True)
bpy.ops.export_scene.gltf(filepath=str(EX/'JishenTrial_Assembly_V3.glb'),export_format='GLB',use_selection=True,
    export_yup=True,export_apply=True,export_animations=False,export_extras=True)
(OUT/'logs'/'game_candidate.json').write_text(json.dumps(stats,indent=2),encoding='utf8')
print('GAME_CANDIDATE_EXPORTED',json.dumps(stats),flush=True)
