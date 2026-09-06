def mesh(name,vs,fs,mat='Gunmetal',key='01',bevel=.30,smooth=False):
    me=bpy.data.meshes.new('EBR_'+name+'_Mesh'); me.from_pydata(vs,[],fs); me.update()
    bm=bmesh.new(); bm.from_mesh(me)
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces)); bm.to_mesh(me); bm.free()
    o=bpy.data.objects.new('EBR_'+name,me); COL[key].objects.link(o)
    par=GROUPS.get(key,root); o.parent=par
    o.matrix_parent_inverse=Matrix.Translation(-par.location) if par!=root else Matrix.Identity(4)
    me.materials.append(M[mat])
    if smooth:
        for f in me.polygons:
            if len(f.vertices)==4:f.use_smooth=True
    if bevel:
        b=o.modifiers.new('Machined edge','BEVEL'); b.width=bevel*S; b.segments=2; b.limit_method='ANGLE'; b.harden_normals=True
        n=o.modifiers.new('Weighted normals','WEIGHTED_NORMAL'); n.keep_sharp=True; n.weight=50
    o['part_role']=name; PARTS.append(o); return o

def poly(name,pts,front,back,mat='Gunmetal',key='01',bevel=.30):
    n=len(pts); vs=[p(x,z,d) for d in (front,back) for x,z in pts]
    fs=[tuple(range(n-1,-1,-1)),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    return mesh(name,vs,fs,mat,key,bevel)

def box(name,x,z,l,h,depth,mat='Gunmetal',key='01',d=0,bevel=.30):
    return poly(name,[(x-l/2,z-h/2),(x+l/2,z-h/2),(x+l/2,z+h/2),(x-l/2,z+h/2)],d+depth/2,d-depth/2,mat,key,bevel)

def panel(name,pts,d,thick,mat='Panel',key='01',both=True,bevel=.25):
    return [poly(name+('_L' if s==1 else '_R'),pts,s*d,s*(d-thick),mat,key,bevel) for s in ((1,-1) if both else (1,))]

def rod(name,a,b,r,mat='Steel',key='01',n=32,r2=None,bevel=.16):
    a,b=Vector(a),Vector(b);q=(b-a).to_track_quat('Z','Y')
    vs=[pt+q@Vector((rr*S*math.cos(i*math.tau/n),rr*S*math.sin(i*math.tau/n),0))
        for pt,rr in ((a,r),(b,r if r2 is None else r2)) for i in range(n)]
    fs=[tuple(range(n-1,-1,-1)),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    return mesh(name,vs,fs,mat,key,bevel,True)

def ring(name,a,b,r,ri,mat='Gunmetal',key='01',n=48,bevel=.12):
    a,b=Vector(a),Vector(b);q=(b-a).to_track_quat('Z','Y')
    vs=[pt+q@Vector((rr*S*math.cos(i*math.tau/n),rr*S*math.sin(i*math.tau/n),0))
        for pt,rr in ((a,r),(b,r),(a,ri),(b,ri)) for i in range(n)]
    fs=[]
    for i in range(n):
        j=(i+1)%n
        fs.extend([(i,j,n+j,n+i),(2*n+i,3*n+i,3*n+j,2*n+j),
                   (i,2*n+i,2*n+j,j),(n+i,n+j,3*n+j,3*n+i)])
    return mesh(name,vs,fs,mat,key,bevel,True)

def bolt(name,x,z,d,r=1.35,key='01',both=True,mat='Steel'):
    for s in ((1,-1) if both else (1,)):
        ring(name+('_L' if s==1 else '_R')+'_Seat',p(x,z,s*(d-.12)),p(x,z,s*(d+.15)),r*1.25,r*.75,'Recess',key,20,.02)
        ring(name+('_L' if s==1 else '_R')+'_Hex',p(x,z,s*d),p(x,z,s*(d+.7)),r,r*.40,mat,key,6,.035)

def trim(name,coords,d,width,mat='Steel',key='01',both=True):
    for i,(a,b) in enumerate(zip(coords,coords[1:])):
        a,b=Vector(a),Vector(b);v=(b-a).normalized();w=Vector((-v.y,v.x))*width/2
        panel(name+'_'+str(i),[a+w,b+w,b-w,a-w],d,.30,mat,key,both,.07)

def cut(target,holes,name):
    vs=[];fs=[]
    for pts in holes:
        n=len(pts);b=len(vs)
        vs.extend(p(x,z,d) for d in (-80,80) for x,z in pts)
        fs.extend([tuple(b+i for i in range(n-1,-1,-1)),tuple(b+i for i in range(n,2*n))])
        fs.extend((b+i,b+(i+1)%n,b+(i+1)%n+n,b+i+n) for i in range(n))
    cutter=mesh('TEMP_'+name,vs,fs,'Recess','01',0)
    bpy.context.view_layer.update();bpy.context.view_layer.objects.active=target
    mod=target.modifiers.new(name,'BOOLEAN');mod.operation='DIFFERENCE';mod.solver='EXACT';mod.object=cutter
    while target.modifiers.find(mod.name)>0:bpy.ops.object.modifier_move_up(modifier=mod.name)
    bpy.ops.object.modifier_apply(modifier=mod.name)
    PARTS.remove(cutter);bpy.data.objects.remove(cutter,do_unlink=True)

def cable(name,coords,r=1.7,mat='DarkSteel',key='01'):
    cu=bpy.data.curves.new(name,'CURVE');cu.dimensions='3D';cu.resolution_u=8;cu.bevel_resolution=2;cu.bevel_depth=r*S
    sp=cu.splines.new('BEZIER');sp.bezier_points.add(len(coords)-1)
    for a,c in zip(sp.bezier_points,coords):a.co=p(*c);a.handle_left_type='AUTO';a.handle_right_type='AUTO'
    o=bpy.data.objects.new('EBR_'+name,cu);COL[key].objects.link(o);o.parent=GROUPS.get(key,root)
    if o.parent!=root:o.matrix_parent_inverse=Matrix.Translation(-o.parent.location)
    cu.materials.append(M[mat]);PARTS.append(o);return o

def label(name,text,x,z,d,size=3,mat='Lettering',key='01',both=True):
    for s in ((1,-1) if both else (1,)):
        cu=bpy.data.curves.new(name,'FONT');cu.body=text;cu.size=size*S;cu.extrude=.015*S;cu.align_x='CENTER'
        ob=bpy.data.objects.new('EBR_'+name+('_L' if s==1 else '_R'),cu);COL[key].objects.link(ob)
        ob.parent=GROUPS.get(key,root)
        if ob.parent!=root:ob.matrix_parent_inverse=Matrix.Translation(-ob.parent.location)
        ob.location=p(x,z,s*d);ob.rotation_euler=(math.pi/2,0,0 if s==1 else math.pi)
        cu.materials.append(M[mat]);PARTS.append(ob)


