"""Native relic acquisition, bounded effects, serialization and controller-focus checks."""
import json,sys,time,shutil
from pathlib import Path
root=Path(__file__).resolve().parents[1];tag=sys.argv[1]
profile=(root/'private'/f'sts2-relic121-{tag}-profile').resolve();evidence_file=root/'private/sts2-evidence'/f'relic121-{tag}.json'
evidence=json.loads(evidence_file.read_text(encoding='utf-8')) if '--resume' in sys.argv and evidence_file.exists() else []
completed=set();active_relic=None
for entry in evidence:
    if entry['command']=='relic-obtain':active_relic=entry['args']['relic']
    if entry['command']=='play' and active_relic=='family_seal' and entry['result']['turn']==2:
        cls=entry['result']['heir']['classId']
        if entry['args']['card']==cls+'_strike':completed.add(cls)
log=root/'private'/f'relic121-{tag}-final.out.log';deadline=time.monotonic()+140
while not log.exists() or 'Time to main menu' not in log.read_text(encoding='utf-8',errors='replace'):
    if time.monotonic()>deadline:raise TimeoutError('main menu')
    time.sleep(.5)
def send(command,**args):
    out=profile/'response.json';out.unlink(missing_ok=True)
    assert not (profile/'request.json').exists()
    pending=profile/'request.pending';pending.write_text(json.dumps(dict(command=command,**args)),encoding='utf-8');pending.replace(profile/'request.json')
    deadline=time.monotonic()+90
    while not out.exists():
        if time.monotonic()>deadline:raise TimeoutError(command)
        time.sleep(.05)
    reply=json.loads(out.read_text(encoding='utf-8'));assert reply['ok'],reply
    evidence.append(dict(command=command,args=args,result=reply['result']));return reply['result']
def ready(turn=None):
    for _ in range(200):
        state=send('state')
        if state['phase']=='Play' and (turn is None or state['turn']==turn):return state
        time.sleep(.1)
    raise TimeoutError('combat phase')
def power(state,key):return next((p['amount'] for p in state['powers'] if p['id']=='POWER.'+key),0)
def play(key):
    result=send('play',card=key);time.sleep(1);return result
def fresh(key,shop=False,room='Boss'):
    send('relic-clear');send('enter',room=room);ready();send('relic-obtain',relic=key,shop=shop);return send('state')
def next_turn(turn):send('turn');time.sleep(.15);return ready(turn)
try:
    for cls in ['knight','mage','ranger']:
        send('start',**{'class':cls,'traits':[],'quick':True});ready()
        info=send('relic-info');assert len(info['drops'])==len(info['shops'])==7
        assert len(info['own'])==11 and sum(r['rarity']=='Starter' for r in info['own'])==1
        assert all(not any('SPIRELEGACY-R_' in r['id'] for r in c['relics']) for c in info['native'])
        if cls not in completed:
            a=fresh('heirloom_brooch',True);b=play('knight_forge_memory');assert b['block']-a['block']==9
            a=b;b=play('knight_forge_memory');assert b['block']-a['block']==4
            send('relic-roundtrip');a=b;b=play('knight_forge_memory');assert b['block']-a['block']==4
            send('enter',room='Boss');ready();b=play('knight_forge_memory');assert b['block']==9
            fresh('training_dummy');play(cls+'_guard');b=play(cls+'_strike');assert power(b,'VIGOR_POWER')==1,b
            send('relic-roundtrip');b=play(cls+'_strike');assert power(b,'VIGOR_POWER')==0,b
            b=next_turn(2);play(cls+'_guard');b=play(cls+'_strike');assert power(b,'VIGOR_POWER')==1,b
            a=fresh('threadbound_ledger');b=play('knight_king_s_ransom');assert len(b['hand'])==len(a['hand'])+1,b
            send('relic-roundtrip');a=b;b=play('knight_king_s_ransom');assert len(b['hand'])==len(a['hand']),b
            fresh('wayfarer_ribbon');b=next_turn(2);assert send('relic-counters')[0]['TriggerTurn']==-1
            a=b;b=play('ranger_empty_quiver');assert b['block']-a['block']==7,b
            send('relic-roundtrip');a=b;b=play('ranger_empty_quiver');assert b['block']-a['block']==4,b
            a=fresh('triune_crest');b=play(cls+('_strike' if cls=='ranger' else '_guard'))
            if cls=='knight':assert b['block']-a['block']==8,b
            elif cls=='mage':assert next(p['display'] for p in b['powers'] if p['id']=='POWER.SPIRELEGACY-P_CLASS')==2 and send('relic-counters')[0]['CombatClaimed'];play(cls+'_strike')
            else:assert power(b,'VIGOR_POWER')==3,b
            send('relic-roundtrip');a=send('state');b=play(cls+('_strike' if cls=='ranger' else '_guard'))
            if cls=='knight':assert b['block']-a['block']==4,b
            if cls=='ranger':assert power(b,'VIGOR_POWER')==0,b
            if cls=='mage':assert next(p['display'] for p in b['powers'] if p['id']=='POWER.SPIRELEGACY-P_CLASS')==1,b
            fresh('legacy_hourglass',room='Monster');b=next_turn(2);assert b['energy']==3 and len(b['hand'])==5,b
            for _ in range(3):play(cls+'_guard')
            b=next_turn(3);assert b['energy']==4 and len(b['hand'])==6,b
            send('relic-roundtrip')
            for _ in range(3):play(cls+'_guard')
            b=next_turn(4);assert b['energy']==3 and len(b['hand'])==5,b
            a=fresh('family_seal');play('knight_forge_memory');send('relic-roundtrip')
            play(cls+'_guard');b=play(cls+'_strike');assert b['block']-a['block']==12 and len(b['hand'])==len(a['hand'])+1,b
            send('relic-roundtrip');a=b;play('knight_forge_memory');play(cls+'_guard');b=play(cls+'_strike');assert b['block']-a['block']==8 and len(b['hand'])==len(a['hand']),b
            b=next_turn(2);assert send('relic-counters')[0]['TypeMask']==0
            a=b;play('knight_forge_memory');play(cls+'_guard');b=play(cls+'_strike');assert b['block']-a['block']==12 and len(b['hand'])==len(a['hand'])+1,b
        send('start',**{'class':cls,'traits':[],'quick':True});ready()
        send('relic-clear');send('enter',room='Boss');ready()
        for row in json.loads((root/'sheets/heir_relics.json').read_text()):
            if row['id'] not in {'guardian_knot','scholar_bookmark','trail_compass'}:send('relic-obtain',relic=row['id'])
        send('save');send('resume');ready();assert len(send('relic-counters'))==7
        time.sleep(1);focus=send('relic-focus',relic='triune_crest');assert focus['focused'] and focus['right'],focus
        shot=send('screenshot');shutil.copy2(shot['path'],root/'private/sts2-evidence'/f'relic121-{tag}-{cls}.png')
        send('cards');send('rewards');send('abandon')
    print(tag+': seven native reward/shop drops; all three classes; bounded effects, turn/combat resets, saved claims/masks/status, full run save/resume, original art and native controller focus pass; existing card/reward checks pass.')
finally:evidence_file.write_text(json.dumps(evidence,indent=2),encoding='utf-8')
