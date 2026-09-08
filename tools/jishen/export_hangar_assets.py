"""Export existing authored assets for the pre-sortie loadout; source weapons stay untouched."""
import bpy, json, math, hashlib, shutil
from pathlib import Path
from mathutils import Matrix, Vector

PROJECT=Path(__file__).resolve().parents[2]
OUT=PROJECT/'Assets/Art/HangarLoadout'
OUT.mkdir(parents=True,exist_ok=True)
ART=PROJECT/'art_prototypes'
materials={};provenance=[]

def note(path):
    provenance.append({'path':str(path.relative_to(PROJECT)),'sha256':hashlib.sha256(path.read_bytes()).hexdigest()})

def material_data(m):
    if m.name in materials:return
    bs=next((n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None) if m.use_nodes else None
    color=list(bs.inputs['Base Color'].default_value) if bs and not bs.inputs['Base Color'].is_linked else list(m.diffuse_color)
    emission=list(bs.inputs['Emission Color'].default_value) if bs else [0,0,0,1]
    strength=float(bs.inputs['Emission Strength'].default_value) if bs else 0
    materials[m.name]={'name':m.name,'color':color,'metallic':float(bs.inputs['Metallic'].default_value) if bs else 0,'roughness':float(bs.inputs['Roughness'].default_value) if bs else .5,'emission':[v*strength for v in emission[:3]]}

def export_static(name,objects,transform,markers):
    scene=bpy.context.scene;dg=bpy.context.evaluated_depsgraph_get()
    col=bpy.data.collections.new('UNITY_EXPORT_'+name);scene.collection.children.link(col)
    copies=[]
    for ob in objects:
        if ob.type not in ('MESH','CURVE','FONT') or ob.hide_render:continue
        mesh=bpy.data.meshes.new_from_object(ob.evaluated_get(dg),preserve_all_data_layers=True,depsgraph=dg)
        if not mesh.vertices:continue
        mesh.transform(transform@ob.matrix_world)
        copy=bpy.data.objects.new('Part_'+ob.name,mesh);col.objects.link(copy);copies.append(copy)
        for mat in mesh.materials:
            if mat:material_data(mat)
    bpy.ops.object.select_all(action='DESELECT')
    for ob in copies:ob.select_set(True)
    bpy.context.view_layer.objects.active=copies[0];bpy.ops.object.join()
    merged=bpy.context.object;merged.name=name+'_Mesh'
    root=bpy.data.objects.new(name,None);col.objects.link(root);merged.parent=root
    for key,value in markers.items():
        empty=bpy.data.objects.new(key,None);col.objects.link(empty);empty.parent=root;empty.location=transform@Vector(value)
    bpy.ops.object.select_all(action='DESELECT')
    for ob in col.objects:ob.select_set(True)
    bpy.context.view_layer.objects.active=root
    bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'.fbx')),use_selection=True,object_types={'MESH','EMPTY'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL',axis_forward='-Z',axis_up='Y',bake_anim=False,add_leaf_bones=False,use_custom_props=True)
    print('EXPORTED',name,len(merged.data.vertices),flush=True)

hero=ART/'JishenTrial_Hero_Assembly_V3/body_mass_refinement_20260906/iteration_v3/game_update/VALKYR_V3_GAME.blend'
note(hero);bpy.ops.wm.open_mainfile(filepath=str(hero))
for m in bpy.data.materials:material_data(m)
shutil.copy2(hero.parent/'exports/VALKYR_RAIKEN_GAME.fbx',OUT/'VALKYR_V3.fbx')
sword=bpy.data.objects['AntiShip_Blade_Display_Root']
grip=bpy.data.objects['RAIKEN_GRIP_SOCKET'].matrix_world.translation
tip=bpy.data.objects['RAIKEN_BLADE_TIP'].matrix_world.translation
axis=(tip-grip).normalized()
rotation=axis.rotation_difference(Vector((0,-1,0))).to_matrix().to_4x4()
export_static('RAIKEN',list(sword.children_recursive),rotation@Matrix.Translation(-grip),{'Grip':grip,'Support':grip-axis*.24,'Muzzle':tip})

m7=ART/'AI_Infantry_M7_20260906/stage_04/AI_INFANTRY_M7_STAGE04.blend'
note(m7);bpy.ops.wm.open_mainfile(filepath=str(m7));bpy.context.view_layer.update()
root=bpy.data.objects['AI_Infantry_M7_Root']
objects=[o for o in root.children_recursive if o.type in ('MESH','CURVE','FONT') and not o.hide_render]
gripob=next(o for o in objects if o.name.endswith('Ergonomic_Pistol_Grip'))
grip=sum((gripob.matrix_world@Vector(v) for v in gripob.bound_box),Vector())/8
pts=[o.matrix_world@Vector(v) for o in objects for v in o.bound_box]
length=max(p.x for p in pts)-min(p.x for p in pts);scale=1.55/length
barrels=[o for o in objects if 'Barrel' in o.name or 'Muzzle' in o.name]
barrel=max(barrels,key=lambda o:o.matrix_world.translation.x)
z=sum((barrel.matrix_world@Vector(v)).z for v in barrel.bound_box)/8
muzzle=Vector((max(p.x for p in pts),0,z))
transform=Matrix.Scale(scale,4)@Matrix.Rotation(-math.pi/2,4,'Z')@Matrix.Translation(-grip)
export_static('M7',objects,transform,{'Grip':grip,'Support':grip+Vector((.53/scale,0,.075/scale)),'Muzzle':muzzle})

m14=ART/'BOSS_M14_EBR_20260906/BOSS_M14_EBR_MASTER.blend'
note(m14);bpy.ops.wm.open_mainfile(filepath=str(m14));bpy.context.view_layer.update()
root=bpy.data.objects['BOSS_M14_EBR_ROOT'];grip=bpy.data.objects['SCK_PRIMARY_GRIP'].matrix_world.translation
transform=Matrix.Scale(1.45/1.12522,4)@Matrix.Rotation(-math.pi/2,4,'Z')@Matrix.Translation(-grip)
export_static('M14',list(root.children_recursive),transform,{key:bpy.data.objects[src].matrix_world.translation for key,src in [('Grip','SCK_PRIMARY_GRIP'),('Support','SCK_SUPPORT_GRIP'),('Muzzle','SCK_MUZZLE')]})

hangar=ART/'VALKYR_Hangar_20260908/VALKYR_HANGAR_MASTER.blend'
bpy.ops.wm.open_mainfile(filepath=str(hangar))
backup=hangar.parent/'iterations/before_unarmed_default.blend'
if not backup.exists():shutil.copy2(hangar,backup)
bpy.data.objects['Forearm.R'].rotation_euler=(0,0,0)
blade=bpy.data.objects['AntiShip_Blade_Display_Root']
for ob in [blade]+list(blade.children_recursive):ob.hide_render=True;ob.hide_set(True)
bpy.data.objects['VALKYR_INSPECTION_ROOT']['display_state']='Unarmed neutral maintenance stance. Weapons are equipped only after player selection.'
bpy.ops.wm.save_as_mainfile(filepath=str(hangar))
objects=[o for c in bpy.context.scene.collection.children if c.name[:2] in ('01','02','03','04','05') for o in c.all_objects]
export_static('HangarRoom',objects,Matrix.Identity(4),{})
(OUT/'materials.json').write_text(json.dumps({'materials':list(materials.values())},indent=2))
(OUT/'sources.json').write_text(json.dumps({'sources':provenance,'scope':'Unity copies; no weapon source edits. Procedural micro-textures remain in Blender.'},indent=2))
print('HANGAR_EXPORT_COMPLETE',flush=True)
