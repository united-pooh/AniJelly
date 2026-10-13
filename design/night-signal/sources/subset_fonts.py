#!/usr/bin/env python3
"""Requires fonttools[woff] and supplied original Noto CJK fonts; changes only assets."""
import argparse
from pathlib import Path
import hashlib
import json
from fontTools import subset
from fontTools.ttLib import TTFont

ROOT = Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser(description='Subset the supplied Noto Sans Mono CJK SC 2.004 fonts for the Night Signal Jellyfin client.')
parser.add_argument('--regular-font', type=Path, required=True, help='Original Regular OTF file (not modified)')
parser.add_argument('--bold-font', type=Path, required=True, help='Original Bold OTF file (not modified)')
args = parser.parse_args()
sources = {'Regular': args.regular_font, 'Bold': args.bold_font}
html_parts = sorted((ROOT / 'parts').glob('*.html'))
script_parts = sorted((ROOT / 'parts').glob('*.js'))
assert html_parts and script_parts, 'Expected HTML and JavaScript client modules'
parts = sorted([*html_parts, *script_parts])
contents = {path: path.read_bytes() for path in parts}
punctuation = ' ，。！？；：、·“”‘’（）《》〈〉【】〔〕［］｛｝「」『』—–…‧﹏～／＼｜＋－＝％＆＠＃＊．　\u00a0'
requested = {ord(character) for data in contents.values() for character in data.decode('utf-8') if ord(character) >= 32}
requested.update(map(ord, punctuation))
manifest = {
    'fontToolsVersion': __import__('fontTools').__version__,
    'sourceFamily': 'Noto Sans Mono CJK SC',
    'sourceVersion': '2.004',
    'subsetFamily': 'Night Signal CJK',
    'license': 'SIL Open Font License 1.1',
    'officialLicenseUrl': 'https://raw.githubusercontent.com/notofonts/noto-cjk/main/Sans/LICENSE',
    'copyright': '© 2014-2021 Adobe (http://www.adobe.com/).',
    'inputFiles': [{'path': str(path.relative_to(ROOT)), 'sha256': hashlib.sha256(data).hexdigest()} for path, data in contents.items()],
    'requestedCodepointCount': len(requested),
    'requestedCodepoints': [f'U+{value:04X}' for value in sorted(requested)],
    'fonts': [],
}
for style in ('Regular', 'Bold'):
    source = sources[style]
    source_sha = hashlib.sha256(source.read_bytes()).hexdigest()
    font = TTFont(source, recalcTimestamp=False)
    assert font['name'].getDebugName(1) == 'Noto Sans Mono CJK SC', 'Expected Noto Sans Mono CJK SC'
    assert '2.004' in font['name'].getDebugName(5), 'Expected source version 2.004'
    assert font['OS/2'].usWeightClass == (400 if style == 'Regular' else 700), 'Source weight does not match argument'
    available = set(font.getBestCmap())
    covered = requested & available
    missing = requested - available
    assert not any(0x3400 <= value <= 0x9fff for value in missing)
    options = subset.Options()
    options.name_IDs = ['*']
    options.name_languages = ['*']
    options.layout_features = ['*']
    options.recalc_timestamp = False
    subsetter = subset.Subsetter(options=options)
    subsetter.populate(unicodes=covered)
    subsetter.subset(font)
    family = 'Night Signal CJK'
    postscript_name = f'NightSignalCJK-{style}'
    names = {1: family, 2: style, 3: f'2.004;{postscript_name};NightSignalSubset',
             4: f'{family} {style}', 6: postscript_name, 16: family, 17: style}
    for name_id, value in names.items():
        for record in list(font['name'].names):
            if record.nameID == name_id:
                font['name'].setName(value, name_id, record.platformID, record.platEncID, record.langID)
        font['name'].setName(value, name_id, 3, 1, 0x409)
    if 'CFF ' in font:
        cff = font['CFF '].cff
        cff.fontNames = [postscript_name]
        top = cff.topDictIndex[0]
        top.FullName = f'{family} {style}'
        top.FamilyName = family
    font.flavor = 'woff2'
    output = ROOT / 'assets' / f'night-cjk-{style.lower()}.woff2'
    font.save(output)
    checked = TTFont(output)
    assert covered <= set(checked.getBestCmap())
    assert checked['name'].getDebugName(1) == family
    assert 'Adobe' in checked['name'].getDebugName(0)
    assert 'Open Font License' in checked['name'].getDebugName(13)
    assert hashlib.sha256(source.read_bytes()).hexdigest() == source_sha
    manifest['fonts'].append({
        'file': output.name, 'bytes': output.stat().st_size,
        'sha256': hashlib.sha256(output.read_bytes()).hexdigest(),
        'sourceFile': source.name, 'sourceSha256': source_sha,
        'weight': checked['OS/2'].usWeightClass,
        'familyName': checked['name'].getDebugName(1),
        'postScriptName': checked['name'].getDebugName(6),
        'glyphCount': checked['maxp'].numGlyphs,
        'cmapCodepointCount': len(checked.getBestCmap()),
        'missingSourceGlyphs': [{'character': chr(value), 'codepoint': f'U+{value:04X}'} for value in sorted(missing)],
        'sourceUnchanged': True,
    })
for path, data in contents.items():
    assert path.read_bytes() == data, f'Input changed during subsetting: {path}'
manifest['licenseSha256'] = hashlib.sha256((ROOT / 'assets' / 'OFL-NotoSansCJK.txt').read_bytes()).hexdigest()
(ROOT / 'assets' / 'night-cjk-manifest.json').write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + '\n')
print(json.dumps({key: value for key, value in manifest.items() if key not in ('requestedCodepoints', 'inputFiles')}, ensure_ascii=False))
