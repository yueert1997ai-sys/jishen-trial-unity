from pathlib import Path
from PIL import Image,ImageDraw,ImageFont,ImageOps
import json,hashlib
ROOT=Path(__file__).resolve().parent;OUT=ROOT/'stage_02_head';R=OUT/'renders'
BG=(20,25,28);FG=(233,237,235);SUB=(165,181,185);ACC=(207,55,45)
def font(n):return ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',n)
def fit(im,path,box):
    x,y,w,h=box;p=ImageOps.contain(Image.open(path).convert('RGB'),(w,h),Image.Resampling.LANCZOS);im.paste(p,(x+(w-p.width)//2,y+(h-p.height)//2))
def board(w,h,title,sub):
    im=Image.new('RGB',(w,h),BG);d=ImageDraw.Draw(im);d.rectangle((26,28,32,83),fill=ACC)
    d.text((48,24),title,font=font(33),fill=FG);d.text((48,79),sub,font=font(20),fill=SUB);return im,d
im,d=board(1780,1050,'TYPE E-01 ELITE  /  头部重制对比','同一观察角度与灯光  ·  重新制作破甲与眼窝  ·  真实 Blender 模型渲染')
d.text((28,138),'上一版',font=font(25),fill=SUB);d.text((918,138),'本次修改',font=font(25),fill=FG)
fit(im,ROOT/'stage_01/renders/HEAD_INVADED.png',(24,182,850,835));fit(im,R/'HEAD_INVADED.png',(906,182,850,835))
im.save(OUT/'HEAD_R02_COMPARISON.jpg',quality=96)
im,d=board(1880,1040,'TYPE E-01 ELITE  /  残破头部 R02','低压眉部与深红独眼  ·  焦黑碎甲  ·  交织侵蚀结构  ·  装回整机检查')
fit(im,R/'HERO.png',(18,137,635,872))
for label,shot,box in [('侵蚀侧','HEAD_INVADED',(687,180,561,550)),('正面','HEAD_FRONT',(1283,180,561,550)),('头部另一侧','HEAD_REVERSE',(701,798,260,210)),('侧面','HEAD_PROFILE',(1003,798,260,210)),('俯视辨识','GAME_READ',(1427,798,280,210))]:
    d.text((box[0],box[1]-36),label,font=font(21),fill=FG);fit(im,R/(shot+'.png'),box)
im.save(OUT/'HEAD_R02_REVIEW.jpg',quality=96)
report={'model':str(OUT/'TYPE_E01_ELITE_HEAD_R02.blend'),'sha256':hashlib.sha256((OUT/'TYPE_E01_ELITE_HEAD_R02.blend').read_bytes()).hexdigest(),'boards':['HEAD_R02_COMPARISON.jpg','HEAD_R02_REVIEW.jpg'],'renders':[p.name for p in sorted(R.glob('*.png'))],'render_source':'Saved and reopened Blender assembly. Only image layout and scaling used.'}
(OUT/'review_manifest.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report,indent=2))
