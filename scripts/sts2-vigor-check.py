"""Paid native Vigor regression for all multi-hit rows and ordinary/native controls."""
import hashlib,json,sys,time
from pathlib import Path
from oracle_protocol import call
root=Path(__file__).resolve().parents[1];profile=root/sys.argv[1];tag=sys.argv[2];evidence=[]
expected=hashlib.sha256((root/'sts2/bin/Release/net9.0/SpireLegacy.dll').read_bytes()).hexdigest()
rows=json.loads((root/'sheets/cards.json').read_text(encoding='utf-8'))
def send(command,**args):
 record=call(profile,dict(command=command,**args),timeout=90,expected_hash=expected);evidence.append(record)
 reply=record['response'];assert reply['ok'],reply
 assert {m['id'] for m in reply['metadata']['mods']}=={'BaseLib','SpireLegacy'},reply['metadata']
 return reply['result']
def ready(after=None):
 deadline=time.monotonic()+30
 while time.monotonic()<deadline:
  state=send('state')
  if state['phase']=='Play' and (after is None or state['turn']>after):return
  time.sleep(.1)
 raise TimeoutError('Playable phase')
try:
 # Complete the disposable FTUE flags before replacing a combat room; otherwise
 # the old room's tutorial can resume against the removed scene.
 send('compat-control',patches=True,upgrade=False)
 for cls in ['knight','mage','ranger']:
  send('start',**{'class':cls,'traits':[],'quick':True});ready();send('vigor-room');ready()
  selected=[r for r in rows if r['hits']>1 and r['classId'] in [cls,'shared']]
  # Shared rows belong to all three pools; run them on Knight once.
  if cls!='knight':selected=[r for r in selected if r['classId']!='shared']
  keys=[r['id'] for r in selected]+[cls+'_strike',cls+'_guard',{'knight':'knight_blood_price','mage':'mage_ash_reading','ranger':'ranger_second_quiver'}[cls]]
  if cls=='knight':keys+=['knight_measured_cut','knight_tempered_steel','native_twin_strike']
  for key in keys:
   row=next((r for r in rows if r['id']==key),None)
   for upgrade in [False,True]:
    result=send('vigor-check',card=key,upgrade=upgrade);a=result['baseline'];b=result['boosted']
    is_attack=row is None or row['type']=='ATTACK'
    expected_hits=(row['hits']*(result['targetCount'] if row['aoe'] else 1) if is_attack else 0) if row else 2
    assert len(a['hits'])==len(b['hits'])==expected_hits,result
    assert b['hits']==[n+7 for n in a['hits']],result
    for initial,state in [(0,a),(7,b)]:
     assert state['attacks']==(1 if is_attack else 0) and state['pile']==('Exhaust' if row and row['exhaust'] else 'Discard') and not state['tableNodePresent'],result
     assert state['vigorRemaining']==(initial if not is_attack else 0)+(row['vigor']+(row['upgradeMagic'] if upgrade and row['vigor'] else 0) if row else 0),result
    assert a['energySpent']==b['energySpent'],result
    print(tag,key,'upgraded' if upgrade else 'base','passed',flush=True)
  turn=send('state')['turn'];send('turn');ready(after=turn)
  send('abandon');send('select')
finally:
 output=root/'private/sts2-evidence'/('vigor-'+tag+'.json');output.write_text(json.dumps(evidence,indent=2),encoding='utf-8')
