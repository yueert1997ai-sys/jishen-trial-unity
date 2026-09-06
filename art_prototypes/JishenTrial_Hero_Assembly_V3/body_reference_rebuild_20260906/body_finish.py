from body_common import *
from body_limbs import limb_basis
# Assembly fitting only: approved head control meshes/materials remain byte-identical.
headroot=bpy.data.objects['HEAD_V3_ROOT'];datum=Vector((0,0,2.974));headroot.matrix_world=Matrix.Translation(datum)@Matrix.Scale(1.35,4)@Matrix.Translation(-datum)
headroot['body_fit_uniform_scale']=1.35;headroot['body_fit_note']='Approved head geometry unchanged; uniform assembly scale around original neck datum to match full-body drawing proportions'
# Free space behind the larger fitted helmet, keeping the backpack deployment hierarchy.
pack=bpy.data.objects['Backpack_Structural_Mount'];m=pack.matrix_world.copy();m.translation.y+=6*S;pack.matrix_world=m;bpy.context.view_layer.update()
cannon=bpy.data.objects['Cannon_Right_Cradle_Mount'];m=cannon.matrix_world.copy();m.translation.x-=5*S;cannon.matrix_world=m;bpy.context.view_layer.update()

def deform(o,fn):
 mw=o.matrix_world.copy();inv=mw.inverted()
 for v in o.data.vertices:v.co=inv@U(fn((mw@v.co)/S))
 bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free();o.data.update()

# Superseded legacy nozzle mounts and bolt locations belonged to the former armor.
removed=[]
for o in list(SC.objects):
 if o.get('V3B_retained_existing_component'):
  if o.name.startswith(('Calf_Vector_','Calf_Armor_Catch_','Thigh_Service_Latch_','Shin_Exposed_Transmission')):
   removed.append(o.name);bpy.data.objects.remove(o,do_unlink=True);continue
  if o.type=='MESH':
   mat='frame'
   if any(t in o.name for t in ('PistonRod','ClevisPin','Axle_Cap','Bearing_Race','DarkSocket','SealCollar')):mat='steel' if 'DarkSocket' not in o.name else 'black'
   if ('Knee_Servo' in o.name or 'Ankle_Rotator' in o.name) and 'Bearing_Race' in o.name:mat='gold'
   o.data.materials.clear();o.data.materials.append(M[mat])
  if o.name.startswith('Foot_Heel_Load_Block'):
   s=1 if o.name.endswith('.L') else -1;c=Vector((s*93,2,20))
   deform(o,lambda p:c+Vector(((p.x-c.x)*.65,(p.y-c.y)*.72,(p.z-c.z)*.9)))
  if o.name.startswith(('Calf_Rear_Actuator','Calf_Actuator_Sleeve','Calf_Long_Damper')):
   deform(o,lambda p:Vector((p.x,3+(p.y-3)*.73,p.z)))

# Tilt the badge panel forward as it descends. The front contour remains matched.
for o in list(SC.objects):
 if o.type!='MESH':continue
 if o.name.startswith(('V3B_Shoulder_Badge_','V3B_Shoulder_Unit_','V3B_Shoulder_Valkyr_','V3B_Shoulder_Corner_')):
  deform(o,lambda p:Vector((p.x,p.y+(p.z-329)*.55,p.z)))
 if o.name.startswith(('V3B_Blue_Iliac_Belt.','V3B_Iliac_Upper_Overlay.')):
  n=len(o.data.vertices)//2;t=3 if 'Blue_Iliac' in o.name else 1.2
  for v in o.data.vertices:
   p=v.co/S;p.y=-27+abs(p.x)*.25+(t if v.index>=n else 0);v.co=U(p)
  bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free()

group('01 Chest and abdomen','Thorax')
for s,side in ((1,'L'),(-1,'R')):
 mi=lambda p:mirror(p,s)
 # Close the neck-to-clavicle transition with dark seals and small gold couplings.
 plate('Neck_Collar_Lateral_Blue_Armor.'+side,mi([(8,-4,349),(13,-3,348),(17,5,344),(12,9,341),(8,5,343)]),(0,2,-1),'blue',.22)
 rod('Neck_Shoulder_Tension_Pin.'+side,(s*13,-1,344),(s*13,-1,348),1.25,'gold',n=20)
 plate('Clavicle_Lateral_White_Return.'+side,mi([(28,-13,340),(42,-5,335),(45,3,331),(40,10,329),(36,0,334)]),(-s*1,1,-1),'silver',.23)
 # Thin armor reveal at the edges of the chest cartridge creates a layered machined finish.
 strip('Pectoral_Outer_Blue_Edge.'+side,mi([(41,-21.4,333),(43,-21.4,316),(40,-22.4,304)]),.65,'panel')
 strip('Pectoral_Lower_Silver_Catch.'+side,mi([(24,-23.1,298),(34,-23.1,302)]),.45,'titanium')
 for x,z in ((39,329),(25,299)):
  bolt('Chest_Cartridge_Captive_Pin_%s.'%x+side,(s*x,-22.4,z),r=.5)
group('02 Waist and pelvis','Pelvis')
for s,side in ((1,'L'),(-1,'R')):
 mi=lambda p:mirror(p,s)
 plate('Hip_Belt_Outer_Closed_Return.'+side,mi([(32,-15,272),(43,-9,270),(47,2,258),(38,12,252),(31,8,255),(38,-9,260)]),(-s*2,1,0),'navy',.25)
 plate('Hip_Joint_White_Seat.'+side,mi([(30,-7,254),(36,-6,252),(34,-7,241),(28,-7,240)]),(0,3,0),'white',.24)
 bolt('Hip_Belt_Silver_Retainer.'+side,(s*40,-12,265),r=.75)

for s,side in ((1,'L'),(-1,'R')):
 group('03 Swept shoulder armor','Shoulder_Armor_Floating_Pivot.'+side);mi=lambda p:mirror(p,s)
 # Continuous wrapped side surfaces beneath the separately hinged front badge.
 v=[(61,-8,361),(88,0,375),(91,18,364),(72,23,347),(53,16,347),(57,2,353)]
 skin('Shoulder_Upper_Closed_Return.'+side,mi(v),[(0,1,2,5),(2,3,4,5)],(-s*1,-1,-1),'navy',.25)
 plate('Shoulder_Side_Ceramic_Interlayer.'+side,mi([(76,13,350),(87,15,361),(89,18,352),(79,20,339)]),(-s*2,0,0),'silver',.2)
 plate('Shoulder_Side_Lower_Blue_Leaf.'+side,mi([(82,13,339),(94,14,347),(91,20,325),(82,17,315),(77,13,324)]),(-s*2,1,0),'blue',.2)
 strip('Shoulder_Rear_Gold_Lock_Line.'+side,mi([(52,18.4,340),(69,21.4,348)]),.8,'gold',(0,-.6,0))
 bearing('Shoulder_Return_Pivot.'+side,(s*82,12,337),(0,1,0),2.2,3)

for s,side in ((1,'L'),(-1,'R')):
 group('04 Arms and articulated hands','Forearm.'+side)
 f=limb_basis((s*87,0,281),(s*112,-2,226),s)
 def lp(name,pts,dep=(0,2,0),mat='blue',bev=.18):return plate(name,[f(p) for p in pts],dep,mat,bev)
 # Broad blue outer-side cartridge and a wrapped aft edge complete the gauntlet volume.
 vv=[(8,-7,19),(12,-2,20),(13,8,21),(10,12,23),(9,11,48),(6,6,55),(8,-5,52)]
 skin('Forearm_Closed_Outer_Housing.'+side,[f(p) for p in vv],[(0,1,6),(1,2,5,6),(2,3,4,5)],(-s*2,0,0),'blue',.24)
 lp('Forearm_Outer_Overlapping_Panel.'+side,[(12,-1,22),(13.2,7,24),(12.5,9,39),(10.8,4,45),(11,-1,35)],(-s*1.2,0,0),'panel',.15)
 lp('Forearm_Machined_Side_Inset.'+side,[(12.7,2,28),(13.2,6,29),(12.3,7,39),(11.7,3,38)],(-s*.7,0,0),'navy',.12)
 lp('Forearm_Inner_White_Guide.'+side,[(-10,-6,25),(-11,2,27),(-10,6,44),(-8,-3,48)],(s*1.5,0,0),'white',.2)
 for t in (25,42):bolt('Forearm_Outer_Panel_Pin_%s.'%t+side,f((13,2,t)),(s,0,0),.48)
 lp('Forearm_Lower_Gold_Key.'+side,[(6,-10,48),(6.6,-9,48),(6.6,-9,52),(6,-10,52)],(0,.5,0),'gold',.05)

 group('05 Thigh and knee armor','Thigh.'+side)
 f=limb_basis((s*31,1,242),(s*58,-1,167),s)
 vv=[(8,-5,26),(12,2,29),(13,12,34),(8,16,36),(5,14,64),(9,8,71),(12,0,62)]
 skin('Thigh_Outer_Closed_Ceramic_Volume.'+side,[f(p) for p in vv],[(0,1,6),(1,2,3,6),(3,4,5,6)],(-s*2,0,0),'white',.27)
 lp('Thigh_Outer_Upper_Silver_Layer.'+side,[(12,1,29),(14,8,32),(13,11,42),(12,4,45)],(-s*1.2,0,0),'silver',.19)
 lp('Thigh_Outer_Lower_Ceramic_Layer.'+side,[(12,3,46),(12.5,11,43),(10,13,61),(8,8,67),(10,1,62)],(-s*1.4,0,0),'white',.24)
 lp('Thigh_Rear_Upper_Blue_Seat.'+side,[(-7,13,18),(6,13,18),(8,15,29),(-7,14,29)],(0,-2,0),'blue',.22)
 for t in (32,56):bolt('Thigh_Outer_Ceramic_Pin_%s.'%t+side,f((13.4,6,t)),(s,0,0),.5)

 group('06 Tapered shin and calf','Shin.'+side)
 f=limb_basis((s*58,-1,167),(s*93,2,40),s)
 # Calf armor wraps fully into the rear, with a broad upper section and a slender ankle.
 vv=[(9,2,18),(15,10,21),(17,20,37),(14,24,56),(9,19,72),(7,10,79),(7,2,63),(12,10,44)]
 skin('Calf_Closed_Rear_Outer_Carapace.'+side,[f(p) for p in vv],[(0,1,7),(1,2,3,7),(3,4,5,6,7),(0,7,6)],(-s*2,-1,0),'navy',.26)
 lp('Calf_Rear_Upper_Blue_Leaf.'+side,[(14,10,23),(17,19,33),(15,24,45),(12,20,51),(11,12,42)],(-s*1,-1,0),'blue',.22)
 lp('Calf_Rear_Middle_Blue_Leaf.'+side,[(12,21,48),(15,25,48),(13,23,62),(8,15,73),(8,10,62)],(-s*1,-1,0),'blue',.22)
 lp('Calf_Rear_White_Highlight.'+side,[(15,19,34),(16,22,37),(14,24,46),(12,21,45)],(-s*.7,-.5,0),'white',.14)
 lp('Shin_Inner_Blue_Wrapped_Rail.'+side,[(-8,-3,73),(-10,4,74),(-9,8,95),(-6,9,114),(-4,3,121),(-5,-5,108)],(s*2,0,0),'blue',.23)
 lp('Shin_Outer_Blue_Wrapped_Rail.'+side,[(7,-3,77),(10,5,78),(10,9,99),(7,11,114),(3,3,120),(5,-5,108)],(-s*2,0,0),'blue',.23)
 lp('Ankle_Outer_Blue_Shingle.'+side,[(8,6,102),(11,12,106),(11,13,117),(6,7,126),(4,0,123)],(-s*2,0,0),'panel',.2)
 lp('Ankle_Rear_White_Slider.'+side,[(-4,10,111),(4,10,111),(5,11,120),(2,12,128),(-3,12,128)],(0,-2,0),'white',.2)
 for j in range(3):
  t=53+j*6;lp('Calf_Side_Heat_Vent_%s.'%j+side,[(15,17,t),(15.8,20,t),(15.2,20,t+1.2),(14.6,17,t+1.2)],(-s*.6,0,0),'black',.04)
 for t in (31,57,109):bolt('Calf_Return_Captive_Pin_%s.'%t+side,f((15 if t<60 else 9,16 if t<60 else 7,t)),(s,0,0),.55,'gold' if t==109 else 'titanium')

 group('07 Feet and ankle couplings','Foot.'+side)
 q=Matrix.Rotation(math.radians(19*s),3,'Z');c=Vector((s*93,2,0))
 def foot(p):return tuple(c+q@Vector((s*p[0],p[1],p[2])))
 for ss in (-1,1):
  for prefix in ('V3B_Foot_White_Split_Toe_','V3B_Toe_Machined_Silver_Insert_'):
   name=prefix+str(ss)+'.'+side
   if name in bpy.data.objects:bpy.data.objects.remove(bpy.data.objects[name],do_unlink=True)
  # Volumetric split toe caps replace the first-pass thin white edge strips.
  pp=[(ss*.8,-38,1.9),(ss*7.9,-37,1.9),(ss*8.8,-34,5.2),(ss*6.8,-30.7,12),(ss*.8,-31.7,11),(ss*.8,-37.7,6.0),
      (ss*.8,-32,3),(ss*6.8,-31,3)]
  hull('Foot_White_Faceted_Toe_Cap_%s.'%ss+side,[foot(p) for p in pp],'white',.21)
  plate('Foot_Toe_Silver_Face_%s.'%ss+side,[foot(p) for p in [(ss*2,-38.2,2.4),(ss*6.4,-37.4,2.4),(ss*6.4,-36.2,6.9),(ss*2,-37.4,6.9)]],(0,.6,0),'titanium',.11)
  # Tiered blue heel wedges conceal the old block while preserving its load-bearing core.
  hull('Foot_Blue_Heel_Side_Volume_%s.'%ss+side,[foot(p) for p in [(ss*6,-5,7),(ss*12,-4,7),(ss*13,12,4),(ss*10,18,4),(ss*7,14,23),(ss*8,4,29),(ss*6,-4,22)]],'blue',.28)
  plate('Foot_Heel_Oblique_Overlap_%s.'%ss+side,[foot(p) for p in [(ss*10,-2,20),(ss*12,6,22),(ss*13,14,9),(ss*12,8,5),(ss*10,-3,9)]],(-s*ss*1,0,0),'panel',.18)
  plate('Heel_Silver_Terminal_%s.'%ss+side,[foot(p) for p in [(ss*7,17,17),(ss*10,18,14),(ss*11,22,3),(ss*7,22,3)]],(-s*ss*1,0,0),'titanium',.18)
  bolt('Heel_Layer_Captive_Pin_%s.'%ss+side,foot((ss*12,8,15)),(s*ss,0,0),.75,'gold')
  plate('Instep_Side_White_Key_%s.'%ss+side,[foot(p) for p in [(ss*8,-21,17),(ss*10,-22,13),(ss*10,-26,9),(ss*8,-24,14)]],(-s*ss*.8,0,0),'silver',.14)
 # Rear heel exhaust slit and a small horizontal catch preserve the drawing's articulated heel.
 plate('Foot_Rear_Blue_Heel_Bridge.'+side,[foot(p) for p in [(-6,20,17),(6,20,17),(8,22,5),(-8,22,5)]],(0,-3,0),'blue',.2)
 for j in range(3):
  plate('Heel_Recessed_Vent_%s.'%j+side,[foot(p) for p in [(-4,22.1,7+j*2),(4,22.1,7+j*2),(4,22.1,8+j*2),(-4,22.1,8+j*2)]],(0,-.5,0),'black',.04)

# Small maintenance markings follow actual service panels rather than covering the design.
for s,side in ((1,'L'),(-1,'R')):
 group('04 Arms and articulated hands','Forearm.'+side)
 f=limb_basis((s*87,0,281),(s*112,-2,226),s)
 text_mesh('Forearm_Service_Stencil.'+side,'V-01',f((0,-15.7,36)),1.7,'mark')
 group('05 Thigh and knee armor','Thigh.'+side)
 f=limb_basis((s*31,1,242),(s*58,-1,167),s)
 text_mesh('Thigh_Unit_Stencil.'+side,'01',f((0,-14.6,37)),1.8,'mark')
 group('06 Tapered shin and calf','Shin.'+side)
 f=limb_basis((s*58,-1,167),(s*93,2,40),s)
 for t in (34,93):marker('Shin_Datum_Mark_%s.'%t+side,f((2,-13,t)),1.7)
SC['body_finish_legacy_external_parts_removed']=removed
SC['body_accepted_head_geometry_unchanged']=True
SC['body_head_fit_scale']=1.35
