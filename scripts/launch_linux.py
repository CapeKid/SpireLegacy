"""Native Steam Deck/Linux launcher. No system installation or Proton required."""
import argparse,json,os,re,subprocess,sys
from pathlib import Path

def steam_libraries(steam):
    libraries=[steam]
    vdf=steam/'steamapps/libraryfolders.vdf'
    if vdf.is_file():
        libraries.extend(Path(p.replace('\\\\','\\')) for p in re.findall(r'"path"\s*"([^"]+)"',vdf.read_text(encoding='utf-8')))
    return list(dict.fromkeys(libraries))

def find_install(libraries,appid):
    for library in libraries:
        manifest=library/'steamapps'/f'appmanifest_{appid}.acf'
        if not manifest.is_file():continue
        match=re.search(r'"installdir"\s*"([^"]+)"',manifest.read_text(encoding='utf-8'))
        if match:
            directory=library/'steamapps/common'/match.group(1)
            if directory.is_dir():return directory
    raise RuntimeError(f'Steam app {appid} is not installed in the detected libraries.')

def plan(package,home,environ,host=None,game=None):
    candidates=[Path(environ['HEIR_STEAM_DIR'])] if environ.get('HEIR_STEAM_DIR') else [home/'.local/share/Steam',home/'.steam/steam',home/'.steam/root']
    steam=next((p for p in candidates if (p/'steamapps').is_dir()),None)
    if steam is None and (not host or not game):raise RuntimeError('Steam libraries were not found. Set HEIR_STEAM_DIR or pass --host and --game.')
    libraries=steam_libraries(steam) if steam else []
    host=Path(host) if host else package.parent if (package.parent/'desktop-1.0.jar').is_file() else find_install(libraries,646570)
    game=Path(game) if game else find_install(libraries,1253920)
    if not (host/'desktop-1.0.jar').is_file():raise RuntimeError('Slay the Spire desktop-1.0.jar is missing.')
    java=host/'jre/bin/java'
    if not java.is_file():raise RuntimeError('Native Linux Slay the Spire Java is missing. Disable forced Proton compatibility for StS1, then verify its files in Steam.')
    with java.open('rb') as binary:magic=binary.read(4)
    if magic!=b'\x7fELF':raise RuntimeError('StS1 has a Windows runtime. Install its native Linux version; keep the regular branch selected.')
    if not (game/'Rogue Legacy 2_Data/resources.assets').is_file():raise RuntimeError('The installed Rogue Legacy 2 content could not be found.')
    data=Path(environ.get('HEIR_DATA_DIR') or str(Path(environ.get('XDG_DATA_HOME',str(home/'.local/share')))/'HeirOfTheSpire'))
    for file in (package/'ModTheSpire.jar',host/'mods/HeirOfTheSpire.jar',host/'mods/BaseMod.jar'):
        if not file.is_file():raise RuntimeError(f'Required mod file is missing: {file}')
    env=dict(environ);env['HEIR_DATA_DIR']=str(data);env['SteamAppId']='646570';env['SteamGameId']='646570'
    return dict(host=str(host),game=str(game),data=str(data),reader=[sys.executable,str(package/'load_rl.py'),'--game',str(game),'--host',str(host),'--sheet',str(package/'assets.json'),'--cache',str(data/'cache'),'--no-dialog'],java=[str(java),'-Xmx1G','-jar',str(package/'ModTheSpire.jar'),'--skip-launcher','--skip-intro','--mods','basemod,heir'],env=env)

def main():
    parser=argparse.ArgumentParser();parser.add_argument('--host');parser.add_argument('--game');parser.add_argument('--check',action='store_true');args=parser.parse_args()
    try:
        value=plan(Path(__file__).resolve().parent,Path.home(),os.environ,args.host,args.game)
        if args.check:
            print(json.dumps({k:v for k,v in value.items() if k!='env'},indent=2));return 0
        data=Path(value['data']);data.mkdir(parents=True,exist_ok=True)
        with (data/'launch.log').open('w',encoding='utf-8') as log:
            result=subprocess.run(value['reader'],env=value['env'],stdout=log,stderr=subprocess.STDOUT)
            if result.returncode:raise RuntimeError(f'Local content preparation failed. See {data}/launch.log')
            return subprocess.call(value['java'],cwd=value['host'],env=value['env'],stdout=log,stderr=subprocess.STDOUT)
    except (OSError,RuntimeError) as exc:
        print('Cannot launch Spire Legacy:',exc,file=sys.stderr);return 1

if __name__=='__main__':raise SystemExit(main())
