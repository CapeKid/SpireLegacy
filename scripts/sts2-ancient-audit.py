"""#56 native dialogue/reward/ending audit with correlated exact-build receipts."""
import hashlib,json,sys,time
from pathlib import Path
from oracle_protocol import call
root=Path(__file__).resolve().parents[1];profile=Path(sys.argv[1]);tag=sys.argv[2];records=[]
expected=hashlib.sha256((root/'sts2/bin/Release/net9.0/SpireLegacy.dll').read_bytes()).hexdigest()
output=root/'private/sts2-evidence'/('ancient-audit-'+tag+'.json');assert not output.exists()
def send(command,**args):
    r=call(profile,dict(command=command,**args),timeout=90,expected_hash=expected);records.append(r);output.write_text(json.dumps(records,indent=2));assert r['response']['ok'],r
    return r['response']['result']
def ready():
    deadline=time.monotonic()+45
    while time.monotonic()<deadline:
        s=send('state')
        if s['phase']=='Play':return
        time.sleep(.1)
    raise TimeoutError('Playable native combat')
def start(cls,traits=[]):
    send('start',**{'class':cls,'traits':traits,'quick':True,'seed':'SPIRELEGACY-ANCIENTS-56','manorTier':0});ready()
send('trait-speed',value=8)
if 'selectors' in tag:
    assert send('ancient-audit-selectors')['checks']==672
    assert send('ancient-audit-fallbacks')['emptyPreserved']
    endings=send('architect-dialogues');assert len(endings)==15
    assert all(all(l['key'].startswith('SPIRELEGACY.ARCHITECT.'+r['classId']+'.') for l in r['lines']) for r in endings)
elif 'saveboundaries' in tag:
    for cls in ['knight','mage','ranger']:
        for visits in [0,4]:
            start(cls);send('ancient-audit-tome');send('save');send('resume');ready();assert send('tome-parity',inspect=True)['saved']
            send('ancient-audit-persistent-neow',visits=visits);send('save');send('resume')
            state=send('ancient-state');assert state['index']==0 and all('.'+cls+'.' in l['key'] for l in state['lines']),state
            for index in [1,2,3]:assert send('ancient-audit-next',controller=visits>0)['index']==index
            time.sleep(.4);send('ancient-audit-choose');send('save');send('resume');assert send('ancient-audit-finished')['finished'];send('abandon')
            print(tag,cls,visits,'before/after reward and Tome saved boundary passed',flush=True)
elif 'endings' in tag:
    for cls in ['knight','mage','ranger']:
        for wins in [0,4]:
            start(cls);r=send('ancient-audit-ending-enter',wins=wins)
            assert all('.'+cls+'.'+('first' if wins==0 else 'return')+'.' in k for k in r['lines']),r
            for index in [1,2,3]:assert send('ancient-audit-ending-next')['index']==index
            r=send('ancient-audit-ending-win');assert r['nativeGameOver'] and r['once'],r
            print(tag,cls,wins,'native victory passed',flush=True)
elif 'modifiers' in tag:
    for row in json.loads((root/'sheets/ancient_cards.json').read_text()):
        for traits in [[],['cantattack'],['megahealth'],['superhealer']]:
            for up in [False,True]:
                start(row['classId'],traits);r=send('ancient-audit-play-modifier',card=row['cardId'],upgrade=up)
                assert r['saveRoundTrip'] and not r['tableNodePresent'] and r['pile']!='Play',r
                if row['cardId']=='mage_elixir_of_life':assert r['hpChange']==(0 if traits==['megahealth'] else 12 if traits==['superhealer'] else 6),r
                if row['cardId']=='knight_kingbreaker' and traits==['cantattack']:assert r['blocked'] and r['energyChange']==0,r
                send('abandon')
        print(tag,row['cardId'],'trait modifiers/paid forms passed',flush=True)
elif 'cards' in tag:
    for cls in ['knight','mage','ranger']:
        for traits in [[],['cantattack'],['megahealth'],['superhealer']]:
            start(cls,traits);send('ancient-audit-tome');send('save');send('resume');ready();send('abandon')
    for row in json.loads((root/'sheets/ancient_cards.json').read_text()):
        for up in [False,True]:
            start(row['classId']);send('ancient-play',card=row['cardId'],upgrade=up);send('abandon')
elif 'dialogues' in tag:
    for cls in ['knight','mage','ranger']:
        for npc in ['NEOW','DARV','TEZCATARA','OROBAS','PAEL','NONUPEIPE','TANX','VAKUU']:
            for visits in [0,4]:
                start(cls);s=send('ancient-enter',ancient=npc,visits=visits)
                assert all('.'+cls+'.'+('first' if visits==0 else 'return')+'.' in l['key'] for l in s['lines']),s
                for index in [1,2,3]:assert send('ancient-audit-next',controller=visits>0)['index']==index
                time.sleep(.4);assert send('ancient-state')['focusable']
                r=send('ancient-audit-choose');assert r['once'],r
                send('save');send('resume');send('abandon')
            print(tag,cls,npc,'dialogue/reward/save passed',flush=True)
else:raise ValueError(tag)
print(tag,'completed',flush=True)
