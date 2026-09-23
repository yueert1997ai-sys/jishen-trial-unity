from pathlib import Path
import hashlib,json,zipfile,os
root=Path(__file__).resolve().parents[2]
build=root/os.environ.get('MECH_LOOP_V2_BUILD','Builds/CombatLoop_V2_20260916')
evidence=root/os.environ.get('MECH_LOOP_V2_EVIDENCE','AuditEvidence/combat-loop-v2')
launch='@echo off\r\nsetlocal\r\nset "MECH_EQUIPMENT_PROFILE=%~dp0UserData\\profile.json"\r\nif not exist "%~dp0UserData" mkdir "%~dp0UserData"\r\nstart "" "%~dp0MECH_TRIAL_P0.exe" -combatSlice -sliceView C -screen-fullscreen 0 -screen-width 1600 -screen-height 900\r\n'
(build/'PLAY_DEFAULT.cmd').write_bytes(launch.encode('ascii'))
(build/'START_HERE.txt').write_text('机神试炼 Combat Loop V2\n持续施压、打出失衡、突进爆发；六组敌人后挑战 Boss。\nWASD 移动，左键射击，右键/Q 斩击，空格闪击/按住推进，E 导弹。\nF3 切换 M7/M14 并重开，F8 直达 Boss，R 重开，Esc 暂停。\n冲刺中按斩击可缓冲，落地后衔接；Boss 冲击条满后露核。\n精英橙弧保护正面：绕侧射击，或 E 导弹逼其侧移后冲刺接斩。\n默认 C 视野，F5/F6/F7 比较视野。\n击败敌人后按 F 夺取装备；永久收藏沿用原有存档。\n自动测试通过不代表玩家已认可战斗手感。\n',encoding='utf-8-sig')
with (build/'START_HERE.txt').open('a',encoding='utf-8') as help_file:
 help_file.write('F9 进入/退出 30 秒短战斗，1/2/3 换方案并重开；短战斗不掉装备、不写收藏。\n每轮结束记录保存到启动入口使用的 UserData/CombatLabRuns。\n')
records=[{'path':p.relative_to(build).as_posix(),'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in sorted(build.rglob('*')) if p.is_file() and 'UserData' not in p.relative_to(build).parts]
(evidence/'build-manifest.json').write_text(json.dumps(records,indent=2),encoding='utf-8')
with zipfile.ZipFile(evidence/'combat-loop-v2-source.zip','w',zipfile.ZIP_DEFLATED) as archive:
 for folder in ['Assets/Scripts','Assets/Editor','tools/p0','docs']:
  for p in (root/folder).rglob('*'):
   if p.is_file():archive.write(p,p.relative_to(root))
 archive.write(root/'README.md','README.md')
print('Packaged files:',len(records))
