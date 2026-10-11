"""Adversarial transport/lab regressions with disposable fixture peers, no game launch."""
import copy,json,os,subprocess,sys,threading,time,uuid
from pathlib import Path
from unittest.mock import patch
from oracle_protocol import call,assert_checks
root=Path(__file__).resolve().parents[1];base=(root/'private'/('harness-fixture-'+uuid.uuid4().hex)).resolve();base.mkdir(parents=True);passed=[]

def check(name,body):
    body();passed.append(name)

def rejected(body,kind):
    try:body()
    except kind:return
    raise AssertionError('Expected rejection')

def peer(profile,writer):
    def run():
        deadline=time.monotonic()+3
        while not (profile/'request.json').exists():
            if time.monotonic()>deadline:raise TimeoutError('fixture delivery')
            time.sleep(.005)
        req=json.loads((profile/'request.json').read_text());(profile/'request.json').unlink();writer(req)
    thread=threading.Thread(target=run);thread.start();return thread

p=base/'stale';p.mkdir();(p/'response.json').write_text(json.dumps(dict(requestId='old',ok=True,result='wrong')))
def reply(req):
    (p/'response.json').write_text('{')
    time.sleep(.1)
    (p/'response.json').write_text(json.dumps(dict(requestId=req['requestId'],ok=True,result='fresh',metadata={'dllSha256':'current'})))
t=peer(p,reply);check('stale/partial response cannot pass',lambda:assert_checks(call(p,{'command':'probe'},expected_hash='current')['response']['result'],'fresh'));t.join()
answered=json.loads((p/'response.json').read_text())['requestId']
check('answered ID cannot accept old successful response',lambda:rejected(lambda:call(p,{'command':'probe','requestId':answered}),RuntimeError))
p=base/'stale-dll';p.mkdir();t=peer(p,lambda req:(p/'response.json').write_text(json.dumps(dict(requestId=req['requestId'],ok=True,metadata={'dllSha256':'old'}))))
check('stale DLL rejected',lambda:rejected(lambda:call(p,{'command':'probe'},expected_hash='current'),RuntimeError));t.join()
p=base/'busy';p.mkdir();(p/'oracle-client.lock').write_text('owned')
check('concurrent client cannot overwrite',lambda:rejected(lambda:call(p,{'command':'probe'}),FileExistsError))
p=base/'pending';p.mkdir();(p/'request.json').write_text('existing')
check('pending mutation cannot be overwritten',lambda:rejected(lambda:call(p,{'command':'probe'}),RuntimeError));assert (p/'request.json').read_text()=='existing'
check('unsafe request ID cannot choose output path',lambda:rejected(lambda:call(base/'unsafe',{'command':'probe','requestId':'../../outside'}),ValueError))
check('outside profile rejected',lambda:rejected(lambda:call(root,{'command':'probe'}),ValueError))
check('transient response lock recovers',lambda:None)
p=base/'locked';p.mkdir();t=peer(p,lambda req:(p/'response.json').write_text(json.dumps(dict(requestId=req['requestId'],ok=True,result=1))))
original=Path.read_text;attempts=[0]
def locked(path,*args,**kwargs):
    if path==p/'response.json' and attempts[0]<2:attempts[0]+=1;raise PermissionError('fixture lock')
    return original(path,*args,**kwargs)
with patch.object(Path,'read_text',locked):assert call(p,{'command':'probe'})['response']['result']==1
t.join();assert attempts[0]==2
check('timeout does not retry mutation',lambda:rejected(lambda:call(base/'timeout',{'command':'mutate'},timeout=.15),TimeoutError))
assert len(list((base/'timeout').glob('request.json')))==1

expected={'owner':2,'payouts':1,'effect':7,'completed':True,'tableNodes':0}
for name,key,value in [('wrong owner','owner',1),('duplicate payout','payouts',2),('skipped effect','effect',0),('unawaited task','completed',False),('missing node','tableNodes',None)]:
    mutant=copy.deepcopy(expected);mutant[key]=value
    check('planted '+name+' detected',lambda mutant=mutant:rejected(lambda:assert_checks(mutant,expected),AssertionError))

game=base/'fake-game';game.mkdir();(game/'owned.bin').write_bytes(b'owned fixture');(game/'release_info.json').write_text('{}')
workshop=base/'Steam/steamapps/workshop/content/2868840';workshop.mkdir(parents=True)
(workshop/'other.json').write_text(json.dumps(dict(id='UnrelatedFixture',has_dll=True)))
lab=base/'independent-lab';env=dict(os.environ,STS2_DIR=str(game),STS2_WORKSHOP_DIR=str(workshop),APPDATA=str(base/'userdata'))
command=[sys.executable,str(root/'scripts/sts2-lab.py'),'--configuration','Release','--lab-directory',str(lab),'--user-directory','SpireLegacy2PortLabFixture']
subprocess.run(command,env=env,check=True,capture_output=True)
assert not os.path.samefile(game/'owned.bin',lab/'owned.bin');(lab/'owned.bin').write_bytes(b'changed lab');assert (game/'owned.bin').read_bytes()==b'owned fixture'
check('lab copy mutation cannot change source',lambda:None)
settings=json.loads((base/'userdata/SpireLegacy2PortLabFixture/default/1/settings.save').read_text())
assert dict(id='UnrelatedFixture',source=2,is_enabled=False) in settings['mod_settings']['mod_list']
check('snapshot lab disables actual subscribed Workshop mods',lambda:None)
assert subprocess.run(command,env=env,capture_output=True).returncode!=0
check('existing lab refused',lambda:None)
command[-1]='SpireLegacy2PortLab"\ninvalid'
assert subprocess.run(command,env=env,capture_output=True).returncode!=0
check('config-injection user directory rejected',lambda:None)
for file in (root/'scripts').glob('sts2-*.py'):
    if file.name=='sts2-oracle.py':continue
    text=file.read_text(encoding='utf-8-sig')
    if 'def send(' in text:assert 'record=call(' in text and ("metadata=reply['metadata']" in text or 'evidence.append(record)' in text),file
check('all native scenario clients use correlated evidence',lambda:None)
assert subprocess.run([sys.executable,'-O','-c','import oracle_protocol'],cwd=root/'scripts',capture_output=True).returncode!=0
check('disabled assertions rejected',lambda:None)
output=root/'private/sts2-evidence/harness-fixtures.json';output.write_text(json.dumps(dict(passed=passed,count=len(passed),fixture=str(base)),indent=2),encoding='utf-8')
print(json.dumps(dict(result='passed',checks=len(passed),evidence=str(output))))
