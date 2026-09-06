"""Render authentic orthographic views of the saved presentation derivatives."""
import bpy, pathlib, sys, hashlib, json, math
from mathutils import Vector

W = pathlib.Path(__file__).resolve().parent
D = W/'iteration_v3/social_showcase_20260906'
args = dict(a.split('=', 1) for a in sys.argv if '=' in a)
body = args.get('body') == '1'
source = D/('VALKYR_V3_BODY_COMPARISON.blend' if body else 'VALKYR_V3_SOCIAL_DISPLAY.blend')
R = D/args.get('out', 'renders')
R.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(source))
sc = bpy.context.scene
dg = bpy.context.evaluated_depsgraph_get()
snap, points, body_points, head_points = [], [], [], []
head_branch = set(bpy.data.objects['Head'].children_recursive)
for o in sc.objects:
    if o.type == 'MESH' and not o.hide_render:
        me = bpy.data.meshes.new_from_object(o.evaluated_get(dg), preserve_all_data_layers=True, depsgraph=dg)
        ps = [o.matrix_world @ v.co for v in me.vertices]
        points.extend(ps)
        if not o.name.startswith('RK_'):
            body_points.extend(ps)
        if o in head_branch:
            head_points.extend(ps)
        snap.append((o, me))
for o, me in snap:
    o.animation_data_clear()
    o.modifiers.clear()
    o.data = me
for o in sc.objects:
    if o.type == 'LIGHT':
        o.hide_render = True

sc.render.engine = 'CYCLES'
sc.cycles.samples = int(args.get('samples', '48'))
sc.cycles.use_denoising = True
pref = bpy.context.preferences.addons['cycles'].preferences
pref.compute_device_type = 'OPTIX'
pref.get_devices()
for dev in pref.devices:
    dev.use = dev.type == 'OPTIX'
sc.cycles.device = 'GPU'
sc.render.resolution_x = 1400
sc.render.resolution_y = 1680
sc.render.resolution_percentage = int(args.get('percent', '100'))
sc.render.image_settings.file_format = 'PNG'
sc.render.image_settings.color_mode = 'RGBA'
sc.render.image_settings.color_depth = '8'
sc.render.film_transparent = True
sc.view_settings.view_transform = 'AgX'
sc.view_settings.exposure = .15
sc.view_settings.gamma = 1
for vl in sc.view_layers:
    vl.material_override = None
world = bpy.data.worlds.new('Social Studio World')
world.use_nodes = True
world.node_tree.nodes.clear()
bg = world.node_tree.nodes.new('ShaderNodeBackground')
output = world.node_tree.nodes.new('ShaderNodeOutputWorld')
bg.inputs[0].default_value = (.14, .18, .25, 1)
bg.inputs[1].default_value = .35
world.node_tree.links.new(bg.outputs[0], output.inputs[0])
sc.world = world

cd = bpy.data.cameras.new('Social Studio Camera')
cd.type = 'ORTHO'
cd.sensor_fit = 'HORIZONTAL'
cam = bpy.data.objects.new(cd.name, cd)
sc.collection.objects.link(cam)
sc.camera = cam
lights = []
for name, power, size, color in [('Key', 1050, 5, (.87,.94,1)),
                                 ('Fill', 450, 4, (.66,.82,1)),
                                 ('Rim', 1300, 3, (1,.87,.7))]:
    data = bpy.data.lights.new('Social_'+name, 'AREA')
    data.energy, data.size, data.color = power, size, color
    data.shape = 'DISK'
    ob = bpy.data.objects.new(data.name, data)
    sc.collection.objects.link(ob)
    lights.append(ob)
views = [('FRONT', (0,-1,0)), ('BACK',(0,1,0)), ('LEFT',(1,0,0)),
         ('RIGHT',(-1,0,0)), ('HERO',(.34,-1,.13)), ('BODY_FRONT',(0,-1,0)), ('HEAD',(0,-1,0))]
selected = args.get('shots', 'BODY_FRONT' if body else 'FRONT,BACK,LEFT,RIGHT,HERO').split(',')
zlo, zhi = min(p.z for p in points), max(p.z for p in points)
height = (zhi-zlo)*1.11
common_scale = height*sc.render.resolution_x/sc.render.resolution_y
manifest = {'source': str(source), 'source_sha256': hashlib.sha256(source.read_bytes()).hexdigest(), 'views': []}
for name, out in views:
    if name not in selected:
        continue
    outward = Vector(out).normalized()
    q = (-outward).to_track_quat('-Z','Y')
    right, up = q @ Vector((1,0,0)), q @ Vector((0,1,0))
    framed = head_points if name == 'HEAD' else points
    rs, us = [p.dot(right) for p in framed], [p.dot(up) for p in framed]
    target = right*((min(rs)+max(rs))/2)+up*((min(us)+max(us))/2)
    cam.location = target+outward*18
    cam.rotation_euler = q.to_euler()
    cd.ortho_scale = max(common_scale, (max(rs)-min(rs))*1.10)
    if name in ('HERO', 'BODY_FRONT', 'HEAD'):
        cd.ortho_scale = max((max(rs)-min(rs)), (max(us)-min(us))*sc.render.resolution_x/sc.render.resolution_y)*1.10
    subject = Vector((0,0,2.1))
    if name == 'HEAD':
        for o in sc.objects:
            if o.type in ('MESH','CURVE','FONT') and o not in head_branch:
                o.hide_render = True
    positions = [subject+outward*4.5-right*3.6+Vector((0,0,4.5)),
                 subject+outward*3.5+right*4.2+Vector((0,0,1.8)),
                 subject-outward*3+right*2.5+Vector((0,0,4.0))]
    for lamp, pos in zip(lights, positions):
        lamp.location = pos
        lamp.rotation_euler = (subject-pos).to_track_quat('-Z','Y').to_euler()
    sc.render.filepath = str(R/(name+'.png'))
    bpy.ops.render.render(write_still=True)
    manifest['views'].append({'name': name, 'ortho_scale': cd.ortho_scale, 'outward': list(outward)})
    print('SOCIAL_RENDER_DONE', name, flush=True)
(R/('body_render_manifest.json' if body else 'render_manifest.json')).write_text(json.dumps(manifest,indent=2),encoding='utf-8')
print('SOCIAL_RENDER_COMPLETE', flush=True)
