import bpy
import math
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree

out=Path(__file__).resolve().parent
scene=bpy.data.scenes['MARE-07 | Abandoned Lunar Service Yard']
bpy.context.window.scene=scene
for ob in scene.objects:
    if ob.type=='FONT':
        ob.visible_shadow=False
rover_id=bpy.data.objects['Rover front stencil']
rover_id.location.y=-2.417
rover_id.location.z=1.949
rover_id.rotation_euler.x=math.atan2(1.41,.52)
rover_id.data.size=.155
# Conform tread marks to the actual triangulated ground instead of raised blocks.
ground=bpy.data.objects['Sculpted regolith with impact basins']
bpy.context.view_layer.update()
bvh=BVHTree.FromObject(ground,bpy.context.evaluated_depsgraph_get())
for ob in list(scene.objects):
    if not ob.name.startswith('Old rover tread impression'):continue
    if ob.data.name.startswith('Ground-conforming compressed tread'):continue
    points=[]
    for x,y in [(-.265,-.037),(.265,-.037),(.265,.037),(-.265,.037)]:
        w=ob.matrix_world@Vector((x,y,0))
        hit,normal,index,distance=bvh.ray_cast(Vector((w.x,w.y,10)),Vector((0,0,-1)))
        if hit is None:raise RuntimeError('Missing terrain beneath tread mark')
        points.append((w.x,w.y,hit.z+.0015))
    me=bpy.data.meshes.new('Ground-conforming compressed tread')
    me.from_pydata(points,[],[(0,1,2,3)])
    me.materials.append(bpy.data.materials['LUNA_compressed regolith'])
    old=ob.data
    ob.data=me
    ob.location=(0,0,0)
    ob.rotation_euler=(0,0,0)
    ob.scale=(1,1,1)
    ob.visible_shadow=False
    if old.users==0:bpy.data.meshes.remove(old)
mat=bpy.data.materials['LUNA_lunar regolith']
for n in mat.node_tree.nodes:
    if n.type=='TEX_NOISE' and n.inputs['Scale'].default_value<10:
        n.inputs['Scale'].default_value=1.3
    if n.type=='VALTORGB':
        n.color_ramp.elements[0].color=(.185,.20,.218,1)
        n.color_ramp.elements[1].color=(.248,.26,.273,1)
    if n.type=='BUMP':
        n.inputs['Strength'].default_value=.18
        n.inputs['Distance'].default_value=.028
scene.camera=bpy.data.objects['01 | Establishing view']
scene.render.resolution_x=1800
scene.render.resolution_y=1350
scene.cycles.samples=48
scene.render.filepath=str(out/'renders/01_ESTABLISHING.png')
if bpy.data.scenes.get('Scene'):
    bpy.data.scenes['Scene'].name='_Original Startup - preserved'
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            area.spaces.active.region_3d.view_perspective='CAMERA'
            area.spaces.active.overlay.show_overlays=False
            area.spaces.active.shading.type='MATERIAL'
            area.spaces.active.shading.use_scene_world=True
            area.spaces.active.shading.use_scene_lights=True
bpy.ops.wm.save_as_mainfile(filepath=str(out/'MARE07_LUNAR_BASE_MASTER.blend'))
print('FINAL_SCENE_SAVED',flush=True)
