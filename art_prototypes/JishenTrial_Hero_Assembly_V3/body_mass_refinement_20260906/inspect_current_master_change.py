import bpy,pathlib,json,hashlib
W=pathlib.Path(__file__).resolve().parent;P=W.parent/'JishenTrial_ASSEMBLED_MASTER.blend'
def inspect(path):
 bpy.ops.wm.open_mainfile(filepath=str(path));sc=bpy.context.scene;out={}
 for o in sc.objects:
  if o.type!='MESH':continue
  out[o.name]={'geometry':hashlib.sha256(repr(([tuple(v.co) for v in o.data.vertices],[tuple(f.vertices) for f in o.data.polygons])).encode()).hexdigest(),'transform':[list(row) for row in o.matrix_world],'materials':[m.name if m else None for m in o.data.materials],'hide_render':o.hide_render}
 return {'mass_revision':sc.get('mass_revision'),'filepath':str(path),'mesh_count':len(out),'objects':out}
current=inspect(P);original=inspect(W/'SOURCE_BEFORE_MASS_PASS.blend')
added=sorted(set(current['objects'])-set(original['objects']));removed=sorted(set(original['objects'])-set(current['objects']));changed=[n for n in current['objects'] if n in original['objects'] and current['objects'][n]!=original['objects'][n]]
report={'current_mass_revision':current['mass_revision'],'current_mesh_count':current['mesh_count'],'added':added,'removed':removed,'changed':changed,'current':current,'change_fields':{n:[k for k in current['objects'][n] if current['objects'][n][k]!=original['objects'][n][k]] for n in changed}}
(W/'current_master_external_change.json').write_text(json.dumps(report,indent=2))
print(json.dumps({k:v for k,v in report.items() if k not in ('current','added','removed','changed','change_fields')},indent=2),flush=True)
