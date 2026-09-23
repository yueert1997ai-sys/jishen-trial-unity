"""Three sequential A/B pairs; run only after other Players/builds finish."""
from pathlib import Path
import datetime, json, subprocess, sys

root=Path(__file__).resolve().parents[2]
evidence=root/'AuditEvidence/drone-vfx-r1'
busy=subprocess.check_output(['powershell.exe','-NoProfile','-Command',"Get-CimInstance Win32_Process | Where-Object { $_.Name -in @('Unity.exe','MECH_TRIAL_P0.exe','blender.exe') } | Select-Object -ExpandProperty Name"],text=True)
if busy.strip():raise RuntimeError('Finish existing rendering/build processes first: '+busy)
result={'method':'Three sequential paired runs, alternating A/B order; all samples retained. The unchanged M02 Player is A, final R1 Player is B. Each Player records its own rendering method and assembly hash.','pairs':[]}
for pair_index in range(3):
    pair={}
    for version in (['baseline','release'] if pair_index%2==0 else ['release','baseline']):
        args=[sys.executable,str(root/'tools/p0/run_drone_vfx_r1.py'),'performance']
        if version=='baseline':args+=['--baseline']
        subprocess.run(args,cwd=root,check=True)
        path=sorted((evidence/version).glob('performance-*'))[-1]
        data=json.loads((path/'performance.json').read_text())
        data['evidence']=path.relative_to(root).as_posix()
        data['assembly_sha256']=json.loads((path/'run.json').read_text())['assembly_sha256']
        pair[version]=data
    result['pairs'].append(pair)
    (evidence/'paired-performance.json').write_text(json.dumps(result,indent=2),encoding='utf8')
    print('Pair',pair_index+1,'complete',flush=True)
