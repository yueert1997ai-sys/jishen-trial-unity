"""Bake the current LOD into five material families and export it."""
import bpy,pathlib,json,numpy as np
W=pathlib.Path(__file__).resolve().parent;D=W/'delivery';T=D/'textures';E=D/'exports';T.mkdir(exist_ok=True);E.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(D/'VALKYR_DEMO_UNBAKED.blend'));sc=bpy.context.scene;col=bpy.data.collections['VALKYR_DEMO_EXPORT'];parts=list(col.objects)
sc.render.engine='CYCLES';sc.cycles.samples=8;sc.cycles.use_denoising=False;sc.cycles.device='GPU'
p=bpy.context.preferences.addons['cycles'].preferences;p.compute_device_type='OPTIX';p.get_devices()
for d in p.devices:d.use=d.type=='OPTIX'
sc.render.bake.use_selected_to_active=False;sc.render.bake.use_clear=True;sc.render.bake.margin=12
for v in sc.view_layers:v.material_override=None
atlas_report={}
for fam,res in (('Armor',4096),('Mechanics',2048),('Weapon',2048)):
 items=[o for o in parts if o['atlas_family']==fam];copies=[]
 for o in items:
  ob=bpy.data.objects.new('BAKE_'+o.name,o.data.copy());sc.collection.objects.link(ob);ob.matrix_world=o.matrix_world.copy();copies.append(ob)
 bpy.ops.object.select_all(action='DESELECT')
 for ob in copies:ob.select_set(True)
 bpy.context.view_layer.objects.active=copies[0];bpy.ops.object.join();target=bpy.context.object;mats=set(target.data.materials);ims={}
 for ch in ('BaseColor','Roughness','Metallic','Normal'):
  im=bpy.data.images.new('VALKYR_'+fam+'_'+ch,res,res,alpha=False);im.colorspace_settings.name='sRGB' if ch=='BaseColor' else 'Non-Color'
  for m in mats:
   ns=m.node_tree.nodes;n=ns.get('VALKYR_BAKE_TARGET') or ns.new('ShaderNodeTexImage');n.name='VALKYR_BAKE_TARGET';n.image=im
   for q in ns:q.select=False
   n.select=True;ns.active=n
  restore=[]
  if ch!='Normal':
   for m in mats:
    ns=m.node_tree.nodes;ls=m.node_tree.links;bs=next(n for n in ns if n.type=='BSDF_PRINCIPLED');out=next(n for n in ns if n.type=='OUTPUT_MATERIAL')
    prev=out.inputs['Surface'].links[0].from_socket;em=ns.new('ShaderNodeEmission');em.inputs['Strength'].default_value=1
    sock=bs.inputs[{'BaseColor':'Base Color','Roughness':'Roughness','Metallic':'Metallic'}[ch]]
    if sock.is_linked:ls.new(sock.links[0].from_socket,em.inputs['Color'])
    else:
     val=sock.default_value;em.inputs['Color'].default_value=val if ch=='BaseColor' else (val,val,val,1)
    ls.new(em.outputs[0],out.inputs['Surface']);restore.append((m,out,prev,em))
  bpy.ops.object.select_all(action='DESELECT');target.select_set(True);bpy.context.view_layer.objects.active=target
  bpy.ops.object.bake(type='NORMAL' if ch=='Normal' else 'EMIT',normal_space='TANGENT')
  for m,out,prev,em in restore:m.node_tree.links.new(prev,out.inputs['Surface']);m.node_tree.nodes.remove(em)
  im.filepath_raw=str(T/(im.name+'.png'));im.file_format='PNG';im.save();im.pack();ims[ch]=im
  print('DEMO_BAKED',fam,ch,flush=True)
 bpy.data.objects.remove(target,do_unlink=True)
 mat=bpy.data.materials.new('VALKYR_DEMO_'+fam);mat.use_nodes=True;ns=mat.node_tree.nodes;ls=mat.node_tree.links;bs=next(n for n in ns if n.type=='BSDF_PRINCIPLED')
 for ch,inp in (('BaseColor','Base Color'),('Roughness','Roughness'),('Metallic','Metallic')):
  tex=ns.new('ShaderNodeTexImage');tex.image=ims[ch];tex.label=ch;ls.new(tex.outputs['Color'],bs.inputs[inp])
 tex=ns.new('ShaderNodeTexImage');tex.image=ims['Normal'];norm=ns.new('ShaderNodeNormalMap');ls.new(tex.outputs['Color'],norm.inputs['Color']);ls.new(norm.outputs['Normal'],bs.inputs['Normal'])
 for o in items:
  o.data.materials.clear();o.data.materials.append(mat)
  for f in o.data.polygons:f.material_index=0
 # Unity Standard import companion: R=metallic, A=smoothness.
 mpx=np.empty(res*res*4,np.float32);rpx=np.empty_like(mpx);ims['Metallic'].pixels.foreach_get(mpx);ims['Roughness'].pixels.foreach_get(rpx)
 dst=np.zeros((res*res,4),np.float32);dst[:,0]=mpx.reshape(-1,4)[:,0];dst[:,3]=1-rpx.reshape(-1,4)[:,0]
 image=bpy.data.images.new('VALKYR_'+fam+'_MetallicSmoothness',res,res,alpha=True);image.colorspace_settings.name='Non-Color';image.pixels.foreach_set(dst.ravel());image.filepath_raw=str(T/(image.name+'.png'));image.file_format='PNG';image.save();image.pack()
 atlas_report[fam]={'resolution':[res,res],'channels':list(ims)+['MetallicSmoothness'],'unique_uv_atlas':True,'normal_bake':'LOD weighted surface normals and material microfinish; no high-to-low detail-transfer claim'}

for fam in ('Sensor','Beam'):
 mat=bpy.data.materials.new('VALKYR_DEMO_'+fam);mat.use_nodes=True;bs=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
 color=(.004,.52,1.0,1);bs.inputs['Base Color'].default_value=color;bs.inputs['Roughness'].default_value=.25;bs.inputs['Metallic'].default_value=.15;bs.inputs['Emission Color'].default_value=color;bs.inputs['Emission Strength'].default_value=5 if fam=='Beam' else 3
 mat.diffuse_color=color
 for o in parts:
  if o['atlas_family']!=fam:continue
  o.data.materials.clear();o.data.materials.append(mat)
  for f in o.data.polygons:f.material_index=0
  if fam=='Beam':
   for path in ('hide_render','hide_viewport'):
    dr=o.driver_add(path).driver;dr.expression='not beam';var=dr.variables.new();var.name='beam';var.type='SINGLE_PROP';var.targets[0].id=bpy.data.objects['AntiShip_Blade_Display_Root'];var.targets[0].data_path='["Beam_On"]'

materials=sorted({m.name for o in parts for m in o.data.materials});assert len(materials)==5,materials
sc.camera=bpy.data.objects['V3B_CAM_THREE_QUARTER'];sc.cycles.samples=48;sc.cycles.use_denoising=True
sc['material_strategy']='Five shared materials, unique 4K armor and 2K mechanics/weapon atlases, separate sensor and beam materials.'
sc['current_delivery']=str(D/'VALKYR_GAME_DEMO.blend')
bpy.ops.object.select_all(action='DESELECT');parents=set()
for o in parts:
 o.select_set(True);pa=o.parent
 while pa:parents.add(pa);pa=pa.parent
for o in parents:o.hide_set(False);o.select_set(True)
bpy.context.view_layer.objects.active=parts[0];bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(D/'VALKYR_GAME_DEMO.blend'))
bpy.ops.export_scene.fbx(filepath=str(E/'VALKYR_GAME_DEMO.fbx'),use_selection=True,object_types={'MESH','EMPTY'},use_mesh_modifiers=True,add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL',path_mode='COPY',embed_textures=True,use_custom_props=True)
bpy.ops.export_scene.gltf(filepath=str(E/'VALKYR_GAME_DEMO.glb'),export_format='GLB',use_selection=True,export_apply=True,export_animations=False,export_extras=True)
(D/'demo_material_manifest.json').write_text(json.dumps({'materials':materials,'atlases':atlas_report,'fbx':str(E/'VALKYR_GAME_DEMO.fbx'),'glb':str(E/'VALKYR_GAME_DEMO.glb')},indent=2))
print('DEMO_EXPORTED',materials,flush=True)
