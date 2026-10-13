"""Validate immutable #60 receipts and publish compact measurements, never game files."""
import collections,hashlib,json
from pathlib import Path
root=Path(__file__).resolve().parents[1];private=root/'private/sts2-evidence';ids=set();stages=[]
cards={r['id']:r for r in json.loads((root/'sheets/cards.json').read_text())}
case_commands={'balance-audit-reset','balance-audit-encounter','balance-loop','balance-audit-native-loop','balance-audit-exchange','balance-audit-native-recovery','balance-audit-draw','balance-audit-coop-play','balance-audit-coop-draw-play'}
def load(name,branch,label,count,select=lambda r:True):
    rs=json.loads((private/name).read_text());hashes=set();sources=set();versions=set();profiles=set();cases=[];references=[];context={}
    for r in rs:
        q=r['request'];a=r['response'];m=a['metadata'];assert a['ok'],(name,a)
        assert q['requestId']==a['requestId']==m['requestId'] and q['requestId'] not in ids;ids.add(q['requestId'])
        assert m['game']==('v0.107.1' if branch=='regular' else 'v0.111.0') and m['sourcePatchSha256'] is None
        assert {x['id']:x['version'] for x in m['mods']}=={'BaseLib':'v3.4.7','SpireLegacy':'1.0.29'}
        hashes.add(m['dllSha256']);sources.add(m['sourceSha']);versions.add(m['platform']);profiles.add(m['profile'])
        if q['command']=='start':context={k:v for k,v in q.items() if k!='requestId'}
        if q['command']=='balance-audit-references':references=a['result']
        if q['command'] in case_commands and select(a['result']):
            result=a['result'];cases.append(dict(start=context,input={k:v for k,v in q.items() if k!='requestId'},result=result))
            if q['command']=='balance-audit-encounter':assert not result['exhaustedBudget']
            if q['command']=='balance-loop':assert not result['returnedRecovery'] and not result['returnedOriginal'] and result['energySpent']==0
            if q['command']=='balance-audit-exchange':assert result['energySpent']==(0 if q['card']=='native' else cards[q['card']]['cost']-int(q['upgrade'] and cards[q['card']]['upgradeCost']))
            if q['command'] in {'balance-audit-coop-play','balance-audit-coop-draw-play'}:assert result['ownerIsolated'] and result['checksumEnabled']
            if q['command']=='balance-audit-coop-draw-play':assert result['nativeChoice'] and result['energySpent']==1 and result['hand']==2+int(q['upgrade']) and result['discarded']==1
    assert len(hashes)==1 and len(cases)==count,(name,len(cases),count)
    logs=[]
    for p in profiles:
        path=Path(p)/'errors.log';text=path.read_text(errors='replace')
        assert 'NCombatRulesFtue.Start' not in text and 'checksum mismatch' not in text.lower() and 'Desync' not in text
        logs.append(dict(profile=Path(p).name,sha256=hashlib.sha256(path.read_bytes()).hexdigest(),headlines=dict(collections.Counter(line for line in text.splitlines() if line.startswith(('[ERROR]','ERROR:','WARNING:'))))))
    stage=dict(file=name,branch=branch,stage=label,dllSha256=hashes.pop(),declaredSources=sorted(sources),platform=sorted(versions),replies=len(rs),commands=dict(collections.Counter(r['request']['command'] for r in rs)),cases=cases,logs=logs)
    if references:stage['nativeReferences']=references
    stages.append(stage)
for branch in ['regular','beta']:
    prefix='adversarial-balance-balance60-'
    load(prefix+'baseline-reflex-'+branch+'.json',branch,'pre-fix-loop',4)
    load(prefix+'fixed-'+branch+'.json',branch,'Exhaust-only; demonstrates remaining Sly mismatch',12)
    load(prefix+'sly-fixed-'+branch+'.json',branch,'both hand-exchange fixes',12)
    load(prefix+'matrix-'+branch+'.json',branch,'completed Knight/Mage subset before driver trait-ID failure',24,lambda r:r['build'] not in {'discard','poison','retention','chain'})
    load(prefix+'matrix-ranger-'+branch+'.json',branch,'corrected Ranger pressure cohort',12)
    load(prefix+'exchange-'+branch+'.json',branch,'all hand-exchange native comparisons',8)
    load(prefix+'controls-'+branch+'.json',branch,'all-mod refusals and permitted Flash of Steel loops',12)
    load(prefix+'native-recovery-ready-'+branch+'.json',branch,'permitted native-enabled recovery loops',6)
    load(prefix+'draw-budget-baseline-'+branch+'.json',branch,'draw-budget baseline',6)
    load(prefix+'draw-budget-fixed-'+branch+'.json',branch,'all-class final draw budgets',18)
    for guest in ['mage','ranger']:
        load('balance-coop-balance60-coop-verified-'+branch+'-'+guest+'.json',branch,'native paid owner actions and synchronized discard choices',8)
campaigns=[]
for branch in ['regular','beta']:
    for cls in ['knight','mage','ranger','ironclad']:
        for tier in [0] if cls=='ironclad' else [0,2]:
            name=f'balance-campaign-balance60-natural-{branch}-{cls}-t{tier}.json';report=json.loads((private/name).read_text())
            assert report['status']=='finished' and report['interventions']==0 and report['ascension']==0 and report['traits']==[],name
            hashes=set();sources=set()
            for r in report['evidence']:
                q=r['request'];a=r['response'];m=a['metadata'];assert a['ok'] and q['requestId']==a['requestId']==m['requestId'] and q['requestId'] not in ids;ids.add(q['requestId'])
                assert q['command'] in {'start','balance-campaign-step'},'Campaign must not inject combat resources'
                assert m['game']==('v0.107.1' if branch=='regular' else 'v0.111.0') and m['seed']==report['seed']
                assert {x['id']:x['version'] for x in m['mods']}=={'BaseLib':'v3.4.7','SpireLegacy':'1.0.29'} and m['sourcePatchSha256'] is None
                hashes.add(m['dllSha256']);sources.add(m['sourceSha'])
            assert len(hashes)==1 and report['lastState']['hp']==0 and not report['lastState']['wins'],name
            s=report['lastState'];commands=collections.Counter(r['response']['result'].get('action') for r in report['evidence'])
            campaigns.append(dict(file=name,branch=branch,cls=cls,tier=tier,seed=report['seed'],source=sorted(sources),dllSha256=hashes.pop(),floor=s['floor'],act=s['act'],hp=s['hp'],crowns=s['lastEarned'],combatTurns=commands['combat-turn'],replies=len(report['evidence']),deck=s['deck'],relics=s['relics']))
proof=dict(issue=60,baseline='1780856',sourceIdentityNote='Early sourceSha values are declared launch checkouts. Updated launchers derive sourceSha from the DLL informational version, so later tests identify the compiled commit even when the checkout advances. Every stage retains its own actual loaded DLL hash.',coreAssertions=164059,payoutCombinations=14560,crownConservationCases=40000,findings=[141,142,143,144,145],nativeReplies=sum(s['replies'] for s in stages)+sum(c['replies'] for c in campaigns),campaigns=campaigns,limits=['Natural runs use a deterministic pilot, one seed per branch/class/tier, first legal map path, basic potion/choice policies and no purposeful shop/upgrade solver. They end in natural deaths; these are not human or campaign win-rate estimates.','Pressure trials add four fixed archetype cards and force Nibbits Normal. Risk cohorts additionally inject Family Seal. They have ordinary HP/Energy, but are controlled fights rather than natural reward acquisition.','Early stages use distinct diagnostic DLLs. The first pressure driver completed Knight/Mage then failed on the test label vegan instead of canonical nomeat. Its duplicate early Ranger rows are excluded; Ranger was rerun in fresh profiles.','The one-card Battle Meditation adjustment follows the initial pressure/campaign measurements. Their original hashes/budgets remain historical observations, not claims of campaigns with the final draw change.','A first ordinary-fuel loop assumption did not sustain; Reflex was required. Two recovery probes copied the prior DLL before compilation finished, answered unknown-command and are excluded. No timed-out mutation was replayed.','An initial co-op draw driver selected a stale visual holder after skipVisuals reset. Verified stages require current Hand membership and a newly opened choice task; failed co-op rows are excluded.','Physical Steam Deck/Linux and Steam transport remain #110 gaps. Higher-Ascension/full-winning multi-seed archetype campaigns and long-term purchase trajectories are tracked in #146.'] )
path=root/'docs/adversarial-balance-evidence.json'
lines=[json.dumps(proof,indent=2)[:-2]+',','  "stages": [']
for i,s in enumerate(stages):
    base={k:v for k,v in s.items() if k not in {'cases','nativeReferences'}}
    lines.append('    '+json.dumps(base)[:-1]+', "cases": [')
    lines.extend('      '+json.dumps(c)+(',' if j+1<len(s['cases']) else '') for j,c in enumerate(s['cases']))
    lines.append('    ], "nativeReferences": '+json.dumps(s.get('nativeReferences',[]))+'}'+(',' if i+1<len(stages) else ''))
lines+=['  ]','}'];path.write_text('\n'.join(lines)+'\n');json.loads(path.read_text())
print(json.dumps(dict(stages=len(stages),cases=sum(len(s['cases']) for s in stages),campaigns=len(campaigns),nativeReplies=proof['nativeReplies'])))
