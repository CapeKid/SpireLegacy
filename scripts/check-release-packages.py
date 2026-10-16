"""Adversarial release/install tests in disposable output and source trees (#62)."""
import copy
import importlib.util
import json
import os
import shutil
import stat
import struct
import subprocess
import sys
import tempfile
import zipfile
from pathlib import Path
from release_contract import payloads,validate_archive,write_archive,digest,ReleaseError,ITEM_ID
from release_transaction import recover,tree_hash

root=Path(__file__).resolve().parents[1]
def module(name):
    spec=importlib.util.spec_from_file_location(name,root/'scripts'/f'{name}.py');mod=importlib.util.module_from_spec(spec);spec.loader.exec_module(mod);return mod
pack=module('sts2-package');install=module('sts2-install-check')
report=dict(seed='SPIRELEGACY-RELEASE-62',checks=[],layouts=[])
def passed(name):report['checks'].append(name);print('passed:',name,flush=True)

with tempfile.TemporaryDirectory(prefix='package62-',dir=root/'private') as directory:
    base=Path(directory).resolve();require_root=(root/'private').resolve();assert base.is_relative_to(require_root)
    def erase(path):
        assert path.resolve().is_relative_to(base)
        for child in path.rglob('*'):assert child.resolve().is_relative_to(base) and not child.is_symlink()
        shutil.rmtree(path)
    source=base/'source é with spaces';source.mkdir()
    for folder in ['scripts','sheets','docs','licenses','src/main/resources']:shutil.copytree(root/folder,source/folder)
    for rel in ['LICENSE','README.md','sts2/SpireLegacy.json','sts2/melty.json','sts2/workshop.json','sts2/bin/Release/net9.0/SpireLegacy.dll']:
        target=source/rel;target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(root/rel,target)
    # No ignored source-license directory or installed game is available here.
    h=digest((source/'sts2/bin/Release/net9.0/SpireLegacy.dll').read_bytes());v,formats=payloads(source,h)
    output=base/'release outputs é';pack.build(source,output,h);before=tree_hash(output)
    archives={kind:output/'dist'/f'SpireLegacy-StS2-{v}{"" if kind=="standalone" else "-"+kind}.zip' for kind in formats}
    for kind,files in formats.items():validate_archive(archives[kind],files)
    passed('clean release formats and public pinned license')
    # Poison all three prior trees; only the validated uploader marker survives.
    poison=[output/f'build/sts2/release/{v}/mods/SpireLegacy/obsolete.png',output/'build/sts2/workshop/content/SpireLegacy/.env',output/f'build/sts2/melty/{v}/mods/SpireLegacy/unexpected.dll']
    for path in poison:path.write_bytes(b'fixture-only stale bytes')
    marker=output/'build/sts2/workshop/mod_id.txt';marker.write_text(str(ITEM_ID)+'\n')
    first=pack.build(source,output,h)
    for path in poison:assert not path.exists()
    assert marker.read_text().strip()==str(ITEM_ID)
    for kind,files in formats.items():validate_archive(archives[kind],files)
    second=pack.build(source,output,h);assert first==second
    passed('dirty staging omitted, item marker preserved, deterministic archives')
    stable=tree_hash(output)
    try:pack.build(source,output,'0'*64)
    except ReleaseError:pass
    else:raise AssertionError('wrong DLL accepted')
    assert tree_hash(output)==stable;passed('wrong checked-DLL hash preserves outputs')
    cases=[('sts2/melty.json',lambda data:data['components'][0].update(fileName='old.zip')),('sts2/workshop.json',lambda data:data.update(dependencies=[1])),('sts2/workshop.json',lambda data:data.update(minBranch='unsupported')),('sts2/SpireLegacy.json',lambda data:data.update(version='../../outside')),
           ('sheets/systems.json',lambda data:next(r for r in data if r['id']=='playtest_crowns').update(enabled=True)),
           ('sheets/icon_art.json',lambda data:data[0].update(license='unreviewed'))]
    for rel,edit in cases:
        path=source/rel;original=path.read_bytes();data=json.loads(original);edit(data);path.write_text(json.dumps(data))
        try:
            try:pack.build(source,output,h)
            except ValueError:pass
            else:raise AssertionError('bad config accepted '+rel)
            assert tree_hash(output)==stable
        finally:path.write_bytes(original)
    passed('invalid version/dependency/branch/Melty configs preserve outputs')
    notes=source/'docs'/f'release-{v}.md';original=notes.read_bytes();notes.unlink()
    try:
        try:pack.build(source,output,h)
        except ReleaseError:pass
        else:raise AssertionError('missing release guide accepted')
        assert tree_hash(output)==stable
    finally:notes.write_bytes(original)
    passed('missing current guide cannot silently retain old notes')
    marker.write_text('999');bad_marker_state=tree_hash(output)
    try:pack.build(source,output,h)
    except ReleaseError:pass
    else:raise AssertionError('wrong uploader item accepted')
    assert tree_hash(output)==bad_marker_state;marker.write_text(str(ITEM_ID)+'\n')
    passed('wrong uploader marker rejected without changing outputs')
    def fail(index,phase):
        if index==1 and phase=='before-new':raise OSError('injected interrupted directory promotion')
    try:pack.build(source,output,h,hook=fail)
    except OSError:pass
    else:raise AssertionError('fault not injected')
    assert tree_hash(output)==stable;passed('promotion exception restores old outputs byte-for-byte')
    class Crash(BaseException):pass
    def crash(index,phase):
        if index==1 and phase=='after-new':raise Crash('simulated process interruption')
    try:pack.build(source,output,h,hook=crash)
    except Crash:pass
    assert (output/'build/sts2/package-transaction.json').exists()
    changed=output/'build/sts2/workshop/README.md';original=changed.read_bytes();changed.write_bytes(original+b'\nuser modification')
    interrupted=tree_hash(output)
    try:recover(output)
    except ReleaseError:pass
    else:raise AssertionError('recovery overwrote modified output')
    assert tree_hash(output)==interrupted;changed.write_bytes(original)
    assert recover(output);assert tree_hash(output)==stable
    passed('process interruption leaves recoverable journal and restores complete old release')
    # Read-only malformed journal checks must never escape the output root.
    journal=output/'build/sts2/package-transaction.json';journal.write_text(json.dumps(dict(version=v,phase='prepared',transaction='../outside',operations=[])))
    try:recover(output)
    except ReleaseError:pass
    else:raise AssertionError('unsafe recovery plan accepted')
    journal.unlink();assert tree_hash(output)==stable;passed('unsafe recovery plan rejected before mutations')
    # Tiny malicious archives exercise the real validator, independent of asserts.
    expected={'mods/SpireLegacy/test.txt':b'valid'}
    invalid=base/'bad.zip'
    names=['../outside','/absolute','C:/drive','mods\\backslash','mods//empty','mods/./dot','mods/SpireLegacy/.env','mods/SpireLegacy/sts2.dll']
    for name in names:
        with zipfile.ZipFile(invalid,'w') as archive:
            item=zipfile.ZipInfo(name);item.create_system=3;item.external_attr=0o100644<<16;archive.writestr(item,b'valid')
        try:validate_archive(invalid,expected)
        except ReleaseError:pass
        else:raise AssertionError('unsafe ZIP accepted '+name)
    for mode in [0o100755,0o120777,0o40755]:
        with zipfile.ZipFile(invalid,'w') as archive:
            item=zipfile.ZipInfo(next(iter(expected)));item.create_system=3;item.external_attr=mode<<16;archive.writestr(item,b'valid')
        try:validate_archive(invalid,expected)
        except ReleaseError:pass
        else:raise AssertionError('bad mode accepted')
    with zipfile.ZipFile(invalid,'w') as archive:
        for name in ['mods/SpireLegacy/a.txt','mods/SpireLegacy/A.txt']:
            item=zipfile.ZipInfo(name);item.create_system=3;item.external_attr=0o100644<<16;archive.writestr(item,b'valid')
    try:validate_archive(invalid,{'mods/SpireLegacy/a.txt':b'valid','mods/SpireLegacy/A.txt':b'valid'})
    except ReleaseError:pass
    else:raise AssertionError('case collision accepted')
    write_archive(invalid,expected);raw=bytearray(invalid.read_bytes());central=raw.index(b'PK\x01\x02');raw[central+8:central+10]=struct.pack('<H',1);invalid.write_bytes(raw)
    try:validate_archive(invalid,expected)
    except ReleaseError:pass
    else:raise AssertionError('encrypted entry accepted')
    passed('path traversal, case collisions, unknown files, modes and encryption rejected')
    with zipfile.ZipFile(invalid,'w') as archive:
        item=zipfile.ZipInfo(next(iter(expected)));item.create_system=3;item.external_attr=0o100644<<16;archive.writestr(item,b'valid')
    raw=bytearray(invalid.read_bytes());name=next(iter(expected));raw[30+len(name.encode())]^=1;invalid.write_bytes(raw)
    try:validate_archive(invalid,expected)
    except ReleaseError:pass
    else:raise AssertionError('CRC corruption accepted')
    passed('corrupt ZIP CRC reports a release error')
    checker=source/'scripts/check-sts2-release.py'
    # A malformed archive still fails under -O: no assertion-based bypass.
    archive=archives['standalone'];old=archive.read_bytes();archive.write_bytes(invalid.read_bytes())
    result=subprocess.run([sys.executable,'-O',str(checker),'--expected-dll-sha256',h,'--output-root',str(output)],capture_output=True,text=True)
    assert result.returncode!=0;archive.write_bytes(old);passed('optimized Python cannot disable release checks')
    # These are layout fixtures, not physical Linux/Proton execution claims.
    for platform,library in [('windows','C/Steam Library é'),('linux','home/deck/SD Card Library é'),('proton','home/deck/Steam Library/steamapps/compatdata/2868840/pfx/drive_c/Steam Library é')]:
        steamapps=base/platform/library/'steamapps';game=steamapps/'common/Slay the Spire 2';game.mkdir(parents=True)
        user=base/platform/'separate family/user-data';user.mkdir(parents=True);save=user/'family.json';save.write_bytes(b'untouched fixture save')
        # Standalone depends on separately installed BaseLib.
        with zipfile.ZipFile(archives['standalone']) as archive:archive.extractall(game)
        base_files={name:data for name,data in formats['melty'].items() if name.startswith('mods/BaseLib/')}
        for name,data in base_files.items():path=game/name;path.parent.mkdir(parents=True,exist_ok=True);path.write_bytes(data)
        state=install.inspect(game,expected_version=v,expected_hash=h);assert state['ok'],state
        dependency=game/'mods/BaseLib/BaseLib.json';original=dependency.read_bytes();data=json.loads(original);data['version']='v1.0.0';dependency.write_text(json.dumps(data))
        assert any('requires BaseLib 3.4.7' in error for error in install.inspect(game)['errors']);dependency.write_bytes(original)
        # Update by whole-directory replacement; all payload bytes match.
        erase(game/'mods/SpireLegacy')
        with zipfile.ZipFile(archives['standalone']) as archive:archive.extractall(game)
        for name,data in formats['standalone'].items():assert (game/name).read_bytes()==data
        workshop=steamapps/'workshop/content/2868840'/str(ITEM_ID);workshop.mkdir(parents=True)
        for name,data in formats['workshop'].items():
            if name.startswith('content/'):
                path=workshop/name.removeprefix('content/');path.parent.mkdir(parents=True,exist_ok=True);path.write_bytes(data)
        duplicate=install.inspect(game,expected_version=v,expected_hash=h);assert not duplicate['ok'] and any('takes precedence' in error for error in duplicate['errors'])
        erase(game/'mods/SpireLegacy');assert install.inspect(game,expected_version=v,expected_hash=h)['ok']
        # Unsubscribe/remove Workshop plus manual dependency; saves remain intact.
        erase(workshop);erase(game/'mods/BaseLib');assert save.read_bytes()==b'untouched fixture save'
        # Melty's two directory mappings copy both complete mods in a Windows layout.
        for name,data in formats['melty'].items():
            if name.startswith('mods/'):
                path=game/name;path.parent.mkdir(parents=True,exist_ok=True);path.write_bytes(data)
        assert install.inspect(game,expected_version=v,expected_hash=h)['ok']
        report['layouts'].append(dict(platform=platform,spacesUnicodeMovedLibrary=True,freshInstall=True,wholeDirectoryUpdate=True,duplicatesActionable=True,uninstallPreservesSaves=True,meltyDirectoryMappingFixture=True,physicalExecution=False))
    passed('Windows/Linux/Proton fresh/update/duplicate/uninstall layouts preserve saves')
    report['packages']=second['packages'];report['dllSha256']=h
report['count']=len(report['checks'])
(root/'private/package62-checks.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps(dict(result='passed',checks=report['count'],layouts=len(report['layouts']))))
