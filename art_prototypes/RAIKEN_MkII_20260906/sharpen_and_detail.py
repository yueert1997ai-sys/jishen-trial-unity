"""Second art pass: cutting geometry, interlocking armor, and working mechanisms."""
# Radii are subordinate to long razor edges and crisp armor corners.
for ob in PARTS:
    for mod in list(ob.modifiers):
        if mod.type=='BEVEL':
            if 'Honed_Leading_Edge' in ob.name or ob.get('beam_component'):
                ob.modifiers.remove(mod)
                continue
            mod.width*=.60
            if any(k in ob.name for k in ('Cutting','Tapered','Guard_','Tip_')):mod.width*=.45

# A second narrower edge grind makes the blade read as hardened steel when off.
for j in range(1,len(edge)):
    a=Vector(edge[j-1]);b=Vector(edge[j]);delta=(b-a).normalized();inward=Vector((delta.y,-delta.x))
    # Longitudinal hairline above the edge, with tiny cross-edge indexing scars.
    bar('Honed_Edge_Secondary_Facet_%02d'%j,[tuple(a+inward*1.0),tuple(b+inward*1.0)],.65,.62,panel,'03')
for j,(x,z) in enumerate(((212,218),(507,237),(749,253))):
    bar('Cutting_Bevel_Index_%02d'%j,[(x+3,z-7),(x-1,z+1)],2.4,.55,frame,'03')

# Swept, faceted tip inset and a narrower point cap; no rounded safety nose.
plate('Tip_Long_Titanium_Facet',[(17,177.3),(93,176.4),(103,184),(93,187)],1.9,1.2,silver,'03',.035)
plate('Tip_Dark_Piercing_Fuller',[(64,179.2),(109,178.8),(122,188.8),(111,189.2)],4.35,.7,navy,'02',.055)
bar('Tip_Engraved_Hardening_Line',[(105,183),(155,189),(174,197)],6.3,.55,frame,'06')

# Back-of-blade open cooling channel. These are separated geometry, not a texture.
for j,(xa,xb,z,dep) in enumerate(((309,366,186,10.6),(401,451,187,11.1),(498,539,194,13.0),(573,611,180,14.0))):
    plate('Cooling_Well_%02d'%j,[(xa,z-2),(xb,z-2),(xb+3,z+4),(xa+3,z+4)],dep,.75,black,'01',.18)
    count=int((xb-xa)/5.7)
    for k in range(count):
        x=xa+3+k*5.7
        plate('Cooling_Fin_%s_%02d'%(j,k),[(x,z-1.6),(x+1.7,z-1.6),(x+3,z+3.7),(x+1.3,z+3.7)],dep+1.1,1.25,panel,'01',.07)
    bar('Cooling_Well_Lower_Lip_%02d'%j,[(xa+4,z+4),(xb+2,z+4)],dep+1.25,.7,silver,'01')

# Longitudinal spine bridge, segmented insulated conductor and keyed load lugs.
for j,(x,z,le,dep) in enumerate(((323,173,38,13),(424,172,43,13.5),(516,168,38,14.5),(633,162,40,15.4))):
    plate('Spine_Interlocking_Shoe_%02d'%j,[(x-le/2,z+1),(x+le/2,z+1),(x+le/2-4,z-5),(x-le/2+2,z-4)],dep,3.3,frame,'01',.16)
    plate('Spine_Bridge_Armor_%02d'%j,[(x-le/2+4,z-4),(x+le/2-3,z-5),(x+le/2-7,z-1),(x-le/2+7,z)],dep+1.2,1.6,panel,'02',.15)
    for sign in (-1,1):
        rod('Spine_Exposed_Piston_%s_%s'%(j,sign),p(x-le/2-6,z+3,sign*(dep-.6)),p(x-le/2+4,z+3,sign*(dep-.6)),1.25,silver,'01',12,bevel=.04)
    bolt('Spine_Shoe_Lock_%02d'%j,x+le*.15,z-2,dep+1.7,.92,gold)

# Recessed frame slots beneath the lower armor leaves with inset capacitors.
for j,(x,z,le,dep) in enumerate(((278,207,23,10.5),(386,216,29,12),(475,224,26,13),(563,231,25,14.5),(675,239,24,15))):
    plate('Lower_Edge_Service_Recess_%02d'%j,[(x-le/2,z-2),(x+le/2,z-1),(x+le/2+3,z+3),(x-le/2+2,z+2)],dep,1.3,black,'01',.13)
    for k in range(3):
        xx=x-le*.3+k*le*.3
        plate('Flux_Capacitor_%s_%s'%(j,k),[(xx,z-1),(xx+le*.17,z-.5),(xx+le*.17+1,z+1.3),(xx+1,z+1)],dep+.7,.7,gold if k==1 else panel,'01',.06)

# Asymmetric root panel seams, tiny datum marks, and visible captive fasteners.
bar('Root_Nameplate_Upper_Recess',[(649,179),(700,178),(706,182)],18.0,.75,navy)
bar('Root_Nameplate_Lower_Recess',[(649,213),(709,218),(722,215)],18.0,.70,navy)
plate('Receiver_Inset_Access_Door',[(609,193),(622,192),(628,205),(615,204)],16.4,1.2,frame,'02',.18)
bolt('Receiver_Access_Lock',617,198,17.8,.8,gold)
for j in range(5):
    x=690+j*3.3
    bar('Root_Datum_Engraving_%02d'%j,[(x,212),(x+1.2,214)],18.2,.45,panel,'06')
label('Receiver_Technical_Serial','RK-02 / LFE-185',664,183,18.15,2.2,panel)
label('Tip_Load_Rating','HTC // 082',145,185,6.4,1.85,white)

# A smaller emitter well sits inside a detailed mechanism, surrounded by clamps.
for j,(coords,dep) in enumerate((([(805,181),(805,212),(808,217)],25), ([(821,182),(821,212),(818,218)],25))):
    bar('Emitter_Thin_Titanium_Rail_%02d'%j,coords,dep,.85,silver,'04')
for sign in (-1,1):
    # Return pipes follow the silhouette outside the optical cell.
    for j,(a,b,c) in enumerate((((796,184),(792,200),(799,217)),((827,182),(836,198),(829,216)))):
        rod('Reactor_Hydraulic_Line_%s_%s_A'%(sign,j),p(*a,sign*21.2),p(*b,sign*21.2),1.3,gold,'04',12,bevel=.09)
        rod('Reactor_Hydraulic_Line_%s_%s_B'%(sign,j),p(*b,sign*21.2),p(*c,sign*21.2),1.3,panel,'04',12,bevel=.09)
        ring('Reactor_Hydraulic_Coupler_%s_%s'%(sign,j),p(*b,sign*20.8),p(*b,sign*23),2.1,1.2,frame,'04',8,.1)
    # Fluted spindle on the grip side behind the front plate.
    for j in range(6 if sign==1 else 0):
        angle=math.tau*j/6
        rod('Receiver_Axial_Tie_%s_%s'%(sign,j),p(847,201+math.cos(angle)*13,sign*math.sin(angle)*13),p(870,201+math.cos(angle)*10,sign*math.sin(angle)*10),.9,silver,'04',10,bevel=.03)
for j,(x,z) in enumerate(((793,178),(832,179),(838,218),(793,219))):
    plate('Reactor_Serrated_Clamp_%02d'%j,[(x-3,z-2),(x+4,z-1),(x+5,z+3),(x+2,z+5),(x-2,z+2)],25,3,panel,'04',.18)
    bolt('Reactor_Clamp_Socket_%02d'%j,x+1,z+1,25.4,1,gold)

# Fin roots gain real cooling notches, bearing plates, a ram and locking pawls.
for j,(x,z,flip,dep) in enumerate(((770,151,-1,15),(861,157,-1,12),(779,248,1,15),(848,252,1,12))):
    plate('Guard_Root_Recess_%02d'%j,[(x-6,z),(x+7,z+flip*5),(x+8,z+flip*9),(x-4,z+flip*4)],dep+1.5,1.5,black,'04',.12)
    for k in range(3):
        xx=x-3+k*3.5
        bar('Guard_Root_Cooling_Rib_%s_%s'%(j,k),[(xx,z+flip*2),(xx+1,z+flip*5)],dep+2.1,.8,panel,'04')
    for sign in (-1,1):
        rod('Guard_Ram_%s_%s'%(j,sign),p(x,z,sign*7),p(x+12,z+flip*10,sign*6),1.2,silver,'04',12,bevel=.05)
    plate('Guard_Locking_Pawl_%02d'%j,[(x-4,z),(x,z-2*flip),(x+3,z+flip*2),(x-1,z+flip*4)],dep+3,2.1,gold,'04',.15)

# Grip control strip, pommel ventilation and serial plaques.
for j in range(7):
    x=889+j*12.5
    bar('Grip_Anti_Slip_Groove_%02d'%j,[(x+1,196),(x+6,196)],10.3,.55,black,'05')
plate('Grip_Thumb_Control_Plate',[(882,194),(888,194),(890,198),(884,198)],10.2,2.3,frame,'05',.15)
bar('Grip_Thumb_Control_Slit',[(884,195.5),(887,195.5)],10.6,.75,cyan,'05')
for j in range(3):
    bar('Pommel_Heat_Slot_%d'%j,[(989,194+j*5),(993,194+j*5)],14.3,.95,black,'05')

# Rear-side text is placed inside the corresponding mirrored armor, not beyond it.
bpy.context.view_layer.update()
for ob in PARTS:
    if ob.type=='FONT' and ob.name.endswith('_B'):
        ob.location.x+=ob.dimensions.x
root['revision']='02 / more mechanism detail; elongated tips; honed single edge'
root['Beam_On']=True
