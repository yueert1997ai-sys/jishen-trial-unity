"""Derive the PC demo LOD0 from the actual refined high-detail master.
Run: blender --background V2_HIGH_DETAIL_FINAL.blend --python build_demo_geometry.py
Builds reduced real geometry, keeps articulated assembly pivots, creates packed UVs.
Sources stay in this intermediate bake file only. Final file is made by bake_demo.py.
"""
import bpy,bmesh,math,pathlib,json,time
from collections import defaultdict
from mathutils import Matrix,Vector
OUT=pathlib.Path(__file__).resolve().parent;scene=bpy.context.scene;T=time.time()
scene['asset_layer']='PC standalone demo LOD0 under construction'
originals=[o for o in scene.objects if o.type in ('MESH','CURVE') and not o.hide_render and o.name!='Studio_Ground']
source_root=bpy.data.collections.new('BAKE_SOURCE | master reference');scene.collection.children.link(source_root)
export=bpy.data.collections.new('EXPORT');scene.collection.children.link(export)
weapon=bpy.data.objects['AntiShip_Blade_Display_Root'];weapon['Beam_On']=True;bpy.context.view_layer.update()
wp=set(weapon.children_recursive);groups=defaultdict(list);sources=defaultdict(list);reduced=[];omitted=[]

def family(o):
    mat=o.data.materials[0].name
    if mat.startswith('11_'):return 'Beam'
    if mat.startswith('10_'):return 'Sensor'
    if o in wp:return 'Weapon'
    if mat.startswith(('01_','02_','03_','04_')):return 'Armor'
    return 'Mechanics'

def ancestor(o):
    p=o.parent
    while p and p.type!='EMPTY':p=p.parent
    return p

# Batch modifier changes, then evaluate once before linking any derived objects.
# This avoids repeated topology/relationship rebuilds in Blender 5.2.
modifier_restore=[];curve_restore=[]
for o in originals:
    hardware=any(p in o.name for p in ('Socket','ServiceBolt','Fastener','Grip_Insulator','Finger_','Thumb_','Louver','Rim_','Pin_','Armor_Lock','Catch_','Latch_'))
    if o.type=='CURVE':
        curve_restore.append((o.data,o.data.bevel_resolution,o.data.resolution_u));o.data.bevel_resolution=1;o.data.resolution_u=6
    for mod in o.modifiers:
        if mod.type=='BEVEL':
            modifier_restore.append((mod,mod.segments,mod.show_viewport,mod.show_render));mod.segments=1
            if hardware:mod.show_viewport=False;mod.show_render=False
bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get();evaluated_meshes={}
for i,o in enumerate(originals):
    if 'Washer' not in o.name:evaluated_meshes[o.name]=bpy.data.meshes.new_from_object(o.evaluated_get(dg),preserve_all_data_layers=True,depsgraph=dg)
    if i%200==0:print('SNAPSHOT',i,flush=True)
for m,seg,vis,ren in modifier_restore:m.segments=seg;m.show_viewport=vis;m.show_render=ren
for d,res,u in curve_restore:d.bevel_resolution=res;d.resolution_u=u

for i,o in enumerate(originals):
    fam=family(o);sources[fam].append(o)
    if 'Washer' in o.name:
        omitted.append(o.name);continue
    me=evaluated_meshes[o.name]
    # Explicitly weld numeric duplicates, remove unused construction vertices and fix winding.
    bm=bmesh.new();bm.from_mesh(me);bmesh.ops.remove_doubles(bm,verts=bm.verts,dist=1e-7)
    loose=[v for v in bm.verts if not v.link_faces]
    if loose:bmesh.ops.delete(bm,geom=loose,context='VERTS')
    bmesh.ops.dissolve_degenerate(bm,edges=list(bm.edges),dist=1e-7)
    # Close swept cable ends inside their housings before unwrapping; no open export borders.
    borders=[e for e in bm.edges if e.is_boundary]
    if borders:bmesh.ops.holes_fill(bm,edges=borders,sides=0)
    bmesh.ops.recalc_face_normals(bm,faces=bm.faces);bm.to_mesh(me);bm.free()
    low=bpy.data.objects.new('LOD0_'+o.name,me);export.objects.link(low);low.matrix_world=o.matrix_world.copy()
    low['source_master_object']=o.name;low['atlas_family']=fam
    p=ancestor(o)
    if p:
        w=low.matrix_world.copy();low.parent=p;low.matrix_world=w
    groups[(p.name if p else 'Weapon_Root',fam)].append(low);reduced.append(low)
    if i%200==0:print('DERIVED',i,len(originals),flush=True)

# Merge by rigid segment and atlas, never across articulation pivots.
merged=[]
for (pn,fam),objs in groups.items():
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs:o.select_set(True)
    bpy.context.view_layer.objects.active=objs[0];bpy.ops.object.join();o=objs[0]
    o.name='LOD0_'+pn.replace('AntiShip_Blade_Display_Root','AntiShipBlade')+'_'+fam
    o['atlas_family']=fam;o['rigid_segment']=pn
    if o.parent:
        # All parts of a rigid segment share the actual articulation origin.
        w=o.matrix_world.copy();target=o.parent.matrix_world.translation.copy();delta=w.translation-target
        for v in o.data.vertices:v.co+=w.to_3x3().inverted()@delta
        w.translation=target;o.matrix_world=w
    merged.append(o)

# Sources are grouped for selected-to-active normal projection and never exported.
for o in originals:
    for c in list(o.users_collection):c.objects.unlink(o)
    source_root.objects.link(o);o['bake_family']=family(o)
    o.hide_render=True;o.hide_set(True)

# Multi-object Smart UV then global material-family packing, with 12 px atlas margins.
for fam in ('Armor','Mechanics','Weapon','Sensor','Beam'):
    objs=[o for o in merged if o['atlas_family']==fam]
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs:
        o.hide_set(False);o.select_set(True)
        if not o.data.uv_layers:o.data.uv_layers.new(name='UV0')
    bpy.context.view_layer.objects.active=objs[0]
    bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(angle_limit=math.radians(66),island_margin=.004,area_weight=.35,correct_aspect=True,scale_to_bounds=True)
    bpy.ops.uv.pack_islands(rotate=True,margin=.006,shape_method='AABB',scale=True)
    bpy.ops.object.mode_set(mode='OBJECT');print('UV_PACKED',fam,len(objs),flush=True)
    for o in objs:o.data.uv_layers.active.name='UV0'

stats={'source':bpy.data.filepath,'master_sources':len(originals),'omitted_washers_baked':len(omitted),'mesh_objects':len(merged),'families':{},'triangles':0,'polygons':0,'seconds':0}
for fam in ('Armor','Mechanics','Weapon','Sensor','Beam'):
    objs=[o for o in merged if o['atlas_family']==fam];tr=0;po=0
    for o in objs:o.data.calc_loop_triangles();tr+=len(o.data.loop_triangles);po+=len(o.data.polygons)
    stats['families'][fam]={'objects':len(objs),'triangles':tr,'polygons':po};stats['triangles']+=tr;stats['polygons']+=po
assert 80000<=stats['triangles']<=150000,stats
stats['seconds']=round(time.time()-T,2)
scene['game_geometry_triangles']=stats['triangles'];scene['game_geometry_plan']='Rigid segment merges; reduced bevels/cables; washer detail baked; 5 atlas materials'
bpy.ops.object.select_all(action='DESELECT')
for o in merged:o.select_set(True)
bpy.context.view_layer.objects.active=merged[0]
text=bpy.data.texts.load(str(OUT/'build_demo_geometry.py'));text.name='build_demo_geometry.py | executed'
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'DEMO_BAKE_WORKING.blend'))
(OUT/'logs'/'demo_geometry.json').write_text(json.dumps(stats,indent=2),encoding='utf8')
print('DEMO_GEOMETRY_COMPLETE',json.dumps(stats),flush=True)
