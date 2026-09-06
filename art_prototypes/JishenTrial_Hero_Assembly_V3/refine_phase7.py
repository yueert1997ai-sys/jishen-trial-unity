import bpy,pathlib,sys
P=pathlib.Path(__file__).resolve().parent;sys.path.insert(0,str(P))
bpy.ops.wm.open_mainfile(filepath=str(P/'refinement'/'PHASE_06.blend'))
from refine_common import *
def surface_frame(target,x,z,back=False):
 o=obj(target);bpy.context.view_layer.update();e=o.evaluated_get(bpy.context.evaluated_depsgraph_get());iv=o.matrix_world.inverted();ray=Vector((0,-1 if back else 1,0))
 ok,p,n,i=e.ray_cast(iv@Vector((x,4 if back else -4,z)),iv.to_3x3()@ray);assert ok,(target,x,z)
 p=o.matrix_world@p;n=(o.matrix_world.to_3x3()@n).normalized();xx=(Vector((1,0,0))-n*n.x).normalized();zz=xx.cross(-n).normalized()
 if zz.z<0:xx=-xx;zz=-zz
 return p,n,xx,zz
def hatch(name,target,x,z,w,h,parent,back=False):
 p,n,xx,zz=surface_frame(target,x,z,back)
 outline=[(-.5,-.30),(-.35,-.5),(.35,-.5),(.5,-.30),(.5,.30),(.35,.5),(-.35,.5),(-.5,.30)]
 for label,factor,off,mat in (('Gasket',1.08,.0006,'dark'),('Cover',1,.002,'panel')):
  pts=[tuple(p+n*off+xx*a*w*factor+zz*b*h*factor) for a,b in outline]
  plate(name+'_'+label,pts,tuple(-n*.003),parent,mat,bevel=.0006)
 for a in (-.34,.34):
  c=p+n*.003+xx*a*w
  rod(name+'_Captive_Key_'+str(a),c,c+n*.0025,.0028,parent,'gun',n=6)
def stencil(name,text,target,x,z,size,parent,back=False):
 p,n,xx,zz=surface_frame(target,x,z,back)
 d=bpy.data.curves.new(name,'FONT');d.body=text;d.size=size;d.align_x='CENTER';d.align_y='CENTER';d.extrude=.00014;d.resolution_u=3
 o=bpy.data.objects.new(name,d);C['secondary'].objects.link(o);d.materials.append(M['white']);o.parent=obj(parent)
 w=Matrix((xx,zz,n)).transposed().to_4x4();w.translation=p+n*.0007;o.matrix_world=w
 bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get();me=bpy.data.meshes.new_from_object(o.evaluated_get(dg),depsgraph=dg)
 bpy.data.objects.remove(o,do_unlink=True);o=bpy.data.objects.new(name,me);C['secondary'].objects.link(o);o.parent=obj(parent);o.matrix_world=w;o['design_role']='Painted unit identification stencil, solid closed extrusion for reliable bake/export'
 return o
for s,side in ((-1,'R'),(1,'L')):
 hatch('R7_Clavicle_Service.'+side,'Clavicle_Swept_Carapace.'+side,s*.25,2.943,.082,.036,'Thorax')
 hatch('R7_Femur_Service.'+side,'Thigh_Main_Lateral_Shell.'+side+'_Lower',s*.293,1.448,.072,.085,'Thigh.'+side)
 hatch('R7_Calf_Rear_Damper_Access.'+side,'Calf_Rear_Spur_Armor.'+side,s*.343,.721,.07,.075,'Shin.'+side,True)
 stencil('R7_Shoulder_Unit_ID.'+side,'01','Shoulder_Front_Service_Armor.'+side,s*.597,2.741,.029,'Shoulder_Armor_Floating_Pivot.'+side)
 stencil('R7_Shin_Unit_ID.'+side,'01','Shin_Long_Forward_Keel.'+side+'_Upper',s*.336,1.02,.038,'Shin.'+side)
hatch('R7_Reactor_Lower_Service','Backpack_Central_Reactor_Spine',0,2.416,.10,.083,'Backpack_Structural_Mount',True)
hatch('R7_Blade_Generator_Service','R6_Blade_Generator_Armor',-1.18,2.106,.068,.036,'AntiShip_Blade_Display_Root')
stencil('R7_Blade_Identification','AS-01','R6_Blade_Load_Panel_0_Front',-1.185,1.783,.027,'AntiShip_Blade_Display_Root')
# Remove dead boolean links left by explicitly replaced service covers.
for o in SC.objects:
 for m in list(o.modifiers):
  if m.type=='BOOLEAN' and m.object is None:o.modifiers.remove(m)
SC['head_status']='Refined and reviewed in actual renders; no user approval assumed'
SC['head_review']='Shorter depth, flatter paired optics, compact faceted mask; interpretive reconstruction from provided views'
obj('JishenTrial_Hero_Assembly_V3')['asset_state']='Existing assembly refined through phases 1-7; editable high-detail master'
save_phase(7,'Limited functional detail pass: gasketed actuator/damper access covers with captive keys; shoulder/shin unit IDs and blade ID. No random fastener scatter. Dead boolean links cleaned. Agent visual review complete; user design approval not assumed.')
