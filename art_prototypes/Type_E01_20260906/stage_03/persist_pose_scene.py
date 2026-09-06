from pathlib import Path
root=Path('D:/project-mecha-design/MECH ROUGE/art_prototypes/Type_E01_20260906')
p=root/'build_stage03.py';s=p.read_text(encoding='utf-8-sig')
s=s.replace("blend=OUT/'TYPE_E01_STAGE03.blend'", "neutral.use_fake_user=True\nready.use_fake_user=True\nprint('SCENE_PERSISTENCE '+str([(s.name,s.users) for s in bpy.data.scenes]),flush=True)\nblend=OUT/'TYPE_E01_STAGE03.blend'",1)
p.write_text(s,encoding='utf8')
p=root/'run_stage03.py';s=p.read_text(encoding='utf-8-sig').replace("str(root/'build_stage03.py'),'--','--preview']", "str(root/'build_stage03.py'),'--',*sys.argv[1:]]")
p.write_text(s,encoding='utf8')
p=root/'render_stage03.py';s=p.read_text(encoding='utf-8-sig').replace("neutral=bpy.data.scenes['01 NEUTRAL", "print('REOPENED_SCENES '+str([(s.name,s.users) for s in bpy.data.scenes]),flush=True)\nneutral=bpy.data.scenes['01 NEUTRAL",1)
p.write_text(s,encoding='utf8')
