"""Package R6 without changing the permanent desktop entry."""
from pathlib import Path
import hashlib,json,zipfile
root=Path(__file__).resolve().parents[2]
build=root/'Builds/CombatSlice_R6_20260915'
evidence=root/'AuditEvidence/combat-slice-r6'
launch='@echo off\r\nsetlocal\r\nset "MECH_EQUIPMENT_PROFILE=%~dp0UserData\\profile.json"\r\nif not exist "%~dp0UserData" mkdir "%~dp0UserData"\r\nstart "" "%~dp0MECH_TRIAL_P0.exe" -combatSlice -sliceView C -screen-fullscreen 0 -screen-width 1600 -screen-height 900\r\n'
(build/'PLAY_DEFAULT.cmd').write_bytes(launch.encode('ascii'))
(build/'START_HERE.txt').write_text('机神试炼 R6\n默认最远 C 视野，基础 M7 + 斩舰刀，零强化。\nWASD 移动，左键射击，右键/Q 斩击，空格闪击/按住推进，R 重开，Esc 暂停。\n首名指定远程敌人会掉落原步枪；靠近到 5.5 米内且无遮挡时按 F，完成牵引、接住、机械锁定、能量启动后直接开火。\n装配时仍可移动；冲刺或受击取消，原枪和掉落物保留，可重试。每局仍从 M7 开始。\n以后正常体验请使用桌面永久入口。\n',encoding='utf-8-sig')
records=[{'path':p.relative_to(build).as_posix(),'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in sorted(build.rglob('*')) if p.is_file() and 'UserData' not in p.relative_to(build).parts]
(evidence/'build-manifest.json').write_text(json.dumps(records,indent=2),encoding='utf-8')
with zipfile.ZipFile(evidence/'r6-source.zip','w',zipfile.ZIP_DEFLATED) as archive:
    for folder in ['Assets/Scripts','Assets/Editor','tools/p0','docs']:
        for p in (root/folder).rglob('*'):
            if p.is_file():archive.write(p,p.relative_to(root))
print('Packaged files:',len(records))
