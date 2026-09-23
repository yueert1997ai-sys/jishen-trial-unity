"""Package the Gundam beam VFX round without changing the permanent desktop entry."""
from pathlib import Path
import hashlib,json,zipfile
root=Path(__file__).resolve().parents[2]
build=root/'Builds/BeamVfx_Gundam_R2'
evidence=root/'AuditEvidence/beam-vfx-r2/release'
launch='@echo off\r\nsetlocal\r\nset "MECH_EQUIPMENT_PROFILE=%~dp0UserData\\profile.json"\r\nif not exist "%~dp0UserData" mkdir "%~dp0UserData"\r\nstart "" "%~dp0MECH_TRIAL_P0.exe" -combatSlice -sliceView C -screen-fullscreen 0 -screen-width 1600 -screen-height 900\r\n'
(build/'PLAY_DEFAULT.cmd').write_bytes(launch.encode('ascii'))
(build/'START_HERE.txt').write_text('机神试炼 · 高达风粒子光束重制\nTYPE-08 粉光束：红白螺旋缠绕、白色过曝芯、行进能量包、螺旋粒子流。\nAX-01 蓝光束：高密度锐利边缘、三股电离螺旋、贯穿星形眩光。\n所有光束命中为定向撕裂爆炸：星形眩光、冲击环、火花流、碎屑与烟。\nWASD 移动，左键射击，右键/Q 斩击，空格闪击/按住推进，R 重开，Esc 暂停。\n以后正常体验请使用桌面永久入口。\n',encoding='utf-8-sig')
records=[{'path':p.relative_to(build).as_posix(),'sha256':hashlib.sha256(p.read_bytes()).hexdigest(),'size':p.stat().st_size} for p in sorted(build.rglob('*')) if p.is_file() and 'UserData' not in p.relative_to(build).parts]
(evidence/'build-manifest.json').write_text(json.dumps(records,indent=2),encoding='utf-8')
with zipfile.ZipFile(evidence/'beam-vfx-source.zip','w',zipfile.ZIP_DEFLATED) as archive:
    for folder in ['Assets/Scripts','Assets/Editor','Assets/Resources/VFX','tools/p0','docs']:
        for p in (root/folder).rglob('*'):
            if p.is_file():archive.write(p,p.relative_to(root))
print('Packaged files:',len(records))
