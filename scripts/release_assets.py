"""Derived provenance inventory of the closed native asset inputs, not legal clearance."""
import hashlib
from pathlib import PurePosixPath

TABLES=('card_art','ui_art','character_art','icon_art','relic_art','potion_art','presentation_art')
GUIDES={'card_art':'README.md','ui_art':'docs/heir-portrait.md','character_art':'docs/character-sprites.md','icon_art':'docs/original-icons.md',
        'relic_art':'docs/heir-relics.md','potion_art':'docs/heir-potions.md','presentation_art':'docs/heir-presentation.md'}

def inventory(root,tables):
    retired={row['output'].casefold() for row in tables['assets']}
    rows=[]
    for table in TABLES:
        guide=GUIDES[table]
        if not (root/guide).is_file():raise ValueError('Missing asset provenance guide: '+guide)
        for row in tables[table]:
            if row.get('license','MIT')!='MIT':raise ValueError('Unreviewed asset license: '+row['id'])
            if not (row.get('generator') or row.get('provenance') or table=='character_art'):
                raise ValueError('Missing asset provenance: '+row['id'])
            parts=[(row,row['id'])]+([(row['death'],row['id']+'.death')] if isinstance(row.get('death'),dict) else [])
            for part,subject in parts:
                for field,hashfield in [('path','sha256'),('outline','outlineSha256')]:
                    if field not in part:continue
                    path=part[field]
                    if PurePosixPath(path).name.casefold() in retired:raise ValueError('Retired extracted asset in native release: '+path)
                    digest=hashlib.sha256((root/'src/main/resources'/path).read_bytes()).hexdigest()
                    if digest!=part[hashfield]:raise ValueError('Asset hash mismatch: '+path)
                    record=row.get('artDirection') or row.get('provenance') or (root/guide).read_text(encoding='utf-8')
                    rows.append(dict(path=path,sha256=digest,table=table,subject=subject,record='sheets/'+table+'.json',
                        author=row.get('author','CapeKid (repository declaration)'),license=row.get('license','MIT (repository LICENSE)'),
                        method=row.get('generator') or row.get('provenance') or 'Generation and atlas assembly documented in '+guide,
                        repositoryGuide=guide,recordSha256=hashlib.sha256(record.encode('utf-8')).hexdigest(),
                        recordPurpose='current art direction' if 'artDirection' in row else 'authoring provenance',
                        gameStyleReferenceDeclared=row.get('gameStyleReferenceDeclared',any(name in record.lower() for name in ('game style','game-style','private game')))))
    if len(rows)!=len({r['path'].casefold() for r in rows}):raise ValueError('Duplicate provenance asset path')
    return dict(scope='Declared native mod assets only; excludes historical source and private reference files.',
        assetCount=len(rows),assets=rows,aiArtworkDisclosed=True,
        limits=['Hashes verify the declared shipped bytes, not independent authorship or ownership.',
                'Some illustrations used private game style/framing references; reference files are not package inputs.',
                'Technical provenance and package checks do not establish legal clearance.'])
