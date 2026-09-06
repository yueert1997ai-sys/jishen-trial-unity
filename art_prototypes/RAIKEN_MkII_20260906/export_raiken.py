"""Preserve the editable master; derive a compact draw-call layout for import."""
import bpy,bmesh,pathlib,json,math
from mathutils import Vector
P=pathlib.Path(__file__).resolve().parent
bpy.ops.wm.open_mainfile(filepath=str(P/'RAIKEN_MkII_MASTER.blend'))
sc=bpy.context.scene;root=bpy.data.objects['RAIKEN_MkII_Grip_Root']
root['Beam_On']=True;root.update_tag();bpy.context.view_layer.update()
parts=[o for o in sc.objects if o.name.startswith('RK_') and o.type in ('MESH','FONT')]
# Small bevels and curves need fewer steps at the production scale. No decimation.
for o in parts:
    for m in o.modifiers:
        if m.type=='BEVEL':m.segments=1
    if o.type=='FONT':o.data.resolution_u=2
bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get()
snapshots=[]
for o in parts:
    me=bpy.data.meshes.new_from_object(o.evaluated_get(dg),preserve_all_data_layers=True,depsgraph=dg)
    me.transform(o.matrix_world)
    if o.type=='FONT':
        bm=bmesh.new();bm.from_mesh(me)
        bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=1e-7)
        bmesh.ops.dissolve_degenerate(bm,edges=list(bm.edges),dist=1e-8)
        bm.to_mesh(me);bm.free()
    snapshots.append((o.name,me,bool(o.get('beam_component'))))
for o in parts:bpy.data.objects.remove(o,do_unlink=True)
collection=bpy.data.collections.new('RAIKEN | Game meshes');sc.collection.children.link(collection)
groups={}
for name,me,isbeam in snapshots:
    o=bpy.data.objects.new(name,me);collection.objects.link(o);o.parent=root
    o['beam_component']=isbeam
    key=(me.materials[0].name,isbeam);groups.setdefault(key,[]).append(o)
beamroot=bpy.data.objects.new('LOD0_AntiShipBlade_Beam',None);collection.objects.link(beamroot);beamroot.parent=root;beamroot['beam_component']=True
joined=[]
for (material,isbeam),obs in groups.items():
    bpy.ops.object.select_all(action='DESELECT')
    for o in obs:o.select_set(True)
    bpy.context.view_layer.objects.active=obs[0];bpy.ops.object.join()
    o=obs[0];o.name=('RAIKEN_Beam_' if isbeam else 'RAIKEN_Solid_')+material.removeprefix('RAIKEN_')
    o.parent=beamroot if isbeam else root
    bm=bmesh.new();bm.from_mesh(o.data)
    # Preserve touching but independent armor solids after joining by material.
    bmesh.ops.dissolve_degenerate(bm,edges=list(bm.edges),dist=1e-8)
    bmesh.ops.triangulate(bm,faces=list(bm.faces))
    bmesh.ops.dissolve_degenerate(bm,edges=list(bm.edges),dist=1e-8)
    bm.to_mesh(o.data);bm.free()
    joined.append(o)
for path in ('hide_render','hide_viewport'):
    # Mesh visibility drivers are also retained in the game .blend.
    for o in [o for o in joined if o.get('beam_component')]:
        dr=o.driver_add(path).driver;dr.expression='not beam'
        v=dr.variables.new();v.name='beam';v.type='SINGLE_PROP';v.targets[0].id=root;v.targets[0].data_path='["Beam_On"]'
scale=3.5/18.4
root.scale=(scale,)*3
root['production_scale']=scale;root['production_length_m']=18.5*scale
root['runtime_beam_object']='LOD0_AntiShipBlade_Beam'
root['export_notes']='Switch the beam group; solid cutting blade and reactor remain visible.'
for o in sc.objects:
    if o.type in ('CAMERA','LIGHT'):
        o.location*=scale
        if o.type=='CAMERA':o.data.ortho_scale*=scale
        else:
            o.data.energy*=scale*scale;o.data.size*=scale
            if o.data.type=='AREA' and o.data.shape=='RECTANGLE':o.data.size_y*=scale
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            area.spaces.active.region_3d.view_distance=4.3
            area.spaces.active.region_3d.view_location=Vector((-7.9*scale,0,0))
sc.camera=bpy.data.objects['RAIKEN_CAM_SIDE']
bpy.context.view_layer.update()
# Verify only new weapon geometry; no long game or whole-project audit is needed.
report={'scale':scale,'production_length_m':18.5*scale,'meshes':[]}
for o in joined:
    bm=bmesh.new();bm.from_mesh(o.data)
    report['meshes'].append({'name':o.name,'triangles':len(o.data.polygons),'nonmanifold_edges':sum(not e.is_manifold for e in bm.edges),'degenerate_faces':sum(f.calc_area()<1e-12 for f in bm.faces),'beam':bool(o.get('beam_component'))})
    bm.free()
report['triangles']=sum(m['triangles'] for m in report['meshes'])
report['materials']=len({m.name for o in joined for m in o.data.materials});report['mesh_count']=len(joined)
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(P/'RAIKEN_MkII_GAME.blend'))
bpy.ops.object.select_all(action='DESELECT')
root.select_set(True)
for o in root.children_recursive:o.select_set(True)
bpy.context.view_layer.objects.active=root
bpy.ops.export_scene.gltf(filepath=str(P/'exports/RAIKEN_MkII_GAME.glb'),export_format='GLB',use_selection=True,export_apply=True,export_extras=True,export_animations=False)
bpy.ops.export_scene.fbx(filepath=str(P/'exports/RAIKEN_MkII_GAME.fbx'),use_selection=True,object_types={'MESH','EMPTY'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL',axis_forward='-Z',axis_up='Y',use_mesh_modifiers=True,bake_anim=False,add_leaf_bones=False,use_custom_props=True,path_mode='COPY',embed_textures=True)
(P/'export_manifest.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('EXPORT_COMPLETE',report['mesh_count'],report['triangles'],flush=True)
