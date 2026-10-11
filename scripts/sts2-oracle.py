"""Send a correlated command to the opted-in, isolated StS2 oracle."""
import argparse,json,pathlib
from oracle_protocol import call
repo=pathlib.Path(__file__).resolve().parents[1]
parser=argparse.ArgumentParser();parser.add_argument('request');parser.add_argument('--timeout',type=int,default=60)
parser.add_argument('--name',default='latest');parser.add_argument('--profile-directory',default='private/sts2-test-profile');parser.add_argument('--expected-dll-sha256')
args=parser.parse_args()
if not args.name or pathlib.Path(args.name).name!=args.name:raise SystemExit('Evidence name must be a filename, not a path.')
request=json.loads(pathlib.Path(args.request[1:]).read_text(encoding='utf-8-sig')) if args.request.startswith('@') else json.loads(args.request)
profile=(repo/args.profile_directory).resolve()
if not profile.is_relative_to((repo/'private').resolve()):raise SystemExit('Oracle profile must stay inside the private workspace.')
record=call(profile,request,args.timeout,args.expected_dll_sha256);result=record['response']
evidence=repo/'private/sts2-evidence';evidence.mkdir(parents=True,exist_ok=True)
(evidence/(args.name+'.json')).write_text(json.dumps(record,indent=2),encoding='utf-8')
if not result.get('ok'):print(json.dumps(result,indent=2));raise SystemExit(1)
value=result['result']
if isinstance(value,dict) and 'checks' in value:value=dict(count=value['count'],evidence=str(evidence/(args.name+'.json')))
print(json.dumps(value,ensure_ascii=True))
