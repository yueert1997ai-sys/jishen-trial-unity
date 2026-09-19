import bpy, bmesh, math, json
from pathlib import Path
from mathutils import Vector

ROOT=Path(r'D:\project-mecha-design\MECH ROUGE\art_prototypes\FAZZ_Cannon_ReferenceRebuild_20260909\delivery')
bpy.ops.wm.read_factory_settings(use_empty=True)
scene=bpy.context.scene
scene.name='FAZZ Cannon | reference rebuild 2026-09-09'
scene.unit_settings.system='METRIC'
def coll(name):
 c=bpy.data.collections.new(name);scene.collection.children.link(c);return c
C={k:coll(v) for k,v in [('barrel','01 Forebarrel'),('receiver','02 White receiver and grip'),('rear','03 Swept rear housing'),('drum','04 Rotary side module'),('detail','05 Hardware and inlays'),('sockets','06 Attachment points'),('studio','90 Studio')]}
root=bpy.data.objects.new('FAZZ_CANNON_ROOT',None);C['sockets'].objects.link(root)
root['reference']='reference/USER_REFERENCE.png : lower-left cannon details'
root['nominal_length_m']=3.5
root['scale_note']='Nominal asset scale; resize root to match destination mecha. No physical specification implied.'
root['axis']='Muzzle -X; top +Z; displayed side -Y'
def mat(name,c,metal=.4,rough=.36):
 m=bpy.data.materials.new(name);m.diffuse_color=(*c,1);m.use_nodes=True
 bs=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
 bs.inputs['Base Color'].default_value=(*c,1);bs.inputs['Metallic'].default_value=metal;bs.inputs['Roughness'].default_value=rough
 return m
M={'shell':mat('Matte black body',(.003,.0035,.0045),.05,.53), 'panel':mat('Black armor panels',(.006,.007,.009),.07,.47), 'violet':mat('Black rotary module',(.004,.005,.008),.08,.50), 'white':mat('Ivory ceramic armor',(.72,.74,.72),.2,.32), 'steel':mat('Machined titanium',(.12,.14,.16),.8,.29), 'dark':mat('Recess black',(.0015,.002,.003),.0,.6), 'yellow':mat('Yellow recessed identifiers',(.89,.56,.025),.35,.31)}
for key in ('shell','panel','violet','dark'):
 bs=next(n for n in M[key].node_tree.nodes if n.type=='BSDF_PRINCIPLED')
 bs.inputs['Specular IOR Level'].default_value=.16
def finish(o,name,material,col,bevel=.015):
 o.name=name
 for c in list(o.users_collection):c.objects.unlink(o)
 C[col].objects.link(o)
 if col!='studio':o.parent=root
 if material:o.data.materials.append(M.get(material,material))
 if bevel:
  b=o.modifiers.new('Small manufactured edge bevel','BEVEL');b.width=bevel;b.segments=3
  b=o.modifiers.new('Weighted corner normals','WEIGHTED_NORMAL');b.keep_sharp=True
 return o
def mesh(name,verts,faces,material='shell',col='detail',bevel=.015):
 me=bpy.data.meshes.new(name);me.from_pydata(verts,[],faces);me.update()
 bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free()
 return finish(bpy.data.objects.new(name,me),name,material,col,bevel)
def box(name,p,s,material='shell',col='detail',bevel=.015):
 bpy.ops.mesh.primitive_cube_add(size=1,location=p);o=bpy.context.object
 o.scale=s;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 return finish(o,name,material,col,bevel)
def prism(name,profile,y,w,material='shell',col='rear',bevel=.02):
 n=len(profile);v=[(x,py,z) for py in (y-w/2,y+w/2) for x,z in profile]
 f=[tuple(range(n-1,-1,-1)),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
 return mesh(name,v,f,material,col,bevel)
def loft(name,sections,material='shell',col='barrel',bevel=.015,open_ends=False):
 v=[]
 for x,w,lo,hi in sections:
  c=min(w*.40,(hi-lo)*.22)
  v.extend((x,y,z) for y,z in [(-w,lo+c),(-w+c,lo),(w-c,lo),(w,lo+c),(w,hi-c),(w-c,hi),(-w+c,hi),(-w,hi-c)])
 f=[] if open_ends else [tuple(range(7,-1,-1))]
 for j in range(len(sections)-1):
  for i in range(8):f.append((j*8+i,j*8+(i+1)%8,(j+1)*8+(i+1)%8,(j+1)*8+i))
 if not open_ends:f.append(tuple((len(sections)-1)*8+i for i in range(8)))
 return mesh(name,v,f,material,col,bevel)
def cyl(name,p,r,d,material='steel',col='detail',axis=(0,1,0),n=32,bevel=.008):
 bpy.ops.mesh.primitive_cylinder_add(vertices=n,radius=r,depth=d,location=p)
 o=bpy.context.object;o.rotation_euler=Vector(axis).to_track_quat('Z','Y').to_euler()
 return finish(o,name,material,col,bevel)
def tube(name,points,r=.025,material='dark',col='detail'):
 cu=bpy.data.curves.new(name,'CURVE');cu.dimensions='3D';cu.bevel_depth=r;cu.bevel_resolution=3
 sp=cu.splines.new('POLY');sp.points.add(len(points)-1)
 for p,v in zip(sp.points,points):p.co=(*v,1)
 return finish(bpy.data.objects.new(name,cu),name,material,col,0)
def bolt(name,x,y,z,r=.035):
 s=1 if y>0 else -1
 cyl(name,(x,y,z),r,.016,'steel',n=8,bevel=.002)
 cyl(name+' socket',(x,y+s*.010,z),r*.46,.004,'dark',n=6,bevel=0)
def ring(name,x,z,outer,inner,depth,material='steel',n=8):
 v=[]
 for xx in (x-depth/2,x+depth/2):
  for r in (outer,inner):
   v.extend((xx,math.cos((i+.5)*math.tau/n)*r,z+math.sin((i+.5)*math.tau/n)*r) for i in range(n))
 f=[]
 for i in range(n):
  j=(i+1)%n;f.extend([(i,j,2*n+j,2*n+i),(n+i,3*n+i,3*n+j,n+j),(i,n+i,n+j,j),(2*n+i,2*n+j,3*n+j,3*n+i)])
 return mesh(name,v,f,material,'barrel',.007)

# The primary proportions follow the pixels in the user's LOWER LEFT side view.
# Muzzle at -5.70, white receiver centered at 0, tail at 5.70.
loft('Forebarrel structural core',[(-5.35,.17,.00,.47),(-4.65,.22,-.04,.52),(-3.1,.265,-.05,.56),(-1.65,.29,-.08,.55),(-.62,.22,.0,.48)])
loft('Narrow muzzle squared collar',[(-5.72,.22,.035,.405),(-5.37,.24,.01,.44)],'panel',open_ends=True)
ring('Octagonal open muzzle lip',-5.72,.22,.216,.149,.085)
ring('Deep internal bore liner',-5.43,.22,.149,.133,.52,'dark')
cyl('Bore deep blind interior',(-5.14,0,.22),.133,.015,'dark','barrel',(-1,0,0),32,0)
loft('Segment A narrow nose jacket',[(-5.34,.225,.00,.44),(-4.55,.24,-.02,.48)],'shell')
loft('Segment B forward long jacket',[(-4.51,.242,-.035,.48),(-3.55,.258,-.055,.55)],'shell')
loft('Segment C service barrel jacket',[(-3.51,.262,-.055,.55),(-2.44,.28,-.065,.56)],'shell')
loft('Segment D breech jacket',[(-2.40,.289,-.065,.56),(-1.54,.3,-.07,.57)],'panel')
for s in (-1,1):
 y=s*.255
 prism('Nose inset recess '+str(s),[(-5.34,.29),(-5.2,.46),(-4.62,.46),(-4.49,.3)],y,.023,'dark','barrel',.007)
 prism('Nose yellow identifier '+str(s),[(-5.27,.31),(-5.15,.411),(-4.69,.411),(-4.57,.31)],y+s*.018,.018,'yellow','barrel',.006)
 box('Nose lower cheek '+str(s),(-4.97,s*.257,.12),(.76,.055,.23),'panel','barrel',.014)
 box('Barrel service plate gasket '+str(s),(-3.01,s*.276,.065),(.91,.024,.29),'dark','barrel',.008)
 box('Barrel service plate '+str(s),(-3.01,s*.296,.068),(.84,.023,.244),'panel','barrel',.01)
 for x in (-3.36,-2.67):
  for z in (-.006,.146):bolt('Barrel captive screw',x,s*.313,z,.023)
 tube('Forward underslung cable '+str(s),[(-4.8,s*.21,.10),(-4.55,s*.27,-.22),(-4.12,s*.27,-.23),(-3.91,s*.22,.10)],.032)
 tube('Nose dorsal handle '+str(s),[(-5.3,s*.16,.47),(-5.08,s*.16,.65),(-4.7,s*.16,.66),(-4.54,s*.16,.48)],.039)
 for x in (-4.5,-3.52,-2.42):box('Barrel jacket join '+str(s),(x,s*.27,.07),(.025,.015,.19),'dark','barrel',.002)
loft('Raised forebarrel locator',[(-3.28,.08,.53,.69),(-3.06,.08,.53,.7),(-2.98,.07,.53,.57)],'panel')
box('Front clamp block',(-1.58,0,.13),(.62,.71,.39),'shell','receiver',.04)
for s in (-1,1):
 box('Front clamp cheek '+str(s),(-1.58,s*.374,.13),(.46,.07,.24),'panel','receiver',.02)
 cyl('Front clamp trunnion '+str(s),(-1.6,s*.333,-.10),.155,.11,'shell','receiver')
 cyl('Front clamp recessed pin '+str(s),(-1.6,s*.396,-.10),.075,.018,'dark','receiver',n=12)
 for x in (-1.76,-1.40):bolt('Clamp captive bolt',x,s*.421,.18,.03)

# Small white top service housing, not a projecting telescopic sight.
loft('Dorsal service unit dark base',[(-1.93,.25,.54,.91),(-1.03,.25,.54,.91),(-.91,.2,.5,.78)],'shell','receiver',.025)
loft('White dorsal service unit',[(-1.91,.263,.57,.85),(-1.79,.263,.57,.94),(-1.22,.263,.57,.94),(-1.10,.245,.57,.82)],'white','receiver',.019)
box('Dorsal service face recess',(-1.941,0,.744),(.015,.30,.19),'dark','receiver',.014)
for s in (-1,1):
 box('Dorsal unit corner latch '+str(s),(-1.75,s*.272,.88),(.095,.03,.04),'steel','receiver',.004)
 tube('Dorsal service return '+str(s),[(-1.0,s*.17,.79),(-.77,s*.17,.75),(-.74,s*.17,.55)],.034)
loft('Central receiver chassis',[(-1.18,.255,-.14,.48),(-.58,.28,-.21,.5),(.75,.29,-.20,.49),(1.30,.34,-.17,.56)],'shell','receiver',.026)
for s in (-1,1):
 profile=[(-.89,-.21),(-.99,-.10),(-.99,.18),(-.80,.31),(.79,.31),(1.0,.20),(1.0,-.22)]
 prism('White receiver armor '+str(s),profile,s*.322,.09,'white','receiver',.022)
 box('White receiver inset seam '+str(s),(.02,s*.375,.045),(1.5,.012,.30),'dark','receiver',.015)
 box('White receiver inset panel '+str(s),(.02,s*.386,.05),(1.445,.022,.25),'white','receiver',.013)
 box('Receiver vertical assembly seam '+str(s),(-.69,s*.408,.045),(.021,.008,.36),'panel','receiver',.002)
 for x in (-.8,.64):
  for z in (-.135,.215):bolt('Receiver captive screw',x,s*.391,z,.029)
 box('Grip upper mount gasket '+str(s),(.63,s*.414,-.09),(.41,.037,.24),'dark','receiver',.02)
 box('Grip upper mount cover '+str(s),(.63,s*.44,-.09),(.31,.03,.17),'panel','receiver',.012)
 box('Grip upper slot '+str(s),(.63,s*.459,-.07),(.20,.008,.055),'dark','receiver',.003)
prism('Short downward grip',[(.45,-.21),(.70,-.20),(.83,-.87),(.58,-.88)],0,.24,'dark','receiver',.025)
for s in (-1,1):
 prism('Grip side spine '+str(s),[(.47,-.28),(.51,-.28),(.65,-.79),(.59,-.80)],s*.132,.026,'panel','receiver',.005)
 for i in range(7):box('Grip traction rib '+str(s),(.59+i*.016,s*.138,-.33-i*.065),(.14,.025,.022),'panel','receiver',.004)
box('Grip heel',(.73,0,-.91),(.34,.32,.11),'panel','receiver',.017)
loft('Rear raised neck chassis',[(.02,.25,.31,.60),(.80,.29,.31,1.03),(1.45,.30,.31,1.00)],'panel','rear',.02)

# The rear shroud sweeps down toward the back, as drawn; no generic rounded tank.
rear_sections=[(.95,.32,.02,1.0),(1.47,.38,.02,1.03),(2.1,.41,.08,1.00),(3.35,.385,.27,.98),(4.5,.30,.37,.9),(5.48,.19,.42,.79),(5.69,.15,.44,.68)]
loft('Long swept rear shell',rear_sections,'shell','rear',.028)
for s in (-1,1):
 prism('Swept rear side panel '+str(s),[(1.58,.20),(1.56,.90),(2.22,.94),(3.31,.91),(4.41,.84),(5.24,.72),(5.24,.50),(4.23,.49),(3.51,.41),(3.06,.20)],s*.365,.025,'shell','rear',.013)
 tube('Rear upper panel seam '+str(s),[(1.67,s*.374,.92),(2.25,s*.399,.91),(3.35,s*.374,.87),(4.36,s*.301,.79)],.008,'dark')
 prism('Rear underside layered rail '+str(s),[(3.45,.26),(3.61,.37),(5.42,.39),(5.52,.46),(5.49,.56),(3.53,.50)],s*.255,.07,'panel','rear',.012)
 box('Aft rail dark slot '+str(s),(4.37,s*.30,.39),(1.25,.018,.035),'dark','rear',.003)
loft('Aft shell end cap',[(5.47,.199,.415,.792),(5.69,.155,.43,.69),(5.73,.14,.44,.66)],'panel','rear',.014)
for s in (-1,1):bolt('Aft shell cap fastener',5.59,s*.168,.59,.021)

# Raised front crest of the rear module, a strongly identifying silhouette.
prism('Raised rear crest',[(-.25,1.00),(-.25,1.37),(-.10,1.43),(.83,1.43),(1.08,1.30),(1.55,.63),(1.42,.37),(1.03,.36),(.88,.99)],0,.62,'shell','rear',.025)
for s in (-1,1):
 prism('Crest tall cheek '+str(s),[(.69,1.36),(.91,1.35),(1.36,.69),(1.34,.29),(1.07,.29),(.96,.49),(.65,.50)],s*.335,.05,'panel','rear',.015)
 box('Crest front metal band '+str(s),(-.18,s*.32,1.21),(.075,.025,.33),'steel','rear',.008)
 box('Crest latch '+str(s),(.05,s*.325,1.32),(.12,.03,.058),'dark','rear',.004)
 bolt('Crest latch pin',.12,s*.35,1.32,.021)
 box('Crest lower hinge '+str(s),(1.24,s*.379,.44),(.22,.10,.18),'shell','rear',.017)
 for x,z in ((.91,.62),(1.13,.86)):bolt('Crest panel bolt',x,s*.371,z,.027)
 tube('Top exposed black return line '+str(s),[(1.16,s*.22,1.01),(1.45,s*.23,1.13),(2.20,s*.23,1.10),(2.55,s*.22,1.02)],.04)
 cyl('Crest top rear piston '+str(s),(1.20,s*.19,1.31),.05,1.12,'steel','rear',(1,0,0),24,.006)
 for x in (1.65,1.74):cyl('Piston collar', (x,s*.19,1.31),.067,.045,'shell','rear',(1,0,0),24,.005)

# Yellow power line and lower chassis, inset under the long shell.
loft('Lower rear keel',[(1.24,.27,-.40,-.06),(1.48,.30,-.49,-.04),(2.62,.29,-.49,-.04),(3.06,.23,-.22,.05)],'shell','rear',.021)
for s in (-1,1):
 box('Rear energy line recess '+str(s),(2.25,s*.34,.03),(1.84,.03,.15),'dark','rear',.009)
 box('Rear yellow power line '+str(s),(2.23,s*.36,.027),(1.69,.023,.075),'yellow','rear',.006)
 box('Rear keel raised side '+str(s),(2.03,s*.309,-.30),(1.43,.045,.17),'panel','rear',.013)
 box('Rear keel lock '+str(s),(1.45,s*.34,-.37),(.29,.10,.19),'panel','rear',.014)
 for x in (1.57,2.11):
  cyl('Yellow line clamp '+str(s),(x,s*.381,.03),.081,.037,'shell','rear',n=12)
  bolt('Line clamp pin',x,s*.408,.03,.027)

# Main circular module sits on one side; opposite side carries its shallow mounting plate.
prism('Rotary module diagonal support',[(3.14,.12),(3.31,.39),(3.90,.31),(4.32,.08),(4.29,-.28),(3.83,-.46),(3.43,-.30)],0,.61,'violet','drum',.03)
box('Rotary aft terminal',(4.22,0,-.26),(.55,.74,.52),'violet','drum',.045)
cyl('Rotary black gasket',(3.83,-.41,-.23),.474,.16,'dark','drum',n=48)
cyl('Rotary slate housing',(3.83,-.54,-.23),.452,.25,'violet','drum',n=32,bevel=.017)
cyl('Rotary outer metal edge',(3.83,-.681,-.23),.416,.039,'panel','drum',n=48)
cyl('Rotary inset face',(3.83,-.707,-.23),.356,.022,'violet','drum',n=48)
cyl('Rotary center cap',(3.83,-.726,-.23),.103,.028,'panel','drum',n=16)
for i in range(8):
 a=i*math.tau/8
 tube('Rotary radial recessed seam',[(3.83+math.cos(a)*r,-.723,-.23+math.sin(a)*r) for r in (.11,.347)],.007,'dark','drum')
 bolt('Rotary perimeter bolt',3.83+math.cos(a)*.39,-.714,-.23+math.sin(a)*.39,.022)
for s in (-1,1):
 prism('Rotary upper arc fairing '+str(s),[(3.13,.11),(3.26,.34),(3.61,.41),(4.02,.24),(4.17,.04),(4.01,-.01),(3.85,.16),(3.57,.24),(3.38,.17),(3.29,.01)],s*.42,.115,'violet','drum',.014)
 for x,z in ((3.30,.23),(3.63,.33),(4.02,.14)):bolt('Arc fairing fastener',x,s*.485,z,.026)
cyl('Opposite shallow rotary axle',(3.83,.372,-.23),.27,.075,'panel','drum',n=16)
cyl('Opposite rotary axle socket',(3.83,.417,-.23),.124,.017,'dark','drum',n=8)

# Conform long panel edges to the taper; a constant-width extrusion would float at the tail.
def rear_surface(x,z):
 for a,b in zip(rear_sections, rear_sections[1:]):
  if a[0]<=x<=b[0]:
   t=(x-a[0])/(b[0]-a[0]);w,lo,hi=[a[k]+t*(b[k]-a[k]) for k in (1,2,3)]
   c=min(w*.40,(hi-lo)*.22)
   return w-min(c,max(0,z-(hi-c),(lo+c)-z))
 return .15
for o in list(C['rear'].objects):
 if o.name.startswith('Swept rear side panel'):
  bpy.data.objects.remove(o,do_unlink=True)
 elif o.name.startswith('Aft rail dark slot'):
  bpy.data.objects.remove(o,do_unlink=True)
 elif o.name.startswith('Rear underside layered rail'):
  s=1 if o.name.endswith('1') and not o.name.endswith('-1') else -1
  old=.365 if o.name.startswith('Swept') else .255
  for v in o.data.vertices:
   v.co.y=s*(rear_surface(v.co.x,v.co.z)+.008)+(v.co.y-s*old)
for s in (-1,1):
 tube('Conforming aft rail inset '+str(s),[(x,s*(rear_surface(x,.40)+.045),.40) for x in (3.80,4.20,4.50,4.90,5.15)],.008,'dark','rear')
for s in (-1,1):
 v=[]
 for x,w,lo,hi in rear_sections[1:-1]:
  c=min(w*.40,(hi-lo)*.22)
  for y in (s*(w+.005),s*(w+.026)):
   v.extend([(x,y,lo+c+.035),(x,y,hi-c-.035)])
 faces=[(0,1,3,2)]
 for j in range(len(v)//4-1):
  a=j*4;b=a+4
  faces.extend([(a,a+1,b+1,b),(a+2,b+2,b+3,a+3),(a,b,b+2,a+2),(a+1,a+3,b+3,b+1)])
 a=len(v)-4;faces.append((a,a+2,a+3,a+1))
 mesh('Segmented conforming rear skin '+str(s),v,faces,'shell','rear',.006)

exec(compile((Path(__file__).parent/'detail_pass.py').read_text(encoding='utf-8'), 'detail_pass.py', 'exec'))

# Practical attachment points are empties, excluded from studio and usable for later fitting.
for name,p in [('SOCKET_Grip',(.66,0,-.57)),('SOCKET_Muzzle',(-5.78,0,.22)),('SOCKET_BackMount',(2.0,.39,.31)),('SOCKET_Support',(-1.60,0,-.10))]:
 o=bpy.data.objects.new(name,None);C['sockets'].objects.link(o);o.parent=root;o.location=p;o.empty_display_type='ARROWS';o.empty_display_size=.15
root.scale=(3.5/11.49,)*3
bpy.context.view_layer.update()

# White studio: weapon is always alone, no robot or scale props.
world=bpy.data.worlds.new('Neutral studio');scene.world=world;world.use_nodes=True
world.node_tree.nodes['Background'].inputs[0].default_value=(.55,.60,.70,1)
world.node_tree.nodes['Background'].inputs[1].default_value=.45
wn=world.node_tree.nodes;wl=world.node_tree.links
path=wn.new('ShaderNodeLightPath');back=wn.new('ShaderNodeBackground');mix=wn.new('ShaderNodeMixShader')
back.inputs[0].default_value=(1,1,1,1);back.inputs[1].default_value=.9
wl.new(path.outputs['Is Camera Ray'],mix.inputs[0]);wl.new(wn['Background'].outputs[0],mix.inputs[1]);wl.new(back.outputs[0],mix.inputs[2]);wl.new(mix.outputs[0],wn['World Output'].inputs['Surface'])
def camera(name,p,target,scale):
 d=bpy.data.cameras.new(name);o=bpy.data.objects.new(name,d);C['studio'].objects.link(o);o.location=p
 o.rotation_euler=(Vector(target)-o.location).to_track_quat('-Z','Y').to_euler();d.type='ORTHO';d.ortho_scale=scale;return o
camera('01_HERO',(-2.0,-4.8,2.15),(0,0,.08),4.10)
camera('02_SIDE',(0,-6,.08),(0,0,.08),3.90)
camera('03_TOP',(0,0,6),(0,0,0),3.90)
camera('04_REAR_QUARTER',(2.5,5,2.5),(0,0,.08),4.10)
camera('05_MUZZLE',(-5,0,.075),(-1.5,0,.075),.93)
camera('06_RECEIVER',(-.1,-3,1.1),(.29,0,.045),1.95)
camera('07_DETAIL_REAR',(.9,-3,1.35),(.80,0,.18),1.70)
camera('08_DETAIL_FRONT',(-1.2,-3,1.1),(-1.06,0,.07),1.55)
for name,p,power,size in [('Key',(-1,-3,4),700,4),('Fill',(1,3,2),500,3),('Edge',(2,-.5,3),650,2.5)]:
 d=bpy.data.lights.new(name,'AREA');d.energy=power;d.shape='DISK';d.size=size
 o=bpy.data.objects.new(name,d);C['studio'].objects.link(o);o.location=p;o.rotation_euler=(Vector((0,0,0))-o.location).to_track_quat('-Z','Y').to_euler()
scene.render.engine='CYCLES';scene.cycles.samples=48;scene.cycles.use_denoising=True
try:
 prefs=bpy.context.preferences.addons['cycles'].preferences;prefs.compute_device_type='OPTIX';prefs.get_devices()
 for d in prefs.devices:d.use=d.type!='CPU'
 scene.cycles.device='GPU' if any(d.use for d in prefs.devices) else 'CPU'
except:scene.cycles.device='CPU'
scene.render.resolution_x=2200;scene.render.resolution_y=1100;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG';scene.render.film_transparent=False
scene.view_settings.view_transform='AgX'
scene.camera=bpy.data.objects['01_HERO']
for screen in bpy.data.screens:
 for a in screen.areas:
  if a.type=='VIEW_3D':
   a.spaces.active.region_3d.view_rotation=scene.camera.rotation_euler.to_quaternion();a.spaces.active.region_3d.view_location=(0,0,.08);a.spaces.active.region_3d.view_distance=4.4
   a.spaces.active.shading.type='MATERIAL';a.spaces.active.overlay.show_overlays=False
# Pack the controlling reference in the blend, rather than leaving a fragile temp path.
for name in ('USER_REFERENCE.png','FAZZ_CANNON_DESIGN.png'):
 im=bpy.data.images.load(str(ROOT/'reference'/name));im.pack()
bpy.ops.object.select_all(action='DESELECT');root.select_set(True);bpy.context.view_layer.objects.active=root
scene.render.filepath=str(ROOT/'renders/01_HERO.png')
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'FAZZ_CANNON_REDESIGN.blend'))
weapon=[o for o in root.children_recursive if o.type in {'MESH','CURVE'}]
dg=bpy.context.evaluated_depsgraph_get();tri=0
for o in weapon:
 ev=o.evaluated_get(dg);me=ev.to_mesh();me.calc_loop_triangles();tri+=len(me.loop_triangles);ev.to_mesh_clear()
stats={'asset':'FAZZ long mega beam cannon reference rebuild','parts':len(weapon),'evaluated_triangles':tri,'scale_nominal_m':3.5,'reference':'USER_REFERENCE.png lower left weapon panel','model_status':'Editable hard surface design model; no UV bake, LOD or game integration','cycles_device':scene.cycles.device}
(ROOT/'model_stats.json').write_text(json.dumps(stats,indent=2),encoding='utf-8')
print('BUILD_COMPLETE',stats,flush=True)
for name in ('01_HERO','02_SIDE','03_TOP','04_REAR_QUARTER','05_MUZZLE','06_RECEIVER','07_DETAIL_REAR','08_DETAIL_FRONT'):
 scene.camera=bpy.data.objects[name];scene.render.filepath=str(ROOT/'renders'/f'{name}.png')
 scene.render.resolution_x=1600 if name=='05_MUZZLE' else 2200
 scene.render.resolution_y=1600 if name=='05_MUZZLE' else 1100
 bpy.ops.render.render(write_still=True)
print('RENDERS_COMPLETE',flush=True)
