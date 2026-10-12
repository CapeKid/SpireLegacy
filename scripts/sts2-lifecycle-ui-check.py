"""Chronicle read/motto and heir selection native routes in disposable labs."""
import hashlib,json,sys,time
from pathlib import Path
from oracle_protocol import call
root=Path(__file__).resolve().parents[1];profile=Path(sys.argv[1]).resolve();tag=sys.argv[2]
expected=hashlib.sha256((root/'sts2/bin/Release/net9.0/SpireLegacy.dll').read_bytes()).hexdigest()
output=root/'private/sts2-evidence'/f'lifecycle-ui-{tag}.json';evidence=[]
assert not output.exists()

def send(command,**args):
    record=call(profile,dict(command=command,**args),timeout=90,expected_hash=expected)
    evidence.append(record);output.parent.mkdir(exist_ok=True);output.write_text(json.dumps(evidence,indent=2),encoding='utf-8')
    reply=record['response'];assert reply['ok'],reply
    assert {m['id'] for m in reply['metadata']['mods']}=={'BaseLib','SpireLegacy'}
    return reply['result']

def ready():
    deadline=time.monotonic()+40
    while send('state')['phase']!='Play':
        if time.monotonic()>deadline:raise TimeoutError('UI playable phase')
        time.sleep(.15)

send('chronicle-fixture',unlocked=True)
for cls,title,motto in [('knight','The oath outlives the armor','Unbroken Oath'),('mage','A journal with another page','Living Archive'),('ranger','A path that stays open','Open Trail')]:
    send('start',**{'class':cls,'quick':True,'traits':[],'seed':'SPIRELEGACY-LIFECYCLE-49'});ready()
    for upgraded in [False,True]:
        played=send('expansion-play',card=cls+'_strike',upgrade=upgraded)
        assert played['pile']=='Discard' and not played['tableNodePresent']
    initial=send('lifecycle-state');snapshot=initial['snapshot']
    eligibility=send('chronicle-eligibility');assert len(eligibility['eligible'])==10
    send('manor')
    button=next(b['text'] for b in send('manor-state')['buttons'] if b['text'].startswith('Family Chronicle'))
    send('press',text=button)
    button=next(b['text'] for b in send('manor-state')['buttons'] if b['text'].endswith(title) and not b['disabled'])
    send('press',text=button)
    assert send('manor-state')['focusText'].startswith('Use family motto:')
    send('input',action='ui_select')
    selected=send('lifecycle-state');assert selected['profile']==selected['persisted']
    assert selected['profile']['chronicle']['motto']==motto and cls+'_victory' in selected['profile']['chronicle']['read']
    assert selected['snapshot']==snapshot,'Menu changes altered frozen run'
    send('close');send('save');send('resume');ready()
    restored=send('lifecycle-state');assert restored['profile']==restored['persisted'] and restored['profile']['chronicle']==selected['profile']['chronicle']
    assert restored['snapshot']['motto']==snapshot['motto'] and restored['snapshot']['chronicleRelics']==snapshot['chronicleRelics']
    send('abandon');send('select');send('manor')
    choices=[b['text'] for b in send('manor-state')['buttons'] if b['text']=='Choose this heir' and not b['disabled']]
    assert choices;send('press',text='Choose this heir',index=0)
    chosen=send('lifecycle-state');assert chosen['profile']==chosen['persisted'] and chosen['profile']['active'] is None and chosen['profile']['selected'] is not None
    assert sum(b['text']=='Selected for next climb' for b in send('manor-state')['buttons'])==1,'Persisted selection must keep its visible indicator'
    send('close');print(tag,cls,'paid cards, Chronicle controller/read/motto/frozen snapshot and next heir selection passed',flush=True)
