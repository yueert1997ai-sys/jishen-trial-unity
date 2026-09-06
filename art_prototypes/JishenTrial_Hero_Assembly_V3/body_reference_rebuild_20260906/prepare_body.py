import bpy,math,json,pathlib,hashlib
from mathutils import Vector,Matrix
W=pathlib.Path(__file__).resolve().parent
S=2.974/348
bpy.ops.wm.open_mainfile(filepath=str(W/'ACCEPTED_HEAD_ASSEMBLY_SOURCE.blend'))
sc=bpy.context.scene;bpy.context.view_layer.update()
head=set(bpy.data.objects['HEAD_V3_ROOT'].children_recursive)|{bpy.data.objects['HEAD_V3_ROOT'],bpy.data.objects['Head']}
headworld={o:o.matrix_world.copy() for o in head}
headneck={'14_Low_Profile_Neck_Yaw_Ring','16_Neck_Armor_Collar','17_Neck_Load_Bearing_Pedestal'}
worlds={o:o.matrix_world.copy() for o in sc.objects}
oldp={o.name:o.matrix_world.translation.copy() for o in sc.objects if o.type=='EMPTY'}
newp={}
def p(name,xyz):newp[name]=Vector(xyz)*S
p('Pelvis',(0,0,251));p('Waist',(0,0,274));p('Thorax',(0,1,307))
for s,side in ((1,'L'),(-1,'R')):
 for n,pos in {'Thigh':(31,1,242),'Shin':(58,-1,167),'Foot':(93,2,40),'UpperArm':(64,3,330),'Forearm':(87,0,281),'Hand':(112,-2,226),'Shoulder_Armor_Floating_Pivot':(64,3,340),'Skirt_Hinge':(27,-8,267)}.items():
  p(n+'.'+side,(pos[0]*s,pos[1],pos[2]))
 p('Backpack_Main_Deploy_Hinge.'+side,(24*s,24,345));p('Backpack_Outer_Fold_Pivot.'+side,(37*s,32,341))
p('Backpack_Structural_Mount',(0,21,327));p('Cannon_Right_Cradle_Mount',(-30,19,350));p('Cannon_Pitch_Trunnion',(-37,8,367))
# Preserve original rigid articulation hierarchy, changing the standing joint positions.
def depth(o):
 n=0
 while o.parent:n+=1;o=o.parent
 return n
for o in sorted([o for o in sc.objects if o.type=='EMPTY' and o.name in newp],key=depth):
 m=worlds[o].copy();m.translation=newp[o.name];o.matrix_world=m;bpy.context.view_layer.update()
for o in sorted(headworld,key=depth):o.matrix_world=headworld[o];bpy.context.view_layer.update()
for n in headneck:bpy.data.objects[n].matrix_world=worlds[bpy.data.objects[n]]
bpy.context.view_layer.update()
def bone_map(pa):
 targets={'UpperArm':'Forearm','Forearm':'Hand','Thigh':'Shin','Shin':'Foot'}
 nm,side=pa.split('.')
 aa=oldp[pa];bb=oldp[targets[nm]+'.'+side];na=newp[pa];nb=newp[targets[nm]+'.'+side]
 v=bb-aa;nv=nb-na;q=v.rotation_difference(nv);d=v.normalized();scale=nv.length/v.length
 cross={'UpperArm':1.04,'Forearm':1.0,'Thigh':1.04,'Shin':.87}[nm]
 return lambda p:na+q@(d*(p-aa).dot(d)*scale+((p-aa)-d*(p-aa).dot(d))*cross)
maps={}
for s in ('L','R'):
 for nm in ('UpperArm','Forearm','Thigh','Shin'):maps[nm+'.'+s]=bone_map(nm+'.'+s)
 maps['Foot.'+s]=lambda p,s=s:newp['Foot.'+s]+Vector(((p-oldp['Foot.'+s]).x*.9,(p-oldp['Foot.'+s]).y*.83,(p-oldp['Foot.'+s]).z*1.15))
maps['Thorax']=lambda p:Vector((p.x*.63,p.y*.58,2.64+(p.z-2.64)*.85))
maps['Waist']=lambda p:Vector((p.x*.9,p.y*.9,2.34+(p.z-2.22)*1.08))
maps['Pelvis']=lambda p:Vector((p.x*1.03,p.y*.83,2.10+(p.z-1.99)*.9))
retained=[];removed=[]
bad=('Armor_Lock','Service_Captive','R7_','Power_Bundle','Flex_Loom','Control_Conduit','Lock_','Toe_Flex','R4_','Foot_Continuous','Foot_Sole_','R3_Clavicle','R5_')
for o in list(sc.objects):
 if o.type not in ('MESH','CURVE'):continue
 if o in head or o.name in headneck:continue
 pa=o.parent.name if o.parent else ''
 cs=[c.name for c in o.users_collection]
 keep=pa in maps and any(c in ('FRAME','Joint_Housings') for c in cs) and not any(n in o.name for n in bad) and o.type=='MESH'
 if keep:
  me=o.data.copy();me.transform(worlds[o]);fn=maps[pa]
  for v in me.vertices:v.co=fn(v.co)
  me.update();o.data=me;o.matrix_world=Matrix.Identity(4);o['V3B_retained_existing_component']=True;retained.append(o.name)
 else:
  removed.append(o.name);bpy.data.objects.remove(o,do_unlink=True)
for o in sorted(headworld,key=depth):o.matrix_world=headworld[o];bpy.context.view_layer.update()
sc['accepted_head_unchanged']=True
(W/'body_reuse_manifest.json').write_text(json.dumps({'retained_original_components':retained,'superseded_body_components_in_source_backup':removed,'preserved_head_count':len(head)-2,'source_sha256':hashlib.sha256((W/'ACCEPTED_HEAD_ASSEMBLY_SOURCE.blend').read_bytes()).hexdigest()},indent=2))
print('BODY_PREPARED',len(retained),'existing mechanical components retained;',len(removed),'body plates and attachments superseded',flush=True)
