from mass_ops import *
remove(['V3B_Knee_Leading_Blue_Shield','V3B_Knee_Titanium_Extensor','V3B_Knee_Crown_Panel','V3B_Thigh_Mass2_Knee_Armored_Fork'])
for s,side in ((1,'L'),(-1,'R')):
 group('05 Thigh and knee armor','Thigh.'+side);f=joint_frame('Thigh','Shin',side)
 # Thick inverted-U distal thigh armor covers the upper knee junction.
 vs=[(-13,-16,62),(-8,-21,64),(8,-21,64),(13,-16,62),(-16,-9,76),(-11,-17,84),(-7,-21,75),(7,-21,75),(11,-17,84),(16,-9,76)]
 skin('Knee_Mass2_Upper_Thigh_Ceramic_Hood.'+side,[f(p) for p in vs],[(0,1,6,5,4),(1,2,7,6),(2,3,9,8,7)],Vector(f((0,3.3,0)))-Vector(f((0,0,0))),'white',.36)
 for ss in (-1,1):
  local_plate('Knee_Mass2_Thigh_Lateral_Overhang_%d.'%ss+side,[(ss*13,-10,66),(ss*17,-3,69),(ss*17,8,77),(ss*13,10,82),(ss*10,1,84),(ss*12,-7,76)],f,(-ss*2.5,0,0),'white',.35)
 local_plate('Knee_Mass2_Upper_Posterior_Overlap.'+side,[(-11,16,70),(11,16,70),(11,15,82),(6,14,88),(-6,14,88),(-11,15,82)],f,(0,-2.7,0),'navy',.3)
 group('06 Tapered shin and calf','Shin.'+side);f=joint_frame('Shin','Foot',side)
 # Fully closed front kneecap spans the formerly exposed black cylinder and frame.
 vs=[(-11,-19,-8),(0,-24,-10),(11,-19,-8),(-16,-20,5),(0,-29,7),(16,-20,5),(-12,-20,19),(0,-27,29),(12,-20,19)]
 skin('Knee_Mass2_Closed_Armored_Patella.'+side,[f(p) for p in vs],[(0,1,4,3),(1,2,5,4),(3,4,7,6),(4,5,8,7)],Vector(f((0,5,0)))-Vector(f((0,0,0))),'blue',.4)
 local_plate('Knee_Mass2_Patella_Upper_Crest.'+side,[(-7,-22,-4),(0,-25,-6),(7,-22,-4),(8,-24,4),(0,-28.5,8),(-8,-24,4)],f,(0,1.4,0),'panel',.2)
 for ss in (-1,1):
  local_plate('Knee_Mass2_Patella_Side_Return_%d.'%ss+side,[(ss*11,-18,-8),(ss*17,-11,-3),(ss*18,0,8),(ss*14,-7,20),(ss*11,-18,21),(ss*15,-20,6)],f,(-ss*2.5,0,0),'navy',.3)
  strip('Knee_Mass2_Patella_Lower_Machined_Lip_%d.'%ss+side,[f((ss*11,-20.4,19)),f((0,-27.4,28))],.68,'titanium')
 # Solid angular side pods enclose both rotary races. Only the assembly seam is exposed.
 ctr=Vector(bpy.data.objects['Shin.'+side].matrix_world.translation)/S
 outline=[(-9,-8),(-12,0),(-8,10),(1,12),(9,8),(11,-1),(7,-10),(-2,-12)]
 for ss in (-1,1):
  rings=[]
  for off,rad in ((10.3,1.0),(15.5,1.05),(17.5,.82)):
   rings.append([tuple(ctr+Vector((s*ss*off,y*rad,z*rad))) for y,z in outline])
  loft('Knee_Mass2_Sealed_Rotary_Armor_Pod_%d.'%ss+side,rings,'blue' if ss>0 else 'navy',.32)
  normal=Vector((s*ss,0,0));face=ctr+normal*17.6
  rod('Knee_Mass2_Flush_Service_Cover_%d.'%ss+side,face,face+normal*.6,3.5,'steel',n=8,bevel=.16)
  bolt('Knee_Mass2_Captive_Service_Lock_%d.'%ss+side,face+normal*.7,normal,.62)
  pp=[tuple(ctr+Vector((s*ss*17.75,y,z))) for y,z in ((-5,7),(1,8),(6,5),(5,3),(-1,5),(-5,4))]
  plate('Knee_Mass2_Rotary_Pod_Ceramic_Crest_%d.'%ss+side,pp,-normal*.8,'white',.17)
 # Stepped rear plates close the posterior gap while maintaining the overlapping joint design.
 for j,(t,yy,ww) in enumerate(((-4,13,11),(5,15,12),(14,17,11))):
  local_plate('Knee_Mass2_Posterior_Lamella_%d.'%j+side,[(-ww,yy,t),(ww,yy,t),(ww*.9,yy+1,t+6),(ww*.55,yy+2,t+8),(-ww*.55,yy+2,t+8),(-ww*.9,yy+1,t+6)],f,(0,-2.5,0),'navy' if j!=1 else 'blue',.25)

# Dedicated close-ups include both sides of the enclosed joint.
for label,loc,target,scale in (
 ('KNEES',(3.5,-8,2.4),(0,-.08,1.52),1.75),
 ('KNEE_SIDE',(7,.3,2.0),(.53,-.06,1.50),.90),
 ('LEG_SIDE',(7,.3,2.0),(.66,0,1.15),2.30)):
 name='V3B_CAM_'+label;cam=bpy.data.objects.get(name)
 if cam is None:
  data=bpy.data.cameras.new(name);cam=bpy.data.objects.new(name,data);SC.collection.objects.link(cam)
 cam.location=loc;cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=scale
