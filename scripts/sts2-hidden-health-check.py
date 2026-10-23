"""Check C.I.P. against real health widgets in an isolated owned-game profile."""
import json
import shutil
import sys
import time
from pathlib import Path
from oracle_protocol import call

root = Path(__file__).resolve().parents[1]
profile = Path(sys.argv[1]).resolve()
evidence = []


def send(command, **args):
    record = call(profile, dict(command=command, **args), timeout=90)
    evidence.append(record)
    (profile / 'hidden-health-check.json').write_text(json.dumps(evidence, indent=2), encoding='utf-8')
    assert record['response']['ok'], record['response']
    return record['response']['result']


def ready():
    for _ in range(100):
        state = send('state')
        if state['phase'] == 'Play':
            time.sleep(.5)
            return state
        time.sleep(.1)
    raise TimeoutError('Native Play phase')


classes = json.loads((root / 'sheets/classes.json').read_text(encoding='utf-8'))
if '--vanilla-only' in sys.argv[2:]:
    classes = []
for cls in classes:
    for hidden in [True, False]:
        send('start', **{'class': cls['id'], 'traits': ['nohealthbar'] if hidden else [],
                         'quick': True, 'manorTier': 0, 'ascension': 0, 'seed': 'CLASS-SIZE-COMPARISON'})
        state = ready()
        assert state['maxHp'] == cls['hp'], state
        ui = send('hidden-health')
        assert ui['hidden'] == hidden and ui['topVisible'] != hidden, ui
        before = state['hp']
        hit = send('trait-hit', amount=2, unpowered=True)
        assert hit['hp'] == before - 2, hit
        send('hidden-health')
        changed = send('hidden-health', changeHp=True)
        assert changed['hp'] == before - 9, changed
        time.sleep(.6)
        send('hidden-health')
        send('manual', card=cls['id'] + '_guard')
        time.sleep(.6)
        send('hidden-health')
        if hidden:
            shot = send('screenshot')
            assert shot['error'] == 'Ok', shot
            shutil.copy2(shot['path'], profile / (cls['id'] + '-cip.png'))
        send('save')
        send('resume')
        ready()
        restored = send('hidden-health')
        assert restored['hidden'] == hidden and restored['maxHp'] == cls['hp'], restored
        for room in ['RestSite', 'Shop', 'Monster']:
            send('enter', room=room)
            if room == 'Monster':
                ready()
            time.sleep(.5)
            assert send('hidden-health')['hidden'] == hidden
        print(cls['id'], 'C.I.P.' if hidden else 'normal', 'HP, Block, paid card, damage/healing, save/resume and rooms passed', flush=True)

send('start', **{'class': 'ironclad', 'traits': [], 'quick': True, 'manorTier': 0, 'ascension': 0})
send('enter', room='Monster')
ready()
assert not send('hidden-health')['hidden']
send('abandon')
print('Vanilla control passed.' if '--vanilla-only' in sys.argv[2:] else 'All three classes and vanilla control passed.')
