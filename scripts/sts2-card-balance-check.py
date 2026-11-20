"""Native effect regression for the 1.0.13 card nerfs, in opt-in private labs."""
import json, sys, time
from oracle_protocol import call
from pathlib import Path

root = Path(__file__).resolve().parents[1]
profile = (root / sys.argv[1]).resolve()
tag = sys.argv[2]
assert profile.is_relative_to((root / 'private').resolve())
evidence = []

def send(command, **args):
    record=call(profile,dict(command=command,**args),timeout=90)
    reply=record['response']
    assert reply['ok'],reply
    evidence.append(dict(command=command,args=args,result=reply['result'],metadata=reply['metadata']))
    return reply['result']

def ready(turn=1):
    for _ in range(150):
        state = send('state')
        if state['phase'] == 'Play' and state['turn'] >= turn: return state
        time.sleep(.1)
    raise TimeoutError('combat')

def start(cls):
    send('start', **{'class': cls, 'traits': [], 'quick': True})
    return ready()

def amount(powers, key):
    return sum(p['amount'] for p in powers if key.lower() in p['id'].lower())

try:
    for upgrade in [False, True]:
        start('mage')
        ward = send('play', card='mage_perfect_ward', upgrade=upgrade)
        assert amount(ward['powers'], 'intangible') == 1 and amount(ward['powers'], 'buffer') == 0, ward
        for hit in range(1, 3):
            guarded = send('injure', amount=8)
            assert guarded['hp'] == ward['hp']-hit, guarded
        send('turn')
        after = ready(2)
        assert amount(after['powers'], 'intangible') == 0, after
        before = start('ranger')
        timing = send('play', card='ranger_perfect_timing', upgrade=upgrade)
        assert amount(timing['powers'], 'berserk') == 1, timing
        assert timing['hp'] == before['hp']-3, timing
        send('turn'); turn2 = ready(2)
        assert turn2['energy'] == 4, turn2
        send('turn'); turn3 = ready(3)
        assert turn3['energy'] == 4, turn3
        before = start('ranger')
        tripwire = send('play', card='ranger_tripwire', upgrade=upgrade)
        assert tripwire['block']-before['block'] == (7 if upgrade else 4), tripwire
        assert amount(tripwire['enemies'][0]['powers'], 'weak') == 1, tripwire
    send('abandon')
    print(tag + ': base/upgraded Ward limits both hits and expires after the enemy turn; Timing grants only +1 Energy on consecutive turns; Tripwire gives 4/7 Block and exactly 1 Weak.')
finally:
    (root / 'private/sts2-evidence' / f'balance113-{tag}.json').write_text(json.dumps(evidence, indent=2), encoding='utf-8')
