"""Review the effect-based illustrations added in 0.3.0, one complete class at a time."""
import json,sys
from pathlib import Path
from PIL import Image,ImageDraw
ROOT=Path(__file__).resolve().parents[1]
art={r['id']:r for r in json.loads((ROOT/'sheets/card_art.json').read_text())}
cards={r['id']:r for r in json.loads((ROOT/'sheets/cards.json').read_text())}
roles=json.loads((ROOT/'sheets/card_builds.json').read_text())
for cls in sys.argv[1:] or ('knight','mage','ranger'):
    rows=[r for r in roles if r['classId']==cls]
    if any(art[r['id']]['sha256']=='pending' for r in rows):raise SystemExit('Finish '+cls+' illustrations before reviewing.')
    sheet=Image.new('RGB',(1300,((len(rows)+4)//5)*228),'#e8dcc6');draw=ImageDraw.Draw(sheet)
    for i,row in enumerate(rows):
        x=(i%5)*260;y=(i//5)*228
        with Image.open(ROOT/'src/main/resources'/art[row['id']]['path']) as im:sheet.paste(im,(x,y))
        draw.text((x+4,y+193),cards[row['id']]['name'],fill='#252019')
        draw.text((x+4,y+207),row['build']+' / '+row['role'],fill='#4e4031')
    path=ROOT/'media'/('added-card-art-'+cls+'.png');sheet.save(path);print(path)
