"""Reference-unit modeling on the accepted existing assembly. -Y is front."""
import bpy,bmesh,math,pathlib,json,hashlib
from mathutils import Vector,Matrix
from mathutils.geometry import tessellate_polygon
W=pathlib.Path(__file__).resolve().parent
S=2.974/348
SC=bpy.context.scene
M={k:bpy.data.materials[v] for k,v in dict(blue='V3H_01_Valkyr_Blue',navy='V3H_02_Deep_Blue',silver='V3H_04_Gunmetal_Armor',steel='V3H_05_Machined_Rim',frame='V3H_06_Inner_Frame',black='V3H_07_Optical_Well',gold='V3H_08_Antenna_Gold',cyan='V3H_09_Cyan_Sensor',core='V3H_10_Cyan_Core',rubber='V3H_11_Seals').items()}
def material(key,hexcol,metal,rough,emission=0):
 existing=bpy.data.materials.get('V3B_'+key)
 if existing:M[key]=existing;return existing
 m=bpy.data.materials.new('V3B_'+key);m.use_nodes=True
 rgb=[int(hexcol[i:i+2],16)/255 for i in (0,2,4)];rgb=[c/12.92 if c<=.04045 else ((c+.055)/1.055)**2.4 for c in rgb]
 m.diffuse_color=(*rgb,1);b=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
 for n,v in [('Base Color',(*rgb,1)),('Metallic',metal),('Roughness',rough)]:b.inputs[n].default_value=v
 if emission:b.inputs['Emission Color'].default_value=(*rgb,1);b.inputs['Emission Strength'].default_value=emission
 M[key]=m;return m
material('white','CED5DF',.23,.37);material('panel','365689',.3,.4);material('edge','4C6FA6',.32,.34)
material('mark','DDEAF5',.05,.5);material('titanium','818996',.74,.3);material('beam','04A5FF',.12,.22,5);material('beam_core','69E1FF',.1,.16,10)
COLS={};PARENT=None;COL=None;PARTS=[]
def group(name,parent):
 global PARENT,COL
 PARENT=bpy.data.objects[parent] if isinstance(parent,str) else parent
 cn='BODY_V3 | '+name
 if cn not in COLS:
  c=bpy.data.collections.get(cn)
  if c is None:c=bpy.data.collections.new(cn);SC.collection.children.link(c)
  COLS[cn]=c
 COL=COLS[cn]
def U(p):return Vector(p)*S
def fp(x,v,y=-20):return (x,y,509-v)
def mesh(name,vs,fs,mat='blue',bevel=.18,smooth=False,parent=None):
 me=bpy.data.meshes.new('V3B_'+name+'_mesh');me.from_pydata([U(v) for v in vs],[],fs);me.update()
 bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free()
 o=bpy.data.objects.new('V3B_'+name,me);COL.objects.link(o);o.parent=parent or PARENT;o.matrix_world=Matrix.Identity(4)
 me.materials.append(M[mat]);o['design_part']=name;o['reference']='REFERENCE.png / VALKYR TYPE-01 full body'
 if bevel:
  b=o.modifiers.new('Manufactured edge radius','BEVEL');b.width=bevel*S;b.segments=2;b.harden_normals=True
  n=o.modifiers.new('Weighted corner normals','WEIGHTED_NORMAL');n.keep_sharp=True;n.weight=40
 if smooth:
  for f in me.polygons:f.use_smooth=True
 PARTS.append(o);return o
def skin(name,vs,fs,inward,mat='blue',bevel=.18):
 n=len(vs);v=list(vs)+[tuple(Vector(p)+Vector(inward)) for p in vs]
 ff=list(fs)+[tuple(k+n for k in reversed(f)) for f in fs];ee={}
 for f in fs:
  for a,b in zip(f,f[1:]+f[:1]):
   k=tuple(sorted((a,b)));ee[k]=None if k in ee else (a,b)
 for e in ee.values():
  if e:a,b=e;ff.append((a,b,b+n,a+n))
 return mesh(name,v,ff,mat,bevel)
def plate(name,pts,depth=(0,2,0),mat='blue',bevel=.18):
 vv=[Vector(p) for p in pts];ff=[]
 for t in tessellate_polygon([vv]):ff.append(tuple(i if isinstance(i,int) else min(range(len(vv)),key=lambda j:(vv[j]-i).length_squared) for i in t))
 return skin(name,pts,ff,depth,mat,bevel)
def front(name,xv,y=-20,thick=2,mat='blue',s=1,bevel=.18):
 return plate(name,[(s*x,y,509-v) for x,v in xv],(0,thick,0),mat,bevel)
def hull(name,vs,mat='frame',bevel=.18):
 bm=bmesh.new()
 for v in vs:bm.verts.new(v)
 bmesh.ops.convex_hull(bm,input=list(bm.verts),use_existing_faces=False)
 loose=[v for v in bm.verts if not v.link_faces]
 if loose:bmesh.ops.delete(bm,geom=loose,context='VERTS')
 bmesh.ops.dissolve_limit(bm,angle_limit=.0001,verts=list(bm.verts),edges=list(bm.edges))
 bm.verts.ensure_lookup_table();bm.verts.index_update();vs=[tuple(v.co) for v in bm.verts];fs=[tuple(v.index for v in f.verts) for f in bm.faces];bm.free()
 return mesh(name,vs,fs,mat,bevel)
def box(name,c,d,mat='frame',bevel=.2):
 return hull(name,[(c[0]+i*d[0]/2,c[1]+j*d[1]/2,c[2]+k*d[2]/2) for i in(-1,1) for j in(-1,1) for k in(-1,1)],mat,bevel)
def rod(name,a,b,r,mat='steel',r2=None,n=24,bevel=.1):
 a=Vector(a);b=Vector(b);q=(b-a).to_track_quat('Z','Y')
 vs=[tuple(p+q@Vector((rr*math.cos(i*math.tau/n),rr*math.sin(i*math.tau/n),0))) for p,rr in ((a,r),(b,r if r2 is None else r2)) for i in range(n)]
 fs=[tuple(range(n-1,-1,-1)),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
 return mesh(name,vs,fs,mat,bevel)
def ring(name,a,b,r,ri,mat='titanium',n=32,bevel=.1):
 a=Vector(a);b=Vector(b);q=(b-a).to_track_quat('Z','Y')
 vs=[tuple(p+q@Vector((rr*math.cos(i*math.tau/n),rr*math.sin(i*math.tau/n),0))) for p,rr in ((a,r),(b,r),(a,ri),(b,ri)) for i in range(n)]
 fs=[]
 for i in range(n):
  j=(i+1)%n;fs.extend([(i,j,n+j,n+i),(2*n+i,3*n+i,3*n+j,2*n+j),(i,2*n+i,2*n+j,j),(n+i,n+j,3*n+j,3*n+i)])
 return mesh(name,vs,fs,mat,bevel)
def bolt(name,p,normal=(0,-1,0),r=.7,mat='titanium'):
 p=Vector(p);n=Vector(normal).normalized()
 rod(name+'_Seat',p,p+n*.25,r*1.15,'black',n=16,bevel=.03)
 rod(name+'_Hex',p+n*.22,p+n*.65,r,mat,n=12,bevel=.06)
 rod(name+'_Socket',p+n*.66,p+n*.72,r*.42,'black',n=6,bevel=.01)
def piston(name,a,b,r=1.4):
 a=Vector(a);b=Vector(b);d=b-a
 rod(name+'_Rod',a,b,r,'titanium');rod(name+'_Sleeve',a,a+d*.55,r*1.7,'frame');ring(name+'_Gold_Seal',a+d*.50,a+d*.57,r*1.95,r*.96,'gold',24)
 for i,p in enumerate((a,b)):rod(name+'_Clevis_'+str(i),p+Vector((-2.1,0,0)),p+Vector((2.1,0,0)),r*1.65,'titanium',n=16)
def bearing(name,c,axis=(1,0,0),r=5,depth=4,gold=True):
 c=Vector(c);a=Vector(axis).normalized()
 rod(name+'_Drum',c-a*depth/2,c+a*depth/2,r,'frame',n=32)
 for s in (-1,1):
  p=c+a*s*(depth/2+.15);ring(name+'_Race_'+str(s),p,p+a*s*.55,r*.89,r*.58,'gold' if gold else 'titanium')
  rod(name+'_Cap_'+str(s),p+a*s*.6,p+a*s*.9,r*.46,'steel',n=16)
def strip(name,pts,width=.5,mat='gold',depth=(0,.3,0)):
 for i,(a,b) in enumerate(zip(pts,pts[1:])):
  a=Vector(a);b=Vector(b);d=(b-a).normalized();side=d.cross(Vector(depth).normalized()).normalized()*width*.5
  plate(name+'_'+str(i),[a+side,b+side,b-side,a-side],depth,mat,.04)
def recess(name,outer,inner,depth=3,mat='blue',bars=0):
 # An open armor frame, physical sidewalls, and a separate dark recessed floor.
 n=len(outer);assert len(inner)==n
 out=list(outer)+list(inner)+[tuple(Vector(p)+Vector((0,depth,0))) for p in inner]+[tuple(Vector(p)+Vector((0,depth,0))) for p in outer]
 fs=[]
 for i in range(n):
  j=(i+1)%n;fs.extend([(i,j,n+j,n+i),(n+i,n+j,2*n+j,2*n+i),(i,3*n+i,3*n+j,j),(2*n+i,2*n+j,3*n+j,3*n+i)])
 mesh(name+'_Frame',out,fs,mat,.12)
 plate(name+'_Well',[tuple(Vector(p)+Vector((0,depth+.2,0))) for p in inner],(0,.5,0),'black',.06)
def text_mesh(name,text,center,size,mat='mark',rotation=(math.pi/2,0,0)):
 cu=bpy.data.curves.new('V3B_'+name,'FONT');cu.body=text;cu.align_x='CENTER';cu.align_y='CENTER';cu.size=size*S;cu.extrude=.015*S;cu.resolution_u=2
 fontpath=pathlib.Path('C:/Windows/Fonts/bahnschrift.ttf')
 if fontpath.exists():cu.font=bpy.data.fonts.load(str(fontpath),check_existing=True)
 o=bpy.data.objects.new('V3B_'+name,cu);COL.objects.link(o);o.location=U(center);o.rotation_euler=rotation;cu.materials.append(M[mat]);bpy.context.view_layer.update();mw=o.matrix_world.copy();o.parent=PARENT;o.matrix_world=mw
 bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o;bpy.ops.object.convert(target='MESH');o=bpy.context.object;o['design_part']=name;PARTS.append(o);return o
def marker(name,p,length=2,s=1):
 x,y,z=p;box(name,(x,y,z),(length,.1,.32),'gold',.02)
def mirror(pts,s):return [(x*s,y,z) for x,y,z in pts]
def save_stage(stage,notes):
 bpy.context.view_layer.update();SC['body_reference_stage']=stage;SC['body_reference_notes']=notes;SC['body_reference_source']='Accepted current assembly; original pivot hierarchy and internal drive components retained'
 SC['design_height_m']=18.4;SC['design_mass_t']=62.1;SC['production_scale_m_per_reference_pixel']=S
 bpy.context.preferences.filepaths.save_version=0
 D=W/f'stage_{stage:02d}';D.mkdir(exist_ok=True)
 bpy.ops.wm.save_as_mainfile(filepath=str(D/'ASSEMBLED.blend'))
 (D/'build_report.json').write_text(json.dumps({'stage':stage,'new_meshes':len(PARTS),'retained_existing_components':sum(bool(o.get('V3B_retained_existing_component')) for o in SC.objects),'notes':notes},indent=2))
