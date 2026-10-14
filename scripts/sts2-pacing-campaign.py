"""Natural family trajectory: carry actual crowns/Chronicle and buy with earned funds."""
import hashlib,json,sys,time
from pathlib import Path
from oracle_protocol import call
from pacing_state import ORDER,completed,assert_live
root=Path(__file__).resolve().parents[1];profile=Path(sys.argv[1]);tag,cls=sys.argv[2:4]
count=int(sys.argv[sys.argv.index('--end-run')+1]) if '--end-run' in sys.argv else 12
assert 1<=count<=200
stop_full='--stop-when-full' in sys.argv
expected=hashlib.sha256((root/'sts2/bin/Release/net9.0/SpireLegacy.dll').read_bytes()).hexdigest()
output=root/'private/sts2-evidence'/f'pacing-campaign-{tag}.json';assert not output.exists()
journal=output.with_suffix('.jsonl');assert not journal.exists()
report=dict(cls=cls,status='running',runs=[],purchases=[],evidence=[])
resume='--resume-report' in sys.argv
extend='--continue-report' in sys.argv
assert not (resume and extend)
continuity=None
if extend:
    prior=Path(sys.argv[sys.argv.index('--continue-report')+1]).resolve()
    assert prior.is_relative_to((root/'private').resolve())
    report=json.loads(prior.read_text());continuity=completed(report,cls)
    assert count>continuity['run']
    report.setdefault('batches',[]).append(dict(priorReport=prior.name,afterRun=continuity['run'],endRun=count,
                                             forkedFromDurableReceipt='--forked-profile' in sys.argv))
    report['status']='running'
if resume:
    prior=Path(sys.argv[sys.argv.index('--resume-report')+1]).resolve()
    assert prior.is_relative_to((root/'private').resolve())
    report=json.loads(prior.read_text());assert report['status']=='error' and report['cls']==cls
    # Never resume a timed-out or failed mutation. This path only recovers a
    # completed terminal observation whose settlement assertion ran too early.
    assert all(r['response']['ok'] for r in report['evidence'])
    last=report['evidence'][-1]['response'].get('result',{})
    supported=last.get('action')=='unsupported-overlay' and last.get('detail',{}).get('type')=='NChooseABundleSelectionScreen'
    assert (report['runs'][-1]['status']=='finished' or supported) and 'earned' not in report['runs'][-1]
    if 'continuation' in report:report.setdefault('continuations',[]).append(report.pop('continuation'))
    report.setdefault('continuations',[]).append(dict(priorReport=prior.name,reason='native-checkpoint recovery after early settlement observation or unsupported bundle'))
    report.update(status='running')
    report.pop('error',None)
def save():output.parent.mkdir(exist_ok=True);output.write_text(json.dumps(report,indent=2),encoding='utf-8')
def send(command,**args):
    record=call(profile,dict(command=command,**args),timeout=180,expected_hash=expected)
    # Append every receipt immediately; avoid rewriting an increasingly large
    # trajectory on every decision. Full reports checkpoint every 25 receipts
    # and at every durable return/exception. A crash journal is not a replay API.
    with journal.open('a',encoding='utf-8') as stream:
        stream.write(json.dumps(record)+'\n');stream.flush()
    report['evidence'].append(record)
    if not output.exists() or len(report['evidence'])%25==0:save()
    assert record['response']['ok'],record
    return record['response']['result']
try:
    assert resume or extend or not (profile/'family.json').exists()
    order=ORDER
    if extend:assert_live(continuity,send('lifecycle-state'))
    purchased=len(report['purchases']);last_purchase=report['purchases'][-1]['run'] if purchased else 0
    first=report['runs'][-1]['run'] if resume else continuity['run']+1 if extend else 1
    report['targetReturns']=count
    report['returnCap']=count
    report['stopAtFullManor']=stop_full
    for number in range(first,count+1):
        before=send('lifecycle-state')['profile']
        args=dict(**{'class':cls},traits=[],seed='PACING146-'+str(number),ascension=0)
        if number==1:args.update(manorTier=0,quick=False)
        if resume and number==first:
            assert before['active'] is not None,'Recorded unfinished native run required'
            send('resume');entry=report['runs'][-1];entry.update(status='running',checkpointRecovered=True)
        else:
            send('start',**args)
            entry=dict(run=number,seed=args['seed'],crownsBefore=before['crowns'],manorBefore=before['manor'],status='running');report['runs'].append(entry)
        deadline=time.monotonic()+1200;waiting_since=None
        for step in range(1200):
            assert time.monotonic()<deadline,'20-minute run cap'
            result=send('balance-campaign-step',policy='planned',rewardPolicy='defensive')
            entry['lastState']=result['state'];action=result['action']
            if action=='finished':entry['status']='finished';break
            assert not action.startswith('unsupported') and action not in ('play-cap','turn-cap'),result
            if action=='wait':
                waiting_since=waiting_since or time.monotonic()
                assert time.monotonic()-waiting_since<45,('Stalled trajectory',result)
            else:waiting_since=None
            time.sleep(.2)
        else:raise AssertionError('Decision cap')
        final=send('lifecycle-state');assert final['profile']['active'] is None and final['profile']==final['persisted'],final
        after=final['profile'];entry.update(crownsAfter=after['crowns'],earned=after['crowns']-before['crowns'],generation=after['generation'],settledId=after['settledId'])
        send('select')
        if purchased<len(order):
            purchase=send('campaign-purchase',upgrade=order[purchased])
            if purchase['purchased']:
                purchase.update(run=number,runsSincePurchase=number-last_purchase)
                assert purchase['crownsBefore']==after['crowns'] and purchase['crownsAfter']>=0 and purchase['crownsAfter']<purchase['crownsBefore'],purchase
                report['purchases'].append(purchase);purchased+=1;last_purchase=number
                durable=send('lifecycle-state');assert durable['profile']==durable['persisted']
        print(tag,number,entry['lastState']['floor'],entry['earned'],after['crowns'],'purchases',purchased,flush=True);save()
        if stop_full and purchased==len(order):
            report.update(targetReturns=number,stopReason='full-manor');break
    else:report['stopReason']='return-cap'
    report['status']='finished'
except Exception as error:report.update(status='error',error=str(error));raise
finally:save()
