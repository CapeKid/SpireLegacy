"""Send a command to the explicitly opted-in, isolated StS2 oracle."""
import argparse, json, os, pathlib, time
repo = pathlib.Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser()
parser.add_argument('request', help='JSON request, or @path to a request file')
parser.add_argument('--timeout',type=int,default=60)
parser.add_argument('--name',default='latest')
args = parser.parse_args()
request = json.loads(pathlib.Path(args.request[1:]).read_text()) if args.request.startswith('@') else json.loads(args.request)
profile = repo / 'private/sts2-test-profile'
request_path = profile / 'request.json'; response_path = profile / 'response.json'
if request_path.exists(): raise SystemExit('An oracle request is already pending; refusing to overwrite it.')
if response_path.exists(): response_path.unlink()
pending_path=profile/'request.pending'
pending_path.write_text(json.dumps(request),encoding='utf-8')
os.replace(pending_path,request_path)
deadline = time.monotonic() + args.timeout
while not response_path.exists():
    if time.monotonic() > deadline: raise SystemExit('Oracle request timed out. Inspect the private game log before retrying.')
    time.sleep(.1)
result = json.loads(response_path.read_text(encoding='utf-8'))
evidence = repo / 'private/sts2-evidence'; evidence.mkdir(parents=True,exist_ok=True)
(evidence / (args.name + '.json')).write_text(json.dumps(dict(request=request,response=result),indent=2),encoding='utf-8')
if not result.get('ok'): print(json.dumps(result,indent=2)); raise SystemExit(1)
value = result['result']
if isinstance(value,dict) and 'checks' in value: value = dict(count=value['count'],evidence=str(evidence / (args.name + '.json')))
print(json.dumps(value,ensure_ascii=True))
