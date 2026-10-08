import json
import hashlib
from PIL import Image
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]

def validate():
    sheets={p.stem:json.loads(p.read_text()) for p in (ROOT/'sheets').glob('*.json')}
    errors=[]; cells=0
    for name,rows in sheets.items():
        if not rows: errors.append(f'{name}: empty sheet'); continue
        columns=set(rows[0]); ids=set()
        for row in rows:
            id=row.get('id','?')
            if id in ids: errors.append(f'{name}: duplicate {id}')
            ids.add(id)
            for col in columns|set(row):
                cells+=1
                if col not in row or row[col] is None or row[col]=='': errors.append(f'{name}.{id}.{col}: unfilled')
            if set(row)!=columns: errors.append(f'{name}.{id}: inconsistent columns')
    classes={r['id'] for r in sheets['classes']}; assets={r['id'] for r in sheets['assets']}; cards={r['id'] for r in sheets['cards']}; manor={r['id'] for r in sheets['manor']}; systems={r['id'] for r in sheets['systems']}
    def ref(name,row,col,ids):
        if row[col] not in ids: errors.append(f'{name}.{row["id"]}.{col}: unresolved {row[col]}')
    for row in sheets['classes']: ref('classes',row,'asset',assets); ref('classes',row,'signature',cards)
    for row in sheets['traits']: ref('traits',row,'asset',assets)
    for row in sheets['cards']:
        ref('cards',row,'classId',classes); ref('cards',row,'asset',assets)
        ref('cards',row,'art',{r['id'] for r in sheets.get('card_art',[])})
        if row['type'] not in ('ATTACK','SKILL','POWER'): errors.append('invalid card type '+row['id'])
    for row in sheets['decks']:
        ref('decks',row,'classId',classes)
        for c in row['cards']:
            if c not in cards: errors.append('deck references '+c)
    for row in sheets['manor']: ref('manor',row,'requires',manor|{'none'})
    for row in sheets['hooks']: ref('hooks',row,'system',systems)
    art_hashes=set()
    for row in sheets.get('card_art',[]):
        ref('card_art',row,'cardId',cards)
        path=ROOT/'src/main/resources'/row['path']
        if not path.is_file():errors.append('Missing card art '+row['id']);continue
        digest=hashlib.sha256(path.read_bytes()).hexdigest()
        if digest!=row['sha256']:errors.append('Unverified card art '+row['id'])
        if digest in art_hashes:errors.append('Duplicate card art '+row['id'])
        art_hashes.add(digest)
        with Image.open(path) as image:
            if image.size!=(row['width'],row['height']):errors.append('Wrong art dimensions '+row['id'])
    # Confirm every selected source object against the real installed game's asset index.
    for row in sheets.get('ui_art',[]):
        path=ROOT/'src/main/resources'/row['path']
        if not path.is_file():errors.append('Missing UI art '+row['id']);continue
        if hashlib.sha256(path.read_bytes()).hexdigest()!=row['sha256']:errors.append('Unverified UI art '+row['id'])
        with Image.open(path) as image:
            if image.size!=(row['width'],row['height']):errors.append('Wrong UI dimensions '+row['id'])
            if row['transparent'] and ('A' not in image.getbands() or image.getchannel('A').getextrema()[0]==255):errors.append('UI art needs transparency '+row['id'])
    index=ROOT/'private'/'rl_asset_index.json'
    if not index.exists(): errors.append('Real Rogue Legacy 2 asset inspection missing')
    else:
        found={(r['file'],r['type'],r['name']) for r in json.loads(index.read_text())}
        for row in sheets['assets']:
            if (row['sourceFile'],row['sourceType'],row['sourceName']) not in found: errors.append('Missing real asset '+row['id'])
    atlas=ROOT/'private/cardui.atlas'
    if not atlas.exists(): errors.append('Host card atlas inspection missing')
    else:
        names=set(atlas.read_text().splitlines())
        for row in sheets['host_assets']:
            if row['sourceName'] not in names:errors.append('Missing host region '+row['sourceName'])
    if errors:
        print('\n'.join(errors)); raise SystemExit(1)
    print(f'Preflight clean: {len(sheets)} sheets, {sum(map(len,sheets.values()))} rows, {cells} filled and checked cells; references and source assets resolve.')
    return sheets

if __name__=='__main__': validate()
