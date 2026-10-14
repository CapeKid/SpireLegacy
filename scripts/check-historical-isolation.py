"""Challenge retired entry points in a disposable tree; never launch a game."""
import hashlib
import io
import json
import os
from pathlib import Path
import runpy
import shutil
import subprocess
import sys
import tempfile
import types
import zipfile
from PIL import Image
from historical_tools import RETIRED_WRITERS, RETIRED_CLI, RETIRED_POWERSHELL
from release_contract import exclude_historical_files, ReleaseError

ROOT = Path(__file__).resolve().parents[1]
BASE = '5787bd97d16416d8b3de744dd28e44177b992a3f'
checks = []


def passed(name):
    checks.append(name)


def snapshot(root):
    return {p.relative_to(root).as_posix(): hashlib.sha256(p.read_bytes()).hexdigest()
            for p in root.rglob('*') if p.is_file()}


def execute(command, cwd, env):
    return subprocess.run(command, cwd=cwd, env=env, capture_output=True, text=True, timeout=30)


def main():
    with tempfile.TemporaryDirectory(prefix='spire-historical-') as temp:
        scratch = Path(temp); scripts = scratch/'scripts'; scripts.mkdir()
        (scratch/'sheets').mkdir(); (scratch/'sheets/cards.json').write_text('[{"id":"current-sentinel"}]')
        (scratch/'profile').mkdir(); (scratch/'profile/family.json').write_text('personal-save-sentinel')
        (scratch/'dist').mkdir(); (scratch/'dist/current.zip').write_bytes(b'current-package-sentinel')
        env = dict(os.environ, PYTHONDONTWRITEBYTECODE='1', HEIR_DATA_DIR=str(scratch/'profile'))
        # Before-fix reproduction uses only a historical source snapshot in scratch.
        old = subprocess.check_output(['git', 'show', BASE+':scripts/create_sheets.py'], cwd=ROOT)
        baseline = scratch/'baseline'; (baseline/'scripts').mkdir(parents=True); (baseline/'sheets').mkdir()
        (baseline/'sheets/cards.json').write_text('[{"id":"current-sentinel"}]')
        (baseline/'sheets/classes.json').write_text('[{"id":"current-sentinel"}]')
        (baseline/'scripts/create_sheets.py').write_bytes(old)
        result = execute([sys.executable, '-B', str(baseline/'scripts/create_sheets.py')], baseline, env)
        assert result.returncode != 0 and "unexpected keyword argument 'power'" in result.stderr, result.stderr
        baseline_diagnostic = result.stderr
        assert 'current-sentinel' not in (baseline/'sheets/classes.json').read_text()
        passed('baseline initializer partly overwrites design before failing on obsolete card schema')
        for name in RETIRED_WRITERS+RETIRED_CLI+RETIRED_POWERSHELL+('historical_tools.py',):
            shutil.copy2(ROOT/'scripts'/name, scripts/name)
        for name in ('Play.cmd', 'Play.sh'): shutil.copy2(ROOT/name, scratch/name)
        before = snapshot(scratch)
        for name in RETIRED_WRITERS+RETIRED_CLI:
            result = execute([sys.executable, '-B', str(scripts/name), '--help'], scratch, env)
            assert result.returncode != 0 and 'retired StS1 tool' in result.stderr, (name, result.stderr)
            assert snapshot(scratch) == before, name
            passed(name+' CLI refuses before writes/dependencies')
        for name in RETIRED_WRITERS:
            result = execute([sys.executable, '-B', '-c',
                'import runpy,sys;sys.path.insert(0,"scripts");runpy.run_path(sys.argv[1],run_name="import_probe")',
                str(scripts/name)], scratch, env)
            assert result.returncode != 0 and 'retired StS1 tool' in result.stderr
            assert snapshot(scratch) == before, name
            passed(name+' import refuses before top-level reconstruction')
        pwsh = shutil.which('pwsh') or shutil.which('powershell')
        assert pwsh, 'PowerShell needed for actual retired-wrapper checks'
        for name in RETIRED_POWERSHELL:
            result = execute([pwsh, '-NoProfile', '-File', str(scripts/name)], scratch, env)
            assert result.returncode != 0 and 'Retired StS1 entry point' in result.stderr
            assert snapshot(scratch) == before
            passed(name+' refuses before file/process operations')
        result = execute([os.environ.get('COMSPEC','cmd.exe'), '/d', '/c', str(scratch/'Play.cmd')], scratch, env)
        assert result.returncode != 0 and 'Retired StS1 launcher' in result.stdout
        assert snapshot(scratch) == before
        passed('Windows launcher refuses before reader or game')
        bash = Path('C:/Program Files/Git/bin/bash.exe')
        assert bash.is_file(), 'Git Bash needed for actual shell-wrapper check'
        result = execute([str(bash), str(scratch/'Play.sh')], scratch, env)
        assert result.returncode != 0 and 'Retired StS1 launcher' in result.stderr
        assert snapshot(scratch) == before
        passed('Bash launcher refuses before chmod, reader or game')
        # Import only reader helpers, with an inert UnityPy stub. Images are synthetic.
        prior = sys.modules.get('UnityPy'); sys.modules['UnityPy'] = types.ModuleType('UnityPy')
        try: reader = runpy.run_path(str(ROOT/'scripts/load_rl.py'), run_name='reader_fixture')
        finally:
            if prior is None: sys.modules.pop('UnityPy', None)
            else: sys.modules['UnityPy'] = prior
        content = scratch/'synthetic-game'
        for folder in ('First_Data', 'Second_Data'):
            (content/folder).mkdir(parents=True); (content/folder/'resources.assets').touch()
        try: reader['content_directory'](content)
        except RuntimeError as error: assert 'Multiple Unity content folders' in str(error)
        else: raise AssertionError('Ambiguous legacy discovery accepted')
        passed('legacy discovery rejects ambiguous synthetic Unity folders')
        host = scratch/'synthetic-host'; host.mkdir(); cache=scratch/'cache'; cache.mkdir()
        png=io.BytesIO(); Image.new('RGBA',(1,1),'red').save(png,format='PNG')
        with zipfile.ZipFile(host/'desktop-1.0.jar','w') as archive:
            archive.writestr('cardui/cardui.atlas','fake.png\nicon\n  rotate: false\n  xy: 0, 0\n  size: 1, 1\n  offset: 0, 0\n  orig: 1, 1\n')
            archive.writestr('cardui/fake.png',png.getvalue())
        sheet=scratch/'assets.json'; (scratch/'host_assets.json').write_text('not json')
        try: reader['host_assets'](host,cache,sheet)
        except json.JSONDecodeError: pass
        else: raise AssertionError('Malformed legacy manifest accepted')
        assert not list(cache.iterdir())
        passed('legacy malformed manifest rejects before image output')
        (scratch/'host_assets.json').write_text(json.dumps([dict(sourceName='icon',output='../escaped.png',resize=0)]))
        reader['host_assets'](host,cache,sheet)
        assert (scratch/'escaped.png').is_file()
        passed('deferred legacy helper traversal reproduced with synthetic one-pixel asset')
        # Exercise the release exclusion independently of expected-archive equality.
        bad=['Play.cmd','Play.sh','load_rl.py','content-reader.zip','HeirOfTheSpire.jar',
             'heir/Reader/runtime.dll','heir/ReaderLinux/bin/python3.12','heir/SpireLegacyRuntime/a.png',
             'historical/sts1/melty.json','host_assets.json','assets.json','family.json','source.json']
        for name in bad:
            try: exclude_historical_files({'mods/SpireLegacy/'+name:b'fixture'})
            except ReleaseError: pass
            else: raise AssertionError('Historical release contamination accepted: '+name)
            passed('release refuses '+name)
        exclude_historical_files({'mods/SpireLegacy/SpireLegacy.dll':b'fixture','mods/BaseLib/BaseLib.pck':b'fixture'})
        passed('native DLL/PCK inputs remain permitted')
        assert not (ROOT/'melty.json').exists() and not (ROOT/'workshop/config.json').exists()
        assert json.loads((ROOT/'historical/sts1/melty.json').read_text())['games'][0]['slug']=='slay-the-spire'
        assert json.loads((ROOT/'sts2/melty.json').read_text())['games']==[dict(slug='slay-the-spire-2',role='primary')]
        passed('archived configs cannot shadow root/current StS2 discovery')
    report=dict(baseSource=BASE,checks=checks,count=len(checks),nativeRuntimeChanged=False,
                gameLaunched=False,ownedAssetsUsed=False,legacyTraversalDeferred=True,
                baselineDiagnostic=baseline_diagnostic,
                expected='retired entry points refuse without changing sentinel sheets, saves or packages',
                fixtureInput='synthetic sentinel tree; one-pixel atlas; no RNG seed or owned assets')
    (ROOT/'private/historical63-checks.json').write_text(json.dumps(report,indent=2)+'\n')
    print(json.dumps(dict(result='passed',checks=len(checks))))


if __name__=='__main__': main()
