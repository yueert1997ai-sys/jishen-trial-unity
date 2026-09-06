import bpy,pathlib,json,hashlib
from mathutils import Vector
P=pathlib.Path(__file__).resolve().parent;W=P/'refinement'
bpy.ops.wm.open_mainfile(filepath=str(W/'PHASE_07.blend'))
sc=bpy.context.scene;dg=bpy.context.evaluated_depsgraph_get()
tri=poly=raw=0;meshes=[];bounds={}
for o in sc.objects:
 if o.type not in ('MESH','CURVE') or o.hide_render or o.name=='Studio_Ground':continue
 e=o.evaluated_get(dg);me=e.to_mesh();me.calc_loop_triangles();tri+=len(me.loop_triangles);poly+=len(me.polygons)
 if o.type=='MESH':raw+=len(o.data.polygons)
 vs=[o.matrix_world@v.co for v in me.vertices]
 if vs:bounds[o.name]=[[min(v[i] for v in vs) for i in range(3)],[max(v[i] for v in vs) for i in range(3)]]
 e.to_mesh_clear();meshes.append(o)
old=json.loads((P/'logs'/'refinement_inventory.json').read_text());oldnames={o['name'] for o in old};newnames={o.name for o in sc.objects}
hn=[n for n in bounds if n.startswith(('01_','02_','03_','04_'))];crown=max(bounds[n][1][2] for n in hn)
weapon=set(o.name for o in bpy.data.objects['AntiShip_Blade_Display_Root'].children_recursive)
wb=[bounds[n] for n in bounds if n in weapon];length=max(b[1][2] for b in wb)-min(b[0][2] for b in wb)
stats=dict(evaluated_triangles=tri,evaluated_polygons=poly,control_polygons=raw,mesh_curve_parts=len(meshes),crown_height_m=crown,weapon_length_m=length,weapon_height_ratio=length/crown,retained_original_object_names=len(oldnames&newnames),original_object_names=len(oldnames),new_object_names=len(newnames-oldnames),collections=[c.name for c in bpy.data.collections],master_source=str(P/'JishenTrial_ASSEMBLED_MASTER.blend'),master_source_sha256=hashlib.sha256((P/'JishenTrial_ASSEMBLED_MASTER.blend').read_bytes()).hexdigest())
assert .65<=length/crown<=.75,stats
(W/'logs'/'master_stats.json').write_text(json.dumps(stats,indent=2))
print(json.dumps(stats,indent=2))
