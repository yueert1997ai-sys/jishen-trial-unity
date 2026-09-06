"""Restore only the right hand's articulation from the frozen source; never save that source."""
import bpy, json, pathlib, re, hashlib
from mathutils import Vector
root=pathlib.Path(__file__).resolve().parents[1]
source=root.parents[1]/'art_prototypes/JishenTrial_Hero_Assembly_V3/body_mass_refinement_20260906/iteration_v3/game_update/SOURCE_CURRENT_MASTER.blend'
bpy.ops.wm.open_mainfile(filepath=str(source))
hand=bpy.data.objects['Hand.R']; inv=hand.matrix_world.inverted()
def convert(v): return Vector((-v.x,v.y,v.z))
grip=convert(inv@bpy.data.objects['V3B_Sword_Grip_Socket'].matrix_world.translation)
long=grip.normalized(); transverse=(Vector((0,1,0))-long*long.y).normalized()
original=convert((inv@bpy.data.objects['Finger_3_Joint_0.R'].matrix_world.translation)-(inv@bpy.data.objects['Finger_0_Joint_0.R'].matrix_world.translation)).normalized()
correct=original.rotation_difference(transverse)
def adjusted(v): return grip+correct@(v-grip)
objects=[o for o in hand.children_recursive if o.type=='MESH' and o.name.startswith('V3B_')]
for o in objects:
    for mod in o.modifiers:
        if mod.type=='BEVEL': mod.segments=1
bpy.context.view_layer.update(); dg=bpy.context.evaluated_depsgraph_get()
groups={'Palm':dict(parent='',position=Vector((0,0,0)),objects=[])}
for i in range(4):
    for j in range(3):
        name=f'Finger_{i}_{j}'; joint=bpy.data.objects[f'Finger_{i}_Joint_{j}.R']
        groups[name]=dict(parent=f'Finger_{i}_{j-1}' if j else '',position=adjusted(convert(inv@joint.matrix_world.translation)),objects=[])
for j in range(3):
    ob=bpy.data.objects[f'V3B_Opposed_Grip_Thumb_{j}.R_Pivot']
    center=sum((Vector(v) for v in ob.bound_box),Vector())/8
    groups[f'Thumb_{j}']=dict(parent=f'Thumb_{j-1}' if j else '',position=adjusted(convert(inv@ob.matrix_world@center)),objects=[])
for o in objects:
    match=re.search(r'Grip_Finger_(\d)_Phalanx_(\d)',o.name)
    thumb=re.search(r'Opposed_Grip_Thumb_(\d)',o.name)
    group=f'Finger_{match[1]}_{match[2]}' if match else f'Thumb_{thumb[1]}' if thumb else 'Palm'
    groups[group]['objects'].append(o)
materials=[]; parts=[]
def vec(v): return dict(zip(('x','y','z'),(round(x,7) for x in v)))
for name,g in groups.items():
    batches={}
    for o in g['objects']:
        ev=o.evaluated_get(dg); me=ev.to_mesh(); me.calc_loop_triangles()
        matrix=inv@o.matrix_world; normal=matrix.to_3x3().inverted().transposed()
        for triangle in me.loop_triangles:
            mat=me.materials[triangle.material_index].name
            if mat not in materials: materials.append(mat)
            mi=materials.index(mat); batch=batches.setdefault(mi,dict(material=mi,vertices=[],normals=[],triangles=[]))
            start=len(batch['vertices'])
            for li in triangle.loops:
                loop=me.loops[li]; pos=convert(matrix@me.vertices[loop.vertex_index].co); n=convert(normal@me.corner_normals[li].vector).normalized()
                if name!='Palm': pos=adjusted(pos); n=correct@n
                batch['vertices'].append(vec(pos-g['position'])); batch['normals'].append(vec(n))
            batch['triangles'] += [start,start+2,start+1]
        ev.to_mesh_clear()
    parent=groups[g['parent']]['position'] if g['parent'] else Vector()
    parts.append(dict(name=name,parent=g['parent'],position=vec(g['position']-parent),batches=list(batches.values())))
out=root/'Assets/Art/ValkyrHand_V6';out.mkdir(parents=True,exist_ok=True)
data=dict(parts=parts,materials=materials,grip=vec(grip),axis=vec(transverse),source=str(source),source_sha256=hashlib.sha256(source.read_bytes()).hexdigest())
(out/'hand_meshes.json').write_text(json.dumps(data,separators=(',',':')),encoding='utf-8')
print('V6_HAND_EXPORTED',len(parts),'parts',len(objects),'source meshes',sum(len(b['triangles'])//3 for p in parts for b in p['batches']),'triangles',flush=True)
