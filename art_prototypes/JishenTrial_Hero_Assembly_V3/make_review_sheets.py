"""Layout of actual renders only: identical scale, camera and framing in before/after."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
OUT=Path(__file__).resolve().parent
font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',28)
small=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',21)
sheet=Image.new('RGB',(1680,1120),(231,233,236));draw=ImageDraw.Draw(sheet)
draw.text((28,16),'头身比例调整 · 实际 Blender 模型渲染',font=font,fill=(30,40,50))
panels=[(OUT/'iterations/small_head_before_feedback/FRONT.png','调整前 · 头盔宽 0.195 m'),
        (OUT/'renders/FRONT.png','调整后 · 等比放大 1.50×'),
        (OUT/'renders/FRONT_3Q.png','调整后 · 整机四分之三视角')]
for i,(path,label) in enumerate(panels):
    im=Image.open(path).convert('RGB');im.thumbnail((544,980),Image.Resampling.LANCZOS)
    x=16+i*556;sheet.paste(im,(x+(544-im.width)//2,110));draw.text((x+10,70),label,font=small,fill=(30,40,50))
draw.text((28,1030),'头部整体放大并下移 20 mm；机身及拍摄尺度一致。侧面造型仍待修改。',font=small,fill=(45,55,65))
sheet.save(OUT/'renders/HEAD_SIZE_BEFORE_AFTER.jpg',quality=94)
names=['FRONT','LEFT','BACK','RIGHT','FRONT_3Q','HEAD_ON_BODY','HEAD_SIDE','CLAY_3Q']
allviews=Image.new('RGB',(1600,1110),(231,233,236));d=ImageDraw.Draw(allviews)
for i,name in enumerate(names):
    x=(i%4)*400;y=(i//4)*550;im=Image.open(OUT/'renders'/(name+'.png')).convert('RGB');im.thumbnail((390,495),Image.Resampling.LANCZOS)
    allviews.paste(im,(x+(400-im.width)//2,y+35));d.text((x+12,y+6),name,font=small,fill=(30,40,50))
allviews.save(OUT/'renders/ASSEMBLY_REVIEW_SHEET.jpg',quality=93)
