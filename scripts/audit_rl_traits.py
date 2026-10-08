"""Inspect the installed trait catalog locally; never distribute extracted data."""
import json,sys,types,argparse
from pathlib import Path
sys.modules['fmod_toolkit']=types.ModuleType('fmod_toolkit')
import UnityPy
from UnityPy.helpers.TypeTreeGenerator import TypeTreeGenerator
p=argparse.ArgumentParser();p.add_argument('--game',default='C:/Program Files (x86)/Steam/steamapps/common/Rogue Legacy 2');args=p.parse_args()
root=Path(args.game);data=root/'Rogue Legacy 2_Data';bundles=data/'StreamingAssets/AssetBundles'
env=UnityPy.load(*[str(path) for path in data.glob('*.assets')],*[str(bundles/name) for name in ('prefabs','scriptable_objects','languages')])
generator=TypeTreeGenerator('2020.3.47f1');generator.load_local_game(str(root));env.typetree_generator=generator
rows=[]
for obj in env.objects:
    if obj.type.name=='TextAsset':
        text=obj.read()
        if text.m_Name=='English_tsv':
            payload=text.m_Script
            if isinstance(payload,bytes):payload=payload.decode('utf-8')
            Path('private/rl-english.tsv').write_text(payload,encoding='utf-8')
    if obj.type.name!='MonoBehaviour':continue
    try:
        head=obj.parse_monobehaviour_head();script=head.m_Script.deref_parse_as_object();name=script.m_ClassName
        if name in ('TraitData','TraitLibrary','TraitTypeSpriteDictionary','LanguageSourceAsset') or name.endswith('_Trait'):
            rows.append({'file':obj.assets_file.name,'id':obj.path_id,'class':name,'data':obj.read_typetree()})
    except Exception as exc:
        print('Skipped trait inspection object',obj.path_id,type(exc).__name__,file=sys.stderr)
out=Path('private/rl-traits.json');out.write_text(json.dumps(rows,indent=2))
for row in rows:
    if row['class']=='TraitData':print(row['id'],row['data'].get('Name'),row['data'].get('Rarity'),row['data'].get('Title'))
print('Inspected',len(rows),'trait objects; private output:',out)
