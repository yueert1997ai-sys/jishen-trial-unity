import bpy,pathlib,sys
P=pathlib.Path(__file__).resolve().parent;sys.path.insert(0,str(P))
bpy.ops.wm.open_mainfile(filepath=str(P/'JishenTrial_ASSEMBLED_MASTER.blend'))
from refine_common import *
# Existing meshes and boolean cutters undergo the same regional surgery.
for o in list(SC.objects):
 if o.type not in ('MESH','CURVE'):continue
 pn=o.parent.name if o.parent else ''
 if pn=='Thorax' and not o.name[:2].isdigit():
  affine(o,center=(0,.02,2.65),scale=(1.12,1.23,1.025))
 if pn=='Waist':affine(o,center=(0,0,2.2),scale=(.91,1.02,1))
 for s,side in ((-1,'R'),(1,'L')):
  if pn=='Thigh.'+side:
   affine(o,center=(s*.265,.01,2.015),scale=(.93,1,1.055))
  if pn=='Shin.'+side:
   affine(o,center=(s*.35,.025,.27),scale=(1.06,1.05,.953))
   if o.name.startswith('Calf_Deep_Main_Nacelle'):
    deform(o,lambda v:Vector((s*.35+(v.x-s*.35)*(.78+.16*max(0,min(1,(v.z-.4)/.7))),v.y,v.z)))
  if pn=='Foot.'+side:
   affine(o,center=(s*.36075,.02,0),scale=(1.13,1.03,1.10))
   deform(o,lambda v:Vector((s*.36075+(v.x-s*.36075)*(1-.13*max(0,min(1,(-v.y-.18)/.23))),v.y,v.z)))
# Move complete arm/shoulder assemblies laterally without deforming their joints.
for s,side in ((-1,'R'),(1,'L')):
 for n in ('UpperArm.','Shoulder_Armor_Floating_Pivot.'):
  o=obj(n+side);o.location.x+=s*.036
pack=obj('Backpack_Structural_Mount');cannon=obj('Cannon_Right_Cradle_Mount');cm=cannon.matrix_world.copy()
pack.scale=(.96,.87,.90);pack.location.y-=.025
bpy.context.view_layer.update();cannon.matrix_world=cm;cannon.location.z-=.035
# Weapon length becomes approximately 70 percent of the 3.35m crown height.
for o in obj('AntiShip_Blade_Display_Root').children_recursive:
 if o.type in ('MESH','CURVE'):affine(o,center=(-1.17,-.60,.22),scale=(1.13,1.12,1.04))
save_phase(1,'Chest +12% width/+23% depth; waist -9%; femur +5.5%; calf taper and +6% outer volume; foot wider with tapered toes; folded pack depth -13%, height -10%; cannon seated lower; blade 1.04 length / 1.13 width. No new geometry.')
