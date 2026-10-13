#!/usr/bin/env python3
"""Bundle the Jellyfin client and its locally served assets."""
import base64
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parent
PARTS = ROOT / 'parts'
OUTPUT = ROOT / '夜航私人影院.html'


def read_part(name: str) -> str:
    return (PARTS / name).read_text(encoding='utf-8')


def data_uri(name: str) -> str:
    return 'data:image/png;base64,' + base64.b64encode((ROOT / 'assets' / name).read_bytes()).decode('ascii')


def main() -> None:
    art = json.dumps({'hero': data_uri('blue-hour-radio.png')}, ensure_ascii=False)
    fonts = ''
    for weight, name in ((400, 'regular'), (700, 'bold')):
        encoded = base64.b64encode((ROOT / 'assets' / f'night-cjk-{name}.woff2').read_bytes()).decode('ascii')
        fonts += f'@font-face{{font-family:NightCJK;src:url(data:font/woff2;base64,{encoded}) format("woff2");font-weight:{weight};font-display:swap;unicode-range:U+2E80-2EFF,U+2F00-2FDF,U+3000-303F,U+31C0-31EF,U+3400-4DBF,U+4E00-9FFF,U+F900-FAFF,U+FE10-FE1F,U+FE30-FE4F,U+FF00-FFEF;}}'
    license_text = (ROOT / 'assets' / 'OFL-NotoSansCJK.txt').read_text()
    styles = fonts + '\n'.join(read_part(name + '.css') for name in ('gallery', 'login', 'player'))
    markup = '\n'.join(read_part(name + '.html') for name in ('gallery', 'login', 'player'))
    scripts = '\n'.join(read_part(name + '.js') for name in ('api', 'login', 'player', 'gallery'))
    scripts = re.sub(r'</script', r'<\\/script', scripts, flags=re.IGNORECASE)
    hls = re.sub(r'</script', r'<\\/script', (ROOT / 'assets' / 'hls.min.js').read_text(), flags=re.IGNORECASE)
    hls_license = (ROOT / 'assets' / 'LICENSE-hls.js').read_text()
    document = f'''<!doctype html>
<!-- Embedded subset fonts: © 2014-2021 Adobe. Night Signal CJK, modified from Noto Sans Mono CJK SC.\n{license_text} -->
<!-- hls.js 1.6.16 license:\n{hls_license} --><html lang="zh-CN"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><meta name="color-scheme" content="dark"><meta name="referrer" content="no-referrer"><meta name="description" content="夜航 NIGHT SIGNAL — 连接 Jellyfin 的私人影院。"><title>夜航 · NIGHT SIGNAL — 私人放映室</title><link rel="icon" href="data:,"><style>{styles}</style></head><body>{markup}<script>window.NIGHT_ART={art};</script><script>{hls}</script><script>{scripts}</script></body></html>'''
    OUTPUT.write_text(document, encoding='utf-8')
    print(json.dumps({'file': str(OUTPUT), 'bytes': OUTPUT.stat().st_size, 'cdn_dependencies': 0, 'backend': '/api/'}, ensure_ascii=False))


if __name__ == '__main__':
    main()
