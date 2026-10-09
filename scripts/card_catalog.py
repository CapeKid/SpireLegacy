"""Export the authoring sheets into a reviewable player card catalogue."""
import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
read=lambda name:json.loads((ROOT/'sheets'/f'{name}.json').read_text())
powers={r['id']:r['summary'] for r in read('power_effects')}
special={r['id']:r['summary'] for r in read('special_effects')}
lines=['# Spire Legacy card catalogue','',
       'Each class has 120 cards: 3 basics and 117 rewards (35 common, 56 uncommon, 26 rare). Each heir uses its own class library. Starter genes modify basic cards separately. Values below are the base cards before inherited traits and combat powers.','']
for cls in read('classes'):
    lines.extend(['## '+cls['name'],'','| Card | Rarity | Type | Energy | Effects |','| --- | --- | --- | ---: | --- |'])
    for row in read('cards'):
        if row['classId']!=cls['id']:continue
        effects=[]
        if row['damage']:effects.append(str(row['damage'])+' damage'+(' ×'+str(row['hits']) if row['hits']>1 else '')+(' to all enemies' if row['aoe'] else ''))
        for key,label in [('block','Block'),('draw','draw'),('discard','discard'),('energy','Energy'),('strength','Strength'),('dexterity','Dexterity'),('weak','Weak'),('vulnerable','Vulnerable'),('poison','Poison'),('vigor','next Attack damage'),('thorns','Thorns'),('plated','Plated Armor'),('heal','healing'),('hpLoss','HP cost'),('nextDraw','draw next turn'),('nextEnergy','Energy next turn'),('nextBlock','Block next turn')]:
            if row[key]:effects.append(str(row[key])+' '+label+(' to all enemies' if row['aoe'] and key in ('weak','vulnerable','poison') else ''))
        if row['power'] in powers:effects.append(powers[row['power']].replace('!M!',str(row['magic'])))
        if row['power']=='reservoir':effects.append('Each Arcane Charge adds '+str(row['magic'])+' extra attack damage.')
        if row['power']=='quiver':effects.append('Hunter Rhythm draws '+str(row['magic'])+' extra card(s).')
        if row['special']!='none':
            text=special[row['special']].replace('{n}',str(row['specialAmount']))
            if row['special'] in ('block_damage','chain_damage'):
                text=text.rstrip('.')+(' to all enemies' if row['aoe'] else '')+(' '+str(row['hits'])+' times' if row['hits']>1 else '')+'.'
            effects.append(text)
        for key,label in [('retain','Retain'),('ethereal','Ethereal'),('innate','Innate'),('exhaust','Exhaust')]:
            if row[key]:effects.append(label)
        lines.append('| '+row['name']+' | '+row['rarity'].title()+' | '+row['type'].title()+' | '+str(row['cost'])+' | '+'; '.join(effects)+' |')
    lines.append('')
(ROOT/'docs/card-catalogue.md').write_text('\n'.join(lines).rstrip()+'\n')
print('Exported',len(read('cards')),'card catalogue')
