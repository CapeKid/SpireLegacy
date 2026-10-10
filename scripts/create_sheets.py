"""Initial approved design. Subsequent edits belong in sheets, not this initializer."""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
def sheet(name, rows):
    (ROOT/'sheets'/f'{name}.json').write_text(json.dumps(rows, indent=2)+'\n')

classes = [
    dict(id='knight', name='Knight', asset='Icons_Classes_SwordClass', hp=78, strength=0, dexterity=1, heal=2, signature='shield', summary='Sword strikes and dependable defenses.'),
    dict(id='mage', name='Mage', asset='Icons_Classes_MagicWandClass', hp=64, strength=0, dexterity=0, heal=2, signature='flame', summary='Spell attacks, drawing cards and energy.'),
    dict(id='ranger', name='Ranger', asset='Icons_Classes_BowClass', hp=70, strength=1, dexterity=0, heal=2, signature='aim', summary='Multi-hit arrows and carefully timed attacks.'),
]
sheet('classes', classes)
traits = [
    ('large','Gigantism','Icons_Traits_YouAreLarge',12,0,-1,0,15,1.3,'More health, but less Dexterity.'),
    ('small','Dwarfism','Icons_Traits_YouAreSmall',-10,0,2,0,15,.8,'Less health, but more Dexterity.'),
    ('weapon','Musclebound','Icons_Traits_WeaponDamageBoost',-12,2,0,0,20,1.,'More Strength, but less health.'),
    ('magic','Magic Gift','Icons_Traits_MagicDamageBoost',-10,0,0,1,20,1.,'Draw another card each turn, but lose health.'),
    ('vampire','Vampirism','Icons_Traits_Vampire',-14,0,0,0,25,1.,'Heal 4 after combat, but have less health.'),
    ('health','Healthy','Icons_Traits_BonusHealth',16,-1,0,0,10,1.,'More health, but less Strength.'),
    ('strength','Strong','Icons_Traits_BonusStrength',0,1,-1,0,15,1.,'More Strength, but less Dexterity.'),
    ('smallhitbox','Hollow Bones','Icons_Traits_SmallHitbox',-16,0,3,0,25,.9,'Excellent Dexterity, but fragile.'),
]
sheet('traits', [dict(id=i,name=n,asset=a,hp=h,strength=s,dexterity=d,draw=r,goldBonus=g,scale=z,summary=t,heal=4 if i=='vampire' else 0) for i,n,a,h,s,d,r,g,z,t in traits])
cards=[]
def card(id,name,cl,type,cost,rarity,damage=0,block=0,draw=0,energy=0,strength=0,dexterity=0,weak=0,vulnerable=0,hits=1,exhaust=False,upgradeDamage=0,upgradeBlock=0,upgradeMagic=0,asset=None):
    cards.append(dict(id=id,name=name,classId=cl,type=type,cost=cost,rarity=rarity,damage=damage,block=block,draw=draw,energy=energy,strength=strength,dexterity=dexterity,weak=weak,vulnerable=vulnerable,hits=hits,exhaust=exhaust,upgradeDamage=upgradeDamage,upgradeBlock=upgradeBlock,upgradeMagic=upgradeMagic,asset=asset or next(c['asset'] for c in classes if c['id']==cl)))
for cl in classes:
    i=cl['id']; card(i+'_strike','Sword Strike' if i=='knight' else 'Spark' if i=='mage' else 'Arrow',i,'ATTACK',1,'BASIC',damage=6,upgradeDamage=3)
    card(i+'_guard','Guard' if i=='knight' else 'Ward' if i=='mage' else 'Sidestep',i,'SKILL',1,'BASIC',block=5,upgradeBlock=3)
card('shield','Shield Bash','knight','ATTACK',1,'BASIC',damage=7,block=5,upgradeDamage=2,upgradeBlock=2)
card('cleave','Greatsword','knight','ATTACK',2,'COMMON',damage=18,upgradeDamage=6)
card('fortify','Fortify','knight','SKILL',1,'COMMON',block=10,upgradeBlock=4)
card('riposte','Riposte','knight','ATTACK',1,'COMMON',damage=8,block=4,upgradeDamage=3,upgradeBlock=2)
card('challenge','Challenge','knight','ATTACK',1,'UNCOMMON',damage=8,vulnerable=2,upgradeDamage=3,upgradeMagic=1)
card('valor','Valor','knight','POWER',1,'UNCOMMON',strength=2,upgradeMagic=1)
card('bastion','Bastion','knight','POWER',1,'RARE',dexterity=3,upgradeMagic=1)
card('oath','Family Oath','knight','ATTACK',2,'RARE',damage=14,hits=2,exhaust=True,upgradeDamage=4)
card('flame','Flame Barrier','mage','SKILL',1,'BASIC',block=5,upgradeBlock=2,power='flame_barrier',magic=2,upgradeMagic=1)
card('fireball','Fireball','mage','ATTACK',2,'COMMON',damage=20,upgradeDamage=6)
card('study','Study','mage','SKILL',1,'COMMON',draw=2,upgradeMagic=1)
card('barrier','Magic Barrier','mage','SKILL',1,'COMMON',block=9,draw=1,upgradeBlock=3)
card('concentrate','Concentrate','mage','SKILL',0,'UNCOMMON',energy=1,exhaust=True,upgradeMagic=1)
card('frost','Frost Bolt','mage','ATTACK',1,'UNCOMMON',damage=8,weak=2,upgradeDamage=3,upgradeMagic=1)
card('surge','Spell Surge','mage','POWER',1,'RARE',strength=3,upgradeMagic=1)
card('nova','Nova','mage','ATTACK',3,'RARE',damage=36,exhaust=True,upgradeDamage=12)
card('aim','Steady Aim','ranger','ATTACK',1,'BASIC',damage=9,draw=1,upgradeDamage=3)
card('double','Double Shot','ranger','ATTACK',1,'COMMON',damage=4,hits=2,upgradeDamage=2)
card('retreat','Retreat','ranger','SKILL',1,'COMMON',block=8,draw=1,upgradeBlock=3)
card('volley','Volley','ranger','ATTACK',2,'COMMON',damage=5,hits=3,upgradeDamage=2)
card('mark','Mark Prey','ranger','ATTACK',0,'UNCOMMON',damage=3,vulnerable=1,upgradeDamage=2,upgradeMagic=1)
card('focus','Focus Shot','ranger','ATTACK',1,'UNCOMMON',damage=12,upgradeDamage=4)
card('evasion','Evasion','ranger','POWER',1,'RARE',dexterity=3,upgradeMagic=1)
card('snipe','Snipe','ranger','ATTACK',2,'RARE',damage=26,exhaust=True,upgradeDamage=8)
sheet('cards', cards)
sheet('decks',[dict(id=c['id'],classId=c['id'],cards=[c['id']+'_strike']*5+[c['id']+'_guard']*4+[c['signature']]) for c in classes])
sheet('manor',[
    dict(id='vitality',name='Living Quarters',effect='hp',perLevel=4,maxLevel=5,baseCost=40,costStep=25,requires='none',summary='+4 maximum health per level.'),
    dict(id='smith',name='Blacksmith',effect='strength',perLevel=1,maxLevel=3,baseCost=90,costStep=80,requires='vitality',summary='+1 Strength per level.'),
    dict(id='armory',name='Armory',effect='dexterity',perLevel=1,maxLevel=3,baseCost=90,costStep=80,requires='vitality',summary='+1 Dexterity per level.'),
    dict(id='vault',name='Treasury',effect='gold',perLevel=10,maxLevel=4,baseCost=60,costStep=35,requires='none',summary='+10% legacy earnings per level.'),
    dict(id='library',name='Library',effect='draw',perLevel=1,maxLevel=1,baseCost=180,costStep=0,requires='armory',summary='+1 card drawn each turn.'),
    dict(id='garden',name='Healing Garden',effect='heal',perLevel=1,maxLevel=4,baseCost=60,costStep=35,requires='none',summary='+1 healing after combat per level.'),
])
sheet('systems',[
    dict(id='lineage',hook='run_start',amount=3,enabled=True,summary='Random first heir; later choose one of three, each with two distinct traits.'),
    dict(id='legacy',hook='run_end',amount=8,enabled=True,summary='Legacy crowns awarded per cleared floor, plus one-quarter of remaining run gold.'),
    dict(id='victory',hook='run_end',amount=120,enabled=True,summary='Victory adds 120 legacy crowns. Each run settles at most once.'),
    dict(id='quickstart',hook='first_room',amount=1,enabled=True,summary='First run skips Neow and enters a normal floor-one fight.'),
    dict(id='family',hook='manor',amount=4,enabled=True,summary='Name the family and choose one of four banner colors between runs.'),
    dict(id='source',hook='launch',amount=1,enabled=True,summary='Require Rogue Legacy 2; read genuine class and trait sprites into a local cache at launch.'),
    dict(id='persistence',hook='save',amount=1,enabled=True,summary='Atomic profile saves, backup recovery, and a unique run identity.'),
])
sheet('assets',[dict(id=x,sourceFile='resources.assets',sourceType='Sprite',sourceName=x,output=x+'.png') for x in sorted({c['asset'] for c in classes}|{t[2] for t in traits}|{'KnightEmote','PlayerHUD_ResourcesCoinIcon_Texture'})])
sheet('hooks',[
    dict(id='content',api='EditCardsSubscriber',system='source',implementation='HeirMod.receiveEditCards'),
    dict(id='character',api='PostInitializeSubscriber',system='lineage',implementation='HeirMod.receivePostInitialize'),
    dict(id='start',api='PostDungeonInitializeSubscriber',system='lineage',implementation='HeirMod.receivePostDungeonInitialize'),
    dict(id='death',api='PostDeathSubscriber',system='legacy',implementation='HeirMod.receivePostDeath'),
    dict(id='victory',api='VictoryScreen.constructor',system='victory',implementation='Hooks.Victory'),
    dict(id='battle',api='PostBattleSubscriber',system='legacy',implementation='HeirMod.receivePostBattle'),
    dict(id='turn',api='OnPlayerTurnStartSubscriber',system='lineage',implementation='HeirMod.receiveOnPlayerTurnStart'),
    dict(id='ui_update',api='PostUpdateSubscriber',system='family',implementation='HeirMod.receivePostUpdate'),
    dict(id='ui_render',api='PostRenderSubscriber',system='family',implementation='HeirMod.receivePostRender'),
    dict(id='save',api='CustomSavable',system='persistence',implementation='HeirMod.onSave'),
    dict(id='first_room',api='Exordium.constructor',system='quickstart',implementation='Hooks.QuickStart'),
])
print('Wrote 8 design sheets.')
