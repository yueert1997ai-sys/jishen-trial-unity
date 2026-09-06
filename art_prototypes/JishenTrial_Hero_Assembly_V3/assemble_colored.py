"""Run in Blender 5.2.1: blender -b --python-exit-code 1 --python assemble_colored.py.
Creates a new editable body + HEAD_V2 assembly. Never saves over either source.
"""
import bpy, pathlib, json
from mathutils import Vector, Matrix
OUT=pathlib.Path(__file__).resolve().parent
SRC=OUT.parent/'JishenTrial_V2_HighDetail'
HEAD=OUT.parent/'JishenTrial_HEAD_V2'/'HEAD_V2.blend'
bpy.ops.wm.open_mainfile(filepath=str(SRC/'V2_HIGH_DETAIL_FINAL.blend'))
sc=bpy.context.scene
old=bpy.data.objects['Head']
removed=[o.name for o in old.children_recursive]
for o in list(old.children_recursive):bpy.data.objects.remove(o,do_unlink=True)
with bpy.data.libraries.load(str(HEAD),link=False) as (src,dst):
    dst.collections=['HEAD_V2 | PRIMARY FORMS ONLY']
hc=dst.collections[0];sc.collection.children.link(hc);hc.name='HEAD_V2 | colored editable parts'
bpy.context.view_layer.update()
root=bpy.data.objects['HEAD_V2_ROOT']
mw=root.matrix_world.copy();root.parent=old;root.matrix_world=mw
bpy.context.view_layer.update()
assert abs(root.matrix_world.translation.z-3.007888)<.001,list(root.matrix_world.translation)
root['source_file']=str(HEAD)
root['review_status']='Side form remains rejected by user; assembly and color review only'
names={
    'navy':'01_Painted_Military_Navy','navy2':'02_Navy_Secondary_Panels',
    'white':'04_Offwhite_Ceramic_Armor','frame':'05_Black_Mechanical_Frame',
    'metal':'06_Dark_Gunmetal','black':'12_Optical_Recess','eye':'10_Blue_Sensor_Glass'}
mats={k:bpy.data.materials[v] for k,v in names.items()}
# Head uses the body's palette; two actual recessed lenses get the shared sensor shader.
# No additional forms, bolts, decals or weathering are introduced.
palette={1:'navy2',2:'navy',3:'navy',4:'navy',5:'frame',6:'white',7:'frame',
         8:'eye',9:'metal',10:'navy2',11:'frame',12:'navy',13:'metal',14:'frame',15:'metal',16:'navy'}
headobjs=[o for o in hc.all_objects if o.type=='MESH']
for o in headobjs:
    index=int(o.name[:2]);o.data.materials.clear();o.data.materials.append(mats[palette[index]])
    for f in o.data.polygons:f.material_index=0
    o['assembly_color_family']=palette[index]
    o['source_head_name']=o.name
    if index==8:o.name=o.name.replace('Clay_NoEmission','Blue_Sensor')
# Neck yaw ring stays on the thorax-side interface; the whole head remains editable.
for index in (14,16):
    o=next(o for o in headobjs if o.name.startswith(f'{index:02d}_'))
    w=o.matrix_world.copy();o.parent=old.parent;o.matrix_world=w
    o['mount_role']='Thorax-side neck interface; does not follow later head pitch'
bpy.context.view_layer.update()
# User's whole-body feedback: the 195 mm helmet was only 11.4% of the outer
# shoulder width. Enlarge all head forms uniformly, with a small lower seating
# offset. Do not widen the face independently or distort the approved source shape.
HEAD_SCALE=1.50;HEAD_SEAT_DROP=.020
pivot=root.matrix_world.translation.copy()
head_transform=Matrix.Translation(Vector((0,0,-HEAD_SEAT_DROP))) @ Matrix.Translation(pivot) @ Matrix.Scale(HEAD_SCALE,4) @ Matrix.Translation(-pivot)
for o in headobjs:
    if int(o.name[:2])<=12:o.matrix_world=head_transform@o.matrix_world
bpy.context.view_layer.update()
for m in mats.values():
    p=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
    m.diffuse_color=p.inputs['Base Color'].default_value

# Measured torso surface z=2.92825, head collar base z=2.967328.
# Bridge their 39 mm gap with a short physical load-bearing neck seat.
bpy.ops.mesh.primitive_cylinder_add(vertices=20,radius=.038,depth=.052,location=(0,.0195,2.948))
seat=bpy.context.object;seat.name='17_Neck_Load_Bearing_Pedestal'
for c in list(seat.users_collection):c.objects.unlink(seat)
hc.objects.link(seat);seat.data.materials.append(mats['frame'])
w=seat.matrix_world.copy();seat.parent=old.parent;seat.matrix_world=w
seat['mount_role']='Measured neck-to-thorax gap bridge; fixed to thorax'
seat['assembly_color_family']='frame';headobjs.append(seat)
bevel=seat.modifiers.new('Neck_Seat_Edge','BEVEL');bevel.width=.0008;bevel.segments=2

# A single exportable origin is shared by the body and the separately staged weapon.
assembly=bpy.data.objects.new('JishenTrial_Hero_Assembly_V3',None);sc.collection.objects.link(assembly)
for name in ('JISHEN_Master_Root','AntiShip_Blade_Display_Root'):
    o=bpy.data.objects[name];w=o.matrix_world.copy();o.parent=assembly;o.matrix_world=w
bpy.context.view_layer.update()
headverts=[o.matrix_world@v.co for o in headobjs for v in o.data.vertices]
assert min(v.z for v in headverts)>2.9,(min(v.z for v in headverts),max(v.z for v in headverts))
assembly['crown_height_m']=3.351056
assembly['asset_state']='Colored assembly preview; unrigged; not an approved final head'
beam=bpy.data.objects['AntiShip_Blade_Display_Root'];beam['Beam_On']=True;beam.update_tag()
sc.frame_set(sc.frame_current);bpy.context.view_layer.update()

def camera(name,at,target,scale):
    obj=bpy.data.objects.get(name)
    if obj is None:
        obj=bpy.data.objects.new(name,bpy.data.cameras.new(name));bpy.data.collections['CAMERAS'].objects.link(obj)
    obj.location=at;obj.rotation_euler=(Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()
    obj.data.type='ORTHO';obj.data.ortho_scale=scale;obj.data.clip_start=.02;obj.data.clip_end=100
    return obj
camera('ASSEMBLY_FRONT',(-.12,-12,1.7),(-.12,0,1.7),3.85)
camera('ASSEMBLY_BACK',(-.12,12,1.7),(-.12,0,1.7),3.85)
camera('ASSEMBLY_LEFT',(12,0,1.7),(0,0,1.7),3.85)
camera('ASSEMBLY_RIGHT',(-12,0,1.7),(0,0,1.7),3.85)
camera('ASSEMBLY_3Q',(-6.2,-12,4.7),(-.12,0,1.7),4.05)
camera('ASSEMBLY_HEAD',(-2.6,-6,3.8),(0,0,3.055),.86)
camera('ASSEMBLY_HEAD_SIDE',(5,-.01,3.17),(0,-.01,3.17),.62)
camera('ASSEMBLY_HEAD_FRONT',(0,-6,3.17),(0,0,3.17),.62)
sc.camera=bpy.data.objects['ASSEMBLY_3Q']
sc['assembly_sources']='V2_HIGH_DETAIL_FINAL body + HEAD_V2 ITER05'
sc['head_fit']='User size correction: head forms uniformly 1.5x, seated 20mm lower; crown 3.351056m. Neck bearing and torso preserved.'
sc['head_status']='User: side still unsatisfactory. Not marked approved.'
sc['rigging_state']='Unrigged rigid assembly. No animation controller compatibility claimed.'
sc.world.color=(.6,.6,.6)
sc.render.engine='CYCLES';sc.cycles.samples=48;sc.cycles.use_denoising=True
sc.view_settings.view_transform='AgX';sc.view_settings.exposure=0
sc.render.resolution_x=1280;sc.render.resolution_y=1600;sc.render.resolution_percentage=100
for layer in sc.view_layers:layer.material_override=None
for screen in bpy.data.screens:
    for a in screen.areas:
        if a.type=='VIEW_3D':
            v=a.spaces.active;v.clip_start=.02;v.clip_end=25
            v.overlay.show_relationship_lines=False;v.overlay.show_extras=False
            v.shading.type='SOLID';v.shading.color_type='MATERIAL'
            v.region_3d.view_perspective='CAMERA'
for name in ('assemble_colored.py',):
    t=bpy.data.texts.load(str(OUT/name));t.name=name+' | executed'
bpy.context.preferences.filepaths.save_version=0
bpy.ops.object.select_all(action='DESELECT');root.select_set(True);bpy.context.view_layer.objects.active=root
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'JishenTrial_ASSEMBLED_MASTER.blend'))
report={'removed_old_head_objects':removed,'new_head_parts':22,'new_neck_interface_parts':1,
        'head_root_world':list(root.matrix_world.translation),'color_map':palette,
        'blender_version':bpy.app.version_string,'head_source':str(HEAD),
        'head_uniform_scale':HEAD_SCALE,'head_seat_drop_m':HEAD_SEAT_DROP,
        'helmet_width_before_m':.19498,'helmet_width_after_m':.29247,'crown_height_m':3.351056}
(OUT/'logs'/'assembly.json').write_text(json.dumps(report,indent=2),encoding='utf8')

# Evaluated snapshot for stable rendering; editable master is already saved above.
dg=bpy.context.evaluated_depsgraph_get();snap=[]
for o in sc.objects:
    if o.type in ('MESH','CURVE') and not o.hide_render:
        snap.append((o,bpy.data.meshes.new_from_object(o.evaluated_get(dg),preserve_all_data_layers=True,depsgraph=dg),o.matrix_world.copy()))
for o,me,w in snap:
    if o.type=='MESH':o.modifiers.clear();o.data=me
    else:
        name=o.name;p=o.parent;cols=list(o.users_collection)
        bpy.data.objects.remove(o,do_unlink=True);o=bpy.data.objects.new(name,me)
        for c in cols:c.objects.link(o)
        o.parent=p;o.matrix_world=w
sc['snapshot_source']='JishenTrial_ASSEMBLED_MASTER.blend'
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'ASSEMBLY_RENDER_SNAPSHOT.blend'))
print('ASSEMBLY_COMPLETE',len(headobjs),len(snap),flush=True)
