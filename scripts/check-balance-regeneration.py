"""Prove pool expansion preserves targeted fixes in a disposable authoring tree."""
import json,shutil,subprocess,sys,tempfile
from pathlib import Path
from balance_policy import CARD_OVERRIDES
root=Path(__file__).resolve().parents[1]
with tempfile.TemporaryDirectory(prefix='spire-balance-authoring-') as directory:
    scratch=Path(directory);shutil.copytree(root/'sheets',scratch/'sheets');(scratch/'scripts').mkdir()
    for name in ['sts2-pool-expansion.py','balance_policy.py']:
        shutil.copy2(root/'scripts'/name,scratch/'scripts'/name)
    subprocess.run([sys.executable,str(scratch/'scripts/sts2-pool-expansion.py')],check=True)
    original={r['id']:r for r in json.loads((root/'sheets/cards.json').read_text())}
    regenerated={r['id']:r for r in json.loads((scratch/'sheets/cards.json').read_text())}
    for key,overrides in CARD_OVERRIDES.items():
        for field,value in overrides.items():
            assert regenerated[key][field]==value,(key,field,'override lost')
        assert regenerated[key]==original[key],(key,'targeted card changed during expansion')
    assert set(regenerated)==set(original),'Stable saved identities changed'
    for sheet in ['card_pools','branch_cards','pool_targets']:
        assert json.loads((scratch/'sheets'/f'{sheet}.json').read_text())==json.loads((root/'sheets'/f'{sheet}.json').read_text()),sheet
print('Pool expansion preserves all targeted cards, identities, memberships and branch sizes.')
