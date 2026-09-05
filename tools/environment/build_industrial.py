"""Reproducible, project-authored industrial modules; no external asset dependency."""
import bpy
import math
import os
import sys
from mathutils import Vector

root = sys.argv[sys.argv.index('--') + 1]
out = os.path.join(root, 'Assets', 'Art', 'IndustrialModules')
os.makedirs(out, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
palette = {
    'Structure': (0.16, 0.19, 0.20, 1),
    'Armor': (0.44, 0.49, 0.49, 1),
    'Edge': (0.66, 0.70, 0.69, 1),
    'Recess': (0.045, 0.065, 0.068, 1),
    'Safety': (0.88, 0.55, 0.08, 1),
    'Signal': (0.06, 0.65, 0.73, 1),
}
mats = {}
for name, color in palette.items():
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = color
    mats[name] = mat


def finish(obj, name, mat, bevel=0.04):
    obj.name = name
    obj.data.materials.append(mats[mat])
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        modifier = obj.modifiers.new('Machined edges', 'BEVEL')
        modifier.width = bevel
        modifier.segments = 2
        bpy.ops.object.modifier_apply(modifier=modifier.name)
    return obj


def box(name, p, s, mat='Armor', bevel=0.04):
    bpy.ops.mesh.primitive_cube_add(size=1, location=p)
    obj = bpy.context.object
    obj.scale = s
    return finish(obj, name, mat, bevel)


def cyl(name, p, radius, depth, mat='Structure', axis=None):
    bpy.ops.mesh.primitive_cylinder_add(vertices=16, radius=radius, depth=depth, location=p)
    obj = bpy.context.object
    if axis:
        obj.rotation_euler = Vector(axis).to_track_quat('Z', 'Y').to_euler()
    return finish(obj, name, mat, 0.025)


def pipe(name, a, b, radius=0.13, mat='Structure'):
    a, b = Vector(a), Vector(b)
    return cyl(name, (a + b) / 2, radius, (a - b).length, mat, b - a)


def export(name):
    bpy.ops.object.select_all(action='SELECT')
    bpy.context.view_layer.objects.active = next(o for o in bpy.context.scene.objects if o.type == 'MESH')
    bpy.ops.object.join()
    obj = bpy.context.object
    obj.name = name
    bpy.context.scene.cursor.location = (0, 0, 0)
    bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    bpy.ops.export_scene.fbx(filepath=os.path.join(out, name + '.fbx'), use_selection=True,
                            object_types={'MESH'}, bake_anim=False, add_leaf_bones=False,
                            apply_scale_options='FBX_SCALE_ALL', axis_forward='-Z', axis_up='Y')
    print('MODULE', name, 'vertices', len(obj.data.vertices), 'polygons', len(obj.data.polygons))
    bpy.ops.object.delete(use_global=False)


# A real service building: recessed door, buttresses, roof plant and glazing.
box('Foundation', (0, 0, .22), (7.2, 5.4, .44), 'Structure', .12)
box('BuildingShell', (0, 0, 2.05), (6.7, 4.8, 3.7), 'Armor', .22)
box('RoofCoping', (0, 0, 4.05), (7, 5.15, .35), 'Edge', .09)
box('RoofInset', (0, 0, 4.23), (6.4, 4.6, .12), 'Recess')
for x in [-3.05, 3.05]:
    box('Buttress', (x, -2.47, 1.8), (.5, .42, 3.45), 'Structure')
    box('EdgeTrim', (x, -2.7, 2), (.13, .09, 2.9), 'Safety', .01)
box('DoorRecess', (0, -2.43, 1.45), (2.8, .12, 2.9), 'Recess')
for x in [-.69, .69]:
    box('BlastDoor', (x, -2.52, 1.4), (1.25, .16, 2.65), 'Structure')
    for z in [.45, 1.0, 1.55, 2.1]:
        box('DoorRib', (x, -2.64, z), (1.12, .09, .08), 'Armor', .015)
box('DoorLight', (0, -2.68, 2.95), (2.6, .08, .1), 'Signal', .015)
for x in [-2.15, 2.15]:
    box('WindowWell', (x, -2.45, 2.65), (.95, .12, .65), 'Recess')
    box('Glass', (x, -2.53, 2.65), (.79, .05, .45), 'Signal', .015)
    box('WindowMullion', (x, -2.59, 2.65), (.08, .04, .55), 'Edge', .008)
for x in [-1.7, 1.7]:
    box('RoofVentHousing', (x, .4, 4.45), (2.2, 2.4, .38), 'Armor')
    cyl('FanWell', (x, .4, 4.66), .84, .12, 'Recess')
    cyl('FanHub', (x, .4, 4.77), .22, .18, 'Edge')
    for k in range(6):
        angle = k * math.pi / 3
        blade = box('FanBlade', (x + math.cos(angle)*.44, .4 + math.sin(angle)*.44, 4.74), (.66, .17, .07), 'Structure', .02)
        blade.rotation_euler.z = angle + .3
for x in [-3.45, 3.45]:
    pipe('ExternalRiser', (x, .9, .4), (x, .9, 3.6), .16)
    pipe('Feed', (x, .9, 3.6), (x, -1.6, 3.6), .16)
for y in [-1.6, -.8, 0, .8, 1.6]:
    box('SidePanelSeam', (3.38, y, 1.85), (.05, .06, 2.8), 'Recess', .008)
export('ServiceBuilding')

# Low, wide machine that blocks fire without hiding the player behind tall walls.
box('PumpBase', (0, 0, .18), (3, 4.7, .36), 'Structure', .12)
for y in [-1.25, 1.25]:
    cyl('PumpBody', (0, y, 1.0), .7, 2.1, 'Armor', (1, 0, 0))
    for x in [-1.05, 1.05]:
        cyl('PumpFlange', (x, y, 1.0), .77, .14, 'Edge', (1, 0, 0))
        cyl('PumpEnd', (x*1.08, y, 1.0), .51, .17, 'Recess', (1, 0, 0))
    pipe('CoolantFeed', (-.7, y, 1.55), (.7, y, 1.55), .17, 'Safety')
box('ControlHousing', (0, 0, 1.1), (1.45, .7, 1.45), 'Structure')
box('ControlScreen', (.74, 0, 1.35), (.06, .42, .37), 'Signal')
for x in [-1.34, 1.34]:
    for y in [-2.04, 2.04]:
        cyl('AnchorBolt', (x, y, .39), .09, .12, 'Edge')
export('CoolantPump')

box('CargoFeet', (0, 0, .12), (3.3, 2.0, .24), 'Structure')
box('CargoShell', (0, 0, 1.0), (3.1, 1.85, 1.7), 'Armor', .13)
for x in [-1.3, 1.3]:
    box('CargoBand', (x, 0, 1.06), (.17, 1.99, 1.85), 'Structure')
for x in [-.85, -.42, 0, .42, .85]:
    box('CargoRoofRib', (x, 0, 1.9), (.09, 1.62, .1), 'Edge', .015)
for y in [-.96, .96]:
    box('CargoLatch', (0, y, 1), (.55, .12, .38), 'Safety')
export('CargoCrate')

box('TankPlinth', (0, 0, .22), (4.3, 4.3, .44), 'Structure')
cyl('Reservoir', (0, 0, 2.5), 1.75, 4.3, 'Armor')
for z in [.65, 2.5, 4.35]:
    cyl('TankRing', (0, 0, z), 1.83, .18, 'Edge')
cyl('TankCap', (0, 0, 4.75), 1.58, .3, 'Structure')
for x in [-.42, .42]:
    pipe('LadderRail', (x, -1.85, .4), (x, -1.85, 4.8), .055, 'Safety')
for k in range(12):
    pipe('LadderRung', (-.42, -1.87, .6+k*.34), (.42, -1.87, .6+k*.34), .045, 'Edge')
pipe('Outlet', (1.7, 0, 1.2), (2.4, 0, 1.2), .24)
export('Reservoir')

box('GrateFrame', (0, 0, .025), (3, 1.6, .05), 'Structure', .02)
for x in range(19):
    box('GrateSlat', (-1.35+x*.15, 0, .065), (.052, 1.45, .06), 'Edge', .008)
export('DrainGrate')
