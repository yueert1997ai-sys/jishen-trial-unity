"""Package R9 without changing the permanent desktop entry."""
from pathlib import Path
import hashlib,json,zipfile
root=Path(__file__).resolve().parents[2]
build=root/'Builds/CombatSlice_R9_20260916'
evidence=root/'AuditEvidence/combat-slice-r9'
launch='@echo off\r\nsetlocal\r\nset "MECH_EQUIPMENT_PROFILE=%~dp0UserData\\profile.json"\r\nif not exist "%~dp0UserData" mkdir "%~dp0UserData"\r\nstart "" "%~dp0MECH_TRIAL_P0.exe" -combatSlice -sliceView C -screen-fullscreen 0 -screen-width 1600 -screen-height 900\r\n'
(build/'PLAY_DEFAULT.cmd').write_bytes(launch.encode('ascii'))
(build/'START_HERE.txt').write_text('机神试炼 R9 · 战斗声音重整\n默认最远 C 视野，基础 M7 + 斩舰刀，零强化。\n实弹爆发、三段刀声、金属承压、破防、贯穿斩杀、自机受击、推进与机械装配已重做。\n破防与斩杀瞬间，持续引擎和枪声尾响会短暂避让。\nWASD 移动，左键射击，右键/Q 斩击，空格闪击/按住推进，R 重开，Esc 暂停。\n靠近首名指定远程敌人的掉落武器，按 F 牵引装配；冲刺或受击可取消重试。\n以后正常体验请使用桌面永久入口。\n',encoding='utf-8-sig')
records=[{'path':p.relative_to(build).as_posix(),'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in sorted(build.rglob('*')) if p.is_file() and 'UserData' not in p.relative_to(build).parts]
(evidence/'build-manifest.json').write_text(json.dumps(records,indent=2),encoding='utf-8')
with zipfile.ZipFile(evidence/'r9-source.zip','w',zipfile.ZIP_DEFLATED) as archive:
    for folder in ['Assets/Scripts','Assets/Editor','Assets/Resources/Audio/Combat/R9','tools/p0','docs']:
        for p in (root/folder).rglob('*'):
            if p.is_file():archive.write(p,p.relative_to(root))
print('Packaged files:',len(records))
