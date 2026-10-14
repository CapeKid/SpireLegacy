"""Checked-in authoring contracts; validate before interpreting or writing design data."""
import json
import math
import re
import hashlib
import xml.etree.ElementTree as ET
import wave
from PIL import Image
from pathlib import Path, PurePosixPath


class DesignError(ValueError):
    pass


def read_json(path):
    def unique(pairs):
        result = {}
        for key, value in pairs:
            if key in result:
                raise DesignError(f'{path.name}: duplicate JSON property {key}')
            result[key] = value
        return result
    try:
        def invalid_number(value):
            raise DesignError(f'{path.name}: invalid number {value}')
        return json.loads(path.read_text(encoding='utf-8-sig'), object_pairs_hook=unique, parse_constant=invalid_number)
    except (OSError, UnicodeError, json.JSONDecodeError) as error:
        raise DesignError(f'{path.name}: {error}') from error


def check(value, spec, location, errors):
    kind = type(value).__name__
    if kind not in spec['types']:
        errors.append(f'{location}: expected {"/".join(spec["types"])}, got {kind}')
        return
    if kind == 'dict':
        for key in spec['required']:
            if key not in value:
                errors.append(f'{location}.{key}: missing required field')
        for key, child in value.items():
            if key not in spec['fields']:
                errors.append(f'{location}.{key}: unknown field')
            else:
                check(child, spec['fields'][key], f'{location}.{key}', errors)
    elif kind == 'list':
        for index, child in enumerate(value):
            check(child, spec['items'], f'{location}[{index}]', errors)
    elif kind in {'int', 'float'}:
        if not math.isfinite(value) or not spec.get('min', -10000) <= value <= spec.get('max', 10000):
            errors.append(f'{location}: number outside authoring bounds {spec.get("min")}..{spec.get("max")}')
    elif kind == 'str':
        if not value.strip():
            errors.append(f'{location}: empty text')
        if 'pattern' in spec and not re.fullmatch(spec['pattern'], value):
            errors.append(f'{location}: invalid identifier/hash {value!r}')
        if value not in spec.get('enum', spec.get('enumStrings', [value])):
            errors.append(f'{location}: unsupported value {value!r}')


def resource_path(root, value, location):
    path = PurePosixPath(value)
    if '\\' in value or ':' in value or path.is_absolute() or any(p in {'', '.', '..'} for p in value.split('/')):
        raise DesignError(f'{location}: unsafe resource path {value!r}')
    base = (root / 'src/main/resources').resolve()
    target = (base / path).resolve()
    if not target.is_relative_to(base):
        raise DesignError(f'{location}: resource escapes asset root')
    return target


def load_sheets(root):
    contracts = read_json(root / 'scripts/design-schema.json')
    paths = {p.stem: p for p in (root / 'sheets').glob('*.json')}
    errors = [f'{name}: missing sheet' for name in contracts.keys() - paths.keys()]
    errors += [f'{name}: undeclared sheet' for name in paths.keys() - contracts.keys()]
    tables = {}
    for name in sorted(paths.keys() & contracts.keys()):
        try:
            rows = read_json(paths[name])
        except DesignError as error:
            errors.append(str(error)); continue
        if not isinstance(rows, list) or not rows:
            errors.append(f'{name}: expected nonempty row array'); continue
        ids = set()
        for index, row in enumerate(rows):
            label = row.get('id', f'row {index}') if isinstance(row, dict) else f'row {index}'
            location = f'{name}.{label}'
            check(row, contracts[name], location, errors)
            if not isinstance(row, dict): continue
            key = row.get('id')
            if isinstance(key, str):
                if key in ids: errors.append(f'{location}: duplicate ID')
                ids.add(key)
            for field in ('path', 'outline', 'output', 'sourceFile'):
                if isinstance(row.get(field), str):
                    try: resource_path(root, row[field], f'{location}.{field}')
                    except DesignError as error: errors.append(str(error))
            if isinstance(row.get('death'), dict) and isinstance(row['death'].get('path'), str):
                try: resource_path(root, row['death']['path'], f'{location}.death.path')
                except DesignError as error: errors.append(str(error))
        tables[name] = rows
    if errors:
        raise DesignError('\n'.join(errors))
    return tables


def validate_assets(root, tables):
    """Check every manifest, including nested atlases, before legacy visual checks."""
    errors = []
    claimed = {}
    for table in ('card_art', 'ui_art', 'character_art', 'icon_art', 'relic_art', 'potion_art', 'presentation_art'):
        for row in tables[table]:
            parts = [(row, f'{table}.{row["id"]}')]
            if isinstance(row.get('death'), dict): parts.append((row['death'], f'{table}.{row["id"]}.death'))
            for part, location in parts:
                for field, hashfield in [('path', 'sha256'), ('outline', 'outlineSha256')]:
                    if field not in part: continue
                    path = resource_path(root, part[field], f'{location}.{field}')
                    if part[field].casefold() in claimed:
                        errors.append(f'{location}.{field}: resource path collision with {claimed[part[field].casefold()]}')
                    claimed[part[field].casefold()] = location
                    try:
                        data = path.read_bytes()
                        if hashlib.sha256(data).hexdigest() != part[hashfield]:
                            errors.append(f'{location}.{hashfield}: artwork hash mismatch')
                        if path.suffix == '.svg':
                            text = data.decode('utf-8-sig')
                            svg = ET.fromstring(text)
                            if '<!DOCTYPE' in text.upper() or '<!ENTITY' in text.upper():
                                errors.append(f'{location}: SVG declarations are not self-contained')
                            for element in svg.iter():
                                if element.tag.split('}')[-1] in {'image','text','script','foreignObject','style'}:
                                    errors.append(f'{location}: unsupported SVG element')
                                for attr, value in element.attrib.items():
                                    if attr.split('}')[-1].startswith('on') or attr.split('}')[-1] in {'href','src'} or re.search(r'url\s*\(',value,re.I):
                                        errors.append(f'{location}: external/active SVG attribute {attr}')
                        elif path.suffix == '.png':
                            with Image.open(path) as image:
                                if image.width*image.height>16_000_000:
                                    raise ValueError('raster exceeds the authoring pixel budget')
                                image.load()
                                if 'width' in part and image.size != (part['width'],part['height']):
                                    errors.append(f'{location}: wrong image dimensions')
                                if 'regions' in part:
                                    regions = part['regions']
                                    count = 4 if location.endswith('.death') else 12
                                    if len(regions)!=count or len(part['feet'])!=count or ('insets' in part and len(part['insets'])!=count):
                                        errors.append(f'{location}: atlas/frame alignment mismatch')
                                    for index, region in enumerate(regions):
                                        if len(region)!=4 or any(type(v)!=int for v in region):
                                            errors.append(f'{location}.regions[{index}]: expected four integer coordinates');continue
                                        x,y,w,h=region
                                        if x<0 or y<0 or w<=0 or h<=0 or x+w>image.width or y+h>image.height:
                                            errors.append(f'{location}.regions[{index}]: outside atlas')
                                        if index<len(part['feet']) and not 0<=part['feet'][index]<=h:
                                            errors.append(f'{location}.feet[{index}]: outside frame')
                        elif path.suffix == '.wav':
                            with wave.open(str(path),'rb') as wav:
                                if wav.getparams()[:3]!=(1,2,22050) or not 4000<=wav.getnframes()<=16000:
                                    errors.append(f'{location}: wrong PCM format')
                                elif len(wav.readframes(wav.getnframes()))!=wav.getnframes()*2:
                                    errors.append(f'{location}: truncated PCM data')
                        elif path.suffix not in {'.wav','.tres','.tscn'}:
                            errors.append(f'{location}: unsupported asset extension {path.suffix}')
                    except (OSError, ValueError, ET.ParseError, wave.Error, EOFError, Image.DecompressionBombError) as error:
                        errors.append(f'{location}.{field}: invalid/missing asset: {error}')
    if errors: raise DesignError('\n'.join(errors))
