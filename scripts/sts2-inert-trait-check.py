"""Formerly inert trait gameplay regression checks; pass private profile path and branch tag."""
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
  plain=start(cls,[]);basehp=plain['maxHp'];draw=len(plain['hand']);energy=plain['energy']
  send('trait-guard',amount=100);b=send('state')['enemies'][0]['block'];a=send('play',card=cls+'_strike');damage1=b-a['enemies'][0]['block'];b=a['enemies'][0]['block'];a=send('play',card=cls+'_strike');damage2=b-a['enemies'][0]['block']
  start(cls,['projectilesnowalls']);assert send('state')['maxHp']==basehp-6
  b=send('trait-guard',amount=100)['enemies'][0]['block'];a=send('play',card=cls+'_strike');assert b-a['enemies'][0]['block']==damage1+3,(cls,a,damage1)
  b=a['enemies'][0]['block'];a=send('play',card=cls+'_strike');assert b-a['enemies'][0]['block']==damage2,(cls,a,damage2)
  start(cls,[]);a=send('play',card=cls+'_guard');guard1=a['block'];a=send('play',card=cls+'_guard');guard2=a['block']-guard1
  start(cls,['fmffan']);a=send('play',card=cls+'_guard');assert a['block']==guard1+2,(cls,a,guard1)
  a=send('play',card=cls+'_guard');assert a['block']==guard1+guard2+2,(cls,a,guard2)
  send('turn');time.sleep(2);ready(2);a=send('play',card=cls+'_guard');assert a['block']==guard1+2,(cls,a)
  a=start(cls,['mapreveal']);assert len(a['hand'])==draw+1 and a['energy']==energy-1,(cls,a,draw,energy)
  send('turn');time.sleep(2);a=ready(2);assert len(a['hand'])==draw and a['energy']==energy,(cls,a)
  a=start(cls,['fart']);assert any('WEAK' in p['id'] and p['amount']==1 for p in a['powers']),a
  a=start(cls,['mushroomgrow']);assert a['maxHp']==basehp-6,a
  a=send('trait-hit',amount=2,unpowered=True);assert a['block']==0,a
  a=send('trait-guard',amount=2,self=True);a=send('trait-hit',amount=1);assert a['block']==1,a
  a=send('trait-hit',amount=2);assert a['block']==3,a
  a=send('trait-hit',amount=5);assert a['block']==0,a
  send('turn');time.sleep(2);ready(2);a=send('trait-hit',amount=1);assert a['block']==3,a
 # Hypergonadism also triggers once per target on the first Attack.
 for cls in ['knight','mage','ranger']:
  start(cls,['enemyknockedfar']);a=send('play',card=cls+'_strike');assert any('WEAK' in p['id'] and p['amount']==1 for p in a['enemies'][0]['powers']),a
  a=send('play',card=cls+'_strike');assert next(p['amount'] for p in a['enemies'][0]['powers'] if 'WEAK' in p['id'])==1,a
 # A multi-hit card strips guard once, not per hit.
 start('ranger',[]);b=send('trait-guard',amount=100)['enemies'][0]['block'];a=send('play',card='double');loss=b-a['enemies'][0]['block']
 start('ranger',['projectilesnowalls']);b=send('trait-guard',amount=100)['enemies'][0]['block'];a=send('play',card='double');assert b-a['enemies'][0]['block']==loss+3,(loss,a)
 # Area attacks strip each living target's guard exactly once.
 send('start',**{'class':'knight','traits':[],'quick':True});ready();b=send('trait-guard',amount=100)['enemies'];a=send('play',card='knight_sweeping_edge')['enemies'];losses=[x['block']-y['block'] for x,y in zip(b,a)]
 send('start',**{'class':'knight','traits':['projectilesnowalls'],'quick':True});ready();b=send('trait-guard',amount=100)['enemies'];a=send('play',card='knight_sweeping_edge')['enemies'];assert [x['block']-y['block'] for x,y in zip(b,a)]==[n+3 for n in losses],(b,a,losses)
 send('abandon')
 print(tag+': all six trait abilities pass for all three classes, including bounded triggers, turn reset, opening tradeoff, multi-hit and area attacks')
finally:
 (root/'private/sts2-evidence'/f'traits111-{tag}.json').write_text(json.dumps(evidence,indent=2),encoding='utf-8')
