"""Reopen editable/candidate files, import the actual exported FBX, inspect geometry."""
import bpy,bmesh,pathlib,json
from mathutils import Vector
OUT=pathlib.Path(__file__).resolve().parent
report={}
for file in ('JishenTrial_ASSEMBLED_MASTER.blend','JishenTrial_GAME_CANDIDATE.blend'):
    bpy.ops.wm.open_mainfile(filepath=str(OUT/file));sc=bpy.context.scene
    if 'MASTER' in file:
        col=bpy.data.collections['HEAD_V2 | colored editable parts'];objs=[o for o in col.all_objects if o.type=='MESH']
        report[file]={'reopened':True,'head_and_neck_parts':len(objs),'head_root_z':bpy.data.objects['HEAD_V2_ROOT'].matrix_world.translation.z}
    else:
        objs=list(bpy.data.collections['EXPORT'].objects)
        pts=[o.matrix_world@Vector(v) for o in objs for v in o.bound_box]
        report['source_bounds']=[[fn(v[i] for v in pts) for i in range(3)] for fn in (min,max)]
        report['source_mesh_count']=len(objs)
        bad=[]
        for o in objs:
            bm=bmesh.new();bm.from_mesh(o.data);nm=[e for e in bm.edges if not e.is_manifold]
            if nm:bad.append({'object':o.name,'edges':[{'boundary':e.is_boundary,'wire':e.is_wire,'faces':len(e.link_faces),'length':e.calc_length()} for e in nm]})
            bm.free()
        report['topology_edges']=bad
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(OUT/'exports/JishenTrial_Assembly_V3.fbx'))
bpy.context.view_layer.update();objects=[o for o in bpy.context.scene.objects if o.type=='MESH']
pts=[o.matrix_world@Vector(v) for o in objects for v in o.bound_box]
report['fbx_bounds']=[[fn(v[i] for v in pts) for i in range(3)] for fn in (min,max)]
report['fbx_mesh_count']=len(objects);report['fbx_materials']=sorted({m.name for o in objects for m in o.data.materials})
report['fbx_head_world']=list(bpy.data.objects['Head'].matrix_world.translation)
report['fbx_separate_beam']=bpy.data.objects.get('LOD0_AntiShipBlade_Beam') is not None
assert len(objects)==report['source_mesh_count']
assert abs(report['fbx_head_world'][2]-3.003)<.002
assert max(abs(a-b) for aa,bb in zip(report['source_bounds'],report['fbx_bounds']) for a,b in zip(aa,bb))<1e-4
assert report['fbx_separate_beam'] and len(report['fbx_materials'])==5
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'FBX_REIMPORT_CHECK.blend'))
(OUT/'logs'/'reopen_fbx_verification.json').write_text(json.dumps(report,indent=2),encoding='utf8')
print('FBX_REIMPORT_PASS',json.dumps(report),flush=True)
