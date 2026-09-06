"""Place unretouched, actual Blender renders on white; align reference by total length."""
from PIL import Image,ImageDraw,ImageFont
from pathlib import Path
import sys,json
D=Path(__file__).resolve().parent
args=dict(a.split('=',1) for a in sys.argv if '=' in a)
R=D/args.get('folder','renders');W=R/'white';W.mkdir(exist_ok=True)
for f in R.glob('*.png'):
    im=Image.open(f).convert('RGBA');bg=Image.new('RGBA',im.size,'white');bg.alpha_composite(im);bg.convert('RGB').save(W/f.name)
font=lambda n:ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',n)
if (R/'HERO.png').exists():
    im=Image.open(R/'HERO.png').convert('RGBA');bb=im.getchannel('A').getbbox()
    if bb:
        crop=im.crop((max(0,bb[0]-80),max(0,bb[1]-70),min(im.width,bb[2]+80),min(im.height,bb[3]+70)))
        bg=Image.new('RGBA',crop.size,'white');bg.alpha_composite(crop)
        bg.convert('RGB').save(D/('BOSS_M14_EBR_PREVIEW.jpg' if R.name=='renders' else 'preview/HERO_WHITE.jpg'),quality=96)
if (R/'SIDE.png').exists():
    # Exact photo, without its drop shadow; both silhouettes use one uniform scale.
    ref=Image.open(D/'references/BOSS_M14_EBR_REFERENCE.png').convert('RGB').crop((35,148,680,309))
    mod=Image.open(R/'SIDE.png').convert('RGBA');bb=mod.getchannel('A').getbbox();mod=mod.crop(bb)
    width=2520;refscale=width/ref.width;modelscale=(622*refscale)/mod.width
    ref=ref.resize((width,round(ref.height*refscale)),Image.Resampling.LANCZOS)
    mod=mod.resize((round(mod.width*modelscale),round(mod.height*modelscale)),Image.Resampling.LANCZOS)
    sheet=Image.new('RGB',(2760,1540),'white');dr=ImageDraw.Draw(sheet)
    dr.text((120,54),'原图',fill='#333631',font=font(32));sheet.paste(ref,(120,112))
    dr.text((120,812),'模型 · 同长度对齐',fill='#333631',font=font(32))
    sheet.paste(mod,(120+round(11*refscale),906+round(13*refscale)),mod)
    if R.name=='renders':out=D/'BOSS_M14_EBR_REFERENCE_COMPARISON.jpg'
    else:out=R/'REFERENCE_COMPARISON.jpg'
    sheet.save(out,quality=97)
if all((W/(n+'.png')).exists() for n in ['STOCK','RECEIVER','HANDGUARD','MUZZLE']):
    sh=Image.new('RGB',(3000,2230),'white');dr=ImageDraw.Draw(sh)
    for i,(name,lab) in enumerate([('STOCK','枪托与镂空支架'),('RECEIVER','机匣、照门与握把'),('HANDGUARD','弧面护木与导轨'),('MUZZLE','前准星与枪口')]):
        x=50+(i%2)*1500;y=50+(i//2)*1100;dr.text((x+20,y),lab,fill='#333631',font=font(34))
        im=Image.open(W/(name+'.png'));im.thumbnail((1400,950),Image.Resampling.LANCZOS)
        sh.paste(im,(x+(1400-im.width)//2,y+90+(950-im.height)//2))
    sh.save(D/'BOSS_M14_EBR_DETAILS.jpg',quality=97)
print('EBR_WHITE_COMPOSITES_READY',R)
