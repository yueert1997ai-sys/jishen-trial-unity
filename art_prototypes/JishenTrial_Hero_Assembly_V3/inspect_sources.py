import bpy, pathlib, json
from mathutils import Vector
OUT=pathlib.Path(__file__).resolve().parent
SOURCE=OUT.parent/'JishenTrial_V2_HighDetail'
def bounds(o):
    pts=[o.matrix_world@Vector(v) for v in o.bound_box]
    return [[round(fn(v[i] for v in pts),5) for i in range(3)] for fn in (min,max)]
for filename in ('V2_HIGH_DETAIL_FINAL.blend','V2_GAME_READY_DEMO.blend'):
    bpy.ops.wm.open_mainfile(filepath=str(SOURCE/filename))
    sc=bpy.context.scene
    report={'file':bpy.data.filepath,'units':sc.unit_settings.system,'scale':sc.unit_settings.scale_length,
            'collections':{},'objects':[],'materials':[]}
    for c in bpy.data.collections:report['collections'][c.name]=len(c.all_objects)
    for o in sc.objects:
        if o.type=='EMPTY' or 'HEAD' in o.name.upper() or 'NECK' in o.name.upper() or filename.startswith('V2_GAME'):
            report['objects'].append({'name':o.name,'type':o.type,'parent':o.parent.name if o.parent else None,
                'loc':list(o.matrix_world.translation),'bounds':bounds(o) if o.type in ('MESH','CURVE') else None,
                'collection':[c.name for c in o.users_collection], 'hidden':o.hide_render,
                'mats':[m.name for m in o.data.materials] if o.type=='MESH' else [],'props':{k:str(o[k]) for k in o.keys()}})
    for m in bpy.data.materials:
        p=next((n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None) if m.use_nodes else None
        report['materials'].append({'name':m.name,'color':list(p.inputs['Base Color'].default_value) if p else list(m.diffuse_color),
            'metallic':p.inputs['Metallic'].default_value if p else 0,'roughness':p.inputs['Roughness'].default_value if p else 0})
    (OUT/'logs'/(filename.replace('.blend','_inspection.json'))).write_text(json.dumps(report,indent=2),encoding='utf8')
    print('INSPECTED',filename,len(sc.objects),flush=True)
