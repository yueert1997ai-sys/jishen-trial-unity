import bpy, math, json
from pathlib import Path
from mathutils import Vector, Matrix
BASE=Path(r'D:/project-mecha-design/MECH ROUGE/art_prototypes/HALBREAKER_AX01_20260908')
S=3.491269/1020
scene=bpy.data.scenes.new('AX-01 | HALBREAKER | LIVE BUILD')
bpy.context.window.scene=scene
scene.unit_settings.system='METRIC'
scene.render.engine='CYCLES'
scene.cycles.samples=32
scene.cycles.use_denoising=True
scene.render.resolution_x=1800;scene.render.resolution_y=1100;scene.render.resolution_percentage=100
scene.world=bpy.data.worlds.new('AX01 studio world');scene.world.use_nodes=True
bg=next(n for n in scene.world.node_tree.nodes if n.type=='BACKGROUND')
bg.inputs[0].default_value=(.26,.28,.33,1)
bg.inputs[1].default_value=.5
scene.view_settings.view_transform='AgX'
groups={}
def group(name):
    global active_group
    if name not in groups:
        c=bpy.data.collections.new(name);scene.collection.children.link(c);groups[name]=c
    active_group=groups[name]
    return active_group
group('00 | Assembly controls')
root=bpy.data.objects.new('AX01_SHOULDER_CANNON_ROOT',None);active_group.objects.link(root)
root['mount_mode']='SHOULDER ONLY - load borne by shoulder saddle; hands stabilize'
root['overall_length_m']=3.491269
root['reference_length_px']=1020
root['reference']='User AX-01 HALBREAKER orthographic and perspective sheets'
def mat(name,color,metal=.0,rough=.4,emit=0):
    m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True
    p=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED');p.inputs['Base Color'].default_value=(*color,1)
    p.inputs['Metallic'].default_value=metal;p.inputs['Roughness'].default_value=rough
    if emit:p.inputs['Emission Color'].default_value=(*color,1);p.inputs['Emission Strength'].default_value=emit
    return m
M={
 'white':mat('AX01 | Ceramic ivory armor',(.69,.73,.77),.45,.32),
 'edge':mat('AX01 | Bright machined edges',(.37,.43,.49),.78,.27),
 'purple':mat('AX01 | Violet armor',(.16,.12,.37),.5,.32),
 'purple_light':mat('AX01 | Violet bevel panels',(.28,.23,.52),.45,.34),
 'steel':mat('AX01 | Barrel gunmetal',(.075,.09,.115),.83,.31),
 'black':mat('AX01 | Recess graphite',(.018,.023,.031),.4,.43),
 'rubber':mat('AX01 | Grip elastomer',(.025,.03,.037),.0,.69),
 'copper':mat('AX01 | Copper windings',(.52,.19,.065),.79,.28),
 'gold':mat('AX01 | Anodized bronze',(.7,.43,.09),.73,.26),
 'cyan':mat('AX01 | Cyan status optics',(.015,.64,.7),.3,.21,2),
 'red':mat('AX01 | Vermilion warning stripes',(.63,.025,.025),.2,.37),
 'ink':mat('AX01 | Stencil ink',(.14,.17,.20),.0,.5)}
def put(ob,name,material=None):
    ob.name=name
    for c in list(ob.users_collection):c.objects.unlink(ob)
    active_group.objects.link(ob);ob.parent=root
    if material:ob.data.materials.append(M[material] if isinstance(material,str) else material)
    return ob
def bevel(ob,w=.008,seg=3):
    m=ob.modifiers.new('Manufactured edge bevel','BEVEL');m.width=w;m.segments=seg
    m=ob.modifiers.new('Face weighted normals','WEIGHTED_NORMAL');m.keep_sharp=True
    return ob
def xyz(px,py,depth=0):return ((px-534)*S,depth,(350-py)*S)
def box(name,px,py,sx,sz,width,material='steel',depth=0,bev=2):
    bpy.ops.mesh.primitive_cube_add(size=1,location=xyz(px,py,depth*S))
    ob=put(bpy.context.object,name,material);ob.scale=(sx*S,width*S,sz*S)
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if bev:bevel(ob,bev*S)
    return ob
def plate(name,points,depth,thick,material='white',bev=1.3):
    n=len(points);verts=[xyz(x,z,(depth+d)*S) for d in (-thick/2,thick/2) for x,z in points]
    faces=[tuple(range(n-1,-1,-1)),tuple(range(n,n*2))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    me=bpy.data.meshes.new(name);me.from_pydata(verts,[],faces);me.update()
    ob=bpy.data.objects.new(name,me);active_group.objects.link(ob);ob.parent=root;ob.data.materials.append(M[material])
    if bev:bevel(ob,bev*S)
    return ob
def cyl(name,px,py,radius,length,material='steel',depth=0,axis='X',verts=64):
    bpy.ops.mesh.primitive_cylinder_add(vertices=verts,radius=radius*S,depth=length*S,location=xyz(px,py,depth*S))
    ob=put(bpy.context.object,name,material)
    ob.rotation_euler=(0,math.pi/2,0) if axis=='X' else (math.pi/2,0,0) if axis=='Y' else (0,0,0)
    for p in ob.data.polygons:p.use_smooth=len(p.vertices)==4
    bevel(ob,.65*S,2);return ob
def tube(name,profile,py=311,material='steel',depth=0,segments=96):
    # Closed radial cross-section revolves about the bore axis; no fake solid muzzle cap.
    vs=[xyz(x,py-math.sin(t*math.tau/segments)*r,(depth+math.cos(t*math.tau/segments)*r)*S) for x,r in profile for t in range(segments)]
    fs=[]
    for j in range(len(profile)):
        for i in range(segments):
            a=j*segments+i;b=j*segments+(i+1)%segments;c=((j+1)%len(profile))*segments+(i+1)%segments;d=((j+1)%len(profile))*segments+i
            fs.append((a,b,c,d))
    me=bpy.data.meshes.new(name);me.from_pydata(vs,[],fs);me.update()
    ob=bpy.data.objects.new(name,me);active_group.objects.link(ob);ob.parent=root;ob.data.materials.append(M[material])
    for f in me.polygons:f.use_smooth=True
    return ob
def bar(name,p1,p2,r,material='steel'):
    a,b=Vector(p1),Vector(p2);bpy.ops.mesh.primitive_cylinder_add(vertices=24,radius=r,depth=(b-a).length,location=(a+b)/2)
    ob=put(bpy.context.object,name,material);ob.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler()
    for f in ob.data.polygons:f.use_smooth=True
    return ob
def path(name,points,r,material='steel'):
    cu=bpy.data.curves.new(name,'CURVE');cu.dimensions='3D';cu.bevel_depth=r;cu.bevel_resolution=3
    spl=cu.splines.new('POLY');spl.points.add(len(points)-1)
    for p,v in zip(spl.points,points):p.co=(*v,1)
    ob=bpy.data.objects.new(name,cu);active_group.objects.link(ob);ob.parent=root;cu.materials.append(M[material]);return ob
def bolt(name,x,z,depth,r=3):
    ob=cyl(name,x,z,r,1.8,'edge',depth,axis='Y',verts=6)
    cyl(name+' socket',x,z,r*.43,2,'black',depth+(-1 if depth<0 else 1),axis='Y',verts=6)
    return ob
def cut(ob,cutter):
    bpy.context.view_layer.objects.active=ob
    mod=ob.modifiers.new('Through aperture','BOOLEAN');mod.operation='DIFFERENCE';mod.object=cutter
    bpy.ops.object.modifier_move_up(modifier=mod.name)
    while list(ob.modifiers).index(mod)>0:bpy.ops.object.modifier_move_up(modifier=mod.name)
    bpy.ops.object.modifier_apply(modifier=mod.name);bpy.data.objects.remove(cutter,do_unlink=True)
def view(loc=(-4,-6,3),target=(0,0,0),distance=5.5):
    for area in bpy.context.screen.areas:
        if area.type=='VIEW_3D':
            sp=area.spaces.active;sp.region_3d.view_location=Vector(target)
            sp.region_3d.view_rotation=(Vector(loc)-Vector(target)).to_track_quat('Z','Y')
            sp.region_3d.view_distance=distance;sp.region_3d.view_perspective='ORTHO'
            sp.shading.type='SOLID';sp.shading.light='STUDIO';sp.shading.color_type='MATERIAL'
            sp.shading.show_shadows=True;sp.shading.show_cavity=True;sp.shading.cavity_type='BOTH'
            sp.overlay.show_floor=False;sp.overlay.show_axis_x=False;sp.overlay.show_axis_y=False
            sp.clip_end=1000
    bpy.context.view_layer.update()
def save(stage):
    bpy.ops.object.select_all(action='DESELECT');root.select_set(True);bpy.context.view_layer.objects.active=root
    bpy.context.scene['build_stage']=stage
    bpy.ops.wm.save_as_mainfile(filepath=str(BASE/'stages'/f'{stage}.blend'))
    bpy.ops.wm.redraw_timer(type='DRAW_WIN_SWAP',iterations=1)
    print('STAGE',stage,'parts',len(root.children_recursive))
view()
print('AX01 scene ready; working at 3.491269 m overall length')
