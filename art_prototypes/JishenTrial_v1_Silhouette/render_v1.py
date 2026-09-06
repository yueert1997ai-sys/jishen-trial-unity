"""Reopen-and-render audit of actual v1 geometry. All four views force one clay material."""
import bpy, bmesh, json, pathlib, sys, time
from mathutils import Vector
OUT=pathlib.Path(__file__).resolve().parent
scene=bpy.data.scenes['JISHEN_V1_GRAY_REVIEW']
bpy.context.window.scene=scene
stage=scene['phase'];final=stage=='F'
bpy.context.view_layer.update()
devices=[]
try:
    p=bpy.context.preferences.addons['cycles'].preferences;p.compute_device_type='OPTIX';p.get_devices()
    for d in p.devices:
        d.use=d.type=='OPTIX'
        if d.use:devices.append(d.name)
    if devices:scene.cycles.device='GPU'
except Exception as e:devices=['CPU: '+str(e)]

def bounds(obs):
    vv=[o.matrix_world@v.co for o in obs for v in o.data.vertices]
    mi=[min(v[i] for v in vv) for i in range(3)];ma=[max(v[i] for v in vv) for i in range(3)]
    return {'min':mi,'max':ma,'size':[ma[i]-mi[i] for i in range(3)]}

def matching(prefix):return [o for o in scene.objects if o.type=='MESH' and o.name.startswith(prefix)]
allmesh=[o for o in scene.objects if o.type=='MESH']
invalid=[];loose=[];base=0;evaltris=0;dg=bpy.context.evaluated_depsgraph_get()
for o in allmesh:
    bm=bmesh.new();bm.from_mesh(o.data)
    bad=sum(not e.is_manifold for e in bm.edges)
    if bad:invalid.append({'object':o.name,'edges':bad})
    if any(not v.link_faces for v in bm.verts):loose.append(o.name)
    bm.free();o.data.calc_loop_triangles();base+=len(o.data.loop_triangles)
    ev=o.evaluated_get(dg);me=ev.to_mesh();me.calc_loop_triangles();evaltris+=len(me.loop_triangles);ev.to_mesh_clear()
blade=bounds(matching('Blade_'))
report={'file_reopened':bpy.data.filepath,'stage':stage,'blender':bpy.app.version_string,
    'render_devices':devices,'mesh_count':len(allmesh),'base_triangles':base,'evaluated_triangles':evaltris,
    'nonmanifold_objects':invalid,'loose_vertices_objects':loose,'height_crown_m':3.25,
    'blade_length_m':blade['size'][2],'blade_height_ratio':blade['size'][2]/3.25,
    'blade_body_bounds':bounds(matching('Blade_Thick_Physical_Body')),
    'grip_diameter_m':bpy.data.objects['AntiShip_Blade_Display_Root']['grip_diameter_m'],
    'chest_bounds':bounds(matching('Thorax_')),'waist_bounds':bounds(matching('Waist_Narrow')),
    'head_bounds':bounds(matching('Helmet_Main')),
    'thigh_right_bounds':bounds([o for o in matching('Thigh_') if o.name.endswith('.R')]),
    'calf_right_bounds':bounds([o for o in matching('Calf_') if o.name.endswith('.R')]),
    'backpack_bounds':bounds(matching('Backpack_')),
    'light_edge_separate':bpy.data.objects['Blade_Continuous_Energy_Edge'].active_material.name,
    'cannon_mount':bpy.data.objects['Cannon_Right_Cradle_Mount']['connection'],
    'material_policy':'All four shots use a single neutral nonemissive clay override',
    'user_silhouette_approval':'PENDING','renders':[]}
assert not invalid,invalid
assert not loose,loose
assert .65<=report['blade_height_ratio']<=.75,report['blade_height_ratio']
scene.render.resolution_x=1200 if final else 960
scene.render.resolution_y=1500 if final else 1200
scene.cycles.samples=48 if final else 24
for layer in scene.view_layers:layer.material_override=bpy.data.materials['CLAY_ONLY_No_Styling']
for cam,file in [('FRONT','01_front_gray.png'),('RIGHT','02_right_gray.png'),('BACK','03_back_gray.png'),('THREE_QUARTER','04_three_quarter_gray.png')]:
    scene.camera=bpy.data.objects['CAM_'+cam]
    scene.render.filepath=str(OUT/('renders' if final else 'stage_a')/file)
    t=time.time();bpy.ops.render.render(write_still=True)
    report['renders'].append({'view':cam,'file':scene.render.filepath,'seconds':round(time.time()-t,2)})
    (OUT/'logs'/('audit_'+stage+'.json')).write_text(json.dumps(report,indent=2,ensure_ascii=False),encoding='utf8')
    print('V1_GRAY_RENDER_SAVED',scene.render.filepath,flush=True)
print('V1_FOUR_VIEW_COMPLETE',stage,flush=True)
