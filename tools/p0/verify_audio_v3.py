"""Offline PCM, loop and representative mix validation; not an audible playtest."""
from pathlib import Path
import json,subprocess,wave
import numpy as np
root=Path(__file__).resolve().parents[2]
audit=root/'AuditEvidence/p0-audio-v3'
sr=48000

def read(path,channels=1):
    p=subprocess.run(['D:/Tools/ffmpeg/bin/ffmpeg.exe','-v','error','-i',str(path),'-f','f32le','-ac',str(channels),'-ar',str(sr),'-'],capture_output=True,check=True)
    return np.frombuffer(p.stdout,np.float32).reshape(-1,channels)

bank=root/'Assets/Resources/Audio/Combat/P0EnergyKinetic'
clips={p.stem:read(p)[:,0] for p in bank.glob('*.wav')}
checks=[]
for name,x in clips.items():
    assert np.all(np.isfinite(x)) and max(abs(x))<.86,name+' clips/invalid samples'
    assert abs(float(x.mean()))<.002,name+' DC offset'
    assert abs(x[0])<.002 and abs(x[-1])<.002,name+' discontinuous boundary'
    checks.append('PASS PCM peak/DC/fades '+name)
music=read(root/'Assets/Resources/Audio/Music/P0_HeavyBattle.ogg',2)
assert abs(len(music)/sr-64*4*60/185)<.015,'musical loop was shortened'
assert np.max(abs(music[0]-music[-1]))<.025,'loop has a discontinuous edge'
checks.append('PASS complete 64-bar, 185 BPM musical loop and quiet sample boundary')
# Representative gun -> fast/fast/heavy -> gun passage, using runtime voice gains.
mix=music[:6*sr].copy()*.17
events=[(i*.18,'rifle_shot_'+str(i%3+1),.82*.72) for i in range(8)]
events += [(1.65,'cut_1',.74*.72),(1.88,'cut_2',.74*.72),(2.3,'cut_3',.9*.72),(2.34,'hit_heavy',.9*.72)]
events += [(3.1+i*.18,'rifle_shot_'+str(i%3+1),.82*.72) for i in range(10)]
for t,name,gain in events:
    x=clips[name]*gain;start=round(t*sr);mix[start:start+len(x)]+=x[:,None]
assert np.max(np.abs(mix))<.97,'representative mix overload'
checks.append('PASS representative gun/combo/music mix has headroom (without ducking)')
with wave.open(str(audit/'offline_mix_not_runtime_capture.wav'),'wb') as w:
    w.setnchannels(2);w.setsampwidth(2);w.setframerate(sr);w.writeframes(np.round(mix*32767).astype('<i2').tobytes())
(audit/'offline-check.txt').write_text('\n'.join(checks)+f'\nMix peak: {20*np.log10(np.max(abs(mix))):.2f} dBFS\nNo audio device output or subjective audition.\n')
print('\n'.join(checks))
