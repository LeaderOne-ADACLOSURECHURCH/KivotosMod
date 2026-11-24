"""Import the user's Sora 3.8 export without rewriting skeleton data."""
import hashlib
import json
from pathlib import Path
import sys
from PIL import Image
from prepare_textures import MOD, SOURCES, TOOLS, publish_pma

source = Path(sys.argv[1] if len(sys.argv) > 1 else 'E:/Spine3.8.75Pro/sora2')
target = MOD / 'Assets/Tablet/Home/Sora38'
target.mkdir(parents=True, exist_ok=True)
SOURCES.mkdir(exist_ok=True)
(target / 'sora.atlas.bin').write_text((source / 'Sora.atlas').read_text('utf8').replace('Sora.png', 'sora.png'), 'utf8')
(target / 'sora.skel.bin').write_bytes((source / 'Sora.skel').read_bytes())
image = Image.open(source / 'Sora.png').convert('RGBA')
image.save(SOURCES / 'sora38.png')
texture = publish_pma(image, target / 'sora.png')
report = dict(source=str(source.resolve()), skeletonUnmodified=True,
    files=[dict(name=p.name, bytes=p.stat().st_size, sha256=hashlib.sha256(p.read_bytes()).hexdigest())
        for p in [source / 'Sora.atlas', source / 'Sora.skel', source / 'Sora.png']],
    outputFolder=target.relative_to(MOD).as_posix(), atlasPageRename={'Sora.png': 'sora.png'},
    runtime='KivotosMod.Runtime.Spine38', version='3.8.75', animations=['animation', 'blink'], texture=texture)
(TOOLS / 'sora38_import.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), 'utf8')
print('Imported Sora:', report['outputFolder'])
