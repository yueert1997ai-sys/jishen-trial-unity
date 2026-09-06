"""V01 visual corrections after viewing the actual first two preview renders.
Run in Blender after opening JT_Hero_V01.blend. Idempotent visual refinements.
"""
import bpy
import pathlib
import json
from mathutils import Vector
OUT=pathlib.Path(__file__).resolve().parent
scene=bpy.data.scenes['JT_HERO_V01']
if bpy.context.window: bpy.context.window.scene=scene

palette={
    'M01_Military_Navy':((.014,.027,.057),.16,.47),
    'M02_Navy_Upper_Facets':((.030,.055,.108),.20,.44),
    'M03_Graphite_Armor':((.024,.029,.039),.28,.48),
    'M04_Joint_Frame':((.012,.017,.025),.48,.43),
    'M05_Mechanical_Steel':((.125,.158,.194),.66,.36),
    'M06_Offwhite_Accent':((.52,.55,.59),.20,.46),
}
for name,(rgb,metal,rough) in palette.items():
    m=bpy.data.materials[name]
    m.diffuse_color=(*rgb,1)
    p=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
    p.inputs['Base Color'].default_value=(*rgb,1)
    p.inputs['Metallic'].default_value=metal
    p.inputs['Roughness'].default_value=rough
    for o in scene.objects:
        if o.type=='MESH' and o.active_material==m: o.color=m.diffuse_color

# Thin one-sided surface eliminates the black vertical slab edge in orthographic shots.
floor=bpy.data.objects['Studio_Floor']
if len(floor.data.vertices)!=4:
    plane=bpy.data.meshes.new('Studio_Floor_Plane_Mesh')
    plane.from_pydata([(-40,-40,-.004),(40,-40,-.004),(40,40,-.004),(-40,40,-.004)],[],[(0,1,2,3)])
    plane.materials.append(bpy.data.materials['M91_Light_Grey_Background'])
    floor.data=plane
    floor.location=(0,0,0)

sword=bpy.data.objects['Weapon_ShipCleaver_ROOT']
sword.location=Vector((-3.25,-1.85,7.68))*.325
sword['presentation']='Detached display position forward and to character right, clear in orthographic side view'
camera=bpy.data.objects['Camera_RIGHT_SIDE']
camera.location=(-10,-.12,1.73)
camera.rotation_euler=(Vector((0,-.12,1.73))-camera.location).to_track_quat('-Z','Y').to_euler()
scene.camera=bpy.data.objects['Camera_THREE_QUARTER']
scene['visual_revision']='V01.1: darker painted navy, plane backdrop, clearer side-view weapon display'
for screen in bpy.data.screens:
    for a in screen.areas:
        if a.type=='VIEW_3D':
            a.spaces.active.shading.type='SOLID'
            a.spaces.active.shading.color_type='MATERIAL'
            a.spaces.active.overlay.show_extras=False
            a.spaces.active.overlay.show_floor=False
            a.spaces.active.region_3d.view_distance=5.0
            a.spaces.active.region_3d.view_location=Vector((-.12,0,1.7))
            a.spaces.active.region_3d.view_rotation=scene.camera.rotation_euler.to_quaternion()
            a.spaces.active.region_3d.view_perspective='ORTHO'
if not bpy.data.texts.get('refine_hero.py | executed visual corrections'):
    txt=bpy.data.texts.load(str(OUT/'refine_hero.py'))
    txt.name='refine_hero.py | executed visual corrections'
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'JT_Hero_V01.blend'))
(OUT/'logs'/'visual_corrections.json').write_text(json.dumps({
    'scene':scene.name,'saved_file':str(OUT/'JT_Hero_V01.blend'),
    'reviewed_previews':['progress/01_front.png','progress/04_three_quarter.png'],
    'corrections':['Deepened navy painted material and graphite contrast','Removed floor slab edge from orthographic background','Moved detached sword forward so right side view exposes torso and leg profile'],
    'unchanged_proportions':{'crown_height_m':3.25,'weapon_total_m':2.275}
},ensure_ascii=False,indent=2),encoding='utf8')
print('JT_VISUAL_CORRECTIONS_SAVED',flush=True)
