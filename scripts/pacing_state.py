"""Receipt-only validation for extending a completed natural manor trajectory."""
ORDER=['vitality','armory','smith','garden','library','vitality','smith','armory','garden',
       'vitality','vitality','vitality','vault','vault','vault','vault']


def completed(report, cls):
    if report.get('status')!='finished' or report.get('cls')!=cls or not report.get('runs'):
        raise ValueError('Only a completed matching-class trajectory may continue')
    evidence=report.get('evidence',[])
    if not evidence or any(not r['response']['ok'] for r in evidence):
        raise ValueError('Successful recorded commands required; never retry failed mutations')
    ids=[r['request']['requestId'] for r in evidence]
    if len(ids)!=len(set(ids)) or any(r['request']['requestId']!=r['response']['requestId'] for r in evidence):
        raise ValueError('Duplicate or mismatched command receipts')
    purchases=report['purchases'];byrun={p['run']:p for p in purchases}
    if len(byrun)!=len(purchases) or [p['upgrade'] for p in purchases]!=ORDER[:len(purchases)]:
        raise ValueError('Purchase order differs from declared trajectory policy')
    settled=set();bank=report['runs'][0]['crownsBefore'];manor={}
    for number,run in enumerate(report['runs'],1):
        if run['run']!=number or run['status']!='finished' or not run.get('settledId') or run['settledId'] in settled:
            raise ValueError('Contiguous unique durable settlements required')
        settled.add(run['settledId'])
        if run['crownsBefore']!=bank or run['crownsAfter']!=bank+run['earned']:
            raise ValueError('Recorded bank does not conserve earned crowns')
        bank=run['crownsAfter'];purchase=byrun.get(number)
        if purchase:
            if not purchase['purchased'] or purchase['crownsBefore']!=bank or not 0<=purchase['crownsAfter']<bank:
                raise ValueError('Invalid paid purchase receipt')
            bank=purchase['crownsAfter'];manor[purchase['upgrade']]=purchase['level']
    if any(n not in range(1,len(report['runs'])+1) for n in byrun):raise ValueError('Purchase outside trajectory')
    return dict(run=report['runs'][-1]['run'],crowns=bank,manor=manor,
                settledId=report['runs'][-1]['settledId'],generation=report['runs'][-1]['generation'])


def assert_live(expected, observed):
    profile=observed['profile']
    if profile!=observed['persisted'] or profile['active'] is not None:
        raise ValueError('Completed durable family required; unfinished saves cannot extend')
    if any(profile[k]!=expected[k] for k in ('crowns','settledId','generation')):
        raise ValueError('Live family differs from the last completed receipt')
    if {k:v for k,v in profile['manor'].items() if v}!={k:v for k,v in expected['manor'].items() if v}:
        raise ValueError('Live manor differs from paid purchases')


def durable_snapshot(report, cls):
    expected=completed(report,cls)
    record=next((r for r in reversed(report['evidence']) if r['request'].get('command')=='lifecycle-state'),None)
    if record is None:raise ValueError('Durable family snapshot missing')
    observed=record['response']['result'];assert_live(expected,observed)
    return observed['persisted']


if __name__=='__main__':
    import json,sys
    from pathlib import Path
    private=(Path(__file__).resolve().parents[1]/'private').resolve()
    source=Path(sys.argv[1]).resolve();target=Path(sys.argv[3]).resolve()
    if not source.is_relative_to(private) or not target.is_relative_to(private) or target.name!='family.json' or target.exists():
        raise ValueError('Use a private recorded source and a fresh private family.json target')
    target.write_text(json.dumps(durable_snapshot(json.loads(source.read_text()),sys.argv[2])),encoding='utf-8')
