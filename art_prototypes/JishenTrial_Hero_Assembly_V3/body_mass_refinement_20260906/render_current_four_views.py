"""Read the current native master and render four orthographic review views only."""
import bpy, pathlib, hashlib, json, sys
from mathutils import Vector
W=pathlib.Path(__file__).resolve().parent
args={a.split('=',1)[0]:a.split('=',1)[1] for a in sys.argv if '=' in a}
SOURCE=pathlib.Path(args.get('source',str(W.parent/'JishenTrial_ASSEMBLED_MASTER.blend')))
R=pathlib.Path(args.get('out',str(W/'delivery/four_views')));R.mkdir(exist_ok=True,parents=True)
sha=hashlib.sha256(SOURCE.read_bytes()).hexdigest()
bpy.ops.wm.open_mainfile(filepath=str(SOURCE));sc=bpy.context.scene
dg=bpy.context.evaluated_depsgraph_get();snap=[];points=[]
for o in sc.objects:
    if o.type=='MESH' and not o.hide_render:
        me=bpy.data.meshes.new_from_object(o.evaluated_get(dg),preserve_all_data_layers=True,depsgraph=dg)
        points.extend(o.matrix_world@v.co for v in me.vertices)
        snap.append((o,me))
for o,me in snap:o.animation_data_clear();o.modifiers.clear();o.data=me
lo=Vector(tuple(min(p[k] for p in points) for k in range(3)))
hi=Vector(tuple(max(p[k] for p in points) for k in range(3)))
center=(lo+hi)/2;span=hi-lo
rx,ry=1800,1320
height=max(span.z,max(span.x,span.y)/(rx/ry))*1.12
for o in sc.objects:
    if o.type=='LIGHT':o.hide_render=True
sc.render.engine='CYCLES';sc.cycles.samples=32;sc.cycles.use_denoising=True
pref=bpy.context.preferences.addons['cycles'].preferences;pref.compute_device_type='OPTIX';pref.get_devices()
for dev in pref.devices:dev.use=dev.type=='OPTIX'
sc.cycles.device='GPU'
sc.render.resolution_x,sc.render.resolution_y=rx,ry;sc.render.resolution_percentage=100
sc.render.image_settings.file_format='PNG';sc.render.image_settings.color_mode='RGBA';sc.render.film_transparent=True
for vl in sc.view_layers:vl.material_override=None
cd=bpy.data.cameras.new('Four_View_Inspection');cd.type='ORTHO'
# Blender's orthographic scale is the horizontal extent for a landscape frame.
cd.ortho_scale=height*rx/ry
cam=bpy.data.objects.new('Four_View_Inspection',cd);sc.collection.objects.link(cam);sc.camera=cam
lights=[]
for name,power,size,color in [('Key',800,4,(.93,.96,1)),('Fill',520,4,(.86,.93,1)),('Rim',900,3,(1,.93,.83))]:
    data=bpy.data.lights.new('Four_View_'+name,'AREA');data.energy=power;data.shape='DISK';data.size=size;data.color=color
    obj=bpy.data.objects.new(data.name,data);sc.collection.objects.link(obj);lights.append(obj)
views=[('FRONT',Vector((0,-1,0))),('BACK',Vector((0,1,0))),
       ('LEFT',Vector((1,0,0))),('RIGHT',Vector((-1,0,0)))]
manifest={'source':str(SOURCE),'source_sha256':sha,'mass_revision':sc.get('mass_revision'),'weapon_pose_revision':sc.get('weapon_pose_revision'),'pose_reference':sc.get('pose_reference'),
          'mesh_count':len(snap),'projection':'ORTHOGRAPHIC','includes_current_weapon':True,'views':[]}
for name,outward in views:
    cam.location=center+outward*20
    cam.rotation_euler=(center-cam.location).to_track_quat('-Z','Y').to_euler()
    right=cam.rotation_euler.to_matrix()@Vector((1,0,0))
    subject=Vector((0,0,1.8))
    positions=[subject+outward*4-right*3.5+Vector((0,0,4.2)),
               subject+outward*3+right*4+Vector((0,0,2.1)),
               subject-outward*3+right*2+Vector((0,0,4.0))]
    for lamp,pos in zip(lights,positions):
        lamp.location=pos;lamp.rotation_euler=(subject-pos).to_track_quat('-Z','Y').to_euler()
    sc.render.filepath=str(R/(name+'.png'));bpy.ops.render.render(write_still=True)
    manifest['views'].append({'view':name,'file':str(R/(name+'.png')),'camera_location':list(cam.location),'ortho_scale':cd.ortho_scale})
    print('FOUR_VIEW_DONE',name,flush=True)
assert hashlib.sha256(SOURCE.read_bytes()).hexdigest()==sha,'Source changed during review rendering'
(R/'render_manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
print('FOUR_VIEWS_COMPLETE',flush=True)
