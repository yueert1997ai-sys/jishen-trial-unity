"""Reopen the saved blend, measure actual geometry and render honest inspection views.
Run: blender -b JT_Hero_V01.blend --python render_verify.py [-- preview]
"""
import bpy
import bmesh
import json
import pathlib
import sys
import time
from mathutils import Vector

OUT=pathlib.Path(__file__).resolve().parent
scene=bpy.data.scenes['JT_HERO_V01']
if bpy.context.window: bpy.context.window.scene=scene
bpy.context.view_layer.update()
preview='preview' in sys.argv
devices=[]
try:
    prefs=bpy.context.preferences.addons['cycles'].preferences
    prefs.compute_device_type='OPTIX'
    prefs.get_devices()
    for dev in prefs.devices:
        dev.use=dev.type=='OPTIX'
        devices.append({'name':dev.name,'type':dev.type,'used':dev.use})
    if any(d['used'] for d in devices): scene.cycles.device='GPU'
except Exception as e:
    devices.append({'fallback':'CPU','reason':str(e)})

def meshes(collection):
    return [o for o in collection.all_objects if o.type=='MESH']

body=meshes(bpy.data.collections['01_MECH | editable parts'])
weapon=meshes(bpy.data.collections['02_WEAPON | body and light edge separate'])
def bounds(objects):
    vs=[o.matrix_world@v.co for o in objects for v in o.data.vertices]
    return {'min':[min(v[i] for v in vs) for i in range(3)],
            'max':[max(v[i] for v in vs) for i in range(3)]}

dg=bpy.context.evaluated_depsgraph_get()
base_tris=0; evaluated_tris=0; invalid=[]; counts=[]
for o in body+weapon:
    o.data.calc_loop_triangles();base_tris+=len(o.data.loop_triangles)
    bm=bmesh.new();bm.from_mesh(o.data)
    bad=sum(not e.is_manifold for e in bm.edges)
    if bad:invalid.append({'object':o.name,'nonmanifold_edges':bad})
    bm.free()
    ev=o.evaluated_get(dg);md=ev.to_mesh();md.calc_loop_triangles()
    evaluated_tris+=len(md.loop_triangles)
    counts.append({'name':o.name,'vertices':len(o.data.vertices),'base_triangles':len(o.data.loop_triangles),'evaluated_triangles':len(md.loop_triangles),
                   'parent':o.parent.name if o.parent else None})
    ev.to_mesh_clear()

bb=bounds(body);wb=bounds(weapon)
shoulders=bounds([o for o in body if o.name.startswith('Shoulder_')])
waist=bounds([bpy.data.objects['Waist_Flexible_Core']])
report={'reopened_file':bpy.data.filepath,'blender':bpy.app.version_string,'scene':scene.name,
        'body_bounds_m':bb,'weapon_bounds_m':wb,'body_mesh_count':len(body),'weapon_mesh_count':len(weapon),
        'crown_height_m':scene['height_to_crown_m'],
        'measured_weapon_total_m':wb['max'][2]-wb['min'][2],
        'measured_blade_to_crown_ratio':(wb['max'][2]-wb['min'][2])/scene['height_to_crown_m'],
        'shoulder_width_m':shoulders['max'][0]-shoulders['min'][0],
        'waist_core_width_m':waist['max'][0]-waist['min'][0],
        'hip_pivot_height_m':bpy.data.objects['Leg_Thigh.R'].matrix_world.translation.z,
        'base_triangles':base_tris,'evaluated_triangles_with_bevel':evaluated_tris,
        'nonmanifold_objects':invalid,'light_edge_independent':bpy.data.objects['Weapon_Light_Edge'].type=='MESH',
        'render_devices':devices,'objects':counts,'renders':[]}
assert abs(report['measured_blade_to_crown_ratio']-.7)<.003,report['measured_blade_to_crown_ratio']
assert len(invalid)==0,invalid
assert bpy.data.objects['Cannon_Right_Yaw_Pivot'].matrix_world.translation.x<0
assert len([o for o in body if o.name.startswith('Head_Eye_Sensor.')])==2

shots=[('Camera_THREE_QUARTER','04_three_quarter',False),('Camera_FRONT','01_front',False),
       ('Camera_RIGHT_SIDE','02_right_side',False),('Camera_BACK','03_back',False),
       ('Camera_THREE_QUARTER','05_clay_inspection',True),('Camera_FRONT','06_clay_front',True),
       ('Camera_DETAIL_HEAD','07_head_and_cannon',False)]
if preview: shots=shots[:2]
scene.render.resolution_x=840 if preview else 1200
scene.render.resolution_y=1050 if preview else 1500
scene.render.resolution_percentage=100
scene.cycles.samples=24 if preview else 64
for cam,name,clay in shots:
    scene.camera=bpy.data.objects[cam]
    for layer in scene.view_layers:
        layer.material_override=bpy.data.materials['M90_Inspection_Clay'] if clay else None
    path=OUT/('progress' if preview else 'renders')/(name+'.png')
    scene.render.filepath=str(path)
    start=time.time()
    bpy.ops.render.render(write_still=True,scene=scene.name)
    report['renders'].append({'path':str(path),'camera':cam,'clay':clay,'seconds':round(time.time()-start,2)})
    print('JT_RENDER_SAVED',path,flush=True)
    (OUT/'logs'/('preview_report.json' if preview else 'verification_report.json')).write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf8')
for layer in scene.view_layers:layer.material_override=None
print('JT_REOPEN_AND_RENDER_COMPLETE',flush=True)
