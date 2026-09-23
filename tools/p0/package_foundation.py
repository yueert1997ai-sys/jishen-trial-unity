"""Summarize actual Player evidence and package the authored V3 source/configuration."""
from pathlib import Path
import difflib, hashlib, json, sys, zipfile

root=Path(__file__).resolve().parents[2]
base=root/'AuditEvidence/combat-foundation-v3'
build=root/'Builds/CombatFoundation_V3_20260917'
modes=['foundation','lab','tactics','loop','break','play','compare','check','gun','rhythm','absorption',
       'contact','regression','terrain','audio','sound','impact','detail','replay','labreplay']
assembly=build/'MECH_TRIAL_P0_Data/Managed/Assembly-CSharp.dll'
fingerprint=hashlib.sha256(assembly.read_bytes()).hexdigest()
records=[]
for mode in modes:
    run=sorted(p for p in base.glob(mode+'-*') if p.is_dir())[-1]
    report=(run/'quick-check.txt').read_text('utf-8-sig')
    metadata=json.loads((run/'run.json').read_text('utf-8-sig'))
    assert metadata['exit']==0 and 'P0_CHECK code=0 errors=0' in report, f'Failed {mode}'
    assert Path(metadata['args'][0]).resolve()==(build/'MECH_TRIAL_P0.exe').resolve(), f'Wrong build: {mode}'
    assert assembly.stat().st_mtime<=(run/'quick-check.txt').stat().st_mtime, f'Stale test: {mode}'
    records.append({'mode':mode,'path':run.relative_to(root).as_posix(),'passes':sum(line.startswith('PASS ') for line in report.splitlines())})
summary={'build':build.relative_to(root).as_posix(),'assemblySha256':fingerprint,'suiteCount':len(records),
         'passCount':sum(r['passes'] for r in records),'suites':records,
         'scope':'Actual Windows Player contracts, regressions and scripted public-input runs; no human feel or listening approval.'}
(base/'test-summary.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2),encoding='utf-8')
if '--summary-only' in sys.argv:
    print(json.dumps(summary,ensure_ascii=False,indent=2));raise SystemExit(0)
launch='@echo off\r\nsetlocal\r\nset "MECH_EQUIPMENT_PROFILE=%~dp0UserData\\profile.json"\r\nif not exist "%~dp0UserData" mkdir "%~dp0UserData"\r\nstart "" "%~dp0MECH_TRIAL_P0.exe" -combatSlice -sliceView C -screen-fullscreen 0 -screen-width 1600 -screen-height 900\r\n'
(build/'PLAY_DEFAULT.cmd').write_bytes(launch.encode('ascii'))
(build/'START_HERE.txt').write_text('机神试炼 · Combat Foundation V3\n2026-09-17\n'
    '沿用桌面“机神试炼”入口。此包重构动作请求、伤害结算、敌人攻击阶段与遭遇推进。\n'
    'WASD 移动，左键射击，右键/Q 斩击，空格冲刺/按住推进，E 导弹，F 夺取。\n'
    'F3 换 M7/M14 并重开；F8 直达 Boss；F9 进入/退出短战斗，1/2/3 换方案。\n'
    'R 重开，Esc 暂停；F5/F6/F7 比较视野，默认 C。\n'
    '普通近战/远程敌人出手后有短暂恢复；冲刺预输入会保留按下时的方向。\n'
    '技术检查通过；是否好玩与音色是否满意仍需实际试玩。\n',encoding='utf-8-sig')
manifest=[{'path':p.relative_to(build).as_posix(),'sha256':hashlib.sha256(p.read_bytes()).hexdigest()}
          for p in sorted(build.rglob('*')) if p.is_file() and 'UserData' not in p.relative_to(build).parts]
(base/'build-manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
folders=['Assets/Scripts','Assets/Editor','Assets/Resources/Foundation','tools/p0','docs','Packages','ProjectSettings']
paths={p.relative_to(root).as_posix():p for folder in folders for p in (root/folder).rglob('*') if p.is_file()}
for name in ['README.md','AGENTS.md','Assets/Resources/Foundation.meta']:
    if(root/name).is_file():paths[name]=root/name
with zipfile.ZipFile(base/'combat-foundation-v3-source.zip','w',zipfile.ZIP_DEFLATED) as z:
    for name,p in sorted(paths.items()):z.write(p,name)
changed=[];patch=[]
with zipfile.ZipFile(base/'before-foundation-source.zip') as old:
    baseline=set(old.namelist())
    for name,p in sorted(paths.items()):
        new=p.read_bytes();before=old.read(name) if name in baseline else b''
        if new==before:continue
        changed.append({'path':name,'status':'modified' if name in baseline else 'added','sha256':hashlib.sha256(new).hexdigest()})
        if p.suffix.lower() in ['.cs','.json','.md','.py']:
            patch.extend(difflib.unified_diff(before.decode('utf-8-sig').splitlines(True),new.decode('utf-8-sig').splitlines(True),
                                             fromfile='baseline/'+name,tofile='current/'+name))
(base/'foundation-changes.patch').write_text(''.join(patch),encoding='utf-8')
(base/'changed-files.json').write_text(json.dumps(changed,indent=2),encoding='utf-8')
print(json.dumps({'suites':len(records),'passes':summary['passCount'],'packagedFiles':len(manifest),
                  'changedFiles':len(changed),'assemblySha256':fingerprint},indent=2))
