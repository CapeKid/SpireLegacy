"""Validate #55 native receipts and export compact, issue-specific public proof."""
import collections, hashlib, json, subprocess
from pathlib import Path
from item_audit_assertions import relic_effect, potion_effect

root = Path(__file__).resolve().parents[1]
private = root / 'private/sts2-evidence'
production = 'sts2/Native/BloodlineRelic.cs'
proof = dict(issue=55, baselineSource='24c506861e70232c41315dee2122738e028f8858', findings=[128], branches=[])
profiles = set()

def stage(name, game, baseline=False):
    records = json.loads((private / name).read_text())
    ids, hashes, sources = set(), set(), set()
    commands, results = collections.Counter(), []
    cls, unlocked, traits = None, None, []
    for record in records:
        q, a = record['request'], record['response']
        m, command = a['metadata'], q['command']
        if not baseline: profiles.add(m['profile'])
        assert q['requestId'] == a['requestId'] == m['requestId'] and q['requestId'] not in ids
        ids.add(q['requestId'])
        assert a['ok'] and m['game'] == game and m['sourcePatchSha256'] is None, a
        assert {x['id']: x['version'] for x in m['mods']} == {'BaseLib':'v3.4.7', 'SpireLegacy':'1.0.29'}
        hashes.add(m['dllSha256']); sources.add(m['sourceSha']); commands[command] += 1
        value = a['result']
        if command == 'start': cls, traits = q['class'], q['traits']
        elif command == 'chronicle-fixture': unlocked = q['unlocked']
        elif command == 'item-clones':
            assert len(value) == 4
            assert all(c['shared'] == baseline and c['canonicalShared'] == baseline for c in value)
        elif command == 'item-relic': relic_effect(cls, value)
        elif command in {'potion-use', 'potion-controller-use'}: potion_effect(cls, value)
        elif command == 'relic-info':
            allowed = 10 if unlocked else 7
            if traits: allowed -= (5 if unlocked else 3) if cls == 'ranger' else (4 if unlocked else 2)
            assert len(value['drops']) == len(value['shops']) == allowed
            assert set(value['drops']) == set(value['shops'])
            assert len(value['native']) == 5 and all('SPIRELEGACY' not in r['id'] for n in value['native'] for r in n['relics'])
            value = {k: value[k] for k in ['drops', 'shops', 'rewards', 'inventories']}
            value['nativePoolsUncontaminated'] = True
        elif command == 'potion-info':
            assert len(value['native']) == 5 and all('SPIRELEGACY' not in p['id'] for n in value['native'] for p in n['potions'])
            assert len(value['drops']) == len(value['shops']) == len(value['generated']) == 3
            assert set(value['drops']) == set(value['shops']) == set(value['generated'])
            value = {k:value[k] for k in ['drops', 'shops', 'generated']}
            value['nativePoolsUncontaminated'] = True
        if command.startswith('item-') or command in {'relic-info', 'potion-info', 'potion-use', 'potion-controller-use'}:
            results.append(dict(input={k:v for k,v in q.items() if k!='requestId'}, cls=cls, traits=traits, chronicleUnlocked=unlocked, result=value))
    assert len(hashes) == len(sources) == 1
    source = sources.pop()
    digest = hashlib.sha256(subprocess.check_output(['git','show',source+':'+production],cwd=root)).hexdigest()
    # Git stores LF; Windows checkouts may use CRLF. Compare normalized source,
    # while retaining the exact loaded DLL hash independently for each stage.
    if not baseline: assert digest == hashlib.sha256((root/production).read_bytes().replace(b'\r\n',b'\n')).hexdigest(), name
    return dict(file=name, replies=len(records), sourceSha=source, dllSha256=hashes.pop(), productionSha256=digest,
                commands=dict(commands), seeds=sorted({r['response']['metadata']['seed'] for r in records if r['response']['metadata']['seed']}), results=results)

for branch, game in [('regular','v0.107.1'), ('beta','v0.111.0')]:
    names = [f'item-audit-items55-relics-{branch}-v2.json', f'item-audit-items55-potions-{branch}.json',
             f'item-audit-items55-sources-{branch}.json', f'item-audit-items55-relic-edges-{branch}.json',
             f'item-audit-items55-edges-{branch}-native-control.json', f'item-audit-items55-antique-{branch}.json',
             f'item-audit-items55-controller-{branch}.json', f'harness-items55-completed-{branch}-v2-coop.json',
             f'harness-items55ranger-completed-{branch}-coop.json']
    stages = [stage(n,game) for n in names]
    assert stages[0]['commands']['item-relic'] == 60 and stages[0]['commands']['save'] == stages[0]['commands']['resume'] == 60
    assert stages[1]['commands']['potion-use'] == 54 and stages[1]['commands']['save'] == stages[1]['commands']['resume'] == 18
    assert stages[2]['commands']['relic-info'] == stages[2]['commands']['potion-info'] == 12
    assert stages[3]['commands']['item-relic-edges'] == stages[4]['commands']['item-potion-edges'] == 3
    for r in stages[4]['results']:
        if r['input']['command']=='item-potion-edges':
            assert r['result']['duplicateUses']==1 and r['result']['duplicateBlock']==6
            assert r['result']['nativeControl']==dict(invalidTargetCanceled=True,duplicateUses=1,duplicateBlock=12)
    assert stages[5]['commands']['item-antique-empty'] == 1
    assert stages[6]['commands']['potion-controller-use'] == 9
    for s in stages[7:]:
        attacks = [r['result'] for r in s['results'] if r['input']['command']=='item-coop-attack']
        assert [r['weak'] for r in attacks] == [1,1,0,0]
        assert s['commands']['item-coop-potion'] == 2 and s['commands']['power-coop-checkpoint'] == 12
    baseline_name = 'harness-items55-shared-baseline-regular-coop.json' if branch=='regular' else 'item-audit-items55-clones-baseline-beta.json'
    baseline = stage(baseline_name,game,True)
    if branch=='regular': assert [r['result']['weak'] for r in baseline['results'] if r['input']['command']=='item-coop-attack'] == [1,0]
    proof['branches'].append(dict(branch=branch, game=game, baseline=baseline, stages=stages, replies=sum(s['replies'] for s in stages)))

proof['limitations'] = [
    'Windows owned-game labs; physical Steam Deck/Linux, Workshop/Melty delivery and full campaigns were not tested.',
    'Controller checks use native focused callbacks and Godot action events, not physical controller hardware.',
    'Zero-enemy case checks native target validity synchronously; a live zero-enemy queued use would enter the native victory transition.',
    'Doubled potion effectiveness is deferred: neither tested branch exposes a supported potion-effect multiplier or Sacred Bark model. No synthetic game API was added.',
    'Full native combat saves restart the encounter; scalar mid-sequence persistence is tested with native relic serialization/replacement separately.',
    'Co-op tests cover starter first-hit ownership and Triune Draught effects, not the Cartesian product of every drop relic/potion/trait/peer count.',
]
proof['checks'] = dict(coreAssertions=164053, harnessChecks=20, preflight=dict(sheets=32,rows=2062,cells=26477))
diagnostics = {}
logs = []
for raw in sorted(profiles):
    profile = Path(raw)
    assert profile.resolve().is_relative_to((root/'private').resolve())
    path = profile/'errors.log'; content = path.read_text(errors='replace')
    blocks, current = [], []
    for line in content.splitlines():
        if line.startswith(('ERROR:', '[ERROR]', 'WARNING:')) and current:
            blocks.append('\n'.join(current)); current=[]
        current.append(line)
    if current: blocks.append('\n'.join(current))
    block_ids = []
    for block in blocks:
        # Full, deduplicated diagnostics; local usernames are not relevant to proof.
        text = block.strip().replace(str(root), '<repository>')
        if not text: continue
        digest = hashlib.sha256(text.encode()).hexdigest()
        diagnostics[digest] = text; block_ids.append(digest)
    logs.append(dict(profile=profile.name,sha256=hashlib.sha256(path.read_bytes()).hexdigest(),diagnostics=block_ids))
proof['logs'] = logs
proof['diagnostics'] = diagnostics

def compact(value, level=0):
    if isinstance(value,dict):
        return '{\n' + ',\n'.join('  '*(level+1)+json.dumps(str(k))+': '+compact(v,level+1) for k,v in value.items()) + '\n'+'  '*level+'}'
    if isinstance(value,list) and all(not isinstance(v,(dict,list)) for v in value): return json.dumps(value)
    if isinstance(value,list): return '[\n'+',\n'.join('  '*(level+1)+compact(v,level+1) for v in value)+'\n'+'  '*level+']'
    return json.dumps(value)

serialized = compact(proof)+'\n'
assert json.loads(serialized) == json.loads(json.dumps(proof))
(root/'docs/adversarial-items-evidence.json').write_text(serialized,encoding='utf-8')
print('Validated #55 native proof:',[(b['branch'],b['replies']) for b in proof['branches']])
