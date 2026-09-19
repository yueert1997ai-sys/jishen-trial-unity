from pathlib import Path
import os,subprocess,sys
root=Path(__file__).resolve().parents[2]
out=root/'AuditEvidence/e01-elite'
out.mkdir(parents=True,exist_ok=True)
method=sys.argv[1] if len(sys.argv)>1 else 'E01EliteIntegration.Integrate'
log=out/(method.split('.')[-1]+'.log')
env=os.environ.copy()
temp=Path('D:/Tools/BlenderUserData/Temp/e01-unity')
temp.mkdir(parents=True,exist_ok=True)
env['TEMP']=env['TMP']=str(temp)
args=['D:/Editor/6000.3.18f1/Editor/Unity.exe','-batchmode','-quit','-projectPath',str(root),'-executeMethod',method,'-logFile',str(log)]
result=subprocess.run(args,cwd=root,env=env,creationflags=0x08000000)
lines=log.read_text(encoding='utf-8',errors='replace').splitlines()
important=[line for line in lines if any(word in line for word in ('_PASS','Exception:','error CS','Error','failed','Failed'))]
print('\n'.join(important[-24:]+lines[-5:]))
raise SystemExit(result.returncode)
