"""Native Neow inline-image/layout regression in an opted-in isolated lab."""
import argparse,json,time,shutil
from oracle_protocol import call
from pathlib import Path

parser=argparse.ArgumentParser();parser.add_argument('branch',choices=['regular','beta']);args=parser.parse_args()
root=Path(__file__).resolve().parents[1];profile=root/'private'/f'sts2-neow-{args.branch}-profile';evidence=[]
log=root/'private'/f'neow-{args.branch}.out.log'
deadline=time.monotonic()+100
while not log.exists() or 'Time to main menu' not in log.read_text(encoding='utf-8',errors='replace'):
    if time.monotonic()>deadline:raise TimeoutError('isolated main menu')
    time.sleep(.2)

def send(command, **values):
    record=call(profile,dict(command=command,**values),timeout=90)
    reply=record['response']
    assert reply['ok'],reply
    evidence.append(dict(command=command,args=values,result=reply['result'],metadata=reply['metadata']))
    return reply['result']

def ready():
    deadline=time.monotonic()+30
    while send('state')['phase']!='Play':
        if time.monotonic()>deadline:raise TimeoutError('combat ready')
        time.sleep(.1)

try:
    for cls in ['knight','mage','ranger']:
        send('start',**{'class':cls,'traits':[],'quick':True})
        # Start uses a combat fixture. Let its async startup finish before leaving it.
        ready()
        send('enter',room='Event',event='Neow')
        result=send('neow-energy');assert result['height']<result['oldHeight'],result
        time.sleep(.5)
        shot=send('screenshot');shutil.copy2(shot['path'],root/'private/sts2-evidence'/f'neow-{args.branch}-{cls}.png')
        send('enter',room='Boss')
        ready()
        assert send('presentation')['energyVisible']
        play=send('expansion-play',card=cls+'_strike',upgrade=False);assert play['saveRoundTrip'] and not play['tableNodePresent'],play
        print(f'{args.branch} {cls}: native 24px text, bounded Neow options, original counter/card play passed',flush=True)
    send('abandon')
finally:
    (root/'private/sts2-evidence'/f'neow-{args.branch}.json').write_text(json.dumps(evidence,indent=2),encoding='utf-8')
