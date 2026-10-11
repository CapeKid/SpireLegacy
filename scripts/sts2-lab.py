"""Create a private, independently copied owned-game oracle.

Launch only with HEIR_TEST_MODE=1: the mod replaces the Steam Cloud save backend.
"""
import argparse, json, os, pathlib, shutil, re, hashlib

repo = pathlib.Path(__file__).resolve().parents[1]
parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('--configuration',choices=['Debug','Release'],default='Debug')
parser.add_argument('--lab-directory',default='private/sts2-lab')
parser.add_argument('--user-directory',default='SpireLegacy2PortLab')
parser.add_argument('--workshop-directory',default=os.environ.get('STS2_WORKSHOP_DIR',r'C:\Program Files (x86)\Steam\steamapps\workshop\content\2868840'),help='Actual Steam Workshop folder, including when the source game is a private snapshot.')
args=parser.parse_args()
configuration=args.configuration
game = pathlib.Path(os.environ.get('STS2_DIR', r'C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2'))
game = game.resolve()
lab = (repo / args.lab_directory).resolve()
if not lab.is_relative_to((repo / 'private').resolve()): raise SystemExit('Lab must stay inside the private workspace.')
if lab==game or lab.is_relative_to(game) or game.is_relative_to(lab):raise SystemExit('Lab and source game must not overlap.')
if not re.fullmatch(r'SpireLegacy2PortLab[A-Za-z0-9_-]*',args.user_directory):raise SystemExit('Use a dedicated SpireLegacy2PortLab user directory with a plain suffix.')
if lab.exists():raise SystemExit('Use a new lab directory; existing labs may contain hardlinks or stale game versions.')
workshop=pathlib.Path(args.workshop_directory).resolve()
if not workshop.is_dir():raise SystemExit('Specify the actual existing --workshop-directory; an unknown mod set is not an isolated baseline.')
lab.mkdir(parents=True, exist_ok=False)
for source in game.iterdir():
    if source.name in ('mods', 'mods_STEAMTEST', 'steam_appid.txt', 'override.cfg'): continue
    target = lab / source.name
    if source.is_dir():
        for root, dirs, files in os.walk(source):
            dest = target / pathlib.Path(root).relative_to(source)
            dest.mkdir(parents=True, exist_ok=True)
            for name in files:
                shutil.copy2(pathlib.Path(root) / name, dest / name)
    else: shutil.copy2(source, target)
(lab / 'steam_appid.txt').write_text('2868840', encoding='utf-8')
(lab / 'override.cfg').write_text(f'[application]\nconfig/use_custom_user_dir=true\nconfig/custom_user_dir_name="{args.user_directory}"\n[display]\nwindow/size/mode=0\n', encoding='utf-8')
mod = lab / 'mods/SpireLegacy'
mod.mkdir(parents=True, exist_ok=True)
shutil.copy2(repo / f'sts2/bin/{configuration}/net9.0/SpireLegacy.dll', mod)
shutil.copy2(repo / 'sts2/SpireLegacy.json', mod)
(mod/'content-reader.zip').unlink(missing_ok=True)
resources = repo / 'src/main/resources/heir'
for source in resources.rglob('*'):
    if source.suffix not in ('.png', '.svg', '.wav', '.tres', '.tscn'): continue
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
disabled = []
for path in workshop.rglob('*.json'):
    try:
        data = json.loads(path.read_text(encoding='utf-8-sig'))
        if 'id' in data and 'has_dll' in data: disabled.append(dict(id=data['id'],source=2,is_enabled=False))
    except (ValueError, OSError): pass
settings = dict(schema_version=5,fullscreen=False,window_size=dict(x=1280,y=800),language='eng',mod_settings=dict(mods_enabled=True,mod_list=disabled + [dict(id='BaseLib',source=1,is_enabled=True),dict(id='SpireLegacy',source=1,is_enabled=True)]))
# Only the unique lab account receives settings; the normal StS2 profile is untouched.
userdata_root = pathlib.Path(os.environ['APPDATA']) / args.user_directory
steam_userdata=workshop.parents[3] / 'userdata'
accounts=['default/1','steam/76561197992561100']
if steam_userdata.is_dir():
    accounts += ['steam/'+str(76561197960265728+int(p.name)) for p in steam_userdata.iterdir() if p.is_dir() and p.name.isdecimal()]
for account in set(accounts):
    userdata = userdata_root / account
    userdata.mkdir(parents=True, exist_ok=True)
    (userdata / 'settings.save').write_text(json.dumps(settings), encoding='utf-8')
print(f'Lab: {lab}; disabled {len(disabled)} Workshop manifests; isolated settings: {userdata}')
print(f'Launch with HEIR_TEST_MODE=1, HEIR_TEST_ROOT={repo / "private"}, and a fresh HEIR_DATA_DIR inside that root.')
(lab/'lab-provenance.json').write_text(json.dumps(dict(sourceGame=str(game),workshopDirectory=str(workshop),disabledWorkshopMods=disabled,independentCopies=True,userDirectory=args.user_directory,dllSha256=hashlib.sha256((mod/'SpireLegacy.dll').read_bytes()).hexdigest()),indent=2),encoding='utf-8')
