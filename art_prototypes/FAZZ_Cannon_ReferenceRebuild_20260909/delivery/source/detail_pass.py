# Additional modeled detail. Uses the constructors and materials of build_fazz.py.
C['detail2']=coll('07 Engraving vents and service hardware')
def carve(target,name,p,sz):
 cutter=box(name+' temporary cutter',p,sz,'dark','detail2',0)
 mod=target.modifiers.new(name,'BOOLEAN');mod.operation='DIFFERENCE';mod.solver='EXACT';mod.object=cutter
 bpy.ops.object.select_all(action='DESELECT');target.select_set(True);bpy.context.view_layer.objects.active=target
 bpy.ops.object.modifier_move_to_index(modifier=mod.name,index=0)
 bpy.ops.object.modifier_apply(modifier=mod.name)
 bpy.data.objects.remove(cutter,do_unlink=True)
def stamp(name,text,x,y,z,size=.045):
 cu=bpy.data.curves.new(name,'FONT');cu.body=text;cu.size=size;cu.extrude=.0006;cu.resolution_u=2
 o=finish(bpy.data.objects.new(name,cu),name,'steel','detail2',0)
 o.location=(x,y,z);o.rotation_euler=(math.pi/2,0,0 if y<0 else math.pi)
 bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o;bpy.ops.object.convert(target='MESH')
 return bpy.context.object
def conform_plate(name,profile,s,offset=.03,depth=.025,material='panel'):
 n=len(profile);v=[]
 for d in (offset,offset+depth):
  v.extend((x,s*(rear_surface(x,z)+d),z) for x,z in profile)
 return mesh(name,v,[tuple(range(n-1,-1,-1)),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)],material,'detail2',min(.005,depth*.18))

for s in (-1,1):
 # Visible mechanical breakup in the long empty rear housing.
 conform_plate('Rear access plate gasket '+str(s),[(1.65,.40),(1.65,.78),(2.06,.80),(2.16,.71),(2.16,.37),(1.72,.37)],s,.029,.018,'dark')
 conform_plate('Rear inset access lid '+str(s),[(1.69,.43),(1.69,.75),(2.03,.77),(2.115,.69),(2.115,.41),(1.75,.405)],s,.047,.026,'panel')
 for x,z in ((1.76,.69),(2.035,.67),(1.76,.47),(2.035,.47)):
  bolt('Rear access screw',x,s*(rear_surface(x,z)+.087),z,.022)
 x,z=1.91,.59;y=s*(rear_surface(x,z)+.087)
 cyl('Access recessed quarter turn',(x,y,z),.065,.014,'dark','detail2',n=12)
 cyl('Quarter turn metal center',(x,y+s*.009,z),.041,.012,'steel','detail2',n=8)
 box('Quarter turn slot',(x,y+s*.017,z),(.052,.008,.009),'dark','detail2',.001)
 # Eight recessed louvers in an actual boolean-cut carrier frame.
 vent=box('Rear vent carrier '+str(s),(2.64,s*.435,.635),(.70,.07,.32),'panel','detail2',.014)
 for i in range(7):
  x=2.365+i*.091
  carve(vent,'Cut ventilation slot %s %s'%(s,i),(x,s*.47,.64),(.034,.046,.211))
  box('Vent dark inner well',(x,s*.453,.64),(.032,.011,.20),'dark','detail2',.002)
  box('Vent angled internal fin',(x+.009,s*.475,.64),(.009,.012,.169),'steel','detail2',.002)
 for x in (2.335,2.945):
  for z in (.507,.77):bolt('Vent carrier retaining screw',x,s*.479,z,.018)
 # Long engraved panel borders run with the shell taper.
 for coords in [[(3.13,.51),(3.13,.80)],[(3.56,.55),(3.69,.72),(4.35,.70)],[(4.49,.57),(4.50,.71)]]:
  tube('Rear recessed panel separation',[(x,s*(rear_surface(x,z)+.029),z) for x,z in coords],.009,'dark','detail2')
 conform_plate('Tail slim access gasket '+str(s),[(4.61,.54),(4.64,.65),(5.24,.61),(5.27,.53)],s,.03,.010,'dark')
 conform_plate('Tail slim access lid '+str(s),[(4.65,.55),(4.67,.631),(5.21,.597),(5.23,.541)],s,.041,.013,'panel')
 for x,z in ((4.7,.59),(5.17,.568)):bolt('Tail access bolt',x,s*(rear_surface(x,z)+.069),z,.016)
 for i in range(3):
  x=3.38+i*.075;z=.59
  conform_plate('Rear small yellow ID stripe',[(x,z),(x+.028,z),(x+.028,z+.095),(x,z+.095)],s,.03,.006,'yellow')
 stamp('Rear technical ID '+str(s),'ACCESS  /  09',3.67,s*(rear_surface(3.67,.60)+.032),.60,.047)
 # Crest has a sunken panel with a shaped perimeter and a retained latch.
 crest=box('Crest shallow framed panel '+str(s),(.27,s*.325,1.205),(.68,.055,.26),'panel','detail2',.017)
 carve(crest,'Crest panel milled recess',(.25,s*.358,1.205),(.51,.035,.152))
 box('Crest panel dark bed '+str(s),(.25,s*.343,1.205),(.495,.008,.14),'dark','detail2',.006)
 for x in (-.015,.535):bolt('Crest captive frame screw',x,s*.366,1.21,.019)
 stamp('Crest small stencil '+str(s),'SERVICE',.08,s*.351,1.18,.043)
 prism('Crest diagonal engraving '+str(s),[(.77,.83),(.85,.83),(1.08,.55),(1.01,.55)],s*.367,.008,'dark','detail2',.005)
 for i in range(3):box('Crest hinge rib '+str(s),(1.24,s*.44,.39+i*.055),(.14,.015,.018),'panel','detail2',.003)
 # White receiver gets real shallow machining instead of a featureless rectangle.
 plate=bpy.data.objects['White receiver inset panel '+str(s)]
 for i in range(3):
  x=-.42+i*.30
  carve(plate,'Receiver shallow milled channel',(x,s*.402,.055),(.225,.023,.028))
  box('Receiver channel inset',(x,s*.393,.055),(.21,.004,.017),'panel','detail2',.002)
 for x in (-.48,-.18,.12):
  for z in (-.045,.145):bolt('Receiver small flush fastener',x,s*.405,z,.016)
 box('Receiver inspection socket gasket '+str(s),(.37,s*.411,.065),(.15,.014,.145),'dark','detail2',.01)
 box('Receiver inspection socket insert '+str(s),(.37,s*.423,.065),(.104,.014,.10),'panel','detail2',.008)
 for i in range(3):box('Receiver diagnostic port contact',(.343+i*.029,s*.434,.065),(.01,.008,.053),'steel','detail2',.001)
 stamp('Receiver lock stencil '+str(s),'LOCK',-.47,s*.411,-.135,.034)
 # Forward jacket details are concentrated in service areas, preserving its long profile.
 plate=bpy.data.objects['Barrel service plate '+str(s)]
 for i in range(5):
  x=-3.25+i*.115
  carve(plate,'Forebarrel recessed vent',(x,s*.311,.07),(.066,.025,.063))
  box('Forebarrel dark slot insert',(x,s*.301,.07),(.06,.004,.054),'dark','detail2',.002)
 for x,z in ((-4.20,.29),(-3.69,.33),(-2.68,.37)):
  box('Forward inset fastener bed',(x,s*.266,z),(.12,.023,.10),'panel','detail2',.009)
  bolt('Forward inset captive screw',x,s*.285,z,.026)
 for x in (-4.18,-3.85):
  box('Forebarrel diagonal assembly cut',(x,s*.251,.14),(.015,.018,.18),'dark','detail2',.002)
 for x in (-5.2,-4.66):
  bolt('Nose lower guard retainer',x,s*.292,.14,.021)
 # Cable endpoints now terminate in layered locking glands.
 for x,y,z in [(-5.28,s*.16,.475),(-4.56,s*.16,.485),(-4.8,s*.21,.075),(-3.91,s*.22,.075),(1.17,s*.22,1.01),(2.54,s*.22,1.02)]:
  cyl('Cable connector ferrule',(x,y,z),.064,.083,'panel','detail2',(0,0,1),12)
  cyl('Cable connector metal ring',(x,y,z+.041),.057,.017,'steel','detail2',(0,0,1),16,.003)
 for x in (1.76,2.44):
  box('Rear keel maintenance seam',(x,s*.339,-.30),(.018,.015,.15),'dark','detail2',.002)
 for x in (1.56,2.63):bolt('Rear keel service lock',x,s*.348,-.28,.024)

# Additional top-surface machining reads in three-quarter view.
for x,w,z in [(-4.01,.25,.531),(-2.72,.27,.569)]:
 box('Top barrel inset rail bed',(x,0,z),(.63,.19,.02),'dark','detail2',.009)
 box('Top barrel service rail',(x,0,z+.016),(.56,.12,.016),'panel','detail2',.005)
 for j in range(4):box('Top rail cross notch',(x-.19+j*.124,0,z+.026),(.017,.123,.007),'dark','detail2',.001)
box('Rear dorsal thermal bay',(3.22,0,.984),(.95,.31,.027),'dark','detail2',.017)
for i in range(9):
 x=2.84+i*.087
 box('Dorsal transverse thermal vane',(x,0,.999),(.026,.258,.017),'panel','detail2',.004)
for x in (2.80,3.64):
 for y in (-.11,.11):cyl('Dorsal bay flush bolt',(x,y,1.016),.018,.008,'steel','detail2',(0,0,1),6,.002)

# Circular module gains stepped machined rings, radial inserts and locking shoes.
for i in range(8):
 a=(i+.5)*math.tau/8
 x=3.83+math.cos(a)*.292;z=-.23+math.sin(a)*.292
 cyl('Rotary sector recessed pin bed',(x,-.736,z),.052,.009,'dark','detail2',n=12,bevel=.003)
 cyl('Rotary sector locking pin',(x,-.746,z),.025,.014,'steel','detail2',n=8,bevel=.002)
 x=3.83+math.cos(a)*.432;z=-.23+math.sin(a)*.432
 o=box('Rotary segmented perimeter shoe',(x,-.658,z),(.065,.11,.045),'panel','detail2',.006)
 o.rotation_euler.y=-a
cyl('Rotary center recessed bearing',(3.83,-.748,-.23),.077,.018,'dark','detail2',n=24,bevel=.003)
cyl('Rotary center hexagonal lock',(3.83,-.762,-.23),.049,.018,'steel','detail2',n=6,bevel=.003)
box('Rotary hub screw slot',(3.83,-.774,-.23),(.055,.006,.012),'dark','detail2',.001)

# Muzzle internal emitter stack and outer collar retention, entirely fictional prop geometry.
o=ring('Muzzle inner stepped collar',-5.58,.22,.144,.127,.057,'panel',16)
o.modifiers[0].width=.001
o=ring('Muzzle deep emitter retaining ring',-5.36,.22,.131,.117,.038,'steel',16)
o.modifiers[0].width=.001
for i in range(8):
 a=(i+.5)*math.tau/8
 y=math.cos(a)*.183;z=.22+math.sin(a)*.183
 cyl('Muzzle face retainer',(-5.773,y,z),.013,.01,'dark','detail2',(-1,0,0),8,.001)

root['detail_revision']='02: recessed service covers, boolean-cut vents and milled receiver channels, mechanical drum, connector glands, muzzle layers'
# Prevent tiny hard-surface details from collapsing when bevel width reaches their thickness.
for attempt in range(5):
 bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get();bad=[]
 for o in root.children_recursive:
  if o.type!='MESH':continue
  ev=o.evaluated_get(dg);me=ev.to_mesh();me.update()
  degenerate=any(p.area<=1e-16 for p in me.polygons);ev.to_mesh_clear()
  if degenerate:bad.append(o)
 if not bad:break
 print('REDUCING_COLLAPSED_BEVELS',attempt,[o.name for o in bad],flush=True)
 for o in bad:
  bevels=[m for m in o.modifiers if m.type=='BEVEL']
  if bevels:
   for m in bevels:m.width*=.30
  else:
   bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.dissolve_degenerate(bm,dist=1e-8,edges=list(bm.edges));bm.to_mesh(o.data);bm.free()
else:raise RuntimeError('Unresolved degenerate surface details: '+str([o.name for o in bad]))
print('DETAIL_PASS_COMPLETE',len(C['detail2'].objects),flush=True)
