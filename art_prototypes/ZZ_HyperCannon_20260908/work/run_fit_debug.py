from pathlib import Path
import traceback
fit_file=Path(__file__).resolve().parents[1]/'fit_to_valkyr.py'
try:
    exec(compile(fit_file.read_text(encoding='utf-8'),str(fit_file),'exec'),{'__file__':str(fit_file)})
except Exception:
    print(traceback.format_exc())
