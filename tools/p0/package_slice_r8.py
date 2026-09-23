"""Package R8 without changing the permanent desktop entry."""
from pathlib import Path
import hashlib,json,zipfile
root=Path(__file__).resolve().parents[2]
build=root/'Builds/CombatSlice_R8_20260916'
evidence=root/'AuditEvidence/combat-slice-r8'
launch='@echo off\r\nsetlocal\r\nset "MECH_EQUIPMENT_PROFILE=%~dp0UserData\\profile.json"\r\nif not exist "%~dp0UserData" mkdir "%~dp0UserData"\r\nstart "" "%~dp0MECH_TRIAL_P0.exe" -combatSlice -sliceView C -screen-fullscreen 0 -screen-width 1600 -screen-height 900\r\n'
(build/'PLAY_DEFAULT.cmd').write_bytes(launch.encode('ascii'))
(build/'START_HERE.txt').write_text('机神试炼 R8 · 实弹与刀击细化\n默认最远 C 视野，基础 M7 + 斩舰刀，零强化。\n枪口喷焰、短曳光、弹壳、局部装甲命中、切痕、碎甲和死亡零件已统一调整。\n枪打破防后，第一刀可以斩杀普通小兵；命中停顿可立刻用冲刺取消。\nWASD 移动，左键射击，右键/Q 斩击，空格闪击/按住推进，R 重开，Esc 暂停。\n首名指定远程敌人会掉落原步枪；靠近到 5.5 米内且无遮挡时按 F 装配。冲刺或受击可取消并重试，每局仍从 M7 开始。\n以后正常体验请使用桌面永久入口。\n',encoding='utf-8-sig')
records=[{'path':p.relative_to(build).as_posix(),'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in sorted(build.rglob('*')) if p.is_file() and 'UserData' not in p.relative_to(build).parts]
(evidence/'build-manifest.json').write_text(json.dumps(records,indent=2),encoding='utf-8')
with zipfile.ZipFile(evidence/'r8-source.zip','w',zipfile.ZIP_DEFLATED) as archive:
    for folder in ['Assets/Scripts','Assets/Editor','Assets/Resources/Audio/Combat/R7','tools/p0','docs']:
        for p in (root/folder).rglob('*'):
            if p.is_file():archive.write(p,p.relative_to(root))
print('Packaged files:',len(records))
