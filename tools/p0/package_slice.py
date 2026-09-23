"""Package native Player recordings and portable launchers; no synthetic footage."""
from pathlib import Path
import subprocess,sys,json,hashlib
root=Path(__file__).resolve().parents[2]
build=root/'Builds/CombatSlice_R1_20260914'
for view in 'ABC':
    launch='@echo off\r\nsetlocal\r\nset "MECH_EQUIPMENT_PROFILE=%~dp0UserData\\profile.json"\r\nif not exist "%~dp0UserData" mkdir "%~dp0UserData"\r\nstart "" "%~dp0MECH_TRIAL_P0.exe" -combatSlice -sliceView '+view+' -screen-fullscreen 0 -screen-width 1600 -screen-height 900\r\n'
    (build/f'PLAY_VIEW_{view}.cmd').write_bytes(launch.encode('ascii'))
(build/'PLAY_DEFAULT.cmd').write_bytes((build/'PLAY_VIEW_C.cmd').read_bytes())
(build/'START_HERE.txt').write_text('机神试炼 R1 镜头试战\n\n双击 PLAY_DEFAULT.cmd 使用已选定的最远C视野（扩大25%）；A/B/C启动器保留对照。\n游戏内 F5/F6/F7 可切换比较，R同配置重开，Esc暂停。\nWASD移动，左键射击，右键/Q挥刀，空格闪击、按住推进。\n基础M7+斩舰刀，零强化，两轮固定混合进攻。尽快清场可提前完成，90秒到时未清场为未完成。\n这一轮只改镜头试验入口和小型战斗编排；E与F等待后续工单。\nUserData为隔离试玩存档，不改原来的仓库。保留整个目录一起使用。\n请反馈：哪档能看清威胁和冲刺落点，哪档角色太小，是否愿意再玩。\n',encoding='utf-8-sig')
if len(sys.argv)>1:
    evidence=Path(sys.argv[1]).resolve()
    delivery=root/'AuditEvidence/combat-slice-r1/videos'/evidence.name;delivery.mkdir(parents=True,exist_ok=True)
    records=[]
    for i,view in enumerate('ABC'):
        frames=list((evidence/f'view-{i}').glob('frame_*.png'))
        assert frames,'Missing actual Player frames'
        target=delivery/f'R1_view_{view}.mp4'
        subprocess.run(['D:/Tools/ffmpeg/bin/ffmpeg.exe','-y','-loglevel','error','-threads','1','-framerate','30','-i',str(evidence/f'view-{i}/frame_%04d.png'),'-i',str(evidence/'game-mix.wav'),'-c:v','libx264','-threads','2','-preset','fast','-crf','20','-pix_fmt','yuv420p','-c:a','aac','-b:a','192k','-movflags','+faststart','-shortest',str(target)],check=True)
        meta=json.loads(subprocess.check_output(['D:/Tools/ffmpeg/bin/ffprobe.exe','-v','error','-show_streams','-show_format','-of','json',str(target)]))
        assert any(s['codec_type']=='video' for s in meta['streams']) and any(s['codec_type']=='audio' for s in meta['streams'])
        records.append({'view':view,'frames':len(frames),'file':str(target),'probe':meta,'sha256':hashlib.sha256(target.read_bytes()).hexdigest()})
    (delivery/'media-manifest.json').write_text(json.dumps(records,ensure_ascii=False,indent=2),encoding='utf-8')
    print(delivery)
print('Portable launchers ready:',build)
