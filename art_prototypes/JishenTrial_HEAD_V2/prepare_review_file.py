"""Prepare an editable review copy; keep iteration geometry and renders intact."""
import bpy,json,pathlib
from mathutils import Vector,Matrix
ROOT=pathlib.Path(__file__).resolve().parent
sc=bpy.context.scene;cfg=json.loads(sc['parameters_json'])
assert cfg['iteration']>=4
def world(p):return Vector((p[0]*.26,p[1]*.26,2.9848+p[2]*.26))
objects=[o for o in sc.objects if o.type=='MESH']
before={o.name:[tuple(o.matrix_world@v.co) for v in o.data.vertices] for o in objects}
root=bpy.data.objects['HEAD_V2_ROOT'];root.location=world((0,.075+(.063-.075)*cfg.get('head_width_factor',1),-.049+.53*(1-cfg['lower_face_factor'])))
bpy.context.view_layer.update()
for o in objects:
    # Saved source meshes use absolute coordinates and an identity parent, so use
    # the recorded coordinates while setting anatomically useful edit origins.
    pts=[Vector(v) for v in before[o.name]]
    center=sum(pts,Vector())/len(pts)
    if o.name.startswith('12_'):
        xx=(.327-cfg.get('fin_inset',0))*cfg.get('head_width_factor',1)
        center=world((-xx if o.name.endswith('.R') else xx,-.013,.834))
    elif o.name.startswith(('13_','15_')):center=root.location.copy()
    elif o.name.startswith(('14_','16_')):
        center=world((0,.075,-.199+cfg.get('neck_ring_raise',0)+.53*(1-cfg['lower_face_factor'])))
    o.parent=None;o.matrix_world=Matrix.Identity(4)
    for v,p in zip(o.data.vertices,pts):v.co=p-center
    o.data.update();o.parent=root;o.matrix_parent_inverse=Matrix.Identity(4)
    o.matrix_basis=Matrix.Translation(center-root.location)
    o['editable_pivot']='Anatomical joint / keyed fin mount / local armor center'
bpy.context.view_layer.update()
max_err=max((Vector(p)-(o.matrix_world@v.co)).length for o in objects for p,v in zip(before[o.name],o.data.vertices))
assert max_err<1e-6,max_err
for source in ('USER_HEAD_BRIEF.txt','README.md'):
    path=ROOT/source
    if path.exists():bpy.data.texts.load(str(path))
root['acceptance']='Primary form review copy, not user-approved; no secondary or tertiary detail'
sc.camera=bpy.data.objects['HEAD_3Q']
for area in bpy.context.screen.areas:
    if area.type=='VIEW_3D':
        sp=area.spaces.active;sp.overlay.show_extras=False;sp.overlay.show_floor=False
        sp.overlay.show_axis_x=False;sp.overlay.show_axis_y=False
        sp.region_3d.view_distance=.53;sp.region_3d.view_location=world((0,0,.48))
        sp.region_3d.view_rotation=sc.camera.rotation_euler.to_quaternion();sp.region_3d.view_perspective='ORTHO'
        sp.shading.light='STUDIO';sp.shading.color_type='MATERIAL';sp.shading.show_shadows=True
        sp.shading.show_cavity=True;sp.shading.cavity_type='BOTH'
    elif area.type=='PROPERTIES':
        area.type='IMAGE_EDITOR';area.spaces.active.image=bpy.data.images.get('REFERENCE_HEAD_DETAIL.png')
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'HEAD_V2.blend'))
(ROOT/'logs'/'review_copy_pivot_check.json').write_text(json.dumps({'source_iteration':cfg['iteration'],'maximum_world_vertex_error_m':max_err,'editable_pivots':True,'body_modified':False},indent=2),encoding='utf8')
print('REVIEW_COPY_SAVED_VERTEX_PRESERVATION',max_err,flush=True)
