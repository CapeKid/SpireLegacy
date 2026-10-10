"""Author the missing shop tiers and class mechanics in the design sheets."""
import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
def read(name):return json.loads((ROOT/'sheets'/f'{name}.json').read_text())
def write(name,rows):(ROOT/'sheets'/f'{name}.json').write_text(json.dumps(rows,indent=2)+'\n')
cards=read('cards');art=read('card_art')
new=[
 ('shieldwall','Shield Wall','knight','SKILL','UNCOMMON',1,dict(block=12),'A tiny broad-helmeted knight locks a blue shield into a wall of three shields, arrows bouncing away; layered defensive cover.'),
 ('rally','Royal Rally','knight','SKILL','RARE',1,dict(block=10,strength=1,exhaust=True),'A stout red-caped knight raises a golden banner behind a blue shield, rallying tiny allies; defense and renewed fighting strength.'),
 ('meditate','Meditate','mage','SKILL','RARE',0,dict(draw=2,energy=1,exhaust=True),'A tiny bearded red-hatted mage sits cross-legged, two floating parchment cards and one bright blue mana crystal circling him; drawing spells and restoring energy.'),
 ('reservoir','Arcane Reservoir','mage','POWER','UNCOMMON',1,dict(power='reservoir',magic=1),'A tiny bearded red-hatted mage pours a blue potion into a huge glowing crystal reservoir, with a wand aimed beside it; storing stronger arcane charges.'),
 ('smokescreen','Smoke Screen','ranger','SKILL','UNCOMMON',1,dict(block=8,draw=1),'A tiny green-hooded archer disappears behind a curling gray smoke cloud, an arrow bouncing away and one parchment card peeking out; evasive defense and card cycling.'),
 ('escapeplan','Escape Plan','ranger','SKILL','RARE',0,dict(block=6,draw=2,exhaust=True),'A tiny green-hooded archer leaps backward behind a tree, two floating parchment cards following the retreat; free evasive defense and drawing options.'),
 ('quiver','Endless Quiver','ranger','POWER','UNCOMMON',1,dict(power='quiver',magic=1),'A tiny green-hooded archer opens a magical quiver spilling a flowing loop of arrows and one parchment card; sustained attack-chain card draw.'),
]
prompt=art[0]['prompt'].split('\nCard:')[0]
for row in cards:
    row.setdefault('power','none');row.setdefault('magic',0);row.setdefault('discard',0)
    if row['id']=='mage_strike':row['damage']=5
    if row['id']=='mage_guard':row['block']=4
    if row['id']=='knight_strike':row['block']=2
    if row['id']=='ranger_guard':row['block']=4;row['draw']=1;row['discard']=1
for key,name,cls,kind,rarity,cost,values,action in new:
    if any(r['id']==key for r in cards):continue
    row=dict(cards[0]);row.update(id=key,name=name,classId=cls,type=kind,rarity=rarity,cost=cost,asset=next(r['asset'] for r in read('classes') if r['id']==cls),art=key)
    for field in ('damage','block','draw','energy','strength','dexterity','weak','vulnerable','upgradeDamage','upgradeBlock','upgradeMagic','magic','discard'):row[field]=0
    row.update(hits=1,exhaust=False,power='none');row.update(values)
    if row['block']:row['upgradeBlock']=3
    if any(row[f]>0 for f in ('draw','energy','strength','magic')):row['upgradeMagic']=1
    cards.append(row)
    art.append(dict(id=key,cardId=key,name=name,path=f'heir/cards/{key}.png',width=250,height=190,prompt=prompt+'\nCard: '+name+'. Action: '+action,generator='built-in imagegen',sha256='pending'))
write('cards',cards);write('card_art',art)
write('class_mechanics',[
 dict(id='knight',name='Counterguard',effect='counterguard',amount=4,limit=1,summary='Playing a Skill primes your next Attack for +4 damage. Attacking consumes the counter.'),
 dict(id='mage',name='Arcane Charges',effect='charges',amount=2,limit=3,summary='Each Skill grants one charge, up to three. Your next Attack gains +2 damage per charge and consumes them.'),
 dict(id='ranger',name='Hunter Rhythm',effect='rhythm',amount=1,limit=3,summary='Every third Attack in a turn draws one card and applies one Vulnerable to its target.'),
])
write('starter_genes',[
 dict(id='large', damage=0, block=0, draw=0, heal=0, poison=0, mercy=False, summary='No additional starter-card modifiers; use the trait description.'),
 dict(id='small', damage=0, block=0, draw=0, heal=0, poison=0, mercy=False, summary='No additional starter-card modifiers; use the trait description.'),
 dict(id='weapon', damage=0, block=0, draw=0, heal=0, poison=0, mercy=False, summary='No additional starter-card modifiers; use the trait description.'),
 dict(id='magic', damage=0, block=0, draw=0, heal=0, poison=0, mercy=False, summary='No additional starter-card modifiers; use the trait description.'),
 dict(id='vampire', damage=0, block=0, draw=0, heal=0, poison=0, mercy=False, summary='No additional starter-card modifiers; use the trait description.'),
 dict(id='smallhitbox', damage=0, block=0, draw=0, heal=0, poison=0, mercy=False, summary='No additional starter-card modifiers; use the trait description.'),
 dict(id='lowergravity', damage=0, block=0, draw=0, heal=0, poison=0, mercy=False, summary='No additional starter-card modifiers; use the trait description.'),
 dict(id='cantattack',damage=0,block=4,draw=0,heal=0,poison=2,mercy=True,summary='Starter Attacks become Mercy Skills: gain four Block and apply two Poison instead of attacking.'),
])
classes=read('classes')
for row in classes:row['summary']=next(r['summary'] for r in read('class_mechanics') if r['id']==row['id'])
write('classes',classes)
print('Authored',len(cards),'cards and class/starter genetics sheets.')
