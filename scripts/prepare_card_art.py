"""Export original generated illustrations into the card engine's image dimensions."""
import hashlib,json
from pathlib import Path
from PIL import Image,ImageOps,ImageDraw
ROOT=Path(__file__).resolve().parents[1]
sheet=ROOT/'sheets/card_art.json'
rows=json.loads(sheet.read_text(encoding='utf-8-sig'))
missing=[]
for row in rows:
    source=ROOT/'art/originals'/(row['id']+'.png')
    if not source.exists():missing.append(row['id']);continue
    target=ROOT/'src/main/resources'/row['path']
    target.parent.mkdir(parents=True,exist_ok=True)
    with Image.open(source) as image:
        ImageOps.fit(image.convert('RGB'),(row['width'],row['height']),Image.Resampling.LANCZOS).save(target)
    row['sha256']=hashlib.sha256(target.read_bytes()).hexdigest()
sheet.write_text(json.dumps(rows,indent=2)+'\n')
if missing:raise SystemExit('Missing generated illustrations: '+', '.join(missing))
# Review exported art at a size close to the illustrations in a game hand.
preview=Image.new('RGB',(1000,1150),(18,25,35));draw=ImageDraw.Draw(preview)
for i,row in enumerate(rows):
    x=(i%5)*200+12;y=(i//5)*190+12
    with Image.open(ROOT/'src/main/resources'/row['path']) as image:
        preview.paste(image.resize((176,134),Image.Resampling.LANCZOS),(x,y))
    draw.text((x,y+140),row['name'],fill=(240,220,170))
preview.save(ROOT/'media/card-art-review.png')
print('Exported and hashed',len(rows),'unique card illustrations.')
