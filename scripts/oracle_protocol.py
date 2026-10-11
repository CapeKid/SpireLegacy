"""Correlated local oracle transport; timed-out mutations must never be retried."""
import json,os,time,uuid
from pathlib import Path
if not __debug__:raise RuntimeError('Native checks require Python assertions; do not use -O or PYTHONOPTIMIZE.')

def call(profile,request,timeout=60,expected_hash=None):
    expected_hash=expected_hash or os.environ.get('HEIR_EXPECTED_DLL_SHA256')
    profile=Path(profile).resolve()
    if not profile.is_relative_to((Path(__file__).resolve().parents[1]/'private').resolve()):raise ValueError('Oracle profile must stay in the private workspace.')
    request=dict(request);request.setdefault('requestId',uuid.uuid4().hex)
    if uuid.UUID(request['requestId']).hex!=request['requestId']:raise ValueError('Invalid request ID.')
    profile.mkdir(parents=True,exist_ok=True)
    lock=profile/'oracle-client.lock';descriptor=os.open(lock,os.O_CREAT|os.O_EXCL|os.O_WRONLY);os.close(descriptor)
    pending=None
    try:
        target=profile/'request.json'
        if target.exists():raise RuntimeError('An oracle command is pending; refusing to overwrite it.')
        response=profile/'response.json'
        for attempt in range(5):
            try:prior=json.loads(response.read_text(encoding='utf-8'));break
            except (FileNotFoundError,json.JSONDecodeError):prior={};break
            except PermissionError:
                if attempt==4:raise
                time.sleep(.05)
        if prior.get('requestId')==request['requestId']:raise RuntimeError('Request ID was already answered; never replay a completed mutation.')
        pending=profile/(request['requestId']+'.pending')
        pending.write_text(json.dumps(request),encoding='utf-8');os.replace(pending,target)
        deadline=time.monotonic()+timeout
        while time.monotonic()<deadline:
            try:reply=json.loads(response.read_text(encoding='utf-8'))
            except (FileNotFoundError,json.JSONDecodeError,PermissionError):time.sleep(.05);continue
            if reply.get('requestId')!=request['requestId']:time.sleep(.05);continue
            if expected_hash and reply.get('metadata',{}).get('dllSha256')!=expected_hash:raise RuntimeError('Oracle loaded a stale or unexpected DLL.')
            return dict(request=request,response=reply)
        raise TimeoutError('Oracle response timed out; inspect logs/receipts. Never automatically retry this mutation.')
    finally:
        if pending is not None:pending.unlink(missing_ok=True)
        lock.unlink(missing_ok=True)

def assert_checks(actual,expected):
    if actual!=expected:raise AssertionError(f'Oracle invariants differ: expected {expected!r}, got {actual!r}')
