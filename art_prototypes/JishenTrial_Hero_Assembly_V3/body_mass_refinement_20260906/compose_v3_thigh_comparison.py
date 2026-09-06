from PIL import Image,ImageDraw,ImageFont
import pathlib
W=pathlib.Path(__file__).resolve().parent;D=W/'iteration_v3';F='C:/Windows/Fonts/msyh.ttc'
sheet=Image.new('RGB',(3064,1640),(18,23,32));draw=ImageDraw.Draw(sheet)
font=ImageFont.truetype(F,40);small=ImageFont.truetype(F,27)
for i,(path,label,subtitle) in enumerate([
 (W/'delivery/renders/KNEES.png','上一版','白色鼓形外壳 · 横向分段前甲'),
 (D/'renders/KNEES.png','第三版','长向蓝色主甲 · 窄侧甲 · 深侧壳')]):
 x=24+i*1516
 draw.text((x+12,20),label,font=font,fill=(231,240,249))
 draw.text((x+12,77),subtitle,font=small,fill=(155,181,211))
 im=Image.open(path).convert('RGBA');assert im.size==(1500,1500)
 sheet.paste(im,(x,128),im)
sheet.save(D/'THIGH_BEFORE_AFTER.png')
print(D/'THIGH_BEFORE_AFTER.png')
