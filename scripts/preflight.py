import json
import hashlib
import re
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
    traits={r['id'] for r in sheets['traits']}
    effects={'none','gray','skill_block','nature','poison','skill_draw','fragile','pacifist','turn_block','first_attack_block','weak','enemy_guard','costly','gold','shop','histrionic','sepia','enemy_strength','hurt_weak','rainbow','vegan','diva','algesia','energy','no_heal','coin_loss','hurt_energy','super_heal','relics','thorns','blue','perfectionist','shock','chest','explosions','medium','festive','kit','piercing','exhausted'}
    for row in sheets['traits']:
        if re.search(r'<[^>]+>|\{\d+\}',row['name']):errors.append('Unresolved trait name formatting '+row['id'])
        ref('traits',row,'asset',assets)
        if row['effect'] not in effects:errors.append('Unimplemented trait effect '+row['id'])
        for excluded in row['excludes']:
            # Source incompatibilities can reference a disabled historical entry.
            if excluded not in traits and excluded not in {r['id'] for r in sheets['trait_coverage']}:errors.append('Unknown exclusion '+excluded)
    for row in sheets.get('trait_coverage',[]):
        if re.search(r'<[^>]+>|\{\d+\}',row['name']):errors.append('Unresolved coverage name formatting '+row['id'])
        if row['status']=='adapted':ref('trait_coverage',row,'traitId',traits)
        elif row['sourceRarity'] in (1,2,3):errors.append('Eligible source trait missing '+row['source'])
    for row in sheets['cards']:
        ref('cards',row,'classId',classes); ref('cards',row,'asset',assets)
        ref('cards',row,'art',{r['id'] for r in sheets.get('card_art',[])})
        if row['type'] not in ('ATTACK','SKILL','POWER'): errors.append('invalid card type '+row['id'])
        if row['power'] not in {'none','reservoir','quiver'}|{r['id'] for r in sheets.get('power_effects',[])}:errors.append('Unsupported class power '+row['id'])
        if row.get('special','none') not in {'none'}|{r['id'] for r in sheets.get('special_effects',[])}:errors.append('Unsupported card mechanic '+row['id'])
        if row.get('aoe') and row.get('special')=='random_attack':errors.append('Conflicting multi-target modes '+row['id'])
    members=sheets['card_pools']
    membership={cls:{r['cardId'] for r in members if r['classId']==cls} for cls in classes}
    shared_sets=[]
    active=set()
    for row in members:
        ref('card_pools',row,'classId',classes);ref('card_pools',row,'cardId',cards)
        if row['scope'] not in ('shared','unique'):errors.append('Invalid membership scope '+row['id'])
    for cls in classes:
        rows=[r for r in members if r['classId']==cls]
        if len(rows)!=len(membership[cls]):errors.append(cls+': duplicate pool membership')
        shared={r['cardId'] for r in rows if r['scope']=='shared'}
        unique={r['cardId'] for r in rows if r['scope']=='unique'}
        if len(shared)!=30 or len(unique)!=45:errors.append(cls+': expected 30 shared and 45 unique')
        shared_sets.append(shared);active.update(membership[cls])
        for key in unique:
            if key not in cards:continue
            card=next(r for r in sheets['cards'] if r['id']==key)
            if card['classId']!=cls:errors.append(cls+': foreign unique card '+key)
        for key in shared:
            if key not in cards:continue
            card=next(r for r in sheets['cards'] if r['id']==key)
            if card['rarity']=='BASIC' or card['power'] in ('reservoir','quiver') or card['special']=='charge_gain':errors.append('Class-specific mechanic in shared core '+key)
    if any(shared!=shared_sets[0] for shared in shared_sets):errors.append('Shared core differs by class')
    if len(active)!=165:errors.append('Expected 165 active cards across all classes')
    for row in sheets['cards']:
        if row['id'] not in active:continue
        for upgraded in (False,True):
            cost=max(0,row['cost']-(1 if upgraded and row['upgradeCost'] else 0))
            draw=row['draw']+(row['upgradeMagic'] if upgraded and row['draw'] else 0)
            discard=row['discard']+(row['upgradeDiscard'] if upgraded else 0)
            if cost==0 and not row['exhaust'] and draw>discard:errors.append('Repeatable free positive-draw card '+row['id'])
    for target in sheets.get('pool_targets',[]):
        ref('pool_targets',target,'id',classes)
        pool=[r for r in sheets['cards'] if r['id'] in membership[target['id']]]
        if len(pool)!=target['total']:errors.append(target['id']+': class pool is not full-sized')
        for rarity in ('BASIC','COMMON','UNCOMMON','RARE'):
            if sum(r['rarity']==rarity for r in pool)!=target[rarity.lower()]:errors.append(target['id']+': incorrect '+rarity+' pool size')
    for cls in classes:
        pool=[r for r in sheets['cards'] if r['id'] in membership[cls] and r['rarity']!='BASIC']
        signatures=set()
        for row in pool:
            signature=tuple((key,str(row[key])) for key in sorted(row) if key not in ('id','name','asset','art','classId','rarity') and not key.startswith('upgrade'))
            if signature in signatures:errors.append(cls+': duplicated card mechanics '+row['id'])
            signatures.add(signature)
    for row in sheets.get('card_builds',[]):
        ref('card_builds',row,'id',cards)
        ref('card_builds',row,'classId',classes)
        if row['id'] not in cards:continue
        card=next(r for r in sheets['cards'] if r['id']==row['id'])
        if card['classId']!=row['classId']:errors.append('Incorrect build class '+row['id'])
    for cls in classes:
        additions=[r for r in sheets.get('card_builds',[]) if r['classId']==cls]
        if len(additions)!=45:errors.append(cls+': incomplete 45-card build expansion')
        for build in {r['build'] for r in additions}:
            group=[r for r in additions if r['build']==build]
            if not any(r['role']=='payoff' for r in group):errors.append(cls+': build has no payoff '+build)
        pool=[r for r in sheets['cards'] if r['id'] in membership[cls] and r['rarity']!='BASIC']
        for kind in ('ATTACK','SKILL'):
            if len([r for r in pool if r['type']==kind])<2:errors.append(f'{cls}: shop needs two distinct {kind} cards')
            for rarity in ('COMMON','UNCOMMON','RARE'):
                if not any(r['type']==kind and r['rarity']==rarity for r in pool):errors.append(f'{cls}: missing {rarity} {kind} shop tier')
        for rarity in ('UNCOMMON','RARE'):
            if not any(r['type']=='POWER' and r['rarity']==rarity for r in pool):errors.append(f'{cls}: missing {rarity} POWER shop tier')
    for row in sheets['class_mechanics']:ref('class_mechanics',row,'id',classes)
    for row in sheets['starter_genes']:ref('starter_genes',row,'id',traits)
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
