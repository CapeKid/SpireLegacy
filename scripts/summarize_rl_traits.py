"""Summarize a private, installed-game trait audit without exporting game text."""
import json,re
from pathlib import Path
root=Path(__file__).resolve().parents[1]
rows=json.loads((root/'private/rl-traits.json').read_text())
objects={r['id']:r for r in rows}
english={}
for line in (root/'private/rl-english.tsv').read_text(encoding='utf-8-sig').splitlines():
    parts=line.split('\t')
    if len(parts)>1:english[parts[0]]=parts[1]
enum={name:int(value) for name,value in re.findall(r'(\w+) = (\d+)',(root/'private/trait-source/TraitType.decompiled.cs').read_text())}
aliases={'BreaksPropsForMana':'BreakPropsForMana','Gay':'Disposition'}
catalog=[]
for ref in next(r for r in rows if r['class']=='TraitLibrary')['data']['m_traitLibrary']:
    component=objects[ref['m_PathID']];c=component['data']
    data=objects[c['m_traitData']['m_PathID']]['data']
    source=component['class'].removesuffix('_Trait');source=aliases.get(source,source)
    number=c.get('m_traitType',enum.get(source))
    source=next(k for k,v in enum.items() if v==number)
    title=data['Title'];friendly=english.get(title.replace('_1','_2'),english.get(title,data['Name']))
    catalog.append(dict(source=source,number=number,name=friendly,rarity=data['Rarity'],gold=data['GoldBonus'],excludes=c.get('m_incompatibleTraits',[])))
catalog.sort(key=lambda r:r['number'])
(root/'private/trait-catalog.json').write_text(json.dumps(catalog,indent=2),encoding='utf-8')
for r in catalog:print(r['source'],repr(r['name']),r['rarity'],r['gold'],r['excludes'])
print('Library entries:',len(catalog))
