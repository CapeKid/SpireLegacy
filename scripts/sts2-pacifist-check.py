"""Native Pacifist balance/cleanup checks in owned, isolated regular/beta labs."""
import json
from oracle_protocol import call
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


def poison(enemy):
    return sum(p['amount'] for p in enemy['powers'] if 'POISON' in p['id'])


try:
    for cls, signature in [('knight', 'shield'), ('mage', None), ('ranger', 'aim')]:
        send('start', **{'class': cls, 'traits': ['cantattack'], 'quick': True})
        before = ready()
        assert all(poison(e) == 0 for e in before['enemies']), before
        played = send('manual', card=cls + '_strike')
        after = played['state']
        assert played['accepted'] and played['pile'] == 'Discard', played
        assert not played['tableNodePresent'] and played['playPileCount'] == 0, played
        assert after['energy'] == before['energy'] - 1 and after['block'] == before['block'], played
        assert after['enemies'][0]['hp'] == before['enemies'][0]['hp'], played
        assert poison(after['enemies'][0]) == 2, played
        send('enter', room='Boss')
        before = ready()
        assert all(poison(e) == 0 for e in before['enemies']), before
        # Includes Shield Bash: its original defensive portion must also be removed.
        for key in [cls + '_strike'] + ([signature] if signature else []):
            for upgrade in [False, True]:
                before = send('state')
                after = send('play', card=key, upgrade=upgrade)
                assert after['block'] == before['block'], (key, upgrade, before, after)
                assert after['enemies'][0]['hp'] == before['enemies'][0]['hp'], (key, after)
                assert poison(after['enemies'][0]) - poison(before['enemies'][0]) == (3 if upgrade else 2), (key, after)
        before = send('state')
        guarded = send('play', card=cls + '_guard')
        dexterity = sum(p['amount'] for p in before['powers'] if 'DEXTERITY' in p['id'])
        assert guarded['block'] - before['block'] == 4 + dexterity, guarded
        before = send('state')
        blocked = send('play', card={'knight': 'knight_sweeping_edge', 'mage': 'mage_arcane_missile', 'ranger': 'double'}[cls])
        assert blocked['enemies'] == before['enemies'] and blocked['block'] == before['block'], blocked
        # Native upgrade vars, text and portraits, with Pacifist snapshots on all Basics.
        cards = send('cards')
        assert cards['count'] == 720, cards
    send('abandon')
    print(tag + ': all three classes pass actual one-Energy starter play/discard; zero opening Poison; base/upgraded Mercy Poison with no Block or direct damage; ordinary Guards; blocked reward Attacks; 720 native cards per class.')
finally:
    (root / 'private/sts2-evidence' / ('pacifist116-' + tag + '.json')).write_text(json.dumps(evidence, indent=2), encoding='utf-8')
