from mass_ops import *
for s,side in ((1,'L'),(-1,'R')):
 group('08 Backpack and propulsion','Backpack_Outer_Fold_Pivot.'+side)
 mi=lambda p:(s*p[0],p[1],p[2])
 # Folded radiator channels and reinforced hinges make the vanes a supported mechanism.
 hull('Mass_Backpack_Vane_Load_Spar.'+side,[mi(p) for p in ((32,49,326),(39,51,331),(53,67,283),(57,72,251),(52,70,254),(44,60,289))],'frame',.35)
 plate('Mass_Backpack_Vane_Radiator_Well.'+side,[mi(p) for p in ((39,60,318),(46,64,310),(56,74,270),(50,73,278))],(0,-1.2,0),'black',.22)
 for j in range(6):
  t=j/5;x=40+t*10;z=312-t*32;y=62+t*9
  strip('Mass_Backpack_Radiator_Louver_%d.'%j+side,[mi((x,y+.3,z)),mi((x+5,y+2.5,z-4))],1.2,'titanium',(0,-.8,0))
 ram('Mass_Backpack_Vane_Deployment_Actuator.'+side,mi((35,51,327)),mi((46,66,286)),2.0)
 hose('Mass_Backpack_Vector_Feed.'+side,[mi(p) for p in ((25,43,330),(28,49,325),(31,53,311),(34,51,301))],1.1)
 group('08 Backpack and propulsion','Backpack_Main_Deploy_Hinge.'+side)
 plate('Mass_Backpack_Upper_Fin_Folded_Return.'+side,[mi(p) for p in ((28,51,353),(32,52,360),(51,59,382),(44,60,362),(33,55,347))],(0,-3,0),'navy',.25)
 group('09 Shoulder cannon','Cannon_Right_Cradle_Mount')
# Reinforce the existing cannon mount without changing its aim or barrel.
for xx in (-43,-29):
 ram('Mass_Cannon_Recoil_Mount_%d'%xx,(xx,25,344),(xx,31,360),1.65)
