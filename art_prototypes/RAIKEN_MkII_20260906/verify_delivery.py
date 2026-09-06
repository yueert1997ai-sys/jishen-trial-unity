"""Quick native toggle and actual FBX/GLB reimport checks for the new weapon."""
import bpy,pathlib,json,math
from mathutils import Vector
P=pathlib.Path(__file__).resolve().parent
report={}
def stat(objects):
    obs=[o for o in objects if o.type=='MESH']
    vs=[o.matrix_world@v.co for o in obs for v in o.data.vertices]
    return {'meshes':len(obs),'triangles':sum(sum(len(f.vertices)-2 for f in o.data.polygons) for o in obs),'min':[min(v[i] for v in vs) for i in range(3)],'max':[max(v[i] for v in vs) for i in range(3)],'materials':sorted({m.name for o in obs for m in o.data.materials if m})}
bpy.ops.wm.open_mainfile(filepath=str(P/'RAIKEN_MkII_GAME.blend'))
root=bpy.data.objects['RAIKEN_MkII_Grip_Root']
native=stat(root.children_recursive);report['native_game']=native
beams=[o for o in root.children_recursive if o.type=='MESH' and o.get('beam_component')]
solids=[o for o in root.children_recursive if o.type=='MESH' and not o.get('beam_component')]
root['Beam_On']=False;root.update_tag();bpy.context.view_layer.update()
report['beam_switch']={'beam_hidden_when_off':all(o.hide_render for o in beams),'solid_visible_when_off':all(not o.hide_render for o in solids),'beam_meshes':len(beams),'solid_meshes':len(solids)}
assert all(report['beam_switch'][k] for k in ('beam_hidden_when_off','solid_visible_when_off'))
root['Beam_On']=True;root.update_tag();bpy.context.view_layer.update()
assert all(not o.hide_render for o in beams)
for format in ('fbx','glb'):
    bpy.ops.wm.open_mainfile(filepath=str(P/'RAIKEN_MkII_GAME.blend'))
    root=bpy.data.objects['RAIKEN_MkII_Grip_Root']
    for o in list(root.children_recursive)+[root]:bpy.data.objects.remove(o,do_unlink=True)
    previous=set(bpy.context.scene.objects)
    if format=='fbx':bpy.ops.import_scene.fbx(filepath=str(P/'exports/RAIKEN_MkII_GAME.fbx'))
    else:bpy.ops.import_scene.gltf(filepath=str(P/'exports/RAIKEN_MkII_GAME.glb'))
    imported=set(bpy.context.scene.objects)-previous;bpy.context.view_layer.update()
    s=stat(imported);s['bounds_error_m']=max(abs(s[k][i]-native[k][i]) for k in ('min','max') for i in range(3))
    s['has_beam_group']=any(o.name.startswith('LOD0_AntiShipBlade_Beam') for o in imported)
    s['has_tip_socket']=any(o.name.startswith('RAIKEN_BLADE_TIP') for o in imported)
    assert s['bounds_error_m']<.00002,(format,s['bounds_error_m'])
    assert s['has_beam_group'] and s['has_tip_socket']
    report[format]=s
    if format=='fbx':
        sc=bpy.context.scene;sc.render.resolution_x=2000;sc.render.resolution_y=680;sc.cycles.samples=20
        sc.camera=bpy.data.objects['RAIKEN_CAM_SIDE'];sc.render.filepath=str(P/'renders/09_ACTUAL_FBX_REIMPORT.png')
        bpy.ops.render.render(write_still=True)
report['passed']=True
(P/'verification.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('VERIFICATION_PASS',json.dumps(report),flush=True)
