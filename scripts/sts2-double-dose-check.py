"""Check fixed-dose poison, paid resolution and save compatibility in an owned lab."""
import hashlib,json,sys,time
from pathlib import Path
from oracle_protocol import call
root=Path(__file__).resolve().parents[1]
profile=Path(sys.argv[1]).resolve()
expected=hashlib.sha256((root/'sts2/bin/Release/net9.0/SpireLegacy.dll').read_bytes()).hexdigest()
records=[]
def send(command,**args):
    record=call(profile,dict(command=command,**args),timeout=90,expected_hash=expected)
    records.append(record)
    (profile/'double-dose-receipts.json').write_text(json.dumps(records,indent=2))
    reply=record['response'];assert reply['ok'],reply
    return reply['result']
def poison(state):
    return sum(p['amount'] for p in state['enemies'][0]['powers'] if 'poison' in p['id'].lower())
send('start',**{'class':'ranger','traits':[],'quick':True,'manorTier':0,'ascension':0,'seed':'DOUBLE-DOSE'})
for _ in range(150):
    if send('state')['phase']=='Play':break
    time.sleep(.1)
else:raise TimeoutError('combat')
for upgraded in [False,True]:
    amount=8 if upgraded else 6
    for mode in ['normal','save-copy','replay','invalid']:
        result=send('card-resolution',card='ranger_double_dose',upgrade=upgraded,mode=mode)
        if mode=='invalid':
            assert result['rejected'] and result['conserved'],result
            continue
        assert result['cost']==1 and result['energyDelta']==-1 and result['pile']=='Discard',result
        assert result['saveRoundTrip'] and not result['tableNodePresent'],result
        assert poison(send('state'))==amount*(2 if mode=='replay' else 1)
    # Establish a different existing stack with a real paid card, then use the
    # test bridge's explicitly free autoplay to distinguish addition/multiplication.
    send('card-resolution',card='ranger_poison_tip',upgrade=False,mode='normal')
    assert poison(send('state'))==4
    assert poison(send('play',card='ranger_double_dose',upgrade=upgraded))==4+amount
    assert poison(send('play',card='ranger_venom_reserve',upgrade=upgraded))==(4+amount)*2
send('abandon')
print('Double Dose base/upgraded: paid resolution, discard, save-copy, replay, invalid target, fixed addition and Venom Reserve multiplier passed.')
