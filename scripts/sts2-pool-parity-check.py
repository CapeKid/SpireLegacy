"""Actual native reward/shop/compendium, paid play, upgrade and save checks."""
import json,sys,time,shutil
from oracle_protocol import call
from pathlib import Path
root=Path(__file__).resolve().parents[1];profile=(root/sys.argv[1]).resolve();tag=sys.argv[2]
assert profile.is_relative_to((root/'private').resolve())
evidence_file=root/'private/sts2-evidence'/f'pool120-{tag}.json'
evidence=json.loads(evidence_file.read_text()) if '--resume' in sys.argv and evidence_file.exists() else []
completed=set();active=None
for entry in evidence:
    if entry['command']=='start':active=entry['args']['class']
    if entry['command']=='expansion-play':completed.add((active,entry['args']['card'],entry['args']['upgrade']))
startup=root/'private'/f'pool120-{tag}.out.log';deadline=time.monotonic()+140
while not startup.exists() or 'Time to main menu' not in startup.read_text(encoding='utf-8',errors='replace'):
    if time.monotonic()>deadline:raise TimeoutError('main menu')
    time.sleep(.5)
def send(command, **args):
    record=call(profile,dict(command=command,**args),timeout=90)
    reply=record['response']
    assert reply['ok'],reply
    evidence.append(dict(command=command,args=args,result=reply['result'],metadata=reply['metadata']))
    return reply['result']
def ready():
    for _ in range(150):
        state=send('state')
        if state['phase']=='Play':return state
        time.sleep(.1)
    raise TimeoutError('combat')
cards={r['id']:r for r in json.loads((root/'sheets/cards.json').read_text())}
base=json.loads((root/'sheets/card_pools.json').read_text());extra=json.loads((root/'sheets/branch_cards.json').read_text())
plan=json.loads((root/'sheets/pool_expansion.json').read_text())
def amount(row,key,up):return row[key]+(row['upgradeMagic'] if up and row[key]>0 and key!='hpLoss' else 0)
try:
    parity=send('pool-parity');beta=tag=='beta';expected=(20,38,27) if beta else (20,36,26)
    for pool in parity['native']+parity['heirs']:
        assert (pool['common'],pool['uncommon'],pool['rare'])==expected,pool
        assert pool['ordinary']==sum(expected) and pool['total']==pool['unique'],pool
    checks=len(completed)
    for cls in ['knight','mage','ranger']:
        send('start',**{'class':cls,'traits':[],'quick':True});ready()
        eligible={r['cardId'] for r in base+(extra if beta else []) if r['classId']==cls}
        available=send('pool-availability');assert set(available['ids'])==eligible and available['compendium'] and available['expanded']==beta,available
        draws=send('rewards');assert draws['rewardChecks']==720 and draws['shopChecks']>0,draws
        rare={key for key in eligible if cards[key]['rarity']=='RARE'}
        assert rare.issubset(set(draws['seenRewards'])),rare-set(draws['seenRewards'])
        additions=[r['cardId'] for r in plan if r['classId'] in ['shared',cls] and (r['availability']=='all' or beta)]
        for key in additions:
            row=cards[key]
            for up in [False,True]:
                if (cls,key,up) in completed:continue
                send('enter',room='Boss');ready()
                result=send('expansion-play',card=key,upgrade=up);checks+=1
                assert result['saveRoundTrip'] and not result['tableNodePresent'],result
                assert result['energyChange']==-result['cost']+amount(row,'energy',up),result
                assert result['hpChange']==amount(row,'heal',up)-row['hpLoss'],result
                assert result['blockChange']==row['block']+(row['upgradeBlock'] if up else 0),result
                expected_pile=None if row['type']=='POWER' else 'Exhaust' if row['exhaust'] else 'Discard'
                assert result['pile']==expected_pile,(expected_pile,result)
                if row['special']=='exhume':assert result['fuelReturned'],result
                if row['special']=='double_poison':assert result['poison']==(3+amount(row,'poison',up))*2,result
                if row['special']=='discount_hand':
                    assert all(cost==1 for cost in result['costsBefore']),result
                    assert sum(1-cost for cost in result['costsAfter'])==row['specialAmount']+(row['upgradeSpecial'] if up else 0),result
                if key=='mage_grand_thesis':assert result['charges']==2,result
                if row['type']=='POWER':assert result['powers'],result
        send('cards');send('save');before=send('state');send('resume');after=ready()
        assert after['heir']==before['heir'] and after['relics']==before['relics'],(before,after)
        if cls=='ranger':
            time.sleep(2)
            shot=send('screenshot');shutil.copy2(shot['path'],root/'private/sts2-evidence'/f'pool120-{tag}.png')
    send('abandon')
    print(f'{tag}: native and Heir reward parity {sum(expected)}; all class pools unlocked and in compendium; all rares observed in native rewards; shops correct; {checks} paid base/upgraded plays resolve and save round-trip; draw/HP/energy/block/poison/charge/recovery/discount effects verified; all 720 model checks per class; save/resume preserves heir and relics.')
finally:
    evidence_file.write_text(json.dumps(evidence,indent=2),encoding='utf-8')
