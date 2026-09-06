"""Only crops supplied references for inspection; never synthesizes model imagery."""
from PIL import Image
from pathlib import Path
p=Path(__file__).resolve().parent/'references'
a=Image.open(p/'concept_multiview.png')
a.crop((28,690,278,923)).resize((1000,932)).save(p/'REFERENCE_HEAD_DETAIL.png')
a.crop((281,684,554,805)).resize((1365,605)).save(p/'REFERENCE_ORTHO_HEADS.png')
a.crop((281,684,370,805)).resize((534,726)).save(p/'REFERENCE_FRONT.png')
a.crop((373,684,468,805)).resize((570,726)).save(p/'REFERENCE_SIDE.png')
print('Prepared crops from user concept, original unchanged')
