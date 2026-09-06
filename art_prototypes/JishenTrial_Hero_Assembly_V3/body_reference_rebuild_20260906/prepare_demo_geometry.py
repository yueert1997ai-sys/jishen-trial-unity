"""Derive a rigid PC-demo LOD from the completed current assembly."""
import bpy,bmesh,pathlib,json,math,sys
from mathutils import Matrix,Vector
W=pathlib.Path(__file__).resolve().parent;D=W/'delivery';D.mkdir(exist_ok=True)
sys.path.insert(0,str(W));from demo_micro_cleanup import clean
bpy.ops.wm.open_mainfile(filepath=str(W/'stage_09/ASSEMBLED.blend'));sc=bpy.context.scene
original=[o for o in sc.objects if o.type=='MESH' and not o.hide_render]
export=bpy.data.collections.new('VALKYR_DEMO_EXPORT');sc.collection.children.link(export)

def family(o):
 if o.get('beam_component'):return 'Beam'
 mats=[m for m in o.data.materials if m]
 for m in mats:
  bs=next((n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None) if m.use_nodes else None
  if bs and bs.inputs['Emission Strength'].default_value>0:return 'Sensor'
 if o.name.startswith('V3B_Sword_'):return 'Weapon'
 if any(any(t in m.name for t in ('Valkyr_Blue','Deep_Blue','Gunmetal_Armor','V3B_white','V3B_panel','V3B_edge','V3B_mark','V3H_03')) for m in mats):return 'Armor'
 return 'Mechanics'

def tris(me):me.calc_loop_triangles();return len(me.loop_triangles)
for o in original:
 ff=family(o)
 for m in o.modifiers:
  if m.type=='BEVEL':
   m.segments=1
   if (max(o.dimensions)<.028 or ff=='Sensor') and not o.name.startswith('V3H_'):m.show_render=False;m.show_viewport=False
dg=bpy.context.evaluated_depsgraph_get();snap=[]
for o in original:
 me=bpy.data.meshes.new_from_object(o.evaluated_get(dg),preserve_all_data_layers=True,depsgraph=dg);me.transform(o.matrix_world)
 ob=bpy.data.objects.new('LOD0_SRC_'+o.name,me);export.objects.link(ob)
 pa=bpy.data.objects['Head'] if o.name.startswith('V3H_') else o.parent
 ob.parent=pa;ob.matrix_world=Matrix.Identity(4);ob['rigid_segment']=pa.name if pa else 'Root';ob['atlas_family']=family(o)
 ob['source_part']=o.name;ob['approved_head_derivative']=o.name.startswith('V3H_');ob['beam_component']=bool(o.get('beam_component'));snap.append(ob)
 ext=[max(v.co[k] for v in me.vertices)-min(v.co[k] for v in me.vertices) for k in range(3)]
 ob['preserve_lod_geometry']=ob['approved_head_derivative'] or ob['atlas_family'] in ('Armor','Beam','Sensor') or min(ext)<.003
for o in original:bpy.data.objects.remove(o,do_unlink=True)
initial=sum(tris(o.data) for o in snap);protected=sum(tris(o.data) for o in snap if o['preserve_lod_geometry'])
ratio=min(.95,(138000-protected)/max(1,initial-protected));report={'source_master':'stage_09/ASSEMBLED.blend','before_lod_triangles':initial,'collapse_ratio_mechanics':ratio,'protected_armor_optics_and_thin_details_triangles':protected}

# Preserve all separate parts and all major edges. Collapse works inside each
# part; no cross-part welding and no removal of a whole silhouette component.
for i,o in enumerate(snap):
 if o['preserve_lod_geometry'] or tris(o.data)<40:continue
 dec=o.modifiers.new('LOD0 internal tessellation reduction','DECIMATE');dec.ratio=ratio;dec.use_collapse_triangulate=True
 dg=bpy.context.evaluated_depsgraph_get();me=bpy.data.meshes.new_from_object(o.evaluated_get(dg),preserve_all_data_layers=True,depsgraph=dg);o.modifiers.clear();o.data=me
 if i%200==0:print('LOD0_REDUCE',i,'/',len(snap),flush=True)

# Merge only within the existing rigid attachment and material family.
groups={}
for o in snap:groups.setdefault((o['rigid_segment'],o['atlas_family']),[]).append(o)
parts=[]
for (parent,fam),items in groups.items():
 bpy.ops.object.select_all(action='DESELECT')
 for o in items:o.select_set(True)
 bpy.context.view_layer.objects.active=items[0]
 if len(items)>1:bpy.ops.object.join()
 o=bpy.context.object;o.name='LOD0_'+parent+'_'+fam;o['rigid_segment']=parent;o['atlas_family']=fam;parts.append(o)
 # Recompute weighted corner normals after decimation; explicit triangulation
 # retains those normals for exchange formats.
 n=o.modifiers.new('LOD0 weighted corner normals','WEIGHTED_NORMAL');n.keep_sharp=True;n.weight=40
 bpy.ops.object.modifier_apply(modifier=n.name)
 t=o.modifiers.new('Explicit export triangulation','TRIANGULATE');t.keep_custom_normals=True;bpy.ops.object.modifier_apply(modifier=t.name)

for o in parts:clean(o)
total=sum(tris(o.data) for o in parts);report['triangles']=total;report['meshes']=len(parts)
report['by_family']={f:sum(tris(o.data) for o in parts if o['atlas_family']==f) for f in ('Armor','Mechanics','Weapon','Sensor','Beam')}
report['topology_issues']=[]
for o in parts:
 bm=bmesh.new();bm.from_mesh(o.data);bad=sum(not e.is_manifold for e in bm.edges);zero=sum(f.calc_area()<1e-13 for f in bm.faces);bm.free()
 if bad or zero:report['topology_issues'].append({'name':o.name,'nonmanifold_edges':bad,'degenerate_faces':zero})
print('LOD0_GEOMETRY',json.dumps(report),flush=True)
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(D/'VALKYR_LOD_GEOMETRY_WORK.blend'))
assert 80000<=total<=150000,report
assert not report['topology_issues'],report['topology_issues']

# Pack each shared-material family into one unique atlas across all rigid parts.
for fam in ('Armor','Mechanics','Weapon'):
 items=[o for o in parts if o['atlas_family']==fam];bpy.ops.object.select_all(action='DESELECT')
 for o in items:o.select_set(True)
 bpy.context.view_layer.objects.active=items[0];bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT')
 bpy.ops.uv.smart_project(angle_limit=math.radians(66),island_margin=.003,area_weight=.4,correct_aspect=True,scale_to_bounds=True)
 bpy.ops.uv.pack_islands(rotate=True,margin=.003,shape_method='AABB',scale=True)
 bpy.ops.object.mode_set(mode='OBJECT')
 for o in items:o.data.uv_layers.active.name='UV0'
 print('LOD0_UV_PACKED',fam,flush=True)
for o in parts:
 if not o.data.uv_layers:o.data.uv_layers.new(name='UV0')
sc['rigging_state']='Rigid attachment hierarchy retained; no new skinned skeleton or combat animation validation.'
sc['lod0_note']='Current approved head derivative and completed reference body, with user-requested side depth correction.'
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(D/'VALKYR_DEMO_UNBAKED.blend'))
(D/'demo_geometry_audit.json').write_text(json.dumps(report,indent=2))
