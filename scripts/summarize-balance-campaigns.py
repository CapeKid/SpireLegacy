"""Export selected immutable campaign journals as public, non-private CSV evidence.

Usage: summarize-balance-campaigns.py [--prefix NAME] TAG [TAG ...]
Offer/pick counts describe a deterministic pilot, never human pick rates.
"""
from collections import Counter
import csv
import json
from pathlib import Path
import sys

root = Path(__file__).resolve().parents[1]
arguments=sys.argv[1:]
prefix='balance-campaign'
if arguments[:1]==['--prefix']:
    prefix=arguments[1]
    assert prefix and all(c.isalnum() or c in '-_' for c in prefix)
    arguments=arguments[2:]
trials, observations = [], []
for tag in arguments:
    assert tag and all(c.isalnum() or c in '-_' for c in tag)
    journal = json.loads((root/'private/sts2-evidence'/f'balance-campaign-{tag}.json').read_text())
    assert journal['status'] != 'running', 'An unfinished journal is not evidence.'
    replies = [e['response'] for e in journal['evidence']]
    assert replies and all(r['ok'] for r in replies), 'Failed commands are diagnostic evidence, not trials.'
    hashes = {r['metadata']['dllSha256'] for r in replies}
    games = {r['metadata']['game'] for r in replies}
    assert len(hashes) == len(games) == 1
    turns = [r['result'] for r in replies if r['result'].get('action') == 'combat-turn']
    rewards = [r['result']['detail'] for r in replies if r['result'].get('action') == 'card-reward']
    state = journal['lastState']
    base = dict(tag=tag, game=next(iter(games)), cls=journal['cls'], tier=journal['tier'],
                policy=journal['policy'], seed=journal['seed'], ascension=journal['ascension'],
                traits=';'.join(journal['traits']),rewardPolicy=journal.get('rewardPolicy','standalone'))
    trials.append(dict(**base, status=journal['status'], act=state['act'], floor=state['floor'],
                       dead=state['hp'] == 0 and not state.get('wins', 0), wins=state.get('wins', 0), finalHp=state['hp'], maxHp=state['maxHp'],
                       crowns=state['lastEarned'], deckSize=len(state['deck']),
                       turns=len(turns), potionUses=sum(len(t['detail'].get('potions',[])) for t in turns), rewardChoices=len(rewards),skippedRewards=sum(r.get('skip',False) for r in rewards), dllSha256=next(iter(hashes))))
    entries, played, left, offers, picks = (Counter() for _ in range(5))
    for turn in turns:
        entries.update(turn['detail']['entries'])
        played.update(p['id'] for p in turn['detail']['plays'])
        left.update(turn['detail']['unplayed'])
    for reward in rewards:
        offers.update(c['id'] for c in reward['offers'])
        if reward['picked'] is not None:picks.update([reward['picked']])
    for card in sorted(entries.keys() | played.keys() | left.keys() | offers.keys() | picks.keys()):
        observations.append(dict(**base, card=card, handEntries=entries[card], plays=played[card],
                                 unplayedAtTurnEnd=left[card], offered=offers[card], picked=picks[card]))

assert trials, 'Select at least one journal.'
for name, rows in [(prefix+'s.csv', trials), (prefix+'-cards.csv', observations)]:
    path = root/'docs'/name
    with path.open('w', newline='', encoding='utf-8') as stream:
        writer = csv.DictWriter(stream, fieldnames=list(rows[0]))
        writer.writeheader()
        writer.writerows(rows)
    print(path, len(rows), 'rows')
