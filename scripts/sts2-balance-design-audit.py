"""Literal-budget sweep for #60; engine/conditional effects require native probes."""
import hashlib,json,subprocess
from collections import Counter
from pathlib import Path
from balance_policy import CARD_OVERRIDES
root=Path(__file__).resolve().parents[1]
def read(name):return json.loads((root/'sheets'/f'{name}.json').read_text())
cards=read('cards');active={r['cardId'] for r in read('card_pools')+read('branch_cards')}
current={r['id']:r for r in cards};audit={r['id']:r for r in read('balance_changes')}
assert set(current)==set(audit) and len(cards)==360
for key,row in current.items():
    assert all(row[k]==v for k,v in audit[key]['after'].items()),key
    assert all(row[k]==v for k,v in CARD_OVERRIDES.get(key,{}).items()),key
    assert row['hits']>=1 and (row['hits']==1 or row['upgradeDamage']<=1),key
risky=[];free_draw=[]
for row in cards:
    for up in [False,True]:
        cost=row['cost']-int(up and row['upgradeCost']);assert cost>=0
        increment=row['upgradeDraw'] if row['upgradeDraw']>=0 else row['upgradeMagic']
        draw=row['draw']+(increment if up and row['draw'] else 0)
        discard=row['discard']+(row['upgradeDiscard'] if up else 0)
        if cost==0 and draw>0 and not row['exhaust']:
            case=dict(id=row['id'],active=row['id'] in active,upgraded=up,draw=draw,discard=discard,hpCost=row['hpLoss'])
            free_draw.append(case)
            assert draw<=discard or row['hpLoss']>0,case
        if row['special'] in ('retrieve','exhume','refill_hand','discount_hand','free_hand','discard_draw','discard_energy') or row['power'] in ('dark_embrace','power_draw','establishment','thousand_cuts','discard_block','discard_energy','evolve'):
            risky.append(dict(id=row['id'],active=row['id'] in active,upgraded=up,cost=cost,exhaust=row['exhaust'],power=row['power'],special=row['special']))
manor=read('manor');costs={r['id']:sum(r['baseCost']+i*r['costStep'] for i in range(r['maxLevel'])) for r in manor}
assert sum(costs.values())==2620
proof=dict(issue=60,sourceSha=subprocess.check_output(['git','rev-parse','HEAD'],cwd=root,text=True).strip(),cards=360,forms=720,activeRegistered=len(active),freeReusableLiteralDraw=free_draw,riskForms=risky,manorTotal=sum(costs.values()),manorCosts=costs,sheetHashes={n:hashlib.sha256((root/'sheets'/f'{n}.json').read_bytes()).hexdigest() for n in ['cards','card_builds','balance_changes','class_mechanics','traits','manor','systems']},limits=['Literal row checks do not prove engine loops impossible. Discard triggers, card copies, native enablers and discounted cards require native tests.','Active membership is the union across classes and both branches; this count is not cards available to one class.','No scalar damage-per-Energy score is used to flatten conditional, multi-hit, Poison, retained or delayed payoffs.'])
path=root/'docs/adversarial-balance-design.json'
path.write_text(json.dumps({k:v for k,v in proof.items() if k!='riskForms'},indent=2)[:-2]+',\n  "riskForms": [\n'+',\n'.join('    '+json.dumps(r) for r in risky)+'\n  ]\n}\n')
print(json.dumps(dict(cards=360,forms=720,riskForms=len(risky),freeReusableDraw=len(free_draw),manorTotal=sum(costs.values()))))
