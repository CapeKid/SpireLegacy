import json,hashlib
from pathlib import Path
from PIL import Image,ImageOps
ROOT=Path(__file__).resolve().parents[1]
sheet=ROOT/'sheets/ui_art.json'
rows=json.loads(sheet.read_text())
for row in rows:
    with Image.open(ROOT/'art/originals'/(row['id']+'.png')) as image:
        image=ImageOps.fit(image.convert('RGBA'),(row['width'],row['height']),Image.Resampling.LANCZOS)
        path=ROOT/'src/main/resources'/row['path'];path.parent.mkdir(parents=True,exist_ok=True);image.save(path)
        row['sha256']=hashlib.sha256(path.read_bytes()).hexdigest()
sheet.write_text(json.dumps(rows,indent=2)+'\n')
print('Prepared selection icon and background.')
