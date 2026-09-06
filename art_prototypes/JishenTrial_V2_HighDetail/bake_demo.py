"""Bake the actual master onto the reduced UV asset and publish local demo files.
Run on DEMO_BAKE_WORKING.blend. Uses Cycles selected-to-active tangent normal bake;
base color / roughness / metallic are unlit material bakes, never painted stand-ins.
"""
import bpy,pathlib,json,time,math,bmesh
from mathutils import Matrix,Vector
OUT=pathlib.Path(__file__).resolve().parent;TEX=OUT/'textures';TEX.mkdir(exist_ok=True)
EX=OUT/'exports';EX.mkdir(exist_ok=True);sc=bpy.context.scene;T=time.time()
export=bpy.data.collections['EXPORT'];lows=list(export.objects)
source=bpy.data.collections['BAKE_SOURCE | master reference'];highs=list(source.objects)
sc.render.engine='CYCLES';sc.cycles.samples=16;sc.cycles.use_denoising=False;sc.cycles.device='GPU'
p=bpy.context.preferences.addons['cycles'].preferences;p.compute_device_type='OPTIX';p.get_devices()
for d in p.devices:d.use=d.type=='OPTIX'
sc.render.bake.margin=12;sc.render.bake.margin_type='EXTEND';sc.render.bake.use_clear=True
sc.render.bake.cage_extrusion=.0025;sc.render.bake.max_ray_distance=.008;sc.render.bake.use_cage=False
for l in sc.view_layers:l.material_override=None

# Freeze the high source in a single pass. This bake project keeps the low UVs intact.
for o in lows:o.hide_render=True;o.hide_set(True)
for o in highs:o.hide_render=False;o.hide_set(False)
bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get();snap=[]
for o in highs:
    snap.append((o,bpy.data.meshes.new_from_object(o.evaluated_get(dg),preserve_all_data_layers=True,depsgraph=dg),o.matrix_world.copy()))
newhigh=[]
for o,me,mw in snap:
    if o.type=='MESH':o.modifiers.clear();o.data=me
    else:
        name=o.name;fam=o['bake_family'];parent=o.parent;bpy.data.objects.remove(o,do_unlink=True)
        o=bpy.data.objects.new(name,me);source.objects.link(o);o.parent=parent;o.matrix_world=mw;o['bake_family']=fam
    o.hide_render=True;o.hide_set(True);newhigh.append(o)
highs=newhigh

def temp_join(objs,name):
    copies=[]
    for o in objs:
        n=bpy.data.objects.new('BAKE_TEMP_'+o.name,o.data.copy());sc.collection.objects.link(n);n.matrix_world=o.matrix_world.copy();copies.append(n)
    bpy.ops.object.select_all(action='DESELECT')
    for o in copies:o.hide_set(False);o.select_set(True)
    bpy.context.view_layer.objects.active=copies[0];bpy.ops.object.join();copies[0].name=name
    return copies[0]

def target_image(obj,img):
    for mat in obj.data.materials:
        nodes=mat.node_tree.nodes
        node=nodes.get('BAKE_TARGET') or nodes.new('ShaderNodeTexImage');node.name='BAKE_TARGET';node.image=img
        for n in nodes:n.select=False
        node.select=True;nodes.active=node

def select_bake(target,src=None):
    bpy.ops.object.select_all(action='DESELECT');target.hide_render=False;target.hide_set(False);target.select_set(True)
    if src:src.hide_render=False;src.hide_set(False);src.select_set(True)
    bpy.context.view_layer.objects.active=target
    sc.render.bake.use_selected_to_active=bool(src)

def image_new(fam,channel):
    im=bpy.data.images.new('JT_'+fam+'_'+channel,2048,2048,alpha=False,float_buffer=False)
    im.generated_color=(.5,.5,1,1) if channel=='Normal' else (.015,.015,.015,1)
    im.colorspace_settings.name='sRGB' if channel=='BaseColor' else 'Non-Color'
    return im

def save_image(im,channel):
    im.filepath_raw=str(TEX/(im.name+'.png'));im.file_format='PNG';im.save()
    return {'image':im.name,'path':im.filepath_raw,'width':im.size[0],'height':im.size[1],'colorspace':im.colorspace_settings.name,'channel':channel}

def channel_shader(mats,channel):
    restore=[]
    for m in set(mats):
        n=m.node_tree.nodes;l=m.node_tree.links;out=next(n for n in n if n.type=='OUTPUT_MATERIAL');p=next(n for n in n if n.type=='BSDF_PRINCIPLED')
        old=out.inputs['Surface'].links[0].from_socket
        en=n.new('ShaderNodeEmission');en.name='BAKE_UNLIT_CHANNEL';en.inputs['Strength'].default_value=1
        sock=p.inputs[{'BaseColor':'Base Color','Roughness':'Roughness','Metallic':'Metallic'}[channel]]
        if sock.is_linked:l.new(sock.links[0].from_socket,en.inputs['Color'])
        elif channel=='BaseColor':en.inputs['Color'].default_value=sock.default_value
        else:en.inputs['Color'].default_value=(sock.default_value,sock.default_value,sock.default_value,1)
        l.new(en.outputs[0],out.inputs['Surface']);restore.append((m,out,old,en))
    return restore

def restore_shader(rs):
    for m,out,sock,node in rs:m.node_tree.links.new(sock,out.inputs['Surface']);m.node_tree.nodes.remove(node)

def game_material(fam,images):
    m=bpy.data.materials.new('JT_DEMO_'+fam);m.use_nodes=True;n=m.node_tree.nodes;l=m.node_tree.links
    p=next(n for n in n if n.type=='BSDF_PRINCIPLED');p.location=(320,60)
    for i,(channel,im) in enumerate(images.items()):
        tex=n.new('ShaderNodeTexImage');tex.image=im;tex.label=channel;tex.location=(-560,220-i*270)
        if channel=='Normal':
            nm=n.new('ShaderNodeNormalMap');nm.space='TANGENT';nm.uv_map='UV0';nm.location=(80,-330)
            l.new(tex.outputs['Color'],nm.inputs['Color']);l.new(nm.outputs['Normal'],p.inputs['Normal'])
        else:l.new(tex.outputs['Color'],p.inputs[{'BaseColor':'Base Color','Roughness':'Roughness','Metallic':'Metallic'}[channel]])
    m.diffuse_color=(.018,.03,.058,1) if fam=='Armor' else (.048,.061,.078,1)
    return m

report={'source':'V2_HIGH_DETAIL_FINAL.blend','method':'Actual selected-to-active tangent normal bake; unlit Cycles material channel bakes','normal_cage_m':.0025,'ray_distance_m':.008,'atlas_size':2048,'families':{},'images':[]}
for fam in ('Armor','Mechanics','Weapon'):
    print('BEGIN_BAKE',fam,flush=True);start=time.time()
    lo=[o for o in lows if o['atlas_family']==fam];hi=[o for o in highs if o['bake_family']==fam]
    target=temp_join(lo,'BAKE_TARGET_'+fam);src=temp_join(hi,'BAKE_HIGH_'+fam)
    images={}
    for channel in ('Normal','BaseColor','Roughness','Metallic'):
        img=image_new(fam,channel);target_image(target,img)
        if channel=='Normal':
            select_bake(target,src);bpy.ops.object.bake(type='NORMAL',normal_space='TANGENT')
        else:
            src.hide_render=True;src.hide_set(True);select_bake(target)
            rs=channel_shader(target.data.materials,channel);bpy.ops.object.bake(type='EMIT');restore_shader(rs)
        images[channel]=img;report['images'].append(save_image(img,channel))
        print('BAKED',fam,channel,flush=True)
    mat=game_material(fam,images)
    for o in lo:
        o.data.materials.clear();o.data.materials.append(mat)
        for p in o.data.polygons:p.material_index=0
    report['families'][fam]={'normal_source_triangles':sum(len(o.data.loop_triangles) for o in hi),'seconds':round(time.time()-start,2)}
    bpy.data.objects.remove(target,do_unlink=True);bpy.data.objects.remove(src,do_unlink=True)
    (OUT/'logs'/'demo_bakes.json').write_text(json.dumps(report,indent=2),encoding='utf8')
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'DEMO_BAKE_PROGRESS.blend'))

for fam,source_name in (('Sensor','10_Blue_Sensor_Glass'),('Beam','11_Blue_Beam_Plasma')):
    mat=bpy.data.materials[source_name].copy();mat.name='JT_DEMO_'+fam
    for o in lows:
        if o['atlas_family']==fam:
            o.data.materials.clear();o.data.materials.append(mat)
            for p in o.data.polygons:p.material_index=0

# Remove bake master/cutters, leaving only the optimized asset and inspection setup.
for o in highs:bpy.data.objects.remove(o,do_unlink=True)
for o in list(sc.objects):
    if o.type=='MESH' and o not in lows and o.name!='Studio_Ground':bpy.data.objects.remove(o,do_unlink=True)
bpy.data.collections.remove(source)
for o in lows:o.hide_render=False;o.hide_set(False)
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'DEMO_TEXTURED_WORKING.blend'))

# Final explicit triangulation locks the tangent-space basis used by game importers.
for o in lows:
    bpy.context.view_layer.objects.active=o;o.select_set(True)
    mod=o.modifiers.new('Final_Game_Triangulation','TRIANGULATE')
    if hasattr(mod,'keep_custom_normals'):mod.keep_custom_normals=True
    bpy.ops.object.modifier_apply(modifier=mod.name);o.select_set(False)
    o.data.validate(verbose=False)
    o['export_state']='UV0; baked PBR; triangulated; rigid segment pivot retained'
beam=next(o for o in lows if o['atlas_family']=='Beam');weapon=bpy.data.objects['AntiShip_Blade_Display_Root'];weapon['Beam_On']=True
for prop in ('hide_render','hide_viewport'):
    d=beam.driver_add(prop).driver;d.type='SCRIPTED';v=d.variables.new();v.name='enabled';v.type='SINGLE_PROP';v.targets[0].id=weapon;v.targets[0].data_path='["Beam_On"]';d.expression='not enabled'

sc['asset_layer']='GAME-READY DEMO / rigid hard-surface LOD0';sc['texture_strategy']='3 x 2K PBR atlases + constant sensor and independent beam; five materials total'
sc['normal_convention']='OpenGL tangent normal, +Y green; packed metallic and roughness available separately'
sc['rigging_state']='No full skeleton/skin/animation. Rigid joints and independent finger/propulsion pivots retained.'
sc.cycles.use_denoising=True;sc.cycles.samples=64
for im in bpy.data.images:
    if im.name.startswith('JT_'):im.pack()
for screen in bpy.data.screens:
    for a in screen.areas:
        if a.type=='VIEW_3D':a.spaces.active.overlay.show_relationship_lines=False;a.spaces.active.shading.color_type='MATERIAL'

usedmats={m.name for o in lows for m in o.data.materials}
stats={'mesh_objects':len(lows),'triangles':sum(len(o.data.polygons) for o in lows),'material_count':len(usedmats),'materials':sorted(usedmats),'textures':len(report['images']),'crown_height_m':3.25,'beam_separate':beam.name,'elapsed_seconds':round(time.time()-T,2)}
assert len(usedmats)==5 and 80000<=stats['triangles']<=150000,stats
for f in ('build_demo_geometry.py','bake_demo.py'):
    text=bpy.data.texts.load(str(OUT/f));text.name=f+' | executed final pipeline'
bpy.ops.object.select_all(action='DESELECT')
for o in lows:o.select_set(True)
parents=set()
for o in lows:
    p=o.parent
    while p:parents.add(p);p=p.parent
for p in parents:p.hide_set(False);p.select_set(True)
bpy.context.view_layer.objects.active=lows[0]
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'V2_GAME_READY_DEMO.blend'))
bpy.ops.export_scene.fbx(filepath=str(EX/'JishenTrial_Hero_LOD0.fbx'),use_selection=True,object_types={'MESH','EMPTY'},
    apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL',axis_forward='-Z',axis_up='Y',use_mesh_modifiers=True,
    add_leaf_bones=False,bake_anim=False,path_mode='COPY',embed_textures=True,use_custom_props=True)
try:
    bpy.ops.export_scene.gltf(filepath=str(EX/'JishenTrial_Hero_LOD0.glb'),export_format='GLB',use_selection=True,
        export_yup=True,export_apply=True,export_animations=False,export_extras=True)
    stats['glb']='exports/JishenTrial_Hero_LOD0.glb'
except Exception as e:stats['glb_error']=str(e)
stats['fbx']='exports/JishenTrial_Hero_LOD0.fbx'
(OUT/'logs'/'demo_final.json').write_text(json.dumps(stats,indent=2),encoding='utf8')
(OUT/'logs'/'demo_bakes.json').write_text(json.dumps(report,indent=2),encoding='utf8')
print('DEMO_ASSET_SAVED',json.dumps(stats),flush=True)
