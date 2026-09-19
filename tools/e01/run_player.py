from pathlib import Path
import os,subprocess,sys,json,time
root=Path(__file__).resolve().parents[2]
build=Path((root/'AuditEvidence/e01-elite/build-path.txt').read_text().strip())
output=root/'AuditEvidence/e01-elite/player'
output.mkdir(parents=True,exist_ok=True)
log=output/'player.log'
env=os.environ.copy()
temp=Path('D:/Tools/BlenderUserData/Temp/e01-player');temp.mkdir(parents=True,exist_ok=True)
env['TEMP']=env['TMP']=str(temp)
startup=subprocess.STARTUPINFO();startup.dwFlags|=subprocess.STARTF_USESHOWWINDOW;startup.wShowWindow=0
args=[str(build/'MECH_TRIAL.exe'),'-screen-width','1600','-screen-height','900','-screen-fullscreen','0','-e01BossSmoke','-e01Evidence',str(output),'-logFile',str(log)]
started=time.time()
result=subprocess.run(args,cwd=build,env=env,startupinfo=startup,timeout=140)
print('PLAYER_EXIT',result.returncode,'SECONDS',round(time.time()-started,2))
report=output/'runtime-check.json'
if report.exists(): print(report.read_text(encoding='utf-8'))
print('\n'.join(log.read_text(encoding='utf-8',errors='replace').splitlines()[-22:]))
raise SystemExit(result.returncode)
