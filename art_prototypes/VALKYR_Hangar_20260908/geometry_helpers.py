def log(s):
    with LOG.open('a') as f:
        f.write(s + '\n')
    print(s, flush=True)

def collection(name):
    global COL
    COL = bpy.data.collections.new(name)
    scene.collection.children.link(COL)
    return COL

def put(obj):
    for c in list(obj.users_collection):
        c.objects.unlink(obj)
    COL.objects.link(obj)
    return obj

def material(name, color, metal=.3, rough=.65, weather=True, emission=0):
    m = bpy.data.materials.new('LUNA_' + name)
    m.diffuse_color = (*color, 1)
    m.use_nodes = True
    n = m.node_tree.nodes
    l = m.node_tree.links
    bs = n.get('Principled BSDF')
    bs.inputs['Base Color'].default_value = (*color, 1)
    bs.inputs['Metallic'].default_value = metal
    bs.inputs['Roughness'].default_value = rough
    if emission:
        bs.inputs['Emission Color'].default_value = (*color, 1)
        bs.inputs['Emission Strength'].default_value = emission
    if weather:
        tex = n.new('ShaderNodeTexNoise')
        tex.inputs['Scale'].default_value = 5.5
        tex.inputs['Detail'].default_value = 3
        coords = n.new('ShaderNodeTexCoord')
        l.new(coords.outputs['Object'], tex.inputs['Vector'])
        ramp = n.new('ShaderNodeValToRGB')
        ramp.color_ramp.elements[0].position = .24
        ramp.color_ramp.elements[0].color = (*(c * .46 for c in color), 1)
        ramp.color_ramp.elements[1].position = .78
        ramp.color_ramp.elements[1].color = (*(min(1, c * 1.12 + .025) for c in color), 1)
        l.new(tex.outputs['Fac'], ramp.inputs[0])
        l.new(ramp.outputs[0], bs.inputs['Base Color'])
        fine = n.new('ShaderNodeTexNoise')
        fine.inputs['Scale'].default_value = 95
        fine.inputs['Detail'].default_value = 2
        l.new(coords.outputs['Object'], fine.inputs['Vector'])
        bump = n.new('ShaderNodeBump')
        bump.inputs['Strength'].default_value = .21
        bump.inputs['Distance'].default_value = .035
        l.new(fine.outputs['Fac'], bump.inputs['Height'])
        l.new(bump.outputs[0], bs.inputs['Normal'])
    return m

def finish(o, name, mat, bevel=0):
    o.name = name
    put(o)
    if mat:
        o.data.materials.append(M[mat] if isinstance(mat, str) else mat)
    if bevel:
        mod = o.modifiers.new('Machined edge bevel', 'BEVEL')
        mod.width = bevel
        mod.segments = 2
        mod.limit_method = 'ANGLE'
    return o

def box(name, p, s, mat='blue', bevel=.035, rot=(0,0,0)):
    x,y,z = [v/2 for v in s]
    verts = [(-x,-y,-z),(-x,-y,z),(-x,y,-z),(-x,y,z),(x,-y,-z),(x,-y,z),(x,y,-z),(x,y,z)]
    faces = [(0,4,6,2),(1,3,7,5),(0,1,5,4),(2,6,7,3),(0,2,3,1),(4,5,7,6)]
    me = bpy.data.meshes.new(name)
    me.from_pydata(verts,[],faces)
    o = bpy.data.objects.new(name,me)
    o.location = p
    o.rotation_euler = rot
    return finish(o,name,mat,bevel)

def mesh(name, verts, faces, mat, bevel=0):
    me = bpy.data.meshes.new(name)
    me.from_pydata(verts,[],faces)
    me.update()
    return finish(bpy.data.objects.new(name,me),name,mat,bevel)

def cyl(name,p,r,d,mat='steel',axis=(0,0,1),vertices=24,bevel=.025):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=r, depth=d, location=p)
    o=bpy.context.object
    o.rotation_euler=Vector(axis).to_track_quat('Z','Y').to_euler()
    return finish(o,name,mat,bevel)

def beam(name,a,b,r=.09,mat='steel'):
    a,b=Vector(a),Vector(b)
    return cyl(name,(a+b)/2,r,(b-a).length,mat,b-a,12,.009)

def bar(name,a,b,width=.15,mat='steel'):
    a,b=Vector(a),Vector(b)
    o=box(name,(a+b)/2,(width,width,(b-a).length),mat,.018)
    o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler()
    return o

def curve(name,points,r=.035,mat='dark',smooth=True):
    cu=bpy.data.curves.new(name,'CURVE')
    cu.dimensions='3D'
    cu.resolution_u=10
    cu.bevel_depth=r
    cu.bevel_resolution=2
    sp=cu.splines.new('BEZIER' if smooth else 'POLY')
    if smooth:
        sp.bezier_points.add(len(points)-1)
        for p,co in zip(sp.bezier_points,points):
            p.co=co
            p.handle_left_type='AUTO'
            p.handle_right_type='AUTO'
    else:
        sp.points.add(len(points)-1)
        for p,co in zip(sp.points,points):p.co=(*co,1)
    return finish(bpy.data.objects.new(name,cu),name,mat)

def label(name,body,p,size=.5,mat='white',rot=(math.pi/2,0,0),align='CENTER'):
    cu=bpy.data.curves.new(name,'FONT')
    cu.body=body
    cu.size=size
    cu.align_x=align
    cu.align_y='CENTER'
    cu.extrude=.0007
    cu.resolution_u=3
    o=finish(bpy.data.objects.new(name,cu),name,mat)
    o.location=p
    o.rotation_euler=rot
    return o

def ring(name,center,r,width,mat,z=.04,start=0,end=math.tau,steps=80):
    verts=[]
    for i in range(steps+1):
        a=start+(end-start)*i/steps
        for rr in (r-width/2,r+width/2):
            verts.append((center[0]+math.cos(a)*rr,center[1]+math.sin(a)*rr,z))
    return mesh(name,verts,[(2*i,2*i+1,2*i+3,2*i+2) for i in range(steps)],mat)

def bolt(p,axis=(0,-1,0),r=.055):
    return cyl('Hex fastener',p,r,.035,'steel',axis,6,.002)

def group(name,objects,p=(0,0,0),yaw=0):
    root=bpy.data.objects.new(name,None)
    COL.objects.link(root)
    for o in objects:
        if o.parent is None:o.parent=root
    root.location=p
    root.rotation_euler.z=yaw
    return root

def begin(): return set(COL.objects)

def added(before):return [o for o in COL.objects if o not in before]

def crate(name,p,size=(1.35,1.05,.95),yaw=0,mat='blue'):
    before=begin()
    sx,sy,sz=size
    box(name+' shell',(0,0,sz/2),size,mat,.09)
    box('Cargo lid',(0,0,sz+.025),(sx+.05,sy+.05,.09),'steel')
    for x in (-sx*.34,sx*.34):
        box('Cargo strap',(x,0,sz+.08),(.10,sy+.1,.045),'ivory',.012)
        for y in (-sy/2-.018,sy/2+.018):box('Cargo corner',(x,y,sz/2),(.1,.06,sz),'steel',.01)
    for x in (-sx*.28,sx*.28):box('Cargo lock',(x,-sy/2-.05,sz*.65),(.16,.08,.22),'orange')
    label('Cargo id','L-07',(0,-sy/2-.058,sz*.35),.17)
    return group(name,added(before),p,yaw)

def height(x,y):
    d=max(abs(x)/31,abs(y)/29)
    flat=max(0,min(1,(d-.68)*2.5))
    h=-.17+noise.fractal(Vector((x*.075,y*.075,1.17)),1,2,3)*.55*flat
    h+=noise.noise_vector(Vector((x*.65,y*.65,.37))).z*.045
    for cx,cy,r in craters:
        t=math.hypot(x-cx,y-cy)/r
        if t<1.5:
            h+= -.95*max(0,1-t*t)**2 + .45*math.exp(-((t-.91)/.17)**2)
    return h

def solar(p,yaw,broken=False):
    before=begin()
    for x in (-2,2):
        box('Array ballast',(x,0,.17),(1.3,1.25,.34),'ivory',.06)
        bar('Array support post',(x,0,.3),(x,0,2),.14,'steel')
        bar('Array diagonal brace',(x,-.52,.3),(x,.85,2.5),.12,'steel')
    frame_before=begin()
    box('Solar frame',(0,0,0),(5.8,3.5,.16),'ivory',.035)
    box('Solar black substrate',(0,0,.09),(5.56,3.26,.04),'dark',.01)
    for x in range(6):
        for y in range(4):
            if broken and ((x==4 and y>=2) or (x==5 and y>=1)):continue
            xx=-2.32+x*.925;yy=-1.23+y*.81
            box('Individual blue PV cell',(xx,yy,.121),(.88,.77,.035),'solar',.006)
            for line in range(3):box('PV silver busbar',(xx-.29+line*.29,yy,.142),(.008,.73,.004),'blue',0)
    panel=group('Fractured panel' if broken else 'Solar panel',added(frame_before),(0,0,2.15),0)
    panel.rotation_euler.x=math.radians(28 if not broken else 11)
    if broken:panel.rotation_euler.y=.2
    for x in (-2,2):bar('Panel hinge crossbar',(x,-1.1,1.56),(x,1.1,2.6),.085,'steel')
    return group('PHOTOVOLTAIC ARRAY',added(before),p,yaw)

def light(name,kind,p,color,energy,size=1,target=(0,0,0)):
    d=bpy.data.lights.new(name,kind);d.energy=energy;d.color=color
    if kind=='AREA':d.shape='DISK';d.size=size
    if kind=='SUN':d.angle=.045
    o=bpy.data.objects.new(name,d);COL.objects.link(o);o.location=p
    o.rotation_euler=(Vector(target)-o.location).to_track_quat('-Z','Y').to_euler()
    return o

def camera(name,p,target,ortho):
    d=bpy.data.cameras.new(name);d.type='ORTHO';d.ortho_scale=ortho;d.clip_end=500
    o=bpy.data.objects.new(name,d);COL.objects.link(o);o.location=p
    o.rotation_euler=(Vector(target)-o.location).to_track_quat('-Z','Y').to_euler()
    return o
