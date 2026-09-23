"""Use the established silent Player runner with one pinned V3 build/evidence location."""
from pathlib import Path
import os, runpy
os.environ['MECH_LOOP_V2_BUILD']='Builds/CombatFoundation_V3_20260917'
os.environ['MECH_LOOP_V2_EVIDENCE']='AuditEvidence/combat-foundation-v3'
os.environ['MECH_LOOP_V2_VERSION']='combat-foundation-v3-20260917'
runpy.run_path(str(Path(__file__).with_name('run_loop_v2.py')),run_name='__main__')
