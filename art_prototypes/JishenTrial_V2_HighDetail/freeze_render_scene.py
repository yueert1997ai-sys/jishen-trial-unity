"""Freeze evaluated master meshes for stable multi-view render and verification.
The editable master is preserved; geometry/shape/materials are identical.
"""
import bpy,pathlib,json
OUT=pathlib.Path(__file__).resolve().parent;sc=bpy.context.scene
bpy.data.objects['AntiShip_Blade_Display_Root']['Beam_On']=True;bpy.context.view_layer.update()
dg=bpy.context.evaluated_depsgraph_get();snap=[]
for o in sc.objects:
    if o.type in ('MESH','CURVE') and not o.hide_render:
        snap.append((o,bpy.data.meshes.new_from_object(o.evaluated_get(dg),preserve_all_data_layers=True,depsgraph=dg),o.matrix_world.copy()))
for o,me,mw in snap:
    if o.type=='MESH':
        o.modifiers.clear();o.data=me
    else:
        name=o.name;parent=o.parent;cols=list(o.users_collection)
        bpy.data.objects.remove(o,do_unlink=True);o=bpy.data.objects.new(name,me)
        for c in cols:c.objects.link(o)
        o.parent=parent;o.matrix_world=mw
for o in list(sc.objects):
    if o.hide_render and o.type=='MESH' and 'BooleanVolume' in o.name:bpy.data.objects.remove(o,do_unlink=True)
sc['snapshot_source']='V2_HIGH_DETAIL_FINAL.blend; evaluated geometry, no design changes'
sc['snapshot_reason']='Stable rendering and visual checks; editable modifier master preserved separately'
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'MASTER_RENDER_SNAPSHOT.blend'))
print('RENDER_SNAPSHOT_SAVED',len(snap),flush=True)
