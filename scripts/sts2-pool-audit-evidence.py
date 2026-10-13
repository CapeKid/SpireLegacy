"""Validate completed #54 receipts; publish exact-build, issue-specific proof."""
import collections,hashlib,json,subprocess
from pathlib import Path
root=Path(__file__).resolve().parents[1];private=root/'private/sts2-evidence'
production=['sts2/Native/CardMaterialRecovery.cs','sts2/Native/PrismaticPoolOwnership.cs','sts2/Native/HeirCompendiumPool.cs']
proof=dict(issue=54,baselineSource='4352d0a',findings=[124,125,126],coreAssertions=164053,harnessChecks=20,
           preflight=dict(sheets=32,rows=2062,cells=26477),branches=[])
def receipt_stage(name,game,baseline=False):
    records=json.loads((private/name).read_text());ids=set();hashes=set();sources=set();commands=collections.Counter();results=[]
    for record in records:
        q=record['request'];a=record['response'];m=a['metadata'];rid=q['requestId']
        assert rid==a['requestId']==m['requestId'] and rid not in ids;ids.add(rid)
        assert m['game']==game and m['mod']=='1.0.29' and m['sourcePatchSha256'] is None
        assert {x['id']:x['version'] for x in m['mods']}=={'BaseLib':'v3.4.7','SpireLegacy':'1.0.29'}
        hashes.add(m['dllSha256']);sources.add(m['sourceSha']);commands[q['command']]+=1
        if not a['ok']:
            assert game=='v0.107.1' and q['command']=='pool-audit-import-run' and 'System.UInt32' in a['error'] and '.rng.seed' in a['error'],a
        if q['command'].startswith('pool-audit-'):
            value=a.get('result')
            # Keep historical ID/form coverage, without duplicating thousands of saved-card rows.
            if q['command']=='pool-audit-history':
                forms={(c['key'],c['upgrade']) for c in value['checks']};assert len(forms)==720
                assert forms=={(r['id'],up) for r in json.loads((root/'sheets/cards.json').read_text()) for up in [False,True]}
                value=dict(imported=value['import'],checks=len(forms),historicalIds=360,baseAndUpgraded=True)
            results.append(dict(input={k:v for k,v in q.items() if k!='requestId'},result=value,diagnostic=a.get('error')))
    assert len(hashes)==len(sources)==1
    source=next(iter(sources))
    stage=dict(file=name,replies=len(records),dllSha256=hashes.pop(),sourceSha=source,commands=dict(commands),
               seeds=sorted({r['response']['metadata']['seed'] for r in records if r['response']['metadata']['seed']}),results=results)
    if not baseline:
        stage['productionSha256']={p:hashlib.sha256(subprocess.check_output(['git','show',source+':'+p],cwd=root)).hexdigest() for p in production}
        for p,digest in stage['productionSha256'].items():assert digest==hashlib.sha256((root/p).read_bytes()).hexdigest(),p
    return stage
for branch,game in [('regular','v0.107.1'),('beta','v0.111.0')]:
    names=[f'pool-audit-pools54-{branch}-completed-export.json',f'pool-audit-pools54-{branch}-completed-import-crossrun.json',
           f'pool-audit-pools54-{branch}-eggs.json',f'pool-audit-pools54-{branch}-library-control-fixed.json',f'pool-audit-pools54-{branch}-parity.json',
           f'harness-pools54-owner-completed-{branch}-fixed-coop.json',f'harness-pools54ranger-owner-completed-{branch}-fixed-coop.json']
    stages=[receipt_stage(name,game) for name in names]
    for stage in stages:
        counts=stage['commands'];results=stage['results']
        if '-completed-export.' in stage['file']:
            assert counts['pool-audit-history']==3 and counts['pool-audit-owner']==8 and counts['pool-audit-sparse']==4 and counts['pool-audit-export-run']==4
            assert counts['pool-audit-library']==20 and counts['save']==counts['resume']==4
            assert all(len(r['result'])==60 for r in results if r['input']['command']=='pool-audit-sparse')
            menus=[r for r in results if r['input']['command']=='pool-audit-library' and r['input'].get('menu')]
            assert [r['input']['selectedClass'] for r in menus]==['knight','mage','ranger','knight']*4
        elif '-completed-import-crossrun.' in stage['file']:
            assert counts['pool-audit-import-run']==4 and counts['pool-audit-history']==3 and counts['pool-audit-owner']==8
            rejected=[r for r in results if r['diagnostic']];assert len(rejected)==(4 if branch=='regular' else 0)
        elif '-eggs.' in stage['file']:
            assert counts['pool-audit-eggs']==4 and all(len(r['result'])==7 for r in results if r['input']['command']=='pool-audit-eggs')
        elif '-library-control-fixed.' in stage['file']:
            library=[r['result'] for r in results if r['input']['command']=='pool-audit-library']
            assert len(library)==2 and all(r['rendered']>0 and not r['staleMaterials'] and not r['filteredLegacy'] for r in library)
        elif '-parity.' in stage['file']:
            parity=next(r['response']['result'] for r in json.loads((private/stage['file']).read_text()) if r['request']['command']=='pool-parity')
            assert len(parity['native'])==5 and len(parity['heirs'])==3
            for pool in parity['native']+parity['heirs']:assert (pool['common'],pool['uncommon'],pool['rare'])==((20,38,27) if branch=='beta' else (20,36,26)) and pool['total']==pool['unique']
            stage['nativeParity']=parity
        elif stage['file'].startswith('harness-'):
            assert counts['pool-audit-owner']==counts['pool-audit-modifiers']==counts['pool-audit-library']==2
            modifiers=[r['result'] for r in results if r['input']['command']=='pool-audit-modifiers'];assert modifiers[0]==modifiers[1]
            assert all(p['ownerPoolOnly'] for p in modifiers[0])
            owners=[r['result'] for r in results if r['input']['command']=='pool-audit-owner'];assert owners[0]==owners[1]
        for r in results:
            if r['input']['command']=='pool-audit-owner':
                assert all(len(p['rewards'])==65 and len(p['shops'])==10 and p['canonicalUnchanged'] for p in r['result'])
                for p in r['result']:
                    assert all(len(reward['ids'])==reward['count']==len(set(reward['ids'])) for reward in p['rewards'])
            if r['input']['command']=='pool-audit-modifiers':assert all(p['bypassPreserved'] and p['filterPreserved'] and p['upgraded']==12 for p in r['result'])
    baseline=receipt_stage(f'pool-audit-pools54-{branch}-library-control.json',game,True)
    libraries=[r['result'] for r in baseline['results'] if r['input']['command']=='pool-audit-library']
    assert not libraries[0]['staleMaterials'] and libraries[1]['staleMaterials'] and not libraries[1]['filteredLegacy']
    log=(root/'private'/f'campaign-pools54-{branch}-library-control-profile/errors.log').read_text(errors='replace')
    lines=log.splitlines();start=next(i for i,l in enumerate(lines) if 'System.ObjectDisposedException' in l);diagnostic=[]
    for line in lines[start:]:
        if diagnostic and not line.strip():break
        diagnostic.append(line)
    assert any('NCard.Reload' in l for l in diagnostic);baseline['completeMaterialDiagnostic']='\n'.join(diagnostic)
    diagnostics=[]
    for stage in stages:
        if stage['file'].startswith('harness-'):
            tag=stage['file'].removeprefix('harness-').removesuffix('-coop.json');folders=[root/'private'/f'coop-audit-{tag}-{i}-profile' for i in [3,4]]
        else:
            tag=stage['file'].removeprefix('pool-audit-').removesuffix('.json');folders=[root/'private'/f'campaign-{tag}-profile']
        for folder in folders:
            log=(folder/'errors.log').read_text(errors='replace')
            assert 'System.ObjectDisposedException' not in log,folder
            diagnostics.append(dict(profile=folder.name,sha256=hashlib.sha256((folder/'errors.log').read_bytes()).hexdigest(),
                                    invalidTaskIds=log.count('Invalid Task ID'),signalAwaiterErrors=log.count('SignalAwaiter'),particleWarnings=log.count('SUB_EMITTER_DISABLED')))
    proof['branches'].append(dict(branch=branch,game=game,baseLib='3.4.7',mod='1.0.29',platform='Microsoft Windows 10.0.26300',stages=stages,materialBaseline=baseline,diagnostics=diagnostics))
prism=receipt_stage('harness-pools54ranger-prism-baseline-coop.json','v0.107.1',True)
values=[r['result'] for r in prism['results'] if r['input']['command']=='pool-audit-modifiers'];assert len(values)==2 and values[0]!=values[1]
cached=receipt_stage('pool-audit-pools54-beta-compendium-baseline.json','v0.111.0',True)
menus=[r['result'] for r in cached['results'] if r['input']['command']=='pool-audit-library']
assert [m['cls'] for m in menus]==['knight','mage','ranger','knight'] and all(m['filteredLegacy']==menus[0]['filteredLegacy'] for m in menus)
proof['prismaticBaseline']=prism;proof['cachedFilterBaseline']=cached
all_stages=[s for b in proof['branches'] for s in b['stages']+[b['materialBaseline']]]+[prism,cached]
for stage in all_stages:
    for result in stage['results']:
        command=result['input']['command'];value=result['result']
        if command=='pool-audit-owner':
            summarized=[]
            for p in value:
                allowed={('CARD.SPIRELEGACY-C_'+key.upper()) if p['cls']!='native' else key for key in p['allowed']}
                assert all(set(r['ids']).issubset(allowed) for r in p['rewards']) and all(set(s['ids']).issubset(allowed) for s in p['shops'])
                assert all(t['created'] in allowed and t['created']!=t['original'] and t['choices']>0 for t in p['transforms'])
                summarized.append(dict(netId=p['NetId'],cls=p['cls'],pool=p['pool'],allowed=p['allowed'],canonicalUnchanged=p['canonicalUnchanged'],
                    rewardRequests=len(p['rewards']),rewardCards=sum(len(r['ids']) for r in p['rewards']),
                    rooms=dict(collections.Counter(r['room'] for r in p['rewards'])),choiceCounts=dict(collections.Counter(r['count'] for r in p['rewards'])),
                    distinctWithinEveryOffer=True,eligibleEveryOffer=True,seenRewards=sorted({key for r in p['rewards'] for key in r['ids']}),
                    shopInventories=len(p['shops']),shopCards=sum(len(s['ids']) for s in p['shops']),seenShopCards=sorted({key for s in p['shops'] for key in s['ids']}),
                    transformChecks=len(p['transforms']),eligibleTransforms=True))
            result['result']=summarized
        elif command=='pool-audit-registry':
            value['detail']=[dict(pool=p['pool'],count=len(p['cards']),rarities=dict(collections.Counter(c['rarity'] for c in p['cards'])),keys=sorted(c['key'] for c in p['cards'])) for p in value['detail']]
proof['limitations']=['Physical Deck/Linux/controller tests and full natural co-op campaigns remain #57/#110.',
    'Beta-to-regular full saves are natively incompatible: beta RNG seeds/state exceed regular UInt32 schema. All three Heirs and Ironclad reject with complete diagnostic; no truncation or save rewriting. Individual historical card data loads both ways; regular full runs load on beta.',
    'Native sparse reward inputs requesting more distinct choices than exist fail explicitly, matching Ironclad. No fallback into foreign class or duplicate cards is injected. Arbitrary third-party filter recovery remains outside supported active pools (#55/#61).',
    'Resource/WorkerThreadPool/signal-await diagnostics beyond the fixed disposed-material failure remain cause-unestablished (#58/#110); these are not clean-log claims.',
    'Regular explicit custom-card-pool API is tested; beta removed that API, so its custom-pool case is unavailable, not a pass.',
    'Card effect/paid play coverage belongs to #52/#53; no balance/win-rate or automatic publishing claims.']
def readable_json(value,depth=0):
    pad='  '*depth;child='  '*(depth+1)
    if isinstance(value,dict):return '{\n'+',\n'.join(child+json.dumps(str(k))+': '+readable_json(v,depth+1) for k,v in value.items())+'\n'+pad+'}' if value else '{}'
    if isinstance(value,list):
        if all(not isinstance(v,(dict,list)) for v in value):return json.dumps(value)
        return '[\n'+',\n'.join(child+readable_json(v,depth+1) for v in value)+'\n'+pad+']' if value else '[]'
    return json.dumps(value)
serialized=readable_json(proof);assert json.loads(serialized)==json.loads(json.dumps(proof))
output=root/'docs/adversarial-pools-evidence.json';output.write_text(serialized+'\n')
print(json.dumps(dict(output=output.name,branches=[dict(branch=b['branch'],stages=len(b['stages']),replies=sum(s['replies'] for s in b['stages'])) for b in proof['branches']])))
