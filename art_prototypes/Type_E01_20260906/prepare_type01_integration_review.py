from pathlib import Path
from PIL import Image,ImageOps,ImageDraw,ImageFont
import json,hashlib
ROOT=Path(__file__).resolve().parent;OUT=ROOT/'stage_04_TYPE01';R=OUT/'renders'
BG=(22,28,34);FG=(236,239,240);SUB=(167,184,195)
def font(size):return ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',size)
def fit(canvas,path,rect):
    x,y,w,h=rect;im=ImageOps.contain(Image.open(path).convert('RGB'),(w,h),Image.Resampling.LANCZOS)
    canvas.paste(im,(x+(w-im.width)//2,y+(h-im.height)//2))
def board(w,h,title,subtitle):
    im=Image.new('RGB',(w,h),BG);d=ImageDraw.Draw(im)
    d.text((30,23),title,font=font(34),fill=FG);d.text((32,78),subtitle,font=font(21),fill=SUB)
    return im,d
im,d=board(1800,1310,'TYPE E-01  /  TYPE-01 激光步枪接装','保留已确认机体  ·  装入完成版步枪  ·  适配双手握持姿态')
for x,w,title,shot in ((28,840,'垂持站姿','NEUTRAL_3Q'),(914,858,'双手持枪','READY_3Q')):
    d.text((x+8,135),title,font=font(24),fill=FG);fit(im,R/(shot+'.png'),(x,180,w,1050))
d.text((32,1260),'真实 Blender 渲染；整机工程保留步枪原始分件、材质和两种可编辑姿态。',font=font(20),fill=SUB)
im.save(OUT/'TYPE01_INTEGRATION_REVIEW.jpg',quality=95)
im,d=board(1920,940,'TYPE E-01  /  新步枪整机四视图','正、左、背、右统一比例展示；小兵机体沿用已确认版本。')
for i,(label,key) in enumerate((('正面','FRONT'),('左侧','LEFT'),('背面','BACK'),('右侧','RIGHT'))):
    x=26+i*474;d.text((x+8,135),label,font=font(23),fill=FG);fit(im,R/(key+'.png'),(x,180,450,690))
im.save(OUT/'TYPE01_INTEGRATION_FOUR_VIEWS.jpg',quality=95)
manifest={'model':str(OUT/'TYPE_E01_TYPE01_RIFLE_MASTER.blend'),'model_sha256':hashlib.sha256((OUT/'TYPE_E01_TYPE01_RIFLE_MASTER.blend').read_bytes()).hexdigest(),'renders':[p.name for p in sorted(R.glob('*.png'))],'review_boards':['TYPE01_INTEGRATION_REVIEW.jpg','TYPE01_INTEGRATION_FOUR_VIEWS.jpg'],'source':'Completed TYPE-01 editable rifle master, appended into the approved E-01 body.','image_processing':'Only layout and resizing; all views are actual Blender renders.'}
(OUT/'review_manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf8')
print(json.dumps(manifest,indent=2))
