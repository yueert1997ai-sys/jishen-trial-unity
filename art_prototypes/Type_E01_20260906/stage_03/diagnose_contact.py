from pathlib import Path
p=Path('D:/project-mecha-design/MECH ROUGE/art_prototypes/Type_E01_20260906/render_stage03.py')
s=p.read_text(encoding='utf8').replace("assert report['left_support_contact_error_m']<.002", "assert report['left_support_contact_error_m']<.002, (report['left_support_contact_error_m'],list(hand_point),list(gun_point),str(left.matrix_world),str(bpy.data.objects['E01_L_HAND'].matrix_world),str(rifle.matrix_world))")
p.write_text(s,encoding='utf8')
