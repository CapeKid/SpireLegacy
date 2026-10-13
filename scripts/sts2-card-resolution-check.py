"""All registered cards: actual paid play, identity/pile conservation and edge cases."""
import hashlib,json,sys,time
from pathlib import Path
from oracle_protocol import call
root=Path(__file__).resolve().parents[1];profile=Path(sys.argv[1]);tag=sys.argv[2]
expected=hashlib.sha256((root/'sts2/bin/Release/net9.0/SpireLegacy.dll').read_bytes()).hexdigest();records=[]
out=root/'private/sts2-evidence'/('card-resolution-'+tag+'.json');assert not out.exists()
stream=out.with_suffix('.jsonl');assert not stream.exists()
def send(command,**args):
    r=call(profile,dict(command=command,**args),timeout=90,expected_hash=expected);records.append(r)
    with stream.open('a',encoding='utf-8') as f:f.write(json.dumps(r)+'\n')
    if command in ['card-resolution','abandon'] or not r['response']['ok']:out.write_text(json.dumps(records,indent=2))
    assert r['response']['ok'],r
    return r['response']['result']
def ready():
    deadline=time.monotonic()+45
    while time.monotonic()<deadline:
        if send('state')['phase']=='Play':return
        time.sleep(.15)
    raise TimeoutError('Native play phase')
def start(cls):
    send('start',**{'class':cls,'traits':[],'quick':True,'seed':'SPIRELEGACY-CARDS-52','manorTier':0})
    if cls=='ironclad':send('enter',room='Monster')
    ready()
def play(row,up,mode='normal'):
    send('enter',room='Monster');ready()
    result=send('card-resolution',card=row['id'],upgrade=up,mode=mode)
    if mode=='normal' and not row['id'].startswith('native:') and row['power']=='none':
        amount=lambda key:row[key]+(row['upgradeMagic'] if up and row[key]>0 and key!='hpLoss' else 0)
        assert result['hpDelta']==min(20+row['hpLoss'],amount('heal'))-row['hpLoss'],result
        damage=row['damage']+(row['upgradeDamage'] if up else 0)
        if row['special'] in ['block_damage','chain_damage']:damage=0
        if row['special']=='exhaust_damage':damage+=row['specialAmount']+(row['upgradeSpecial'] if up else 0)
        assert sum(result['damage'])==damage*row['hits']*(len(result['damage']) if row['aoe'] else 1),result
        block=row['block']+(row['upgradeBlock'] if up else 0)
        if row['special']=='double_block':block*=2
        if row['special']=='exhaust_block':block+=row['specialAmount']+(row['upgradeSpecial'] if up else 0)
        if row['special']=='exhaust_nonattacks':block+=(row['specialAmount']+(row['upgradeSpecial'] if up else 0))*(3+amount('draw')-amount('discard'))
        assert result['blockDelta']==block,result
    return result
cards=json.loads((root/'sheets/cards.json').read_text())
send('trait-speed',value=8)
if 'choices' in tag:
    for cls in ['knight','mage','ranger']:
        for special in ['scry','exhaust_one','retrieve','exhume']:
            row=next(r for r in cards if r['special']==special)
            for up in [False,True]:
                for decline in ([False,True] if special=='scry' else [False]):
                    start(cls);choice=send('card-choice-boundary',card=row['id'],upgrade=up,decline=decline)
                    send('resume');ready();assert send('card-deck-identity')==choice['deck']
    for up in [False,True]:
        start('ironclad');choice=send('card-choice-boundary',card='native:BurningPact',upgrade=up)
        send('resume');assert send('card-deck-identity')==choice['deck']
    send('abandon');print(tag+': legal held/declined native choices, actual saved deck and resume checks pass',flush=True);sys.exit(0)
if 'edges' in tag:
    representative={}
    for row in cards:
        if row['special']!='none':representative.setdefault(row['special'],row)
    for row in representative.values():
        start(row['classId'])
        for up in [False,True]:
            for mode in ['empty','full','discount','save-copy','dupe']:play(row,up,mode)
    for key in ['knight_strike','mage_guard','ranger_rapid_fire','knight_blood_price','mage_blood_to_mana','ranger_shadow_arrow','ranger_barbed_arrow','knight_crimson_sweep']:
        row=next(r for r in cards if r['id']==key);start(row['classId'])
        for up in [False,True]:
            modes=['replay'] if not row['hpLoss'] else ['self-death']
            if row['type']=='ATTACK':modes+=['invalid'] if not row['aoe'] and row['special']!='random_attack' else []
            if row['damage']>0:modes+=['lethal']
            for mode in modes:
                if mode=='self-death':start(row['classId'])
                play(row,up,mode)
    start('ironclad')
    for key in ['StrikeIronclad','TwinStrike','Cleave','BurningPact','Hemokinesis']:
        for up in [False,True]:
            for mode in (['self-death'] if key=='Hemokinesis' else ['empty','full','replay','save-copy','dupe']):
                if mode=='self-death':start('ironclad')
                play({'id':'native:'+key},up,mode)
    send('abandon');print(tag+': native effect-family edge cases and vanilla controls pass',flush=True);sys.exit(0)
if 'smoke' in tag:cards=[r for r in cards if r['special']!='none' or r['id'].endswith('_strike')][:45]
shared={r['cardId'] for r in json.loads((root/'sheets/card_pools.json').read_text()) if r['scope']=='shared'}
total=0
for cls in ['knight','mage','ranger']:
    start(cls)
    for index,row in enumerate([r for r in cards if r['classId']==cls or r['id'] in shared]):
        for up in [False,True]:play(row,up);total+=1
        if index%20==19:print(tag,cls,index+1,'cards both forms pass',flush=True)
send('abandon')
print(tag+': '+str(total)+' registered/shared-owner card forms complete paid play/pile/identity checks',flush=True)
