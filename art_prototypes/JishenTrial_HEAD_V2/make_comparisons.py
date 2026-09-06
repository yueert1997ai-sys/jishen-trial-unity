"""Lay out original concept crops next to actual Blender render files.
No generated/redrawn head imagery. Side render is mirrored only to align viewing
direction with the concept; raw render remains unchanged. Reference is an artistic
concept, not a dimensioned engineering drawing, so no false pixel-match score.
"""
from PIL import Image, ImageOps, ImageDraw, ImageFont
from pathlib import Path
import json, math
ROOT=Path(__file__).resolve().parent
FONT='C:/Windows/Fonts/msyh.ttc'
def font(n):return ImageFont.truetype(FONT,n)
def fit(im,box):
    x,y,w,h=box;im=im.copy().convert('RGB');im.thumbnail((w,h),Image.Resampling.LANCZOS)
    return im,(x+(w-im.width)//2,y+(h-im.height)//2)
def put(canvas,im,box):
    im,at=fit(im,box);canvas.paste(im,at)
def model_crop(path):
    im=Image.open(path).convert('RGB')
    # Fixed framing margins for all actual renders, not shape manipulation.
    return im.crop((260,90,1175,1235))
def txt(d,at,text,size=24,color=(39,44,48)):d.text(at,text,font=font(size),fill=color)
labels=['TOP','ANTENNA ROOT','FOREHEAD','EYE LINE','CHEEK WIDTH','MASK TIP','CHIN','REAR HEAD']
for it in (1,2,3,4,5):
    p=ROOT/'iterations'/f'HEAD_V2_ITER{it:02d}'
    if not (p/'HEAD_3Q.png').exists():continue
    board=Image.new('RGB',(2320,1600),(237,238,239));d=ImageDraw.Draw(board)
    txt(d,(40,25),f'HEAD V2 / ITER {it:02d}   一级造型 · 实际模型对照',38)
    txt(d,(40,82),'概念图仅作形体参考；蓝光、颜色和微小零件不参与本轮验收。',22)
    cols=[(40,'参考 · HEAD DETAIL',ROOT/'references'/'REFERENCE_HEAD_DETAIL.png'),(620,'Blender 实际灰模 · 3/4',p/'HEAD_3Q.png'),(1200,'参考 · 正面小图',ROOT/'references'/'REFERENCE_FRONT.png'),(1780,'Blender 实际灰模 · 正面',p/'HEAD_FRONT.png')]
    for x,title,file in cols:
        txt(d,(x,140),title,24);put(board,model_crop(file) if file.name.startswith('HEAD_') else Image.open(file),(x,188,540,735))
    txt(d,(40,972),'侧面比较：仅将模型图水平翻转，使朝向与参考一致；原始 HEAD_SIDE.png 未修改。',22)
    put(board,Image.open(ROOT/'references'/'REFERENCE_SIDE.png'),(40,1018,480,430))
    put(board,ImageOps.mirror(model_crop(p/'HEAD_SIDE.png')),(580,1018,590,430))
    txt(d,(1250,1030),'逐项轮廓检查',27)
    for i,s in enumerate(labels):txt(d,(1250+(i//4)*425,1090+(i%4)*55),f'{i+1:02d}  {s}',24)
    txt(d,(1250,1350),'固定正交相机 / 灰模 / 无贴图 / 无发光',23)
    txt(d,(1250,1400),'一级造型审查中，未进入二级和三级细节。',23)
    txt(d,(40,1525),'图片来自 references/ 与本轮 Blender 渲染；未用 AI 生图代替模型。',21)
    board.save(p/'REFERENCE_COMPARISON.jpg',quality=94)
    prev=board.copy();prev.thumbnail((1740,1200));prev.save(p/'REFERENCE_COMPARISON_preview.jpg',quality=92)
    # Three clean original render thumbnails together for direct visual review.
    strip=Image.new('RGB',(2160,805),(237,238,239));sd=ImageDraw.Draw(strip)
    for x,name,label in [(0,'HEAD_FRONT','正面'),(720,'HEAD_SIDE','侧面'),(1440,'HEAD_3Q','3/4')]:
        txt(sd,(x+25,20),f'ITER {it:02d} · {label}',26)
        put(strip,Image.open(p/(name+'.png')),(x,70,720,720))
    strip.save(p/'THREE_VIEWS.jpg',quality=95)
    print('Laid out actual reference comparison',it)
