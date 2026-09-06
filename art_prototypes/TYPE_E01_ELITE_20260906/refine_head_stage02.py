"""Replace the rejected helmet with an asymmetrical, internally invaded E-01 husk.
Only the head collection is rebuilt. Body, claw, back and studio remain identical.
"""
from pathlib import Path
import bpy,bmesh,math,random,json,hashlib,ast
from mathutils import Vector,Matrix
from mathutils.geometry import tessellate_polygon

ROOT=Path(__file__).resolve().parent
BASE=ROOT/'stage_01/TYPE_E01_ELITE_STAGE01.blend'
OUT=ROOT/'stage_02_head';OUT.mkdir(exist_ok=True);(OUT/'renders').mkdir(exist_ok=True)
base_hash=hashlib.sha256(BASE.read_bytes()).hexdigest()
bpy.ops.wm.open_mainfile(filepath=str(BASE))
sc=bpy.context.scene;sc.name='02 ELITE - ruptured predatory head';sc.use_fake_user=True
HEAD=bpy.data.collections['03 HALF BREACHED HEAD']
CORE=bpy.data.collections['04 LIVING THORAX AND RED CORE']
ARMOR=bpy.data.collections['01 WHITE ARMOR - inherited and fractured']
PARTS=bpy.data.collections['08 EDITABLE PART PIVOTS - not animation rig']
master=bpy.data.objects['TYPE_E01_ELITE_MASTER'];head=bpy.data.objects['ELITE | E01_HEAD'];chest=bpy.data.objects['ELITE | E01_CHEST']
master.scale=(1,1,1);master.location=(0,0,0);bpy.context.view_layer.update()
def geometry_signature(objects):
    import array
    digest=hashlib.sha256()
    for o in sorted(objects,key=lambda ob:ob.name):
        digest.update(o.name.encode());digest.update(str(o.matrix_world).encode())
        data=array.array('f',[0.0])*(len(o.data.vertices)*3);o.data.vertices.foreach_get('co',data);digest.update(data.tobytes())
        digest.update(str([m.name for m in o.data.materials]).encode())
    return digest.hexdigest()
other_meshes=[o for o in sc.objects if o.type=='MESH' and o not in HEAD.objects[:]]
body_signature=geometry_signature(other_meshes)
removed=[]
for o in list(HEAD.objects):
    if any(s in o.name for s in ('neck bearing','neck concentric seal','neck column')):continue
    removed.append(o.name);bpy.data.objects.remove(o,do_unlink=True)

liquid=bpy.data.materials['ELITE | black liquid titanium']
ridge=bpy.data.materials['ELITE | polished living blade ridges']
red=bpy.data.materials['ELITE | narrow vermilion life veins']
white=bpy.data.materials['ELITE | worn ivory ceramic paint']
owned=[]
# Reuse geometry constructors only; the stage-01 production script is not executed.
source=ast.parse((ROOT/'build_boss.py').read_text(encoding='utf-8'))
names={'material','parent_to','mesh','plate','ellipsoid','sample_path','interpolate','sweep'}
module=ast.Module(body=[node for node in source.body if isinstance(node,ast.FunctionDef) and node.name in names],type_ignores=[])
exec(compile(module,'shared_geometry_constructors','exec'),globals())

rng=random.Random(902106)
ash=material('R02 ash-grey remaining paint',(.21,.235,.219),.33,.45)
fracture=material('R02 dark exposed fracture alloy',(.08,.092,.098),.78,.37)
soot=material('R02 heat blackened cavity',(.006,.008,.010),.44,.49)
metal=material('R02 irregular living cranial metal',(.011,.016,.019),.85,.29)
shine=material('R02 polished tear margins',(.033,.039,.041),.90,.30)
eye_glass=material('R02 deep red monoeye glass',(.42,.0002,.0008),.26,.23,1.7)
eye_hot=material('R02 focused red optic',(.62,.0005,.0006),.15,.24,2.2)
life=material('R02 thin buried red pulses',(.42,.0001,.0006),.35,.29,2.6)

# Named edge-distance attribute produces soot and chipped paint at actual fracture margins.
n=ash.node_tree.nodes;l=ash.node_tree.links;p=n.get('Principled BSDF')
attr=n.new('ShaderNodeAttribute');attr.attribute_name='fracture_edge'
tex=n.new('ShaderNodeTexNoise');tex.inputs['Scale'].default_value=185;tex.inputs['Detail'].default_value=3.1
mix=n.new('ShaderNodeMath');mix.operation='MULTIPLY';mix.inputs[1].default_value=.20;l.new(tex.outputs['Fac'],mix.inputs[0])
add=n.new('ShaderNodeMath');add.operation='ADD';l.new(attr.outputs['Fac'],add.inputs[0]);l.new(mix.outputs[0],add.inputs[1])
r=n.new('ShaderNodeValToRGB');r.color_ramp.elements[0].position=.12;r.color_ramp.elements[0].color=(.24,.258,.239,1);r.color_ramp.elements[1].position=.87;r.color_ramp.elements[1].color=(.012,.016,.017,1)
e=r.color_ramp.elements.new(.52);e.color=(.09,.101,.091,1)
l.new(add.outputs[0],r.inputs[0]);l.new(r.outputs[0],p.inputs['Base Color'])
b=n.new('ShaderNodeBump');b.inputs['Strength'].default_value=.20;b.inputs['Distance'].default_value=.00055;l.new(tex.outputs['Fac'],b.inputs['Height']);l.new(b.outputs[0],p.inputs['Normal'])
for ma in (metal,shine):
    n=ma.node_tree.nodes;l=ma.node_tree.links;p=n.get('Principled BSDF')
    p.inputs['Coat Weight'].default_value=.13;p.inputs['Coat Roughness'].default_value=.28
    tex=n.new('ShaderNodeTexNoise');tex.inputs['Scale'].default_value=87;tex.inputs['Detail'].default_value=3.5
    bump=n.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.18;bump.inputs['Distance'].default_value=.0011
    l.new(tex.outputs['Fac'],bump.inputs['Height']);l.new(bump.outputs[0],p.inputs['Normal'])
    rr=n.new('ShaderNodeMapRange');rr.inputs['To Min'].default_value=.23;rr.inputs['To Max'].default_value=.41
    l.new(tex.outputs['Fac'],rr.inputs[0]);l.new(rr.outputs[0],p.inputs['Roughness'])

def graft(name,path,widths,depth=.42,vein=False,mat=metal,twist=0,steps=58):
    global red
    old=red;red=life
    ob=sweep('R02 | '+name,path,widths,mat,HEAD,head,depth,steps,12,twist,vein,.0011,phase=rng.uniform(-.3,.3))
    red=old;return ob

def shell_point(uv):
    u,v=uv;v=max(0,min(1,v))
    # Swept, elongated E-01 helmet dome with an overhanging frontal brow.
    stations=[(-.212,.070,3.081),(-.160,.116,3.164),(-.080,.147,3.214),(.005,.153,3.224),(.102,.126,3.192),(.185,.071,3.115)]
    t=v*(len(stations)-1);i=min(len(stations)-2,int(t));f=t-i
    y=stations[i][0]*(1-f)+stations[i+1][0]*f
    w=stations[i][1]*(1-f)+stations[i+1][1]*f
    top=stations[i][2]*(1-f)+stations[i+1][2]*f
    return Vector((u*w,y,top-.125*abs(u)**2.05))
def shell_normal(uv):
    u,v=uv;a=shell_point((u+.001,v))-shell_point((u-.001,v));b=shell_point((u,min(.999,v+.001)))-shell_point((u,max(.001,v-.001)))
    normal=a.cross(b).normalized()
    return normal if normal.z>=0 else -normal
def edge_distance(point,outline):
    q=Vector(point);distance=9
    for a,b in zip(outline,outline[1:]+outline[:1]):
        aa=Vector(a);bb=Vector(b);direction=bb-aa
        t=max(0,min(1,(q-aa).dot(direction)/max(1e-12,direction.length_squared)))
        distance=min(distance,(q-aa-direction*t).length)
    return distance
def cap_fragment(name,contour,lift=.002,thickness=.008,mat=ash):
    # Irregular short break segments avoid the previous uniform triangle/saw-tooth seam.
    outline=[]
    for a,b in zip(contour,contour[1:]+contour[:1]):
        aa=Vector(a);bb=Vector(b);steps=max(2,int((bb-aa).length*20))
        direction=(bb-aa).normalized();normal=Vector((-direction.y,direction.x))
        for j in range(steps):
            t=j/steps;offset=rng.uniform(-.007,.007) if j else 0
            outline.append(tuple(aa.lerp(bb,t)+normal*offset))
    coords=[Vector((u,v,0)) for u,v in outline]
    tris=tessellate_polygon([coords]);faces=[tuple(v if isinstance(v,int) else coords.index(v) for v in face) for face in tris]
    temp=bpy.data.meshes.new('fragment construction');temp.from_pydata(coords,[],faces);temp.update()
    bm=bmesh.new();bm.from_mesh(temp)
    bmesh.ops.subdivide_edges(bm,edges=list(bm.edges),cuts=2,use_grid_fill=True)
    bm.verts.ensure_lookup_table();bm.verts.index_update();bm.faces.ensure_lookup_table()
    uv=[tuple(v.co[:2]) for v in bm.verts];surfaces=[tuple(v.index for v in f.verts) for f in bm.faces]
    bm.free();bpy.data.meshes.remove(temp)
    points=[];edge_values=[]
    for u,v in uv:
        pos=shell_point((u,v));norm=shell_normal((u,v))
        pos+=norm*(lift+.00045*math.sin(u*57+v*63));points.append(pos)
        edge_values.append(max(0,1-edge_distance((u,v),outline)/.055))
    count=len(points);norms=[shell_normal(x) for x in uv]
    verts=[tuple(p) for p in points]+[tuple(p-n*thickness) for p,n in zip(points,norms)]
    out_faces=list(surfaces)+[tuple(v+count for v in reversed(f)) for f in surfaces]
    edge_counts={}
    for f in surfaces:
        for a,b in zip(f,f[1:]+f[:1]):
            key=tuple(sorted((a,b)));edge_counts[key]=None if key in edge_counts else (a,b)
    for edge in edge_counts.values():
        if edge:
            a,b=edge;out_faces.append((a,b,b+count,a+count))
    obj=mesh('R02 | '+name,verts,out_faces,mat,HEAD,head,False)
    obj.data.materials.append(fracture)
    for poly in obj.data.polygons:
        if poly.index>=len(surfaces):poly.material_index=1
    attribute=obj.data.attributes.new(name='fracture_edge',type='FLOAT',domain='POINT')
    for j,val in enumerate(edge_values+edge_values):attribute.data[j].value=val
    obj['construction']='Finite curved armor fragment, irregular fracture perimeter, physical exposed alloy thickness'
    return obj

# A dark, irregular interior ties the face to the nape before any remaining armor is added.
ellipsoid('R02 | exposed inner mechanical skull',(.020,.025,3.056),(.111,.151,.145),soot,HEAD,head,44,24)
ellipsoid('R02 | invaded left cranial substrate',(.100,.016,3.093),(.074,.121,.125),metal,HEAD,head,40,24)
for i in range(10):
    a=i*2.39996;cx=.024+math.cos(a)*.088;yy=.022+math.sin(a)*.11;zz=3.037+rng.uniform(-.06,.10)
    ellipsoid('R02 | buried cranial knot '+str(i),(cx,yy,zz),(.027,.029,.032),soot if i%2 else metal,HEAD,head,20,12)

cap_fragment('rear right shell remnant',[(-.96,.30),(-.63,.35),(-.20,.43),(-.27,.52),(-.08,.60),(-.27,.67),(-.18,.76),(-.33,.87),(-.78,.92),(-1.02,.74)],.002)
cap_fragment('fractured low forehead',[(-.92,.03),(-.56,.025),(-.19,.035),(.03,.08),(-.025,.13),(.10,.19),(-.10,.22),(-.04,.26),(-.23,.31),(-.14,.36),(-.42,.38),(-.77,.31),(-.98,.18)],.004)
cap_fragment('lifted crown splinter',[(-.20,.47),(.045,.40),(.14,.49),(.05,.56),(.16,.60),(.025,.66),(-.13,.61)],.020,.006)
cap_fragment('rear broken island',[(.01,.76),(.24,.69),(.43,.80),(.27,.87),(.29,.93),(.04,.90)],.009,.005)

# Pointed, deeply shaded facial structure. No square mouth grille and no complete white U-shaped chin.
plate('R02 | fractured carbon optic housing',[(-.059,-.202,3.062),(.018,-.215,3.057),(.054,-.199,3.025),(.039,-.211,2.990),(.012,-.214,2.944),(-.028,-.205,2.929),(-.064,-.183,2.979)],(0,.020,0),soot,HEAD,head)
plate('R02 | remaining right cheek alloy',[(-.104,-.128,3.064),(-.070,-.205,3.039),(-.061,-.220,2.995),(-.042,-.216,2.947),(-.006,-.215,2.869),(-.040,-.173,2.907),(-.061,-.146,2.977)],(.010,.014,0),fracture,HEAD,head)
plate('R02 | ash grey broken cheek',[(-.099,-.147,3.032),(-.073,-.211,3.025),(-.065,-.216,2.991),(-.052,-.201,2.980),(-.057,-.184,2.951),(-.032,-.189,2.922),(-.048,-.169,2.942),(-.062,-.152,2.992)],(.006,.011,0),ash,HEAD,head)
plate('R02 | small shattered chin remnant',[(-.042,-.206,2.945),(-.023,-.225,2.932),(-.015,-.224,2.901),(.004,-.235,2.870),(-.023,-.207,2.885),(-.038,-.193,2.914)],(.007,.009,0),ash,HEAD,head)
graft('lower jaw tearing under the optic',[(.096,-.078,3.039),(.077,-.151,2.993),(.041,-.209,2.943),(.014,-.231,2.863)],[.032,.037,.022,.0005],.41,True)
graft('right jaw narrow predatory edge',[(-.111,-.121,3.069),(-.100,-.192,3.030),(-.069,-.233,2.970),(-.032,-.227,2.903)],[.012,.018,.011,.0004],.32,False,shine)

# Small retained monoeye inside a distorted, recessed socket.
eye_center=Vector((-.005,-.209,3.025))
def iris_ring(name,radius,width,y_offset,material,broken=False):
    pieces=7 if broken else 1
    for i in range(pieces):
        begin=math.tau*i/pieces+.055 if broken else 0;end=math.tau*(i+1)/pieces-.07 if broken else math.tau
        pp=[]
        for j in range(15 if broken else 61):
            t=j/(14 if broken else 60);a=begin+(end-begin)*t
            pp.append(eye_center+Vector((math.cos(a)*radius,y_offset,math.sin(a)*radius*.92)))
        sweep('R02 | '+name+' '+str(i),pp,[width,width],material,HEAD,head,.55,40 if broken else 100,10)
iris_ring('deep optical socket rim',.030,.007,-.001,metal)
iris_ring('fractured original optical retaining ring',.026,.0025,-.007,fracture,True)
ellipsoid('R02 | dark red recessed sensor',eye_center+Vector((0,-.012,0)),(.020,.006,.0185),eye_glass,HEAD,head,48,28)
ellipsoid('R02 | small intense emitter',eye_center+Vector((0,-.017,0)),(.010,.002,.010),eye_hot,HEAD,head,40,22)

# Continuous low brow masks the top of the round optic and establishes an intent gaze.
plate('R02 | thin shattered visor ledge',[(-.105,-.180,3.105),(-.055,-.208,3.084),(-.005,-.230,3.063),(.039,-.208,3.078),(.071,-.160,3.121),(.067,-.179,3.077),(.025,-.235,3.041),(-.010,-.243,3.045),(-.057,-.221,3.064),(-.104,-.186,3.088)],(0,.009,.004),metal,HEAD,head)
plate('R02 | cracked brow ceramic sliver',[(-.090,-.202,3.094),(-.047,-.222,3.078),(-.031,-.233,3.062),(-.007,-.236,3.057),(-.018,-.223,3.076),(-.064,-.184,3.118)],(0,.008,0),ash,HEAD,head)

# Interweave longitudinal, diagonal and wrapping growth; no evenly spaced rows remain.
macro_paths=[
 [( .106,.100,2.807),(.142,.093,2.954),(.156,.018,3.100),(.129,-.082,3.198),(.178,.013,3.297)],
 [( .064,.093,2.838),(.086,-.051,2.941),(.094,-.156,3.063),(.037,-.153,3.149),(.084,-.032,3.255)],
 [( .148,.079,3.029),(.166,-.035,3.113),(.112,-.129,3.160),(.031,-.077,3.211),(-.066,.031,3.206)],
 [( .099,.115,2.942),(.110,.173,3.084),(.126,.169,3.211),(.103,.220,3.322),(.191,.255,3.381)],
 [(-.016,.159,2.871),(-.066,.181,3.025),(-.079,.161,3.144),(-.026,.124,3.241),(.073,.184,3.300)],
 [( .141,.055,3.184),(.100,-.045,3.243),(.031,-.143,3.182),(-.034,-.175,3.149),(-.083,-.140,3.175)],
 [( .106,-.129,3.069),(.090,-.195,3.012),(.068,-.199,2.964),(.049,-.122,2.900),(.094,.062,2.841)],
 [(-.081,.035,2.912),(-.113,-.027,3.034),(-.139,-.009,3.088),(-.141,.089,3.103),(-.115,.156,3.170)]
]
for i,pp in enumerate(macro_paths):
    width=rng.uniform(.012,.025)
    graft('continuous invading cranial trunk '+str(i),pp,[width*.8,width,width*.9,width*.6,.0004],.45,i%3==0,metal if i%4 else shine,twist=rng.uniform(-.6,.6),steps=70)
    for side in (-1,1):
        points=[Vector(p)+Vector((side*.008,side*.004,math.sin(j+i)*.005)) for j,p in enumerate(pp)]
        graft('fine companion strand %d %d'%(i,side),points,[.005,.008,.007,.002,.0002],.31,i%4==0 and side==1,metal,twist=.35*side)

for i in range(39):
    t=rng.uniform(.02,.97);z=2.925+t*.29
    a=rng.uniform(-1.12,1.6)
    x=.081+math.cos(a)*.067;y=.012+math.sin(a)*.124
    start=Vector((x,y,z));delta=Vector((rng.uniform(-.043,.021),rng.uniform(-.069,.047),rng.uniform(.049,.135)))
    pp=[start,start+delta*.30+Vector((.01,-.014,.001)),start+delta*.70+Vector((-.012,.004,.008)),start+delta]
    w=rng.uniform(.007,.017)
    graft('irregular fused cranial lamina '+str(i),pp,[w*.3,w,w*.75,.00025],.32,i%8==0,metal,twist=rng.uniform(-1,1),steps=42)

# Break up the outermost lateral surface, which must also read as invaded in a true side view.
def lateral_surface(longitude,latitude):
    return Vector((.100+.078*math.cos(longitude)*math.cos(latitude),.016+.126*math.sin(longitude)*math.cos(latitude),3.093+.129*math.sin(latitude)))
for i in range(18):
    a=rng.uniform(-1.14,1.22);b=rng.uniform(-.90,.64)
    pts=[lateral_surface(a+math.sin(t*2.3+i)*.12,b+t*.61) for t in (0,.24,.53,.78,1)]
    w=rng.uniform(.005,.013)
    graft('outer temporal fused growth '+str(i),pts,[.003,w,w*.7,.004,.00025],.29,i%6==0,metal if i%4 else shine,twist=rng.uniform(-.4,.4),steps=42)
substrate=bpy.data.objects['R02 | invaded left cranial substrate']
center=Vector((.100,.016,3.093))
for vertex in substrate.data.vertices:
    delta=vertex.co-center;n=delta.normalized()
    displacement=.0016*math.sin(vertex.co.y*139+vertex.co.z*76)+.0014*math.sin(vertex.co.z*191+vertex.co.x*114)
    vertex.co+=n*displacement
substrate.data.update()

# Stressed remnants and short lifted splinters interrupt the black/white edge.
for i in range(15):
    v=rng.uniform(.14,.81);u=rng.uniform(-.18,.25);du=rng.uniform(.035,.095);dv=rng.uniform(.015,.042)
    contour=[(u-du,v-dv),(u+du*.7,v-dv*.2),(u+du,v+dv),(u-du*.2,v+dv*.8)]
    cap_fragment('embedded ceramic shard %02d'%i,contour,rng.uniform(.009,.020),.004,ash)
for i in range(13):
    v=rng.uniform(.04,.85);u=rng.uniform(-.72,.0)
    base=shell_point((u,v));normal=shell_normal((u,v))
    pp=[base+normal*.005,base+Vector((.035,-.006,.008))+normal*.016,base+Vector((.064,.017,.011))+normal*.007]
    graft('capillary metal crossing fracture '+str(i),pp,[.001,.004,.0002],.30,i%5==0,metal,steps=30)

# Ruptured ear actuator retains the ancestry of the mono-sensor machine without a white disc.
ear=Vector((-.140,.031,3.058))
for i in range(5):
    a=math.tau*i/5
    pp=[ear+Vector((-.005,math.cos(a)*.041,math.sin(a)*.040)),ear+Vector((-.008,math.cos(a+.39)*.040,math.sin(a+.39)*.040)),ear+Vector((0,math.cos(a+.75)*.042,math.sin(a+.75)*.035))]
    graft('shattered temporal actuator arc '+str(i),pp,[.004,.006,.002],.8,False,fracture,steps=30)
for i in (0,2):
    a=math.tau*i/5;p=ear+Vector((-.01,math.cos(a)*.037,math.sin(a)*.038))
    ellipsoid('R02 | retained ear fixing '+str(i),p,(.002,.004,.004),shine,HEAD,head,16,8)

# A few low, swept spikes emerge from the same growth, not an independent crown of horns.
graft('rear swept broken crest',[(.087,.087,3.171),(.130,.129,3.265),(.144,.202,3.340),(.190,.242,3.369)],[.016,.020,.012,.0002],.36,True,metal,twist=.4)
graft('short torn temple crest',[(.147,.015,3.128),(.177,-.007,3.211),(.208,.048,3.265)],[.016,.020,.0002],.28,False,metal)

for obj in HEAD.objects:
    if obj.type=='MESH' and obj.name.startswith('R02 |'):
        obj['head_revision']='02 - asymmetrical ruptured skull, soot-edged residual armor, recessed predatory monoeye'
        # Material attributes are explicitly initialized for non-cap fragments as well.
        if ash in obj.data.materials[:] and not obj.data.attributes.get('fracture_edge'):
            attr=obj.data.attributes.new(name='fracture_edge',type='FLOAT',domain='POINT')
            for j,value in enumerate(attr.data):value.value=.18+rng.random()*.50

bpy.context.view_layer.update()
assert geometry_signature(other_meshes)==body_signature,'Unexpected change outside head collection'
master.scale=(1.5,1.5,1.5);master.location.z=-.084
master['review_stage']='02 head refinement; awaiting visual review before animation rig'
master['head_feedback']='Head looked unnatural, too white, insufficiently ruined or evil, and dull. Head rebuilt.'
master['head_revision']='Reduced and recessed optic; low brow; uneven fracture geometry; soot-boundary armor; fused cross-direction growth'
bpy.context.view_layer.update()
cam_col=bpy.data.collections['91 REVIEW CAMERAS']
def camera(name,loc,target,scale,w=1200,h=1200):
    data=bpy.data.cameras.new(name);data.type='ORTHO';data.ortho_scale=scale
    o=bpy.data.objects.new(name,data);cam_col.objects.link(o)
    o.location=Vector(loc)*1.5+Vector((0,0,-.084))
    o.rotation_euler=(Vector(target)*1.5+Vector((0,0,-.084))-o.location).to_track_quat('-Z','Y').to_euler();o['resolution']=[w,h]
    return o
camera('R02_HEAD_FRONT',(0,-3.1,3.16),(.017,-.02,3.086),.92)
camera('R02_HEAD_INVADED',(.85,-2.1,3.52),(.024,0,3.105),1.10)
camera('R02_HEAD_PROFILE',(2.4,-.24,3.22),(.017,.02,3.086),.99)
camera('R02_HEAD_REVERSE',(-1.1,-2.1,3.40),(.008,.0,3.085),1.02)
sc.camera=bpy.data.objects['ELITE_HERO'];sc.render.filepath=str(OUT/'renders')+'/'
sc.render.resolution_x=1550;sc.render.resolution_y=1800
sc.cycles.samples=48
textblock=bpy.data.texts.get('READ ME | ELITE modeling gate')
if textblock:textblock.write('\n\nHEAD REVISION 02: Rebuilt after user feedback. Body and claw unchanged. Awaiting modeling review.\n')
bpy.ops.outliner.orphans_purge(do_local_ids=True,do_linked_ids=True,do_recursive=True)
try:
    prefs=bpy.context.preferences.addons['cycles'].preferences;prefs.compute_device_type='OPTIX';prefs.get_devices()
    for device in prefs.devices:device.use=device.type=='OPTIX'
    sc.cycles.device='GPU' if any(d.use for d in prefs.devices) else 'CPU'
except Exception:sc.cycles.device='CPU'
target=OUT/'TYPE_E01_ELITE_HEAD_R02.blend'
bpy.ops.wm.save_as_mainfile(filepath=str(target),check_existing=False)
report={'model':str(target),'base':str(BASE),'base_sha256':base_hash,'base_unchanged':hashlib.sha256(BASE.read_bytes()).hexdigest()==base_hash,'other_geometry_unchanged':True,'head_meshes':sum(o.type=='MESH' for o in HEAD.objects),'removed_head_objects':len(removed),'rigged':False,'game_integrated':False,'user_feedback':master['head_feedback']}
(OUT/'head_revision_report.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print('HEAD_R02_SAVED '+json.dumps(report),flush=True)
sc.camera=bpy.data.objects['R02_HEAD_INVADED'];sc.cycles.samples=24
sc.render.resolution_x=1000;sc.render.resolution_y=1000;sc.render.filepath=str(OUT/'renders/HEAD_DRAFT.png')
bpy.ops.render.render(write_still=True)
print('HEAD_DRAFT_READY',flush=True)
