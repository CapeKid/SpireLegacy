"""Private Mage pool: paid native card sequences, fixtures supply Scry choices."""
import hashlib,json,os,sys,time
from pathlib import Path
from oracle_protocol import call
root=Path(__file__).resolve().parents[1];profile=Path(sys.argv[1]).resolve()
expected=os.environ.get('HEIR_EXPECTED_DLL_SHA256') or hashlib.sha256((root/'sts2/bin/Release/net9.0/SpireLegacy.dll').read_bytes()).hexdigest()
records=[]
def send(command,**args):
    record=call(profile,dict(command=command,**args),timeout=90,expected_hash=expected)
    records.append(record);(profile/'mage-pool-receipts.json').write_text(json.dumps(records,indent=2))
    assert record['response']['ok'],record
    if '--beta-id' in sys.argv:
        loaded={m['id'] for m in record['response']['metadata']['mods']}
        assert 'SpireLegacyBeta' in loaded and 'SpireLegacy' not in loaded,loaded
    return record['response']['result']
def start(cls):
    send('start',**{'class':cls,'traits':[],'quick':True,'manorTier':0,'seed':'MAGE-POOL-185'})
    for _ in range(150):
        if send('state')['phase']=='Play':return
        time.sleep(.1)
    raise TimeoutError('combat')
send('trait-speed',value=8)
for up in [False,True]:
    for key in (['mage_spell_notes','mage_rune_detonation'] if '--beta-id' in sys.argv else ['mage_spell_notes'] if '--scry-smoke' in sys.argv else ['mage_ember_dart','mage_spell_notes','mage_moonstone_lance','mage_overcharge','mage_grand_thesis','mage_rune_detonation']):
        for mode in ((['full','replay'] if '--beta-id' in sys.argv else ['zero','one','full','used','replay','foreign']) if key=='mage_rune_detonation' else ['normal']):
            start('knight' if mode=='foreign' else 'mage')
            result=send('mage-pool',card=key,upgrade=up,mode=mode)
            print(key,up,mode,result,'passed',flush=True)
            send('abandon')
