"""Native completion/save-failure checks, isolated owned labs only."""
import hashlib,json,subprocess,sys,time
from pathlib import Path
from oracle_protocol import call

root=Path(__file__).resolve().parents[1]
profile=Path(sys.argv[1]).resolve();tag=sys.argv[2]
expected=hashlib.sha256((root/'sts2/bin/Release/net9.0/SpireLegacy.dll').read_bytes()).hexdigest()
output=root/'private/sts2-evidence'/f'lifecycle-{tag}.json';evidence=[]
assert not output.exists()

def send(command,**args):
    record=call(profile,dict(command=command,**args),timeout=90,expected_hash=expected)
    evidence.append(record);output.parent.mkdir(exist_ok=True)
    output.write_text(json.dumps(evidence,indent=2),encoding='utf-8')
    reply=record['response'];assert reply['ok'],reply
    assert {m['id'] for m in reply['metadata']['mods']}=={'BaseLib','SpireLegacy'}
    return reply['result']

def ready():
    deadline=time.monotonic()+40
    while send('state')['phase']!='Play':
        if time.monotonic()>deadline:raise TimeoutError('Lifecycle play phase')
        time.sleep(.15)

def consistent():
    state=send('lifecycle-state');assert state['profile']==state['persisted'],state
    return state

for cls in ['knight','mage','ranger']:
    for victory,fault in [(False,'none'),(True,'none'),(False,'before-commit'),(True,'after-commit')]:
        send('start',**{'class':cls,'quick':True,'traits':[],'seed':'SPIRELEGACY-LIFECYCLE-49'})
        ready()
        for upgraded in [False,True]:
            result=send('expansion-play',card=cls+'_strike',upgrade=upgraded)
            assert result['pile']=='Discard' and not result['tableNodePresent']
        before=consistent();run_id=before['profile']['active']['id']
        send('save');send('resume');ready();after=consistent()
        assert after['profile']['active']['id']==run_id
        result=send('lifecycle-complete',victory=victory,fault=fault)
        assert result['repeatStable'] and result['nativeHistoryExactlyOnce']
        consistent();print(tag,cls,victory,fault,'native completion/replay passed',flush=True)
    for end in ['abandon','death']:
        send('start',**{'class':cls,'quick':True,'traits':[],'seed':'SPIRELEGACY-LIFECYCLE-49'})
        ready();before=consistent();generation=before['profile']['generation'];run_id=before['profile']['active']['id']
        if end=='abandon':send('abandon')
        else:send('kill',player=True)
        deadline=time.monotonic()+30
        while True:
            after=consistent()
            if after['profile']['active'] is None:break
            if time.monotonic()>deadline:raise TimeoutError('Native death/abandon settlement')
            time.sleep(.2)
        assert after['profile']['generation']==generation+1 and after['profile']['settledId']==run_id
        assert after['profile']['chronicle']['settledRuns'].count(run_id)==1
        print(tag,cls,end,'actual native settlement passed',flush=True)

subprocess.run([sys.executable,str(root/'scripts/sts2-lifecycle-ui-check.py'),str(profile),tag+'-ui'],check=True)
