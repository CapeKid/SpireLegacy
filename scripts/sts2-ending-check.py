"""Private native startup/room abandonment and combat-death presentation oracle."""
import json,sys,time,shutil
from pathlib import Path
root=Path(__file__).resolve().parents[1];tag=sys.argv[1];baseline='--baseline' in sys.argv
profile=(root/'private'/f'sts2-ending123-{tag}-profile').resolve();evidence=[]
log=root/'private'/('ending123-baseline.out.log' if baseline else f'ending123-{tag}.out.log');deadline=time.monotonic()+140
while not log.exists() or 'Time to main menu' not in log.read_text(encoding='utf-8',errors='replace'):
    if time.monotonic()>deadline:raise TimeoutError('main menu')
    time.sleep(.5)
def send(command,**args):
    out=profile/'response.json';out.unlink(missing_ok=True);assert not (profile/'request.json').exists()
    pending=profile/'request.pending';pending.write_text(json.dumps(dict(command=command,**args)),encoding='utf-8');pending.replace(profile/'request.json')
    deadline=time.monotonic()+90
    while not out.exists():
        if time.monotonic()>deadline:raise TimeoutError(command)
        time.sleep(.05)
    reply=json.loads(out.read_text(encoding='utf-8'));assert reply['ok'],reply
    evidence.append(dict(command=command,args=args,result=reply['result']));return reply['result']
def ready():
    for _ in range(200):
        state=send('state')
        if state['phase']=='Play':return state
        time.sleep(.1)
    raise TimeoutError('combat')
try:
    # The first family's quick-start deliberately enters combat; settle that fixture.
    send('start',**{'class':'knight','traits':[],'quick':True});ready();send('abandon');time.sleep(2)
    cases=[('knight','startup',[])] if baseline else [('knight','startup',[]),('mage','startup',[]),('ranger','startup',[]),('knight','combat',[]),('mage','combat',[]),('ranger','RestSite',['small']),('mage','Shop',['colortrails'])]
    for cls,scene,traits in cases:
        send('start',**{'class':cls,'traits':traits,'quick':False});time.sleep(2)
        state=send('state');assert state['phase'] is None and state['hp']>0,state
        if scene=='combat':send('enter',room='Boss');ready();send('kill',player=True)
        else:
            if scene!='startup':send('enter',room=scene);time.sleep(1)
            send('abandon')
        time.sleep(2)
        if not baseline:
            ending=send('ending-visual');assert len(ending)==1 and ending[0]['visible'],ending
            sprites=ending[0]['sprites'];assert len(sprites)==1,sprites
            sprite=sprites[0];assert sprite['classId']==cls and sprite['pose']=='dead' and sprite['visible'] and not sprite['placeholderVisible'],sprite
            if cls=='mage':assert sprite['frames']>=4 and len(sprite['deathFrames'])>=3,sprite
        shot=send('screenshot');shutil.copy2(shot['path'],root/'private/sts2-evidence'/f'ending123-{tag}-{cls}-{scene}{"-baseline" if baseline else ""}.png')
        time.sleep(.5)
    print(tag+': '+('baseline startup abandonment captured' if baseline else 'all-class startup abandonment, Knight/Mage combat deaths and inherited rest/shop endings use only original dead sprites')+'.')
finally:
    (root/'private/sts2-evidence'/f'ending123-{tag}{"-baseline" if baseline else ""}.json').write_text(json.dumps(evidence,indent=2),encoding='utf-8')
