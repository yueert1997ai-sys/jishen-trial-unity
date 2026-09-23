"""Final readback and small review deliverables; the game itself stays in the D: project."""
from pathlib import Path
import hashlib, json, shutil

root=Path(__file__).resolve().parents[2]
base=root/'AuditEvidence/combat-foundation-v3'
out=Path('C:/Users/yue/Documents/Codex/2026-09-17/new-chat/outputs')
out.mkdir(parents=True,exist_ok=True)
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
baseline=json.loads((base/'baseline.json').read_text('utf-8'))
profile=root/'Launcher/UserData/profile.json'
profile_hash=sha(profile) if profile.exists() else None
assert profile_hash==baseline['profileSha256'],'Permanent collection changed'
release=json.loads((root/'Launcher/current.json').read_text('utf-8'))
assert release['version']=='CombatFoundation_V3_20260917' and len(release['tests'])==20
assert (root/baseline['release']['build']/'MECH_TRIAL_P0.exe').is_file(),'Previous playable build missing'
verification=sorted(p for p in (root/'Launcher/Verification').iterdir() if p.is_dir())[-1]
assert 'P0_CHECK code=0 errors=0' in (verification/'quick-check.txt').read_text('utf-8-sig')
for record in json.loads((base/'reference-inventory.json').read_text('utf-8')):
    assert sha(Path(record['path']))==record['sha256'],'Reference file changed'
protected=[]
for record in baseline['files']:
    name=record['path']
    if name.startswith(('Assets/Scenes/','Assets/Resources/ValkyrMotion/','Packages/','ProjectSettings/')):
        assert sha(root/name)==record['sha256'],f'Unexpected asset/settings change: {name}'
        protected.append(name)
result={'release':release['version'],'buildFilesVerified':release['verifiedFiles'],
        'launcherVerification':verification.relative_to(root).as_posix(),
        'permanentProfileUnchanged':True,'profileSha256':profile_hash,
        'retainedPreviousBuild':baseline['release']['build'],
        'protectedSceneAndSettingsFiles':len(protected),'referenceFilesUnchanged':True}
(base/'release-readback.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
shutil.copy2(root/'docs/HADES_FOUNDATION_ANALYSIS.md',out/'Hades玩法分析与迁移.md')
shutil.copy2(root/'docs/COMBAT_FOUNDATION_V3.md',out/'V3底层重构与验证.md')
review=out/'V3底层重构与验证.md'
review.write_text(review.read_text('utf-8').replace('(HADES_FOUNDATION_ANALYSIS.md)',
    '('+(out/'Hades玩法分析与迁移.md').as_posix()+')'),encoding='utf-8')
shutil.copy2(base/'foundation-changes.patch',out/'V3源码差异.patch')
shutil.copy2(base/'V3_full_combat.mp4',out/'V3基础装备实战.mp4')
shutil.copy2(base/'V3_blade_contact.mp4',out/'V3枪刀接触实录.mp4')
text='''# Hades 玩法架构适配 · 机神试炼 V3

已将当前 Unity Demo 的战斗与遭遇底层重构为 Combat Foundation V3，并发布到原桌面“机神试炼”入口。

此次接入：统一动作请求、只读战斗配置、已提交伤害结果、敌人前摇/出手/恢复阶段、独立遭遇状态机、带取消代号的出生预留。修复缓冲冲刺丢方向、致命回调重入和取消出生不归还计数等问题。普通近战/远程敌人出招后新增 0.18/0.22 秒恢复，便于读取反击窗口。

20 组实际 Player 检查、1412 项 PASS；构建零错误、零警告。基础装备、零强化的正常指令录制在 60.7 秒内胜利，剩余耐久 148.5；没有无敌或强制伤害。三组短战斗、声音、吸收、碰撞、暂停与重开回归均通过。

Hades 输入目录包含 Lua/SJSON 玩法层和游戏成品文件；未发现完整原生引擎工程。本次采用职责划分与行为规则，编写适合 Unity 的 C#，没有把第三方脚本或美术音频放进 Demo。手感、音色与乐趣尚未经过你的试玩认可。

- [Hades 代码分析与结构图](Hades玩法分析与迁移.md)
- [重构模块、测试与维护说明](V3底层重构与验证.md)
- [相对本次工作树基线的源码差异](V3源码差异.patch)
- [60.7 秒基础装备原生实战](V3基础装备实战.mp4)
- [7 秒枪击破防与刀击近景](V3枪刀接触实录.mp4)

工程：`D:/project-mecha-design/MECH ROUGE/handoff/GameplayLoop_V1`。构建：`Builds/CombatFoundation_V3_20260917`。证据：`AuditEvidence/combat-foundation-v3`。

继续双击桌面“机神试炼”。WASD 移动，左键射击，右键/Q 斩击，空格闪避/推进，E 导弹，F 夺取；R 重开，F8 Boss，F9 短战斗。永久启动器解析和静音 Player 核验通过；148 个构建文件已核对，旧 V2 Lab 与收藏存档保留，未提交或推送 Git。
'''
for filename in ['Hades玩法分析与迁移.md','V3底层重构与验证.md','V3源码差异.patch','V3基础装备实战.mp4','V3枪刀接触实录.mp4']:
    text=text.replace('('+filename+')','('+(out/filename).as_posix()+')')
(out/'交付说明.md').write_text(text,encoding='utf-8')
print(json.dumps(result,ensure_ascii=False,indent=2))
print('Review deliverables:',out)
