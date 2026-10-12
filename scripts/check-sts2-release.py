"""Inspect all three archives against declared original assets and release inputs."""
import hashlib,json,zipfile
from pathlib import Path
root=Path(__file__).resolve().parents[1]
manifest=json.loads((root/'sts2/SpireLegacy.json').read_text(encoding='utf-8-sig'));version=manifest['version']
digest=lambda data:hashlib.sha256(data).hexdigest()
assets={}
for sheet in ['icon_art','card_art','character_art','relic_art','presentation_art','potion_art']:
    for row in json.loads((root/'sheets'/f'{sheet}.json').read_text(encoding='utf-8-sig')):
        for field,hashfield in [('path','sha256'),('outline','outlineSha256')]:
            if field in row:
                path=row[field];data=(root/'src/main/resources'/path).read_bytes()
                assert digest(data)==row[hashfield],(sheet,path)
                assets[path]=data
        if isinstance(row.get('death'),dict):
            death=row['death'];data=(root/'src/main/resources'/death['path']).read_bytes()
            assert digest(data)==death['sha256'];assets[death['path']]=data
for name in ['select-icon.png','select-bg.png','heir-portrait.png']:
    path='heir/ui/'+name;assets[path]=(root/'src/main/resources'/path).read_bytes()
expected=dict(assets)
expected.update({'SpireLegacy.dll':(root/'sts2/bin/Release/net9.0/SpireLegacy.dll').read_bytes(),
                 'SpireLegacy.json':(root/'sts2/SpireLegacy.json').read_bytes(),
                 'LICENSE':(root/'LICENSE').read_bytes(),
                 'licenses/BaseLib-MIT.txt':(root/'private/sts2-baselib/LICENSE.txt').read_bytes(),
                 'README.md':(root/'docs/sts2-port.md').read_bytes(),
                 'release-notes.md':(root/'docs'/f'release-{version}.md').read_bytes()})
for name in ['traits','trait-effect-audit','character-sprites','original-icons','heir-relics','heir-potions','heir-ancient-dialogue','family-chronicle','coop','heir-presentation','heir-ancient-cards','sts2-pool-expansion','card-pools','card-catalogue']:
    expected[name+'.md']=(root/'docs'/f'{name}.md').read_bytes()
nuget=Path.home()/'.nuget/packages/alchyr.sts2.baselib/3.4.7'
base={name:(nuget/sub/name).read_bytes() for name,sub in [('BaseLib.dll','lib/net9.0'),('BaseLib.json','Content'),('BaseLib.pck','Content')]}
reports=[]
for kind in ['standalone','workshop','melty']:
    suffix='' if kind=='standalone' else '-'+kind
    path=root/'dist'/f'SpireLegacy-StS2-{version}{suffix}.zip'
    prefix='content/SpireLegacy/' if kind=='workshop' else 'mods/SpireLegacy/'
    wanted={prefix+name:data for name,data in expected.items()}
    if kind=='workshop':
        for name,source in [('workshop.json',root/'sts2/workshop.json'),('README.md',root/'docs/sts2-port.md'),('traits.md',root/'docs/traits.md'),('image.png',root/'build/sts2/workshop/image.png')]:wanted[name]=source.read_bytes()
        settings=json.loads(wanted['workshop.json'])
        assert settings['visibility']=='private' and 3737335127 in settings['dependencies']
        assert len(wanted['image.png'])<1_000_000
    if kind=='melty':
        wanted.update({'mods/BaseLib/'+name:data for name,data in base.items()})
        wanted['melty.json']=(root/'sts2/melty.json').read_bytes()
        assert json.loads(wanted['melty.json'])['components'][0]['fileName']==path.name
    with zipfile.ZipFile(path) as archive:
        names=archive.namelist();assert len(names)==len(set(names)),'Duplicate archive names'
        assert set(names)==set(wanted),(kind,'Unexpected/missing files',set(names)^set(wanted))
        assert archive.testzip() is None
        for name,data in wanted.items():assert archive.read(name)==data,(kind,name,'Content mismatch')
    reports.append(dict(kind=kind,file=path.name,size=path.stat().st_size,sha256=digest(path.read_bytes()),files=len(wanted),dllSha256=digest(expected['SpireLegacy.dll'])))
print(json.dumps(dict(version=version,requiresRL2=False,packages=reports),indent=2))
