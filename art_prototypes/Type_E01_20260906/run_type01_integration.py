from pathlib import Path
import subprocess,sys
root=Path('D:/project-mecha-design/MECH ROUGE/art_prototypes/Type_E01_20260906')
out=root/'stage_04_TYPE01';out.mkdir(exist_ok=True)
with (out/'integration.log').open('w',encoding='utf8') as log:
    result=subprocess.run([r'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe','--background','--python-exit-code','1','--python',str(root/'integrate_type01_rifle.py'),'--',*sys.argv[1:]],stdout=log,stderr=subprocess.STDOUT,cwd=str(root))
print((out/'integration.log').read_text(encoding='utf8')[-9000:])
raise SystemExit(result.returncode)
