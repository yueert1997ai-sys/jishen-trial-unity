import bpy,bmesh,pathlib,math,json,hashlib
from mathutils import Vector,Matrix
from mathutils.geometry import tessellate_polygon
W=pathlib.Path(__file__).resolve().parent;R=W.parent;S=.01;PARTS=[]
M={m.name.replace('FAZZ ',''):m for m in bpy.data.materials if m.name.startswith('FAZZ ')}
asset=bpy.data.collections['FAZZ_COMPLETE_ASSEMBLY'];root=bpy.data.objects['FAZZ_MASTER_ROOT'];PARENT=root
COL=bpy.data.collections.new('16 V2 structural refinement');asset.children.link(COL)
exec((W/'v3_geometry.py').read_text())
head=bpy.data.collections['FAZZ_HEAD_ASSET'];heads=set(head.all_objects)
def hashes():
 return sorted(hashlib.sha256(repr(([tuple(v.co) for v in o.data.vertices],[tuple(p.vertices) for p in o.data.polygons])).encode()).hexdigest() for o in heads if o.type=='MESH')
before=hashes()
# Fill the neck surround with folded, load-bearing collar walls and angled shoulder returns.
for s,side in [(-1,'L'),(1,'R')]:
 def mi(p):return (s*p[0],p[1],p[2])
 skin('V2 collar folded cheek '+side,[mi(p) for p in [(10,-13,362),(17,-22,370),(35,-19,365),(40,1,362),(33,14,359),(13,8,358)]],[(0,1,2),(0,2,3,4,5)],(0,2,-2),'grey',.25)
 plate('V2 collar inner rim '+side,[mi(p) for p in [(11,-13,364),(17,-22,372),(33,-20,368),(33,-17,366),(18,-18,369),(13,-10,362)]],(0,1,-1),'panel',.12)
 piston('V2 neck side actuator '+side,mi((10,3,355)),mi((11,2,373)),1.15)
 # Sloping shoulder joint covers visually join shoulder and chest.
 skin('V2 shoulder bridge '+side,[mi(p) for p in [(34,-16,366),(55,-13,369),(61,5,359),(43,16,351),(33,5,350)]],[(0,1,2,3,4)],(0,2,-2),'navy',.32)
 for j in range(4):
  strip('V2 shoulder bridge louver '+side+str(j),[mi((38+j*4,-16,365)),mi((40+j*4,-8,363))],1.4,'steel',(0,.7,-.5))
 # Upper chest has a thick angular return and a ribbed equipment layer behind its edge.
 skin('V2 chest upper return '+side,[mi(p) for p in [(14,-43,357),(46,-35,357),(48,-26,362),(18,-32,365)]],[(0,1,2,3)],(0,1,-2),'white',.22)
 plate('V2 chest underlayer '+side,[mi(p) for p in [(44,-30,351),(50,-23,350),(47,-12,320),(42,-19,318)]],(0,2,0),'navy',.2)
 for j in range(5):
  plate('V2 chest edge cooling rib '+side+str(j),[mi(p) for p in [(46,-28+j*1.7,346-j*5),(50,-23+j*1.7,346-j*5),(50,-23+j*1.7,344-j*5),(46,-28+j*1.7,344-j*5)]],(0,1,0),'steel',.09)
 # Ribbed flexible abdomen, concealed under overlapping front armour.
 for j in range(5):
  plate('V2 abdomen flexible belt '+side+str(j),[mi(p) for p in [(8,-21,309-j*4),(20,-18,307-j*4),(24,-5,307-j*4),(22,8,307-j*4),(10,12,309-j*4)]],(0,0,-1.7),'rubber',.14)
 piston('V2 abdominal front ram '+side,mi((17,-17,282)),mi((23,-14,306)),1.15)
 # Shoulder outer face is a layered shell with a real inset equipment bank.
 skin('V2 shoulder outer cassette '+side,[mi(p) for p in [(87,-10,367),(92,-3,365),(92,15,349),(86,22,338),(83,10,335),(85,-7,344)]],[(0,1,2,3,4,5)],(-s*2,0,0),'panel',.22)
 for j in range(5):
  plate('V2 shoulder side dark port '+side+str(j),[mi(p) for p in [(92.1,-1,359-j*3),(92.1,11,357-j*3),(92.1,11,355.7-j*3),(92.1,-1,357.7-j*3)]],(-s*.5,0,0),'black',.06)
 # Calf side follows the existing multi-plane shell with a separated service spine.
 plate('V2 calf flank dark channel '+side,[mi(p) for p in [(60,3,171),(66.4,12,102),(60,12,75),(57,4,92),(60,-1,155)]],(-s*1,0,0),'navy',.2)
 for j in range(7):
  z=148-j*7;x=62+(148-z)*.04
  plate('V2 calf side cooling blade '+side+str(j),[mi((x,4,z)),mi((x+2.7,13,z-1)),mi((x+2.7,13,z-2.5)),mi((x,4,z-1.5))],(-s*.8,0,0),'steel',.08)
 # Feet gain separate toe armour and an exposed ankle linkage.
 x=s*43
 for j in range(2):
  plate('V2 segmented toe cap '+side+str(j),[(x-12,-46+j*10,14+j*3),(x+12,-46+j*10,14+j*3),(x+12,-38+j*10,16+j*3),(x-12,-38+j*10,16+j*3)],(0,0,-1),'blue',.13)
 piston('V2 ankle side linkage '+side,mi((55,3,39)),mi((56,-17,21)),1.3)
 # Backpack: ribbed central spine and substantial independent radiator cassettes.
 plate('V2 pack rear armour cheek '+side,[mi(p) for p in [(7,72,365),(20,76,358),(19,77,314),(8,73,309)]],(0,-3,0),'navy',.25)
 for j in range(9):
  box('V2 rear radiator vane '+side+str(j),mi((12,77,353-j*4)),(10,3,1.4),'steel',.1)
 for z in [354,312]:bolt('V2 pack rear captive lock '+side+str(z),mi((19,78,z)),(0,1,0),.7,'titanium')
 for z in [322,302]:
  piston('V2 pack nozzle gimbal '+side+str(z),mi((29,61,z+7)),mi((29,77,z-1)),1.25)
 # Additional clamps and service seam on the long propellant cylinders.
 for z in [246,181,121]:
  x=49+(293-z)*5/195;y=62+(293-z)*11/195
  box('V2 tank external latch '+side+str(z),mi((x,y+9,z)),(5,3,5),'steel',.16)
  bolt('V2 tank latch bolt '+side+str(z),mi((x,y+11,z)),(0,1,0),.65)
# Cannon upper face: stepped panels in its own sloped surface basis.
origin=Vector((-83,35,411));axis=Vector((0,-.72,-.694)).normalized();ux=Vector((1,0,0));uy=axis.cross(ux).normalized()
def gun(x,y,t):return tuple(origin+ux*x+uy*y+axis*t)
for j,(t0,t1,w,h) in enumerate([(7,33,13,-22),(37,65,15,-23),(69,88,13,-22)]):
 plate('V2 cannon dorsal segmented armour '+str(j),[gun(-w,h,t0),gun(w,h,t0),gun(w,h,t1-4),gun(w-3,h,t1),gun(-w,h,t1)],tuple(uy*1.4),'navy',.2)
 for x in [-w+2,w-2]:bolt('V2 cannon dorsal lock '+str(j)+str(x),gun(x,h-.3,t0+4),tuple(-uy),.65)
for j in range(10):
 t=138+j*11
 plate('V2 cannon barrel heat fin '+str(j),[gun(-6,-14,t),gun(6,-14,t),gun(6,-14,t+1.2),gun(-6,-14,t+1.2)],tuple(uy*2),'steel',.08)
strip('V2 cannon dorsal warning rail',[gun(-10,-23,14),gun(-10,-23,58)],1.8,'gold',tuple(uy*.3))
# Proportion correction in world space, preserving all head mesh data verbatim.
def deform(p):
 p=p.copy();z=p.z
 p.z = z if z<=.45 else (.45+(z-.45)*.8 if z<2.05 else z-.32)
 blend=max(0,min(1,(z-2.3)/.7));p.x*=1+.10*blend
 return p
for o in list(asset.all_objects):
 if o.type!='MESH' or o in heads:continue
 mw=o.matrix_world.copy();inv=mw.inverted()
 for v in o.data.vertices:v.co=inv@deform(mw@v.co)
 o.data.update()
hm=bpy.data.objects['HEAD_MOUNT'];mw=hm.matrix_world.copy();mw.translation.z-=.37;hm.matrix_world=mw
assert before==hashes(),'Old head changed'
# Hide the narrow gap at the neck base with the existing gimbal, rather than stretching the head.
bpy.context.view_layer.update()
sc=bpy.context.scene
for n in ['hero','front','rear']:
 c=bpy.data.objects['CAM_'+n];c.data.ortho_scale-=.3
 aim=Vector((0,-.25 if n!='rear' else .15,2.02));c.rotation_euler=(aim-c.location).to_track_quat('-Z','Y').to_euler()
c=bpy.data.objects['CAM_head'];c.location.z-=.37
sc.camera=bpy.data.objects['CAM_hero'];sc['iteration']='V2: shortened calves, shoulder/chest mass, collar, abdomen, pack cooling and cannon panels; old head untouched'
out=R/'FAZZ_V2_REFINED.blend';bpy.ops.wm.save_as_mainfile(filepath=str(out))
(W/'v2_head_preservation.json').write_text(json.dumps({'old_head_meshes':len(before),'identical_mesh_hashes':before==hashes(),'added_objects':len(PARTS)},indent=2))
sc.render.resolution_x=1200;sc.render.resolution_y=1400;sc.cycles.samples=32
for n in ['hero','rear']:
 sc.camera=bpy.data.objects['CAM_'+n];sc.render.filepath=str(R/'renders'/('v2_'+n+'.png'));bpy.ops.render.render(write_still=True)
