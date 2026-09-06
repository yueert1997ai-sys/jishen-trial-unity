"""Compose a comparison image from existing source art and an existing render only."""
from PIL import Image, ImageDraw, ImageFont
from pathlib import Path
import hashlib, json

W = Path(__file__).resolve().parent
out_dir = W / 'iteration_v3/social_showcase_20260906/comparison_only'
out_dir.mkdir(parents=True, exist_ok=True)
original = W.parent / 'body_reference_rebuild_20260906/REF_FRONT.png'
model = W / 'iteration_v3/reference_action_pose/four_views/FRONT.png'
out = out_dir / 'VALKYR_TYPE01_ORIGINAL_VS_MODEL.png'

def sha(p):
    return hashlib.sha256(p.read_bytes()).hexdigest()

source_hashes = {'original': sha(original), 'model_render': sha(model)}
canvas = Image.new('RGB', (2000, 1400), (10, 17, 28))
draw = ImageDraw.Draw(canvas)
font_bold = ImageFont.truetype('C:/Windows/Fonts/msyhbd.ttc', 42)
font_title = ImageFont.truetype('C:/Windows/Fonts/segoeuib.ttf', 56)
font_small = ImageFont.truetype('C:/Windows/Fonts/msyh.ttc', 24)

draw.rectangle((0, 0, 2000, 140), fill=(18, 31, 47))
draw.rectangle((72, 72, 184, 78), fill=(43, 220, 255))
draw.text((72, 22), 'VALKYR TYPE-01', fill=(238, 245, 250), font=font_title)
draw.text((1390, 35), 'ORIGINAL  /  BLENDER MODEL', fill=(154, 186, 207), font=font_small)

cards = [(72, 170, 956, 1330), (1044, 170, 1928, 1330)]
for box in cards:
    draw.rounded_rectangle(box, radius=16, fill=(19, 31, 45), outline=(61, 96, 119), width=3)
    draw.line((box[0]+24, box[1]+76, box[2]-24, box[1]+76), fill=(40, 79, 102), width=2)

draw.text((112, 195), '原始设计 · FRONT', fill=(239, 245, 248), font=font_bold)
draw.text((1085, 195), 'Blender 模型 · 当前正面', fill=(239, 245, 248), font=font_bold)

orig = Image.open(original).convert('RGB')
orig.thumbnail((800, 1020), Image.Resampling.LANCZOS)
orig_layer = Image.new('RGB', (840, 1075), (14, 24, 36))
orig_layer.paste(orig, ((840-orig.width)//2, (1075-orig.height)//2))
canvas.paste(orig_layer, (94, 245))

model_im = Image.open(model).convert('RGBA')
bbox = model_im.getchannel('A').getbbox()
if bbox:
    model_im = model_im.crop(bbox)
avail_w, avail_h = 836, 1075
scale = min(avail_w/model_im.width, avail_h/model_im.height)
size = (round(model_im.width*scale), round(model_im.height*scale))
model_im = model_im.resize(size, Image.Resampling.LANCZOS)
model_layer = Image.new('RGBA', (avail_w, avail_h), (0,0,0,0))
model_layer.alpha_composite(model_im, ((avail_w-size[0])//2, (avail_h-size[1])//2))
canvas.paste(model_layer, (1068, 245), model_layer)

draw = ImageDraw.Draw(canvas)
draw.text((112, 1300), '设计原图', fill=(130, 170, 192), font=font_small)
draw.text((1085, 1300), '当前模型渲染（未改动模型文件）', fill=(130, 170, 192), font=font_small)
draw.line((72, 1365, 1928, 1365), fill=(43, 220, 255), width=3)
draw.text((72, 1372), 'MODEL COMPARISON  ·  SOURCE ART + EXISTING RENDER', fill=(116, 151, 170), font=font_small)

canvas.save(out, quality=96, optimize=True)
(out_dir/'comparison_manifest.json').write_text(json.dumps({
    'output': str(out), 'original': str(original), 'model_render': str(model),
    'source_sha256': source_hashes, 'model_file_modified': False,
}, indent=2, ensure_ascii=False), encoding='utf-8')
print(out)
