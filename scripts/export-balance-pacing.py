"""Export derived #146 evidence without distributing owned game files or saves.

Run after all balance146 workers stop. Interrupted receipts are retained in the
manifest, but never treated as completed campaigns. Superseded trajectories are
not counted twice. No profile paths, Steam identifiers, or save UUIDs are exported.
"""
if not __debug__:
    raise RuntimeError('Balance evidence export checks require assertions; do not use Python -O.')
import argparse
import collections
import csv
import hashlib
import json
import re
from pathlib import Path


def export(source, target, stopped_reports=()):
    target.mkdir(parents=True, exist_ok=True)
    manifest, campaigns, trajectories, decisions = [], [], [], []
    cards = collections.Counter()
    superseded = {f'pacing-campaign-balance146-pacing-knight-regular{i}.json' for i in (1, 2, 3)}
    paths = sorted(source.glob('*campaign-balance146-*.json'))
    assert paths, 'No issue 146 receipts found'
    reports={path.name:json.loads(path.read_text(encoding='utf-8')) for path in paths}
    assert set(stopped_reports)<=set(reports), 'Unknown explicitly stopped report'
    assert all(reports[name]['status']=='running' for name in stopped_reports), 'Only interrupted running receipts need explicit stopped annotation'
    for name,data in reports.items():
        if data['status']=='finished':
            checkpoint=data.get('checkpointExtraction')
            if checkpoint:
                from pacing_state import completed_prefix
                prior_path=source/checkpoint['priorReport']
                assert hashlib.sha256(prior_path.read_bytes()).hexdigest()==checkpoint['receiptSha256']
                expected=completed_prefix(reports[checkpoint['priorReport']],data['cls'])
                assert all(data[key][:len(expected[key])]==expected[key] for key in ('runs','purchases','evidence')), 'Changed extracted durable checkpoint prefix'
                assert all(checkpoint[key]==expected['checkpointExtraction'][key] for key in ('settledReturns','excludedRuns')), 'Changed checkpoint exclusion metadata'
                if 'fullManor' in checkpoint:assert checkpoint['fullManor']==expected['checkpointExtraction']['fullManor']
                superseded.add(checkpoint['priorReport'])
            for batch in data.get('batches',[]):
                prior=reports[batch['priorReport']]
                assert prior['status']=='finished' and prior['cls']==data['cls']
                assert data['runs'][:len(prior['runs'])]==prior['runs'], 'Changed completed trajectory prefix'
                assert data['purchases'][:len(prior['purchases'])]==prior['purchases'], 'Changed prior paid purchases'
                assert data['evidence'][:len(prior['evidence'])]==prior['evidence'], 'Changed prior command receipts'
                assert batch['afterRun']==len(prior['runs']) and batch['endRun']>batch['afterRun']
                assert batch['endRun']<=len(data['runs']) or data.get('stopReason')=='full-manor'
                superseded.add(batch['priorReport'])
    for path in paths:
        report = json.loads(path.read_text(encoding='utf-8'))
        assert report['status'] != 'running' or path.name in stopped_reports, f'Worker still active: {path.name}'
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
        # A continued trajectory can span several private profiles. These counts
        # describe the latest launch only; prior logs remain separately archived.
        profile = Path(report['evidence'][-1]['response']['metadata']['profile'])
        log_counts = {}
        for log_name in ('launch.log', 'errors.log'):
            log = profile/log_name
            if log.exists():
                lines = log.read_text(encoding='utf-8', errors='replace').splitlines()
                log_counts[log_name] = {pattern: sum(pattern.lower() in line.lower() for line in lines)
                                        for pattern in ('ERROR', 'InvalidTaskId', 'double free', 'particles', 'backup')}
        manifest.append(dict(trial=path.stem, receiptSha256=hashlib.sha256(path.read_bytes()).hexdigest(),
                             status='interrupted' if path.name in stopped_reports else report['status'], included=included, stages=list(stages.values()),
                             commands=len(seen), hasError=bool(report.get('error')),
                             checkpointRecoveries=len(report.get('continuations', [])), batches=report.get('batches',[]),
                             targetReturns=report.get('targetReturns'), returnCap=report.get('returnCap'),
                             stopReason=report.get('stopReason'), stopAtFullManor=report.get('stopAtFullManor'),
                             checkpointExtraction=report.get('checkpointExtraction'), logMatches=log_counts))
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
        assert len(report['runs']) == report.get('targetReturns',12)
        if report.get('stopReason') in ('full-manor','full-manor-checkpoint'):
            from pacing_state import ORDER,completed
            completed(report,report['cls'])
            assert [p['upgrade'] for p in report['purchases']]==ORDER
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
    parser.add_argument('--stopped-report', action='append',default=[],help='Explicitly verified stopped worker; preserve raw running receipt hash, exclude its outcomes')
    args = parser.parse_args()
    export(args.source, args.target,args.stopped_report)
