"""Regression checks for an opt-in isolated owned-game lab (HEIR_TEST_MODE=1)."""
import argparse,json,time
from pathlib import Path
root=Path(__file__).resolve().parents[1]
parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('--profile-directory',required=True)
parser.add_argument('--branch',choices=['regular','beta'],required=True)
args=parser.parse_args();profile=(root/args.profile_directory).resolve();tag=args.branch;evidence=[]
if not profile.is_relative_to((root/'private').resolve()):raise SystemExit('Use an isolated private lab profile.')
def send(command,**args):
 out=profile/'response.json'
 if out.exists():out.unlink()
 assert not (profile/'request.json').exists()
 (profile/'request.pending').write_text(json.dumps(dict(command=command,**args)),encoding='utf-8')
 (profile/'request.pending').replace(profile/'request.json')
 end=time.monotonic()+60
 while not out.exists():
  if time.monotonic()>end:raise TimeoutError(command)
  time.sleep(.05)
 r=json.loads(out.read_text(encoding='utf-8'));assert r['ok'],r
 evidence.append(dict(command=command,args=args,result=r['result']))
 return r['result']
def key(action):
 s=send('input',action=action);time.sleep(.15);return s
try:
 for cls in ['knight','mage','ranger']:
  send('start',**{'class':cls,'traits':['megahealth','cheeronkills']})
  n=send('enter',room='Event',event='Neow');time.sleep(.5);n=send('state')
  assert n['hp']==n['maxHp'] and n['hp']>0,(cls,n)
  h=send('heal');assert h['hp']==h['maxHp']-10,h
  send('enter',room='Monster')
  for _ in range(100):
   s=send('state')
   if s['phase']=='Play':break
   time.sleep(.1)
  else:raise AssertionError('No playable combat')
  p=send('manual',card=cls+'_strike');assert p['accepted'] and p['pile']=='Discard' and not p['tableNodePresent'],p
  a=send('abandon');assert not a['active'],a
 for traits,ascension in [(['nomeat'],0),(['superhealer'],0),(['megahealth','cheeronkills'],2),([],2)]:
  send('start',**{'class':'mage','traits':traits,'ascension':ascension})
  send('enter',room='Event',event='Neow');time.sleep(.5);n=send('state')
  expected=int(n['maxHp']*(.8 if ascension>=2 else 1))
  assert n['hp']==expected,(traits,ascension,n,expected)
  h=send('heal');expectedHeal=0 if 'megahealth' in traits else 5 if 'nomeat' in traits else 10
  assert h['hp']==min(h['maxHp'],h['maxHp']-10+expectedHeal),h
  send('abandon')
 send('select');time.sleep(.8)
 picker=send('select-state');assert any(c['portrait'] for c in picker['controls']),picker
 key('ui_down');picker=send('select-state');assert picker['focus']=='SpireLegacyManor',picker
 key('ui_select');m=send('manor-state');assert m['focusableLabels']==0 and m['focusText']=='Name your family',m
 key('ui_down');assert send('manor-state')['focusText']=='Change banner'
 key('ui_cancel');assert send('select-state')['focus']!='SpireLegacyManor'
 key('ui_accept');assert send('manor-state')['open']
 key('ui_cancel')
 if tag=='beta':send('screenshot')
 print(tag+': all three Hero Complex/Diva heirs survive Neow, play/discard cards and abandon; Vegan/Super Healer/ascension initialization preserved; healing restriction retained; unique portrait, D-pad/A and Y Manor entry, B exit and selectable-only focus passed')
finally:
 (root/'private/sts2-evidence'/f'manor106-{tag}-checks.json').write_text(json.dumps(evidence,indent=2),encoding='utf-8')
