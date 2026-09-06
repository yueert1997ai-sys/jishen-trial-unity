"""Arrange unretouched Blender renders for the user's first approval gate."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont, ImageOps
import json,hashlib

ROOT=Path(__file__).resolve().parent
OUT=ROOT/'stage_01'
RENDERS=OUT/'renders'
BG=(22,28,34); FG=(235,237,238); MUTED=(160,176,188)
font_path='C:/Windows/Fonts/msyh.ttc'
def font(size): return ImageFont.truetype(font_path,size)

def fitted(canvas,source,rect):
    im=Image.open(source).convert('RGB') if isinstance(source,(Path,str)) else source.convert('RGB')
    x,y,w,h=rect
    im=ImageOps.contain(im,(w,h),Image.Resampling.LANCZOS)
    canvas.paste(im,(x+(w-im.width)//2,y+(h-im.height)//2))

def base(w,h,title,subtitle):
    im=Image.new('RGB',(w,h),BG);d=ImageDraw.Draw(im)
    d.text((32,21),title,font=font(32),fill=FG)
    d.text((34,68),subtitle,font=font(18),fill=MUTED)
    return im,d

im,d=base(1800,805,'TYPE E-01  /  01  整机比例审核','真实 Blender 模型渲染  ·  身体与背包为主轮廓阶段')
for i,(key,title) in enumerate((('FRONT','正面'),('LEFT','左侧'),('BACK','背面'),('RIGHT','右侧'))):
    x=24+i*444
    d.text((x+8,108),title,font=font(21),fill=FG)
    fitted(im,RENDERS/(key+'.png'),(x,146,420,605))
d.text((33,766),'本轮确认：头身比例、肩胸宽度、四肢长度与侧面厚度。',font=font(17),fill=MUTED)
im.save(OUT/'STAGE01_FOUR_VIEWS.jpg',quality=94)

source=Image.open(next(Path('D:/project-mecha-design').glob('*.png')))
# Pixel crops only. No generated or retouched content is used in the review.
front_reference=source.crop((30,238,599,1331))
front_model=Image.open(RENDERS/'FRONT.png').crop((180,38,660,1066))
angled_model=Image.open(RENDERS/'3Q.png').crop((178,51,684,1080))
im,d=base(1580,1070,'TYPE E-01  /  01  参考与实模','左：用户设计图局部  ·  中、右：当前 Blender 实模')
for x,w,label,pic in ((24,440,'设计图',front_reference),(482,478,'模型正面',front_model),(976,580,'模型立体视角',angled_model)):
    d.text((x+8,115),label,font=font(20),fill=FG)
    fitted(im,pic,(x,153,w,845))
d.text((32,1025),'阶段 01：先确认外形方向，身体细节、步枪与最终持枪姿态在确认后推进。',font=font(18),fill=MUTED)
im.save(OUT/'STAGE01_REFERENCE_COMPARISON.jpg',quality=94)

im,d=base(1800,805,'TYPE E-01  /  01  头部结构审核','红色独眼、头盔包覆、圆形耳罩与下颌护甲  ·  三个角度来自同一个模型')
for i,(key,title) in enumerate((('HEAD_FRONT','正面'),('HEAD_3Q','四分之三'),('HEAD_SIDE','侧面'))):
    x=24+i*594
    d.text((x+8,111),title,font=font(21),fill=FG)
    fitted(im,RENDERS/(key+'.png'),(x,150,570,580))
d.text((33,764),'头部已做到结构确认程度，尚待用户确认造型。',font=font(17),fill=MUTED)
im.save(OUT/'STAGE01_HEAD_REVIEW.jpg',quality=95)

manifest={'model_sha256':hashlib.sha256((OUT/'TYPE_E01_STAGE01.blend').read_bytes()).hexdigest(),
          'renders':[p.name for p in sorted(RENDERS.glob('*.png'))],
          'review_sheets':[p.name for p in sorted(OUT.glob('STAGE01_*.jpg'))],
          'status':'awaiting_user_approval','image_processing':'layout and resize only; Blender renders unretouched'}
(OUT/'review_manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf8')
print(json.dumps(manifest,indent=2))
