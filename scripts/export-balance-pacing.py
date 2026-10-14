"""Export derived #146 evidence without distributing owned game files or saves.

Run after all balance146 workers stop. Interrupted receipts are retained in the
manifest, but never treated as completed campaigns. Superseded trajectories are
not counted twice. No profile paths, Steam identifiers, or save UUIDs are exported.
"""
import argparse
import collections
import csv
import hashlib
import json
import re
from pathlib import Path


def export(source, target):
    target.mkdir(parents=True, exist_ok=True)
    manifest, campaigns, trajectories, decisions = [], [], [], []
    cards = collections.Counter()
    superseded = {f'pacing-campaign-balance146-pacing-knight-regular{i}.json' for i in (1, 2, 3)}
    paths = sorted(source.glob('*campaign-balance146-*.json'))
    assert paths, 'No issue 146 receipts found'
    for path in paths:
        report = json.loads(path.read_text(encoding='utf-8'))
        assert report['status'] != 'running', f'Worker still active: {path.name}'
        pacing = path.name.startswith('pacing-')
        included = report['status'] == 'finished' and path.name not in superseded
        stages, seen = {}, set()
        for record in report['evidence']:
            request, response = record['request'], record['response']
            if included:
                assert response['ok'], 'Failed command in completed campaign'
            rid = request['requestId']
            assert re.fullmatch('[a-f0-9]{32}', rid) and response['requestId'] == rid
            assert rid not in seen, 'Duplicate command in canonical receipt'
            seen.add(rid)
            metadata = response['metadata']
            assert metadata['requestId'] == rid
            assert re.fullmatch('[a-f0-9]{64}', metadata['dllSha256'])
            assert re.fullmatch('[a-f0-9]{40}', metadata['sourceSha'])
            mods = {m['id']: m['version'] for m in metadata['mods']}
            assert set(mods) == {'BaseLib', 'SpireLegacy'}
            key = (metadata['sourceSha'], metadata['dllSha256'])
            stages[key] = dict(source=key[0], dllSha256=key[1], game=metadata['game'], mod=metadata['mod'], baseLib=mods['BaseLib'])
            result = response.get('result') or {}
            action, detail = result.get('action'), result.get('detail') or {}
            if not included or not action or action == 'wait':
                continue
            state = result.get('state') or {}
            decisions.append(dict(trial=path.stem, source=key[0], action=action,
                                  act=state.get('act'), floor=state.get('floor'), detail=detail))
            for offer in detail.get('offers', []):
                if isinstance(offer, dict) and 'id' in offer:
                    cards[path.stem, offer['id'], 'offered'] += 1
            picked = detail.get('picked')
            if isinstance(picked, str):
                cards[path.stem, picked, 'picked'] += 1
            for card, count in detail.get('entries', {}).items():
                cards[path.stem, card, 'handEntries'] += count
            for play in detail.get('plays', []):
                cards[path.stem, play['id'], 'played'] += 1
        profile = Path(report['evidence'][0]['response']['metadata']['profile'])
        log_counts = {}
        for log_name in ('launch.log', 'errors.log'):
            log = profile/log_name
            if log.exists():
                lines = log.read_text(encoding='utf-8', errors='replace').splitlines()
                log_counts[log_name] = {pattern: sum(pattern.lower() in line.lower() for line in lines)
                                        for pattern in ('ERROR', 'InvalidTaskId', 'double free', 'particles', 'backup')}
        manifest.append(dict(trial=path.stem, receiptSha256=hashlib.sha256(path.read_bytes()).hexdigest(),
                             status=report['status'], included=included, stages=list(stages.values()),
                             commands=len(seen), hasError=bool(report.get('error')),
                             checkpointRecoveries=len(report.get('continuations', [])), logMatches=log_counts))
        if not included:
            continue
        if not pacing:
            state = report['lastState']
            assert report['interventions'] == 0
            campaigns.append(dict(trial=path.stem, cls=report['cls'], tier=report['tier'],
                                  policy=report['policy'], rewardPolicy=report['rewardPolicy'],
                                  ascension=report['ascension'], seed=report['seed'], traits=','.join(report['traits']),
                                  floor=state['floor'], act=state['act'], hp=state['hp'], wins=state['wins'],
                                  crowns=state['crowns'], earned=state['lastEarned']))
            continue
        assert len(report['runs']) == 12
        purchases = {p['run']: p for p in report['purchases']}
        assert len(purchases) == len(report['purchases'])
        settled, bank = set(), report['runs'][0]['crownsBefore']
        for run in report['runs']:
            assert run['status'] == 'finished' and run['settledId'] not in settled
            settled.add(run['settledId'])
            assert run['crownsBefore'] == bank
            assert run['crownsAfter'] == bank + run['earned']
            purchase = purchases.get(run['run'])
            bank = run['crownsAfter']
            cost = 0
            if purchase:
                assert purchase['crownsBefore'] == bank and purchase['purchased']
                cost = bank - purchase['crownsAfter']
                assert cost > 0 and purchase['crownsAfter'] >= 0
                bank = purchase['crownsAfter']
            trajectories.append(dict(trial=path.stem, cls=report['cls'], run=run['run'], seed=run['seed'],
                                     floor=run['lastState']['floor'], earned=run['earned'],
                                     bankBefore=run['crownsBefore'], bankAfter=bank,
                                     upgrade=purchase['upgrade'] if purchase else '', cost=cost,
                                     runsSincePurchase=purchase['runsSincePurchase'] if purchase else '',
                                     checkpointRecovered=run.get('checkpointRecovered', False)))
        assert bank == sum(r['earned'] for r in report['runs']) - sum(p['crownsBefore']-p['crownsAfter'] for p in report['purchases'])
    (target/'manifest.json').write_text(json.dumps(manifest, indent=2)+'\n', encoding='utf-8')
    (target/'decisions.jsonl').write_text(''.join(json.dumps(d)+'\n' for d in decisions), encoding='utf-8')
    for name, rows in [('campaigns', campaigns), ('trajectories', trajectories),
                       ('cards', [dict(trial=t, card=c, observation=o, count=n) for (t,c,o),n in sorted(cards.items())])]:
        assert rows, name
        with (target/(name+'.csv')).open('w', newline='', encoding='utf-8') as stream:
            writer = csv.DictWriter(stream, fieldnames=list(rows[0]))
            writer.writeheader(); writer.writerows(rows)
    print(f'Exported {len(campaigns)} completed campaigns, {len(trajectories)} settled returns, {len(decisions)} decisions')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('source', type=Path)
    parser.add_argument('target', type=Path)
    args = parser.parse_args()
    export(args.source, args.target)
