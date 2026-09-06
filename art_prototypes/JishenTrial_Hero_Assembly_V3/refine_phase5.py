import bpy,pathlib,sys
P=pathlib.Path(__file__).resolve().parent;sys.path.insert(0,str(P))
bpy.ops.wm.open_mainfile(filepath=str(P/'refinement'/'PHASE_04.blend'))
from refine_common import *
for s,side in ((-1,'R'),(1,'L')):
 def mir(pts):return [(s*x,y,z) for x,y,z in pts]
 arm='UpperArm.'+side;fore='Forearm.'+side;hand='Hand.'+side;shoulder='Shoulder_Armor_Floating_Pivot.'+side
 plate('Refit_Shoulder_Lower.'+side,mir([(.479,-.183,2.831),(.618,-.201,2.851),(.762,-.142,2.823),(.790,-.054,2.743),(.706,-.170,2.675),(.526,-.198,2.726)]),(0,.145,0),shoulder,'navy',replace='Shoulder_Main_Wrapped_Carapace.'+side+'_Lower',bevel=.003)
 plate('R5_Shoulder_Outer_Return.'+side,mir([(.741,-.133,2.816),(.796,-.043,2.835),(.807,.153,2.768),(.752,.162,2.694),(.777,-.039,2.706)]),(-s*.018,0,0),shoulder,'panel',bevel=.002)
 rod('R5_Shoulder_Load_Link.'+side,(s*.495,.082,2.785),(s*.626,.082,2.653),.024,arm,'gun')
 tube('R5_Shoulder_Link_Clevis.'+side,(s*.58,.083,2.692),(s*.616,.083,2.692),.035,.019,arm,'steel')
 plate('R5_Upperarm_Inside_Articulated_Plate.'+side,mir([(.554,-.093,2.640),(.604,-.118,2.654),(.664,-.098,2.493),(.630,-.147,2.438),(.597,-.149,2.513)]),(0,.023,0),arm,'panel',bevel=.002)
 # Forearm long folded ridge supports the gauntlet; the secondary panel stays smaller.
 plate('R5_Forearm_Dorsal_Swept_Ridge.'+side,mir([(.651,-.133,2.280),(.705,-.197,2.251),(.782,-.218,2.076),(.781,-.195,1.999),(.736,-.191,2.040),(.674,-.142,2.176)]),(0,.029,0),fore,'navy',bevel=.0024)
 plate('R5_Forearm_Wrist_Return.'+side,mir([(.711,-.133,1.999),(.778,-.149,2.010),(.818,-.078,1.961),(.791,-.047,1.929),(.731,-.080,1.929)]),(0,.027,0),fore,'gun',bevel=.002)
 for o in list(SC.objects):
  if o.name.startswith('Forearm_Service_Intake.'+side):bpy.data.objects.remove(o,do_unlink=True)
 # Palm remains a solid load-bearing chassis; dorsal cover is replaced by two pieces.
 x=s*.765
 plate('Refit_Hand_Dorsal.'+side,[(x-.077,-.104,1.773),(x+.065,-.108,1.783),(x+.084,-.080,1.701),(x+.066,-.101,1.674),(x-.063,-.111,1.686),(x-.080,-.087,1.727)],(0,.025,0),hand,'gun',replace='Hand_Dorsal_Armor.'+side,bevel=.0024)
 for j,dx in enumerate((-.038,.036)):
  c=x+dx
  plate('R5_Hand_Metacarpal_Armor_'+str(j)+'.'+side,[(c-.031,-.116,1.760),(c+.029,-.116,1.762),(c+.031,-.113,1.703),(c-.024,-.117,1.696)],(0,.012,0),hand,'navy',bevel=.0015)
  rod('R5_Palm_Tendon_'+str(j)+'.'+side,(c,-.062,1.794),(c,-.072,1.703),.006,hand,'steel',n=12)
 tube('R5_Wrist_Protected_Coupler.'+side,(s*.753,-.026,1.829),(s*.753,-.026,1.796),.052,.032,hand,'gun')
 # Enlarge individual phalanx body thickness; keep every original joint and pivot.
 for j in range(4):
  for k in range(3):
   o=obj(f'Finger_{j}_Phalanx_{k}.{side}');vs=[o.matrix_world@v.co for v in o.data.vertices];cen=sum(vs,Vector())/len(vs)
   affine(o,center=cen,scale=(1.06,1.13,1.02))
   for m in o.modifiers:
    if m.type=='BEVEL':m.width=.0015;m.segments=3
  joint=obj(f'Finger_{j}_Joint_0.{side}');c=joint.matrix_world.translation.copy()
  plate(f'R5_MCP_Knuckle_Armor_{j}.{side}',[(c.x-.014,-.024,c.z-.009),(c.x+.014,-.024,c.z-.009),(c.x+.012,-.021,c.z-.042),(c.x-.012,-.021,c.z-.042)],(0,.009,0),joint,'panel',bevel=.0012)
  tube(f'R5_MCP_Pin_Retainer_{j}.{side}',(c.x-.021,c.y,c.z),(c.x-.018,c.y,c.z),.013,.006,joint,'gun',n=12)
 # Remove old hand cover bolts which no longer follow the remeshed dorsal plate.
 for o in list(SC.objects):
  if o.name.startswith('Hand_Dorsal_Fastener_') and '.'+side in o.name:bpy.data.objects.remove(o,do_unlink=True)
save_phase(5,'Wrapped shoulder lower shields, outer returns and real link clevises; layered upperarm/gauntlet armor; wrist coupler; split metacarpal armor, palm tendons and enlarged 3-segment mechanical phalanges with MCP caps/pin retainers. Existing finger pivots retained.')
