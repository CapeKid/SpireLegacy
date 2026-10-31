import json
import hashlib
import re
from PIL import Image
from pathlib import Path
from design_schema import DesignError, load_sheets, validate_assets
ROOT=Path(__file__).resolve().parents[1]

def validate(historical_assets=False):
    try:
        sheets=load_sheets(ROOT)
        validate_assets(ROOT, sheets)
    except DesignError as error:
        print(error); raise SystemExit(1)
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
    for table in ('ancient_cards','pool_expansion'):
        for row in sheets[table]:
            ref(table,row,'cardId',cards);ref(table,row,'classId',classes|({'shared'} if table=='pool_expansion' else set()))
    for table in ('character_art','ancient_dialogue','family_chronicle'):
        for row in sheets[table]:
            ref(table,row,'id' if table=='character_art' else 'classId',classes|({'none'} if table=='family_chronicle' else set()))
    for row in sheets['card_art']:
        if row['id']!=row['cardId']:errors.append(f'card_art.{row["id"]}.cardId: art/card identity mismatch')
    for row in sheets['balance_changes']:
        ref('balance_changes',row,'id',cards)
        card=next((c for c in sheets['cards'] if c['id']==row['id']),None)
        if card:
            for key,value in row['after'].items():
                if key not in card or card[key]!=value:errors.append(f'balance_changes.{row["id"]}.after.{key}: stale current stat')
    for row in sheets['manor']:
        seen={row['id']};parent=row['requires']
        while parent!='none' and parent in manor:
            if parent in seen:errors.append(f'manor.{row["id"]}.requires: prerequisite cycle');break
            seen.add(parent);parent=next(r['requires'] for r in sheets['manor'] if r['id']==parent)
    for row in sheets['classes']: ref('classes',row,'asset',assets); ref('classes',row,'signature',cards)
    traits={r['id'] for r in sheets['traits']}
    effects={'none','gray','skill_block','nature','poison','skill_draw','fragile','pacifist','turn_block','first_attack_block','weak','enemy_guard','costly','gold','shop','histrionic','sepia','enemy_strength','hurt_weak','rainbow','vegan','diva','algesia','energy','no_heal','coin_loss','hurt_energy','super_heal','relics','thorns','blue','perfectionist','shock','chest','explosions','medium','festive','kit','piercing','exhausted'}
    effects.update({'translucent','hidden_health','treasure_gold','first_attack_damage','overcharge','first_attack_block_loss','first_attack_shred','first_attack_weak','opening_draw_energy','opening_weak','first_skill_block','first_hurt_block'})
    for row in sheets['traits']:
        if re.search(r'<[^>]+>|\{\d+\}',row['name']):errors.append('Unresolved trait name formatting '+row['id'])
        ref('traits',row,'asset',assets)
        if row['effect'] not in effects:errors.append('Unimplemented trait effect '+row['id'])
        if row['enabled'] and (row['summary']=='none' or (row['effect']=='none' and all(row[k]==0 for k in ('hp','strength','dexterity','draw','heal')))):errors.append('Selectable trait lacks an explained gameplay or visual effect '+row['id'])
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
        if row['upgradeCost'] and row['cost']==0:errors.append(f'cards.{row["id"]}.upgradeCost: upgrade produces negative cost')
        if not isinstance(row.get('upgradeDraw',-1),int) or row.get('upgradeDraw',-1)<-1:errors.append('Invalid draw upgrade override '+row['id'])
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
    for art in sheets.get('character_art',[]):
        path=ROOT/'src/main/resources'/art['path']
        if not path.is_file() or hashlib.sha256(path.read_bytes()).hexdigest()!=art['sha256']:
            errors.append('Character atlas missing/hash mismatch: '+art['id']);continue
        with Image.open(path) as atlas:
            if atlas.mode!='RGBA':errors.append('Character atlas must preserve transparency: '+art['id'])
            alpha=atlas.getchannel('A').point(lambda value:255 if value>=16 else 0)
            for x,y,w,h in art['regions']:
                bounds=alpha.crop((x,y,x+w,y+h)).getbbox()
                if not bounds or not (0<bounds[0]<bounds[2]<w and 0<bounds[1]<bounds[3]<h):errors.append('Character frame clips at a region edge: '+art['id'])
        if isinstance(art.get('death'),dict):
            death=art['death'];path=ROOT/'src/main/resources'/death['path']
            if not path.is_file() or hashlib.sha256(path.read_bytes()).hexdigest()!=death['sha256']:errors.append('Death atlas missing/hash mismatch: '+art['id']);continue
            with Image.open(path) as atlas:
                if atlas.mode!='RGBA':errors.append('Death atlas must preserve transparency: '+art['id'])
                alpha=atlas.getchannel('A').point(lambda value:255 if value>=16 else 0)
                if len(death['regions'])!=4 or len(death['feet'])!=4 or death['height']<=0:errors.append('Death animation requires four grounded frames: '+art['id'])
                for x,y,w,h in death['regions']:
                    bounds=alpha.crop((x,y,x+w,y+h)).getbbox()
                    if not bounds or not (0<bounds[0]<bounds[2]<w and 0<bounds[1]<bounds[3]<h):errors.append('Death frame clips at region edge: '+art['id'])
    for cls in classes:
        rows=[r for r in members if r['classId']==cls]
        if len(rows)!=len(membership[cls]):errors.append(cls+': duplicate pool membership')
        shared={r['cardId'] for r in rows if r['scope']=='shared'}
        unique={r['cardId'] for r in rows if r['scope']=='unique'}
        if len(shared)!=34 or len(unique)!=51:errors.append(cls+': expected 34 shared and 51 unique')
        shared_sets.append(shared);active.update(membership[cls])
        for key in unique:
            if key not in cards:continue
            card=next(r for r in sheets['cards'] if r['id']==key)
            if card['classId']!=cls:errors.append(cls+': foreign unique card '+key)
        for key in shared:
            if key not in cards:continue
            card=next(r for r in sheets['cards'] if r['id']==key)
            if card['rarity']=='BASIC' or card['power'] in ('reservoir','quiver') or card['special'] in ('charge_gain','charged_energy','arcane_aftershock','charge_burst','cleanse_charge','skill_damage'):errors.append('Class-specific mechanic in shared core '+key)
    if any(shared!=shared_sets[0] for shared in shared_sets):errors.append('Shared core differs by class')
    if len(active)!=187:errors.append('Expected 187 regular active cards across all classes')
    for row in sheets.get('branch_cards',[]):
        ref('branch_cards',row,'classId',classes);ref('branch_cards',row,'cardId',cards)
        if row['cardId'] in active:errors.append('Branch addition duplicates regular membership '+row['cardId'])
        if row['scope']!='unique' or next((c['classId'] for c in sheets['cards'] if c['id']==row['cardId']),None)!=row['classId']:errors.append('Foreign beta class card '+row['id'])
    if len({r['cardId'] for r in sheets.get('branch_cards',[])})!=9:errors.append('Expected nine class-unique beta additions')
    for cls in classes:
        extra=[r for r in sheets.get('branch_cards',[]) if r['classId']==cls]
        if len(extra)!=3 or len({r['cardId'] for r in extra})!=3:errors.append(cls+': expected three distinct beta additions')
        expanded=[r for r in sheets['cards'] if r['id'] in membership[cls]|{r['cardId'] for r in extra}]
        for rarity,count in [('BASIC',3),('COMMON',20),('UNCOMMON',38),('RARE',27)]:
            if sum(r['rarity']==rarity for r in expanded)!=count:errors.append(cls+': beta '+rarity+' parity mismatch')
        signatures=set()
        for card in expanded:
            signature=tuple((k,str(card[k])) for k in sorted(card) if k not in ('id','name','asset','art','classId','rarity') and not k.startswith('upgrade'))
            if signature in signatures:errors.append(cls+': duplicate beta mechanics '+card['id'])
            signatures.add(signature)
    active.update(r['cardId'] for r in sheets.get('branch_cards',[]))
    for row in sheets['cards']:
        if row['id'] not in active:continue
        for upgraded in (False,True):
            cost=max(0,row['cost']-(1 if upgraded and row['upgradeCost'] else 0))
            increment=row['upgradeDraw'] if row['upgradeDraw']>=0 else row['upgradeMagic']
            draw=row['draw']+(increment if upgraded and row['draw'] else 0)
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
            elif row['classId'] in membership and c not in membership[row['classId']]:errors.append(f'decks.{row["id"]}.cards: foreign starter {c}')
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
    icons=sheets.get('icon_art',[])
    expected={'trait_'+r['id'] for r in sheets['traits']}|{'class_'+r['id'] for r in sheets['classes']}
    if {r['id'] for r in icons}!=expected:errors.append('Original trait/class icon coverage incomplete')
    icon_hashes=set()
    for row in icons:
        path=ROOT/'src/main/resources'/row['path']
        if not path.is_file():errors.append('Missing original icon '+row['id']);continue
        digest=hashlib.sha256(path.read_bytes()).hexdigest()
        if digest!=row['sha256'] or digest in icon_hashes:errors.append('Changed or duplicate original icon '+row['id'])
        icon_hashes.add(digest)
        import xml.etree.ElementTree as ET
        svg=ET.fromstring(path.read_text())
        if svg.get('viewBox')!='0 0 128 128' or any(e.tag.split('}')[-1] in {'image','text','script','foreignObject'} for e in svg.iter()):errors.append('Icon is not self-contained vector art '+row['id'])
    relics=sheets.get('heir_relics',[]);relic_art={r['id']:r for r in sheets.get('relic_art',[])}
    chronicle=sheets.get('family_chronicle',[])
    if len(chronicle)!=9 or len({r['id'] for r in chronicle})!=9:errors.append('Nine distinct Chronicle entries required')
    unlocks=[r['rewardId'] for r in chronicle if r['rewardKind']=='relic']
    if set(unlocks)!={'guardian_knot','scholar_bookmark','trail_compass'}:errors.append('Three class lesson unlocks required')
    for entry in chronicle:
        if not 200<len(entry['story'])<1200 or entry['rewardKind'] not in {'story','relic','motto'}:errors.append('Incomplete Chronicle entry '+entry['id'])
        if entry['rewardKind']=='relic' and entry['rewardId'] not in relic_art:errors.append('Missing Chronicle reward/icon '+entry['id'])
    if len(relics)!=10 or set(relic_art)!={r['id'] for r in relics}:errors.append('Ten original exclusive relics/icons required')
    if sorted(r['rarity'] for r in relics)!=sorted(['Common']*2+['Uncommon']*6+['Rare']*2):errors.append('Wrong exclusive relic rarity distribution')
    for row in relics:
        if '{' in row['description'] or not row['flavor'] or row['amount']<=0:errors.append('Incomplete relic rules '+row['id'])
    for art in relic_art.values():
        if art['license']!='MIT' or art['author']!='CapeKid':errors.append('Missing original relic provenance '+art['id'])
        for field,hashfield in [('path','sha256'),('outline','outlineSha256')]:
            path=ROOT/'src/main/resources'/art[field]
            if not path.is_file() or hashlib.sha256(path.read_bytes()).hexdigest()!=art[hashfield]:errors.append('Relic icon hash mismatch '+art['id']);continue
            svg=ET.fromstring(path.read_text())
            if svg.get('viewBox')!='0 0 128 128' or any(e.tag.split('}')[-1] in {'image','text','script','foreignObject'} for e in svg.iter()):errors.append('Relic icon is not self-contained '+art['id'])
    if len({r['sha256'] for r in relic_art.values()})!=10:errors.append('Relic artwork must be distinct')
    potions=sheets.get('heir_potions',[]);potion_art={r['id']:r for r in sheets.get('potion_art',[])}
    if len(potions)!=3 or set(potion_art)!={r['id'] for r in potions}:errors.append('Three original exclusive potions/icons required')
    for row in potions:
        if row['rarity'] not in {'Common','Uncommon','Rare'} or row['target'] not in {'Self','AnyEnemy'} or '{' in row['description'] or row['amount']<=0 or not row['baseline']:errors.append('Incomplete potion rules '+row['id'])
    for art in potion_art.values():
        if art['license']!='MIT' or art['author']!='CapeKid':errors.append('Missing original potion provenance '+art['id'])
        for field,hashfield in [('path','sha256'),('outline','outlineSha256')]:
            path=ROOT/'src/main/resources'/art[field]
            if not path.is_file() or hashlib.sha256(path.read_bytes()).hexdigest()!=art[hashfield]:errors.append('Potion icon hash mismatch '+art['id']);continue
            svg=ET.fromstring(path.read_text())
            if svg.get('viewBox')!='0 0 128 128' or any(e.tag.split('}')[-1] in {'image','text','script','foreignObject'} for e in svg.iter()):errors.append('Potion icon is not self-contained '+art['id'])
    if len({r['sha256'] for r in potion_art.values()})!=3:errors.append('Potion artwork must be distinct')
    for art in sheets.get('presentation_art',[]):
        path=ROOT/'src/main/resources'/art['path']
        if art['license']!='MIT' or art['author']!='CapeKid' or not path.is_file() or hashlib.sha256(path.read_bytes()).hexdigest()!=art['sha256']:
            errors.append('Unverified original presentation asset '+art['id']);continue
        if art['kind']=='vector':
            svg=ET.fromstring(path.read_text())
            if svg.get('viewBox')!='0 0 128 128' or any(e.tag.split('}')[-1] in {'image','text','script','foreignObject'} for e in svg.iter()):errors.append('Presentation vector is not self-contained '+art['id'])
            if path.stem.endswith('-energy-text') and (svg.get('width'),svg.get('height'))!=('24','24'):errors.append('Inline energy artwork must match native 24px text icons '+art['id'])
        elif art['kind']=='audio':
            import wave
            with wave.open(str(path),'rb') as wav:
                if wav.getparams()[:3]!=(1,2,22050) or not 4000<=wav.getnframes()<=16000:errors.append('Wrong presentation PCM format '+art['id'])
    # Historical StS1 asset inspection is optional and never needed for the StS2 build.
    index=ROOT/'private'/'rl_asset_index.json'
    if historical_assets and not index.exists(): errors.append('Historical source-asset inspection missing')
    elif historical_assets:
        found={(r['file'],r['type'],r['name']) for r in json.loads(index.read_text())}
        for row in sheets['assets']:
            if (row['sourceFile'],row['sourceType'],row['sourceName']) not in found: errors.append('Missing real asset '+row['id'])
    atlas=ROOT/'private/cardui.atlas'
    if historical_assets and not atlas.exists(): errors.append('Historical StS1 atlas inspection missing')
    elif historical_assets:
        names=set(atlas.read_text().splitlines())
        for row in sheets['host_assets']:
            if row['sourceName'] not in names:errors.append('Missing host region '+row['sourceName'])
    if errors:
        print('\n'.join(errors)); raise SystemExit(1)
    print(f'Preflight clean: {len(sheets)} sheets, {sum(map(len,sheets.values()))} rows, {cells} checked cells; design references and original mod art resolve.')
    return sheets

if __name__=='__main__':
    import argparse
    parser=argparse.ArgumentParser();parser.add_argument('--historical-sts1-assets',action='store_true')
    validate(parser.parse_args().historical_sts1_assets)
