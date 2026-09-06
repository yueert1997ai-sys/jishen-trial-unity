import bpy,pathlib,sys
P=pathlib.Path(__file__).resolve().parent;sys.path.insert(0,str(P))
bpy.ops.wm.open_mainfile(filepath=str(P/'refinement'/'PHASE_02.blend'))
from refine_common import *
for o in list(SC.objects):
 if o.name.startswith(('Pectoral_Main_Intake.','Chest_Upper_Armored_Brow.','Pectoral_Armored_Top_Cap.')):bpy.data.objects.remove(o,do_unlink=True)
for s,side in ((-1,'R'),(1,'L')):
 def mir(pts):return [(s*x,y,z) for x,y,z in pts]
 plate('Refit_Pectoral.'+side,mir([(.055,-.38,2.884),(.16,-.443,2.946),(.41,-.358,2.943),(.531,-.152,2.824),(.475,-.247,2.700),(.303,-.430,2.649),(.116,-.455,2.700)]),(0,.133,0),'Thorax','navy',replace='Thorax_Pectoral_Wrapped.'+side,bevel=.003)
 plate('R3_Chest_Swept_Upper_Shroud.'+side,mir([(.094,-.421,2.889),(.177,-.457,2.953),(.404,-.371,2.946),(.515,-.215,2.865),(.431,-.310,2.861),(.257,-.440,2.906)]),(0,.030,-.011),'Thorax','panel',bevel=.002)
 plate('R3_Chest_Lower_Return_Flange.'+side,mir([(.120,-.461,2.693),(.309,-.436,2.643),(.482,-.254,2.694),(.466,-.277,2.672),(.306,-.445,2.621),(.127,-.469,2.674)]),(0,.024,0),'Thorax','white',bevel=.0014)
 intake('R3_Pectoral_Duct.'+side,'Thorax_Pectoral_Wrapped.'+side,s*.275,2.777,.255,.068,'Thorax',bars=3)
 # Upper shoulder transfer yokes anchor the floating shoulder to the ribcage.
 rod('R3_Clavicle_Tension_Axle.'+side,(s*.39,.025,2.845),(s*.58,.025,2.845),.032,'Thorax','gun',n=20)
 tube('R3_Clavicle_Armored_Bushing.'+side,(s*.445,.025,2.845),(s*.495,.025,2.845),.052,.034,'Thorax','steel')
 for j in range(2):
  z=2.40+j*.085
  plate('R3_Articulated_Rib_Return_'+str(j)+'.'+side,mir([(.13,-.228,z+.04),(.315,-.18,z+.105),(.379,-.02,z+.132),(.344,-.055,z+.077),(.14,-.246,z)]),(0,.032,0),'Thorax','gun',bevel=.0018)
 rod('R3_Waist_Anterior_Tendon.'+side,(s*.105,-.076,2.11),(s*.205,-.105,2.382),.012,'Waist','steel')
 rod('R3_Waist_Anterior_Sleeve.'+side,(s*.105,-.076,2.11),(s*.147,-.088,2.222),.023,'Waist','gun')
 tube('R3_Waist_Sleeve_Seal.'+side,(s*.144,-.087,2.212),(s*.153,-.090,2.238),.028,.013,'Waist','steel')
 for o in list(obj('Skirt_Hinge.'+side).children):
  if o.type=='MESH' and o.name.startswith('Skirt_Front'):
   affine(o,center=(s*.208,0,2.047),scale=(.95,1,.87))
 plate('R3_Pelvis_Side_Socket.'+side,mir([(.158,-.125,2.097),(.249,-.12,2.091),(.295,-.075,2.025),(.267,-.103,1.977),(.174,-.135,2.012)]),(0,.065,0),'Pelvis','gun',bevel=.002)
 rod('R3_Skirt_Pivot_Axle.'+side,(s*.21,-.195,2.043),(s*.21,-.11,2.043),.027,'Skirt_Hinge.'+side,'gun')
plate('Refit_Sternum',[(0,-.435,2.917),(.104,-.456,2.835),(.115,-.485,2.72),(.070,-.497,2.621),(0,-.415,2.540),(-.070,-.497,2.621),(-.115,-.485,2.72),(-.104,-.456,2.835)],(0,.085,0),'Thorax','navy',replace='Sternum_Forward_Keel',bevel=.0025)
plate('R3_Core_Lower_Armored_Return',[(-.058,-.490,2.660),(.058,-.490,2.660),(.049,-.473,2.627),(0,-.421,2.580),(-.049,-.473,2.627)],(0,.021,0),'Thorax','gun',bevel=.0015)
for n in ('Core_Upper_Access_Shell','Core_Narrow_Sensor_Window'):affine(obj(n),move=(0,-.018,.033))
plate('Refit_Abdomen_Lower',[(-.11,-.167,2.242),(.11,-.167,2.242),(.09,-.192,2.153),(0,-.192,2.10),(-.09,-.192,2.153)],(0,.051,0),'Waist','panel',replace='Abdomen_Single_Tapered_Guard_Lower',bevel=.002)
save_phase(3,'Reshaped wrapped pectorals and sternum; swept outer shrouds, true recessed widened intakes, return flanges, rib layers, clavicle yokes; front waist tendon actuators, independent shortened skirt shields and keyed pelvis sockets.')
