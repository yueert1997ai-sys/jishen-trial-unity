import bpy,pathlib,sys
P=pathlib.Path(__file__).resolve().parent;sys.path.insert(0,str(P))
bpy.ops.wm.open_mainfile(filepath=str(P/'refinement'/'PHASE_05.blend'))
from refine_common import *
for s,side in ((-1,'R'),(1,'L')):
 def mir(pts):return [(s*x,y,z) for x,y,z in pts]
 pod='Backpack_Main_Deploy_Hinge.'+side
 # Preserve the folded engine, replacing the spear-like upper fin with a capped shroud.
 hull('Refit_Folded_Engine_Cap.'+side,mir([(.155,.319,2.962),(.313,.40,2.976),(.328,.496,3.066),(.279,.434,3.244),(.218,.322,3.289),(.180,.280,3.224),(.176,.348,3.022),(.279,.515,3.016)]),pod,'navy',col='pack',replace='Backpack_Tall_Folded_Fin.'+side+'_Upper',bevel=.0024)
 # Transverse locking links explain the default STOWED position.
 rod('R6_Stowed_Locking_Strut.'+side,(s*.082,.294,2.781),(s*.321,.383,2.679),.019,'Backpack_Structural_Mount','gun','pack')
 rod('R6_Stow_Actuator_Sleeve.'+side,(s*.082,.294,2.781),(s*.202,.339,2.730),.029,'Backpack_Structural_Mount','gun','pack')
 tube('R6_Engine_Deploy_Hinge_Collar.'+side,(s*.226,.297,2.793),(s*.286,.297,2.793),.076,.048,pod,'gun','pack')
 plate('R6_Engine_Shroud_Service_Return.'+side,mir([(.198,.506,2.915),(.302,.537,2.908),(.298,.556,2.807),(.210,.53,2.777)]),(0,-.021,0),pod,'panel','pack',bevel=.002)
 # Fit heat-shield petals around the existing real exhaust axis.
 ex=obj('Backpack_Main_Exhaust.'+side);vs=[ex.matrix_world@v.co for v in ex.data.vertices]
 cen=sum(vs,Vector())/len(vs);low=min(v.z for v in vs);high=max(v.z for v in vs)
 axis=Vector((0,.10,-1)).normalized();a=cen-axis*(high-low)*.24;b=cen+axis*(high-low)*.43
 tube('R6_Main_Nozzle_Gimbal_Collar.'+side,a-axis*.009,a+axis*.010,.097,.078,pod,'gun','pack',n=24)
 q=axis.to_track_quat('Z','Y')
 for j in range(8):
  t=j*math.tau/8
  r1=q@Vector((.087*math.cos(t),.087*math.sin(t),0));r2=q@Vector((.087*math.cos(t+.38),.087*math.sin(t+.38),0))
  plate(f'R6_Exhaust_Heat_Petal_{j}.{side}',[tuple(a+r1),tuple(a+r2),tuple(b+r2*1.03),tuple(b+r1*1.03)],tuple(-r1.normalized()*.006),pod,'gun','pack',bevel=.0007)
 # Auxiliary attitude nozzle on each outer module, physically attached to a local yoke.
 rod('R6_RCS_Mount.'+side,(s*.371,.427,2.52),(s*.409,.461,2.48),.027,pod,'gun','pack')
 tube('R6_RCS_Nozzle.'+side,(s*.409,.461,2.48),(s*.44,.50,2.44),.031,.020,pod,'gun','pack',n=16)
# Cannon receiver, return braces and jacket are fitted to the existing barrel/cradle.
pit='Cannon_Pitch_Trunnion';mount='Cannon_Right_Cradle_Mount'
rod('R6_Cannon_Lower_Triangulated_Brace',(-.17,.21,2.80),(-.536,.16,2.985),.027,mount,'gun','cannon')
rod('R6_Cannon_Upper_Recoil_Link',(-.25,.24,2.936),(-.542,.132,3.044),.018,mount,'steel','cannon')
tube('R6_Cannon_Yaw_Lock_Collar',(-.527,.114,2.963),(-.527,.114,2.987),.081,.055,mount,'gun','cannon',n=24)
plate('R6_Cannon_Receiver_Side_Armor',[(-.676,-.093,3.024),(-.676,.113,3.012),(-.673,.198,3.076),(-.664,.163,3.154),(-.672,-.080,3.168)],(.026,0,0),pit,'navy','cannon',bevel=.002)
plate('R6_Cannon_Receiver_Service_Panel',[(-.679,-.026,3.037),(-.679,.116,3.033),(-.677,.138,3.112),(-.677,-.042,3.119)],(.007,0,0),pit,'panel','cannon',bevel=.0013)
box('R6_Cannon_Top_Recoil_Rail',(-.530,-.04,3.203),(.056,.375,.027),pit,'gun','cannon')
tube('R6_Cannon_Muzzle_Reinforcement',(-.5297,-.494,3.107),(-.5297,-.508,3.107),.098,.081,pit,'gun','cannon',n=16)
for j in range(6):
 t=j*math.tau/6;x=-.5297+math.cos(t)*.083;z=3.106+math.sin(t)*.083
 rod('R6_Barrel_Thermal_Rib_'+str(j),(x,-.165,z),(x,-.322,z+.006),.007,pit,'gun','cannon',n=8)
# Anti-ship blade: a continuous structural blade plus a separate one-sided energy edge.
W='AntiShip_Blade_Display_Root'
hull('R6_Blade_Reinforced_Mechanical_Spine',[(-1.079,-.681,2.151),(-1.026,-.672,2.131),(-1.014,-.576,2.106),(-1.060,-.529,2.093),(-1.020,-.612,.704),(-1.046,-.654,.652),(-1.077,-.670,.879),(-1.093,-.571,.821)],W,'gun','weapon',bevel=.0024)
for j,(za,zb) in enumerate(((1.62,1.986),(1.18,1.602),(.72,1.162))):
 for back in (False,True):
  y=-.703 if not back else -.516;d=.026 if not back else -.020
  plate('R6_Blade_Load_Panel_'+str(j)+('_Rear' if back else '_Front'),[(-1.253,y,zb-.026),(-1.153,y-.004 if not back else y+.004,zb),(-1.089,y+.027 if not back else y,zb-.097),(-1.108,y+.010 if not back else y,za+.014),(-1.208,y-.004 if not back else y,za),(-1.256,y,za+.057)],(0,d,0),W,'navy','weapon',bevel=.0015)
plate('R6_Blade_Generator_Receiver',[(-1.271,-.713,2.154),(-1.105,-.713,2.174),(-1.038,-.69,2.101),(-1.064,-.714,1.963),(-1.146,-.722,1.923),(-1.243,-.713,1.982)],(0,.038,0),W,'gun','weapon',bevel=.0023)
plate('R6_Blade_Generator_Armor',[(-1.259,-.723,2.145),(-1.146,-.728,2.155),(-1.087,-.720,2.099),(-1.116,-.73,2.045),(-1.247,-.722,2.068)],(0,.015,0),W,'panel','weapon',bevel=.0018)
for j in range(3):
 z=1.975-j*.072
 rod('R6_Blade_Emitter_Power_Cell_'+str(j),(-1.265,-.708,z),(-1.253,-.708,z-.050),.013,W,'gun','weapon',n=12)
 plate('R6_Blade_Emitter_Window_'+str(j),[(-1.271,-.724,z-.01),(-1.26,-.724,z-.01),(-1.253,-.724,z-.036),(-1.264,-.724,z-.036)],(0,.006,0),W,'eye','weapon',bevel=.0005)
# Connected dovetail ties the receiver to the spine, not to the removable beam.
box('R6_Blade_Spine_Generator_Key',(-1.073,-.674,1.978),(.045,.103,.120),W,'steel','weapon',bevel=.002)
obj('Backpack_Structural_Mount')['mode']='STOWED'
save_phase(6,'Stowed engine caps, load struts/hinge collars, fitted nozzle heat petals and auxiliary RCS; cannon load braces, yaw collar, armored receiver, recoil rail and barrel ribs; blade reinforced spine, bilateral load panels, generator/receiver and power cells. Continuous physical blade and independently switchable single blue edge retained.')
