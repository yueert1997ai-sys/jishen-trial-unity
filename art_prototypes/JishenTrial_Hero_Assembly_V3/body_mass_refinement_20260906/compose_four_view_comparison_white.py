"""Minimal white-background four-view comparison using existing images only."""
from PIL import Image, ImageDraw, ImageFont
from pathlib import Path
import hashlib, json

W = Path(__file__).resolve().parent
body_ref = W.parent / 'body_reference_rebuild_20260906'
model_ref = W / 'iteration_v3/reference_action_pose/four_views'
out_dir = W / 'iteration_v3/social_showcase_20260906/comparison_only'
out_dir.mkdir(parents=True, exist_ok=True)
out = out_dir / 'VALKYR_TYPE01_FOUR_VIEW_COMPARISON_WHITE.png'

def sha(p):
    return hashlib.sha256(p.read_bytes()).hexdigest()

board = Image.open(body_ref/'REFERENCE.png').convert('RGB')
originals = [
    ('FRONT', board.crop((10, 76, 307, 550))),
    ('LEFT',  board.crop((311, 76, 531, 550))),
    ('BACK',  board.crop((534, 76, 773, 550))),
    ('RIGHT', board.crop((777, 76, 1063, 550))),
]
models = [(name, model_ref/(name+'.png')) for name, _ in originals]

canvas = Image.new('RGB', (2400, 1780), (255, 255, 255))
draw = ImageDraw.Draw(canvas)
font_row = ImageFont.truetype('C:/Windows/Fonts/msyhbd.ttc', 32)
font_view = ImageFont.truetype('C:/Windows/Fonts/bahnschrift.ttf', 25)
font_note = ImageFont.truetype('C:/Windows/Fonts/msyh.ttc', 22)
font_title = ImageFont.truetype('C:/Windows/Fonts/msyh.ttc', 28)

draw.text((80, 34), 'VALKYR TYPE-01  ·  四视图还原度对比', fill=(35, 45, 53), font=font_title)
draw.text((80, 90), '原图', fill=(35, 45, 53), font=font_row)
draw.text((80, 890), '模型', fill=(35, 45, 53), font=font_row)
draw.line((150, 110, 2320, 110), fill=(205, 211, 214), width=2)
draw.line((150, 910, 2320, 910), fill=(205, 211, 214), width=2)

left, gap, cell_w, cell_h = 150, 24, 520, 700
xs = [left+i*(cell_w+gap) for i in range(4)]

def contain(im, w, h, alpha=False, margin=8):
    im = im.convert('RGBA')
    if alpha:
        bbox = im.getchannel('A').getbbox()
        if bbox: im = im.crop(bbox)
    avail_w, avail_h = w-2*margin, h-2*margin
    scale = min(avail_w/im.width, avail_h/im.height)
    size = (max(1, round(im.width*scale)), max(1, round(im.height*scale)))
    im = im.resize(size, Image.Resampling.LANCZOS)
    layer = Image.new('RGBA', (w, h), (255,255,255,0))
    layer.alpha_composite(im, ((w-size[0])//2, (h-size[1])//2))
    return layer

for i, ((name, original), (_, model_path)) in enumerate(zip(originals, models)):
    x = xs[i]
    draw.text((x+8, 122), name, fill=(100, 110, 116), font=font_view)
    original_layer = contain(original, cell_w, 650, alpha=False)
    canvas.paste(original_layer, (x, 150), original_layer)
    draw.text((x+8, 922), name, fill=(100, 110, 116), font=font_view)
    model_layer = contain(Image.open(model_path), cell_w, 650, alpha=True)
    canvas.paste(model_layer, (x, 950), model_layer)

draw.line((80, 1690, 2320, 1690), fill=(205, 211, 214), width=2)
draw.text((80, 1710), '同一视角顺序：FRONT  ·  LEFT  ·  BACK  ·  RIGHT', fill=(100, 110, 116), font=font_note)
draw.text((1740, 1710), '仅排版对比，未修改模型文件', fill=(100, 110, 116), font=font_note)
canvas.save(out, quality=96, optimize=True)
(out_dir/'four_view_comparison_white_manifest.json').write_text(json.dumps({
    'output': str(out), 'model_file_modified': False,
    'original_board_sha256': sha(body_ref/'REFERENCE.png'),
    'model_view_sha256': {name: sha(path) for name, path in models},
}, indent=2, ensure_ascii=False), encoding='utf-8')
print(out)
