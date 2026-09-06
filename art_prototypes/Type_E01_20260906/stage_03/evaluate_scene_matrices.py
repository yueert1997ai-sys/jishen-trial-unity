from pathlib import Path
p=Path('D:/project-mecha-design/MECH ROUGE/art_prototypes/Type_E01_20260906/render_stage03.py')
s=p.read_text(encoding='utf8')
s=s.replace("bpy.context.window.scene=ready\nbpy.context.view_layer.update()", "bpy.context.window.scene=neutral\nbpy.context.view_layer.update()\nneutral_hand_matrix=bpy.data.objects['E01_L_HAND'].matrix_world.copy()\nbpy.context.window.scene=ready\nbpy.context.view_layer.update()",1)
s=s.replace("left.matrix_world@bpy.data.objects['E01_L_HAND'].matrix_world.inverted()", "left.matrix_world@neutral_hand_matrix.inverted()",1)
p.write_text(s,encoding='utf8')
