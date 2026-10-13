"""Validate completed #53 native receipts and export compact, exact-build public proof."""
import collections,hashlib,json,subprocess
from pathlib import Path

root=Path(__file__).resolve().parents[1];evidence=root/'private/sts2-evidence'
proof=dict(issue=53,baselineSource='664f288',fixedIssue=121,seed='SPIRELEGACY-POWERS-53',coreAssertions=124044,harnessChecks=20,preflight=dict(sheets=32,rows=2062,cells=26477),branches=[])
for branch,game in [('regular','v0.107.1'),('beta','v0.111.0')]:
    stages=[]
    names=['power-audit-powers53-'+branch+'-'+tag+'.json' for tag in ['class-complete','engines','retention','copies','empty-fixed','supplement','chains','card-scry-complete']]
    names+=['harness-powers53-owner-'+branch+'-checkpoint-coop.json','harness-powers53ranger-owner-'+branch+'-prepared-coop.json']
    for name in names:
        records=json.loads((evidence/name).read_text());ids=set();hashes=set();sources=set();results=[]
        for record in records:
            q=record['request'];a=record['response'];m=a['metadata']
            assert a['ok'] and q['requestId']==a['requestId']==m['requestId'] and q['requestId'] not in ids;ids.add(q['requestId'])
            assert m['game']==game and m['mod']=='1.0.29' and m['sourcePatchSha256'] is None
            assert {x['id']:x['version'] for x in m['mods']}=={'BaseLib':'v3.4.7','SpireLegacy':'1.0.29'}
            hashes.add(m['dllSha256']);sources.add(m['sourceSha'])
            if q['command'].startswith('power-') or q['command'] in ['retention-check','card-choice-boundary']:results.append(dict(input={k:v for k,v in q.items() if k!='requestId'},result=a['result']))
        assert len(hashes)==1
        counts=dict(collections.Counter(r['request']['command'] for r in records))
        stage=dict(file=name,replies=len(records),dllSha256=hashes.pop(),sourceCheckpoints=sorted(sources),commands=counts,
                   seeds=sorted({r['response']['metadata']['seed'] for r in records if r['response']['metadata']['seed']}),results=results)
        if '-class-complete.' in name:
            assert counts['power-class']==50 and counts['power-rhythm']==6 and counts['power-barrier']==4 and counts['save']==counts['resume']==3
            expected={(c,up,mode) for c in ['knight','mage','ranger'] for up in [False,True] for mode in ['single','multi','area','random','zero','replay','dupe','lethal']+(['reservoir'] if c=='mage' else [])}
            assert {(r['result']['cls'],r['result']['up'],r['result']['mode']) for r in results if r['input']['command']=='power-class'}==expected
        elif '-engines.' in name:assert counts['power-engines']==36
        elif '-retention.' in name:assert counts['retention-check']==9
        elif '-copies.' in name:assert counts['power-copies']==12 and sum(len(r['result']) for r in results)==36
        elif '-empty-fixed.' in name:assert counts['power-engines']==6 and all(r['result']['deck']==0 and r['result']['nextTurn'] for r in results)
        elif '-supplement.' in name:assert counts['power-engines']==counts['power-retention-edges']==6 and counts['compat-control']==5
        elif '-chains.' in name:assert counts['power-class']==6 and all(len(r['result']['hits'])==1 for r in results)
        elif '-card-scry-complete.' in name:assert counts['card-choice-boundary']==counts['resume']==counts['card-deck-identity']==12
        elif name.startswith('harness-'):
            assert counts['power-coop-prepare']==counts['power-coop-play']==4 and counts['power-coop-checkpoint']==8
            assert all(r['result']['ownerOnly'] for r in results if r['input']['command']=='power-coop-play')
            assert all(r['result']['enabled'] for r in results if r['input']['command']=='power-coop-checkpoint')
        if not name.startswith('harness-'):assert records[-1]['request']['command']=='abandon'
        # Identify the actual production implementation, separate from fixture-only edits.
        source=stage['sourceCheckpoints'][0]
        stage['scryImplementationSha256']={p:hashlib.sha256(subprocess.check_output(['git','show',source+':'+p],cwd=root)).hexdigest() for p in ['sts2/Native/CardEffects.cs','sts2/Native/LegacyPower.cs']}
        stages.append(stage)
    failed=json.loads((evidence/('power-audit-powers53-'+branch+'-empty.json')).read_text())[-1]
    assert not failed['response']['ok'] and failed['request']['mode']=='foresight-empty'
    log=(root/'private'/('campaign-powers53-'+branch+'-empty-profile/errors.log')).read_text(errors='replace')
    lines=log.splitlines();index=next(i for i,l in enumerate(lines) if 'Sequence contains no elements' in l)
    diagnostic=[]
    for line in lines[index:]:
        if diagnostic and not line.strip():break
        diagnostic.append(line)
    assert any('SpireLegacy.LegacyPower.AfterPlayerTurnStart' in l for l in diagnostic)
    meta=failed['response']['metadata']
    finding=dict(input={k:v for k,v in failed['request'].items() if k!='requestId'},sourceSha=meta['sourceSha'],dllSha256=meta['dllSha256'],diagnostic='\n'.join(diagnostic),oracleFailure=failed['response']['error'],postFixCases=6)
    stages_platforms=sorted({r['response']['metadata']['platform'] for r in records})
    proof['branches'].append(dict(branch=branch,game=game,baseLib='3.4.7',mod='1.0.29',platforms=stages_platforms,stages=stages,finding=finding))
proof['binaryNote']='Each stage identifies its actual diagnostic DLL and source checkpoint, with production Scry implementation hashes. Early engine/copy and beta retention suites precede the Scry fix; post-fix empty-deck and ordinary Scry suites verify the changed path. Later fixture changes do not alter production effects. No package is published.'
proof['deferrals']=['Physical Deck/Linux and real controller/transport aborts or timed disconnects (#57/#110)','Full natural co-op campaigns and every relic/potion/native-alias combination (#55/#110)','Infinite resource loops, campaign balance and malformed/extreme design input (#60/#61)','Native NCard pool/WorkerThreadPool diagnostics, cause unestablished (#58/#110)','Arbitrary executor cancellation is unsupported by native API']
output=root/'docs/adversarial-powers-evidence.json';output.write_text(json.dumps(proof,indent=2)+'\n')
print(json.dumps(dict(output=output.name,branches=[dict(branch=b['branch'],stages=len(b['stages']),replies=sum(s['replies'] for s in b['stages'])) for b in proof['branches']])))
