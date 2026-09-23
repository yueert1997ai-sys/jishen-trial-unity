"""Decode and measure reference audio without any audio-device output."""
from pathlib import Path
import subprocess,json
import numpy as np
root=Path(__file__).resolve().parents[2]
audit=root/'AuditEvidence/p0-audio-v3'
result={}
for path in (audit/'references').glob('*.webm'):
    raw=subprocess.run(['D:/Tools/ffmpeg/bin/ffmpeg.exe','-v','error','-i',str(path),'-f','f32le','-ac','1','-ar','48000','-'],capture_output=True,check=True).stdout
    x=np.frombuffer(raw,np.float32);sr=48000;hop=480;win=2048
    # Stream-copy seeking includes keyframe preroll. The requested M7 chapter ends at 05:30;
    # keep only its requested last 23 seconds, excluding the preceding weapon chapter.
    if path.stem=='delta_m7':x=x[-23*sr:]
    frames=np.lib.stride_tricks.sliding_window_view(x,win)[::hop]
    spec=abs(np.fft.rfft(frames*np.hanning(win),axis=1))**2
    freq=np.fft.rfftfreq(win,1/sr);rms=np.sqrt(np.mean(frames**2,axis=1))
    candidates=np.where((rms[1:-1]>rms[:-2])&(rms[1:-1]>=rms[2:]))[0]+1
    peaks=[]
    for i in sorted(candidates,key=lambda i:-rms[i]):
        if all(abs(i-j)>16 for j in peaks) and rms[i]>max(rms)*.12:peaks.append(i)
        if len(peaks)>=14:break
    events=[]
    for i in sorted(peaks):
        spectrum=spec[i];bands={f'{lo}-{hi}':round(float(spectrum[(freq>=lo)&(freq<hi)].sum()/spectrum.sum()),3) for lo,hi in [(50,250),(250,1000),(1000,3000),(3000,10000)]}
        top=np.argsort(spectrum)[-5:][::-1]
        events.append(dict(time=round(i*hop/sr,3),rms=round(float(rms[i]),4),bands=bands,dominant_hz=[round(float(freq[k])) for k in top]))
    result[path.stem]=dict(seconds=len(x)/sr,events=events,method='Decoded waveform and spectral analysis only; no acoustic audition.')
(audit/'reference-analysis.json').write_text(json.dumps(result,indent=2),encoding='utf8')
print(json.dumps(result,indent=2))
