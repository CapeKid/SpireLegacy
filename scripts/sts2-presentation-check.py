"""Native presentation/resource/card checks in an isolated owned-game profile."""
import json,sys,time,shutil
from pathlib import Path
root=Path(__file__).resolve().parents[1];tag=sys.argv[1];profile=(root/'private'/f'sts2-presentation-{tag}-profile').resolve();evidence=[]
deadline=time.monotonic()+140;log=root/'private'/f'presentation-{tag}.out.log'
while not log.exists() or 'Time to main menu' not in log.read_text(encoding='utf-8',errors='replace'):
    if time.monotonic()>deadline:raise TimeoutError('main menu')
    time.sleep(.5)
def send(command,**args):
    out=profile/'response.json';out.unlink(missing_ok=True);assert not (profile/'request.json').exists()
    pending=profile/'request.pending';pending.write_text(json.dumps(dict(command=command,**args)));pending.replace(profile/'request.json');deadline=time.monotonic()+90
    while not out.exists():
        if time.monotonic()>deadline:raise TimeoutError(command)
        time.sleep(.05)
    reply=json.loads(out.read_text(encoding='utf-8'));assert reply['ok'],reply
    evidence.append(dict(command=command,args=args,result=reply['result']));return reply['result']
def ready():
    for _ in range(200):
        state=send('state')
        if state['phase']=='Play':return
        time.sleep(.1)
    raise TimeoutError('play')
def shot(name):
    time.sleep(.6);r=send('screenshot');shutil.copy2(r['path'],root/'private/sts2-evidence'/f'presentation-{tag}-{name}.png')
try:
    send('select');shot('selection');assert send('presentation-transition')['complete']
    for cls in ['knight','mage','ranger']:
        send('start',**{'class':cls,'traits':[],'quick':True});ready();send('enter',room='Boss');ready()
        result=send('presentation');assert result['cls']==cls and result['energyVisible'] and result['nativeUnchanged'],result
        shot(cls+'-combat')
        for card in [cls+'_strike',cls+'_guard']:
            play=send('expansion-play',card=card,upgrade=False)
            assert play['saveRoundTrip'] and not play['tableNodePresent'],play
        send('enter',room='RestSite');result=send('presentation-rest');assert result['visible'] and result['breathing'],result;shot(cls+'-rest')
        send('enter',room='Shop');time.sleep(.5);send('potion-obtain',potion='triune_draught',shop=True)
        room=send('room-visual');assert any(r['classId']==cls and 'skill' in r['events'] for r in room),room
        shot(cls+'-shop');print(tag+': '+cls+' native energy/map/cookie/trail/audio/room response passed',flush=True)
    send('abandon')
finally:
    (root/'private/sts2-evidence'/f'presentation-{tag}.json').write_text(json.dumps(evidence,indent=2))
