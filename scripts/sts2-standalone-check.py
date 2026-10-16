"""Standalone icon/render/start/save checks with no content reader or extracted cache."""
import json
from oracle_protocol import call
import shutil
import sys
import time
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


def ready():
    for _ in range(150):
        state = send('state')
        if state['phase'] == 'Play':
            return state
        time.sleep(.1)
    raise TimeoutError('combat')


try:
    icons = send('standalone-assets')
    assert icons['count'] == 66 and not icons['readerPresent'] and not icons['cachePresent'], icons
    old = json.loads((root / 'private' / ('sts2-regular103-profile' if tag == 'regular' else 'sts2-test-profile') / 'family.json').read_text())
    assert icons['family'] == old['family'] and icons['crowns'] == old['crowns'], icons
    loaded = json.loads((profile / 'family.json').read_text())
    for key in ['family', 'generation', 'manor', 'selected']:
        assert loaded[key] == old[key], (key, loaded, old)
    for cls in ['knight', 'mage', 'ranger']:
        traits = ['antique', 'health'] if cls == 'mage' else ['small', 'health']
        send('start', **{'class': cls, 'traits': traits, 'quick': True})
        before = ready()
        if cls == 'mage':
            run_before = json.loads((profile / 'family.json').read_text())['active']
            relics_before = before['relics']
            send('save'); send('resume'); before = ready()
            assert before['heir']['classId'] == cls and before['heir']['traits'] == traits, before
            assert before['relics'] == relics_before, (before, relics_before)
            assert json.loads((profile / 'family.json').read_text())['active']['id'] == run_before['id']
        played = send('manual', card=cls + '_strike')
        assert played['accepted'] and played['pile'] == 'Discard' and not played['tableNodePresent'], played
        assert played['state']['energy'] == before['energy'] - 1, played
        send('play', card=cls + '_guard')
        visual = send('visual')
        assert len(visual) == 1 and visual[0]['classId'] == cls and visual[0]['visible'], visual
        send('manor'); manor = send('manor-state')
        assert manor['open'] and all('preparation' not in b['text'].lower() for b in manor['buttons']), manor
        send('close')
        shot = send('screenshot')
        shutil.copy2(shot['path'], root / 'private/sts2-evidence' / ('standalone118-' + tag + '-' + cls + '.png'))
    assert send('cards')['count'] == 720
    send('abandon')
    send('standalone-assets')
    assert not (profile / 'content-preparation.log').exists()
    print(tag + ': 66 original SVG icons render; self-contained artwork, no reader/cache; prior family/manor preserved; all classes start/play/render; native save/resume preserves Antique and run identity; 720 card checks; no preparation UI/log.')
finally:
    (root / 'private/sts2-evidence' / ('standalone118-' + tag + '.json')).write_text(json.dumps(evidence, indent=2), encoding='utf-8')
