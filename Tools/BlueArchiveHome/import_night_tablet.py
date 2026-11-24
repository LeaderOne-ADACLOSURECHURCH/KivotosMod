"""Import supplied night layers and the filtered Plana export; never import 123.png."""
import json, hashlib, zipfile, io
from pathlib import Path
from PIL import Image
from prepare_textures import MOD, TOOLS, SOURCES, publish_pma

background = Path('C:/Users/Sunbinbin/Downloads/什厅之箱背景.zip')
plana = Path('C:/Users/Sunbinbin/Downloads/Plana.zip')
names = {'天空':'Sky','海面':'Sea','地面':'Ground','墙面':'Wall','柜子':'Lockers','黑板':'Board','光':'Light',
    '椅子后面':'ChairBack','桌子后面':'TableBack','椅子中':'ChairMid','桌子中':'TableMid','椅子前':'ChairFront','桌子前':'TableFront'}
originals = TOOLS/'NightTabletSources'
originals.mkdir(exist_ok=True)
records = []
with zipfile.ZipFile(background) as archive:
    for info in archive.infolist():
        if info.is_dir() or info.filename.startswith('__MACOSX/'): continue
        try: decoded = info.filename.encode('cp437').decode('utf8')
        except UnicodeError: decoded = info.filename
        stem = Path(decoded).stem
        name = names[stem]
        image = Image.open(io.BytesIO(archive.read(info))).convert('RGBA')
        image.save(originals/(name+'.png'))
        record = publish_pma(image, MOD/'Assets/Tablet/UI/ClassroomNight'/(name+'.png'))
        record.update(zipEntry=decoded,sourceSha256=hashlib.sha256(archive.read(info)).hexdigest())
        records.append(record)
if len(records) != 13: raise ValueError('Expected 13 night classroom layers')
export = TOOLS/'PlanaExport'
target = MOD/'Assets/Tablet/Home/Plana38'
target.mkdir(exist_ok=True)
atlas = (export/'Atlas/plana.atlas').read_text('utf8').lstrip('\ufeff\r\n')
if '123' in atlas: raise ValueError('Excluded background found in atlas')
(target/'plana.atlas.bin').write_text(atlas,'utf8')
(target/'plana.skel.bin').write_bytes((export/'Plana.skel').read_bytes())
image = Image.open(export/'Atlas/plana.png').convert('RGBA')
image.save(SOURCES/'plana38.png')
records.append(publish_pma(image,target/'plana.png'))
report = dict(backgroundArchive=str(background),backgroundSha256=hashlib.sha256(background.read_bytes()).hexdigest(),
    planaArchive=str(plana),planaSha256=hashlib.sha256(plana.read_bytes()).hexdigest(),
    excluded=['Plana/images/123.png','skeleton slot 123','skin attachment 123'],
    filteredSkeletonSha256=hashlib.sha256((target/'plana.skel.bin').read_bytes()).hexdigest(),files=records)
(TOOLS/'night_tablet_import.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),'utf8')
print('Imported 13 night layers and Plana; 123.png excluded from sources and packaged assets.')
