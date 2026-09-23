"""Validate one immutable built assembly. Native capture runs separately and never plays speakers."""
from concurrent.futures import ThreadPoolExecutor, as_completed
from pathlib import Path
import hashlib, json, subprocess, sys

root=Path(__file__).resolve().parents[2]
assembly=root/'Builds/CombatFoundation_V3_20260917/MECH_TRIAL_P0_Data/Managed/Assembly-CSharp.dll'
fingerprint=hashlib.sha256(assembly.read_bytes()).hexdigest()
modes=sys.argv[1:] or ['loop','break','gun','absorption','contact','impact','audio','check','regression','terrain','lab','play','compare']
def run(mode):
    assert hashlib.sha256(assembly.read_bytes()).hexdigest()==fingerprint, 'Build changed during validation'
    result=subprocess.run([sys.executable,str(Path(__file__).with_name('run_foundation.py')),mode],cwd=root,
                          capture_output=True,text=True,encoding='utf-8',errors='replace')
    print(result.stdout.strip(),flush=True)
    if result.stderr:print(result.stderr[-3000:],flush=True)
    assert hashlib.sha256(assembly.read_bytes()).hexdigest()==fingerprint, 'Build changed during validation'
    return mode,result.returncode
results={}
with ThreadPoolExecutor(max_workers=2) as pool:
    for future in as_completed([pool.submit(run,mode) for mode in modes]):
        mode,code=future.result();results[mode]=code
        print(json.dumps({'completed':mode,'exit':code,'assemblySha256':fingerprint}),flush=True)
out=root/'AuditEvidence/combat-foundation-v3/validation-batch.json'
out.write_text(json.dumps({'assemblySha256':fingerprint,'results':results},indent=2),encoding='utf-8')
sys.exit(1 if any(results.values()) else 0)
