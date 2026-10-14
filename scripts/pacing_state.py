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
            if purchase['level']!=manor.get(purchase['upgrade'],0)+1:
                raise ValueError('Purchase skipped a manor level')
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


def completed_prefix(report, cls):
    """Extract settled receipts only; never resume/replay the interrupted tail."""
    import copy
    if report.get('status') not in ('running','error'):
        raise ValueError('An interrupted report is required')
    result=copy.deepcopy(report)
    runs=[]
    for run in result['runs']:
        if run['status']!='finished' or 'earned' not in run:break
        runs.append(run)
    if not runs:raise ValueError('No settled prefix')
    result.update(status='finished',runs=runs,targetReturns=len(runs),stopReason='settled-checkpoint')
    result.pop('error',None)
    result['purchases']=[p for p in result['purchases'] if p['run']<=len(runs)]
    for batch in result.get('batches',[]):batch['endRun']=min(batch['endRun'],len(runs))
    expected=completed(dict(result,evidence=[r for r in result['evidence'] if r['response']['ok']]),cls)
    for index in range(len(result['evidence'])-1,-1,-1):
        record=result['evidence'][index]
        if record['request'].get('command')!='lifecycle-state' or not record['response']['ok']:continue
        try:assert_live(expected,record['response']['result'])
        except ValueError:continue
        result['evidence']=result['evidence'][:index+1]
        durable_snapshot(result,cls)
        result['checkpointExtraction']=dict(settledReturns=len(runs),excludedRuns=[r['run'] for r in report['runs'][len(runs):]])
        return result
    raise ValueError('No matching completed durable snapshot; do not synthesize settlement')


if __name__=='__main__':
    import hashlib,json,sys
    from pathlib import Path
    private=(Path(__file__).resolve().parents[1]/'private').resolve()
    source=Path(sys.argv[1]).resolve();target=Path(sys.argv[3]).resolve()
    extract='--extract' in sys.argv
    if not source.is_relative_to(private) or not target.is_relative_to(private) or target.exists() or (not extract and target.name!='family.json'):
        raise ValueError('Use a private recorded source and a fresh private target')
    original=json.loads(source.read_text())
    result=completed_prefix(original,sys.argv[2]) if extract else durable_snapshot(original,sys.argv[2])
    if extract:result['checkpointExtraction'].update(priorReport=source.name,receiptSha256=hashlib.sha256(source.read_bytes()).hexdigest())
    target.write_text(json.dumps(result,indent=2),encoding='utf-8')
