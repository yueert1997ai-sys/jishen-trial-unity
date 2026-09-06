from PIL import Image,ImageStat
from pathlib import Path
import sys
D=Path(__file__).resolve().parent
r=Image.open(D/'references/BOSS_M14_EBR_REFERENCE.png').convert('RGB');m=Image.open(D/(sys.argv[1] if len(sys.argv)>1 else 'renders')/'white/SIDE.png').convert('RGB')
def project(x,z):return (int(((x-357)*(1.12522/622)/1.225+.5)*m.width),int((z-226)*(1.12522/622)/1.225*m.width+m.height/2))
for name,b in [('magazine',(308,236,330,263)),('receiver',(262,194,283,205)),('handguard',(392,192,423,211)),('stockpad',(51,215,59,264)),('grip',(181,259,192,276))]:
    box=(*project(*b[:2]),*project(*b[2:]));print(name,'reference',ImageStat.Stat(r.crop(b)).median,'render',ImageStat.Stat(m.crop(box)).median)
