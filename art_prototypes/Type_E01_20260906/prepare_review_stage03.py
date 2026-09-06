"""Layout only: actual Blender renders and a crop of the supplied design sheet."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont,ImageOps
import json,hashlib
ROOT=Path(__file__).resolve().parent;OUT=ROOT/'stage_03';R=OUT/'renders'
BG=(22,28,34);FG=(236,239,240);SUB=(167,184,195)
def font(size):return ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',size)
def fit(canvas,source,rect):
    image=Image.open(source).convert('RGB') if isinstance(source,(str,Path)) else source.convert('RGB')
    x,y,w,h=rect;image=ImageOps.contain(image,(w,h),Image.Resampling.LANCZOS)
    canvas.paste(image,(x+(w-image.width)//2,y+(h-image.height)//2))
def board(w,h,title,subtitle):
    image=Image.new('RGB',(w,h),BG);draw=ImageDraw.Draw(image)
    draw.text((30,23),title,font=font(35),fill=FG);draw.text((32,78),subtitle,font=font(21),fill=SUB)
    return image,draw
image,draw=board(1800,1310,'TYPE E-01  /  03  白色小兵打磨','头部侧面加长  ·  哑光白色装甲  ·  双手持枪姿态')
for x,w,label,key in ((28,814,'整机站姿','3Q'),(880,892,'双手持枪','READY_3Q')):
    draw.text((x+8,135),label,font=font(24),fill=FG);fit(image,R/(key+'.png'),(x,180,w,1050))
draw.text((32,1260),'同一个可编辑 Blender 工程内保留两种姿态；以上均为真实模型渲染。',font=font(20),fill=SUB)
image.save(OUT/'STAGE03_REVIEW.jpg',quality=95)
image,draw=board(1920,940,'TYPE E-01  /  03  真实模型四视图','统一比例、正交相机；用于核对头身比例、装甲分层与装备位置。')
for i,(label,key) in enumerate((('正面','FRONT'),('左侧','LEFT'),('背面','BACK'),('右侧','RIGHT'))):
    x=26+i*474;draw.text((x+8,135),label,font=font(23),fill=FG);fit(image,R/(key+'.png'),(x,180,450,690))
image.save(OUT/'STAGE03_FOUR_VIEWS.jpg',quality=95)
source=Image.open(ROOT/'reference/head_detail_inspection.png').crop((291,15,530,242))
image,draw=board(1920,780,'TYPE E-01  /  03  头部侧面修正','加长头盔后部和颌侧结构，保留正面宽度、高度与内凹独眼。')
items=(('设计图侧面',source),('上一版侧面',ROOT/'stage_02/renders/HEAD_SIDE.png'),('当前侧面',R/'HEAD_SIDE.png'),('当前正面',R/'HEAD_FRONT.png'))
for i,(label,pic) in enumerate(items):
    x=26+i*474;draw.text((x+8,136),label,font=font(23),fill=FG);fit(image,pic,(x,183,450,520))
draw.text((32,734),'参考图之外均为实际几何渲染；未对模型轮廓做图像修饰。',font=font(19),fill=SUB)
image.save(OUT/'STAGE03_HEAD_COMPARISON.jpg',quality=95)
manifest={'model':str(OUT/'TYPE_E01_STAGE03.blend'),'model_sha256':hashlib.sha256((OUT/'TYPE_E01_STAGE03.blend').read_bytes()).hexdigest(),'raw_renders':[p.name for p in sorted(R.glob('*.png'))],'review_boards':[p.name for p in sorted(OUT.glob('STAGE03_*.jpg'))],'status':'Stage 03 modeling and two-hand pose completed; awaiting user visual review','image_processing':'Layout, resize and reference crop only; actual Blender renders, no shape retouching.'}
(OUT/'review_manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf8')
print(json.dumps(manifest,ensure_ascii=False,indent=2))
