from mass_ops import *
wr=bpy.data.objects['AntiShip_Blade_Display_Root'];group('10 Anti-ship beam sword',wr)
gc=Vector(SC['sword_grip_center_units']);a=Vector(SC['sword_grip_axis']).normalized();u=Vector((.9536,0,.301)).normalized();u=(u-a*u.dot(a)).normalized();n=a.cross(u).normalized()
wp=lambda p:tuple(gc+u*p[0]+n*p[1]+a*p[2])
remove(['V3B_Sword_Continuous_Physical_Diamond_Blade','V3B_Sword_Dark_Machinery_Spine_','V3B_Sword_Spine_Cartridge_','V3B_Sword_Gold_Conductor_','V3B_Sword_Cartridge_Pin_','V3B_Sword_Edge_Rail_','V3B_Sword_Root_Armor_Block_'])
for o in list(SC.objects):
 if o.type=='MESH' and o.get('beam_component'):bpy.data.objects.remove(o,do_unlink=True)

# Closed asymmetric wedge sections: broad root shoulder, tapered edge, swept chisel point.
stations=[(36,-10,10,3.7),(52,-16,11.5,4.2),(77,-14.8,10.8,3.8),(126,-11.8,8.7,3.1),(163,-9,7,2.5),(190,-5.8,5.5,1.75),(208,-1.4,2.5,.8),(216,1.93,2.07,.06)]
rings=[]
for t,l,r,h in stations:
 w=r-l;mid=(l+r)*.5
 rings.append([(l,0,t),(l+w*.20,h*.82,t),(mid,h,t),(r-w*.18,h*.95,t),(r,0,t),(r-w*.18,-h*.95,t),(mid,-h,t),(l+w*.20,-h*.82,t)])
blade=loft('Sword_Continuous_Physical_Diamond_Blade',rings,'silver',.07,wp)
blade['blade_section']='Asymmetric solid eight-vertex wedge; 8.4 units thick at root; tapered chisel point'
blade['beam_component']=False
# A raised armored spine reads as a machined volume even in an edge-on view.
sp=[]
for t,w,h,cx in ((41,6.8,5.9,2.0),(62,7.2,6.2,2),(120,5.2,4.9,2),(159,3.8,3.8,2),(182,2.5,2.7,2),(198,.3,.9,2)):
 sp.append([(cx-w,-h*.70,t),(cx-w*.65,-h,t),(cx+w*.65,-h,t),(cx+w,-h*.60,t),(cx+w,h*.60,t),(cx+w*.65,h,t),(cx-w*.65,h,t),(cx-w,h*.70,t)])
loft('Sword_Mass_Armored_Machinery_Spine',sp,'frame',.18,wp)
def height(t):return 6.4 if t<63 else 6.4-(t-63)*.030
for ss in (-1,1):
 # Chamfered generator jaws wrap the emitter-to-blade transition.
 hull('Sword_Mass_Generator_Jaw_%d'%ss,[wp((x,ss*y,t)) for x,y,t in ((-10,4,32),(9,4,32),(14,4,44),(10,4,62),(-8,4,68),(-15,4,49),(-8,7.6,37),(7,7.6,37),(10,7.2,45),(7,6.8,58),(-7,6.8,62),(-11,7.2,49))],'navy',.35)
 plate('Sword_Mass_Generator_Armor_%d'%ss,[wp((x,ss*y,t)) for x,y,t in ((-8,7.9,38),(6,7.9,38),(10,7.5,46),(6,7.2,53),(-7,7.2,57),(-11,7.5,49))],-n*ss*1.0,'blue',.23)
 plate('Sword_Mass_Generator_Vent_%d'%ss,[wp((x,ss*8.1,t)) for x,t in ((-6,43),(4,43),(5,47),(-7,51))],-n*ss*.4,'black',.1)
 for j in range(3):
  strip('Sword_Mass_Generator_Louver_%d_%d'%(ss,j),[wp((-5.5,ss*8.3,44+j*1.7)),wp((3.7,ss*8.3,44+j*1.1))],.65,'titanium',-n*ss*.5)
 for j,(t,le) in enumerate(((73,17),(98,19),(124,18),(149,16),(172,12))):
  h=height(t)+.25;w=4.7-(t-73)*.021
  plate('Sword_Mass_Angled_Spine_Cartridge_%d_%d'%(ss,j),[wp((x,ss*hh,tt)) for x,hh,tt in ((2-w,h,t-le*.5+2),(2+w*.7,h,t-le*.5),(2+w,h,t+le*.5-2),(2-w*.5,h,t+le*.5),(2-w,h,t+le*.5-3))],-n*ss*1.25,'steel' if j in (0,3) else 'frame',.19)
  strip('Sword_Mass_Conductor_Inset_%d_%d'%(ss,j),[wp((2.5,ss*(h+.25),t-le*.29)),wp((3,ss*(h+.25),t+le*.28))],.68,'gold',-n*ss*.3)
  bolt('Sword_Mass_Spine_Captive_Lock_%d_%d'%(ss,j),wp((-.5,ss*(h+.3),t-le*.22)),n*ss,.45)
 # A separate steel lip follows the sharpened bevel; it is not part of the light blade.
 for edge in ('L','R'):
  vals=[(t,l if edge=='L' else r,h) for t,l,r,h in stations[1:-1]]
  strip('Sword_Mass_Silver_Edge_Rail_%s_%d'%(edge,ss),[wp((x+(.65 if edge=='L' else -.65),ss*.5,t)) for t,x,h in vals],.6,'titanium',-n*ss*.28)

# Both independent energy edges follow the physical outline and meet at the pointed tip.
for side in (-1,1):
 points=[]
 for t,l,r,h in stations[:-1]:points.append(Vector((l-.9 if side<0 else r+.8,0,t)))
 points.append(Vector((2,0,218)))
 for j,(pa,pb) in enumerate(zip(points,points[1:])):
  dv=(pb-pa).normalized();cross=Vector((dv.z,0,-dv.x)).normalized()
  for material_key,width,dep in (('beam',1.45 if side<0 else 1.1,.68),('beam_core',.36,.75)):
   pts=[wp(pa+cross*width/2+Vector((0,dep,0))),wp(pb+cross*width/2+Vector((0,dep,0))),wp(pb-cross*width/2+Vector((0,dep,0))),wp(pa-cross*width/2+Vector((0,dep,0)))]
   o=plate('Sword_Mass_%s_Edge_%d_%02d'%(material_key,side,j),pts,-n*dep*2,material_key,.025);o['beam_component']=True
   for path in ('hide_render','hide_viewport'):
    dr=o.driver_add(path).driver;dr.expression='not beam';var=dr.variables.new();var.name='beam';var.type='SINGLE_PROP';var.targets[0].id=wr;var.targets[0].data_path='["Beam_On"]'
wr['Beam_On']=True;wr['weapon_type']='Anti-ship beam blade with asymmetric chisel point and raised solid machinery spine';wr['blade_tip_world_m']=list(U(wp((2,0,218))))
SC['weapon_mass_dimensions_units']={'root_width':27.5,'root_physical_thickness':8.4,'root_spine_thickness':12.4,'blade_length':180}

# Real review cameras for profile, edge thickness and the machined spine.
for label,direction in (('WEAPON_PROFILE',n),('WEAPON_3Q',(n+u*.58).normalized()),('WEAPON_EDGE',(u+n*.13).normalized())):
 name='V3B_CAM_'+label;cam=bpy.data.objects.get(name)
 if cam is None:
  data=bpy.data.cameras.new(name);cam=bpy.data.objects.new(name,data);SC.collection.objects.link(cam)
 target=U(gc+a*99);cam.location=target+direction*7;cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=2.3
