"""Review boards containing only source image crops and actual Blender renders."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont,ImageOps
import json,hashlib
ROOT=Path(__file__).resolve().parent;OUT=ROOT/'stage_02';R=OUT/'renders'
BG=(22,28,34);FG=(235,237,238);SUB=(159,178,190)
def font(n):return ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',n)
def fit(canvas,image,rect):
    image=Image.open(image).convert('RGB') if isinstance(image,(str,Path)) else image.convert('RGB')
    x,y,w,h=rect;image=ImageOps.contain(image,(w,h),Image.Resampling.LANCZOS)
    canvas.paste(image,(x+(w-image.width)//2,y+(h-image.height)//2))
def sheet(w,h,title,subtitle):
    im=Image.new('RGB',(w,h),BG);d=ImageDraw.Draw(im)
    d.text((28,20),title,font=font(31),fill=FG);d.text((30,66),subtitle,font=font(18),fill=SUB)
    return im,d
src=Image.open(ROOT/'reference'/'head_detail_inspection.png').crop((13,9,250,265))
old=Image.open(ROOT/'stage_01'/'renders'/'HEAD_FRONT.png').crop((135,155,765,835))
new=Image.open(R/'HEAD_FRONT.png').crop((155,139,845,930))
three=Image.open(R/'HEAD_3Q.png').crop((135,139,925,940))
im,d=sheet(1800,720,'TYPE E-01  /  02  头部修正对照','收窄面罩与下颌  ·  独眼内收  ·  额甲形成连续折面与下压的眉缘')
for i,(name,pic) in enumerate((('设计图',src),('上一版',old),('当前正面',new),('当前立体',three))):
    x=24+i*444;d.text((x+9,113),name,font=font(21),fill=FG);fit(im,pic,(x,154,420,510))
d.text((30,680),'参考图之外均为真实 Blender 渲染；当前头部等待造型确认。',font=font(17),fill=SUB)
im.save(OUT/'STAGE02_HEAD_COMPARISON.jpg',quality=95)

im,d=sheet(1800,780,'TYPE E-01  /  02  头部多角度','同一份模型的正面、侧面和立体视角；隐藏肩部以便检查完整下颌。')
for i,(name,key) in enumerate((('正面','HEAD_FRONT'),('立体','HEAD_3Q'),('侧面','HEAD_SIDE'))):
    x=24+i*594;d.text((x+8,114),name,font=font(21),fill=FG);fit(im,R/(key+'.png'),(x,153,570,580))
im.save(OUT/'STAGE02_HEAD_REVIEW.jpg',quality=94)

im,d=sheet(1500,1150,'TYPE E-01  /  02  整机与装备细化','装甲分层、肘膝连接、腿后活塞、分节手指与独立步枪')
for x,name,key in ((24,'整机立体','3Q'),(760,'背面结构','BACK')):
    d.text((x+8,110),name,font=font(21),fill=FG);fit(im,R/(key+'.png'),(x,150,710,940))
d.text((30,1109),'当前为右手垂持检查姿势；最终双手持枪动作将在本节点确认后制作。',font=font(18),fill=SUB)
im.save(OUT/'STAGE02_ASSEMBLY_REVIEW.jpg',quality=94)

im,d=sheet(1800,835,'TYPE E-01  /  02  整机四视图','保持已确认的整体比例，推进头部修正与全身结构细化。')
for i,(name,key) in enumerate((('正面','FRONT'),('左侧','LEFT'),('背面','BACK'),('右侧','RIGHT'))):
    x=24+i*444;d.text((x+8,112),name,font=font(21),fill=FG);fit(im,R/(key+'.png'),(x,151,420,630))
im.save(OUT/'STAGE02_FOUR_VIEWS.jpg',quality=94)

im,d=sheet(1800,1160,'TYPE E-01  /  02  步枪与机械结构','实体步枪：枪托、提把、扳机护圈、弹匣、枪管与中空枪口；背包保留独立零件。')
for x,key,title in ((24,'RIFLE_SIDE','步枪侧面'),(920,'RIFLE_3Q','步枪立体')):
    d.text((x+8,115),title,font=font(21),fill=FG);fit(im,R/(key+'.png'),(x,153,854,367))
for x,key,title in ((24,'BODY_DETAIL','装甲与关节'),(920,'BACKPACK_DETAIL','背包与双喷口')):
    d.text((x+8,551),title,font=font(21),fill=FG);fit(im,R/(key+'.png'),(x,590,854,515))
im.save(OUT/'STAGE02_EQUIPMENT_REVIEW.jpg',quality=94)
manifest={'model_sha256':hashlib.sha256((OUT/'TYPE_E01_STAGE02.blend').read_bytes()).hexdigest(),
          'raw_renders':[p.name for p in sorted(R.glob('*.png'))],
          'review_boards':[p.name for p in sorted(OUT.glob('STAGE02_*.jpg'))],
          'status':'awaiting user approval at stage 02',
          'image_processing':'layout, resize and crop only; no retouching of rendered geometry'}
(OUT/'review_manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf8')
print(json.dumps(manifest,indent=2))
