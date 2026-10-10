"""0.5 balance pass: baseline deficits, cost-aware draw, and restrained upgrades.

The first run records original stats; later runs always reuse those originals.
Native baseline rules are audited privately in the owned running game.
"""
import json
import math
from balance_policy import balance_card, balance_manor, class_rules
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
read=lambda name:json.loads((ROOT/'sheets'/f'{name}.json').read_text())
write=lambda name,rows:(ROOT/'sheets'/f'{name}.json').write_text(json.dumps(rows,indent=2)+'\n')
FIELDS=('cost','damage','block','hits','draw','discard','energy','strength','dexterity','weak','vulnerable','poison','heal','hpLoss','vigor','thorns','plated','nextEnergy','nextDraw','nextBlock','power','magic','special','specialAmount','exhaust','upgradeDamage','upgradeBlock','upgradeMagic','upgradeSpecial','upgradeCost','upgradeDiscard')

def main():
    cards=read('cards');active={r['cardId'] for r in read('card_pools')}
    previous={r['id']:r for r in read('balance_changes')} if (ROOT/'sheets/balance_changes.json').exists() else {}
    changes=[]
    for row in cards:
        row.setdefault('upgradeDiscard',0)
        before=previous[row['id']]['before'] if row['id'] in previous else {k:row[k] for k in FIELDS}
        row.update(before)
        for field in ('damage','block','nextBlock'):
            if row[field]>0:row[field]=max(1,math.floor(row[field]*.75))
        for field in ('poison','vigor','thorns','plated'):
            if row[field]>1:row[field]=max(1,math.floor(row[field]*.75))
        if row['strength']>1:row['strength']-=1
        if row['dexterity']>1:row['dexterity']-=1
        if row['magic']>2:row['magic']=max(1,math.floor(row['magic']*.75))
        if row['special'] in ('guard_bonus','marked_bonus','exhaust_damage','exhaust_block','retain_damage','retain_block','discard_block','chain_damage') and row['specialAmount']>1:
            row['specialAmount']=max(1,math.floor(row['specialAmount']*.75))
        if row['upgradeDamage']>2:row['upgradeDamage']=max(1,math.floor(row['upgradeDamage']*.67))
        if row['upgradeBlock']>2:row['upgradeBlock']=max(1,math.floor(row['upgradeBlock']*.67))
        if row['upgradeSpecial']>1:row['upgradeSpecial']=1
        if row['power']!='none' or row['energy'] or row['nextEnergy'] or row['strength'] or row['dexterity']:
            row['upgradeMagic']=0
        if row['power']!='none' and row['magic']>0 and not row['upgradeCost']:
            if row['power'] in ('discard_energy','skill_energy'):row['upgradeCost']=True
            else:row['upgradeMagic']=1
        # Repeated zero-energy positive draw requires a native-style limitation.
        if row['id']=='mage_cold_read':row.update(draw=1,discard=1,upgradeMagic=1,upgradeDiscard=1)
        if row['id']=='ranger_sort_quiver':row.update(cost=1,draw=3,discard=2,upgradeMagic=1,upgradeDiscard=1)
        if row['id']=='mage_ash_reading':row.update(cost=1,upgradeMagic=1,upgradeCost=False)
        if row['id']=='knight_battle_meditation':row.update(draw=2,discard=1,upgradeMagic=1,upgradeDiscard=1)
        if row['id']=='knight_blood_price':row['upgradeMagic']=1
        if row['id']=='meditate':row.update(draw=1,upgradeMagic=1)
        if row['id']=='ranger_master_plan':row.update(cost=3,draw=0,upgradeMagic=0,upgradeCost=True)
        if row['id']=='challenge':row.update(cost=2,upgradeMagic=1)
        if row['power']=='dark_embrace':row.update(cost=2,upgradeCost=True,upgradeMagic=0)
        if row['power']=='thousand_cuts':row.update(cost=2,upgradeCost=False,upgradeMagic=1)
        if row['special']=='discount_hand':row.update(specialAmount=1,upgradeSpecial=1)
        if row['rarity']=='COMMON' and row['cost']==1 and row['damage'] and row['draw']:row['upgradeMagic']=0
        if row['rarity']=='BASIC':
            if row['id'].endswith('_strike'):row.update(damage=5,block=0,upgradeDamage=2,draw=0,discard=0)
            if row['id'].endswith('_guard'):row.update(damage=0,block=4,draw=0,discard=0,upgradeBlock=2,upgradeMagic=0)
            if row['id']=='shield':row.update(damage=5,block=3,upgradeDamage=2,upgradeBlock=1)
            if row['id']=='flame':row.update(damage=7,block=0,upgradeDamage=2)
            if row['id']=='aim':row.update(damage=6,draw=1,upgradeDamage=2)
        improves=bool((row['damage'] and row['upgradeDamage']) or (row['block'] and row['upgradeBlock']) or row['upgradeMagic'] or row['upgradeSpecial'] or row['upgradeCost'])
        if not improves and row['cost']>0:row['upgradeCost']=True
        # Legacy cards can still be reached through cross-color effects or saves.
        if row['cost']==0 and row['draw'] and not row['exhaust']:
            if row['discard']:
                row['discard']=max(row['discard'],row['draw'])
                row['upgradeDiscard']=max(row['upgradeDiscard'],row['upgradeMagic'])
            else:row.update(cost=1,upgradeCost=False)
        balance_card(row)
        reference='Native Strike / Defend / Iron Wave' if row['rarity']=='BASIC' else 'Prepared / Acrobatics / Backflip / Skim' if row['draw'] or row['discard'] else 'Inflame / Footwork / Feel No Pain / After Image' if row['type']=='POWER' else 'Native attack/defense cost and upgrade budgets; class passive included separately'
        changes.append(dict(id=row['id'],reference=reference,before=before,after={k:row[k] for k in FIELDS},reason='Weaker no-manor baseline; avoid compounded effects/upgrades and free positive-draw cycles.'))
    write('cards',cards);write('balance_changes',changes)
    classes=read('classes');hp={'knight':60,'mage':52,'ranger':56}
    for row in classes:row.update(hp=hp[row['id']],strength=0,dexterity=0,heal=0)
    mechanics=read('class_mechanics')
    for row in mechanics:
        if row['id']=='knight':row.update(amount=2,summary='Playing a Skill primes your next Attack for +2 damage. Attacking consumes the counter.')
        if row['id']=='mage':row.update(amount=1,limit=2,summary='Each Skill grants one charge, up to two. Your next Attack gains +1 damage per charge and consumes them.')
        class_rules(row)
    for row in classes:row['summary']=next(m['summary'] for m in mechanics if m['id']==row['id'])
    write('classes',classes);write('class_mechanics',mechanics)
    manor=read('manor')
    costs={'vitality':(80,35),'smith':(120,80),'armory':(120,80),'vault':(100,40),'garden':(100,40),'library':(350,0)}
    for row in manor:
        row['baseCost'],row['costStep']=costs[row['id']]
        if row['id']=='vault':row.update(perLevel=5,summary='+5% legacy earnings per level.')
        balance_manor(row)
    write('manor',manor)
    print('Balanced',len(changes),'cards, including legacy save/cross-color cards.')

if __name__=='__main__':main()
