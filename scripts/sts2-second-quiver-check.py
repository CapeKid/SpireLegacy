"""Paid native selective reload checks; deterministic choices in isolated beta labs."""
import json,os,sys,time
from pathlib import Path
from oracle_protocol import call
profile=Path(sys.argv[1]);records=[]
def send(command,**args):
 r=call(profile,dict(command=command,**args),timeout=90,expected_hash=os.environ['HEIR_EXPECTED_DLL_SHA256']);records.append(r)
 (profile/'second-quiver-receipts.json').write_text(json.dumps(records,indent=2))
 assert r['response']['ok'],r
 assert {m['id'] for m in r['response']['metadata']['mods']}=={'BaseLib','SpireLegacyBeta'}
 return r['response']['result']
send('trait-speed',value=8)
for up in [False,True]:
 for mode in ['full','partial','empty','skills','cancel','replay','hand-cap','rhythm','pacifist','meditate']:
  send('start',**{'class':'ranger','traits':['cantattack'] if mode=='pacifist' else [],'quick':True,'manorTier':0,'seed':'SECOND-QUIVER'})
  for _ in range(150):
   if send('state')['phase']=='Play':break
   time.sleep(.1)
  result=send('second-quiver',upgrade=up,mode=mode)
  print(up,mode,result,'passed',flush=True)
  send('abandon')
