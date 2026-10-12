"""Compare recorded decisions before one reward choice; never compare timestamps/wait counts."""
import hashlib,json,sys
from pathlib import Path
root=Path(__file__).resolve().parents[1]
left,right,floor=sys.argv[1:4];floor=int(floor)
assert floor>0 and all(t and all(c.isalnum() or c in '-_' for c in t) for t in [left,right])
def prefix(tag):
    journal=json.loads((root/'private/sts2-evidence'/f'balance-campaign-{tag}.json').read_text())
    records=[];hashes=set();games=set()
    for entry in journal['evidence']:
        reply=entry['response'];assert reply['ok'],'Failed commands cannot establish a matching prefix.'
        hashes.add(reply['metadata']['dllSha256']);games.add(reply['metadata']['game'])
        result=reply['result'];action=result.get('action')
        if action=='card-reward' and result['state']['floor']==floor:
            assert len(hashes)==len(games)==1
            return journal,records,result,next(iter(hashes)),next(iter(games))
        if action and action!='wait':records.append(result)
    raise AssertionError(f'{tag}: requested reward not reached yet')
a,ap,ar,ah,ag=prefix(left);b,bp,br,bh,bg=prefix(right)
assert ah==bh and ag==bg,'Pair must use the same immutable DLL and branch.'
assert all(a[k]==b[k] for k in ['cls','tier','policy','rewardPolicy','seed','ascension','traits']),'Pair inputs differ.'
same=ap==bp and ar['detail']['offers']==br['detail']['offers']
def digest(records):return hashlib.sha256(json.dumps(records,sort_keys=True,separators=(',',':')).encode()).hexdigest()
print(json.dumps(dict(left=left,right=right,floor=floor,game=ag,dllSha256=ah,
    identicalRecordedPrefix=same,leftDecisions=len(ap),rightDecisions=len(bp),
    leftPrefixSha256=digest(ap),rightPrefixSha256=digest(bp),
    offers=ar['detail']['offers'],leftPick=ar['detail']['picked'],rightPick=br['detail']['picked']),indent=2))
assert same,'Recorded decisions diverged before the reward; do not call this a matched intervention.'
