"""Native #105: no start/abandon farming, earned gold, real first-fight loss/win."""
import hashlib,json,sys,time
from pathlib import Path
from oracle_protocol import call
root=Path(__file__).resolve().parents[1];profile=Path(sys.argv[1]);tag=sys.argv[2];records=[]
expected=hashlib.sha256((root/'sts2/bin/Release/net9.0/SpireLegacy.dll').read_bytes()).hexdigest()
output=root/'private/sts2-evidence'/('crowns-'+tag+'.json');assert not output.exists()
def send(command,**args):
    r=call(profile,dict(command=command,**args),timeout=90,expected_hash=expected);records.append(r)
    output.write_text(json.dumps(records,indent=2));assert r['response']['ok'],r
    return r['response']['result']
def ready():
    deadline=time.monotonic()+45
    while time.monotonic()<deadline:
        if send('state')['phase']=='Play':return
        time.sleep(.1)
    raise TimeoutError('Playable first map fight')
def settled(expected,bank,generation):
    deadline=time.monotonic()+30
    while time.monotonic()<deadline:
        s=send('lifecycle-state')
        if s['profile']['active'] is None:
            assert s['profile']==s['persisted'] and s['profile']['crowns']==bank+expected and s['profile']['generation']==generation+1,s
            return s
        time.sleep(.1)
    raise TimeoutError('Native settlement')
send('trait-speed',value=8)
if 'campaign' in tag:
    for cls in ['knight','mage','ranger']:
        send('start',**{'class':cls,'quick':False,'traits':[],'seed':'SPIRELEGACY-CROWNS-105','manorTier':0})
        before=send('lifecycle-state')['profile'];ended=False
        for step in range(200):
            result=send('balance-campaign-step',policy='tactical',rewardPolicy='defensive')
            action=result['action'];s=result['state']
            assert not action.startswith('unsupported') and action not in ['play-cap','turn-cap'],result
            if action=='finished':ended=True;break
            if s['floor']>=6:break
            time.sleep(.1)
        else:raise TimeoutError('Bounded normal campaign comparison')
        p=send('crown-state');stats=[s for h in p['history'] for s in h['stats']]
        completed=p['history'] if p['finished'] else p['history'][:-1]
        combat=any(any(r in ['Monster','Elite','Boss'] for r in h['rooms']) for h in completed)
        participation=p['dead'] and p['manualPlays']>0 and not p['abandoned']
        points=sum(h['type'] not in ['Ancient','Unassigned'] for h in completed)+(1 if participation else 0)
        gold=min(p['gold'],max(0,sum(s['GoldGained'] for s in stats)-sum(s['GoldSpent']+s['GoldLost'] for s in stats)-max(0,sum(s['GoldStolen'] for s in stats))))
        if not combat and not participation:points=gold=0
        assert (p['earnedFloors'],p['earnedGold'])==(points,gold),p
        if not ended:send('abandon')
        settled(points*2+gold//25,before['crowns'],before['generation'])
        print(tag,cls,'natural map comparison',p['floor'],points,gold,'pass',flush=True)
    sys.exit(0)
for cls in ['knight','mage','ranger']:
    for quick in [False,True]:
        send('start',**{'class':cls,'quick':quick,'traits':[],'seed':'SPIRELEGACY-CROWNS-105','manorTier':0})
        if quick:ready()
        before=send('lifecycle-state')['profile'];p=send('crown-state');assert p['earnedFloors']==p['earnedGold']==0,p
        send('abandon');settled(0,before['crowns'],before['generation'])
    for up in [False,True]:
        for kind in ['played-abandon','death','win','spent','lost','returned']:
            send('start',**{'class':cls,'quick':True,'traits':[],'seed':'SPIRELEGACY-CROWNS-105','manorTier':0});ready()
            before=send('lifecycle-state')['profile']
            played=send('crown-play',upgrade=up,win=kind not in ['played-abandon','death'])
            if kind=='death':send('kill',player=True);settled(2,before['crowns'],before['generation']);continue
            if kind=='played-abandon':send('abandon');settled(0,before['crowns'],before['generation']);continue
            p=send('crown-gold',gained=75,spent=50 if kind=='spent' else 0,lost=25 if kind=='lost' else 0,stolen=25 if kind=='returned' else 0,returned=25 if kind=='returned' else 0)
            baseline=sum(s['GoldGained']-s['GoldSpent']-s['GoldLost']-s['GoldStolen'] for h in played['history'] for s in h['stats'])
            assert p['earnedFloors']==1 and p['earnedGold']==baseline+({'spent':25,'lost':50}.get(kind,75)),p
            send('save');send('resume');p2=send('crown-state');assert (p2['earnedFloors'],p2['earnedGold'])==(p['earnedFloors'],p['earnedGold']),p2
            send('abandon');settled(2+p['earnedGold']//25,before['crowns'],before['generation'])
        print(tag,cls,up,'native payout cases pass',flush=True)
print(tag+': immediate/played abandon, paid death/win, gold conservation and resume pass',flush=True)
