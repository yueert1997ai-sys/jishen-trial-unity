from PIL import Image,ImageOps,ImageDraw,ImageFont
from pathlib import Path
import json,math
P=Path(__file__).resolve().parent;W=P/'refinement';R=W/'renders'/'phase_07'
font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',23)
small=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',17)
def sheet(name,shots,cols,w=440,h=480):
 rows=math.ceil(len(shots)/cols);im=Image.new('RGB',(cols*w,rows*h),(224,227,232));d=ImageDraw.Draw(im)
 for i,(label,path) in enumerate(shots):
  src=Image.open(path).convert('RGB');src=ImageOps.contain(src,(w-14,h-42));x=(i%cols)*w;y=(i//cols)*h
  d.text((x+9,y+6),label,font=small,fill=(32,39,48));im.paste(src,(x+(w-src.width)//2,y+35+(h-40-src.height)//2))
 im.save(W/'renders'/name,quality=93)
sheet('WHOLE_BODY_REVIEW.jpg',[(n,R/(n+'.png')) for n in ['FRONT','LEFT','BACK','RIGHT','FRONT_3Q','CLAY_3Q']],3,530,690)
sheet('CLOSEUP_REVIEW.jpg',[(n,R/(n+'.png')) for n in ['HEAD_FRONT','HEAD_SIDE','HEAD_3Q','HEAD_ON_BODY','CHEST_FRONT','CHEST_3Q','WAIST_3Q','LEG_FRONT','LEG_SIDE','FOOT_CLOSEUP','ARM_CLOSEUP','HAND_CLOSEUP']],4,440,480)
sheet('PROPULSION_WEAPON_REVIEW.jpg',[(n,R/(n+'.png')) for n in ['BACKPACK_CLOSEUP','SHOULDER_CANNON_CLOSEUP','WEAPON_BEAM_ON','WEAPON_BEAM_OFF']],4,440,590)
sheet('BEFORE_AFTER.jpg',[('原整机 / BEFORE',P/'renders'/'FRONT_3Q.png'),('本轮精修 / AFTER',R/'FRONT_3Q.png')],2,650,850)
demo=W/'renders'/'demo'
if (demo/'FBX_ARTICULATION_CHECK.png').exists():sheet('FBX_DEMO_REVIEW.jpg',[(n,demo/(n+'.png')) for n in ['FBX_3Q','FBX_BEAM_OFF','FBX_BACK','FBX_ARTICULATION_CHECK']],4,400,550)
print('ACTUAL_RENDER_REVIEW_SHEETS_CREATED')
