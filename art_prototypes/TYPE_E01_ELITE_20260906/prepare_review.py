"""Arrange actual Blender renders into readable approval boards; no AI image substitution."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont,ImageOps
import json,hashlib
ROOT=Path(__file__).resolve().parent;OUT=ROOT/'stage_01';R=OUT/'renders'
BG=(19,25,29);FG=(232,238,238);SUB=(149,171,179);RED=(235,73,62)
def font(size):return ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',size)
def fit(canvas,path,box,top=False):
    x,y,w,h=box;im=ImageOps.contain(Image.open(path).convert('RGB'),(w,h),Image.Resampling.LANCZOS)
    canvas.paste(im,(x+(w-im.width)//2,y if top else y+(h-im.height)//2))
def board(w,h,title,sub):
    im=Image.new('RGB',(w,h),BG);d=ImageDraw.Draw(im)
    d.rectangle((28,31,35,80),fill=RED);d.text((52,25),title,font=font(33),fill=FG)
    d.text((52,78),sub,font=font(20),fill=SUB)
    return im,d
im,d=board(1880,1220,'TYPE E-01 ELITE  /  第一版整机造型','液态生命金属侵蚀体  ·  白色 E-01 同源机体  ·  1.5 倍身高  ·  真实 Blender 模型渲染')
fit(im,R/'HERO.png',(24,128,935,1055))
for title,shot,rect in [('半毁头部与独眼','HEAD_INVADED',(1000,166,395,420)),('侵蚀核心','CORE',(1432,166,405,420)),('异化右臂','CLAW',(1000,680,400,465)),('背部生长结构','DORSAL',(1420,680,410,465))]:
    d.text((rect[0],rect[1]-35),title,font=font(22),fill=FG);fit(im,R/(shot+'.png'),rect)
im.save(OUT/'ELITE_MODEL_REVIEW.jpg',quality=96)
im,d=board(2040,940,'TYPE E-01 ELITE  /  整机四视图','正面、左侧、背面、右侧统一比例  ·  同一保存文件与同一套几何')
for i,(title,shot) in enumerate([('正面','FRONT'),('左侧','LEFT'),('背面','BACK'),('右侧','RIGHT')]):
    x=24+i*504;d.text((x+10,143),title,font=font(24),fill=FG);fit(im,R/(shot+'.png'),(x,184,480,700))
im.save(OUT/'ELITE_FOUR_VIEWS.jpg',quality=96)
im,d=board(1880,850,'TYPE E-01 ELITE  /  侵蚀细节','实模近景  ·  破甲、交织金属、红色细脉与生长根部')
for i,(title,shot) in enumerate([('头部破损','HEAD_INVADED'),('胸腹核心','CORE'),('右臂巨爪','CLAW'),('背部触须','DORSAL')]):
    x=25+i*466;d.text((x+10,145),title,font=font(24),fill=FG);fit(im,R/(shot+'.png'),(x,190,435,620),top=True)
im.save(OUT/'ELITE_DETAILS.jpg',quality=96)
manifest={'model':str(OUT/'TYPE_E01_ELITE_STAGE01.blend'),'sha256':hashlib.sha256((OUT/'TYPE_E01_ELITE_STAGE01.blend').read_bytes()).hexdigest(),'renders':[p.name for p in sorted(R.glob('*.png'))],'review_boards':['ELITE_MODEL_REVIEW.jpg','ELITE_FOUR_VIEWS.jpg','ELITE_DETAILS.jpg'],'images_are':'Native Blender render outputs; only resized and arranged into boards','stage':'Modeling approval gate. Not rigged or integrated into the game yet.'}
(OUT/'review_manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
print(json.dumps(manifest,indent=2))
