from pathlib import Path
import json,subprocess
import numpy as np
from scipy.io import wavfile
from scipy import signal
root=Path(r'D:\project-mecha-design\MECH ROUGE\handoff\GameplayLoop_V1')
out=Path(r'E:\SteamLibrary\JishenBuildWork\CombatVelocity_R1\Evidence\release')
def decode(p):
    data=subprocess.check_output([r'D:\Tools\ffmpeg\bin\ffmpeg.exe','-v','error','-i',str(p),'-f','f32le','-ar','48000','-ac','2','pipe:1'])
    return np.frombuffer(data,dtype='<f4').reshape(-1,2)
records=[]
for p in sorted((root/'Assets/Resources/Audio/Combat/VelocityR1').glob('*.wav')):
    sr,a=wavfile.read(p);a=a.astype(np.float64)/32768
    assert sr==48000 and a.ndim==1 and len(a)<24001,p
    assert np.max(abs(a))<.81 and abs(a[0])<.003 and abs(a[-1])<.003,p
    assert abs(a.mean())<.015,p
    bass=signal.sosfilt(signal.butter(2,[60,200],btype='bandpass',fs=sr,output='sos'),a)
    records.append(dict(file=p.name,peak=float(np.max(abs(a))),rms=float(np.sqrt(np.mean(a*a))),low_band_rms=float(np.sqrt(np.mean(bass*bass))),duration=len(a)/sr))
assert len(records)==36
music=decode(root/'Assets/Resources/Audio/Music/Velocity_Overdrive.ogg')
assert abs(len(music)/48000-64*4*60/170)<.005
assert np.max(abs(music))<.85
seam=float(np.max(abs(music[0]-music[-1])))
assert seam<.035,('loop sample discontinuity',seam)
result=dict(processed_variants=records,music_seconds=len(music)/48000,music_peak=float(np.max(abs(music))),loop_boundary_step=seam,status='PASS')
(out/'audio-asset-audit.json').write_text(json.dumps(result,indent=2))
print({k:v for k,v in result.items() if k!='processed_variants'})
