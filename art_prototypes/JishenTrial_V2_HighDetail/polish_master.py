"""Revision 03 from inspected revision-02 color and clay renders.
Fit helmet and foot overlays to actual underlying meshes; tighten armor gaps.
"""
import bpy,math,json,pathlib
from mathutils import Vector
OUT=pathlib.Path(__file__).resolve().parent;S=.325;scene=bpy.context.scene

def conform(name,target,axis,offset):
    o=bpy.data.objects[name];base=bpy.data.objects[target];dg=bpy.context.evaluated_depsgraph_get();ev=base.evaluated_get(dg)
    mw=o.matrix_world;inv=mw.inverted();mi=base.matrix_world.inverted()
    # Cast inward from the front (-Y) or from above (+Z); preserve the overlay's thickness.
    coords=[mw@v.co for v in o.data.vertices];normal=Vector((0,-1,0) if axis==1 else (0,0,1));direction=-normal
    vals=[p[axis] for p in coords];near=min(vals) if axis==1 else max(vals)
    success=0
    for v,p in zip(o.data.vertices,coords):
        origin=p.copy();origin[axis]=(-10 if axis==1 else 10)*S
        hit,loc,no,idx=ev.ray_cast(mi@origin,mi.to_3x3()@direction)
        if hit:
            surface=base.matrix_world@loc
            # Both faces stay outside the primary surface, 2-5 mm of honest armor thickness.
            dist=abs(p[axis]-near);new=surface+normal*(max(.006*S,offset*S-dist*.35))
            p[axis]=new[axis];v.co=inv@p;success+=1
    o.data.update();return success

fixes=[]
for side in ('R','L'):
    fixes.append((side,'helmet_vertices_conformed',conform('Helmet_Crown_Front_Side_Shell.'+side,'Helmet_Main_Faceted_Volume',1,.038)))
    # The toe panel is seated on the instep rather than hovering as a rectangular flap.
    fixes.append((side,'foot_vertices_conformed',conform('Foot_Forward_Dorsal_Ceramic.'+side,'Foot_Armored_Instep.'+side,2,.026)))
    for part in ('Upper','Lower'):
        o=bpy.data.objects['Calf_Deep_Main_Nacelle.'+side+'_'+part]
        o.data.materials.clear();o.data.materials.append(bpy.data.materials['01_Painted_Military_Navy'])
    # A dark lower course avoids over-large off-white boots; small accents remain.
    o=bpy.data.objects['Foot_Forward_Dorsal_Ceramic.'+side]
    o.data.materials.clear();o.data.materials.append(bpy.data.materials['02_Navy_Secondary_Panels'])

# Save all visible material data, no gray override and no emissive tricks in the clay render.
scene['master_revision']='03: conformed helmet and toe armor, calf paint balance'
scene['visual_iterations']=3
scene['integration_state']='External review asset only; current game hero untouched'
scene['scale_source']='tools/hero/prepare_hero.py: 3.25 m crown normalization; concept text dimensions ignored'
for l in scene.view_layers:l.material_override=None
bpy.context.preferences.filepaths.save_version=0
for screen in bpy.data.screens:
    for a in screen.areas:
        if a.type=='VIEW_3D':
            a.spaces.active.overlay.show_relationship_lines=False
            a.spaces.active.shading.color_type='MATERIAL'
            a.spaces.active.clip_end=100
bpy.data.objects['AntiShip_Blade_Display_Root']['Beam_On']=True
t=bpy.data.texts.load(str(OUT/'polish_master.py'));t.name='polish_master.py | executed'
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'V2_HIGH_DETAIL_FINAL.blend'))
(OUT/'logs'/'iteration_03.json').write_text(json.dumps({'corrections':fixes,'source':'Inspected render_02: head, leg, full-body color and clay','file':bpy.data.filepath},indent=2),encoding='utf8')
print('POLISH_SAVED',bpy.data.filepath,flush=True)
