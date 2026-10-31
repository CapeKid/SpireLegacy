"""Inspect Hollow Bones sprite opacity, size, palettes and room rendering."""
import json, os, re, shutil, sys, time
from pathlib import Path
from oracle_protocol import call
root=Path(__file__).resolve().parents[1];profile=Path(sys.argv[1]);records=[]
def send(command,**args):
 record=call(profile,dict(command=command,**args),timeout=150,expected_hash=os.environ['HEIR_EXPECTED_DLL_SHA256'])
 records.append(record);(profile/'hollow-bones-receipts.json').write_text(json.dumps(records,indent=2),encoding='utf-8')
 assert record['response']['ok'],record
 return record['response']['result']
def ready():
 for _ in range(150):
  state=send('state')
  if state['phase']=='Play':time.sleep(.3);return state
  time.sleep(.1)
 raise TimeoutError('Play phase')
def start(cls,traits):
 send('start',**{'class':cls,'traits':traits,'quick':True,'manorTier':0,'ascension':0,'seed':'HOLLOW-BONES'})
 return ready()
def appearance(room=False,opacity=.75):
 values=send('trait-appearance');matches=[r for r in values if r['room']==room];assert len(matches)==1,values
 look=matches[0];assert abs(look['opacity']-opacity)<.001,look
 if not room:
  visual=[r for r in send('visual') if r['visible']];assert len(visual)==1,visual
  assert visual[0]['bannerVisible'] and visual[0]['bannerOpacity']==1 and visual[0]['symbolOpacity']==1,visual
 return look
def scale(look):return [abs(float(n)) for n in re.findall(r'-?\d+(?:\.\d+)?',look['spriteScale'])]
classes=json.loads((root/'sheets/classes.json').read_text(encoding='utf-8'))
for cls in classes:
 baselines={};start(cls['id'],[])
 baselines['Combat']=scale(appearance(opacity=1))
 for room in ['RestSite','Shop']:
  send('enter',room=room);time.sleep(.4);baselines[room]=scale(appearance(True,1))
 state=start(cls['id'],['lowergravity'])
 assert state['maxHp']==cls['hp']-12,state
 assert sum(p['amount'] for p in state['powers'] if p['id'].endswith('DEXTERITY_POWER'))==2,state
 assert scale(appearance())==baselines['Combat']
 shot=send('screenshot');shutil.copy2(shot['path'],profile/(cls['id']+'-translucent-combat.png'))
 for room in ['RestSite','Shop']:
  send('enter',room=room);time.sleep(.4);assert scale(appearance(True))==baselines[room]
  shot=send('screenshot');shutil.copy2(shot['path'],profile/(cls['id']+'-translucent-'+room+'.png'))
 print(cls['id'],'normal size and 75% opacity in combat/rest/shop; HP/Dexterity preserved',flush=True)
for index,partner in enumerate(['nocolor','oldyellowtint','colortrails','youareblue','disposition','halloweenholiday','christmasholiday']):
 cls=['knight','mage','ranger'][index%3];start(cls,['lowergravity',partner]);look=appearance()
 assert look['gray']==(partner=='nocolor') and look['sepia']==(partner=='oldyellowtint'),look
 assert look['rainbow']==(partner=='colortrails'),look
 for room in ['RestSite','Shop']:
  send('enter',room=room);time.sleep(.4);appearance(True)
 print(partner,'palette combination passed',flush=True)
for partner,factor in [('small',.8),('large',1.3)]:
 start('knight',[]);baseline=scale(appearance(opacity=1))
 start('knight',['lowergravity',partner]);actual=scale(appearance())
 assert all(abs(a-b*factor)<.001 for a,b in zip(actual,baseline)),(partner,actual,baseline)
start('ranger',['lowergravity','fakeselfdamage']);baseline=appearance();before=send('state')
after=send('injure',amount=3);assert after['hp']==before['hp']-3
damaged=appearance();assert damaged['flash']>0,damaged
time.sleep(1.1);settled=appearance();assert settled['flash']==0 and settled['tint']==baseline['tint'],settled
send('save');send('resume');ready();appearance()
send('abandon');print('All Hollow Bones appearance checks passed.',flush=True)
