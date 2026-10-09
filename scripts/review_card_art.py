"""Export a full-size, labeled artwork review for each completed class library."""
import json
from pathlib import Path
from PIL import Image, ImageDraw
ROOT=Path(__file__).resolve().parents[1]
cards=json.loads((ROOT/'sheets/cards.json').read_text())
art={r['cardId']:r for r in json.loads((ROOT/'sheets/card_art.json').read_text())}
members=json.loads((ROOT/'sheets/card_pools.json').read_text())
for cls in ('knight','mage','ranger'):
    pool={r['cardId']:r['scope'] for r in members if r['classId']==cls}
    rows=[r for r in cards if r['id'] in pool]
    if any(art[r['id']]['sha256']=='pending' for r in rows):raise SystemExit('Finish original artwork before exporting the release review.')
    sheet=Image.new('RGB',(1560,((len(rows)+5)//6)*216),'#e8dcc6')
    draw=ImageDraw.Draw(sheet)
    for index,row in enumerate(rows):
        left=(index%6)*260;top=(index//6)*216
        with Image.open(ROOT/'src/main/resources'/art[row['id']]['path']) as image:sheet.paste(image.convert('RGB'),(left,top))
        draw.text((left+4,top+193),row['name']+(' [shared]' if pool[row['id']]=='shared' else ''),fill='#252019')
    path=ROOT/'media'/('card-art-'+cls+'.png');sheet.save(path);print(path)
