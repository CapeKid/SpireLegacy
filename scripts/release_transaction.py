"""Promote a validated release, recovering interruptions before the next build."""
import json
import os
import re
import shutil
from pathlib import Path
from release_contract import require, version, digest, ReleaseError
from design_schema import read_json

def checked(base,path):
    path=Path(path)
    link=path.is_symlink() or getattr(path,'is_junction',lambda:False)()
    require(path.resolve().is_relative_to(base.resolve()) and not link,'Output escapes release root or uses a directory link: '+str(path))
    return path

def tree_hash(path):
    if path.is_file():return digest(path.read_bytes())
    records=[]
    for item in sorted(path.rglob('*')):
        require(not item.is_symlink(),'Symlink in staged output')
        if item.is_file():records.append(item.relative_to(path).as_posix()+':'+digest(item.read_bytes()))
    return digest('\n'.join(records).encode())

def remove(base,path):
    checked(base,path)
    if path.is_dir():
        # Check every descendant before recursive deletion; never follow a directory link.
        for item in path.rglob('*'):checked(base,item)
        shutil.rmtree(path)
    elif path.exists():path.unlink()

def targets(v):
    version(v)
    return [f'build/sts2/release/{v}','build/sts2/workshop',f'build/sts2/melty/{v}']+[f'dist/SpireLegacy-StS2-{v}{suffix}.zip' for suffix in ['', '-workshop','-melty']]+['build/sts2/package-inspection.json']

def recover(base):
    journal=base/'build/sts2/package-transaction.json'
    if not journal.exists():return False
    record=read_json(checked(base,journal));require(isinstance(record,dict),'Invalid package recovery record');v=version(record.get('version'))
    require(record.get('phase') in {'prepared','committed'},'Invalid package recovery phase')
    relative=record.get('transaction','')
    require(isinstance(relative,str) and re.fullmatch(r'build/sts2/\.package-txn-[a-zA-Z0-9_-]+',relative),'Invalid recovery transaction path')
    transaction=checked(base,base/relative)
    require(transaction.is_dir(),'Recovery transaction directory is missing; outputs were left untouched')
    ops=record.get('operations',[])
    require(isinstance(ops,list) and all(isinstance(op,dict) for op in ops),'Invalid recovery operations')
    require([op.get('target') for op in ops]==targets(v),'Invalid recovery target set')
    # Validate the complete plan before touching anything, including modified new files.
    for index,op in enumerate(ops):
        target=checked(base,base/op['target']);backup=checked(base,transaction/'backup'/str(index));new=checked(base,transaction/'new'/str(index))
        require(type(op.get('hadOld')) is bool and isinstance(op.get('newHash'),str) and re.fullmatch('[a-f0-9]{64}',op['newHash']) is not None,'Invalid recovery operation')
        changed=backup.exists() or (not op['hadOld'] and not new.exists())
        if record['phase']=='prepared' and changed and target.exists():
            require(tree_hash(target)==op['newHash'],'Interrupted output was modified; move it aside before retrying: '+str(target))
    if record['phase']=='prepared':
        for index,op in reversed(list(enumerate(ops))):
            target=base/op['target'];backup=transaction/'backup'/str(index);new=transaction/'new'/str(index)
            if backup.exists():
                if target.exists():remove(base,target)
                target.parent.mkdir(parents=True,exist_ok=True);os.replace(backup,target)
            elif not op['hadOld'] and not new.exists() and target.exists():remove(base,target)
    if transaction.exists():remove(base,transaction)
    journal.unlink()
    return True

def promote(base,transaction,v,prepared,hook=None):
    require(len(prepared)==len(targets(v)),'Incomplete promotion plan')
    journal=checked(base,base/'build/sts2/package-transaction.json')
    require(not journal.exists(),'Recover the previous package transaction first')
    (transaction/'new').mkdir();(transaction/'backup').mkdir()
    ops=[]
    for index,(relative,source) in enumerate(zip(targets(v),prepared)):
        target=checked(base,base/relative);new=transaction/'new'/str(index)
        if target.is_dir():
            for descendant in target.rglob('*'):checked(base,descendant)
        os.replace(source,new)
        ops.append(dict(target=relative,hadOld=target.exists(),newHash=tree_hash(new)))
    record=dict(version=v,phase='prepared',transaction=transaction.relative_to(base).as_posix(),operations=ops)
    def save():
        temporary=journal.with_suffix('.tmp');temporary.write_text(json.dumps(record,indent=2)+'\n',encoding='utf-8');os.replace(temporary,journal)
    save()
    try:
        for index,op in enumerate(ops):
            target=base/op['target'];target.parent.mkdir(parents=True,exist_ok=True)
            if op['hadOld']:os.replace(target,transaction/'backup'/str(index))
            if hook:hook(index,'before-new')
            os.replace(transaction/'new'/str(index),target)
            if hook:hook(index,'after-new')
        record['phase']='committed';save()
    except Exception:
        recover(base);raise
    # A process interruption leaves the journal/backups for the next invocation.
    recover(base)
