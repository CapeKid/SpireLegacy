"""Validate #105 native receipts and export exact-build payout evidence."""
import collections,json
from pathlib import Path
root=Path(__file__).resolve().parents[1];proof=dict(issue=105,baseline='6a58dc9',coreAssertions=164053,conservationGrid=40000,branches=[])
for branch,game in [('regular','v0.107.1'),('beta','v0.111.0')]:
    stages=[]
    for kind in ['matrix','campaign']:
        name='crowns-crowns105-'+branch+'-'+kind+'.json';rs=json.loads((root/'private/sts2-evidence'/name).read_text())
        ids=set();hashes=set();sources=set();cases=[];pending=None;before=None;observations=[]
        for r in rs:
            q=r['request'];a=r['response'];m=a['metadata']
            assert a['ok'] and q['requestId']==a['requestId']==m['requestId'] and q['requestId'] not in ids;ids.add(q['requestId'])
            assert m['game']==game and m['mod']=='1.0.29' and m['sourcePatchSha256'] is None
            assert {x['id']:x['version'] for x in m['mods']}=={'BaseLib':'v3.4.7','SpireLegacy':'1.0.29'}
            hashes.add(m['dllSha256']);sources.add(m['sourceSha'])
            if q['command']=='start':pending=dict(cls=q['class'],quick=q['quick']);before=None;observations=[]
            if q['command'] in ['crown-state','crown-play','crown-gold']:observations.append(dict(input={k:v for k,v in q.items() if k!='requestId'},result=a['result']))
            if q['command']=='lifecycle-state':
                p=a['result']['profile'];assert p==a['result']['persisted']
                if p['active'] is not None and before is None:before=p
                elif p['active'] is None and pending is not None:
                    assert before and p['generation']==before['generation']+1 and p['settledId']==before['active']['id']
                    assert p['chronicle']['settledRuns'].count(p['settledId'])==1
                    delta=p['crowns']-before['crowns'];assert delta==p['lastEarned']
                    cases.append(dict(**pending,crowns=delta,chronicle=p['chronicle']['classes'].get(pending['cls']),observations=observations));pending=None
        assert pending is None and len(hashes)==1 and len(cases)==(42 if kind=='matrix' else 3)
        assert all(c['crowns']==0 for c in cases if not c['quick']) if kind=='matrix' else True
        if kind=='campaign':assert not any(r['request']['command'] in ['enter','kill','crown-play','crown-gold'] for r in rs)
        stages.append(dict(kind=kind,file=name,source=sorted(sources),dllSha256=hashes.pop(),replies=len(rs),commands=dict(collections.Counter(r['request']['command'] for r in rs)),cases=cases))
    proof['branches'].append(dict(branch=branch,game=game,baseLib='3.4.7',mod='1.0.29',stages=stages))
proof['policy']='After qualifying combat progress: 2 crowns per completed non-Ancient map point plus earned unspent gold at 25:1, then existing capped trait/Treasury multipliers. A non-abandoned combat death after a completed manual card play gets one participation point. Reached floors remain separate Chronicle data. Starting gold is excluded and earned gold is spent/lost first.'
proof['limits']=['Matrix wins lower enemy HP and grant fixture card/Energy; death uses native induced damage. They are controlled integration cases, not campaign balance samples.','Campaigns use natural map entry and paid actions without HP/gold/debug interventions; bounded at reached floor six or natural death. They are not full campaigns or win-rate estimates.','Physical Deck/Linux, Steam transport and full natural multiplayer campaigns are untested. Existing owner settlement guards and pure co-op checks pass.','Stages have distinct diagnostic DLLs; the second build only adds observation fields and campaign driver support. No publishing or release-package changes.']
path=root/'docs/earned-crowns-evidence.json';path.write_text(json.dumps(proof,indent=2)+'\n');print(json.dumps([(b['branch'],[(s['kind'],len(s['cases']),s['replies']) for s in b['stages']]) for b in proof['branches']]))
