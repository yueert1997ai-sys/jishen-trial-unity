from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
import shutil
W=Path(__file__).resolve().parent;D=W/'delivery';R=D/'renders'
fontpath='C:/Windows/Fonts/msyh.ttc'
def font(n):return ImageFont.truetype(fontpath,n)
def canvas(w,h,title,subtitle):
 im=Image.new('RGB',(w,h),(12,20,29));d=ImageDraw.Draw(im);d.text((40,24),title,font=font(36),fill=(226,238,249));d.text((42,82),subtitle,font=font(22),fill=(145,176,198));return im,d
def panel(out,d,box,path,title,fit_crop=True,height_cap=None):
 x,y,w,h=box;d.rounded_rectangle((x,y,x+w,y+h),8,fill=(21,32,44),outline=(50,72,92),width=2);d.text((x+18,y+13),title,font=font(24),fill=(219,233,245))
 im=Image.open(path).convert('RGBA')
 if fit_crop:
  bounds=im.getchannel('A').getbbox()
  if bounds:im=im.crop(bounds)
 scale=min((w-28)/im.width,(h-65)/im.height,(height_cap or h)/im.height);im=im.resize((round(im.width*scale),round(im.height*scale)),Image.Resampling.LANCZOS)
 out.paste(im,(x+(w-im.width)//2,y+h-14-im.height),im)

out,d=canvas(2550,1030,'VALKYR TYPE-01 / 当前整机检查','正交四视图 + 前 3/4 · 完成身体和侧面体积修正后的实际 Blender 渲染')
for i,(name,title) in enumerate((('FRONT','正面'),('LEFT','左侧'),('BACK','背面'),('RIGHT','右侧'),('MASTER_REOPENED_3Q','前 3/4 · 主文件重新打开'))):panel(out,d,(18+i*508,133,498,820),R/(name+'.png'),title,height_cap=740)
d.text((40,977),'已认可头部保留 · 胸肩、腰胯、四肢、脚部、背包、肩炮与持剑均已装配',font=font(22),fill=(177,197,211));out.save(D/'VALKYR_FULL_BODY_REVIEW.jpg',quality=95)
out,d=canvas(2400,1370,'VALKYR TYPE-01 / 局部完成度检查','装甲分层、机械连接与脚部结构 · 实际主文件渲染')
for i,(name,title) in enumerate((('CHEST','胸肩与颈部'),('WAIST','腰部与骨盆'),('ARM','手臂叠甲'),('HAND','机械手与握柄'),('LEGS','腿部轮廓'),('FOOT','脚踝、前掌和脚跟'),('BACKPACK','背包与推进模组'),('CANNON','肩炮与支架'))):panel(out,d,(16+i%4*596,133+i//4*605,584,592),R/(name+'.png'),title)
out.save(D/'VALKYR_DETAIL_REVIEW.jpg',quality=95)
out,d=canvas(1800,1110,'VALKYR TYPE-01 / 武器检查','独立实体刀身 + 可开关蓝色光刃 · 右手实际握持')
panel(out,d,(20,132,1760,458),R/'WEAPON_BEAM_ON.png','光刃开启');panel(out,d,(20,606,1760,458),R/'WEAPON_BEAM_OFF.png','光刃关闭 · 实体刀身保留');out.save(D/'VALKYR_WEAPON_REVIEW.jpg',quality=95)
exec((W/'side_comparison.py').read_text(encoding='utf8'),{'__file__':str(W/'side_comparison.py')})
shutil.copy2(W/'SIDE_DEPTH_COMPARISON.jpg',D/'SIDE_DEPTH_COMPARISON.jpg')
print('REVIEW_SHEETS_READY')
