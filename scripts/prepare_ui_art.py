import json,hashlib
from pathlib import Path
from design_schema import load_sheets
from PIL import Image,ImageOps
ROOT=Path(__file__).resolve().parents[1]
sheet=ROOT/'sheets/ui_art.json'
rows=load_sheets(ROOT)['ui_art']
def source(row):
    return ROOT/'src/main/resources'/row['path'] if row['exportMode']=='preserve' else ROOT/'art/originals'/(row['id']+'.png')
for row in rows:
    try:
        with Image.open(source(row)) as image:
            if row['exportMode']=='preserve' and (image.size!=(row['width'],row['height']) or hashlib.sha256(source(row).read_bytes()).hexdigest()!=row['sha256']):
                raise ValueError('retained original differs from its manifest')
            image.verify()
    except (OSError,ValueError) as error:raise SystemExit(f'ui_art.{row["id"]}: invalid/missing original: {error}')
for row in rows:
    if row['exportMode']=='preserve':continue
    with Image.open(source(row)) as image:
        image=ImageOps.fit(image.convert('RGBA'),(row['width'],row['height']),Image.Resampling.LANCZOS)
        path=ROOT/'src/main/resources'/row['path'];path.parent.mkdir(parents=True,exist_ok=True);image.save(path)
        row['sha256']=hashlib.sha256(path.read_bytes()).hexdigest()
sheet.write_text(json.dumps(rows,indent=2)+'\n', encoding='utf-8', newline='\n')
print('Prepared selection icon and background.')
