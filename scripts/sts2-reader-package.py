"""Package the existing licensed content reader; game assets are never included."""
import argparse, pathlib, zipfile
repo = pathlib.Path(__file__).resolve().parents[1]
parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('--runtime-source',type=pathlib.Path,default=repo/'build/workshop/content/SpireLegacyRuntime',help='Licensed runtime staging directory containing Reader/, ReaderLinux/ and licenses/.')
source=parser.parse_args().runtime_source.resolve()
for required in ['Reader/ReadRogueLegacy.exe','ReaderLinux/bin/python3.12','licenses']:
    if not (source/required).exists():raise SystemExit('Missing licensed reader dependency: '+str(source/required))
output = repo / 'build/sts2/content-reader.zip'
output.parent.mkdir(parents=True, exist_ok=True)
with zipfile.ZipFile(output,'w',zipfile.ZIP_DEFLATED,compresslevel=6) as archive:
    for folder in ['Reader','ReaderLinux','licenses']:
        for path in sorted((source / folder).rglob('*')):
            if path.is_file():
                name=path.relative_to(source).as_posix()
                if any(token in name.lower() for token in ('source.json','family.json','assembly-csharp','slaythespire2','desktop-1.0','libfmod','fmod.dll')):raise ValueError('Forbidden reader file: '+name)
                entry=zipfile.ZipInfo(name);entry.create_system=3;entry.compress_type=zipfile.ZIP_DEFLATED
                executable=name.endswith('/bin/python3.12') or name.endswith('.so')
                entry.external_attr=(0o100755 if executable else 0o100644)<<16
                archive.writestr(entry,path.read_bytes())
    archive.write(repo / 'scripts/load_rl.py','load_rl.py')
    archive.write(repo / 'sheets/assets.json','assets.dat')
print(output, output.stat().st_size)
