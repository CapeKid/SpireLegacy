"""Historical 0.2.0 recipes for the 75-card expansion. Current additions use expand_build_choices.py. No game assets are copied."""
import json, re, hashlib
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
read=lambda name:json.loads((ROOT/'sheets'/f'{name}.json').read_text())
def write(name,rows):(ROOT/'sheets'/f'{name}.json').write_text(json.dumps(rows,indent=2)+'\n')

# name | type (A/S/P) | cost | effects. Rarity comes from the enclosing tier.
# These are individually authored combinations, not a numeric-variant generator.
RECIPES={
'knight':{
'COMMON':'''Pommel Knock|A|1|damage=6 weak=1
Spear Thrust|A|1|damage=9 vulnerable=1
Sweeping Edge|A|1|damage=5 aoe=True
Twin Cut|A|1|damage=4 hits=2
Shoulder Check|A|0|damage=3 block=3
Tower Guard|S|1|block=8 retain=True
Tactical Pause|S|1|block=4 draw=1
War Cry|S|0|draw=1 exhaust=True
Iron Resolve|S|1|block=6 vigor=3
Blood Price|S|0|energy=1 hpLoss=3
Brave Advance|A|1|damage=7 nextEnergy=1
Guard Break|A|2|damage=15 vulnerable=2
Long Reach|A|1|damage=6 draw=1
Field Rations|S|1|heal=3 exhaust=True
Shield Spikes|S|1|block=5 thorns=2 exhaust=True
Reforge|S|1|block=5 special=exhaust_one specialAmount=1 draw=1
Armored Feint|A|0|damage=4 weak=1 exhaust=True''',
'UNCOMMON':'''Shield Slam|A|1|special=block_damage
Cross Slash|A|2|damage=7 hits=3
Whirling Blade|A|2|damage=9 aoe=True
Executioner Axe|A|2|damage=13 special=execute specialAmount=2
Counter Lunge|A|1|damage=9 special=guard_bonus specialAmount=6
Banner Charge|A|1|damage=8 strength=1 exhaust=True
Merciless|A|1|damage=8 weak=2 vulnerable=2 exhaust=True
Crushing Grip|A|2|damage=18 weak=2
Rallying Strike|A|1|damage=9 draw=2 discard=1
Bloodied Blade|A|0|damage=12 hpLoss=3
Knight Hammer|A|2|damage=24 exhaust=True
Serrated Spear|A|1|damage=7 poison=3
Sword Dance|A|1|damage=3 hits=4
Castle Cleaver|A|3|damage=26 aoe=True
Shield Toss|A|1|damage=8 aoe=True weak=1
Hold the Line|S|2|block=18 retain=True
Second Wind|S|1|special=exhaust_one specialAmount=2 block=12
Battle Meditation|S|1|draw=3 discard=1
Reinforce|S|1|block=7 nextBlock=7
Tempered Steel|S|0|vigor=6 exhaust=True
Double Guard|S|1|special=double_block exhaust=True
Recover Arms|S|1|special=retrieve specialAmount=1 block=3
Clean Armor|S|1|special=cleanse block=6 exhaust=True
First Aid|S|1|heal=5 block=4 exhaust=True
Last Stand|S|0|block=12 hpLoss=3 exhaust=True
Arms Vault|S|1|special=exhume draw=1 exhaust=True
Iron Skin|S|2|plated=4 block=5 exhaust=True
Bulwark|P|1|power=metallicize magic=3
Battle Rhythm|P|1|power=juggernaut magic=3
Forged in Pain|P|1|power=rupture magic=1
Burnished Legacy|P|1|power=feel_no_pain magic=3
Vengeful Armor|P|1|power=thorns magic=3
Veteran Training|P|1|power=brutality magic=1''',
'RARE':'''Royal Execution|A|3|damage=32 special=execute specialAmount=2 exhaust=True
Meteor Hammer|A|3|damage=12 hits=3
Kingbreaker|A|2|damage=20 vulnerable=3 weak=3
Crimson Sweep|A|2|damage=17 aoe=True hpLoss=4
Unbroken Wall|S|2|block=30 exhaust=True
Guardian Angel|S|1|power=buffer magic=1 exhaust=True
Battlefield Recovery|S|2|heal=10 draw=2 exhaust=True
Royal Arsenal|S|0|energy=2 draw=2 exhaust=True
Perfect Counter|S|1|power=double_tap magic=1 block=8 exhaust=True
Living Fortress|P|3|power=barricade magic=1
Ancestral Fury|P|3|power=demon_form magic=2
Forge Memory|P|2|power=dark_embrace magic=1
Siege Resolve|P|2|power=berserk magic=1 hpLoss=3'''
},
'mage':{
'COMMON':'''Ember Dart|A|0|damage=4
Ice Shard|A|1|damage=6 weak=1
Arcane Missile|A|1|damage=3 hits=3 special=random_attack
Witchfire|A|1|damage=5 poison=2
Shock Lance|A|1|damage=8 vulnerable=1
Frost Ring|A|1|damage=4 aoe=True weak=1
Rune Bolt|A|1|damage=6 draw=1
Hot Coals|A|1|damage=10 exhaust=True
Mana Shield|S|1|block=6 nextEnergy=1
Cold Read|S|0|draw=1 discard=1
Spell Notes|S|1|draw=2 discard=1 retain=True
Crystal Guard|S|1|block=8 retain=True
Kindle|S|0|special=charge_gain specialAmount=1 exhaust=True
Ley Pulse|S|1|energy=2 exhaust=True
Potion Pouch|S|1|heal=3 exhaust=True
Rune Scrub|S|1|block=4 special=exhaust_one specialAmount=1
Hasty Formula|A|0|damage=6 discard=1''',
'UNCOMMON':'''Thunderchain|A|1|damage=3 hits=4 special=random_attack
Inferno Wave|A|2|damage=14 aoe=True
Frozen Spear|A|2|damage=14 weak=3
Prismatic Ray|A|1|damage=10 vulnerable=2
Venom Rune|A|1|damage=6 poison=5
Cinder Volley|A|2|damage=6 hits=3
Mystic Echo|A|1|damage=7 draw=2 discard=1
Mana Burn|A|0|damage=13 hpLoss=3
Meteor Fragment|A|2|damage=23 exhaust=True
Echoing Stars|A|1|damage=5 hits=2 aoe=True
Fracture Rune|A|1|damage=8 weak=1 vulnerable=1
Ice Needle|A|0|damage=5 weak=1 exhaust=True
Rune Detonation|A|1|damage=12 retain=True
Toxic Cloud|S|2|poison=5 aoe=True
Crystal Shell|S|2|block=17 nextBlock=5
Arcane Refill|S|1|draw=3 discard=1
Overcharge|S|0|special=charge_gain specialAmount=2 hpLoss=3
Rekindle|S|1|special=exhume draw=1 exhaust=True
Rewrite|S|1|special=exhaust_one specialAmount=1 draw=2
Memory Prism|S|1|special=retrieve specialAmount=1 retain=True
Cleanse Sigil|S|1|special=cleanse block=5 exhaust=True
Spellweave|S|1|nextDraw=2 nextEnergy=1
Time Pocket|S|1|power=equilibrium magic=1 draw=1
Ice Mirror|S|1|block=6 thorns=3 exhaust=True
Blood to Mana|S|0|hpLoss=4 energy=2 exhaust=True
Enchanted Ink|S|0|special=discount_hand exhaust=True
Sorcery Lesson|S|1|vigor=8 draw=1 exhaust=True
Rune Storm|P|1|power=panache magic=8
Spell Threads|P|1|power=thousand_cuts magic=1
Arcane Lens|P|1|power=tools magic=1
Frost Mantle|P|1|power=metallicize magic=3
Secret Thesis|P|1|power=dark_embrace magic=1
Toxic Theory|P|1|power=sadistic magic=3''',
'RARE':'''Celestial Spear|A|3|damage=42 exhaust=True
Starfall|A|3|damage=9 hits=3 aoe=True
Soulfire|A|2|damage=18 poison=8
Spell Cascade|A|2|damage=4 hits=6 special=random_attack
Perfect Ward|S|2|power=buffer magic=2 exhaust=True
Mana Fountain|S|0|energy=3 exhaust=True
Grand Thesis|S|2|draw=5 discard=1
Hourglass|S|2|special=free_hand exhaust=True
Transmutation|S|1|special=exhaust_one specialAmount=2 energy=2 draw=2 exhaust=True
Elixir of Life|S|2|heal=10 exhaust=True
Infinite Script|P|2|power=brutality magic=2
Arcane Conductor|P|2|power=berserk magic=1 hpLoss=3
Astral Mantle|P|2|power=after_image magic=1'''
},
'ranger':{
'COMMON':'''Quick Nock|A|0|damage=4
Barbed Arrow|A|1|damage=7 poison=2
Pinning Shot|A|1|damage=6 weak=1
Piercing Flight|A|1|damage=8 vulnerable=1
Scattershot|A|1|damage=4 aoe=True
Hidden Knife|A|0|damage=3 draw=1 exhaust=True
Tracking Shot|A|1|damage=6 draw=1
Snap Shot|A|0|damage=7 discard=1
Camouflage|S|1|block=8 retain=True
Trail Sense|S|1|draw=2 discard=1
Loose Quiver|S|0|draw=1 discard=1
Fleet Step|S|1|block=5 nextEnergy=1
Hunting Salve|S|1|heal=3 exhaust=True
Poison Tip|S|1|poison=4
Watch the Wind|S|1|block=4 special=scry specialAmount=3
Ready Arrow|S|0|vigor=4 exhaust=True
Tripwire|S|1|block=5 weak=1''',
'UNCOMMON':'''Ambush|A|1|damage=9 special=marked_bonus specialAmount=7 retain=True
Fan of Arrows|A|2|damage=8 aoe=True hits=2
Rapid Fire|A|1|damage=3 hits=4
Razor Fletching|A|1|damage=7 poison=5
Crippling Bolt|A|2|damage=15 weak=3
Predator Shot|A|2|damage=14 special=execute specialAmount=2
Running Volley|A|1|damage=5 hits=2 block=4
Finishing Flurry|A|1|special=chain_damage specialAmount=4
Deadeye|A|2|damage=19 vulnerable=2
Shadow Arrow|A|0|damage=11 hpLoss=2
Forest Sweep|A|1|damage=6 aoe=True weak=1
Hooked Arrow|A|1|damage=8 special=retrieve specialAmount=1
Ricochet|A|1|damage=4 hits=3 special=random_attack
Snakebite|A|1|damage=4 poison=7 exhaust=True
Feather Step|S|0|block=6 discard=1
Reposition|S|1|block=7 draw=2 discard=1
Deep Cover|S|2|block=18 retain=True
Scouting|S|0|special=scry specialAmount=4 draw=1
Reclaim Arrows|S|1|special=retrieve specialAmount=2
Quiet Camp|S|1|heal=5 block=3 exhaust=True
Venom Flask|S|2|poison=5 aoe=True
Double Dose|S|1|special=double_poison exhaust=True
Escape Route|S|0|energy=1 discard=2
Pocket Arsenal|S|1|special=discount_hand draw=1 exhaust=True
Patient Hunter|S|1|nextDraw=2 vigor=5
Clean Trail|S|1|special=cleanse block=4 exhaust=True
Thorn Cloak|P|1|power=thorns magic=3
Ghost Steps|P|1|power=after_image magic=1
Ranger Kit|P|1|power=tools magic=1
Measured Breath|P|1|power=well_laid magic=1
Hunting Tactics|P|1|power=panache magic=8
Cruel Precision|P|1|power=sadistic magic=3''',
'RARE':'''Storm of Arrows|A|3|damage=6 hits=4 aoe=True
Assassinate|A|2|damage=24 special=marked_bonus specialAmount=12 exhaust=True
Heartseeker|A|2|damage=10 hits=3 retain=True
Black Arrow|A|2|damage=18 poison=8
Perfect Escape|S|1|power=buffer magic=1 draw=2 exhaust=True
Master Plan|S|2|special=free_hand draw=2 exhaust=True
Serpent Oil|S|1|special=double_poison aoe=True exhaust=True
Second Quiver|S|0|energy=2 draw=2 exhaust=True
Forest Remedy|S|2|heal=10 special=cleanse exhaust=True
Poisoned Arsenal|P|2|power=envenom magic=1
Unseen Hunter|P|2|power=after_image magic=2
Endless Pursuit|P|3|power=demon_form magic=2
Perfect Timing|P|2|power=berserk magic=1 hpLoss=3'''
}}

EXTRA=dict(aoe=False,retain=False,ethereal=False,innate=False,upgradeCost=False,hpLoss=0,heal=0,poison=0,vigor=0,thorns=0,plated=0,nextEnergy=0,nextDraw=0,nextBlock=0,special='none',specialAmount=0,upgradeSpecial=0)
POWER_TEXT={
'metallicize':'At the end of your turn, gain !M! Block.',
'juggernaut':'Whenever you gain Block, deal !M! damage to a random enemy.',
'rupture':'Whenever you lose HP from a card, gain !M! Strength.',
'feel_no_pain':'Whenever you Exhaust a card, gain !M! Block.',
'thorns':'Whenever an enemy attacks you, deal !M! damage to it.',
'brutality':'At the start of your turn, lose !M! HP and draw !M! card(s).',
'barricade':'Your Block is not removed at the start of your turn.',
'demon_form':'At the start of your turn, gain !M! Strength.',
'dark_embrace':'Whenever you Exhaust a card, draw !M! card(s).',
'berserk':'At the start of your turn, gain !M! Energy.',
'buffer':'Prevent the next !M! time(s) you would lose HP.',
'double_tap':'Your next !M! Attack(s) this turn are played twice.',
'equilibrium':'Retain your hand for !M! turn(s).',
'panache':'Every 5 cards played in a turn, deal !M! damage to ALL enemies.',
'thousand_cuts':'Whenever you play a card, deal !M! damage to ALL enemies.',
'tools':'At the start of your turn, draw !M! card(s), then discard !M! card(s).',
'sadistic':'Whenever you apply a debuff to an enemy, deal !M! damage to it.',
'after_image':'Whenever you play a card, gain !M! Block.',
'well_laid':'At the end of your turn, Retain up to !M! card(s).',
'envenom':'Whenever an Attack deals unblocked damage, apply !M! Poison.'}
SPECIAL_TEXT={
'block_damage':'Deal damage equal to your current Block.',
'execute':'Deal {n} times damage if the target is at or below half HP.',
'guard_bonus':'Deal {n} additional damage if you have Block.',
'marked_bonus':'Deal {n} additional damage if the target is Vulnerable or Poisoned.',
'random_attack':'Hits target random enemies.',
'exhaust_one':'Exhaust {n} card(s) from your hand.',
'double_block':'Double your current Block.',
'retrieve':'Return {n} card(s) from your discard pile to your hand.',
'cleanse':'Remove all debuffs from yourself.',
'exhume':'Return a card from your Exhaust pile to your hand.',
'charge_gain':'Gain {n} Arcane Charge(s), up to the normal charge cap.',
'discount_hand':'Cards currently in your hand cost 1 less this turn.',
'free_hand':'Cards currently in your hand cost 0 this turn.',
'scry':'Scry {n}.',
'double_poison':'Double Poison on the target.',
'chain_damage':'Deal {n} damage for each OTHER Attack played this turn.'}

def main():
    cards=read('cards');art=read('card_art');classes={r['id']:r for r in read('classes')}
    initial_ids={r['id'] for r in cards};added=[]
    prefix=art[0]['prompt'].split('\nCard:')[0]
    for row in cards:
        for key,value in EXTRA.items():row.setdefault(key,value)
    for cls,tiers in RECIPES.items():
        for rarity,lines in tiers.items():
            for line in lines.strip().splitlines():
                name,kind,cost,values=line.split('|');key=cls+'_'+re.sub(r'[^a-z0-9]+','_',name.lower()).strip('_')
                if key in initial_ids:continue
                row=dict(cards[0]);row.update(EXTRA)
                for field in ('damage','block','draw','energy','strength','dexterity','weak','vulnerable','upgradeDamage','upgradeBlock','upgradeMagic','magic','discard'):row[field]=0
                row.update(id=key,name=name,classId=cls,type={'A':'ATTACK','S':'SKILL','P':'POWER'}[kind],cost=int(cost),rarity=rarity,asset=classes[cls]['asset'],art=key,power='none',exhaust=False,hits=1)
                for token in values.split():
                    field,value=token.split('=');row[field]=True if value=='True' else int(value) if re.fullmatch(r'-?\d+',value) else value
                row['upgradeDamage']=3 if row['damage'] else 0
                row['upgradeBlock']=3 if row['block'] else 0
                row['upgradeMagic']=1 if any(row[f] for f in ('draw','energy','strength','dexterity','weak','vulnerable','magic','poison','vigor','thorns','plated','nextEnergy','nextDraw','nextBlock','heal')) else 0
                row['upgradeSpecial']=1 if row['special'] in ('scry','guard_bonus','marked_bonus','chain_damage','charge_gain') else 0
                if row['special']=='retrieve' and not any(row[f] for f in ('upgradeDamage','upgradeBlock','upgradeMagic')):row['upgradeSpecial']=1
                row['upgradeCost']=row['cost']>0 and not(row['upgradeDamage'] or row['upgradeBlock'] or row['upgradeMagic'] or row['upgradeSpecial'])
                if row['power'] in ('barricade','brutality'):row['upgradeMagic']=0;row['upgradeCost']=True
                cards.append(row);added.append(row)
                portrait={'knight':'silver-armored red-plumed knight','mage':'small bearded mage in burgundy robes','ranger':'small green-hooded bow-wielding ranger'}[cls]
                effects=[]
                for f,label in [('damage','attack damage'),('block','defensive Block'),('draw','draw cards'),('energy','gain energy'),('poison','Poison on enemy'),('weak','Weak on enemy'),('vulnerable','Vulnerable on enemy'),('heal','heal HP'),('vigor','next attack damage'),('thorns','reflect damage'),('plated','regenerating armor'),('nextEnergy','energy next turn'),('nextDraw','draw next turn'),('nextBlock','Block next turn')]:
                    if row[f]:effects.append(label)
                if row['power']!='none':effects.append(POWER_TEXT.get(row['power'],row['power']).replace('!M!','enhanced'))
                if row['special']!='none':effects.append(SPECIAL_TEXT[row['special']].format(n=row['specialAmount']))
                if row['aoe']:effects.append('affects every enemy')
                if row['hits']>1:effects.append('multiple hits')
                if row['retain']:effects.append('prepared and kept for later')
                cue='; '.join(effects)
                prompt=prefix+'\nCard: '+name+'. Depict an original, distinct action scene illustrating '+name+' with a '+portrait+'. Mechanics to communicate visually: '+cue+'. Use a specific prop, pose and clear symbolic action matching this card, not a generic standing portrait. No writing or numbers. Warm flat parchment backdrop. Keep entire subject crop-safe.'
                art.append(dict(id=key,cardId=key,name=name,path=f'heir/cards/{key}.png',width=250,height=190,prompt=prompt,generator='built-in imagegen',sha256='pending'))
    # Give the existing uncommon smoke card a distinct control role.
    for row in cards:
        if row['id']=='smokescreen':row['weak']=1;row['aoe']=True
    for cls in classes:
        counts={r:sum(c['classId']==cls and c['rarity']==r for c in cards) for r in ('BASIC','COMMON','UNCOMMON','RARE')}
        assert counts==dict(BASIC=3,COMMON=20,UNCOMMON=36,RARE=16),(cls,counts)
    write('cards',cards);write('card_art',art)
    write('power_effects',[dict(id=k,summary=v) for k,v in POWER_TEXT.items()])
    write('special_effects',[dict(id=k,summary=v) for k,v in SPECIAL_TEXT.items()])
    (ROOT/'build/expansion-art-plan.json').write_text(json.dumps([dict(id=r['id'],classId=r['classId'],prompt=next(a['prompt'] for a in art if a['id']==r['art'])) for r in added],indent=2))
    print('Cards:',len(cards),'New:',len(added),'Each class: 75 (3 basic, 20 common, 36 uncommon, 16 rare)')

if __name__=='__main__':main()
