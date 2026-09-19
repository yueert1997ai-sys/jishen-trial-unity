import bpy,bmesh,math,json,ast,shutil
from pathlib import Path
from mathutils import Vector,Matrix
OUT=Path(__file__).resolve().parents[1]
shutil.copy2(OUT/'VALKYR_PARALLEL_R01.blend',OUT/'work/VALKYR_R01_BLOCKOUT.blend')
bpy.ops.wm.open_mainfile(filepath=str(OUT/'VALKYR_PARALLEL_R01.blend'))
sc=bpy.context.scene;full=bpy.data.collections['VALKYR_R01_ASSET'];root=bpy.data.objects['VALKYR_PARALLEL_R01_ROOT']
rig=bpy.data.collections['01_ASSEMBLY_PIVOTS'];core=bpy.data.collections['02_INNER_FRAME'];body=bpy.data.collections['03_BODY_ARMOUR'];hardware=bpy.data.collections['04_MECHANICAL_HARDWARE'];head=bpy.data.collections['05_HEAD_SHELLS'];optics=bpy.data.collections['06_OPTICS'];pack=bpy.data.collections['07_FLIGHT_PACK'];weapon=bpy.data.collections['08_OPTIONAL_SWORD'];studio=bpy.data.collections['90_REVIEW_STUDIO']
pelvis=bpy.data.objects['HIP_MOUNT'];torso=bpy.data.objects['TORSO_MOUNT'];neck=bpy.data.objects['HEAD_MOUNT'];back=bpy.data.objects['BACKPACK_MOUNT']
blue=bpy.data.materials['Valkyr blue ceramic alloy'];navy=bpy.data.materials['Deep navy under-armour'];edge=bpy.data.materials.get('Blue chamfer highlight',blue);white=bpy.data.materials['Cool white ceramic'];dark=bpy.data.materials['Graphite inner chassis'];steel=bpy.data.materials['Machined titanium'];black=bpy.data.materials['Cavity and joint seals'];gold=bpy.data.materials['Satin gold accents'];cyan=bpy.data.materials['Recessed cyan optics'];labelmat=bpy.data.materials['Technical markings']
tree=ast.parse((OUT/'work/build_r01.py').read_text(encoding='utf-8-sig'));exec(compile(ast.Module(body=[n for n in tree.body if isinstance(n,ast.FunctionDef)],type_ignores=[]),'r01_pure_geometry_helpers','exec'))
def remove(n):
 o=bpy.data.objects.get(n)
 if o:bpy.data.objects.remove(o,do_unlink=True)
def color(m,h):
 cc=[int(h[i:i+2],16)/255 for i in (0,2,4)];cc=[c/12.92 if c<=.04045 else ((c+.055)/1.055)**2.4 for c in cc];m.diffuse_color=(*cc,1);next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED').inputs['Base Color'].default_value=(*cc,1)
color(blue,'244F96');color(white,'B6C2D1');color(gold,'BF9446')
bs=next(n for n in cyan.node_tree.nodes if n.type=='BSDF_PRINCIPLED');bs.inputs['Emission Strength'].default_value=.75
for n,mult in [('Large softbox',.70),('Warm frontal fill',.65),('Back edge',.70)]:bpy.data.objects[n].data.energy*=mult
# Close the crown gap with a continuous helmet spine, leaving only purposeful vents.
plate('Crown continuous centre spine',[(-.11,-.14,6.30),(.11,-.14,6.30),(.13,.12,6.30),(.105,.31,6.13),(-.105,.31,6.13),(-.13,.12,6.30)],.055,blue,head,neck,.005,.008)
for s in (-1,1):
 lab='L' if s>0 else 'R'
 def mp(q):return [(s*x,z) for x,z in q]
 for key in ('Eye chamber rim.','Recessed angular cyan lens.','White eye brow knife.') :remove(key+lab)
 # Explicit four-loop eye socket: outer mouth, inner throat and closed wall return.
 outer=mp([(.048,5.86),(.292,5.96),(.283,5.885),(.111,5.79)])
 inner=mp([(.077,5.851),(.265,5.929),(.253,5.897),(.125,5.829)])
 v=[]
 for pts,y in [(outer,-.405),(inner,-.408),(inner,-.355),(outer,-.350)]:v.extend([(x,y,z) for x,z in pts])
 f=[]
 for j in range(4):
  for i in range(4):f.append((j*4+i,j*4+(i+1)%4,((j+1)%4)*4+(i+1)%4,((j+1)%4)*4+i))
 mesh('Hollow eye socket bezel.'+lab,v,f,navy,head,neck,.002)
 panel('Inset precision cyan eye.'+lab,inner,-.389,.016,cyan,optics,neck,.001,0)
 panel('Silver brow narrow blade.'+lab,mp([(.035,5.883),(.30,6.015),(.296,5.973),(.066,5.870)]),-.416,.045,white,head,neck,.002,.005)
 # Multi-plane cheek ear shell and overhanging crown blade follow the side reference.
 plate('Upper helmet swept brow shell.'+lab,[(s*.12,-.28,6.21),(s*.28,-.19,6.29),(s*.39,.08,6.20),(s*.25,-.20,6.03)],.032,blue,head,neck,.003)
 plate('Temple second swept scale.'+lab,[(s*.33,-.12,6.01),(s*.43,.11,6.03),(s*.40,.20,5.86),(s*.29,-.10,5.81)],.028,navy,head,neck,.003)
 plate('Cheek third swept scale.'+lab,[(s*.32,-.12,5.80),(s*.41,.13,5.83),(s*.36,.20,5.65),(s*.26,-.14,5.52)],.026,blue,head,neck,.003)
 plate('White cheek narrow machined inset.'+lab,[(s*.22,-.342,5.74),(s*.265,-.293,5.73),(s*.22,-.284,5.50),(s*.155,-.359,5.43)],.018,steel,head,neck,.002)
 for z in (5.67,5.73,5.79):box('Head cheek recessed grille.'+lab,(s*.305,-.16,z),(.027,.025,.016),black,head,neck,.001)
 for x,y,z in ((.235,-.27,6.16),(.34,-.13,5.81)):
  cylinder('Head flush screw.'+lab,(s*x,y,z),(s*x,y-.011,z),.007,steel,hardware,neck,12)
remove('Forehead inner sensor channel');remove('Forehead cyan sensor')
panel('Forehead full blue sensor housing',[(-.12,6.19),(.12,6.19),(.10,5.99),(0,5.89),(-.10,5.99)],-.321,.09,blue,head,neck,.004,.01)
panel('Forehead machined slot',[(-.035,6.115),(.035,6.115),(.035,5.99),(0,5.962),(-.035,5.99)],-.352,.031,black,head,neck,.002,0)
panel('Forehead inset narrow cyan sensor',[(-.021,6.10),(.021,6.10),(.020,6.001),(0,5.98),(-.020,6.001)],-.357,.015,cyan,optics,neck,.001,0)
remove('Central folded white mask')
mesh('Face mask explicit centre fold',[(-.11,-.40,5.78),(0,-.477,5.79),(.11,-.40,5.78),(-.097,-.411,5.58),(0,-.484,5.44),(.097,-.411,5.58),(-.11,-.35,5.78),(.11,-.35,5.78),(.097,-.36,5.58),(0,-.41,5.44),(-.097,-.36,5.58)],[(0,3,4,1),(1,4,5,2),(0,1,2,7,6),(2,5,8,7),(5,4,9,8),(4,3,10,9),(3,0,6,10),(6,7,8,9,10)],white,head,neck,.0025)
# Reduce the head's overall bulk around the mount while preserving meaningful antenna reach.
bpy.context.view_layer.update()
for ob in list(full.all_objects):
 if ob.type=='MESH' and (ob.parent==neck):
  for vert in ob.data.vertices:
   wp=ob.matrix_world@vert.co;wp.x*=.92;wp.z=5.42+(wp.z-5.42)*.89;vert.co=ob.matrix_world.inverted()@wp
# Actual cut service seams, never floating decorative black lines.
def groove(n,pts,yfront,yback,width=.007):
 target=bpy.data.objects[n]
 for i in range(len(pts)-1):
  a=Vector(pts[i]);b=Vector(pts[i+1]);d=b-a
  if d.length<1e-5:continue
  nn=Vector((-d.y,d.x)).normalized()*width/2
  cut=panel('R01_TEMP_CUT',[a+nn,b+nn,b-nn,a-nn],yfront,yback-yfront,black,hardware,None,0,0)
  md=target.modifiers.new('Recessed panel machining','BOOLEAN');md.operation='DIFFERENCE';md.solver='EXACT';md.object=cut;bpy.context.view_layer.objects.active=target;bpy.ops.object.modifier_move_to_index(modifier=md.name,index=0);bpy.ops.object.modifier_apply(modifier=md.name);bpy.data.objects.remove(cut,do_unlink=True)
for s in (-1,1):
 lab='L' if s>0 else 'R'
 def mp(q):return [(s*x,z) for x,z in q]
 arm=bpy.data.objects['SHOULDER_PIVOT.'+lab];fore=bpy.data.objects['ELBOW_PIVOT.'+lab];thigh=bpy.data.objects['THIGH_MOUNT.'+lab];shin=bpy.data.objects['KNEE_PIVOT.'+lab];foot=bpy.data.objects['ANKLE_PIVOT.'+lab];pod=bpy.data.objects['VECTOR_THRUSTER.'+lab]
 plate('Clavicle shaped cap.'+lab,[(s*.24,-.19,5.26),(s*.78,-.18,5.29),(s*.88,.06,5.20),(s*.49,.16,5.16)],.045,blue,p=torso)
 plate('Shoulder upper dark service hatch.'+lab,[(s*1.10,-.395,5.29),(s*1.34,-.32,5.39),(s*1.38,-.37,5.26),(s*1.16,-.441,5.17)],.022,navy,p=arm,b=.003)
 plate('Pauldron lower overlapping facet.'+lab,[(s*1.11,-.468,5.07),(s*1.52,-.477,5.17),(s*1.48,-.512,4.99),(s*1.29,-.538,4.90)],.025,blue,p=arm,b=.003)
 groove('Main delta pauldron.'+lab,mp([(1.12,5.14),(1.18,5.07),(1.48,5.22),(1.53,5.37)]),-.61,-.38,.006)
 groove('Forearm long blue front plate.'+lab,mp([(1.28,3.95),(1.35,3.82),(1.50,3.85),(1.60,3.55)]),-.465,-.34,.007)
 groove('Long quadriceps blue shield.'+lab,mp([(.42,2.91),(.48,2.63),(.54,2.43),(.67,2.29)]),-.414,-.275,.006)
 groove('Shin long blue keel.'+lab,mp([(.57,1.75),(.67,1.49),(.72,1.02),(.78,.75)]),-.486,-.35,.006)
 # Inner white thighs/calf accents are narrower than their blue outer armour.
 bpy.data.objects['Thigh structural ceramic shell.'+lab].data.materials[0]=navy
 bpy.data.objects['Calf white closed shell.'+lab].data.materials[0]=navy
 plate('Thigh slim white inner armour.'+lab,[(s*.34,-.12,3.02),(s*.39,-.26,2.94),(s*.43,-.28,2.53),(s*.49,-.12,2.34),(s*.37,-.01,2.49)],.04,white,p=thigh)
 plate('Thigh lateral ceramic return.'+lab,[(s*.82,.02,3.03),(s*.88,-.03,2.92),(s*.92,-.06,2.63),(s*.80,-.13,2.39),(s*.79,.02,2.52)],.06,white,p=thigh)
 plate('Calf inner ceramic panel.'+lab,[(s*.50,-.07,1.76),(s*.49,-.20,1.63),(s*.51,-.28,1.23),(s*.65,-.24,.81),(s*.63,-.08,1.07)],.055,white,p=shin)
 plate('Calf outside ceramic shroud.'+lab,[(s*.94,-.14,1.70),(s*1.025,-.02,1.59),(s*1.032,-.02,1.28),(s*.93,-.16,.91),(s*.91,-.23,1.13)],.07,white,p=shin)
 # Separable knee/ankle carriers, paired piston rods and guard ribs.
 for dx in (-.095,.095):
  cylinder('Forearm rear actuator rod.'+lab,(s*1.37+dx,.20,4.03),(s*1.49+dx,.13,3.61),.019,steel,hardware,fore)
  cylinder('Shin service guide.'+lab,(s*.74+dx,.405,1.65),(s*.80+dx,.325,.90),.022,steel,hardware,shin)
 cylinder('Elbow second bearing.'+lab,(s*1.16,-.04,4.03),(s*1.49,-.04,4.03),.08,black,hardware,fore)
 for j in range(3):
  box('Knee articulated underplate.%s.%d'%(lab,j),(s*.67,-.20,1.87-j*.045),(.24,.11,.027),steel,hardware,shin,.003)
  box('Ankle front bellows.%s.%d'%(lab,j),(s*.80,-.24,.53+j*.052),(.28,.06,.028),black,hardware,foot,.003)
 # Actual foot overlay follows the inclined instep, with a narrow centre groove.
 xc=s*.81
 for j in range(2):
  yy=-.50+j*.19;zz=.38+j*.076
  plate('Instep floating tile.%s.%d'%(lab,j),[(xc-.16,yy-.06,zz-.025),(xc+.16,yy-.06,zz-.025),(xc+.14,yy+.06,zz+.03),(xc-.14,yy+.06,zz+.03)],.024,navy if j==0 else blue,p=foot,b=.004)
 for dx in (-.18,.18):
  cylinder('Toe pivot cap.'+lab,(xc+dx,-.53,.315),(xc+dx,-.54,.33),.025,dark,hardware,foot,16)
 # Backpack upper intake has a recessed bottom and built-in slats instead of an empty cup.
 box('Thruster top intake bottom.'+lab,(s*.47,.61,5.26),(.38,.38,.035),black,pack,pod,.005)
 for j in range(6):box('Thruster top intake grille.%s.%d'%(lab,j),(s*.47,.455+j*.060,5.325),(.365,.021,.036),steel,pack,pod,.003)
 for j in range(3):
  box('Forearm recessed vent.%s.%d'%(lab,j),(s*1.535,-.392,3.68-j*.045),(.12,.012,.012),black,hardware,fore,.001)
  box('Quadriceps service slot.%s.%d'%(lab,j),(s*.62,-.415,2.74-j*.024),(.08,.01,.009),black,hardware,thigh,.001)
 for x,z in ((1.22,5.28),(1.43,5.37)):
  bolt('Shoulder service recessed pin.'+lab,s*x,-.44,z,arm)
 text('Shoulder maintenance stencil.'+lab,'CAUTION / 07',(s*1.28,-.57,4.99),.022,p=arm)
 text('Forearm maintenance stencil.'+lab,'ACTUATOR',(s*1.48,-.437,3.50),.022,p=fore)
 text('Calf maintenance stencil.'+lab,'VKR / 01',(s*.77,-.483,1.24),.026,p=shin)
# Additional torso internal pressure hoses and machined chest slots.
for s in (-1,1):
 lab='L' if s>0 else 'R'
 pipe('Abdominal braided service line.'+lab,[(s*.27,.08,4.39),(s*.36,.09,4.18),(s*.30,.08,3.98),(s*.26,.08,3.78)],.022,black)
 for j in range(4):
  cylinder('Waist hose collar.'+lab,(s*.29,.08,3.83+j*.043),(s*.29,.08,3.848+j*.043),.029,steel,hardware,torso,12)
# Front sightline: better connection to neck, no exposed cylindrical stalk.
neck.location.z-=.07
# Viewport master setup always shows the complete assembled mech.
sc.camera=bpy.data.objects['R01_ThreeQuarter'];sc.render.resolution_x=1500;sc.render.resolution_y=1700
bpy.context.view_layer.update();bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'VALKYR_PARALLEL_R01.blend'),compress=True)
for n in ['01_THREE_QUARTER','02_FRONT','05_HEAD']:
 camname={'01_THREE_QUARTER':'R01_ThreeQuarter','02_FRONT':'R01_Front','05_HEAD':'R01_Head'}[n];sc.camera=bpy.data.objects[camname];sc.render.filepath=str(OUT/'renders'/f'{n}.png');bpy.ops.render.render(write_still=True)
print('R01_DETAIL_PASS_COMPLETE',len([o for o in full.all_objects if o.type=='MESH']),flush=True)

