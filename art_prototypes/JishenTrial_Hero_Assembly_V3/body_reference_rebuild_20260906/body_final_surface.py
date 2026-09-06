"""Final surface cleanup in the existing assembly; accepted head is excluded."""
from body_common import *

# These overlays crossed the crest of the newly deepened chest. The underlying
# folded armor is complete and gives a cleaner manufactured contour on its own.
removed=[]
for name in ('V3B_Sternum_Crown_Secondary','V3B_Clavicle_Facing.L','V3B_Clavicle_Facing.R'):
 o=bpy.data.objects.get(name)
 if o:removed.append(name);bpy.data.objects.remove(o,do_unlink=True)

# Replace the variable-font outline with a closed regular-font solid, preserving
# its exact visible box and shoulder attachment after the depth correction.
old=bpy.data.objects['V3B_Shoulder_Valkyr_Wordmark'];pa=old.parent;cols=list(old.users_collection)
pts=[old.matrix_world@v.co for v in old.data.vertices]
lo=Vector([min(p[k] for p in pts) for k in range(3)]);hi=Vector([max(p[k] for p in pts) for k in range(3)])
font_report=[];replacement=None
for file in ('C:/Windows/Fonts/arial.ttf','C:/Windows/Fonts/segoeui.ttf'):
 cu=bpy.data.curves.new('Valkyr_regular_font','FONT');cu.body='VALKYR';cu.size=.1;cu.extrude=.0001;cu.resolution_u=4
 cu.font=bpy.data.fonts.load(file,check_existing=True)
 ob=bpy.data.objects.new('Valkyr_regular_font',cu);cols[0].objects.link(ob);ob.rotation_euler=(math.pi/2,0,0)
 bpy.ops.object.select_all(action='DESELECT');ob.select_set(True);bpy.context.view_layer.objects.active=ob;bpy.ops.object.convert(target='MESH')
 ob=bpy.context.object;ob.data.transform(ob.matrix_world);ob.matrix_world=Matrix.Identity(4)
 bm=bmesh.new();bm.from_mesh(ob.data);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.0000005);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
 bad=sum(not e.is_manifold for e in bm.edges);bm.to_mesh(ob.data);bm.free();font_report.append({'font':file,'nonmanifold_edges':bad})
 if bad==0:replacement=ob;break
 bpy.data.objects.remove(ob,do_unlink=True)
assert replacement is not None,font_report
pts=[v.co.copy() for v in replacement.data.vertices];a=Vector([min(p[k] for p in pts) for k in range(3)]);b=Vector([max(p[k] for p in pts) for k in range(3)])
for v in replacement.data.vertices:
 v.co=Vector([lo[k]+(v.co[k]-a[k])/(b[k]-a[k])*(hi[k]-lo[k]) for k in range(3)])
name=old.name;bpy.data.objects.remove(old,do_unlink=True);replacement.name=name;replacement.parent=pa;replacement.matrix_world=Matrix.Identity(4);replacement.data.materials.append(M['mark'])
replacement['design_part']='Shoulder_Valkyr_Wordmark';replacement['closed_regular_font']='Arial / exact prior label bounds'

# Remove bevels only where the audit identified microscopic collapsed bevel faces.
# All approved head meshes and modifiers remain untouched.
issues=json.loads((W/'stage_08/native_audit.json').read_text())['geometry_issues']
bevel_report=[]
def mesh_check(o):
 bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get();ev=o.evaluated_get(dg);me=ev.to_mesh()
 bm=bmesh.new();bm.from_mesh(me);out=(sum(not e.is_manifold for e in bm.edges),sum(f.calc_area()<1e-13 for f in bm.faces));bm.free();ev.to_mesh_clear();return out
for issue in issues:
 o=bpy.data.objects.get(issue['name'])
 if not o or o.name.startswith('V3H_') or not issue['degenerate_faces']:continue
 before=mesh_check(o);mods=[m for m in o.modifiers if m.type=='BEVEL' and m.show_render]
 for m in mods:m.show_render=False;m.show_viewport=False
 after=mesh_check(o)
 if after[0]>before[0] or after[1]>=before[1]:
  for m in mods:m.show_render=True;m.show_viewport=True
 else:bevel_report.append({'object':o.name,'before':before,'after':after})

# Missing body UVs are unwrapped per object; approved head UVs are retained.
missing=[o for o in SC.objects if o.type=='MESH' and not o.hide_render and not o.data.uv_layers and not o.name.startswith('V3H_')]
uvdone=[]
for start in range(0,len(missing),160):
 bpy.ops.object.select_all(action='DESELECT');batch=missing[start:start+160]
 for o in batch:o.hide_set(False);o.select_set(True)
 bpy.context.view_layer.objects.active=batch[0];bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT')
 bpy.ops.uv.smart_project(angle_limit=math.radians(66),island_margin=.02,correct_aspect=True,scale_to_bounds=True)
 bpy.ops.object.mode_set(mode='OBJECT');uvdone.extend(o.name for o in batch)
 print('UV_BODY',len(uvdone),'/',len(missing),flush=True)
bpy.ops.object.select_all(action='DESELECT')

wr=bpy.data.objects['AntiShip_Blade_Display_Root']
tip=Vector(SC['sword_grip_center_units'])+Vector((.30,-.08,-.9506)).normalized()*216
wr['blade_tip_world_m']=list(tip*S)
SC['body_surface_final_pass']=9
SC['body_final_review_note']='Side shell depth, forward chest wedge, stance and extended feet reviewed in orthographic views. Approved head mesh/material identity retained.'
(W/'surface_completion.json').write_text(json.dumps({'removed_crossing_overlays':removed,'wordmark':font_report,'microscopic_bevel_cleanup':bevel_report,'unwrapped_body_meshes':len(uvdone)},indent=2))
