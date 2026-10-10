"""Native class-animation regression; requires an opt-in rendered isolated lab. Arguments: private profile path and branch tag."""
import json,time,sys
from pathlib import Path
root=Path(__file__).resolve().parents[1];profile=(root/sys.argv[1]).resolve();tag=sys.argv[2];evidence=[]
if not profile.is_relative_to((root/"private").resolve()):raise SystemExit("Use an isolated private lab profile.")
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
 send('start',**{'class':cls,'traits':traits,'quick':True});return ready()
try:
 for cls in ['knight','mage','ranger']:
  start(cls,[]);time.sleep(.4);v=send('visual');assert len(v)==1 and v[0]['classId']==cls and v[0]['pose']=='idle' and v[0]['frames']==4,v
  before=send('state');p=send('manual',card=cls+'_strike');assert p['accepted'] and p['pile']=='Discard' and not p['tableNodePresent'],p
  v=send('visual');assert 'attack' in v[0]['events'],v
  send('play',card=cls+'_guard');v=send('visual');assert 'skill' in v[0]['events'],v
  send('injure',amount=1);time.sleep(.4);v=send('visual');assert 'hurt' in v[0]['events'],v
  assert v[0]['pose']=='idle',v
  shot=send('screenshot');import shutil;shutil.copy2(shot['path'],root/'private/sts2-evidence'/f'characters110-{tag}-{cls}.png')
  send('kill');time.sleep(.2);v=send('visual');assert 'victory' in v[0]['events'] and v[0]['pose']=='victory',v
  start(cls,[]);send('injure',amount=999);time.sleep(.3);v=send('visual');assert v[0]['pose']=='dead',v
  send('abandon')
 print(tag+': Knight/Mage/Ranger distinct idle frames; native paid Attack cleanup, Skill, actual damage hurt/return-idle, combat victory and player death passed')
finally:
 (root/'private/sts2-evidence'/f'characters110-{tag}.json').write_text(json.dumps(evidence,indent=2),encoding='utf-8')
