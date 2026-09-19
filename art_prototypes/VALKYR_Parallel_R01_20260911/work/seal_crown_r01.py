import bpy,bmesh,math
from pathlib import Path
from mathutils import Vector
OUT=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(OUT/'VALKYR_PARALLEL_R01.blend'))
head=bpy.data.collections['05_HEAD_SHELLS'];blue=bpy.data.materials['Valkyr blue ceramic alloy'];neck=bpy.data.objects['HEAD_MOUNT']
def skin(n,verts,faces,mat,col,parent,thick):
 me=bpy.data.meshes.new(n);me.from_pydata(verts,[],faces);me.update();bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free()
 ob=bpy.data.objects.new(n,me);col.objects.link(ob);me.materials.append(mat);ob.parent=parent;ob.matrix_parent_inverse=parent.matrix_world.inverted()
 so=ob.modifiers.new('Normal thickness and rim','SOLIDIFY');so.thickness=thick;so.offset=-1;so.use_even_offset=True
 be=ob.modifiers.new('Local edge bevel','BEVEL');be.width=.003;be.segments=2;ob.modifiers.new('Weighted normals','WEIGHTED_NORMAL');return ob
old=bpy.data.objects['Crown continuous centre spine'];bpy.data.objects.remove(old,do_unlink=True)
vs=[]
for y,z,w in [(-.18,6.31,.125),(.02,6.345,.155),(.18,6.285,.143),(.32,6.12,.10)]:
 for f in (-1,0,1):vs.append((f*w*.92,y,5.42+(z+(0.025 if f==0 else 0)-5.42)*.89-.07))
faces=[(j*3+k,j*3+k+1,(j+1)*3+k+1,(j+1)*3+k) for j in range(3) for k in range(2)]
skin('Crown sealed continuous ridge',vs,faces,blue,head,neck,.029)
# Rebuild horizontal/oblique covers with thickness along normals, not a fixed-axis extrusion.
for old in [o for o in list(bpy.data.objects) if o.name.startswith(('Clavicle shaped cap.','Instep floating tile.'))]:
 N=len(old.data.vertices)//3;pts=[old.matrix_world@v.co for v in old.data.vertices[:N]];name=old.name;mat=old.data.materials[0];col=old.users_collection[0];parent=old.parent
 bpy.data.objects.remove(old,do_unlink=True);skin(name,pts,[tuple(range(N))],mat,col,parent,.035)
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'VALKYR_PARALLEL_R01.blend'),compress=True)
print('Crown and oblique covers rebuilt with normal thickness')
