"""Closed native release inputs and archive validation, also used by install fixtures."""
import hashlib
import json
import io
import re
import posixpath
from urllib.parse import quote
import zipfile
from pathlib import Path
from PIL import Image
from design_schema import read_json, load_sheets, validate_assets
from release_assets import inventory

APP_ID=2868840
ITEM_ID=3816525393
BASELIB_ITEM=3737335127
GUIDES=['traits','trait-effect-audit','character-sprites','original-icons','heir-relics','heir-potions','heir-ancient-dialogue','family-chronicle','coop','heir-presentation','heir-ancient-cards','sts2-pool-expansion','card-pools','card-catalogue','adversarial-harness','adversarial-coop','adversarial-pools','earned-crowns']

class ReleaseError(ValueError):pass
def require(condition,message):
    if not condition:raise ReleaseError(message)
def digest(data):return hashlib.sha256(data).hexdigest()
def version(value):
    require(isinstance(value,str) and re.fullmatch(r'(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)',value),'Invalid release version')
    return value
def safe_name(name):
    require(isinstance(name,str) and name and '\\' not in name and ':' not in name and not name.startswith('/'),'Unsafe archive path: '+str(name))
    require(all(part not in {'','.','..'} for part in name.split('/')),'Unsafe archive path: '+name)
    require(not any(ord(c)<32 for c in name),'Control character in archive path')
    return name

def exclude_historical_files(files):
    """Retired executable content is forbidden even if future inputs declare it."""
    for name in files:
        safe_name(name)
        parts=name.lower().split('/')
        require(not any(p in {'reader','readerlinux','spirelegacyruntime','historical'} for p in parts),
                'Historical runtime directory in native release: '+name)
        require(not parts[-1].endswith(('.jar','.java','.py','.cmd','.sh','.exe')),
                'Historical executable/source in native release: '+name)
        require(parts[-1] not in {'content-reader.zip','assets.json','host_assets.json','source.json','family.json'},
                'Historical reader or save data in native release: '+name)

def configuration(root):
    manifest=read_json(root/'sts2/SpireLegacy.json');require(isinstance(manifest,dict),'Invalid mod manifest');v=version(manifest.get('version'))
    require(manifest.get('id')=='SpireLegacy' and manifest.get('author')=='CapeKid','Wrong mod identity/author')
    for key,expected in [('has_dll',True),('has_pck',False),('affects_gameplay',True)]:
        require(type(manifest.get(key)) is bool and manifest[key]==expected,'Invalid manifest '+key)
    require(manifest.get('min_game_version')=='0.107.1','Unexpected minimum game version')
    require(manifest.get('dependencies')==[dict(id='BaseLib',min_version='3.4.7')],'Unexpected BaseLib dependency')
    workshop=read_json(root/'sts2/workshop.json')
    require(isinstance(workshop,dict),'Invalid Workshop configuration')
    require(workshop.get('visibility')=='public' and workshop.get('dependencies')==[BASELIB_ITEM],'Workshop visibility/dependency differs from existing release')
    require(not workshop.get('minBranch') and not workshop.get('maxBranch'),'Workshop restricts a supported branch')
    require(workshop.get('changeNote','').startswith(v+':'),'Workshop change note version mismatch')
    melty=read_json(root/'sts2/melty.json');filename=f'SpireLegacy-StS2-{v}-melty.zip'
    require(isinstance(melty,dict),'Invalid Melty configuration')
    require(type(melty.get('schemaVersion')) is int and melty['schemaVersion']==1 and melty.get('mode')=='installed','Wrong Melty format')
    require(melty.get('games')==[dict(slug='slay-the-spire-2',role='primary')],'Wrong Melty game')
    components=melty.get('components',[])
    require(isinstance(components,list) and len(components)==1 and isinstance(components[0],dict) and components[0].get('required') is True and all(components[0].get(k)==val for k,val in dict(id='main',fileName=filename,kind='main').items()),'Melty component/version mismatch')
    require(melty.get('launch')==dict(kind='exe',path='{game}/SlayTheSpire2.exe',cwd='{game}'),'Melty Windows launch mapping mismatch')
    require(melty.get('mappings')==[dict(component='main',**{'from':'mods/SpireLegacy/','to':'{game}/mods/SpireLegacy'}),dict(component='main',**{'from':'mods/BaseLib/','to':'{game}/mods/BaseLib'})],'Melty installation mapping mismatch')
    require(melty.get('runtimeData')==['{localappdata}/SpireLegacy2'],'Unexpected Melty save mapping')
    return manifest,workshop,melty

def text(path):
    # Portable guides must be UTF-8; normalize only checkout line endings.
    try:return path.read_text(encoding='utf-8-sig').replace('\r\n','\n').encode('utf-8')
    except (OSError,UnicodeError) as error:raise ReleaseError(f'{path.name}: missing/invalid UTF-8 guide: {error}') from error

def payloads(root,expected_hash,baselib=None):
    manifest,workshop,melty=configuration(root);v=manifest['version']
    require(re.fullmatch('[a-f0-9]{64}',expected_hash or '') is not None,'Supply --expected-dll-sha256 from the release checks')
    dll=(root/'sts2/bin/Release/net9.0/SpireLegacy.dll').read_bytes()
    require(digest(dll)==expected_hash,'Release DLL differs from the expected checked binary')
    require(dll.startswith(b'MZ'),'Release DLL is not a PE assembly')
    tables=load_sheets(root);validate_assets(root,tables)
    require(next(r for r in tables['systems'] if r['id']=='playtest_crowns')['enabled'] is False,'Public release must disable the retired crown editor')
    try:provenance=inventory(root,tables)
    except ValueError as error:raise ReleaseError(str(error)) from error
    mod={'SpireLegacy.dll':dll,'SpireLegacy.json':(root/'sts2/SpireLegacy.json').read_bytes(),'LICENSE':text(root/'LICENSE'),'licenses/BaseLib-MIT.txt':text(root/'licenses/BaseLib/LICENSE.txt'),'README.md':text(root/'docs/sts2-port.md'),'release-notes.md':text(root/'docs'/f'release-{v}.md'),
         'asset-provenance.json':(json.dumps(provenance,indent=2)+'\n').encode('utf-8')}
    for name in GUIDES:mod[name+'.md']=text(root/'docs'/f'{name}.md')
    # Package every declared original asset, never an arbitrary staging directory.
    for table in ['card_art','ui_art','icon_art','character_art','relic_art','potion_art','presentation_art']:
        for row in tables[table]:
            parts=[row]+([row['death']] if isinstance(row.get('death'),dict) else [])
            for part in parts:
                for field in ['path','outline']:
                    if field in part:
                        name=safe_name(part[field]);require(name not in mod,'Duplicate asset '+name);mod[name]=(root/'src/main/resources'/name).read_bytes()
    # Close the local guide graph. Source/evidence links remain repository links;
    # those files are not needed to install the mod and may describe private labs.
    pending=[name for name in mod if name.endswith('.md')]
    while pending:
        name=pending.pop()
        for link in re.findall(r'\]\(([^)]+)\)',mod[name].decode('utf-8')):
            target=link.split('#')[0]
            if target.endswith('.md') and '/' not in target and target not in mod:
                mod[target]=text(root/'docs'/target);pending.append(target)
    for name,data in list(mod.items()):
        if name.endswith('.md'):
            def source_link(match):
                target=match.group(1);filename=target.split('#')[0]
                if filename and not re.match(r'https?://',filename) and (filename.startswith('../') or filename not in mod):
                    relative=posixpath.normpath('docs/'+filename)
                    require(not relative.startswith('../') and (root/relative).is_file(),'Missing source guide target '+target)
                    require(not relative.startswith(('private/','build/','dist/')),'Guide links private output '+target)
                    suffix='#'+target.split('#',1)[1] if '#' in target else ''
                    return '](https://github.com/CapeKid/SpireLegacy/blob/main/'+quote(relative)+suffix+')'
                return match.group(0)
            mod[name]=re.sub(r'\]\(([^)]+)\)',source_link,data.decode('utf-8')).encode('utf-8')
    check_links(mod)
    base=Path(baselib) if baselib else Path.home()/'.nuget/packages/alchyr.sts2.baselib/3.4.7'
    base_files={name:(base/sub/name).read_bytes() for name,sub in [('BaseLib.dll','lib/net9.0'),('BaseLib.json','Content'),('BaseLib.pck','Content')]}
    base_manifest=read_json(base/'Content/BaseLib.json')
    require(base_manifest.get('id')=='BaseLib' and base_manifest.get('version','').lstrip('v')=='3.4.7','Wrong bundled BaseLib version')
    require(base_manifest.get('has_dll') is True and base_manifest.get('has_pck') is True,'Incomplete BaseLib bundle')
    image=io.BytesIO()
    with Image.open(root/'src/main/resources/heir/ui/select-bg.png') as source:
        source.convert('RGB').resize((640,400),Image.Resampling.LANCZOS).save(image,format='PNG',optimize=True)
    require(len(image.getvalue())<1_000_000,'Workshop preview exceeds 1MB')
    standalone={'mods/SpireLegacy/'+name:data for name,data in mod.items()}
    workspace={'content/SpireLegacy/'+name:data for name,data in mod.items()}
    workspace.update({'workshop.json':(root/'sts2/workshop.json').read_bytes(),'README.md':mod['README.md'],'traits.md':mod['traits.md'],'image.png':image.getvalue()})
    bundle=dict(standalone);bundle.update({'mods/BaseLib/'+name:data for name,data in base_files.items()});bundle['melty.json']=(root/'sts2/melty.json').read_bytes()
    for files in (standalone,workspace,bundle):exclude_historical_files(files)
    return v,dict(standalone=standalone,workshop=workspace,melty=bundle)

def check_links(mod):
    for name,data in mod.items():
        if not name.endswith('.md'):continue
        for link in re.findall(r'\]\(([^)]+)\)',data.decode('utf-8')):
            if link.startswith('#') or re.match(r'https?://',link):continue
            target=link.split('#')[0]
            require(target in mod,f'{name}: missing bundled guide/link {target}')

def write_archive(path,files):
    with zipfile.ZipFile(path,'w',zipfile.ZIP_DEFLATED,compresslevel=6) as output:
        for name,data in sorted(files.items()):
            entry=zipfile.ZipInfo(safe_name(name));entry.create_system=3;entry.compress_type=zipfile.ZIP_DEFLATED;entry.external_attr=0o100644<<16
            output.writestr(entry,data)
    validate_archive(path,files)

def validate_archive(path,expected):
    try:return _validate_archive(path,expected)
    except (zipfile.BadZipFile,RuntimeError) as error:raise ReleaseError(f'{path.name}: malformed archive: {error}') from error

def _validate_archive(path,expected):
    with zipfile.ZipFile(path) as archive:
        infos=archive.infolist();names=[safe_name(item.filename) for item in infos]
        require(len(names)==len(set(names))==len({name.casefold() for name in names}),'Duplicate/case-colliding archive entries')
        require(set(names)==set(expected),'Archive contains unexpected/missing files')
        for item in infos:
            require(item.create_system==3 and item.external_attr>>16==0o100644,'Unexpected executable/symlink/file permissions: '+item.filename)
            require(not item.flag_bits&1 and item.file_size==len(expected[item.filename]),'Encrypted or wrong-size archive entry')
            require(archive.read(item)==expected[item.filename],'Archive content differs: '+item.filename)
        require(archive.testzip() is None,'Archive CRC failure')
    return dict(file=path.name,size=path.stat().st_size,sha256=digest(path.read_bytes()),files=len(expected))
