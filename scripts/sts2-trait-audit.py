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
def ready(turn=1):
    deadline=time.monotonic()+45
    while time.monotonic()<deadline:
        state=send('state')
        if state['phase']=='Play' and state['turn']>=turn:return state
        time.sleep(.15)
    raise TimeoutError('Trait audit playable phase')
def start(cls,traits,quick=True):
    send('start',**{'class':cls,'traits':traits,'quick':quick,'manorTier':0,'seed':'SPIRELEGACY-TRAITS-51'});return ready() if quick else send('state')
send('trait-speed',value=8)
start('knight',[])
transfers=send('trait-transfer')
if 'baseline' in tag:
    if 'cost' not in tag:assert any(not r['rebound'] for r in transfers),transfers
    start('knight',['manacostanddamageup']);redraw=send('trait-redraw');assert not redraw['repeatedDrawStable'],redraw
    send('abandon');print(tag,'reproduced stale recipient inheritance and repeated-draw cost stacking',flush=True);sys.exit(0)
assert all(r['rebound'] and r['returned'] and r['duplicatedRecipient'] and r['preservesBuff'] for r in transfers),transfers
traits=json.loads((root/'sheets/traits.json').read_text())
if 'smoke' in tag:traits=[r for r in traits if r['id'] in ['nocolor','cantattack','manacostanddamageup','disarmonhurt','manafromhurt','vampire','cheeronkills','itemsgoflying','explosiveenemies']]
mutations=6*sum(r['rarity']=='BASIC' for r in json.loads((root/'sheets/cards.json').read_text()))
for index,trait in enumerate(traits):
    cls=['knight','mage','ranger'][index%3]
    start(cls,[trait['id']]);result=send('trait-audit');assert result['mutations']==mutations,result
    assert not any(result['scopes'].values()),result
    look=send('trait-appearance')
    assert look and any(v['classId']==cls and v['visible'] for v in look)
    if trait['effect']=='rainbow':assert any(v['rainbow'] and v['motes']==6 for v in look)
    if trait['effect']=='gray':assert any(v['gray'] for v in look)
    if trait['effect']=='sepia':assert any(v['sepia'] for v in look)
    if trait['effect'] in ['nature','blue','medium','festive']:assert any(trait['effect'] in v['accents'] for v in look)
    state=send('state')
    if trait['effect']=='costly':
        redraw=send('trait-redraw');assert redraw['repeatedDrawStable'] and redraw['first']==2
        send('turn');ready(2);redraw=send('trait-redraw',reuse=True);assert redraw['first']==2 and redraw['second']==2
    if trait['effect']=='shock':
        send('trait-hit',amount=2);draw=send('trait-redraw');assert draw['firstPile']=='Discard' and draw['secondPile']=='Hand'
    if trait['effect']=='exhausted':send('turn');assert ready(2)['energy']==2
    if trait['effect']=='turn_block':assert state['block']==trait['amount']
    if trait['effect']=='overcharge':assert state['energy']==4 and any('VULNERABLE' in p['id'] and p['amount']==1 for p in state['powers'])
    if trait['effect']=='algesia':assert send('trait-hit',amount=2)['hp']==state['hp']-4
    if trait['effect']=='hurt_weak':assert any('WEAK' in p['id'] and p['amount']==1 for p in send('trait-hit',amount=2)['powers'])
    if trait['effect']=='opening_weak':assert any('WEAK' in p['id'] and p['amount']==1 for p in state['powers'])
    if trait['effect']=='enemy_guard':assert all(e['block']==trait['amount'] for e in state['enemies'])
    if trait['effect']=='enemy_strength':assert all(any('STRENGTH' in p['id'] and p['amount']==trait['amount'] for p in e['powers']) for e in state['enemies'])
    if trait['effect']=='poison':assert all(any('POISON' in p['id'] and p['amount']==trait['amount'] for p in e['powers']) for e in state['enemies'])
    if trait['effect'] in ['treasure_gold','chest']:
        send('enter',room='Treasure');after=send('state')
        assert after['gold']==state['gold']+(trait['amount'] if trait['effect']=='treasure_gold' else 0)
        assert after['hp']==state['hp']-(trait['amount'] if trait['effect']=='chest' else 0)
    if trait['effect']=='histrionic':
        send('trait-hit',amount=2)
        assert any(v['flash']>0 for v in send('trait-appearance'))
    if trait['effect']=='pacifist':send('trait-restrictions')
    if trait['effect'] not in ['fragile','treasure_gold','chest','histrionic','costly','shock']:
        send('trait-guard',amount=100)
        for upgraded in [False,True]:
            played=send('expansion-play',card=cls+'_strike',upgrade=upgraded)
            assert played['pile']=='Discard' and not played['tableNodePresent']
    if trait['effect'] in ['hurt_energy','coin_loss','diva','explosions'] or trait['heal']>0:send('trait-triggers')
    if index%10==9:print(tag,index+1,'trait probes pass',flush=True)
if 'smoke' in tag:
    send('abandon');print(tag,'focused native trigger and ownership smoke passes',flush=True);sys.exit(0)
start('ironclad',[]);send('trait-restrictions')
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
