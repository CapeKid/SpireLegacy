"""Native potion generation, queued use, saved belts and controller-focus checks."""
import json,sys,time,shutil
from oracle_protocol import call
from pathlib import Path
root=Path(__file__).resolve().parents[1];tag=sys.argv[1];prototype='--prototype' in sys.argv
controller='--controller' in sys.argv
profile=(root/'private'/f'sts2-potion122-{tag}-profile').resolve();evidence=[]
deadline=time.monotonic()+140;log=root/'private'/f'potion122-{tag}.out.log'
while not log.exists() or 'Time to main menu' not in log.read_text(encoding='utf-8',errors='replace'):
    if time.monotonic()>deadline:raise TimeoutError('main menu')
    time.sleep(.5)
def send(command, **args):
    record=call(profile,dict(command=command,**args),timeout=90)
    reply=record['response']
    assert reply['ok'],reply
    evidence.append(dict(command=command,args=args,result=reply['result'],metadata=reply['metadata']))
    return reply['result']
def ready():
    for _ in range(200):
        state=send('state')
        if state['phase']=='Play':return state
        time.sleep(.1)
    raise TimeoutError('combat')
def use(key):
    b=send('potion-use',potion=key);assert b['completed'] and b['energyChange']==0,b;time.sleep(1);return b
def power(result,key,enemy=False):return next((p['amount'] for p in result['enemyPowers' if enemy else 'powers'] if p['id']=='POWER.'+key),0)
try:
    for cls in ([] if controller else ['knight'] if prototype else ['knight','mage','ranger']):
        send('start',**{'class':cls,'traits':[],'quick':True});ready();send('enter',room='Boss');ready();send('potion-clear')
        send('potion-obtain',potion='borrowed_breath');assert send('potion-focus',potion='borrowed_breath')['focused']
        time.sleep(.5);shot=send('screenshot');shutil.copy2(shot['path'],root/'private/sts2-evidence'/f'potion122-{tag}-{cls}-tonic.png')
        b=use('borrowed_breath');assert b['blockChange']==6 and b['handChange']==1,b
        if prototype:send('abandon');continue
        info=send('potion-info');assert len(info['drops'])==len(info['generated'])==len(info['shops'])==3
        send('potion-obtain',potion='briar_vial',shop=True);b=use('briar_vial');assert power(b,'POISON_POWER',True)==4 and power(b,'WEAK_POWER',True)==1,b
        send('potion-obtain',potion='triune_draught');b=use('triune_draught')
        if cls=='knight':assert b['blockChange']==8 and power(b,'VIGOR_POWER')==2,b
        elif cls=='mage':assert b['charges']==2 and b['handChange']==1,b
        else:assert power(b,'VIGOR_POWER')==3 and b['handChange']==2,b
        send('potion-obtain',potion='triune_draught');b=use('triune_draught')
        if cls=='mage':assert b['charges']==2,b
        send('potion-clear')
        for key in ['borrowed_breath','briar_vial','triune_draught']:send('potion-obtain',potion=key)
        slots=send('potion-slots');send('save');send('resume');ready();assert send('potion-slots')==slots
        assert send('potion-focus',potion='briar_vial')['focused'];time.sleep(.5)
        shot=send('screenshot');shutil.copy2(shot['path'],root/'private/sts2-evidence'/f'potion122-{tag}-{cls}-belt.png')
        for key in ['borrowed_breath','briar_vial','triune_draught']:use(key)
        assert all(p is None for p in send('potion-slots'))
        time.sleep(1.5);send('abandon')
    if controller:
        cases=[('knight',[]),('mage',[]),('ranger',[]),('knight',['megahealth','cheeronkills']),('mage',['superhealer','playerknockedfar']),('ranger',['cantattack'])]
        for cls,traits in (cases[:1] if '--smoke' in sys.argv else cases):
            send('start',**{'class':cls,'traits':traits,'quick':True});ready();send('enter',room='Boss');ready();send('potion-clear')
            for key in ['borrowed_breath','briar_vial','triune_draught']:send('potion-obtain',potion=key)
            slots=send('potion-slots');send('save');send('resume');ready();assert send('potion-slots')==slots
            assert send('potion-focus',potion='borrowed_breath')['focused']
            for key in ['borrowed_breath','briar_vial','triune_draught']:
                b=send('potion-controller-use',potion=key);assert b['controller'] and b['completed'] and b['hpChange']==b['energyChange']==0,b;time.sleep(1)
                if key=='borrowed_breath':assert b['blockChange']==6 and b['handChange']==1,b
                elif key=='briar_vial':assert b['cancelPreserved'] and power(b,'POISON_POWER',True)==4 and power(b,'WEAK_POWER',True)==1,b
                elif cls=='knight':assert b['blockChange']==8 and power(b,'VIGOR_POWER')==2,b
                elif cls=='mage':assert b['charges']==2 and b['handChange']==1,b
                else:assert power(b,'VIGOR_POWER')==3 and b['handChange']==2,b
            assert all(p is None for p in send('potion-slots'));time.sleep(1.5);send('abandon')
    print(tag+': '+('native controller use/cancel/confirm and trait saved-belt checks' if controller else 'vertical slice' if prototype else 'all-class potion sources, distinct shared options, exact native effects, charge cap, targets, removal, native save/resume and controller focus')+' passed.')
finally:
    path=root/'private/sts2-evidence'/f'potion122-{tag}{"-smoke" if "--smoke" in sys.argv else "-controller" if controller else "-prototype" if prototype else ""}.json'
    path.write_text(json.dumps(evidence,indent=2),encoding='utf-8')
