"""Paid starters and save/resume from complete installed package files, all classes."""
import hashlib,json,sys,time
from pathlib import Path
from oracle_protocol import call
root=Path(__file__).resolve().parents[1];profile=Path(sys.argv[1]).resolve();tag=sys.argv[2]
expected=hashlib.sha256((root/'sts2/bin/Release/net9.0/SpireLegacy.dll').read_bytes()).hexdigest()
output=root/'private/sts2-evidence'/f'package-native-{tag}.json';records=[]
assert profile.is_relative_to((root/'private').resolve()) and not output.exists()
def send(command,**args):
    record=call(profile,dict(command=command,**args),timeout=90,expected_hash=expected);records.append(record);output.write_text(json.dumps(records,indent=2)+'\n')
    reply=record['response'];assert reply['ok'],reply
    assert {m['id']:m['version'] for m in reply['metadata']['mods']}=={'BaseLib':'v3.4.7','SpireLegacy':json.loads((root/'sts2/SpireLegacy.json').read_text())['version']}
    return reply['result']
def ready():
    deadline=time.monotonic()+45
    while time.monotonic()<deadline:
        state=send('state')
        if state['phase']=='Play':return state
        time.sleep(.15)
    raise TimeoutError('Package lab not playable')
send('trait-speed',value=8)
for cls in ['knight','mage','ranger']:
    send('start',**{'class':cls,'traits':[],'quick':True,'manorTier':0,'seed':'SPIRELEGACY-PACKAGE-62'});ready()
    appearance=send('trait-appearance');assert any(a['classId']==cls and a['visible'] for a in appearance),appearance
    for kind in ['strike','guard']:
        card=cls+'_'+kind
        for upgraded in [False,True]:
            result=send('expansion-play',card=card,upgrade=upgraded)
            assert result['pile']=='Discard' and not result['tableNodePresent'],result
            assert result['energyChange']==-result['cost'] and result['saveRoundTrip'],result
            print(tag,card,upgraded,'paid resolved and discarded',flush=True)
    send('save');send('resume');ready();send('trait-audit')
    print(tag,cls,'appearance and native save/resume pass',flush=True)
send('abandon')
