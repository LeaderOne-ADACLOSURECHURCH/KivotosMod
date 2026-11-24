"""Import the supplied 1x1 sheets without resizing/repainting any source pixels."""
from pathlib import Path
from PIL import Image, ImageDraw
import hashlib, io, json, wave, zipfile
import numpy as np

tools = Path(__file__).resolve().parent
root = tools.parents[1]
out = root / 'Content/Items/Vanity/Mika'
out.mkdir(parents=True, exist_ok=True)
downloads = Path('C:/Users/Sunbinbin/Downloads')
sprite_zip = downloads / '\u672a\u82b11X1.zip'
voice_zip = downloads / '\u672a\u82b1.zip'
sources = {}
with zipfile.ZipFile(sprite_zip) as z:
    for entry in z.infolist():
        if entry.is_dir() or entry.filename.startswith('__MACOSX/'):
            continue
        name = entry.filename.encode('cp437').decode('utf8') if not entry.flag_bits & 0x800 else entry.filename
        sources[Path(name).stem] = Image.open(io.BytesIO(z.read(entry))).convert('RGBA')

head = sources['\u5934'].copy()
arms_path = tools/'SourceArms.png'
arms = Image.open(arms_path).convert('RGBA')
assert arms.size == (360,224) and arms.getbbox(), 'A nonempty native composite arm atlas is required'
assets = {
    'MikaHead_Head': head,
    'MikaBoots_Legs': sources['\u817f'],
    'MikaTorso': sources['\u8eab\u5b50'],
    'MikaBackHair': sources['\u540e\u53d1'],
    'MikaSideLocks': sources['\u9b13\u53d1'],
    'MikaHalo': sources['\u5149\u73af'],
    'MikaDecorWings': sources['\u7fc5\u8180'],
    'MikaFlight': sources['\u7fc5\u8180\uff08\u98de\u884c\uff09'],
    # tML's native composite framing drives the supplied foreground/rear arms.
    # The torso itself remains in its separate 40x56 source strip.
    'MikaBody_Body': arms,
}
for name, image in assets.items():
    image.save(out / (name + '.png'))
for name, src in [('MikaHead', head), ('MikaBody', assets['MikaTorso']), ('MikaBoots', assets['MikaBoots_Legs'])]:
    frame = src.crop((0, 0, src.width, 56))
    frame.crop(frame.getbbox()).save(out / (name + '.png'))

def character(frame=0, flight=None, flip=False):
    # Same integer pivots as the runtime; body-frame top-left is (32,32).
    canvas = Image.new('RGBA', (104, 112))
    wing = assets['MikaFlight'] if flight is not None else assets['MikaDecorWings']
    height = 68 if flight is not None else 74
    row = flight if flight is not None else frame
    canvas.alpha_composite(assets['MikaBackHair'].crop((0, frame*56, 40, (frame+1)*56)), (32, 32))
    # Restore the reference's native body offset within each wide canvas.
    canvas.alpha_composite(wing.crop((0, row*height, 63, (row+1)*height)), (19, 32+56-height))
    # CreateCompositeData uses front (2,0), rear (2,2) for body frame 0.
    # This is a zero-rotation material preview, not an in-game render.
    canvas.alpha_composite(arms.crop((80,112,120,168)), (32,32))
    for key in ['MikaBoots_Legs', 'MikaTorso', 'MikaHead_Head']:
        canvas.alpha_composite(assets[key].crop((0, frame*56, 40, (frame+1)*56)), (32, 32))
    canvas.alpha_composite(arms.crop((80,0,120,56)), (32,32))
    canvas.alpha_composite(assets['MikaSideLocks'].crop((0,frame*56,40,(frame+1)*56)), (32,32))
    canvas.alpha_composite(assets['MikaHalo'], (19, 14))
    return canvas.transpose(Image.Transpose.FLIP_LEFT_RIGHT) if flip else canvas

thumb = character()
thumb_dir = root/'Assets/Tablet/Students/Mika'
thumb_dir.mkdir(parents=True, exist_ok=True)
thumb.crop(thumb.getbbox()).save(thumb_dir/'Thumbnail.png')
preview = Image.new('RGBA', (7*208, 224), (44, 58, 74, 255))
draw = ImageDraw.Draw(preview)
for i in range(7):
    pic = character(flight=i-1 if 1 <= i <= 5 else None, flip=i==6)
    preview.alpha_composite(pic.resize((208,224), Image.Resampling.NEAREST), (i*208,0))
    draw.text((i*208+5,5), 'idle flipped' if i==6 else f'fly {i-1}' if i else 'idle', fill='white')
preview.save(tools/'mika_preview.png')

audio_dir = root/'Assets/Sounds/Students/Mika'
audio_dir.mkdir(parents=True, exist_ok=True)
voices = []
with zipfile.ZipFile(voice_zip) as z:
    for i, entry in enumerate(f for f in z.infolist() if not f.is_dir() and f.filename.lower().endswith('.wav')):
        data = z.read(entry)
        target = audio_dir/f'Hurt{i+1}.wav'
        target.write_bytes(data)
        with wave.open(io.BytesIO(data)) as wav:
            samples = np.frombuffer(wav.readframes(wav.getnframes()), dtype='<i2').astype(float) / 32768
            mono = samples.reshape(-1, wav.getnchannels()).mean(axis=1)
            active = mono[np.abs(mono) > .005]
            voices.append(dict(path=target.relative_to(root).as_posix(), seconds=wav.getnframes()/wav.getframerate(),
                peakDb=float(20*np.log10(np.max(np.abs(mono)))), activeRmsDb=float(20*np.log10(np.sqrt(np.mean(active**2)))),
                heavy=i+1 in (2,4), sha256=hashlib.sha256(data).hexdigest()))
report = dict(spriteZipSha256=hashlib.sha256(sprite_zip.read_bytes()).hexdigest(),
    armsSourceSha256=hashlib.sha256(arms_path.read_bytes()).hexdigest(),
    voiceZipSha256=hashlib.sha256(voice_zip.read_bytes()).hexdigest(), sourceScale=1,
    assets={k:dict(size=v.size, sha256=hashlib.sha256((out/(k+'.png')).read_bytes()).hexdigest()) for k,v in assets.items()}, voices=voices)
(tools/'import_manifest.json').write_text(json.dumps(report,indent=2),encoding='utf8')
print('Imported Mika 1x1 sprites and 4 original PCM voices; preview/manifest saved.')
