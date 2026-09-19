import bpy,bmesh,pathlib,math,json
from mathutils import Vector,Matrix
from mathutils.geometry import tessellate_polygon
W=pathlib.Path(__file__).resolve().parent;ROOT=W.parent;S=.01;SC=bpy.context.scene;PARTS=[];M={m.name.replace('FAZZ ',''):m for m in bpy.data.materials if m.name.startswith('FAZZ ')};asset=bpy.data.collections['FAZZ_COMPLETE_ASSEMBLY'];root=bpy.data.objects['FAZZ_MASTER_ROOT'];COL=None;PARENT=None
exec((W/'v3_geometry.py').read_text())
def group(n,p=root):
 global COL,PARENT
 COL=bpy.data.collections.get(n)
 if not COL:COL=bpy.data.collections.new(n);asset.children.link(COL)
 PARENT=p
def remove(n):
 o=bpy.data.objects.get('FAZZ_'+n)
 if o:bpy.data.objects.remove(o,do_unlink=True)
def cut(ob,c):
 if isinstance(ob,str):ob=bpy.data.objects.get('FAZZ_'+ob)
 if not ob:return
 md=ob.modifiers.new('Physical aperture','BOOLEAN');md.object=c;md.operation='DIFFERENCE';md.solver='EXACT';bpy.context.view_layer.objects.active=ob;bpy.ops.object.modifier_move_to_index(modifier=md.name,index=0);bpy.ops.object.modifier_apply(modifier=md.name)
def slot_from_frame(frame_name,targets):
 fr=bpy.data.objects.get('FAZZ_'+frame_name+'_Frame')
 if not fr:return
 # V3 frame constructor: outer ring, inner ring, inside/back ring, outer back ring.
 n=len(fr.data.vertices)//4
 front=[fr.data.vertices[n+i].co/S for i in range(n)];back=[fr.data.vertices[2*n+i].co/S for i in range(n)]
 vv=[tuple(p+Vector((0,-.35,0))) for p in front]+[tuple(p+Vector((0,1,0))) for p in back]
 fs=[tuple(range(n-1,-1,-1)),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
 c=mesh('TEMP aperture',vv,fs,'black',0)
 for name in targets:cut(name,c)
 bpy.data.objects.remove(c,do_unlink=True)
group('15 Machined detail',root)
for side in ['L','R']:
 slot_from_frame('Chest lower oblique ventilation '+side,['Chest wrapped side '+side,'Thoracic underside trim '+side])
 slot_from_frame('Shoulder bottom inlet '+side,['Shoulder long ceramic cassette '+side])
 for t in [47,61,115]:slot_from_frame('Calf recessed cooling cassette '+side+str(t),['Calf front multi-plane armour '+side,'Calf faceted heavy gaiter '+side,'Calf lower long cover '+side])
 slot_from_frame('Calf narrow blue knee cassette '+side,['Calf front multi-plane armour '+side,'Calf faceted heavy gaiter '+side])
 slot_from_frame('Eye socket '+side,['Face inner core'])
 slot_from_frame('Cheek recessed acoustic slot '+side,['Face inner core'])
slot_from_frame('Left chest missile hatch',['Pectoral ceramic panel L','Chest wrapped side L'])
slot_from_frame('Central torso narrow sensor',['Central chest long ceramic keel'])
# Add deep seat below missing front chest panels rather than an exposed large neck slab.
core=bpy.data.objects['FAZZ_Thoracic cast carrier']
for v in core.data.vertices:
 if v.co.z>3.43:v.co.z=3.43+(v.co.z-3.43)*.35
# New forehead: raked pointed lower extension, folded top cap and inset navy face.
head=bpy.data.objects['HEAD_MOUNT'];group('09 Head shell',head)
for n in ['Blue forehead cowl','Forehead recessed dark sensor_Frame','Forehead recessed dark sensor_Well','Forehead inner sensor glass']:remove(n)
vs=[(-5,-20,398),(5,-20,398),(9,-18,407),(12,-12,426),(7,-9,430),(-7,-9,430),(-12,-12,426),(-9,-18,407)]
skin('Forehead sloping blue perimeter',vs,[(0,1,2,3,4,5,6,7)],(0,5,0),'blue',.2)
# Inset opening conforms to fore/aft slope; narrow beaked bottom is the FAZZ signature.
def ff(x,z,dep=0):return (x,-20+(z-398)*.31+dep,z)
out=[ff(x,z) for x,z in [(-3,401),(3,401),(6.5,410),(7.5,423),(4,427),(-4,427),(-7.5,423),(-6.5,410)]]
inn=[ff(x,z,-.08) for x,z in [(-1.6,403),(1.6,403),(4.7,411),(5.7,422),(3,425),(-3,425),(-5.7,422),(-4.7,411)]]
recess('Forehead layered long aperture',out,inn,2,'blue')
slot_from_frame('Forehead layered long aperture',['Forehead sloping blue perimeter'])
plate('Forehead seated navy insert',[tuple(Vector(p)+Vector((0,1.8,0))) for p in inn],(0,.4,0),'navy',.12)
strip('Forehead inset highlight rail',[ff(-2,407,1.2),ff(-3.3,420,1.2)],.30,'steel',(0,.25,0))
# Tighten the mask and cheek assembly into the eyes, reduce the empty black rectangle.
mask=bpy.data.objects['FAZZ_Face folded white mask']
for v in mask.data.vertices:
 if v.co.z>3.90:v.co.z+=.012
for side in ['L','R']:
 brow=bpy.data.objects['FAZZ_White angular brow '+side]
 for v in brow.data.vertices:v.co.y+=.015
 # A broad luminous lens remains inside the metal lip, with visible depth.
 eye=bpy.data.objects['FAZZ_Recessed emerald lens '+side]
 for v in eye.data.vertices:v.co.y-=.004
# Add stepped temple cladding, tiny recessed ports and helmet panel seams.
for s,side in [(-1,'L'),(1,'R')]:
 mi=lambda p:(s*p[0],p[1],p[2])
 plate('Temple forehead lower return '+side,[mi(p) for p in [(13,-9,408),(20,-6,405),(21,-8,401),(15,-12,402)]],(0,1.3,-.4),'grey',.13)
 strip('Helmet top narrow panel seam '+side,[mi(p) for p in [(12,0,410),(17,6,407),(19,13,402)]],.25,'frame',(-s*.1,.1,-.2))
 for j in range(3):strip('Temple rear vent '+side+str(j),[mi(p) for p in [(24,6,392-j*2),(24,13,391-j*2)]],.7,'frame',(-s*.4,0,0))
 bolt('Head temple flush pivot '+side,mi((24,2,396)),(s,0,0),.55,'steel')
# Proper upper pylon closures; staged return lips, not open white tubes.
for s,side in [(-1,'L'),(1,'R')]:
 group('10 Backpack',bpy.data.objects['BACKPACK_MOUNT']);mi=lambda p:(s*p[0],p[1],p[2])
 box('Pylon sealed top cap '+side,mi((41,38,434)),(8,9,1.4),'white',.4)
 # Vent banks and bolt strips join the side shell to the rear propulsion unit.
 for j in range(4):
  plate('Pack layered lower exhaust guide '+side+str(j),[mi(p) for p in [(16,72,335-j*4),(28,73,335-j*4),(28,74,333-j*4),(16,73,333-j*4)]],(0,-1,0),'grey',.12)
 # Real mechanical support pads flank each propellant tank.
 for z in [260,180]:
  box('Tank suspension pad '+side+str(z),mi((48,57,z)),(16,10,6),'frame',.35)
  bolt('Tank suspension cap '+side+str(z),mi((57,60,z)),(s,0,0),1,'steel')
# Recessed perimeter channels on the major plates: actual boolean grooves along local raked planes.
def engrave(name,pts,normal=(0,1,0),width=.28,depth=.5):
 target=bpy.data.objects.get('FAZZ_'+name)
 if not target:return
 norm=Vector(normal).normalized()
 for j,(a,b) in enumerate(zip(pts[:-1],pts[1:])):
  a=Vector(a);b=Vector(b);t=(b-a).normalized();side=t.cross(norm).normalized()*width/2
  c=plate('TEMP fine channel',[a+side-norm*.25,b+side-norm*.25,b-side-norm*.25,a-side-norm*.25],tuple(norm*depth),'frame',0);cut(target,c);bpy.data.objects.remove(c,do_unlink=True)
for s,side in [(-1,'L'),(1,'R')]:
 mi=lambda p:(s*p[0],p[1],p[2]);group('15 Machined detail',root)
 def lf(x,t,d=0):return mi((x,-24-.043*t+d,197-t))
 engrave('Calf front multi-plane armour '+side,[lf(x,t) for x,t in [(23,7),(21,31),(20,38)]],width=.30,depth=.7)
 engrave('Calf front multi-plane armour '+side,[lf(x,t) for x,t in [(47,35),(51,56),(50,87),(46,109)]],width=.30,depth=.7)
 engrave('Calf lower long cover '+side,[lf(x,t) for x,t in [(26,73),(27,94),(33,102)]],width=.28,depth=.6)
 # Corner clamps are seated against the front surface, connected to internal support ribs.
 for x,t in [(24,36),(48,107)]:
  p=lf(x,t,-.65);box('Calf captive clamp '+side+str(t),p,(2,1.5,3),'grey',.12)
  bolt('Calf clamp fastener '+side+str(t),Vector(p)+Vector((0,-.8,0)),(0,-1,0),.45,'steel')
 def cf(x,t,d=0):return mi((x,-42+.5*t+.11*x+d,356-t))
 engrave('Pectoral ceramic panel '+side,[cf(x,t) for x,t in [(17,3),(43,3),(44,8)]],width=.25,depth=.7)
 engrave('Pectoral ceramic panel '+side,[cf(x,t) for x,t in [(18,29),(25,32),(40,30)]],width=.25,depth=.7)
 def sf(x,t,d=0):return mi((x,-20+.08*t+d,376-t))
 engrave('Shoulder long ceramic cassette '+side,[sf(x,t) for x,t in [(59,5),(60,18)]],width=.30,depth=.7)
 engrave('Shoulder long ceramic cassette '+side,[sf(x,t) for x,t in [(73,29),(76,30),(76,33)]],width=.3,depth=.7)
 group('04 Arms '+side,bpy.data.objects['FOREARM_'+side])
 def af(x,t,d=0):return mi((x,-22+.04*t+d,288-t))
 engrave('Forearm wrapped heavy cover '+side,[af(x,t) for x,t in [(63,5),(63,25),(66,30)]],width=.25,depth=.75)
 # Side recessed beam channels connect forearm front to back.
 plate('Forearm side split metal insert '+side,[mi(p) for p in [(83,-6,280),(86,0,278),(86,0,262),(82,-8,258)]],(-s*.7,0,0),'frame',.18)
 for j in range(4):strip('Forearm side heat vane '+side+str(j),[mi(p) for p in [(83,-5,265+j*3),(85,-1,266+j*3)]],.6,'steel',(-s*.5,0,0))
# Stronger dark monochrome cannon and denser side equipment.
group('12 Hyper mega cannon',bpy.data.objects['HYPER_MEGA_CANNON'])
origin=Vector((-83,35,411));axis=Vector((0,-.72,-.694)).normalized();ux=Vector((1,0,0));uy=axis.cross(ux).normalized()
def gun(x,y,t):return tuple(origin+ux*x+uy*y+axis*t)
for o in list(COL.objects):
 for i,m in enumerate(o.data.materials if o.type=='MESH' else []):
  if m in [M['navy'],M['frame']]:o.data.materials[i]=M['black']
for s in [-1,1]:
 # Casing inset border with real rectangular recess cut into the side skin.
 p=[gun(s*20.3,-9,24),gun(s*20.3,-9,57),gun(s*20.3,9,64),gun(s*20.3,12,30)]
 engrave('Cannon side upper service plate '+str(s),p+[p[0]],(-s,0,0),.4,.8)
 for j in range(5):
  a=gun(s*20.6,-8,32+j*4);b=gun(s*20.6,6,32+j*4);strip('Cannon side heat sink '+str(s)+str(j),[a,b],1.0,'frame',tuple(ux*(-s*1.4)))
 for t in [128,146,183,233]:
  plate('Cannon staggered access cover '+str(s)+str(t),[gun(s*10,-7,t),gun(s*10,6,t),gun(s*10,6,t+8),gun(s*10,-7,t+8)],tuple(ux*(-s*.8)),'frame',.18)
  bolt('Cannon hatch screw '+str(s)+str(t),gun(s*10.7,0,t+4),(s,0,0),.65,'steel')
# Wrap the canonical yellow strip around upper cannon cover.
strip('Cannon rear yellow identification',[gun(0,-22,20),gun(0,-22,73)],1.4,'gold',tuple(uy*.7))
# Match text to inclined panel planes; currently horizontal text was partly buried.
for o in list(asset.all_objects):
 if o.type!='MESH':continue
 n=o.name
 slope=None
 if 'Chest large unit' in n or 'Chest small stencil' in n:slope=.5
 elif 'Calf Anaheim' in n or 'Calf service text' in n:slope=-.043
 elif 'Shoulder caution' in n:slope=.08
 if slope is not None:
  # All converted text keeps its world transform. Rake around its own centre to surface slope.
  loc=o.matrix_world.translation.copy();o.rotation_euler.x=math.pi/2-math.atan(slope);o.location.y-=.006
# Separate text surfaces remain open by intention, mark them as decals for the audit.
for o in asset.all_objects:
 if o.type=='MESH' and any(k in o.name for k in ['stencil','large unit','caution','logo','service text','unit number','small serial']):o['surface_marking']=True
bpy.context.view_layer.update();bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'FAZZ_V3_METHOD.blend'))
print('REFINEMENT SAVED')
