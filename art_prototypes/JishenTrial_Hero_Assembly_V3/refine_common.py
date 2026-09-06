import bpy,bmesh,math,pathlib,json
from mathutils import Vector,Matrix
OUT=pathlib.Path(__file__).resolve().parent
WORK=OUT/'refinement'
WORK.mkdir(exist_ok=True)
for d in ('renders','logs','exports','textures'): (WORK/d).mkdir(exist_ok=True)
SC=bpy.context.scene
M={k:bpy.data.materials[n] for k,n in dict(navy='01_Painted_Military_Navy',panel='02_Navy_Secondary_Panels',white='04_Offwhite_Ceramic_Armor',frame='05_Black_Mechanical_Frame',gun='06_Dark_Gunmetal',steel='07_Bare_Machined_Steel',eye='10_Blue_Sensor_Glass',beam='11_Blue_Beam_Plasma',dark='12_Optical_Recess').items()}
C={k:bpy.data.collections[n] for k,n in dict(primary='ARMOR',secondary='Secondary_Armor',frame='FRAME',joint='Joint_Housings',pack='BACKPACK',cannon='SHOULDER_CANNON',weapon='WEAPON').items()}
def obj(name):return bpy.data.objects[name]
def deform(o,fn):
 w=o.matrix_world.copy(); inv=w.inverted()
 if o.type=='MESH':
  if o.data.users>1:o.data=o.data.copy()
  for v in o.data.vertices:v.co=inv@Vector(fn(w@v.co))
  o.data.update()
 elif o.type=='CURVE':
  for sp in o.data.splines:
   for p in sp.bezier_points:
    p.co=inv@Vector(fn(w@p.co));p.handle_left=inv@Vector(fn(w@p.handle_left));p.handle_right=inv@Vector(fn(w@p.handle_right))
   for p in sp.points:p.co=(*(inv@Vector(fn(w@Vector(p.co[:3])))),p.co.w)
 o['refinement_modified']=True
def affine(o,center=(0,0,0),scale=(1,1,1),move=(0,0,0)):
 c=Vector(center);s=Vector(scale);d=Vector(move)
 deform(o,lambda v:c+Vector(((v[i]-c[i])*s[i] for i in range(3)))+d)
def mesh(name,verts,faces,parent,mat='navy',col='primary',bevel=.002,replace=None):
 me=bpy.data.meshes.new(name+'_Mesh');me.from_pydata(verts,[],faces);me.update()
 bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free()
 if replace:
  o=obj(replace);me.transform(o.matrix_world.inverted());o.data=me;o.modifiers.clear()
 else:
  o=bpy.data.objects.new(name,me);C[col].objects.link(o);o.parent=obj(parent) if isinstance(parent,str) else parent;o.matrix_world=Matrix.Identity(4)
 me.materials.append(M[mat]);o['refinement_modified']=True;o['design_role']=name
 if bevel:
  b=o.modifiers.new('Manufactured edge radii','BEVEL');b.width=bevel;b.segments=3
  b.harden_normals=True
 return o
def hull(name,pts,parent,mat='navy',col='primary',bevel=.002,replace=None):
 bm=bmesh.new()
 for p in pts:bm.verts.new(p)
 bmesh.ops.convex_hull(bm,input=list(bm.verts),use_existing_faces=False)
 loose=[v for v in bm.verts if not v.link_faces]
 if loose:bmesh.ops.delete(bm,geom=loose,context='VERTS')
 bmesh.ops.dissolve_limit(bm,angle_limit=.0001,verts=list(bm.verts),edges=list(bm.edges))
 bm.verts.ensure_lookup_table();bm.verts.index_update()
 vs=[tuple(v.co) for v in bm.verts];fs=[tuple(v.index for v in f.verts) for f in bm.faces];bm.free()
 return mesh(name,vs,fs,parent,mat,col,bevel,replace)
def plate(name,pts,depth,parent,mat='navy',col='secondary',bevel=.002,replace=None):
 return hull(name,list(pts)+[tuple(Vector(p)+Vector(depth)) for p in pts],parent,mat,col,bevel,replace)
def rod(name,a,b,r,parent,mat='gun',col='frame',n=16,r2=None):
 a=Vector(a);b=Vector(b);q=(b-a).to_track_quat('Z','Y')
 vs=[tuple(p+q@Vector((rr*math.cos(i*math.tau/n),rr*math.sin(i*math.tau/n),0))) for p,rr in ((a,r),(b,r if r2 is None else r2)) for i in range(n)]
 fs=[tuple(range(n-1,-1,-1)),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
 return mesh(name,vs,fs,parent,mat,col,.0008)
def tube(name,a,b,r,ri,parent,mat='gun',col='joint',n=20):
 a=Vector(a);b=Vector(b);q=(b-a).to_track_quat('Z','Y')
 vs=[tuple(p+q@Vector((rr*math.cos(i*math.tau/n),rr*math.sin(i*math.tau/n),0))) for p,rr in ((a,r),(b,r),(a,ri),(b,ri)) for i in range(n)]
 fs=[]
 for i in range(n):
  j=(i+1)%n;fs.extend([(i,j,n+j,n+i),(2*n+i,3*n+i,3*n+j,2*n+j),(i,2*n+i,2*n+j,j),(n+i,n+j,3*n+j,3*n+i)])
 return mesh(name,vs,fs,parent,mat,col,.0008)
def box(name,c,d,parent,mat='gun',col='frame',bevel=.002):
 return hull(name,[(c[0]+x*d[0]/2,c[1]+y*d[1]/2,c[2]+z*d[2]/2) for x in (-1,1) for y in (-1,1) for z in (-1,1)],parent,mat,col,bevel)
def intake(name,target,x,z,width,height,parent,back=False,bars=3):
 o=obj(target);bpy.context.view_layer.update();ev=o.evaluated_get(bpy.context.evaluated_depsgraph_get());inv=o.matrix_world.inverted()
 ray=Vector((0,-1 if back else 1,0));origin=Vector((x,4 if back else -4,z))
 ok,hit,normal,idx=ev.ray_cast(inv@origin,inv.to_3x3()@ray);assert ok,(name,target,x,z)
 p=o.matrix_world@hit;n=(o.matrix_world.to_3x3()@normal).normalized()
 xx=(Vector((1,0,0))-n*n.x).normalized();zz=xx.cross(-n).normalized()
 if zz.z<0:xx=-xx;zz=-zz
 def lb(label,c,d,mat,bevel=.0008):
  pts=[tuple(p+xx*(c[0]+a*d[0]/2)-n*(c[1]+b*d[1]/2)+zz*(c[2]+k*d[2]/2)) for a in (-1,1) for b in (-1,1) for k in (-1,1)]
  return hull(label,pts,parent,mat,'secondary',bevel)
 cut=lb(name+'_Cutter',(0,.040,0),(width,.160,height),'dark',0)
 for c in list(cut.users_collection):c.objects.unlink(cut)
 bpy.data.collections['Construction_Cutters | hidden'].objects.link(cut);cut.hide_render=True;cut.hide_set(True)
 m=o.modifiers.new(name+'_True_Recess','BOOLEAN');m.operation='DIFFERENCE';m.solver='EXACT';m.object=cut
 # Boolean ahead of bevels, keeping all editable cutting volumes.
 bpy.context.view_layer.objects.active=o
 while o.modifiers.find(m.name)>0:bpy.ops.object.modifier_move_up(modifier=m.name)
 lb(name+'_RecessFloor',(0,.09,0),(width*.99,.014,height*.98),'dark')
 for k,(c,d) in enumerate([((0,-.001,height/2+.003),(width+.017,.012,.006)),((0,-.001,-height/2-.003),(width+.017,.012,.006)),((width/2+.003,-.001,0),(.006,.012,height)),((-width/2-.003,-.001,0),(.006,.012,height))]):lb(name+'_Rim_'+str(k),c,d,'gun')
 for j in range(bars):lb(name+'_Louver_'+str(j),(0,.024,(j-(bars-1)/2)*height/bars),(width*.94,.024,height/bars*.22),'steel')
 return p
def save_phase(n,notes):
 bpy.context.view_layer.update();SC['refinement_phase']=n;SC['refinement_source']='Existing JishenTrial_ASSEMBLED_MASTER.blend; retained assembly, materials, collections, cameras'
 SC['refinement_notes']=notes
 bpy.context.preferences.filepaths.save_version=0
 for layer in SC.view_layers:layer.material_override=None
 bpy.ops.wm.save_as_mainfile(filepath=str(WORK/f'PHASE_{n:02d}.blend'))
 (WORK/'logs'/f'phase_{n:02d}.json').write_text(json.dumps({'phase':n,'notes':notes,'objects':len(SC.objects),'modified':sum(bool(o.get('refinement_modified')) for o in SC.objects)},indent=2))
