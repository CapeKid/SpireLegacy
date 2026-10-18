"""Native hand UI lifetime regression; replacement synchronizer is fault injection."""
import hashlib,json,os,sys,time
from pathlib import Path
from oracle_protocol import call
root=Path(__file__).resolve().parents[1];profile=Path(sys.argv[1]).resolve()
baseline='--baseline' in sys.argv[2:]
expected=os.environ.get('HEIR_EXPECTED_DLL_SHA256') or hashlib.sha256((root/'sts2/bin/Release/net9.0/SpireLegacy.dll').read_bytes()).hexdigest();records=[]
def send(command,**args):
    r=call(profile,dict(command=command,**args),timeout=90,expected_hash=expected)
    records.append(r);(profile/'choice-lifetime-receipts.json').write_text(json.dumps(records,indent=2))
    assert r['response']['ok'],r
    return r['response']['result']
def start():
    send('start',**{'class':'ranger','traits':['bounceterrain','disposition'],'quick':True,'manorTier':0,'seed':'CHOICE-LIFETIME'})
    for _ in range(150):
        if send('state')['phase']=='Play':return
        time.sleep(.1)
    raise TimeoutError('combat')
if not baseline:
    for upgraded in [False,True]:
        start()
        send('card-coop-prepare',card='mage_cold_read',upgrade=upgraded,replay=False)
        before=send('manual',card='mage_cold_read')
        assert before['accepted'] and before['pile']=='Play',before
        send('retention-confirm',count=2 if upgraded else 1)
        after=send('manual',card='ranger_guard')
        assert after['pile']=='Discard' and after['action'] is None and not after['tableNodePresent'],after
        assert len(before['state']['hand'])-len(after['state']['hand'])==(3 if upgraded else 2),(before,after)
        assert after['state']['energy']==before['state']['energy']-1,(before,after)
        print('paid Cold Read',upgraded,'real UI and next card passed',flush=True)
        send('abandon')
for upgraded in [False,True]:
    for mode in (['changed-sync'] if baseline or '--release-smoke' in sys.argv else ['valid','changed-sync','ended-run','active-sync-error']):
        start()
        result=send('card-choice-lifetime',upgrade=upgraded,mode=mode)
        assert result['unscopedRejected'],result
        if baseline:
            assert result['error']=='ArgumentOutOfRangeException' and 'GetChoiceId' in result['stack'] and (result['diagnosticChanged'] or upgraded),result
        elif mode=='valid':assert result['error'] is None and not result['diagnosticChanged'],result
        elif mode=='active-sync-error':assert result['error']=='ArgumentOutOfRangeException' and not result['cancelled'],result
        else:assert result['cancelled'] and not result['diagnosticChanged'],result
        print(upgraded,mode,result['error'],flush=True)
        send('abandon')
