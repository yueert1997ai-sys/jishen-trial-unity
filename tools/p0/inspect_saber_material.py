from pathlib import Path
import subprocess,json
import numpy as np
root=Path(__file__).resolve().parents[2]
for p in (root/'AuditEvidence/p0-audio-v5/sources').glob('*.mp3'):
    raw=subprocess.run(['D:/Tools/ffmpeg/bin/ffmpeg.exe','-v','error','-i',str(p),'-f','f32le','-ac','1','-ar','48000','-'],capture_output=True,check=True).stdout
    x=np.frombuffer(raw,np.float32);hop=2400
    rms=np.sqrt(np.mean(x[:len(x)//hop*hop].reshape(-1,hop)**2,axis=1));peak=max(rms)
    print(p.name,'duration',len(x)/48000,'active bins (50ms):',[(round(i*.05,2),round(float(v/peak),2)) for i,v in enumerate(rms) if v>peak*.06])
