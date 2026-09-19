import bpy,bmesh,pathlib,math,json
from mathutils import Vector,Matrix
from mathutils.geometry import tessellate_polygon
W=pathlib.Path(__file__).resolve().parent;R=W.parent;S=.01;PARTS=[]
M={m.name.replace('FAZZ ',''):m for m in bpy.data.materials if m.name.startswith('FAZZ ')}
asset=bpy.data.collections['FAZZ_COMPLETE_ASSEMBLY'];PARENT=bpy.data.objects['FAZZ_MASTER_ROOT'];COL=bpy.data.collections['17 V3 INTERNAL FRAME']
exec((W/'v3_geometry.py').read_text())
def deform(p):
 p=Vector(p);z=p.z;p.z=z if z<=.45 else (.45+(z-.45)*.8 if z<2.05 else z-.32);p.x*=1+.1*max(0,min(1,(z-2.3)/.7));return p
def legpose(p,s,foot=False):
 p=p.copy();p.x+=s*.17*max(0,min(1,(2.35-p.z)/1.8));p.y-=.09*max(0,1-abs(p.z-1.65)/.9)
 if foot:
  c=Vector((s*.60,-.1,0));p=c+Matrix.Rotation(s*math.radians(-10),3,'Z')@(p-c)
 return p
for o in list(asset.all_objects):
 if o.name.startswith(('FAZZ_Calf service slotted screw','FAZZ_Calf cover recessed guide')):bpy.data.objects.remove(o,do_unlink=True);continue
 if o.name.startswith('FAZZ_V2 segmented toe'):
  s=-1 if ' L' in o.name else 1;mw=o.matrix_world.copy();inv=mw.inverted()
  for v in o.data.vertices:v.co=inv@legpose(mw@v.co,s,True)
for s,side in [(-1,'L'),(1,'R')]:
 PARENT=bpy.data.objects['SHIN_'+side];mi=lambda p:(s*p[0],p[1],p[2]);first=len(PARTS)
 # Exposed twin carriage cylinders are tied to the tibia by crossheads inside the access opening.
 for x in [31,37]:
  piston('V3 exposed calf carriage '+side+str(x),mi((x,-21,97)),mi((x,-21,145)),1.3)
 for z in [98,144]:
  box('V3 calf carriage crosshead '+side+str(z),mi((34,-17,z)),(11,10,3),'frame',.18)
  for x in [31,37]:bolt('V3 carriage mount '+side+str(x)+str(z),mi((x,-23,z)),(0,-1,0),.55)
 # A narrow metal lip follows both edges of the rectangular service opening.
 for x in [27.7,40.3]:
  plate('V3 tibia access rim '+side+str(x),[mi((x,-28,95)),mi((x+.7,-28,95)),mi((x+.7,-26,147)),mi((x,-26,147))],(0,.8,0),'steel',.05)
 for o in PARTS[first:]:
  mw=o.matrix_world.copy();inv=mw.inverted()
  for v in o.data.vertices:v.co=inv@legpose(deform(mw@v.co),s)
# Resolve microscopic boolean/bevel slivers on the two opened calf plates.
for side in ['L','R']:
 o=bpy.data.objects['FAZZ_Calf front multi-plane armour '+side];dg=bpy.context.evaluated_depsgraph_get();me=bpy.data.meshes.new_from_object(o.evaluated_get(dg))
 bm=bmesh.new();bm.from_mesh(me);bmesh.ops.triangulate(bm,faces=list(bm.faces));bmesh.ops.dissolve_degenerate(bm,dist=1e-5,edges=list(bm.edges));bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=1e-6);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));assert all(e.is_manifold for e in bm.edges);bm.to_mesh(me);bm.free();o.modifiers.clear();o.data=me
sc=bpy.context.scene;sc.camera=bpy.data.objects['CAM_hero'];bpy.ops.wm.save_as_mainfile(filepath=str(R/'FAZZ_V3_INNER_FRAME.blend'))
sc.render.resolution_x=1300;sc.render.resolution_y=1500;sc.cycles.samples=36
sc.render.filepath=str(R/'renders'/'frame_v3_hero.png');bpy.ops.render.render(write_still=True)
# Naked frame is saved as a separate editable inspection file as well as an actual render.
heads=set(bpy.data.collections['FAZZ_HEAD_ASSET'].all_objects)
for o in asset.all_objects:
 if o.type!='MESH' or o in heads:continue
 keep=COL in o.users_collection or any(c.name.startswith(('01 ','05 ','06 ','08 ')) for c in o.users_collection)
 if not keep:o.hide_render=True;o.hide_set(True)
bpy.ops.wm.save_as_mainfile(filepath=str(R/'FAZZ_V3_FRAME_INSPECTION.blend'))
sc.render.filepath=str(R/'renders'/'frame_v3_unarmoured.png');bpy.ops.render.render(write_still=True)
