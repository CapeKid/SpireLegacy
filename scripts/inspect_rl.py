from pathlib import Path
import json
import UnityPy

root = Path(r'C:\Program Files (x86)\Steam\steamapps\common\Rogue Legacy 2\Rogue Legacy 2_Data')
rows = []
for file in [root/'resources.assets', root/'sharedassets1.assets', root/'globalgamemanagers.assets']:
    env = UnityPy.load(str(file))
    for obj in env.objects:
        if obj.type.name in ('Sprite','Texture2D','TextAsset','MonoBehaviour'):
            try:
                data = obj.read()
                name = getattr(data, 'm_Name', '')
                if name:
                    row = {'file':file.name, 'type':obj.type.name, 'id':obj.path_id, 'name':name}
                    if obj.type.name == 'TextAsset':
                        text = data.m_Script
                        if isinstance(text, bytes): text = text.decode('utf-8', 'replace')
                        row['preview'] = text[:250]
                    rows.append(row)
            except Exception:
                pass
Path('private/rl_asset_index.json').write_text(json.dumps(rows,indent=2), encoding='utf-8')
for row in rows:
    if row['type']=='TextAsset' or any(s in row['name'].lower() for s in ['knight','mage','ranger','trait','class','manor','player','hero']):
        print(json.dumps(row))
print('Total named objects:',len(rows))
