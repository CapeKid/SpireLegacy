"""Disposable native Manor UI navigation and transaction regressions (#57)."""
import hashlib,json,sys,time,shutil
from pathlib import Path
from oracle_protocol import call
root=Path(__file__).resolve().parents[1];profile=Path(sys.argv[1]);tag=sys.argv[2]
expected=hashlib.sha256((root/'sts2/bin/Release/net9.0/SpireLegacy.dll').read_bytes()).hexdigest()
records=[];output=root/'private/sts2-evidence'/('manor-'+tag+'.json');assert not output.exists()
def send(command,**args):
    r=call(profile,dict(command=command,**args),timeout=90,expected_hash=expected)
    records.append(r);output.write_text(json.dumps(records,indent=2));assert r['response']['ok'],r
    return r['response']['result']
def activate(text):
    state=send('manor-audit');controls=[c for c in state['controls'] if c['focusable'] and not c['disabled']]
    target=next(i for i,c in enumerate(controls) if c['text']==text)
    current=next((i for i,c in enumerate(controls) if c['name']==state['focus']),-1)
    for _ in range(abs(target-current)):
        send('input',action='ui_down' if target>current else 'ui_up')
    assert send('manor-audit')['focusText']==text
    send('input',action='ui_select')
send('chronicle-fixture',unlocked=True)
send('select',previewAscension=10);send('input',action='controller_face_button_north')
state=send('manor-state');assert state['paused'] and state['focusableLabels']==0
activate(next(b['text'] for b in state['buttons'] if b['text'].startswith('Family Chronicle')))
story=next(b['text'] for b in send('manor-state')['buttons'] if b['text'].endswith('The oath outlives the armor') and not b['disabled'])
activate(story);activate('Use family motto: Unbroken Oath')
assert send('lifecycle-state')['profile']['chronicle']['motto']=='Unbroken Oath'
send('input',action='ui_cancel');assert send('manor-state')['focusText']=='Read: The oath outlives the armor'
send('input',action='ui_cancel');assert send('manor-state')['focusText'].startswith('Family Chronicle')
activate('Name your family');time.sleep(.3)
assert send('manor-audit')['focusType']=='LineEdit'
activate('Clear text');activate('A');activate('B');activate('Space');activate('c')
assert next(c['text'] for c in send('manor-audit')['controls'] if c['type']=='LineEdit')=='AB c'
activate('Save');assert send('lifecycle-state')['profile']['family']=='AB c'
activate('Name your family');send('edit',value='ABCDEFGHIJKLMNOPQRSTUVWX');activate('Save')
before=send('lifecycle-state')['profile'];assert before['family']=='ABCDEFGHIJKLMNOPQRSTUVWX'
activate('Change banner');assert send('lifecycle-state')['profile']['banner']==(before['banner']+1)%4
activate('PLAYTEST: Set crowns');send('edit',value='-1');activate('Save')
assert send('manor-audit')['open'] and send('lifecycle-state')['profile']['crowns']==before['crowns']
send('edit',value='100000');activate('Save')
before=send('lifecycle-state')['profile']
state=send('manor-state');upgrade=next(b['text'] for b in state['buttons'] if b['text'].startswith('Select upgrade') and not b['disabled'])
activate(upgrade);state=send('manor-state');assert state['focusText'].startswith('Select upgrade')
assert send('lifecycle-state')['profile']['crowns']==before['crowns']
activate('Review purchases');send('input',action='ui_cancel');assert send('manor-state')['focusText']=='Review purchases'
activate('Close');activate('Keep selecting')
activate('Review purchases');activate('Confirm and save')
after=send('lifecycle-state');assert after['profile']==after['persisted'] and after['profile']['crowns']<before['crowns']
upgrade=next(b['text'] for b in send('manor-state')['buttons'] if b['text'].startswith('Select upgrade') and not b['disabled'])
assert send('manor-double-press',text=upgrade)['removed']
assert any(c['text'].startswith('Selected 1 upgrades') for c in send('manor-audit')['controls'] if c['type']=='Label')
activate('Close');activate('Discard and close');assert not send('manor-audit')['open']
assert send('lifecycle-state')['profile']==after['profile']
send('manor');fixture=send('manor-trait-fixture',offset=0);traits=fixture['traits'];time.sleep(.2)
for width,height in [(1280,800),(1280,720),(1200,900),(1920,1080)]:
    state=send('manor-resize',width=width,height=height)
    assert all(c['rect']['x']>=0 and c['rect']['x']+c['rect']['width']<=float(state['viewport'].strip('()').split(',')[0])+1 for c in state['controls'] if c['type'] in ['Button','Label','LineEdit'])
    labels=[c for c in state['controls'] if c['type']=='Label']
    assert all(any(t['name']+': '+t['summary'] in l['text'] for l in labels) for t in traits)
    assert not any(l['focusable'] for l in labels)
    send('input',action='ui_up',milliseconds=1600)
    path=send('screenshot')['path'];shutil.copyfile(path,root/'private/sts2-evidence'/f'manor-{tag}-{width}x{height}.png')
for offset in range(3,fixture['total'],3):
    traits=send('manor-trait-fixture',offset=offset)['traits'];time.sleep(.15)
    state=send('manor-audit');labels=[c for c in state['controls'] if c['type']=='Label']
    assert all(any(t['name']+': '+t['summary'] in l['text'] for l in labels) for t in traits)
    send('input',action='mega_view_exhaust_pile_and_tab_right');assert send('manor-state')['scroll']>0
send('input',action='ui_cancel');assert not send('manor-audit')['open'];send('select')
for cls in ['knight','mage','ranger']:
    send('start',**{'class':cls,'traits':[],'quick':True,'seed':'SPIRELEGACY-MANOR-57'})
    deadline=time.monotonic()+45
    while send('state')['phase']!='Play':
        assert time.monotonic()<deadline;time.sleep(.2)
    send('manor');state=send('manor-state');assert state['paused'] and state['focusableLabels']==0
    assert not any(b['text']=='Choose this heir' or b['text'].startswith('Select upgrade') and not b['disabled'] for b in state['buttons'])
    send('input',action='mega_view_exhaust_pile_and_tab_right');send('input',action='ui_down',milliseconds=700)
    send('input',action='ui_cancel');assert not send('manor-audit')['paused'];send('abandon')
print(tag,'native Manor baseline passed',flush=True)
