"""Editable head, constructed against the supplied front/side/back drawings.
Coordinates: reference drawing units, X right, Y rear, Z from neck datum.
Only the rejected head's descendants are replaced in the existing assembly.
"""
import bpy, bmesh, math, pathlib, json, sys, hashlib
from mathutils import Vector, Matrix
from mathutils.geometry import tessellate_polygon
W=pathlib.Path(__file__).resolve().parent
ITER=int(next((a.split('=')[1] for a in sys.argv if a.startswith('iter=')),'1'))
D=W/f'iteration_{ITER:02d}'; D.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(W/'BODY_SOURCE_BEFORE_HEAD.blend'))
sc=bpy.context.scene
old=bpy.data.objects['HEAD_V2_ROOT']; removed=[o.name for o in old.children_recursive]+[old.name]
for name in removed: bpy.data.objects.remove(bpy.data.objects[name],do_unlink=True)
def coll(name):
 c=bpy.data.collections.new(name); sc.collection.children.link(c); return c
HC=coll('HEAD_V3 | reference matched editable assembly')
LC=coll('HEAD_V3 | review lights'); CC=coll('HEAD_V3 | inspection cameras')
RC=coll('HEAD_V3 | packed reference'); RC.hide_render=True
root=bpy.data.objects.new('HEAD_V3_ROOT',None); HC.objects.link(root)
root.parent=bpy.data.objects['Head']; root.matrix_world=Matrix.Identity(4)
S=.0012; Z0=2.974
root['reference']='REFERENCE.png / supplied VALKYR TYPE-01 head orthographic sheet'
root['construction']='Individual closed armor shells, recessed twin optics, solid antenna mounts, articulated neck'
root['unit_scale_m']=S; root['neck_datum_m']=Z0
def world(p):return Vector((p[0]*S,p[1]*S,Z0+p[2]*S))
def linear(c):return c/12.92 if c<=.04045 else ((c+.055)/1.055)**2.4
def material(name,hexcol,metal,rough,emission=0):
 m=bpy.data.materials.new('V3H_'+name);m.use_nodes=True
 rgb=tuple(linear(int(hexcol[i:i+2],16)/255) for i in (0,2,4))
 m.diffuse_color=(*rgb,1); bs=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
 bs.inputs['Base Color'].default_value=(*rgb,1);bs.inputs['Metallic'].default_value=metal;bs.inputs['Roughness'].default_value=rough
 if emission:bs.inputs['Emission Color'].default_value=(*rgb,1);bs.inputs['Emission Strength'].default_value=emission
 return m
M={'blue':material('01_Valkyr_Blue','223D75',.27,.4), 'navy':material('02_Deep_Blue','17284F',.32,.4), 'edge':material('03_Blue_Edge','2B477D',.32,.36),
 'silver':material('04_Gunmetal_Armor','99A5B8',.3,.42), 'steel':material('05_Machined_Rim','667483',.72,.3), 'frame':material('06_Inner_Frame','181D25',.35,.52),
 'black':material('07_Optical_Well','050B12',.3,.4), 'gold':material('08_Antenna_Gold','D6B26F',.65,.3), 'cyan':material('09_Cyan_Sensor','0089FF',.12,.2,.9),
 'core':material('10_Cyan_Core','13C5FF',.1,.2,1.5), 'rubber':material('11_Seals','12181F',.08,.65)}
parts=[]
def mesh(name,vs,fs,mat='blue',bevel=.45,smooth=False):
 if name.startswith(('Fronto_Temporal_Shell','Temporal_Wrapped_Band','Swept_Crown_Crest','Occipital_','Rear_Outer_Shield')):
  vs=[(x*(1-.23*min(1,max(0,(z-185)/75))) if y>0 else x,y,z) for x,y,z in vs]
 me=bpy.data.meshes.new('V3H_'+name+'_mesh');me.from_pydata([world(p) for p in vs],[],fs);me.update()
 bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free()
 o=bpy.data.objects.new('V3H_'+name,me);HC.objects.link(o);o.parent=root;o.matrix_world=Matrix.Identity(4)
 me.materials.append(M[mat]);o['part_role']=name;o['source_reference']='REFERENCE.png'
 if bevel:
  mod=o.modifiers.new('Small manufactured edge bevel','BEVEL');mod.width=bevel*S;mod.segments=2;mod.harden_normals=True
  mod=o.modifiers.new('Weighted corner normals','WEIGHTED_NORMAL');mod.keep_sharp=True;mod.weight=35
 if smooth:
  for p in me.polygons:p.use_smooth=True
 parts.append(o);return o
def skin(name,vs,fs,inward,mat='blue',bevel=.45):
 n=len(vs);out=list(vs)+[tuple(Vector(v)+Vector(inward)) for v in vs]
 faces=list(fs)+[tuple(i+n for i in reversed(f)) for f in fs];edges={}
 for f in fs:
  for a,b in zip(f,f[1:]+f[:1]):
   k=tuple(sorted((a,b)));edges[k]=None if k in edges else (a,b)
 for e in edges.values():
  if e:
   a,b=e;faces.append((a,b,b+n,a+n))
 return mesh(name,out,faces,mat,bevel)
def plate(name,vs,depth=(0,3,0),mat='blue',bevel=.45):
 vv=[Vector(v) for v in vs];fs=[]
 for t in tessellate_polygon([vv]):fs.append(tuple(i if isinstance(i,int) else min(range(len(vv)),key=lambda j:(vv[j]-i).length_squared) for i in t))
 return skin(name,vs,fs,depth,mat,bevel)
def hull(name,vs,mat='frame',bevel=.5):
 bm=bmesh.new()
 for v in vs:bm.verts.new(v)
 bmesh.ops.convex_hull(bm,input=list(bm.verts),use_existing_faces=False)
 loose=[v for v in bm.verts if not v.link_faces]
 if loose:bmesh.ops.delete(bm,geom=loose,context='VERTS')
 bmesh.ops.dissolve_limit(bm,angle_limit=.0001,verts=list(bm.verts),edges=list(bm.edges))
 bm.verts.ensure_lookup_table();bm.verts.index_update();pts=[tuple(v.co) for v in bm.verts];fs=[tuple(v.index for v in f.verts) for f in bm.faces];bm.free()
 return mesh(name,pts,fs,mat,bevel)
def box(name,c,d,mat='frame',bevel=.5):
 return hull(name,[(c[0]+a*d[0]/2,c[1]+b*d[1]/2,c[2]+k*d[2]/2) for a in(-1,1) for b in(-1,1) for k in(-1,1)],mat,bevel)
def cylinder(name,a,b,r,mat='frame',n=48,r2=None,bevel=.35):
 a=Vector(a);b=Vector(b);q=(b-a).to_track_quat('Z','Y')
 vs=[tuple(p+q@Vector((rr*math.cos(i*math.tau/n),rr*math.sin(i*math.tau/n),0))) for p,rr in ((a,r),(b,r if r2 is None else r2)) for i in range(n)]
 fs=[tuple(range(n-1,-1,-1)),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
 return mesh(name,vs,fs,mat,bevel)
def ring(name,a,b,r,ri,mat='frame',n=48):
 a=Vector(a);b=Vector(b);q=(b-a).to_track_quat('Z','Y')
 vs=[tuple(p+q@Vector((rr*math.cos(i*math.tau/n),rr*math.sin(i*math.tau/n),0))) for p,rr in ((a,r),(b,r),(a,ri),(b,ri)) for i in range(n)]
 fs=[]
 for i in range(n):
  j=(i+1)%n;fs.extend([(i,j,n+j,n+i),(2*n+i,3*n+i,3*n+j,2*n+j),(i,2*n+i,2*n+j,j),(n+i,n+j,3*n+j,3*n+i)])
 return mesh(name,vs,fs,mat,.28)
def cable(name,pts,r,mat='rubber'):
 c=bpy.data.curves.new(name,'CURVE');c.dimensions='3D';c.bevel_depth=r*S;c.bevel_resolution=2;c.resolution_u=12;c.use_fill_caps=True
 sp=c.splines.new('BEZIER');sp.bezier_points.add(len(pts)-1)
 for p,v in zip(sp.bezier_points,pts):p.co=world(v);p.handle_left_type='AUTO';p.handle_right_type='AUTO'
 o=bpy.data.objects.new('V3H_'+name,c);HC.objects.link(o);o.parent=root;o.matrix_world=Matrix.Identity(4);c.materials.append(M[mat]);parts.append(o);return o
def mirrored(vs,s):return [(s*x,y,z) for x,y,z in vs]
def bolt(name,p,normal=(0,-1,0),r=1.05):
 p=Vector(p);n=Vector(normal).normalized()
 cylinder(name,tuple(p),tuple(p+n*.65),r,'steel',12,bevel=.08)
 cylinder(name+'_Socket',tuple(p+n*.66),tuple(p+n*.76),r*.45,'black',6,bevel=.02)

# Closed internal frame, shaped behind the shell rather than exposed as a cap.
ringspec=[(48,43,49,8),(85,71,83,12),(155,88,95,13),(215,72,83,22),(264,36,53,29),(284,23,20,20)]
vs=[];N=12
for z,rx,ry,cy in ringspec:
 for i in range(N):
  a=math.tau*i/N;vs.append((rx*math.cos(a),cy+ry*math.sin(a),z))
fs=[tuple(range(N-1,-1,-1)),tuple(range((len(ringspec)-1)*N,len(ringspec)*N))]
for j in range(len(ringspec)-1):
 for i in range(N):fs.append((j*N+i,j*N+(i+1)%N,(j+1)*N+(i+1)%N,(j+1)*N+i))
mesh('Cranial_Frame',vs,fs,'frame',.6)

# Crown spine wraps continuously from the forehead over the top to the occiput.
sections=[(-106,194,14),(-80,245,24),(-22,291,23),(22,294,22),(61,279,22),(94,257,21),(111,224,19)]
for k in range(len(sections)-1):
 ya,za,xa=sections[k];yb,zb,xb=sections[k+1]
 t=.009; a=Vector((ya,za)).lerp(Vector((yb,zb)),t);b=Vector((ya,za)).lerp(Vector((yb,zb)),1-t)
 vv=[(-xa,a.x,a.y),(xa,a.x,a.y),(-xb,b.x,b.y),(xb,b.x,b.y),(-xa-4,a.x+2,a.y-6),(xa+4,a.x+2,a.y-6),(-xb-4,b.x+2,b.y-6),(xb+4,b.x+2,b.y-6)]
 skin('Crown_Spine_%02d'%k,vv,[(0,1,3,2),(0,2,6,4),(1,5,7,3)],(0,1,-3),'blue' if k<4 else 'navy',.32)
for s,side in ((1,'L'),(-1,'R')):
 mi=lambda p:mirrored(p,s)
 # Narrow intermediate rails delimit the central channel without rounding the crown.
 v=[(29,-21,288),(41,-10,276),(46,-37,248),(29,-94,179),(21,-104,162),(26,-79,228)]
 skin('Forehead_Inner_Rail.'+side,mi(v),[(0,1,2,5),(2,3,4,5)],(-s*3,3,-1),'navy',.35)
 # Front plane and swept temporal plane share edges, forming one wrapped armor volume.
 v=[(41,-7,274),(64,-18,247),(84,-25,219),(80,-61,178),(27,-108,138),(31,-97,185),(40,-55,237),
    (55,51,277),(82,69,251),(100,57,215),(101,24,183),(85,-2,178)]
 skin('Fronto_Temporal_Shell.'+side,mi(v),[(0,7,8,1),(2,9,10,11,3)],(-s*3,2,-2),'navy',.5)
 l=lambda a,b,t:tuple(Vector(a).lerp(Vector(b),t))
 skin('Temporal_Wrapped_Band_Upper.'+side,mi([v[1],v[8],l(v[8],v[9],.47),l(v[1],v[2],.47)]),[(0,1,2,3)],(-s*3,1,-2),'blue',.35)
 skin('Temporal_Wrapped_Band_Lower.'+side,mi([l(v[1],v[2],.51),l(v[8],v[9],.51),v[9],v[2]]),[(0,1,2,3)],(-s*3,1,-2),'navy',.35)
 skin('Crown_Inner_Underlap.'+side,mi([(26,-16,285),(37,0,278),(44,40,268),(26,57,268)]),[(0,1,2,3)],(0,0,-4),'navy',.35)
 uv=[(40,-3,273),(53,-2,260),(66,-18,244),(82,-25,218),(56,-64,206),(33,-87,195),(39,-72,231)]
 skin('Temporal_Upper_Leaf.'+side,mi(uv),[(0,1,6),(1,2,6),(2,3,4,6),(4,5,6)],(-s*3,3,-1),'blue',.42)
 lv=[(31,-92,190),(82,-28,215),(80,-63,178),(27,-111,138),(23,-113,153),(28,-103,176),(54,-81,178)]
 skin('Frontal_Lower_Leaf.'+side,mi(lv),[(0,1,6,5),(1,2,3,6),(3,4,5,6)],(-s*2,3,-1),'blue',.35)
 # The inner sweep forms a second discrete leaf, producing the characteristic tall front channels.
 v=[(34,-45,255),(42,-28,261),(49,-48,235),(38,-90,187),(26,-108,147),(28,-105,184)]
 skin('Inner_Forehead_Leaf.'+side,mi(v),[(0,1,2,5),(2,3,4,5)],(-s*2,3,-1),'navy',.3)
 # Crown side crest and rear cascade are deliberately separated with dark reveal gaps.
 v=[(40,2,271),(48,45,273),(73,80,253),(85,76,242),(65,41,257),(49,-7,264)]
 skin('Swept_Crown_Crest.'+side,mi(v),[(0,1,4,5),(1,2,3,4)],(-s*3,-2,-4),'blue',.35)
 v=[(43,65,270),(71,84,254),(93,113,224),(75,111,218),(55,102,237),(39,94,252)]
 skin('Occipital_Upper_Overlap.'+side,mi(v),[(0,1,4,5),(1,2,3,4)],(-s*3,-5,-2),'navy',.45)
 v=[(93,65,231),(111,83,213),(113,112,183),(94,124,178),(68,107,203),(80,90,224)]
 skin('Occipital_Middle_Overlap.'+side,mi(v),[(0,1,2,5),(2,3,4,5)],(-s*5,-5,0),'blue',.6)
 v=[(104,76,175),(113,109,154),(101,118,114),(82,107,121),(71,88,151)]
 plate('Occipital_Lower_Overlap.'+side,mi(v),(-s*4,-4,1),'navy',.5)
 v=[(80,89,120),(94,106,104),(81,108,71),(59,86,78),(57,65,99)]
 plate('Nape_Floating_Shingle.'+side,mi(v),(-s*4,-5,1),'blue',.45)
 # Temple hinge carries the entire antenna root, with three concentric machined seats.
 cylinder('Temporal_Mount_Base.'+side,(s*81,3,162),(s*100,3,162),23,'frame')
 ring('Temporal_Bearing_Rim.'+side,(s*99,3,162),(s*103,3,162),20.5,15.5,'steel')
 cylinder('Temporal_Bearing_Cap.'+side,(s*101,3,162),(s*105,3,162),14,'frame')
 ring('Temporal_Cap_Seal.'+side,(s*105,3,162),(s*105.8,3,162),11.5,9.8,'black')
 cylinder('Temporal_Cap_Center.'+side,(s*105.9,3,162),(s*107,3,162),8.8,'frame',32)
 # Four-sided silver blade antenna; root lies inside the hinge carrier.
 av=[(88,-7,167),(103,-2,180),(132,126,322),(110,31,240),(93,0,180),(116,66,261)]
 skin('Silver_Antenna.'+side,mi(av),[(0,1,4),(1,2,5,4),(2,3,0,4,5)],(-s*3,2,-1),'silver',.16)
 # Gold lower vane hugs the leading side of the silver blade.
 gv=[(69,-95,146),(90,-36,160),(103,3,227),(107,13,247),(102,12,244),(87,-45,200),(87,-52,181)]
 skin('Gold_Antenna_Root.'+side,mi(gv),[(0,1,6),(1,2,6),(2,3,5,6),(3,4,5),(5,0,6)],(-s*2.2,3,0),'gold',.18)
 hull('Antenna_Leading_Root_Support.'+side,mi([(78,-45,151),(88,-31,161),(100,-9,170),(97,-6,153),(81,-43,149)]),'frame',.3)
 plate('Antenna_Root_Key.'+side,mi([(88,-5,150),(103,9,166),(105,18,190),(99,6,201),(91,-9,177)]),(-s*4,4,0),'frame',.4)
 # Deep orbital frame with an actual inset opening, not a floating rectangular lamp.
 fy=lambda x:-117+.49*x
 out=[(12,103),(19,127),(72,159),(83,145),(69,116),(29,98)]
 aperture=[(23,110),(27,122),(60,137),(66,138),(57,121),(32,110)]
 a=[(x,fy(x)+5,z) for x,z in out];b=[(x,fy(x)+5,z) for x,z in aperture];c=[(x,fy(x)+11,z) for x,z in aperture];d=[(x,fy(x)+17,z) for x,z in out]
 n=6;vv=a+b+c+d;ff=[]
 for i in range(n):
  j=(i+1)%n;ff.extend([(i,j,n+j,n+i),(n+i,n+j,2*n+j,2*n+i),(i,3*n+i,3*n+j,j)])
 ff.extend([tuple(range(2*n,3*n)),tuple(reversed(range(3*n,4*n)))])
 mesh('Recessed_Orbital_Chassis.'+side,mi(vv),ff,'black',.24)
 lens=[(25,113),(29,120),(63,135),(54,122),(33,114)]
 plate('Twin_Cyan_Optic.'+side,mi([(x,fy(x)+9,z) for x,z in lens]),(0,1.5,0),'cyan',.13)
 lens=[(28,114),(32,119),(58,131),(51,123),(34,116)]
 plate('Optic_Inner_Filament.'+side,mi([(x,fy(x)+8.7,z) for x,z in lens]),(0,.5,0),'core',.08)
 # A shallow V brow, broad gunmetal face and razor-thin dark underside.
 v=[(.6,-126,113),(31,-110,143),(80,-74,179),(85,-64,180),(73,-79,155),(26,-113,122),(.6,-127,98), (47,-102,151)]
 skin('Chevron_Brow.'+side,mi(v),[(0,1,7,5,6),(1,2,3,7),(3,4,5,7)],(0,3.2,-.4),'silver',.30)
 plate('Brow_Lower_Gasket.'+side,mi([(.6,-124,98),(26,-110,121),(73,-76,154),(66,-78,148),(24,-111,115),(.6,-123,95)]),(0,3,0),'black',.15)
 # Thin lower orbital rail ties the eye to the cheek, staying behind the bright optics.
 plate('Lower_Orbital_Rail.'+side,mi([(16,-108,100),(30,-96,105),(64,-73,123),(75,-61,139),(66,-58,115),(31,-91,95)]),(0,4,0),'navy',.25)
 # Split silver face shield with central vertical ridge and a forked lower cut.
 v=[(.6,-118,87),(32,-96,107),(35,-88,91),(23,-92,54),(14,-104,37),(4,-111,62),(.6,-115,65),(16,-108,77)]
 v=[(x,y+14,z) for x,y,z in v]
 skin('Split_Face_Shield.'+side,mi(v),[(0,1,7,6),(1,2,3,7),(3,4,5,6,7)],(0,3.5,0),'silver',.35)
 # Lower chin spear is blue; the silver mask ends on either side of it.
 plate('Chin_Spear.'+side,mi([(.5,-99,63),(4,-98,60),(13,-90,29),(.5,-98,15)]),(0,5,1),'blue',.25)
 v=[(28,-79,104),(63,-49,125),(59,-35,83),(27,-63,42),(25,-71,64),(36,-73,87)]
 skin('Malar_Inner_Armor.'+side,mi(v),[(0,1,2,5),(2,3,4,5)],(0,6,0),'navy',.4)
 # Main lateral cheek is a wrapped shell, with a long silver inner edge.
 v=[(83,-43,145),(109,20,181),(109,83,196),(103,82,137),(79,5,55),(74,-41,42),(74,-56,78),(81,-45,104),(108,32,137)]
 skin('Lateral_Cheek_Armor.'+side,mi(v),[(0,1,8,7),(1,2,3,8),(3,4,5,6,7,8)],(-s*4,3,0),'blue',.6)
 v=[(104,51,157),(115,79,154),(106,91,111),(90,35,80),(84,-8,55),(89,9,87)]
 skin('Cheek_Outer_Overlap.'+side,mi(v),[(0,1,2,5),(2,3,4,5)],(-s*4,-2,1),'navy',.45)
 v=[(68,-50,132),(80,-34,145),(75,-41,111),(72,-45,73),(56,-69,24),(48,-82,35),(56,-68,80),(61,-58,115)]
 skin('Cheek_Gunmetal_Edge.'+side,mi(v),[(0,1,2,7),(2,3,6,7),(3,4,5,6)],(-s*2.5,3,0),'silver',.34)
 # Recessed mechanical cheek yoke, load strut and visible fasteners.
 plate('Jaw_Internal_Yoke.'+side,mi([(57,-48,77),(66,-15,74),(51,26,43),(31,24,28),(33,-14,42),(45,-56,41)]),(-s*6,2,0),'frame',.45)
 cylinder('Jaw_Link_Pivot.'+side,(s*54,-7,57),(s*64,-7,57),10,'steel',32)
 cylinder('Jaw_Link_Pin.'+side,(s*64,-7,57),(s*65,-7,57),5.5,'frame',24)
 cylinder('Jaw_Actuator_Barrel.'+side,(s*43,-31,22),(s*49,-31,51),4.8,'frame',24)
 cylinder('Jaw_Actuator_Rod.'+side,(s*43,-31,22),(s*39,-27,8),2.7,'steel',20)

# Forehead well follows the sagittal channel, with narrow metal cheeks and a recessed tall optic.
plate('Forehead_Sensor_Recess',[(-13,-110,191),(13,-110,191),(15,-118,154),(10,-119,138),(-10,-119,138),(-15,-118,154)],(0,10,0),'black',.45)
for s,side in ((1,'L'),(-1,'R')):
 plate('Forehead_Sensor_Rail.'+side,mirrored([(10,-116,188),(14,-112,188),(16,-119,155),(11,-123,140),(8,-122,144)],s),(0,4,0),'navy',.25)
 plate('Forehead_Inner_Bezel.'+side,mirrored([(6.5,-116,184),(8,-116,184),(7,-121,147),(5.5,-121,147)],s),(0,1,0),'steel',.12)
plate('Forehead_Glass',[(-5.5,-115.7,183),(5.5,-115.7,183),(4.8,-120.7,148),(-4.8,-120.7,148)],(0,2,0),'cyan',.22)
plate('Forehead_Core',[(-2,-116.2,179),(2,-116.2,179),(1.8,-121,152),(-1.8,-121,152)],(0,.6,0),'core',.15)
plate('Forehead_Lower_Sill',[(-10,-121,139),(10,-121,139),(6,-124,132),(-6,-124,132)],(0,4,0),'navy',.3)

# Central rear service channel and bifurcated spine.
plate('Rear_Service_Well',[(-17,113,211),(17,113,211),(16,122,87),(10,110,63),(-10,110,63),(-16,122,87)],(0,-8,0),'black',.35)
plate('Rear_Service_Insert',[(-10,118,179),(10,118,179),(10,124,108),(-10,124,108)],(0,-4,0),'frame',.35)
outer=[(-14,121,212),(14,121,212),(19,128,192),(16,128,83),(9,116,61),(-9,116,61),(-16,128,83),(-19,128,192)]
inner=[(-6,130,178),(6,130,178),(8,130,174),(8,130,101),(6,130,96),(-6,130,96),(-8,130,101),(-8,130,174)]
vv=outer+inner+[(x,y-5,z) for x,y,z in inner]+[(x,y-5,z) for x,y,z in outer];ff=[]
for i in range(8):
 j=(i+1)%8;ff.extend([(i,j,8+j,8+i),(8+i,8+j,16+j,16+i),(i,24+i,24+j,j),(24+i,16+i,16+j,24+j)])
mesh('Rear_Blue_Service_Spine',vv,ff,'blue',.36)
for s,side in ((1,'L'),(-1,'R')):
 plate('Rear_Channel_Rail.'+side,mirrored([(19,116,224),(28,111,216),(26,124,110),(18,119,69),(13,122,91)],s),(0,-5,0),'navy',.4)
 plate('Rear_Outer_Shield.'+side,mirrored([(37,109,219),(68,113,209),(77,117,172),(63,120,145),(31,120,154)],s),(0,-6,0),'blue',.5)
 plate('Rear_Nape_Fork.'+side,mirrored([(31,115,145),(59,116,132),(62,107,81),(38,82,45),(29,82,58)],s),(0,-6,1),'navy',.45)
 cable('Rear_Neck_Power_Loom.'+side,[(s*24,73,66),(s*30,58,43),(s*23,45,24),(s*23,35,4)],3)
 box('Rear_Loom_Clamp.'+side,(s*28,56,39),(9,6,10),'steel',.6)

# A complete mechanical neck seats on the body's retained yaw ring and pedestal.
ring('Neck_Yaw_Upper_Seal',(0,16,2),(0,16,8),29,20,'frame')
cylinder('Neck_Cardan_Pitch',(-31,16,26),(31,16,26),18,'frame')
for s,side in ((1,'L'),(-1,'R')):
 ring('Neck_Pitch_Seat.'+side,(s*29,16,26),(s*33,16,26),14,8,'steel')
 plate('Neck_Cardan_Fork.'+side,mirrored([(28,0,9),(34,1,17),(35,17,45),(28,28,47),(23,23,18)],s),(-s*5,0,0),'frame',.6)
 cylinder('Neck_Front_Piston.'+side,(s*17,-5,9),(s*21,-9,39),4.6,'frame',24)
 cylinder('Neck_Front_Piston_Rod.'+side,(s*21,-9,37),(s*23,-15,57),2.6,'steel',24)
 box('Neck_Cranium_Socket.'+side,(s*22,-14,56),(10,12,10),'frame',1)
for z in (5,11,17):box('Neck_Front_Flexible_Lamella_'+str(z),(0,-8,z),(24,8,3.7),'rubber',.4)

# Weld the generated curve end caps so the editable cable meshes are also closed solids.
bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get()
for o in list(parts):
 if o.type!='CURVE':continue
 me=bpy.data.meshes.new_from_object(o.evaluated_get(dg),depsgraph=dg)
 bm=bmesh.new();bm.from_mesh(me);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=1e-7);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free()
 nm=o.name;pa=o.parent;w=o.matrix_world.copy();parts.remove(o);bpy.data.objects.remove(o,do_unlink=True)
 n=bpy.data.objects.new(nm,me);HC.objects.link(n);n.parent=pa;n.matrix_world=w;parts.append(n)

# Every small key is seated on the evaluated armor surface, including the curved back.
bpy.context.view_layer.update()
def surface_key(label,target,x,z,back=False):
 o=bpy.data.objects['V3H_'+target];ev=o.evaluated_get(bpy.context.evaluated_depsgraph_get());inv=o.matrix_world.inverted()
 origin=Vector((x*S,3 if back else -3,Z0+z*S));direction=Vector((0,-1 if back else 1,0))
 ok,p,n,idx=ev.ray_cast(inv@origin,inv.to_3x3()@direction)
 assert ok,(label,target,x,z)
 p=o.matrix_world@p;n=(o.matrix_world.to_3x3()@n).normalized();v=((p.x)/S,p.y/S,(p.z-Z0)/S)
 bolt(label,v,tuple(n),.72)
for s,side in ((1,'L'),(-1,'R')):
 surface_key('Crown_Key_Lower.'+side,'Crown_Spine_00',s*10,203)
 surface_key('Crown_Key_Upper.'+side,'Crown_Spine_01',s*16,252)
 surface_key('Forehead_Plate_Key.'+side,'Frontal_Lower_Leaf.'+side,s*44,159)
 surface_key('Cheek_Gunmetal_Key.'+side,'Cheek_Gunmetal_Edge.'+side,s*60,60)
 surface_key('Occipital_Key.'+side,'Rear_Outer_Shield.'+side,s*46,183,True)

# Reference stays packed in the project, hidden from all renders.
im=bpy.data.images.load(str(W/'REFERENCE.png'),check_existing=True);im.pack()
ref=bpy.data.objects.new('V3H_REFERENCE_front_side_back_top',None);RC.objects.link(ref);ref.empty_display_type='IMAGE';ref.data=im;ref.hide_render=True;ref.hide_set(True)

def cam(name,at,target,scale):
 c=bpy.data.objects.new('V3H_CAM_'+name,bpy.data.cameras.new('V3H_CAM_'+name));CC.objects.link(c)
 c.location=at;c.rotation_euler=(Vector(target)-c.location).to_track_quat('-Z','Y').to_euler();c.data.type='ORTHO';c.data.ortho_scale=scale;c.data.clip_start=.01;c.data.clip_end=50;return c
mid=Z0+161*S
cam('FRONT',(0,-4,mid),(0,0,mid),.44)
cam('LEFT',(4,0,mid),(0,0,mid),.44)
cam('RIGHT',(-4,0,mid),(0,0,mid),.44)
cam('BACK',(0,4,mid),(0,0,mid),.44)
cam('TOP',(0,0,7),(0,0,mid),.43)
cam('THREE_QUARTER',(-2.7,-5,mid+.62),(0,0,mid),.46)
cam('UNDER',(-1,-3,mid-1.4),(0,0,mid-.07),.44)
cam('ON_BODY',(-2.3,-5,3.66),(0,0,2.99),1.06)
for name,at,power,size,color in [('KEY',(-1.2,-1.5,4.6),180,1.5,(.82,.89,1)),('FILL',(1.1,-.7,3.6),85,1.1,(.74,.84,1)),('RIM',(.3,.8,4.25),230,1.0,(.9,.94,1))]:
 d=bpy.data.lights.new('V3H_LIGHT_'+name,'AREA');d.energy=power;d.shape='DISK';d.size=size;d.color=color
 o=bpy.data.objects.new(d.name,d);LC.objects.link(o);o.location=at;o.rotation_euler=(Vector((0,0,mid))-o.location).to_track_quat('-Z','Y').to_euler();o.hide_render=True
sc.camera=bpy.data.objects['V3H_CAM_ON_BODY'];sc['head_revision']='V3 supplied reference recreation iteration %02d'%ITER
sc['head_reference_path']=str(W/'REFERENCE.png');sc['head_only_replacement']=True
bpy.context.preferences.filepaths.save_version=0
for vl in sc.view_layers:vl.material_override=None
bpy.context.view_layer.update()
bpy.ops.wm.save_as_mainfile(filepath=str(D/'ASSEMBLED.blend'))
(D/'build_manifest.json').write_text(json.dumps({'iteration':ITER,'source_sha256':hashlib.sha256((W/'BODY_SOURCE_BEFORE_HEAD.blend').read_bytes()).hexdigest(),'removed_head_objects':removed,'new_head_objects':[o.name for o in parts],'head_unit_m':S,'neck_datum_m':Z0},indent=2))
print('HEAD_BUILD_COMPLETE',ITER,len(parts),flush=True)
