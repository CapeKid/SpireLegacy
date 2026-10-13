"""Validate #58 native receipts and export compact issue-specific proof."""
import collections,hashlib,json,subprocess
from pathlib import Path
root=Path(__file__).resolve().parents[1];private=root/'private/sts2-evidence'
paths=['sts2/Native/'+p for p in ['HeirSprite.cs','HeirVisuals.cs','HeirEndingVisuals.cs','HeirPresentation.cs','TraitAppearance.cs','Runtime.cs']]
proof=dict(issue=58,baselineMain='0c10f2d',mod='1.0.29',BaseLib='v3.4.7',findings=[64,85,136,137,138,139],coreAssertions=164053,harnessChecks=20,stages=[])
profiles=set()
for branch,game in [('regular','v0.107.1'),('beta','v0.111.0')]:
    for kind,name in [('solo',f'presentation-audit-presentation58-{branch}4.json'),('coop-mage',f'presentation-coop-presentation58-{branch}-room-mage.json'),('coop-ranger',f'presentation-coop-presentation58-{branch}-room-ranger.json'),('cleanup',f'presentation-audit-presentation58-{branch}-cleanup-visual.json'),('effects',f'presentation-audit-presentation58-{branch}-effects.json')]:
        records=json.loads((private/name).read_text());ids=set();hashes=set();sources=set();counts=collections.Counter();observations=[]
        for r in records:
            q,a=r['request'],r['response'];m=a['metadata'];cmd=q['command']
            assert a['ok'] and q['requestId']==a['requestId']==m['requestId'] and q['requestId'] not in ids,a
            ids.add(q['requestId']);assert m['game']==game and m['sourcePatchSha256'] is None
            assert {x['id']:x['version'] for x in m['mods']}=={'BaseLib':'v3.4.7','SpireLegacy':'1.0.29'}
            hashes.add(m['dllSha256']);sources.add(m['sourceSha']);profiles.add(m['profile']);counts[cmd]+=1;value=a['result']
            if cmd=='presentation-paid':
                assert value['accepted'] and value['energySpent']==value['cost'] and value['pile']=='Discard'
                if 'presentation-crab' in counts and counts['start']<=3:assert (value['after']['nativeX']<0)==(value['after']['spriteX']<0)
                unique=[]
                for s in value['samples']:
                    if not unique or s!=unique[-1]:unique.append(s)
                value=dict(value,sampledFrames=len(value['samples']),samples=unique)
            elif cmd=='presentation-trail':assert all(s['alpha']==1 and s['parent']=='Trails' for s in value['samples'])
            elif cmd=='presentation-room-facing':assert all((s['nativeX']<0)==(s['spriteX']<0) for s in value)
            elif cmd=='presentation-shop':
                assert value['bought'] and value['goldSpent']>0 and value['cardsAdded']==1
                assert all(s['skillEvents']==(1 if s['owner']==value['owner'] else 0) for s in value['sprites'])
            elif cmd=='state':continue
            elif cmd not in ['visual','room-visual','trait-appearance','presentation','presentation-rest','presentation-transition','presentation-resources','presentation-texture-failures','ending-visual','presentation-motion','presentation-prepare','start','enter','save','resume','kill','abandon']:continue
            observations.append(dict(command={k:v for k,v in q.items() if k!='requestId'},result=value))
        assert len(hashes)==len(sources)==1;source=sources.pop()
        compare=paths+(['sts2/Native/HeirRoomFacing.cs'] if kind!='solo' else [])
        digests={p:hashlib.sha256(subprocess.check_output(['git','show',source+':'+p],cwd=root)).hexdigest() for p in compare}
        later_patches=[]
        for p,d in digests.items():
            current=(root/p).read_bytes().replace(b'\r\n',b'\n')
            if d==hashlib.sha256(current).hexdigest():continue
            assert p=='sts2/Native/HeirPresentation.cs' and kind!='cleanup'
            def without_motion(text):
                start=text.index('public partial class HeirRoomMotion');end=text.index('[HarmonyPatch]',start)
                return text[:start]+'<later-tested HeirRoomMotion>'+text[end:]
            old=subprocess.check_output(['git','show',source+':'+p],cwd=root).decode()
            assert without_motion(old)==without_motion(current.decode())
            later_patches.append(dict(file=p,scope='HeirRoomMotion reparent/cleanup fix',verifiedBy='cleanup stages'))
        if kind=='solo':
            assert records[-1]['request']['command']=='screenshot' and counts['start']==24 and counts['presentation-paid']==24 and counts['ending-visual']==12
            stats=[o['result'] for o in observations if o['command']['command']=='presentation-resources'];assert len(stats)==9
            assert all(s['sprites']==1 and s['audio']==s['trails']==0 for s in stats)
            assert len({s['textureSlots'] for s in stats[3:]})==1
        elif kind=='effects':
            assert records[-1]['request']['command']=='presentation-texture-failures' and counts['start']==3 and counts['presentation-paid']==12 and counts['presentation']==3
            assert 'NCombatRulesFtue.Start' not in (Path(records[0]['response']['metadata']['profile'])/'errors.log').read_text(errors='replace')
        elif kind=='cleanup':
            assert records[-1]['request']['command']=='screenshot' and counts['presentation-motion']==3 and counts['start']==9
            assert all(o['result']['moving'] and o['result']['secondMove'] for o in observations if o['command']['command']=='presentation-motion')
            assert 'disconnect a nonexistent connection' not in (Path(records[0]['response']['metadata']['profile'])/'errors.log').read_text(errors='replace')
        else:assert records[-1]['request']['command']=='coop-abandon' and counts['presentation-shop']==counts['presentation-room-facing']==2
        proof['stages'].append(dict(branch=branch,kind=kind,game=game,file=name,receiptSha256=hashlib.sha256((private/name).read_bytes()).hexdigest(),replies=len(records),sourceSha=source,dllSha256=hashes.pop(),seeds=sorted({r['response']['metadata']['seed'] for r in records if r['response']['metadata']['seed']}),platforms=sorted({r['response']['metadata']['platform'] for r in records}),productionSha256=digests,laterCoveredPatches=later_patches,commands=dict(counts),observations=observations))
proof['baselines']=[]
for name,commands in [('presentation-audit-presentation58-baseline.json',{'presentation-paid','presentation-trail'}),('presentation-audit-presentation58-beta1.json',{'presentation-texture-failures'}),('presentation-coop-presentation58-baseline-shop3.json',{'presentation-shop'}),('presentation-coop-presentation58-baseline-rest2.json',{'presentation-room-facing'})]:
    raw=json.loads((private/name).read_text());chosen=[r for r in raw if r['request']['command'] in commands]
    proof['baselines'].append(dict(file=name,receiptSha256=hashlib.sha256((private/name).read_bytes()).hexdigest(),receipts=chosen))
proof['baselineNote']='The initial crab/ribbon baseline overlays opt-in test probes on unchanged main presentation code; its declared main SHA is not a commit of those probe additions. Exact loaded DLL hashes and native outcomes are retained. Later baselines have their own source/DLL metadata.'
proof['assets']=[dict(path=r['path'],sha256=r['sha256']) for r in json.loads((root/'sheets/presentation_art.json').read_text())]
for asset in proof['assets']:assert hashlib.sha256((root/'src/main/resources'/asset['path']).read_bytes()).hexdigest()==asset['sha256']
proof['logs']=[];proof['diagnostics']={}
for raw in sorted(profiles):
    profile=Path(raw);assert profile.resolve().is_relative_to((root/'private').resolve());path=profile/'errors.log';blocks=[];current=[]
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
proof['limitations']=['Physical Steam Deck/Linux/Proton, audible quality, Steam transport, full campaigns and long-duration soak are not passes.','Compatibility OpenGL renderer was used; Vulkan and reduced-particle configuration matrices are deferred.','Synthetic fixtures fund paid plays and purchases and directly select rooms; co-op purchases exercise native callbacks locally rather than the complete network shop UI action route.','Godot static-memory monitor returned zero/unavailable; memory figures are process PrivateMemorySize64, not GPU or resident-memory measurements.','Corrupt WAV/scene/material injection and arbitrary missing-file combinations are deferred; tested failures are invalid SVG, missing PNG and invalid sound role.','Solo lifecycle stages precede the added co-op rest flip hook. Existing presentation components match final source; the additional hook is covered by both final co-op class pairs.']
def render(v,depth=0):
    pad='  '
    if isinstance(v,dict):return '{\n'+',\n'.join(pad*(depth+1)+json.dumps(k)+': '+render(x,depth+1) for k,x in v.items())+'\n'+pad*depth+'}'
    if isinstance(v,list):
        if v and all(isinstance(x,dict) and ('command' in x or 'request' in x) for x in v):return '[\n'+',\n'.join(pad*(depth+1)+json.dumps(x) for x in v)+'\n'+pad*depth+']'
        if all(not isinstance(x,(list,dict)) for x in v):return json.dumps(v)
        return '[\n'+',\n'.join(pad*(depth+1)+render(x,depth+1) for x in v)+'\n'+pad*depth+']'
    return json.dumps(v)
serialized=render(proof)+'\n';assert json.loads(serialized)==proof
(root/'docs/adversarial-presentation-evidence.json').write_text(serialized)
print([(s['branch'],s['kind'],s['replies']) for s in proof['stages']])
