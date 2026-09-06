from body_common import *
LC=bpy.data.collections.new('BODY_V3 | review lighting');SC.collection.children.link(LC)
CC=bpy.data.collections.new('BODY_V3 | reference inspection cameras');SC.collection.children.link(CC)
for o in SC.objects:
 if o.type=='LIGHT':o.hide_render=True
def area(name,loc,power,size,color,target=(0,0,1.8)):
 d=bpy.data.lights.new('V3B_'+name,'AREA');d.energy=power*.55;d.shape='DISK';d.size=size;d.color=color
 o=bpy.data.objects.new('V3B_'+name,d);LC.objects.link(o);o.location=loc;o.rotation_euler=(Vector(target)-o.location).to_track_quat('-Z','Y').to_euler()
area('Key',(-3.4,-4.2,6),950,4,(.89,.94,1))
area('Fill',(4,-3,3.8),620,3,(.76,.86,1))
area('Rim',(2.5,3,5.7),1300,3,(.84,.92,1))
area('Warm_Rim',(-3,2,2.6),500,2.5,(1,.87,.69))
SC.world=bpy.data.worlds.new('V3B_Studio_World');SC.world.use_nodes=True
bg=next(n for n in SC.world.node_tree.nodes if n.type=='BACKGROUND');bg.inputs['Color'].default_value=(.14,.17,.22,1);bg.inputs['Strength'].default_value=.25
def camera(name,loc,target,scale):
 d=bpy.data.cameras.new('V3B_CAM_'+name);d.type='ORTHO';d.ortho_scale=scale;d.lens=65
 o=bpy.data.objects.new('V3B_CAM_'+name,d);CC.objects.link(o);o.location=loc;o.rotation_euler=(Vector(target)-o.location).to_track_quat('-Z','Y').to_euler();return o
camera('FRONT',(0,-12,1.82),(0,0,1.82),3.92)
camera('LEFT',(12,0,1.82),(0,0,1.82),3.92)
camera('RIGHT',(-12,0,1.82),(0,0,1.82),3.92)
camera('SIDE_CORE',(12,0,1.82),(0,0,1.82),3.92)
camera('BACK',(0,12,1.82),(0,0,1.82),3.92)
camera('THREE_QUARTER',(5,-9,4.5),(0,0,1.83),4.12)
camera('REAR_QUARTER',(-5,9,4.1),(0,0,1.8),4.12)
camera('CHEST',(1,-8,3.5),(0,-.03,2.79),1.95)
camera('WAIST',(2,-7,2.9),(0,0,2.18),1.25)
camera('LEGS',(3,-9,2.6),(0,0,1.04),2.45)
camera('ARM',(-5,-9,3.4),(-.73,0,2.3),1.75)
camera('FOOT',(3,-7,2),( .80,-.03,.35),1.16)
camera('BACKPACK',(4,8,4.7),(0,.28,2.8),1.96)
camera('CANNON',(-4,-6,4.5),(-.40,0,3.23),1.62)
SC.camera=bpy.data.objects['V3B_CAM_THREE_QUARTER']
SC.render.engine='CYCLES';SC.cycles.samples=40;SC.cycles.use_denoising=True
SC.view_settings.view_transform='Standard';SC.view_settings.exposure=0;SC.view_settings.gamma=1
SC.render.resolution_x=SC.render.resolution_y=1600;SC.render.resolution_percentage=100;SC.render.film_transparent=True
SC.render.image_settings.file_format='PNG';SC.render.image_settings.color_mode='RGBA'
im=bpy.data.images.load(str(W/'REFERENCE.png'),check_existing=True);im.pack()
for screen in bpy.data.screens:
 for ar in screen.areas:
  if ar.type=='VIEW_3D':
   ar.spaces.active.shading.type='MATERIAL';ar.spaces.active.region_3d.view_distance=5.5;ar.spaces.active.region_3d.view_location=Vector((0,0,1.8))
SC['reference_image']=im.name
