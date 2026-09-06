from PIL import Image,ImageDraw,ImageFont
import pathlib,sys
W=pathlib.Path(__file__).resolve().parent;I=int(sys.argv[1]);D=W/f'iteration_{I:02d}'
ref=Image.open(W/'REFERENCE.png').convert('RGBA');mod=Image.open(D/'renders'/'FRONT.png').convert('RGBA')
bg=(31,40,49,255);out=Image.new('RGBA',(1600,1050),bg);font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',24);draw=ImageDraw.Draw(out)
rfbox=(30,90,324,423);scale=mod.width*.0012/.44
mbox=((30-180)*scale+mod.width/2, (90-422+161)*scale+mod.height/2,(324-180)*scale+mod.width/2,(423-422+161)*scale+mod.height/2)
a=ref.crop(rfbox).resize((760,860));b=mod.crop(tuple(round(v) for v in mbox)).resize((760,860))
out.alpha_composite(a,(20,80));out.alpha_composite(b,(820,80))
draw.text((20,20),'目标参考 / 同一高度与宽度比例',font=font,fill='white');draw.text((820,20),'Blender 实模 / 第 %02d 轮'%I,font=font,fill='white')
for z,label in [(294,'头顶'),(166,'额头传感器'),(124,'双眼'),(15,'下巴')]:
 y=80+int((422-z-90)*860/333)
 draw.line((20,y,780,y),fill=(218,158,54,100),width=1);draw.line((820,y,1580,y),fill=(218,158,54,100),width=1)
out.convert('RGB').save(D/'FRONT_ALIGNED_COMPARISON.jpg',quality=95)
