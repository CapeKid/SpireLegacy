"""Issue #61: isolated mutation, clean-output and authoring reproducibility checks."""
import copy
import hashlib
import json
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path
from design_schema import DesignError, load_sheets, validate_assets, check

root=Path(__file__).resolve().parents[1]
report={'seed':'SPIRELEGACY-DESIGN-61','mutations':[], 'reproduction':[]}
def digest(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def run(lab, name, *args, ok=True):
    result=subprocess.run([sys.executable,str(lab/'scripts'/name),*args],cwd=lab.parent,capture_output=True,text=True,encoding='utf-8',errors='replace')
    if ok and result.returncode:raise AssertionError((name,result.stdout,result.stderr))
    return result

with tempfile.TemporaryDirectory(prefix='design61-',dir=root/'private') as directory:
    lab=Path(directory)/'design with spaces é';lab.mkdir()
    assert lab.resolve().is_relative_to((root/'private').resolve())
    for folder in ['scripts','sheets','src/main/resources','sts2/Generated','art/originals']:
        shutil.copytree(root/folder,lab/folder)
    shutil.copy2(root/'sts2/SpireLegacy.csproj',lab/'sts2/SpireLegacy.csproj')
    (lab/'media').mkdir()
    tables=load_sheets(lab);validate_assets(lab,tables)
    contracts=json.loads((lab/'scripts/design-schema.json').read_text())
    def mutate(table,edit,fragment,stage='schema'):
        rows=copy.deepcopy(tables[table]);edit(rows)
        if stage=='row':
            errors=[];check(rows[0],contracts[table],f'{table}.{tables[table][0]["id"]}',errors)
            assert errors and any(fragment in message for message in errors),(fragment,errors)
            report['mutations'].append({'table':table,'expectedDiagnostic':fragment,'rejected':True,'layer':'typed-row'})
            return
        path=lab/'sheets'/f'{table}.json';original=path.read_bytes()
        path.write_text(json.dumps(rows,ensure_ascii=False),encoding='utf-8')
        try:
            if stage=='schema':
                try:load_sheets(lab)
                except DesignError as error:message=str(error)
                else:raise AssertionError('Accepted invalid '+fragment)
            elif stage=='assets':
                try:validate_assets(lab,load_sheets(lab))
                except DesignError as error:message=str(error)
                else:raise AssertionError('Accepted invalid '+fragment)
            else:
                result=run(lab,'preflight.py',ok=False);assert result.returncode and 'Traceback' not in result.stderr,(result.stdout,result.stderr);message=result.stdout
            assert fragment in message,(fragment,message)
            report['mutations'].append({'table':table,'expectedDiagnostic':fragment,'rejected':True,'layer':stage})
        finally:path.write_bytes(original)
    # Every required top-level column is checked independently; empty/malformed rows
    # must fail before downstream indexing, not merely produce a Python traceback.
    for table,spec in contracts.items():
        for field in spec['required']:
            mutate(table,lambda rows,f=field:rows[0].pop(f),f'.{field}: missing required','row')
        mutate(table,lambda rows:rows.append(copy.deepcopy(rows[0])),'duplicate ID')
        mutate(table,lambda rows:rows.__setitem__(0,None),'expected dict')
        for field,rule in spec['fields'].items():
            if rule['types']==['int']:
                mutate(table,lambda rows,f=field:rows[0].update({f:True}),f'.{field}: expected int','row')
    print('schema mutations passed',len(report['mutations']),flush=True)
    for value in [-1,2147483648,True,1.5,'5',None]:
        mutate('cards',lambda rows,v=value:rows[0].update(damage=v),'cards.knight_strike.damage')
    for field,value in [('id','../../outside'),('type','NOT_A_CARD'),('rarity','MYTHIC'),('upgradeDraw',-2),('exhaust',1),('surprise',0)]:
        mutate('cards',lambda rows,f=field,v=value:rows[0].update({f:v}),'cards.knight_strike.'+field if field!='id' else 'invalid identifier')
    for value in ['../outside.png','/tmp/out.png','C:/out.png','heir\\out.png','heir/./out.png']:
        mutate('card_art',lambda rows,v=value:rows[0].update(path=v),'unsafe resource path')
    for table,field in [('assets','output'),('assets','sourceFile')]:
        mutate(table,lambda rows,f=field:rows[0].update({f:'../outside'}),'unsafe resource path')
    for table,field in [('cards','classId'),('cards','asset'),('cards','art'),('classes','signature'),('ancient_cards','cardId'),('pool_expansion','cardId'),('ancient_dialogue','classId')]:
        mutate(table,lambda rows,f=field:rows[0].update({f:'unknown'}),'unresolved','preflight')
    mutate('decks',lambda rows:rows[0]['cards'].__setitem__(0,'mage_strike'),'foreign starter','preflight')
    mutate('cards',lambda rows:rows[0].update(power='unknown'),'Unsupported class power','preflight')
    mutate('cards',lambda rows:rows[0].update(special='unknown'),'Unsupported card mechanic','preflight')
    mutate('traits',lambda rows:rows[0].update(effect='unknown'),'Unimplemented trait effect','preflight')
    mutate('manor',lambda rows:rows[0].update(requires=rows[0]['id']),'prerequisite cycle','preflight')
    mutate('balance_changes',lambda rows:rows[0]['after'].update(damage=9),'stale current stat','preflight')
    mutate('card_art',lambda rows:rows[0].update(sha256='0'*64),'artwork hash mismatch','assets')
    mutate('character_art',lambda rows:rows[0]['regions'][0].__setitem__(2,9999),'outside atlas','assets')
    mutate('character_art',lambda rows:rows[0]['feet'].pop(),'alignment mismatch','assets')
    mutate('relic_art',lambda rows:rows[0].update(license='unknown'),'unsupported value')
    for invalid,fragment in [(b'not xml','invalid/missing asset'),(b'<svg xmlns="http://www.w3.org/2000/svg"><use href="https://example.invalid/x"/></svg>','external/active SVG'),(b'<svg xmlns="http://www.w3.org/2000/svg"><path fill="url(https://example.invalid/x)"/></svg>','external/active SVG')]:
        row=tables['icon_art'][0];path=lab/'src/main/resources'/row['path'];original=path.read_bytes();path.write_bytes(invalid)
        try:mutate('icon_art',lambda rows:rows[0].update(sha256=hashlib.sha256(invalid).hexdigest()),fragment,'assets')
        finally:path.write_bytes(original)
    for table in ['classes','traits','manor','heir_relics','heir_potions','family_chronicle']:
        mutate(table,lambda rows:rows[0].update(id='renamed_saved_identity'),'removed saved model IDs')
    print('reference and asset mutations passed',flush=True)
    # JSON decoding failures are contextual and duplicate properties cannot hide IDs.
    path=lab/'sheets/cards.json';original=path.read_bytes()
    for invalid,fragment in [('not json','cards.json'),('[{"id":"a","id":"b"}]','duplicate JSON property'),('[NaN]','invalid number'),('{}','nonempty row array')]:
        path.write_text(invalid)
        try:
            try:load_sheets(lab)
            except DesignError as error:assert fragment in str(error)
            else:raise AssertionError('Accepted malformed JSON')
            report['mutations'].append({'table':'cards','expectedDiagnostic':fragment,'rejected':True})
        finally:path.write_bytes(original)
    # Valid BOM and Unicode names round-trip without locale or working-dir dependence.
    rows=copy.deepcopy(tables['cards']);rows[0]['name']='Épée 家族'
    path.write_text(json.dumps(rows,ensure_ascii=False),encoding='utf-8-sig')
    assert load_sheets(lab)['cards'][0]['name']=='Épée 家族';path.write_bytes(original)
    # Failure must not overwrite either model file.
    before={p.name:p.read_bytes() for p in (lab/'sts2/Generated').glob('*.cs')}
    path=lab/'sheets/card_pools.json';original=path.read_bytes();rows=copy.deepcopy(tables['card_pools']);rows.pop();path.write_text(json.dumps(rows))
    failed=run(lab,'sts2-generate.py',ok=False)
    assert failed.returncode and {p.name:p.read_bytes() for p in (lab/'sts2/Generated').glob('*.cs')}==before
    path.write_bytes(original)
    gate=subprocess.run(['dotnet','msbuild',str(lab/'sts2/SpireLegacy.csproj'),'-t:ValidateDesign','-p:DesignPython='+sys.executable],capture_output=True,text=True)
    assert gate.returncode==0,(gate.stdout,gate.stderr)
    path=lab/'sheets/cards.json';original=path.read_bytes();rows=copy.deepcopy(tables['cards']);rows[0]['damage']=True;path.write_text(json.dumps(rows))
    gate=subprocess.run(['dotnet','msbuild',str(lab/'sts2/SpireLegacy.csproj'),'-t:ValidateDesign','-p:DesignPython='+sys.executable],capture_output=True,text=True)
    assert gate.returncode and 'cards.knight_strike.damage' in gate.stdout,(gate.stdout,gate.stderr);path.write_bytes(original)
    report['msbuildGateRejectsInvalidDesign']=True
    for _ in range(2):run(lab,'sts2-generate.py')
    generated={p.name:digest(p) for p in (lab/'sts2/Generated').glob('*.cs')}
    run(lab,'sts2-generate.py');assert generated=={p.name:digest(p) for p in (lab/'sts2/Generated').glob('*.cs')}
    run(lab,'sts2-generate.py','--check')
    path=lab/'sts2/Generated/Cards.cs';path.write_text(path.read_text()+'// stale\n');assert run(lab,'sts2-generate.py','--check',ok=False).returncode
    run(lab,'sts2-generate.py')
    path=lab/'sts2/Generated/Orphan.cs';path.write_text('orphan');assert run(lab,'sts2-generate.py','--check',ok=False).returncode;path.unlink()
    report['generated']=dict(cards=360,powers=41,hashes=generated,invalidInputPreservesOutputs=True,staleRejected=True,orphanRejected=True)
    # Even deliberately regenerated output cannot silently drop the saved registry.
    path=lab/'scripts/saved-model-ids.json';saved=path.read_bytes();registry=json.loads(saved);registry['cards'].append('retired_saved_identity');path.write_text(json.dumps(registry))
    failed=run(lab,'sts2-generate.py',ok=False);assert failed.returncode and 'removed saved model IDs' in failed.stdout+failed.stderr;path.write_bytes(saved)
    print('stable model generation passed',flush=True)
    # All deterministic authors run twice. Art manifests must describe exact files;
    # retained PNG originals are inputs, not something an AI generator can reproduce.
    authors=['author-heir-icons.py','author-heir-relics.py','author-heir-potions.py','author-heir-presentation.py','author-heir-ancient-dialogue.py','prepare_card_art.py','prepare_ui_art.py','sts2-pool-expansion.py']
    def snapshots():
        return {p.relative_to(lab).as_posix():digest(p) for folder in ['sheets','src/main/resources','sts2/Generated'] for p in (lab/folder).rglob('*') if p.is_file()}
    for name in authors:
        prior=snapshots();run(lab,name);first=snapshots();run(lab,name);assert first==snapshots(),name
        changed=sorted(key for key in prior.keys()|first.keys() if prior.get(key)!=first.get(key))
        report['reproduction'].append(dict(script=name,byteIdenticalTwice=True,changedFromInput=changed))
        print('deterministic author passed',name,flush=True)
    run(lab,'preflight.py');validate_assets(lab,load_sheets(lab))
    for value in ['../../escaped','unknown']:
        before=snapshots();assert run(lab,'export_expansion_art.py',value,str(lab/'art/originals/knight_strike.png'),ok=False).returncode;assert before==snapshots()
    for name,table in [('prepare_card_art.py','card_art'),('prepare_ui_art.py','ui_art')]:
        selected=next(row for row in tables[table] if row.get('exportMode','fit')=='fit')
        original=lab/'art/originals'/(selected['id']+'.png');data=original.read_bytes();original.write_bytes(b'corrupt input')
        before=snapshots();result=run(lab,name,ok=False);assert result.returncode and before==snapshots(),(name,result.stderr)
        original.write_bytes(data)
    path=lab/'sheets/traits.json';original=path.read_bytes();rows=json.loads(original);rows[0]['name']='Épée & <famille>';path.write_text(json.dumps(rows,ensure_ascii=False),encoding='utf-8-sig')
    run(lab,'author-heir-icons.py');validate_assets(lab,load_sheets(lab));path.write_bytes(original)
    report['bomUnicodeWorkingDirectory']=True
    report['assetFiles']=len(list((lab/'src/main/resources/heir').rglob('*')))
report['mutationCount']=len(report['mutations'])
(root/'private/design61-checks.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps({'result':'passed','mutationCount':report['mutationCount'],'authors':len(report['reproduction']),'generated':report['generated']}))
