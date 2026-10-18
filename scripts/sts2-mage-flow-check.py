"""Issue #185: paid native Arcane Flow checks in an isolated owned-game lab."""
import hashlib,json,os,sys,time
from pathlib import Path
from oracle_protocol import call

root=Path(__file__).resolve().parents[1];profile=Path(sys.argv[1]).resolve()
expected=os.environ.get('HEIR_EXPECTED_DLL_SHA256') or hashlib.sha256((root/'sts2/bin/Release/net9.0/SpireLegacy.dll').read_bytes()).hexdigest()
records=[]
def send(command,**args):
    record=call(profile,dict(command=command,**args),timeout=90,expected_hash=expected)
    records.append(record);(profile/'mage-flow-receipts.json').write_text(json.dumps(records,indent=2))
    assert record['response']['ok'],record
    return record['response']['result']
def start(cls):
    send('start',**{'class':cls,'traits':[],'quick':True,'manorTier':0,'seed':'MAGE-FLOW-185'})
    for _ in range(150):
        if send('state')['phase']=='Play':return
        time.sleep(.1)
    raise TimeoutError('combat')
send('trait-speed',value=8)
for upgraded in [False,True]:
    for charges,mode in [(0,'normal'),(1,'normal'),(2,'normal'),(4,'normal'),(2,'replay'),(2,'blocked'),(2,'full'),(2,'reservoir')]:
        start('mage');result=send('mage-flow',charges=charges,upgrade=upgraded,mode=mode)
        print('mage flow',upgraded,charges,mode,result['draws'],'passed',flush=True)
        send('abandon')
