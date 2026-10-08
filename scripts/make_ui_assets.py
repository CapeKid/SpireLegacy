from pathlib import Path
from PIL import Image,ImageDraw
root=Path(__file__).resolve().parents[1]/'src/main/resources/heir'
for size in (512,1024):
    img=Image.new('RGBA',(size,size),(35,48,67,255));d=ImageDraw.Draw(img);d.rounded_rectangle((size*.1,size*.12,size*.9,size*.88),radius=size*.08,fill=(36,48,66),outline=(130,103,57),width=4);img.save(root/f'bg{size}.png')
for size in (128,32):
    img=Image.new('RGBA',(size,size));d=ImageDraw.Draw(img);d.ellipse((2,2,size-2,size-2),fill=(38,54,76),outline=(227,185,95),width=max(2,size//16));img.save(root/f'orb{size}.png')
print('Generated original interface backgrounds and energy orb.')
