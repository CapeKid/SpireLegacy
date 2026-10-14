"""Author original vector icons. No game images, fonts, external references or extraction."""
import hashlib
import json
from html import escape
from pathlib import Path
from design_schema import load_sheets

ROOT = Path(__file__).resolve().parents[1]
tables = load_sheets(ROOT)
OUT = ROOT / 'src/main/resources/heir/icons'
OUT.mkdir(parents=True, exist_ok=True)

def path(d, fill='#eed095'):
    return f'<path d="{d}" fill="{fill}"/>'

def circle(x, y, radius, fill='#eed095'):
    return f'<circle cx="{x}" cy="{y}" r="{radius}" fill="{fill}"/>'

def line(x1, y1, x2, y2, color='#eed095'):
    return f'<path d="M{x1} {y1}L{x2} {y2}" fill="none" stroke="{color}"/>'

def symbol(name):
    return {
        'heart': path('M64 99C12 67 21 27 45 32Q57 34 64 46Q75 27 91 33C119 46 100 77 64 99', '#ed697b'),
        'shield': path('M30 29Q64 16 98 29L93 69Q85 90 64 102Q39 88 35 70Z', '#7ec5ed') + path('M64 35L64 84', 'none'),
        'sword': path('M69 22L89 20L87 40L53 78L43 67Z', '#c6e5e9') + path('M35 62L63 86L57 93L29 70Z', '#eac36a') + path('M41 81L28 95L22 88L34 74', '#be7a63'),
        'book': path('M25 30Q48 22 64 37Q80 22 103 30L103 89Q79 81 64 98Q47 81 25 89Z', '#d19ee5') + line(64,37,64,95,'#342535') + line(34,44,52,48) + line(75,48,94,44) + line(34,59,52,63) + line(75,63,94,59),
        'bolt': path('M72 18L35 67L60 64L52 108L94 53L67 57Z', '#f6d668'),
        'eye': path('M19 64Q64 22 109 64Q64 103 19 64', '#f0e4c6') + circle(64,64,18,'#76c6e2') + circle(64,64,6,'#302432'),
        'coin': circle(64,64,37,'#efc461') + circle(64,64,25,'#d79543') + path('M64 45L76 64L64 83L52 64Z', '#ffed99'),
        'chest': path('M23 56Q23 27 64 27Q105 27 105 56L105 94L23 94Z', '#c88958') + path('M23 59L105 59M42 31L42 94M86 31L86 94', 'none') + path('M57 54L71 54L71 74L57 74Z', '#efcf70'),
        'leaf': path('M28 93Q14 38 100 25Q110 101 28 93', '#80c38a') + line(29,94,85,43,'#315842') + line(48,73,47,47,'#315842') + line(62,62,86,65,'#315842'),
        'skull': path('M32 78C5 16 120 10 98 76L82 80L82 98L45 98L45 81Z', '#eddfc1') + circle(47,59,10,'#302432') + circle(82,59,10,'#302432') + path('M64 68L57 81L70 81Z', '#302432') + line(56,89,56,98,'#302432') + line(70,89,70,98,'#302432'),
        'cloud': path('M28 84C1 72 20 44 39 48C40 14 90 19 91 50C120 49 128 86 99 88Z', '#b2d6e9'),
        'boot': path('M35 27L69 27L72 67Q106 67 106 88L106 97L29 97L29 77L36 70Z', '#be855d') + line(35,40,68,40) + line(32,89,105,89),
        'wing': path('M21 92Q15 32 104 27L90 47L62 60L96 53L82 74L52 81L79 77L66 96Z', '#d2e6e5'),
        'person': circle(64,35,15,'#edc18d') + path('M42 59Q64 49 86 59L94 84L79 90L73 74L74 104L55 104L56 74L47 91L33 83Z', '#94a9da'),
        'mushroom': path('M50 63L78 63L82 100L46 100Z', '#ead9aa') + path('M18 69C25 5 103 5 112 69Z', '#de7771') + circle(44,48,8,'#efdfbb') + circle(80,39,9,'#efdfbb'),
        'palette': path('M92 100C39 122 8 59 33 32C58 3 119 28 106 61Q103 71 89 66Q63 66 77 89Z', '#edcf97') + circle(41,44,7,'#e87579') + circle(66,32,7,'#e9c664') + circle(89,44,7,'#80c495') + circle(36,68,7,'#8fcce5'),
        'crown': path('M26 36L45 55L64 23L83 55L103 36L95 91L34 91Z', '#e9c664') + circle(64,76,7,'#de7771'),
        'star': path('M64 19L78 47L109 52L87 74L91 105L64 90L37 105L42 74L19 52L50 47Z', '#f0d27b'),
        'bow': path('M40 22Q117 64 40 105L49 94Q95 64 49 33Z', '#c88958') + line(40,22,40,105) + line(27,64,105,64) + path('M97 54L110 64L97 74Z', '#c6e5e9'),
        'wand': path('M28 94L75 42L84 50L37 101Z', '#bf865c') + path('M84 22L104 39L86 58L68 39Z', '#91cfe6') + line(57,20,63,26) + line(108,65,115,72),
        'portal': '<ellipse cx="64" cy="64" rx="34" ry="45" fill="#967ed1"/>' + '<ellipse cx="64" cy="64" rx="20" ry="31" fill="#342b53"/>' + path('M31 61L98 61L89 52M98 61L89 71', 'none'),
        'hand': path('M34 85L20 65Q16 54 25 54L40 66L35 29Q36 16 45 26L52 59L50 21Q54 10 61 23L64 58L69 24Q76 15 79 29L76 63L87 41Q96 33 99 45L88 89Q75 109 48 103Z', '#edc18d'),
        'snow': line(64,23,64,105,'#b4def2') + line(29,44,99,84,'#b4def2') + line(29,84,99,44,'#b4def2') + path('M48 31L64 44L80 31M48 97L64 84L80 97M31 60L48 55L45 38M96 68L80 73L83 90M31 68L48 73L45 90M96 60L80 55L83 38', 'none'),
        'pumpkin': path('M62 32L66 19L78 19L72 36', '#80c38a') + '<ellipse cx="64" cy="70" rx="44" ry="35" fill="#e79d56"/>' + path('M38 58L52 65L34 70ZM91 58L75 65L95 70ZM41 83L51 77L62 85L73 77L86 84L76 94L54 94Z', '#342535'),
        'glass': path('M35 24L95 24L85 63L69 72L69 94L87 99L39 99L58 94L58 72L44 63Z', '#add6e8'),
        'mask': path('M22 28Q64 43 106 28L100 73Q64 111 28 73Z', '#d19ee5') + path('M36 57Q45 43 56 57M74 57Q85 43 94 57M43 72Q65 94 86 72', 'none'),
        'bone': path('M30 34C7 24 13 55 27 54L76 86C71 107 107 112 101 90C122 78 94 63 87 76L41 45C51 24 28 17 30 34', '#eddfc1'),
        'hourglass': path('M35 23L94 23L86 44L72 64L86 81L94 105L35 105L42 83L57 64L43 44Z', '#abd3e3') + path('M43 34L85 34L64 57ZM45 94L64 76L83 94Z', '#efc461'),
        'fang': path('M24 32Q64 51 104 32L91 72L80 97L74 54L54 54L46 97L34 72Z', '#eddfc1'),
        'map': path('M21 33L48 24L79 34L107 24L107 94L79 103L48 93L21 103Z', '#edcf97') + line(48,25,48,93,'#a67957') + line(79,34,79,103,'#a67957') + path('M33 82L51 65L68 73L94 47', 'none') + circle(94,47,6,'#df7374'),
        'flame': path('M64 16Q73 42 95 43Q116 92 65 106Q10 94 34 56L43 74Q62 60 64 16', '#e69563') + path('M65 63Q92 84 66 100Q43 90 65 63', '#efcf76'),
    }[name]

def badge(mark):
    art = {'plus': path('M93 77L93 89L81 89L81 101L93 101L93 113L105 113L105 101L117 101L117 89L105 89L105 77Z','#f4db91'),
           'minus': path('M81 89L117 89L117 101L81 101Z','#ec8181'),
           'cross': path('M82 81L115 112M115 81L82 112','none'),
           'two': circle(88,91,5,'#efcf76')+circle(108,101,5,'#efcf76'),
           'burst': path('M99 76L104 87L117 87L109 97L113 111L100 105L86 111L90 97L81 87L95 87Z','#efcf76')}
    return '<g stroke-width="4">'+circle(99,96,24,'#514164')+art[mark]+'</g>'

def compose(name, mark=None, transform=None, overlay=''):
    glyph = symbol(name)
    if transform:
        glyph = f'<g transform="{transform}">{glyph}</g>'
    return glyph + overlay + (badge(mark) if mark else '')

# Every icon expresses the trait's idea with original geometric silhouettes.
ICONS = {
    'nocolor': compose('palette',overlay=path('M19 104L105 19','none')),
    'nohealthbar': compose('heart',overlay=path('M19 104L105 19','none')),
    'easybreakables': compose('glass',overlay=path('M67 26L57 48L74 58L56 76','none')),
    'small': compose('person','minus','translate(25 25) scale(.6)'),
    'playerknockedlow': compose('shield',overlay=path('M64 43L64 79M49 63L64 79L79 63','none')),
    'playerknockedfar': compose('person',overlay=path('M18 91L48 91M36 80L48 91L36 102','none')),
    'disposition': compose('leaf'),
    'large': compose('person','plus','translate(-7 -7) scale(1.1)'),
    'fart': compose('cloud',overlay=path('M29 93Q37 104 46 94M60 95Q68 109 76 95','none')),
    'enemyknockedlow': compose('sword','minus'),
    'breakpropsformana': compose('glass','plus',overlay=path('M52 36L69 57L50 69','none')),
    'onehitdeath': compose('skull',overlay=path('M24 28L108 106','none')),
    'cantattack': compose('sword','cross'),
    'omnidash': compose('shield',overlay=path('M21 83Q39 103 63 86Q82 72 105 86','none')),
    'highjump': compose('boot',overlay=path('M87 60L87 22M74 35L87 22L100 35','none')),
    'gaindownstrike': compose('wing',overlay=path('M91 76L91 108M80 97L91 108L102 97','none')),
    'weapon': compose('sword','plus'),
    'magic': compose('book'),
    'enemyknockedfar': compose('sword',overlay=path('M80 81L111 81M99 70L111 81L99 92','none')),
    'longercd': compose('hourglass','minus'),
    'health': compose('heart','plus'),
    'strength': compose('hand','plus'),
    'bonusmagicstrength': compose('book','minus'),
    'fmffan': compose('shield',overlay=path('M43 49L83 76L42 91Z','#dd889c')),
    'noenemyhealthbar': compose('eye','cross'),
    'manacostanddamageup': compose('bolt','plus'),
    'revealallchests': compose('chest',overlay=circle(65,39,8,'#abd3e3')),
    'bounceterrain': compose('boot',overlay=path('M18 88Q35 107 54 89Q75 64 107 83','none')),
    'lowerstoreprice': compose('coin','minus'),
    'fakeselfdamage': compose('mask'),
    'oldyellowtint': compose('hourglass',overlay=circle(64,64,8,'#d79543')),
    'enemiescensored': compose('eye',overlay=path('M19 52L109 52L109 76L19 76Z','#302432')),
    'enemiesblackfill': compose('person',overlay=path('M45 51L84 51L91 86L73 86L73 102L53 102L53 86L36 86Z','#302432')),
    'lowergravity': compose('bone',overlay=path('M94 49L94 22M83 34L94 22L105 34','none')),
    'bluronhit': compose('eye',overlay=path('M15 40L36 40M94 91L115 91M13 91L29 91','none')),
    'colortrails': compose('palette','burst'),
    'nomeat': compose('leaf',overlay=path('M38 73L57 88L89 51','none')),
    'smallhitbox': compose('heart',overlay=path('M24 37L24 22L39 22M89 22L104 22L104 37M104 90L104 105L89 105M39 105L24 105L24 90','none')),
    'cheeronkills': compose('crown','burst'),
    'superfart': compose('cloud','two'),
    'vampire': compose('fang'),
    'mapreveal': compose('map'),
    'noimmunitywindow': compose('heart','minus'),
    'nomanacap': compose('bolt',overlay=path('M22 83C4 62 43 59 62 80C85 103 118 101 111 82C99 63 72 65 62 80C49 99 28 99 22 83','none')),
    'megahealth': compose('heart',overlay=path('M46 49L56 60L64 40L76 60L85 49L81 76L49 76Z','#efcf76')),
    'itemsgoflying': compose('hand','minus'),
    'manafromhurt': compose('bolt',overlay=path('M26 38L41 48L23 59','none')),
    'bonuschestgold': compose('chest','plus'),
    'superhealer': compose('heart','two'),
    'twinrelics': compose('star','two'),
    'mushroomgrow': compose('mushroom'),
    'youareblue': compose('person',overlay=path('M45 62L83 62L83 85L73 85L73 102L55 102L55 85L45 85Z','#80b7e2')),
    'skillcritsonly': compose('shield','burst'),
    'disarmonhurt': compose('hand',overlay=path('M77 27L66 50L88 47L74 70','#efcf76')),
    'explosivechests': compose('chest','burst'),
    'explosiveenemies': compose('skull','burst'),
    'halloweenholiday': compose('pumpkin'),
    'christmasholiday': compose('snow'),
    'randomizekit': compose('book',overlay=path('M31 46L48 46L43 38M48 46L43 54M97 76L80 76L85 68M80 76L85 84','none')),
    'superspinkick': compose('boot',overlay=path('M88 39Q111 59 95 80M88 75L95 80L105 77','none')),
    'cointimer': compose('coin',overlay=path('M64 43L64 65L81 74','none')),
    'projectilesnowalls': compose('portal'),
    'antique': compose('star',overlay=circle(64,64,11,'#a791c5')),
}

rows = []
for table, kind in [('traits', 'trait'), ('classes', 'class')]:
    for row in tables[table]:
        key = row['id']
        art = ICONS[key] if kind == 'trait' else compose({'knight':'sword', 'mage':'wand', 'ranger':'bow'}[key])
        svg = '<svg xmlns="http://www.w3.org/2000/svg" width="128" height="128" viewBox="0 0 128 128"><title>'+escape(row['name'])+'</title><g stroke="#302432" stroke-width="5" stroke-linejoin="round" stroke-linecap="round"><rect x="5" y="5" width="118" height="118" rx="27" fill="#514164"/><rect x="10" y="10" width="108" height="108" rx="23" fill="none" stroke="#c9a775" stroke-width="3"/>'+art+'</g></svg>\n'
        target = OUT / f'{kind}-{key}.svg'
        target.write_text(svg, encoding='utf-8', newline='\n')
        rows.append(dict(id=f'{kind}_{key}', subjectId=key, kind=kind, name=row['name'], path='heir/icons/'+target.name, sha256=hashlib.sha256(target.read_bytes()).hexdigest(), width=128, height=128, author='CapeKid', license='MIT', provenance='Original hand-authored SVG geometry; scripts/author-heir-icons.py; no imported artwork'))
(ROOT/'sheets/icon_art.json').write_text(json.dumps(rows,indent=2)+'\n',encoding='utf-8', newline='\n')
print(f'Authored {len(rows)} original vector icons.')
