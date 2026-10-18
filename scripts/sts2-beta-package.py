"""Create a separate-ID private beta workspace from the validated payload; upload nothing."""
import argparse,json
from pathlib import Path
from release_contract import payloads,write_archive,digest,ReleaseError,ITEM_ID

repo=Path(__file__).resolve().parents[1]
def build(output,expected_hash):
    output=Path(output).resolve()
    if output.exists():raise ReleaseError('Use a fresh beta output directory; preserve existing uploader mod_id.txt for updates.')
    config=json.loads((repo/'sts2/workshop-beta.json').read_text(encoding='utf-8'))
    if config.get('visibility')!='private':raise ReleaseError('Beta Workshop visibility must be private.')
    v,formats=payloads(repo,expected_hash)
    files={}
    for name,data in formats['standalone'].items():
        relative=name.removeprefix('mods/SpireLegacy/')
        if relative=='SpireLegacy.json':
            manifest=json.loads(data);manifest.update(id='SpireLegacyBeta',name='Spire Legacy — Beta',description='Private beta. Enable this OR Spire Legacy, then restart; never enable both.')
            relative='SpireLegacyBeta.json';data=(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n').encode()
        elif relative=='SpireLegacy.dll':relative='SpireLegacyBeta.dll'
        if relative in ('README.md','mage-private-test.md'):data=(repo/'docs/beta-workshop.md').read_bytes()
        files[relative]=data
    if digest(files['SpireLegacyBeta.dll'])!=expected_hash:raise ReleaseError('Renaming changed the validated DLL.')
    workspace=output/'workshop';workspace.mkdir(parents=True)
    for relative,data in files.items():
        target=workspace/'content/SpireLegacyBeta'/relative;target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(data)
    (workspace/'workshop.json').write_text(json.dumps(config,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    (workspace/'image.png').write_bytes(formats['workshop']['image.png'])
    (workspace/'README.md').write_bytes((repo/'docs/beta-workshop.md').read_bytes())
    archive=output/f'SpireLegacyBeta-StS2-{v}.zip';write_archive(archive,{'mods/SpireLegacyBeta/'+n:d for n,d in files.items()})
    receipt=dict(version=v,modId='SpireLegacyBeta',visibility='private',dllSha256=expected_hash,archive=archive.name,archiveSha256=digest(archive.read_bytes()),files=len(files),publicItemNeverTarget=ITEM_ID,workshopItemId=None,uploaded=False)
    (output/'inspection.json').write_text(json.dumps(receipt,indent=2)+'\n')
    return receipt
if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--output-root',type=Path,required=True);parser.add_argument('--expected-dll-sha256',required=True);args=parser.parse_args()
    try:print(json.dumps(build(args.output_root,args.expected_dll_sha256),indent=2))
    except (ValueError,OSError) as error:raise SystemExit(str(error))
