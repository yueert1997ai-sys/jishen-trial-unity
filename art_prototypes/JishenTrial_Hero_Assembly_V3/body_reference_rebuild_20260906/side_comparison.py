from pathlib import Path
from PIL import Image,ImageDraw,ImageFont,ImageOps
W=Path(__file__).resolve().parent
fontpath='C:/Windows/Fonts/msyh.ttc'
def font(n):return ImageFont.truetype(fontpath,n)
ref=Image.open(W/'REFERENCE.png').crop((334,82,518,515)).convert('RGBA')
old=Image.open(W/'stage_04/renders/ALIGNED_LEFT.png').convert('RGBA');old=old.crop(old.getchannel('A').getbbox())
new=Image.open(W/'stage_09/renders/LEFT.png').convert('RGBA');new=new.crop(new.getchannel('A').getbbox())
out=Image.new('RGB',(1650,1410),(12,20,29));d=ImageDraw.Draw(out)
d.text((45,24),'VALKYR TYPE-01 / 侧面体积复查',font=font(34),fill=(226,238,249))
d.text((45,80),'同方向、同高度对照 · 实际 Blender 渲染',font=font(22),fill=(129,160,184))
for i,(im,title) in enumerate(((ref,'设计图'),(old,'修改前'),(new,'侧面体积修正后'))):
 x=i*550;d.rounded_rectangle((x+12,130,x+538,1320),8,fill=(20,31,42),outline=(50,75,95),width=2)
 d.text((x+35,151),title,font=font(25),fill=(204,224,238))
 scale=min(492/im.width,1070/im.height);im=im.resize((round(im.width*scale),round(im.height*scale)),Image.Resampling.LANCZOS);out.paste(im,(x+(550-im.width)//2,1290-im.height),im)
d.text((45,1342),'胸腹、骨盆、四肢与脚部补足前后体积；同步修正膝踝站姿。',font=font(22),fill=(171,192,207))
out.save(W/'SIDE_DEPTH_COMPARISON.jpg',quality=94)
