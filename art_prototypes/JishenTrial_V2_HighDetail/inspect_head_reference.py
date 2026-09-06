"""Inspection crops only. Original concept references are unchanged."""
from pathlib import Path
from PIL import Image
p=Path(__file__).resolve().parent
q=p/'references'/'head_inspection';q.mkdir(exist_ok=True)
with Image.open(p/'references'/'concept_multiview.png') as im:
    im.crop((28,700,306,925)).resize((1112,900)).save(q/'REFERENCE_HEAD_DETAIL_1.png')
    im.crop((312,689,551,803)).resize((1195,570)).save(q/'REFERENCE_HEAD_SMALL_VIEWS.png')
with Image.open(p/'references'/'concept_perspective.png') as im:
    im.crop((849,19,1105,200)).resize((1280,905)).save(q/'REFERENCE_HEAD_DETAIL_2.png')
print('Cropped original references for inspection, no generated images')
