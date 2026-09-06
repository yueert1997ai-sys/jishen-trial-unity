import bpy, json, pathlib
p=pathlib.Path(__file__).resolve().parents[3]/'art_prototypes/JishenTrial_Hero_Assembly_V3/body_mass_refinement_20260906/iteration_v3/game_update/SOURCE_CURRENT_MASTER.blend'
bpy.ops.wm.open_mainfile(filepath=str(p))
h=bpy.data.objects['Hand.R'];g=bpy.data.objects['V3B_Sword_Grip_Socket']
print('HAND_INFO',json.dumps(dict(hand_matrix=[list(row) for row in h.matrix_world],grip=list(h.matrix_world.inverted()@g.matrix_world.translation),axis=list(g.get('grip_axis',[])),children=[dict(name=o.name,type=o.type,parent=o.parent.name if o.parent else None) for o in h.children_recursive if o.type=='MESH' or 'Joint' in o.name]),ensure_ascii=False),flush=True)
