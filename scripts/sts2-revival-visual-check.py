"""Exercise real automatic Fairy revival and ordinary death for every Heir class."""
import hashlib,json,sys,time
from pathlib import Path
from oracle_protocol import call
root=Path(__file__).resolve().parents[1]
profile=Path(sys.argv[1]).resolve()
expect_stuck='--expect-stuck' in sys.argv[2:]
expected=hashlib.sha256((root/'sts2/bin/Release/net9.0/SpireLegacy.dll').read_bytes()).hexdigest()
records=[]
def send(command,**args):
    record=call(profile,dict(command=command,**args),timeout=90,expected_hash=expected)
    records.append(record)
    (profile/'revival-receipts.json').write_text(json.dumps(records,indent=2))
    reply=record['response'];assert reply['ok'],reply
    return reply['result']
def visual():
    values=send('visual');assert len(values)==1,values
    return values[0]
def start(cls):
    send('start',**{'class':cls,'traits':[],'quick':True,'manorTier':0,'ascension':0,'seed':'FAIRY-REVIVAL'})
    for _ in range(150):
        if send('state')['phase']=='Play':return
        time.sleep(.1)
    raise TimeoutError('combat')
for cls in ['ranger','knight','mage']:
    start(cls)
    initial=visual();assert initial['pose']=='idle',initial
    prepared=send('revival-potion-prepare')
    send('trait-hit',amount=999,unpowered=True)
    time.sleep(1.2)
    revived=visual()
    assert revived['hp']>0 and revived['visible'],revived
    assert not any('FAIRY' in (slot or '') for slot in send('potion-slots'))
    assert revived['pose']==('dead' if expect_stuck else 'idle'),revived
    assert 'dead' in revived['events'],'Fixture never observed the lethal zero-HP pose'
    if not expect_stuck:
        assert revived['scale']==initial['scale'],(initial,revived)
        send('play',card=cls+'_strike')
        time.sleep(.8)
        after=visual();assert after['pose']=='idle' and 'attack' in after['events'],after
    send('screenshot',name=cls+'-fairy-revival')
    print(cls,'revived',revived['hp'],'pose',revived['pose'],flush=True)
    send('abandon')
    if not expect_stuck:
        start(cls)
        send('potion-clear')
        send('trait-hit',amount=999,unpowered=True)
        time.sleep(1.2)
        dead=visual();assert dead['hp']==0 and dead['pose']=='dead',dead
        final_frame=dead['frame'];time.sleep(.4)
        held=visual();assert held['pose']=='dead' and held['frame']==final_frame,held
        send('abandon')
print('Baseline reproduced for all classes.' if expect_stuck else 'Fairy revival, post-revival attack and real-death pose passed for all classes.')
