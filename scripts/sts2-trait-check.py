"""Clumsy and Hero Complex regression checks; pass private profile path and branch tag."""
import json,time,sys
from pathlib import Path
root=Path(__file__).resolve().parents[1];profile=(root/sys.argv[1]).resolve();tag=sys.argv[2];evidence=[]
if not profile.is_relative_to((root/"private").resolve()): raise SystemExit("Use an isolated private lab profile.")
def send(command,**args):
 out=profile/'response.json'
 if out.exists():out.unlink()
 assert not (profile/'request.json').exists()
 (profile/'request.pending').write_text(json.dumps(dict(command=command,**args)),encoding='utf-8');(profile/'request.pending').replace(profile/'request.json')
 end=time.monotonic()+40
 while not out.exists():
  if time.monotonic()>end:raise TimeoutError(command)
  time.sleep(.05)
 r=json.loads(out.read_text(encoding='utf-8'));assert r['ok'],r
 evidence.append(dict(command=command,args=args,result=r['result']));return r['result']
def ready(turn=1):
 for _ in range(150):
  s=send('state')
  if s['phase']=='Play' and s['turn']>=turn:return s
  time.sleep(.1)
 raise TimeoutError('combat')
def start(cls,traits):
 send('start',**{'class':cls,'traits':traits,'quick':True});ready();send('enter',room='Boss');return ready()
try:
 for cls in ['knight','mage','ranger']:
  start(cls,['easybreakables']);g=send('play',card=cls+'_guard');a=send('play',card=cls+'_strike');assert a['block']==max(0,g['block']-2),(g,a)
  b=send('play',card=cls+'_strike');assert b['block']==a['block'],b
  send('turn');time.sleep(2);ready(2);g=send('play',card=cls+'_guard');a=send('play',card=cls+'_strike');assert a['block']==max(0,g['block']-2),(g,a)
  start(cls,['easybreakables']);a=send('play',card=cls+'_strike');assert a['block']==0,a
  g=send('play',card=cls+'_guard');b=send('play',card=cls+'_strike');assert b['block']==g['block'],(g,b)
 start('knight',['easybreakables','bounceterrain']);a=send('play',card='knight_strike');assert a['block']==0,a
 start('mage',['megahealth','cheeronkills']);send('enter',room='Event',event='Neow');time.sleep(.5);s=send('state');assert s['hp']==s['maxHp'] and s['hp']>0,s
 h=send('heal');assert h['hp']==h['maxHp']-10,h
 send('abandon')
 print(tag+': all three classes lose 2 Block once per turn; reset, zero clamp, attack-first sequencing, Clown interaction and Hero Complex startup/healing passed')
finally:
 (root/'private/sts2-evidence'/f'traits109-{tag}.json').write_text(json.dumps(evidence,indent=2),encoding='utf-8')
