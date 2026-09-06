"""Executed through the local Blender Python Console after opening the master."""
import bpy,pathlib
from mathutils import Vector
OUT=pathlib.Path(__file__).resolve().parent
assert pathlib.Path(bpy.data.filepath).resolve()==(OUT/'JishenTrial_ASSEMBLED_MASTER.blend').resolve()
sc=bpy.context.scene;root=bpy.data.objects['HEAD_V2_ROOT']
root['head_only']=False;root['crown_height_m']=3.351056;root['assembly_uniform_scale']=1.5
root['acceptance']='Whole-body size corrected after user feedback; side silhouette remains unapproved.'
preview=bpy.data.images.load(str(OUT/'renders/FRONT_3Q.png'),check_existing=True);preview.pack()
main=max(bpy.context.screen.areas,key=lambda a:a.width*a.height);main.type='VIEW_3D'
sp=main.spaces.active;sp.clip_start=.02;sp.clip_end=25
sp.overlay.show_floor=False;sp.overlay.show_axis_x=False;sp.overlay.show_axis_y=False
sp.overlay.show_extras=False;sp.overlay.show_relationship_lines=False
sp.shading.type='MATERIAL';sp.shading.use_scene_world=False;sp.shading.use_scene_lights=False
sp.region_3d.view_location=Vector((-.10,0,1.70));sp.region_3d.view_distance=6.2
sp.region_3d.view_rotation=sc.camera.rotation_euler.to_quaternion();sp.region_3d.view_perspective='ORTHO'
for area in bpy.context.screen.areas:
    if area.type=='PROPERTIES':area.type='IMAGE_EDITOR';area.spaces.active.image=preview
for name in ('README.md','configure_blender_review.py'):
    path=OUT/name
    if path.exists():
        txt=bpy.data.texts.get(name) or bpy.data.texts.new(name);txt.clear();txt.write(path.read_text(encoding='utf8'))
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'JishenTrial_ASSEMBLED_MASTER.blend'))
print('COLORED_ASSEMBLY_REVIEW_WORKSPACE_SAVED')
