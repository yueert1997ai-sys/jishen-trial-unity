from pathlib import Path
import subprocess
root=Path('D:/project-mecha-design/MECH ROUGE/art_prototypes/Type_E01_20260906');out=root/'stage_04_TYPE01'
with (out/'verify_render.log').open('w',encoding='utf8') as log:
    result=subprocess.run([r'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe','--background','--python-exit-code','1','--python',str(root/'verify_type01_integration.py')],stdout=log,stderr=subprocess.STDOUT,cwd=str(root))
print((out/'verify_render.log').read_text(encoding='utf8')[-8000:])
raise SystemExit(result.returncode)
