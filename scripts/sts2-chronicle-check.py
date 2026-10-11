"""Family Chronicle native gating, bounded relics, persistence and controller routes."""
import json,sys,time,shutil
from pathlib import Path
root=Path(__file__).resolve().parents[1];tag=sys.argv[1];profile=(root/'private'/f'sts2-chronicle-{tag}-profile').resolve();evidence=[]
log=root/'private'/f'chronicle-{tag}.out.log';deadline=time.monotonic()+140
while not log.exists() or 'Time to main menu' not in log.read_text(encoding='utf-8',errors='replace'):
    if time.monotonic()>deadline:raise TimeoutError('main menu')
    time.sleep(.5)
def send(command,**args):
    out=profile/'response.json';out.unlink(missing_ok=True);assert not (profile/'request.json').exists()
    p=profile/'request.pending';p.write_text(json.dumps(dict(command=command,**args)),encoding='utf-8');p.replace(profile/'request.json');end=time.monotonic()+90
    while not out.exists():
        if time.monotonic()>end:raise TimeoutError(command)
        time.sleep(.05)
    r=json.loads(out.read_text(encoding='utf-8'));assert r['ok'],r;evidence.append(dict(command=command,args=args,result=r['result']));return r['result']
def ready(turn=None):
    for _ in range(200):
        r=send('state')
        if r['phase']=='Play' and (turn is None or r['turn']==turn):return r
        time.sleep(.1)
    raise TimeoutError('play')
def start(cls):send('start',**{'class':cls,'traits':[],'quick':True});return ready()
def enter(room='Boss'):send('relic-clear');send('enter',room=room);return ready()
def play(card):
    r=send('play',card=card);time.sleep(.1);return r
def press(text):send('press',text=text)
def shot(name):
    time.sleep(.5);r=send('screenshot');shutil.copy2(r['path'],root/'private/sts2-evidence'/f'chronicle-{tag}-{name}.png')
def power(r,id):return next((p['amount'] for p in r['powers'] if p['id']=='POWER.'+id),0)
try:
    if send('chronicle-state')['snapshot'] is not None:send('abandon')
    r=send('chronicle-fixture',unlocked=False);assert not r['unlockedRelics'] and len(r['availableRelics'])==7,r
    start('knight');r=send('chronicle-eligibility');assert len(r['eligible'])==7 and all(not c['contaminated'] for c in r['native']),r
    info=send('relic-info');assert len(info['drops'])==len(info['shops'])==7 and len(info['own'])==11,info
    send('manor');state=send('manor-state');assert state['focusText']=='Name your family',state
    send('input',action='ui_down');state=send('manor-state');assert state['focusText'].startswith('Family Chronicle'),state
    send('input',action='ui_select');state=send('manor-state');assert state['focusableLabels']==0 and any(b['text'].startswith('Locked:') and b['disabled'] for b in state['buttons']),state
    shot('locked-list');send('input',action='ui_select');r=send('chronicle-state');assert 'founding' in r['chronicle']['read'],r
    shot('founding');send('input',action='ui_cancel');send('input',action='ui_cancel');send('input',action='ui_cancel');assert not send('chronicle-state')['snapshot']==None
    send('abandon');r=send('chronicle-state');assert r['chronicle']['classes']['knight']['returns']==1,r
    r=send('chronicle-fixture',unlocked=True);assert len(r['chronicle']['unlocked'])==9 and len(r['unlockedRelics'])==3,r
    for cls in ['knight','mage','ranger']:
        start(cls);r=send('chronicle-eligibility');assert len(r['eligible'])==10,r
        info=send('relic-info');assert len(info['drops'])==len(info['shops'])==10,info
        send('relic-clear');send('enter',room='Boss');ready();send('relic-obtain',relic='guardian_knot',shop=True)
        r=play(cls+'_strike');assert r['block']==0 and not send('relic-counters')[0]['CombatClaimed'],r
        a=play(cls+'_guard');b=play(cls+'_strike');assert b['block']-a['block']==3,b
        send('relic-roundtrip');a=b;b=play(cls+'_strike');assert b['block']==a['block'],b
        send('enter',room='Boss');ready();a=play(cls+'_guard');b=play(cls+'_strike');assert b['block']-a['block']==3,b
        enter();send('relic-obtain',relic='scholar_bookmark');a=send('state');b=play('knight_forge_memory');assert len(b['hand'])==len(a['hand'])+1,b
        send('relic-roundtrip');a=b;b=play('knight_forge_memory');assert len(b['hand'])==len(a['hand']),b
        enter('Monster');send('relic-obtain',relic='trail_compass');assert power(send('state'),'VIGOR_POWER')==0
        send('turn');r=ready(2);assert power(r,'VIGOR_POWER')==3,r
        send('relic-roundtrip');r=play(cls+'_strike');assert power(r,'VIGOR_POWER')==0,r
        for _ in range(3):play(cls+'_guard')
        send('turn');r=ready(3);assert power(r,'VIGOR_POWER')==0,r
        # Already acquired relics are correctly removed from this run's grab bag.
        # Begin another run before testing a combined acquisition/save snapshot.
        start(cls);enter();
        for key in ['guardian_knot','scholar_bookmark','trail_compass']:send('relic-obtain',relic=key)
        send('save');before=send('chronicle-state');send('resume');ready();after=send('chronicle-state');assert before==after,(before,after);assert len(send('relic-counters'))==3
        send('manor');press(next(b['text'] for b in send('manor-state')['buttons'] if b['text'].startswith('Family Chronicle')))
        press('NEW · Read: '+{'knight':'The oath outlives the armor','mage':'A journal with another page','ranger':'A path that stays open'}[cls])
        state=send('manor-state');assert state['focusText'].startswith('Use family motto:'),state
        send('input',action='ui_select');r=send('chronicle-state');assert r['motto']=={'knight':'Unbroken Oath','mage':'Living Archive','ranger':'Open Trail'}[cls],r
        shot(cls+'-victory-story');send('input',action='ui_cancel');shot(cls+'-unlocked-list')
        send('input',action='mega_view_exhaust_pile_and_tab_right');state=send('manor-state');assert state['scroll']>0,state
        send('input',action='ui_cancel');send('input',action='ui_cancel');assert not send('chronicle-state')['snapshot']==None
        shot(cls+'-banner');send('abandon');print(tag+': '+cls+' native unlock pool, reward/shop acquisition, bounded relics/save and Chronicle controller routes pass',flush=True)
finally:
    (root/'private/sts2-evidence'/f'chronicle-{tag}.json').write_text(json.dumps(evidence,indent=2),encoding='utf-8')
