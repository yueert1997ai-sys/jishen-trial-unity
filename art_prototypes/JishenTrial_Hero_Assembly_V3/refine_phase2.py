import bpy,pathlib,sys
P=pathlib.Path(__file__).resolve().parent;sys.path.insert(0,str(P))
bpy.ops.wm.open_mainfile(filepath=str(P/'refinement'/'PHASE_01.blend'))
from refine_common import *
H=obj('HEAD_V2_ROOT')
for o in H.children:
 if o.type=='MESH' and o.name[:2].isdigit() and int(o.name[:2])<=12:
  deform(o,lambda v:(v.x,v.y*(.77 if v.y<0 else .78),3.0+(v.z-3.0)*.88))
for s,side in ((-1,'R'),(1,'L')):
 def mir(pts):return [(s*x,y,z) for x,y,z in pts]
 plate('Refit_Optical_Armor_Brow.'+side,mir([(.009,-.151,3.153),(.085,-.149,3.166),(.132,-.089,3.180),(.140,-.068,3.166),(.093,-.140,3.154),(.022,-.153,3.142)]),(0,.013,.002),H,'white',replace='06_Outer_Armor_Brow.'+side,bevel=.0011)
 # Two discrete rectangular mechanical lenses, recessed behind the bridge.
 plate('Refit_Short_Optic.'+side,mir([(.031,-.149,3.136),(.075,-.149,3.138),(.075,-.149,3.131),(.031,-.149,3.129)]),(0,.003,0),H,'eye',replace='08_Inner_Optic_Blue_Sensor.'+side,bevel=.0004)
 box('R2_Optical_Mount_Block.'+side,(s*.053,-.128,3.134),(.063,.038,.026),H,'dark',bevel=.001)
 for z in (3.123,3.146):box('R2_Optical_Recess_Lip_'+str(z)+'.'+side,(s*.053,-.150,z),(.062,.006,.004),H,'gun',bevel=.0005)
 plate('Refit_Cheek_Mount.'+side,mir([(.080,-.112,3.148),(.124,-.064,3.150),(.123,-.060,3.080),(.079,-.120,3.015),(.060,-.143,3.024),(.080,-.124,3.079)]),(0,.024,0),H,'panel',replace='10_Outer_Cheek_Armor.'+side,bevel=.0018)
 rod('R2_Cheek_Keyed_Bearing.'+side,(s*.105,-.074,3.103),(s*.129,-.074,3.103),.018,H,'gun',n=12)
 rod('R2_Cheek_Mount_Pin.'+side,(s*.129,-.074,3.103),(s*.132,-.074,3.103),.006,H,'steel',n=6)
 plate('R2_Neck_Lateral_Load_Fork.'+side,mir([(.027,.025,2.959),(.049,.025,2.971),(.054,.018,3.033),(.034,.010,3.038)]),(0,.025,0),H,'gun',col='frame',bevel=.0012)
plate('Refit_Flat_Mask',[(-.047,-.150,3.116),(.047,-.150,3.116),(.054,-.147,3.087),(.028,-.160,3.040),(0,-.163,3.030),(-.028,-.160,3.040),(-.054,-.147,3.087)],(0,.027,0),H,'gun',replace='09_Face_Mask_Independent_Faceted_Shield',bevel=.0015)
bpy.context.view_layer.update()
target=obj('02_Forehead_Armored_Wedge');ev=target.evaluated_get(bpy.context.evaluated_depsgraph_get());inv=target.matrix_world.inverted()
def fit(points,offset):
 out=[]
 for x,z in points:
  ok,p,n,i=ev.ray_cast(inv@Vector((x,-2,z)),inv.to_3x3()@Vector((0,1,0)))
  assert ok,(x,z)
  w=target.matrix_world@p;out.append((x,w.y-offset,z))
 return out
plate('R2_Forehead_Recess',fit([(-.012,3.189),(.012,3.189),(.013,3.235),(-.013,3.235)],.0015),(0,.006,0),H,'dark',bevel=.0005)
plate('R2_Forehead_Range_Optic',fit([(-.005,3.195),(.005,3.195),(.005,3.211),(-.005,3.211)],.002),(0,.003,0),H,'eye',bevel=.0003)
save_phase(2,'Retained original head parts. Depth compressed 23% front/22% rear, crown height compressed 12%; brow remeshed into short inset optical bridge; flat face shield; keyed cheek bearings; twin neck supports; restrained central optic.')
