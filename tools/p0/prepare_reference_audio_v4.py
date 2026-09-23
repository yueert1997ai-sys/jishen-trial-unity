"""Local reference edit: retain recorded timbre, no synthesis, EQ, layering or pitch changes.
These are reference excerpts, not original or CC0 production assets.
"""
from pathlib import Path
import subprocess,wave,json,hashlib,re,uuid
import numpy as np
root=Path(__file__).resolve().parents[2]
audit=root/'AuditEvidence/p0-audio-v4'
out=root/'Assets/Resources/Audio/Combat/P0Reference'
out.mkdir(parents=True,exist_ok=True)
sr=48000
refs=root/'AuditEvidence/p0-audio-v3/references'
def decode(p):
    raw=subprocess.run(['D:/Tools/ffmpeg/bin/ffmpeg.exe','-v','error','-i',str(p),'-f','f32le','-ac','1','-ar',str(sr),'-'],capture_output=True,check=True).stdout
    return np.frombuffer(raw,np.float32).astype(float)
source={name:decode(refs/(name+'.webm')) for name in ['gundam','delta_m7']}
# Delta reference has keyframe preroll. File times 22.6..24.1 are within the M7
# chapter (~05:22..05:24), before the AUG chapter at 05:31.
edits=[
    ('cut_1','gundam',.68,1.00,.64),
    ('cut_2','gundam',2.10,2.38,.64),
    ('cut_3','gundam',2.48,3.04,.72),
    ('heavy_load','gundam',.09,.22,.23),
    ('hit_1','gundam',1.02,1.19,.48),
    ('hit_2','gundam',1.25,1.42,.48),
    ('hit_heavy','gundam',3.08,3.38,.58),
    ('rifle_shot_1','delta_m7',22.63,22.95,.76),
    ('rifle_shot_2','delta_m7',23.46,23.78,.76),
    ('rifle_shot_3','delta_m7',23.93,24.10,.76),
]
manifest=[]
for name,ref,start,end,peak in edits:
    x=source[ref][round(start*sr):round(end*sr)].copy()
    assert len(x)==round((end-start)*sr)
    if name.startswith('rifle'):
        onset=max(0,int(np.flatnonzero(abs(x)>.12*np.max(abs(x)))[0])-24)
        x=x[onset:];start+=onset/sr
    fade=np.ones(len(x));a=round(.001*sr);r=round(.018*sr)
    fade[:a]=np.linspace(0,1,a);fade[-r:]=np.linspace(1,0,r)
    x*=fade
    gain=peak/np.max(abs(x));x*=gain
    path=out/(name+'.wav')
    pcm=np.round(x*32767).astype('<i2')
    with wave.open(str(path),'wb') as w:
        w.setnchannels(1);w.setsampwidth(2);w.setframerate(sr);w.writeframes(pcm.tobytes())
    meta=Path(str(path)+'.meta')
    if not meta.exists():
        template=(root/'Assets/Resources/Audio/Combat/P0EnergyKinetic/cut_1.wav.meta').read_text()
        meta.write_text(re.sub(r'guid: \w+','guid: '+uuid.uuid4().hex,template))
    # This check verifies source identity and lack of unintended processing, not subjective sound quality.
    restored=pcm.astype(float)/32767
    assert np.max(abs(restored-x))<1/32767
    assert abs(restored[0])<1e-6 and abs(restored[-1])<1e-6
    manifest.append(dict(name=name,source=ref,start=start,end=end,gain=gain,peak_dbfs=float(20*np.log10(np.max(abs(x)))),sha256=hashlib.sha256(path.read_bytes()).hexdigest(),source_identity_verified=True))
(audit/'reference-edit-manifest.json').write_text(json.dumps(dict(clips=manifest,processing='Trim, 1 ms attack, 18 ms release, gain, mono PCM only. Original speed and pitch.',rights='Reference recording excerpts for local comparison; not original or cleared release assets.',auditioned=False),indent=2))
print('PASS all 10 source excerpts match reference PCM after declared trim/fades/gain; no synthesis or pitch processing')
