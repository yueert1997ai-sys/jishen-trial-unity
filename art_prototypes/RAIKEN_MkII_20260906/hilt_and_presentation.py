"""Executed by build_raiken.py inside the shared modeling context."""
# Reactor: an angular load chassis, segmented titanium cage, and a vertical cell.
octo=[(795,161),(824,164),(842,180),(846,211),(831,234),(800,237),(780,218),(776,184)]
poly('Octagonal_Reactor_Load_Chassis',octo,17,-17,frame,'04',1.05)
inside=[(799,173),(819,174),(830,184),(833,207),(824,224),(803,226),(790,213),(787,188)]
rim('Reactor_Outer_Segmented_Cage',octo,inside,20,6,frame)
plate('Reactor_Recessed_Core_Bed',inside,18.7,3,black,'04',.5)
inner2=[(801,177),(818,177),(826,186),(828,206),(821,220),(804,222),(794,212),(792,189)]
rim('Reactor_Inner_Gold_Gasket',inside,inner2,19.3,1.4,panel)
caps=[
 ('Cage_Upper_Titanium_Key',[(797,162),(813,164),(812,175),(799,174),(791,170)]),
 ('Cage_Lower_Titanium_Key',[(803,224),(820,223),(828,231),(819,237),(802,235),(796,229)]),
 ('Cage_Forward_Key',[(778,185),(788,187),(789,204),(782,217),(776,207)]),
 ('Cage_Grip_Key',[(833,182),(843,183),(846,207),(838,213),(833,206)]),
]
for name,xy in caps:plate(name,xy,23,5,silver,'04',.6)

# The luminous unit is deliberately a tall narrow octagonal window, not a disc.
cellouter=[(806,176),(819,176),(824,182),(824,214),(818,222),(807,222),(801,216),(801,183)]
cellinner=[(810,180),(816,180),(819,184),(819,212),(815,217),(810,217),(806,213),(806,185)]
plate('Linear_Emitter_Metal_Housing',cellouter,23.2,6.3,frame,'04',.75)
rim('Linear_Emitter_Machined_Rim',cellouter,cellinner,24.5,3,navy)
plate('Linear_Emitter_Cyan_Cell',cellinner,24.0,1.8,cyan,'04',.55)
plate('Linear_Emitter_White_Filament',[(811,183),(814,183),(815.3,185),(815.3,212),(813,214),(811,212)],24.65,1,core,'04',.28)
for j,(x,z,dd) in enumerate(((787,179,23),(834,177,22),(841,215,22),(794,224,23),(824,234,23))):
    bolt('Reactor_Lock_%02d'%j,x,z,dd,1.8,gold if j in (1,3) else silver)
for j,z in enumerate((184,195,206)):
    plate('Reactor_Recessed_Vent_%02d'%j,[(828,z),(835,z+1),(836,z+5),(829,z+4)],21.5,2,black,'04',.2)
    bar('Reactor_Vent_Louver_%02d'%j,[(829,z+1),(834,z+2)],22.2,1.2,gold,'04')

# Rear of reactor is a real transverse gearbox with layered cooling ribs.
rod('Hilt_Axial_Load_Bearing',p(824,201,0),p(878,201,0),18,frame,'04',12,r2=12,bevel=.6)
for j,x in enumerate((850,858,867)):
    ring('Hilt_Axial_Cooling_Ring_%02d'%j,p(x,201,0),p(x+3,201,0),16-j,11,navy,'04',12,.25)
for d in (-1,1):
    rod('Hilt_External_Conduit_'+str(d),p(829,215,d*14),p(871,207,d*10),2.5,gold,'04',12,bevel=.14)
    rod('Hilt_Piston_Sleeve_'+str(d),p(834,216,d*14),p(854,212,d*12),3.7,frame,'04',12,bevel=.22)

# Four tapered guard wings are individual solid blades with raised crests.
wings=[
 ('Upper_Forward',[(744,151),(758,141),(850,94),(822,132),(788,157),(768,161)],[(756,149),(835,107),(813,132),(777,154)],blue,15.5),
 ('Upper_Rear',[(831,163),(850,149),(947,105),(910,141),(869,165),(848,175)],[(853,149),(933,117),(899,145),(865,160)],panel,12),
 ('Lower_Forward',[(748,241),(768,233),(802,248),(830,285),(793,266),(768,258)],[(761,244),(779,242),(815,271),(802,266),(777,254)],panel,15.5),
 ('Lower_Rear',[(820,233),(842,237),(880,263),(935,305),(877,282),(840,263),(806,247)],[(835,244),(852,249),(920,293),(882,275),(840,257)],blue,12),
]
for name,outline,inset,material,depth in wings:
    # Taper the actual wing section to a thin point, rather than extruding a slab.
    tip=2 if name.startswith('Upper') else 3
    base=(Vector(outline[0])+Vector(outline[-1]))*.5
    endpoint=Vector(outline[tip]);axis=endpoint-base
    def wd(x,z):return max(.35,depth*(1-.965*max(0,min(1,(Vector((x,z))-base).dot(axis)/axis.length_squared))))
    nn=len(outline)
    vv=[p(x,z,sg*wd(x,z)) for sg in (1,-1) for x,z in outline]
    ff=[tuple(range(nn-1,-1,-1)),tuple(range(nn,2*nn))]+[(j,(j+1)%nn,(j+1)%nn+nn,j+nn) for j in range(nn)]
    mesh('Guard_'+name+'_Tapered_Frame',vv,ff,frame,'04',.28)
    # Matching front and back facets, with a sharp metal leading edge.
    def wingplate(label,pts,offset,thick,ma):
        n=len(pts)
        for sg in (-1,1):
            verts=[p(x,z,sg*(wd(x,z)+off)) for off in (offset,offset-thick) for x,z in pts]
            faces=[tuple(range(n-1,-1,-1)),tuple(range(n,2*n))]+[(j,(j+1)%n,(j+1)%n+n,j+n) for j in range(n)]
            mesh('Guard_'+name+'_'+label+'_'+str(sg),verts,faces,ma,'04',.13)
    wingplate('Armor',inset,.6,1.5,material)
    a=Vector(outline[tip-1]);b=endpoint;v=(b-a).normalized();perp=Vector((-v.y,v.x))*.55
    wingplate('Honed_Leading_Edge',[tuple(a),tuple(b),tuple(b-perp*.1),tuple(a-perp)],.8,.4,silver)
    c=sum((Vector(pt) for pt in inset),Vector((0,0)))/len(inset)
    wingplate('Raised_Crest',[tuple(c+(Vector(pt)-c)*.62) for pt in inset],1.4,.9,blue if material==panel else panel)
    for j,pt in enumerate((inset[0],inset[-1])):
        q=Vector(pt)*.78+c*.22
        bolt('Guard_'+name+'_Pin_'+str(j),q.x,q.y,wd(q.x,q.y)+1.8,.9,gold)

# Load forks connect all four fins to the reactor instead of floating beside it.
for j,(a,b) in enumerate((((794,171),(774,152)),((829,174),(859,157)),((792,223),(778,246)),((827,223),(844,248)))):
    for sg in (-1,1):
        rod('Guard_Fork_%d_%s'%(j,sg),p(*a,sg*12),p(*b,sg*10),4.3,frame,'04',8,bevel=.25)
    bolt('Guard_Fork_Pivot_'+str(j),*a,23,2.15,silver)

# A keyed collar, segmented black grip and angular armored pommel.
rod('Handle_Continuous_Tang',p(869,201,0),p(996,201,0),8.6,frame,'05',12,bevel=.3)
for j,(x,w,r,material) in enumerate(((868,6,13.2,silver),(877,5,11.2,panel),(882,3,10.2,black),(975,5,11.2,navy),(981,5,12.1,silver))):
    ring('Handle_Collar_%02d'%j,p(x,201,0),p(x+w,201,0),r,7.1,material,'05',12,.34)
for j in range(7):
    x=887+j*12.5
    rod('Grip_Rubber_Segment_%02d'%j,p(x,201,0),p(x+11,201,0),9.6,black,'05',8,bevel=.55)
    # Four separated metal grip tiles leave the black soft insert readable.
    for sg in (-1,1):
        poly('Grip_Front_Tile_%s_%s'%(j,sg),[(x+.7,194),(x+9.6,194),(x+10.3,196),(x+10.3,204),(x+8.6,207),(x+.7,207)],sg*9.7,sg*8.2,frame,'05',.32)
        bolt('Grip_Fastener_%s_%s'%(j,sg),x+6.2,201,sg*10.15,.72,panel,False)
    rod('Grip_Separation_Rib_%02d'%j,p(x+10.9,201,0),p(x+12,201,0),10,panel,'05',8,bevel=.13)
pom=[(981,183),(993,183),(1000,191),(1000,210),(992,219),(981,216),(984,207),(984,193)]
poly('Angular_Pommel_Armor',pom,12,-12,silver,'05',.65)
plate('Pommel_Navy_Face',[(986,189),(992,189),(996,194),(996,207),(991,213),(985,212),(989,206),(989,194)],12.7,1.8,frame,'05',.28)
plate('Pommel_Status_Cyan_Slit',[(983,196),(986,196),(986,204),(983,204)],13,1.3,cyan,'05',.18)
bolt('Pommel_Lock',992,201,14,1.3,gold)

# Small deliberately sparse markings. The stylised bird is mesh geometry.
label('Weapon_Designation','RAIKEN Mk-II',638,201,18.2,9)
label('Root_Serial','ASB / 02   -   185',645,209,18.15,2.1,panel)
bird=[[(737,189),(745,195),(748,195),(753,198),(749,193),(759,187),(754,196),(750,199),(751,201),(747,204),(743,200),(745,198)],[(738,191),(744,201),(747,205),(744,206),(740,201)],[(758,190),(751,204),(748,206),(751,207),(756,200)]]
for j,coords in enumerate(bird):plate('Valkyr_Insignia_%02d'%j,coords,18.05,.16,white,'06',.01)
label('Valkyr_Brand','VALKYR',738,213,18.1,3.7)
label('Spine_Caution','CAUTION  //  HIGH ENERGY',456,216,13.1,2.3,gold)
for j,x in enumerate((406,413,420)):
    plate('Maintenance_Tally_%d'%j,[(x,211),(x+3,211),(x+6,215),(x+3,215)],12.3,.2,panel,'06',.02)

for name,loc in (('GRIP_SOCKET',(934,201,0)),('BLADE_TIP',(15,177,0)),('EMITTER_SOCKET',(813,201,0))):
    ob=bpy.data.objects.new('RAIKEN_'+name,None);COL['01'].objects.link(ob);ob.parent=root;ob.location=p(*loc);ob.empty_display_size=.1

exec(compile((P/'sharpen_and_detail.py').read_text(encoding='utf-8'),str(P/'sharpen_and_detail.py'),'exec'))

# Render the actual model, using a neutral studio with a restrained bloom pass.
sc.render.engine='CYCLES';sc.cycles.samples=32;sc.cycles.use_denoising=True
try:
    pref=bpy.context.preferences.addons['cycles'].preferences
    pref.compute_device_type='OPTIX';pref.get_devices()
    for device in pref.devices:device.use=device.type=='OPTIX'
    sc.cycles.device='GPU'
except Exception:pass
world=bpy.data.worlds.new('RAIKEN_Neutral_Studio');world.use_nodes=True
world.node_tree.nodes['Background'].inputs['Color'].default_value=(.13,.16,.21,1)
world.node_tree.nodes['Background'].inputs['Strength'].default_value=.45;sc.world=world
sc.view_settings.view_transform='AgX';sc.view_settings.look='AgX - Medium High Contrast'
sc.view_settings.exposure=-.25
sc.render.resolution_x=2400;sc.render.resolution_y=820;sc.render.resolution_percentage=100
sc.render.image_settings.file_format='PNG';sc.render.image_settings.color_mode='RGBA';sc.render.film_transparent=True
def light(name,loc,target,power,size,color,size_y=None):
    d=bpy.data.lights.new(name,'AREA');d.energy=power;d.shape='RECTANGLE';d.size=size;d.size_y=size_y or size*.7;d.color=color
    o=bpy.data.objects.new(name,d);COL['08'].objects.link(o);o.location=loc;o.rotation_euler=(Vector(target)-o.location).to_track_quat('-Z','Y').to_euler()
light('RAIKEN_Key_Softbox',(-7,-9,9),(-7,0,0),4200,12,(.91,.95,1),6)
light('RAIKEN_Front_Fill',(-7,-13,-2),(-7,0,0),950,14,(.83,.91,1),5)
light('RAIKEN_Upper_Strip',(-8,3,7),(-7,0,0),5000,14,(.75,.88,1),2)
light('RAIKEN_Hilt_Warm_Rim',(2,1,4),(-2,0,0),1200,5,(1,.88,.70),4)
light('RAIKEN_Cutting_Edge_Strip',(-8,-6,-5),(-7,0,-.5),1800,16,(.87,.94,1),1.8)
def cam(name,loc,target,width):
    d=bpy.data.cameras.new('RAIKEN_CAM_'+name);d.type='ORTHO';d.ortho_scale=width;d.lens=70
    o=bpy.data.objects.new('RAIKEN_CAM_'+name,d);COL['08'].objects.link(o);o.location=loc;o.rotation_euler=(Vector(target)-o.location).to_track_quat('-Z','Y').to_euler();return o
cam('SIDE',(-7.9,-36,0),(-7.9,0,0),20.1)
cam('THREE_QUARTER',(1,-30,12),(-7.6,0,0),19.7)
cam('TOP',(-7.9,0,35),(-7.9,0,0),20.1)
cam('HILT',(.6,-13,6),(-1.9,0,.05),6.3)
cam('CROSS_SECTION',(-32,-.15,.3),(-7.8,0,0),4.1)
sc.camera=bpy.data.objects['RAIKEN_CAM_SIDE']
sc.use_nodes=True
nt=sc.compositing_node_group
if nt is None:
    nt=bpy.data.node_groups.new('RAIKEN_Studio_Compositor','CompositorNodeTree');sc.compositing_node_group=nt
nt.nodes.clear()
render=nt.nodes.new('CompositorNodeRLayers')
glow=nt.nodes.new('CompositorNodeGlare')
glow.inputs['Type'].default_value='Fog Glow'
glow.inputs['Quality'].default_value='High'
glow.inputs['Threshold'].default_value=1.8
glow.inputs['Strength'].default_value=.4
glow.inputs['Size'].default_value=.3
nt.links.new(render.outputs['Image'],glow.inputs['Image'])
out=nt.nodes.new('NodeGroupOutput')
if not nt.interface.items_tree:nt.interface.new_socket(name='Image',in_out='OUTPUT',socket_type='NodeSocketColor')
nt.links.new(glow.outputs['Image'],out.inputs['Image'])
im=bpy.data.images.load(str(P/'references/RAIKEN_DESIGN.png'));im.pack()
sc['reference_image']=im.name;sc['weapon_version']='RAIKEN Mk-II / 2026-09-06'
sc['authoring_units']='18.5 m overall; use assembly scale for the 3.50 m production mech'
for screen in bpy.data.screens:
    for ar in screen.areas:
        if ar.type=='VIEW_3D':
            ar.spaces.active.shading.type='MATERIAL'
            ar.spaces.active.clip_end=1000
            ar.spaces.active.region_3d.view_distance=22
            ar.spaces.active.region_3d.view_location=Vector((-7.9,0,0))
            ar.spaces.active.region_3d.view_rotation=sc.camera.rotation_euler.to_quaternion()
            ar.spaces.active.overlay.show_overlays=False
bpy.context.view_layer.objects.active=root;root.select_set(True)
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(P/'RAIKEN_MkII_MASTER.blend'))
bpy.context.view_layer.update()
dg=bpy.context.evaluated_depsgraph_get();tris=0
for o in PARTS:
    if o.type not in ('MESH','FONT'):continue
    ev=o.evaluated_get(dg);me=ev.to_mesh();me.calc_loop_triangles();tris+=len(me.loop_triangles);ev.to_mesh_clear()
(P/'build_manifest.json').write_text(json.dumps({'name':'RAIKEN Mk-II','length_m':18.5,'mesh_and_text_parts':len(PARTS),'evaluated_triangles':tris,'materials':len(M),'beam_objects':[o.name for o in PARTS if o.get('beam_component')],'source_reference':'references/RAIKEN_DESIGN.png','coordinate_system':'X to pommel, -Y front, Z up; origin grip centre'},indent=2),encoding='utf-8')
print('RAIKEN_BUILD_COMPLETE',len(PARTS),tris,flush=True)
if 'render=1' in sys.argv:
    sc.render.resolution_x=2000;sc.render.resolution_y=680
    sc.render.filepath=str(P/'renders/01_SIDE_BEAM_ON.png');bpy.ops.render.render(write_still=True)
    print('RAIKEN_RENDER_COMPLETE',flush=True)
