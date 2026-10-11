"""Measured Breath native paid-play and multi-turn flush checks in isolated labs."""
import json,sys,time
from pathlib import Path

root=Path(__file__).resolve().parents[1];tag=sys.argv[1]
profile=(root/'private'/f'sts2-retention-{tag}-profile').resolve();evidence=[]
deadline=time.monotonic()+140;log=root/'private'/f'retention-{tag}.out.log'
while not log.exists() or 'Time to main menu' not in log.read_text(encoding='utf-8',errors='replace'):
    if time.monotonic()>deadline:raise TimeoutError('main menu')
    time.sleep(.5)
def send(command,**args):
    response=profile/'response.json';response.unlink(missing_ok=True)
    assert not (profile/'request.json').exists()
    pending=profile/'request.pending';pending.write_text(json.dumps(dict(command=command,**args)),encoding='utf-8');pending.replace(profile/'request.json')
    deadline=time.monotonic()+90
    while not response.exists():
        if time.monotonic()>deadline:raise TimeoutError(command)
        time.sleep(.05)
    reply=json.loads(response.read_text(encoding='utf-8'));assert reply['ok'],reply
    evidence.append(dict(command=command,args=args,result=reply['result']));return reply['result']
def ready():
    for _ in range(200):
        state=send('state')
        if state['phase']=='Play':return state
        time.sleep(.1)
    raise TimeoutError('play phase')
try:
    cases=[('knight',False,False,False,False),('mage',True,False,True,False),('ranger',False,False,False,False),('ranger',True,True,False,False),('mage',False,False,False,True)]
    for cls,upgrade,stack,intrinsic,full in cases:
        send('start',**{'class':cls,'traits':[],'quick':True});ready()
        result=send('retention-check',upgrade=upgrade,stack=stack,intrinsic=intrinsic,fullHand=full)
        assert result['powerId'].endswith('P_WELL_LAID') and result['secondRetained']==0,result
        assert result['title']=='Measured Breath' and str(result['amount']) in result['description'],result
        print(tag+': '+cls+' '+str(result),flush=True)
    send('abandon')
finally:
    (root/'private/sts2-evidence'/f'retention-{tag}.json').write_text(json.dumps(evidence,indent=2),encoding='utf-8')
