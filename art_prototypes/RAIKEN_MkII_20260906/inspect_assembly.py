import bpy,pathlib,json
P=pathlib.Path(__file__).resolve().parent
source=P.parent/'JishenTrial_Hero_Assembly_V3/JishenTrial_ASSEMBLED_MASTER.blend'
bpy.ops.wm.open_mainfile(filepath=str(source))
sc=bpy.context.scene
data={'source':str(source),'scene_props':dict(sc.items()),'objects':[]}
for o in sc.objects:
    if o.type=='EMPTY' and any(x in o.name for x in ('Hand','Arm','Forearm','Sword','Blade','Grip')):
        data['objects'].append({'name':o.name,'parent':o.parent.name if o.parent else None,'world':[list(r) for r in o.matrix_world],'props':dict(o.items())})
data['weapons']=[o.name for o in sc.objects if any(c.name.startswith('BODY_V3 | 10') for c in o.users_collection)]
(P/'assembly_inspection.json').write_text(json.dumps(data,indent=2,default=str),encoding='utf-8')
print(json.dumps(data['objects'],indent=2,default=str))
