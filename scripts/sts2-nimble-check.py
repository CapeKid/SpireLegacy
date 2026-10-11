"""Native Nimble eligibility, preview/save and paid Block effects in isolated labs."""
import json, re, sys, time
from oracle_protocol import call
from pathlib import Path

root = Path(__file__).resolve().parents[1]
tag = sys.argv[1]
profile = root / 'private' / f'sts2-nimble-{tag}-profile'
log = root / 'private' / f'nimble-{tag}.out.log'
evidence = []
deadline = time.monotonic() + 140
while not log.exists() or 'Time to main menu' not in log.read_text(encoding='utf-8', errors='replace'):
    if time.monotonic() > deadline:
        raise TimeoutError('main menu')
    time.sleep(.5)

def send(command, **args):
    record=call(profile,dict(command=command,**args),timeout=90)
    reply=record['response']
    assert reply['ok'],reply
    evidence.append(dict(command=command,args=args,result=reply['result'],metadata=reply['metadata']))
    return reply['result']

def ready():
    for _ in range(200):
        state = send('state')
        if state['phase'] == 'Play':
            return
        time.sleep(.1)
    raise TimeoutError('play phase')

def start(cls):
    send('start', **{'class': cls, 'traits': [], 'quick': True})
    ready()
    send('enter', room='Boss')
    ready()

try:
    start('knight')
    eligibility = send('nimble-eligibility')
    assert eligibility['checks'] == 2 * len(json.loads((root / 'sheets/cards.json').read_text()))
    print(tag, eligibility, flush=True)
    for key in ['knight_guard', 'knight_reinforce', 'knight_stand_ready',
                'mage_crystal_shell', 'mage_scripted_spark', 'ranger_ancient_canopy',
                'knight_salvage_guard', 'mage_spell_pyre', 'ranger_shed_cloak']:
        for upgraded in [False, True]:
            start(key.split('_')[0])
            result = send('nimble-play', card=key, upgrade=upgraded)
            assert result['paid'] and not result['tableNodePresent'], result
            plain = re.sub(r'\[[^]]*\]', '', result['description'])
            if result['nextAmount']:
                assert f"Next turn, gain {result['nextAmount']} Block." in plain, result
            if result['discardBlock']:
                assert f"gain {result['discardBlock']} Block." in plain, result
            print(tag, key + ('+' if upgraded else ''), 'paid effects passed', flush=True)
    send('abandon')
finally:
    (root / 'private/sts2-evidence' / f'nimble-{tag}.json').write_text(json.dumps(evidence, indent=2), encoding='utf-8')
