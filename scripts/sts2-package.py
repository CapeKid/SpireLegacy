"""Stage exact validated StS2 files; publish nothing and never reuse dirty staging."""
import argparse,json,pathlib,tempfile,shutil
from release_contract import payloads,write_archive,digest,ITEM_ID,APP_ID,ReleaseError
from release_transaction import checked,remove,recover,promote
repo=pathlib.Path(__file__).resolve().parents[1]

def build(root,output,expected_hash,baselib=None,hook=None):
    root=pathlib.Path(root);output=pathlib.Path(output).resolve();output.mkdir(parents=True,exist_ok=True)
    checked(output,output/'build/sts2');checked(output,output/'dist')
    recover(output)
    v,formats=payloads(root,expected_hash,baselib)
    parent=output/'build/sts2';parent.mkdir(parents=True,exist_ok=True)
    transaction=pathlib.Path(tempfile.mkdtemp(prefix='.package-txn-',dir=parent));checked(output,transaction)
    try:
        trees=[];archives=[];reports=[]
        for kind,files in formats.items():
            tree=transaction/kind;tree.mkdir()
            for name,data in files.items():
                target=tree/name;checked(output,target);target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(data)
            suffix='' if kind=='standalone' else '-'+kind
            archive=transaction/f'SpireLegacy-StS2-{v}{suffix}.zip';write_archive(archive,files)
            trees.append(tree);archives.append(archive);reports.append(dict(kind=kind,file=archive.name,size=archive.stat().st_size,sha256=digest(archive.read_bytes()),files=len(files),dllSha256=expected_hash))
        marker=output/'build/sts2/workshop/mod_id.txt'
        if marker.exists():
            checked(output,marker);value=marker.read_bytes()
            if value.decode('utf-8-sig').strip()!=str(ITEM_ID):raise ReleaseError('Uploader marker does not match the existing StS2 item')
            (trees[1]/'mod_id.txt').write_bytes(value)
        inspection=transaction/'inspection.json'
        inspection.write_text(json.dumps(dict(version=v,app=APP_ID,item=ITEM_ID,packages=reports,mod_sha256=expected_hash,standalone_assets=True),indent=2)+'\n',encoding='utf-8')
        promote(output,transaction,v,trees+archives+[inspection],hook)
        return json.loads((output/'build/sts2/package-inspection.json').read_text())
    except Exception:
        if not (output/'build/sts2/package-transaction.json').exists() and transaction.exists():remove(output,transaction)
        raise

def main():
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--expected-dll-sha256',required=True);parser.add_argument('--output-root',type=pathlib.Path,default=repo);parser.add_argument('--baselib-root',type=pathlib.Path);args=parser.parse_args()
    print(json.dumps(build(repo,args.output_root,args.expected_dll_sha256,args.baselib_root),indent=2))
if __name__=='__main__':
    try:main()
    except (ValueError,OSError) as error:raise SystemExit(str(error))
