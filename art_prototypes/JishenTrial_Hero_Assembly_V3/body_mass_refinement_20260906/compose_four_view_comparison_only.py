"""Compose existing original and model four views; does not open or modify a blend file."""
from PIL import Image, ImageDraw, ImageFont
from pathlib import Path
import hashlib, json

W = Path(__file__).resolve().parent
body_ref = W.parent / 'body_reference_rebuild_20260906'
model_ref = W / 'iteration_v3/reference_action_pose/four_views'
out_dir = W / 'iteration_v3/social_showcase_20260906/comparison_only'
out_dir.mkdir(parents=True, exist_ok=True)
out = out_dir / 'VALKYR_TYPE01_FOUR_VIEW_COMPARISON.png'

def sha(p):
    return hashlib.sha256(p.read_bytes()).hexdigest()

# The original design board's top strip contains the four orthographic panels.
board = Image.open(body_ref/'REFERENCE.png').convert('RGB')
original_crops = [
    ('FRONT', (10, 76, 307, 550)),
    ('LEFT',  (311, 76, 531, 550)),
    ('BACK',  (534, 76, 773, 550)),
    ('RIGHT', (777, 76, 1063, 550)),
]
originals = [(name, board.crop(box)) for name, box in original_crops]
model_paths = [(name, model_ref/(name+'.png')) for name, _ in original_crops]

canvas = Image.new('RGB', (2400, 1900), (9, 16, 27))
draw = ImageDraw.Draw(canvas)
font_title = ImageFont.truetype('C:/Windows/Fonts/segoeuib.ttf', 60)
font_row = ImageFont.truetype('C:/Windows/Fonts/msyhbd.ttc', 38)
font_view = ImageFont.truetype('C:/Windows/Fonts/bahnschrift.ttf', 26)
font_small = ImageFont.truetype('C:/Windows/Fonts/msyh.ttc', 22)

draw.rectangle((0, 0, 2400, 142), fill=(18, 31, 47))
draw.rectangle((80, 84, 240, 90), fill=(43, 220, 255))
draw.text((80, 25), 'VALKYR TYPE-01  ·  FOUR VIEW COMPARISON', fill=(239,245,250), font=font_title)
draw.text((1870, 45), 'SOURCE / MODEL', fill=(151,185,205), font=font_small)

left_margin, gap, card_w, card_h = 80, 24, 548, 710
xs = [left_margin + i*(card_w+gap) for i in range(4)]
top_y, bottom_y = 212, 1050
for y, label, color in [(top_y, '原始设计', (207, 231, 241)), (bottom_y, '当前模型', (207, 231, 241))]:
    draw.text((80, y-54), label, fill=color, font=font_row)
    draw.line((260, y-31, 2320, y-31), fill=(43, 86, 111), width=2)
    for x in xs:
        draw.rounded_rectangle((x, y, x+card_w, y+card_h), radius=14,
                               fill=(18, 29, 43), outline=(56, 92, 115), width=3)

def contain(im, box, alpha=False, margin=22):
    x0,y0,x1,y1 = box
    im = im.convert('RGBA')
    if alpha:
        bbox = im.getchannel('A').getbbox()
        if bbox: im = im.crop(bbox)
    avail_w, avail_h = x1-x0-2*margin, y1-y0-2*margin
    scale = min(avail_w/im.width, avail_h/im.height)
    size = (max(1, round(im.width*scale)), max(1, round(im.height*scale)))
    im = im.resize(size, Image.Resampling.LANCZOS)
    layer = Image.new('RGBA', (x1-x0, y1-y0), (0,0,0,0))
    layer.alpha_composite(im, ((layer.width-size[0])//2, (layer.height-size[1])//2))
    return layer

for i, ((name, orig), (_, model_path)) in enumerate(zip(originals, model_paths)):
    x = xs[i]
    draw.text((x+22, top_y+16), name, fill=(147, 191, 211), font=font_view)
    # Leave the original board panel itself intact inside its card.
    original_layer = contain(orig, (x+12, top_y+54, x+card_w-12, top_y+card_h-12), alpha=False, margin=8)
    canvas.paste(original_layer, (x+12, top_y+54), original_layer)
    draw.text((x+22, bottom_y+16), name, fill=(147, 191, 211), font=font_view)
    model_layer = contain(Image.open(model_path), (x+12, bottom_y+54, x+card_w-12, bottom_y+card_h-12), alpha=True, margin=8)
    canvas.paste(model_layer, (x+12, bottom_y+54), model_layer)

draw.line((80, 1810, 2320, 1810), fill=(43,220,255), width=3)
draw.text((80, 1824), '原设与现有模型渲染并排 · 仅排版对比，未修改模型文件', fill=(132, 166, 184), font=font_small)
canvas.save(out, quality=96, optimize=True)
(out_dir/'four_view_comparison_manifest.json').write_text(json.dumps({
    'output': str(out),
    'original_board': str(body_ref/'REFERENCE.png'),
    'model_views': [str(p) for _, p in model_paths],
    'original_board_sha256': sha(body_ref/'REFERENCE.png'),
    'model_view_sha256': {name: sha(path) for name, path in model_paths},
    'model_file_modified': False,
}, indent=2, ensure_ascii=False), encoding='utf-8')
print(out)
