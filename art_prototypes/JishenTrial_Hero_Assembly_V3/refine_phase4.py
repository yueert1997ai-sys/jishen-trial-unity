import bpy,pathlib,sys
P=pathlib.Path(__file__).resolve().parent;sys.path.insert(0,str(P))
bpy.ops.wm.open_mainfile(filepath=str(P/'refinement'/'PHASE_03.blend'))
from refine_common import *
for s,side in ((-1,'R'),(1,'L')):
 def mir(pts):return [(s*x,y,z) for x,y,z in pts]
 sh='Shin.'+side;ft='Foot.'+side;th='Thigh.'+side
 # Main calf shell becomes a recessed mechanical housing; armor floats over it.
 for suffix in ('_Upper','_Lower'):
  o=obj('Calf_Deep_Main_Nacelle.'+side+suffix);o.data.materials.clear();o.data.materials.append(M['gun'])
  affine(o,center=(s*.35,.07,.73),scale=(.90,.88,1))
 # Rebuild existing forward shells as two folded plates, with a visible central rail.
 plate('Refit_Shin_Upper.'+side,mir([(.233,-.156,1.086),(.318,-.231,1.107),(.423,-.185,1.049),(.451,-.161,.878),(.382,-.221,.773),(.296,-.234,.787),(.242,-.176,.907)]),(0,.037,0),sh,'navy',replace='Shin_Long_Forward_Keel.'+side+'_Upper',bevel=.0024)
 plate('Refit_Shin_Lower.'+side,mir([(.287,-.233,.751),(.390,-.220,.756),(.408,-.145,.642),(.389,-.145,.404),(.346,-.190,.356),(.302,-.200,.419),(.276,-.173,.616)]),(0,.031,0),sh,'panel',replace='Shin_Long_Forward_Keel.'+side+'_Lower',bevel=.002)
 # Outer calf shield follows the muscle-like nacelle volume and has a rear return.
 plate('R4_Calf_Outer_Swept_Shroud.'+side,mir([(.407,-.116,1.039),(.523,-.043,.988),(.584,.104,.858),(.555,.170,.683),(.484,.105,.574),(.432,-.093,.667),(.454,-.144,.824)]),(-s*.026,.029,0),sh,'navy',bevel=.003)
 plate('R4_Calf_Outer_Secondary_Return.'+side,mir([(.458,-.163,.825),(.542,-.069,.877),(.575,.081,.806),(.551,.114,.725),(.475,-.083,.703)]),(0,.025,0),sh,'panel',bevel=.002)
 intake('R4_Calf_Outer_Heat_Exchanger.'+side,'R4_Calf_Outer_Secondary_Return.'+side,s*.523,.806,.065,.074,sh,bars=4)
 plate('R4_Calf_Inner_Service_Shield.'+side,mir([(.211,-.081,1.004),(.167,.042,.932),(.181,.131,.741),(.239,.048,.620),(.267,-.062,.735),(.238,-.104,.910)]),(s*.023,0,0),sh,'panel',bevel=.002)
 intake('R4_Shin_Drive_Service.'+side,'Shin_Long_Forward_Keel.'+side+'_Upper',s*.343,.92,.055,.092,sh,bars=3)
 # Two exposed guides sit behind the separation between the upper/lower shin armor.
 for dx in (-.022,.022):
  rod('R4_Shin_Linear_Guide_'+str(dx)+'.'+side,(s*(.341+dx),-.205,.706),(s*(.341+dx),-.205,.801),.006,sh,'steel',n=12)
 plate('R4_Knee_Tibial_Yoke.'+side,mir([(.254,-.108,1.155),(.307,-.147,1.217),(.395,-.087,1.143),(.378,-.077,1.087),(.315,-.175,1.119)]),(0,.027,0),sh,'gun',bevel=.002)
 # Long-thigh load rail and mounting clevis retain negative space around the knee.
 rod('R4_Femur_Exposed_Rail.'+side,(s*.219,-.088,1.82),(s*.269,-.100,1.39),.013,th,'steel')
 plate('R4_Thigh_Upper_Folded_Plate.'+side,mir([(.179,-.132,1.883),(.242,-.151,1.901),(.31,-.125,1.737),(.281,-.148,1.581),(.236,-.166,1.667)]),(0,.020,0),th,'panel',bevel=.0017)
 # Foot: tapered faceted forefoot, midfoot bridge, split armor and heel load path.
 x=s*.36075
 hull('Refit_Midfoot.'+side,[(x+dx,y,z) for dx,y,z in [(-.136,-.27,.06),(.136,-.27,.06),(-.135,.115,.05),(.135,.115,.05),(-.095,-.228,.156),(.095,-.228,.156),(-.09,-.04,.257),(.09,-.04,.257),(-.104,.091,.206),(.104,.091,.206)]],ft,'gun',replace='Foot_Armored_Instep.'+side,bevel=.002)
 for j,dx in enumerate((-.077,.077)):
  c=x+dx
  pts=[(c-.061,-.408,.037),(c+.061,-.408,.037),(c-.073,-.187,.062),(c+.073,-.187,.062),(c-.046,-.397,.103),(c+.046,-.397,.103),(c-.059,-.287,.159),(c+.059,-.287,.159),(c-.053,-.195,.185),(c+.053,-.195,.185)]
  hull('Refit_Forefoot.'+side+str(j),pts,ft,'navy',replace=f'Foot_Split_Heavy_Toe_{j}.{side}_Upper',bevel=.0025)
  plate('R4_Midfoot_Split_Armor_'+str(j)+'.'+side,[(c-.055,-.266,.172),(c+.052,-.266,.172),(c+.043,-.083,.267),(c-.035,-.057,.252)],(0,.010,-.020),ft,'panel',bevel=.0016)
  rod('R4_Forefoot_Hinge_'+str(j)+'.'+side,(c-.052,-.206,.096),(c+.052,-.206,.096),.017,ft,'gun',n=16)
 # Ankle bridge follows the bearing instead of being a broad white blank plate.
 plate('Refit_Ankle_Bridge.'+side,[(x-.109,-.093,.375),(x-.018,-.141,.403),(x+.103,-.102,.351),(x+.096,-.11,.264),(x+.031,-.152,.226),(x-.073,-.137,.264)],(0,.032,0),ft,'white',replace='Ankle_Armored_Bridge.'+side,bevel=.002)
 for dx in (-.077,.077):
  rod('R4_Ankle_Fork_'+str(dx)+'.'+side,(x+dx,-.03,.265),(x+dx,-.11,.147),.014,ft,'gun')
 plate('R4_Heel_Armored_Return.'+side,[(x-.121,.206,.071),(x+.121,.206,.071),(x+.094,.143,.205),(x-.094,.143,.205)],(0,.027,0),ft,'navy',bevel=.002)
 # Remove obsolete superimposed covers whose old positions no longer follow the new shells.
 for prefix in ('Shin_Front_Access_Recess.','Calf_Forward_Upper_Service_Panel.','Calf_Outer_Visible_Cooling.','Calf_Outer_Overlapping_Winglet.','Foot_Instep_Ceramic_Overlay.','Foot_Forward_Dorsal_Ceramic.','Foot_Toe_Protective_Cap_'):
  for o in list(SC.objects):
   if o.name.startswith(prefix) and ('.'+side in o.name):bpy.data.objects.remove(o,do_unlink=True)
save_phase(4,'Recessed gunmetal calf mechanics; two new folded shin plates, asymmetrical outer/inner calf shields, service cavity, exposed linear guides and knee yoke; femur rail; remodeled split faceted forefeet, midfoot armor, ankle fork/bridge and heel return. Existing rear thrusters and dampers retained.')
