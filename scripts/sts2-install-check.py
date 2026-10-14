"""Read-only mod-copy diagnostics; never install, move or alter saves/settings."""
import argparse,json,re
from pathlib import Path
from release_contract import digest

def inspect(game,workshop=None,expected_version=None,expected_hash=None):
    game=Path(game).resolve()
    if workshop is None:
        steamapps=next((p for p in game.parents if p.name.casefold()=='steamapps'),None)
        workshop=steamapps/'workshop/content/2868840' if steamapps else None
    roots=[('manual',game/'mods')]+([('workshop',Path(workshop).resolve())] if workshop else [])
    copies=[];errors=[]
    for origin,root in roots:
        if not root.exists():continue
        for path in sorted(root.rglob('*.json')):
            try:data=json.loads(path.read_text(encoding='utf-8-sig'))
            except (OSError,ValueError):continue  # Non-manifest data is also present in mod trees.
            if not isinstance(data,dict) or data.get('id') not in {'SpireLegacy','BaseLib'}:continue
            dll=path.parent/(data['id']+'.dll');pck=path.parent/(data['id']+'.pck')
            entry=dict(id=data['id'],version=data.get('version'),origin=origin,manifest=str(path),directory=str(path.parent),dllSha256=digest(dll.read_bytes()) if dll.is_file() else None)
            copies.append(entry)
            if data.get('has_dll') is True and not dll.is_file():errors.append(f'{path}: required DLL missing')
            if data.get('has_pck') is True and not pck.is_file():errors.append(f'{path}: required PCK missing')
    for identity in ['SpireLegacy','BaseLib']:
        matches=[c for c in copies if c['id']==identity]
        if not matches:errors.append(identity+' is not installed; install/enable it before launching')
        if len(matches)>1:
            errors.append(identity+' has multiple copies. An enabled local mods copy takes precedence over Workshop. Close the game and move the unwanted manual directory outside the game mods and Workshop roots; renaming it inside those roots does not stop recursive discovery. Keep saves outside these mod folders unchanged.')
        for entry in matches:
            if identity=='SpireLegacy' and expected_version and entry['version']!=expected_version:errors.append(f'{entry["manifest"]}: expected Spire Legacy {expected_version}, found {entry["version"]}')
            if identity=='SpireLegacy' and expected_hash and entry['dllSha256']!=expected_hash:errors.append(f'{entry["manifest"]}: DLL differs from the expected release')
            if identity=='BaseLib':
                match=re.fullmatch(r'v?(\d+)\.(\d+)\.(\d+)',str(entry['version']))
                if not match or tuple(map(int,match.groups()))<(3,4,7):errors.append(f'{entry["manifest"]}: Spire Legacy requires BaseLib 3.4.7 or newer; update the dependency and remove/disable the old manual copy')
    return dict(game=str(game),workshop=str(workshop) if workshop else None,copies=copies,errors=errors,ok=not errors,readOnly=True)

def main():
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--game',required=True,type=Path);parser.add_argument('--workshop',type=Path);parser.add_argument('--expected-version');parser.add_argument('--expected-dll-sha256');args=parser.parse_args()
    result=inspect(args.game,args.workshop,args.expected_version,args.expected_dll_sha256);print(json.dumps(result,indent=2));return 0 if result['ok'] else 1
if __name__=='__main__':raise SystemExit(main())
