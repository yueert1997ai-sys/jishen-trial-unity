from pathlib import Path
import os, subprocess, sys

ROOT = Path(__file__).resolve().parent
cache = ROOT / 'cache'
cache.mkdir(exist_ok=True)
env = os.environ.copy()
for key, folder in [('TEMP','temp'),('TMP','temp'),('BLENDER_USER_CONFIG','blender_config'),('OPTIX_CACHE_PATH','optix'),('CUDA_CACHE_PATH','cuda')]:
    target = cache / folder
    target.mkdir(exist_ok=True)
    env[key] = str(target)
script = sys.argv[1] if len(sys.argv) > 1 else 'build_boss.py'
log_path = ROOT / (Path(script).stem + '.log')
with log_path.open('w', encoding='utf-8') as log:
    result = subprocess.run([r'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe', '--background', '--python-exit-code', '1', '--python', str(ROOT/script), '--', *sys.argv[2:]], cwd=str(ROOT), env=env, stdout=log, stderr=subprocess.STDOUT)
print(log_path.read_text(encoding='utf-8',errors='replace')[-6500:])
raise SystemExit(result.returncode)
