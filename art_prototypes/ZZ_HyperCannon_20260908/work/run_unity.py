from pathlib import Path
import os,subprocess,sys
asset=Path(__file__).resolve().parents[1]
root=asset.parents[1]
out=asset/'unity_evidence';out.mkdir(exist_ok=True)
method=sys.argv[1] if len(sys.argv)>1 else 'HeavyCannonIntegration.BuildAndAudit'
log=out/(method.split('.')[-1]+'.log')
env=os.environ.copy()
temp=Path('D:/Tools/BlenderUserData/Temp/type08-unity');temp.mkdir(parents=True,exist_ok=True)
env['TEMP']=env['TMP']=str(temp)
args=['D:/Editor/6000.3.18f1/Editor/Unity.exe','-batchmode','-projectPath',str(root),'-executeMethod',method,'-logFile',str(log)]
if not any(w in method for w in ('Audit','Run')):args.append('-quit')
result=subprocess.run(args,cwd=root,env=env,creationflags=0x08000000)
lines=log.read_text(encoding='utf-8',errors='replace').splitlines()
print('\n'.join([s for s in lines if any(w in s for w in ('_PASS','_FAIL','Exception:','error CS','Error','failed','Failed'))][-30:]+lines[-5:]))
raise SystemExit(result.returncode)
