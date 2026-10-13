"""Validate #56 private native receipts and export issue-specific proof."""
import collections,hashlib,json,subprocess
from pathlib import Path
root=Path(__file__).resolve().parents[1];private=root/'private/sts2-evidence'
production=['sts2/Core/AncientConversation.cs','sts2/Core/ArchitectConversation.cs','sts2/Native/AncientCompatibility.cs','sts2/Native/HeirAncientConversations.cs','sts2/Native/HeirArchitectDialogue.cs','sheets/ancient_dialogue.json','sheets/ancient_cards.json']
proof=dict(issue=56,baselineMain='cc998f3',productionFindings=[],coreAssertions=164053,harnessChecks=20,branches=[])
proof['productionUnchanged']=all(subprocess.check_output(['git','show','cc998f3:'+p],cwd=root).replace(b'\r\n',b'\n')==(root/p).read_bytes().replace(b'\r\n',b'\n') for p in production)
assert proof['productionUnchanged']
profiles=set()
def stage(name,game):
    records=json.loads((private/name).read_text());ids=set();hashes=set();sources=set();counts=collections.Counter();results=[];cls=None;traits=[]
    for record in records:
        q,a=record['request'],record['response'];m=a['metadata'];cmd=q['command']
        assert a['ok'] and q['requestId']==a['requestId']==m['requestId'] and q['requestId'] not in ids,a
        ids.add(q['requestId']);assert m['game']==game and m['sourcePatchSha256'] is None
        assert {x['id']:x['version'] for x in m['mods']}=={'BaseLib':'v3.4.7','SpireLegacy':'1.0.29'}
        hashes.add(m['dllSha256']);sources.add(m['sourceSha']);profiles.add(m['profile']);counts[cmd]+=1
        if cmd=='start':cls=q['class'];traits=q.get('traits',[])
        if cmd.startswith(('ancient-audit-','architect-dialogues','ancient-play')) or cmd=='ancient-enter':
            value=a['result']
            if cmd=='ancient-audit-selectors':
                assert value['checks']==len(value['details'])==672
                actual={(r['ancient'],r['cls'],r['visits'],r['total'],r['allowAny']) for r in value['details']}
                expected={(n,c,v,t,b) for n in value['ancients'] for c in ['knight','mage','ranger'] for v in [-1,0,1,4,10,100,2147483647] for t in [0,200] for b in [False,True]}
                assert actual==expected
                for r in value['details']:assert all('.'+r['cls']+'.'+('first' if r['visits']<=0 else 'return')+'.' in k for k in r['keys'])
                value=dict(checks=value['checks'],ancients=value['ancients'],cases=[list(row) for row in sorted(actual)])
            elif cmd=='ancient-audit-choose':assert value['finished'] and value['once'] and len(value['acquired'])>=1,value
            elif cmd=='ancient-audit-ending-win':assert value['nativeGameOver'] and value['once'],value
            elif cmd=='ancient-audit-tome':assert len(value['seen'])==2 and value['upgrade']==1 and value['saveRoundTrip'],value
            elif cmd=='ancient-audit-play-modifier':assert value['saveRoundTrip'] and not value['tableNodePresent'] and value['pile']!='Play',value
            elif cmd=='ancient-play':assert value['played']['saveRoundTrip'] and not value['played']['tableNodePresent'],value
            results.append(dict(input={k:v for k,v in q.items() if k!='requestId'},cls=cls,traits=traits,result=value))
    assert len(hashes)==len(sources)==1;source=sources.pop()
    digests={p:hashlib.sha256(subprocess.check_output(['git','show',source+':'+p],cwd=root)).hexdigest() for p in production}
    assert all(d==hashlib.sha256((root/p).read_bytes().replace(b'\r\n',b'\n')).hexdigest() for p,d in digests.items())
    return dict(file=name,replies=len(records),sourceSha=source,dllSha256=hashes.pop(),productionSha256=digests,commands=dict(counts),seeds=sorted({r['response']['metadata']['seed'] for r in records if r['response']['metadata']['seed']}),results=results)

for branch,game in [('regular','v0.107.1'),('beta','v0.111.0')]:
    names=[f'ancient-audit-ancients56-selectors-{branch}-completed.json',
           'ancient-audit-ancients56-dialogues-regular-full.json' if branch=='regular' else 'ancient-audit-ancients56-dialogues-beta-native.json',
           'ancient-audit-ancients56-endings-regular.json' if branch=='regular' else 'ancient-audit-ancients56-endings-beta-completed.json',
           f'ancient-audit-ancients56-cards-{branch}.json',f'ancient-audit-ancients56-modifiers-{branch}-completed.json',
           'ancient-coop-ancients56-mage-regular.json' if branch=='regular' else 'ancient-coop-ancients56-mage-beta-ready.json',
           f'ancient-coop-ancients56-ranger-{branch}.json',f'ancient-coop-ancients56-native-{branch}.json',f'ancient-audit-ancients56-saveboundaries-{branch}-completed.json']
    stages=[stage(n,game) for n in names]
    assert stages[0]['commands']['ancient-audit-selectors']==stages[0]['commands']['ancient-audit-fallbacks']==stages[0]['commands']['architect-dialogues']==1
    assert stages[1]['commands']['ancient-enter']==stages[1]['commands']['ancient-audit-choose']==stages[1]['commands']['save']==stages[1]['commands']['resume']==48
    assert stages[1]['commands']['ancient-audit-next']==144
    assert stages[2]['commands']['ancient-audit-ending-win']==6 and stages[2]['commands']['ancient-audit-ending-next']==18
    assert stages[3]['commands']['ancient-audit-tome']==stages[3]['commands']['ancient-play']==12
    assert stages[4]['commands']['ancient-audit-play-modifier']==48
    expected_cards={r['cardId'] for r in json.loads((root/'sheets/ancient_cards.json').read_text())}
    modifiers=[r for r in stages[4]['results'] if r['input']['command']=='ancient-audit-play-modifier']
    assert {(r['input']['card'],r['input']['upgrade'],tuple(r['traits'])) for r in modifiers}=={(card,up,trait) for card in expected_cards for up in [False,True] for trait in [(),('cantattack',),('megahealth',),('superhealer',)]}
    for r in modifiers:
        if r['input']['card']=='mage_elixir_of_life':assert r['result']['hpChange']==(0 if r['traits']==['megahealth'] else 12 if r['traits']==['superhealer'] else 6)
        if r['input']['card']=='knight_kingbreaker' and r['traits']==['cantattack']:assert r['result']['blocked'] and r['result']['energyChange']==0
    for s in stages[5:8]:
        owners=[r['result'] for r in s['results'] if r['input']['command']=='ancient-audit-owners'];assert len(owners)==2 and owners[0]==owners[1]
        assert all(len(o['ancientKeys'])==24 for o in owners[0])
    assert stages[8]['commands']['save']==12 and stages[8]['commands']['resume']==18 and stages[8]['commands']['ancient-audit-save-finished']==6 and stages[8]['commands']['ancient-audit-finished']==6 and stages[8]['commands']['tome-parity']==6
    proof['branches'].append(dict(branch=branch,game=game,stages=stages,replies=sum(s['replies'] for s in stages)))

logs=[];diagnostics={}
for raw in sorted(profiles):
    profile=Path(raw);assert profile.resolve().is_relative_to((root/'private').resolve())
    path=profile/'errors.log';content=path.read_text(errors='replace');blocks=[];current=[]
    for line in content.splitlines():
        if line.startswith(('ERROR:','[ERROR]','WARNING:')) and current:blocks.append('\n'.join(current));current=[]
        current.append(line)
    if current:blocks.append('\n'.join(current))
    refs=[]
    for block in blocks:
        text=block.strip().replace(str(root),'<repository>')
        if text: digest=hashlib.sha256(text.encode()).hexdigest();diagnostics[digest]=text;refs.append(digest)
    logs.append(dict(profile=profile.name,sha256=hashlib.sha256(path.read_bytes()).hexdigest(),diagnostics=refs))
proof.update(logs=logs,diagnostics=diagnostics,limitations=[
    'Physical Steam Deck/Linux, non-English locale switching and full campaigns were not tested.',
    'Synthetic input and native forced-click callbacks are not physical mouse/controller passes.',
    'Co-op covers actual peer rosters with native owner-specific model initialization and selectors; remote event UI and full co-op Architect victory are deferred to #110.',
    'Ending fixtures place a disposable run in its final act and set native prior-win counters; these are callback tests, not natural campaign completions.',
    'Pacifist intentionally forbids non-basic Ancient Attacks; Dusty Tome can still offer such a card. Existing trait/reward balance was not changed.',
])
def compact(value,level=0):
    if isinstance(value,dict):return '{\n'+',\n'.join('  '*(level+1)+json.dumps(str(k))+': '+compact(v,level+1) for k,v in value.items())+'\n'+'  '*level+'}'
    if isinstance(value,list) and all(not isinstance(v,dict) and (not isinstance(v,list) or all(not isinstance(x,(dict,list)) for x in v)) for v in value):return json.dumps(value)
    if isinstance(value,list):return '[\n'+',\n'.join('  '*(level+1)+compact(v,level+1) for v in value)+'\n'+'  '*level+']'
    return json.dumps(value)
serialized=compact(proof)+'\n';assert json.loads(serialized)==json.loads(json.dumps(proof))
(root/'docs/adversarial-ancients-evidence.json').write_text(serialized,encoding='utf-8')
print('Validated #56 proof:',[(b['branch'],b['replies']) for b in proof['branches']])
