"""Validate recorded native outcomes, including draws, attack totals and installed powers."""
import json,sys
from pathlib import Path
root=Path(__file__).resolve().parents[1]
cards={r['id']:r for r in json.loads((root/'sheets/cards.json').read_text())}
plan=json.loads((root/'sheets/pool_expansion.json').read_text())
power_ids=dict(well_laid='WELL_LAID_PLANS_POWER',dark_embrace='DARK_EMBRACE_POWER',
               after_image='AFTERIMAGE_POWER',thorns='THORNS_POWER')
for tag in sys.argv[1:] or ['regular','beta']:
    evidence=json.loads((root/'private/sts2-evidence'/f'pool120-{tag}.json').read_text())
    seen=set();cls=None
    for entry in evidence:
        if entry['command']=='start':cls=entry['args']['class']
        if entry['command']!='expansion-play':continue
        result=entry['result'];row=cards[result['key']];up=result['upgrade']
        amount=lambda key:row[key]+(row['upgradeMagic'] if up and row[key]>0 and key!='hpLoss' else 0)
        assert len(result['hand'])==5+amount('draw')+(row['special']=='exhume'),result
        damage=(row['damage']+(row['upgradeDamage'] if up else 0))*row['hits']
        if row['special']=='execute':damage*=row['specialAmount']
        assert result['enemyDamage']==damage,result
        powers={p['id']:p['amount'] for p in result['powers']}
        if row['power']!='none':
            identity='POWER.'+power_ids.get(row['power'],'SPIRELEGACY-P_'+row['power'].upper())
            assert powers.get(identity)==row['magic']+(row['upgradeMagic'] if up else 0),result
        for field,identity in [('strength','STRENGTH_POWER'),('vigor','VIGOR_POWER'),('nextDraw','DRAW_CARDS_NEXT_TURN_POWER')]:
            if row[field]:assert powers.get('POWER.'+identity)==amount(field),result
        if row['special']=='discount_hand':
            assert all(cost==1 for cost in result['costsBefore']),result
            assert sum(1-cost for cost in result['costsAfter'])==row['specialAmount']+(row['upgradeSpecial'] if up else 0),result
        assert result['saveRoundTrip'] and not result['tableNodePresent'],result
        assert result['energyChange']==-result['cost']+amount('energy'),result
        assert result['hpChange']==amount('heal')-row['hpLoss'],result
        assert result['blockChange']==row['block']+(row['upgradeBlock'] if up else 0),result
        assert result['pile']==(None if row['type']=='POWER' else 'Exhaust' if row['exhaust'] else 'Discard'),result
        if row['special']=='exhume':assert result['fuelReturned'],result
        if row['special']=='double_poison':assert result['poison']==(3+amount('poison'))*2,result
        if row['id']=='mage_grand_thesis':assert result['charges']==2,result
        seen.add((cls,row['id'],up))
    expected={(cls,r['cardId'],up) for cls in ['knight','mage','ranger'] for r in plan
              if r['classId'] in ['shared',cls] and (r['availability']=='all' or tag=='beta') for up in [False,True]}
    assert seen==expected,(tag,'missing',expected-seen,'extra',seen-expected)
    print(f'{tag}: {len(seen)} distinct native plays; exact draws, damage, power amounts, costs, HP, Block, poison, recovery, cleanup and serialization verified.')
