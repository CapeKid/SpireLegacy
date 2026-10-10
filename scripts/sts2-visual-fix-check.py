"""Mage death and room-scale regression; requires an opt-in rendered isolated lab. Arguments: private profile path and branch tag."""
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
 start('mage',[]);send('trait-hit',amount=999);time.sleep(1);v=send('visual')
 assert v[0]['pose']=='dead' and v[0]['frames']==4 and v[0]['frame']==3 and v[0]['visible'],v
 assert set(v[0]['deathFrames'])=={0,1,2,3},v
 assert v[0]['tint']=='(1, 1, 1, 1)',v
 time.sleep(.4);assert send('visual')[0]['frame']==3
 import shutil
 shot=send('screenshot');shutil.copy2(shot['path'],root/'private/sts2-evidence'/f'visual112-{tag}-mage-death.png');send('abandon')
 for cls in ['knight','mage','ranger']:
  start(cls,[]);send('kill')
  for room in ['RestSite','Shop']:
   send('enter',room=room);time.sleep(.6);v=send('room-visual')
   assert len(v)==1 and v[0]['classId']==cls and v[0]['pose']=='idle',v
   assert (170 if room=='RestSite' else 195)<v[0]['pixelHeight']<280,v
   shot=send('screenshot');shutil.copy2(shot['path'],root/'private/sts2-evidence'/f'visual112-{tag}-{cls}-{room}.png')
 send('abandon')
 print(tag+': Mage lethal enemy Attack plays all four death frames and holds final defeat; all classes enlarged in rest sites and shops')
finally:
 (root/'private/sts2-evidence'/f'visual112-{tag}.json').write_text(json.dumps(evidence,indent=2),encoding='utf-8')
