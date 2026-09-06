import bpy,bmesh,pathlib,json,math
from mathutils import Vector
OUT=pathlib.Path(__file__).resolve().parent;sc=bpy.context.scene
root=bpy.data.objects['AntiShip_Blade_Display_Root'];root['Beam_On']=True;bpy.context.view_layer.update()
asset=[o for o in sc.objects if o.type=='MESH' and not o.hide_render and o.name!='Studio_Ground']
tri=poly=0;bad=[];degenerate=[]
for o in asset:
    o.data.calc_loop_triangles();tri+=len(o.data.loop_triangles);poly+=len(o.data.polygons)
    bm=bmesh.new();bm.from_mesh(o.data)
    n=sum(not e.is_manifold for e in bm.edges);z=sum(f.calc_area()<1e-12 for f in bm.faces)
    if n:bad.append({'object':o.name,'nonmanifold_edges':n})
    if z:degenerate.append({'object':o.name,'zero_area_faces':z})
    bm.free()
def box(objects):
    pts=[o.matrix_world@Vector(v) for o in objects for v in o.bound_box]
    mn=[min(p[i] for p in pts) for i in range(3)];mx=[max(p[i] for p in pts) for i in range(3)]
    return {'min':mn,'max':mx,'dimensions':[b-a for a,b in zip(mn,mx)]}
named={}
for label,prefix in [('head','Helmet_Main_Faceted'),('chest','Thorax_Pectoral'),('waist','Waist_Narrow'),('thigh','Thigh_Main_Lateral_Shell.R'),('calf','Calf_Deep_Main_Nacelle.R'),('backpack','Backpack_'),('blade','Blade_')]:
    named[label]=box([o for o in asset if o.name.startswith(prefix)])
beam=bpy.data.objects['Blade_Continuous_Energy_Edge'];toggle=[]
for val in (True,False,True):
    root['Beam_On']=val;root.update_tag();sc.frame_set(sc.frame_current);bpy.context.view_layer.update();toggle.append({'switch':val,'hidden_render':beam.hide_render,'hidden_viewport':beam.hide_viewport})
data={'reopened_file':bpy.data.filepath,'blender':bpy.app.version_string,'mesh_objects':len(asset),'triangles':tri,'polygons':poly,
    'nonmanifold_objects':bad,'zero_area_objects':degenerate,'bounds_m':named,'beam_switch_test':toggle,
    'coordinate_system':'meters, Z up, -Y front, anatomical right -X','crown_height_m':3.25,'blade_design_ratio':.7,
    'collection_names':[c.name for c in sc.collection.children],'armatures':sum(o.type=='ARMATURE' for o in sc.objects)}
(OUT/'logs'/'master_audit.json').write_text(json.dumps(data,indent=2),encoding='utf8')
print('MASTER_AUDIT',json.dumps(data),flush=True)
