"""Side-elevation correction requested by the user. Preserve the front silhouette."""
from body_common import *
from body_limbs import limb_basis

def region(o):
 p=o
 names=[]
 while p:names.append(p.name);p=p.parent
 if any(n.startswith('V3H_') or n in ('Head','HEAD_V3_ROOT') for n in names):return 'head'
 if 'AntiShip_Blade_Display_Root' in names:return 'weapon'
 if 'Cannon_Right_Cradle_Mount' in names:return 'cannon'
 if 'Backpack_Structural_Mount' in names:return 'pack'
 for nm in ('Hand','Forearm','UpperArm','Foot','Shin','Thigh','Shoulder_Armor_Floating_Pivot','Skirt_Hinge'):
  if any(n.startswith(nm+'.') for n in names):return {'Shoulder_Armor_Floating_Pivot':'shoulder','Skirt_Hinge':'skirt'}.get(nm,nm.lower())
 pa=o.parent.name if o.parent else ''
 return {'Thorax':'thorax','Waist':'waist','Pelvis':'pelvis'}.get(pa,'other')
objs=[o for o in SC.objects if o.type=='MESH'];bpy.context.view_layer.update()
oldworld={o:o.matrix_world.copy() for o in SC.objects}
oldreg={o:region(o) for o in objs}
def stats():
 dg=bpy.context.evaluated_depsgraph_get();out={}
 for o in objs:
  reg=oldreg[o]
  if reg in ('head','weapon','cannon','pack','other') or o.hide_render:continue
  me=o.evaluated_get(dg).to_mesh();ys=[(o.matrix_world@v.co).y for v in me.vertices];o.evaluated_get(dg).to_mesh_clear()
  if not ys:continue
  if reg not in out:out[reg]=[min(ys),max(ys)]
  else:out[reg]=[min(out[reg][0],min(ys)),max(out[reg][1],max(ys))]
 return {k:{'front_y_m':v[0],'rear_y_m':v[1],'depth_m':v[1]-v[0]} for k,v in out.items()}
before=stats()
def clamp(x):return max(0,min(1,x))
def mapping(reg,p,joint=False,cy=None):
 p=Vector(p);y=p.y;z=p.z
 if reg in ('head','other','cannon'):return p
 if reg=='pack':p.y+=10;return p
 if reg in ('hand','weapon'):p.y-=16;return p
 if reg=='thorax':
  if z>344:fac=1+(1.30-1)*clamp((349-z)/5) if y<0 else 1+(1.70-1)*clamp((349-z)/5)
  else:fac=1.30 if y<0 else 1.70
  yy=y*fac
 elif reg=='waist':yy=y*(1.42 if y<0 else 1.50)
 elif reg=='pelvis':yy=y*(1.42 if y<0 else 1.50)
 elif reg=='shoulder':yy=3+(y-3)*1.35+2
 elif reg=='skirt':yy=y*1.35
 elif reg=='upperarm':
  t=clamp((330-z)/49);base=3*(1-t);shift=2*(1-t)-4*t;yy=base+(y-base)*1.40+shift
 elif reg=='forearm':
  t=clamp((281-z)/55);base=-2*t;shift=-4*(1-t)-16*t;yy=base+(y-base)*1.60+shift
 elif reg=='thigh':
  t=clamp((242-z)/75);base=1-2*t;shift=-14*t;yy=base+(y-base)*1.50+shift
 elif reg=='shin':
  t=clamp((167-z)/127);base=-1+3*t;shift=-14*(1-t)+3*t;yy=base+(y-base)*(1.35 if y<base else 1.40)+shift
 elif reg=='foot':
  t=clamp((42-z)/24);fac=1+(1.65-1)*t if y<2 else 1+(1.30-1)*t;yy=2+(y-2)*fac+3
 else:yy=y
 if joint and cy is not None:
  # Move complete rotary bearings with their centers, keeping circular races circular.
  cc=Vector(p);cc.y=cy;p.y=y+mapping(reg,cc,False).y-cy
 else:p.y=yy
 return p
newmeshes={}
for o in objs:
 reg=oldreg[o]
 if reg in ('head','other'):continue
 me=o.data.copy();me.transform(oldworld[o]);pts=[v.co/S for v in me.vertices]
 cy=sum(p.y for p in pts)/max(1,len(pts))
 joint=(o.get('V3B_retained_existing_component') and any(t in o.name for t in ('Servo.','Rotator.','Bearing','Knee_Axle','Knee_Large_Hinge','Waist_Yaw_Drive','Hip_Load_Bearing')))
 for v,p in zip(me.vertices,pts):v.co=U(mapping(reg,p,joint,cy))
 bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free();newmeshes[o]=me

# Reposition retained animation pivots, then restore transformed meshes in world space.
def depth(o):
 n=0
 while o.parent:n+=1;o=o.parent
 return n
newp={}
for o in SC.objects:
 if o.type!='EMPTY':continue
 reg=region(o)
 if o.name.startswith(('UpperArm.','Shoulder_Armor_Floating_Pivot.')):dy=2
 elif o.name.startswith('Forearm.'):dy=-4
 elif o.name.startswith(('Hand.','Finger_')) or o.name=='V3B_Sword_Grip_Socket':dy=-16
 elif o.name.startswith('Shin.'):dy=-14
 elif o.name.startswith('Foot.'):dy=3
 elif reg=='pack':dy=10
 else:dy=0
 m=oldworld[o].copy();m.translation.y+=dy*S;newp[o]=m
for o in sorted(newp,key=depth):o.matrix_world=newp[o];bpy.context.view_layer.update()
for o,me in newmeshes.items():o.data=me;o.matrix_world=Matrix.Identity(4)
for o in sorted([o for o in objs if oldreg[o]=='head'],key=depth):o.matrix_world=oldworld[o]
bpy.context.view_layer.update()
SC['sword_grip_center_units']=[-116,-24,209]
after=stats();(W/'side_depth_measurements.json').write_text(json.dumps({'before':before,'after':after,'head_geometry_and_assembly_transform_preserved':True,'front_x_and_height_coordinates_preserved':True,'stance_changes_reference_units':{'knee_forward':14,'ankle_rearward':3,'wrist_forward':16}},indent=2))

# Closed sculpted volumes under the front armor make the new depth legible from the side.
group('01 Chest and abdomen','Thorax')
for s,side in ((1,'L'),(-1,'R')):
 mi=lambda p:mirror(p,s)
 v=[(22,-21,337),(31,-17,336),(38,-5,331),(37,17,328),(27,26,325),(19,24,321),(18,9,311),(25,-9,311),(31,-16,318)]
 skin('Thorax_Deep_Side_Clamshell.'+side,mi(v),[(0,1,8),(1,2,3,8),(3,4,5,6,7,8),(0,8,7)],(-s*3,0,-1),'blue',.3)
 plate('Thorax_Rear_Lower_Wrapped_Lamella.'+side,mi([(28,21,319),(36,17,321),(34,21,309),(22,24,297),(20,17,305)]),(-s*2,-1,0),'navy',.25)
 plate('Thorax_Side_White_Return.'+side,mi([(31,-7,314),(34,2,313),(30,14,301),(24,17,293),(22,9,301)]),(-s*2,0,0),'white',.25)
 # Side heat exchanger inserted into the clamshell, angled down toward the waist.
 plate('Thorax_Lateral_Vent_Well.'+side,mi([(37,1,328),(37,12,327),(34,16,319),(34,3,319)]),(-s*.8,0,0),'black',.1)
 for j in range(3):
  y=3+j*3.5
  strip('Thorax_Lateral_Gold_Louver_%s.'%j+side,mi([(37.2,y,326),(34.7,y+2,321)]),.8,'steel',(-s*.6,0,0))
 bolt('Thorax_Side_Captive_Latch.'+side,(s*36,9,317),(s,0,0),.7)

group('02 Waist and pelvis','Waist')
for s,side in ((1,'L'),(-1,'R')):
 mi=lambda p:mirror(p,s)
 hull('Waist_Deep_Lateral_Servo.'+side,mi([(12,-12,294),(18,-14,290),(20,4,292),(18,20,281),(12,21,278),(10,-9,277)]),'frame',.4)
 plate('Waist_Side_Silver_Coupling.'+side,mi([(17,-5,289),(20,1,287),(18,15,281),(15,15,277),(15,0,281)]),(-s*2,0,0),'silver',.22)
 piston('Waist_Side_Load_Ram.'+side,(s*18,14,298),(s*17,18,270),1.65)
group('02 Waist and pelvis','Pelvis')
for s,side in ((1,'L'),(-1,'R')):
 mi=lambda p:mirror(p,s)
 hull('Pelvis_Deep_Side_Cassette.'+side,mi([(22,-21,259),(35,-14,263),(39,4,258),(34,24,252),(24,25,244),(20,11,239),(19,-10,244)]),'frame',.5)
 plate('Pelvis_Side_Armored_Carapace.'+side,mi([(33,-14,262),(39,4,258),(35,20,254),(29,23,252),(27,12,252),(34,-2,259)]),(-s*2,0,-1),'blue',.27)
 plate('Pelvis_Rear_Sloped_Blue_Return.'+side,mi([(16,25,261),(27,25,259),(32,25,251),(25,25,241),(15,25,247)]),(0,-3,0),'navy',.23)
 bearing('Pelvis_Side_Drive_Coupling.'+side,(s*30,6,247),(1,0,0),4.4,11)

for s,side in ((1,'L'),(-1,'R')):
 group('04 Arms and articulated hands','UpperArm.'+side)
 f=limb_basis((s*64,5,330),(s*87,-4,281),s)
 vv=[(7,-10,18),(11,-3,18),(12,10,21),(8,20,27),(6,20,38),(9,6,44),(9,-7,39)]
 skin('Upperarm_Deep_Wrapped_Ceramic_Casing.'+side,[f(p) for p in vv],[(0,1,6),(1,2,5,6),(2,3,4,5)],(-s*2,0,0),'white',.27)
 plate('Upperarm_Side_Gray_Insert.'+side,[f(p) for p in [(11,-1,22),(12,8,24),(10,16,29),(9,14,34),(10,0,32)]],(-s*1,0,0),'silver',.2)
 group('04 Arms and articulated hands','Forearm.'+side)
 f=limb_basis((s*87,-4,281),(s*112,-18,226),s)
 vv=[(8,-12,20),(12,-2,18),(13,13,21),(10,21,28),(7,21,49),(7,7,57),(8,-8,52)]
 skin('Forearm_Deep_Closed_Side_Pod.'+side,[f(p) for p in vv],[(0,1,6),(1,2,5,6),(2,3,4,5)],(-s*2,0,0),'blue',.26)
 plate('Forearm_Deep_Side_Overlapping_Cartridge.'+side,[f(p) for p in [(12,1,22),(13.2,12,25),(10.8,20,31),(9.2,17,43),(10.2,5,46)]],(-s*1,0,0),'panel',.2)
 plate('Forearm_Deep_White_Rear_Rail.'+side,[f(p) for p in [(9,19,27),(10,22,30),(8,22,45),(6,19,51)]],(-s*.8,-.5,0),'white',.17)
 group('05 Thigh and knee armor','Thigh.'+side)
 f=limb_basis((s*31,1,242),(s*58,-15,167),s)
 vv=[(9,-14,30),(13,-2,28),(14,14,33),(9,23,40),(6,23,58),(9,11,70),(12,-7,62)]
 skin('Thigh_Deep_White_Lateral_Casing.'+side,[f(p) for p in vv],[(0,1,6),(1,2,5,6),(2,3,4,5)],(-s*2,0,0),'white',.28)
 plate('Thigh_Deep_Silver_Service_Panel.'+side,[f(p) for p in [(13.2,0,32),(14.2,11,34),(11,20,41),(10,17,51),(12,1,50)]],(-s*1,0,0),'silver',.21)
 for t in (35,54):bolt('Thigh_Deep_Captive_Pin_%s.'%t+side,f((13,10,t)),(s,0,0),.55)
 group('06 Tapered shin and calf','Shin.'+side)
 f=limb_basis((s*58,-15,167),(s*93,5,40),s)
 vv=[(9,7,22),(16,18,24),(18,31,41),(13,35,58),(8,24,78),(6,10,77),(10,10,48)]
 skin('Calf_Deep_Outer_Nacelle.'+side,[f(p) for p in vv],[(0,1,6),(1,2,3,6),(3,4,5,6)],(-s*2,-1,0),'blue',.27)
 plate('Calf_Deep_Upper_Overlapping_Plate.'+side,[f(p) for p in [(14,19,26),(18,29,38),(15,34,48),(11,26,52),(11,18,41)]],(-s*1,-1,0),'panel',.21)
 plate('Calf_Deep_Lower_Overlapping_Plate.'+side,[f(p) for p in [(12,28,50),(15,35,52),(11,30,66),(6,21,78),(7,18,65)]],(-s*1,-1,0),'blue',.22)
 strip('Calf_Deep_Gold_Damper_Guide.'+side,[f((14,31,50)),f((11,28,65)),f((7,21,76))],.7,'gold',(-s*.6,0,0))
SC['side_depth_revision']='User-requested full side elevation review: expanded thorax, pelvis, limb shells and feet; forward knees and wrists; circular rotary joints preserved.'
