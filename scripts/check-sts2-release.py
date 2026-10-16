"""Validate all release formats using checks that stay enabled under Python -O."""
import argparse,json,pathlib
from release_contract import payloads,validate_archive
root=pathlib.Path(__file__).resolve().parents[1]
def main():
    parser=argparse.ArgumentParser();parser.add_argument('--expected-dll-sha256',required=True);parser.add_argument('--output-root',type=pathlib.Path,default=root);parser.add_argument('--baselib-root',type=pathlib.Path);args=parser.parse_args()
    v,formats=payloads(root,args.expected_dll_sha256,args.baselib_root);reports=[]
    for kind,files in formats.items():
        suffix='' if kind=='standalone' else '-'+kind
        reports.append(dict(kind=kind,**validate_archive(args.output_root/'dist'/f'SpireLegacy-StS2-{v}{suffix}.zip',files)))
    print(json.dumps(dict(version=v,standaloneAssets=True,packages=reports),indent=2))
if __name__=='__main__':
    try:main()
    except (ValueError,OSError) as error:raise SystemExit(str(error))
