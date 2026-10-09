"""Create a private, hardlinked owned-game oracle. Never edits the installed game.

Launch only with HEIR_TEST_MODE=1: the mod replaces the Steam Cloud save backend.
"""
import argparse, json, os, pathlib, shutil

repo = pathlib.Path(__file__).resolve().parents[1]
parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('--configuration',choices=['Debug','Release'],default='Debug')
configuration=parser.parse_args().configuration
game = pathlib.Path(os.environ.get('STS2_DIR', r'C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2'))
lab = repo / 'private/sts2-lab'
lab.mkdir(parents=True, exist_ok=True)
for source in game.iterdir():
    if source.name in ('mods', 'mods_STEAMTEST', 'steam_appid.txt', 'override.cfg'): continue
    target = lab / source.name
    if source.is_dir():
        for root, dirs, files in os.walk(source):
            dest = target / pathlib.Path(root).relative_to(source)
            dest.mkdir(parents=True, exist_ok=True)
            for name in files:
                if not (dest / name).exists(): os.link(pathlib.Path(root) / name, dest / name)
    elif not target.exists(): os.link(source, target)
(lab / 'steam_appid.txt').write_text('2868840', encoding='utf-8')
(lab / 'override.cfg').write_text('[application]\nconfig/use_custom_user_dir=true\nconfig/custom_user_dir_name="SpireLegacy2PortLab"\n[display]\nwindow/size/mode=0\n', encoding='utf-8')
mod = lab / 'mods/SpireLegacy'
mod.mkdir(parents=True, exist_ok=True)
shutil.copy2(repo / f'sts2/bin/{configuration}/net9.0/SpireLegacy.dll', mod)
shutil.copy2(repo / 'sts2/SpireLegacy.json', mod)
reader_archive = repo / 'build/sts2/content-reader.zip'
if reader_archive.exists(): shutil.copy2(reader_archive, mod)
resources = repo / 'src/main/resources/heir'
for source in resources.rglob('*.png'):
    target = mod / 'heir' / source.relative_to(resources)
    target.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(source,target)
for name in ('assets.json','host_assets.json'):
    leftover = mod / 'heir' / name
    if leftover.exists(): leftover.unlink()
base = lab / 'mods/BaseLib'
base.mkdir(parents=True, exist_ok=True)
nuget = pathlib.Path.home() / '.nuget/packages/alchyr.sts2.baselib/3.4.7'
for src in [nuget / 'lib/net9.0/BaseLib.dll', nuget / 'Content/BaseLib.json', nuget / 'Content/BaseLib.pck']: shutil.copy2(src, base)
workshop = game.parent.parent / 'workshop/content/2868840'
disabled = []
for path in workshop.rglob('*.json'):
    try:
        data = json.loads(path.read_text(encoding='utf-8-sig'))
        if 'id' in data and 'has_dll' in data: disabled.append(dict(id=data['id'],source=2,is_enabled=False))
    except (ValueError, OSError): pass
settings = dict(schema_version=5,fullscreen=False,window_size=dict(x=1280,y=800),language='eng',mod_settings=dict(mods_enabled=True,mod_list=disabled + [dict(id='BaseLib',source=1,is_enabled=True),dict(id='SpireLegacy',source=1,is_enabled=True)]))
# Only the unique lab account receives settings; the normal StS2 profile is untouched.
userdata = pathlib.Path(os.environ['APPDATA']) / 'SpireLegacy2PortLab/steam/76561197992561100'
userdata.mkdir(parents=True, exist_ok=True)
(userdata / 'settings.save').write_text(json.dumps(settings), encoding='utf-8')
print(f'Lab: {lab}; disabled {len(disabled)} Workshop manifests; isolated settings: {userdata}')
