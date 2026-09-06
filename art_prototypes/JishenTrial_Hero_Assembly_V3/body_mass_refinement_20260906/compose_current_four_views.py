"""Lay out unretouched Blender renders on a labeled contact sheet."""
from PIL import Image,ImageDraw,ImageFont
import pathlib,sys
W=pathlib.Path(__file__).resolve().parent
args={a.split('=',1)[0]:a.split('=',1)[1] for a in sys.argv if '=' in a}
R=pathlib.Path(args.get('out',str(W/'delivery/four_views')))
fontpath=pathlib.Path('C:/Windows/Fonts/msyh.ttc')
font=ImageFont.truetype(str(fontpath),42)
small=ImageFont.truetype(str(fontpath),24)
tile_w,tile_h=1800,1320
margin,gap,header,label=40,24,106,72
sheet=Image.new('RGB',(tile_w*2+margin*2+gap,(tile_h+label)*2+header+margin+gap),(17,21,29))
draw=ImageDraw.Draw(sheet)
draw.text((margin,24),args.get('title','VALKYR  /  当前模型四视图'),font=font,fill=(234,240,249))
draw.text((sheet.width-margin,48),'正交视图 · 含 RAIKEN Mk-II',font=small,fill=(151,173,198),anchor='rm')
for i,(name,title) in enumerate([('FRONT','正面  /  FRONT'),('BACK','背面  /  BACK'),('LEFT','左侧  /  LEFT'),('RIGHT','右侧  /  RIGHT')]):
    x=margin+(i%2)*(tile_w+gap);y=header+(i//2)*(tile_h+label+gap)
    draw.rounded_rectangle((x,y,x+tile_w,y+tile_h+label),radius=12,fill=(25,30,40))
    draw.text((x+28,y+17),title,font=small,fill=(225,234,246))
    im=Image.open(R/(name+'.png')).convert('RGBA')
    assert im.size==(tile_w,tile_h),im.size
    sheet.paste(im,(x,y+label),im)
sheet.save(R/'FOUR_VIEWS.png')
print(R/'FOUR_VIEWS.png')
