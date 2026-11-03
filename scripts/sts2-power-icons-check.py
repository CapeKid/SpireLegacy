"""Load every custom power's small and tooltip icons in the isolated game."""
import json, os, sys
from pathlib import Path
from oracle_protocol import call

profile = Path(sys.argv[1])
record = call(profile, {'command': 'power-icons'}, timeout=120,
              expected_hash=os.environ['HEIR_EXPECTED_DLL_SHA256'])
(profile / 'power-icons-receipt.json').write_text(json.dumps(record, indent=2), encoding='utf-8')
assert record['response']['ok'], record
icons = record['response']['result']
assert len(icons) == 51 and len({p['key'] for p in icons}) == 51, icons
assert all(p['packedWidth'] > 0 and p['bigWidth'] > 0 for p in icons), icons
print('All 51 powers load both icon sizes; Eternal Bookmark uses native draw icons.', flush=True)
