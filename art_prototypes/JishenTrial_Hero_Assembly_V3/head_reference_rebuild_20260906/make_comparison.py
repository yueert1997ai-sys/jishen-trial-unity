from PIL import Image,ImageDraw,ImageFont
import pathlib,sys
W=pathlib.Path(__file__).resolve().parent
I=int(sys.argv[1]); D=W/f'iteration_{I:02d}';R=D/'renders'
font=ImageFont.truetype('C:/Windows/Fonts/msyh.ttc',28)
ref=Image.open(W/'REFERENCE.png').convert('RGB')
bg=(31,40,49)
def fit(im,w,h):
 im=im.convert('RGBA');scale=min(w/im.width,h/im.height);im=im.resize((round(im.width*scale),round(im.height*scale)),Image.Resampling.LANCZOS)
 out=Image.new('RGBA',(w,h),(*bg,255));out.alpha_composite(im,((w-im.width)//2,(h-im.height)//2));return out.convert('RGB')
sheet=Image.new('RGB',(1500,1160),bg);draw=ImageDraw.Draw(sheet)
for k,(name,box) in enumerate([('FRONT',(43,96,315,425)),('LEFT',(348,99,610,425)),('BACK',(645,99,894,425))]):
 draw.text((k*500+20,12),'参考 / '+name,font=font,fill='white');sheet.paste(fit(ref.crop(box),480,475),(k*500+10,60))
 p=R/(name+'.png')
 if p.exists():
  im=Image.open(p);bbox=im.getbbox();im=im.crop(bbox) if bbox else im
  draw.text((k*500+20,565),'Blender 实模 / 第 %02d 轮'%I,font=font,fill=(100,214,255));sheet.paste(fit(im,480,510),(k*500+10,620))
sheet.save(D/'REFERENCE_COMPARISON.jpg',quality=95)
for name in ('FRONT','LEFT','THREE_QUARTER','BACK','TOP','UNDER'):
 if (R/(name+'.png')).exists():
  im=Image.open(R/(name+'.png'));out=Image.new('RGBA',im.size,(*bg,255));out.alpha_composite(im);out.convert('RGB').save(R/(name+'_VIEW.jpg'),quality=95)
