"""Export the authoring sheets into a reviewable player card catalogue."""
import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
read=lambda name:json.loads((ROOT/'sheets'/f'{name}.json').read_text())
powers={r['id']:r['summary'] for r in read('power_effects')}
special={r['id']:r['summary'] for r in read('special_effects')}
lines=['# Spire Legacy card catalogue','',
       'Each class has 82 rewards on regular (20 common, 36 uncommon, 26 rare), or 85 on public-beta (20 common, 38 uncommon, 27 rare), plus 3 basics. The 34 shared family cards count toward every class. Rows marked Public-beta are added only on that branch. Historical cards remain loadable by stable ID. Values below are base cards before inherited traits and combat powers.','']
members=read('card_pools')+read('branch_cards')
beta={r['cardId'] for r in read('branch_cards')}
for cls in read('classes'):
    pool={r['cardId']:r['scope'] for r in members if r['classId']==cls['id']}
    lines.extend(['## '+cls['name'],'','| Card | Rarity | Type | Energy | Effects |','| --- | --- | --- | ---: | --- |'])
    for row in read('cards'):
        if row['id'] not in pool:continue
        effects=['Shared family card' if pool[row['id']]=='shared' else 'Class unique']
        if row['id'] in beta:effects.append('Public-beta only')
        if row['damage']:effects.append(str(row['damage'])+' damage'+(' ×'+str(row['hits']) if row['hits']>1 else '')+(' to all enemies' if row['aoe'] else ''))
        for key,label in [('block','Block'),('draw','draw'),('discard','discard'),('energy','Energy'),('strength','Strength'),('dexterity','Dexterity'),('weak','Weak'),('vulnerable','Vulnerable'),('poison','Poison'),('vigor','next Attack damage'),('thorns','Thorns'),('plated','Plated Armor'),('heal','healing'),('hpLoss','HP cost'),('nextDraw','draw next turn'),('nextEnergy','Energy next turn'),('nextBlock','Block next turn')]:
            if key=='draw' and row['special']=='scry':effects.append(special['scry'].replace('{n}',str(row['specialAmount'])))
            if row[key]:effects.append(str(row[key])+' '+label+(' to all enemies' if row['aoe'] and key in ('weak','vulnerable','poison') else ''))
        if row['power'] in powers:effects.append(powers[row['power']].replace('!M!',str(row['magic'])))
        if row['power']=='reservoir':effects.append('Each Arcane Charge adds '+str(row['magic'])+' extra damage to every hit of your next Attack.')
        if row['power']=='quiver':effects.append('Hunter Rhythm draws '+str(row['magic'])+' extra card(s).')
        if row['special'] not in ('none','scry'):
            text=special[row['special']].replace('{n}',str(row['specialAmount']))
            if row['special'] in ('block_damage','chain_damage'):
                text=text.rstrip('.')+(' to all enemies' if row['aoe'] else '')+(' '+str(row['hits'])+' times' if row['hits']>1 else '')+'.'
            effects.append(text)
        for key,label in [('retain','Retain'),('ethereal','Ethereal'),('innate','Innate'),('exhaust','Exhaust')]:
            if row[key]:effects.append(label)
        lines.append('| '+row['name']+' | '+row['rarity'].title()+' | '+row['type'].title()+' | '+str(row['cost'])+' | '+'; '.join(effects)+' |')
    lines.append('')
(ROOT/'docs/card-catalogue.md').write_text('\n'.join(lines).rstrip()+'\n',encoding='utf-8')
print('Exported',len({r['cardId'] for r in members}),'active cards across three pools')
