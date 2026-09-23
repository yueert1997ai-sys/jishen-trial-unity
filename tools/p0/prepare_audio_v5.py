"""Author-licensed saber material and a shot-synchronous M7 body/tail pair. No playback."""
from pathlib import Path
import subprocess,wave,json,hashlib,uuid,re
import numpy as np
root=Path(__file__).resolve().parents[2];sr=48000
audit=root/'AuditEvidence/p0-audio-v5';out=root/'Assets/Resources/Audio/Combat/P0Cadence';out.mkdir(parents=True,exist_ok=True)
def decode(p):
    raw=subprocess.run(['D:/Tools/ffmpeg/bin/ffmpeg.exe','-v','error','-i',str(p),'-f','f32le','-ac','1','-ar',str(sr),'-'],capture_output=True,check=True).stdout
    return np.frombuffer(raw,np.float32).astype(float)
manifest=[]
def save(name,x):
    assert np.max(abs(x))<.9 and np.isfinite(x).all()
    path=out/(name+'.wav')
    with wave.open(str(path),'wb') as w:w.setnchannels(1);w.setsampwidth(2);w.setframerate(sr);w.writeframes(np.round(x*32767).astype('<i2').tobytes())
    meta=Path(str(path)+'.meta')
    if not meta.exists():
        template=(root/'Assets/Resources/Audio/Combat/P0Reference/cut_1.wav.meta').read_text()
        meta.write_text(re.sub(r'guid: \w+','guid: '+uuid.uuid4().hex,template))
    manifest.append(dict(name=name,seconds=len(x)/sr,peak_dbfs=float(20*np.log10(max(abs(x)))),sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
def ends(x):
    x=x.copy();a=48;r=720;x[:a]*=np.linspace(0,1,a);x[-r:]*=np.linspace(1,0,r);return x
# Keep the COMPLETE authored swing and its natural decay. Runtime blends out the previous
# swing when the next one starts. Never cut arbitrary middle fragments out of an anime clip.
for i in [1,2]:
    x=decode(audit/f'sources/dova_saber3_track{i}.mp3');x=ends(x*.65/np.max(abs(x)))
    save('saber_swing_'+str(i),x)
heavy=decode(audit/'sources/dova_saber3_track1.mp3')
save('saber_heavy',ends(heavy*.76/np.max(abs(heavy))))
# Separate physical contacts; empty swings never include a pre-recorded collision.
for name,src,dur,peak in [('saber_hit','impactMetal_heavy_000.ogg',.22,.52),('saber_hit_heavy','impactMetal_heavy_001.ogg',.34,.65)]:
    x=decode(root/'Assets/Resources/Audio/Combat'/src)[:round(dur*sr)];x=ends(x);save(name,x*peak/max(abs(x)))
# Quiet electrical preparation from the author's third track, used only for heavy windup.
x=decode(audit/'sources/dova_saber3_track3.mp3')[:round(.16*sr)];x=ends(x);save('saber_load',x*.23/max(abs(x)))
# One isolated M7 shot. All fire events use this same recording at the same pitch and level.
raw=decode(root/'AuditEvidence/p0-audio-v3/references/delta_m7.webm')
x=raw[round(22.60*sr):round(23.03*sr)]
onset=max(0,int(np.flatnonzero(abs(x)>.12*max(abs(x)))[0])-48);x=ends(x[onset:]);x*=.76/max(abs(x))
t=np.arange(len(x))/sr
attack_gain=1-np.clip((t-.070)/.035,0,1)
attack=x*attack_gain;tail=x*(1-attack_gain)
assert np.max(abs(attack+tail-x))<1e-9,'body/tail must reconstruct the same single shot'
save('m7_attack',attack[:round(.106*sr)]);save('m7_tail',tail)
# Both posted swing variants currently decode identically. Record it rather than claim variation.
a=decode(audit/'sources/dova_saber3_track1.mp3');b=decode(audit/'sources/dova_saber3_track2.mp3')
(audit/'audio-manifest.json').write_text(json.dumps(dict(clips=manifest,saber_variants_identical=bool(np.array_equal(a,b)),rifle_source_start=22.60+onset/sr,rifle_processing='one isolated shot, complementary body/tail split at 70..105 ms, no pitch/random sample changes',auditioned=False),indent=2))
print('PASS complete saber materials prepared; M7 attack + tail reconstruct one isolated shot exactly')
