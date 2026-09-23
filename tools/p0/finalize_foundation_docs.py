"""Align the current handoff labels; preserve historical release descriptions."""
from pathlib import Path
root=Path(__file__).resolve().parents[2]
p=root/'README.md';s=p.read_text('utf-8-sig')
s=s.replace('2026-09-17 本地当前版本：**Combat Loop V2 · 30 秒战斗对照**。','上一版本：**Combat Loop V2 · 30 秒战斗对照**。')
p.write_text(s,encoding='utf-8')
p=root/'docs/HANDOFF.md';s=p.read_text('utf-8-sig')
s=s.replace('2026-09-17 当前本地版本为 Combat Loop V2 · 30 秒战斗对照，','上一版本 Combat Loop V2 · 30 秒战斗对照，')
s=s.replace('Current local work: [R9 combat audio]','Historical local work: [R9 combat audio]')
header='# MECH ROUGE Handoff Status'
if not s.startswith(header):
    before,after=s.split(header,1)
    paragraphs=after.lstrip().split('\n\n',1)
    s=header+'\n\n'+paragraphs[0]+'\n\n'+before.strip()+'\n\n'+paragraphs[1]
p.write_text(s,encoding='utf-8')
p=root/'AGENTS.md';s=p.read_text('utf-8-sig')
s=s.replace('Current development checkpoint is R9 (combat audio overhaul); R8 ballistic/blade presentation is retained.',
    'Current development checkpoint is Combat Foundation V3; V2 Lab, R9 audio, and R8 presentation builds are retained. Read docs/COMBAT_FOUNDATION_V3.md and docs/HADES_FOUNDATION_ANALYSIS.md for the current architecture and evidence. CombatRules.json owns slice definitions, CombatActionQueue owns buffered requests, DamageResult owns committed hit outcomes, and EncounterFlow/CombatEncounterDirector own slice progression. Keep these ownership boundaries when extending the demo.')
p.write_text(s,encoding='utf-8')
print('Current documentation labels aligned')
