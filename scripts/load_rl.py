"""Read only selected sprites from the player's own Rogue Legacy 2 installation."""
import argparse, hashlib, json, os, re, sys, tempfile, zipfile, io
import types
from pathlib import Path
# This reader handles sprites only. Prevent UnityPy's optional audio exporter
# from loading FMOD, which is unnecessary and is not bundled in this release.
sys.modules['fmod_toolkit'] = types.ModuleType('fmod_toolkit')
import UnityPy
from PIL import Image

def find_game(explicit=None):
    if explicit:
        root=Path(explicit)
        if (root/'Rogue Legacy 2_Data/resources.assets').is_file(): return root
        raise RuntimeError('The supplied Rogue Legacy 2 folder is not a valid installation.')
    steam_roots=[]
    try:
        import winreg
        with winreg.OpenKey(winreg.HKEY_CURRENT_USER,r'Software\Valve\Steam') as key:
            steam_roots.append(Path(winreg.QueryValueEx(key,'SteamPath')[0]))
    except OSError: pass
    steam_roots += [Path(os.environ.get('PROGRAMFILES(X86)',r'C:\Program Files (x86)'))/'Steam',Path(r'C:\Steam')]
    libs=list(steam_roots)
    for root in steam_roots:
        vdf=root/'steamapps/libraryfolders.vdf'
        if vdf.is_file():
            libs.extend(Path(s.replace('\\\\','\\')) for s in re.findall(r'"path"\s*"([^"]+)"',vdf.read_text(encoding='utf-8')))
    for lib in libs:
        manifest=lib/'steamapps/appmanifest_1253920.acf'
        if manifest.is_file():
            match=re.search(r'"installdir"\s*"([^"]+)"',manifest.read_text())
            if match:
                root=lib/'steamapps/common'/match.group(1)
                if (root/'Rogue Legacy 2_Data/resources.assets').is_file(): return root
    raise RuntimeError('Rogue Legacy 2 is required. Install your own Steam copy, then press Play again. No game content is included with this mod.')

def host_assets(host,cache,sheet):
    if not host: return
    with zipfile.ZipFile(Path(host)/'desktop-1.0.jar') as jar:
        atlas=jar.read('cardui/cardui.atlas').decode()
        blocks={};page=None;lines=atlas.splitlines();i=0
        while i<len(lines):
            line=lines[i]
            if line.endswith('.png') and not line.startswith(' '): page=line
            if line and not line.startswith(' ') and i+1<len(lines) and lines[i+1].startswith('  rotate:'):
                props={};j=i+1
                while j<len(lines) and lines[j].startswith('  '):
                    key,value=lines[j].strip().split(':',1);props[key]=value.strip();j+=1
                blocks[line]=(page,props)
            i+=1
        images={}
        for row in json.loads(Path(sheet).with_name('host_assets.json').read_text()):
            key=row['sourceName'];page,p=blocks[key]
            if p['rotate']!='false': raise RuntimeError('Unsupported rotated host card asset '+key)
            if page not in images: images[page]=Image.open(io.BytesIO(jar.read('cardui/'+page))).convert('RGBA')
            x,y=map(int,p['xy'].split(','));w,h=map(int,p['size'].split(','));ox,oy=map(int,p['offset'].split(','));ow,oh=map(int,p['orig'].split(','))
            crop=images[page].crop((x,y,x+w,y+h));canvas=Image.new('RGBA',(ow,oh));canvas.alpha_composite(crop,(ox,oh-oy-h))
            if row['resize']:canvas=crop.resize((row['resize'],row['resize']),Image.Resampling.LANCZOS)
            canvas.save(cache/row['output'])

def run(game,sheet,cache,host=None):
    root=find_game(game); cache=Path(cache); cache.mkdir(parents=True,exist_ok=True)
    host_assets(host,cache,sheet)
    rows=json.loads(Path(sheet).read_text())
    required=[cache/r['output'] for r in rows]+[cache/'button.png',cache/'portrait.png',cache/'hero.png']
    source=root/'Rogue Legacy 2_Data/resources.assets'
    fingerprint='sprite-layout-v2:'+hashlib.sha256(source.read_bytes()).hexdigest()
    fingerprint+=hashlib.sha256(source.with_suffix('.assets.resS').read_bytes()).hexdigest()
    fingerprint+=hashlib.sha256(Path(sheet).read_bytes()).hexdigest()
    signature=cache/'source.json'
    if signature.is_file():
        previous=json.loads(signature.read_text())
        if previous.get('fingerprint')==fingerprint and all(p.is_file() for p in required):
            print('Rogue Legacy 2 content cache verified:',root); return
    wanted={r['sourceName']:r for r in rows}; images={}
    for file in {r['sourceFile'] for r in rows}:
        env=UnityPy.load(str(root/'Rogue Legacy 2_Data'/file))
        for obj in env.objects:
            if obj.type.name=='Sprite':
                sprite=obj.read()
                if sprite.m_Name in wanted:
                    images[sprite.m_Name]=sprite.image.convert('RGBA')
    missing=set(wanted)-set(images)
    if missing: raise RuntimeError('This Rogue Legacy 2 version lacks required sprites: '+', '.join(sorted(missing)))
    for name,image in images.items():
        image.save(cache/wanted[name]['output'])
        # Card illustrations are derived at runtime; never included in a release.
        canvas=Image.new('RGBA',(250,190),(18,28,43,255))
        icon=image.copy(); icon.thumbnail((120,120),Image.Resampling.NEAREST)
        scale=max(1,min(120//icon.width,120//icon.height)); icon=icon.resize((icon.width*scale,icon.height*scale),Image.Resampling.NEAREST)
        canvas.alpha_composite(icon,((250-icon.width)//2,(190-icon.height)//2))
        canvas.save(cache/(name+'_card.png'))
    hero=images['KnightEmote']; hero=hero.resize((hero.width*8,hero.height*8),Image.Resampling.NEAREST); hero.save(cache/'hero.png')
    button=Image.new('RGBA',(128,128)); icon=images['Icons_Classes_SwordClass'].resize((96,96),Image.Resampling.NEAREST); button.alpha_composite(icon,(16,16)); button.save(cache/'button.png')
    portrait=Image.new('RGBA',(1920,1200),(13,22,36,255)); big=images['KnightEmote'].resize((480,640),Image.Resampling.NEAREST); portrait.alpha_composite(big,(720,280)); portrait.save(cache/'portrait.png')
    signature.write_text(json.dumps({'game':str(root),'fingerprint':fingerprint,'assets':sorted(images)},indent=2))
    print('Read',len(images),'genuine Rogue Legacy 2 sprites from',root)

if __name__=='__main__':
    parser=argparse.ArgumentParser(); parser.add_argument('--game');parser.add_argument('--host');parser.add_argument('--sheet',required=True);parser.add_argument('--cache',required=True);parser.add_argument('--no-dialog',action='store_true');args=parser.parse_args()
    try: run(args.game,args.sheet,args.cache,args.host)
    except Exception as exc:
        print('Cannot prepare Spire Legacy:',exc,file=sys.stderr)
        if getattr(sys,'frozen',False) and not args.no_dialog:
            import ctypes
            ctypes.windll.user32.MessageBoxW(0,str(exc),'Spire Legacy — Rogue Legacy 2 needed',0x10)
        raise SystemExit(1)
