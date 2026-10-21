"""Activate and balance original historical cards for native regular/beta pool parity.

All identities and illustrations already existed; preserve saves and artwork provenance.
Run against current checked-in sheets, before sts2-generate.py. Historical
reconstruction scripts are retired; do not regenerate their old baseline first.
"""
import json
from pathlib import Path
from balance_policy import CARD_OVERRIDES
root=Path(__file__).resolve().parents[1]
def read(name):return json.loads((root/'sheets'/f'{name}.json').read_text(encoding='utf-8-sig'))
def write(name,rows):(root/'sheets'/f'{name}.json').write_text(json.dumps(rows,indent=2)+'\n',encoding='utf-8')
# ID, eligibility, strategy, native comparison, complete gameplay overrides.
plan=[
 ('oath','shared','strength','Inflame: slower setup, smaller Strength gain, one-use attack.',dict(type='ATTACK',cost=2,damage=6,hits=2,strength=1,exhaust=True,upgradeDamage=1,upgradeMagic=1)),
 ('knight_ancestral_salvage','shared','exhaust recovery','Attack-only Exhaust recovery with Vigor on successful return; shared Arms Vault remains general immediate recovery.',dict(cost=2,block=6,special='exhume',specialAmount=1,exhaust=True,upgradeBlock=3)),
 ('mage_eternal_bookmark','shared','retention','Well-Laid Plans: pay 2 Energy to retain two cards; upgrade lowers setup cost.',dict(type='POWER',cost=2,power='well_laid',magic=2,upgradeCost=True)),
 ('ranger_forest_remedy','shared','healing and cleanse','Paid, one-use small heal and debuff removal; respects inherited healing and Hero Complex.',dict(cost=2,heal=4,special='cleanse',exhaust=True,upgradeCost=True)),
 ('knight_royal_execution','knight','finisher','Bludgeon: lower damage unless the enemy is already at half HP; no Energy refund.',dict(type='ATTACK',cost=3,damage=16,special='execute',specialAmount=2,upgradeDamage=4)),
 ('knight_meteor_hammer','knight','multi-hit and Weak','Twin Strike: two-Energy, three-hit, one-use attack with one turn of Weak; upgrade adds only one per hit.',dict(type='ATTACK',cost=2,damage=5,hits=3,weak=1,exhaust=True,upgradeDamage=1)),
 ('knight_unbroken_wall','knight','defense and exhaust','Impervious: lower one-use Block budget, same two-Energy cost.',dict(cost=2,block=18,exhaust=True,upgradeBlock=5)),
 ('knight_battlefield_recovery','knight','HP and draw','Offering: paid one-use recovery with smaller draw, small healing and no Energy gain.',dict(cost=2,draw=2,heal=4,exhaust=True,upgradeMagic=1)),
 ('knight_forge_memory','knight','exhaust draw engine','Offensive exhaust engine: gain Vigor when exhausting, distinct from shared Secret Thesis draw.',dict(type='POWER',cost=2,block=4,power='dark_embrace',magic=1,upgradeCost=True)),
 ('knight_king_s_ransom','knight','HP for Energy','Offering: trades HP for Energy once; draws no cards.',dict(cost=0,energy=2,hpLoss=4,exhaust=True,upgradeMagic=1)),
 ('mage_celestial_spear','mage','heavy attack','Bludgeon: smaller repeatable damage budget, benefiting from bounded Arcane Charge.',dict(type='ATTACK',cost=3,damage=24,upgradeDamage=6)),
 ('mage_grand_thesis','mage','Arcane Flow setup','One-use draw/charge setup banks next-turn Energy to play Arcane Flow draws; one fewer immediate card, upgrade adds one card and one delayed Energy.',dict(cost=2,draw=3,special='charge_gain',specialAmount=1,exhaust=True,upgradeMagic=1,upgradeSpecial=1)),
 ('mage_hourglass','mage','temporary discounts','Bullet Time: discounts only two random eligible cards by one, once, rather than freeing the hand.',dict(cost=1,special='discount_hand',specialAmount=2,exhaust=True,upgradeSpecial=1)),
 ('mage_infinite_script','mage','HP for sustained draw','Paid setup, one HP per turn for one extra card, plus one next-turn card; neither free nor HP-neutral.',dict(type='POWER',cost=2,power='brutality',magic=1,nextDraw=1,upgradeCost=True)),
 ('mage_archmage_thesis','mage','three-Skill defense engine','Afterimage: triggers only every third Skill; paid setup includes modest initial Block.',dict(type='POWER',cost=2,block=6,power='skill_block',magic=4,upgradeBlock=3)),
 ('mage_returned_prophecy','mage','exhaust recovery and draw','Delay Exhaust recovery until next turn, with a one-turn discount; no immediate draw. Recovery-loop exclusions apply.',dict(cost=2,draw=1,special='exhume',specialAmount=1,exhaust=True,upgradeMagic=1)),
 ('ranger_heartseeker','ranger','retained multi-hit','Twin Strike: slower retained three-hit attack; per-hit upgrade stays at one.',dict(type='ATTACK',cost=2,damage=5,hits=3,retain=True,upgradeDamage=1)),
 ('ranger_serpent_oil','ranger','poison seed and multiplier','Venom Reserve is a retained multiplier; Serpent Oil adds a small Poison seed before doubling, once. Its upgrade improves only the seed.',dict(cost=2,poison=2,special='double_poison',exhaust=True,upgradeMagic=1)),
 ('ranger_second_quiver','ranger','Attack reload','Zero Energy to select up to two discarded Attacks (three upgraded), then Exhaust. Selective recovery instead of Meditate-style draw and Energy.',dict(cost=0,draw=1,energy=1,hpLoss=4,exhaust=True,upgradeMagic=1)),
 ('ranger_empty_quiver','ranger','discard and redraw','Calculated Gamble: costs one Energy, offers modest Block; redraw excludes the played card.',dict(cost=1,block=4,special='refill_hand',upgradeBlock=3)),
 ('ranger_ghost_luggage','ranger','discard defense engine','Discard banks stronger Block for next turn rather than duplicating Light Pack immediate Block.',dict(type='POWER',cost=2,block=5,power='discard_block',magic=2,upgradeMagic=1)),
 ('ranger_hunt_without_end','ranger','three-Attack Vigor engine','Tracking Rhythm: slower setup with initial Vigor; subsequent Vigor only every third Attack.',dict(type='POWER',cost=2,vigor=3,power='attack_vigor',magic=3,upgradeMagic=1)),
 ('knight_serrated_spear','beta','Vulnerable setup','Bash: smaller attack and one turn of Vulnerable at one Energy.',dict(type='ATTACK',cost=1,damage=5,vulnerable=1,upgradeDamage=2)),
 ('knight_vengeful_armor','beta','retaliation','Caltrops: smaller recurring Thorns budget with modest initial Block.',dict(type='POWER',cost=1,block=3,power='thorns',magic=2,upgradeMagic=1)),
 ('knight_castle_heart','beta','end-turn defense','Afterimage: fixed four end-turn Block instead of scaling with cards played; paid setup and modest initial defense.',dict(type='POWER',cost=2,block=5,power='metallicize',magic=4,upgradeCost=True)),
 ('mage_frozen_spear','beta','debuff setup','Uppercut: smaller damage and one turn each of Weak and Vulnerable.',dict(type='ATTACK',cost=2,damage=8,weak=1,vulnerable=1,upgradeDamage=3)),
 ('mage_cleanse_sigil','beta','cleanse defense','Cleanse and fill Arcane Charges instead of duplicating shared Clean Armor; upgrade reduces cost without a wasted charge increment.',dict(cost=1,block=3,special='cleanse',exhaust=True,upgradeBlock=3)),
 ('mage_book_of_embers','beta','exhaust damage engine','Phoenix Forge: smaller per-Exhaust damage budget, supporting Mage exhaust recovery.',dict(type='POWER',cost=2,power='exhaust_damage',magic=3,upgradeMagic=1)),
 ('ranger_scouting','beta','scry and draw','Prepared/Skim: one paid card with Scry selection, no immediate Energy gain or large draw burst.',dict(cost=1,draw=1,special='scry',specialAmount=3,upgradeSpecial=1)),
 ('ranger_pocket_arsenal','beta','draw and temporary discount','Prepared/Bullet Time: paid, one-use draw and small random hand discount.',dict(cost=1,draw=1,special='discount_hand',specialAmount=1,exhaust=True,upgradeSpecial=1)),
 ('ranger_unseen_hunter','beta','per-card defense','Afterimage: paid setup with modest initial Block; one Block per played card.',dict(type='POWER',cost=2,block=4,power='after_image',magic=1,upgradeCost=True)),
]
cards=read('cards');byid={r['id']:r for r in cards};members=read('card_pools');expansion=[];branch=[]
defaults={k:v for k,v in byid['knight_strike'].items() if k not in ('id','name','classId','rarity','asset','art')}
for key in defaults:
    defaults[key]=False if isinstance(defaults[key],bool) else 0 if isinstance(defaults[key],int) else 'none'
defaults.update(type='SKILL',hits=1,upgradeDraw=-1)
for cardid,scope,build,reference,stats in plan:
    row=byid[cardid];row.update(defaults);row.update(stats)
    rarity='UNCOMMON' if scope=='beta' and cardid not in ('knight_castle_heart','mage_book_of_embers','ranger_unseen_hunter') else 'RARE'
    row['rarity']=rarity
    expansion.append(dict(id=cardid,cardId=cardid,classId='shared' if scope=='shared' else row['classId'],availability='public-beta' if scope=='beta' else 'all',build=build,reference=reference))
    if scope=='beta':branch.append(dict(id=row['classId']+'__'+cardid,classId=row['classId'],cardId=cardid,scope='unique'))
    else:
        for cls in ('knight','mage','ranger') if scope=='shared' else (scope,):
            id=cls+'__'+cardid
            if not any(r['id']==id for r in members):members.append(dict(id=id,classId=cls,cardId=cardid,scope='shared' if scope=='shared' else 'unique'))
targets=read('pool_targets')
for row in targets:row.update(reference='StS2 regular v0.107.1: 82 rewards; public-beta v0.111.0: 85 rewards with branch additions',basic=3,common=20,uncommon=36,rare=26,total=85)
for row in cards:
    row.setdefault('upgradeDraw', -1)
    row.update(CARD_OVERRIDES.get(row['id'], {}))
write('cards',cards);write('card_pools',members);write('pool_targets',targets);write('branch_cards',branch);write('pool_expansion',expansion)
print('Activated 22 original rare cards (4 shared + 6/class) and 9 beta additions (2 uncommon + 1 rare/class); saved model IDs and illustration paths preserved.')
