"""Startup/branch native regression; requires a fresh guarded owned-game profile."""
import hashlib,json,sys,time
from pathlib import Path
from oracle_protocol import call
root=Path(__file__).resolve().parents[1];profile=root/sys.argv[1];tag=sys.argv[2];evidence=[];expected_mods={"BaseLib","SpireLegacy",*sys.argv[3:]}
expected=hashlib.sha256((root/'sts2/bin/Release/net9.0/SpireLegacy.dll').read_bytes()).hexdigest()
def send(command,**args):
    record=call(profile,dict(command=command,**args),timeout=90,expected_hash=expected);evidence.append(record)
    reply=record['response'];assert reply['ok'],reply
    assert {m['id'] for m in reply['metadata']['mods']}==expected_mods,reply['metadata']
    return reply['result']
def ready():
    deadline=time.monotonic()+30
    while time.monotonic()<deadline:
        state=send('state')
        if state['phase']=='Play':return state
        time.sleep(.1)
    raise TimeoutError('Playable phase')
try:
    assert send('compat-reinitialize')['unchanged']
    rollback=send('compat-rollback');assert rollback['rolledBack'] and 'HeirRuntimeTextureLookup' in rollback['error']
    for cls in ['knight','mage','ranger']:
        for upgrade in [False,True]:
            send('start',**{'class':cls,'traits':['megahealth','cheeronkills'],'quick':True});ready()
            if not upgrade:assert send('compat-scopes')=={'restored':True,'failures':2}
            played=send('expansion-play',card=cls+'_strike',upgrade=upgrade)
            assert played['enemyDamage']>0 and played['energyChange']==-played['cost'] and played['pile']=='Discard' and not played['tableNodePresent'],played
            send('save');send('resume');ready()
            guard=send('manual',card=cls+'_guard');assert guard['accepted'] and guard['pile']=='Discard' and not guard['tableNodePresent'],guard
    exported=send('compat-export-player');assert exported['exported']
    send('abandon');send('select')
    for upgrade in [False,True]:
        before=send('compat-control',patches=False,upgrade=upgrade)
        after=send('compat-control',patches=True,upgrade=upgrade)
        assert before==after,(before,after)
        assert after['maxHp']==80 and after['healing']==5 and after['heirSprites']==0 and after['familyUnchanged'],after
    assert send('compat-reinitialize')['unchanged']
    print(tag+': initialization/rollback, failed-prefix scope cleanup, three classes/base+upgrade paid cards and resume, and native Ironclad controls passed')
finally:
    output=root/'private/sts2-evidence'/('compatibility-'+tag+'.json');output.write_text(json.dumps(evidence,indent=2),encoding='utf-8')
