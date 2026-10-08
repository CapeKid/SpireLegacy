"""Stage the official uploader workspace; both platform readers, no game assets."""
from pathlib import Path
import json, shutil, zipfile, hashlib
from PIL import Image
from preflight import ROOT, validate
validate()
version=json.loads((ROOT/'src/main/resources/ModTheSpire.json').read_text(encoding='utf-8-sig'))['version']
workspace=ROOT/'build/workshop';content=workspace/'content';runtime=content/'SpireLegacyRuntime'
runtime.mkdir(parents=True,exist_ok=True)
shutil.copy2(ROOT/'dist/HeirOfTheSpire.jar',content)
# The regular packages already assemble pinned runtimes and their license notices.
windows=ROOT/'build/package/HeirOfTheSpire';linux=ROOT/'build/package-linux-native/HeirOfTheSpire'
for name in ('Reader','assets.json','host_assets.json'):
    source=windows/name
    if source.is_dir():shutil.copytree(source,runtime/name,dirs_exist_ok=True)
    else:shutil.copy2(source,runtime/name)
shutil.copytree(linux/'ReaderLinux',runtime/'ReaderLinux',dirs_exist_ok=True)
shutil.copy2(ROOT/'scripts/load_rl.py',runtime/'load_rl.py')
shutil.copytree(windows/'licenses',runtime/'licenses/windows',dirs_exist_ok=True)
shutil.copytree(linux/'licenses',runtime/'licenses/linux',dirs_exist_ok=True)
shutil.copy2(ROOT/'LICENSE',content/'LICENSE')
shutil.copy2(ROOT/'docs/workshop.md',workspace/'README.md')
shutil.copy2(ROOT/'workshop/dependencies.json',workspace/'dependencies.json')
# Preserve the Steam item ID across updates; the checked-in config controls visibility.
config=json.loads((ROOT/'workshop/config.json').read_text())
target=workspace/'config.json'
if target.exists():
    previous=json.loads(target.read_text())
    if previous.get('steamPublishedID'):config['steamPublishedID']=previous['steamPublishedID']
target.write_text(json.dumps(config,indent=2)+'\n')
Image.open(ROOT/'src/main/resources/heir/ui/select-bg.png').convert('RGB').resize((960,600),Image.Resampling.LANCZOS).save(workspace/'image.jpg',quality=90)
archive=ROOT/'dist'/f'SpireLegacy-{version}-workshop.zip'
with zipfile.ZipFile(archive,'w',zipfile.ZIP_DEFLATED) as out:
    for file in sorted(workspace.rglob('*')):
        if not file.is_file():continue
        name=file.relative_to(workspace).as_posix()
        if any(token in name.lower() for token in ('desktop-1.0','assembly-csharp','source.json','family.json','fmod.dll','libfmod')):raise RuntimeError('Forbidden Workshop file '+name)
        entry=zipfile.ZipInfo(name);entry.create_system=3;entry.compress_type=zipfile.ZIP_DEFLATED
        executable=name.endswith('/bin/python3.12') or name.endswith('.so')
        entry.external_attr=(0o100755 if executable else 0o100644)<<16
        out.writestr(entry,file.read_bytes())
print(archive.name,archive.stat().st_size,'bytes; SHA-256',hashlib.sha256(archive.read_bytes()).hexdigest())
