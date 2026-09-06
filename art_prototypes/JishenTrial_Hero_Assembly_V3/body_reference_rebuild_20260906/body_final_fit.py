from body_common import *
from body_finish import deform
from body_limbs import limb_basis

for o in list(SC.objects):
 if o.type!='MESH':continue
 if o.name.startswith('V3B_Backpack_Central_Spine_Chassis'):
  # Rear lens must look into its optical well, not into the underlying solid chassis.
  deform(o,lambda p:Vector((p.x,min(p.y,45.0),p.z))) # Includes the six-unit rearward assembly offset.
 if o.name.startswith('V3B_Backpack_Upper_Crossmember'):
  deform(o,lambda p:Vector((p.x,p.y,p.z-8)))
 if o.name.startswith(('V3B_Rear_Tail_Skirt.','V3B_Rear_Tail_Blue_Facet.')):
  deform(o,lambda p:Vector((p.x*.50,p.y,p.z)))
 if o.name.startswith(('Thigh_Long_Inner_Housing.','Femur_Load_Frame.','UpperArm_Load_Frame.')):
  s=1 if o.name.endswith('.L') else -1
  a,b=((s*31,1,242),(s*58,-1,167)) if 'Thigh' in o.name or 'Femur' in o.name else ((s*64,3,330),(s*87,0,281))
  a=Vector(a);d=(Vector(b)-a).normalized();fac=.72 if 'Thigh' in o.name or 'Femur' in o.name else .86
  deform(o,lambda p,a=a,d=d,fac=fac:a+d*(p-a).dot(d)+((p-a)-d*(p-a).dot(d))*fac)
 # Weld text front/back/side boundaries so unit markings are closed editable solids.
 if any(t in o.name for t in ('Shoulder_Unit_01','Shoulder_Valkyr_Wordmark','Service_Stencil','Thigh_Unit_Stencil')):
  bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.0000005);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free()

for s,side in ((1,'L'),(-1,'R')):
 group('05 Thigh and knee armor','Thigh.'+side)
 f=limb_basis((s*31,1,242),(s*58,-1,167),s)
 def lp(name,pts,dep=(0,-2,0),mat='white',bev=.2):return plate(name,[f(p) for p in pts],dep,mat,bev)
 lp('Thigh_Posterior_Segmented_Ceramic_Upper.'+side,[(-10,12,31),(-6,15,29),(-4,15,45),(-8,14,49),(-11,11,42)])
 lp('Thigh_Posterior_Segmented_Ceramic_Lower.'+side,[(4,15,46),(9,14,43),(10,12,58),(5,15,67),(3,15,59)])
 lp('Thigh_Rear_Blue_Interlocking_Cap.'+side,[(-5,16,48),(3,16,47),(3,17,61),(-2,17,67),(-5,16,61)],(0,-2,0),'blue',.18)
 for t in (34,57):bolt('Thigh_Posterior_Ceramic_Pin_%s.'%t+side,f((-7 if t==34 else 7,15.3,t)),(0,1,0),.52)
 group('07 Feet and ankle couplings','Foot.'+side)
 q=Matrix.Rotation(math.radians(19*s),3,'Z');c=Vector((s*93,2,0))
 def foot(p):return tuple(c+q@Vector((s*p[0],p[1],p[2])))
 for ss in (-1,1):
  vv=[(ss*6,9,47),(ss*11,9,43),(ss*12,17,34),(ss*8,21,30),(ss*4,16,35)]
  plate('Ankle_Rear_Upper_Blue_Petal_%s.'%ss+side,[foot(p) for p in vv],(-s*ss*1.5,-1,0),'blue',.22)
  vv=[(ss*7,18,34),(ss*10,20,31),(ss*11,24,19),(ss*7,24,15),(ss*5,19,25)]
  plate('Ankle_Rear_Lower_Blue_Petal_%s.'%ss+side,[foot(p) for p in vv],(-s*ss*1.5,-1,0),'navy',.22)
  strip('Ankle_Rear_Gold_Service_Key_%s.'%ss+side,[foot((ss*9,18,32)),foot((ss*9,21,24))],.6,'gold',(0,-.5,0))

# Low-amplitude surface finish: the reference calls for clean composite paint.
# The material change applies to body-only blue variants; accepted head materials stay intact.
for key in ('panel','edge','white'):
 m=M[key];bs=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
 noise=m.node_tree.nodes.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=850;noise.inputs['Detail'].default_value=2;noise.inputs['Roughness'].default_value=.45
 bump=m.node_tree.nodes.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.10;bump.inputs['Distance'].default_value=.00006
 m.node_tree.links.new(noise.outputs['Fac'],bump.inputs['Height']);m.node_tree.links.new(bump.outputs['Normal'],bs.inputs['Normal'])
SC['body_visual_fit_pass']=5
