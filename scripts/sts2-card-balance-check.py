"""Native effect regression for the 1.0.13 card nerfs, in opt-in private labs."""
import json, sys, time
from pathlib import Path

root = Path(__file__).resolve().parents[1]
profile = (root / sys.argv[1]).resolve()
tag = sys.argv[2]
assert profile.is_relative_to((root / 'private').resolve())
evidence = []

def send(command, **args):
    out = profile / 'response.json'
    if out.exists(): out.unlink()
    assert not (profile / 'request.json').exists()
    pending = profile / 'request.pending'
    pending.write_text(json.dumps(dict(command=command, **args)), encoding='utf-8')
    pending.replace(profile / 'request.json')
    deadline = time.monotonic() + 40
    while not out.exists():
        if time.monotonic() > deadline: raise TimeoutError(command)
        time.sleep(.05)
    reply = json.loads(out.read_text(encoding='utf-8'))
    assert reply['ok'], reply
    evidence.append(dict(command=command, args=args, result=reply['result']))
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
        charges = 2 if upgrade else 1
        assert amount(ward['powers'], 'buffer') == charges, ward
        for hit in range(charges):
            guarded = send('injure', amount=1)
            assert guarded['hp'] == ward['hp'], guarded
            assert amount(guarded['powers'], 'buffer') == charges-hit-1, guarded
        assert send('injure', amount=1)['hp'] == ward['hp']-1
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
        assert tripwire['block']-before['block'] == (5 if upgrade else 3), tripwire
        assert amount(tripwire['enemies'][0]['powers'], 'weak') == 1, tripwire
    send('abandon')
    print(tag + ': base/upgraded Ward consumes exactly 1/2 prevented hits; Timing grants only +1 Energy on consecutive turns; Tripwire gives 3/5 Block and exactly 1 Weak.')
finally:
    (root / 'private/sts2-evidence' / f'balance113-{tag}.json').write_text(json.dumps(evidence, indent=2), encoding='utf-8')
