"""All registered cards: actual paid play, identity/pile conservation and edge cases."""
import hashlib,json,sys,time
from pathlib import Path
from oracle_protocol import call
root=Path(__file__).resolve().parents[1];profile=Path(sys.argv[1]);tag=sys.argv[2]
expected=hashlib.sha256((root/'sts2/bin/Release/net9.0/SpireLegacy.dll').read_bytes()).hexdigest();records=[]
out=root/'private/sts2-evidence'/('card-resolution-'+tag+'.json');assert not out.exists()
def send(command,**args):
    r=call(profile,dict(command=command,**args),timeout=90,expected_hash=expected);records.append(r);out.write_text(json.dumps(records,indent=2));assert r['response']['ok'],r
    return r['response']['result']
def ready():
    deadline=time.monotonic()+45
    while time.monotonic()<deadline:
        if send('state')['phase']=='Play':return
        time.sleep(.15)
    raise TimeoutError('Native play phase')
def start(cls):
    send('start',**{'class':cls,'traits':[],'quick':True,'seed':'SPIRELEGACY-CARDS-52','manorTier':0});ready()
def play(row,up,mode='normal'):
    send('enter',room='Monster');ready()
    return send('card-resolution',card=row['id'],upgrade=up,mode=mode)
cards=json.loads((root/'sheets/cards.json').read_text())
send('trait-speed',value=8)
if 'smoke' in tag:cards=[r for r in cards if r['special']!='none' or r['id'].endswith('_strike')][:45]
for cls in ['knight','mage','ranger']:
    start(cls)
    for index,row in enumerate([r for r in cards if r['classId']==cls]):
        for up in [False,True]:play(row,up)
        if index%20==19:print(tag,cls,index+1,'cards both forms pass',flush=True)
send('abandon')
print(tag+': '+str(2*len(cards))+' registered card forms complete paid play/pile/identity checks',flush=True)
