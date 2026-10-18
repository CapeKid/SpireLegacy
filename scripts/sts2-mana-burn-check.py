"""Issue #184: native paid Mana Burn sequencing in an isolated beta installation."""
import json,os,sys,time
from pathlib import Path
from oracle_protocol import call
profile=Path(sys.argv[1]);records=[]
def send(command,**args):
 r=call(profile,dict(command=command,**args),timeout=90,expected_hash=os.environ['HEIR_EXPECTED_DLL_SHA256']);records.append(r)
 (profile/'mana-burn-receipts.json').write_text(json.dumps(records,indent=2))
 assert r['response']['ok'],r
 assert {m['id'] for m in r['response']['metadata']['mods']}=={'BaseLib','SpireLegacyBeta'}
 return r['response']['result']
send('trait-speed',value=8)
for up in [False,True]:
 for mode in ['plain','charged','flow','one','uncharged-attack','reset','blocked','replay','replay-charge','foreign']:
  send('start',**{'class':'knight' if mode=='foreign' else 'mage','traits':[],'quick':True,'manorTier':0,'seed':'MANA-BURN-184'})
  for _ in range(150):
   if send('state')['phase']=='Play':break
   time.sleep(.1)
  result=send('mana-burn',upgrade=up,mode=mode)
  print(up,mode,result,'passed',flush=True)
  send('abandon')
