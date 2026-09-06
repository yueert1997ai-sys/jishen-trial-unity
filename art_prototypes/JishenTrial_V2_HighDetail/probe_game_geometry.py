import bpy,json,pathlib
OUT=pathlib.Path(__file__).resolve().parent
scene=bpy.context.scene
stats={}
for o in scene.objects:
    if o.type not in ('MESH','CURVE') or o.hide_render:continue
    group='hardware' if any(p in o.name for p in ('Washer','Socket','ServiceBolt','Fastener','Grip_Insulator','Finger_','Thumb_','Louver','Rim_','Pin_','Armor_Lock','Catch_','Latch_')) else 'structure'
    if o.type=='CURVE':o.data.bevel_resolution=1;o.data.resolution_u=6
    for mod in o.modifiers:
        if mod.type=='BEVEL':
            mod.segments=1
            if group=='hardware':mod.show_viewport=False;mod.show_render=False
    stats.setdefault(group,{'objects':0,'tris':0,'top':[]})
    dg=bpy.context.evaluated_depsgraph_get();ev=o.evaluated_get(dg);me=ev.to_mesh();me.calc_loop_triangles();n=len(me.loop_triangles)
    stats[group]['objects']+=1;stats[group]['tris']+=n;stats[group]['top'].append((o.name,n));ev.to_mesh_clear()
for s in stats.values():s['top']=sorted(s['top'],key=lambda x:-x[1])[:14]
(OUT/'logs'/'game_geometry_probe.json').write_text(json.dumps(stats,indent=2),encoding='utf8')
print(json.dumps(stats),flush=True)
