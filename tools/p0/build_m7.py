import subprocess
from pathlib import Path
root=Path.cwd()
startup=subprocess.STARTUPINFO();startup.dwFlags|=subprocess.STARTF_USESHOWWINDOW;startup.wShowWindow=0
p=subprocess.run(['D:/Editor/6000.3.18f1/Editor/Unity.exe','-batchmode','-quit','-projectPath',str(root),'-executeMethod','M7RefinementBuild.Build','-logFile',str(root/'AuditEvidence/m7-refinement/build.log')],startupinfo=startup)
print('Unity build exit',p.returncode)
