from pathlib import Path
import struct, subprocess, json
src=Path(r'E:\SteamLibrary\GameStudyExports\GB4\UmodelSaved\GB4\Content\Sound\SE\bnk_se_mission_reside.uexp')
out=Path(r'E:\SteamLibrary\JishenBuildWork\CombatVelocity_R1\AudioSources')
b=src.read_bytes(); start=b.find(b'@UTF'); size=struct.unpack_from('>I',b,start+4)[0]+8
assert start>=0 and start+size<=len(b)
bank=out/'mission.acb'; bank.write_bytes(b[start:start+size])
vgm=Path(r'E:\SteamLibrary\GameStudyExports\MechaRefinement_R04\ChatWorkspace\work\vgmstream\vgmstream-cli.exe')
r=subprocess.run([str(vgm),'-m',str(bank)],capture_output=True,text=True)
print(r.stdout, r.stderr)
(out/'bank-meta.txt').write_text(r.stdout,encoding='utf8')
all_streams=subprocess.run([str(vgm),'-m','-s','1','-S','0',str(bank)],capture_output=True,text=True,check=True)
(out/'all-streams.txt').write_text(all_streams.stdout,encoding='utf8')
print('Named stream index exported:',all_streams.stdout.count('stream index:'))
