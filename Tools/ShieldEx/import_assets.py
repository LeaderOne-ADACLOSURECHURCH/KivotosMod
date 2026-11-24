from pathlib import Path
from zipfile import ZipFile
from PIL import Image
import hashlib, json

root = Path(__file__).resolve().parents[2]
source = Path.home() / 'Downloads/images.zip'
out = root / 'Assets/Skills/BlackHole'
out.mkdir(parents=True, exist_ok=True)
names = {'光谱.png': 'Spectrum', '图层 2.png': 'Core',
         '图层 9.png': 'Band', '图层 10.png': 'Glow'}
manifest = []
with ZipFile(source) as archive:
    for entry in archive.infolist():
        if entry.is_dir():
            continue
        name = entry.filename.encode('cp437').decode('gbk').split('/')[-1]
        target = out / (names[name] + '.png')
        data = archive.read(entry)
        target.write_bytes(data)
        with Image.open(target) as image:
            manifest.append(dict(source=name, output=str(target.relative_to(root)),
                                 size=list(image.size), sha256=hashlib.sha256(data).hexdigest()))
# Use the AE canvas and the layer's real offset, including its half-pixel Y.
from make_ae_mask import make_mask
make_mask()
Path(__file__).with_name('assets.json').write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding='utf-8')
print('Imported four original PNGs and AE-aligned full-canvas clipping mask.')
from prepare_alpha import prepare
prepare()
