import bpy, pathlib, sys, math, json, hashlib
W=pathlib.Path(__file__).resolve().parent
sys.path.insert(0,str(W))
bpy.ops.wm.open_mainfile(filepath=str(W/'SOURCE_BEFORE_MASS_PASS.blend'))
from mass_ops import *

def clamp(x):return max(0,min(1,x))
# Pull the lower breastplate back into the rib cage while retaining its upper crest.
for o in list(SC.objects):
 if o.type!='MESH' or not o.name.startswith('V3B_'):continue
 if o.name.startswith(('V3B_Pectoral_','V3B_Sternum_','V3B_Ceramic_Oblique_Rib_','V3B_Sloping_Clavicle_','V3B_Clavicle_Trim','V3B_Clavicle_Armor_Pin','V3B_Clavicle_Lateral_')):
  def chest(p):
   x,y,z=p; y+=max(0,329-z)*.53*clamp(-y/20)
   return (x*1.055,y,z)
  deform(o,chest)
group('01 Chest and layered thorax','Thorax')
remove(['V3B_Thoracic_Upper_Bulkhead'])
loft('Mass_Thoracic_Continuous_Load_Cage',[
 oct_ring(294,18,-19,18),oct_ring(304,29,-27,23),oct_ring(319,38.5,-34,27),
 oct_ring(333,38,-29,26),oct_ring(343,23,-9,21)],'frame',.65)

def radial_scale(o,a,b,s,wx,wy,weight):
 a=Vector(a);d=(Vector(b)-a).normalized();u=Vector((s,0,0));u=(u-d*u.dot(d)).normalized();v=d.cross(u)
 if v.y<0:v=-v
 def fn(p):
  q=p-a;t=q.dot(d);w=weight(t)
  return a+d*t+u*q.dot(u)*(1+(wx-1)*w)+v*q.dot(v)*(1+(wy-1)*w)
 deform(o,fn)

for s,side in ((1,'L'),(-1,'R')):
 # Main armor and its fitted seams move together. Joint drums remain circular.
 for part,end,wx,wy in (('Forearm','Hand',1.20,1.23),('UpperArm','Forearm',1.13,1.14),('Shin','Foot',1.30,1.36)):
  a=Vector(bpy.data.objects[part+'.'+side].matrix_world.translation)/S
  b=Vector(bpy.data.objects[end+'.'+side].matrix_world.translation)/S;ln=(b-a).length
  if part=='Shin':weight=lambda t:clamp((t-10)/55)*clamp((133-t)/17)
  else:weight=lambda t,ln=ln:clamp(t/13)*clamp((ln-t)/12)
  for o in list(SC.objects):
   if o.type!='MESH' or o.parent!=bpy.data.objects[part+'.'+side] or not o.name.startswith('V3B_'):continue
   if any(k in o.name for k in ('_Rod','_Sleeve','_Seal','_Clevis','_Race','_Drum','_Cap','Bearing','Piston','Tendon')):continue
   radial_scale(o,a,b,s,wx,wy,weight)
 group('04 Arms and articulated hands','Forearm.'+side);f=joint_frame('Forearm','Hand',side)
 loft('Mass_Forearm_Closed_Torsion_Chassis.'+side,[oct_ring(8,8,-9,9),oct_ring(20,13.5,-20,15),oct_ring(40,13,-20,15),oct_ring(55,8,-10,10)],'frame',.55,f)
 group('06 Tapered shin and calf','Shin.'+side);f=joint_frame('Shin','Foot',side)
 loft('Mass_Shin_Closed_Load_Cassette.'+side,[oct_ring(16,9,-8,13),oct_ring(47,13,-12,18),oct_ring(72,12.8,-14,19),oct_ring(100,11.3,-13.5,17),oct_ring(119,8,-9,12)],'frame',.5,f)
 for ss in (-1,1):
  local_plate('Mass_Shin_Longitudinal_Armor_Rail_%d.'%ss+side,[(ss*11,-1,65),(ss*13,7,69),(ss*11.8,16,80),(ss*10.5,13,103),(ss*7,5,118),(ss*8,-3,107)],f,(-ss*2,0,0),'navy',.4)
 # Shorten the long, flat toe silhouette and raise the load-bearing instep.
 for o in list(SC.objects):
  if o.type!='MESH' or o.parent!=bpy.data.objects['Foot.'+side] or not o.name.startswith('V3B_'):continue
  if 'Gimbal' in o.name:continue
  def foot_mass(p):
   x,y,z=p;w=clamp((43-z)/20)
   return (s*93+(x-s*93)*(1+.13*w),2+(y-2)*(1-.115*w),z*(1+.24*clamp((40-z)/40)))
  deform(o,foot_mass)

# Verify the complete approved head, including UVs and world transforms, before saving.
proof=json.loads((W/'source_inventory.json').read_text())['head_proof']
for item in proof:
 o=bpy.data.objects[item['name']]
 h=hashlib.sha256(repr(([tuple(v.co) for v in o.data.vertices],[tuple(f.vertices) for f in o.data.polygons],[[tuple(l.uv) for l in uv.data] for uv in o.data.uv_layers])).encode()).hexdigest()
 assert h==item['mesh_hash'],o.name
 assert [list(row) for row in o.matrix_world]==item['matrix_world'],o.name
save(1)
