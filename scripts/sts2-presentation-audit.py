"""#58 native presentation regression and lifecycle/resource audit."""
import hashlib,json,sys,time,shutil
from pathlib import Path
from oracle_protocol import call
root=Path(__file__).resolve().parents[1];profile=Path(sys.argv[1]);tag=sys.argv[2];baseline='baseline' in tag
expected=hashlib.sha256((root/'sts2/bin/Release/net9.0/SpireLegacy.dll').read_bytes()).hexdigest()
records=[];output=root/'private/sts2-evidence'/('presentation-audit-'+tag+'.json');assert not output.exists()
def send(command,**args):
    r=call(profile,dict(command=command,**args),timeout=90,expected_hash=expected);records.append(r);output.write_text(json.dumps(records,indent=2));assert r['response']['ok'],r
    return r['response']['result']
def ready():
    deadline=time.monotonic()+45
    while send('state')['phase']!='Play':
        assert time.monotonic()<deadline;time.sleep(.15)
trait_ids={r['id'] for r in json.loads((root/'sheets/traits.json').read_text())}
def start(cls,traits=[]):
    assert set(traits)<=trait_ids
    send('start',**{'class':cls,'traits':traits,'quick':True,'seed':'SPIRELEGACY-PRESENTATION-58'});ready()
def shot(name):
    p=send('screenshot')['path'];shutil.copyfile(p,root/'private/sts2-evidence'/f'presentation58-{tag}-{name}.png')
if not baseline:send('manor-resize',width=1280,height=800)
send('presentation-prepare')
if 'cleanup' in tag:
    for cls in ['knight','mage','ranger']:
        r=send('presentation-motion',**{'class':cls});assert r['moving'] and r['secondMove']
        for room in ['RestSite','Shop','Monster']:
            start(cls);send('enter',room=room)
            if room=='Monster':ready();send('kill',player=True)
            else:time.sleep(.3);send('abandon')
            time.sleep(1)
            ending=send('ending-visual');assert len(ending)==1 and not ending[0]['sprites'][0]['placeholderVisible']
            shot(cls+'-'+room+'-ending')
    time.sleep(.3)
    errors=(profile/'errors.log').read_text(errors='replace')
    assert 'disconnect a nonexistent connection' not in errors and 'NCombatRulesFtue.Start' not in errors
    print(tag,'live reparenting and all-class room-ending cleanup passed',flush=True);raise SystemExit(0)
for cls in ['knight'] if baseline else ['knight','mage','ranger']:
    start(cls);send('presentation-crab');ready()
    for upgraded in [False,True]:
        for left in [True,False]:
            r=send('presentation-paid',card=cls+'_strike',upgrade=upgraded,left=left)
            assert r['accepted'] and r['energySpent']==r['cost'] and r['pile']=='Discard'
            if not baseline:assert (r['after']['nativeX']<0)==(r['after']['spriteX']<0)
            print(tag,cls,upgraded,left,'facing',r['after'],flush=True)
    shot(cls+'-crab')
    r=send('presentation-trail',**{'class':cls})
    if not baseline:assert all(s['parent']=='Trails' and s['alpha']==1 for s in r['samples']),r
    else:print(tag,'trail early/late',r['samples'][0],r['samples'][-1],flush=True)
    if 'effects' in tag:assert send('presentation')['nativeUnchanged']
    send('abandon')
if 'effects' in tag:
    r=send('presentation-texture-failures');assert r['disposed'] and r['reloaded'] and len(r['errors'])==3
    time.sleep(.2);assert 'NCombatRulesFtue.Start' not in (profile/'errors.log').read_text(errors='replace')
    print(tag,'tutorial-free paid crab facing, trails, identity/audio and asset failures passed',flush=True);raise SystemExit(0)
if not baseline:
    negative=send('presentation-texture-failures');assert negative['disposed'] and negative['reloaded'] and len(negative['errors'])==3
    trend=[]
    for cycle in range(3):
        for cls,traits in [('knight',['small','nocolor']),('mage',['large','colortrails']),('ranger',['youareblue','disposition'])]:
            start(cls,traits);v=send('visual');assert len(v)==1 and v[0]['classId']==cls and v[0]['visible']
            if cycle==0:
                for upgrade in [False,True]:
                    for key in [cls+'_strike',cls+'_guard']:
                        r=send('presentation-paid',card=key,upgrade=upgrade);assert r['accepted'] and r['pile']=='Discard' and r['energySpent']==r['cost']
                send('injure',amount=1);time.sleep(.5);assert 'hurt' in send('visual')[0]['events']
                assert send('presentation')['nativeUnchanged'];assert send('presentation-transition')['complete']
                send('save');send('resume');ready();assert send('visual')[0]['classId']==cls
            for room in ['RestSite','Shop','Monster']:
                send('enter',room=room)
                if room=='Monster':ready()
                else:
                    time.sleep(.4);v=send('room-visual');assert len(v)==1 and v[0]['classId']==cls and v[0]['pixelHeight']>85
                    if cycle==0:
                        if room=='RestSite':assert send('presentation-rest')['breathing']
                        else:assert send('presentation-shop')['bought']
                        shot(cls+'-'+room)
            time.sleep(.8);r=send('presentation-resources');assert r['sprites']==1 and r['audio']==0 and r['trails']==0,r
            trend.append(r);send('kill');time.sleep(.3);assert send('visual')[0]['pose']=='victory'
            send('abandon')
        print(tag,'lifecycle cycle',cycle,'resource samples',trend[-3:],flush=True)
    warm=trend[3:6];last=trend[6:9]
    for before,after in zip(warm,last):
        assert after['textureSlots']==before['textureSlots'] and after['nodes']<=before['nodes']+100 and after['resources']<=before['resources']+100
        assert after['memory']<=before['memory']+64*1024*1024
    for cls in ['knight','mage','ranger']:
        for room in ['startup','RestSite','Shop','combat']:
            send('start',**{'class':cls,'traits':[],'quick':False,'seed':'SPIRELEGACY-PRESENTATION-58'});time.sleep(.4)
            if room=='combat':send('enter',room='Monster');ready();send('kill',player=True)
            else:
                if room!='startup':send('enter',room=room)
                send('abandon')
            time.sleep(1);ending=send('ending-visual');assert len(ending)==1 and ending[0]['visible']
            sprite=ending[0]['sprites'][0];assert sprite['classId']==cls and sprite['pose']=='dead' and not sprite['placeholderVisible'],ending
            if cls=='mage':assert sprite['frame']==sprite['frames']-1 and len(sprite['deathFrames'])==sprite['frames']
            if room=='combat':shot(cls+'-death')
print(tag,'presentation audit phase passed',flush=True)
