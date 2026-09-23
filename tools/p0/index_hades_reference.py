"""Read-only reference index. Exports symbols, locations and hashes, never source or assets."""
from pathlib import Path
import hashlib, json, re

root=Path(__file__).resolve().parents[2]
source=Path('D:/BaiduNetdiskDownload/Hades.Multi.9/Content')
out=root/'AuditEvidence/combat-foundation-v3'
symbols={};files=[]
for p in sorted((source/'Scripts').glob('*.lua')):
    data=p.read_bytes();text=data.decode('utf-8-sig',errors='replace')
    found=[]
    for match in re.finditer(r'^function\s+([\w.:]+)\s*\(',text,re.M):
        name=match.group(1);line=text.count('\n',0,match.start())+1
        entry={'function':name,'path':p.relative_to(source).as_posix(),'line':line}
        symbols.setdefault(name,[]).append(entry);found.append(entry)
    files.append({'path':p.relative_to(source).as_posix(),'bytes':len(data),
                  'sha256':hashlib.sha256(data).hexdigest(),'topLevelNamedFunctions':len(found)})
targets=['wait','waitScreenTime','waitUntil','CalculateDamageMultipliers','Damage','DamageEnemy','DamageHero',
         'KillEnemy','GetWeaponData','DoWeaponHitSimulationSlow','AddSimSpeedChange','LastKillPresentation',
         'AttackerAI','DoAttackerAILoop','AttackOnce','GetWeaponAIData','SetupRunData','ProcessDataInheritance',
         'StartNewRun','ChooseEncounter','SetupEncounter','ChooseRoomReward','StartEncounter','StartEncounterEffects',
         'EndEncounterEffects','HandleEnemySpawns','HandleNextSpawn','CalculateActiveEnemyCap','SpawnRoomReward',
         'CheckForEncounterEnemiesDead','KillMapThreads']
result={'reference':str(source),'indexType':'lexical top-level named function index; not a complete dynamic call graph',
        'luaFiles':len(files),'luaBytes':sum(f['bytes'] for f in files),
        'indexedFunctions':sum(len(v) for v in symbols.values()),'files':files,
        'entryPoints':{name:symbols.get(name,[]) for name in targets}}
(out/'hades-script-index.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({k:v for k,v in result.items() if k not in ['files','entryPoints']},ensure_ascii=False,indent=2))
