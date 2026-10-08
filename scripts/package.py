"""Package only original content and redistributable dependencies; never private game files."""
from pathlib import Path
import importlib.metadata as metadata
import json,shutil,zipfile,hashlib
from preflight import validate,ROOT
validate()
stage=ROOT/'build/package';app=stage/'HeirOfTheSpire';mods=stage/'mods'
app.mkdir(parents=True,exist_ok=True);mods.mkdir(exist_ok=True)
shutil.copy2(ROOT/'dist/HeirOfTheSpire.jar',mods)
shutil.copy2(ROOT/'tools/BaseMod.jar',mods)
shutil.copy2(ROOT/'tools/ModTheSpire.jar',app)
for path in ('Play.cmd','README.md','LICENSE'):shutil.copy2(ROOT/path,app/path)
shutil.copy2(ROOT/'src/main/resources/ModTheSpire.json',app/'ModTheSpire.json')
for path in ('assets.json','host_assets.json'):shutil.copy2(ROOT/'src/main/resources/heir'/path,app/path)
shutil.copytree(ROOT/'private/helper-release/ReadRogueLegacy',app/'Reader',dirs_exist_ok=True)
licenses=app/'licenses';licenses.mkdir(exist_ok=True)
shutil.copy2(ROOT.parent/'tools/BaseMod/LICENSE',licenses/'BaseMod-MIT.txt')
shutil.copy2(ROOT.parent/'tools/ModTheSpire-source/LICENSE',licenses/'ModTheSpire-MIT.txt')
credits=[]
for dist in metadata.distributions():
    name=dist.metadata.get('Name','unknown')
    if name.lower() in ('pyfmodex','fmod-toolkit','dnfile'):continue
    found=[]
    for file in dist.files or []:
        text=str(file)
        if '.dist-info/' in text and any(t in file.name.lower() for t in ('license','licence','copying','notice')):
            path=Path(dist.locate_file(file))
            if path.is_file():
                target=licenses/name/file.name;target.parent.mkdir(exist_ok=True);shutil.copy2(path,target);found.append(str(target.relative_to(app)))
    credits.append({'name':name,'version':dist.version,'license':dist.metadata.get('License-Expression',dist.metadata.get('License','see included notices')),'notices':found})
# Python's redistribution notice accompanies the embedded interpreter.
import sys
python_license=Path(sys.base_prefix)/'LICENSE.txt'
if python_license.exists():shutil.copy2(python_license,licenses/'Python-LICENSE.txt')
(licenses/'dependencies.json').write_text(json.dumps(credits,indent=2))
version=json.loads((ROOT/'src/main/resources/ModTheSpire.json').read_text(encoding='utf-8-sig'))['version']
archive=ROOT/('dist/HeirOfTheSpire-'+version+'.zip')
entries=[]
with zipfile.ZipFile(archive,'w',zipfile.ZIP_DEFLATED) as z:
    for file in sorted(stage.rglob('*')):
        if not file.is_file():continue
        name=file.relative_to(stage).as_posix()
        if any(s in name.lower() for s in ('desktop-1.0','assembly-csharp','source.json','family.json','fmod.dll')):raise RuntimeError('Forbidden release file '+name)
        z.write(file,name);entries.append({'path':name,'size':file.stat().st_size})
files={p: (stage/p).read_text() for p in ('HeirOfTheSpire/README.md','HeirOfTheSpire/Play.cmd','HeirOfTheSpire/ModTheSpire.json')}
with zipfile.ZipFile(ROOT/'dist/HeirOfTheSpire.jar') as jar:files['mods/HeirOfTheSpire.jar!/ModTheSpire.json']=jar.read('ModTheSpire.json').decode()
(ROOT/'build/package-inspection.json').write_text(json.dumps({'fileName':archive.name,'entries':entries,'files':files,'gameSlugs':['slay-the-spire','custom-rogue-legacy-2'],'says':(ROOT/'README.md').read_text()},indent=2))
print(archive.name,archive.stat().st_size,'bytes; SHA-256',hashlib.sha256(archive.read_bytes()).hexdigest(),';',len(entries),'entries')
