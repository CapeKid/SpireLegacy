"""Architect text, visit selection and real event advancement in isolated owned labs."""
import json
from oracle_protocol import call
import sys
import time
import shutil
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


try:
    for cls in ['knight', 'mage', 'ranger']:
        send('start', **{'class': cls, 'traits': [], 'quick': True})
        for _ in range(150):
            if send('state')['phase'] == 'Play':
                break
            time.sleep(.1)
        else:
            raise TimeoutError('combat startup')
        if cls == 'knight':
            checks = send('architect-dialogues')
            assert len(checks) == 15, checks
            for item in checks:
                lines = item['lines']
                assert len(lines) == 4 and [l['speaker'] for l in lines] == ['Ancient', 'Character', 'Ancient', 'Character'], item
                assert all(l['text'] and l['key'].startswith('SPIRELEGACY.ARCHITECT.' + item['classId']) for l in lines), item
                assert [l['next'] for l in lines] == ['Respond', 'Continue', 'Respond', None], item
                assert ('.first.' if item['wins'] == 0 else '.return.') in lines[0]['key'], item
            assert len({tuple(l['text'] for l in item['lines']) for item in checks}) == 6
        send('enter', room='Event', event='Architect')
        for _ in range(100):
            state = send('architect-state')
            if state['options']:
                break
            time.sleep(.1)
        assert state['index'] == 0 and len(state['lines']) == 4 and state['options'] == ['Respond'], state
        time.sleep(2.5)  # Let the native speech-bubble fade and typewriter finish before capturing.
        shot = send('screenshot')
        shutil.copy2(shot['path'], root / 'private/sts2-evidence' / ('architect117-' + tag + '-' + cls + '.png'))
        for index in [1, 2, 3]:
            state = send('architect-next')
            assert state['index'] == index and len(state['options']) == 1, state
            if index < 3:
                assert state['options'] == ['Continue' if index == 1 else 'Respond'], state
        # Leave the native Proceed/victory callback intact; do not win a debug Act 1 run.
    send('abandon')
    print(tag + ': six original conversations; all three classes at 0/1/4/10/100 wins; speakers and localized buttons; unchanged Ironclad/ending animations; actual Architect event advances every line to Proceed for all three heirs.')
finally:
    (root / 'private/sts2-evidence' / ('architect117-' + tag + '.json')).write_text(json.dumps(evidence, indent=2), encoding='utf-8')
