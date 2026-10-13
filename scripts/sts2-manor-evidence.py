"""Validate private #57 native receipts; export bounded issue-specific proof."""
import collections,hashlib,json,subprocess
from pathlib import Path
root=Path(__file__).resolve().parents[1];private=root/'private/sts2-evidence'
production=['sts2/Native/ManorUi.cs','sts2/Native/ManorInput.cs','sts2/Native/HeirVisuals.cs']
proof=dict(issue=57,baselineMain='be412a0',findings=[133,134],coreAssertions=164053,harnessChecks=20,stages=[])
profiles=set()
for branch,game in [('regular','v0.107.1'),('beta','v0.111.0')]:
    for kind,filename in [('solo',f'manor-manor57-{branch}6.json'),('coop',f'manor-coop-manor57-{branch}6.json')]:
        records=json.loads((private/filename).read_text());ids=set();hashes=set();sources=set();counts=collections.Counter();observations=[]
        for r in records:
            q,a=r['request'],r['response'];m=a['metadata'];cmd=q['command']
            assert a['ok'] and q['requestId']==a['requestId']==m['requestId'] and q['requestId'] not in ids,a
            ids.add(q['requestId']);assert m['game']==game and m['sourcePatchSha256'] is None
            assert {x['id']:x['version'] for x in m['mods']}=={'BaseLib':'v3.4.7','SpireLegacy':'1.0.29'}
            hashes.add(m['dllSha256']);sources.add(m['sourceSha']);profiles.add(m['profile']);counts[cmd]+=1
            value=a['result']
            if cmd=='input':value={k:v for k,v in value.items() if k!='labels'}
            elif cmd=='lifecycle-state':
                assert value['profile']==value['persisted']
                value={k:value['profile'][k] for k in ['family','crowns','manor','banner','selected','active','chronicle']}
            elif cmd=='manor-audit':value={k:v for k,v in value.items() if k!='controls'}
            elif cmd=='manor-trait-fixture':value=dict(total=value['total'],ids=[t['id'] for t in value['traits']])
            elif cmd=='manor-resize':
                value=dict(window=value['window'],viewport=value['viewport'],controls=[c for c in value['controls'] if c['type'] in ['Label','Button','LineEdit']])
            elif cmd not in ['coop-play','coop-ready','coop-abandon','manor-state','manor-run-launch','manor-double-press','select','close','manor','edit','press','start','abandon']:continue
            observations.append(dict(command={k:v for k,v in q.items() if k!='requestId'},result=value))
        assert len(hashes)==len(sources)==1;source=sources.pop()
        digests={p:hashlib.sha256(subprocess.check_output(['git','show',source+':'+p],cwd=root)).hexdigest() for p in production}
        assert all(d==hashlib.sha256((root/p).read_bytes().replace(b'\r\n',b'\n')).hexdigest() for p,d in digests.items())
        if kind=='solo':
            assert records[-1]['request']['command']=='abandon' and counts['start']==3 and counts['manor-resize']==4
            traits={t for o in observations if o['command']['command']=='manor-trait-fixture' for t in o['result']['ids']}
            assert traits=={r['id'] for r in json.loads((root/'sheets/traits.json').read_text())}
        else:
            assert records[-1]['request']['command']=='coop-abandon' and counts['coop-play']==1
            banks=[o['result']['crowns'] for o in observations if o['command']['command']=='lifecycle-state']
            assert banks[:2]==[123,123]
        proof['stages'].append(dict(branch=branch,kind=kind,game=game,file=filename,replies=len(records),sourceSha=source,dllSha256=hashes.pop(),productionSha256=digests,commands=dict(counts),observations=observations))
proof['logs']=[];proof['diagnostics']={}
for raw in sorted(profiles):
    profile=Path(raw);assert profile.resolve().is_relative_to((root/'private').resolve())
    path=profile/'errors.log';blocks=[];current=[]
    for line in path.read_text(errors='replace').splitlines():
        if line.startswith(('ERROR:','[ERROR]','WARNING:')) and current:blocks.append('\n'.join(current));current=[]
        current.append(line)
    if current:blocks.append('\n'.join(current))
    refs=[]
    for block in blocks:
        text=block.strip().replace(str(root),'<repository>')
        if text:
            digest=hashlib.sha256(text.encode()).hexdigest();proof['diagnostics'][digest]=text;refs.append(digest)
    proof['logs'].append(dict(profile=profile.name,sha256=hashlib.sha256(path.read_bytes()).hexdigest(),diagnostics=refs))
proof['limitations']=['Synthetic native action events and callbacks are not physical Deck/controller/touch passes.','Physical Deck/Linux/Proton, Steam transport, hardware disconnect/reconnect, virtual-keyboard dismissal and touch/controller switching remain #110 device checks.','No natural full campaign or arbitrary UI-scale/localization matrix was run.','Trait fixtures render one trait per heir in valid three-offer batches; generated compatible pairs remain covered by existing trait/core checks.']
(root/'docs/adversarial-manor-evidence.json').write_text(json.dumps(proof,indent=2)+'\n')
print([(s['branch'],s['kind'],s['replies']) for s in proof['stages']])
