"""Reopen saved model, audit mesh bounds and render fixed clay cameras."""
import bpy, pathlib, time, json, sys, bmesh
ROOT=pathlib.Path(__file__).resolve().parent
sc=bpy.context.scene;dest=pathlib.Path(bpy.data.filepath).parent
assert sc.get('iteration'), 'Open a HEAD_V2 iteration .blend first'
sc.render.resolution_percentage=100
report=[]
pref=bpy.context.preferences.addons['cycles'].preferences
try:
    pref.compute_device_type='OPTIX';pref.get_devices()
    for d in pref.devices:d.use=d.type=='OPTIX'
    sc.cycles.device='GPU'
except Exception:sc.cycles.device='CPU'
shots=['HEAD_FRONT','HEAD_SIDE','HEAD_3Q']
if 'back' in sys.argv:shots.append('HEAD_BACK')
for name in shots:
    sc.camera=bpy.data.objects[name];sc.render.filepath=str(dest/(name+'.png'));t=time.time()
    bpy.ops.render.render(write_still=True)
    report.append({'camera':name,'path':sc.render.filepath,'seconds':round(time.time()-t,2),'resolution':[sc.render.resolution_x,sc.render.resolution_y]})
    print('RENDERED',name,flush=True)
audit=[]
for o in sc.objects:
    if o.type!='MESH':continue
    bm=bmesh.new();bm.from_mesh(o.data)
    audit.append({'name':o.name,'vertices':len(bm.verts),'faces':len(bm.faces),'nonmanifold_edges':sum(not e.is_manifold for e in bm.edges)})
    bm.free()
(dest/'render_reopen_audit.json').write_text(json.dumps({'reopened_blend':bpy.data.filepath,'views':report,'raw_editable_meshes':audit},indent=2),encoding='utf8')
print('ACTUAL_REOPEN_RENDER_COMPLETE',flush=True)
