"""Reproducible original potion silhouettes and white selection outlines."""
import hashlib,json,re
from pathlib import Path
from design_schema import load_sheets
root=Path(__file__).resolve().parents[1];out=root/'src/main/resources/heir/potions';out.mkdir(parents=True,exist_ok=True)
tables = load_sheets(root)
bodies={
 'borrowed_breath':'<path d="M49 16L79 16L79 45Q100 59 98 90Q98 112 64 114Q29 113 29 90Q27 61 49 45Z" fill="#9bcbd2"/><path d="M36 80Q64 66 92 80L91 98Q64 119 37 98Z" fill="#6eb8ad"/><path d="M47 11L81 11L81 24L47 24Z" fill="#c29462"/><path d="M48 61L79 61L76 83L64 92L52 83Z" fill="#e4c479"/><path d="M53 50L57 41M41 87L43 95" fill="none" stroke="#e7f5e8"/>',
 'briar_vial':'<path d="M46 20L82 20L82 41L99 87Q106 109 85 113L43 113Q23 109 30 87L46 41Z" fill="#a3c686"/><path d="M37 82L91 82L99 101L85 107L43 107L30 101Z" fill="#8d75b0"/><path d="M44 11L84 11L84 25L44 25Z" fill="#9c6b51"/><path d="M45 65Q64 43 83 65M49 68L55 53M66 57L73 44M77 65L85 56" fill="none" stroke="#56765a"/><path d="M54 89L58 100M70 91L76 101" fill="none" stroke="#dab9df"/>',
 'triune_draught':'<path d="M47 21L81 21L81 40L99 57L95 97L80 113L48 113L33 97L29 57L47 40Z" fill="#afbdd5"/><path d="M36 72L92 72L89 95L77 106L51 106L39 95Z" fill="#8b77b8"/><path d="M44 11L84 11L84 27L44 27Z" fill="#d9b36a"/><path d="M47 56L81 56L79 79L64 93L49 79Z" fill="#e7c583"/><path d="M64 58L64 84M50 72L78 72" fill="none" stroke="#775e89"/><circle cx="55" cy="63" r="3" fill="#9ac6c6"/><circle cx="73" cy="63" r="3" fill="#db8f9b"/>',
}
rows=[]
for key,body in bodies.items():
    svg='<svg xmlns="http://www.w3.org/2000/svg" width="128" height="128" viewBox="0 0 128 128"><g stroke="#302b36" stroke-width="5" stroke-linejoin="round" stroke-linecap="round">'+body+'</g></svg>\n'
    path=out/(key+'.svg');path.write_text(svg,encoding='utf-8', newline='\n')
    outline=re.sub(r'#[0-9a-fA-F]{6}','#ffffff',svg).replace('stroke-width="5"','stroke-width="9"');op=out/(key+'-outline.svg');op.write_text(outline,encoding='utf-8', newline='\n')
    rows.append(dict(id=key,path=path.relative_to(root/'src/main/resources').as_posix(),sha256=hashlib.sha256(path.read_bytes()).hexdigest(),outline=op.relative_to(root/'src/main/resources').as_posix(),outlineSha256=hashlib.sha256(op.read_bytes()).hexdigest(),author='CapeKid',license='MIT',provenance='Original hand-authored vector geometry; scripts/author-heir-potions.py; no game-derived or external assets.'))
(root/'sheets/potion_art.json').write_text(json.dumps(rows,indent=2)+'\n',encoding='utf-8', newline='\n')
print('Authored three original potion icons and selection outlines.')
