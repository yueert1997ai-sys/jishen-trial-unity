import bpy,bmesh,pathlib,json,math,sys
W=pathlib.Path(__file__).resolve().parent;D=W/'delivery';sys.path.insert(0,str(W));from demo_micro_cleanup import clean
bpy.ops.wm.open_mainfile(filepath=str(D/'VALKYR_LOD_GEOMETRY_WORK.blend'));sc=bpy.context.scene;parts=list(bpy.data.collections['VALKYR_DEMO_EXPORT'].objects)
raw=(W/'demo_geometry.log').read_bytes();log=raw.decode('utf-16' if raw.startswith(b'\xff\xfe') else 'utf-8')
report=json.loads(next(line[len('LOD0_GEOMETRY '):] for line in log.splitlines() if line.startswith('LOD0_GEOMETRY ')))
fixed=[]
for o in parts:
 if clean(o):fixed.append(o.name)
report['microscopic_triangulation_cleanup']=fixed;report['topology_issues']=[];report['triangles']=0
for o in parts:
 o.data.calc_loop_triangles();report['triangles']+=len(o.data.loop_triangles)
 bm=bmesh.new();bm.from_mesh(o.data);bad=sum(not e.is_manifold for e in bm.edges);z=sum(f.calc_area()<1e-13 for f in bm.faces);bm.free()
 if bad or z:report['topology_issues'].append((o.name,bad,z))
assert not report['topology_issues'],report
report['by_family']={f:sum(len(o.data.loop_triangles) for o in parts if o['atlas_family']==f) for f in ('Armor','Mechanics','Weapon','Sensor','Beam')}
assert 80000<=report['triangles']<=150000,report
source=(W/'prepare_demo_geometry.py').read_text(encoding='utf8');tail=source[source.index('# Pack each shared-material family'):]
exec(compile(tail,str(W/'prepare_demo_geometry.py'),'exec'))
print('DEMO_FINISH_RECOVERED',report['triangles'],fixed,flush=True)
