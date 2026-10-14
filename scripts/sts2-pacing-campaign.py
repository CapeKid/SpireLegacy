"""Natural family trajectory: carry actual crowns/Chronicle and buy with earned funds."""
import hashlib,json,sys,time
from pathlib import Path
from oracle_protocol import call
root=Path(__file__).resolve().parents[1];profile=Path(sys.argv[1]);tag,cls=sys.argv[2:4]
count=12;expected=hashlib.sha256((root/'sts2/bin/Release/net9.0/SpireLegacy.dll').read_bytes()).hexdigest()
output=root/'private/sts2-evidence'/f'pacing-campaign-{tag}.json';assert not output.exists()
report=dict(cls=cls,status='running',runs=[],purchases=[],evidence=[])
def save():output.parent.mkdir(exist_ok=True);output.write_text(json.dumps(report,indent=2),encoding='utf-8')
def send(command,**args):
    record=call(profile,dict(command=command,**args),timeout=180,expected_hash=expected)
    report['evidence'].append(record);save();assert record['response']['ok'],record
    return record['response']['result']
try:
    assert not (profile/'family.json').exists()
    order=['vitality','armory','smith','garden','library','vitality','smith','armory','garden','vitality','vitality','vitality','vault','vault','vault','vault']
    purchased=0;last_purchase=0
    for number in range(1,count+1):
        before=send('lifecycle-state')['profile']
        args=dict(**{'class':cls},traits=[],seed='PACING146-'+str(number),ascension=0)
        if number==1:args.update(manorTier=0,quick=False)
        send('start',**args)
        entry=dict(run=number,seed=args['seed'],crownsBefore=before['crowns'],manorBefore=before['manor'],status='running');report['runs'].append(entry)
        deadline=time.monotonic()+1200;stalls=0
        for step in range(1200):
            assert time.monotonic()<deadline,'20-minute run cap'
            result=send('balance-campaign-step',policy='planned',rewardPolicy='defensive')
            entry['lastState']=result['state'];action=result['action']
            if action=='finished':entry['status']='finished';break
            assert not action.startswith('unsupported') and action not in ('play-cap','turn-cap'),result
            stalls=stalls+1 if action=='wait' else 0;assert stalls<30,'Stalled trajectory'
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
    report['status']='finished'
except Exception as error:report.update(status='error',error=str(error));raise
finally:save()
