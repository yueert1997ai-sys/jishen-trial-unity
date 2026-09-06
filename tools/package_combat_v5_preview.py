"""Encode the actual game-camera and close-up frame sequences at their captured 30 fps."""
import pathlib,subprocess
root=pathlib.Path(__file__).resolve().parents[1]
out=root/'AuditEvidence/combat-v5'
for frames,name in [('game-frames','gameplay'),('frames','closeup')]:
    subprocess.run(['D:/Tools/ffmpeg/bin/ffmpeg.exe','-hide_banner','-loglevel','error','-y','-framerate','30',
                    '-i',str(out/'preview'/frames/'frame_%04d.png'),'-c:v','libx264','-preset','fast','-crf','19',
                    '-pix_fmt','yuv420p','-movflags','+faststart',str(out/(name+'.mp4'))],check=True)
print('Encoded actual game preview:',out)
