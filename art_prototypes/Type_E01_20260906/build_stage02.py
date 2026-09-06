"""Load approved body proportions; reconstruct the rejected head and refine details.
Outputs are isolated in stage_02. Stage_01 and the actual game stay untouched.
"""
from pathlib import Path
from types import SimpleNamespace
import bpy,bmesh,math,json,sys,ast,time
from mathutils import Vector,Matrix

ROOT=Path(__file__).resolve().parent
sys.path.insert(0,str(ROOT))
OUT=ROOT/'stage_02';OUT.mkdir(exist_ok=True);(OUT/'renders').mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'stage_01'/'TYPE_E01_STAGE01.blend'))
sc=bpy.context.scene
sc.render.filepath = str(OUT / 'renders') + '/'
# Reuse the already reviewed construction helpers, without executing the old build.
ns={'bpy':bpy,'bmesh':bmesh,'math':math,'Vector':Vector,'sc':sc,'ASSET':[]}
for key,name in {'HEAD':'01 HEAD - review secondary forms','BODY':'02 BODY - primary proportions only','PACK':'03 BACKPACK - primary masses','RIG':'04 EDITABLE PART ROOTS - no animation','STUDIO':'90 REVIEW STUDIO','CAMS':'91 REVIEW CAMERAS','REF':'99 PACKED USER REFERENCE'}.items():
    ns[key]=bpy.data.collections[name]
for key,name in {'white':'E01 | warm grey-white armor','edge':'E01 | recessed sub armor','dark':'E01 | charcoal mechanical frame','rubber':'E01 | black seals','steel':'E01 | exposed machined metal','red':'E01 | red mono sensor','red_core':'E01 | red optical core','ground':'Studio | neutral graphite'}.items():
    ns[key]=bpy.data.materials[name]
tree=ast.parse((ROOT/'build_stage01.py').read_text(encoding='utf8'))
functions=ast.Module(body=[n for n in tree.body if isinstance(n,ast.FunctionDef)],type_ignores=[])
exec(compile(functions,'stage01_geometry_library','exec'),ns)
G=SimpleNamespace(**ns)
for key,name in {'root':'TYPE_E01_MASTER_ROOT','head_root':'E01_HEAD','torso_root':'E01_CHEST','waist_root':'E01_WAIST','pack_root':'E01_BACKPACK'}.items():
    setattr(G,key,bpy.data.objects[name])
G.root['review_status']='STAGE 02 - head correction and full-body detail approval'
G.root['user_feedback']='Head likeness insufficient; make it sharper. Continue other detail work.'
G.root['next_gate']='User reviews revised head and complete equipment before final action pose.'
# Less glossy paint supports close inspection of the constructed surfaces.
for mat,color,rough,metal in ((G.white,(.50,.525,.52),.43,.22),(G.dark,(.027,.034,.040),.43,.52),(G.edge,(.108,.125,.141),.43,.55)):
    p=mat.node_tree.nodes.get('Principled BSDF')
    p.inputs['Base Color'].default_value=(*color,1);p.inputs['Roughness'].default_value=rough;p.inputs['Metallic'].default_value=metal
    mat.diffuse_color=(*color,1)
from refine_head import build as head_build
head_build(G)

head_only='--head-only' in sys.argv
if not head_only:
    from refine_body import build as body_build
    from build_rifle import build as rifle_build
    body_build(G)
    rifle_build(G)

# Put the soles on the review floor without changing any accepted limb ratios.
bpy.data.objects['Studio ground'].location.z=.051
bpy.data.lights['Soft face fill'].energy=12
bpy.data.lights['Cool frontal fill'].energy=300
sc.cycles.samples=32;sc.cycles.use_denoising=True
sc.camera=bpy.data.objects['E01_3Q']
sc.render.resolution_x=1050;sc.render.resolution_y=1400
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            area.spaces.active.shading.type='MATERIAL'
            area.spaces.active.overlay.show_extras=False
bpy.context.view_layer.update()
readme=bpy.data.texts.get('READ ME - FIRST APPROVAL GATE')
if readme:
    readme.clear();readme.name='READ ME - SECOND APPROVAL GATE'
    readme.write('TYPE E-01 | Stage 02\n\nHead reconstructed after feedback: recessed lens, swept helmet, narrow angular cowl.\nBody proportions continue from stage 01.\nThis is a review checkpoint, not final approval.\nFinal action pose follows user review. No game export.\nAll meshes and bevels remain editable. Original reference remains packed.\n')
blend=OUT/('TYPE_E01_HEAD_DIRECTION.blend' if head_only else 'TYPE_E01_STAGE02.blend')
bpy.ops.wm.save_as_mainfile(filepath=str(blend),compress=True)
report={'stage':'02','head_only':head_only,'saved':str(blend),'head_meshes':len(G.HEAD.objects),'body_meshes':len(G.BODY.objects),'backpack_meshes':len(G.PACK.objects),'approval_status':'awaiting user review'}
(OUT/('head_direction_report.json' if head_only else 'stage_report.json')).write_text(json.dumps(report,indent=2),encoding='utf8')
print('STAGE02 SAVED '+json.dumps(report),flush=True)
if '--render' in sys.argv:
    prefs=bpy.context.preferences.addons['cycles'].preferences
    try:
        prefs.compute_device_type='OPTIX';prefs.get_devices()
        for device in prefs.devices:device.use=device.type=='OPTIX'
        sc.cycles.device='GPU'
    except Exception:sc.cycles.device='CPU'
    shots=('HEAD_FRONT','HEAD_3Q','HEAD_SIDE') if head_only else ('HEAD_FRONT','HEAD_3Q','HEAD_SIDE','3Q','FRONT','LEFT','BACK','RIGHT','RIFLE_SIDE','RIFLE_3Q','BACKPACK_DETAIL','BODY_DETAIL')
    for shot in shots:
        if 'E01_'+shot not in bpy.data.objects:continue
        sc.camera=bpy.data.objects['E01_'+shot]
        if shot.startswith('RIFLE'):
            sc.render.resolution_x=1500;sc.render.resolution_y=650
        elif shot.startswith('HEAD') or shot.endswith('DETAIL'):
            sc.render.resolution_x=1000;sc.render.resolution_y=1000
        else:
            sc.render.resolution_x=1050;sc.render.resolution_y=1400
        # Rifle detail cameras use a separate display collection in the same saved file.
        wc=bpy.data.collections.get('06 RIFLE DETAIL DISPLAY')
        if wc:
            wc.hide_render=not shot.startswith('RIFLE')
        for c in (G.HEAD,G.BODY,G.PACK):c.hide_render=shot.startswith('RIFLE')
        w=bpy.data.collections.get('05 E-01 ASSAULT RIFLE')
        if w:w.hide_render=shot.startswith('RIFLE')
        sc.render.filepath=str(OUT/'renders'/(shot+'.png'))
        t=time.time();bpy.ops.render.render(write_still=True)
        print('STAGE02 RENDERED',shot,round(time.time()-t,2),flush=True)
