"""Lossless PNG inspection copies of the actual Blender renders, downsampled for display.
Original full-resolution renders are never modified.
"""
from PIL import Image,ImageDraw
from pathlib import Path
ROOT=Path(__file__).resolve().parent
dest=ROOT/'previews';dest.mkdir(exist_ok=True)
files=sorted((ROOT/'renders').glob('*.png'))
for f in files:
    with Image.open(f) as im:
        im.thumbnail((1100,1400),Image.Resampling.LANCZOS);im.save(dest/f.name)
if files:
    width=1500;cellw=375;cellh=480;rows=(len(files)+3)//4
    board=Image.new('RGB',(width,rows*cellh),(230,232,235));draw=ImageDraw.Draw(board)
    for i,f in enumerate(files):
        with Image.open(f) as im:
            im.thumbnail((365,440),Image.Resampling.LANCZOS);x=(i%4)*cellw+(cellw-im.width)//2;y=(i//4)*cellh
            board.paste(im,(x,y));draw.text(((i%4)*cellw+8,y+448),f.stem,fill=(20,24,30))
    board.save(dest/'ALL_RENDER_CONTACT_SHEET.jpg',quality=93)
print('Verified and made display copies for',len(files),'actual renders')
