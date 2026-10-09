"""Keep original generated art and export thumbnail resources with provenance."""
import argparse, json, hashlib, shutil
from pathlib import Path
from PIL import Image, ImageOps
ROOT=Path(__file__).resolve().parents[1]
parser=argparse.ArgumentParser();parser.add_argument('card');parser.add_argument('source',type=Path);args=parser.parse_args()
original=ROOT/'art/originals'/f'{args.card}.png';original.parent.mkdir(parents=True,exist_ok=True)
shutil.copy2(args.source,original)
destination=ROOT/'src/main/resources/heir/cards'/f'{args.card}.png'
with Image.open(original) as image:
    ImageOps.fit(image.convert('RGB'),(250,190),method=Image.Resampling.LANCZOS).save(destination)
sheet=ROOT/'sheets/card_art.json';rows=json.loads(sheet.read_text())
row=next(r for r in rows if r['id']==args.card);row['sha256']=hashlib.sha256(destination.read_bytes()).hexdigest()
sheet.write_text(json.dumps(rows,indent=2)+'\n')
print(args.card,'exported')
