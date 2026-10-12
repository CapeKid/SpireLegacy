"""Native immutable-family save/resume and guarded adoption regression."""
import hashlib,json,sys,time
from pathlib import Path
from oracle_protocol import call
root=Path(__file__).resolve().parents[1];profile=root/sys.argv[1];tag=sys.argv[2];evidence=[]
expected=hashlib.sha256((root/'sts2/bin/Release/net9.0/SpireLegacy.dll').read_bytes()).hexdigest()
def send(command,**args):
 record=call(profile,dict(command=command,**args),timeout=90,expected_hash=expected);evidence.append(record);reply=record['response'];assert reply['ok'],reply
 assert {m['id'] for m in reply['metadata']['mods']}=={'BaseLib','SpireLegacy'},reply['metadata'];return reply['result']
def ready():
 deadline=time.monotonic()+30
 while time.monotonic()<deadline:
  if send('state')['phase']=='Play':return
  time.sleep(.1)
 raise TimeoutError('Playable phase')
try:
 send('compat-control',patches=True,upgrade=False)
 for cls in ['knight','mage','ranger']:
  for upgrade in [False,True]:
   send('start',**{'class':cls,'traits':['easybreakables'],'quick':True});ready()
   played=send('expansion-play',card=cls+'_strike',upgrade=upgrade);assert played['pile']=='Discard' and not played['tableNodePresent']
   snapshot=send('persistence-state')['snapshot'];send('save');changed=send('persistence-menu');assert changed['selected']!=cls and changed['snapshot']==snapshot
   send('resume');ready();restored=send('persistence-state')
   before=json.loads(snapshot);after=json.loads(restored['snapshot'])
   # Floor/quick are runtime lifecycle counters, not immutable inherited effects.
   for field in ['floors','quick']:before.pop(field);after.pop(field)
   assert after==before,restored
   guarded=send('persistence-adoption');assert guarded==dict(refusals=2,failures=5,familyUnchanged=True),guarded
   played=send('manual',card=cls+'_guard');assert played['accepted'] and played['pile']=='Discard' and not played['tableNodePresent']
   print(tag,cls,upgrade,'immutable resume and guarded adoption passed',flush=True)
 send('abandon');send('select')
finally:
 (root/'private/sts2-evidence'/('persistence-'+tag+'.json')).write_text(json.dumps(evidence,indent=2),encoding='utf-8')
