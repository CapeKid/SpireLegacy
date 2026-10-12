"""61-trait native identity/stat map plus risky paid-card interactions."""
import hashlib,json,subprocess,sys,time
from pathlib import Path
from oracle_protocol import call
root=Path(__file__).resolve().parents[1];profile=Path(sys.argv[1]).resolve();tag=sys.argv[2]
expected=hashlib.sha256((root/'sts2/bin/Release/net9.0/SpireLegacy.dll').read_bytes()).hexdigest()
output=root/'private/sts2-evidence'/f'trait-audit-{tag}.json';evidence=[]
assert not output.exists()
def send(command,**args):
    record=call(profile,dict(command=command,**args),timeout=90,expected_hash=expected)
    evidence.append(record);output.parent.mkdir(exist_ok=True);output.write_text(json.dumps(evidence,indent=2))
    r=record['response'];assert r['ok'],r
    assert {m['id']:m['version'] for m in r['metadata']['mods']}=={'BaseLib':'v3.4.7','SpireLegacy':'1.0.29'}
    return r['result']
def ready():
    deadline=time.monotonic()+45
    while time.monotonic()<deadline:
        state=send('state')
        if state['phase']=='Play':return state
        time.sleep(.15)
    raise TimeoutError('Trait audit playable phase')
def start(cls,traits,quick=True):
    send('start',**{'class':cls,'traits':traits,'quick':quick,'manorTier':0,'seed':'SPIRELEGACY-TRAITS-51'});return ready() if quick else send('state')
start('knight',[])
transfers=send('trait-transfer')
if 'baseline' in tag:
    assert any(not r['rebound'] for r in transfers),transfers
    send('abandon');print(tag,'reproduced stale recipient inheritance',flush=True);sys.exit(0)
assert all(r['rebound'] and r['returned'] for r in transfers),transfers
traits=json.loads((root/'sheets/traits.json').read_text())
for index,trait in enumerate(traits):
    cls=['knight','mage','ranger'][index%3]
    start(cls,[trait['id']]);result=send('trait-audit');assert result['mutations']==36,result
    look=send('trait-appearance')
    assert look and any(v['classId']==cls and v['visible'] for v in look)
    if trait['effect']=='rainbow':assert any(v['rainbow'] and v['motes']==6 for v in look)
    if trait['effect']=='gray':assert any(v['gray'] for v in look)
    if trait['effect']=='sepia':assert any(v['sepia'] for v in look)
    if trait['effect'] in ['nature','blue','medium','festive']:assert any(trait['effect'] in v['accents'] for v in look)
    if index%10==9:print(tag,index+1,'trait probes pass',flush=True)
for cls in ['knight','mage','ranger']:
    for pair in [['megahealth','cheeronkills'],['superhealer','nomeat'],['cantattack','breakpropsformana']]:
        start(cls,pair);send('trait-audit')
        for upgraded in [False,True]:
            played=send('expansion-play',card=cls+'_strike',upgrade=upgraded)
            assert played['pile']=='Discard' and not played['tableNodePresent']
        send('save');send('resume');ready();send('trait-audit')
    start(cls,['megahealth','cheeronkills'],quick=False)
    send('enter',room='Event',event='Neow');state=send('state');assert state['hp']==state['maxHp'] and state['hp']>0
send('abandon')
# Existing native interaction regressions retain their complete private diagnostics.
for driver in ['sts2-trait-check.py','sts2-inert-trait-check.py']:
    subprocess.run([sys.executable,str(root/'scripts'/driver),str(profile),tag],check=True)
print(tag,'61 traits, 9 risky pairs, all classes paid base/upgraded/save, native startup and existing interaction probes pass',flush=True)
