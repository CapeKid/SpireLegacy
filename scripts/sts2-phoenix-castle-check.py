"""Exercise Phoenix Forge and Castle Heart through paid plays and real native turn boundaries."""
import json, os, sys, time
from pathlib import Path
from oracle_protocol import call
profile=Path(sys.argv[1]);records=[]
def send(command, **args):
 record=call(profile,dict(command=command,**args),timeout=150,expected_hash=os.environ['HEIR_EXPECTED_DLL_SHA256'])
 records.append(record)
 (profile/'phoenix-castle-receipts.json').write_text(json.dumps(records,indent=2),encoding='utf-8')
 assert record['response']['ok'],record
 return record['response']['result']
send('trait-speed',value=8)
for up in [False,True]:
 send('start',**{'class':'knight','traits':[],'quick':True,'manorTier':0,'seed':'PHOENIX-CASTLE'})
 for _ in range(150):
  if send('state')['phase']=='Play':break
  time.sleep(.1)
 else:raise TimeoutError('Play phase')
 for mode in ['full','stacked','ethereal','legacy-capped']:
  result=send('card-roles',card='knight_phoenix_forge',upgrade=up,mode=mode)
  print(up,mode,result,'passed',flush=True)
 result=send('card-roles',card='knight_castle_heart',upgrade=up,mode='full')
 print(up,'Castle Heart',result,'passed',flush=True)
 send('screenshot')
 send('abandon')
print('10 Phoenix Forge / Castle Heart scenarios passed.')
