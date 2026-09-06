from pathlib import Path
import subprocess,sys
root=Path('D:/project-mecha-design/MECH ROUGE/art_prototypes/Type_E01_20260906')
(root/'stage_03').mkdir(exist_ok=True)
with (root/'stage_03/build.log').open('w',encoding='utf8') as log:
    result=subprocess.run([r'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe','--background','--python-exit-code','1','--python',str(root/'build_stage03.py'),'--',*sys.argv[1:]],stdout=log,stderr=subprocess.STDOUT,cwd=str(root))
print((root/'stage_03/build.log').read_text(encoding='utf8')[-10000:])
raise SystemExit(result.returncode)
