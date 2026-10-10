"""Stage a native StS2 mod and separate Workshop/Melty packages; no owned game files."""
import hashlib, json, pathlib, shutil, zipfile
from PIL import Image
repo=pathlib.Path(__file__).resolve().parents[1]
manifest=json.loads((repo/'sts2/SpireLegacy.json').read_text(encoding='utf-8-sig'))
version=manifest['version']
stage=repo/'build/sts2/release'/version
mod=stage/'mods/SpireLegacy'
mod.mkdir(parents=True,exist_ok=True)
def copy(source,target):
    target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(source,target)
copy(repo/'sts2/bin/Release/net9.0/SpireLegacy.dll',mod/'SpireLegacy.dll')
copy(repo/'sts2/SpireLegacy.json',mod/'SpireLegacy.json')
copy(repo/'build/sts2/content-reader.zip',mod/'content-reader.zip')
illustrations=json.loads((repo/'sheets/card_art.json').read_text(encoding='utf-8-sig'))
for art in illustrations:
    source=repo/'src/main/resources'/art['path']
    if hashlib.sha256(source.read_bytes()).hexdigest()!=art['sha256']: raise ValueError('Illustration hash mismatch: '+art['path'])
    copy(source,mod/art['path'])
for art in json.loads((repo/'sheets/character_art.json').read_text(encoding='utf-8')):
    source=repo/'src/main/resources'/art['path']
    if hashlib.sha256(source.read_bytes()).hexdigest()!=art['sha256']:raise ValueError('Character atlas hash mismatch: '+art['id'])
    copy(source,mod/art['path'])
for art in ['select-icon.png','select-bg.png','heir-portrait.png']:copy(repo/'src/main/resources/heir/ui'/art,mod/'heir/ui'/art)
copy(repo/'LICENSE',mod/'LICENSE')
copy(repo/'private/sts2-baselib/LICENSE.txt',mod/'licenses/BaseLib-MIT.txt')
copy(repo/'docs/sts2-port.md',mod/'README.md')
copy(repo/'docs/traits.md',mod/'traits.md')
copy(repo/'docs/character-sprites.md',mod/'character-sprites.md')
def archive(root,target):
    with zipfile.ZipFile(target,'w',zipfile.ZIP_DEFLATED,compresslevel=6) as output:
        for path in sorted(root.rglob('*')):
            if not path.is_file():continue
            if path.suffix.lower()=='.log' or path.name=='mod_id.txt':continue
            name=path.relative_to(root).as_posix()
            if any(token in name.lower() for token in ('family.json','source.json','sts2.dll','godotsharp.dll','slaythespire2.pck','assembly-csharp','host_assets','heir/test')):raise ValueError('Forbidden packaged file: '+name)
            entry=zipfile.ZipInfo(name);entry.create_system=3;entry.compress_type=zipfile.ZIP_DEFLATED;entry.external_attr=0o100644<<16
            output.writestr(entry,path.read_bytes())
    with zipfile.ZipFile(target) as check:
        if check.testzip() is not None:raise ValueError('Archive failed CRC validation')
    return dict(path=str(target),size=target.stat().st_size,sha256=hashlib.sha256(target.read_bytes()).hexdigest())
dist=repo/'dist';dist.mkdir(exist_ok=True)
deliveries=[archive(stage,dist/f'SpireLegacy-StS2-{version}.zip')]
workshop=repo/'build/sts2/workshop'
workshop.mkdir(parents=True,exist_ok=True)
shutil.copytree(mod,workshop/'content/SpireLegacy',dirs_exist_ok=True)
copy(repo/'sts2/workshop.json',workshop/'workshop.json')
copy(repo/'docs/sts2-port.md',workshop/'README.md')
copy(repo/'docs/traits.md',workshop/'traits.md')
Image.open(repo/'src/main/resources/heir/ui/select-bg.png').convert('RGB').resize((640,400),Image.Resampling.LANCZOS).save(workshop/'image.png',optimize=True)
if (workshop/'image.png').stat().st_size>=1_000_000:raise ValueError('Workshop preview exceeds 1MB')
deliveries.append(archive(workshop,dist/f'SpireLegacy-StS2-{version}-workshop.zip'))
melty=repo/'build/sts2/melty'/version
shutil.copytree(stage,melty,dirs_exist_ok=True)
nuget=pathlib.Path.home()/'.nuget/packages/alchyr.sts2.baselib/3.4.7'
for source in [nuget/'lib/net9.0/BaseLib.dll',nuget/'Content/BaseLib.json',nuget/'Content/BaseLib.pck']:copy(source,melty/'mods/BaseLib'/source.name)
melty_manifest=json.loads((repo/'sts2/melty.json').read_text())
copy(repo/'sts2/melty.json',melty/'melty.json')
deliveries.append(archive(melty,dist/f'SpireLegacy-StS2-{version}-melty.zip'))
(repo/'build/sts2/package-inspection.json').write_text(json.dumps(dict(deliveries=deliveries,mod_sha256=hashlib.sha256((mod/'SpireLegacy.dll').read_bytes()).hexdigest(),card_illustrations=len(illustrations),melty=melty_manifest),indent=2),encoding='utf-8')
for item in deliveries:print(json.dumps(item))
