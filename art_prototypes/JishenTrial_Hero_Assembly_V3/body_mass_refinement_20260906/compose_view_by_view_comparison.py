"""Make a clean one-by-one comparison sheet from existing art and renders only."""
from PIL import Image, ImageDraw, ImageFont
from pathlib import Path
import hashlib, json

W = Path(__file__).resolve().parent
body_ref = W.parent / 'body_reference_rebuild_20260906'
model_ref = W / 'iteration_v3/reference_action_pose/four_views'
out_dir = W / 'iteration_v3/social_showcase_20260906/comparison_only'
out_dir.mkdir(parents=True, exist_ok=True)
out = out_dir / 'VALKYR_TYPE01_VIEW_BY_VIEW_COMPARISON.png'

def sha(p):
    return hashlib.sha256(p.read_bytes()).hexdigest()

# Dedicated original panels are used where available. The right panel is cropped
# from the original design board so the source remains the untouched artwork.
board = Image.open(body_ref/'REFERENCE.png').convert('RGB')
original_paths = {
    'FRONT': body_ref/'REF_FRONT.png',
    'LEFT': body_ref/'REF_LEFT.png',
    'BACK': body_ref/'REF_BACK.png',
}
originals = {
    'FRONT': Image.open(original_paths['FRONT']).convert('RGB'),
    'LEFT': Image.open(original_paths['LEFT']).convert('RGB'),
    'BACK': Image.open(original_paths['BACK']).convert('RGB'),
    'RIGHT': board.crop((824, 76, 1060, 550)),
}
models = {name: model_ref/(name+'.png') for name in ('FRONT','LEFT','BACK','RIGHT')}

WOUT, HOUT = 1800, 2720
canvas = Image.new('RGB', (WOUT, HOUT), 'white')
draw = ImageDraw.Draw(canvas)
font_title = ImageFont.truetype('C:/Windows/Fonts/msyh.ttc', 30)
font_row = ImageFont.truetype('C:/Windows/Fonts/msyhbd.ttc', 30)
font_label = ImageFont.truetype('C:/Windows/Fonts/msyh.ttc', 24)
font_view = ImageFont.truetype('C:/Windows/Fonts/bahnschrift.ttf', 25)

draw.text((84, 34), 'VALKYR TYPE-01  ·  逐视角还原度对比', fill=(32, 42, 49), font=font_title)
draw.text((84, 76), '每组左：原图　右：模型', fill=(104, 114, 120), font=font_label)
draw.line((84, 116, 1716, 116), fill=(205, 211, 214), width=2)

row_h = 630
row_gap = 20
row_y0 = 140
left_x, right_x = 120, 960
box_w, box_h = 720, 550

def fit_bottom(im, w, h, alpha=False, margin=8):
    im = im.convert('RGBA')
    if alpha:
        bbox = im.getchannel('A').getbbox()
        if bbox:
            im = im.crop(bbox)
    avail_w, avail_h = w-2*margin, h-2*margin
    scale = min(avail_w/im.width, avail_h/im.height)
    size = (max(1, round(im.width*scale)), max(1, round(im.height*scale)))
    im = im.resize(size, Image.Resampling.LANCZOS)
    layer = Image.new('RGBA', (w, h), (255,255,255,0))
    layer.alpha_composite(im, ((w-size[0])//2, h-margin-size[1]))
    return layer

for idx, name in enumerate(('FRONT','LEFT','BACK','RIGHT')):
    y = row_y0 + idx*(row_h+row_gap)
    draw.text((84, y+8), name, fill=(48, 60, 67), font=font_view)
    draw.text((left_x+box_w//2-24, y+8), '原图', fill=(105, 115, 121), font=font_label)
    draw.text((right_x+box_w//2-24, y+8), '模型', fill=(105, 115, 121), font=font_label)
    draw.line((left_x+box_w+34, y+66, right_x-34, y+66), fill=(220,224,226), width=2)
    original_layer = fit_bottom(originals[name], box_w, box_h, alpha=False)
    model_layer = fit_bottom(Image.open(models[name]), box_w, box_h, alpha=True)
    canvas.paste(original_layer, (left_x, y+55), original_layer)
    canvas.paste(model_layer, (right_x, y+55), model_layer)
    draw.line((84, y+row_h-2, 1716, y+row_h-2), fill=(226,229,230), width=2)

canvas.save(out, quality=96, optimize=True)
(out_dir/'view_by_view_manifest.json').write_text(json.dumps({
    'output': str(out), 'model_file_modified': False,
    'original_source_sha256': {name: sha(path) for name,path in original_paths.items()},
    'original_board_sha256': sha(body_ref/'REFERENCE.png'),
    'model_view_sha256': {name: sha(path) for name,path in models.items()},
}, indent=2, ensure_ascii=False), encoding='utf-8')
print(out)
