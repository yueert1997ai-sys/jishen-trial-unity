"""Mux actual Unity mixer capture with matching real Player frame pairs."""
import pathlib,subprocess
root=pathlib.Path(__file__).resolve().parents[1];out=root/'AuditEvidence/power-combo-v7'
for frames,name in [('game-frames','gameplay'),('frames','closeup')]:
    subprocess.run(['D:/Tools/ffmpeg/bin/ffmpeg.exe','-hide_banner','-loglevel','error','-y','-framerate','30','-i',str(out/'film'/frames/'frame_%04d.png'),'-i',str(out/'film/game-mix.wav'),'-c:v','libx264','-preset','fast','-crf','19','-pix_fmt','yuv420p','-c:a','aac','-b:a','160k','-shortest','-movflags','+faststart',str(out/(name+'.mp4'))],check=True)
print(out)
