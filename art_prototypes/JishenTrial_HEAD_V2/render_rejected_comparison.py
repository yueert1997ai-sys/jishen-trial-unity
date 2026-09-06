"""Actual neutral clay render of the preserved rejected head, for fair comparison."""
import bpy,pathlib,json
from mathutils import Vector
ROOT=pathlib.Path(__file__).resolve().parent
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'HEAD_REJECTED_V1.blend'))
dg=bpy.context.evaluated_depsgraph_get();snapshot=[]
for o in bpy.context.scene.objects:
    if not o.name.startswith('HEAD04_') or o.type not in ('MESH','CURVE') or o.hide_render:continue
    ev=o.evaluated_get(dg);me=ev.to_mesh()
    if me:
        snapshot.append((o.name,[tuple(ev.matrix_world@v.co) for v in me.vertices],[tuple(p.vertices) for p in me.polygons],[p.use_smooth for p in me.polygons]))
        ev.to_mesh_clear()
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'HEAD_V2.blend'))
sc=bpy.context.scene;hc=bpy.data.collections['HEAD_V2 | PRIMARY FORMS ONLY'];clay=bpy.data.materials['CLAY | neutral gray, no emission, no textures']
for o in list(hc.objects):bpy.data.objects.remove(o,do_unlink=True)
for name,verts,faces,smooth in snapshot:
    me=bpy.data.meshes.new(name);me.from_pydata(verts,[],faces);me.update()
    o=bpy.data.objects.new('REJECTED_'+name,me);hc.objects.link(o);me.materials.append(clay)
    for p,s in zip(me.polygons,smooth):p.use_smooth=s
cam=bpy.data.objects['HEAD_3Q'];sc.camera=cam;cam.location.z+=.065;cam.data.ortho_scale=.60
sc.render.filepath=str(ROOT/'references'/'HEAD_REJECTED_V1_CLAY_3Q.png')
pref=bpy.context.preferences.addons['cycles'].preferences
pref.compute_device_type='OPTIX';pref.get_devices()
for d in pref.devices:d.use=d.type=='OPTIX'
sc.cycles.device='GPU';sc.cycles.samples=48
bpy.ops.render.render(write_still=True)
print('REJECTED_HEAD_ACTUAL_CLAY_RENDER_COMPLETE',len(snapshot),flush=True)
