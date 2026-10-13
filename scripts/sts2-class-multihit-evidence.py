"""Validate #130 native controls, baseline and multi-hit regressions."""
import collections,hashlib,json,subprocess
from pathlib import Path
root=Path(__file__).resolve().parents[1];private=root/'private/sts2-evidence'
production=['sts2/Native/LegacyPower.cs','sts2/Core/CardDesign.cs','sheets/classes.json','sheets/class_mechanics.json']
proof=dict(issue=130,baselineMain='70ff82d',coreAssertions=164053,harnessChecks=20,branches=[])

def stage(name,game,baseline=False):
    records=json.loads((private/name).read_text());ids=set();hashes=set();sources=set();results=[];counts=collections.Counter()
    for record in records:
        q,a=record['request'],record['response'];m=a['metadata'];command=q['command']
        assert q['requestId']==a['requestId']==m['requestId'] and q['requestId'] not in ids;ids.add(q['requestId'])
        assert a['ok'] and m['game']==game and m['mod']=='1.0.29' and m['sourcePatchSha256'] is None,a
        assert {x['id']:x['version'] for x in m['mods']}=={'BaseLib':'v3.4.7','SpireLegacy':'1.0.29'}
        hashes.add(m['dllSha256']);sources.add(m['sourceSha']);counts[command]+=1
        if command in {'power-class','power-damage-control','power-multihit-coop-play','power-coop-checkpoint'}:
            results.append(dict(input={k:v for k,v in q.items() if k!='requestId'},result=a['result']))
    assert len(hashes)==len(sources)==1;source=sources.pop()
    digests={p:hashlib.sha256(subprocess.check_output(['git','show',source+':'+p],cwd=root)).hexdigest() for p in production}
    if not baseline:assert all(d==hashlib.sha256((root/p).read_bytes().replace(b'\r\n',b'\n')).hexdigest() for p,d in digests.items())
    return dict(file=name,replies=len(records),dllSha256=hashes.pop(),sourceSha=source,productionSha256=digests,commands=dict(counts),seeds=sorted({r['response']['metadata']['seed'] for r in records if r['response']['metadata']['seed']}),results=results)

for branch,game in [('regular','v0.107.1'),('beta','v0.111.0')]:
    baseline=stage(f'power-audit-class130-baseline-{branch}.json',game,True)
    fixed=stage(f'power-audit-class130-fixed-{branch}.json',game)
    coop=stage(f'harness-class130-fixed-{branch}-coop.json',game)
    before=[r['result'] for r in baseline['results'] if r['input']['command']=='power-class']
    after=[r['result'] for r in fixed['results'] if r['input']['command']=='power-class']
    assert len(before)==6 and len(after)==38
    for r in before:
        base=7 if r['up'] else 5;bonus=0 if r['cls']=='ranger' else 2
        assert r['hits']==[base+bonus,base] and r['Charges']==0,r
    for r in after:
        assert r['cloneIndependent'] and r['savedProperties'] and r['Charges']==0
        if r['mode']=='multi':assert r['hits']==[(7 if r['up'] else 5)+r['bonus']]*2,r
        elif r['mode']=='reservoir':assert r['hits']==[(7 if r['up'] else 5)+6]*2,r
        elif r['mode']=='replay':
            base=7 if r['up'] else 5;assert r['hits']==[base+2,base+2,base,base],r
    for s in [baseline,fixed]:
        controls=[r['result'] for r in s['results'] if r['input']['command']=='power-damage-control'];assert len(controls)==4
        for r in controls:
            assert r['first']==[9 if r['up'] else 7]*2 and r['vigorRemaining']==0
            assert r['next']==(9 if r['up'] else 6)+(2 if r['power']=='strength' else 0)
    pairs=[r['result'] for r in coop['results'] if r['input']['command']=='power-multihit-coop-play']
    assert len(pairs)==4 and {(r['cls'],r['up']) for r in pairs}=={(c,u) for c in ['knight','mage'] for u in [False,True]}
    for r in pairs:
        base=7 if r['up'] else 5;assert r['ownerOnly'] and r['Charges']==0 and r['hits']==[[base+2]*2,[base]*2],r
    assert coop['commands']['power-coop-checkpoint']==8
    assert all(r['result']['enabled'] for r in coop['results'] if r['input']['command']=='power-coop-checkpoint')
    proof['branches'].append(dict(branch=branch,game=game,baseline=baseline,fixed=fixed,coop=coop))
proof['limitations']=['Windows copied-game labs; physical Deck/Linux, full campaigns and distribution updates were not tested.','This intentionally increases multi-hit class synergy; charge caps, card amounts and Ranger mechanics are unchanged.']
(root/'docs/class-multihit-evidence.json').write_text(json.dumps(proof,indent=2)+'\n',encoding='utf-8')
print('Validated native #130 controls, 76 solo class cases and 8 two-peer class cases.')
