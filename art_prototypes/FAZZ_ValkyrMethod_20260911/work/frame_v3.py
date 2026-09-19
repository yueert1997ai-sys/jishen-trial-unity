import bpy,bmesh,pathlib,math,json,hashlib
from mathutils import Vector,Matrix
from mathutils.geometry import tessellate_polygon
W=pathlib.Path(__file__).resolve().parent;R=W.parent;S=.01;PARTS=[]
M={m.name.replace('FAZZ ',''):m for m in bpy.data.materials if m.name.startswith('FAZZ ')}
asset=bpy.data.collections['FAZZ_COMPLETE_ASSEMBLY'];root=bpy.data.objects['FAZZ_MASTER_ROOT'];PARENT=root
COL=bpy.data.collections.new('17 V3 INTERNAL FRAME');asset.children.link(COL)
exec((W/'v3_geometry.py').read_text())
framecol=COL
headset=set(bpy.data.collections['FAZZ_HEAD_ASSET'].all_objects)
def headhash():return sorted(hashlib.sha256(repr(([tuple(v.co) for v in o.data.vertices],[tuple(p.vertices) for p in o.data.polygons])).encode()).hexdigest() for o in headset if o.type=='MESH')
hh=headhash()
def remove(n):
 o=bpy.data.objects.get('FAZZ_'+n)
 if o:bpy.data.objects.remove(o,do_unlink=True)
def deform(p):
 p=Vector(p);z=p.z;p.z=z if z<=.45 else (.45+(z-.45)*.8 if z<2.05 else z-.32)
 p.x*=1+.1*max(0,min(1,(z-2.3)/.7));return p
def beam(n,a,b,w=3,d=3,mat='frame'):
 a=Vector(a);b=Vector(b);q=(b-a).to_track_quat('Z','Y');vs=[tuple(p+q@Vector((x*w/2,y*d/2,0))) for p in (a,b) for x,y in [(-1,-1),(1,-1),(1,1),(-1,1)]]
 return mesh(n,vs,[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)],mat,.2)
def hose(n,pts,r=.8):
 for j,(a,b) in enumerate(zip(pts[:-1],pts[1:])):rod(n+str(j),a,b,r,'rubber',n=12,bevel=.06)
def gear(n,c,axis,r=6,depth=4):
 c=Vector(c);ax=Vector(axis).normalized();q=ax.to_track_quat('Z','Y')
 bearing(n,c,axis,r,depth,False)
 for j in range(16):
  t=j*math.tau/16;p=c+q@Vector((r*.94*math.cos(t),r*.94*math.sin(t),depth*.55))
  rod(n+' toothed race '+str(j),p,p+ax*.7,r*.105,'steel',n=8,bevel=.025)
 for j in range(6):
  t=j*math.tau/6;p=c+q@Vector((r*.66*math.cos(t),r*.66*math.sin(t),depth*.65))
  bolt(n+' flange '+str(j),p,ax,.5)
# Remove closed placeholder cores; genuine space remains between the new frame rails.
for n in ['Thoracic cast carrier','Waist armoured bearing housing','Spine column']:
 remove(n)
for side in ['L','R']:
 for pre in ['Thigh underlying shroud ','Upper arm grey enclosure ','Forearm closed inner jacket ','Humerus load rail ','Femur beam ','Shin longitudinal beam ']:remove(pre+side)
# Thoracic box truss with cross-members, central power module and separate equipment cassettes.
PARENT=bpy.data.objects['THORAX']
for s,side in [(-1,'L'),(1,'R')]:
 mi=lambda p:(s*p[0],p[1],p[2])
 for y in [-12,16]:
  beam('V3 thorax vertical spar '+side+str(y),mi((20,y,307)),mi((30,y,358)),5,6)
  beam('V3 thorax triangulation '+side+str(y),mi((21,y,310)),mi((36,y,343)),3,4,'steel')
 beam('V3 clavicle structural bridge '+side,mi((4,0,358)),mi((59,0,351)),10,11)
 gear('V3 shoulder differential '+side,mi((54,0,351)),(s,0,0),10,9)
 piston('V3 clavicle support ram '+side,mi((25,15,318)),mi((53,9,348)),2)
 for z in [316,338,357]:
  beam('V3 thorax transverse rib '+side+str(z),mi((19,-14,z)),mi((27,17,z)),3,5,'steel')
  bolt('V3 rib fixing '+side+str(z),mi((21,-17,z)),(0,-1,0),.8)
 # Independently mounted vertical cylindrical power conditioning units.
 rod('V3 chest energy cylinder '+side,mi((15,2,320)),mi((15,2,348)),6,'navy',n=32)
 for z in [322,333,346]:ring('V3 energy retaining clamp '+side+str(z),mi((15,2,z)),mi((15,2,z+1.5)),6.5,5.7,'steel')
 hose('V3 chest power loom '+side,[mi(p) for p in [(15,-4,341),(22,-10,341),(24,-14,327),(15,-18,318),(12,-16,306)]],1.1)
 for j in range(4):box('V3 thoracic control stack '+side+str(j),mi((31,12,319+j*5)),(8,13,3),'navy',.18)
for z in [312,353]:beam('V3 chest cross tie '+str(z),(-25,14,z),(25,14,z),6,7)
gear('V3 sternum main coupling',(0,-7,335),(0,-1,0),8,7)
# Segmented vertebral core, two load columns and a pelvis cradle with real hip sockets.
PARENT=bpy.data.objects['PELVIS']
for z in [278,288,298,308]:
 box('V3 spine vertebra '+str(z),(0,9,z),(16,12,6),'frame',.7)
 gear('V3 vertebral locking wheel '+str(z),(0,-.5,z),(0,-1,0),3.7,2)
for s,side in [(-1,'L'),(1,'R')]:
 mi=lambda p:(s*p[0],p[1],p[2])
 piston('V3 spine front paired ram '+side,mi((10,-8,277)),mi((13,-7,310)),1.8)
 piston('V3 spine rear paired ram '+side,mi((12,15,275)),mi((16,13,312)),1.7)
 beam('V3 pelvic upper bridge '+side,(0,8,284),mi((28,2,275)),8,9)
 beam('V3 pelvic lower yoke '+side,(0,6,261),mi((29,2,266)),7,8)
 gear('V3 hip drive flange '+side,mi((24,0,265)),(s,0,0),11,10)
 plate('V3 pelvic open flank web '+side,[mi(p) for p in [(5,-8,282),(23,-10,279),(28,-10,270),(20,-10,265),(7,-9,263)]],(0,3,0),'steel',.3)
 hose('V3 pelvis coolant loom '+side,[mi(p) for p in [(8,12,298),(19,18,291),(30,17,279),(30,12,268)]],1.1)
# Four-sided limbs: articulated sideplates, dual rams, front sliding blocks and cable channels.
for s,side in [(-1,'L'),(1,'R')]:
 mi=lambda p:(s*p[0],p[1],p[2])
 PARENT=bpy.data.objects['THIGH_'+side]
 for dx in [-8,8]:
  beam('V3 femur forged rail '+side+str(dx),mi((24+dx,1,259)),mi((32+dx,1,210)),4,8)
  piston('V3 thigh hydraulic '+side+str(dx),mi((24+dx,-8,253)),mi((32+dx,-8,213)),1.8)
 for z,x in [(250,25),(235,29),(219,31)]:
  box('V3 femur sliding spacer '+side+str(z),mi((x,1,z)),(17,12,5),'navy',.3)
  bolt('V3 femur front lock '+side+str(z),mi((x,-6,z)),(0,-1,0),.85)
 hose('V3 femur loom '+side,[mi(p) for p in [(20,9,258),(23,11,244),(28,11,225),(33,8,213)]],1.1)
 PARENT=bpy.data.objects['SHIN_'+side]
 gear('V3 knee main toothed hub '+side,mi((32,1,205)),(s,0,0),11,24)
 for dx in [-10,10]:
  plate('V3 knee fork cheek '+side+str(dx),[mi(p) for p in [(32+dx,-9,216),(32+dx,9,216),(32+dx,15,203),(32+dx,7,188),(32+dx,-7,188),(32+dx,-12,202)]],(s*2,0,0),'steel',.28)
  beam('V3 tibia long structural rail '+side+str(dx),mi((34+dx,3,192)),mi((42+dx,0,48)),4,7)
  piston('V3 calf front ram '+side+str(dx),mi((35+dx,-8,187)),mi((42+dx,-10,71)),1.7)
 for j in range(8):
  z=174-j*15;x=35+(174-z)*.045
  box('V3 tibial bridge '+side+str(j),mi((x,3,z)),(23,12,4),'frame',.28)
  box('V3 tibial front sliding plate '+side+str(j),mi((x,-5,z)),(10,4,7),'navy',.2)
  for dx in [-7,7]:bolt('V3 tibia captive screw '+side+str(j)+str(dx),mi((x+dx,-5,z)),(0,-1,0),.65)
 hose('V3 leg flexible line '+side,[mi(p) for p in [(32,12,198),(40,17,173),(45,18,131),(46,13,86),(44,8,46)]],1.3)
 PARENT=bpy.data.objects['FOOT_'+side]
 gear('V3 ankle geared joint '+side,mi((41,-3,38)),(s,0,0),7,27)
 beam('V3 foot load keel '+side,mi((43,10,16)),mi((43,-37,11)),12,9)
 PARENT=bpy.data.objects['UPPER_ARM_'+side]
 for dx in [-6,6]:
  beam('V3 upper arm fork '+side+str(dx),mi((65+dx,0,343)),mi((70+dx,-2,309)),4,7)
  piston('V3 biceps actuator '+side+str(dx),mi((65+dx,-7,340)),mi((70+dx,-9,311)),1.3)
 gear('V3 elbow outer geared hub '+side,mi((70,-2,303)),(s,0,0),8.2,22)
 PARENT=bpy.data.objects['FOREARM_'+side]
 for dx in [-7,7]:
  beam('V3 forearm radius rail '+side+str(dx),mi((71+dx,-1,295)),mi((76+dx,-5,257)),3,6)
  piston('V3 wrist drive ram '+side+str(dx),mi((72+dx,-9,290)),mi((76+dx,-10,255)),1.25)
 for j in range(4):box('V3 forearm power block '+side+str(j),mi((73+j*.7,0,285-j*7)),(12,12,4),'navy',.2)
 hose('V3 arm hydraulic line '+side,[mi(p) for p in [(72,8,298),(78,12,285),(80,9,268),(77,3,254)]],.9)
# Match the established V2 proportions before posing.
for o in PARTS:
 mw=o.matrix_world.copy();inv=mw.inverted()
 for v in o.data.vertices:v.co=inv@deform(mw@v.co)
# Expose the new mechanics through intentional armour service openings.
PARENT=root
def cut_slot(target,c,w,d,h):
 ob=bpy.data.objects.get('FAZZ_'+target)
 if not ob:return
 cutter=box('temporary opening',c,(w,d,h),'black',0)
 for v in cutter.data.vertices:v.co=deform(v.co)
 md=ob.modifiers.new('Open frame service access','BOOLEAN');md.object=cutter;md.operation='DIFFERENCE';md.solver='EXACT';bpy.context.view_layer.objects.active=ob
 bpy.ops.object.modifier_move_to_index(modifier=md.name,index=0);bpy.ops.object.modifier_apply(modifier=md.name)
 bpy.data.objects.remove(cutter,do_unlink=True)
for s,side in [(-1,'L'),(1,'R')]:
 # Preserve long armour edges, open the central tibia access lane.
 for n in ['Calf front multi-plane armour ','Calf faceted heavy gaiter ','Calf lower long cover ']:cut_slot(n+side,(s*34,-21,121),12,28,53)
 # Remove the cover's circular decoration where the access window now sits.
 for pre in ['Calf large round service cap ','Calf service slotted screw _','Calf Anaheim logo ','Calf service text ']:
  for o in list(bpy.data.objects):
   if o.name.startswith('FAZZ_'+pre) and side in o.name:bpy.data.objects.remove(o,do_unlink=True)
# Pose with a wider grounded stance and flexed elbows. Transform world geometry once to avoid stale pivot transforms.
def ancestors(o):
 a=[]
 while o.parent:o=o.parent;a.append(o.name)
 return a
for o in list(asset.all_objects):
 if o.type!='MESH' or o in headset:continue
 names=ancestors(o);colnames=[c.name for c in o.users_collection];side='L' if any('_L' in n for n in names) else ('R' if any('_R' in n for n in names) else None)
 # Existing root-parented surface details identify their side by suffix.
 if side is None:
  if ' L' in o.name:side='L'
  elif ' R' in o.name:side='R'
 if side is None:continue
 s=-1 if side=='L' else 1
 leg=any(n.startswith(('THIGH_','SHIN_','FOOT_')) for n in names) or any(n.startswith(('06 ','07 ','08 ')) for n in colnames) or any(k in o.name for k in ['V2 calf','V2 ankle','V2 segmented toe','Calf '])
 arm=any(n.startswith(('UPPER_ARM_','FOREARM_','HAND_')) for n in names)
 mw=o.matrix_world.copy();inv=mw.inverted()
 if leg:
  for v in o.data.vertices:
   p=mw@v.co;blend=max(0,min(1,(2.35-p.z)/1.8));p.x+=s*.17*blend
   # Lower legs angle outward; knee has a slight forward loaded offset.
   p.y-=.09*max(0,1-abs(p.z-1.65)/.9)
   if any(n.startswith('FOOT_') for n in names):
    center=Vector((s*.43+s*.17,-.1,0));p=center+Matrix.Rotation(s*math.radians(-10),3,'Z')@(p-center)
   v.co=inv@p
 if arm:
  shoulder=deform(Vector((s*.65,0,3.51)));elbow=deform(Vector((s*.70,-.02,3.03)))
  upperrot=Matrix.Rotation(-s*math.radians(10),3,'Y');fore=any(n.startswith(('FOREARM_','HAND_')) for n in names)
  for v in o.data.vertices:
   p=mw@v.co
   if fore:p=elbow+Matrix.Rotation(math.radians(-16),3,'X')@(p-elbow)
   p=shoulder+upperrot@(p-shoulder);v.co=inv@p
 o.data.update()
assert hh==headhash()
sc=bpy.context.scene;sc.camera=bpy.data.objects['CAM_hero'];sc['iteration']='V3 internal load frame with exposed access lanes and spread stance; original head preserved'
bpy.ops.wm.save_as_mainfile(filepath=str(R/'FAZZ_V3_INNER_FRAME.blend'))
sc.render.resolution_x=1300;sc.render.resolution_y=1500;sc.cycles.samples=36
for n in ['hero','rear']:
 sc.camera=bpy.data.objects['CAM_'+n];sc.render.filepath=str(R/'renders'/('frame_v3_'+n+'.png'));bpy.ops.render.render(write_still=True)
# Actual naked-frame view: retain true frame members, old head and feet, hide removable armour/equipment.
hidden=[]
for o in asset.all_objects:
 if o.type!='MESH' or o in headset:continue
 keep=framecol in o.users_collection or any(c.name.startswith(('01 ','05 ','06 ','08 ')) for c in o.users_collection)
 if not keep:hidden.append((o,o.hide_render));o.hide_render=True
sc.camera=bpy.data.objects['CAM_hero'];sc.render.filepath=str(R/'renders'/'frame_v3_unarmoured.png');bpy.ops.render.render(write_still=True)
for o,h in hidden:o.hide_render=h
(W/'frame_v3_preservation.json').write_text(json.dumps({'old_head_geometry_identical':hh==headhash(),'old_head_meshes':len(hh),'new_frame_objects':len(framecol.objects)},indent=2))
