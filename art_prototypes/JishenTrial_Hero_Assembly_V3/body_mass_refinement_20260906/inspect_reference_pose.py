import bpy,pathlib,json
from mathutils import Vector
W=pathlib.Path(__file__).resolve().parent
bpy.ops.wm.open_mainfile(filepath=str(W.parent/'JishenTrial_ASSEMBLED_MASTER.blend'))
bpy.context.view_layer.update()
data=[]
for o in bpy.context.scene.objects:
 if o.type!='EMPTY':continue
 if len(o.children_recursive)<5 and not any(k in o.name for k in ('GRIP','TIP','Grip','Root')):continue
 data.append({'name':o.name,'parent':o.parent.name if o.parent else None,'pos':list(o.matrix_world.translation),'axes':[list((o.matrix_world.to_3x3()@Vector(v)).normalized()) for v in ((1,0,0),(0,1,0),(0,0,1))],'children':len(o.children_recursive),'props':{k:str(v)[:500] for k,v in o.items()}})
print('POSE_RIG_DATA',json.dumps(data,indent=2))
for name in ('Hand.R','Hand.L','Forearm.R','Forearm.L'):
 o=bpy.data.objects.get(name)
 if o:
  print('CHILDREN',name,json.dumps([{'name':c.name,'pos':list(c.matrix_world.translation),'parent':c.parent.name} for c in o.children if not c.name.startswith('RK_')]))
root=bpy.data.objects['AntiShip_Blade_Display_Root']
for name in ('RAIKEN_GRIP_SOCKET','RAIKEN_BLADE_TIP','V3B_Sword_Grip_Socket','Hand.R','Hand.L'):
 o=bpy.data.objects.get(name)
 if o:print('IN_WEAPON',name,list(root.matrix_world.inverted()@o.matrix_world.translation))
