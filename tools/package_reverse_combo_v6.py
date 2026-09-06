"""Package 30 fps captures from the actual Windows Player; no generated animation."""
import pathlib,subprocess
root=pathlib.Path(__file__).resolve().parents[1];out=root/'AuditEvidence/reverse-combo-v6'
for frames,name in [('game-frames','gameplay'),('frames','closeup')]:
    subprocess.run(['D:/Tools/ffmpeg/bin/ffmpeg.exe','-hide_banner','-loglevel','error','-y','-framerate','30','-i',str(out/'film'/frames/'frame_%04d.png'),'-c:v','libx264','-preset','fast','-crf','19','-pix_fmt','yuv420p','-movflags','+faststart',str(out/(name+'.mp4'))],check=True)
print(out)
