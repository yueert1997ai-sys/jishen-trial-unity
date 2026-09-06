"""Put authentic model renders on plain white for review; preserve source pixels."""
from PIL import Image, ImageDraw, ImageFont
from pathlib import Path
import sys
D=Path(__file__).resolve().parent
args=dict(a.split('=',1) for a in sys.argv[1:] if '=' in a)
R=D/args.get('folder','renders')
O=R/'white';O.mkdir(exist_ok=True,parents=True)
for p in R.glob('*.png'):
    im=Image.open(p).convert('RGBA')
    out=Image.new('RGB',im.size,(249,249,248));out.paste(im,mask=im.getchannel('A'))
    out.save(O/p.name)
    print(p.name,im.size,im.getchannel('A').getbbox())
    if p.stem=='HERO' and R.name=='renders':
        bbox=im.getchannel('A').getbbox()
        x0,y0,x1,y1=bbox
        cut=out.crop((max(0,x0-110),max(0,y0-100),min(out.width,x1+110),min(out.height,y1+100)))
        cut.save(D/'TYPE01_MODEL_PREVIEW.png')
        cut.save(D/'TYPE01_MODEL_PREVIEW.jpg',quality=96,subsampling=0)
if args.get('sheet')=='1':
    W,H=3000,2410
    page=Image.new('RGB',(W,H),'white');draw=ImageDraw.Draw(page)
    font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',30)
    hero=Image.open(O/'HERO.png');hero.thumbnail((2880,1250),Image.Resampling.LANCZOS)
    page.paste(hero,((W-hero.width)//2,15))
    draw.text((76,35),'TYPE-01 · LASER RIFLE',fill=(45,51,55),font=font)
    positions=[(60,1240,'EMITTER','发射筒'),(1530,1240,'ENERGY_CELL','能量模块与握把'),
               (60,1810,'OPTIC','瞄具与导轨'),(1530,1810,'STOCK','枪托')]
    for x,y,name,label in positions:
        im=Image.open(O/(name+'.png'));im.thumbnail((1400,520),Image.Resampling.LANCZOS)
        page.paste(im,(x+(1400-im.width)//2,y))
        draw.text((x+24,y+510),label,fill=(70,76,80),font=font)
    page.save(D/'TYPE01_MODEL_DETAILS.jpg',quality=95,subsampling=0)
    page.save(D/'TYPE01_MODEL_DETAILS.png')
    reference=Image.open(D/'references/TYPE01_LASER_RIFLE_REFERENCE.png').convert('RGB')
    ref_side=reference.crop((22,400,796,632))
    mod_side=Image.open(R/'SIDE.png').convert('RGBA')
    mod_side=mod_side.crop(mod_side.getchannel('A').getbbox())
    comparison=Image.new('RGB',(2800,1920),'white')
    cd=ImageDraw.Draw(comparison)
    for y,im,title in [(75,ref_side,'原始设计 · 侧面'),(1000,mod_side,'Blender 模型 · 侧面')]:
        cd.text((90,y),title,font=font,fill=(55,62,65))
        im=im.convert('RGBA')
        scale=2620/im.width
        im=im.resize((2620,round(im.height*scale)),Image.Resampling.LANCZOS)
        comparison.paste(im,(90,y+90),im)
    comparison.save(D/'TYPE01_REFERENCE_COMPARISON.jpg',quality=95,subsampling=0)
