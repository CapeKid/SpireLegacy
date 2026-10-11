"""Original dialogues and native controller callbacks, isolated owned-game profiles only."""
import json,sys,time,shutil
from pathlib import Path
root=Path(__file__).resolve().parents[1];tag=sys.argv[1];profile=(root/'private'/f'sts2-dialogue-{tag}-profile').resolve();evidence=[]
deadline=time.monotonic()+140;log=root/'private'/f'dialogue-{tag}.out.log'
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
        if send('state')['phase']=='Play':return
        time.sleep(.1)
    raise TimeoutError('play')
try:
    send('start',**{'class':'knight','traits':[],'quick':True});ready()
    coverage=send('ancient-dialogues');assert coverage['checks']==240 and len(coverage['ancients'])==8,coverage
    assert len(send('architect-dialogues'))==15
    for cls in ['knight','mage','ranger']:
        send('start',**{'class':cls,'traits':[],'quick':True});ready()
        for npc in coverage['ancients']:
            for visits in [0,4]:
                state=send('ancient-enter',ancient=npc,visits=visits);time.sleep(.3)
                assert state['index']==0 and len(state['lines'])==4 and state['focusable'],state
                assert all('.'+cls+'.'+('first' if visits==0 else 'return')+'.' in r['key'] for r in state['lines']),state
                for index in [1,2,3]:
                    state=send('ancient-next');assert state['index']==index,state;time.sleep(.1)
                time.sleep(.9);state=send('ancient-state');assert state['focusable'] and state['options'],state
                if npc in ['NEOW','TANX'] and visits==4:
                    shot=send('screenshot');shutil.copy2(shot['path'],root/'private/sts2-evidence'/f'dialogue-{tag}-{cls}-{npc.lower()}.png')
            print(tag+': '+cls+' '+npc+' first/repeat native dialogue and reward focus passed',flush=True)
    send('abandon')
finally:
    (root/'private/sts2-evidence'/f'dialogue-{tag}.json').write_text(json.dumps(evidence,indent=2))
