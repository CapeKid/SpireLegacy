"""Verify native paired cosmetic traits, room rendering, and real damage flashes."""
import json
import shutil
import sys
import time
from pathlib import Path

root = Path(__file__).resolve().parents[1]
profile = (root / sys.argv[1]).resolve()
tag = sys.argv[2]
assert profile.is_relative_to((root / 'private').resolve())
evidence = []
startup = root/'private'/f'traits119-{tag}.out.log'
deadline = time.monotonic()+140
while not startup.exists() or 'Time to main menu' not in startup.read_text(encoding='utf-8',errors='replace'):
    if time.monotonic()>deadline: raise TimeoutError('main menu')
    time.sleep(.5)
def send(command, **args):
    out=profile/'response.json'; out.unlink(missing_ok=True)
    assert not (profile/'request.json').exists()
    pending=profile/'request.pending'
    pending.write_text(json.dumps(dict(command=command, **args)),encoding='utf-8')
    pending.replace(profile/'request.json')
    deadline=time.monotonic()+50
    while not out.exists():
        if time.monotonic()>deadline: raise TimeoutError(command)
        time.sleep(.05)
    reply=json.loads(out.read_text(encoding='utf-8'))
    assert reply['ok'],reply
    evidence.append(dict(command=command,args=args,result=reply['result']))
    return reply['result']
def ready():
    for _ in range(150):
        state=send('state')
        if state['phase']=='Play': return state
        time.sleep(.1)
    raise TimeoutError('combat')
def appearance(room=False):
    values=send('trait-appearance')
    result=[v for v in values if v['room']==room]
    assert len(result)==1,values
    return result[0]
def shot(name):
    time.sleep(.3)
    path=send('screenshot')['path']
    shutil.copy2(path,root/'private/sts2-evidence'/f'traits119-{tag}-{name}.png')
try:
    for index, partner in enumerate(['nocolor','youareblue','oldyellowtint','disposition','halloweenholiday','christmasholiday']):
        cls=['knight','mage','ranger'][index%3]
        send('start',**{'class':cls,'traits':['colortrails',partner],'quick':True}); ready()
        first=appearance()
        assert first['rainbow'] and first['motes']==6 and first['arcs']==6,first
        assert first['gray']==(partner=='nocolor') and first['sepia']==(partner=='oldyellowtint'),first
        if partner in ['youareblue','disposition','halloweenholiday','christmasholiday']: assert len(first['accents'])==1,first
        time.sleep(.35)
        assert appearance()['motePositions']!=first['motePositions'],'Rainbow motion is frozen'
        played=send('manual',card=cls+'_strike')
        assert played['accepted'] and played['pile']=='Discard' and not played['tableNodePresent'],played
        if index<3:
            shot(cls+'-combat')
            for room in ['RestSite','Shop']:
                send('enter',room=room); time.sleep(.5)
                look=appearance(True)
                assert look['rainbow'] and look['motes']==6 and look['classId']==cls,look
                assert look['gray']==first['gray'] and look['sepia']==first['sepia'] and look['accents']==first['accents'],look
                shot(cls+'-'+room.lower())
    for traits, expected in [(['youareblue','disposition'],['blue','nature']),(['oldyellowtint','christmasholiday'],['festive']),(['halloweenholiday','christmasholiday'],['medium','festive'])]:
        send('start',**{'class':'mage','traits':traits,'quick':True}); ready()
        look=appearance(); assert sorted(look['accents'])==sorted(expected),look
        assert look['sepia']==('oldyellowtint' in traits),look
    send('start',**{'class':'ranger','traits':['fakeselfdamage','nocolor'],'quick':True}); ready()
    baseline=appearance(); assert baseline['flash']==0 and baseline['gray'],baseline
    before=send('state'); after=send('injure',amount=3)
    assert after['hp']==before['hp']-3,(before,after)
    damaged=appearance(); assert damaged['flash']>0,damaged
    shot('histrionic-flash')
    time.sleep(1.1)
    healed_color=appearance(); assert healed_color['flash']==0 and healed_color['tint']==baseline['tint'],healed_color
    assert send('state')['hp']==after['hp'],'Cosmetic flash changed HP'
    send('abandon')
    print(tag+': rainbow visible with all six palettes; orbit motion, manual card play, all three class combat/rest/shop appearances; paired palette accents coexist; real HP-loss red flash returns to gray without modifying HP.')
finally:
    (root/'private/sts2-evidence'/f'traits119-{tag}.json').write_text(json.dumps(evidence,indent=2),encoding='utf-8')
