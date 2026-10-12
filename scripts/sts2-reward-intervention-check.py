"""Verify only existing reward choices can be overridden in the guarded native pilot."""
import hashlib,json,sys
from pathlib import Path
from oracle_protocol import call
root=Path(__file__).resolve().parents[1]
profile=Path(sys.argv[1]).resolve();tag=sys.argv[2]
assert tag and all(c.isalnum() or c in '-_' for c in tag)
output=root/'private/sts2-evidence'/f'reward-intervention-{tag}.json'
assert not output.exists()
expected=hashlib.sha256((root/'sts2/bin/Release/net9.0/SpireLegacy.dll').read_bytes()).hexdigest()
evidence=[]
def send(command,**args):
    record=call(profile,dict(command=command,**args),timeout=90,expected_hash=expected)
    evidence.append(record);output.parent.mkdir(parents=True,exist_ok=True)
    output.write_text(json.dumps(evidence,indent=2),encoding='utf-8')
    reply=record['response'];assert reply['ok'],reply
    assert {m['id'] for m in reply['metadata']['mods']}=={'BaseLib','SpireLegacy'}
    return reply['result']
for kind in ['force-present','force-absent']:
    send('start',**{'class':'ironclad','manorTier':0,'quick':False,'traits':[],'seed':'SPIRELEGACY-REWARD-CHECK'})
    result=send('balance-control-probe',kind=kind)
    assert result['closed'],result
    if kind=='force-present':assert result['added']==1 and result['successfullySelected'],result
    else:assert result['unchangedDeck'] and not result['successfullySelected'],result
    print(tag,result,flush=True)
