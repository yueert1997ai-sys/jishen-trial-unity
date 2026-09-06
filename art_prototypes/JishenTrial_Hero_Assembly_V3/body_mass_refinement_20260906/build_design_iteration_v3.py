"""Bounded third design iteration on the saved whole mech; native modeling only."""
import bpy,bmesh,pathlib,sys,json,hashlib,math
from mathutils import Vector,Matrix
W=pathlib.Path(__file__).resolve().parent;D=W/'iteration_v3';D.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(D/'SOURCE_BEFORE_V3.blend'))
sys.path.insert(0,str(W))
from mass_ops import *
changed=[]
def alter(o,fn):deform(o,fn);changed.append(o.name)
chest_prefixes=('V3B_Pectoral_','V3B_Thorax_','V3B_Sternum_','V3B_Mass_Thoracic_','V3B_Mass_Interlocking_','V3B_Chest_')
for o in list(SC.objects):
 if o.type=='MESH' and o.name.startswith(chest_prefixes):
  alter(o,lambda p:(p.x*1.035,-10+(p.y+10)*1.08,p.z))

def radial_group(side,a,b,prefixes,sx,sy):
 f=joint_frame(a,b,side);origin=Vector(f((0,0,0)))
 ux=Vector(f((1,0,0)))-origin;uy=Vector(f((0,1,0)))-origin;uz=Vector(f((0,0,1)))-origin
 for o in list(SC.objects):
  if o.type!='MESH' or not o.name.startswith(prefixes) or not ('.'+side in o.name):continue
  def move(p):
   v=Vector(p)-origin
   return origin+ux*(v.dot(ux)*sx)+uy*(v.dot(uy)*sy)+uz*v.dot(uz)
  alter(o,move)
for side in ('L','R'):
 radial_group(side,'Forearm','Hand',('V3B_Forearm_','V3B_Mass_Forearm_'),1.025,1.065)
 radial_group(side,'Shin','Foot',('V3B_Calf_Mass2_',),1.025,1.065)

removed=[o.name for o in SC.objects if o.type=='MESH' and o.name.startswith('V3B_Thigh_Mass2_')]
remove(['V3B_Thigh_Mass2_'])
for s,side in ((1,'L'),(-1,'R')):
 group('05 Thigh and knee armor','Thigh.'+side);f=joint_frame('Thigh','Shin',side)
 origin=Vector(f((0,0,0)));normal_front=(Vector(f((0,-1,0)))-origin).normalized();normal_outer=(Vector(f((1,0,0)))-origin).normalized()
 # Straight, load-bearing faceted section. Depth increases without a rounded white barrel.
 loft('Thigh_V3_Closed_Load_Chassis.'+side,[oct_ring(t,w,fy,by) for t,w,fy,by in
      ((5,9,-8,10),(16,15,-15,18),(28,18,-20,23),(53,17,-20,23),(67,11.5,-13,16),(75,9,-8,9))],'frame',.4,f)
 rows=[(19,10,-22.5),(29,13.4,-28),(50,11.8,-27.7),(59,8.2,-22.8)]
 def surface(x,t,offset=0):
  for i in range(len(rows)-1):
   if t<=rows[i+1][0]:break
  ta,wa,ya=rows[i];tb,wb,yb=rows[i+1];q=max(0,min(1,(t-ta)/(tb-ta)))
  width=wa+(wb-wa)*q;y=ya+(yb-ya)*q
  chamfer=max(0,(abs(x)/width-.30)/.70)*2.7
  return (x,y-1+chamfer+offset,t)
 def folded_front(name,offset,width_extra,mat):
  vs=[]
  for t,w,y in rows:
   for frac in (-1,-.30,.30,1):
    x=frac*(w+width_extra)
    vs.append(f((x,y+offset+(1.7 if abs(frac)==1 else -1),t)))
  fs=[(4*j+k,4*j+k+1,4*(j+1)+k+1,4*(j+1)+k) for j in range(3) for k in range(3)]
  skin(name,vs,fs,Vector(f((0,3.2,0)))-origin,mat,.24)
 folded_front('Thigh_V3_Longitudinal_Shield_Underframe.'+side,.85,1.2,'steel')
 folded_front('Thigh_V3_Long_Blue_Quadriceps_Armor.'+side,-.55,0,'blue')
 # An inset-looking small maintenance cap and a narrow machining line preserve a long main face.
 local_plate('Thigh_V3_Upper_Service_Hatch_Frame.'+side,[surface(x,t,-1.0) for x,t in ((-3.4,31),(3.4,31),(3.4,38),(-3.4,38))],f,(0,.8,0),'navy',.12)
 local_plate('Thigh_V3_Upper_Service_Hatch.'+side,[surface(x,t,-1.35) for x,t in ((-2.8,32),(2.8,32),(2.8,36.8),(-2.8,36.8))],f,(0,.65,0),'panel',.1)
 for ss in (-1,1):
  pts=[f(surface(ss*x,t,-.87)) for x,t in ((3.2,42),(3.2,48))]
  strip('Thigh_V3_Long_Recessed_Score_%d.'%ss+side,pts,.34,'navy',Vector(f((0,.38,0)))-origin)
  # Narrow ceramic rails have long straight chamfers and a tapered knee return.
  rail_rows=[(20,10.2,-23,16.4,-18.5),(29,13.6,-27,21,-17),(51,12.1,-26.7,19.3,-16),(64,8.3,-19.8,14.7,-11.5)]
  vs=[]
  for t,xi,yi,xo,yo in rail_rows:
   vs.extend((f((ss*xi,yi,t)),f((ss*xo,yo,t)),f((ss*(xo+.6),yo+6.5,t))))
  fs=[(3*j+k,3*j+k+1,3*(j+1)+k+1,3*(j+1)+k) for j in range(3) for k in (0,1)]
  skin('Thigh_V3_Ceramic_Long_Side_Rail_%d.'%ss+side,vs,fs,Vector(f((-ss*2.5,1.2,0)))-origin,'white',.27)
  # Separate deep side casings carry the thickness and visibly overlap the front rails.
  def casing_ring(t,x,fy,by):
   return [(ss*(x-3.3),fy,t),(ss*x,fy+2,t),(ss*(x+.4),by-2,t),(ss*(x-2.6),by,t)]
  loft('Thigh_V3_Deep_Side_Casing_%d.'%ss+side,[casing_ring(*v) for v in
       ((21,17.2,-9,15),(29,22,-10,22),(49,20.5,-10,22),(63,14.8,-5,14))],'navy',.28,f)
  if ss>0:
   local_plate('Thigh_V3_Outer_Blue_Long_Cartridge.'+side,[(22.7,-6,29),(22.7,12,29),(21.9,18,37),(20.9,16,49),(17.8,7,59),(18.4,-5,56),(21.1,-7,44)],f,(-2.5,0,0),'blue',.24)
   local_plate('Thigh_V3_Outer_Step_Armor.'+side,[(19.3,-5.6,53),(19.8,8,53),(16.4,12,62),(13.5,4,67),(14.4,-3,65)],f,(-2.0,0,0),'panel',.19)
   local_plate('Thigh_V3_Outer_Service_Recess.'+side,[(22.95,1,33),(22.65,10,33),(21.8,12,42),(22.1,2,42)],f,(-.7,0,0),'black',.11)
   for j in range(3):
    t=35+j*2.3;x=23.0-(t-35)*.11
    rod('Thigh_V3_Outer_Recess_Vane_%d.'%j+side,f((x,3,t)),f((x-.25,9.7,t)),.36,'titanium',n=8,bevel=.03)
   hose('Thigh_V3_Outer_Inset_Power_Conduit.'+side,[f(p) for p in ((22.8,16,31),(22.2,19,39),(20.1,18,48),(18.7,11,55))],.53,'gold')
  else:
   local_plate('Thigh_V3_Inner_Faceted_Cover.'+side,[(-22.2,-6,30),(-22.1,10,31),(-20,17,42),(-18.4,11,54),(-16.2,1,61),(-18.2,-6,51)],f,(2.3,0,0),'silver',.25)
   local_plate('Thigh_V3_Inner_Overlapping_Navy_Rail.'+side,[(-21.9,8,31),(-21.2,16,34),(-18.8,18,47),(-16.8,9,60),(-18.1,5,54),(-19.9,10,42)],f,(1.7,0,0),'navy',.2)
  # Purposeful attachment straps; pins are anchored on these straps instead of floating.
  for j,(t,xi,yi,xo,yo) in enumerate(((29.5,14.1,-26.7,19.5,-20),(53,11.7,-25.8,17.7,-18.0))):
   local_plate('Thigh_V3_Ceramic_Captive_Strap_%d_%d.'%(ss,j)+side,[(ss*xi,yi-.22,t),(ss*xo,yo-.22,t),(ss*(xo-.15),yo-.1,t+2),(ss*(xi-.15),yi-.1,t+2)],f,(0,.7,0),'titanium',.10)
   # Front-facing pins are seated near the inner end of each long strap.
   pos=f((ss*(xi+.5),yi-.5,t+.9))
   bolt('Thigh_V3_Captive_Pin_%d_%d.'%(ss,j)+side,pos,normal_front,.45,'steel')
  ram('Thigh_V3_Upper_Femoral_Actuator_%d.'%ss+side,f((ss*7.5,-11.5,4)),f((ss*7.5,-18.5,25)),1.7)
  ram('Thigh_V3_Rear_Damper_%d.'%ss+side,f((ss*9,26,16)),f((ss*8.2,24,64)),1.75)
 # A closed tapered lower transition remains behind the approved knee enclosure.
 local_plate('Thigh_V3_Lower_Articulated_Tongue.'+side,[(-7.7,-22,57),(7.7,-22,57),(8,-20,63),(5,-15,70),(-5,-15,70),(-8,-20,63)],f,(0,3.5,0),'navy',.24)
 local_plate('Thigh_V3_Knee_Travel_Index.'+side,[(-3.8,-23,59),(3.8,-23,59),(3.3,-22,61),(-3.3,-22,61)],f,(0,.9,0),'gold',.12)
 local_plate('Thigh_V3_Posterior_Blue_Spine.'+side,[(-5,24,20),(5,24,20),(7,25,42),(5,23,60),(0,21,69),(-5,23,60),(-7,25,42)],f,(0,-3,0),'blue',.27)
 for ss in (-1,1):
  local_plate('Thigh_V3_Rear_Ram_Protective_Edge_%d.'%ss+side,[(ss*13,21,22),(ss*16,20,28),(ss*15,21,48),(ss*11,21,61),(ss*10,18,61),(ss*12,19,46)],f,(-ss*1.8,0,0),'white',.2)

bpy.context.view_layer.update()
# Re-seat small existing fasteners on the revised visible armor surfaces.
from mathutils.bvhtree import BVHTree
fittings=[]
for side in ('L','R'):
 for key,target in [('V3B_Forearm_Fastener_58.','V3B_Mass_Forearm_Ceramic_Cuff.'),
                    ('V3B_Calf_Mass2_Service_Armor_Fastener_0.','V3B_Calf_Mass2_Upper_Ceramic_Armored_Cheek.'),
                    ('V3B_Calf_Mass2_Service_Armor_Fastener_1.','V3B_Calf_Mass2_Inner_Ceramic_Overlapping_Leaf.'),
                    ('V3B_Calf_Mass2_Service_Armor_Fastener_2.','V3B_Calf_Mass2_Ceramic_Mid_Coupling.'),
                    ('V3B_Calf_Mass2_Service_Armor_Fastener_3.','V3B_Calf_Mass2_Folded_Front_Spear_Armor.')]:
  stem=key+side;seat=bpy.data.objects[stem+'_Seat'];hexpart=bpy.data.objects[stem+'_Hex'];shell=bpy.data.objects[target+side]
  def center_world(o):return sum((o.matrix_world@v.co for v in o.data.vertices),Vector())/len(o.data.vertices)
  axis=(center_world(hexpart)-center_world(seat)).normalized();oldbase=center_world(seat)-axis*(.125*S)
  dg=bpy.context.evaluated_depsgraph_get();tree=BVHTree.FromObject(shell,dg)
  result=tree.find_nearest(shell.matrix_world.inverted()@oldbase)
  assert result[0] is not None,(stem,target)
  point=shell.matrix_world@result[0];normal=(shell.matrix_world.to_3x3().inverted().transposed()@result[1]).normalized()
  turn=axis.rotation_difference(normal).to_matrix();newbase=point+normal*(.015*S)
  for suffix in ('_Seat','_Hex','_Socket'):
   part=bpy.data.objects[stem+suffix];mw=part.matrix_world.copy();inv=mw.inverted()
   for v in part.data.vertices:v.co=inv@(newbase+turn@(mw@v.co-oldbase))
   part.data.update()
  fittings.append(stem)
bpy.context.view_layer.update()
new=[o for o in SC.objects if o.type=='MESH' and o.name.startswith('V3B_Thigh_V3_')]
missing=[o for o in new if not o.data.uv_layers]
if missing:
 bpy.ops.object.select_all(action='DESELECT')
 for o in missing:o.select_set(True)
 bpy.context.view_layer.objects.active=missing[0];bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT')
 bpy.ops.uv.smart_project(angle_limit=math.radians(66),island_margin=.003,area_weight=.3,correct_aspect=True,scale_to_bounds=True)
 bpy.ops.object.mode_set(mode='OBJECT')
 for o in missing:o.data.uv_layers.active.name='UV0'

def h(o):return hashlib.sha256(repr(([tuple(v.co) for v in o.data.vertices],[tuple(p.vertices) for p in o.data.polygons],[[tuple(l.uv) for l in uv.data] for uv in o.data.uv_layers])).encode()).hexdigest()
baseline=json.loads((D/'source_proof.json').read_text());preserved_errors=[]
for name,p in baseline['objects'].items():
 if name in removed or name in changed:continue
 o=bpy.data.objects[name]
 actual={'type':o.type,'hash':h(o) if o.type=='MESH' else None,'matrix':[list(r) for r in o.matrix_world],'parent':o.parent.name if o.parent else None,'materials':[m.name if m else None for m in o.data.materials] if o.type=='MESH' else []}
 if actual!=p:preserved_errors.append(name)
assert not preserved_errors,preserved_errors
def issues(me):
 bm=bmesh.new();bm.from_mesh(me);v=[sum(not e.is_manifold for e in bm.edges),sum(f.calc_area()<1e-13 for f in bm.faces)];bm.free();return v
geometry_errors=[];bevel_repairs=[]
for o in new+[bpy.data.objects[n] for n in changed]:
 dg=bpy.context.evaluated_depsgraph_get();ev=o.evaluated_get(dg);me=ev.to_mesh();bad=issues(me);ev.to_mesh_clear()
 if not any(bad):continue
 if any(issues(o.data)):geometry_errors.append([o.name,bad]);continue
 for mod in o.modifiers:
  if mod.type=='BEVEL':mod.show_render=False;mod.show_viewport=False
 bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get();ev=o.evaluated_get(dg);me=ev.to_mesh();after=issues(me);ev.to_mesh_clear()
 if any(after):geometry_errors.append([o.name,after])
 else:bevel_repairs.append(o.name)
assert not geometry_errors,geometry_errors
SC['design_iteration']='V3';SC['mass_revision']=11
SC['mass_revision_notes']='V3: long angular blue thigh armor with narrow ceramic side rails, deeper overlapping side cases, enclosed knee joins. Chest depth +8%, forearm and calf depth +6.5%; approved head, RAIKEN weapon and joint transforms retained.'
SC['export_status']='Native modeling review only; user requested no game/FBX/GLB export.'
SC.camera=bpy.data.objects['RAIKEN_ASSEMBLED_3Q']
bpy.ops.object.select_all(action='DESELECT');bpy.context.view_layer.objects.active=None
SC['current_delivery']=str(D/'VALKYR_ITERATION_V3_MASTER.blend')
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(D/'VALKYR_ITERATION_V3_MASTER.blend'))
report={'source_sha256':baseline['source_sha256'],'design_iteration':'V3','removed_previous_thigh_meshes':len(removed),'new_thigh_meshes':len(new),'modified_existing_body_meshes':len(changed),'changed_objects':changed,'removed_objects':removed,'preserved_errors':preserved_errors,'geometry_issues':geometry_errors,'bevel_repairs':bevel_repairs,'reseated_fittings':fittings,'missing_uv_count':sum(not o.data.uv_layers for o in new),'exports_run':False}
(D/'build_audit.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps({k:v for k,v in report.items() if k not in ('changed_objects','removed_objects')},indent=2),flush=True)
