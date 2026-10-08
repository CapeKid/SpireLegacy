"""Build a native Linux x86_64 package using redistributable reader dependencies."""
import json,shutil,zipfile,hashlib,importlib.metadata as metadata
from pathlib import Path
from preflight import validate,ROOT
validate()
version=json.loads((ROOT/'src/main/resources/ModTheSpire.json').read_text(encoding='utf-8-sig'))['version']
stage=ROOT/'build/package-linux-native';app=stage/'HeirOfTheSpire';mods=stage/'mods'
app.mkdir(parents=True,exist_ok=True);mods.mkdir(exist_ok=True)
runtime=ROOT/'tools/linux-python/cpython-3.12.13-linux-x86_64-gnu'
site=ROOT/'tools/linux-site';reader=app/'ReaderLinux'
if not (runtime/'bin/python3.12').is_file() or not (site/'UnityPy').is_dir():raise SystemExit('Prepare the pinned Linux reader dependencies first; see docs/steam-deck.md.')
(reader/'bin').mkdir(parents=True,exist_ok=True)
shutil.copy2(runtime/'bin/python3.12',reader/'bin/python3.12')
shutil.copytree(runtime/'lib',reader/'lib',dirs_exist_ok=True,ignore=shutil.ignore_patterns('site-packages','__pycache__','*.pyc','*.a','pkgconfig'))
shutil.copytree(site,reader/'site',dirs_exist_ok=True,ignore=shutil.ignore_patterns('__pycache__','*.pyc','bin','fmod_toolkit','pyfmodex','fmod_toolkit-*.dist-info','pyfmodex-*.dist-info'))
for name in ('Play.sh','README.md','LICENSE'):shutil.copy2(ROOT/name,app/name)
for name in ('load_rl.py','launch_linux.py'):shutil.copy2(ROOT/'scripts'/name,app/name)
shutil.copy2(ROOT/'docs/steam-deck.md',app/'STEAM-DECK.md')
for name in ('assets.json','host_assets.json'):shutil.copy2(ROOT/'src/main/resources/heir'/name,app/name)
shutil.copy2(ROOT/'dist/HeirOfTheSpire.jar',mods)
shutil.copy2(ROOT/'tools/BaseMod.jar',mods)
shutil.copy2(ROOT/'tools/ModTheSpire.jar',app)
licenses=app/'licenses';licenses.mkdir(exist_ok=True)
shutil.copy2(ROOT/'LICENSE',licenses/'SpireLegacy-MIT.txt')
shutil.copy2(runtime/'lib/python3.12/LICENSE.txt',licenses/'Python-LICENSE.txt')
shutil.copy2(ROOT.parent/'tools/BaseMod/LICENSE',licenses/'BaseMod-MIT.txt')
shutil.copy2(ROOT.parent/'tools/ModTheSpire-source/LICENSE',licenses/'ModTheSpire-MIT.txt')
shutil.copytree(ROOT/'licenses/python-build-standalone',licenses/'python-build-standalone',dirs_exist_ok=True)
dependencies=[dict(name=d.metadata['Name'],version=d.version,license=d.metadata.get('License-Expression',d.metadata.get('License','See included dist-info notices'))) for d in metadata.distributions(path=[str(site)]) if d.metadata['Name'].lower() not in ('fmod-toolkit','pyfmodex')]
(licenses/'linux-dependencies.json').write_text(json.dumps(dependencies,indent=2))
archive=ROOT/'dist'/f'SpireLegacy-{version}-linux-x86_64.zip'
entries=[]
with zipfile.ZipFile(archive,'w',zipfile.ZIP_DEFLATED) as out:
    for file in sorted(stage.rglob('*')):
        if not file.is_file():continue
        name=file.relative_to(stage).as_posix()
        if any(p in name.lower() for p in ('desktop-1.0','assembly-csharp','source.json','family.json','fmod.dll','libfmod')):raise RuntimeError('Forbidden release file '+name)
        entry=zipfile.ZipInfo(name);entry.create_system=3;entry.compress_type=zipfile.ZIP_DEFLATED
        executable=name.endswith('/Play.sh') or name.endswith('/bin/python3.12') or name.endswith('.so')
        entry.external_attr=(0o100755 if executable else 0o100644)<<16
        payload=file.read_bytes();out.writestr(entry,payload);entries.append(dict(path=name,size=len(payload)))
(ROOT/'build/linux-package-inspection.json').write_text(json.dumps(dict(fileName=archive.name,entries=entries),indent=2))
print(archive.name,archive.stat().st_size,'bytes; SHA-256',hashlib.sha256(archive.read_bytes()).hexdigest())
