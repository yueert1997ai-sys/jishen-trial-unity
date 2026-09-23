"""Encode only newly captured native Player frames; verify counts, decoding and PCM headroom."""
from pathlib import Path
import array, hashlib, json, subprocess, wave

root=Path(__file__).resolve().parents[2]
base=root/'AuditEvidence/combat-foundation-v3'
ffmpeg='D:/Tools/ffmpeg/bin/ffmpeg.exe'
ffprobe='D:/Tools/ffmpeg/bin/ffprobe.exe'
latest=lambda mode:sorted(p for p in base.glob(mode+'-*') if p.is_dir())[-1]
replay=latest('replay');detail=latest('detail');lab=latest('labreplay')
jobs=[('V3_full_combat',replay/'view-2',replay/'game-mix.wav'),
      ('V3_blade_contact',detail/'detail',detail/'game-mix.wav')]
jobs += [('V3_lab_'+v,lab/('capture-'+v),lab/('capture-'+v)/'game-mix.wav')
         for v in ['BasicFeedback','FullFeedback','NoBreak']]
records=[]
for name,frames,audio in jobs:
    paths=sorted(frames.glob('frame_*.png'));assert paths, f'Missing frames: {name}'
    assert paths[-1].name==f'frame_{len(paths)-1:04d}.png', f'Missing frame index: {name}'
    target=base/(name+'.mp4')
    subprocess.run([ffmpeg,'-hide_banner','-loglevel','error','-y','-framerate','30','-i',str(frames/'frame_%04d.png'),
                    '-i',str(audio),'-c:v','libx264','-threads','2','-preset','fast','-crf','21','-pix_fmt','yuv420p',
                    '-c:a','aac','-b:a','192k','-movflags','+faststart',str(target)],check=True)
    probe=json.loads(subprocess.check_output([ffprobe,'-v','error','-count_frames','-show_streams','-show_format','-of','json',str(target)]))
    video=next(s for s in probe['streams'] if s['codec_type']=='video')
    assert int(video['nb_read_frames'])==len(paths),f'Wrong frame count: {name}'
    subprocess.run([ffmpeg,'-v','error','-i',str(target),'-f','null','-'],check=True)
    with wave.open(str(audio),'rb') as w:
        assert w.getsampwidth()==2
        audio_seconds=w.getnframes()/w.getframerate()
        samples=array.array('h',w.readframes(w.getnframes()))
    peak=max(abs(n) for n in samples)/32767
    assert 0<peak<.99 and abs(audio_seconds-len(paths)/30)<.08,f'Invalid audio capture: {name}'
    record={'path':target.relative_to(root).as_posix(),'frames':len(paths),'seconds':len(paths)/30,
            'audioSeconds':audio_seconds,'audioPeak':peak,'bytes':target.stat().st_size,
            'sha256':hashlib.sha256(target.read_bytes()).hexdigest(),'fullDecode':'passed'}
    records.append(record);print(json.dumps(record),flush=True)
(base/'media-verification.json').write_text(json.dumps(records,indent=2),encoding='utf-8')
