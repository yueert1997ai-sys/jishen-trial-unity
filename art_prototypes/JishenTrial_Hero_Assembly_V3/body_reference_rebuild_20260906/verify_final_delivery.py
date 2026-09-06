import bpy,bmesh,pathlib,json,math,hashlib
from mathutils import Vector
W=pathlib.Path(__file__).resolve().parent;D=W/'delivery';R=D/'renders';report={}
report['file_sha256']={str(p.relative_to(D)):hashlib.sha256(p.read_bytes()).hexdigest() for p in (D/'VALKYR_GAME_DEMO.blend',D/'exports/VALKYR_GAME_DEMO.fbx',D/'exports/VALKYR_GAME_DEMO.glb')}
def stats(objs,topology=False):
 pts=[];tri=0;poly=0;missing=[];bad=[];uvoutside=0
 for o in objs:
  o.data.calc_loop_triangles();tri+=len(o.data.loop_triangles);poly+=len(o.data.polygons);pts.extend(o.matrix_world@v.co for v in o.data.vertices)
  if not o.data.uv_layers:missing.append(o.name)
  else:uvoutside+=sum(any(v < -1e-5 or v > 1+1e-5 for v in loop.uv) for loop in o.data.uv_layers.active.data)
  if topology:
   bm=bmesh.new();bm.from_mesh(o.data);n=sum(not e.is_manifold for e in bm.edges);z=sum(f.calc_area()<1e-13 for f in bm.faces);bm.free()
   if n or z:bad.append({'object':o.name,'nonmanifold_edges':n,'degenerate_faces':z})
 return {'meshes':len(objs),'triangles':tri,'polygons':poly,'materials':sorted({m.name for o in objs for m in o.data.materials if m}),'bounds_m':[[fn(v[k] for v in pts) for k in range(3)] for fn in (min,max)],'missing_uv':missing,'uv_outside_0_1':uvoutside,'topology_issues':bad}

bpy.ops.wm.open_mainfile(filepath=str(W.parent/'JishenTrial_ASSEMBLED_MASTER.blend'))
sc=bpy.context.scene;objs=[o for o in sc.objects if o.type=='MESH' and not o.hide_render]
report['master_control_mesh']=stats(objs)
report['master_source_identity']=hashlib.sha256((W.parent/'JishenTrial_ASSEMBLED_MASTER.blend').read_bytes()).hexdigest()==json.loads((D/'native_delivery_manifest.json').read_text())['master_sha256'];assert report['master_source_identity']
bpy.ops.wm.open_mainfile(filepath=str(D/'VALKYR_GAME_DEMO.blend'));sc=bpy.context.scene;objs=list(bpy.data.collections['VALKYR_DEMO_EXPORT'].objects)
report['demo_native']=stats(objs,True);base=report['demo_native'];assert not base['topology_issues'] and not base['missing_uv'] and not base['uv_outside_0_1']
assert len(base['materials'])==5 and 80000<=base['triangles']<=150000
report['demo_images']=[{'name':im.name,'size':list(im.size),'packed':bool(im.packed_file)} for im in bpy.data.images if im.name.startswith('VALKYR_')]
assert all(im['packed'] for im in report['demo_images'])
wr=bpy.data.objects['AntiShip_Blade_Display_Root'];beams=[o for o in objs if o['atlas_family']=='Beam']
for on in (False,True):
 wr['Beam_On']=on;wr.update_tag();sc.frame_set(sc.frame_current);bpy.context.view_layer.update();assert all(o.hide_render != on for o in beams)
report['demo_native_beam_switch_pass']=True

def configure_import_review():
 sc=bpy.context.scene
 with bpy.data.libraries.load(str(D/'VALKYR_GAME_DEMO.blend'),link=False) as (src,dst):
  dst.collections=['BODY_V3 | review lighting','BODY_V3 | reference inspection cameras'];dst.worlds=['V3B_Studio_World']
 for col in dst.collections:sc.collection.children.link(col)
 sc.world=dst.worlds[0];sc.camera=bpy.data.objects['V3B_CAM_THREE_QUARTER'];sc.render.engine='CYCLES';sc.cycles.samples=32;sc.cycles.use_denoising=True
 prefs=bpy.context.preferences.addons['cycles'].preferences;prefs.compute_device_type='OPTIX';prefs.get_devices()
 for d in prefs.devices:d.use=d.type=='OPTIX'
 sc.cycles.device='GPU';sc.render.resolution_x=sc.render.resolution_y=1500;sc.render.resolution_percentage=100;sc.render.film_transparent=True;sc.render.image_settings.file_format='PNG';sc.render.image_settings.color_mode='RGBA';sc.view_settings.view_transform='Standard'
 return sc

bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(D/'exports/VALKYR_GAME_DEMO.fbx'));bpy.context.view_layer.update()
objs=[o for o in bpy.context.scene.objects if o.type=='MESH'];report['fbx_reimport']=stats(objs,True);fb=report['fbx_reimport']
assert fb['meshes']==base['meshes'] and fb['triangles']==base['triangles'] and len(fb['materials'])==5
assert not fb['missing_uv'] and not fb['topology_issues'],fb
err=max(abs(x-y) for a,b in zip(base['bounds_m'],fb['bounds_m']) for x,y in zip(a,b));report['fbx_bounds_max_error_m']=err;assert err<.0001
report['fbx_loaded_images']=[{'name':im.name,'size':list(im.size),'available':im.has_data} for im in bpy.data.images if im.source=='FILE'];assert all(im['available'] for im in report['fbx_loaded_images'])
sc=configure_import_review();bpy.context.preferences.filepaths.save_version=0;bpy.ops.wm.save_as_mainfile(filepath=str(D/'VALKYR_FBX_REIMPORT_CHECK.blend'))
for name in ('THREE_QUARTER','LEFT'):
 sc.camera=bpy.data.objects['V3B_CAM_'+name];sc.render.filepath=str(R/('DEMO_FBX_'+name+'.png'));bpy.ops.render.render(write_still=True);print('VERIFIED_FBX_RENDER',name,flush=True)

bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.gltf(filepath=str(D/'exports/VALKYR_GAME_DEMO.glb'),merge_vertices=True);bpy.context.view_layer.update()
objs=[o for o in bpy.context.scene.objects if o.type=='MESH'];report['glb_reimport']=stats(objs);gl=report['glb_reimport'];assert gl['triangles']==base['triangles'] and len(gl['materials'])==5 and not gl['missing_uv']
err=max(abs(x-y) for a,b in zip(base['bounds_m'],gl['bounds_m']) for x,y in zip(a,b));report['glb_bounds_max_error_m']=err;assert err<.0001
(D/'final_reimport_verification.json').write_text(json.dumps(report,indent=2))
print('FINAL_REIMPORT_PASS',json.dumps({k:v for k,v in report.items() if k not in ('demo_images','fbx_loaded_images')}),flush=True)
