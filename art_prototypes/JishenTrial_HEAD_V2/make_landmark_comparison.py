"""Reference alignment by crown/chin only, without non-uniform warping."""
from PIL import Image,ImageDraw,ImageFont
from pathlib import Path
import json
R=Path(__file__).resolve().parent;P=R/'iterations'/'HEAD_V2_ITER05'
audit=json.loads((P/'geometry_inspection.json').read_text(encoding='utf8'))
font=lambda n:ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',n)
canvas=Image.new('RGB',(1800,1320),(239,240,241));d=ImageDraw.Draw(canvas)
d.text((35,25),'HEAD V2 · 轮廓基准对照',font=font(36),fill=(26,35,41))
d.text((35,80),'仅以 TOP / CHIN 等比缩放对齐；没有拉宽、压扁或重绘模型。参考小图不是工程图。',font=font(22),fill=(62,67,72))
panel_top=145;top=220;chin=930;head_h=chin-top
ref_lm={'TOP':(237,176),'ANTENNA ROOT':(114,261),'FOREHEAD':(237,344),'EYE LINE':(163,482),'CHEEK WIDTH':(49,421),'MASK TIP':(237,605),'CHIN':(237,660)}
mdl_lm=audit['landmarks_pixels']['HEAD_FRONT']
for idx,(file,lm,cx,title) in enumerate([(R/'references'/'REFERENCE_FRONT.png',ref_lm,237,'概念参考 · 正面'),(P/'HEAD_FRONT.png',mdl_lm,720,'Blender 实际灰模 · ITER05')]):
    panel=Image.new('RGB',(900,1120),(230,231,232));im=Image.open(file).convert('RGB')
    k=head_h/(lm['CHIN'][1]-lm['TOP'][1]);sz=(round(im.width*k),round(im.height*k))
    im=im.resize(sz,Image.Resampling.LANCZOS);ox=450-round(cx*k);oy=top-round(lm['TOP'][1]*k)
    panel.paste(im,(ox,oy));pd=ImageDraw.Draw(panel)
    for n,(x,y) in lm.items():
        if n=='REAR HEAD':continue
        xx=ox+x*k;yy=oy+y*k
        pd.ellipse((xx-5,yy-5,xx+5,yy+5),fill=(30,150,197),outline='white',width=1)
        label=n;tx=xx+12;ty=yy-12
        if n in ('TOP','FOREHEAD','MASK TIP','CHIN'):tx=xx+30
        if n=='CHEEK WIDTH':tx=xx+14;ty=yy-37
        pd.text((tx,ty),label,font=font(19),fill=(12,75,104),stroke_width=1,stroke_fill=(234,241,244))
    for yy in (top,chin):pd.line((20,yy,880,yy),fill=(42,142,183),width=2)
    canvas.paste(panel,(idx*900,panel_top));d.text((idx*900+30,113),title,font=font(23),fill=(26,35,41))
d.text((35,1283),'REAR HEAD 在侧面 / 3/4 对照中检查；图上蓝点是检查基准，不是新增的模型零件。',font=font(20),fill=(46,57,64))
canvas.save(R/'HEAD_V2_LANDMARK_COMPARISON.jpg',quality=95)
canvas.resize((1440,1056)).save(R/'HEAD_V2_LANDMARK_COMPARISON_preview.jpg',quality=94)
print('Saved proportional landmark comparison from actual images')
