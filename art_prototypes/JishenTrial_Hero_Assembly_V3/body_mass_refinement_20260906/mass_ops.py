import pathlib,sys,bpy,bmesh,math,json
from mathutils import Vector,Matrix
_MASS_WORK=pathlib.Path(__file__).resolve().parent;_LEGACY=_MASS_WORK.parent/'body_reference_rebuild_20260906';sys.path.insert(0,str(_LEGACY))
import body_common as c
from body_common import *
WORK=_MASS_WORK;W=WORK;SOURCE=WORK/'SOURCE_BEFORE_MASS_PASS.blend'
def remove(prefixes):
 for o in list(SC.objects):
  if o.type=='MESH' and o.name.startswith(tuple(prefixes)):bpy.data.objects.remove(o,do_unlink=True)
def deform(o,fn):
 mw=o.matrix_world.copy();inv=mw.inverted()
 for v in o.data.vertices:v.co=inv@U(fn((mw@v.co)/S))
 bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free();o.data.update()
def limb_basis(a,b,s):
 a=Vector(a);d=(Vector(b)-a).normalized();u=Vector((s,0,0));u=(u-d*u.dot(d)).normalized();v=d.cross(u)
 if v.y<0:v=-v
 return lambda p:tuple(a+u*p[0]+v*p[1]+d*p[2])
def joint_frame(a,b,side):
 s=1 if side=='L' else -1
 return limb_basis(bpy.data.objects[a+'.'+side].matrix_world.translation/S,bpy.data.objects[b+'.'+side].matrix_world.translation/S,s)
def loft(name,rings,mat='blue',bevel=.35,f=lambda p:p):
 n=len(rings[0]);assert all(len(r)==n for r in rings);vs=[f(p) for r in rings for p in r]
 fs=[tuple(range(n-1,-1,-1)),tuple(range((len(rings)-1)*n,len(rings)*n))]
 for j in range(len(rings)-1):
  for k in range(n):fs.append((j*n+k,j*n+(k+1)%n,(j+1)*n+(k+1)%n,(j+1)*n+k))
 return mesh(name,vs,fs,mat,bevel)
def oct_ring(t,w,front,back):
 return [(-w*.67,front,t),(w*.67,front,t),(w,front*.65,t),(w,back*.65,t),(w*.65,back,t),(-w*.65,back,t),(-w,back*.65,t),(-w,front*.65,t)]
def hose(name,pts,r=.65,mat='rubber'):
 # A closed swept tube follows the visible hydraulic route.
 ps=[Vector(p) for p in pts];rings=[]
 for i,p in enumerate(ps):
  d=(ps[min(i+1,len(ps)-1)]-ps[max(0,i-1)]).normalized();q=d.to_track_quat('Z','Y');rings.append([p+q@Vector((r*math.cos(j*math.tau/10),r*math.sin(j*math.tau/10),0)) for j in range(10)])
 return loft(name,rings,mat,.04)
def ram(name,a,b,r=1.6):
 a=Vector(a);b=Vector(b);d=b-a;q=d.normalized()
 rod(name+'_Chromed_Rod',a,b,r,'titanium',n=24,bevel=.07)
 rod(name+'_Armored_Cylinder',a,a+d*.58,r*1.72,'frame',n=24,bevel=.2)
 ring(name+'_Gland',a+d*.53,a+d*.62,r*1.96,r*.97,'gold',24,.1)
 ring(name+'_End_Collar',a-q*.5,a+q*1.1,r*1.92,r*.97,'steel',24,.1)
 for i,p in enumerate((a,b)):
  axis=Vector((1,0,0));rod(name+'_Mount_'+str(i),p-axis*r*1.7,p+axis*r*1.7,r*1.42,'steel',n=20,bevel=.12)
  bolt(name+'_End_Cap_'+str(i),p+axis*r*1.8,(1,0,0),r*.55,'gold')
def local_plate(name,pts,f,depth=(0,2,0),mat='blue',bevel=.25):
 origin=Vector(f((0,0,0)));dep=Vector(f(depth))-origin
 return plate(name,[f(p) for p in pts],dep,mat,bevel)
def save(stage):
 D=WORK/f'iteration_{stage:02d}';D.mkdir(exist_ok=True);bpy.context.view_layer.update();SC['mass_revision']=stage
 bpy.context.preferences.filepaths.save_version=0;bpy.ops.wm.save_as_mainfile(filepath=str(D/'ASSEMBLED.blend'));print('MASS_STAGE_SAVED',stage,flush=True)
