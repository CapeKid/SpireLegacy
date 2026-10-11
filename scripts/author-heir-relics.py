"""Reproducible original relic silhouettes, with separate white selection outlines."""
import hashlib,json,re
from pathlib import Path
root=Path(__file__).resolve().parents[1]
out=root/'src/main/resources/heir/relics';out.mkdir(parents=True,exist_ok=True)
symbols={
 'heirloom_brooch':'<path d="M35 22L93 22L102 44L88 105L40 105L26 44Z" fill="#e7bc66"/><path d="M44 40Q64 26 84 40L81 80L64 96L47 80Z" fill="#7cc6d9"/><path d="M53 62L64 48L75 62L64 76Z" fill="#e6f2ed"/><circle cx="35" cy="30" r="6" fill="#eeede1"/><circle cx="93" cy="30" r="6" fill="#eeede1"/>',
 'training_dummy':'<path d="M60 16L69 16L69 111L60 111Z" fill="#a36f46"/><path d="M20 54L107 54L107 66L20 66Z" fill="#c69762"/><path d="M36 24Q64 11 93 25L86 89Q64 101 42 89Z" fill="#dfb88a"/><path d="M39 44L90 44M43 78L85 78" fill="none"/><circle cx="64" cy="62" r="17" fill="#c77366"/><circle cx="64" cy="62" r="7" fill="#edcd9a"/><path d="M42 112L89 112" fill="none"/>',
 'threadbound_ledger':'<path d="M25 25L94 20Q105 21 104 34L104 100L36 106Q23 105 23 94Z" fill="#946546"/><path d="M39 25L40 96L99 92L99 26Z" fill="#7d6bb5"/><path d="M25 95Q33 88 43 94L103 89L103 101L39 111Q25 112 25 102Z" fill="#eddfb7"/><path d="M71 21L84 21L84 63L77 57L71 66Z" fill="#d86e6c"/><path d="M48 45L63 42M48 59L63 56M48 76L89 72" fill="none" stroke="#e5c887"/>',
 'wayfarer_ribbon':'<path d="M55 57L31 92L17 94L30 108L66 66L96 111L100 94L114 91L79 57Z" fill="#75b397"/><path d="M62 47C21 11 6 27 22 61Q43 75 63 56C110 79 121 43 104 23Q85 15 62 47Z" fill="#79b8df"/><path d="M29 39Q42 41 52 51M83 52Q97 42 102 34" fill="none"/><path d="M54 42L72 42L77 61L54 65L49 54Z" fill="#e9bd69"/>',
 'triune_crest':'<path d="M27 24Q64 12 101 24L95 73Q86 97 64 114Q39 97 33 73Z" fill="#e0b461"/><path d="M36 31Q64 22 92 31L87 69Q80 88 64 101Q46 88 42 70Z" fill="#8075ac"/><path d="M64 28L64 80M41 64L87 64" fill="none" stroke="#e7ce96"/><path d="M45 38L49 34L57 42L52 47Z" fill="#cde4e2"/><path d="M44 47L52 39M44 47L42 51" fill="none" stroke="#d1e7df"/><path d="M71 39L79 34L85 37L85 52L77 49L71 53Z" fill="#e4d8b1"/><path d="M53 73Q81 82 54 91M53 73L53 91M48 82L77 82" fill="none" stroke="#bee1c5"/>',
 'legacy_hourglass':'<path d="M30 18L99 18L99 31L30 31ZM30 99L99 99L99 112L30 112Z" fill="#dab56a"/><path d="M38 31L39 44Q44 56 61 64Q41 74 39 88L38 99L90 99L89 87Q85 74 68 64Q88 52 89 43L90 31Z" fill="#a9d7dd"/><path d="M44 36L83 36L66 57ZM46 94Q53 74 64 72Q76 76 82 94Z" fill="#d19ab7"/><path d="M64 61L64 72" fill="none" stroke="#eed287"/><path d="M31 33L31 98M97 33L97 98" fill="none" stroke="#e4c681"/>',
 'family_seal':'<path d="M38 18L57 14L70 25L84 17L99 32L97 48L111 61L104 80L112 93L93 108L77 102L64 115L48 105L30 109L22 91L15 74L25 60L19 43L34 34Z" fill="#bd6365"/><circle cx="65" cy="66" r="33" fill="#da8880"/><circle cx="65" cy="66" r="24" fill="#bd6365"/><path d="M48 76L48 51L56 51L56 60L61 60L61 46L70 46L70 60L75 60L75 51L83 51L83 77Z" fill="#edca82"/><path d="M60 77L60 68Q65 63 70 68L70 77Z" fill="#ae595c"/>',
 'guardian_knot':'<path d="M33 28L96 28L89 82L64 109L39 82Z" fill="#d8b266"/><path d="M45 39L84 39L80 77L64 93L48 77Z" fill="#638dac"/><path d="M22 51Q38 20 58 56Q80 94 107 60M22 72Q38 107 63 64Q84 28 105 49" fill="none" stroke="#e4d5a0" stroke-width="9"/><path d="M54 51L73 51L77 71L54 78L48 64Z" fill="#c78060"/>',
 'scholar_bookmark':'<path d="M26 25L99 25L99 104L26 104Z" fill="#b5a0d4"/><path d="M34 33L64 29L91 33L91 92L64 88L34 92Z" fill="#f0dfb4"/><path d="M64 30L64 89M44 45L55 44M72 44L81 45M44 59L55 58M72 58L81 59" fill="none"/><path d="M70 24Q112 14 89 61L69 81L62 61Z" fill="#87c9cc"/><path d="M63 82L91 23M69 66L85 62M76 49L94 43" fill="none" stroke="#53749c"/>',
 'trail_compass':'<path d="M54 16L75 16L75 30L54 30Z" fill="#dfb86a"/><circle cx="64" cy="68" r="41" fill="#d5b46e"/><circle cx="64" cy="68" r="32" fill="#8ab9a6"/><path d="M64 34L64 43M64 94L64 103M30 68L39 68M90 68L98 68" fill="none"/><path d="M83 42L74 77L47 91L55 58Z" fill="#ebe3bc"/><path d="M83 42L64 67L55 58Z" fill="#c37169"/><circle cx="64" cy="67" r="6" fill="#dfb86a"/>',
}
rows=[]
for key,body in symbols.items():
    svg='<svg xmlns="http://www.w3.org/2000/svg" width="128" height="128" viewBox="0 0 128 128"><g stroke="#302b36" stroke-width="5" stroke-linejoin="round" stroke-linecap="round">'+body+'</g></svg>\n'
    path=out/(key+'.svg');path.write_text(svg,encoding='utf-8')
    outline=re.sub(r'#[0-9a-fA-F]{6}', '#ffffff', svg).replace('stroke-width="5"','stroke-width="9"')
    op=out/(key+'-outline.svg');op.write_text(outline,encoding='utf-8')
    rows.append(dict(id=key,path=path.relative_to(root/'src/main/resources').as_posix(),sha256=hashlib.sha256(path.read_bytes()).hexdigest(),outline=op.relative_to(root/'src/main/resources').as_posix(),outlineSha256=hashlib.sha256(op.read_bytes()).hexdigest(),author='CapeKid',license='MIT',provenance='Original hand-authored vector geometry; scripts/author-heir-relics.py; no game-derived or external assets.'))
(root/'sheets/relic_art.json').write_text(json.dumps(rows,indent=2)+'\n',encoding='utf-8')
print('Authored ten original relic icons and ten selection outlines.')
