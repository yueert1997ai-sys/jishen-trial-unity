"""Focused checks on saved geometry, not an aesthetic-pass assertion."""
import bpy,json,pathlib,bmesh
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from bpy_extras.object_utils import world_to_camera_view
sc=bpy.context.scene;p=pathlib.Path(bpy.data.filepath).parent
cfg=json.loads(sc['parameters_json']);U=.26;BASE=2.9848
def zface(z):return .47+(z-.47)*cfg.get('lower_face_factor',1) if z<.47 else z
def ray(x,z):
    origin=Vector((x*U,-2,BASE+z*U));direction=Vector((0,1,0));hits=[]
    for name,bvh in bvhs.items():
        loc,no,idx,dist=bvh.ray_cast(origin,direction,4)
        if loc:hits.append({'object':name,'y_design_units':round(loc.y/U,5),'distance_m':dist})
    return sorted(hits,key=lambda v:v['distance_m'])
dg=bpy.context.evaluated_depsgraph_get()
bvhs={}
for o in sc.objects:
    if o.type!='MESH':continue
    bm=bmesh.new();bm.from_object(o,dg);bm.transform(o.matrix_world)
    bvhs[o.name]=BVHTree.FromBMesh(bm);bm.free()
probe={}
for name,x,z in [('FOREHEAD_CENTER',0,.70),('FOREHEAD_R',-.22,.75),('OPTIC_R_INNER',-.13,zface(.448)),('OPTIC_R_OUTER',-.207,zface(.466)),('OPTIC_L_INNER',.13,zface(.448)),('OPTIC_L_OUTER',.207,zface(.466)),('MASK_RIDGE',0,zface(.22)),('BROW_CENTER',0,.46)]:
    probe[name]=ray(x*cfg.get('head_width_factor',1),z)[:5]
projected={}
for camname in ('HEAD_FRONT','HEAD_SIDE','HEAD_BACK','HEAD_3Q'):
    cam=bpy.data.objects[camname];projected[camname]={}
    for o in bpy.data.collections['LANDMARKS | inspection datums'].objects:
        v=world_to_camera_view(sc,cam,o.location)
        projected[camname][o.name[3:]]=[round(v.x*sc.render.resolution_x,2),round((1-v.y)*sc.render.resolution_y,2)]
raw=[];evaluated=[]
for o in sc.objects:
    if o.type!='MESH':continue
    bm=bmesh.new();bm.from_mesh(o.data)
    raw.append({'object':o.name,'nonmanifold_edges':sum(not e.is_manifold for e in bm.edges),'loose_vertices':sum(not v.link_faces for v in bm.verts),'min_face_area_m2':min((f.calc_area() for f in bm.faces),default=0)})
    bm.free()
    ev=o.evaluated_get(dg);me=ev.to_mesh();bm=bmesh.new();bm.from_mesh(me)
    evaluated.append({'object':o.name,'nonmanifold_edges':sum(not e.is_manifold for e in bm.edges),'zero_area_faces':sum(f.calc_area()<1e-13 for f in bm.faces)})
    bm.free();ev.to_mesh_clear()
result={'file_reopened':bpy.data.filepath,'primary_mesh_count':len(raw),'materials':len([m for m in bpy.data.materials if m.users]),'raw_meshes':raw,'evaluated_meshes':evaluated,'front_surface_probes':probe,'landmarks_pixels':projected,'note':'Ray probes and manifold checks are mechanical evidence; aesthetic review remains visual.'}
(p/'geometry_inspection.json').write_text(json.dumps(result,indent=2),encoding='utf8')
print(json.dumps({'meshes':len(raw),'raw_nonmanifold_total':sum(v['nonmanifold_edges'] for v in raw),'eval_nonmanifold_total':sum(v['nonmanifold_edges'] for v in evaluated),'frontmost_at_probes':{k:v[0]['object'] if v else None for k,v in probe.items()}},indent=2),flush=True)
